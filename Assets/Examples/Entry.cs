using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

using BedrockFramework;
using BedrockFramework.Scenes;


namespace Encounters
{
    public class Entry : MonoBehaviour
    {
        public SceneDefinition nextScene;

        void Awake()
        {
            // Register our default services.
            ServiceLocator.RegisterSceneService(new BedrockFramework.Scenes.SceneService(this));
            ServiceLocator.RegisterPoolService(new BedrockFramework.Pool.PoolService(this));

            // Load PrePooled gameobjects.
            ServiceLocator.PoolService.PrePool();
        }

        void Start()
        {
#if UNITY_EDITOR
            // In the editor we load any previous scene definition.
            SceneDefinition previousEditorSceneDefinition = EditorUtility.InstanceIDToObject(EditorPrefs.GetInt(EditorSceneManager_Loader.previousSceneDefinitionKey)) as SceneDefinition;
            if (previousEditorSceneDefinition != null)
            {
                ServiceLocator.SceneService.LoadScene(previousEditorSceneDefinition);
                return;
            }
#endif

            ServiceLocator.SceneService.LoadScene(nextScene);
        }
    }

}
