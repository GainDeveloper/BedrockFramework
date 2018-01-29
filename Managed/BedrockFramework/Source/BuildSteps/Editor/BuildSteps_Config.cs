using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;

namespace BedrockFramework.BuildSteps
{
    [CreateAssetMenu(fileName = "BuildSettings", menuName = "BedrockFramework/BuildSettings", order = 0)]
    class BuildSteps_Config : Sirenix.OdinInspector.SerializedScriptableObject
    {
#pragma warning disable
        public BuildTarget buildTarget = BuildTarget.StandaloneWindows64;
        public string executableExtension;
#pragma warning restore
    }
}