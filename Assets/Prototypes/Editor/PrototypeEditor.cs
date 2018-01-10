using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Prototype), true)]
public class PrototypeEditor : Editor 
{
	SerializedProperty prototype;
    SerializedObject prototypeSerialized;
    
    void OnEnable()
    {
        prototype = serializedObject.FindProperty("prototype").Copy();
        UpdateSerializedPrototype();
    }

    bool PropertyExistsInPrototype(string property)
    {
        if (prototypeSerialized == null)
            return false;

        if (prototypeSerialized.FindProperty(property) == null)
            return false;

        return true;
    }

    #region ModfiedProperties

    bool IsPropertyModified(string name)
    {
        return PropertyModifiedIndex(name) >= 0;
    }

    int PropertyModifiedIndex(string name)
    {
        SerializedProperty modifiedValues = serializedObject.FindProperty("modifiedValues");
        for (int i = 0; i < modifiedValues.arraySize; i++)
        {
            if (modifiedValues.GetArrayElementAtIndex(i).stringValue == name)
                return i;
        }
        return -1;
    }

    void SetPropertyModified(string property, bool modified)
    {
        SerializedProperty modifiedValues = serializedObject.FindProperty("modifiedValues");
        int i = PropertyModifiedIndex(property);
        if (modified)
        {
            if (i == -1)
            {
                modifiedValues.InsertArrayElementAtIndex(0);
                modifiedValues.GetArrayElementAtIndex(0).stringValue = property;
            }
        }
        else if (i >= 0)
        {
            modifiedValues.DeleteArrayElementAtIndex(i);
        }
    }

    #endregion

    void UpdateNonModifiedProperties()
    {
        SerializedProperty prop = serializedObject.GetIterator();
        if (prop.NextVisible(true))
        {
            do
            {
                if (prop.name != "m_Script" && prop.name != "prototype" && prop.name != "modifiedValues")
                {
                    bool isPropertyModified = IsPropertyModified(prop.name);
                    if (!PropertyExistsInPrototype(prop.name)) isPropertyModified = true;

                    if (!isPropertyModified && prototypeSerialized != null)
                    {
                        serializedObject.CopyFromSerializedProperty(prototypeSerialized.FindProperty(prop.name));
                    }
                }
            }
            while (prop.NextVisible(false));
        }

        serializedObject.ApplyModifiedProperties();
    }

    void UpdateSerializedPrototype()
    {
        if (prototype.objectReferenceValue != null)
            prototypeSerialized = new SerializedObject(prototype.objectReferenceValue);
        else
            prototypeSerialized = null;
    }

	public override void OnInspectorGUI () 
	{
		serializedObject.Update();
        Object newPrototypeObject = EditorGUILayout.ObjectField(prototype.objectReferenceValue, typeof(Prototype), false);
        if (prototype.objectReferenceValue != newPrototypeObject)
        {
            prototype.objectReferenceValue = newPrototypeObject;
            UpdateSerializedPrototype();
        }

        SerializedProperty prop = serializedObject.GetIterator();
        if (prop.NextVisible(true))
        {
            do
            {
                if (prop.name != "m_Script" && prop.name != "prototype" && prop.name != "modifiedValues")
                {
                    bool isPropertyModified = IsPropertyModified(prop.name);
                    if (!PropertyExistsInPrototype(prop.name)) isPropertyModified = true;

                    EditorGUILayout.BeginHorizontal();

                    isPropertyModified = EditorGUILayout.Toggle(isPropertyModified, GUILayout.Width(16));
                    SetPropertyModified(prop.name, isPropertyModified);

                    GUI.enabled = isPropertyModified;
                    EditorGUILayout.PropertyField(prop, true);
                    EditorGUILayout.EndHorizontal();
                    GUI.enabled = true;
                }
            }
            while (prop.NextVisible(false));
        }

        serializedObject.ApplyModifiedProperties();
        UpdateNonModifiedProperties();
    }
}
