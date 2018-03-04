using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BedrockFramework.Pool
{
    public class Pool
    {
        public static readonly string logCategory = "Pool";

        private GameObject prefab;
        private Stack<GameObject> cache = new Stack<GameObject>();

        public delegate void PrefabSpawned(GameObject newPrefab);
        public event PrefabSpawned OnPrefabSpawned;

        public Pool (GameObject prefab)
        {
            this.prefab = prefab;
        }

        public GameObject SpawnPrefab(Vector3 position, Quaternion rotation, Transform parent, bool subSpawn)
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
                clone = InstantiateGameObject(prefab);

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

            // Spawn Events
            // Used by other systems to spawn any additional gameObjects before OnSpawn is called.
            if (OnPrefabSpawned != null)
                OnPrefabSpawned(clone);

            if (!subSpawn)
            {
                IPool[] pItems = clone.GetComponentsInChildren<IPool>();
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
                        PoolManager.DeSpawnGameObject(child.gameObject, despawnChildren: false, warnNonePooled: false);
                }
            }

            IPool[] pItems = toDespawn.GetComponentsInChildren<IPool>();
            for (int i = 0; i < pItems.Length; i++)
                pItems[i].OnDeSpawn();

            toDespawn.transform.SetParent(null, false);
            GameObject.DontDestroyOnLoad(toDespawn);
            toDespawn.SetActive(false);
        }

        private GameObject InstantiateGameObject(GameObject prefab)
        {
            GameObject instantiatedPrefab = GameObject.Instantiate(prefab);
            GameObject.DontDestroyOnLoad(instantiatedPrefab);

            return instantiatedPrefab;
        }
    }

    public interface IPool
    {
        void OnSpawn();
        void OnDeSpawn();
    }

}