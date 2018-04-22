/********************************************************           
BEDROCKFRAMEWORK : https://github.com/GainDeveloper/BedrockFramework

Handles registering the GameMode.
********************************************************/
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

namespace BedrockFramework.GameMode
{
    public interface IGameModeService
    {
        GameMode CurrentGameMode { get; }
        event Action<GameMode> OnFinishedGameSetup;
    }

    public class NullGameModeService : IGameModeService
    {
        public GameMode CurrentGameMode { get { return null; } }
        public event Action<GameMode> OnFinishedGameSetup = delegate { };
    }

    public class GameModeService : Service, IGameModeService
    {
        public event Action<GameMode> OnFinishedGameSetup = delegate { };

        private GameMode currentGameMode;

        public GameModeService(MonoBehaviour owner): base(owner)
        {
            ServiceLocator.SceneService.OnFinishedLoading += SceneService_OnFinishedLoading;
        }

        private void SceneService_OnFinishedLoading(Scenes.SceneDefinition loadedScene)
        {
            currentGameMode = loadedScene.sceneSettings.defaultGameModeInfo.GameSetup();
            OnFinishedGameSetup(currentGameMode);
        }

        public GameMode CurrentGameMode { get { return currentGameMode; } }

    }
}