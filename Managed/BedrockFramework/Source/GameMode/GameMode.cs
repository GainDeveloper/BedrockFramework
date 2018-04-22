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
        }
    }
}