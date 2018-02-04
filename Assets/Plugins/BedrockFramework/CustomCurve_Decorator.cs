using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace BedrockFramework.CustomLine
{
    [EditorOnlyComponent, ExecuteInEditMode, DisallowMultipleComponent]
    [RequireComponent(typeof(CustomCurve)), RequireComponent(typeof(MeshFilter)), RequireComponent(typeof(MeshRenderer))]
    [AddComponentMenu("BedrockFramework/Curve Decorator")]
    public class CustomCurve_Decorator : MonoBehaviour
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

        [OnValueChanged("Curve_OnCurveModified")]
        public GameObject startGameObject;

        [OnValueChanged("Curve_OnCurveModified")]
        public GameObject middleGameObject;

        [OnValueChanged("Curve_OnCurveModified")]
        public GameObject endGameObject;

        void OnEnable()
        {
            curve = GetComponent<CustomCurve>();
            mf = GetComponent<MeshFilter>();
            mr = GetComponent<MeshRenderer>();

            curveMesh = GetCurveMesh();

            curve.OnCurveModified += Curve_OnCurveModified;
        }

        void OnDisable()
        {
            //TODO: Should probably remove any generated meshes for this component.
            curve.OnCurveModified -= Curve_OnCurveModified;
        }

        private void Curve_OnCurveModified()
        {
            RebuildCurveMesh();
        }

        private Mesh GetCurveMesh()
        {
            if (mf.sharedMesh == null)
            {
                mf.sharedMesh = new Mesh();
                mf.sharedMesh.name = gameObject.name + "_CurveMesh";
            }
                
            return mf.sharedMesh;
        }

        private int currentVertexCount, currentTriangleCount;
        private float currentPosition;
        private DecoratorGameObject[] previousCurveGameObjects;
        private Dictionary<Material, int> materialTriangleCount;

        //TODO: Add option to only update vertices.
        private void RebuildCurveMesh()
        {
            currentVertexCount = 0;
            currentTriangleCount = 0;
            currentPosition = 0;

            DecoratorGameObject[] curveGameObjects = GetGameObjectsToAdd();
            bool requiresNewMesh = !MatchingPreviousGameObjects(curveGameObjects);

            Vector3[] curveVertices = new Vector3[GetGameObjectsVertexCount(curveGameObjects)];
            Vector3[] curveNormals = new Vector3[GetGameObjectsVertexCount(curveGameObjects)];
            int[] curveTriangles = new int[0];

            if (requiresNewMesh)
            {
                curveMesh.Clear();
                curveTriangles = new int[GetGameObjectsTriangleCount(curveGameObjects)];
            }
            

            for (int i = 0; i < curveGameObjects.Length; i++)
                PlaceMeshOnCurve(curveGameObjects[i], ref curveVertices, ref curveNormals, ref curveTriangles);

            // Update Mesh
            curveMesh.vertices = curveVertices;
            curveMesh.normals = curveNormals;

            if (requiresNewMesh)
                curveMesh.triangles = curveTriangles;

            // TODO: Can this be put off until we save the asset?
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

        private void PlaceMeshOnCurve(DecoratorGameObject meshGameObject, ref Vector3[] vertices, ref Vector3[] normals, ref int[] triangles)
        {
            Mesh placedGameObjectMesh = meshGameObject.gameObject.GetComponent<MeshFilter>().sharedMesh;

            for (int i = 0; i < placedGameObjectMesh.vertexCount; i++)
            {
                float t;
                vertices[currentVertexCount + i] = MapMeshPositionToCurve(placedGameObjectMesh.vertices[i], meshGameObject.xScale, out t);
                normals[currentVertexCount + i] = TransformNormalToCurve(placedGameObjectMesh.normals[i], t);
            }

            if (triangles.Length > 0)
            {
                for (int i = 0; i < placedGameObjectMesh.triangles.Length; i++)
                {
                    triangles[currentTriangleCount + i] = currentVertexCount + placedGameObjectMesh.triangles[i];
                }
            }


            currentVertexCount += placedGameObjectMesh.vertexCount;
            currentTriangleCount += placedGameObjectMesh.triangles.Length;

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