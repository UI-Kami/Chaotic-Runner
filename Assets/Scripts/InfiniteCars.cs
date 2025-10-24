using System.Collections.Generic;
using UnityEngine;

public class CarSpawnerPool : MonoBehaviour
{
    public static CarSpawnerPool Instance { get; private set; }

    [Header("References")]
    public Transform player;
    public GameObject[] carPrefabs;
    public GameObject drunkDriverPrefab;

    [Header("Pool Settings")]
    public int poolSize = 10;

    [Header("Spawn Settings")]
    public float spawnDistanceAhead = 120f;
    public float respawnDistance = 250f;
    public float[] lanePositions = { -27f, -21f, 0.96f, 6f };
    public float laneY = 0.5f;

    [Header("Car Movement")]
    public float carSpeed = 25f;

    [Header("Drunk Driver Settings")]
    public float drunkSpawnMinDistance = 150f;
    public float drunkSpawnMaxDistance = 200f;
    public float drunkYOffset = 0.5f;
    public float drunkSpeed = 60f;
    public float drunkAggroRange = 80f;
    public float drunkSteerStrength = 8f;
    public int minDrunkCarsPerWave = 1;
    public int maxDrunkCarsPerWave = 3;

    private readonly Queue<GameObject> carPool = new Queue<GameObject>();
    private readonly List<GameObject> activeCars = new List<GameObject>();

    private float nextSpawnZ;
    private float nextDrunkSpawnZ;
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

        if (player)
            nextDrunkSpawnZ = player.position.z + Random.Range(drunkSpawnMinDistance, drunkSpawnMaxDistance);
    }

    // --------------------------------------------------
    // 🔹 Initialize Object Pool
    void InitializePool()
    {
        foreach (Transform child in transform)
            Destroy(child.gameObject);

        carPool.Clear();
        activeCars.Clear();

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

    // --------------------------------------------------
    // 🔹 Main Update Loop
    private void Update()
    {
        if (!initialized || player == null)
            return;

        MoveAndRecycleCars();

        while (nextSpawnZ < player.position.z + respawnDistance)
        {
            SpawnCar();
            nextSpawnZ += Random.Range(35f, 55f);
        }

        // Spawn Drunk Driver Wave
        if (player.position.z >= nextDrunkSpawnZ)
        {
            SpawnDrunkDriverWave();
            nextDrunkSpawnZ = player.position.z + Random.Range(drunkSpawnMinDistance, drunkSpawnMaxDistance);
        }
    }

    // --------------------------------------------------
    // 🔹 Car Movement + Cleanup
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

            if (car.GetComponent<DrunkDriverAI>() == null)
                car.transform.Translate(Vector3.back * carSpeed * Time.deltaTime, Space.World);

            if (car.transform.position.z < player.position.z - 25f)
            {
                ReturnCarToPool(car);
                activeCars.RemoveAt(i);
            }
        }
    }

    // --------------------------------------------------
    // 🔹 Regular Car Spawner
    void SpawnCar()
    {
        GameObject car = GetCarFromPool();
        if (car == null) return;

        float laneX = lanePositions[Random.Range(0, lanePositions.Length)];
        Vector3 spawnPos = new Vector3(laneX, laneY, nextSpawnZ + Random.Range(-5f, 5f));

        car.transform.SetPositionAndRotation(spawnPos, Quaternion.Euler(0f, 180f, 0f));
        car.SetActive(true);
        activeCars.Add(car);
    }

    // --------------------------------------------------
    // 🔹 Drunk Driver Wave
    void SpawnDrunkDriverWave()
    {
        if (drunkDriverPrefab == null || player == null) return;

        int drunkCount = Random.Range(minDrunkCarsPerWave, maxDrunkCarsPerWave + 1);

        for (int i = 0; i < drunkCount; i++)
        {
            float laneX = lanePositions[Random.Range(0, lanePositions.Length)];
            float spawnZ = player.position.z + Random.Range(180f, 220f);
            float spawnY = laneY + drunkYOffset;

            Vector3 spawnPos = new Vector3(laneX, spawnY, spawnZ);
            GameObject drunk = Instantiate(drunkDriverPrefab, spawnPos, Quaternion.Euler(0f, 180f, 0f));

            DrunkDriverAI ai = drunk.GetComponent<DrunkDriverAI>();
            if (ai != null)
            {
                ai.player = player;
                ai.speed = drunkSpeed;
                ai.aggroRange = drunkAggroRange;
                ai.steerStrength = drunkSteerStrength;
                ai.yOffset = drunkYOffset;
            }

            activeCars.Add(drunk);
        }

        Debug.Log($"🚗💥 Drunk driver wave spawned ({drunkCount})");
    }

    // --------------------------------------------------
    // 🔹 Pool Management
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

        if (car.GetComponent<DrunkDriverAI>() != null)
        {
            Destroy(car, 2f);
            return;
        }

        car.SetActive(false);
        car.transform.SetParent(transform, false);

        if (!carPool.Contains(car))
            carPool.Enqueue(car);
    }

    // --------------------------------------------------
    // 🔹 API for Map Manager
    public void SpawnCarOnMap(float mapStartZ, float mapEndZ)
    {
        if (drunkDriverPrefab && Random.value < 0.08f)
        {
            float laneX = lanePositions[Random.Range(0, lanePositions.Length)];
            float zPos = Random.Range(mapStartZ + 20f, mapEndZ - 20f);
            float spawnY = laneY + drunkYOffset;

            GameObject drunk = Instantiate(drunkDriverPrefab, new Vector3(laneX, spawnY, zPos), Quaternion.Euler(0f, 180f, 0f));
            DrunkDriverAI ai = drunk.GetComponent<DrunkDriverAI>();
            if (ai != null)
            {
                ai.player = player;
                ai.speed = drunkSpeed;
                ai.aggroRange = drunkAggroRange;
                ai.steerStrength = drunkSteerStrength;
                ai.yOffset = drunkYOffset;
            }
            activeCars.Add(drunk);
            return;
        }

        GameObject car = GetCarFromPool();
        if (car == null) return;

        float lane = lanePositions[Random.Range(0, lanePositions.Length)];
        float z = Random.Range(mapStartZ + 20f, mapEndZ - 20f);
        car.transform.SetPositionAndRotation(new Vector3(lane, laneY, z), Quaternion.Euler(0f, 180f, 0f));
        car.SetActive(true);
        activeCars.Add(car);
    }
}
