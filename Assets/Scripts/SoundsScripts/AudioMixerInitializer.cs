using UnityEngine;
using UnityEngine.Audio;

public class AudioMixerInitializer : MonoBehaviour
{
    public AudioMixer audioMixer;

    const float MIN_DB = -80f;

    const string MASTER_KEY = "MasterVolume";
    const string MUSIC_KEY  = "MusicVolume";
    const string SFX_KEY    = "SFXVolume";
    const string MUTE_KEY   = "Mute";

    void Awake()
    {
        ApplySavedSettings();
    }

    void ApplySavedSettings()
    {
        float master = PlayerPrefs.GetFloat(MASTER_KEY, 1f);
        float music  = PlayerPrefs.GetFloat(MUSIC_KEY, 1f);
        float sfx    = PlayerPrefs.GetFloat(SFX_KEY, 1f);
        bool muted   = PlayerPrefs.GetInt(MUTE_KEY, 0) == 1;

        audioMixer.SetFloat("MusicVolume", LinearToDB(music));
        audioMixer.SetFloat("SFXVolume", LinearToDB(sfx));

        audioMixer.SetFloat(
            "MasterVolume",
            muted ? MIN_DB : LinearToDB(master)
        );
    }

    float LinearToDB(float value)
    {
        return Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20f;
    }
}
