using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Prototype : ScriptableObject {
	public Prototype prototype;
	[HideInInspector]
	public List<PrototypeProperty> modifiedValues;

	public object GetModifiedValue (string name) 
	{
		for (int i = 0; i < modifiedValues.Count; i++) 
		{
			if (modifiedValues[i].name == name)
				return modifiedValues[i].value;
		}
		return null;
	}
}

[System.Serializable]
public class PrototypeProperty {
	public string name;
	public object value;
}