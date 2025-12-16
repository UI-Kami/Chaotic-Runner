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
    public GameObject swordModelPrefab; // optional model to attach to player's hand when picked
    public float swordDuration = 8f;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        var player = other.gameObject;

        // Give player the sword power (adds PlayerSword component if missing)
        var ps = player.GetComponent<PlayerSword>();
        if (ps == null)
            ps = player.AddComponent<PlayerSword>();

        // Activate manual sword mode: player must press mouse button to slash
        ps.ActivateSwordManual(swordDuration, slashClip, slashVolume);

        // Return this pickup to the pool (or destroy)
        var cleanupComp = GetComponent<PowerSpawner.PowerCleanup>();
        if (cleanupComp != null)
            cleanupComp.HandlePickup();
        else
            Destroy(gameObject, 0.05f);
    }
}
