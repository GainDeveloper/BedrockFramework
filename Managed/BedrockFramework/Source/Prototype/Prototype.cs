using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BedrockFramework.Prototype
{
    public class PrototypeObject : ScriptableObject
#if UNITY_EDITOR
        , ISerializationCallbackReceiver
#endif
    {
#if UNITY_EDITOR
        public PrototypeObject prototype;
        public List<string> modifiedValues = new List<string>();

        public void OnBeforeSerialize()
        {
            if (BuildSteps.BuildSteps_Settings.isBuilding)
            {
                Debug.Log("OnBeforeSerialize Compile");
            }
        }

        public void OnAfterDeserialize()
        {
            if (BuildSteps.BuildSteps_Settings.isBuilding)
            {
                Debug.Log("OnAfterDeserialize Compile");
            }
        }
#endif
    }
}