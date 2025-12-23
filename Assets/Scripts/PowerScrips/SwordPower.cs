using System.Collections.Generic;
using UnityEngine;

public class SwordPower : MonoBehaviour
{
    [Header("Slash Settings")]
    public float slashDistance = 4f;
    [Range(1, 9)] public int rayCount = 5;
    [Range(0f, 90f)] public float raySpreadAngle = 30f;
    public float rayHeight = 1.2f;
    public float rayWidth = 0.5f;
    public LayerMask hitMask = ~0; // default: everything

    [Header("Sound")]
    public AudioClip slashClip;
    public float slashVolume = 1f;

    [Header("Pickup Settings")]
    public GameObject swordModelPrefab; // optional model to attach to player's hand when picked (legacy)
    public GameObject swordRingPrefab; // new: the spinning ring prefab to spawn at player when picked up
    public float swordDuration = 8f;

    // Prevent multiple pickup triggers from firing repeatedly (e.g., multiple player colliders)
    private bool pickedUp = false;

    void OnTriggerEnter(Collider other)
    {
        // Only log player pickups to avoid noisy car collisions flooding the Console
        if (other.CompareTag("Player"))
            Debug.Log($"SwordPower.OnTriggerEnter (instance={GetInstanceID()}) picked by {other.name}");
        if (!other.CompareTag("Player")) return;

        // prevent double-activation if multiple colliders overlap
        if (pickedUp) return;
        pickedUp = true;

        // disable our trigger immediately so no other OnTriggerEnter runs
        var selfCol = GetComponent<Collider>();
        if (selfCol != null) selfCol.enabled = false;

        var player = other.gameObject;

        // Give player the sword power (adds PlayerSword component if missing)
        var ps = player.GetComponent<PlayerSword>();
        if (ps == null)
            ps = player.AddComponent<PlayerSword>();

        // Activate ring sword: spawn spinning ring at player that follows and destroys obstacles/cars on contact
        // If pickup does not have a ring prefab assigned, try to find a suitable child (fallback)
        GameObject prefabToPass = swordRingPrefab;
        if (prefabToPass == null)
        {
            // Prefer a child that looks visual (has Renderer or Animator) and doesn't itself act as a pickup/cleanup object.
            foreach (Transform c in transform)
            {
                var n = c.name.ToLower();
                if (!(n.Contains("ring") || n.Contains("sword") || n.Contains("blade"))) continue;

                bool isVisual = c.GetComponentInChildren<Renderer>(true) != null || c.GetComponentInChildren<Animator>(true) != null;
                bool isPickupLike = c.GetComponentInChildren<PowerSpawner>(true) != null || c.GetComponentInChildren<SwordPower>(true) != null || c.GetComponentInChildren<PowerSpawner.PowerCleanup>(true) != null;
                if (isVisual && !isPickupLike)
                {
                    prefabToPass = c.gameObject;
                    Debug.Log("SwordPower: Using local visual child '" + c.name + "' as ring prefab fallback.");
                    break;
                }
            }
            if (prefabToPass == null)
                Debug.LogWarning("SwordPower: No swordRingPrefab assigned on pickup and no safe child ring candidate found — ring will not be spawned. Please assign the ring prefab in inspector.");
        }
        
        ps.ActivateRing(prefabToPass, swordDuration, slashClip, slashVolume);

        // Return this pickup to the pool (or destroy)
        var cleanupComp = GetComponent<PowerSpawner.PowerCleanup>();
        if (cleanupComp != null)
            cleanupComp.HandlePickup();
        else
            Destroy(gameObject, 0.05f);
    }

        // ps.ActivateRing(prefabToPass, swordDuration, slashClip, slashVolume);

        // // Return this pickup to the pool (or destroy)
        // var cleanupComp = GetComponent<PowerSpawner.PowerCleanup>();
        // if (cleanupComp != null)
        //     cleanupComp.HandlePickup();
        // else
        //     Destroy(gameObject, 0.05f);
}


