using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class DrunkDriverAI : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    private PlayerAnimation playerAnim;

    [Header("Movement Settings")]
    public float baseSpeed = 70f;
    public float steerStrength = 8f;
    public float aggroRange = 150f;
    public float yOffset = 1.2f;
    public float swerveAmount = 3f;
    public float swerveSpeed = 4f;
    public float despawnDistance = 150f;

    [Header("Fall Cleanup Settings")]
    public float fallDestroyY = -5f; // 🔥 Below this height → destroy
    public float fallDestroyDelay = 2f;

    private Rigidbody rb;
    private float swerveTimer;
    private bool isFallingDestroyed = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = false;

        if (player == null)
            Debug.LogWarning($"⚠️ {name} has no player assigned! Assign manually in the spawner.");

        if (player != null)
            playerAnim = player.GetComponent<PlayerAnimation>();
    }

    void FixedUpdate()
    {
        if (player == null) return;

        // 🧹 Destroy if falls off map
        if (!isFallingDestroyed && transform.position.y < fallDestroyY)
        {
            isFallingDestroyed = true;
            Destroy(gameObject, fallDestroyDelay);
            return;
        }

        // 🔸 Keep car flat
        Vector3 pos = rb.position;
        pos.y = yOffset;
        rb.MovePosition(pos);

        // 🔸 Steer toward player
        Vector3 toPlayer = player.position - transform.position;
        float distance = toPlayer.magnitude;

        Vector3 targetDir = Vector3.forward;
        if (distance < aggroRange)
            targetDir = toPlayer.normalized;

        // 🔸 Wobble motion
        swerveTimer += Time.fixedDeltaTime * swerveSpeed;
        float swerveOffset = Mathf.Sin(swerveTimer) * swerveAmount;
        targetDir += transform.right * swerveOffset * 0.02f;
        targetDir.y = 0;
        targetDir.Normalize();

        // 🔸 Smooth motion
        Vector3 desiredVelocity = targetDir * baseSpeed;
        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, desiredVelocity, Time.fixedDeltaTime * steerStrength);

        if (rb.linearVelocity.sqrMagnitude > 1f)
        {
            Quaternion lookRot = Quaternion.LookRotation(rb.linearVelocity.normalized, Vector3.up);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, lookRot, Time.fixedDeltaTime * 10f));
        }

        // 🔸 Despawn if far behind
        if (transform.position.z < player.position.z - despawnDistance)
            Destroy(gameObject);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Player") &&
            !collision.gameObject.CompareTag("Car") &&
            !collision.gameObject.CompareTag("DrunkCar"))
            return;

        ExplosionManager.Instance?.SpawnCarExplosion(transform.position);

        if (collision.gameObject.CompareTag("Player"))
        {
            if (playerAnim == null)
                playerAnim = collision.gameObject.GetComponent<PlayerAnimation>();

            if (playerAnim != null)
            {
                if (playerAnim.IsSprinting())
                {
                    ExplosionManager.Instance?.SpawnPlasmaExplosion(transform.position);
                    TimeManager.Instance?.TriggerSlowMotion(2.5f);
                    Destroy(gameObject);
                    return;
                }

                Rigidbody playerRb = collision.gameObject.GetComponent<Rigidbody>();
                if (playerRb != null)
                {
                    Vector3 pushDir = (collision.transform.position - transform.position).normalized;
                    playerRb.AddForce(pushDir * 60f + Vector3.up * 20f, ForceMode.Impulse);
                }

                TimeManager.Instance?.TriggerSlowMotion(1.5f);
                playerAnim.TriggerDeath();
            }

            Destroy(gameObject, 0.5f);
        }
        else
        {
            Destroy(gameObject, 0.5f);
        }
    }
}
