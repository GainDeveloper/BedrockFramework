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
            get
            {
                return _isRecording;
            }
        }

        private static GUIContent _recordGUIIcon, _recordingGUIIcon;


        static PlayModeEdit_SceneView()
        {
            EditorApplication.playModeStateChanged += LogPlayModeState;

            _recordGUIIcon = EditorGUIUtility.IconContent("TimelineAutokey");
            _recordGUIIcon.text = "Save Edits";
            _recordingGUIIcon = EditorGUIUtility.IconContent("TimelineAutokey_active");
            _recordingGUIIcon.text = "Cancel Edits";
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
            Handles.BeginGUI();


            if (GUILayout.Button(GetRecordIcon(), GUILayout.Width(100)))
            {
                ToggleRecording();
            }

            Handles.EndGUI();
        }

        private static GUIContent GetRecordIcon()
        {
            if (IsRecording)
            {
                return _recordingGUIIcon;
            }
            else
            {
                return _recordGUIIcon;
            }
        }

        private static void ToggleRecording()
        {
            _isRecording = !_isRecording;
        }
    }
}

