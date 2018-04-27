/********************************************************           
BEDROCKFRAMEWORK : https://github.com/GainDeveloper/BedrockFramework
Class wrapper for saving Object references.
********************************************************/
using UnityEngine;
using System;


namespace BedrockFramework.Saves
{
    [Serializable]
    public class SavedObjectReference<T> where T : UnityEngine.Object
    {
        T objectReference;
        [SerializeField]
        private int objectReferenceID = 0;

        public SavedObjectReference() {}

        public SavedObjectReference(T newObjectReference)
        {
            ObjectReference = newObjectReference;
        }

        public T ObjectReference
        {
            get
            {
                if (objectReferenceID == 0)
                    return default(T);

                if (objectReference == null)
                {
                    objectReference = ServiceLocator.SaveService.SavedObjectReferences.GetSavedObject<T>(objectReferenceID);
                }

                return objectReference;
            }

            set
            {
                objectReference = value;
                objectReferenceID = ServiceLocator.SaveService.SavedObjectReferences.GetSavedObjectID(objectReference);
            }
        }
    }
}