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
        struct GameScenePath
        {
            public GameScenePath(string name, string path)
            {
                sceneName = name;
                scenePath = path;
            }

            public string sceneName;
            public string scenePath;
        }

        static List<GameScenePath> gameScenes = new List<GameScenePath>();

        [MenuItem("Tools/Open Game Scenes")]
        public static void OpenGameScenes()
        {
            RootGameScene_EditorWindow window = (RootGameScene_EditorWindow)EditorWindow.GetWindow(typeof(RootGameScene_EditorWindow), true, "Game Scenes");
        }

        void OnEnable()
        {
            gameScenes.Clear();

            foreach (string sceneAssetPath in AssetDatabase.FindAssets("t:sceneDefinition").Select(s => AssetDatabase.GUIDToAssetPath(s)))
            {
                SceneDefinition sceneDefinition = AssetDatabase.LoadAssetAtPath<SceneDefinition>(sceneAssetPath);
                gameScenes.Add(new GameScenePath(sceneDefinition.PrimaryScene, sceneDefinition.PrimaryScenePath()));
            }

            gameScenes.OrderBy(x => x.sceneName);
        }

        void OnGUI()
        {
            for (int i = 0; i < gameScenes.Count; i++)
            {
                if (GUILayout.Button(gameScenes[i].sceneName))
                {
                    if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        EditorSceneManager.OpenScene(gameScenes[i].scenePath);

                    this.Close();
                }
            }
        }
    }
}