using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BedrockFramework;

public class LogTest : MonoBehaviour {

	// Use this for initialization
	void Start () {
        
    }
	
	// Update is called once per frame
	void Update () {
        BedrockFramework.Logger.Logger.Log("TheBest", "A = {}, B = {}, C = {}", 1, 2, Time.realtimeSinceStartup);
    }
}
