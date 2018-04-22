/********************************************************           
BEDROCKFRAMEWORK : https://github.com/GainDeveloper/BedrockFramework

Stores any data required for the GameMode.
Also handles instantiation of the GameMode Monobehaviour.
********************************************************/
using UnityEngine;
using Sirenix.OdinInspector;

namespace BedrockFramework.GameMode
{
    [System.Serializable, HideMonoScript]
    public class GameModeInfo : Saves.SaveableScriptableObject
    {
        protected GameMode gameInstance;
        public GameMode GameInstance
        {
            get
            {
                return gameInstance;
            }
        }

        public virtual System.Type GameModeType
        {
            get { return typeof(GameMode); }
        }

        public virtual GameMode GameSetup()
        {
            gameInstance = new GameObject("GameMode").AddComponent(GameModeType) as GameMode;
            gameInstance.SetupGameMode(this);
            return gameInstance;
        }
    }
}