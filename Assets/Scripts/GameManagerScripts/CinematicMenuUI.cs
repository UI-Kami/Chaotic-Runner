using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class CinematicMenuUI : MonoBehaviour
{
    public string mainGameSceneName = "3d";
    public GameObject menuPanel; // can hide this when game starts

    [Header("Start Delay Settings")]
    [Tooltip("If true, use a random delay between Min / Max. If false, use Fixed Start Delay.")]
    public bool useRandomStartDelay = true;

    [Tooltip("Minimum random initial delay (seconds)")]
    public float minStartDelay = 5f;

    [Tooltip("Maximum random initial delay (seconds)")]
    public float maxStartDelay = 10f;

    [Tooltip("Fixed initial delay (seconds) if not using random")]
    public float fixedStartDelay = 5f;

    [Header("Test Mode UI")]
    [Tooltip("Optional TextMeshPro label to show Test Mode state on the Test button.")]
    public TMPro.TMP_Text testModeButtonText;

    void Start()
    {
        // ensure cinematic mode is on while in this scene
        GameMode.IsCinematic = true;
        Time.timeScale = 1f;

        UpdateTestModeUI();
    }

    // --------------------------------------------------------------------
    // Test Mode button: start gameplay directly in Test Mode (no death, immediate run)
    public void OnTestPressed()
    {
        // Enable Test Mode and start the game immediately
        GameMode.IsTestMode = true;
        GameMode.IsCinematic = false;
        // Start immediately with no initial spawn suppression for quick testing
        GameMode.SetInitialSpawnSuppression(0f);

        // Load gameplay scene
        if (SceneTransition.Instance != null)
            SceneTransition.Instance.LoadSceneWithFade(mainGameSceneName);
        else
            SceneManager.LoadScene(mainGameSceneName);

        Debug.Log("Test Mode started");
    }

    private void UpdateTestModeUI()
    {
        if (testModeButtonText != null)
        {
            // Show current state briefly while on menu (useful if user toggles before scene load)
            testModeButtonText.text = GameMode.IsTestMode ? "Test Mode: ON" : "Test Mode: OFF";
            testModeButtonText.color = GameMode.IsTestMode ? Color.green : Color.white;
        }
    }

    public void OnStartPressed()
    {
        // disable cinematic mode and load main gameplay
        GameMode.IsCinematic = false;

        // determine configured delay and apply suppression
        float delay = useRandomStartDelay ? Random.Range(minStartDelay, maxStartDelay) : fixedStartDelay;
        GameMode.SetInitialSpawnSuppression(Mathf.Max(0f, delay));

        // Use transition if available
        if (SceneTransition.Instance != null)
            SceneTransition.Instance.LoadSceneWithFade(mainGameSceneName);
        else
            SceneManager.LoadScene(mainGameSceneName);

    }

    public void OnRestartPressed()
    {
        // reload this cinematic scene using transition if available
        string name = SceneManager.GetActiveScene().name;
        if (SceneTransition.Instance != null)
            SceneTransition.Instance.LoadSceneWithFade(name);
        else
            SceneManager.LoadScene(name);
    }

    public void OnExitPressed()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
