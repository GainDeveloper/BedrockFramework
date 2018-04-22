/********************************************************           
BEDROCKFRAMEWORK : https://github.com/GainDeveloper/BedrockFramework

Saveable Scriptable Objects store references to themselves in a shared list.
********************************************************/
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using Sirenix.OdinInspector;

namespace BedrockFramework.Saves
{
    [CreateAssetMenu(fileName = "SavedScriptableObjects", menuName = "BedrockFramework/SavedScriptableObjects", order = 0)]
    public class SavedScriptableObjects : ScriptableObject, ISerializationCallbackReceiver
    {
        [ReadOnly, ShowInInspector]
        private Dictionary<int, SaveableScriptableObject> savedScriptableObjects = new Dictionary<int, SaveableScriptableObject>();

        [SerializeField, HideInInspector]
        private List<int> savedScriptableObjectsKeys;
        [SerializeField, HideInInspector]
        private List<SaveableScriptableObject> savedScriptableObjectsValues;

        void OnEnable()
        {
            Cleanup();
        }

        public void OnBeforeSerialize()
        {
            savedScriptableObjectsKeys = new List<int>();
            savedScriptableObjectsValues = new List<SaveableScriptableObject>();

            foreach (var savedScriptableObject in savedScriptableObjects)
            {
                savedScriptableObjectsKeys.Add(savedScriptableObject.Key);
                savedScriptableObjectsValues.Add(savedScriptableObject.Value);
            }
        }

        public void OnAfterDeserialize()
        {
            savedScriptableObjects = new Dictionary<int, SaveableScriptableObject>();

            for (int i = 0; i != savedScriptableObjectsKeys.Count; i++)
                savedScriptableObjects.Add(savedScriptableObjectsKeys[i], savedScriptableObjectsValues[i]);
        }

        public static void AddSavedScriptableObject(SaveableScriptableObject so)
        {
            foreach (SavedScriptableObjects saveableSO in Resources.LoadAll<SavedScriptableObjects>(""))
            {
                saveableSO.savedScriptableObjects[so.SerializedInstanceID] = so;
                saveableSO.Cleanup();
            }
        }

        private void Cleanup()
        {
            List<int> toRemove = savedScriptableObjects.Where(x => x.Value == null).Select(x => x.Key).ToList();

            for (int i = 0; i != toRemove.Count; i++)
                savedScriptableObjects.Remove(toRemove[i]);
        }

        public T GetSavedScriptableObject<T>(int instanceID) where T : SaveableScriptableObject
        {
            return savedScriptableObjects[instanceID] as T;
        }
    }
}