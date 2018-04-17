/********************************************************           
BEDROCKFRAMEWORK : https://github.com/GainDeveloper/BedrockFramework

Scene Service. Handles loading scenes, subscenes and sending out the correct messaging.
********************************************************/
using UnityEngine;
#if (UNITY_EDITOR)
using UnityEditor;
using System.IO;
#endif

namespace BedrockFramework.Scenes
{
    [System.Serializable]
    public class SceneDefinition : ScriptableObject
    {
        public SceneField primaryScene;
        public SceneField[] additionalScenes = new SceneField[] { };

#if (UNITY_EDITOR)
        public static SceneDefinition FromPath(string path)
        {
            if (path == "")
                return null;

            return AssetDatabase.LoadAssetAtPath<SceneDefinition>(AssetPathFromScenePath(path));
        }

        public static SceneDefinition CreateFromScene(UnityEngine.SceneManagement.Scene scene)
        {
            SceneDefinition asset = ScriptableObject.CreateInstance<SceneDefinition>();
            asset.primaryScene = new SceneField(AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path));

            string assetPath = AssetPathFromScenePath(scene.path);

            string projectDirectory = Path.GetDirectoryName(Application.dataPath);
            string directory = Path.Combine(projectDirectory, Path.GetDirectoryName(assetPath));
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            AssetDatabase.CreateAsset(asset, assetPath);

            return asset;
        }

        private static string AssetPathFromScenePath(string path)
        {
            string filename = Path.GetFileNameWithoutExtension(path);
            string directory = Path.Combine(Path.GetDirectoryName(path), filename);
            return Path.Combine(directory, "SceneDefinition" + ".asset");
        }
#endif
    }
}