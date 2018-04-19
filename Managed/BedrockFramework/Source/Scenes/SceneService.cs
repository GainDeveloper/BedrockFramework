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

namespace BedrockFramework.Scenes
{
    public interface ISceneService
    {
        void LoadScene(SceneDefinition sceneToLoad);
        event Action OnPreFinishedLoading;
        event Action OnFinishedLoading;
    }

    public class NullSceneService : ISceneService
    {
        public void LoadScene(SceneDefinition sceneToLoad) { }
        public event Action OnPreFinishedLoading = delegate { };
        public event Action OnFinishedLoading = delegate { };
    }

    public class SceneService : Service, ISceneService
    {
        const string SceneServiceLog = "Scenes";

        public event Action OnPreFinishedLoading = delegate { };
        public event Action OnFinishedLoading = delegate { };

        private SceneDefinition currentlyLoaded = null;

        public SceneService(MonoBehaviour owner): base(owner) { }

        public void LoadScene(SceneDefinition sceneToLoad)
        {
            // TODO: Take a SceneLoadInfo class to serialize load data for the scene.

            if (currentlyLoaded == null)
            {
                owner.StartCoroutine(LoadActiveAsync(sceneToLoad, LoadSceneMode.Single));
            } else
            {
                owner.StartCoroutine(UnloadAndLoadAsync(currentlyLoaded, sceneToLoad));
            }
        }

        // Used for loading next level.
        IEnumerator LoadActiveAsync(SceneDefinition sceneToLoad, LoadSceneMode loadSceneMode)
        {
            List<AsyncOperation> toWaitFor = new List<AsyncOperation>();

            foreach (string sceneName in sceneToLoad.AllScenes)
            {
                if (!SceneManager.GetSceneByName(sceneName).isLoaded)
                {
                    Logger.Logger.Log(SceneServiceLog, "Loading {}", () => new object[] { sceneName });
                    toWaitFor.Add(SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive));
                }  
            }

            foreach (AsyncOperation asyncLevelUnload in toWaitFor)
                yield return asyncLevelUnload;

            Scene primaryScene = SceneManager.GetSceneByName(sceneToLoad.PrimaryScene);
            SceneManager.SetActiveScene(primaryScene);

            currentlyLoaded = sceneToLoad;

            // Use to change the state of the active scenes GameObjects before progressing (Loading scene save data ect.)
            OnPreFinishedLoading();

            Logger.Logger.Log(SceneServiceLog, "Finished Loading");

            OnFinishedLoading();
        }

        // Used for switching between levels.
        IEnumerator UnloadAndLoadAsync(SceneDefinition sceneToUnload, SceneDefinition sceneToLoad)
        {
            List<AsyncOperation> toWaitFor = new List<AsyncOperation>();

            // Ensure we only unload scenes that we won't be using in the next scene.
            foreach (string sceneName in sceneToLoad.AllScenes)
            {
                if (sceneToLoad.AllScenes.Where(item => item == sceneName).Count() == 0)
                {
                    Logger.Logger.Log(SceneServiceLog, "Unloading {}", () => new object[] { sceneName });
                    toWaitFor.Add(SceneManager.UnloadSceneAsync(sceneName));
                }
            }

            foreach (AsyncOperation asyncLevelUnload in toWaitFor)
            {
                yield return asyncLevelUnload;
            }

            System.GC.Collect();
            Logger.Logger.Log(SceneServiceLog, "Finished Unloading");

            yield return LoadActiveAsync(sceneToLoad, LoadSceneMode.Additive);
        }
    }
}