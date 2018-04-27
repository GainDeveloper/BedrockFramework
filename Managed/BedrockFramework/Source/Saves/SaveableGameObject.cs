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
using ProtoBuf;

namespace BedrockFramework.Saves
{
    [HideMonoScript]
    public class SaveableGameObject : MonoBehaviour, IPool
    {
        [ProtoContract]
        public class TransformSaveData : SaveService.SavedData
        {
            public static readonly int Key = Animator.StringToHash("TransformSaveData");

            [ProtoMember(1)]
            public SaveableVector3 position;
            [ProtoMember(2)]
            public SaveableQuaternion rotation;

            public TransformSaveData() { }

            public TransformSaveData(Transform transform)
            {
                position = transform.position;
                rotation = transform.rotation;
            }

            public static void ApplySaveData(Transform transform, TransformSaveData data)
            {
                transform.transform.position = data.position;
                transform.transform.rotation = data.rotation;
            }
        }

        [ProtoContract]
        public class RigidBodySaveData : SaveService.SavedData
        {
            public static readonly int Key = Animator.StringToHash("RigidBodySaveData");

            [ProtoMember(1)]
            public SaveableVector3 velocity;
            [ProtoMember(2)]
            public SaveableVector3 angularVelocity;

            public RigidBodySaveData() { }

            public RigidBodySaveData(Rigidbody rigidbody)
            {
                velocity = rigidbody.velocity;
                angularVelocity = rigidbody.angularVelocity;
            }

            public static void ApplySaveData(Rigidbody rigidbody, RigidBodySaveData data)
            {
                rigidbody.velocity = data.velocity;
                rigidbody.angularVelocity = data.angularVelocity;
            }
        }

        [ProtoContract]
        public class AnimatorSaveData : SaveService.SavedData
        {
            public static readonly int Key = Animator.StringToHash("AnimatorSaveData");

            [ProtoMember(1)]
            public int currentStateHash;
            [ProtoMember(2)]
            public float currentStateTime;

            public AnimatorSaveData() { }

            public AnimatorSaveData(Animator animator)
            {
                //TODO: Serialize animation properties.
                //TODO: Handle multiple animation layers.
                currentStateHash = animator.GetCurrentAnimatorStateInfo(0).fullPathHash;
                currentStateTime = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            }

            public static void ApplySaveData(Animator animator, AnimatorSaveData data)
            {
                animator.Play(data.currentStateHash, 0, data.currentStateTime);
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

            // Do Unity components manually here. (Transform, Rigidbody, Animator ect.)
            savedData.savedData[TransformSaveData.Key] = new TransformSaveData(transform);

            Rigidbody rb = gameObject.GetComponent<Rigidbody>();
            if (rb != null)
                savedData.savedData[RigidBodySaveData.Key] = new RigidBodySaveData(rb);

            Animator animator = gameObject.GetComponent<Animator>();
            if (animator != null)
                savedData.savedData[AnimatorSaveData.Key] = new AnimatorSaveData(animator);

            return savedData;
        }

        public void ApplySaveData(SaveService.SavedGameObject savedData)
        {
            if (savedData.savedData.ContainsKey(TransformSaveData.Key))
                TransformSaveData.ApplySaveData(gameObject.transform, (TransformSaveData)savedData.savedData[TransformSaveData.Key]);
            if (savedData.savedData.ContainsKey(RigidBodySaveData.Key))
                RigidBodySaveData.ApplySaveData(gameObject.GetComponent<Rigidbody>(), (RigidBodySaveData)savedData.savedData[RigidBodySaveData.Key]);
            if (savedData.savedData.ContainsKey(AnimatorSaveData.Key))
                AnimatorSaveData.ApplySaveData(gameObject.GetComponent<Animator>(), (AnimatorSaveData)savedData.savedData[AnimatorSaveData.Key]);
        }
    }
}