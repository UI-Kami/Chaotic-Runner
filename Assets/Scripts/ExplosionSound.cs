using UnityEngine;

public class ExplosionSoundManager : MonoBehaviour
{
    public static ExplosionSoundManager Instance { get; private set; }

    [Header("Explosion Sounds")]
    public AudioClip carExplosionClip;
    public AudioClip meteorExplosionClip;
    public AudioClip plasmaExplosionClip;

    [Header("Audio Settings")]
    public float minPitch = 0.9f;
    public float maxPitch = 1.1f;
    public float volume = 1.0f;
    public float maxDistance = 120f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // --------------------------------------------------------------------
    // 💥 Play car explosion sound
    public void PlayCarExplosion(Vector3 position)
    {
        PlayExplosionSound(carExplosionClip, position);
    }

    // ☄️ Play meteor explosion sound
    public void PlayMeteorExplosion(Vector3 position)
    {
        PlayExplosionSound(meteorExplosionClip, position);
    }

    // ⚡ Play plasma explosion sound
    public void PlayPlasmaExplosion(Vector3 position)
    {
        PlayExplosionSound(plasmaExplosionClip, position);
    }

    // --------------------------------------------------------------------
    // 🔊 Internal method
    private void PlayExplosionSound(AudioClip clip, Vector3 position)
    {
        if (clip == null) return;

        GameObject soundObj = new GameObject("ExplosionSound");
        soundObj.transform.position = position;

        AudioSource source = soundObj.AddComponent<AudioSource>();
        source.clip = clip;
        source.spatialBlend = 1f; // 3D sound
        source.maxDistance = maxDistance;
        source.volume = Random.Range(volume * 0.8f, volume);
        source.pitch = Random.Range(minPitch, maxPitch);
        source.Play();

        Destroy(soundObj, clip.length + 0.2f);
    }
}
