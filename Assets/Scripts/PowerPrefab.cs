using UnityEngine;

public class SprintPower : MonoBehaviour
{
    [Header("Power Settings")]
    public float sprintDuration = 3f;

    [Header("Sound Settings")]
    public AudioClip pickupSound;
    public float soundVolume = 1f;

    private AudioSource audioSource;

    void Awake()
    {
        // Get or add AudioSource if it doesn't exist
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerAnimation anim = other.GetComponent<PlayerAnimation>();
            if (anim != null)
                anim.ActivateSprintBoost(sprintDuration);

            // Play pickup sound locally
            PlayPickupSound();

            // If this instance is pooled, return it to the pool via PowerCleanup.
            // Otherwise fallback to destroying the object (non-pooled usage).
            var cleanup = GetComponent<TimedMapSpawner.PowerCleanup>();
            if (cleanup != null)
            {
                cleanup.HandlePickup();
            }
            else
            {
                Destroy(gameObject, 0.05f);
            }
        }
    }

    public void PlayPickupSound()
    {
        if (pickupSound != null)
        {
            // If this object has its own AudioSource, play locally
            if (audioSource != null)
            {
                audioSource.PlayOneShot(pickupSound, soundVolume);
            }
            else
            {
                // Fallback: Play a one-shot sound at the pickup position
                AudioSource.PlayClipAtPoint(pickupSound, transform.position, soundVolume);
            }
        }
    }
}