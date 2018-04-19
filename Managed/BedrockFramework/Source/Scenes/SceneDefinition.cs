/********************************************************           
BEDROCKFRAMEWORK : https://github.com/GainDeveloper/BedrockFramework

Scene Definition stores all the scenes required for the primary scene.
Is used by the SceneService to determine what scenes to unload/ load when moving between levels.
********************************************************/
using UnityEngine;

namespace BedrockFramework.Scenes
{
    [System.Serializable]
    public class SceneDefinition : ScriptableObject
    {
        public const string entryScenePath = "Assets/Entry.unity";
        public const string entryScene = "Entry";

        public SceneField primaryScene;
        public SceneField[] additionalScenes = new SceneField[] { };

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