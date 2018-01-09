using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Prototype), true)]
public class PrototypeEditor : Editor 
{
    Prototype prototypeTarget;
	SerializedProperty prototype;
    SerializedObject prototypeSerialized;
    List<string> allOtherProperties;
    
    void OnEnable()
    {
        prototypeTarget = (Prototype)target;
        allOtherProperties = new List<string>();

        SerializedProperty prop = serializedObject.GetIterator();
		if (prop.NextVisible(true)) {
			do {
				if (prop.name == "prototype")
                {
					prototype = prop.Copy();
                    if (prototype.objectReferenceValue != null)
                        prototypeSerialized = new SerializedObject(prototype.objectReferenceValue);
                }
                else if (prop.name != "m_Script")
                {
					allOtherProperties.Add(prop.name);
				}
			}
			while (prop.NextVisible(false));
		}
    }

	public override void OnInspectorGUI () 
	{
		serializedObject.Update();
		prototype.objectReferenceValue = EditorGUILayout.ObjectField(prototype.objectReferenceValue, serializedObject.targetObject.GetType(), false);

		bool prototypeAssigned = prototype.objectReferenceValue != null;

		foreach(string propName in allOtherProperties)
		{
			EditorGUILayout.BeginHorizontal();

            bool isPropertyModified = prototypeTarget.IsPropertyModified(propName);
            if (!prototypeAssigned) isPropertyModified = true;

            isPropertyModified = EditorGUILayout.Toggle(isPropertyModified, GUILayout.Width(16));
            prototypeTarget.SetPropertyModified(propName, isPropertyModified);

            GUI.enabled = isPropertyModified;

            if (!isPropertyModified && prototypeSerialized != null)
            {
                // Copy value from prototype.
                serializedObject.Update();
                serializedObject.CopyFromSerializedProperty(prototypeSerialized.FindProperty(propName));
            }
            EditorGUILayout.PropertyField(serializedObject.FindProperty(propName), true);

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.EndHorizontal();
            GUI.enabled = true;
        }

        
	}
}
