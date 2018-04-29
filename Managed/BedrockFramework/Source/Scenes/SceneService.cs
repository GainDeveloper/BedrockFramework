/********************************************************           
BEDROCKFRAMEWORK : https://github.com/GainDeveloper/BedrockFramework

Scene Service. Handles loading scenes, subscenes and sending out the correct messaging.
********************************************************/
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using BedrockFramework.Utilities;
using ProtoBuf;

namespace BedrockFramework.Scenes
{
    public interface ISceneService
    {
        Coroutine LoadScene(SceneLoadInfo sceneToLoad);
        event Action OnUnload;
        event Action OnPreFinishedLoading;
        event Action<SceneLoadInfo> OnFinishedLoading;

        SceneLoadInfo CurrentLoaded { get; }
    }

    public class NullSceneService : ISceneService
    {
        public Coroutine LoadScene(SceneLoadInfo sceneToLoad) { return null; }
        public event Action OnUnload = delegate { };
        public event Action OnPreFinishedLoading = delegate { };
        public event Action<SceneLoadInfo> OnFinishedLoading = delegate { };

        public SceneLoadInfo CurrentLoaded { get { return null; } }
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
    }

    public class SceneService : Service, ISceneService
    {
        const string SceneServiceLog = "Scenes";

        public event Action OnUnload = delegate { };
        public event Action OnPreFinishedLoading = delegate { };
        public event Action<SceneLoadInfo> OnFinishedLoading = delegate { };

        private SceneLoadInfo currentlyLoaded = null;
        public SceneLoadInfo CurrentLoaded { get { return currentlyLoaded; } }

        public SceneService(MonoBehaviour owner): base(owner)
        {
            ServiceLocator.SaveService.OnPreSave += SaveService_OnPreSave;
            ServiceLocator.SaveService.OnPreLoad += SaveService_OnPreLoad;

            if (Debug.isDebugBuild)
            {
                foreach (SceneDefinition sceneDefinition in ServiceLocator.SaveService.SavedObjectReferences.GetObjectsOfType<SceneDefinition>())
                    DevTools.DebugMenu.AddDebugItem("Scenes", "Load " + sceneDefinition.sceneSettings.SceneTitle, () => { LoadScene(new SceneLoadInfo(sceneDefinition)); });
            }
        }

        private void SaveService_OnPreLoad(CoroutineEvent coroutineEvent)
        {
            SceneLoadInfo sceneLoadInfo = ServiceLocator.SaveService.GetSaveData<SceneLoadInfo>(Animator.StringToHash("PreviousScene"));
            sceneLoadInfo.fromSave = true;
            coroutineEvent.coroutines.Add(LoadScene(sceneLoadInfo));
        }

        private void SaveService_OnPreSave()
        {
            ServiceLocator.SaveService.AppendSaveData(Animator.StringToHash("PreviousScene"), currentlyLoaded);
        }

        public Coroutine LoadScene(SceneLoadInfo sceneToLoad)
        {
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

            DevTools.Logger.Log(SceneServiceLog, "Finished Loading");

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
    }
}