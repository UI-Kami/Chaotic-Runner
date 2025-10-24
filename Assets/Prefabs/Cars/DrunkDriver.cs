using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class DrunkDriverAI : MonoBehaviour
{
    public Transform player;
    public float speed = 60f;
    public float aggroRange = 80f;
    public float steerStrength = 8f;
    public float yOffset = 0.5f;
    public float swerveAmount = 2.5f;
    public float swerveSpeed = 3f;
    public float despawnDistance = 120f;

    private Rigidbody rb;
    private float swerveTimer;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.useGravity = false; // disable gravity so car stays on track
    }

    void FixedUpdate()
    {
        if (player == null) return;

        // Horizontal drunken swerve
        swerveTimer += Time.fixedDeltaTime * swerveSpeed;
        float lateralOffset = Mathf.Sin(swerveTimer) * swerveAmount;

        Vector3 targetPos = new Vector3(player.position.x + lateralOffset, yOffset, player.position.z);
        Vector3 toPlayer = (targetPos - transform.position);
        float distance = toPlayer.magnitude;

        if (distance < aggroRange)
        {
            Vector3 desiredVel = toPlayer.normalized * speed;
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, desiredVel, Time.fixedDeltaTime * steerStrength);
        }
        else
        {
            rb.linearVelocity = Vector3.back * speed * 0.8f;
        }

        // Smooth rotation
        if (rb.linearVelocity.sqrMagnitude > 0.1f)
        {
            Quaternion lookRot = Quaternion.LookRotation(rb.linearVelocity.normalized, Vector3.up);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, lookRot, Time.fixedDeltaTime * 4f));
        }

        // Ground lock
        Vector3 pos = rb.position;
        pos.y = yOffset;
        rb.MovePosition(pos);

        // Cleanup if too far behind
        if (transform.position.z < player.position.z - despawnDistance)
            Destroy(gameObject);
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
