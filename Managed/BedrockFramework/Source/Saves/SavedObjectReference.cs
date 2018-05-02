/********************************************************           
BEDROCKFRAMEWORK : https://github.com/GainDeveloper/BedrockFramework
Class wrapper for saving Object references.
********************************************************/
using UnityEngine;
using Sirenix.OdinInspector;
using ProtoBuf;

namespace BedrockFramework.Saves
{
    [ProtoContract]
    public class SavedObjectReference<T> where T : UnityEngine.Object
    {
        [ReadOnly, ShowInInspector]
        T objectReference;
        [ProtoMember(1)]
        private short objectReferenceID = 0;

        public SavedObjectReference() {}

        public SavedObjectReference(T newObjectReference)
        {
            ObjectReference = newObjectReference;
        }

        public SavedObjectReference(short newObjectReferenceID)
        {
            objectReferenceID = newObjectReferenceID;
        }

        public short ObjectReferenceID { get { return objectReferenceID; } }

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