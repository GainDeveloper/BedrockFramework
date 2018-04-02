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
        private MeshRenderer mr;

        public void Awake()
        {
#if (UNITY_EDITOR)
            if (additionalVertexStreamMesh == null)
                CreateAdditonalVertexStreamMesh(GetComponent<MeshFilter>().sharedMesh);
#endif

            mr = GetComponent<MeshRenderer>();
            AssignVertexStream();
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

        public void AssignVertexStream()
        {
            if (additionalVertexStreamMesh != null)
                mr.additionalVertexStreams = additionalVertexStreamMesh;
        }

#if (UNITY_EDITOR)

        public void Update()
        {
            AssignVertexStream();
        }

        public void UpdateMeshVertexColours(Color[] colors, bool recordChanges = true)
        {
            Undo.RecordObject(additionalVertexStreamMesh, "Vertex Painting");
            additionalVertexStreamMesh.colors = colors;
        }
#endif
    }
}