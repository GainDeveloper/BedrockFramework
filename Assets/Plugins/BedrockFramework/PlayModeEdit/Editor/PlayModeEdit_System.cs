using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace BedrockFramework.PlayModeEdit
{
    public static class PlayModeEdit_System
    {
        private static Dictionary<int, string> _cachedObjects;
        private static List<PrefabInstance> _newPrefabInstances;

        struct PrefabInstance {
            public GameObject prefab;
            public Dictionary<string, string> typeComponents;
        }

        public static void CacheCurrentState()
        {
            _cachedObjects = new Dictionary<int, string>();
            _newPrefabInstances = new List<PrefabInstance>();

            foreach (Object obj in ActivePlayModeEditObjects(includeNewGameObjects: true))
            {
                if (obj is GameObject)
                {
                    PlayModeEdit editObject = ((GameObject)obj).GetComponent<PlayModeEdit>();
                    Dictionary<string, string> typeComponentsJSON = new Dictionary<string, string>();

                    foreach (Object component in PlayModeEditComponents(editObject))
                    {
                        typeComponentsJSON[component.GetType().Name] = EditorJsonUtility.ToJson(component);
                    }

                    _newPrefabInstances.Add(new PrefabInstance { prefab = editObject.PrefabInstance,
                        typeComponents = typeComponentsJSON
                    });
                }
                else
                {
                    _cachedObjects.Add(obj.GetInstanceID(), EditorJsonUtility.ToJson(obj));
                }
            }
        }

        public static void ApplyCache()
        {
            HashSet<GameObject> removedGameObjects = new HashSet<GameObject>();

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
                Undo.DestroyObjectImmediate(deletedGameObject);
            }

            // Handle new prefab instances.
            foreach (PrefabInstance prefabInst in _newPrefabInstances)
            {
                GameObject newPrefabInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefabInst.prefab);

                foreach (KeyValuePair<string, string> entry in prefabInst.typeComponents)
                {
                    EditorJsonUtility.FromJsonOverwrite(entry.Value, newPrefabInstance.GetComponent(entry.Key));
                }
                Undo.RegisterCreatedObjectUndo(newPrefabInstance, "Play Mode Edits");
            }

            ClearCache();
        }

        private static void ClearCache()
        {
            _cachedObjects = null;
            _newPrefabInstances = null;
        }

        /// <summary>
        /// Gets active components in all GameObjects with a PlayModeEdit component.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<Object> ActivePlayModeEditObjects(bool includeNewGameObjects = false)
        {
            foreach (PlayModeEdit editObject in GameObject.FindObjectsOfType<PlayModeEdit>())
            {
                if (editObject.PrefabInstance != null)
                {
                    if (includeNewGameObjects)
                        yield return editObject.gameObject;

                    continue;
                }

                foreach (Object obj in PlayModeEditComponents(editObject))
                {
                    yield return obj;
                }
            }
        }

        public static IEnumerable<Object> PlayModeEditComponents(PlayModeEdit editObject)
        {
            foreach (Component obj in editObject.gameObject.GetComponents<Component>())
            {
                if (obj == editObject)
                    continue;

                if (!editObject.recordedComponents.Contains(obj))
                    continue;

                yield return obj;
            }
        }
    }
}
