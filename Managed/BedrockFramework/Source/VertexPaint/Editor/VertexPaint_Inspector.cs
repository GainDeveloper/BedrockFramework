using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using BedrockFramework.Utilities;

namespace BedrockFramework.VertexPaint
{
    [CustomEditor(typeof(VertexPaint), true)]
    [CanEditMultipleObjects]
    public class VertexPaint_Inspector : Editor
    {
        [System.Serializable]
        class VertexPaint_InspectorSettings
        {
            private const string editorPrefsKey = "VertexPaintSettings";

            public float brushSize = 1;
            public float brushFalloff = 0.5f;
            public float brushDepth = 0.25f;
            public float brushNormalBias = 0.5f;
            public float brushStrength = 1.0f;
            public Color paintColour = Color.white;
            public Color eraseColour = Color.black;
            public bool ignoreBackfacing = true;
            public bool channelR = true, channelG = true, channelB = true, channelA = false;
            public VertexPaint_ViewMode vertexPaintViewMode = VertexPaint_ViewMode.Off;

            public void SaveSettings()
            {
                EditorPrefs.SetString(editorPrefsKey, EditorJsonUtility.ToJson(this));
            }

            public void LoadSettings()
            {
                if (!EditorPrefs.HasKey(editorPrefsKey))
                    return;

                EditorJsonUtility.FromJsonOverwrite(EditorPrefs.GetString(editorPrefsKey), this);
            }
        }

        enum VertexPaint_ViewMode
        {
            Off,
            RGB,
            R,
            G,
            B,
            A
        }

        private const float raduisDragSpeed = 0.25f;
        private const float falloffDragSpeed = 0.01f;
        private const float vertexDisplaySize = 0.025f;
        private const float sceneViewWindowWidth = 300;
        private const float sceneViewWindowHeight = 200;
        private const float sceneViewWindowPadding = 10;
        private Color outerBrushColour = new Color(0.6f, 0.8f, 0.5f);
        private Color innerBrushColour = new Color(0.8f, 1f, 0.7f);

        VertexPaint_InspectorSettings vertexPaintSettings = new VertexPaint_InspectorSettings();

        Vector3 m_BrushPos;
        Vector3 m_BrushNorm;
        int m_BrushFace = -1;
        Plane mousePlane;

        Shader shader_rgb, shader_r, shader_g, shader_b, shader_a;

        struct VertexPaint_EditorInstance
        {
            public Transform transform;
            public VertexPaint vertexPaint;
            public Mesh localMesh;
        }

        VertexPaint_EditorInstance[] activeInstances;

        void OnEnable()
        {
            vertexPaintSettings.LoadSettings();
            activeInstances = new VertexPaint_EditorInstance[targets.Length];

            for (int i = 0; i < targets.Length; i++)
            {
                VertexPaint vertexPaint = (VertexPaint)targets[i];
                Mesh localMesh = vertexPaint.GetComponent<MeshFilter>().sharedMesh;

                Mesh additionalMeshStream = vertexPaint.additionalVertexStreamMesh;
                if (additionalMeshStream != null && additionalMeshStream.colors.Length != localMesh.vertexCount)
                {
                    Debug.LogError("Vertex Stream Source Mismatch!");
                    additionalMeshStream = vertexPaint.CreateAdditonalVertexStreamMesh(localMesh);
                }
                    
                activeInstances[i] = new VertexPaint_EditorInstance { vertexPaint = vertexPaint, transform = vertexPaint.transform, localMesh = localMesh };
            }

            shader_rgb = Shader.Find("Unlit/Vertex/RGB");
            shader_r = Shader.Find("Unlit/Vertex/R");
            shader_g = Shader.Find("Unlit/Vertex/G");
            shader_b = Shader.Find("Unlit/Vertex/B");
            shader_a = Shader.Find("Unlit/Vertex/A");

            Undo.undoRedoPerformed += UndoCallback;
            UpdateViewMode();
        }

        void OnDisable()
        {
            vertexPaintSettings.SaveSettings();
            Undo.undoRedoPerformed -= UndoCallback;

            if (SceneView.lastActiveSceneView != null)
                SceneView.lastActiveSceneView.SetSceneViewShaderReplace(null, null);
        }

        void UndoCallback()
        {
            for (int i = 0; i < activeInstances.Length; i++)
            {
                activeInstances[i].vertexPaint.UpdateMeshVertexColours(activeInstances[i].vertexPaint.additionalVertexStreamMesh.colors, recordChanges : false);
            }
        }

        void OnSceneGUI()
        {
            if (Event.current.type == EventType.Repaint)
            {
                UpdatePreviewBrush();

                if (m_BrushFace >= 0)
                {
                    DrawBrush();
                    DrawVertices();
                }
            }

            if (m_BrushFace >= 0)
            {
                PaintSceneGUI();
            }

            Handles.BeginGUI();
            GUILayout.Window(0, new Rect(Screen.width - sceneViewWindowWidth - sceneViewWindowPadding, Screen.height - sceneViewWindowHeight - sceneViewWindowPadding - 18, sceneViewWindowWidth, sceneViewWindowHeight), PaintingWindow, "Vertex Painting");
            Handles.EndGUI();
        }

        void PaintSceneGUI()
        {
            int id = GUIUtility.GetControlID(FocusType.Passive);
            Event e = Event.current;
            HandleUtility.AddDefaultControl(id);

            EventType type = e.type;
            if (e.shift || e.alt)
                return;

            if (type == EventType.MouseDown || type == EventType.MouseDrag)
            {
                if (e.button == 0)
                {
                    if (e.control)
                    {
                        PaintVertices(erase : true);
                    } else
                    {
                        PaintVertices();
                    }
                    
                }
                else if (e.button == 1)
                {
                    vertexPaintSettings.brushSize += e.delta.x * raduisDragSpeed;
                }
                else if (e.button == 2)
                {
                    vertexPaintSettings.brushFalloff += e.delta.x * falloffDragSpeed;
                }

                e.Use();
            } else if (type == EventType.ScrollWheel)
            {
                vertexPaintSettings.brushDepth += Mathf.Clamp(e.delta.y, -1, 1) * 0.1f;
                e.Use();
            }
        }

        void PaintingWindow(int windowID)
        {
            EditorGUIUtility.labelWidth = 80;

            EditorGUILayout.LabelField("Brush Settings", EditorStyles.boldLabel, GUILayout.Width(sceneViewWindowWidth - sceneViewWindowPadding));
            vertexPaintSettings.brushSize = EditorGUILayout.Slider("Radius", vertexPaintSettings.brushSize, 0.1f, 50, GUILayout.Width(sceneViewWindowWidth - sceneViewWindowPadding));
            vertexPaintSettings.brushFalloff = EditorGUILayout.Slider("Falloff", vertexPaintSettings.brushFalloff, 0f, 1, GUILayout.Width(sceneViewWindowWidth - sceneViewWindowPadding));
            vertexPaintSettings.brushDepth = EditorGUILayout.Slider("Depth", vertexPaintSettings.brushDepth, 0.1f, 1, GUILayout.Width(sceneViewWindowWidth - sceneViewWindowPadding));
            vertexPaintSettings.ignoreBackfacing = EditorGUILayout.Toggle("Ignore Backfacing", vertexPaintSettings.ignoreBackfacing);

            EditorGUILayout.LabelField("View Settings", EditorStyles.boldLabel, GUILayout.Width(sceneViewWindowWidth - sceneViewWindowPadding));
            VertexPaint_ViewMode previousViewMode = vertexPaintSettings.vertexPaintViewMode;
            vertexPaintSettings.vertexPaintViewMode = (VertexPaint_ViewMode)EditorGUILayout.EnumPopup("Colour Mode", vertexPaintSettings.vertexPaintViewMode);
            if (vertexPaintSettings.vertexPaintViewMode != previousViewMode) UpdateViewMode();

            EditorGUILayout.LabelField("Paint Settings", EditorStyles.boldLabel, GUILayout.Width(sceneViewWindowWidth - sceneViewWindowPadding));
            vertexPaintSettings.brushStrength = EditorGUILayout.Slider("Strength", vertexPaintSettings.brushStrength, 0, 1, GUILayout.Width(sceneViewWindowWidth - sceneViewWindowPadding));
            //vertexPaintSettings.paintColour = EditorGUILayout.ColorField(vertexPaintSettings.paintColour);
            //vertexPaintSettings.eraseColour = EditorGUILayout.ColorField(vertexPaintSettings.eraseColour);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Channels", GUILayout.Width(80));
            EditorGUIUtility.labelWidth = 20;
            vertexPaintSettings.channelR = EditorGUILayout.Toggle("R", vertexPaintSettings.channelR);
            vertexPaintSettings.channelG = EditorGUILayout.Toggle("G", vertexPaintSettings.channelG);
            vertexPaintSettings.channelB = EditorGUILayout.Toggle("B", vertexPaintSettings.channelB);
            vertexPaintSettings.channelA = EditorGUILayout.Toggle("A", vertexPaintSettings.channelA);
            EditorGUILayout.EndHorizontal();
        }

        void UpdateViewMode()
        {
            switch (vertexPaintSettings.vertexPaintViewMode)
            {
                case VertexPaint_ViewMode.Off:
                    SceneView.lastActiveSceneView.SetSceneViewShaderReplace(null, null);
                    break;
                case VertexPaint_ViewMode.RGB:
                    SceneView.lastActiveSceneView.SetSceneViewShaderReplace(shader_rgb, null);
                    break;
                case VertexPaint_ViewMode.R:
                    SceneView.lastActiveSceneView.SetSceneViewShaderReplace(shader_r, null);
                    break;
                case VertexPaint_ViewMode.G:
                    SceneView.lastActiveSceneView.SetSceneViewShaderReplace(shader_g, null);
                    break;
                case VertexPaint_ViewMode.B:
                    SceneView.lastActiveSceneView.SetSceneViewShaderReplace(shader_b, null);
                    break;
                case VertexPaint_ViewMode.A:
                    SceneView.lastActiveSceneView.SetSceneViewShaderReplace(shader_a, null);
                    break;
            }
        }

        void UpdatePreviewBrush()
        {
            Raycast(out m_BrushPos, out m_BrushNorm, out m_BrushFace);
            mousePlane = new Plane(m_BrushNorm, m_BrushPos);
        }

        public bool Raycast(out Vector3 pos, out Vector3 norm, out int face)
        {
            Ray mouseRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            bool hitSomething = false;

            norm = Vector2.zero;
            pos = Vector3.zero;
            face = -1;

            for (int i = 0; i < activeInstances.Length; i++)
            {
                RaycastHit hit;

                if (EditorHandles_UnityInternal.IntersectRayMesh(mouseRay, activeInstances[i].localMesh, activeInstances[i].transform.localToWorldMatrix, out hit))
                {
                    if (hitSomething)
                    {
                        if (Vector3.Distance(hit.point, mouseRay.origin) > Vector3.Distance(pos, mouseRay.origin))
                            continue;
                    }

                    norm = hit.normal.normalized;
                    pos = hit.point;
                    face = hit.triangleIndex;
                    hitSomething = true;
                }
            }

            return hitSomething;
        }



        void DrawBrush()
        {
            Handles.color = innerBrushColour;
            Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
            Handles.DrawLine(m_BrushPos - m_BrushNorm * vertexPaintSettings.brushDepth, m_BrushPos + m_BrushNorm * vertexPaintSettings.brushDepth);

            Handles.color = outerBrushColour;
            Handles.zTest = UnityEngine.Rendering.CompareFunction.Greater;
            Handles.DrawLine(m_BrushPos - m_BrushNorm * vertexPaintSettings.brushDepth, m_BrushPos + m_BrushNorm * vertexPaintSettings.brushDepth);


            Handles.color = innerBrushColour;
            Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
            Handles.DrawWireDisc(m_BrushPos, m_BrushNorm, vertexPaintSettings.brushSize * vertexPaintSettings.brushFalloff);

            Handles.color = outerBrushColour;
            Handles.DrawWireDisc(m_BrushPos, m_BrushNorm, vertexPaintSettings.brushSize);
        }

        void DrawVertices()
        {
            for (int i = 0; i < activeInstances.Length; i++)
            {
                Vector3[] normals = activeInstances[i].localMesh.normals;
                Vector3[] vertices = activeInstances[i].localMesh.vertices;

                for (int x = 0; x < normals.Length; x++)
                {
                    Vector3 normal = activeInstances[i].transform.TransformVector(normals[x]);
                    Vector3 position = activeInstances[i].transform.TransformPoint(vertices[x]);

                    float vertexStrength = GetVertexStrength(position, normal);

                    Handles.color = Color.Lerp(outerBrushColour, innerBrushColour, vertexStrength);
                    if (vertexStrength > 0)
                        Handles.DotHandleCap(0, position, Quaternion.identity, vertexDisplaySize * HandleUtility.GetHandleSize(position), EventType.Repaint);
                }
            }
        }

        void PaintVertices(bool erase = false)
        {
            for (int i = 0; i < activeInstances.Length; i++)
            {
                Vector3[] normals = activeInstances[i].localMesh.normals;
                Vector3[] vertices = activeInstances[i].localMesh.vertices;
                Color[] colors = activeInstances[i].vertexPaint.additionalVertexStreamMesh.colors;

                for (int x = 0; x < normals.Length; x++)
                {
                    Vector3 normal = activeInstances[i].transform.TransformVector(normals[x]);
                    Vector3 position = activeInstances[i].transform.TransformPoint(vertices[x]);

                    float vertexStrength = GetVertexStrength(position, normal) * vertexPaintSettings.brushStrength;
                    Color paintColour = vertexPaintSettings.paintColour;
                    if (erase) paintColour = vertexPaintSettings.eraseColour;


                    colors[x] = BlendColourByChannel(colors[x], paintColour, vertexStrength);
                }

                activeInstances[i].vertexPaint.UpdateMeshVertexColours(colors);
            }
        }

        Color BlendColourByChannel(Color baseColor, Color toColor, float i)
        {
            float r = baseColor.r;
            float g = baseColor.g;
            float b = baseColor.b;
            float a = baseColor.a;

            if (vertexPaintSettings.channelR)
                r = Mathf.Lerp(r, toColor.r, i);
            if (vertexPaintSettings.channelG)
                g = Mathf.Lerp(g, toColor.g, i);
            if (vertexPaintSettings.channelB)
                b = Mathf.Lerp(b, toColor.b, i);
            if (vertexPaintSettings.channelA)
                a = Mathf.Lerp(a, toColor.a, i);

            return new Color(r, g, b, a);
        }

        float GetVertexStrength(Vector3 position, Vector3 normal)
        {
            bool forwardFacing = Vector3.Dot(normal, Camera.current.transform.forward) <= 0;
            if (!forwardFacing)
                return 0;

            if (vertexPaintSettings.ignoreBackfacing)
            {
                bool alignsWithBrush = Vector3.Dot(normal, m_BrushNorm) > vertexPaintSettings.brushNormalBias;
                if (!alignsWithBrush)
                    return 0;
            }

            float distanceFromBrush = Vector3.Distance(position, m_BrushPos);
            bool withinBrush = distanceFromBrush < vertexPaintSettings.brushSize;
            if (!withinBrush)
                return 0;

            
            if (Mathf.Abs(mousePlane.GetDistanceToPoint(position)) > vertexPaintSettings.brushDepth)
                return 0;

            float distanceFromEdgeOfBrush = Mathf.Max(0, distanceFromBrush - vertexPaintSettings.brushSize * vertexPaintSettings.brushFalloff);
            float fallOffArea = (vertexPaintSettings.brushSize * (1 - vertexPaintSettings.brushFalloff));

            return distanceFromEdgeOfBrush.Remap(0, fallOffArea, 1, 0);
        }
    }
}