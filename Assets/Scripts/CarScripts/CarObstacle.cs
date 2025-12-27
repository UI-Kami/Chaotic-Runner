using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CarObstacle : MonoBehaviour
{
    public float pushForce = 150f;
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

        // If Test Mode is active, do not apply knockback or trigger death — just destroy the car.
        if (GameMode.IsTestMode)
        {
            Destroy(gameObject, 1f);
            return;
        }

        // Force backward-only push: use the player's backward direction
        Vector3 pushDir = -collision.gameObject.transform.forward;
        pushDir.y = 0f;
        if (pushDir.sqrMagnitude < 0.0001f)
            pushDir = (collision.gameObject.transform.position - transform.position).normalized;
        else
            pushDir.Normalize();

        float deathPushMultiplier = 1.6f;

        // Try to use PlayerMovement (CharacterController) first
        PlayerMovement pm = collision.gameObject.GetComponent<PlayerMovement>();
        if (pm != null)
        {
            pm.ApplyKnockback(pushDir, pushForce * deathPushMultiplier, liftForce);
        }
        else
        {
            // Fallback for Rigidbody-based players
            Rigidbody playerRb = collision.gameObject.GetComponent<Rigidbody>();
            if (playerRb != null)
            {
                playerRb.AddForce(pushDir * pushForce * deathPushMultiplier + Vector3.up * liftForce, ForceMode.Impulse);
            }
        }

        // slow motion on death
        TimeManager.Instance?.TriggerSlowMotion(1.5f);

        // 💀 Trigger death (after pushing)
        playerAnim?.TriggerDeath();

        Destroy(gameObject, 1f);
    }
}