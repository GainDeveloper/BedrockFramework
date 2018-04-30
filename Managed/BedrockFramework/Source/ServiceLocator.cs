/********************************************************           
BEDROCKFRAMEWORK : https://github.com/GainDeveloper/BedrockFramework

Service locator. Used by services to register themselves. 
********************************************************/
using UnityEngine;

namespace BedrockFramework
{
    public static class ServiceLocator
    {
        // Scene Service
        private static Scenes.ISceneService sceneService;
        public static Scenes.ISceneService SceneService { get { return sceneService; } }
        public static void RegisterSceneService(Scenes.ISceneService service)
        {
            if (service == null)
            {
                sceneService = new Scenes.NullSceneService();
            } else
            {
                sceneService = service;
            }
        }

        // Pool Service
        private static Pool.IPoolService poolService;
        public static Pool.IPoolService PoolService { get { return poolService; } }
        public static void RegisterPoolService(Pool.IPoolService service)
        {
            if (service == null)
            {
                poolService = new Pool.NullPoolService();
            }
            else
            {
                poolService = service;
            }
        }

        // Save Service
        private static Saves.ISaveService saveService;
        public static Saves.ISaveService SaveService { get { return saveService; } }
        public static void RegisterSaveService(Saves.ISaveService service)
        {
            if (service == null)
            {
                saveService = new Saves.NullSaveService();
            }
            else
            {
                saveService = service;
            }
        }

        // Game Service
        private static GameMode.IGameModeService gameModeService;
        public static GameMode.IGameModeService GameModeService { get { return gameModeService; } }
        public static void RegisterGameModeService(GameMode.IGameModeService service)
        {
            if (service == null)
            {
                gameModeService = new GameMode.NullGameModeService();
            }
            else
            {
                gameModeService = service;
            }
        }

        // Network Service
        private static Network.INetworkService networkService;
        public static Network.INetworkService NetworkService { get { return networkService; } }
        public static void RegisterNetworkService(Network.INetworkService service)
        {
            if (service == null)
            {
                networkService = new Network.NullNetworkService();
            }
            else
            {
                networkService = service;
            }
        }

        // Register null services as a fallback.
        static ServiceLocator()
        {
            RegisterSceneService(null);
            RegisterPoolService(null);
            RegisterSaveService(null);
            RegisterGameModeService(null);
            RegisterNetworkService(null);
        }
    }
}