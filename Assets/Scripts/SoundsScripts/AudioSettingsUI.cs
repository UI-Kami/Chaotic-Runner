using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioSettingsUI : MonoBehaviour
{
    [Header("Mixer")]
    public AudioMixer audioMixer;

    [Header("UI - Sliders")]
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;
    public Toggle muteToggle;

    [Header("UI - Panels")]
    public GameObject menuPanel;
    public GameObject settingsPanel;

    const float MIN_DB = -80f;

    // PlayerPrefs keys
    const string MASTER_KEY = "MasterVolume";
    const string MUSIC_KEY  = "MusicVolume";
    const string SFX_KEY    = "SFXVolume";
    const string MUTE_KEY   = "Mute";

    void Awake()
    {
        LoadSettings();
    }

    void Start()
    {
        masterSlider.onValueChanged.AddListener(SetMasterVolume);
        musicSlider.onValueChanged.AddListener(SetMusicVolume);
        sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        muteToggle.onValueChanged.AddListener(SetMute);

        // Apply values to mixer on scene load
        ApplyAll();
    }

    // -------------------- LOAD / SAVE --------------------

    void LoadSettings()
    {
        masterSlider.value = PlayerPrefs.GetFloat(MASTER_KEY, 1f);
        musicSlider.value  = PlayerPrefs.GetFloat(MUSIC_KEY, 1f);
        sfxSlider.value    = PlayerPrefs.GetFloat(SFX_KEY, 1f);
        muteToggle.isOn    = PlayerPrefs.GetInt(MUTE_KEY, 0) == 1;
    }

    void SaveSettings()
    {
        PlayerPrefs.SetFloat(MASTER_KEY, masterSlider.value);
        PlayerPrefs.SetFloat(MUSIC_KEY, musicSlider.value);
        PlayerPrefs.SetFloat(SFX_KEY, sfxSlider.value);
        PlayerPrefs.SetInt(MUTE_KEY, muteToggle.isOn ? 1 : 0);
        PlayerPrefs.Save();
    }

void ApplyAll()
{
    if (AudioManager.Instance != null)
        AudioManager.Instance.ApplySavedSettings();
}


    // -------------------- VOLUME CONTROLS --------------------

    void SetMasterVolume(float value)
    {
        audioMixer.SetFloat("MasterVolume", LinearToDB(value));
        SaveSettings();
    }

    void SetMusicVolume(float value)
    {
        audioMixer.SetFloat("MusicVolume", LinearToDB(value));
        SaveSettings();
    }

    void SetSFXVolume(float value)
    {
        audioMixer.SetFloat("SFXVolume", LinearToDB(value));
        SaveSettings();
    }

    void SetMute(bool isMuted)
    {
        audioMixer.SetFloat(
            "MasterVolume",
            isMuted ? MIN_DB : LinearToDB(masterSlider.value)
        );
        SaveSettings();
    }

    float LinearToDB(float value)
    {
        return Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20f;
    }

    // -------------------- UI FLOW --------------------

    public void OpenSettings()
    {
        if (menuPanel != null) menuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (menuPanel != null) menuPanel.SetActive(true);
    }
}
