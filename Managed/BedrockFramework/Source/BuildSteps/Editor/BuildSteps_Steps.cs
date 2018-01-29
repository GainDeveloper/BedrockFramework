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

        public virtual UnityEngine.Object[] AssetsToModify()
        {
            return new UnityEngine.Object[0];
        }

        public virtual void ModifyAsset(UnityEngine.Object assetsToModify)
        {

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

    public class BuildSteps_ChangeMaterialColor : BuildSteps_Steps
    {
        public Color newColor;

        public override UnityEngine.Object[] AssetsToModify()
        {
            return AssetDatabase.FindAssets("t:material").
                Select(x => AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(AssetDatabase.GUIDToAssetPath(x))).
                Where(x => x.GetType() == typeof(Material)).ToArray();
        }

        public override void ModifyAsset(UnityEngine.Object assetToModify)
        {
            Material materialAsset = assetToModify as Material;
            materialAsset.color = newColor;
        }
    }
}