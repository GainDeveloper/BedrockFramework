using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using BedrockFramework.Utilities;

namespace BedrockFramework
{
    [CustomEditor(typeof(SceneAsset), true)]
    public class Scene_Inspector : Editor
    {
        SceneAsset editTarget;
        FolderImportOverride.ImportOverideAction_SceneCache.SceneCache_Data sceneCacheData;

        void OnEnable()
        {
            editTarget = target as SceneAsset;

            string userData = AssetImporter.GetAtPath(AssetDatabase.GetAssetOrScenePath(editTarget)).userData;
            sceneCacheData = FolderImportOverride.ImportOverideAction_SceneCache.SceneCache_Data.Deserialize(userData);
        }

        public override void OnInspectorGUI()
        {
            if (sceneCacheData != null)
                sceneCacheData.OnInspectorGUI();
        }
    }
}