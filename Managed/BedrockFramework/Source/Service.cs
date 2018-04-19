/********************************************************           
BEDROCKFRAMEWORK : https://github.com/GainDeveloper/BedrockFramework

Service locator. Used by services to register themselves. 
********************************************************/
using UnityEngine;

namespace BedrockFramework
{
    public class Service
    {
        protected MonoBehaviour owner;

        public Service(MonoBehaviour owner)
        {
            this.owner = owner;
        }
    }
}