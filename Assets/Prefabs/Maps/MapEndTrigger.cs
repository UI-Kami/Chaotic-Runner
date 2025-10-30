using UnityEngine;

[RequireComponent(typeof(Collider))]
public class MapEndTrigger : MonoBehaviour
{
    public TimedMapSpawner spawner;
    public float destroyDelay = -1f;
    public string playerTag = "Player";

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void Start()
    {
        if (spawner == null)
            spawner = FindAnyObjectByType<TimedMapSpawner>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        GameObject mapRoot = transform.root.gameObject;
        if (spawner != null)
        {
            spawner.RequestDestroyMap(mapRoot, destroyDelay);
            Debug.Log($"🗺 MapEndTrigger → Destroying {mapRoot.name} after delay {destroyDelay}");
        }
        else
        {
            Debug.LogWarning("[MapEndTrigger] No TimedMapSpawner found!");
        }
    }
}
