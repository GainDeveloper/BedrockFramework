/********************************************************           
BEDROCKFRAMEWORK : https://github.com/GainDeveloper/BedrockFramework

Service locator. Used by services to register themselves. 
********************************************************/
using UnityEngine;

namespace BedrockFramework
{
    public static class Bedrock
    {
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

        // Register null services as a fallback.
        static Bedrock()
        {
            sceneService = new Scenes.NullSceneService();
        }
    }
}