using System.Collections;
using UnityEngine;

public class TimedMapSpawner : MonoBehaviour
{
    [Header("References (auto-find if blank)")]
    public MapManager mapManager;
    public PowerSpawner powerSpawner;
    public MeteoriteSpawner meteoriteSpawner;
    public RoadObstacles roadObstacles;

    [Header("Spawn Settings")]
    public float spawnInterval = 2f;
    public float initialDelay = 0.5f;
    public bool enableSpawning = true;

    void Awake()
    {
        if (mapManager == null) mapManager = FindAnyObjectByType<MapManager>();
        if (powerSpawner == null) powerSpawner = FindAnyObjectByType<PowerSpawner>();
        if (meteoriteSpawner == null) meteoriteSpawner = FindAnyObjectByType<MeteoriteSpawner>();
        if (roadObstacles == null) roadObstacles = FindAnyObjectByType<RoadObstacles>();
    }

    void Start()
    {
        // If mapManager exists, ensure its power/obstacle managers are wired (keeps compatibility)
        if (mapManager != null)
        {
            if (powerSpawner != null) mapManager.powerSpawner = powerSpawner;
            if (roadObstacles != null) mapManager.roadObstacles = roadObstacles;
        }

        // Start the periodic spawn loop that calls the MapManager to spawn new maps.
        if (enableSpawning && mapManager != null)
            StartCoroutine(SpawnLoop());
    }

    private IEnumerator SpawnLoop()
    {
        // small initial delay so scenes that create initial maps have time to finish setup
        if (initialDelay > 0f) yield return new WaitForSeconds(initialDelay);

        var wait = new WaitForSeconds(Mathf.Max(0.01f, spawnInterval));

        while (true)
        {
            yield return wait;

            if (!enableSpawning || mapManager == null) continue;

            // Optional safety: don't spawn when map prefab is missing
            mapManager.SpawnNextMap();
        }
    }

    // Keep compatibility for other code that calls these on the old script:
    public void RequestDestroyMap(GameObject map, float delay = -1f)
    {
        mapManager?.RequestDestroyMap(map, delay);
    }

    public void ReturnPowerToPool(GameObject power)
    {
        // forwards to the PowerSpawner or MapManager depending on your previous wiring
        if (powerSpawner != null)
            powerSpawner.ReturnPowerToPool(power);
        else
            mapManager?.powerSpawner?.ReturnPowerToPool(power);
    }
}