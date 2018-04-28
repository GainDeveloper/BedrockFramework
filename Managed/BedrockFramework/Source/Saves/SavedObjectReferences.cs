/********************************************************           
BEDROCKFRAMEWORK : https://github.com/GainDeveloper/BedrockFramework

Saveable Scriptable Objects store references to themselves in a shared list.
TODO: Need to only store asset paths in the end to ensure we don't have all saveable assets loaded into memory.
********************************************************/
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;

namespace BedrockFramework.Saves
{
    [CreateAssetMenu(fileName = "SavedScriptableObjects", menuName = "BedrockFramework/SavedScriptableObjects", order = 0)]
    public class SavedObjectReferences : ScriptableObject, ISerializationCallbackReceiver
    {
        [ReadOnly, ShowInInspector]
        private Map<int, Object> savedObjects = new Map<int, Object>();
        [SerializeField, HideInInspector]
        private List<int> savedScriptableObjectKeys;
        [SerializeField, HideInInspector]
        private List<Object> savedObjectValues;

        void OnEnable()
        {
            if (!Application.isPlaying)
                Cleanup();
        }

        public void OnBeforeSerialize()
        {
            savedScriptableObjectKeys = new List<int>();
            savedObjectValues = new List<Object>();
            IEnumerator enumerator = savedObjects.GetEnumerator();

            while (enumerator.MoveNext())
            {
                KeyValuePair<int, Object> savedScriptableObject = (KeyValuePair<int, Object>)enumerator.Current;
                savedScriptableObjectKeys.Add(savedScriptableObject.Key);
                savedObjectValues.Add(savedScriptableObject.Value);
            }
        }

        public void OnAfterDeserialize()
        {
            savedObjects = new Map<int, Object>();

            for (int i = 0; i != savedScriptableObjectKeys.Count; i++)
                savedObjects.Add(savedScriptableObjectKeys[i], savedObjectValues[i]);
        }

        public T GetSavedObject<T>(int instanceID) where T : Object
        {
            return savedObjects.Forward[instanceID] as T;
        }

        public int GetSavedObjectID(Object objectInstance, bool logIfNone = true)
        {
            if (!savedObjects.Reverse.Contains(objectInstance))
            {
                if (logIfNone)
                    Logger.Logger.LogError(SaveService.SaveServiceLog, "Received ID Request for {} but it has not been added to the saved references.", () => new object[] { objectInstance.name });
                return 0;
            }

            return savedObjects.Reverse[objectInstance];
        }

        public void AddObject(Object so)
        {
            savedObjects.Add(Random.Range(1, int.MaxValue), so);
            Cleanup();
        }

        private void Cleanup()
        {
            List<int> toRemove = savedObjects.Where(x => x.Value == null).Select(x => x.Key).ToList();

            for (int i = 0; i != toRemove.Count; i++)
                savedObjects.Remove(toRemove[i]);
        }
    }
}