/********************************************************           
BEDROCKFRAMEWORK : https://github.com/GainDeveloper/BedrockFramework

Scene Service. Handles loading scenes, subscenes and sending out the correct messaging.
********************************************************/
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using BedrockFramework.Utilities;
using ProtoBuf;

namespace BedrockFramework.Scenes
{
    public enum SceneLoadingState
    {
        Loaded,
        Loading
    }

    public interface ISceneService
    {
        Coroutine LoadScene(SceneLoadInfo sceneToLoad);
        event Action<SceneLoadInfo> OnLoadScene;
        event Action OnUnload;
        event Action OnPreFinishedLoading;
        event Action<SceneLoadInfo> OnFinishedLoading;

        SceneLoadInfo CurrentLoaded { get; }
        SceneLoadingState CurrentState { get; }

        void AddSceneObject(object toAdd);
        IEnumerable<T> GetSceneObjectsOfType<T>(Type ofType);
        void RemoveSceneObject(object toRemove);
        event Action<Type, object> SceneObjectAdded;
        event Action<Type, object> SceneObjectRemoved;
    }

    public class NullSceneService : ISceneService
    {
        public Coroutine LoadScene(SceneLoadInfo sceneToLoad) { return null; }
        public event Action<SceneLoadInfo> OnLoadScene = delegate { };
        public event Action OnUnload = delegate { };
        public event Action OnPreFinishedLoading = delegate { };
        public event Action<SceneLoadInfo> OnFinishedLoading = delegate { };

        public SceneLoadInfo CurrentLoaded { get { return null; } }
        public SceneLoadingState CurrentState { get { return SceneLoadingState.Loaded; } }

        public void AddSceneObject(object toAdd) { }
        public IEnumerable<T> GetSceneObjectsOfType<T>(Type ofType) { yield break; }
        public void RemoveSceneObject(object toRemove) { }
        public event Action<Type, object> SceneObjectAdded = delegate { };
        public event Action<Type, object> SceneObjectRemoved = delegate { };
    }

    [ProtoContract]
    public class SceneLoadInfo
    {
        [ProtoMember(1)]
        public Saves.SavedObjectReference<SceneDefinition> sceneDefinition;
        public bool fromSave = false;

        public SceneLoadInfo() { }

        public SceneLoadInfo(SceneDefinition sceneDefinition)
        {
            this.sceneDefinition = new Saves.SavedObjectReference<SceneDefinition>(sceneDefinition);
        }

        public SceneLoadInfo(NetworkReader reader)
        {
            this.sceneDefinition = new Saves.SavedObjectReference<SceneDefinition>(reader.ReadInt16());
            fromSave = true;
        }

        public void NetworkWrite(NetworkWriter writer)
        {
            writer.Write(sceneDefinition.ObjectReferenceID);
        }
    }

    public class SceneService : Service, ISceneService
    {
        const string SceneServiceLog = "Scenes";

        public event Action<SceneLoadInfo> OnLoadScene = delegate { };
        public event Action OnUnload = delegate { };
        public event Action OnPreFinishedLoading = delegate { };
        public event Action<SceneLoadInfo> OnFinishedLoading = delegate { };

        private SceneLoadInfo currentlyLoaded = null;
        public SceneLoadInfo CurrentLoaded { get { return currentlyLoaded; } }

        private SceneLoadingState currentState = SceneLoadingState.Loaded;
        public SceneLoadingState CurrentState { get { return currentState; } }

        public Dictionary<Type, List<object>> sceneObjectsOfType = new Dictionary<Type, List<object>>();
        public event Action<Type, object> SceneObjectAdded = delegate { };
        public event Action<Type, object> SceneObjectRemoved = delegate { };

        public SceneService(MonoBehaviour owner): base(owner)
        {
            ServiceLocator.SaveService.OnPreSave += SaveService_OnPreSave;
            ServiceLocator.SaveService.OnPreLoad += SaveService_OnPreLoad;

            if (Debug.isDebugBuild)
            {
                foreach (SceneDefinition sceneDefinition in ServiceLocator.SaveService.SavedObjectReferences.GetObjectsOfType<SceneDefinition>())
                    DevTools.DebugMenu.AddDebugButton("Scenes", "Load " + sceneDefinition.sceneSettings.SceneTitle, () => { LoadScene(new SceneLoadInfo(sceneDefinition)); });
            }
        }

        private void SaveService_OnPreSave()
        {
            ServiceLocator.SaveService.AppendSaveData(Animator.StringToHash("PreviousScene"), currentlyLoaded);
        }

        private void SaveService_OnPreLoad(CoroutineEvent coroutineEvent)
        {
            SceneLoadInfo sceneLoadInfo = ServiceLocator.SaveService.GetSaveData<SceneLoadInfo>(Animator.StringToHash("PreviousScene"));
            sceneLoadInfo.fromSave = true;
            coroutineEvent.coroutines.Add(LoadScene(sceneLoadInfo));
        }

        public Coroutine LoadScene(SceneLoadInfo sceneToLoad)
        {
            currentState = SceneLoadingState.Loading;
            sceneObjectsOfType.Clear();

            OnLoadScene(sceneToLoad);

            if (currentlyLoaded == null)
            {
                return owner.StartCoroutine(LoadActiveAsync(sceneToLoad, LoadSceneMode.Single));
            }

            return owner.StartCoroutine(UnloadAndLoadAsync(currentlyLoaded, sceneToLoad));
        }

        // Used for loading next level.
        IEnumerator LoadActiveAsync(SceneLoadInfo sceneToLoad, LoadSceneMode loadSceneMode)
        {
            List<AsyncOperation> toWaitFor = new List<AsyncOperation>();

            foreach (string sceneName in sceneToLoad.sceneDefinition.ObjectReference.AllScenes)
            {
                if (!SceneManager.GetSceneByName(sceneName).isLoaded)
                {
                    DevTools.Logger.Log(SceneServiceLog, "Loading {}", () => new object[] { sceneName });
                    toWaitFor.Add(SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive));
                }  
            }

            foreach (AsyncOperation asyncLevelUnload in toWaitFor)
                yield return asyncLevelUnload;

            Scene primaryScene = SceneManager.GetSceneByName(sceneToLoad.sceneDefinition.ObjectReference.PrimaryScene);
            SceneManager.SetActiveScene(primaryScene);

            currentlyLoaded = sceneToLoad;

            // Use to change the state of the active scenes GameObjects before progressing (Loading scene save data ect.)
            OnPreFinishedLoading();

            currentState = SceneLoadingState.Loaded;
            DevTools.Logger.Log(SceneServiceLog, "Finished Loading Scenes");
            OnFinishedLoading(sceneToLoad);
        }

        // Used for switching between levels.
        IEnumerator UnloadAndLoadAsync(SceneLoadInfo sceneToUnload, SceneLoadInfo sceneToLoad)
        {
            List<AsyncOperation> toWaitFor = new List<AsyncOperation>();

            // Ensure we only unload scenes that we won't be using in the next scene.
            foreach (string sceneName in currentlyLoaded.sceneDefinition.ObjectReference.AllScenes)
            {
                if (sceneToLoad.sceneDefinition.ObjectReference.AllScenes.Where(item => item == sceneName).Count() == 0)
                {
                    DevTools.Logger.Log(SceneServiceLog, "Unloading {}", () => new object[] { sceneName });
                    toWaitFor.Add(SceneManager.UnloadSceneAsync(sceneName));
                }
            }

            foreach (AsyncOperation asyncLevelUnload in toWaitFor)
            {
                yield return asyncLevelUnload;
            }

            System.GC.Collect();
            OnUnload();
            DevTools.Logger.Log(SceneServiceLog, "Finished Unloading");

            yield return LoadActiveAsync(sceneToLoad, LoadSceneMode.Additive);
        }

        //
        // Scene Objects
        //

        public void AddSceneObject(object toAdd)
        {
            Type objectType = toAdd.GetType();
            if (sceneObjectsOfType.ContainsKey(objectType))
            {
                sceneObjectsOfType[objectType].Add(toAdd);
            } else
            {
                sceneObjectsOfType[objectType] = new List<object>() { toAdd };
            }

            SceneObjectAdded(objectType, toAdd);
        }

        public IEnumerable<T> GetSceneObjectsOfType<T>(Type ofType)
        {
            if (sceneObjectsOfType.ContainsKey(ofType))
            {
                foreach (object sceneObject in sceneObjectsOfType[ofType])
                    yield return (T)sceneObject;
            } else
            {
                yield break;
            }
        }

        public void RemoveSceneObject(object toRemove)
        {
            Type objectType = toRemove.GetType();
            if (sceneObjectsOfType.ContainsKey(objectType))
            {
                SceneObjectRemoved(objectType, toRemove);
                sceneObjectsOfType[objectType].Remove(toRemove);
            }
        }
    }
}