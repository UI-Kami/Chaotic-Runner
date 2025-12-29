using UnityEngine;

public class DebuffPower : MonoBehaviour
{
    [Header("Debuff Settings")]
    [Tooltip("Duration (seconds) of forced first-person view")]
    public float debuffDuration = 3f;

    [Header("Sound Settings")]
    public AudioClip pickupSound;
    [Range(0f, 1f)]
    public float soundVolume = 1f;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        PlayerAnimation anim = other.GetComponent<PlayerAnimation>();
        if (anim != null)
        {
            anim.ApplyFirstPersonDebuff(debuffDuration);
            PlayPickupSound(other.transform.position);
        }

        // Return to pool or destroy immediately (sound is safe)
        var cleanup = GetComponent<PowerSpawner.PowerCleanup>();
        if (cleanup != null)
        {
            cleanup.HandlePickup();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void PlayPickupSound(Vector3 position)
    {
        if (pickupSound == null)
            return;

        // Temporary AudioSource survives object destruction
        AudioSource.PlayClipAtPoint(
            pickupSound,
            position,
            soundVolume
        );
    }
}
