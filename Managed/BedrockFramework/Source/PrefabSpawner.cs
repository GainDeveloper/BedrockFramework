using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BedrockFramework
{
    public class PrefabSpawner: MonoBehaviour
    {
        public GameObject prefab;

        static PrefabSpawner()
        {
            Pool.PoolManager.OnPoolCreated += PoolManager_OnPoolCreated;
        }

        private static void PoolManager_OnPoolCreated(Pool.Pool newPool)
        {
            newPool.OnPrefabSpawned += NewPool_OnPrefabSpawned;
        }

        private static void NewPool_OnPrefabSpawned(GameObject newPrefab)
        {
            foreach (PrefabSpawner prefabSpawner in newPrefab.GetComponentsInChildren<PrefabSpawner>())
            {
                prefabSpawner.SpawnPrefab();
            }
        }

        public GameObject SpawnPrefab()
        {
            return Pool.PoolManager.SpawnPrefab(prefab, transform, subSpawn:true);
        }
    }
}