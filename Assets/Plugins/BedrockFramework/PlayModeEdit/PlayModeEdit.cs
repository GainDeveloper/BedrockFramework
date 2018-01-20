using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BedrockFramework.PlayModeEdit
{
    [DisallowMultipleComponent, ExecuteInEditMode]
    public class PlayModeEdit : MonoBehaviour
    {
        public List<Component> recordedComponents;

        public void Awake()
        {
            if (recordedComponents == null)
            {
                recordedComponents = new List<Component>();
                recordedComponents.Add(GetComponent<Transform>());
            }
        }
    }
}