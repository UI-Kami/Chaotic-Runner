using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.IO;

/// <summary>
/// Handles score tracking, saving, and UI updates (TextMeshPro-based)
/// Persists across scenes and sessions.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("Score Settings")]
    [SerializeField] private float score = 0f;
    [SerializeField] private float scoreRate = 10f;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI highScoreText;

    private float highScore = 0f;
    private string savePath = string.Empty;

    private void Awake()
    {
        // --- Singleton setup ---
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        savePath = Path.Combine(Application.persistentDataPath, "scoreData.json");

        LoadData();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Try to rebind UI after a scene change
        if (scoreText == null || highScoreText == null)
        {
            ScoreUIBinder binder = FindFirstObjectByType<ScoreUIBinder>(FindObjectsInactive.Include);
            if (binder != null)
                binder.BindUI(this);
        }

        UpdateUI();
    }

    private void Update()
    {
        // Only track score during gameplay
        if (GameMode.IsCinematic) return;

        score += Time.deltaTime * scoreRate;
        UpdateUI();
    }

    public void AddScore(float value)
    {
        score += value;
        UpdateUI();
    }

    public void ResetScore()
    {
        score = 0f;
        UpdateUI();
    }

    public void UpdateUI()
    {
        if (scoreText != null)
            scoreText.text = $"<b>Score:</b> {Mathf.FloorToInt(score)}";

        if (highScoreText != null)
            highScoreText.text = $"<b>High Score:</b> {Mathf.FloorToInt(highScore)}";

        if (score > highScore)
        {
            highScore = score;
            SaveData();
        }
    }

    // ----------------- Persistence -----------------
    private void SaveData()
    {
        try
        {
            ScoreData data = new ScoreData { highScore = highScore };
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(savePath, json);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ScoreManager] Error saving data: {ex.Message}");
        }
    }

    private void LoadData()
    {
        if (!File.Exists(savePath))
        {
            highScore = 0;
            return;
        }

        try
        {
            string json = File.ReadAllText(savePath);
            ScoreData data = JsonUtility.FromJson<ScoreData>(json);
            highScore = data.highScore;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ScoreManager] Error loading data: {ex.Message}");
            highScore = 0;
        }
    }

    // --- Accessors for external scripts ---
    public float CurrentScore => score;
    public float HighScore => highScore;
}
