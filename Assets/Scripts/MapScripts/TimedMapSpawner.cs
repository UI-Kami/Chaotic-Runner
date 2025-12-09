using UnityEngine;

public class TimedMapSpawner : MonoBehaviour
{
    public MapManager mapManager;
    public PowerSpawner powerSpawner;
    public MeteoriteSpawner meteoriteSpawner;
    public RoadObstacles roadObstacles;

    void Awake()
    {
        if (mapManager == null) mapManager = FindAnyObjectByType<MapManager>();
        if (powerSpawner == null) powerSpawner = FindAnyObjectByType<PowerSpawner>();
        if (meteoriteSpawner == null) meteoriteSpawner = FindAnyObjectByType<MeteoriteSpawner>();
        if (roadObstacles == null) roadObstacles = FindAnyObjectByType<RoadObstacles>();
    }

    // Keep compatibility for other code that calls these on the old script:
    public void RequestDestroyMap(GameObject map, float delay = -1f)
    {
        mapManager?.RequestDestroyMap(map, delay);
    }

    public void ReturnPowerToPool(GameObject power)
    {
        powerSpawner?.ReturnPowerToPool(power);
    }
}