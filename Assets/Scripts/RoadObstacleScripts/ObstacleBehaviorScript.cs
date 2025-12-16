using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ObstacleBehaviorScript : MonoBehaviour
{
    [Header("Fence Jump Detection")]
    [Tooltip("Start the cast this high above the obstacle's position.")]
    [SerializeField] private float rayStartHeight = 1.0f;

    [Tooltip("How far in front of the fence to detect the player.")]
    [SerializeField] private float detectionDistance = 5f;

    [Tooltip("Minimum dot product (0..1) between fence forward and direction to player to consider it 'in-front'.")]
    [SerializeField] private float inFrontDotThreshold = 0.25f;

    [Header("Jump & Push")]
    [Tooltip("Upwards impulse (m/s) applied to the player for the fence jump.")]
    [SerializeField] private float fenceJumpUpImpulse = 8f;

    [Tooltip("Forward horizontal impulse (m/s) applied to the player for the fence jump.")]
    [SerializeField] private float fenceJumpHorizontalImpulse = 3f;

    [Header("Multi-Raycast")]
    [Tooltip("Number of rays cast outward from the fence. Increase for wider detection.")]
    [SerializeField, Range(1, 9)] private int rayCount = 3;

    [Tooltip("Total spread angle (degrees) across which rays are cast (centered on forward).")]
    [SerializeField, Range(0f, 90f)] private float raySpreadAngle = 20f;

    [Tooltip("Optional lateral width (meters) to offset the ray origins across the fence local X axis.")]
    [SerializeField] private float rayWidth = 0f;

    [Header("Cooldown")]
    [Tooltip("Cooldown (seconds) before the same fence can trigger another fence jump.")]
    [SerializeField] private float fenceJumpCooldown = 0.9f;

    [Header("Behavior")]
    [Tooltip("If true the obstacle will automatically perform the fence jump when the player is detected. If false, the obstacle only registers as 'nearby' and the player must press Space to trigger the fence jump.")]
    [SerializeField] private bool autoTriggerFenceJump = false;

    private RaycastHit hitInfo;
    private float lastTriggeredTime = -100f;
    // When slashed by a sword, ignore detection briefly to avoid clashing behaviors
    private float recentlySlashedUntil = -100f;

    void Update()
    {
        DetectPlayer();
    }

    // Public: perform the fence jump effect on the specified player (trigger animation and apply impulses)
    public void PerformFenceJump(GameObject playerObj)
    {
        if (playerObj == null) return;

        var pa = playerObj.GetComponent<PlayerAnimation>();
        var pm = playerObj.GetComponent<PlayerMovement>();
        if (pa == null || pm == null) return;

        // Trigger animation on player with randomized style
        pa.TriggerFenceJumpRandom();

        // Apply knockback/push
        Vector3 pushDir = transform.forward;
        pushDir.y = 0f;
        if (pushDir.sqrMagnitude < 0.0001f)
            pushDir = (playerObj.transform.position - transform.position).normalized;
        else
            pushDir.Normalize();

        pm.ApplyKnockback(pushDir, fenceJumpHorizontalImpulse, fenceJumpUpImpulse);
    }

    private void DetectPlayer()
    {
        // If this fence was just slashed, ignore detection until the short timer expires
        if (Time.time < recentlySlashedUntil) return;

        if (detectionDistance <= 0f) return;

        // Don't spam triggers
        if (Time.time - lastTriggeredTime < fenceJumpCooldown)
            return;

        Vector3 baseOrigin = transform.position + (transform.up * rayStartHeight);
        Vector3 forward = transform.forward;

        // Loop through rays across the spread (if rayCount == 1, center only)
        for (int i = 0; i < rayCount; i++)
        {
            float t = (rayCount == 1) ? 0f : ((float)i / (rayCount - 1)) - 0.5f; // -0.5 .. 0 .. +0.5

            // Angle offset
            float angle = t * raySpreadAngle;
            Vector3 dir = Quaternion.AngleAxis(angle, transform.up) * forward;

            // Lateral offset of origin across fence local X (-rayWidth/2 .. +rayWidth/2)
            Vector3 origin = baseOrigin + transform.right * (t * rayWidth);

            if (Physics.Raycast(origin, dir, out hitInfo, detectionDistance))
            {
                if (!hitInfo.collider.CompareTag("Player"))
                    continue;

                var playerObj = hitInfo.collider.gameObject;
                var pa = playerObj.GetComponent<PlayerAnimation>();
                var pm = playerObj.GetComponent<PlayerMovement>();

                // If essential components missing, skip
                if (pa == null || pm == null) continue;

                // Respect cinematic mode
                if (GameMode.IsCinematic) continue;

                // Only trigger when the player is "able" to fence jump
                if (pa.IsDead() || pa.IsMovementLocked()) continue;

                // Require roughly in-front of the fence (dot check)
                Vector3 toPlayer = (playerObj.transform.position - transform.position).normalized;
                float dot = Vector3.Dot(forward.normalized, toPlayer);
                if (dot < inFrontDotThreshold) continue;

                // Register that this fence is near the player (so player can press Space to fence-jump)
                pa.RegisterNearbyFence(this);

                if (autoTriggerFenceJump)
                {
                    // FIRE: trigger animation and apply impulse now (auto behavior)
                    PerformFenceJump(playerObj);
                    lastTriggeredTime = Time.time;
                    Debug.Log($"Fence jump triggered for player by {name} at time {Time.time} (ray {i + 1}/{rayCount})");
                }

                // One hit is enough per detection (either registration only or auto-trigger)
                return;
            }
        }

        // No rays hit the player — clear nearby fence registration (if any)

        // No rays hit the player — clear nearby fence registration (if any)
        // Find the player in scene and clear if this fence was registered (defensive)
        // Note: We only clear explicit registration on this fence to avoid interfering with others
        // Attempt to find the player object by tag
        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
        {
            var paClear = playerGO.GetComponent<PlayerAnimation>();
            if (paClear != null)
                paClear.ClearNearbyFence(this);
        }
    }

    // Called when an external effect (e.g., sword) slashes this obstacle. Prevents
    // immediate re-trigger and allows obstacle to be removed safely.
    public void OnSlashed(float ignoreSeconds = 0.2f)
    {
        recentlySlashedUntil = Time.time + Mathf.Max(0f, ignoreSeconds);
    }

    // Visualize the detection rays in the editor (select the fence to see indicators)
    private void OnDrawGizmosSelected()
    {
        Vector3 baseOrigin = transform.position + (transform.up * rayStartHeight);
        Vector3 forward = transform.forward;
        Gizmos.color = Color.cyan;

        for (int i = 0; i < Mathf.Max(1, rayCount); i++)
        {
            float t = (rayCount == 1) ? 0f : ((float)i / (rayCount - 1)) - 0.5f;
            float angle = t * raySpreadAngle;
            Vector3 dir = Quaternion.AngleAxis(angle, transform.up) * forward;
            Vector3 origin = baseOrigin + transform.right * (t * rayWidth);
            Vector3 end = origin + dir * Mathf.Max(0.001f, detectionDistance);

            Gizmos.DrawLine(origin, end);
            Gizmos.DrawWireSphere(end, 0.12f);
        }
    }
}