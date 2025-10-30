using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CarObstacle : MonoBehaviour
{
    public float pushForce = 50f;
    public float liftForce = 20f;

    void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;

        // In cinematic menu: don't push or kill the player
        if (GameMode.IsCinematic)
        {
            // still show the explosion VFX but do not apply forces or kill
            ExplosionManager.Instance?.SpawnCarExplosion(transform.position);
            // optionally destroy car after some seconds to keep scene clean
            Destroy(gameObject, 1f);
            return;
        }

        if (!collision.gameObject.CompareTag("Player")) return;

        PlayerAnimation playerAnim = collision.gameObject.GetComponent<PlayerAnimation>();

        // ✅ If player is sprinting, trigger plasma explosion + slow motion (player survives)
        if (playerAnim != null && playerAnim.IsSprinting())
        {
            ExplosionManager.Instance?.SpawnPlasmaExplosion(transform.position);
            TimeManager.Instance?.TriggerSlowMotion(2.5f); // 🎬 Short slow-motion impact

            // Optional: Add a knockback effect for realism
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb)
                rb.AddExplosionForce(pushForce * 2f, collision.contacts[0].point, 10f);

            // Destroy the car immediately after the explosion
            Destroy(gameObject);
            return; // Player survives in sprint mode
        }

        // 🚗 Normal collision (player NOT sprinting)
        ExplosionManager.Instance?.SpawnCarExplosion(transform.position);
        TimeManager.Instance?.TriggerSlowMotion(1.5f); // 🕒 Dramatic crash effect

        // Apply knockback to the player
        Rigidbody playerRb = collision.gameObject.GetComponent<Rigidbody>();
        if (playerRb != null)
        {
            Vector3 pushDir = (collision.transform.position - transform.position).normalized;
            playerRb.AddForce(pushDir * pushForce + Vector3.up * liftForce, ForceMode.Impulse);
        }

        // 💀 Trigger death for non-sprinting player
        playerAnim?.TriggerDeath();

        // Destroy car after short delay
        Destroy(gameObject, 1f);
    }
}
