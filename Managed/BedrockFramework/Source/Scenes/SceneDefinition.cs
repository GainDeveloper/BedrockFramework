/********************************************************           
BEDROCKFRAMEWORK : https://github.com/GainDeveloper/BedrockFramework

Scene Service. Handles loading scenes, subscenes and sending out the correct messaging.
********************************************************/
using UnityEngine;

namespace BedrockFramework.Scenes
{
    [System.Serializable, CreateAssetMenu(fileName = "SceneDefinition",menuName = "BedrockFramework/SceneDefinition")]
    public class SceneDefinition : ScriptableObject
    {
        public SceneField primaryScene;
        public SceneField[] additionalScenes;
    }
}