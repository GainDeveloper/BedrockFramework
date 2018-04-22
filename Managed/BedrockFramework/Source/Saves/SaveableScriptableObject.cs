/********************************************************           
BEDROCKFRAMEWORK : https://github.com/GainDeveloper/BedrockFramework

Saveable Scriptable Objects store references to themselves in a shared list.
********************************************************/
using UnityEngine;

namespace BedrockFramework.Saves
{
    public class SaveableScriptableObject : ScriptableObject
    {
        [SerializeField, HideInInspector]
        private int serializedInstanceID;
        public int SerializedInstanceID { get { return serializedInstanceID; } }

        void OnEnable()
        {
            if (!Application.isPlaying)
            {
                serializedInstanceID = GetInstanceID();
                SavedScriptableObjects.AddSavedScriptableObject(this);
            }
        }
    }
}