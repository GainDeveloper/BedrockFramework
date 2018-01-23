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
        private ImportOverideAction overrideAction;

        public void InvokePreAction(AssetImporter assetImporter)
        {
            overrideAction.InvokePreAction(assetImporter);
        }

        public void InvokePostAction(GameObject gameObject)
        {
            overrideAction.InvokePostAction(gameObject);
        }

        public void InvokeDeleteAction(string assetPath)
        {
            overrideAction.InvokeDeleteAction(assetPath);
        }
    }

    public class ImportOverideAction
    {
        public virtual void InvokePreAction(AssetImporter assetImporter)
        {
        }

        public virtual void InvokePostAction(GameObject gameObject)
        {
        }

        public virtual void InvokeDeleteAction(string assetPath)
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
                if (entry.Value == null)
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
        public override void InvokePostAction(GameObject gameObject)
        {
            List<GameObject> toDestroy = new List<GameObject>();

            foreach(Transform transform in gameObject.GetComponentsInChildren<Transform>())
                if (transform.GetComponents<Component>().Length == 1)
                    toDestroy.Add(transform.gameObject);

            foreach (GameObject gameObjectToDestroy in toDestroy)
                GameObject.DestroyImmediate(gameObjectToDestroy);
        }
    }

    /// <summary>
    /// Merges all meshes in this GameObject and stores the combined mesh on the root GameObject.
    /// </summary>
    public class ImportOverideAction_MergeMeshes : ImportOverideAction
    {
        public override void InvokePostAction(GameObject gameObject)
        {
            List<UnityEngine.Object> toDestroy = new List<UnityEngine.Object>();
            List<SmartMeshData> meshData = new List<SmartMeshData>();
            Mesh original = null; // We keep one of the original meshes so we can use it to store the combined meshes.

            foreach (MeshFilter meshFilter in gameObject.GetComponentsInChildren<MeshFilter>())
            {
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
        private static readonly Type BaseType = typeof(MonoBehaviour);
#pragma warning disable
        private static readonly List<Type> Types =
            AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => BaseType.IsAssignableFrom(t))
                .ToList();
#pragma warning restore

        [ValueDropdown("Types")]
        public List<Type> components = new List<Type>();

        public override void InvokePostAction(GameObject gameObject)
        {
            foreach (Type componentType in components)
                gameObject.AddComponent(componentType);
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

            string materialName = Path.GetFileNameWithoutExtension(assetPath);
            Debug.Log(materialName);

            //TODO: Itterate over all mesh assets, check if any of the source remaps point to a material with this name,
            // if the remap points to a null object, tell Unity to reimport the mesh asset.
        }
    }
}
