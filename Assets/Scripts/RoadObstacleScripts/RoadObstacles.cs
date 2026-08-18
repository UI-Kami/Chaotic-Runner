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

    [Header("Per-Map Obstacle Controls")]
    [Tooltip("Maximum total road obstacles to spawn per map segment.")]
    public int maxObstaclesPerMap = 4;
    [Tooltip("Minimum Z spacing (meters) between obstacles in the same lane.")]
    public float minZSpacingInLane = 40f;
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

        // Get actual map bounds to ensure spawns are within the physical map
        GetMapBounds(map, out float actualMapStartZ, out float actualMapEndZ);

        // Track Z positions of obstacles spawned per lane
        Dictionary<int, List<float>> laneSpawnedZ = new Dictionary<int, List<float>>();
        for (int i = 0; i < lanePositions.Length; i++)
            laneSpawnedZ[i] = new List<float>();

        List<int> availableLanes = new List<int>();
        for (int i = 0; i < lanePositions.Length; i++)
            availableLanes.Add(i);

        int totalSpawned = 0;
        int targetSpawnCount = Mathf.Min(maxObstaclesPerMap, lanePositions.Length * 2);

        for (int attempt = 0; attempt < targetSpawnCount * 4 && totalSpawned < targetSpawnCount; attempt++)
        {
            int laneIndex = availableLanes[Random.Range(0, availableLanes.Count)];
            float laneX = lanePositions[laneIndex];

            // Use actual map bounds with conservative buffers
            float spawnZ = Random.Range(actualMapStartZ + 25f, actualMapEndZ - 25f);

            // Verify minimum Z spacing from existing obstacles in the same lane
            bool spaceValid = true;
            foreach (float existingZ in laneSpawnedZ[laneIndex])
            {
                if (Mathf.Abs(spawnZ - existingZ) < minZSpacingInLane)
                {
                    spaceValid = false;
                    break;
                }
            }

            if (!spaceValid) continue;

            GameObject obs = GetFromPoolOrCreate();
            if (obs == null) continue;

            obs.SetActive(true);
            obs.transform.SetParent(null, false);
            obs.transform.position = new Vector3(laneX, obstacleY, spawnZ);
            obs.transform.rotation = Quaternion.LookRotation(Vector3.back);

            laneSpawnedZ[laneIndex].Add(spawnZ);
            totalSpawned++;

            var cleanup = EnsureCleanup(obs);
            cleanup.SetPool(this);
            cleanup.mapAssociation = map;

            if (!mapObstacles.TryGetValue(map, out var list))
            {
                list = new List<GameObject>();
                mapObstacles[map] = list;
            }
            list.Add(obs);
        }
    }

    // Track obstacles per map tile for instant lookup without FindObjectsOfType
    private readonly Dictionary<GameObject, List<GameObject>> mapObstacles = new();

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

        // reset behavior state
        var obsBehavior = obs.GetComponent<ObstacleBehaviorScript>();
        obsBehavior?.ResetVaultedState();

        // unparent and move off-screen
        obs.transform.SetParent(null);
        obs.transform.position = new Vector3(0f, -1000f, 0f);
        obs.SetActive(false);

        if (!pool.Contains(obs))
            pool.Enqueue(obs);
    }

    /// <summary>
    /// Return all obstacles associated with a map to the pool. Call this before destroying a map.
    /// </summary>
    public void ReturnObstaclesOnMap(GameObject map)
    {
        if (map == null) return;

        if (mapObstacles.TryGetValue(map, out var obsList))
        {
            for (int i = obsList.Count - 1; i >= 0; i--)
            {
                if (obsList[i] != null)
                    ReturnObstacleToPool(obsList[i]);
            }
            obsList.Clear();
            mapObstacles.Remove(map);
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
        public GameObject mapAssociation; // tracks which map this obstacle was spawned on
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
            // Ignore collisions if obstacle was already vaulted over
            var obsBehavior = GetComponent<ObstacleBehaviorScript>();
            if (obsBehavior != null && obsBehavior.IsVaulted()) return;

            // Player hit
            if (other.collider.CompareTag("Player"))
            {
                var playerObj = other.gameObject;
                var pa = playerObj.GetComponent<PlayerAnimation>();
                var pm = playerObj.GetComponent<PlayerMovement>();

                if (pa != null && (pa.IsSprinting() || pa.IsPerformingStunt()))
                {
                    // Player powered/sprinting or performing stunt vault: spawn plasma, destroy obstacle
                    ExplosionManager.Instance?.SpawnPlasmaExplosion(transform.position);
                    TimeManager.Instance?.TriggerSlowMotion(2.5f);
                    pool?.ReturnObstacleToPool(gameObject);
                    return;
                }

                // Safety grace buffer: if player collides with fence while holding/pressing jump, execute fence jump instead of death
                if (obsBehavior != null && pa != null && pa.HasNearbyFence() && Input.GetKey(KeyCode.Space))
                {
                    obsBehavior.PerformFenceJump(playerObj);
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
            // Ignore triggers if obstacle was already vaulted over
            var obsBehavior = GetComponent<ObstacleBehaviorScript>();
            if (obsBehavior != null && obsBehavior.IsVaulted()) return;

            // Player hit (trigger collider path)
            if (other.CompareTag("Player"))
            {
                var playerObj = other.gameObject;
                var pa = playerObj.GetComponent<PlayerAnimation>();
                var pm = playerObj.GetComponent<PlayerMovement>();

                if (pa != null && (pa.IsSprinting() || pa.IsPerformingStunt()))
                {
                    ExplosionManager.Instance?.SpawnPlasmaExplosion(transform.position);
                    TimeManager.Instance?.TriggerSlowMotion(2.5f);
                    pool?.ReturnObstacleToPool(gameObject);
                    return;
                }

                // Safety grace buffer: if player collides with fence while holding/pressing jump, execute fence jump instead of death
                if (obsBehavior != null && pa != null && pa.HasNearbyFence() && Input.GetKey(KeyCode.Space))
                {
                    obsBehavior.PerformFenceJump(playerObj);
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

    // Helper method to get the actual Z bounds of a map based on its colliders/renderers
    private void GetMapBounds(GameObject map, out float startZ, out float endZ)
    {
        Renderer[] renderers = map.GetComponentsInChildren<Renderer>();
        Collider[] colliders = map.GetComponentsInChildren<Collider>();

        startZ = float.MaxValue;
        endZ = float.MinValue;

        // Check renderers
        foreach (Renderer r in renderers)
        {
            if (r.bounds.center.z - r.bounds.extents.z < startZ)
                startZ = r.bounds.center.z - r.bounds.extents.z;
            if (r.bounds.center.z + r.bounds.extents.z > endZ)
                endZ = r.bounds.center.z + r.bounds.extents.z;
        }

        // Check colliders
        foreach (Collider c in colliders)
        {
            Bounds b = c.bounds;
            if (b.center.z - b.extents.z < startZ)
                startZ = b.center.z - b.extents.z;
            if (b.center.z + b.extents.z > endZ)
                endZ = b.center.z + b.extents.z;
        }

        // Fallback if no renderers or colliders found
        if (startZ == float.MaxValue || endZ == float.MinValue)
        {
            startZ = map.transform.position.z;
            endZ = map.transform.position.z + 200f; // default map length
        }
    }
}