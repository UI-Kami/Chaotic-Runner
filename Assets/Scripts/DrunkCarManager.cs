using System.Collections.Generic;
using UnityEngine;

public class DrunkCarSpawner : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public GameObject drunkCarPrefab;

    [Header("Spawn Settings")]
    public float[] lanePositions = { -27f, -21f, 0.96f, 6f };
    public float laneY = 0.6f;
    public float minSpawnAhead = 150f;
    public float maxSpawnAhead = 200f;
    public float yOffset = 0.5f;

    [Header("Spawn Control")]
    [Tooltip("Chance of spawning a drunk car per new map section (0 to 1).")]
    [Range(0f, 1f)]
    public float spawnChancePerMap = 0.15f; // 15% chance per map

    [Tooltip("Maximum drunk cars that can exist at once.")]
    public int maxActiveDrunkCars = 3;

    private readonly List<GameObject> activeDrunkCars = new();
    private float lastMapZ = 0f;

    private void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }

        if (drunkCarPrefab == null)
        {
            Debug.LogError("[DrunkCarSpawner] ❌ Missing drunk car prefab!");
            enabled = false;
        }
    }

    private void Update()
    {
        CleanupDestroyedCars();
    }

    // 🔹 This should be called from TimedMapSpawner when each new map section is spawned.
    // But it can also auto-track based on player's forward progress.
    public void OnNewMapSpawned(float mapStartZ, float mapEndZ)
    {
        if (drunkCarPrefab == null || player == null)
            return;

        if (activeDrunkCars.Count >= maxActiveDrunkCars)
            return; // too many active drunk cars

        if (Random.value > spawnChancePerMap)
            return; // chance failed, skip this section

        // Determine a random lane and spawn Z position within the map section
        float laneX = lanePositions[Random.Range(0, lanePositions.Length)];
        float spawnZ = Random.Range(mapStartZ + minSpawnAhead, mapEndZ - maxSpawnAhead);
        float spawnY = laneY + yOffset;

        Vector3 spawnPos = new Vector3(laneX, spawnY, spawnZ);

        // Instantiate drunk car
        GameObject drunk = Instantiate(drunkCarPrefab, spawnPos, Quaternion.identity);
        var ai = drunk.GetComponent<DrunkDriverAI>();
        if (ai != null)
        {
            ai.player = player;
        }

        activeDrunkCars.Add(drunk);
        Debug.Log($"🚗💥 [DrunkCarSpawner] Drunk car spawned at {spawnPos}");
    }

    private void CleanupDestroyedCars()
    {
        for (int i = activeDrunkCars.Count - 1; i >= 0; i--)
        {
            if (activeDrunkCars[i] == null)
                activeDrunkCars.RemoveAt(i);
        }
    }
}
