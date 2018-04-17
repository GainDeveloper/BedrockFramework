using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BedrockFramework.Pool
{
    public static class PoolManager
    {
        private static Dictionary<int, Pool> activePools = new Dictionary<int, Pool>();
        private static Dictionary<int, Pool> activePooledGameObjects = new Dictionary<int, Pool>();

        public delegate void PoolCreated(Pool newPool);
        public static event PoolCreated OnPoolCreated = delegate { };

        public static void PrePool() {
            foreach (PrePool prePoolObject in Resources.LoadAll<PrePool>(""))
            {
                foreach(PrePool.PrePoolObject poolObject in prePoolObject.prePooledObjects)
                {
                    GetPrefabsPool(poolObject.prefab).PrePool(poolObject.prePoolCount);
                }
            }
        }

        public static GameObject SpawnPrefab(GameObject prefab, Transform parent = null, bool subSpawn = false)
        {
            return SpawnPrefab(prefab, Vector3.zero, Quaternion.identity, parent, subSpawn);
        }

        public static T SpawnPrefab<T>(GameObject prefab, Transform parent = null, bool subSpawn = false)
            where T : Component
        {
            GameObject clone = SpawnPrefab(prefab, Vector3.zero, Quaternion.identity, parent, subSpawn);
            return (clone != null) ? clone.GetComponent<T>() : null;
        }

        public static T SpawnPrefab<T>(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null, bool subSpawn = false)
            where T : Component
        {
            GameObject clone = SpawnPrefab(prefab, position, rotation, parent, subSpawn);
            return (clone != null) ? clone.GetComponent<T>() : null;
        }

        public static GameObject SpawnPrefab(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null, bool subSpawn = false)
        {
            Pool prefabPool = GetPrefabsPool(prefab);

            GameObject spawnedPrefab = prefabPool.SpawnPrefab(position, rotation, parent, subSpawn);
            activePooledGameObjects[spawnedPrefab.GetInstanceID()] = prefabPool;
            return spawnedPrefab;
        }

        public static void DeSpawnGameObject(GameObject gameObject, bool despawnChildren = true, bool warnNonePooled = true)
        {
            int gameObjectInstanceID = gameObject.GetInstanceID();

            if (activePooledGameObjects.ContainsKey(gameObjectInstanceID))
            {
                activePooledGameObjects[gameObjectInstanceID].DeSpawnGameObject(gameObject, despawnChildren);
                activePooledGameObjects.Remove(gameObjectInstanceID);
            } else if (warnNonePooled)
            {
                Logger.Logger.LogError(Pool.logCategory, "{} is not a pooled GameObject and can not be despawned.", () => new object[] { gameObject.name });
            }
        }

        private static Pool GetPrefabsPool(GameObject prefab)
        {
            Pool prefabPool;

            if (activePools.ContainsKey(prefab.GetInstanceID()))
            {
                prefabPool = activePools[prefab.GetInstanceID()];
            }
            else
            {
                prefabPool = CreatePrefabPool(prefab);
            }

            return prefabPool;
        }

        private static Pool CreatePrefabPool(GameObject prefab)
        {
            Pool newPool = new Pool(prefab);
            activePools[prefab.GetInstanceID()] = newPool;
            OnPoolCreated(newPool);

            return newPool;
        }
    }
}