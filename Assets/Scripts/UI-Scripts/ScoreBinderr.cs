using UnityEngine;
using TMPro;

/// <summary>
/// Handles connecting the TextMeshPro UI elements to the persistent ScoreManager.
/// </summary>
public class ScoreUIBinder : MonoBehaviour
{
    [Header("Assign your TextMeshProUGUI elements here")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI highScoreText;

    private void Start()
    {
        if (ScoreManager.Instance != null)
            BindUI(ScoreManager.Instance);
    }

    public void BindUI(ScoreManager manager)
    {
        manager.GetType()
               .GetField("scoreText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
               ?.SetValue(manager, scoreText);

        manager.GetType()
               .GetField("highScoreText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
               ?.SetValue(manager, highScoreText);

        manager.UpdateUI();
    }
}
