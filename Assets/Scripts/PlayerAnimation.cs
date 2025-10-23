using UnityEngine;
using UnityEngine.SceneManagement; // For restart or future handling

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

    [Header("Roll Settings")]
    public float rollCooldown = 1.2f;
    public float rollDuration = 0.6f;

    [Header("Lateral Settings")]
    public float lateralThreshold = 0.1f;

    [Header("Death Settings")]
    public float deathGameFreezeDelay = 1.2f; // Wait before pausing the game

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

    public bool IsSprinting() => isSprinting;



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

        HandleSprinting();
        HandleSlideInput();
        HandleRollInput();
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
    void HandleRollInput()
    {
        rollCooldownTimer -= Time.deltaTime;

        if (isRolling)
        {
            rollTimer -= Time.deltaTime;
            if (rollTimer <= 0f)
                isRolling = false;
            return;
        }

        if (rollCooldownTimer <= 0f && movement.GetComponent<CharacterController>().isGrounded)
        {
            if (Input.GetKeyDown(KeyCode.R))
                StartRoll();
        }
    }

    void StartRoll()
    {
        isRolling = true;
        rollTimer = rollDuration;
        rollCooldownTimer = rollCooldown;
        animator.SetTrigger("roll");
    }

    // --------------------------------------------------------------------
    void UpdateAnimation()
    {
        if (isDead) return;

        var controller = movement.GetComponent<CharacterController>();
        bool grounded = controller.isGrounded;

        if (grounded)
        {
            animator.SetBool("isJumping", false);

            if (Input.GetKeyDown(KeyCode.Space) && !isSliding && !isRolling && jumpLockTimer <= 0f)
            {
                animator.SetTrigger("jump");
                animator.SetBool("isJumping", true);
            }
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

        isDead = true;
        isSprinting = false;
        isSliding = false;
        isRolling = false;

        animator.SetTrigger("dead");

        movement.forwardSpeed = 0f;
        movement.enabled = false;

        deathTimer = 0f;
        Debug.Log("💀 Player has died!");





    }


}
