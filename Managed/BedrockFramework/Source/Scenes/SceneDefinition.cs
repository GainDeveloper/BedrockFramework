/********************************************************           
BEDROCKFRAMEWORK : https://github.com/GainDeveloper/BedrockFramework

Scene Definition stores all the scenes required for the primary scene.
Is used by the SceneService to determine what scenes to unload/ load when moving between levels.
********************************************************/
using UnityEngine;
using Sirenix.OdinInspector;

namespace BedrockFramework.Scenes
{
    [System.Serializable, HideMonoScript]
    public class SceneDefinition : ScriptableObject
    {
        public const string entryScenePath = "Assets/Entry.unity";
        public const string entryScene = "Entry";

        public SceneField primaryScene;
        public SceneField[] additionalScenes = new SceneField[] { };

        [HideLabel, InlineProperty, Title("Scene Settings")]
        public SceneSettings sceneSettings = new SceneSettings();

        public string PrimaryScene
        {
            get
            {
                return primaryScene.SceneName;
            }
        }

        public string[] AllScenes
        {
            get
            {
                string[] allScenes = new string[additionalScenes.Length + 2];

                allScenes[0] = entryScene;
                allScenes[1] = primaryScene.SceneName;
                for (int i = 0; i < additionalScenes.Length; i++)
                    allScenes[2 + i] = additionalScenes[i].SceneName;
                return allScenes;
            }
        }

        public string[] AdditionalScenes
        {
            get
            {
                string[] finalAdditionalScenes = new string[additionalScenes.Length];
                for (int i = 0; i < additionalScenes.Length; i++)
                    finalAdditionalScenes[i] = additionalScenes[i].SceneName;
                return finalAdditionalScenes;
            }
        }
    }
}