using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;

using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;


namespace BedrockFramework.BuildSteps
{
    class BuildSteps_Editor : OdinMenuEditorWindow
    {
        [MenuItem("Tools/BuildSteps")]
        static void Init()
        {
            BuildSteps_Editor window = (BuildSteps_Editor)EditorWindow.GetWindow(typeof(BuildSteps_Editor), false, "Build Steps");
            window.Show();
        }

        protected override OdinMenuTree BuildMenuTree()
        {
            OdinMenuTree tree = new OdinMenuTree(supportsMultiSelect: true) { };

            tree.AddAssetAtPath("Build", "Configs/Editor/Build.asset");

            tree.AddAllAssetsAtPath("", "", typeof(BuildSteps_Config), includeSubDirectories: true, flattenSubDirectories: true)
    .AddThumbnailIcons();
            return tree;
        }
    }

    [CreateAssetMenu(fileName = "Build", menuName = "BedrockFramework/Build", order = 0)]
    class BuildSteps_Settings : Sirenix.OdinInspector.SerializedScriptableObject
    {
        public static bool isBuilding = false;

#pragma warning disable
        [AssetList]
        public BuildSteps_SettingsPreset buildPreset;
#pragma warning restore


        [InfoBox("$OutputPath")]
        [BoxGroup("Output Settings")]
        [FolderPath]
        public string outputFolder = "/";


        [BoxGroup("Output Settings")]
        [HorizontalGroup("Output Settings/More")]
        public bool appendType = true;
        [BoxGroup("Output Settings")]
        [HorizontalGroup("Output Settings/More")]
        public bool appendRevision = true;
        [BoxGroup("Output Settings")]
        public string appendRemark = "Test";

#pragma warning disable
        [AssetList()]
        public List<BuildSteps_Config> buildPlatforms;
        [AssetList()]
        public List<SceneAsset> buildScenes;
#pragma warning restore


        [Button("Build Selected", ButtonSizes.Medium)]
        public void Build()
        {
            isBuilding = true;
            EditorApplication.delayCall += () => DelayedBuild();
        }

        void DelayedBuild()
        {
            // Modify Assets
            foreach (BuildSteps_Steps step in buildPreset.buildSteps)
            {
                Object[] toModify = step.AssetsToModify();

                if (toModify.Count() > 0)
                {
                    Undo.RecordObjects(toModify, "BuildModifications");
                    for (int i = 0; i < toModify.Count(); i++)
                        step.ModifyAsset(toModify[i]);
                }
            }

            foreach (BuildSteps_Config buildPlatform in buildPlatforms)
            {
                BuildPipeline.BuildPlayer(buildScenes.Select(x => AssetDatabase.GetAssetPath(x)).ToArray(),
                    OutputPath() + buildPlatform.executableExtension, buildPlatform.buildTarget, buildPreset.buildOptions);
            }

            // Revert Modified Assets
            Undo.PerformUndo();
            isBuilding = false;
        }

        private string OutputPath()
        {
            return Path.Combine(outputFolder, ExecutableName());
        }

        private string ExecutableName()
        {
            List<string> parts = new List<string> { Application.productName };

            if (!string.IsNullOrEmpty(appendRemark))
                parts.Add(appendRemark);
            if (appendType)
                parts.Add(buildPreset.presetName);
            if (appendRevision)
                parts.Add("324");

            return string.Join("_", parts.ToArray());
        }

        [PostProcessSceneAttribute]
        public static void OnPostProcessScene()
        {
            if (!isBuilding)
                return;

            foreach (BuildSteps_Steps step in AssetDatabase.LoadAssetAtPath<BuildSteps_Settings>("Assets/Configs/Editor/Build.asset").buildPreset.buildSteps)
                step.OnSceneBuild();
        }
    }

    [CreateAssetMenu(fileName = "BuildPreset", menuName = "BedrockFramework/BuildPreset", order = 0)]
    class BuildSteps_SettingsPreset : Sirenix.OdinInspector.SerializedScriptableObject
    {
#pragma warning disable
        public string presetName;
        public BuildOptions buildOptions;
#pragma warning restore


        public List<BuildSteps_Steps> buildSteps = new List<BuildSteps_Steps>();
    }
}