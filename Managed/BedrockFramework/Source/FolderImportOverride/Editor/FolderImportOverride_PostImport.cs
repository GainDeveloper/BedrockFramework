using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

namespace BedrockFramework.FolderImportOverride
{
    public class FolderImportOverride_PostImport : AssetPostprocessor
    {
        public delegate void AssetImported(Object importedAsset);
        public static event AssetImported OnAssetImported = delegate { };

        public static FolderImportOverride_FolderSettings GetAssetFolderSettings(string assetPath)
        {
            if (!assetPath.Contains("Assets/"))
                return null;

            string assetDirectory = Path.GetDirectoryName(assetPath);
            string projectPath = Path.GetDirectoryName(Application.dataPath);

            while (!string.IsNullOrEmpty(assetDirectory) && Directory.Exists(Path.Combine(projectPath, assetDirectory).Replace('\\', '/')))
            {
                List<string> assetPaths = AssetDatabase.FindAssets("t:FolderImportOverride_FolderSettings", new string[] { assetDirectory })
                    .Select(x => AssetDatabase.GUIDToAssetPath(x)).Where(x => !x.Replace(assetDirectory + "/", "").Contains("/")).ToList();

                foreach (string folderSettingsPath in assetPaths)
                {
                    return AssetDatabase.LoadAssetAtPath<FolderImportOverride_FolderSettings>(folderSettingsPath);
                }

                assetDirectory = Path.GetDirectoryName(assetDirectory);
            }

            return null;
        }

        void OnPreprocessModel()
        {
            FolderImportOverride_FolderSettings folderSettings = GetAssetFolderSettings(assetPath);
            if (folderSettings == null)
                return;

            ModelImporter modelImport = (ModelImporter)assetImporter;
            folderSettings.OverrideModelImporter(modelImport);
        }

        void OnPostprocessModel(GameObject gameObject)
        {
            FolderImportOverride_FolderSettings folderSettings = GetAssetFolderSettings(assetPath);
            if (folderSettings == null)
                return;

            folderSettings.PostModelImport(gameObject);
        }

        static void OnPostprocessAssetDeleted(string assetPath)
        {
            FolderImportOverride_FolderSettings folderSettings = GetAssetFolderSettings(assetPath);
            if (folderSettings == null)
                return;

            folderSettings.AssetDeleted(assetPath);
        }

        static void OnPostprocessAssetImported(string assetPath)
        {
            FolderImportOverride_FolderSettings folderSettings = GetAssetFolderSettings(assetPath);
            if (folderSettings == null)
                return;

            folderSettings.AssetImported(assetPath);
        }

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            AssetDatabase.StartAssetEditing();

            foreach (string str in deletedAssets)
            {
                OnPostprocessAssetDeleted(str);
            }

            foreach (string str in importedAssets)
            {
                OnPostprocessAssetImported(str);
            }

            foreach (string str in movedFromAssetPaths)
            {
                OnPostprocessAssetDeleted(str);
            }

            AssetDatabase.StopAssetEditing();

            foreach (string str in importedAssets)
                OnAssetImported(AssetDatabase.LoadAssetAtPath<Object>(str));
        }
    }
}