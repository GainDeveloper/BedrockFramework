using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using BedrockFramework.Utilities;

namespace BedrockFramework.CustomLine
{
    [CustomEditor(typeof(CustomCurve))]
    public class CustomCurve_Editor : Editor
    {
        private const int stepsPerCurve = 10;
        private const float handleSize = 0.04f;
        private const float pickSize = 0.06f;
        private const float rulerMarkerDistance = 1;
        private const float rulerMarkerLength = 0.1f;
        private const float curveLineWidth = 2f;
        private const float tangentHandleSize = 0.1f;



        private CustomCurve line;
        private Transform handleTransform;
        private SerializedProperty linePoints;
        private int selectedIndex = -1;



        private void OnEnable()
        {
            line = target as CustomCurve;
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Length: " + line.CurveLength().ToString());
            EditorGUILayout.LabelField("Segments: " + line.CurveCount.ToString());
            EditorGUILayout.EndHorizontal();

        }

        private void AddCurvePoint()
        {
            linePoints = serializedObject.FindProperty("points");
            linePoints.InsertArrayElementAtIndex(linePoints.arraySize);

            CustomCurve.CurvePoint newCurvePoint = line.NewCurvePoint();

            SerializedProperty curvePoint = linePoints.GetArrayElementAtIndex(linePoints.arraySize - 1);
            curvePoint.FindPropertyRelative("curvePoint").vector3Value = newCurvePoint.curvePoint;
            curvePoint.FindPropertyRelative("curveTangent").vector3Value = newCurvePoint.curveTangent;

            selectedIndex = linePoints.arraySize - 1;
        }

        private void OnSceneGUI()
        {
            // Disable selecting under mouse if we have a selected index.
            if (Event.current.type == EventType.Layout)
            {
                if (selectedIndex != -1)
                    HandleUtility.AddDefaultControl(0);
            }

            // Drop the selected index if mouse up over no handle.
            if (Event.current.type == EventType.MouseUp)
            {
                if (GUIUtility.hotControl == 0)
                    selectedIndex = -1;
            }

            serializedObject.Update();
            handleTransform = line.transform;

            Handles.color = Color.white;

            linePoints = serializedObject.FindProperty("points");

            EditorGUI.BeginChangeCheck();

            CustomCurve.CurvePoint p0 = ShowPoint(0);
            for (int i = 1; i < line.points.Length; i += 1)
            {
                Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
                CustomCurve.CurvePoint p1 = ShowPoint(i);

                Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
                Handles.DrawBezier(p0.curvePoint, p1.curvePoint, p0.curvePoint + p0.curveTangent, p1.curvePoint - p1.curveTangent, Color.white, null, curveLineWidth);

                Handles.zTest = UnityEngine.Rendering.CompareFunction.Greater;
                Handles.DrawBezier(p0.curvePoint, p1.curvePoint, p0.curvePoint + p0.curveTangent, p1.curvePoint - p1.curveTangent, Color.grey, null, curveLineWidth);

                Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;

                if (i == linePoints.arraySize - 1)
                    DrawAddButton(p1);

                p0 = p1;
            }

            if (EditorGUI.EndChangeCheck() || line.transform.hasChanged)
            {
                line.CurveModified();
                line.transform.hasChanged = false;
            }

            DrawRuler();
            //ShowDirections();

            serializedObject.ApplyModifiedProperties();
        }

        void DrawRuler()
        {
            float coveredDistance = rulerMarkerDistance;

            while (coveredDistance < line.CurveLength())
            {
                float t = line.DistanceToT(coveredDistance);

                Vector3 lineMiddle = line.GetPoint(t);
                Vector3 biNormal = line.GetBiNormal(t);

                Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
                Handles.color = Color.white;
                Handles.DrawLine(lineMiddle - biNormal * rulerMarkerLength, lineMiddle + biNormal * rulerMarkerLength);

                Handles.zTest = UnityEngine.Rendering.CompareFunction.Greater;
                Handles.color = Color.grey;
                Handles.DrawLine(lineMiddle - biNormal * rulerMarkerLength, lineMiddle + biNormal * rulerMarkerLength);

                coveredDistance += rulerMarkerDistance;
                Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
            }
        }

        void ShowDirections()
        {
            Handles.color = Color.green;

            int steps = stepsPerCurve * line.CurveCount;
            for (int i = 1; i <= steps; i++)
            {
                Vector3 lineEnd = line.GetPoint(i / (float)steps);
                Handles.color = Color.green;
                Handles.DrawLine(lineEnd, lineEnd + line.GetDirection(i / (float)steps));

                Handles.color = Color.red;
                Handles.DrawLine(lineEnd, lineEnd + line.GetNormal(i / (float)steps));
            }
        }

        private CustomCurve.CurvePoint ShowPoint(int index)
        {
            SerializedProperty curvePoint = linePoints.GetArrayElementAtIndex(index);
            SerializedProperty curvePos = curvePoint.FindPropertyRelative("curvePoint");
            SerializedProperty curveTangent = curvePoint.FindPropertyRelative("curveTangent");

            bool selected = index == selectedIndex;

            // Position Drawer
            Vector3 point = handleTransform.TransformPoint(curvePos.vector3Value);
            float size = HandleUtility.GetHandleSize(point);

            Handles.color = Color.white;
            if (selected)
            {
                float tValue = line.IndexToT(index);
                Quaternion handleRotation = Tools.pivotRotation == PivotRotation.Local ? Quaternion.LookRotation(line.GetDirection(tValue), line.GetNormal(tValue)) : Quaternion.identity;
                point = Handles.DoPositionHandle(point, handleRotation);
            }
            else if (Handles.Button(point, Quaternion.identity, size * handleSize, size * pickSize, Handles.DotHandleCap))
                selectedIndex = index;

            curvePos.vector3Value = handleTransform.InverseTransformPoint(point).Round(1000);

            // Tangent Drawer
            Vector3 tangent = handleTransform.TransformVector(curveTangent.vector3Value) + point;
            if (selected)
                tangent = Handles.FreeMoveHandle(tangent, Quaternion.identity, tangentHandleSize, Vector3.one, Handles.CircleHandleCap);
            tangent -= point;
            curveTangent.vector3Value = handleTransform.InverseTransformVector(tangent).Round(1000);

            Handles.color = Color.grey;
            if (selected)
                Handles.DrawLine(point, point + tangent);

            return new CustomCurve.CurvePoint(point, tangent);
        }

        private void DrawAddButton(CustomCurve.CurvePoint curvePoint)
        {
            Handles.BeginGUI();

            //float size = HandleUtility.GetHandleSize(curvePoint.curvePoint + curvePoint.curveTangent.normalized);
            Vector3 screenPoint = Camera.current.WorldToScreenPoint(curvePoint.curvePoint + curvePoint.curveTangent * 0.5f);


            if (GUI.Button(new Rect(screenPoint.x - 10, Screen.height - screenPoint.y - 50, 20, 20), "+"))
                AddCurvePoint();

            Handles.EndGUI();
        }
    }
}



