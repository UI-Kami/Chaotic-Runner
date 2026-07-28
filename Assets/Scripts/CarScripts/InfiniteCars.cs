using System.Collections.Generic;
using UnityEngine;

public class CarSpawnerPool : MonoBehaviour
{
    public static CarSpawnerPool Instance { get; private set; }

    [Header("References")]
    public Transform player;
    public GameObject[] carPrefabs;

    [Header("Pool Settings")]
    public int poolSize = 10;

    [Header("Spawn Settings")]
    public float spawnDistanceAhead = 120f;
    public float respawnDistance = 250f;
    public float[] lanePositions = { -27f, -21f, -12.37f, -7.17f, 0.96f, 6f };
    public float laneY = 0.5f;

    [Header("Car Movement")]
    public float carSpeed = 25f;

    [Header("Lane Distribution & Spacing")]
    [Tooltip("Minimum distance (meters) between cars in the same lane.")]
    public float minCarSpacingInLane = 35f;

    private readonly Queue<GameObject> carPool = new Queue<GameObject>();
    private readonly List<GameObject> activeCars = new List<GameObject>();

    private float nextSpawnZ;
    private bool initialized = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (!player)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }

        InitializePool();
    }

    void InitializePool()
    {
        // Clear any leftover cars
        foreach (Transform child in transform)
            Destroy(child.gameObject);

        carPool.Clear();
        activeCars.Clear();

        // Create pooled cars
        for (int i = 0; i < poolSize; i++)
        {
            GameObject prefab = carPrefabs[Random.Range(0, carPrefabs.Length)];
            GameObject car = Instantiate(prefab, transform);
            car.SetActive(false);
            carPool.Enqueue(car);
        }

        if (player)
            nextSpawnZ = player.position.z + spawnDistanceAhead;

        initialized = true;
    }

    private void Update()
    {
        if (!initialized || player == null)
            return;

        // If an initial no-spawn window is active, skip spawning and keep nextSpawnZ ahead
        if (GameMode.IsInitialSpawnSuppressed)
        {
            nextSpawnZ = player.position.z + spawnDistanceAhead;
            return;
        }

        MoveAndRecycleCars();

        // Continuously spawn cars ahead
        while (nextSpawnZ < player.position.z + respawnDistance)
        {
            SpawnCar();
            nextSpawnZ += Random.Range(35f, 55f);
        }
    }

    // 🚗 Move forward and recycle cars behind player
    void MoveAndRecycleCars()
    {
        for (int i = activeCars.Count - 1; i >= 0; i--)
        {
            GameObject car = activeCars[i];
            if (car == null)
            {
                activeCars.RemoveAt(i);
                continue;
            }

            car.transform.Translate(Vector3.back * carSpeed * Time.deltaTime, Space.World);

            // Recycle when behind player
            if (car.transform.position.z < player.position.z - 25f)
            {
                ReturnCarToPool(car);
                activeCars.RemoveAt(i);
            }
        }
    }

    // Finds the optimal lane index to balance car distribution across lanes and prevent clustering
    int GetBestLaneIndex(float targetZ)
    {
        if (lanePositions == null || lanePositions.Length == 0) return 0;

        List<int> validLanes = new List<int>();
        Dictionary<int, int> laneCarCounts = new Dictionary<int, int>();

        for (int i = 0; i < lanePositions.Length; i++)
        {
            laneCarCounts[i] = 0;
            bool spaceOk = true;
            float laneX = lanePositions[i];

            foreach (var car in activeCars)
            {
                if (car == null || !car.activeInHierarchy) continue;

                if (Mathf.Abs(car.transform.position.x - laneX) < 1.5f)
                {
                    laneCarCounts[i]++;
                    if (Mathf.Abs(car.transform.position.z - targetZ) < minCarSpacingInLane)
                    {
                        spaceOk = false;
                    }
                }
            }

            if (spaceOk)
            {
                validLanes.Add(i);
            }
        }

        if (validLanes.Count > 0)
        {
            int minCars = int.MaxValue;
            foreach (int idx in validLanes)
            {
                if (laneCarCounts[idx] < minCars)
                    minCars = laneCarCounts[idx];
            }

            List<int> bestLanes = new List<int>();
            foreach (int idx in validLanes)
            {
                if (laneCarCounts[idx] == minCars)
                    bestLanes.Add(idx);
            }

            return bestLanes[Random.Range(0, bestLanes.Count)];
        }

        int globalMin = int.MaxValue;
        foreach (var kvp in laneCarCounts)
        {
            if (kvp.Value < globalMin)
                globalMin = kvp.Value;
        }

        List<int> fallbackLanes = new List<int>();
        foreach (var kvp in laneCarCounts)
        {
            if (kvp.Value == globalMin)
                fallbackLanes.Add(kvp.Key);
        }

        return fallbackLanes[Random.Range(0, fallbackLanes.Count)];
    }

    // 🚘 Spawns a pooled car using balanced lane distribution
    void SpawnCar()
    {
        GameObject car = GetCarFromPool();
        if (car == null) return;

        float targetZ = nextSpawnZ + Random.Range(-5f, 5f);
        int laneIdx = GetBestLaneIndex(targetZ);
        float laneX = lanePositions[laneIdx];
        Vector3 spawnPos = new Vector3(laneX, laneY, targetZ);

        car.transform.SetPositionAndRotation(spawnPos, Quaternion.Euler(0f, 180f, 0f));
        car.SetActive(true);
        activeCars.Add(car);
    }

    // 🚦 Used by map manager for section-based spawns
    public void SpawnCarOnMap(float mapStartZ, float mapEndZ)
    {
        GameObject car = GetCarFromPool();
        if (car == null) return;

        float zPos = Random.Range(mapStartZ + 20f, mapEndZ - 20f);
        int laneIdx = GetBestLaneIndex(zPos);
        float laneX = lanePositions[laneIdx];
        Vector3 spawnPos = new Vector3(laneX, laneY, zPos);

        car.transform.SetPositionAndRotation(spawnPos, Quaternion.Euler(0f, 180f, 0f));
        car.SetActive(true);
        activeCars.Add(car);
    }

    // ♻️ Pool helpers
    GameObject GetCarFromPool()
    {
        if (carPool.Count == 0)
        {
            GameObject prefab = carPrefabs[Random.Range(0, carPrefabs.Length)];
            GameObject newCar = Instantiate(prefab, transform);
            newCar.SetActive(false);
            carPool.Enqueue(newCar);
        }

        GameObject car = carPool.Dequeue();
        if (car == null)
        {
            GameObject prefab = carPrefabs[Random.Range(0, carPrefabs.Length)];
            car = Instantiate(prefab, transform);
        }

        return car;
    }

    public void ReturnCarToPool(GameObject car)
    {
        if (car == null) return;

        car.SetActive(false);
        car.transform.SetParent(transform, false);

        if (!carPool.Contains(car))
            carPool.Enqueue(car);
    }

    public void SetPoolSize(int size)
    {
        poolSize = size;
    }
}
