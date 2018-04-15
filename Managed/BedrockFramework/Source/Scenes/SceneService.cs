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
        event Action OnFinishedLoading;
    }

    public class NullSceneService : ISceneService
    {
        public void LoadScene(SceneDefinition sceneToLoad) { }
        public event Action OnFinishedLoading = delegate { };
    }

    public class SceneService : BedrockService, ISceneService
    {
        const string SceneServiceLog = "Scenes";

        public event Action OnFinishedLoading = delegate { };

        private SceneDefinition currentlyLoaded = null;

        public SceneService(MonoBehaviour owner): base(owner)
        {
        }

        public void LoadScene(SceneDefinition sceneToLoad)
        {
            if (currentlyLoaded == null)
            {
                owner.StartCoroutine(LoadActiveAsync(currentlyLoaded, LoadSceneMode.Single));
            } else
            {
                owner.StartCoroutine(UnloadAndLoadAsync(currentlyLoaded, sceneToLoad));
            }
        }

        // Used for loading next level.
        IEnumerator LoadActiveAsync(SceneDefinition sceneToLoad, LoadSceneMode loadSceneMode)
        {
            yield return SceneManager.LoadSceneAsync(sceneToLoad.primaryScene, loadSceneMode);
            Scene primaryScene = SceneManager.GetSceneByName(sceneToLoad.primaryScene);
            SceneManager.SetActiveScene(primaryScene);

            // TODO: Send message that primary scene has finished loading.
            // Note: This may not be necessary. In Turpedo we established the game mode before loading additional scenes. Not sure why?

            if (sceneToLoad.additionalScenes.Length > 0)
            {
                List<AsyncOperation> toWaitFor = new List<AsyncOperation>();

                foreach (SceneField sceneField in sceneToLoad.additionalScenes)
                {
                    if (!SceneManager.GetSceneByName(sceneField.SceneName).isLoaded)
                        toWaitFor.Add(SceneManager.LoadSceneAsync(sceneField.SceneName, LoadSceneMode.Additive));
                }

                foreach (AsyncOperation asyncLevelUnload in toWaitFor)
                    yield return asyncLevelUnload;
            }

            // TODO: Send message that additional scenes have finished loading.
            Logger.Logger.Log(SceneServiceLog, "Finished Loading {0}", () => new object[] { sceneToLoad.primaryScene.SceneName });

            OnFinishedLoading();
        }

        // Used for switching between levels.
        IEnumerator UnloadAndLoadAsync(SceneDefinition sceneToUnload, SceneDefinition sceneToLoad)
        {
            List<AsyncOperation> toWaitFor = new List<AsyncOperation>();
            toWaitFor.Add(SceneManager.UnloadSceneAsync(sceneToUnload.primaryScene));

            // Ensure we only unload scenes that we won't be using in the next scene.
            foreach (SceneField scene in sceneToUnload.additionalScenes)
            {
                if (sceneToLoad.additionalScenes.Where(item => item.SceneName == scene.SceneName).Count() == 0)
                {
                    toWaitFor.Add(SceneManager.UnloadSceneAsync(scene));
                }
            }

            foreach (AsyncOperation asyncLevelUnload in toWaitFor)
            {
                yield return asyncLevelUnload;
            }

            System.GC.Collect();
            Logger.Logger.Log(SceneServiceLog, "Finished UnLoading {0}", () => new object[] { sceneToUnload.primaryScene.SceneName });

            yield return LoadActiveAsync(sceneToLoad, LoadSceneMode.Additive);
        }
    }
}