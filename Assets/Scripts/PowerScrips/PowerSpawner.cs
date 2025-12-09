using System.Collections.Generic;
using UnityEngine;

public class PowerSpawner : MonoBehaviour
{
    [Header("Power Settings")]
    public GameObject powerPrefab;
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
        if (powerPrefab == null || map == null) return;

        int count = Random.Range(2, 4);
        for (int i = 0; i < count; i++)
        {
            float laneX = lanePositions[Random.Range(0, lanePositions.Length)];
            float spawnZ = Random.Range(mapStartZ + 5f, mapEndZ - 5f);
            Vector3 spawnPos = new Vector3(laneX, powerY, spawnZ);

            GameObject power = powerPool.Count > 0 ? powerPool.Dequeue() : Instantiate(powerPrefab);
            if (power == null) continue;

            power.SetActive(true);
            power.transform.SetParent(map.transform, true);
            power.transform.position = spawnPos;
            power.transform.rotation = Quaternion.identity;

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
            rb.velocity = Vector3.zero;
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
                // Play pickup logic is supposed to be in the SprintPower script; ensure SprintPower calls HandlePickup.
                poolManager?.ReturnPowerToPool(gameObject);
            }
        }
    }
}