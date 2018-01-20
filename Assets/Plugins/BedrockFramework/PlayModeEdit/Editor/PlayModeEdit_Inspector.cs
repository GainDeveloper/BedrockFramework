using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace BedrockFramework.PlayModeEdit
{
    [CustomEditor(typeof(PlayModeEdit), true)]
    public class PlayModeEdit_Inspector : Editor
    {
        PlayModeEdit editTarget;

        void OnEnable()
        {
            editTarget = target as PlayModeEdit;
        }

        public override void OnInspectorGUI()
        {
            GUI.enabled = !Application.isPlaying;

            serializedObject.Update();

            SerializedProperty recordedComponentsProperty = serializedObject.FindProperty("recordedComponents");

            foreach (Component component in editTarget.GetComponents<Component>())
            {
                if (component == editTarget)
                    continue;

                bool componentRecorded = recordedComponentsProperty.ArrayContains(component);
                bool result = EditorGUILayout.Toggle(component.GetType().Name, componentRecorded);

                if (componentRecorded && !result)
                {
                    recordedComponentsProperty.ArrayDeleteElement(component);
                } else if (!componentRecorded && result)
                {
                    recordedComponentsProperty.ArrayAddElement(component);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}