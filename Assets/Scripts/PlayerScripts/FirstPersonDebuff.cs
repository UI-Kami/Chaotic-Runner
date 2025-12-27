using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class FirstPersonDebuff : MonoBehaviour
{
    [Header("Camera References")]
    [Tooltip("Main gameplay camera (usually third person)")]
    public Camera mainCamera;

    [Tooltip("First-person camera (starts disabled)")]
    public Camera fpCamera;

    [Header("Transition")]
    public float transitionDuration = 0.6f;
    public float followSpeed = 12f;
    public float rotationSmooth = 14f;

    private Coroutine debuffRoutine;

    private void Awake()
    {
        // Auto-assign main camera if not set
        if (!mainCamera)
            mainCamera = Camera.main;
    }

    public void StartFirstPersonDebuff(float duration)
    {
        if (debuffRoutine != null)
            StopCoroutine(debuffRoutine);

        debuffRoutine = StartCoroutine(FirstPersonRoutine(duration));
    }

    private IEnumerator FirstPersonRoutine(float duration)
    {
        if (!mainCamera || !fpCamera)
        {
            Debug.LogWarning("FirstPersonDebuff: Camera references not assigned.");
            yield break;
        }

        Transform mainT = mainCamera.transform;
        Transform fpT = fpCamera.transform;

        // Cache original transform
        Vector3 originalPos = mainT.position;
        Quaternion originalRot = mainT.rotation;

        // === ENABLE FP CAMERA ===
        fpCamera.gameObject.SetActive(true);

        // === TRANSITION TO FP CAMERA ===
        float t = 0f;
        while (t < transitionDuration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Clamp01(t / transitionDuration);

            mainT.position = Vector3.Lerp(originalPos, fpT.position, alpha);
            mainT.rotation = Quaternion.Slerp(originalRot, fpT.rotation, alpha);

            yield return null;
        }

        mainT.SetPositionAndRotation(fpT.position, fpT.rotation);

        // === HOLD PHASE ===
        float elapsed = 0f;
        while (elapsed < duration)
        {
            mainT.position = Vector3.Lerp(
                mainT.position,
                fpT.position,
                Time.deltaTime * followSpeed
            );

            mainT.rotation = Quaternion.Slerp(
                mainT.rotation,
                fpT.rotation,
                Time.deltaTime * rotationSmooth
            );

            elapsed += Time.deltaTime;
            yield return null;
        }

        // === RETURN TO ORIGINAL CAMERA ===
        t = 0f;
        Vector3 returnStartPos = mainT.position;
        Quaternion returnStartRot = mainT.rotation;

        while (t < transitionDuration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Clamp01(t / transitionDuration);

            mainT.position = Vector3.Lerp(returnStartPos, originalPos, alpha);
            mainT.rotation = Quaternion.Slerp(returnStartRot, originalRot, alpha);

            yield return null;
        }

        mainT.SetPositionAndRotation(originalPos, originalRot);

        // === DISABLE FP CAMERA AGAIN ===
        fpCamera.gameObject.SetActive(false);

        debuffRoutine = null;
    }
}
