using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class DrunkDriverAI : MonoBehaviour
{
    [Header("References")]
    public Transform player; // Assign manually in inspector or via spawner
    private PlayerAnimation playerAnim;

    [Header("Movement Settings")]
    public float speed = 60f;
    public float aggroRange = 100f;
    public float steerStrength = 5f;
    public float yOffset = 1.2f;
    public float despawnDistance = 150f;

    private Rigidbody rb;
    private Vector3 moveDirection;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.useGravity = false;
    }

    void Start()
    {
        playerAnim = player?.GetComponent<PlayerAnimation>();
        if (player == null)
        {
            Debug.LogWarning($"{name} has no player assigned! Please assign the player transform manually.");
            return;
        }

        // Face toward player on spawn
        Vector3 dir = (player.position - transform.position).normalized;
        transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        moveDirection = dir;
    }

    void FixedUpdate()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p)
            {
                player = p.transform;
                playerAnim = p.GetComponent<PlayerAnimation>();
            }
        }



        if (player == null) return;

        // Keep the car locked to ground level
        Vector3 pos = rb.position;
        pos.y = yOffset;
        rb.MovePosition(pos);

        // Track player if in range
        Vector3 toPlayer = player.position - transform.position;
        float distance = toPlayer.magnitude;
        if (distance < aggroRange)
        {
            Vector3 targetDir = toPlayer.normalized;
            moveDirection = Vector3.Lerp(moveDirection, targetDir, Time.fixedDeltaTime * steerStrength);
        }

        // Apply forward movement
        rb.linearVelocity = moveDirection * speed;

        // Rotate toward movement direction
        if (rb.linearVelocity.sqrMagnitude > 0.1f)
        {
            Quaternion targetRot = Quaternion.LookRotation(rb.linearVelocity.normalized, Vector3.up);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, Time.fixedDeltaTime * 50f));
        }

        // Despawn once behind the player
        if (transform.position.z < player.position.z - despawnDistance)
            Destroy(gameObject);
    }

    void OnCollisionEnter(Collision collision)
    {
        // ✅ Only react to Player or other cars
        if (!collision.gameObject.CompareTag("Player") &&
            !collision.gameObject.CompareTag("Car") &&
            !collision.gameObject.CompareTag("DrunkCar"))
            return;

        // 🚗 Explosion & impact for any collision
        ExplosionManager.Instance?.SpawnCarExplosion(transform.position);

        // 💣 Handle Player collision
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerAnimation playerAnim = collision.gameObject.GetComponent<PlayerAnimation>();

            // 🎬 If player is sprinting → special plasma explosion (like normal cars)
            if (playerAnim != null && playerAnim.IsSprinting())
            {
                ExplosionManager.Instance?.SpawnPlasmaExplosion(transform.position);
                TimeManager.Instance?.TriggerSlowMotion(2.5f);

                // 💥 Knockback effect
                Rigidbody rb = GetComponent<Rigidbody>();
                if (rb)
                    rb.AddExplosionForce(120f, collision.contacts[0].point, 15f);

                Destroy(gameObject);
                return; // Player survives sprint impact
            }

            // 💀 Normal death (non-sprinting)
            TimeManager.Instance?.TriggerSlowMotion(1.5f);

            // 🔥 Add knockback to player for stronger impact
            Rigidbody playerRb = collision.gameObject.GetComponent<Rigidbody>();
            if (playerRb != null)
            {
                Vector3 pushDir = (collision.transform.position - transform.position).normalized;
                playerRb.AddForce(pushDir * 50f + Vector3.up * 20f, ForceMode.Impulse);
            }

            // 💀 Trigger death animation
            playerAnim?.TriggerDeath();

            Destroy(gameObject, 1f);
        }
        else
        {
            // 🚙 Collision with another car or drunk car
            Destroy(gameObject, 0.5f);
        }
    }



}
