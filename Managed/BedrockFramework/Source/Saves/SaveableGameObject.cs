/********************************************************           
BEDROCKFRAMEWORK : https://github.com/GainDeveloper/BedrockFramework
Saves how this GameObject was instantiated.
TODO: Should reach out to other components in this GameObject and ask what they want to save.
********************************************************/

using System.Linq;
using UnityEngine;
using BedrockFramework.Pool;
using Sirenix.OdinInspector;
using ProtoBuf;

namespace BedrockFramework.Saves
{
    public interface ISaveableObject
    {
        int SaveDataKey { get; } 
        object GetSaveData();
        void ApplySaveData(object data);
    }

    [HideMonoScript]
    public class SaveableGameObject : MonoBehaviour, IPool
    {
        /// <summary>
        /// Save class for applying and loading transform data.
        /// </summary>
        [ProtoContract]
        public class TransformSaveData : object
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

        /// <summary>
        /// Save class for applying and loading rigidbody data.
        /// </summary>
        [ProtoContract]
        public class RigidBodySaveData : object
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

        /// <summary>
        /// Save class for applying and loading animator data.
        /// </summary>
        [ProtoContract]
        public class AnimatorSaveData : object
        {
            public static readonly int Key = Animator.StringToHash("AnimatorSaveData");

            /// <summary>
            /// Stores and applies state save data.
            /// </summary>
            [ProtoContract]
            class AnimatorLayerSaveData
            {
                [ProtoMember(1)]
                int stateHash;
                [ProtoMember(2)]
                float time;

                public AnimatorLayerSaveData() { }

                public AnimatorLayerSaveData(Animator animator, AnimatorStateInfo stateInfo)
                {
                    stateHash = stateInfo.fullPathHash;
                    time = stateInfo.normalizedTime;
                }

                public void ApplyState(Animator animator, int layer)
                {
                    animator.Play(stateHash, layer, time);
                }
            }

            /// <summary>
            /// Stores and applies animator state save data.
            /// </summary>
            [ProtoContract]
            class AnimatorParameterSaveData
            {
                [ProtoMember(1)]
                int id;
                [ProtoMember(5)]
                AnimatorControllerParameterType parameterType;
                [ProtoMember(2)]
                float floatValue;
                [ProtoMember(3)]
                int intValuie;
                [ProtoMember(4)]
                bool boolValue;

                public AnimatorParameterSaveData() { }

                public AnimatorParameterSaveData(Animator animator, AnimatorControllerParameter parameter)
                {
                    id = parameter.nameHash;
                    parameterType = parameter.type;
                    switch (parameterType)
                    {
                        case AnimatorControllerParameterType.Bool:
                            boolValue = animator.GetBool(id);
                            break;
                        case AnimatorControllerParameterType.Float:
                            floatValue = animator.GetFloat(id);
                            break;
                        case AnimatorControllerParameterType.Int:
                            intValuie = animator.GetInteger(id);
                            break;
                    }
                }

                public void ApplyParameter(Animator animator)
                {
                    switch (parameterType)
                    {
                        case AnimatorControllerParameterType.Bool:
                            animator.SetBool(id, boolValue);
                            break;
                        case AnimatorControllerParameterType.Float:
                            animator.SetFloat(id, floatValue);
                            break;
                        case AnimatorControllerParameterType.Int:
                            animator.SetInteger(id, intValuie);
                            break;
                    }
                }
            }

            [ProtoMember(3)]
            AnimatorParameterSaveData[] savedParams;
            [ProtoMember(4)]
            AnimatorLayerSaveData[] savedLayers;

            public AnimatorSaveData() { }

            public AnimatorSaveData(Animator animator)
            {
                savedParams = animator.parameters.Select(x => new AnimatorParameterSaveData(animator, x)).ToArray();

                savedLayers = new AnimatorLayerSaveData[animator.layerCount];
                for (int i = 0; i < animator.layerCount; i++)
                {
                    savedLayers[i] = new AnimatorLayerSaveData(animator, animator.GetCurrentAnimatorStateInfo(i));
                }
            }

            public static void ApplySaveData(Animator animator, AnimatorSaveData data)
            {
                for (int i = 0; i < data.savedParams.Length; i++)
                    data.savedParams[i].ApplyParameter(animator);

                for (int i = 0; i < data.savedLayers.Length; i++)
                    data.savedLayers[i].ApplyState(animator, i);

                animator.Update(Time.fixedDeltaTime);
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

            // Add component data to this object as SaveData objects.
            foreach (ISaveableObject saveObject in GetComponents<ISaveableObject>())
                savedData.savedData[saveObject.SaveDataKey] = saveObject.GetSaveData();

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

            foreach (ISaveableObject saveObject in GetComponents<ISaveableObject>())
            {
                if (!savedData.savedData.ContainsKey(saveObject.SaveDataKey))
                    continue;

                saveObject.ApplySaveData(savedData.savedData[saveObject.SaveDataKey]);
            }
        }
    }
}