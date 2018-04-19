using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using BedrockFramework.FolderImportOverride;

namespace BedrockFramework.CustomLine
{
    [EditorOnlyComponent, ExecuteInEditMode, DisallowMultipleComponent]
    [RequireComponent(typeof(CustomCurve)), RequireComponent(typeof(MeshFilter)), RequireComponent(typeof(MeshRenderer))]
    [AddComponentMenu("BedrockFramework/Curve Decorator")]
    [HideMonoScript]
    public class CustomCurveDecorator : MonoBehaviour
    {
        public struct DecoratorGameObject
        {
            public GameObject gameObject;
            public float xScale;

            public DecoratorGameObject(GameObject newGameObject, float newXScale)
            {
                gameObject = newGameObject;
                xScale = newXScale;
            }
        }

        CustomCurve curve;
        MeshFilter mf;
        MeshRenderer mr;
        Mesh curveMesh;

        [TitleGroup("Curve Pieces")]
        [OnValueChanged("Curve_OnCurveModified"), HideLabel, PreviewField(75, ObjectFieldAlignment.Left), HorizontalGroup("Curve Pieces/Split"), AssetsOnly]
        public GameObject startGameObject;

        [OnValueChanged("Curve_OnCurveModified"), HideLabel, PreviewField(75, ObjectFieldAlignment.Left), HorizontalGroup("Curve Pieces/Split"), AssetsOnly]
        public GameObject middleGameObject;

        [OnValueChanged("Curve_OnCurveModified"), HideLabel, PreviewField(75, ObjectFieldAlignment.Left), HorizontalGroup("Curve Pieces/Split"), AssetsOnly]
        public GameObject endGameObject;

        void OnEnable()
        {
            curve = GetComponent<CustomCurve>();
            mf = GetComponent<MeshFilter>();
            mr = GetComponent<MeshRenderer>();

            GetCurveMesh();

            curve.OnCurveModified += Curve_OnCurveModified;
            FolderImportOverride_PostImport.OnAssetImported += AssetImported;
        }

        private void AssetImported(UnityEngine.Object importedAsset)
        {
            if (importedAsset == startGameObject ||
                importedAsset == endGameObject ||
                importedAsset == middleGameObject)
                RebuildCurveMesh(true);
        }

        void OnDisable()
        {
            curve.OnCurveModified -= Curve_OnCurveModified;
            FolderImportOverride_PostImport.OnAssetImported -= AssetImported;
        }

        private void Curve_OnCurveModified()
        {
            RebuildCurveMesh();
        }

        private void GetCurveMesh()
        {
            string expectedName = gameObject.name + "_CurveMesh_" + GetInstanceID();

            if (mf.sharedMesh == null || mf.sharedMesh.name != expectedName)
            {
                mf.sharedMesh = new Mesh();
                mf.sharedMesh.name = expectedName;
                curveMesh = mf.sharedMesh;

                RebuildCurveMesh();
            }

            curveMesh = mf.sharedMesh;
        }

        private int currentVertexCount;
        private float currentPosition;
        private DecoratorGameObject[] previousCurveGameObjects;
        private Dictionary<Material, int> currentMaterialTriangleCount;

        private void RebuildCurveMesh(bool forceNewMesh = false)
        {
            currentVertexCount = 0;
            currentPosition = 0;

            DecoratorGameObject[] curveGameObjects = GetGameObjectsToAdd();
            bool requiresNewMesh = !MatchingPreviousGameObjects(curveGameObjects) || forceNewMesh;

            currentMaterialTriangleCount = BuildGameObjectsMaterials(curveGameObjects);
            Vector3[] curveVertices = new Vector3[GetGameObjectsVertexCount(curveGameObjects)];
            Vector3[] curveNormals = new Vector3[curveVertices.Length];
            Vector2[] curveUVs0 = new Vector2[curveVertices.Length];
            Dictionary<Material, int[]> curveMaterialTrianges = new Dictionary<Material, int[]>();

            if (requiresNewMesh)
            {
                curveMesh.Clear();
                BuildGameObjectsMaterialsTriangleCount(curveGameObjects, ref curveMaterialTrianges);
            }
            

            for (int i = 0; i < curveGameObjects.Length; i++)
                PlaceMeshOnCurve(curveGameObjects[i], ref curveVertices, ref curveNormals, ref curveMaterialTrianges, ref curveUVs0, requiresNewMesh);

            // Update Mesh
            curveMesh.vertices = curveVertices;
            curveMesh.normals = curveNormals;
            mr.sharedMaterials = currentMaterialTriangleCount.Keys.ToArray();

            if (requiresNewMesh)
            {
                curveMesh.uv = curveUVs0;
                curveMesh.subMeshCount = curveMaterialTrianges.Count;
                for (int i = 0; i < curveMaterialTrianges.Count; i++)
                    curveMesh.SetTriangles(curveMaterialTrianges.Values.ElementAt(i), i);
            }

            curveMesh.RecalculateBounds();
            curveMesh.RecalculateTangents();
            previousCurveGameObjects = curveGameObjects;
        }

        private bool MatchingPreviousGameObjects(DecoratorGameObject[] gameObjects)
        {
            if (previousCurveGameObjects == null)
                return false;

            if (gameObjects.Length != previousCurveGameObjects.Length)
                return false;

            for (int i = 0; i < gameObjects.Length; i++)
            {
                if (gameObjects[i].gameObject != previousCurveGameObjects[i].gameObject)
                    return false;
            }

            return true;
        }

        private DecoratorGameObject[] GetGameObjectsToAdd()
        {
            List<DecoratorGameObject> gameObjects = new List<DecoratorGameObject>();

            if (startGameObject != null)
                gameObjects.Add(new DecoratorGameObject(startGameObject, 1));

            if (endGameObject != null)
                gameObjects.Add(new DecoratorGameObject(endGameObject, 1));

            float remainingDistance = curve.CurveLength() - GetGameObjectsLength(gameObjects.ToArray());
            int middleStartIndex = startGameObject != null ? 1 : 0;

            if (middleGameObject != null)
            {
                float middleLength = middleGameObject.GetComponent<MeshFilter>().sharedMesh.bounds.size.x;
                float middleCount = remainingDistance / middleLength;
                int fullMiddleCount = Mathf.FloorToInt(middleCount);

                float middleScale = 1 + (middleCount % 1) / fullMiddleCount;

                for (int i = 0; i < fullMiddleCount; i++)
                {
                    gameObjects.Insert(middleStartIndex, new DecoratorGameObject(middleGameObject, middleScale));
                }
            }
                

            return gameObjects.ToArray();
        }

        private int GetGameObjectsVertexCount(DecoratorGameObject[] gameobjects)
        {
            int vertexCount = 0;
            for (int i = 0; i < gameobjects.Length; i++)
                vertexCount += gameobjects[i].gameObject.GetComponent<MeshFilter>().sharedMesh.vertexCount;
            return vertexCount;
        }

        private Dictionary<Material, int> BuildGameObjectsMaterials(DecoratorGameObject[] gameobjects)
        {
            Dictionary<Material, int> newMaterialToTriangles = new Dictionary<Material, int>();

            for (int i = 0; i < gameobjects.Length; i++)
            {
                foreach (Material material in gameobjects[i].gameObject.GetComponent<MeshRenderer>().sharedMaterials)
                    newMaterialToTriangles[material] = 0;
            }
            return newMaterialToTriangles;
        }

        private void BuildGameObjectsMaterialsTriangleCount(DecoratorGameObject[] gameobjects, ref Dictionary<Material, int[]> newMaterialToTriangles)
        {
            for (int i = 0; i < gameobjects.Length; i++)
            {
                Material[] gameObjectsMaterials = gameobjects[i].gameObject.GetComponent<MeshRenderer>().sharedMaterials;

                for (int x = 0; x < gameObjectsMaterials.Length; x++)
                {
                    int existingCount = 0;
                    if (newMaterialToTriangles.ContainsKey(gameObjectsMaterials[x]))
                        existingCount = newMaterialToTriangles[gameObjectsMaterials[x]].Length;

                    int numTriangles = gameobjects[i].gameObject.GetComponent<MeshFilter>().sharedMesh.GetTriangles(x).Length;

                    newMaterialToTriangles[gameObjectsMaterials[x]] = new int[existingCount + numTriangles];
                }
            }
        }

        private int GetGameObjectsTriangleCount(DecoratorGameObject[] gameobjects)
        {
            int triangleCount = 0;
            for (int i = 0; i < gameobjects.Length; i++)
                triangleCount += gameobjects[i].gameObject.GetComponent<MeshFilter>().sharedMesh.triangles.Length;
            return triangleCount;
        }

        private float GetGameObjectsLength(DecoratorGameObject[] gameobjects)
        {
            float length = 0;
            for (int i = 0; i < gameobjects.Length; i++)
                length += gameobjects[i].gameObject.GetComponent<MeshFilter>().sharedMesh.bounds.size.x;
            return length;
        }

        private void PlaceMeshOnCurve(DecoratorGameObject meshGameObject, ref Vector3[] vertices, ref Vector3[] normals, ref Dictionary<Material, int[]> triangles, ref Vector2[] curveUVs0, 
            bool updateTriangles)
        {
            Mesh placedGameObjectMesh = meshGameObject.gameObject.GetComponent<MeshFilter>().sharedMesh;
            Material[] gameObjectsMaterials = meshGameObject.gameObject.GetComponent<MeshRenderer>().sharedMaterials;

            for (int i = 0; i < placedGameObjectMesh.vertexCount; i++)
            {
                float t;
                vertices[currentVertexCount + i] = MapMeshPositionToCurve(placedGameObjectMesh.vertices[i], meshGameObject.xScale, out t);
                normals[currentVertexCount + i] = TransformNormalToCurve(placedGameObjectMesh.normals[i], t);

                if (updateTriangles)
                    curveUVs0[currentVertexCount + i] = placedGameObjectMesh.uv[i];
            }

            if (updateTriangles)
            {
                for (int x = 0; x < gameObjectsMaterials.Length; x++)
                {
                    int materialTriangleCount = currentMaterialTriangleCount[gameObjectsMaterials[x]];
                    int[] materialTriangles = placedGameObjectMesh.GetTriangles(x);

                    for (int i = 0; i < materialTriangles.Length; i++)
                    {
                        triangles[gameObjectsMaterials[x]][materialTriangleCount + i] = currentVertexCount + materialTriangles[i];
                    }
                }
            }


            currentVertexCount += placedGameObjectMesh.vertexCount;

            // Increment triangle count.
            for (int x = 0; x < gameObjectsMaterials.Length; x++)
                currentMaterialTriangleCount[gameObjectsMaterials[x]] += placedGameObjectMesh.GetTriangles(x).Length;

            currentPosition += placedGameObjectMesh.bounds.size.x * meshGameObject.xScale;
        }

        private Vector3 MapMeshPositionToCurve(Vector3 meshPosition, float xScale, out float t)
        {
            t = curve.DistanceToT(currentPosition + meshPosition.x * xScale);

            Vector3 toReturn = curve.GetPoint(t, false);
            toReturn += curve.GetNormal(t, false) * meshPosition.y;
            toReturn += curve.GetBiNormal(t, false) * meshPosition.z;

            return toReturn;
        }

        private Vector3 TransformNormalToCurve(Vector3 normal, float t)
        {
            return Quaternion.LookRotation(curve.GetBiNormal(t, false), curve.GetNormal(t, false)) * normal;
        }
    }
}