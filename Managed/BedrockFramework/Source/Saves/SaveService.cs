/********************************************************           
BEDROCKFRAMEWORK : https://github.com/GainDeveloper/BedrockFramework

Save Service. Handles holding data and saving/ loading it. 
TODO: Need to be able to categorize the data types for different saves. i.e Scene State vs Game State (Character/ Story Progression)
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
        SavedObjectReferences SavedObjectReferences { get; }

        event Action OnPreLoad;
    }

    public class NullSaveService : ISaveService
    {
        public void LoadSavedData() { }
        public void SaveData() { }

        public void SaveObject(string key, object data) { }
        public SavedObjectReferences SavedObjectReferences { get { return null; } }

        public event Action OnPreLoad = delegate { };
    }

    public class SaveService : Service, ISaveService
    {
        public class SavedData { };

        public class SavedGameObject
        {
            public SavedObjectReference<Pool.PoolDefinition> gameObjectPool;
            public Dictionary<int, SavedData> savedData = new Dictionary<int, SavedData>();

            public SavedGameObject(Pool.PoolDefinition pool) {
                gameObjectPool = new SavedObjectReference<Pool.PoolDefinition>(pool);
            }
        };

        class GameSave
        {
            public Dictionary<int, SavedData> savedData = new Dictionary<int, SavedData>();
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

        public void LoadSavedData()
        {
            // Tell objects we are about to load (so pooled objects can despawn).
            OnPreLoad();

            
            // IF LOADING FROM FILE: Load binary from file and convert to save game class.
            // Tell objects we have loaded the data. (Used to load the correct scene definition as an example).

            // Reinstantiate the SaveableGameObjects
            foreach (SavedGameObject savedGameObject in currentGameSave.savedPooledObjects)
            {
                //TODO: Need to consider how to handle transform parents. Should it be done here or by the SaveableGameObjectComponent.
                ServiceLocator.PoolService.SpawnDefinition<SaveableGameObject>(savedGameObject.gameObjectPool.ObjectReference).ApplySaveData(savedGameObject);
            }
        }


        public void SaveData()
        {
            // Define a SaveGame class that we fill with various properties.
            currentGameSave = new GameSave();

            // We could just request an object, along with a key that we retain.

            // SaveableGameObject could be a special case for us to re instantiate.
            foreach (SaveableGameObject saveableGameObject in GameObject.FindObjectsOfType<SaveableGameObject>())
            {
                currentGameSave.savedPooledObjects.Add(saveableGameObject.GameObjectSaveData());
            }

            // IF SAVING TO FILE: Convert same game class to binary and write to a file.
        }


        public void SaveObject(string key, object data) {}


    }
}