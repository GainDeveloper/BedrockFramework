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

        void SaveObject(string key, object data);
        SavedObjectReferences SavedObjectReferences { get; }

        event Action OnPreLoad;
    }

    public class NullSaveService : ISaveService
    {
        public void LoadSavedData(string filePath) { }
        public void SaveData(string filePath) { }

        public void SaveObject(string key, object data) { }
        public SavedObjectReferences SavedObjectReferences { get { return null; } }

        public event Action OnPreLoad = delegate { };
    }

    public class SaveService : Service, ISaveService
    {
        [ProtoContract]
        [ProtoInclude(7, typeof(SaveableGameObject.TransformSaveData))]
        [ProtoInclude(8, typeof(SaveableGameObject.RigidBodySaveData))]
        [ProtoInclude(9, typeof(SaveableGameObject.AnimatorSaveData))]
        public class SavedData { };

        [ProtoContract]
        public class SavedGameObject
        {
            [ProtoMember(1)]
            public SavedObjectReference<Pool.PoolDefinition> gameObjectPool;
            [ProtoMember(2)]
            public Dictionary<int, SavedData> savedData = new Dictionary<int, SavedData>();

            public SavedGameObject() { }

            public SavedGameObject(Pool.PoolDefinition pool) {
                gameObjectPool = new SavedObjectReference<Pool.PoolDefinition>(pool);
            }
        };

        [ProtoContract]
        class GameSave
        {
            [ProtoMember(1)]
            public Dictionary<int, SavedData> savedData = new Dictionary<int, SavedData>();
            [ProtoMember(2)]
            public List<SavedGameObject> savedPooledObjects = new List<SavedGameObject>();
        }

        public const string SaveServiceLog = "Saves";

        private SavedObjectReferences savedObjectReferences;
        public SavedObjectReferences SavedObjectReferences { get { return savedObjectReferences; } }

        public event Action OnPreLoad = delegate { };

        public SaveService(MonoBehaviour owner) : base(owner)
        {
            savedObjectReferences = Resources.LoadAll<Saves.SavedObjectReferences>("")[0];
        }

        private GameSave currentGameSave;
        protected byte[] currentGameSaveBuffer;

        protected void LoadGameSaveFromFile(string filePath)
        {
            using (var file = File.OpenRead("E:/GameSave.bin"))
            {
                currentGameSave = Serializer.Deserialize<GameSave>(file);
            }
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
            if (currentGameSave == null)
                LoadGameSaveFromFile(filePath);

            // Tell objects we are about to load (so pooled objects can despawn).
            OnPreLoad();

            // Tell objects we have loaded the data. (Used to load the correct scene definition as an example).

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

            // SaveableGameObject could be a special case for us to re instantiate.
            foreach (SaveableGameObject saveableGameObject in GameObject.FindObjectsOfType<SaveableGameObject>())
            {
                currentGameSave.savedPooledObjects.Add(saveableGameObject.GameObjectSaveData());
            }

            SaveGameSaveToFile(filePath);
        }


        public void SaveObject(string key, object data) {}


    }
}