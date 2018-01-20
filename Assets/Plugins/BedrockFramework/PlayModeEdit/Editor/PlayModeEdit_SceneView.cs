using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace BedrockFramework.PlayModeEdit
{
    [InitializeOnLoadAttribute]
    public class PlayModeEdit_SceneView : EditorWindow
    {
        private static bool _isRecording = false;

        public static bool IsRecording
        {
            get { return _isRecording; }
        }

        private static GUIStyle _recordTextStyle;

        static PlayModeEdit_SceneView()
        {
            EditorApplication.playModeStateChanged += LogPlayModeState;
        }

        private static void LogPlayModeState(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                _isRecording = false;
                SceneView.onSceneGUIDelegate += OnScene;
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                if (_isRecording)
                {
                    PlayModeEdit_System.CacheCurrentState();
                }
                SceneView.onSceneGUIDelegate -= OnScene;
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                if (_isRecording)
                {
                    PlayModeEdit_System.ApplyCache();
                }
            }
        }

        private static void OnScene(SceneView sceneview)
        {
            if (_recordTextStyle == null)
            {
                _recordTextStyle = new GUIStyle(GUI.skin.label);
                _recordTextStyle.fontSize = 12;
                _recordTextStyle.normal.textColor = Color.red;
            }

            Handles.BeginGUI();

            GUILayout.BeginArea(new Rect(0, 0, Screen.width, 128));
            GUILayout.BeginHorizontal();

            if (GUILayout.Toggle(IsRecording, "Save Edits"))
            {
                _isRecording = true;
            } else
            {
                _isRecording = false;
            }

            Debug.Log(Screen.width);

            GameObject selectedObject = Selection.activeGameObject;
            if (selectedObject != null && selectedObject.GetComponent<PlayModeEdit>() == null || selectedObject != null && !IsRecording)
            {
                GUILayout.Label(selectedObject.name + " edits will not be saved.", _recordTextStyle);
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            Handles.EndGUI();
        }
    }
}

