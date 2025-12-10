using UnityEngine;
using System.Collections;

// --------------------------------------------------------------------
// 🎥 Global Camera Shake Manager
public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance;

    private Transform camTransform;
    private Vector3 originalPos;
    private float shakeTimeRemaining;
    private float shakeIntensity;

    void Awake()
    {
        // Singleton pattern (one instance globally)
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Find main camera
        camTransform = Camera.main != null ? Camera.main.transform : transform;
        originalPos = camTransform.localPosition;
    }

    void Update()
    {
        if (shakeTimeRemaining > 0)
        {
            camTransform.localPosition = originalPos + Random.insideUnitSphere * shakeIntensity;
            shakeTimeRemaining -= Time.deltaTime;
        }
        else if (camTransform.localPosition != originalPos)
        {
            camTransform.localPosition = Vector3.Lerp(camTransform.localPosition, originalPos, Time.deltaTime * 5f);
        }
    }

    // Call this from anywhere: CameraShake.Instance.ShakeCamera(...)
    public void ShakeCamera(float intensity, float duration)
    {
        shakeIntensity = intensity;
        shakeTimeRemaining = duration;
    }
}
