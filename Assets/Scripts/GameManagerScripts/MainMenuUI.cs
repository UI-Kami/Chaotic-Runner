using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Small UI helper for the Main Menu to toggle Test Mode. Attach this to a MainMenu GameObject
/// and hook the button's OnClick to ToggleTestMode. Optionally assign a `testModeButtonText`
/// Text component to show current state.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    // Optional: assign the button's child text to show current state ("Test Mode: ON" / "OFF").
    public Text testModeButtonText;

    public void ToggleTestMode()
    {
        GameMode.IsTestMode = !GameMode.IsTestMode;
        UpdateUI();
        Debug.Log($"Test Mode set to {GameMode.IsTestMode}");
    }

    public void SetTestMode(bool enabled)
    {
        GameMode.IsTestMode = enabled;
        UpdateUI();
        Debug.Log($"Test Mode set to {GameMode.IsTestMode}");
    }

    void Start() => UpdateUI();

    void UpdateUI()
    {
        if (testModeButtonText != null)
            testModeButtonText.text = GameMode.IsTestMode ? "Test Mode: ON" : "Test Mode: OFF";
    }
}