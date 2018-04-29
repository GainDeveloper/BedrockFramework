/********************************************************           
BEDROCKFRAMEWORK : https://github.com/GainDeveloper/BedrockFramework

Handles creating pools and tracking pooled objects.
********************************************************/

using System;
using System.Linq;
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
        GameObject SpawnDefinition(PoolDefinition prefab, Transform parent = null, bool subSpawn = false);
        T SpawnDefinition<T>(PoolDefinition prefab, Transform parent = null, bool subSpawn = false) where T : Component;
        T SpawnDefinition<T>(PoolDefinition prefab, Vector3 position, Quaternion rotation, Transform parent = null, bool subSpawn = false) where T : Component;
        GameObject SpawnDefinition(PoolDefinition prefab, Vector3 position, Quaternion rotation, Transform parent = null, bool subSpawn = false);

        void DeSpawnGameObject(GameObject gameObject, bool despawnChildren = true, bool warnNonePooled = true);
    }

    public class NullPoolService : IPoolService
    {
        public event Action<Pool> OnPoolCreated = delegate { };
        public event Action<GameObject> OnPrefabSpawned = delegate { };

        public bool IsGameObjectPooled(GameObject gameObject) { return false; }

        public void PrePool() { }
        public GameObject SpawnDefinition(PoolDefinition prefab, Transform parent = null, bool subSpawn = false) { return null; }
        public T SpawnDefinition<T>(PoolDefinition prefab, Transform parent = null, bool subSpawn = false) where T : Component { return default(T); }
        public T SpawnDefinition<T>(PoolDefinition prefab, Vector3 position, Quaternion rotation, Transform parent = null, bool subSpawn = false) where T : Component { return default(T); }
        public GameObject SpawnDefinition(PoolDefinition prefab, Vector3 position, Quaternion rotation, Transform parent = null, bool subSpawn = false) { return null; }

        public void DeSpawnGameObject(GameObject gameObject, bool despawnChildren = true, bool warnNonePooled = true) { }
    }

    public class PoolService : Service, IPoolService
    {
        const string PoolServiceLog = "Pools";

        private static Dictionary<int, Pool> activePools = new Dictionary<int, Pool>();
        private static Dictionary<GameObject, Pool> activePooledGameObjects = new Dictionary<GameObject, Pool>();

        public event Action<Pool> OnPoolCreated = delegate { };
        public event Action<GameObject> OnPrefabSpawned = delegate { };

        public PoolService(MonoBehaviour owner): base(owner)
        {
            ServiceLocator.SceneService.OnUnload += DeSpawnAllPools;
        }

        private void DeSpawnAllPools()
        {
            DevTools.Logger.Log(PoolServiceLog, "Despawning All.");

            GameObject[] activePooledGameObjectsCache = activePooledGameObjects.Select(x => x.Key).ToArray();

            for (int i = 0; i < activePooledGameObjectsCache.Length; i++)
            {
                DeSpawnGameObject(activePooledGameObjectsCache[i]);
            }
        }

        public bool IsGameObjectPooled(GameObject gameObject)
        {
            return activePooledGameObjects.ContainsKey(gameObject);
        }

        public void PrePool() {
            DevTools.Logger.Log(PoolServiceLog, "PrePooling.");
            foreach (PrePool prePoolObject in Resources.LoadAll<PrePool>(""))
            {
                foreach(PrePool.PrePoolObject poolObject in prePoolObject.prePooledObjects)
                {
                    GetPrefabsPool(poolObject.prefab, warnNewPool: false).FillPool(poolObject.prePoolCount);
                }
            }
        }

        public GameObject SpawnDefinition(PoolDefinition poolDefinition, Transform parent = null, bool subSpawn = false)
        {
            return SpawnDefinition(poolDefinition, Vector3.zero, Quaternion.identity, parent, subSpawn);
        }

        public T SpawnDefinition<T>(PoolDefinition poolDefinition, Transform parent = null, bool subSpawn = false)
            where T : Component
        {
            GameObject clone = SpawnDefinition(poolDefinition, Vector3.zero, Quaternion.identity, parent, subSpawn);
            return (clone != null) ? clone.GetComponent<T>() : null;
        }

        public T SpawnDefinition<T>(PoolDefinition poolDefinition, Vector3 position, Quaternion rotation, Transform parent = null, bool subSpawn = false)
            where T : Component
        {
            GameObject clone = SpawnDefinition(poolDefinition, position, rotation, parent, subSpawn);
            return (clone != null) ? clone.GetComponent<T>() : null;
        }

        public GameObject SpawnDefinition(PoolDefinition poolDefinition, Vector3 position, Quaternion rotation, Transform parent = null, bool subSpawn = false)
        {
            if (!IsGameObjectPrefab(poolDefinition.PooledObject))
            {
                DevTools.Logger.LogError(PoolServiceLog, "Instanced GameObject {} can not be pooled.", () => new object[] { poolDefinition.name });
                return null;
            }

            Pool prefabPool = GetPrefabsPool(poolDefinition.PooledObject);

            GameObject spawnedPrefab = prefabPool.SpawnPrefab(poolDefinition, position, rotation, parent, subSpawn, (x) => OnPrefabSpawned(x));
            activePooledGameObjects[spawnedPrefab] = prefabPool;
            return spawnedPrefab;
        }

        public void DeSpawnGameObject(GameObject gameObject, bool despawnChildren = true, bool warnNonePooled = true)
        {
            if (activePooledGameObjects.ContainsKey(gameObject))
            {
                activePooledGameObjects[gameObject].DeSpawnGameObject(gameObject, despawnChildren);
                activePooledGameObjects.Remove(gameObject);
            } else if (warnNonePooled)
            {
                DevTools.Logger.LogError(PoolServiceLog, "{} is not a pooled GameObject and can not be despawned.", () => new object[] { gameObject.name });
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
                    DevTools.Logger.Log(PoolServiceLog, "New pool for {} created.", () => new object[] { prefab.name });
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