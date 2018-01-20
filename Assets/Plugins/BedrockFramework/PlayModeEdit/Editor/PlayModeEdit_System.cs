using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace BedrockFramework.PlayModeEdit
{
    public static class PlayModeEdit_System
    {
        private static Dictionary<int, string> _cachedObjects;

        public static void CacheCurrentState()
        {
            _cachedObjects = new Dictionary<int, string>();

            foreach (Object obj in ActivePlayModeEditObjects())
            {
                _cachedObjects.Add(obj.GetInstanceID(), EditorJsonUtility.ToJson(obj));
            }
        }

        public static void ApplyCache()
        {
            if (_cachedObjects == null)
            {
                Debug.LogError("No cache to apply!");
                return;
            }

            foreach (Object obj in ActivePlayModeEditObjects())
            {
                int objInstanceID = obj.GetInstanceID();

                if (!_cachedObjects.ContainsKey(objInstanceID))
                    continue;

                Undo.RecordObject(obj, "Play Mode Edits");
                EditorJsonUtility.FromJsonOverwrite(_cachedObjects[objInstanceID], obj);
            }

            ClearCache();
        }

        private static void ClearCache()
        {
            _cachedObjects = null;
        }

        /// <summary>
        /// Gets active components in all GameObjects with a PlayModeEdit component.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<Object> ActivePlayModeEditObjects()
        {
            foreach (PlayModeEdit editObject in GameObject.FindObjectsOfType<PlayModeEdit>())
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
}
