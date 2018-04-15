/********************************************************           
BEDROCKFRAMEWORK : https://github.com/GainDeveloper/BedrockFramework

Service locator. Used by services to register themselves. 
********************************************************/
using UnityEngine;

namespace BedrockFramework
{
    public class BedrockService
    {
        protected MonoBehaviour owner;

        public BedrockService(MonoBehaviour owner)
        {
            this.owner = owner;
        }
    }
}