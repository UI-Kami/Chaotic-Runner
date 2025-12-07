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
    public float[] lanePositions = { -27f, -21f, -12.37f, -7.17f, 0.96f, 6f };
    public float powerSpawnDistanceAhead = 120f; // not used now for per-map spawning
    public float powerRespawnDistance = 250f;    // not used now for per-map spawning
    public float powerFallDestroyY = -5f;
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
    public float meteorShakeIntensity = 0.6f;
    public float meteorShakeDuration = 0.45f;

    private SkyDarkener_Builtin skyDarkener;
    private float timer;
    private float currentZ;
    private readonly List<GameObject> activeMaps = new();
    private readonly Queue<GameObject> powerPool = new();

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

        StartCoroutine(MeteoriteRoutine());
    }

    private void InitializePowerPool()
    {
        if (powerPrefab == null) return;

        for (int i = 0; i < powerPoolSize; i++)
        {
            // Instantiate pooled items offscreen (so they don't visually stack at origin)
            GameObject p = Instantiate(powerPrefab);
            p.transform.position = new Vector3(0f, -1000f, 0f);
            p.name = $"{powerPrefab.name}(Pooled)";
            p.SetActive(false);
            powerPool.Enqueue(p);
        }
    }

    private void Update()
    {
        // Map spawning
        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            timer = 0f;
            SpawnNextMap();
        }

        // Player velocity tracking (kept for other systems)
        if (player != null)
        {
            float dt = Mathf.Max(Time.deltaTime, 0.0001f);
            playerVelocity = (player.position - prevPlayerPos) / dt;
            prevPlayerPos = player.position;
        }

        // No per-frame chance-based power spawn anymore (powers spawn per map)
    }

    // Spawn N powers when a new map is created, N = 2..3
    private void SpawnPowersOnMap(GameObject map)
    {
        if (powerPrefab == null || map == null) return;

        float mapStartZ = map.transform.position.z;
        float mapEndZ = mapStartZ + mapLength;

        int count = Random.Range(2, 4); // 2 or 3 powers per map
        for (int i = 0; i < count; i++)
        {
            // choose random lane from the lanePositions array (treated as absolute X positions)
            float laneX = lanePositions[Random.Range(0, lanePositions.Length)];
            // choose random Z within the map extents (with a small margin)
            float spawnZ = Random.Range(mapStartZ + 5f, mapEndZ - 5f);
            Vector3 spawnPos = new Vector3(laneX, powerY, spawnZ);

            GameObject power = null;
            if (powerPool.Count > 0)
            {
                power = powerPool.Dequeue();
            }
            else
            {
                // fallback: create a new instance if pool is empty
                power = Instantiate(powerPrefab);
            }

            if (power == null) continue;

            power.SetActive(true);
            power.transform.SetParent(map.transform, true); // parent to map so it will be found when map is destroyed
            power.transform.position = spawnPos;
            power.transform.rotation = Quaternion.identity;

            PowerCleanup cleaner = power.GetComponent<PowerCleanup>();
            if (cleaner == null)
                cleaner = power.AddComponent<PowerCleanup>();

            cleaner.fallDestroyY = powerFallDestroyY;
            // If player exists, give the cleaner a reference for behind-player recycling
            cleaner.SetupAutoDestroy(player);
            cleaner.SetPoolReference(this);
        }
    }

    // Return pooled power to pool (safe unparent + move offscreen)
    public void ReturnPowerToPool(GameObject power)
    {
        if (power == null) return;

        // Reset physics if present
        Rigidbody rb = power.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Unparent and move offscreen (or parent under powerParent if it's a valid scene object)
        if (powerParent != null && powerParent.gameObject.scene.IsValid())
        {
            power.transform.SetParent(powerParent, false);
        }
        else
        {
            power.transform.SetParent(null);
            power.transform.position = new Vector3(0f, -1000f, 0f);
        }

        power.SetActive(false);

        if (!powerPool.Contains(power))
            powerPool.Enqueue(power);
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
                // use velocity — 'linearVelocity' is not a standard public Rigidbody property
                rb.velocity = direction * meteoriteSpeed;
                rb.useGravity = true;
                rb.AddTorque(Random.insideUnitSphere * 20f, ForceMode.VelocityChange);
            }

            MeteoriteDestroyer destroyer = meteor.AddComponent<MeteoriteDestroyer>();
            destroyer.DestroyDelay = meteoriteDestroyDelay;
            destroyer.fallDestroyY = meteoriteFallDestroyY;
            destroyer.shakeIntensity = meteorShakeIntensity;
            destroyer.shakeDuration = meteorShakeDuration;
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

        // spawn 2-3 powers on this map at random lanes
        SpawnPowersOnMap(newMap);
    }

    // 🧹 Map destruction
    public void RequestDestroyMap(GameObject map, float delay = -1f)
    {
        if (map == null) return;
        float d = delay > 0f ? delay : defaultDestroyDelay;

        // Return any pooled powers that are children of this map before destroying it.
        PowerCleanup[] cleans = map.GetComponentsInChildren<PowerCleanup>(true);
        foreach (var c in cleans)
        {
            if (c == null || c.gameObject == null) continue;
            ReturnPowerToPool(c.gameObject);
        }

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

        // shake values (set from spawner)
        public float shakeIntensity = 0.6f;
        public float shakeDuration = 0.45f;

        private bool hasImpacted = false;

        void Update()
        {
            if (transform.position.y < fallDestroyY)
                Destroy(gameObject);
        }

        private void HandleImpact()
        {
            if (hasImpacted) return;
            hasImpacted = true;

            Vector3 impactPos = transform.position;

            // spawn explosion (particle prefab + plays sound via ExplosionManager)
            if (ExplosionManager.Instance != null)
                ExplosionManager.Instance.SpawnMeteorExplosion(impactPos);
            else
                Debug.LogWarning("[MeteoriteDestroyer] ExplosionManager.Instance is null; no explosion/spawned sound.");

            // shake camera (only if manager exists)
            if (CameraShake.Instance != null)
                CameraShake.Instance.ShakeCamera(shakeIntensity, shakeDuration);
            else
                Debug.LogWarning("[MeteoriteDestroyer] CameraShake.Instance is null; cannot shake camera.");

            // destroy meteor after delay so explosion can play
            Destroy(gameObject, DestroyDelay);
        }

        void OnCollisionEnter(Collision other)
        {
            if (other.collider.CompareTag("RedZone") || other.collider.CompareTag("House"))
            {
                HandleImpact();
            }
            if (other.collider.CompareTag("Player"))
            {
                HandleImpact();
                FindAnyObjectByType<PlayerAnimation>()?.TriggerDeath();
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("RedZone") || other.CompareTag("House"))
            {
                HandleImpact();
            }
            if (other.CompareTag("Player"))
            {
                HandleImpact();
                FindAnyObjectByType<PlayerAnimation>()?.TriggerDeath();
            }
        }
    }

    // ⚡ Power Cleanup (returns to pool)
    public class PowerCleanup : MonoBehaviour
    {
        private SprintPower sprintPower;
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

        // call this when the object is picked up to return it to the pool
        public void HandlePickup()
        {
            poolManager?.ReturnPowerToPool(gameObject);
        }

        void Update()
        {
            if (transform.position.y < fallDestroyY ||
                (player != null && transform.position.z < player.position.z - destroyBehindDistance))
            {
                poolManager?.ReturnPowerToPool(gameObject);
            }
        }
    }
}
    