using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using BedrockFramework;

using Debug = BedrockFramework.Logger.Logger;

[EditorOnlyComponent]
public class LogTest : MonoBehaviour {

	// Use this for initialization
	void Start () {
        Debug.LogWarning("[Build]", this);
    }
	
	// Update is called once per frame
	void Update () {
        //Debug.Log("TheBest", "A = {}, B = {}, C = {}", () => new object[] { 1, 2, Time.realtimeSinceStartup });
        Debug.Log("SceneLoading", this);
        Debug.Log("Sealife", this);
        Debug.Log("Pooling", this);


    }
}
