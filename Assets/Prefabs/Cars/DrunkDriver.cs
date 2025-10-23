using UnityEngine;

public class DrunkDriverAI : MonoBehaviour
{
    public Transform player;
    public float speed = 60f;
    public float aggroRange = 60f;
    public float steerStrength = 6f;
    public float selfDestructDelay = 3f;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.useGravity = true;
    }

    void FixedUpdate()
    {
        if (player == null) return;

        Vector3 toPlayer = (player.position - transform.position);
        if (toPlayer.magnitude < aggroRange)
        {
            Vector3 dir = toPlayer.normalized;
            Vector3 desiredVelocity = dir * speed;
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, desiredVelocity, Time.fixedDeltaTime * steerStrength);
            transform.rotation = Quaternion.LookRotation(rb.linearVelocity);
        }
        else
        {
            rb.linearVelocity = Vector3.back * speed;
        }
    }

    void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Player") || other.gameObject.CompareTag("Car"))
        {
            ExplosionManager.Instance?.SpawnCarExplosion(transform.position);
            Destroy(gameObject, 0.1f);
        }
    }
}
