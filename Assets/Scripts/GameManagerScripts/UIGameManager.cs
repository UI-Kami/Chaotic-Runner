using UnityEngine;
using UnityEngine.SceneManagement;

public class CinematicMenuUI : MonoBehaviour
{
    public string mainGameSceneName = "3d";
    public GameObject menuPanel; // can hide this when game starts

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
        SceneManager.LoadScene(mainGameSceneName);

    }

    public void OnRestartPressed()
    {
        // reload this cinematic scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
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
