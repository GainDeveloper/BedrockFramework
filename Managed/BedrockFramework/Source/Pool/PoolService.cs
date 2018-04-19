/********************************************************           
BEDROCKFRAMEWORK : https://github.com/GainDeveloper/BedrockFramework

Handles creating pools and tracking pooled objects.
********************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;

namespace BedrockFramework.Pool
{
    public interface IPoolService
    {
        event Action<Pool> OnPoolCreated;
        event Action<GameObject> OnPrefabSpawned;

        bool IsGameObjectPooled(GameObject gameObject);

        void PrePool();
        GameObject SpawnPrefab(GameObject prefab, Transform parent = null, bool subSpawn = false);
        T SpawnPrefab<T>(GameObject prefab, Transform parent = null, bool subSpawn = false) where T : Component;
        T SpawnPrefab<T>(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null, bool subSpawn = false) where T : Component;
        GameObject SpawnPrefab(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null, bool subSpawn = false);

        void DeSpawnGameObject(GameObject gameObject, bool despawnChildren = true, bool warnNonePooled = true);
    }

    public class NullPoolService : IPoolService
    {
        public event Action<Pool> OnPoolCreated = delegate { };
        public event Action<GameObject> OnPrefabSpawned = delegate { };

        public bool IsGameObjectPooled(GameObject gameObject) { return false; }

        public void PrePool() { }
        public GameObject SpawnPrefab(GameObject prefab, Transform parent = null, bool subSpawn = false) { return null; }
        public T SpawnPrefab<T>(GameObject prefab, Transform parent = null, bool subSpawn = false) where T : Component { return default(T); }
        public T SpawnPrefab<T>(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null, bool subSpawn = false) where T : Component { return default(T); }
        public GameObject SpawnPrefab(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null, bool subSpawn = false) { return null; }

        public void DeSpawnGameObject(GameObject gameObject, bool despawnChildren = true, bool warnNonePooled = true) { }
    }

    public class PoolService : Service, IPoolService
    {
        const string PoolServiceLog = "Pools";

        private static Dictionary<int, Pool> activePools = new Dictionary<int, Pool>();
        private static Dictionary<int, Pool> activePooledGameObjects = new Dictionary<int, Pool>();

        public event Action<Pool> OnPoolCreated = delegate { };
        public event Action<GameObject> OnPrefabSpawned = delegate { };

        public PoolService(MonoBehaviour owner): base(owner) { }

        public bool IsGameObjectPooled(GameObject gameObject)
        {
            return activePooledGameObjects.ContainsKey(gameObject.GetInstanceID());
        }

        public void PrePool() {
            Logger.Logger.Log(PoolServiceLog, "PrePooling.");
            foreach (PrePool prePoolObject in Resources.LoadAll<PrePool>(""))
            {
                foreach(PrePool.PrePoolObject poolObject in prePoolObject.prePooledObjects)
                {
                    GetPrefabsPool(poolObject.prefab, warnNewPool: false).FillPool(poolObject.prePoolCount);
                }
            }
        }

        public GameObject SpawnPrefab(GameObject prefab, Transform parent = null, bool subSpawn = false)
        {
            return SpawnPrefab(prefab, Vector3.zero, Quaternion.identity, parent, subSpawn);
        }

        public T SpawnPrefab<T>(GameObject prefab, Transform parent = null, bool subSpawn = false)
            where T : Component
        {
            GameObject clone = SpawnPrefab(prefab, Vector3.zero, Quaternion.identity, parent, subSpawn);
            return (clone != null) ? clone.GetComponent<T>() : null;
        }

        public T SpawnPrefab<T>(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null, bool subSpawn = false)
            where T : Component
        {
            GameObject clone = SpawnPrefab(prefab, position, rotation, parent, subSpawn);
            return (clone != null) ? clone.GetComponent<T>() : null;
        }

        public GameObject SpawnPrefab(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null, bool subSpawn = false)
        {
            if (!IsGameObjectPrefab(prefab))
            {
                Logger.Logger.LogError(PoolServiceLog, "Instanced GameObject {} can not be pooled.", () => new object[] { prefab.name });
                return null;
            }

            Pool prefabPool = GetPrefabsPool(prefab);

            GameObject spawnedPrefab = prefabPool.SpawnPrefab(position, rotation, parent, subSpawn, (x) => OnPrefabSpawned(x));
            activePooledGameObjects[spawnedPrefab.GetInstanceID()] = prefabPool;
            return spawnedPrefab;
        }

        public void DeSpawnGameObject(GameObject gameObject, bool despawnChildren = true, bool warnNonePooled = true)
        {
            int gameObjectInstanceID = gameObject.GetInstanceID();

            if (activePooledGameObjects.ContainsKey(gameObjectInstanceID))
            {
                activePooledGameObjects[gameObjectInstanceID].DeSpawnGameObject(gameObject, despawnChildren);
                activePooledGameObjects.Remove(gameObjectInstanceID);
            } else if (warnNonePooled)
            {
                Logger.Logger.LogError(PoolServiceLog, "{} is not a pooled GameObject and can not be despawned.", () => new object[] { gameObject.name });
            }
        }

        /// <summary>
        /// Gets the pool for a specific prefab.
        /// Creates a pool if one does not exist.
        /// </summary>
        /// <param name="prefab"></param>
        /// <returns></returns>
        private Pool GetPrefabsPool(GameObject prefab, bool warnNewPool = true)
        {
            Pool prefabPool;

            if (activePools.ContainsKey(prefab.GetInstanceID()))
            {
                prefabPool = activePools[prefab.GetInstanceID()];
            }
            else
            {
                prefabPool = CreatePrefabPool(prefab);
                if (warnNewPool)
                    Logger.Logger.Log(PoolServiceLog, "New pool for {} created.", () => new object[] { prefab.name });
            }

            return prefabPool;
        }

        /// <summary>
        /// Creates a new pool for the GameObject.
        /// </summary>
        /// <param name="prefab"></param>
        /// <returns></returns>
        private Pool CreatePrefabPool(GameObject prefab)
        {
            Pool newPool = new Pool(prefab);
            activePools[prefab.GetInstanceID()] = newPool;
            OnPoolCreated(newPool);

            return newPool;
        }

        /// <summary>
        /// Test if a GameObject is a prefab or a scene instance.
        /// </summary>
        /// <param name="prefab"></param>
        /// <returns></returns>
        private bool IsGameObjectPrefab(GameObject prefab)
        {
            return prefab.scene.rootCount == 0;
        }
    }
}