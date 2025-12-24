using UnityEngine;
using UnityEngine.SceneManagement;

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

    void Start()
    {
        // ensure cinematic mode is on while in this scene
        GameMode.IsCinematic = true;
        Time.timeScale = 1f;
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
