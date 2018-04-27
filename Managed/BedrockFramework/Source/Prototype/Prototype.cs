using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BedrockFramework.Prototype
{
    public class PrototypeObject : ScriptableObject
    {
        public PrototypeObject prototype;
        public List<string> modifiedValues = new List<string>();
    }
}