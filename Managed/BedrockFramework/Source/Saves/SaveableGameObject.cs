/********************************************************           
BEDROCKFRAMEWORK : https://github.com/GainDeveloper/BedrockFramework
Saves how this GameObject was instantiated.
TODO: Should reach out to other components in this GameObject and ask what they want to save.
********************************************************/

using System;
using UnityEngine;
using BedrockFramework.Pool;
using BedrockFramework.Utilities;
using Sirenix.OdinInspector;

namespace BedrockFramework.Saves
{
    [HideMonoScript]
    public class SaveableGameObject : MonoBehaviour, IPool
    {
        //TODO: Remaining transform values.
        class TransformSaveData : SaveService.SavedData
        {
            public static readonly int Key = Animator.StringToHash("");

            public Vector3 position;

            public TransformSaveData(Transform transform)
            {
                position = transform.position;
            }

            public static void ApplyTransformSaveData(GameObject gameObject, TransformSaveData transformData)
            {
                gameObject.transform.position = transformData.position;
            }
        }

        [ReadOnly, ShowInInspector]
        private PoolDefinition poolDefinition;

        // Pool
        PoolDefinition IPool.PoolDefinition { set { poolDefinition = value; } }
        void IPool.OnDeSpawn() {}
        void IPool.OnSpawn() {}

        public SaveService.SavedGameObject GameObjectSaveData()
        {
            SaveService.SavedGameObject savedData = new SaveService.SavedGameObject(poolDefinition);

            // TODO: Add component data to this object as SaveData objects. Key should be the components type.

            // Will need to do Unity components manually here. (Transform, Rigidbody, Animator ect.)
            savedData.savedData[TransformSaveData.Key] = new TransformSaveData(transform);

            return savedData;
        }

        public void ApplySaveData(SaveService.SavedGameObject savedData)
        {
            if (savedData.savedData.ContainsKey(TransformSaveData.Key))
                TransformSaveData.ApplyTransformSaveData(gameObject, (TransformSaveData)savedData.savedData[TransformSaveData.Key]);
        }
    }
}