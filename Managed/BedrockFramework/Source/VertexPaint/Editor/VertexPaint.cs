using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

namespace BedrockFramework.VertexPaint
{
    [DisallowMultipleComponent, EditorOnlyComponent, ExecuteInEditMode]
    [AddComponentMenu("BedrockFramework/VertexPaint")]
    public class VertexPaint : MonoBehaviour
    {
        public Mesh additionalVertexStreamMesh;
        private MeshRenderer mr;
        private MeshFilter mf;

        public void Awake()
        {
            mr = GetComponent<MeshRenderer>();
            mf = GetComponent<MeshFilter>();

            AssignVertexStream();
            CheckValidWithLocalMesh(GetComponent<MeshFilter>().sharedMesh);
        }

        public void AssignVertexStream()
        {
            if (additionalVertexStreamMesh != null)
                mr.additionalVertexStreams = additionalVertexStreamMesh;
        }

        public bool HasVertexColours{
        get { return additionalVertexStreamMesh != null; }
        }

        public Color[] ExistingColours
        {
            get {
                if (!HasVertexColours)
                {
                    if (mf.sharedMesh.colors.Length != mf.sharedMesh.vertexCount)
                        return Enumerable.Repeat<Color>(Color.white, mf.sharedMesh.vertexCount).ToArray();
                    return mf.sharedMesh.colors;
                }

                return additionalVertexStreamMesh.colors;
            }
        }

        public void CheckValidWithLocalMesh(Mesh localMesh)
        {
            if (!HasVertexColours) 
                return;

            if (additionalVertexStreamMesh.colors.Length != localMesh.vertexCount)
                CreateAdditonalVertexStreamMesh();
        }

        public void Update()
        {
            AssignVertexStream();
        }

        public void RemoveVertexColours()
        {
            if (HasVertexColours) {
                Undo.RecordObject(this, "Removed Vertex Colours");
                additionalVertexStreamMesh = null;
                mr.additionalVertexStreams = null;
            }
        }

        public void UpdateMeshVertexColours(Color[] colors, bool recordChanges = true)
        {
            if (!HasVertexColours)
            {
                CreateAdditonalVertexStreamMesh();
            } else 
            {
                CheckValidWithLocalMesh(mf.sharedMesh);
            }
                
            Undo.RecordObject(additionalVertexStreamMesh, "Vertex Painting");
            additionalVertexStreamMesh.colors = colors;
        }

        public Mesh CreateAdditonalVertexStreamMesh()
        {
            //Debug.LogWarning("Generating new Vertex Stream Mesh");
            additionalVertexStreamMesh = new Mesh();
            additionalVertexStreamMesh.name = gameObject.name + " VPStream";
            additionalVertexStreamMesh.vertices = mf.sharedMesh.vertices;
            additionalVertexStreamMesh.colors = new Color[mf.sharedMesh.vertexCount];
            AssignVertexStream();

            return additionalVertexStreamMesh;
        }

        public void MakeUnique()
        {
            if (!HasVertexColours)
                return;
            
            Undo.RecordObject(this, "Vertex Painting");
            Color[] existingColours = ExistingColours;
            CreateAdditonalVertexStreamMesh();
            UpdateMeshVertexColours(existingColours);
        }

        public void SetVertexPaintStream(Mesh streamMesh)
        {
            Undo.RecordObject(this, "Vertex Painting");
            additionalVertexStreamMesh = streamMesh;
            AssignVertexStream();
        }
    }
}