using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Component that performs simple cinematic zooms (FOV and/or position offset) on a Camera.
/// Attach to the camera you want to animate in a scene and configure the Normal/Zoom targets.
/// Use SceneTransition to automatically trigger zoom-out before loading a scene and zoom-in after.
/// </summary>
[DisallowMultipleComponent]
[System.Obsolete("CinematicCamera zooming was removed — this component is now a no-op stub.")]
public class CinematicCamera : MonoBehaviour
{
    [Header("Target Camera")]
    public Camera targetCamera; // if null, will use Camera on this GameObject or child

    [Header("FOV Settings")]
    public bool animateFOV = true;
    public float normalFOV = 60f;
    public float zoomFOV = 30f; // target FOV when "zoomed"

    [Header("Position Settings (optional)")]
    public bool animatePosition = false;
    public Vector3 zoomPositionOffset = new Vector3(0f, 0f, -10f); // offset applied to localPosition when zoomed

    [Header("Timing")]
    public float zoomDuration = 0.8f;

    // runtime caches
    private Vector3 originalLocalPosition;
    private bool initialized = false;

    void Start()
    {
        EnsureCameraReference();
    }

    void EnsureCameraReference()
    {
        if (initialized) return;
        if (targetCamera == null)
        {
            targetCamera = GetComponent<Camera>() ?? GetComponentInChildren<Camera>();
        }
        originalLocalPosition = transform.localPosition;
        initialized = true;
    }

    /// <summary>
    /// Play zoom coroutine. If toZoomed == true, moves to the Zoom targets; otherwise returns to Normal.
    /// </summary>
    public IEnumerator PlayZoom(bool toZoomed)
    {
        EnsureCameraReference();

        float startTime = Time.unscaledTime;
        float endTime = startTime + Mathf.Max(0.001f, zoomDuration);

        float startFOV = animateFOV && targetCamera != null ? targetCamera.fieldOfView : 0f;
        float targetFOV = animateFOV && targetCamera != null ? (toZoomed ? zoomFOV : normalFOV) : startFOV;

        Vector3 startPos = transform.localPosition;
        Vector3 targetPos = animatePosition ? (toZoomed ? originalLocalPosition + zoomPositionOffset : originalLocalPosition) : startPos;

        while (Time.unscaledTime < endTime)
        {
            float t = Mathf.Clamp01((Time.unscaledTime - startTime) / zoomDuration);
            if (animateFOV && targetCamera != null)
                targetCamera.fieldOfView = Mathf.Lerp(startFOV, targetFOV, t);
            if (animatePosition)
                transform.localPosition = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        if (animateFOV && targetCamera != null)
            targetCamera.fieldOfView = targetFOV;
        if (animatePosition)
            transform.localPosition = targetPos;
    }

    /// <summary>
    /// Helper: find a CinematicCamera that belongs to the active scene (not a DontDestroyOnLoad object).
    /// </summary>
    public static CinematicCamera FindInActiveScene()
    {
        var active = SceneManager.GetActiveScene();
        var roots = active.GetRootGameObjects();
        foreach (var r in roots)
        {
            var cam = r.GetComponentInChildren<CinematicCamera>(true);
            if (cam != null)
                return cam;
        }
        return null;
    }
}
