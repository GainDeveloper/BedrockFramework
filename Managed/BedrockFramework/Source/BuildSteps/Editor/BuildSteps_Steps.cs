using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using System;

namespace BedrockFramework.BuildSteps
{
    public class BuildSteps_Steps
    {
        public virtual void OnSceneBuild()
        {
            Logger.Logger.Log("Build", "OnSceneBuild");
        }
    }

    public class BuildSteps_DeleteEditorComponents : BuildSteps_Steps
    {
        public override void OnSceneBuild()
        {
            MonoBehaviour[] sceneActive = GameObject.FindObjectsOfType<MonoBehaviour>();

            foreach (MonoBehaviour mono in sceneActive)
            {
                EditorOnlyComponent attribute = mono.GetType().GetCustomAttributes(typeof(EditorOnlyComponent), true).FirstOrDefault() as EditorOnlyComponent;

                if (attribute != null)
                    GameObject.DestroyImmediate(mono);
            }
        }
    }
}