using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimedMapSpawner : MonoBehaviour
{
    [Header("References")]
    public GameObject mapPrefab;
    public Transform spawnOrigin;
    public CarSpawnerPool carSpawner;
    public GameObject powerPrefab;
    public Transform powerParent;
    public GameObject meteoritePrefab;
    public Transform player;

    [Header("Map Settings")]
    public float mapLength = 200f;
    public float overlapFix = 0.5f;
    public float spawnInterval = 2f;
    public bool autoDetectLength = true;
    public bool startImmediately = true;

    [Header("Map Destruction")]
    public float defaultDestroyDelay = 2f;

    [Header("Power-Up Settings")]
    public float powerY = 1.5f;
    public float[] lanePositions = { -6f, -2f, 2f, 6f };
    public float powerSpawnDistanceAhead = 120f;
    public float powerRespawnDistance = 250f;
    public float powerFallDestroyY = -5f;
    public float powerSpawnChance = 0.4f;
    public int powerPoolSize = 5;

    [Header("Meteorite Settings")]
    public float meteoriteInterval = 4f;
    public float meteoriteSpawnHeight = 60f;
    public float meteoriteSpeed = 200f;
    public float meteoriteDestroyDelay = 1f;
    public float meteoriteFallDestroyY = -10f;
    public int minMeteorsPerWave = 2;
    public int maxMeteorsPerWave = 6;
    public float meteoriteSpread = 10f;

    private SkyDarkener_Builtin skyDarkener;
    private float timer;
    private float currentZ;
    private float nextPowerSpawnZ;

    private readonly List<GameObject> activeMaps = new();
    private readonly Queue<GameObject> powerPool = new();
    private GameObject activePower = null;

    private Vector3 prevPlayerPos;
    private Vector3 playerVelocity;

    private void Start()
    {
        skyDarkener = FindAnyObjectByType<SkyDarkener_Builtin>();

        if (mapPrefab == null)
            Debug.LogError("[TimedMapSpawner] mapPrefab is not assigned!");

        if (autoDetectLength && mapPrefab != null)
            mapLength = GetMapLength(mapPrefab);

        currentZ = spawnOrigin ? spawnOrigin.position.z : 0f;

        if (carSpawner == null)
            carSpawner = FindAnyObjectByType<CarSpawnerPool>();

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }

        if (player != null)
            prevPlayerPos = player.position;

        if (startImmediately)
        {
            for (int i = 0; i < 8; i++)
                SpawnNextMap();
        }

        InitializePowerPool();

        if (player)
            nextPowerSpawnZ = player.position.z + powerSpawnDistanceAhead;

        StartCoroutine(MeteoriteRoutine());
    }

    private void InitializePowerPool()
    {
        if (powerPrefab == null) return;

        for (int i = 0; i < powerPoolSize; i++)
        {
            GameObject p = Instantiate(powerPrefab, powerParent);
            p.SetActive(false);
            powerPool.Enqueue(p);
        }
    }

    private void Update()
    {
        if (player == null) return;

        // Map spawning
        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            timer = 0f;
            SpawnNextMap();
        }

        // Player velocity tracking
        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
        playerVelocity = (player.position - prevPlayerPos) / dt;
        prevPlayerPos = player.position;

        HandlePowerSpawning();
    }

    // ⚡ Power Spawning Logic (like cars)
    private void HandlePowerSpawning()
    {
        // if active power still exists ahead, skip
        if (activePower != null && activePower.activeSelf)
        {
            // if player has passed the power, recycle it
            if (activePower.transform.position.z < player.position.z - 30f)
                ReturnPowerToPool(activePower);

            return;
        }

        // spawn a new one ahead when player approaches threshold
        if (player.position.z + powerRespawnDistance > nextPowerSpawnZ)
        {
            TrySpawnPower();
            nextPowerSpawnZ = player.position.z + Random.Range(200f, 300f);
        }
    }

    private void TrySpawnPower()
    {
        if (powerPrefab == null || player == null || powerPool.Count == 0)
            return;

        if (Random.value > powerSpawnChance) return;

        GameObject power = powerPool.Dequeue();
        power.SetActive(true);

        float laneX = lanePositions[Random.Range(0, lanePositions.Length)];
        float spawnZ = player.position.z + Random.Range(80f, 150f);
        Vector3 spawnPos = new Vector3(laneX, powerY, spawnZ);

        power.transform.position = spawnPos;
        power.transform.rotation = Quaternion.identity;

        PowerCleanup cleaner = power.GetComponent<PowerCleanup>();
        if (cleaner == null)
            cleaner = power.AddComponent<PowerCleanup>();

        cleaner.fallDestroyY = powerFallDestroyY;
        cleaner.SetupAutoDestroy(player);
        cleaner.SetPoolReference(this);

        activePower = power;
    }

    public void ReturnPowerToPool(GameObject power)
    {
        if (power == null) return;
        power.SetActive(false);
        if (!powerPool.Contains(power))
            powerPool.Enqueue(power);
        if (activePower == power)
            activePower = null;
    }

    // 🌠 Meteorite routine
    private IEnumerator MeteoriteRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(meteoriteInterval + Random.Range(-1f, 2f));
            if (player == null || meteoritePrefab == null) continue;

            skyDarkener?.DarkenSky();
            LaunchMeteoriteWave();
        }
    }

    private void LaunchMeteoriteWave()
    {
        if (meteoritePrefab == null || player == null || activeMaps.Count == 0) return;

        GameObject lastMap = activeMaps[activeMaps.Count - 1];
        float spawnZ = lastMap.transform.position.z + (mapLength * 0.5f);

        int meteorCount = Random.Range(minMeteorsPerWave, maxMeteorsPerWave + 1);

        for (int i = 0; i < meteorCount; i++)
        {
            float xOffset = Random.Range(-meteoriteSpread, meteoriteSpread);
            Vector3 spawnPos = new Vector3(
                player.position.x + xOffset,
                player.position.y + meteoriteSpawnHeight,
                spawnZ + Random.Range(-15f, 20f)
            );

            GameObject meteor = Instantiate(meteoritePrefab, spawnPos, Quaternion.identity);
            Rigidbody rb = meteor.GetComponent<Rigidbody>();

            if (skyDarkener != null)
                skyDarkener.RegisterMeteor();

            if (rb != null)
            {
                Vector3 target = player.position + new Vector3(Random.Range(-5f, 5f), -15f, Random.Range(10f, 25f));
                Vector3 direction = (target - spawnPos).normalized;
                rb.linearVelocity = direction * meteoriteSpeed;
                rb.useGravity = true;
                rb.AddTorque(Random.insideUnitSphere * 20f, ForceMode.VelocityChange);
            }

            MeteoriteDestroyer destroyer = meteor.AddComponent<MeteoriteDestroyer>();
            destroyer.DestroyDelay = meteoriteDestroyDelay;
            destroyer.fallDestroyY = meteoriteFallDestroyY;
        }

        Debug.Log($"☄️ Meteorite wave launched ({meteorCount} meteors)");
    }

    // 🌍 Map spawning
    private void SpawnNextMap()
    {
        if (mapPrefab == null) return;

        Vector3 spawnPos = new Vector3(0f, 0f, currentZ);
        GameObject newMap = Instantiate(mapPrefab, spawnPos, Quaternion.identity);
        currentZ += mapLength - overlapFix;
        activeMaps.Add(newMap);

        MapEndTrigger endTrigger = newMap.GetComponentInChildren<MapEndTrigger>();
        if (endTrigger != null)
        {
            endTrigger.spawner = this;
            endTrigger.destroyDelay = defaultDestroyDelay;
        }

        if (carSpawner != null)
        {
            float mapStartZ = spawnPos.z;
            float mapEndZ = mapStartZ + mapLength;
            int carCount = Random.Range(1, 4);
            for (int i = 0; i < carCount; i++)
                carSpawner.SpawnCarOnMap(mapStartZ, mapEndZ);

            FindAnyObjectByType<DrunkCarSpawner>()?.OnNewMapSpawned(mapStartZ, mapEndZ);
        }
    }

    // 🧹 Map destruction
    public void RequestDestroyMap(GameObject map, float delay = -1f)
    {
        if (map == null) return;
        float d = delay > 0f ? delay : defaultDestroyDelay;
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

    // Utility
    private float GetMapLength(GameObject prefab)
    {
        Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return mapLength;
        Bounds bounds = renderers[0].bounds;
        foreach (Renderer r in renderers)
            bounds.Encapsulate(r.bounds);
        return bounds.size.z;
    }

    // ⚡ Meteorite Destroyer
    public class MeteoriteDestroyer : MonoBehaviour
    {
        public float DestroyDelay = 1f;
        public float fallDestroyY = -10f;

        void Update()
        {
            if (transform.position.y < fallDestroyY)
                Destroy(gameObject);
        }
    }

    // ⚡ Power Cleanup (returns to pool)
    public class PowerCleanup : MonoBehaviour
    {
        public float fallDestroyY = -5f;
        private Transform player;
        private float destroyBehindDistance = 30f;
        private TimedMapSpawner poolManager;

        public void SetupAutoDestroy(Transform playerTransform)
        {
            player = playerTransform;
        }

        public void SetPoolReference(TimedMapSpawner spawner)
        {
            poolManager = spawner;
        }

        void Update()
        {
            if (transform.position.y < fallDestroyY ||
                (player != null && transform.position.z < player.position.z - destroyBehindDistance))
            {
                poolManager?.ReturnPowerToPool(gameObject);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                poolManager?.ReturnPowerToPool(gameObject);
            }
        }
    }
}
