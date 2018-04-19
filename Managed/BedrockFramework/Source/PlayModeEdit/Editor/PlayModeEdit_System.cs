/********************************************************           
BEDROCKFRAMEWORK : https://github.com/GainDeveloper/BedrockFramework

We cache all PlayModeEdits recorded Objects before we return to EditMode.
Once back in EditMode we assign recorded Objects the serialized data.
- Basic CSharp types of components are serialized.
- Destruction of PlayModeEdit GameObjects is supported.
- Instation of PlayModeEdit Prefabs is supported.
- Parenting between PlayModeEdit GameObjects is supported.

Can not record references to Objects within the scene. Care must be taken to ensure recorded components do not have any scene references as these will break.
********************************************************/

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace BedrockFramework.PlayModeEdit
{
    public static class PlayModeEdit_System
    {
        private static Dictionary<int, string> _cachedObjects;
        private static Dictionary<int, int> _cachedObjectsParentTransformsInstanceIDs;

        private static List<PrefabInstance> _newPrefabInstances;

        struct PrefabInstance {
            public GameObject prefab;
            public string prefabGameObjectJSON;
            public int parentInstanceID, transformInstanceID;
            public Dictionary<string, string> typeComponents; //TODO: Support multiple components of the same type.
        }

        public struct ObjectCacheInstance
        {
            public int cahedID;
            public Object cachedObject;
        }

        static int TransformToParentInstanceID(Transform transform)
        {
            int parentCachedID = 0;
            if (transform.parent != null)
            {
                PlayModeEdit parentPlayMode = transform.parent.GetComponent<PlayModeEdit>();
                if (parentPlayMode == null)
                {
                    Debug.LogWarning("Can not transfer transform parenting to a none play mode edit object.");
                    return 0;
                }
                else if (parentPlayMode.CachedID == 0)
                {
                    return parentPlayMode.transform.GetInstanceID();
                }

                parentCachedID = parentPlayMode.CachedID;
            }
            return parentCachedID;
        }

        public static void CacheCurrentState()
        {
            _cachedObjects = new Dictionary<int, string>();
            _cachedObjectsParentTransformsInstanceIDs = new Dictionary<int, int>();
            _newPrefabInstances = new List<PrefabInstance>();

            bool objIsNewInstance = false;
            foreach (ObjectCacheInstance cachedInstance in ActivePlayModeEditObjects(includeNewGameObjects: true, isNewInstance: (x) => objIsNewInstance = x))
            {
                if (objIsNewInstance)
                {
                    PlayModeEdit editObject = (PlayModeEdit)cachedInstance.cachedObject;
                    Dictionary<string, string> typeComponentsJSON = new Dictionary<string, string>();

                    foreach (Object component in PlayModeEditComponents(editObject, includePlayModeEdit: false))
                    {
                        typeComponentsJSON[component.GetType().Name] = EditorJsonUtility.ToJson(component);
                    }

                    int transformParentID = TransformToParentInstanceID(editObject.transform);

                    _newPrefabInstances.Add(new PrefabInstance { prefab = editObject.PrefabInstance,
                        prefabGameObjectJSON = EditorJsonUtility.ToJson(editObject.gameObject),
                        parentInstanceID = transformParentID,
                        transformInstanceID = editObject.transform.GetInstanceID(),
                        typeComponents = typeComponentsJSON,
                    });
                }
                else
                {
                    // Handle parenting.
                    if (cachedInstance.cachedObject is Transform)
                    {
                        Transform transform = (Transform)cachedInstance.cachedObject;
                        int parentCachedID = TransformToParentInstanceID(transform);
                        _cachedObjectsParentTransformsInstanceIDs.Add(cachedInstance.cahedID, parentCachedID);
                    }

                    _cachedObjects.Add(cachedInstance.cahedID, EditorJsonUtility.ToJson(cachedInstance.cachedObject));
                }
            }
        }

        public static void ApplyCache()
        {
            HashSet<GameObject> removedGameObjects = new HashSet<GameObject>();
            Dictionary<int, Transform> transformOriginalToNew = new Dictionary<int, Transform>();

            if (_cachedObjects == null)
            {
                Debug.LogError("No cache to apply!");
                return;
            }

            // Handle edits to existing objects.
            foreach (ObjectCacheInstance cachedInstance in ActivePlayModeEditObjects())
            {
                int objInstanceID = cachedInstance.cahedID;
                Object obj = EditorUtility.InstanceIDToObject(objInstanceID);

                if (!_cachedObjects.ContainsKey(objInstanceID))
                {
                    GameObject componentGameObject = ((Component)obj).gameObject;
                    removedGameObjects.Add(componentGameObject);
                    continue;
                }

                Undo.RecordObject(obj, "Play Mode Edits");
                EditorJsonUtility.FromJsonOverwrite(_cachedObjects[objInstanceID], obj);
            }

            // Handle deleted GameObjects.
            foreach (GameObject deletedGameObject in removedGameObjects)
            {
                if (deletedGameObject.GetComponent<PlayModeEdit>().recordDestruction)
                    Undo.DestroyObjectImmediate(deletedGameObject);
            }

            // Handle new prefab instances.
            foreach (PrefabInstance prefabInst in _newPrefabInstances)
            {
                GameObject newPrefabInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefabInst.prefab);
                EditorJsonUtility.FromJsonOverwrite(prefabInst.prefabGameObjectJSON, newPrefabInstance);

                foreach (KeyValuePair<string, string> entry in prefabInst.typeComponents)
                {
                    EditorJsonUtility.FromJsonOverwrite(entry.Value, newPrefabInstance.GetComponent(entry.Key));
                }

                _cachedObjectsParentTransformsInstanceIDs.Add(newPrefabInstance.transform.GetInstanceID(), prefabInst.parentInstanceID);
                transformOriginalToNew.Add(prefabInst.transformInstanceID, newPrefabInstance.transform);

                Undo.RegisterCreatedObjectUndo(newPrefabInstance, "Play Mode Edits");
            }

            // Do parenting after all edits.
            foreach (KeyValuePair<int, int> entry in _cachedObjectsParentTransformsInstanceIDs)
            {
                Transform transform = EditorUtility.InstanceIDToObject(entry.Key) as Transform;
                if (transform == null)
                    continue;

                if (entry.Value == 0)
                {
                    transform.SetParent(null, false);
                    continue;
                }

                PlayModeEdit parent = EditorUtility.InstanceIDToObject(entry.Value) as PlayModeEdit;
                if (parent == null) // If the parent is null it's probably because it's a reference to the original transform id of a new object.
                {
                    transform.SetParent(transformOriginalToNew[entry.Value], false);
                } else
                {
                    transform.SetParent(parent.transform, false);
                }        
            }

            ClearCache();
        }

        private static void ClearCache()
        {
            _cachedObjects = null;
            _newPrefabInstances = null;
            _cachedObjectsParentTransformsInstanceIDs = null;
        }

        /// <summary>
        /// Gets active components in all GameObjects with a PlayModeEdit component.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<ObjectCacheInstance> ActivePlayModeEditObjects(bool includeNewGameObjects = false, System.Action<bool> isNewInstance = null)
        {
            foreach (PlayModeEdit editObject in Resources.FindObjectsOfTypeAll<PlayModeEdit>())
            {
                if (editObject.gameObject.scene.rootCount == 0)
                {
                    continue;
                }

                if (isNewInstance != null)
                    isNewInstance(editObject.PrefabInstance != null);

                if (editObject.PrefabInstance != null)
                {
                    if (includeNewGameObjects) { }
                        yield return new ObjectCacheInstance { cachedObject = editObject };

                    continue;
                }

                foreach (ObjectCacheInstance cacheInstance in editObject.CachedComponents)
                {
                    yield return cacheInstance;
                }
            }
        }

        public static IEnumerable<Object> PlayModeEditComponents(PlayModeEdit editObject, bool includePlayModeEdit = false, bool recordedComponentsOnly = true)
        {
            foreach (Component obj in editObject.gameObject.GetComponents<Component>())
            {
                if (obj == editObject && !includePlayModeEdit)
                    continue;

                if (!editObject.recordedComponents.Contains(obj) && recordedComponentsOnly)
                    continue;

                yield return obj;
            }
        }
    }
}
