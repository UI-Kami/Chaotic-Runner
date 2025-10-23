using UnityEngine;
using System.Collections;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; }

    [Header("Slow Motion Settings")]
    [Range(0f, 1f)] public float slowMotionScale = 0.2f;
    public float slowMotionDuration = 0.8f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // --------------------------------------------------------------------
    // 🎬 Trigger slow motion effect
    public void TriggerSlowMotion(float duration = -1f)
    {
        StopAllCoroutines();
        StartCoroutine(SlowMotionCoroutine(duration > 0 ? duration : slowMotionDuration));
    }

    // --------------------------------------------------------------------
    // 🕒 Smooth slow motion + audio sync
    private IEnumerator SlowMotionCoroutine(float duration)
    {
        // Enter slow motion
        Time.timeScale = slowMotionScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        // Wait in real time (unaffected by slow motion)
        yield return new WaitForSecondsRealtime(duration);

        // Gradually restore to normal time
        float elapsed = 0f;
        while (elapsed < 0.5f)
        {
            elapsed += Time.unscaledDeltaTime;

            Time.timeScale = Mathf.Lerp(slowMotionScale, 1f, elapsed / 0.5f);
            Time.fixedDeltaTime = 0.02f * Time.timeScale;

            yield return null;
        }

        // Reset to normal
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
    }
}
