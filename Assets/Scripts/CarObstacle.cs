using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CarObstacle : MonoBehaviour
{
    public float pushForce = 50f;
    public float liftForce = 20f;
    public float fallDestroyY = -5f; // 🔥 Below this height, destroy
    public float fallDestroyDelay = 2f;

    private bool isFallingDestroyed = false;

    void Update()
    {
        // ✅ If car falls off map, destroy it after delay
        if (!isFallingDestroyed && transform.position.y < fallDestroyY)
        {
            isFallingDestroyed = true;
            Destroy(gameObject, fallDestroyDelay);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;

        // In cinematic menu: don't push or kill the player
        if (GameMode.IsCinematic)
        {
            ExplosionManager.Instance?.SpawnCarExplosion(transform.position);
            Destroy(gameObject, 1f);
            return;
        }

        PlayerAnimation playerAnim = collision.gameObject.GetComponent<PlayerAnimation>();

        // ✅ If player is sprinting → plasma explosion + survive
        if (playerAnim != null && playerAnim.IsSprinting())
        {
            ExplosionManager.Instance?.SpawnPlasmaExplosion(transform.position);
            TimeManager.Instance?.TriggerSlowMotion(2.5f);

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb)
                rb.AddExplosionForce(pushForce * 2f, collision.contacts[0].point, 10f);

            Destroy(gameObject);
            return;
        }

        // 🚗 Normal collision (non-sprinting)
        ExplosionManager.Instance?.SpawnCarExplosion(transform.position);
        TimeManager.Instance?.TriggerSlowMotion(1.5f);

        // Knockback player
        Rigidbody playerRb = collision.gameObject.GetComponent<Rigidbody>();
        if (playerRb != null)
        {
            Vector3 pushDir = (collision.transform.position - transform.position).normalized;
            playerRb.AddForce(pushDir * pushForce + Vector3.up * liftForce, ForceMode.Impulse);
        }

        // 💀 Trigger death
        playerAnim?.TriggerDeath();

        Destroy(gameObject, 1f);
    }
}
