using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
using SmartCombine;

namespace BedrockFramework.FolderImportOverride
{
    [CreateAssetMenu(fileName = "FolderAction", menuName = "BedrockFramework/FolderAction", order = 0)]
    class FolderImportOverride_Actions : SerializedScriptableObject
    {
        [SerializeField]
#pragma warning disable
        private ImportOverideAction overrideAction;
#pragma warning restore

        public void InvokePreAction(AssetImporter assetImporter)
        {
            overrideAction.InvokePreAction(assetImporter);
        }

        public void InvokePostAction(UnityEngine.Object gameObject)
        {
            overrideAction.InvokePostAction(gameObject);
        }

        public void InvokeDeleteAction(string assetPath)
        {
            overrideAction.InvokeDeleteAction(assetPath);
        }

        public void InvokeImportAction(string assetPath)
        {
            overrideAction.InvokeImportAction(assetPath);
        }
    }

    public class ImportOverideAction
    {
        public virtual void InvokePreAction(AssetImporter assetImporter)
        {
        }

        public virtual void InvokePostAction(UnityEngine.Object gameObject)
        {
        }

        public virtual void InvokeDeleteAction(string assetPath)
        {
        }

        public virtual void InvokeImportAction(string assetPath)
        {
        }
    }

    /// <summary>
    /// Tells the importer to search and remap materials.
    /// Removes any missing material remaps before
    /// </summary>
    public class ImportOverideAction_SearchAndRemap : ImportOverideAction
    {
        public override void InvokePreAction(AssetImporter assetImporter)
        {
            ModelImporter modelImporter = (ModelImporter)assetImporter;

            // Remove any missing remaps.
            Dictionary<AssetImporter.SourceAssetIdentifier, UnityEngine.Object> remappedMaterials = assetImporter.GetExternalObjectMap();
            foreach (KeyValuePair<AssetImporter.SourceAssetIdentifier, UnityEngine.Object> entry in remappedMaterials)
                if (entry.Value == null || entry.Key.name != entry.Value.name)
                    modelImporter.RemoveRemap(new AssetImporter.SourceAssetIdentifier(entry.Key.type, entry.Key.name));

            modelImporter.SearchAndRemapMaterials(modelImporter.materialName, modelImporter.materialSearch);
        }
    }

    /// <summary>
    /// Removes all GameObjects that only contain a transform.
    /// </summary>
    public class ImportOverideAction_DeleteEmptyGameObjects : ImportOverideAction
    {
        //TODO: Might need to cover cases where child GameObjects are required.
        //TODO: Might need to add a suffix/prefix that disables this for certain GameObjects.
        public override void InvokePostAction(UnityEngine.Object gameObject)
        {
            List<GameObject> toDestroy = new List<GameObject>();

            foreach(Transform transform in ((GameObject)gameObject).GetComponentsInChildren<Transform>())
                if (transform.GetComponents<Component>().Length == 1)
                    toDestroy.Add(transform.gameObject);

            // If we have a skinned mesh renderer then ignore any transforms part of the root hierarchy.
            SkinnedMeshRenderer smr = ((GameObject)gameObject).GetComponentInChildren<SkinnedMeshRenderer>();
            if (smr != null && smr.rootBone != null)
            {
                toDestroy = toDestroy.Except(smr.rootBone.GetComponentsInChildren<Transform>().Select(x => x.gameObject)).ToList();
            }

            foreach (GameObject gameObjectToDestroy in toDestroy)
                GameObject.DestroyImmediate(gameObjectToDestroy);
        }
    }

    /// <summary>
    /// Merges all meshes in this GameObject and stores the combined mesh on the root GameObject.
    /// </summary>
    public class ImportOverideAction_MergeMeshes : ImportOverideAction
    {
        public override void InvokePostAction(UnityEngine.Object importedObject)
        {
            List<UnityEngine.Object> toDestroy = new List<UnityEngine.Object>();
            List<SmartMeshData> meshData = new List<SmartMeshData>();
            Mesh original = null; // We keep one of the original meshes so we can use it to store the combined meshes.
            GameObject gameObject = (GameObject)importedObject;

            foreach (MeshFilter meshFilter in gameObject.GetComponentsInChildren<MeshFilter>())
            {
                if (meshFilter.gameObject == gameObject)
                    continue;

                MeshRenderer meshRenderer = meshFilter.GetComponent<MeshRenderer>();
                Transform meshTransform = meshFilter.GetComponent<Transform>();
                meshData.Add(new SmartMeshData(meshFilter.sharedMesh, meshRenderer.sharedMaterials, meshTransform.localToWorldMatrix));

                if (original == null)
                    original = meshFilter.sharedMesh;
                else
                    toDestroy.Add(meshFilter.sharedMesh);

                toDestroy.Add(meshRenderer);
                toDestroy.Add(meshFilter);
            }

            if (toDestroy.Count == 0)
                return;

            Mesh combinedMesh = new Mesh();
            Material[] combinedMaterials;
            combinedMesh.CombineMeshesSmart(meshData.ToArray(), out combinedMaterials);
            original.CopyFromMesh(combinedMesh);

            gameObject.AddComponent<MeshFilter>().mesh = original;
            gameObject.AddComponent<MeshRenderer>().sharedMaterials = combinedMaterials;

            foreach (UnityEngine.Object toDestroyObject in toDestroy)
                UnityEngine.Object.DestroyImmediate(toDestroyObject);
        }
    }

    /// <summary>
    /// Creates a new component of the users choice on the root GameObject.
    /// </summary>
    public class ImportOverideAction_AddComponents : ImportOverideAction
    {
        private static readonly Type BaseType = typeof(Component);
#pragma warning disable
        private static readonly List<Type> Types =
            AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => BaseType.IsAssignableFrom(t))
                .ToList();
#pragma warning restore

        [ValueDropdown("Types")]
        public List<Type> components = new List<Type>();

        public override void InvokePostAction(UnityEngine.Object gameObject)
        {
            foreach (Type componentType in components)
                ((GameObject)gameObject).AddComponent(componentType);
        }
    }

    /// <summary>
    /// Assigns the specified layer and tag to the root GameObject.
    /// </summary>
    public class ImportOverideAction_SetTagLayer : ImportOverideAction
    {
#pragma warning disable
        private static List<string> GetLayers()
        {
            return Enumerable.Range(0, 32)
                .Select(i => LayerMask.LayerToName(i))
                .Where(s => s.Length > 0)
                .ToList();
        }

        private static List<string> GetTags()
        {
            return UnityEditorInternal.InternalEditorUtility.tags.ToList();
        }
#pragma warning restore

        [ValueDropdown("GetTags")]
        public string tag;

        [ValueDropdown("GetLayers")]
        public string layer;

        //TODO: Might need to add a suffix/prefix that disables this for certain GameObjects.
        public override void InvokePostAction(UnityEngine.Object importedObject)
        {
            GameObject gameObject = (GameObject)importedObject;
            gameObject.tag = tag;
            gameObject.layer = LayerMask.NameToLayer(layer);
        }
    }

    /// <summary>
    /// Removes any material remaps to this material and reimports the mesh.
    /// </summary>
    public class ImportOverideAction_UpdateModelsWithDeletedMaterials : ImportOverideAction
    {
        public override void InvokeDeleteAction(string assetPath)
        {
            if (Path.GetExtension(assetPath) != ".mat")
                return;

            string materialName = Path.GetFileNameWithoutExtension(assetPath).ToLower();

            foreach (string modelAssetGUID in AssetDatabase.FindAssets("t:model"))
            {
                string modelAssetPath = AssetDatabase.GUIDToAssetPath(modelAssetGUID);
                ModelImporter modelImporter = (ModelImporter)AssetImporter.GetAtPath(modelAssetPath);

                // Check for any meshes that were referencing this material.
                Dictionary<AssetImporter.SourceAssetIdentifier, UnityEngine.Object> remappedMaterials = modelImporter.GetExternalObjectMap();
                foreach (KeyValuePair<AssetImporter.SourceAssetIdentifier, UnityEngine.Object> entry in remappedMaterials)
                {
                    if (entry.Key.name.ToLower() == materialName)
                    {
                        AssetDatabase.ImportAsset(modelAssetPath);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Reimports any meshes that hold the same material name internally as the one that has been imported.
    /// </summary>
    public class ImportOverideAction_UpdateModelsInternalMaterials : ImportOverideAction
    {
        public override void InvokeImportAction(string assetPath)
        {
            if (Path.GetExtension(assetPath) != ".mat")
                return;

            string materialName = Path.GetFileNameWithoutExtension(assetPath).ToLower();

            foreach (string modelAssetGUID in AssetDatabase.FindAssets(materialName + " t:material"))
            {
                // We search for materials and then narrow it down to GameObjects containing materials (internal assets).
                // This should be faster than going through all models and loading them to get internal assets.
                string materialAssetPath = AssetDatabase.GUIDToAssetPath(modelAssetGUID);
                if (AssetDatabase.GetMainAssetTypeAtPath(materialAssetPath) != typeof(GameObject))
                    continue;

                foreach (UnityEngine.Object modelAssetObject in AssetDatabase.LoadAllAssetsAtPath(materialAssetPath))
                    if (modelAssetObject.name.ToLower() == materialName && modelAssetObject.GetType() == typeof(Material))
                    {
                        AssetDatabase.ImportAsset(materialAssetPath);
                        break;
                    }
            }
        }
    }

    /// <summary>
    /// Reimports any meshes that hold the same material name internally as the one that has been imported.
    /// </summary>
    public class ImportOverideAction_EnsureUniqueAssetName : ImportOverideAction
    {
        public string extensionMask = ".mat";

        public override void InvokeImportAction(string importedObjectPath)
        {
            if (Path.GetExtension(importedObjectPath) != extensionMask)
                return;

            UnityEngine.Object importedObject = AssetDatabase.LoadAssetAtPath(importedObjectPath, typeof(UnityEngine.Object));

            string importedObjectName = importedObject.name.ToLower();
            string importedObjectType = importedObject.GetType().Name;

            string[] matchingAssets = AssetDatabase.FindAssets(importedObjectName + " t:" + importedObjectType).Select(x => AssetDatabase.GUIDToAssetPath(x)).
                Where(x => x.Contains(extensionMask) && Path.GetFileNameWithoutExtension(x) == importedObjectName).ToArray();

            if (matchingAssets.Length > 1)
            {
                EditorUtility.DisplayDialog("Material Name Conflict", "Material names must be unique, the following clash:\n"+string.Join("\n", matchingAssets), "Okay");
            }
        }
    }

    /// <summary>
    /// Caches any information we require from the scene.
    /// Current caches whether the scene is marked as a root game scene.
    /// </summary>
    public class ImportOverideAction_SceneCache : ImportOverideAction
    {
        [System.Serializable]
        public class SceneCache_Data
        {
            public bool isRootGameScene = false;

            public string Serialize()
            {
                return JsonUtility.ToJson(this);
            }

            public static SceneCache_Data Deserialize(string json)
            {
                if (json.Length == 0)
                    return null;

                return JsonUtility.FromJson<SceneCache_Data>(json);
            }

            public void OnInspectorGUI()
            {
                GUI.enabled = false;
                EditorGUILayout.Toggle("Has World Info ", isRootGameScene);
                GUI.enabled = true;
            }
        }

        const string extensionMask = ".unity";

        public override void InvokeImportAction(string importedObjectPath)
        {
            if (Path.GetExtension(importedObjectPath) != extensionMask)
                return;

            SceneCache_Data cacheData = new SceneCache_Data();
            cacheData.isRootGameScene = InterfaceHelper.FindObject<IRootGameScene>() != null;

            AssetImporter.GetAtPath(importedObjectPath).userData = cacheData.Serialize();
        }
    }
}
