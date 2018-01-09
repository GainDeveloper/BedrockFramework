using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class Prototype : ScriptableObject {
	public Prototype prototype;
    [HideInInspector]
    public List<string> modifiedValues = new List<string>();

	public bool IsPropertyModified (string name) 
	{
		return modifiedValues.Contains(name);
	}

    public void SetPropertyModified(string property, bool modified)
    {
        if (modified)
        {
            if (!modifiedValues.Contains(property))
            {
                modifiedValues.Add(property);
                EditorUtility.SetDirty(this);
            }  
        }
        else
        {
            modifiedValues.Remove(property);
            EditorUtility.SetDirty(this);
        }
    }
}