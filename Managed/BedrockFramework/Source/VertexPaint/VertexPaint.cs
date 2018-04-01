using System.Linq;
using System.Collections.Generic;
using UnityEngine;
#if (UNITY_EDITOR)
using UnityEditor;
#endif

namespace BedrockFramework.VertexPaint
{
    [DisallowMultipleComponent, EditorOnlyComponent, ExecuteInEditMode]
    [AddComponentMenu("BedrockFramework/VertexPaint")]
    public class VertexPaint : MonoBehaviour
    {
        public Mesh additionalVertexStreamMesh;

        public void Awake()
        {
            if (additionalVertexStreamMesh == null)
                CreateAdditonalVertexStreamMesh(GetComponent<MeshFilter>().sharedMesh);

            Debug.Log("Assigning Vertex Stream");
            foreach (Color col in additionalVertexStreamMesh.colors)
                Debug.Log(col);

            GetComponent<MeshRenderer>().additionalVertexStreams = additionalVertexStreamMesh;
        }

        public Mesh CreateAdditonalVertexStreamMesh(Mesh localMesh)
        {
            Debug.LogWarning("Generating new Vertex Stream Mesh");

            additionalVertexStreamMesh = new Mesh();
            additionalVertexStreamMesh.name = gameObject.name + " VPStream";
            additionalVertexStreamMesh.vertices = localMesh.vertices;
            additionalVertexStreamMesh.colors = new Color[localMesh.vertexCount];
            return additionalVertexStreamMesh;
        }

#if (UNITY_EDITOR)
        public void UpdateMeshVertexColours(Color[] colors)
        {
            Undo.RecordObject(additionalVertexStreamMesh, "Vertex Painting");
            additionalVertexStreamMesh.colors = colors;
        }
#endif
    }
}