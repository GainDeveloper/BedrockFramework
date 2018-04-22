/********************************************************           
BEDROCKFRAMEWORK : https://github.com/GainDeveloper/BedrockFramework

Accessible from SceneDefition. Stores basic scene data.
Can not contain references into the scene itself.
********************************************************/

using UnityEngine;

namespace BedrockFramework.Scenes
{
    [System.Serializable]
    public class SceneSettings
    {
        [SerializeField]
        private string sceneTitle = "Default Scene Title";
        public string SceneTitle { get { return sceneTitle; } }

        public GameMode.GameModeInfo defaultGameModeInfo;
    }
}
