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

        void AddSceneObject(int category, object toAdd);
        IEnumerable<T> GetSceneObjectsOfCategory<T>(int ofType);
        List<T> GetSceneObjectsOfCategoryList<T>(int ofType);
        void RemoveSceneObject(int category, object toRemove);
        event Action<int, object> SceneObjectAdded;
        event Action<int, object> SceneObjectRemoved;
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

        public void AddSceneObject(int category, object toAdd) { }
        public IEnumerable<T> GetSceneObjectsOfCategory<T>(int ofType) { yield break; }
        public List<T> GetSceneObjectsOfCategoryList<T>(int ofType) { return null; }
        public void RemoveSceneObject(int category, object toRemove) { }
        public event Action<int, object> SceneObjectAdded = delegate { };
        public event Action<int, object> SceneObjectRemoved = delegate { };
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

        public Dictionary<int, List<object>> sceneObjectsOfCategory = new Dictionary<int, List<object>>();
        public event Action<int, object> SceneObjectAdded = delegate { };
        public event Action<int, object> SceneObjectRemoved = delegate { };

        public SceneService(MonoBehaviour owner): base(owner)
        {
            ServiceLocator.SaveService.OnPreSave += SaveService_OnPreSave;
            ServiceLocator.SaveService.OnPreLoad += SaveService_OnPreLoad;

            if (Debug.isDebugBuild)
            {
                foreach (SceneDefinition sceneDefinition in ServiceLocator.SaveService.SavedObjectReferences.GetObjectsOfType<SceneDefinition>())
                    DevTools.DebugMenu.AddDebugButton("Scenes", "Load " + sceneDefinition.sceneSettings.SceneTitle, () => { LoadScene(new SceneLoadInfo(sceneDefinition)); });

                DevTools.DebugMenu.AddDebugStats("Scene Stats", SceneStats);
            }
        }

        IEnumerable<string> SceneStats()
        {
            yield return "State: " + currentState;
            if (currentState != SceneLoadingState.Loaded)
                yield break;
            yield return "Current: " + currentlyLoaded.sceneDefinition.ObjectReference.name;

            foreach (KeyValuePair<int, List<object>> entry in sceneObjectsOfCategory)
                yield return entry.Key + " : " + entry.Value.Count + " items";
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
            sceneObjectsOfCategory.Clear();
            DevTools.Logger.Log(SceneServiceLog, "Finished Unloading");

            yield return LoadActiveAsync(sceneToLoad, LoadSceneMode.Additive);
        }

        //
        // Scene Objects
        //

        public void AddSceneObject(int category, object toAdd)
        {
            if (sceneObjectsOfCategory.ContainsKey(category))
            {
                sceneObjectsOfCategory[category].Add(toAdd);
            } else
            {
                sceneObjectsOfCategory[category] = new List<object>() { toAdd };
            }

            SceneObjectAdded(category, toAdd);
        }

        public IEnumerable<T> GetSceneObjectsOfCategory<T>(int ofType)
        {
            if (sceneObjectsOfCategory.ContainsKey(ofType))
            {
                foreach (object sceneObject in sceneObjectsOfCategory[ofType])
                    yield return (T)sceneObject;
            } else
            {
                yield break;
            }
        }

        public List<T> GetSceneObjectsOfCategoryList<T>(int ofType)
        {
            if (sceneObjectsOfCategory.ContainsKey(ofType))
            {
                return sceneObjectsOfCategory[ofType].Cast<T>().ToList();
            }
            else
            {
                return new List<T>();
            }
        }

        public void RemoveSceneObject(int category, object toRemove)
        {
            if (sceneObjectsOfCategory.ContainsKey(category))
            {
                SceneObjectRemoved(category, toRemove);
                sceneObjectsOfCategory[category].Remove(toRemove);
            }
        }
    }
}