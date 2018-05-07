/********************************************************           
BEDROCKFRAMEWORK : https://github.com/GainDeveloper/BedrockFramework
Used as a serializable way to override components on pooled GameObjects.
Referenced by the PoolDefinitions object.
********************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;
using BedrockFramework.Network;

namespace BedrockFramework.Pool
{
    public class ComponentOverride : ScriptableObject
    {
        public virtual void OverrideGameObject(GameObject toOverride)
        {

        }
    }

    [CreateAssetMenu(fileName = "AnimatorOverride", menuName = "BedrockFramework/Overrides/Animator", order = 0)]
    public class AnimatorOverride : ComponentOverride
    {
        public RuntimeAnimatorController controller;
        public bool applyRootMotion = false;
        public AnimatorUpdateMode updateMode = AnimatorUpdateMode.Normal;
        public AnimatorCullingMode cullingMode = AnimatorCullingMode.AlwaysAnimate;

        public override void OverrideGameObject(GameObject toOverride)
        {
            Animator animator = toOverride.GetComponent<Animator>();

            if (animator == null)
                return;

            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = applyRootMotion;
            animator.updateMode = updateMode;
            animator.cullingMode = cullingMode;
        }
    }

    [CreateAssetMenu(fileName = "RigidbodyOverride", menuName = "BedrockFramework/Overrides/RigidBody", order = 0)]
    public class RigidbodyOverride : ComponentOverride
    {
        public float mass = 1;
        public float drag = 0;
        public float angularDrag = 0.05f;
        public bool useGravity = true;
        public RigidbodyConstraints constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        public override void OverrideGameObject(GameObject toOverride)
        {
            Rigidbody rigidbody = toOverride.GetComponent<Rigidbody>();

            if (rigidbody == null)
                return;

            rigidbody.mass = mass;
            rigidbody.drag = drag;
            rigidbody.angularDrag = angularDrag;
            rigidbody.useGravity = useGravity;
            rigidbody.constraints = constraints;
        }
    }

    [CreateAssetMenu(fileName = "CapsuleColliderOverride", menuName = "BedrockFramework/Overrides/CapsuleCollider", order = 0)]
    public class CapsuleColliderOverride : ComponentOverride
    {
        public PhysicMaterial material;
        public Vector3 center;
        public float radius = 0.5f;
        public float height = 2;

        public override void OverrideGameObject(GameObject toOverride)
        {
            CapsuleCollider capsuleCollider = toOverride.GetComponent<CapsuleCollider>();

            if (capsuleCollider == null)
                return;

            capsuleCollider.sharedMaterial = material;
            capsuleCollider.center = center;
            capsuleCollider.radius = radius;
            capsuleCollider.height = height;
        }
    }

    [CreateAssetMenu(fileName = "NetworkGameObjectOverride", menuName = "BedrockFramework/Overrides/NetworkGameObject", order = 0)]
    public class NetworkGameObjectOverride : ComponentOverride
    {
        public bool networkTransformEnabled = true;
        public bool networkRigidbody = true;
        public bool networkAnimator = true;


        public override void OverrideGameObject(GameObject toOverride)
        {
            NetworkGameObject network = toOverride.GetComponent<NetworkGameObject>();

            if (network == null)
                return;

            network.networkTransform.enabled = networkTransformEnabled;
            network.networkRigidbody.enabled = networkRigidbody;
            network.networkAnimator.enabled = networkAnimator;
        }
    }

    [CreateAssetMenu(fileName = "MaterialOverride", menuName = "BedrockFramework/Overrides/Material", order = 0)]
    public class MaterialOverride : ComponentOverride
    {
        public Material[] materials;

        public override void OverrideGameObject(GameObject toOverride)
        {
            Renderer renderer = toOverride.GetComponentInChildren<Renderer>();
            if (renderer == null)
                return;

            renderer.sharedMaterials = materials;
        }
    }
}