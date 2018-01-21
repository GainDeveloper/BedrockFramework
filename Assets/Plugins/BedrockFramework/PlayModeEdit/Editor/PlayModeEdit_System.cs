using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace BedrockFramework.PlayModeEdit
{
    public static class PlayModeEdit_System
    {
        private static Dictionary<int, string> _cachedObjects;
        private static Dictionary<Transform, int> _cachedObjectsParentTransformsInstanceIDs;

        private static List<PrefabInstance> _newPrefabInstances;

        struct PrefabInstance {
            public GameObject prefab;
            public string prefabGameObjectJSON;
            public int parentInstanceID, transformInstanceID;
            public Dictionary<string, string> typeComponents; //TODO: Support multiple components of the same type.
            public List<string> recordedComponentsTypes; //TODO: Support multiple components of the same type.
        }

        public static void CacheCurrentState()
        {
            _cachedObjects = new Dictionary<int, string>();
            _cachedObjectsParentTransformsInstanceIDs = new Dictionary<Transform, int>();
            _newPrefabInstances = new List<PrefabInstance>();

            bool objIsNewInstance = false;
            foreach (Object obj in ActivePlayModeEditObjects(includeNewGameObjects: true, isNewInstance: (x) => objIsNewInstance = x))
            {
                if (objIsNewInstance)
                {
                    PlayModeEdit editObject = ((GameObject)obj).GetComponent<PlayModeEdit>();
                    Dictionary<string, string> typeComponentsJSON = new Dictionary<string, string>();

                    foreach (Object component in PlayModeEditComponents(editObject, includePlayModeEdit: false, ignoreRecordedComponents: true))
                    {
                        typeComponentsJSON[component.GetType().Name] = EditorJsonUtility.ToJson(component);
                    }

                    int transformParentID = editObject.transform.parent != null ? transformParentID = editObject.transform.parent.GetInstanceID() : 0;

                    _newPrefabInstances.Add(new PrefabInstance { prefab = editObject.PrefabInstance,
                        prefabGameObjectJSON = EditorJsonUtility.ToJson(editObject.gameObject),
                        parentInstanceID = transformParentID,
                        transformInstanceID = editObject.transform.GetInstanceID(),
                        typeComponents = typeComponentsJSON,
                        recordedComponentsTypes = editObject.RecordedComponentTypes
                    });
                }
                else
                {
                    if (obj is Transform)
                    {
                        Transform transform = (Transform)obj;
                        int transformParentID = transform.parent != null ? transformParentID = transform.parent.GetInstanceID() : 0;
                        _cachedObjectsParentTransformsInstanceIDs.Add(transform, transformParentID);
                    }

                    _cachedObjects.Add(obj.GetInstanceID(), EditorJsonUtility.ToJson(obj));
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
            foreach (Object obj in ActivePlayModeEditObjects())
            {
                int objInstanceID = obj.GetInstanceID();

                if (!_cachedObjects.ContainsKey(objInstanceID))
                {
                    Debug.Log(obj);
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

                _cachedObjectsParentTransformsInstanceIDs.Add(newPrefabInstance.transform, prefabInst.parentInstanceID);
                transformOriginalToNew.Add(prefabInst.transformInstanceID, newPrefabInstance.transform);

                // Specific handling for PlayModeEdit (as components serialized won't exist after scene reload).
                PlayModeEdit newPlayModeEdit = newPrefabInstance.GetComponent<PlayModeEdit>();
                newPlayModeEdit.recordedComponents.Clear();
                foreach (string recordedComponentType in prefabInst.recordedComponentsTypes)
                    newPlayModeEdit.recordedComponents.Add(newPrefabInstance.GetComponent(recordedComponentType));

                Undo.RegisterCreatedObjectUndo(newPrefabInstance, "Play Mode Edits");
            }

            // Do parenting after all edits.
            foreach (KeyValuePair<Transform, int> entry in _cachedObjectsParentTransformsInstanceIDs)
            {
                if (entry.Key == null)
                    continue; //Case where an object has been instanced that isn't connected to a prefab.

                if (entry.Value == 0)
                {
                    entry.Key.SetParent(null, false);
                    continue;
                }

                Object transform = EditorUtility.InstanceIDToObject(entry.Value);
                entry.Key.SetParent(transform != null ? (Transform)transform : transformOriginalToNew[entry.Value], false);
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
        public static IEnumerable<Object> ActivePlayModeEditObjects(bool includeNewGameObjects = false, System.Action<bool> isNewInstance = null)
        {
            foreach (PlayModeEdit editObject in GameObject.FindObjectsOfType<PlayModeEdit>())
            {
                if (isNewInstance != null)
                    isNewInstance(editObject.PrefabInstance != null);

                if (editObject.PrefabInstance != null)
                {
                    if (includeNewGameObjects) { }
                        yield return editObject.gameObject;

                    continue;
                }

                yield return editObject.gameObject;

                foreach (Object obj in PlayModeEditComponents(editObject))
                {
                    yield return obj;
                }
            }
        }

        public static IEnumerable<Object> PlayModeEditComponents(PlayModeEdit editObject, bool includePlayModeEdit = false, bool ignoreRecordedComponents = false)
        {
            foreach (Component obj in editObject.gameObject.GetComponents<Component>())
            {
                if (obj == editObject && !includePlayModeEdit)
                    continue;

                if (!editObject.recordedComponents.Contains(obj) && !ignoreRecordedComponents)
                    continue;

                yield return obj;
            }
        }
    }
}
