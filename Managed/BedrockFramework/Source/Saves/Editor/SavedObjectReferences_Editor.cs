/********************************************************           
BEDROCKFRAMEWORK : https://github.com/GainDeveloper/BedrockFramework

Saveable Scriptable Objects store references to themselves in a shared list.
********************************************************/
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace BedrockFramework.Saves
{
    public class SavedObjectReferences_Editor
    {
        [MenuItem("Assets/Save Object Reference", priority = 30)]
        private static void AddObjectReference()
        {
            Object selectedObject = Selection.activeObject;
            SavedObjectReferences.AddObject(selectedObject);
        }

        [MenuItem("Assets/Save Object Reference", true)]
        private static bool AddObjectReference_Validation()
        {
            Object selectedObject = Selection.activeObject;
            return !SavedObjectReferences.IsSaved(selectedObject);
        }
    }
}