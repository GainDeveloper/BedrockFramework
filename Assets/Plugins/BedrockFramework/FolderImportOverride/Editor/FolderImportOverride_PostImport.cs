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
        FolderImportOverride_FolderSettings GetAssetFolderSettings()
        {
            string assetDirectory = Path.GetDirectoryName(assetPath);

            while (!string.IsNullOrEmpty(assetDirectory))
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
            FolderImportOverride_FolderSettings folderSettings = GetAssetFolderSettings();
            if (folderSettings == null)
                return;

            ModelImporter modelImport = (ModelImporter)assetImporter;
            folderSettings.OverrideModelImporter(modelImport);
        }

        void OnPostprocessModel(GameObject gameObject)
        {
            FolderImportOverride_FolderSettings folderSettings = GetAssetFolderSettings();
            if (folderSettings == null)
                return;

            folderSettings.PostModelImport(gameObject);
        }
    }
}