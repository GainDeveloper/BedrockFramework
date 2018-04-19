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

        // Register null services as a fallback.
        static ServiceLocator()
        {
            RegisterSceneService(null);
            RegisterPoolService(null);
        }
    }
}