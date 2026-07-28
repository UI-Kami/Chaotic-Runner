using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapManager : MonoBehaviour
{
    [Header("Map Settings")]
    public GameObject mapPrefab;
    public Transform spawnOrigin;
    public float mapLength = 200f;
    public float overlapFix = 0.5f;
    public bool autoDetectLength = true;
    public bool startImmediately = true;
    public float defaultDestroyDelay = 2f;

    [Header("Dynamic Generation Settings")]
    public Transform playerTransform;
    [Tooltip("Minimum distance (meters) to maintain generated road ahead of the player.")]
    public float minSpawnAheadDistance = 600f;

    [Header("References")]
    public CarSpawnerPool carSpawner;
    public PowerSpawner powerSpawner;
    public RoadObstacles roadObstacles;

    // state
    private float currentZ;
    public readonly List<GameObject> activeMaps = new();

    void Start()
    {
        if (mapPrefab == null)
        {
            Debug.LogError("[MapManager] mapPrefab is not assigned!");
            return;
        }

        if (playerTransform == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p) playerTransform = p.transform;
        }

        if (autoDetectLength)
            mapLength = GetMapLength(mapPrefab);

        currentZ = spawnOrigin ? spawnOrigin.position.z : 0f;

        if (carSpawner == null)
            carSpawner = FindAnyObjectByType<CarSpawnerPool>();

        if (powerSpawner == null)
            powerSpawner = FindAnyObjectByType<PowerSpawner>();

        if (roadObstacles == null)
            roadObstacles = FindAnyObjectByType<RoadObstacles>();

        powerSpawner?.InitializePowerPool();
        roadObstacles?.InitializePool();

        if (startImmediately)
        {
            for (int i = 0; i < 8; i++)
                SpawnNextMap();
        }
    }

    void Update()
    {
        if (playerTransform == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }

        // Dynamically spawn new map sections ahead of the player to ensure they never catch up or fall out of the world
        if (playerTransform != null && mapPrefab != null)
        {
            while (currentZ - playerTransform.position.z < minSpawnAheadDistance)
            {
                SpawnNextMap();
            }
        }
    }

    // Spawns next map and delegates car/power/obstacle spawns
    public void SpawnNextMap()
    {
        if (mapPrefab == null) return;

        Vector3 spawnPos = new Vector3(0f, 0f, currentZ);
        GameObject newMap = Instantiate(mapPrefab, spawnPos, Quaternion.identity);
        currentZ += mapLength - overlapFix;
        activeMaps.Add(newMap);

        float mapStartZ = spawnPos.z;
        float mapEndZ = mapStartZ + mapLength;

        MapEndTrigger endTrigger = newMap.GetComponentInChildren<MapEndTrigger>();
        if (endTrigger != null)
        {
            endTrigger.spawner = FindAnyObjectByType<TimedMapSpawner>() ?? gameObject.GetComponent<TimedMapSpawner>();
            endTrigger.destroyDelay = defaultDestroyDelay;
        }

        bool suppressed = GameMode.IsInitialSpawnSuppressed;
        Debug.Log($"MapManager: SpawnNextMap called for {newMap.name} suppressed={suppressed} powerSpawnerAssigned={(powerSpawner!=null)}");

        if (carSpawner != null && !suppressed)
        {
            int carCount = Random.Range(1, 4);
            for (int i = 0; i < carCount; i++)
                carSpawner.SpawnCarOnMap(mapStartZ, mapEndZ);

            FindAnyObjectByType<DrunkCarSpawner>()?.OnNewMapSpawned(mapStartZ, mapEndZ);
        }

        // spawn powers and obstacles via dedicated managers (if assigned)
        if (!suppressed)
        {
            powerSpawner?.SpawnPowersOnMap(newMap, mapStartZ, mapEndZ);
            roadObstacles?.SpawnObstaclesForMap(newMap, powerSpawner != null ? powerSpawner.lanePositions : new float[0], mapStartZ, mapEndZ);
        }
    }

    public void RequestDestroyMap(GameObject map, float delay = -1f)
    {
        if (map == null) return;
        float d = delay > 0f ? delay : defaultDestroyDelay;

        // Return powers and obstacles on this map to their pools
        powerSpawner?.ReturnPowersOnMap(map);
        roadObstacles?.ReturnObstaclesOnMap(map);

        StartCoroutine(DestroyMapAfterDelay(map, d));
    }

    private IEnumerator DestroyMapAfterDelay(GameObject map, float delay)
    {
        if (map == null) yield break;
        yield return new WaitForSeconds(delay);
        if (map == null) yield break;

        if (activeMaps.Contains(map))
            activeMaps.Remove(map);

        Destroy(map);
    }

    public GameObject GetLastMap()
    {
        if (activeMaps.Count == 0) return null;
        return activeMaps[activeMaps.Count - 1];
    }

    public float GetMapLength() => mapLength;

    private float GetMapLength(GameObject prefab)
    {
        Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return mapLength;
        Bounds bounds = renderers[0].bounds;
        foreach (Renderer r in renderers)
            bounds.Encapsulate(r.bounds);
        return bounds.size.z;
    }
}