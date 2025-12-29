using UnityEngine;

public class SwordPower : MonoBehaviour
{
    [Header("Slash Settings")]
    public float slashDistance = 4f;
    [Range(1, 9)] public int rayCount = 5;
    [Range(0f, 90f)] public float raySpreadAngle = 30f;
    public float rayHeight = 1.2f;
    public float rayWidth = 0.5f;
    public LayerMask hitMask = ~0;

    [Header("Sound Settings")]
    public AudioClip slashClip;
    [Range(0f, 1f)]
    public float slashVolume = 1f;

    [Header("Pickup Settings")]
    public GameObject swordModelPrefab;   // legacy (optional)
    public GameObject swordRingPrefab;    // spinning ring prefab
    public float swordDuration = 8f;

    // Prevent multiple trigger activations
    private bool pickedUp = false;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (pickedUp)
            return;

        pickedUp = true;

        // Disable trigger immediately to prevent duplicate pickups
        Collider selfCol = GetComponent<Collider>();
        if (selfCol != null)
            selfCol.enabled = false;

        GameObject player = other.gameObject;

        // Ensure PlayerSword component exists
        PlayerSword ps = player.GetComponent<PlayerSword>();
        if (ps == null)
            ps = player.AddComponent<PlayerSword>();

        // Resolve ring prefab (assigned or fallback from children)
        GameObject prefabToPass = ResolveRingPrefab();

        // Activate sword power
        ps.ActivateRing(prefabToPass, swordDuration, slashClip, slashVolume);

        // Play pickup sound safely (survives destroy / pooling)
        PlayPickupSound(player.transform.position);

        // Return pickup to pool or destroy
        var cleanup = GetComponent<PowerSpawner.PowerCleanup>();
        if (cleanup != null)
            cleanup.HandlePickup();
        else
            Destroy(gameObject);
    }

    private void PlayPickupSound(Vector3 position)
    {
        if (slashClip == null)
            return;

        AudioSource.PlayClipAtPoint(slashClip, position, slashVolume);
    }

    private GameObject ResolveRingPrefab()
    {
        if (swordRingPrefab != null)
            return swordRingPrefab;

        // Fallback: search suitable visual child
        foreach (Transform c in transform)
        {
            string n = c.name.ToLower();
            if (!(n.Contains("ring") || n.Contains("sword") || n.Contains("blade")))
                continue;

            bool isVisual =
                c.GetComponentInChildren<Renderer>(true) != null ||
                c.GetComponentInChildren<Animator>(true) != null;

            bool isPickupLike =
                c.GetComponentInChildren<PowerSpawner>(true) != null ||
                c.GetComponentInChildren<SwordPower>(true) != null ||
                c.GetComponentInChildren<PowerSpawner.PowerCleanup>(true) != null;

            if (isVisual && !isPickupLike)
            {
                Debug.Log($"SwordPower: Using child '{c.name}' as ring prefab fallback.");
                return c.gameObject;
            }
        }

        Debug.LogWarning(
            "SwordPower: No swordRingPrefab assigned and no safe child fallback found. Ring will not spawn."
        );

        return null;
    }
}
