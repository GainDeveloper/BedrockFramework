using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Prototype : ScriptableObject {
#if UNITY_EDITOR
    public Prototype prototype;
    public List<string> modifiedValues = new List<string>();
#endif
}