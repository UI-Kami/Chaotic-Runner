using UnityEngine;
using UnityEngine.SceneManagement; // For restart or future handling
using System.Collections;

[RequireComponent(typeof(Animator))]
public class PlayerAnimation : MonoBehaviour
{
    [Header("References")]
    public Animator animator;
    public PlayerMovement movement;

    [Header("Sprint Settings")]
    public float sprintBoostDuration = 3f;

    [Header("Slide Settings")]
    public float slideCooldown = 1.0f;
    public float slideDuration = 0.8f;
    public float postSlideJumpDelay = 0.3f;

    // [Header("Roll Settings")]
    // public float rollCooldown = 1.2f;
    // public float rollDuration = 0.6f;

    [Header("Lateral Settings")]
    public float lateralThreshold = 0.1f;

    [Header("Death Settings")]
    public float deathGameFreezeDelay = 1.2f; // Wait before pausing the game
    [Tooltip("Delay (real seconds) before disabling PlayerMovement so knockback is visible.")]
    public float deathMovementDisableDelay = 0.18f;
    [Header("Post-Death")]
    [Tooltip("If true, load the main menu scene after the death delay.")]
    public bool loadMainMenuOnDeath = true;
    [Tooltip("Name of the main menu scene to load on death. Ensure it's included in Build Settings.")]
    public string mainMenuSceneName = "MainMenu";

    private bool isSprinting = false;
    private bool isSliding = false;
    private bool isRolling = false;
    private bool isDead = false;

    private float sprintTimer = 0f;
    private float slideTimer = 0f;
    private float slideCooldownTimer = 0f;
    private float rollTimer = 0f;
    private float rollCooldownTimer = 0f;
    private float jumpLockTimer = 0f;
    private float deathTimer = 0f;

    [Header("Stunt / Fence Jump Settings")]
    [Tooltip("Duration (seconds) of stunt invulnerability and vaulting state.")]
    public float stuntDuration = 1.2f;
    private bool isPerformingStunt = false;
    private float stuntTimer = 0f;

    // Nearby fence reference (set by obstacle when player is in detection rays)
    private ObstacleBehaviorScript nearbyFence = null;

    [Header("Fence Jump Settings")]
    [Tooltip("List of animator trigger names to pick from when performing a fence jump. If empty, uses 'fenceJump'.")]
    public string[] fenceJumpTriggers = new string[0];

    public bool IsSprinting() => isSprinting;
    public bool IsPerformingStunt() => isPerformingStunt || stuntTimer > 0f;
    public bool HasNearbyFence() => nearbyFence != null;

    public void StartStunt(float duration = -1f)
    {
        isPerformingStunt = true;
        stuntTimer = duration > 0f ? duration : stuntDuration;
    }

    void Start()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (movement == null) movement = GetComponent<PlayerMovement>();
    }

    void Update()
    {
        if (isDead)
        {
            // Slowly count down before freezing the game
            deathTimer += Time.deltaTime;
            if (deathTimer >= deathGameFreezeDelay)
                Time.timeScale = 0f; // Freeze the game after death
            return;
        }

        if (stuntTimer > 0f)
        {
            stuntTimer -= Time.deltaTime;
            if (stuntTimer <= 0f)
                isPerformingStunt = false;
        }

        HandleSprinting();
        HandleSlideInput();
        // HandleRollInput();
        UpdateAnimation();

        if (jumpLockTimer > 0f)
            jumpLockTimer -= Time.deltaTime;

        // 🔒 Keep roll locked
        var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.IsName("Roll") || stateInfo.IsName("PlayerRoll"))
            isRolling = true;
    }

    // --------------------------------------------------------------------
    void HandleSlideInput()
    {
        slideCooldownTimer -= Time.deltaTime;

        if (isSliding)
        {
            slideTimer -= Time.deltaTime;
            if (slideTimer <= 0f)
            {
                isSliding = false;
                jumpLockTimer = postSlideJumpDelay;
            }
            return;
        }

        if (slideCooldownTimer <= 0f && movement.GetComponent<CharacterController>().isGrounded)
        {
            if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.S))
                StartSlide();
        }
    }

    void StartSlide()
    {
        isSliding = true;
        slideTimer = slideDuration;
        slideCooldownTimer = slideCooldown;
        animator.SetTrigger("slide");
    }

    // --------------------------------------------------------------------
    // void HandleRollInput()
    // {
    //     rollCooldownTimer -= Time.deltaTime;

    //     if (isRolling)
    //     {
    //         rollTimer -= Time.deltaTime;
    //         if (rollTimer <= 0f)
    //             isRolling = false;
    //         return;
    //     }

    //     if (rollCooldownTimer <= 0f && movement.GetComponent<CharacterController>().isGrounded)
    //     {
    //         if (Input.GetKeyDown(KeyCode.R))
    //             StartRoll();
    //     }
    // }

    // void StartRoll()
    // {
    //     isRolling = true;
    //     rollTimer = rollDuration;
    //     rollCooldownTimer = rollCooldown;
    //     animator.SetTrigger("roll");
    // }

    // --------------------------------------------------------------------
    void UpdateAnimation()
    {
        if (isDead) return;

        var controller = movement.GetComponent<CharacterController>();
        bool grounded = controller.isGrounded;

        if (Input.GetKeyDown(KeyCode.Space) && !isSliding && !isRolling && jumpLockTimer <= 0f)
        {
            // If a nearby fence is registered, perform a fence jump regardless of grounded state
            if (nearbyFence != null && !IsMovementLocked())
            {
                var targetFence = nearbyFence;
                // Clear the nearby fence reference first so we don't accidentally re-trigger
                nearbyFence = null;
                // Invoke fence's PerformFenceJump which also triggers the animation via TriggerFenceJumpRandom
                targetFence.PerformFenceJump(gameObject);
            }
            else if (grounded)
            {
                animator.SetTrigger("jump");
                animator.SetBool("isJumping", true);
            }
        }

        if (grounded)
        {
            animator.SetBool("isJumping", false);
        }
        else
        {
            animator.SetBool("isJumping", true);
        }

        animator.SetBool("isSprinting", isSprinting);
    }

    // --------------------------------------------------------------------
    void HandleSprinting()
    {
        if (isDead) return;

        if (isSprinting)
        {
            movement.forwardSpeed = Mathf.Lerp(movement.forwardSpeed, movement.sprintSpeed, Time.deltaTime * 5f);
            sprintTimer -= Time.deltaTime;

            if (sprintTimer <= 0f)
            {
                isSprinting = false;
                animator.SetBool("isSprinting", false);
            }
        }
        else
        {
            movement.forwardSpeed = Mathf.Lerp(movement.forwardSpeed, movement.normalSpeed, Time.deltaTime * 3f);
        }
    }

    public void ActivateSprintBoost(float duration = -1f)
    {
        if (isDead) return;

        isSprinting = true;
        sprintTimer = duration > 0 ? duration : sprintBoostDuration;
        animator.SetBool("isSprinting", true);
    }

    /// <summary>
    /// Applied when the player picks up a debuff that forces first-person view for a short time.
    /// This will delegate to the FirstPersonDebuff component if present on the player.
    /// </summary>
    public void ApplyFirstPersonDebuff(float duration)
    {
        if (isDead) return;
        var deb = GetComponent<FirstPersonDebuff>();
        if (deb != null)
        {
            deb.StartFirstPersonDebuff(duration);
            Debug.Log($"PlayerAnimation: Applied first-person debuff for {duration} seconds.");
        }
        else
        {
            Debug.LogWarning("PlayerAnimation: No FirstPersonDebuff component found on player.");
        }
    }

    // --------------------------------------------------------------------
    public void SetLateral(float horizontalInput)
    {
        if (isDead) return;

        var controller = movement.GetComponent<CharacterController>();

        if (isSliding || isRolling || !controller.isGrounded)
        {
            animator.SetBool("isRunningLeft", false);
            animator.SetBool("isRunningRight", false);
            return;
        }

        if (horizontalInput <= -lateralThreshold)
        {
            animator.SetBool("isRunningLeft", true);
            animator.SetBool("isRunningRight", false);
        }
        else if (horizontalInput >= lateralThreshold)
        {
            animator.SetBool("isRunningLeft", false);
            animator.SetBool("isRunningRight", true);
        }
        else
        {
            animator.SetBool("isRunningLeft", false);
            animator.SetBool("isRunningRight", false);
        }
    }

    public void TriggerFenceJump()
    {
        if (isDead) return;

        // Prevent fence-jump while locked (sliding/rolling/etc)
        if (IsMovementLocked()) return;

        // You must add a "fenceJump" Trigger parameter in the Animator Controller
        // and create the animation transition you want there.
        if (animator != null)
        {
            TriggerFenceJumpRandom();
            Debug.Log("TriggerFenceJump called — firing randomized fence-jump trigger");
        }
    }

    public void TriggerSwordSlash()
    {
        if (isDead) return;
        if (IsMovementLocked()) return;
        if (animator == null) return;

        animator.SetTrigger("swordSlash");
        Debug.Log("TriggerSwordSlash fired");
    }

    // Choose a random fence-jump trigger name from `fenceJumpTriggers` and fire it.
    // Falls back to the classic 'fenceJump' trigger when none configured.
    public void TriggerFenceJumpRandom()
    {
        if (isDead) return;
        if (IsMovementLocked()) return;

        if (animator == null) return;

        string triggerName = "fenceJump";
        if (fenceJumpTriggers != null && fenceJumpTriggers.Length > 0)
        {
            int idx = UnityEngine.Random.Range(0, fenceJumpTriggers.Length);
            triggerName = fenceJumpTriggers[idx];
        }

        animator.SetTrigger(triggerName);
        Debug.Log($"TriggerFenceJumpRandom fired trigger '{triggerName}'");
    }

    // Called by obstacles when the player enters/maintains detection. Only stores the reference.
    public void RegisterNearbyFence(ObstacleBehaviorScript fence)
    {
        nearbyFence = fence;
    }

    // Called by obstacles when the player leaves detection for this fence. Only clears if matching.
    public void ClearNearbyFence(ObstacleBehaviorScript fence)
    {
        if (nearbyFence == fence)
            nearbyFence = null;
    }

    // --------------------------------------------------------------------
    public bool IsSliding() => isSliding;
    public bool IsRolling() => isRolling;
    public bool IsMovementLocked() => isSliding || isRolling || isDead;

    public bool IsDead() => isDead;

    // --------------------------------------------------------------------
    // 💀 Handle death
    public void TriggerDeath()
    {
        if (isDead) return;

        // If Test Mode is active we should not actually die — keep gameplay flowing for testing.
        if (GameMode.IsTestMode)
        {
            Debug.Log("Test Mode active: ignoring death.");
            return;
        }

        isDead = true;
        isSprinting = false;
        isSliding = false;
        isRolling = false;

        animator.SetTrigger("dead");

        // Let knockback be applied visually: disable movement after a short real-time delay.
        if (movement != null && deathMovementDisableDelay > 0f)
            StartCoroutine(DisableMovementAfterDelay(deathMovementDisableDelay));

        deathTimer = 0f;
        Debug.Log("💀 Player has died!");

        // Reset score back to 0 on death so the next run starts fresh
        ScoreManager.Instance?.ResetScore();

        // Optionally load main menu after the death freeze delay
        if (loadMainMenuOnDeath)
            StartCoroutine(LoadMainMenuAfterDelay(deathGameFreezeDelay + 0.2f));
    }

    private IEnumerator DisableMovementAfterDelay(float delay)
    {
        // Use real-time wait so slow-motion / timeScale changes don't affect this interval.
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, delay));

        if (movement != null)
        {
            // stop forward motion before disabling if you want immediate visual stop
            movement.forwardSpeed = 0f;
            movement.enabled = false;
        }
    }

    private bool deathSceneLoading = false;
    private IEnumerator LoadMainMenuAfterDelay(float delay)
    {
        if (deathSceneLoading) yield break;
        deathSceneLoading = true;

        // wait in real time so timeScale doesn't affect it
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, delay));

        // ensure timeScale is normal before loading
        Time.timeScale = 1f;

        // Load main menu scene after death delay (no in-game death menu integration)
        if (!string.IsNullOrEmpty(mainMenuSceneName))
        {
            Debug.Log($"Loading main menu scene '{mainMenuSceneName}' after death.");
            if (SceneTransition.Instance != null)
                SceneTransition.Instance.LoadSceneWithFade(mainMenuSceneName);
            else
                SceneManager.LoadScene(mainMenuSceneName);
        }
        else
        {
            Debug.LogWarning("Main menu scene name is empty: cannot load main menu on death.");
        }
    }
}