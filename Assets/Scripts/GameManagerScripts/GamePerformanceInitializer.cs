using UnityEngine;

/// <summary>
/// Sets global performance settings on Awake so the game runs smoothly
/// on all target devices without frame pacing issues, stutter, or excessive heating.
/// Also ensures an active AudioListener is present in the scene to prevent audio warnings.
/// </summary>
public class GamePerformanceInitializer : MonoBehaviour
{
    [Header("Frame Rate Settings")]
    [Tooltip("Target frame rate. Set to 60 for smooth 60 FPS, or -1 for uncapped.")]
    public int targetFrameRate = 60;

    [Tooltip("VSync count. 0 = Disabled (uses targetFrameRate), 1 = Every VBlank, 2 = Every Second VBlank.")]
    public int vSyncCount = 0;

    [Header("Physics Settings")]
    [Tooltip("Recommended fixed timestep for 60 FPS physics consistency.")]
    public float fixedTimestep = 0.02f; // 50 Hz default

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoInitialize()
    {
        // Enforce 60 FPS and disable VSync by default before scene loads
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
    }

    private void Awake()
    {
        QualitySettings.vSyncCount = vSyncCount;
        Application.targetFrameRate = targetFrameRate;
        Time.fixedDeltaTime = fixedTimestep;

        EnsureAudioListener();
    }

    private void Start()
    {
        EnsureAudioListener();
    }

    /// <summary>
    /// Guarantees at least one active AudioListener exists in the scene to suppress 'no audio listener' warnings.
    /// </summary>
    private void EnsureAudioListener()
    {
        AudioListener existing = FindFirstObjectByType<AudioListener>();
        if (existing != null && existing.enabled) return;

        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            if (mainCam.GetComponent<AudioListener>() == null)
                mainCam.gameObject.AddComponent<AudioListener>();
        }
        else
        {
            Camera anyCam = FindFirstObjectByType<Camera>();
            if (anyCam != null)
            {
                if (anyCam.GetComponent<AudioListener>() == null)
                    anyCam.gameObject.AddComponent<AudioListener>();
            }
            else
            {
                GameObject listenerObj = new GameObject("GlobalAudioListener");
                listenerObj.AddComponent<AudioListener>();
            }
        }
    }
}
