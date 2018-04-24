/********************************************************           
BEDROCKFRAMEWORK : https://github.com/GainDeveloper/BedrockFramework
Saves how this GameObject was instantiated.
********************************************************/

using System;
using UnityEngine;
using BedrockFramework.Pool;
using Sirenix.OdinInspector;

namespace BedrockFramework.Saves
{
    [HideMonoScript]
    public class SaveableGameObject : MonoBehaviour, IPool
    {
        [ReadOnly, ShowInInspector]
        private PoolDefinition poolDefinition;



        // Pool
        PoolDefinition IPool.PoolDefinition { set { poolDefinition = value; } }
        void IPool.OnDeSpawn() {}
        void IPool.OnSpawn() {}
    }
}