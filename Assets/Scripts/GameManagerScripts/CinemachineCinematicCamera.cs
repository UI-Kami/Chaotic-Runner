using System.Collections;
using UnityEngine;
#if CINEMACHINE
using Cinemachine;
#endif

/// <summary>
/// Adapter to animate Cinemachine virtual camera lens FOV for cinematic zooms.
/// Attach this to a root object in the scene (or to a Virtual Camera) and configure the FOVs/duration.
/// SceneTransition will automatically find and use any ICinematicZoom implementer.
/// </summary>
[System.Obsolete("Cinemachine zoom adapter removed — this component is now a no-op stub.")]
public class CinemachineCinematicCamera : MonoBehaviour
{
    [Header("Cinemachine Zoom Settings (removed)")]
    public float normalFOV = 60f;
    public float zoomFOV = 30f;
    public float zoomDuration = 0.8f;

    // No-op placeholder to keep old references safe. This does not perform any zooming.
    public System.Collections.IEnumerator PlayZoom(bool toZoomed)
    {
        yield break;
    }
}