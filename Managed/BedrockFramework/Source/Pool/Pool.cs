/********************************************************           
BEDROCKFRAMEWORK : https://github.com/GainDeveloper/BedrockFramework

Handles instantiating prefabs and activating/ deactiving as required.
********************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;

namespace BedrockFramework.Pool
{
    public class Pool
    {
        private GameObject prefab;
        private Stack<GameObject> cache = new Stack<GameObject>();

        public Pool (GameObject prefab)
        {
            this.prefab = prefab;
        }

        public void FillPool(int count)
        {
            for (int i = 0; i < count; i++)
            {
                GameObject instance = InstantiateGameObject();
                DeSpawnGameObject(instance);
            }
        }

        public GameObject SpawnPrefab(PoolDefinition poolDefinition, Vector3 position, Quaternion rotation, Transform parent, 
            bool callOnSpawn, Action<GameObject> OnSpawn)
        {
            // Get GameObject
            GameObject clone = null;
            while (cache.Count > 0)
            {
                clone = cache.Pop();

                if (clone == null)
                    continue;

                clone.SetActive(true);
                break;
            }

            if (clone == null)
                clone = InstantiateGameObject();

            // Setup Transform
            Transform tr = clone.transform;
            if (parent != null)
            {
                tr.SetParent(parent, false);
            }
            else
            {
                tr.localPosition = position;
                tr.localRotation = rotation;
                tr.SetParent(null, false);
            }

            // PoolDefinitions
            poolDefinition.OverrideGameObjectComponents(clone);
            IPool[] pItems = clone.GetComponentsInChildren<IPool>();
            for (int i = 0; i < pItems.Length; i++)
                pItems[i].PoolDefinition = poolDefinition;

            // Spawn Events
            // Used by other systems to do any changes before OnSpawn is called.
            OnSpawn(clone);

            if (!callOnSpawn)
            {
                pItems = clone.GetComponentsInChildren<IPool>();
                for (int i = 0; i < pItems.Length; i++)
                    pItems[i].OnSpawn();
            }

            return clone;
        }

        public void DeSpawnGameObject(GameObject toDespawn, bool despawnChildren = true)
        {
            cache.Push(toDespawn);

            if (despawnChildren)
            {
                foreach (Transform child in toDespawn.GetComponentsInChildren<Transform>())
                {
                    if (child != toDespawn.transform)
                        ServiceLocator.PoolService.DeSpawnGameObject(child.gameObject, despawnChildren: false, warnNonePooled: false);
                }
            }

            IPool[] pItems = toDespawn.GetComponentsInChildren<IPool>();
            for (int i = 0; i < pItems.Length; i++)
                pItems[i].OnDeSpawn();

            toDespawn.transform.SetParent(null, false);
            GameObject.DontDestroyOnLoad(toDespawn);
            toDespawn.SetActive(false);
        }

        private GameObject InstantiateGameObject()
        {
            GameObject instantiatedPrefab = GameObject.Instantiate(prefab);
            GameObject.DontDestroyOnLoad(instantiatedPrefab);

            return instantiatedPrefab;
        }
    }

    public interface IPool
    {
        PoolDefinition PoolDefinition { set; }
        void OnSpawn();
        void OnDeSpawn();
    }

}