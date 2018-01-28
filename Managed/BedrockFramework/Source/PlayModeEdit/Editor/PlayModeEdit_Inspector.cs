using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using BedrockFramework.Utilities;

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

        void SetBool(bool isBold)
        {
            if (isBold)
                EditorStyles.label.fontStyle = FontStyle.Bold;
            else
                EditorStyles.label.fontStyle = FontStyle.Normal;
        }

        public override void OnInspectorGUI()
        {
            GUI.enabled = !Application.isPlaying;

            serializedObject.Update();

            SerializedProperty recordDestructionProperty = serializedObject.FindProperty("recordDestruction");
            SerializedProperty recordInstantiationProperty = serializedObject.FindProperty("recordInstantiation");

            EditorGUILayout.LabelField("Record Options", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            SetBool(recordDestructionProperty.prefabOverride);
            recordDestructionProperty.boolValue = EditorGUILayout.Toggle("Destruction", recordDestructionProperty.boolValue);
            SetBool(recordInstantiationProperty.prefabOverride);
            recordInstantiationProperty.boolValue = EditorGUILayout.Toggle("Instantiation", recordInstantiationProperty.boolValue);
            EditorGUILayout.EndHorizontal();
            SetBool(false);
            EditorGUILayout.LabelField("Recorded Components", EditorStyles.miniBoldLabel);

            SerializedProperty recordedComponentsProperty = serializedObject.FindProperty("recordedComponents");

            EditorGUILayout.BeginHorizontal();
            SetBool(recordedComponentsProperty.prefabOverride);
            int i = 0;
            int numPerRow = 2;
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

                i++;
                if (i == numPerRow)
                {
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    i = 0;
                }
            }
            EditorGUILayout.EndHorizontal();

            SetBool(false);
            serializedObject.ApplyModifiedProperties();
        }
    }
}