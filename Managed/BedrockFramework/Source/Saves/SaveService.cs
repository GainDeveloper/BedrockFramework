/********************************************************           
BEDROCKFRAMEWORK : https://github.com/GainDeveloper/BedrockFramework

Save Service. Handles holding data and saving/ loading it. 
TODO: Need to be able to categorize the data types for different saves.
********************************************************/
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

namespace BedrockFramework.Saves
{
    public interface ISaveService
    {
        void LoadSavedData();
        void SaveData();

        void SaveObject(string key, object data);
        void SaveObjectReference(string key, SaveableScriptableObject objectRef);

        T GetSaveableObject<T>(string key) where T : SaveableScriptableObject;
    }

    public class NullSaveService : ISaveService
    {
        public void LoadSavedData() { }
        public void SaveData() { }

        public void SaveObject(string key, object data) { }
        public void SaveObjectReference(string key, SaveableScriptableObject objectRef) { }

        public T GetSaveableObject<T>(string key) where T : SaveableScriptableObject { return null; }
    }

    public class SaveService : Service, ISaveService
    {
        private Dictionary<string, object> cachedSavedObjects = new Dictionary<string, object>();
        private Dictionary<string, int> cachedSavedObjectReference = new Dictionary<string, int>();
        private SavedScriptableObjects savedScriptableObjects;

        public SaveService(MonoBehaviour owner) : base(owner)
        {
            savedScriptableObjects = Resources.LoadAll<Saves.SavedScriptableObjects>("")[0];
        }


        public void LoadSavedData() { }
        public void SaveData() { }


        public void SaveObject(string key, object data) {}

        public void SaveObjectReference(string key, SaveableScriptableObject objectRef)
        {
            cachedSavedObjectReference[key] = objectRef.SerializedInstanceID;
        }

        public T GetSaveableObject<T>(string key) where T : SaveableScriptableObject
        {
            return savedScriptableObjects.GetSavedScriptableObject<T>(cachedSavedObjectReference[key]);
        }
    }
}