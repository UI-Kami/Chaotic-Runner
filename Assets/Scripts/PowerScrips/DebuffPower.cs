using UnityEngine;

public class DebuffPower : MonoBehaviour
{
    [Header("Debuff Settings")]
    [Tooltip("Duration (seconds) of forced first-person view")]
    public float debuffDuration = 3f;

    [Header("Sound Settings")]
    public AudioClip pickupSound;
    public float soundVolume = 1f;

    private AudioSource audioSource;

    void Awake()
    {
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
            {
                anim.ApplyFirstPersonDebuff(debuffDuration);
            }

            PlayPickupSound();

            var cleanup = GetComponent<PowerSpawner.PowerCleanup>();
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
            if (audioSource != null)
                audioSource.PlayOneShot(pickupSound, soundVolume);
            else
                AudioSource.PlayClipAtPoint(pickupSound, transform.position, soundVolume);
        }
    }
}
