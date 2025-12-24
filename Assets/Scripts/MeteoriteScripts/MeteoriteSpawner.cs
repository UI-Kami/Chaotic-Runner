using System.Collections;
using UnityEngine;

public class MeteoriteSpawner : MonoBehaviour
{
    [Header("References")]
    public MapManager mapManager;
    public Transform player;

    [Header("Meteorite Settings")]
    public GameObject meteoritePrefab;
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

    void Start()
    {
        skyDarkener = FindAnyObjectByType<SkyDarkener_Builtin>();

        if (mapManager == null)
            mapManager = FindAnyObjectByType<MapManager>();

        StartCoroutine(MeteoriteRoutine());
    }

    private IEnumerator MeteoriteRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(meteoriteInterval + Random.Range(-1f, 2f));
            if (player == null || meteoritePrefab == null || mapManager == null) continue;

            // Skip launching meteor waves while initial spawn suppression is active
            if (GameMode.IsInitialSpawnSuppressed) continue;

            skyDarkener?.DarkenSky();
            LaunchMeteoriteWave();
        }
    }

    private void LaunchMeteoriteWave()
    {
        if (meteoritePrefab == null || player == null || mapManager == null || mapManager.activeMaps.Count == 0) return;

        GameObject lastMap = mapManager.GetLastMap();
        float spawnZ = lastMap.transform.position.z + (mapManager.GetMapLength() * 0.5f);

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

            if (rb != null)
            {
                Vector3 target = player.position + new Vector3(Random.Range(-5f, 5f), -15f, Random.Range(10f, 25f));
                Vector3 direction = (target - spawnPos).normalized;
                rb.velocity = direction * meteoriteSpeed;
                rb.useGravity = true;
                rb.AddTorque(Random.insideUnitSphere * 20f, ForceMode.VelocityChange);
            }

            MeteoriteDestroyer destroyer = meteor.AddComponent<MeteoriteDestroyer>();
            destroyer.DestroyDelay = meteoriteDestroyDelay;
            destroyer.fallDestroyY = meteoriteFallDestroyY;
            destroyer.shakeIntensity = meteorShakeIntensity;
            destroyer.shakeDuration = meteorShakeDuration;

            // hook up the sky darkener reference and register this meteor so the sky knows an active meteor exists
            destroyer.skyDarkener = skyDarkener;
            skyDarkener?.RegisterMeteor();
        }

        Debug.Log($"☄️ Meteorite wave launched ({meteorCount} meteors)");
    }
}