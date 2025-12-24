using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Reusable scene transition helper. Attach to a GameObject (make a prefab for reuse) or let it be added at runtime.
/// It will create a fullscreen Canvas+Image if `overlayImage` is not assigned in the inspector.
/// Call `SceneTransition.Instance.LoadSceneWithFade(sceneName)` to load scenes with a smooth fade.
/// </summary>
[DisallowMultipleComponent]
public class SceneTransition : MonoBehaviour
{
    public static SceneTransition Instance { get; private set; }

    [Header("Fade Settings")]
    public float fadeDuration = 0.6f;
    public Color fadeColor = Color.black;

    [Header("UI (optional)")]
    public Canvas canvas;
    public Image overlayImage; // full-screen image used for fade

    [Tooltip("If true the transition GameObject will persist across scenes (useful for global transitions).")]
    public bool dontDestroyOnLoad = true;

    private Coroutine running;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);

        EnsureOverlayExists();
        SetOverlayAlpha(0f);
    }

    void EnsureOverlayExists()
    {
        if (overlayImage != null && canvas != null) return;

        // Create a fullscreen Canvas + Image if not provided
        GameObject cObj = new GameObject("SceneTransition_Canvas");
        cObj.transform.SetParent(transform, false);
        canvas = cObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        cObj.AddComponent<CanvasGroup>();

        GameObject imgObj = new GameObject("Overlay");
        imgObj.transform.SetParent(cObj.transform, false);
        overlayImage = imgObj.AddComponent<Image>();
        RectTransform rt = overlayImage.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        overlayImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);

        // Ensure the overlay object persists visually above other UI
    }

    void SetOverlayAlpha(float a)
    {
        if (overlayImage == null) return;
        Color c = overlayImage.color;
        c.r = fadeColor.r;
        c.g = fadeColor.g;
        c.b = fadeColor.b;
        c.a = Mathf.Clamp01(a);
        overlayImage.color = c;
    }


    public void LoadSceneWithFade(string sceneName)
    {
        if (running != null)
        {
            StopCoroutine(running);
            running = null;
        }
        running = StartCoroutine(DoLoadSceneWithFade(sceneName));
    }

    private IEnumerator DoLoadSceneWithFade(string sceneName)
    {
        // Fade to color
        yield return FadeTo(1f);

        // Start async load
        var op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = true;

        while (!op.isDone)
            yield return null;

        // Small frame to let the new scene settle
        yield return null;

        // Fade back in
        yield return FadeTo(0f);

        running = null;
    }

    public IEnumerator FadeTo(float targetAlpha)
    {
        if (overlayImage == null)
        {
            yield break;
        }

        float start = overlayImage.color.a;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime; // use unscaled to be unaffected by timeScale
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            SetOverlayAlpha(Mathf.Lerp(start, targetAlpha, t));
            yield return null;
        }

        SetOverlayAlpha(targetAlpha);
    }

    /// <summary>
    /// Helper to perform a custom action during a fade (fade out, execute action, fade in).
    /// </summary>
    public void FadeOutIn(Action midAction)
    {
        if (running != null)
        {
            StopCoroutine(running);
            running = null;
        }
        running = StartCoroutine(DoFadeOutIn(midAction));
    }

    private IEnumerator DoFadeOutIn(Action midAction)
    {
        yield return FadeTo(1f);
        midAction?.Invoke();
        yield return FadeTo(0f);
        running = null;
    }
}
