using UnityEngine;
using System.Collections;

[RequireComponent(typeof(PlayerAnimation))]
public class PlayerSoundManager : MonoBehaviour
{
    [Header("References")]
    public PlayerAnimation playerAnim;
    public CharacterController controller;

    [Header("Audio")]
    public AudioSource runBreathingSource;  // single sound for running + breathing

    [Header("Settings")]
    public float fadeSpeed = 4f;            // how fast to fade in/out
    public float minVolume = 0f;
    public float maxVolume = 0.9f;

    private bool wasPlaying = false;

    void Start()
    {
        if (!playerAnim) playerAnim = GetComponent<PlayerAnimation>();
        if (!controller) controller = GetComponent<CharacterController>();

        if (runBreathingSource)
        {
            runBreathingSource.loop = true;
            runBreathingSource.volume = 0f;
            runBreathingSource.Stop();
        }
    }

    void Update()
    {
        if (!controller || !playerAnim || runBreathingSource == null) return;

        bool grounded = controller.isGrounded;
        bool moving = grounded && controller.velocity.magnitude > 0.2f;

        bool shouldPlay = moving &&
                          !playerAnim.IsSliding() &&
                          !playerAnim.IsRolling() &&
                          !playerAnim.IsDead();  // add a bool for death state in your PlayerAnimation

        HandleRunningSound(shouldPlay);
    }

    void HandleRunningSound(bool shouldPlay)
    {
        if (shouldPlay)
        {
            if (!wasPlaying)
            {
                runBreathingSource.Play();
                wasPlaying = true;
            }

            runBreathingSource.volume = Mathf.Lerp(runBreathingSource.volume, maxVolume, Time.deltaTime * fadeSpeed);
            runBreathingSource.pitch = playerAnim.IsSprinting() ? 1.15f : 1.0f; // slight pitch change on sprint
        }
        else if (wasPlaying)
        {
            runBreathingSource.volume = Mathf.Lerp(runBreathingSource.volume, minVolume, Time.deltaTime * fadeSpeed);

            // Stop completely when faded out
            if (runBreathingSource.volume <= 0.05f)
            {
                runBreathingSource.Stop();
                wasPlaying = false;
            }
        }
    }
}
    