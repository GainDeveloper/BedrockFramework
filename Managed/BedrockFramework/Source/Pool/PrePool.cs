using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BedrockFramework.Pool
{
    [CreateAssetMenu(fileName = "PrePool", menuName = "BedrockFramework/PrePool", order = 0)]
    public class PrePool : ScriptableObject
    {
        [System.Serializable]
        public class PrePoolObject
        {
            public GameObject prefab;
            public int prePoolCount = 1;
        }

        public PrePoolObject[] prePooledObjects;
    }
}