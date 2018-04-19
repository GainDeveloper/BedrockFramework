/********************************************************           
BEDROCKFRAMEWORK : https://github.com/GainDeveloper/BedrockFramework

SceneAsset Editor
********************************************************/

using UnityEngine;
using UnityEditor;
using System.IO;

namespace BedrockFramework.Scenes
{
    public static class SceneDefinition_Editor
    {
        public static SceneDefinition FromPath(string path)
        {
            if (path == "")
                return null;

            return AssetDatabase.LoadAssetAtPath<SceneDefinition>(AssetPathFromScenePath(path));
        }

        public static SceneDefinition CreateFromScene(UnityEngine.SceneManagement.Scene scene)
        {
            SceneDefinition asset = ScriptableObject.CreateInstance<SceneDefinition>();
            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path);
            asset.primaryScene = new SceneField(sceneAsset, sceneAsset.name);

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
            return Path.Combine(directory, filename + " SceneDefinition" + ".asset");
        }

        public static string[] AllScenePaths(this SceneDefinition sceneDefinition)
        {
            string[] allScenes = new string[sceneDefinition.additionalScenes.Length + 2];

            allScenes[0] = SceneDefinition.entryScenePath;
            allScenes[1] = sceneDefinition.primaryScene.SceneFilePath();
            for (int i = 0; i < sceneDefinition.additionalScenes.Length; i++)
                allScenes[2 + i] = sceneDefinition.additionalScenes[i].SceneFilePath();
            return allScenes;
        }

        public static string[] AdditionalScenePaths(this SceneDefinition sceneDefinition)
        {
            string[] finalAdditionalScenes = new string[sceneDefinition.additionalScenes.Length];
            for (int i = 0; i < sceneDefinition.additionalScenes.Length; i++)
                finalAdditionalScenes[i] = sceneDefinition.additionalScenes[i].SceneFilePath();
            return finalAdditionalScenes;
        }

        public static string PrimaryScenePath(this SceneDefinition sceneDefinition)
        {
            return sceneDefinition.primaryScene.SceneFilePath();
        }
    }
}