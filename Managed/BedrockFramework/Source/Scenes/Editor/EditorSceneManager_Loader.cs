/********************************************************           
BEDROCKFRAMEWORK : https://github.com/GainDeveloper/BedrockFramework

Loads all the additional scenes when a scene has a definition file for it.
********************************************************/

using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;

namespace BedrockFramework.Scenes
{
    [InitializeOnLoad]
    public static class EditorSceneManager_Loader
    {
        public static SceneDefinition currentDefinition;
        static bool ignoreSceneEvents = false;

        public static event Action OnDefinitionChange = delegate { };

        static EditorSceneManager_Loader()
        {
            EditorSceneManager.sceneOpened += OnSceneLoaded;
            EditorSceneManager.sceneClosed += OnSceneClosed;
            EditorSceneManager.newSceneCreated += OnSceneCreated;
        }

        static void OnSceneCreated(UnityEngine.SceneManagement.Scene scene, NewSceneSetup setup, NewSceneMode mode)
        {
            //Debug.Log("Scene Created!");
            RefreshCurrentSceneDefinition();
        }

        static void OnSceneClosed(UnityEngine.SceneManagement.Scene scene)
        {
            // Check if scene was closed or just unloaded.
            if (EditorSceneManager.GetSceneManagerSetup().Where(x => x.path == scene.path).Count() == 0)
            {
                //Debug.Log("Scene Closed!");
                RefreshCurrentSceneDefinition();
            }
        }

        static void OnSceneLoaded(UnityEngine.SceneManagement.Scene arg0, UnityEditor.SceneManagement.OpenSceneMode arg1)
        {
            //Debug.Log("Scene Loaded!");
            RefreshCurrentSceneDefinition();
        }

        public static void RefreshCurrentSceneDefinition()
        {
            if (ignoreSceneEvents)
                return;

            currentDefinition = FindSceneDefinition();
            OnDefinitionChange();

            if (currentDefinition == null)
            {
                return;
            }

            RefreshLoadedScenes();
        }

        public static void RefreshLoadedScenes()
        {
            UnityEngine.SceneManagement.Scene rootScene = RootScene();
            ignoreSceneEvents = true;

            IEnumerable<UnityEngine.SceneManagement.Scene> currentScenes = Enumerable.Range(0, EditorSceneManager.loadedSceneCount).Select(x => EditorSceneManager.GetSceneAt(x));
            IEnumerable<string> desiredScenes = currentDefinition.additionalScenes.Select(x => x.SceneFilePath);
            desiredScenes = desiredScenes.Concat(new[] { rootScene.path });

            // Unload any scenes no longer part of the additional scenes.
            foreach (UnityEngine.SceneManagement.Scene scene in currentScenes.Where(x => !desiredScenes.Contains(x.path)))
                EditorSceneManager.CloseScene(scene, true);

            // Load remaining.
            foreach (string scenePath in desiredScenes.Where(x => !currentScenes.Select(y => y.path).Contains(x)))
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

            EditorSceneManager.SetActiveScene(rootScene);
            ignoreSceneEvents = false;
        }

        static SceneDefinition FindSceneDefinition()
        {
            for (int i = 0; i < EditorSceneManager.loadedSceneCount; i++)
            {
                SceneDefinition sceneDefintion = SceneDefinition.FromPath(EditorSceneManager.GetSceneAt(i).path);
                if (sceneDefintion != null)
                {
                    return sceneDefintion;
                }
            }

            return null;
        }

        static UnityEngine.SceneManagement.Scene RootScene()
        {
            for (int i = 0; i < EditorSceneManager.loadedSceneCount; i++)
            {
                UnityEngine.SceneManagement.Scene scene = EditorSceneManager.GetSceneAt(i);
                SceneDefinition sceneDefintion = SceneDefinition.FromPath(scene.path);
                if (sceneDefintion != null)
                {
                    return scene;
                }
            }

            return new UnityEngine.SceneManagement.Scene();
        }
    }
}