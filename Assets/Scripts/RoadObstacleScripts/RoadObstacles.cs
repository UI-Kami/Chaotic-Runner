using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Obstacle pooling and per-map spawner.
/// Attach one instance (for example to the same GameObject that has TimedMapSpawner)
/// and assign the `obstaclePrefab`. Call `SpawnObstaclesForMap` from your map spawner
/// when each map is created. The manager will parent obstacles to the map so they
/// will be found and returned to the pool when the map is destroyed.
/// </summary>
public class RoadObstacles : MonoBehaviour
{
    [Header("Pool Settings")]
    public GameObject[] obstaclePrefab;
    public int poolSize = 20;

    [Header("Per-Lane Settings")]
    public int minPerLane = 1;
    public int maxPerLane = 3;
    public float obstacleY = 0f;

    // internal pool storage
    private readonly Queue<GameObject> pool = new();

    void Awake()
    {
        InitializePool();
    }

    // Initialize or refill the pool
    public void InitializePool()
    {
        // clear any existing references (if re-initializing)
        // (do not Destroy pooled objects here to avoid accidental runtime deletes)
        // We'll instantiate fresh pool objects if pool is empty.
        if (obstaclePrefab == null) return;

        // Only add objects until poolSize is reached
        int toCreate = poolSize - pool.Count;
        for (int i = 0; i < toCreate; i++)
        {
            GameObject o = Instantiate(obstaclePrefab[Random.Range(0, obstaclePrefab.Length)]);
            o.name = $"{obstaclePrefab[Random.Range(0, obstaclePrefab.Length)].name}(Pooled)";
            o.transform.position = new Vector3(0f, -1000f, 0f);
            o.SetActive(false);
            EnsureCleanup(o);
            pool.Enqueue(o);
        }
    }

    /// <summary>
    /// Spawn obstacles for the specified map. lanePositions is an array of X coordinates
    /// (absolute world X positions) where obstacles may be placed. mapStartZ/mapEndZ define
    /// the Z extents of the map.
    /// </summary>
    public void SpawnObstaclesForMap(GameObject map, float[] lanePositions, float mapStartZ, float mapEndZ)
    {
        if (obstaclePrefab == null || map == null || lanePositions == null || lanePositions.Length == 0)
            return;

        for (int laneIndex = 0; laneIndex < lanePositions.Length; laneIndex++)
        {
            int count = Random.Range(minPerLane, maxPerLane + 1);
            for (int i = 0; i < count; i++)
            {
                float spawnZ = Random.Range(mapStartZ + 1f, mapEndZ - 1f);
                Vector3 spawnPos = new Vector3(lanePositions[laneIndex], obstacleY, spawnZ);

                GameObject obs = GetFromPoolOrCreate();
                if (obs == null) continue;

                obs.SetActive(true);
                obs.transform.SetParent(map.transform, true); // parent to map
                obs.transform.position = spawnPos;
                obs.transform.rotation = Vector3.back == Vector3.zero ? Quaternion.identity : Quaternion.LookRotation(Vector3.back);

                var cleanup = EnsureCleanup(obs);
                cleanup.SetPool(this);
            }
        }
    }

    /// <summary>
    /// Return a particular obstacle to the pool (safe unparent + disable).
    /// </summary>
    public void ReturnObstacleToPool(GameObject obs)
    {
        if (obs == null) return;

        // reset rigidbody if any
        Rigidbody rb = obs.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = false;
        }

        // unparent and move off-screen
        obs.transform.SetParent(null);
        obs.transform.position = new Vector3(0f, -1000f, 0f);
        obs.SetActive(false);

        if (!pool.Contains(obs))
            pool.Enqueue(obs);
    }

    /// <summary>
    /// Return all obstacle children of a map to the pool. Call this before destroying a map.
    /// </summary>
    public void ReturnObstaclesOnMap(GameObject map)
    {
        if (map == null) return;
        var cleans = map.GetComponentsInChildren<ObstacleCleanup>(true);
        foreach (var c in cleans)
        {
            if (c == null || c.gameObject == null) continue;
            ReturnObstacleToPool(c.gameObject);
        }
    }

    // Grab object from pool or instantiate fallback
    private GameObject GetFromPoolOrCreate()
    {
        if (pool.Count > 0)
            return pool.Dequeue();

        if (obstaclePrefab == null) return null;
        GameObject o = Instantiate(obstaclePrefab[Random.Range(0, obstaclePrefab.Length)]);
        EnsureCleanup(o);
        return o;
    }

    // Ensure obstacle has the cleanup helper that returns it to the pool on hit
    private ObstacleCleanup EnsureCleanup(GameObject o)
    {
        var cleanup = o.GetComponent<ObstacleCleanup>();
        if (cleanup == null)
            cleanup = o.AddComponent<ObstacleCleanup>();
        cleanup.SetPool(this);
        return cleanup;
    }

    // Small helper component attached to each obstacle instance. It returns the obstacle
    // to the manager pool when it collides with the player (either trigger or collision).
    public class ObstacleCleanup : MonoBehaviour
    {
        private RoadObstacles pool;

        public void SetPool(RoadObstacles p) { pool = p; }

        // Called externally when the obstacle is slashed by the sword power.
        public void HandleSlashed()
        {
            // Notify behavior script (if present) to ignore detection briefly
            var obsBehavior = GetComponent<ObstacleBehaviorScript>();
            obsBehavior?.OnSlashed(0.25f);
            // Play plasma explosion & slow-motion like sprint mode when slashed
            ExplosionManager.Instance?.SpawnPlasmaExplosion(transform.position);
            TimeManager.Instance?.TriggerSlowMotion(2.5f);

            pool?.ReturnObstacleToPool(gameObject);
        }

        void OnCollisionEnter(Collision other)
        {
            // Player hit
            if (other.collider.CompareTag("Player"))
            {
                var playerObj = other.gameObject;
                var pa = playerObj.GetComponent<PlayerAnimation>();
                var pm = playerObj.GetComponent<PlayerMovement>();

                if (pa != null && pa.IsSprinting())
                {
                    // Player powered/sprinting: spawn plasma, destroy obstacle
                    ExplosionManager.Instance?.SpawnPlasmaExplosion(transform.position);
                    TimeManager.Instance?.TriggerSlowMotion(2.5f);
                    pool?.ReturnObstacleToPool(gameObject);
                    return;
                }

                // Normal player hit: simple death animation (no explosion)
                if (pa != null)
                {
                    // push player backward-only for visual feedback
                    if (pm != null)
                    {
                        Vector3 pushDir = -playerObj.transform.forward;
                        pushDir.y = 0f;
                        if (pushDir.sqrMagnitude < 0.0001f)
                            pushDir = (playerObj.transform.position - transform.position).normalized;
                        else
                            pushDir.Normalize();

                        pm.ApplyKnockback(pushDir, 8f, 6f);
                    }

                    pa.TriggerDeath();
                }

                pool?.ReturnObstacleToPool(gameObject);
                return;
            }

            // Car collision path (non-trigger collisions): same policy as trigger path
            /*
            if (other.collider.CompareTag("Car") || other.collider.CompareTag("DrunkCar") || other.gameObject.GetComponent<CarObstacle>() != null)
            {
                // Only remove the obstacle; do NOT destroy the car or play any VFX
                pool?.ReturnObstacleToPool(gameObject);
                return;
            }
            */
        }

        void OnTriggerEnter(Collider other)
        {
            // Player hit (trigger collider path)
            if (other.CompareTag("Player"))
            {
                var playerObj = other.gameObject;
                var pa = playerObj.GetComponent<PlayerAnimation>();
                var pm = playerObj.GetComponent<PlayerMovement>();

                if (pa != null && pa.IsSprinting())
                {
                    ExplosionManager.Instance?.SpawnPlasmaExplosion(transform.position);
                    TimeManager.Instance?.TriggerSlowMotion(2.5f);
                    pool?.ReturnObstacleToPool(gameObject);
                    return;
                }

                if (pa != null)
                {
                    if (pm != null)
                    {
                        Vector3 pushDir = -playerObj.transform.forward;
                        pushDir.y = 0f;
                        if (pushDir.sqrMagnitude < 0.0001f)
                            pushDir = (playerObj.transform.position - transform.position).normalized;
                        else
                            pushDir.Normalize();

                        pm.ApplyKnockback(pushDir, 8f, 6f);
                    }

                    pa.TriggerDeath();
                }

                pool?.ReturnObstacleToPool(gameObject);
                return;
            }

            // Car or other vehicle hits the obstacle → destroy obstacle (and optionally the car)
            /*
            if (other.CompareTag("Car") || other.CompareTag("DrunkCar") || other.GetComponent<CarObstacle>() != null)
            {
                // When a car collides with an obstacle, only destroy the obstacle.
                // Do NOT destroy the car or spawn any explosion/VFX/sound.
                pool?.ReturnObstacleToPool(gameObject);
                return;
            }
            */

            // Meteor collision (direct): destroy obstacle and optionally spawn meteor explosion
            if (other.GetComponent<MeteoriteDestroyer>() != null)
            {
                ExplosionManager.Instance?.SpawnMeteorExplosion(transform.position);
                pool?.ReturnObstacleToPool(gameObject);
                return;
            }
        }
    }
}