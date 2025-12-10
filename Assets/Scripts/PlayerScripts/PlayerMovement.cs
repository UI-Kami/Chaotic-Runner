using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float forwardSpeed = 25f;
    public float horizontalSpeed = 40f;
    private float horizontalLimit = 32f;
    public float normalSpeed = 25f;
    public float sprintSpeed = 40f;

    [Header("Jump Settings")]
    public float jumpHeight = 2.5f;
    public float gravity = -25f;

    [Header("Knockback Settings")]
    public float knockbackDecay = 8f; // how fast external horizontal velocity decays (higher = faster stop)

    private CharacterController controller;
    private Vector3 velocity;
    private bool isJumping;
    private PlayerAnimation animationScript;

    // external horizontal velocity applied by knockback (m/s)
    private Vector3 externalHorizontal = Vector3.zero;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animationScript = GetComponent<PlayerAnimation>();
    }

    void Update()
    {
        // 🛑 Prevent movement when the controller or object is inactive
        if (controller == null || !controller.enabled || !gameObject.activeInHierarchy)
            return;

        HandleMovement();
    }

    void HandleMovement()
    {
        if (controller == null || !controller.enabled)
            return;

        Vector3 move = Vector3.forward * forwardSpeed;

        bool movementLocked = animationScript != null && animationScript.IsMovementLocked();

        float horizontalInput = 0f;
        if (!movementLocked)
        {
            horizontalInput = Input.GetAxis("Horizontal");
            move += Vector3.right * horizontalInput * horizontalSpeed;
            animationScript?.SetLateral(horizontalInput);
        }
        else
        {
            animationScript?.SetLateral(0f);
        }

        // --- Jump + Gravity ---
        if (controller.isGrounded)
        {
            if (isJumping && velocity.y < 0)
                isJumping = false;

            velocity.y = -2f;

            if (Input.GetKeyDown(KeyCode.Space) &&
                animationScript != null &&
                !animationScript.IsMovementLocked())
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                isJumping = true;
            }
        }
        else
        {
            velocity.y += gravity * Time.deltaTime;
        }

        // Apply external horizontal velocity (from knockback), in world-space (m/s)
        move += externalHorizontal;

        // vertical from velocity (gravity + jump + any added upward impulse previously)
        move.y = velocity.y;

        // ✅ Safe move call
        controller.Move(move * Time.deltaTime);

        // --- Decay external horizontal velocity smoothly ---
        externalHorizontal = Vector3.MoveTowards(externalHorizontal, Vector3.zero, knockbackDecay * Time.deltaTime);

        // --- Clamp Player Horizontally ---
        Vector3 clamped = transform.position;
        clamped.x = Mathf.Clamp(clamped.x, -horizontalLimit, horizontalLimit);
        transform.position = clamped;
    }

    public float GetVerticalVelocity() => velocity.y;

    // Public API: apply an instantaneous knockback impulse to the player.
    // direction: world-space direction (Y will be ignored for horizontal part)
    // horizontalMagnitude: m/s to add horizontally
    // upImpulse: instant vertical velocity (m/s) to add
    public void ApplyKnockback(Vector3 direction, float horizontalMagnitude, float upImpulse = 0f)
    {
        if (controller == null) controller = GetComponent<CharacterController>();

        // normalize direction ignoring Y so horizontal only
        Vector3 dir = new Vector3(direction.x, 0f, direction.z);
        if (dir.sqrMagnitude < 0.0001f)
            dir = transform.forward;

        dir.Normalize();
        externalHorizontal += dir * horizontalMagnitude;

        if (upImpulse != 0f)
            velocity.y += upImpulse;
    }
}