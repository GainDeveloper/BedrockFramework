/********************************************************           
BEDROCKFRAMEWORK : https://github.com/GainDeveloper/BedrockFramework

********************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace BedrockFramework.Pool
{
    [CreateAssetMenu(fileName = "PoolDefinition", menuName = "BedrockFramework/PoolDefinition", order = 0)]
    public class PoolDefinition : ScriptableObject
    {
        [SerializeField, AssetsOnly, InlineEditor(InlineEditorModes.LargePreview)]
        private GameObject pooledObject;
        [SerializeField, AssetsOnly]
        private ComponentOverride[] overrides;

        public GameObject PooledObject { get { return pooledObject; } }

        public void OverrideGameObjectComponents(GameObject toOverride)
        {
            for (int i = 0; i < overrides.Length; i++)
            {
                overrides[i].OverrideGameObject(toOverride);
            }
        }
    }
}