/********************************************************           
BEDROCKFRAMEWORK : https://github.com/GainDeveloper/BedrockFramework

Component for spawning pooled prefabs within another pooled object.
********************************************************/

using UnityEngine;
using Sirenix.OdinInspector;

namespace BedrockFramework.Pool
{
    [AddComponentMenu("BedrockFramework/PrefabSpawner")]
    public class PrefabSpawner: MonoBehaviour
    {
        [AssetsOnly]
        public GameObject prefab;

        static PrefabSpawner()
        {
            ServiceLocator.PoolService.OnPrefabSpawned += OnPrefabSpawned;
        }

        private static void OnPrefabSpawned(GameObject newPrefab)
        {
            foreach (PrefabSpawner prefabSpawner in newPrefab.GetComponentsInChildren<PrefabSpawner>())
            {
                prefabSpawner.SpawnPrefab(true);
            }
        }

        public void SpawnPrefab(bool subSpawn)
        {
            ServiceLocator.PoolService.SpawnPrefab(prefab, transform, subSpawn);
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

        private void SceneService_OnFinishedLoading(Scenes.SceneDefinition loadedScene)
        {
            if (!ServiceLocator.PoolService.IsGameObjectPooled(gameObject))
            {
                SpawnPrefab(false);
            }
        }
    }
}