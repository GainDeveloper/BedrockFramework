/********************************************************           
BEDROCKFRAMEWORK : https://github.com/GainDeveloper/BedrockFramework

Save Service. Handles holding data and saving/ loading it. 
********************************************************/
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;

using ProtoBuf;

namespace BedrockFramework.Saves
{
    public interface ISaveService
    {
        void LoadSavedData(string filePath);
        void SaveData(string filePath);

        SavedObjectReferences SavedObjectReferences { get; }

        void AppendSaveData(int key, object saveData);
        T GetSaveData<T>(int key);

        event Action OnPreLoad;
        event Action OnPostLoad;
        event Action OnPreSave;
    }

    public class NullSaveService : ISaveService
    {
        public void LoadSavedData(string filePath) { }
        public void SaveData(string filePath) { }

        public SavedObjectReferences SavedObjectReferences { get { return null; } }

        public void AppendSaveData(int key, object saveData) { }
        public T GetSaveData<T>(int key) { return default(T); }

        public event Action OnPreLoad = delegate { };
        public event Action OnPostLoad = delegate { };
        public event Action OnPreSave = delegate { };
    }

    public class SaveService : Service, ISaveService
    {
        [ProtoContract]
        public class SavedGameObject
        {
            [ProtoMember(1)]
            public SavedObjectReference<Pool.PoolDefinition> gameObjectPool;
            [ProtoMember(2, DynamicType = true)]
            public Dictionary<int, object> savedData = new Dictionary<int, object>();

            public SavedGameObject() { }

            public SavedGameObject(Pool.PoolDefinition pool) {
                gameObjectPool = new SavedObjectReference<Pool.PoolDefinition>(pool);
            }
        };

        [ProtoContract]
        class GameSave
        {
            [ProtoMember(1, DynamicType = true)]
            public Dictionary<int, object> savedData = new Dictionary<int, object>();
            [ProtoMember(2)]
            public List<SavedGameObject> savedPooledObjects = new List<SavedGameObject>();
        }

        public const string SaveServiceLog = "Saves";

        private SavedObjectReferences savedObjectReferences;
        public SavedObjectReferences SavedObjectReferences { get { return savedObjectReferences; } }

        public event Action OnPreLoad = delegate { };
        public event Action OnPostLoad = delegate { };
        public event Action OnPreSave = delegate { };

        public SaveService(MonoBehaviour owner) : base(owner)
        {
            savedObjectReferences = Resources.LoadAll<Saves.SavedObjectReferences>("")[0];
        }

        private GameSave currentGameSave;

        public void AppendSaveData(int key, object saveData)
        {
            if (currentGameSave == null)
            {
                Logger.Logger.LogError(SaveServiceLog, "No currentGameSave to save data to!");
                return;
            }

            currentGameSave.savedData[key] = saveData;
        }

        public T GetSaveData<T>(int key)
        {
            if (currentGameSave == null)
            {
                Logger.Logger.LogError(SaveServiceLog, "No currentGameSave to load data from!");
                return default(T);
            }

            if (!currentGameSave.savedData.ContainsKey(key))
            {
                Logger.Logger.LogError(SaveServiceLog, "No save data for key {} exists!", () => new object[] { key });
                return default(T);
            }

            return (T)currentGameSave.savedData[key];
        }

        protected bool LoadGameSaveFromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Logger.Logger.LogError(SaveServiceLog, "Save file {} does not exist", () => new object[] { filePath });
                return false;
            }

            using (var file = File.OpenRead(filePath))
            {
                currentGameSave = Serializer.Deserialize<GameSave>(file);
            }
            return true;
        }

        protected void SaveGameSaveToFile(string filePath)
        {
            if (currentGameSave == null)
            {
                Logger.Logger.LogError(SaveServiceLog, "No currentGameSave to save to file!");
                return;
            }

            using (var file = File.Create(filePath))
            {
                Serializer.Serialize(file, currentGameSave);
            }
        }

        public void LoadSavedData(string filePath)
        {
            // Tell objects we are about to load (so pooled objects can despawn).
            OnPreLoad();

            if (currentGameSave == null)
            {
                if (!LoadGameSaveFromFile(filePath))
                    return;
            }

            // Tell objects we have loaded the data. (Used to load the correct scene definition as an example).
            OnPostLoad();

            // Reinstantiate the SaveableGameObjects
            foreach (SavedGameObject savedGameObject in currentGameSave.savedPooledObjects)
            {
                //TODO: Need to consider how to handle transform parents. Should it be done here or by the SaveableGameObjectComponent.
                ServiceLocator.PoolService.SpawnDefinition<SaveableGameObject>(savedGameObject.gameObjectPool.ObjectReference).ApplySaveData(savedGameObject);
            }
        }


        public void SaveData(string filePath)
        {
            // Define a SaveGame class that we fill with various properties.
            currentGameSave = new GameSave();

            // Will need to do some generic key/object saving here.
            OnPreSave();

            // SaveableGameObject could be a special case for us to re instantiate.
            foreach (SaveableGameObject saveableGameObject in GameObject.FindObjectsOfType<SaveableGameObject>())
            {
                currentGameSave.savedPooledObjects.Add(saveableGameObject.GameObjectSaveData());
            }

            SaveGameSaveToFile(filePath);
        }
    }
}