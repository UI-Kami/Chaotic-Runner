using System.Collections.Generic;
using UnityEngine;

public class CarSpawnerPool : MonoBehaviour
{
    public static CarSpawnerPool Instance { get; private set; }

    [Header("References")]
    public Transform player;
    public GameObject[] carPrefabs;
    public int poolSize = 10; // reduced for performance

    [Header("Spawn Settings")]
    public float spawnDistanceAhead = 120f;
    public float respawnDistance = 250f;
    public float[] lanePositions = { -6f, -2f, 2f, 6f };
    public float laneY = 0.5f;

    [Header("Car Movement")]
    public float carSpeed = 25f;

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
        // ✅ Clear any leftover cars
        foreach (Transform child in transform)
            Destroy(child.gameObject);

        carPool.Clear();
        activeCars.Clear();

        // ✅ Create a small number of cars initially
        for (int i = 0; i < poolSize; i++)
        {
            GameObject prefab = carPrefabs[Random.Range(0, carPrefabs.Length)];
            GameObject car = Instantiate(prefab, transform);
            car.SetActive(false);
            carPool.Enqueue(car);
        }

        if (player)
            nextSpawnZ = player.position.z + spawnDistanceAhead;

        //// ✅ Spawn a few cars only (no big startup burst)
        //for (int i = 0; i < 3; i++)
        //    SpawnCar();

        initialized = true;
    }

    private void Update()
    {
        if (!initialized || player == null)
            return;

        // Move cars
        for (int i = activeCars.Count - 1; i >= 0; i--)
        {
            GameObject car = activeCars[i];
            if (car == null)
            {
                activeCars.RemoveAt(i);
                continue;
            }

            car.transform.Translate(Vector3.back * carSpeed * Time.deltaTime, Space.World);

            // Despawn when behind player
            if (car.transform.position.z < player.position.z - 25f)
            {
                ReturnCarToPool(car);
                activeCars.RemoveAt(i);
            }
        }

        // Spawn cars ahead
        while (nextSpawnZ < player.position.z + respawnDistance)
        {
            SpawnCar();
            nextSpawnZ += Random.Range(35f, 55f);
        }
    }

    void SpawnCar()
    {
        GameObject car = GetCarFromPool();
        if (car == null) return;

        float laneX = lanePositions[Random.Range(0, lanePositions.Length)];
        Vector3 spawnPos = new Vector3(laneX, laneY, nextSpawnZ + Random.Range(-5f, 5f));

        // Rotate cars to face player
        car.transform.SetPositionAndRotation(spawnPos, Quaternion.Euler(0f, 180f, 0f));
        car.SetActive(true);

        activeCars.Add(car);
    }

    GameObject GetCarFromPool()
    {
        // Refill pool if needed
        if (carPool.Count == 0)
        {
            GameObject prefab = carPrefabs[Random.Range(0, carPrefabs.Length)];
            GameObject newCar = Instantiate(prefab, transform);
            newCar.SetActive(false);
            carPool.Enqueue(newCar);
        }

        GameObject car = carPool.Dequeue();

        // Handle destroyed references
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

        // Instead of Destroy, disable and reuse
        car.SetActive(false);
        car.transform.SetParent(transform, false);

        if (!carPool.Contains(car))
            carPool.Enqueue(car);
    }

    // Called by MapManager to spawn cars per map section
    public void SpawnCarOnMap(float mapStartZ, float mapEndZ)
    {
        GameObject car = GetCarFromPool();
        if (car == null) return;

        float laneX = lanePositions[Random.Range(0, lanePositions.Length)];
        float zPos = Random.Range(mapStartZ + 20f, mapEndZ - 20f);

        Vector3 spawnPos = new Vector3(laneX, laneY, zPos);
        car.transform.SetPositionAndRotation(spawnPos, Quaternion.Euler(0f, 180f, 0f));
        car.SetActive(true);

        activeCars.Add(car);
    }
    public void SetPoolSize(int size)
    {
        poolSize = size;
    }

}
