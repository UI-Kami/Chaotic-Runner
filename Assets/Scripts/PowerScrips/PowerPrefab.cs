using UnityEngine;

public class SprintPower : MonoBehaviour
{
    [Header("Power Settings")]
    public float sprintDuration = 3f;

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
            anim.ActivateSprintBoost(sprintDuration);
            PlayPickupSound();
        }

        // Handle pooling or destroy fallback
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

    private void PlayPickupSound()
    {
        if (pickupSound == null)
            return;

        AudioManager.PlaySFX2D(pickupSound, soundVolume);
    }
}
