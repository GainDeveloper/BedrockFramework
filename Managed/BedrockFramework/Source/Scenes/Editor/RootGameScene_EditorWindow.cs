/********************************************************           
BEDROCKFRAMEWORK : https://github.com/GainDeveloper/BedrockFramework

Creates a utility window displaying all scenes marked as a root game scene.
********************************************************/

using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace BedrockFramework.Scenes
{
    public class RootGameScene_EditorWindow : EditorWindow
    {
        static List<string> gameScenes = new List<string>();

        [MenuItem("Tools/Open Game Scenes")]
        public static void OpenGameScenes()
        {
            RootGameScene_EditorWindow window = (RootGameScene_EditorWindow)EditorWindow.GetWindow(typeof(RootGameScene_EditorWindow), true, "Game Scenes");
        }

        void OnEnable()
        {
            gameScenes.Clear();

            foreach (string sceneAssetPath in AssetDatabase.FindAssets("t:scene").Select(s => AssetDatabase.GUIDToAssetPath(s)))
            {
                FolderImportOverride.ImportOverideAction_SceneCache.SceneCache_Data sceneCacheData = FolderImportOverride.ImportOverideAction_SceneCache.SceneCache_Data.Deserialize(AssetImporter.GetAtPath(sceneAssetPath).userData);
                if (sceneCacheData != null && sceneCacheData.isRootGameScene)
                    gameScenes.Add(sceneAssetPath);
            }
        }

        void OnGUI()
        {
            for (int i = 0; i < gameScenes.Count; i++)
            {
                if (GUILayout.Button(gameScenes[i]))
                {
                    if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        EditorSceneManager.OpenScene(gameScenes[i]);

                    this.Close();
                }
            }
        }
    }
}