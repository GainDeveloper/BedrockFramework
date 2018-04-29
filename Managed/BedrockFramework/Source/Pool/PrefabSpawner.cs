/********************************************************           
BEDROCKFRAMEWORK : https://github.com/GainDeveloper/BedrockFramework

Component for spawning pooled prefabs within another pooled object.
********************************************************/

using UnityEngine;
using Sirenix.OdinInspector;

namespace BedrockFramework.Pool
{
    [AddComponentMenu("BedrockFramework/PoolSpawner")]
    public class PoolSpawner: MonoBehaviour
    {
        [AssetsOnly]
        public PoolDefinition poolDefinition;

        static PoolSpawner()
        {
            ServiceLocator.PoolService.OnPrefabSpawned += OnPrefabSpawned;
        }

        private static void OnPrefabSpawned(GameObject newPrefab)
        {
            foreach (PoolSpawner prefabSpawner in newPrefab.GetComponentsInChildren<PoolSpawner>())
            {
                prefabSpawner.SpawnPrefab(true);
            }
        }

        public void SpawnPrefab(bool subSpawn)
        {
            ServiceLocator.PoolService.SpawnDefinition(poolDefinition, transform, subSpawn);
        }

        void OnEnable()
        {
            //TODO: Should be waiting until the game says it has setup.
            ServiceLocator.SceneService.OnFinishedLoading += SceneService_OnFinishedLoading;
        }

        void OnDisable()
        {
            ServiceLocator.SceneService.OnFinishedLoading -= SceneService_OnFinishedLoading;
        }

        private void SceneService_OnFinishedLoading(Scenes.SceneLoadInfo loadedScene)
        {
            if (!ServiceLocator.PoolService.IsGameObjectPooled(gameObject))
            {
                SpawnPrefab(false);
            }
        }
    }
}