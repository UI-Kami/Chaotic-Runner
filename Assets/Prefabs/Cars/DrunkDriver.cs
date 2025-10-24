using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class DrunkDriverAI : MonoBehaviour
{
    [Header("References")]
    public Transform player;

    [Header("Behavior Settings")]
    public float speed = 60f;
    public float aggroRange = 80f;
    public float steerStrength = 8f;
    public float yOffset = 0.5f;
    public float despawnDistance = 80f;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.useGravity = true;
    }

    void FixedUpdate()
    {
        if (player == null) return;

        // Ignore vertical difference for driving
        Vector3 playerXZ = new Vector3(player.position.x, transform.position.y, player.position.z);
        Vector3 toPlayer = playerXZ - transform.position;

        // If player within aggro range → chase
        if (toPlayer.sqrMagnitude < aggroRange * aggroRange)
        {
            Vector3 desiredDir = toPlayer.normalized;
            Vector3 desiredVel = desiredDir * speed;

            // Smooth steering
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, desiredVel, Time.fixedDeltaTime * steerStrength);

            // Rotate smoothly toward velocity direction
            if (rb.linearVelocity.sqrMagnitude > 0.1f)
            {
                Quaternion lookRot = Quaternion.LookRotation(rb.linearVelocity.normalized, Vector3.up);
                rb.MoveRotation(Quaternion.Slerp(rb.rotation, lookRot, Time.fixedDeltaTime * 3f));
            }
        }
        else
        {
            // Drive straight if not chasing
            rb.linearVelocity = Vector3.back * speed * 0.8f;
        }

        // Keep car grounded with offset
        Vector3 pos = rb.position;
        pos.y = yOffset;
        rb.MovePosition(pos);

        // Despawn if too far behind
        if (transform.position.z < player.position.z - despawnDistance)
        {
            Destroy(gameObject);
        }
    }

    void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Player") || other.gameObject.CompareTag("Car") || other.gameObject.CompareTag("RedZone"))
        {
            ExplosionManager.Instance?.SpawnCarExplosion(transform.position);
            Destroy(gameObject, 0.05f);
        }
    }
}
