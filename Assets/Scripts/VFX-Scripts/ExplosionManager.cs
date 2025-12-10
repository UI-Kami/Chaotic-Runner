using UnityEngine;

public class ExplosionManager : MonoBehaviour
{
    public static ExplosionManager Instance { get; private set; }

    public GameObject carExplosionPrefab;
    public GameObject meteorExplosionPrefab;
    public GameObject plasmaExplosionPrefab;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void SpawnCarExplosion(Vector3 position)
    {
        SpawnAndAutoDestroy(carExplosionPrefab, position);
        ExplosionSoundManager.Instance?.PlayCarExplosion(position);
    }

    public void SpawnMeteorExplosion(Vector3 position)
    {
        SpawnAndAutoDestroy(meteorExplosionPrefab, position);
        ExplosionSoundManager.Instance?.PlayMeteorExplosion(position);
    }

    public void SpawnPlasmaExplosion(Vector3 position)
    {
        SpawnAndAutoDestroy(plasmaExplosionPrefab, position);
        ExplosionSoundManager.Instance?.PlayPlasmaExplosion(position);
    }

    private void SpawnAndAutoDestroy(GameObject prefab, Vector3 position)
    {
        if (prefab == null) return;

        GameObject explosion = Instantiate(prefab, position, Quaternion.identity);
        ParticleSystem ps = explosion.GetComponentInChildren<ParticleSystem>();

        if (ps != null)
            Object.Destroy(explosion, ps.main.duration + ps.main.startLifetime.constantMax + 0.5f);
        else
            Object.Destroy(explosion, 5f); // fallback
    }
}
