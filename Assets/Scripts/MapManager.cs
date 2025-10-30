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
    [Tooltip("Delay before map is destroyed after hitting mapEnd trigger")]
    public float defaultDestroyDelay = 2f;

    [Header("Power-Up Settings")]
    public int minPowerUpsPerMap = 0;
    public int maxPowerUpsPerMap = 2;
    public float[] lanePositions = { -6f, -2f, 2f, 6f };
    public float powerY = 1.5f;

    [Header("Meteorite Settings")]
    public float meteoriteInterval = 4f;
    public float meteoriteSpawnHeight = 60f;
    public float meteoriteSpeed = 200f;
    public float meteoriteDestroyDelay = 1f;
    public int minMeteorsPerWave = 2;
    public int maxMeteorsPerWave = 6;
    public float meteoriteSpread = 10f;

    private SkyDarkener_Builtin skyDarkener;
    private float timer;
    private float currentZ;
    private readonly List<GameObject> activeMaps = new();

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

        StartCoroutine(MeteoriteRoutine());
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
    }

    // --------------------------------------------------------------------
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
        }

        Debug.Log($"☄️ Meteorite wave launched ({meteorCount} meteors)");
    }

    // --------------------------------------------------------------------
    // 🌍 Map + Powerup Spawning
    private void SpawnNextMap()
    {
        if (mapPrefab == null) return;

        Vector3 spawnPos = new Vector3(0f, 0f, currentZ);
        GameObject newMap = Instantiate(mapPrefab, spawnPos, Quaternion.identity);
        currentZ += mapLength - overlapFix;
        activeMaps.Add(newMap);

        // Link mapEnd trigger (if exists)
        MapEndTrigger endTrigger = newMap.GetComponentInChildren<MapEndTrigger>();
        if (endTrigger != null)
        {
            endTrigger.spawner = this;
        }

        // Spawn cars + drunk car + powerups
        if (carSpawner != null)
        {
            float mapStartZ = spawnPos.z;
            float mapEndZ = mapStartZ + mapLength;
            int carCount = Random.Range(1, 4);
            for (int i = 0; i < carCount; i++)
                carSpawner.SpawnCarOnMap(mapStartZ, mapEndZ);

            FindAnyObjectByType<DrunkCarSpawner>()?.OnNewMapSpawned(mapStartZ, mapEndZ);
        }

        SpawnPowerUps(newMap.transform.position.z, mapLength);
    }

    private void SpawnPowerUps(float mapStartZ, float mapLength)
    {
        if (powerPrefab == null) return;

        int powerCount = Random.Range(minPowerUpsPerMap, maxPowerUpsPerMap + 1);
        for (int i = 0; i < powerCount; i++)
        {
            float laneX = lanePositions[Random.Range(0, lanePositions.Length)];
            float zPos = Random.Range(mapStartZ + 10f, mapStartZ + mapLength - 10f);
            Vector3 spawnPos = new(laneX, powerY, zPos);

            GameObject power = Instantiate(powerPrefab, spawnPos, Quaternion.identity);
            if (powerParent != null && powerParent.gameObject.scene.IsValid())
                power.transform.SetParent(powerParent, true);

            PowerCleanup cleaner = power.AddComponent<PowerCleanup>();
            cleaner.player = player;
            cleaner.lifetime = 20f;
        }
    }

    // --------------------------------------------------------------------
    // 🧹 Map destruction system (triggered by MapEndTrigger)
    public void RequestDestroyMap(GameObject map, float delay = -1f)
    {
        if (map == null) return;
        float d = delay > 0f ? delay : defaultDestroyDelay;
        StartCoroutine(DestroyMapAfterDelay(map, d));
    }

    private IEnumerator DestroyMapAfterDelay(GameObject map, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (activeMaps.Contains(map))
            activeMaps.Remove(map);

        if (map != null)
            Destroy(map);
    }

    // --------------------------------------------------------------------
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

    // --------------------------------------------------------------------
    // ⚡ Meteorite Destroyer
    public class MeteoriteDestroyer : MonoBehaviour
    {
        public float DestroyDelay = 1f;
        public float shakeIntensity = 1.2f;
        public float shakeDuration = 0.5f;

        private Light fireLight;
        private ParticleSystem staticFire;
        private ParticleSystem movingFire;

        private void Start()
        {
            staticFire = GetParticleByName("Fire_Static");
            movingFire = GetParticleByName("Fire_Moving");
            fireLight = GetComponentInChildren<Light>();

            if (staticFire) staticFire.Play();
            if (movingFire) movingFire.Play();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!collision.gameObject.CompareTag("Player") &&
                !collision.gameObject.CompareTag("RedZone") &&
                !collision.gameObject.CompareTag("House"))
                return;

            if (fireLight) fireLight.enabled = false;
            if (movingFire) movingFire.Stop();
            if (staticFire) staticFire.Stop();

            if (GameMode.IsCinematic)
            {
                ExplosionManager.Instance?.SpawnMeteorExplosion(transform.position);
                Destroy(gameObject, DestroyDelay);
                return;
            }

            PlayerAnimation playerAnim = collision.gameObject.GetComponent<PlayerAnimation>();
            if (collision.gameObject.CompareTag("Player") && playerAnim != null && playerAnim.IsSprinting())
            {
                ExplosionManager.Instance?.SpawnPlasmaExplosion(transform.position);
                TimeManager.Instance?.TriggerSlowMotion(0.5f);
            }
            else
            {
                ExplosionManager.Instance?.SpawnMeteorExplosion(transform.position);
                TimeManager.Instance?.TriggerSlowMotion(1.5f);
                if (collision.gameObject.CompareTag("Player") && playerAnim != null)
                    playerAnim.TriggerDeath();
            }

            CameraShake.Instance?.ShakeCamera(shakeIntensity, shakeDuration);
            Destroy(gameObject, DestroyDelay);

            SkyDarkener_Builtin sky = FindAnyObjectByType<SkyDarkener_Builtin>();
            sky?.UnregisterMeteor();
        }

        private ParticleSystem GetParticleByName(string name)
        {
            Transform t = transform.Find(name);
            return t ? t.GetComponent<ParticleSystem>() : null;
        }
    }

    // --------------------------------------------------------------------
    // ⚡ Power Cleanup
    public class PowerCleanup : MonoBehaviour
    {
        public Transform player;
        public float lifetime = 30f;
        private float timer;

        private void Update()
        {
            timer += Time.deltaTime;
            if (timer >= lifetime)
                Destroy(gameObject);
        }
    }
}
