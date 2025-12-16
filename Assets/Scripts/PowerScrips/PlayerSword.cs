using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Transform))]
public class PlayerSword : MonoBehaviour
{
    [Header("Hand & Visual")]
    [Tooltip("Assign the Transform on the player where the sword should be parented (hand).")]
    public Transform handTransform;
    [Tooltip("If you already placed a sword model under the hand, assign it here (set inactive in prefab). If empty, the script will try to find the first child under the hand transform).")]
    public GameObject handWeapon;

    [Header("Manual Slash Settings")]
    [Tooltip("Cooldown between manual slashes (seconds).")]
    public float slashCooldown = 0.25f;
    [Tooltip("How long (seconds) the sword remains active after pickup if not used.")]
    public float defaultSwordDuration = 8f;
    [Tooltip("When slashing we enable the sword collider for this many seconds (very short).")]
    public float slashColliderEnableTime = 0.06f;

    private float lastSlashTime = -100f;
    private AudioClip slashClip;
    private float slashVolume = 1f;

    private bool hasSword = false;
    private float swordExpiresAt = 0f;
    private Collider swordCollider = null;

    void Update()
    {
        if (!hasSword) return;

        if (Time.time >= swordExpiresAt)
        {
            DeactivateSword();
            return;
        }

        // Manual slash: listen for left mouse button press
        if (Input.GetMouseButtonDown(0) && Time.time - lastSlashTime >= slashCooldown)
        {
            DoManualSlash();
        }
    }

    private void DoManualSlash()
    {
        if (!hasSword) return;
        lastSlashTime = Time.time;

        // trigger player sword animation
        var pa = GetComponent<PlayerAnimation>();
        pa?.TriggerSwordSlash();

        // play sound
        if (slashClip != null)
            AudioSource.PlayClipAtPoint(slashClip, transform.position, slashVolume);

        // briefly enable the sword collider to allow collision-based hits
        if (swordCollider != null)
            StartCoroutine(EnableSwordColliderBriefly());
    }

    private System.Collections.IEnumerator EnableSwordColliderBriefly()
    {
        swordCollider.enabled = true;
        yield return new WaitForSeconds(slashColliderEnableTime);
        if (swordCollider != null)
            swordCollider.enabled = false;
    }

    public void ActivateSword(GameObject swordModelPrefab, float duration, AudioClip clip, float volume)
    {
        if (hasSword)
            DeactivateSword();

        // Try to auto-find a hand transform if none assigned in inspector
        if (handTransform == null)
        {
            handTransform = FindHandTransform();
            if (handTransform == null)
                Debug.LogWarning("PlayerSword: No handTransform assigned and auto-find failed. Sword model will not be parented.");
            else
                Debug.Log("PlayerSword: Auto-found hand transform: " + handTransform.name);
        }

        hasSword = true;
        swordExpiresAt = Time.time + Mathf.Max(0.001f, duration);
        slashClip = clip;
        slashVolume = volume;

        // Prefer an already-placed hand weapon (set inactive in the Player prefab). If not assigned, try to pick the first child under handTransform.
        if (handWeapon == null && handTransform != null && handTransform.childCount > 0)
        {
            handWeapon = handTransform.GetChild(0).gameObject;
            Debug.Log("PlayerSword: Auto-found hand weapon: " + handWeapon.name);
        }

        if (handWeapon != null)
        {
            handWeapon.SetActive(true);
            // find collider on the hand weapon and disable it until slash
            swordCollider = handWeapon.GetComponentInChildren<Collider>(true);
            if (swordCollider != null)
                swordCollider.enabled = false;
        }

        Debug.Log($"PlayerSword: Activated sword for {duration} seconds. HandTransform={(handTransform!=null?handTransform.name:"null")}");
    }

    // Attempt to find a likely hand transform by name search in children
    private Transform FindHandTransform()
    {
        var kids = GetComponentsInChildren<Transform>(true);
        foreach (var t in kids)
        {
            if (t.name.ToLower().Contains("hand") || t.name.ToLower().Contains("right"))
                return t;
        }
        return null;
    }

    public void DeactivateSword()
    {
        hasSword = false;
        swordExpiresAt = 0f;
        slashClip = null;
        if (handWeapon != null)
            handWeapon.SetActive(false);
        handWeapon = null;
        swordCollider = null;
    }

    // Convenience method used by pickups to activate manual sword mode
    public void ActivateSwordManual(float duration, AudioClip clip, float volume)
    {
        ActivateSword(null, duration > 0f ? duration : defaultSwordDuration, clip, volume);
    }
}
