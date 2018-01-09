using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Prototype), true)]
public class PrototypeEditor : Editor 
{
	SerializedProperty prototype;
	List<SerializedProperty> allOtherProperties = new List<SerializedProperty>();
    
    void OnEnable()
    {
		SerializedProperty prop = serializedObject.GetIterator();
		if (prop.NextVisible(true)) {
			do {
				if (prop.name == "prototype") {
					prototype = prop.Copy();
				}
				else if (prop.name != "m_Script") {
					allOtherProperties.Add(prop.Copy());
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
		

		foreach(SerializedProperty prop in allOtherProperties)
		{
			EditorGUILayout.BeginHorizontal();
			GUI.enabled = !prototypeAssigned;
			EditorGUILayout.Toggle(false, GUILayout.Width(16));
			EditorGUILayout.PropertyField(prop, true);
			EditorGUILayout.EndHorizontal();
		}					

		serializedObject.ApplyModifiedProperties();
	}
}
