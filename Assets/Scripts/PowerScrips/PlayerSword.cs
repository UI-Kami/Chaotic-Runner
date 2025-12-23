using UnityEngine;

[RequireComponent(typeof(Transform))]
public class PlayerSword : MonoBehaviour
{
    [Header("Sword Ring")]
    [Tooltip("Assign the spinning sword ring prefab to spawn when the power is picked up.")]
    public GameObject swordRingPrefab;
    [Tooltip("If true the ring will be parented to the player so it moves with the player.")]
    public bool parentRingToPlayer = true;
    [Tooltip("Local X/Y offset to apply to the ring when spawned (X = horizontal, Y = vertical height).")]
    public Vector2 ringOffset = Vector2.zero;
    [Tooltip("How long (seconds) the ring remains active after pickup if not used.")]
    public float defaultSwordDuration = 8f;

    private bool hasSword = false;
    private float swordExpiresAt = 0f;
    private GameObject ringInstance = null;

    [Tooltip("Minimum seconds between consecutive activations to prevent overlapping pickups from rapidly replacing the ring.")]
    public float activationDebounce = 0.3f;
    private float lastActivationTime = -100f;

    void Update()
    {
        if (!hasSword) return;

        if (Time.time >= swordExpiresAt)
        {
            DeactivateSword();
            return;
        }

        // nothing else needed — ring follows player via parent or kept in Update if desired
    }

    /// <summary>
    /// Spawn and activate a spinning sword ring at the player. If ringPrefab is null, attempts to use the inspector-assigned prefab.
    /// </summary>
    public void ActivateRing(GameObject ringPrefab, float duration, AudioClip clip, float volume)
    {
        // simple debounce to avoid rapid repeated activations (e.g., overlapping pickups)
        if (Time.time - lastActivationTime < activationDebounce)
        {
            Debug.Log("PlayerSword: ActivateRing debounced");
            return;
        }
        lastActivationTime = Time.time;

        if (hasSword)
            DeactivateSword();

        hasSword = true;
        swordExpiresAt = Time.time + Mathf.Max(0.001f, duration > 0f ? duration : defaultSwordDuration);

        GameObject prefabToUse = ringPrefab ?? swordRingPrefab;
        if (prefabToUse == null)
        {
            Debug.LogWarning("PlayerSword: No ring prefab provided or assigned in inspector.");
            return;
        }

        ringInstance = Instantiate(prefabToUse, transform.position, Quaternion.identity);
        // sanitize instance: remove pickup/cleanup scripts and rigidbodies if any (avoid nested power behavior)
        var allComps = ringInstance.GetComponentsInChildren<Component>(true);
        foreach (var c in allComps)
        {
            if (c == null) continue;
            // keep rendering, animators, transforms, colliders; remove manager/cleanup scripts
            if (c is PowerSpawner || c is SwordPower || c.GetType().Name == "PowerCleanup")
                Destroy(c);
        }

        // Parent and snap to player so it's centered and follows exactly
        if (parentRingToPlayer)
        {
            ringInstance.transform.SetParent(transform, false);
            ringInstance.transform.localPosition = new Vector3(ringOffset.x, ringOffset.y, 0f);
            ringInstance.transform.localRotation = Quaternion.identity;
            ringInstance.transform.localScale = Vector3.one;
        }
        else
        {
            ringInstance.transform.position = transform.position + new Vector3(ringOffset.x, ringOffset.y, 0f);
        }

        // normalize name to avoid repeated "(Clone)(Clone)..." chains
        var cleanName = prefabToUse.name.Replace("(Clone)", "");
        ringInstance.name = cleanName + "(PlayerRing)";

        // Ensure GameObject is active and any root renderers/animators are enabled so it is visible
        ringInstance.SetActive(true);
        var anim = ringInstance.GetComponentInChildren<Animator>();
        if (anim != null)
        {
            anim.Rebind();
            anim.Update(0f);
        }
        var renderers = ringInstance.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers) r.enabled = true;

        // If prefab has no Animator and no obvious rotating component, add a small runtime spin so it doesn't appear static
        bool hasVisualSpinner = ringInstance.GetComponentInChildren<Animator>() != null || ringInstance.GetComponentInChildren<RuntimeSpin>() != null;
        if (!hasVisualSpinner)
        {
            var rs = ringInstance.AddComponent<RuntimeSpin>();
            rs.degreesPerSec = new UnityEngine.Vector3(0f, 180f, 0f);
        }

        // Ensure there's a trigger collider for hits (if prefab doesn't supply one)
        var anyCollider = ringInstance.GetComponentInChildren<Collider>();
        if (anyCollider == null)
        {
            var sc = ringInstance.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 1.5f; // reasonable default — tweak in prefab if needed
        }
        else if (!anyCollider.isTrigger)
        {
            anyCollider.isTrigger = true;
        }

        // Ensure there's a Rigidbody so trigger/collision callbacks fire reliably while the ring is kinematic and parented to the player.
        var rb = ringInstance.GetComponent<Rigidbody>() ?? ringInstance.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // Ensure hit logic exists so ring destroys obstacles/cars. If the prefab doesn't already have SwordHitBox, add it.
        var sb = ringInstance.GetComponent<SwordHitBox>() ?? ringInstance.GetComponentInChildren<SwordHitBox>();
        if (sb == null)
        {
            sb = ringInstance.AddComponent<SwordHitBox>();
        }
        // Activate the hitbox so it only acts when owned by the player (prevents world instances from triggering)
        sb.ActivateForPlayer();

        Debug.Log($"PlayerSword: Spawned ring '{ringInstance.name}' at {ringInstance.transform.position} parent={(ringInstance.transform.parent!=null?ringInstance.transform.parent.name:"null")} for {duration} seconds.");
    }

    public void DeactivateSword()
    {
        hasSword = false;
        swordExpiresAt = 0f;
        if (ringInstance != null)
        {
            Destroy(ringInstance);
            ringInstance = null;
        }
    }

    // Backwards-compatible: power pickups may call this. It will try to use the inspector-assigned ring prefab.
    public void ActivateSwordManual(float duration, AudioClip clip, float volume)
    {
        ActivateRing(null, duration > 0f ? duration : defaultSwordDuration, clip, volume);
    }
}
