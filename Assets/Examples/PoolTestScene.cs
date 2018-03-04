using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolTestScene : MonoBehaviour {

    public GameObject toSpawn;
    public List<GameObject> spawnedGameObjects = new List<GameObject>();

    public void Awake()
    {
        BedrockFramework.Pool.PoolManager.PrePool();
    }

    public void Spawn()
    {
        spawnedGameObjects.Add(BedrockFramework.Pool.PoolManager.SpawnPrefab(toSpawn, Random.insideUnitSphere, Quaternion.identity));
    }

    public void DeSpawnAll()
    {
        foreach (GameObject spawnedGameObject in spawnedGameObjects)
            BedrockFramework.Pool.PoolManager.DeSpawnGameObject(spawnedGameObject);
        spawnedGameObjects = new List<GameObject>();
    }
}
