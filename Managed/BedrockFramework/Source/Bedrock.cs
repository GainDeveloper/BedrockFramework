/********************************************************           
BEDROCKFRAMEWORK : https://github.com/GainDeveloper/BedrockFramework

Service locator. Used by services to register themselves. 
********************************************************/
using UnityEngine;

namespace BedrockFramework
{
    public class Bedrock : MonoBehaviour
    {
        private Scenes.ISceneService sceneService;
        public Scenes.ISceneService SceneService { get { return sceneService; } }
        public void RegisterSceneService(Scenes.ISceneService service)
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
        protected virtual void Awake()
        {
            sceneService = new Scenes.NullSceneService();
        }
    }
}