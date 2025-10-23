using System.Collections.Generic;
using UnityEngine;

public class CarSpawnerPool : MonoBehaviour
{
    public static CarSpawnerPool Instance { get; private set; }

    [Header("References")]
    public Transform player;
    public GameObject[] carPrefabs;
    public GameObject drunkDriverPrefab;
    public int poolSize = 10;

    [Header("Spawn Settings")]
    public float spawnDistanceAhead = 120f;
    public float respawnDistance = 250f;
    public float[] lanePositions = { -6f, -2f, 2f, 6f };
    public float laneY = 0.5f;

    [Header("Car Movement")]
    public float carSpeed = 25f;

    [Header("Drunk Driver Settings")]
    public float drunkSpawnMinDistance = 150f;
    public float drunkSpawnMaxDistance = 200f;
    public float drunkSpeed = 60f;
    public float drunkAggroRange = 60f;
    public float drunkSteerStrength = 6f;

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

        // schedule first drunk driver spawn ahead
        if (player)
            nextDrunkSpawnZ = player.position.z + Random.Range(drunkSpawnMinDistance, drunkSpawnMaxDistance);
    }

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

    private void Update()
    {
        if (!initialized || player == null)
            return;

        MoveAndRecycleCars();

        // Regular traffic spawning
        while (nextSpawnZ < player.position.z + respawnDistance)
        {
            SpawnCar();
            nextSpawnZ += Random.Range(35f, 55f);
        }

        // Drunk driver spawn logic
        if (player.position.z >= nextDrunkSpawnZ)
        {
            SpawnDrunkDriver();
            nextDrunkSpawnZ = player.position.z + Random.Range(drunkSpawnMinDistance, drunkSpawnMaxDistance);
        }
    }

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

            // skip drunk drivers (AI moves them)
            if (car.GetComponent<DrunkDriverAI>() == null)
                car.transform.Translate(Vector3.back * carSpeed * Time.deltaTime, Space.World);

            // Despawn behind player
            if (car.transform.position.z < player.position.z - 25f)
            {
                ReturnCarToPool(car);
                activeCars.RemoveAt(i);
            }
        }
    }

    // 🔸 Normal car spawner
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

    // 🔸 Called by map manager for per-section spawning
    public void SpawnCarOnMap(float mapStartZ, float mapEndZ)
    {
        // Occasionally spawn a drunk driver in a map section
        if (drunkDriverPrefab && Random.value < 0.08f) // 8% chance
        {
            SpawnDrunkDriver(mapStartZ, mapEndZ);
            return;
        }

        GameObject car = GetCarFromPool();
        if (car == null) return;

        float laneX = lanePositions[Random.Range(0, lanePositions.Length)];
        float zPos = Random.Range(mapStartZ + 20f, mapEndZ - 20f);
        Vector3 spawnPos = new Vector3(laneX, laneY, zPos);

        car.transform.SetPositionAndRotation(spawnPos, Quaternion.Euler(0f, 180f, 0f));
        car.SetActive(true);
        activeCars.Add(car);
    }

    // 🔥 Drunk driver logic
    void SpawnDrunkDriver()
    {
        if (drunkDriverPrefab == null || player == null) return;

        float laneX = lanePositions[Random.Range(0, lanePositions.Length)];
        float spawnZ = player.position.z + Random.Range(150f, 200f);
        Vector3 spawnPos = new Vector3(laneX, laneY, spawnZ);

        GameObject drunk = Instantiate(drunkDriverPrefab, spawnPos, Quaternion.Euler(0f, 180f, 0f));
        var ai = drunk.GetComponent<DrunkDriverAI>();
        if (ai != null)
        {
            ai.player = player;
            ai.speed = drunkSpeed;
            ai.aggroRange = drunkAggroRange;
            ai.steerStrength = drunkSteerStrength;
        }

        activeCars.Add(drunk);
        Debug.Log("🚗💥 Drunk driver spawned at Z: " + spawnZ);
    }

    void SpawnDrunkDriver(float mapStartZ, float mapEndZ)
    {
        if (drunkDriverPrefab == null || player == null) return;

        float laneX = lanePositions[Random.Range(0, lanePositions.Length)];
        float zPos = Random.Range(mapStartZ + 20f, mapEndZ - 20f);
        Vector3 spawnPos = new Vector3(laneX, laneY, zPos);

        GameObject drunk = Instantiate(drunkDriverPrefab, spawnPos, Quaternion.Euler(0f, 180f, 0f));
        var ai = drunk.GetComponent<DrunkDriverAI>();
        if (ai != null)
        {
            ai.player = player;
            ai.speed = drunkSpeed;
            ai.aggroRange = drunkAggroRange;
            ai.steerStrength = drunkSteerStrength;
        }

        activeCars.Add(drunk);
        Debug.Log("🚗💥 Drunk driver spawned inside map section.");
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

    public void SetPoolSize(int size)
    {
        poolSize = size;
    }
}
