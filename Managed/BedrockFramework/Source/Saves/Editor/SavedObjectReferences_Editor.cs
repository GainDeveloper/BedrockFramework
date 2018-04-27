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
        public static void AddObject(UnityEngine.Object so)
        {
            foreach (SavedObjectReferences saveableSO in Resources.LoadAll<SavedObjectReferences>(""))
            {
                saveableSO.AddObject(so);
                EditorUtility.SetDirty(saveableSO);
            }
        }

        public static bool IsSaved(UnityEngine.Object so)
        {
            foreach (SavedObjectReferences saveableSO in Resources.LoadAll<SavedObjectReferences>(""))
            {
                if (saveableSO.GetSavedObjectID(so, logIfNone: false) != 0)
                    return true;
            }

            return false;
        }

        [MenuItem("Assets/Save Object Reference", priority = 30)]
        private static void AddObjectReference()
        {
            Object selectedObject = Selection.activeObject;
            AddObject(selectedObject);
        }

        [MenuItem("Assets/Save Object Reference", true)]
        private static bool AddObjectReference_Validation()
        {
            Object selectedObject = Selection.activeObject;
            return !IsSaved(selectedObject);
        }
    }
}