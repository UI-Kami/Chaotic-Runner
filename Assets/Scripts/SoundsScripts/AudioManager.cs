using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Mixer")]
    public AudioMixer audioMixer;

    const float MIN_DB = -80f;

    const string MASTER_KEY = "MasterVolume";
    const string MUSIC_KEY  = "MusicVolume";
    const string SFX_KEY    = "SFXVolume";
    const string MUTE_KEY   = "Mute";

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        ApplySavedSettings();
    }

    // ---------------- APPLY SETTINGS ----------------

    public void ApplySavedSettings()
    {
        float master = PlayerPrefs.GetFloat(MASTER_KEY, 1f);
        float music  = PlayerPrefs.GetFloat(MUSIC_KEY, 1f);
        float sfx    = PlayerPrefs.GetFloat(SFX_KEY, 1f);
        bool muted   = PlayerPrefs.GetInt(MUTE_KEY, 0) == 1;

        audioMixer.SetFloat("MusicVolume", SliderToDB(music));
        audioMixer.SetFloat("SFXVolume", SliderToDB(sfx));

        audioMixer.SetFloat(
            "MasterVolume",
            muted ? MIN_DB : SliderToDB(master)
        );
    }

    // ---------------- PROPER LOUDNESS CURVE ----------------

    float SliderToDB(float value)
    {
        if (value <= 0.0001f)
            return MIN_DB;

        // Aggressive falloff at low values (feels natural)
        return Mathf.Lerp(-40f, 0f, Mathf.Pow(value, 0.5f));
    }

    /// <summary>
    /// Play a 2D SFX clip instantly at full volume with zero 3D spatial delay or distance attenuation.
    /// </summary>
    public static void PlaySFX2D(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;

        GameObject soundObj = new GameObject($"SFX_2D_{clip.name}");
        AudioSource source = soundObj.AddComponent<AudioSource>();
        source.clip = clip;
        source.spatialBlend = 0f; // 2D (non-spatial) for immediate crisp playback
        source.volume = Mathf.Clamp01(volume);
        source.pitch = 1f;
        source.playOnAwake = false;

        if (Instance != null && Instance.audioMixer != null)
        {
            var sfxGroup = Instance.audioMixer.FindMatchingGroups("SFX");
            if (sfxGroup != null && sfxGroup.Length > 0)
                source.outputAudioMixerGroup = sfxGroup[0];
        }

        source.Play();
        Destroy(soundObj, clip.length + 0.1f);
    }
}
