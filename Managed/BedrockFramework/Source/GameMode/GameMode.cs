/********************************************************           
BEDROCKFRAMEWORK : https://github.com/GainDeveloper/BedrockFramework

Handles the game loop. 
Instantion of players, game events and game over conditions.
********************************************************/
using UnityEngine;
using Sirenix.OdinInspector;

namespace BedrockFramework.GameMode
{
    [System.Serializable, HideMonoScript]
    public class GameMode : MonoBehaviour
    {
        private GameModeInfo info;

        public virtual void SetupGameMode(GameModeInfo info)
        {
            this.info = info;
            ServiceLocator.SceneService.OnLoadScene += SceneService_OnLoadScene;
        }

        private void SceneService_OnLoadScene(Scenes.SceneLoadInfo newScene)
        {
            ShutdownGameMode();
        }

        public virtual void ShutdownGameMode() // Called when the level changes.
        {

        }
    }
}