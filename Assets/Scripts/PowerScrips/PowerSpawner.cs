using System.Collections.Generic;
using UnityEngine;

public class PowerSpawner : MonoBehaviour
{
    [Header("Power Settings")]
    public GameObject powerPrefab;
    [Tooltip("Optional: a dedicated sword power prefab. If set, sword prefabs may spawn on maps according to swordSpawnChance.")]
    public GameObject swordPrefab;
    [Range(0f,1f)] public float swordSpawnChance = 0.15f;

    [Header("Debuff Settings")]
    public GameObject debuffPrefab;
    [Range(0f,1f)] public float debuffSpawnChance = 0.08f; // chance a spawn is a debuff

    public Transform powerParent;
    public float powerY = 1.5f;
    public float[] lanePositions = { -27f, -21f, -12.37f, -7.17f, 0.96f, 6f };
    public float powerFallDestroyY = -5f;
    public int powerPoolSize = 5;

    private readonly Queue<GameObject> powerPool = new();

    public void InitializePowerPool()
    {
        if (powerPrefab == null) return;
        for (int i = powerPool.Count; i < powerPoolSize; i++)
        {
            GameObject p = Instantiate(powerPrefab);
            p.transform.position = new Vector3(0f, -1000f, 0f);
            p.name = $"{powerPrefab.name}(Pooled)";
            p.SetActive(false);
            powerPool.Enqueue(p);
        }
    }

    // Called by MapManager to spawn powers on the map (2-3 per map)
    public void SpawnPowersOnMap(GameObject map, float mapStartZ, float mapEndZ)
    {
        if (map == null) return;

        // If none of the spawnable prefabs are assigned, nothing to do
        if (powerPrefab == null && swordPrefab == null && debuffPrefab == null)
        {
            Debug.LogWarning("PowerSpawner: No power/sword/debuff prefabs assigned - skipping spawn.");
            return;
        }

        Debug.Log($"PowerSpawner: SpawnPowersOnMap called for map={map.name} zRange={mapStartZ:F1}-{mapEndZ:F1} (powerPrefab={(powerPrefab!=null)}, swordPrefab={(swordPrefab!=null)}, debuffPrefab={(debuffPrefab!=null)}), swordChance={swordSpawnChance}, debuffChance={debuffSpawnChance}");

        int count = Random.Range(2, 4);
        for (int i = 0; i < count; i++)
        {
            // pick a spawn position and attempt to avoid overlapping other powers on this map
            Vector3 spawnPos;
            const int maxAttempts = 8;
            int attempts = 0;
            float minSpacing = 2.2f; // minimum distance between powers
            bool found = false;
            do
            {
                float laneX = lanePositions[Random.Range(0, lanePositions.Length)];
                float spawnZ = Random.Range(mapStartZ + 5f, mapEndZ - 5f);
                spawnPos = new Vector3(laneX, powerY, spawnZ);

                // Check existing powers already parented to the map to avoid overlap
                found = true; // assume OK
                var existing = map.GetComponentsInChildren<PowerCleanup>(true);
                foreach (var e in existing)
                {
                    if (e == null || e.gameObject == null) continue;
                    if (Vector3.Distance(e.gameObject.transform.position, spawnPos) < minSpacing)
                    {
                        found = false; // too close, try again
                        break;
                    }
                }
                attempts++;
            } while (!found && attempts < maxAttempts);

            if (!found)
            {
                // couldn't find a clean spot after some attempts; adjust Z slightly to reduce stacking
                spawnPos.z += attempts * 1.5f;
            }

            // Decide whether this spawn is a sword power, a debuff, or the regular power
            bool isSword = false;
            bool isDebuff = false;
            GameObject power = null;

            // If the configured spawn chances can overlap, log a helpful message to clarify selection order.
            if (swordSpawnChance + debuffSpawnChance > 1f)
            {
                Debug.LogWarning($"PowerSpawner: swordSpawnChance + debuffSpawnChance > 1.0 (={swordSpawnChance + debuffSpawnChance:F2}). Debuff is checked first and may take precedence.");
            }

            float r = Random.value;
            Debug.Log($"PowerSpawner: roll={r:F3} (swordChance={swordSpawnChance:F2}, debuffChance={debuffSpawnChance:F2})");

            // Check debuff first so that debuffs get a chance even when swordChance is high.
            if (debuffPrefab != null && r < debuffSpawnChance)
            {
                power = Instantiate(debuffPrefab);
                isDebuff = true;
                Debug.Log($"PowerSpawner: chose DEBUFF at {spawnPos}");
            }
            else if (swordPrefab != null && r < debuffSpawnChance + swordSpawnChance)
            {
                power = Instantiate(swordPrefab);
                isSword = true;
                Debug.Log($"PowerSpawner: chose SWORD at {spawnPos}");
            }
            else
            {
                if (powerPrefab == null && powerPool.Count == 0)
                {
                    Debug.Log($"PowerSpawner: falling back but no regular power prefab/pool available — skipping this spawn slot");
                    continue; // skip this spawn attempt if no regular power available
                }
                power = powerPool.Count > 0 ? powerPool.Dequeue() : Instantiate(powerPrefab);
            }
            if (power == null) continue;

            power.SetActive(true);
            power.transform.SetParent(map.transform, true);
            power.transform.position = spawnPos;
            power.transform.rotation = Quaternion.identity;

            string spawnType = isSword ? "Sword" : (isDebuff ? "Debuff" : "Power");
            Debug.Log($"PowerSpawner: Spawned {spawnType} '{power.name}' at {spawnPos} (map={map.name})");

            PowerCleanup cleaner = power.GetComponent<PowerCleanup>();
            if (cleaner == null) cleaner = power.AddComponent<PowerCleanup>();
            cleaner.fallDestroyY = powerFallDestroyY;
            cleaner.SetPoolReference(this);
        }
    }

    public void ReturnPowerToPool(GameObject power)
    {
        if (power == null) return;

        Rigidbody rb = power.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (powerParent != null && powerParent.gameObject.scene.IsValid())
            power.transform.SetParent(powerParent, false);
        else
        {
            power.transform.SetParent(null);
            power.transform.position = new Vector3(0f, -1000f, 0f);
        }

        power.SetActive(false);
        if (!powerPool.Contains(power))
            powerPool.Enqueue(power);
    }

    // Return all powers under the map back to the pool
    public void ReturnPowersOnMap(GameObject map)
    {
        if (map == null) return;
        PowerCleanup[] cleans = map.GetComponentsInChildren<PowerCleanup>(true);
        foreach (var c in cleans)
        {
            if (c == null || c.gameObject == null) continue;
            ReturnPowerToPool(c.gameObject);
        }
    }

    // nested cleanup component for power instances
    public class PowerCleanup : MonoBehaviour
    {
        public float fallDestroyY = -5f;
        // private TimedMapSpawners.PowerCleanup _oldTypeRef; // unused, compatibility note
        private PowerSpawner poolManager;
        private Transform player;
        private float destroyBehindDistance = 30f;

        public void SetPoolReference(PowerSpawner spawner) => poolManager = spawner;

        public void SetupAutoDestroy(Transform playerTransform) => player = playerTransform;

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

        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                // If this object has already been deactivated by the pickup logic, don't try to start a coroutine on it.
                if (!gameObject.activeInHierarchy) return;
                // Delay returning to pool by one frame so any pickup scripts (e.g., SprintPower, SwordPower)
                // that run OnTriggerEnter on the same GameObject have a chance to execute first.
                StartCoroutine(DelayedReturnOneFrame());
            }
        }

        private System.Collections.IEnumerator DelayedReturnOneFrame()
        {
            yield return null; // wait one frame
            // If the object has already been returned/handled by the pickup script it will likely be inactive; skip returning in that case.
            if (!gameObject.activeInHierarchy) yield break;
            poolManager?.ReturnPowerToPool(gameObject);
        }
    }
}