using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

namespace BedrockFramework.FolderImportOverride
{
    public class FolderImportOverride_Modifications : UnityEditor.AssetModificationProcessor
    {
        static string[] OnWillSaveAssets(string[] paths)
        {
            foreach (string assetPath in paths)
            {
                FolderImportOverride_FolderSettings folderSettings = FolderImportOverride_PostImport.GetAssetFolderSettings(assetPath);
                if (folderSettings == null)
                    continue;

                folderSettings.AssetSaved(assetPath);
            }
            return paths;
        }
    }
}