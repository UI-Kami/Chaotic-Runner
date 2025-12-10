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

    [Header("3D Audio Tuning")]
    [Range(0f, 1f)] public float defaultSpatialBlend = 0.6f;
    public AudioRolloffMode defaultRolloff = AudioRolloffMode.Linear;
    public float defaultMinDistance = 2f;
    public float defaultDoppler = 0f;

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

    // Optional: play meteor explosion as global (non-spatial) so it's always audible
    public void PlayMeteorExplosionGlobal(Vector3 position)
    {
        if (meteorExplosionClip == null) return;

        GameObject s = new GameObject("ExplosionSound_Global");
        s.transform.position = position;
        AudioSource src = s.AddComponent<AudioSource>();
        src.clip = meteorExplosionClip;
        src.spatialBlend = 0f; // 2D (non-spatial)
        src.volume = volume;
        src.pitch = Random.Range(minPitch, maxPitch);
        src.Play();
        Destroy(s, meteorExplosionClip.length / Mathf.Abs(src.pitch) + 0.2f);
    }

    // --------------------------------------------------------------------
    // 🔊 Internal method (tuned for audible, timely explosions)
    private void PlayExplosionSound(AudioClip clip, Vector3 position)
    {
        if (clip == null) return;

        GameObject soundObj = new GameObject("ExplosionSound");
        soundObj.transform.position = position;

        AudioSource source = soundObj.AddComponent<AudioSource>();
        source.clip = clip;

        // Spatial settings — tuned for explosions that should be heard from a distance
        source.spatialBlend = Mathf.Clamp01(defaultSpatialBlend); // 0.0 = 2D, 1.0 = fully 3D. Blend so it's still audible at range.
        source.rolloffMode = defaultRolloff;
        source.minDistance = Mathf.Max(0.01f, defaultMinDistance);   // close-in volume stays high
        source.maxDistance = Mathf.Max(1f, maxDistance);            // ensure positive
        source.dopplerLevel = defaultDoppler;
        source.spatialize = false;

        // Volume & pitch variation
        source.volume = Random.Range(volume * 0.85f, Mathf.Clamp01(volume));
        source.pitch = Random.Range(minPitch, maxPitch);

        source.Play();

        // Destroy taking pitch into account (so pitched clips are cleaned up correctly)
        float effectiveLength = clip.length / Mathf.Abs(source.pitch);
        Destroy(soundObj, effectiveLength + 0.2f);
    }
}