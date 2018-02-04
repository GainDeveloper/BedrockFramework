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

        //TODO: Add option to only update vertices.
        private void RebuildCurveMesh()
        {
            curveMesh.Clear();
            currentVertexCount = 0;
            currentTriangleCount = 0;
            currentPosition = 0;

            GameObject[] curveGameObjects = GetGameObjectsToAdd();
            Vector3[] curveVertices = new Vector3[GetGameObjectsVertexCount(curveGameObjects)];
            Vector3[] curveNormals = new Vector3[GetGameObjectsVertexCount(curveGameObjects)];
            int[] curveTriangles = new int[GetGameObjectsTriangleCount(curveGameObjects)];


            for (int i = 0; i < curveGameObjects.Length; i++)
                PlaceMeshOnCurve(curveGameObjects[i], ref curveVertices, ref curveNormals, ref curveTriangles);

            // Update Mesh
            curveMesh.vertices = curveVertices;
            curveMesh.normals = curveNormals;
            curveMesh.triangles = curveTriangles;

            // TODO: Can this be put off until we save the asset?
            curveMesh.RecalculateBounds();
            curveMesh.RecalculateTangents();
        }

        //TODO: This should include some sort of scale required per GameObject
        private GameObject[] GetGameObjectsToAdd()
        {
            List<GameObject> gameObjects = new List<GameObject>();

            if (startGameObject != null)
                gameObjects.Add(startGameObject);

            if (endGameObject != null)
                gameObjects.Add(endGameObject);

            //float coveredDistance = 

            return gameObjects.ToArray();
        }

        private int GetGameObjectsVertexCount(GameObject[] gameobjects)
        {
            int vertexCount = 0;
            for (int i = 0; i < gameobjects.Length; i++)
                vertexCount += gameobjects[i].GetComponent<MeshFilter>().sharedMesh.vertexCount;
            return vertexCount;
        }

        private int GetGameObjectsTriangleCount(GameObject[] gameobjects)
        {
            int triangleCount = 0;
            for (int i = 0; i < gameobjects.Length; i++)
                triangleCount += gameobjects[i].GetComponent<MeshFilter>().sharedMesh.triangles.Length;
            return triangleCount;
        }

        private void PlaceMeshOnCurve(GameObject meshGameObject, ref Vector3[] vertices, ref Vector3[] normals, ref int[] triangles)
        {
            Mesh placedGameObjectMesh = meshGameObject.GetComponent<MeshFilter>().sharedMesh;

            for (int i = 0; i < placedGameObjectMesh.vertexCount; i++)
            {
                float t;
                vertices[currentVertexCount + i] = MapMeshPositionToCurve(placedGameObjectMesh.vertices[i], out t);
                normals[currentVertexCount + i] = TransformNormalToCurve(placedGameObjectMesh.normals[i], t);
            }

            for (int i = 0; i < placedGameObjectMesh.triangles.Length; i++)
            {
                triangles[currentTriangleCount + i] = currentVertexCount + placedGameObjectMesh.triangles[i];
            }

            currentVertexCount += placedGameObjectMesh.vertexCount;
            currentTriangleCount += placedGameObjectMesh.triangles.Length;

            currentPosition += placedGameObjectMesh.bounds.size.x;
        }

        private Vector3 MapMeshPositionToCurve(Vector3 meshPosition, out float t)
        {
            t = curve.DistanceToT(currentPosition + meshPosition.x);

            Vector3 toReturn = curve.GetPoint(t, false);
            toReturn += curve.GetNormal(t, false) * meshPosition.y;
            toReturn += curve.GetBiNormal(t, false) * -1 * meshPosition.z;

            return toReturn;
        }

        private Vector3 TransformNormalToCurve(Vector3 normal, float t)
        {
            //return Quaternion.LookRotation(Vector3.forward, Vector3.up) * normal;

            return Quaternion.LookRotation(curve.GetDirection(t, false), curve.GetNormal(t, false)) * normal;
        }
    }
}