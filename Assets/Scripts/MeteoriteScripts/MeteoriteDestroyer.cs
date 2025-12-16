using System.Collections;
using UnityEngine;

public class MeteoriteDestroyer : MonoBehaviour
    {
        public float DestroyDelay = 1f;
        public float fallDestroyY = -10f;
        public float pushForce = 50f;
        public float liftForce = 20f;
        public float shakeIntensity = 0.6f;
        public float shakeDuration = 0.45f;
        [Tooltip("Radius in meters to affect nearby obstacles and cars when this meteor impacts.")]
        public float areaBlastRadius = 8f;

        private bool hasImpacted = false;

        void Update()
        {
            if (transform.position.y < fallDestroyY)
                Destroy(gameObject);
        }

        private void HandleImpact()
        {
            if (hasImpacted) return;
            hasImpacted = true;

            Vector3 impactPos = transform.position;
            ExplosionManager.Instance?.SpawnMeteorExplosion(impactPos);
            if (CameraShake.Instance != null)
                CameraShake.Instance.ShakeCamera(shakeIntensity, shakeDuration);
            // Destroy nearby obstacles and cars within blast radius
            if (areaBlastRadius > 0f)
            {
                Collider[] hits = Physics.OverlapSphere(impactPos, areaBlastRadius);
                var roadObsManager = FindObjectOfType<RoadObstacles>();

                foreach (var c in hits)
                {
                    if (c == null || c.gameObject == null) continue;

                    // Destroy/return obstacles found
                    var cleanup = c.GetComponentInParent<RoadObstacles.ObstacleCleanup>();
                    if (cleanup != null)
                    {
                        // Play meteor explosion sound at obstacle (no additional VFX)
                        ExplosionSoundManager.Instance?.PlayMeteorExplosion(cleanup.transform.position);
                        // Return obstacle to pool via manager (if available)
                        if (roadObsManager != null)
                            roadObsManager.ReturnObstacleToPool(cleanup.gameObject);
                        else
                            Destroy(cleanup.gameObject);
                        continue;
                    }

                    // Destroy cars found in area
                    if (c.CompareTag("Car") || c.CompareTag("DrunkCar") || c.GetComponentInParent<DrunkDriverAI>() != null || c.GetComponentInParent<CarObstacle>() != null)
                    {
                        // Play meteor explosion sound for cars (no additional VFX)
                        ExplosionSoundManager.Instance?.PlayMeteorExplosion(c.transform.position);
                        Destroy(c.gameObject);
                        continue;
                    }
                }
            }

            Destroy(gameObject, DestroyDelay);
            
        }

        private void HandlePlayerCollision(GameObject playerObj, Collision collision = null)
        {
            if (playerObj == null) return;
            PlayerAnimation playerAnim = playerObj.GetComponent<PlayerAnimation>();

            if (playerAnim != null && playerAnim.IsSprinting())
            {
                ExplosionManager.Instance?.SpawnPlasmaExplosion(transform.position);
                TimeManager.Instance?.TriggerSlowMotion(2.5f);

                Rigidbody rb = GetComponent<Rigidbody>();
                if (rb != null)
                {
                    if (collision != null && collision.contacts.Length > 0)
                        rb.AddExplosionForce(pushForce * 2f, collision.contacts[0].point, 10f);
                    else
                    {
                        Vector3 pushDir = (transform.position - playerObj.transform.position).normalized;
                        rb.AddForce(pushDir * pushForce + Vector3.up * liftForce, ForceMode.Impulse);
                    }
                }

                Destroy(gameObject);
                return;
            }

            // non-sprint: apply knockback (backward-only), slow-motion, then kill player
            PlayerMovement pm = playerObj.GetComponent<PlayerMovement>();
            if (pm != null)
            {
                // Use backward-only direction relative to player's facing
                Vector3 pushDir = -playerObj.transform.forward;
                pushDir.y = 0f;
                if (pushDir.sqrMagnitude < 0.0001f)
                    pushDir = (playerObj.transform.position - transform.position).normalized;
                else
                    pushDir.Normalize();

                float deathPushMultiplier = 1.8f;
                pm.ApplyKnockback(pushDir, pushForce * deathPushMultiplier, liftForce);
            }
            else
            {
                Rigidbody playerRb = playerObj.GetComponent<Rigidbody>();
                if (playerRb != null)
                {
                    Vector3 pushDir = -playerObj.transform.forward;
                    pushDir.y = 0f;
                    if (pushDir.sqrMagnitude < 0.0001f)
                        pushDir = (playerObj.transform.position - transform.position).normalized;
                    else
                        pushDir.Normalize();

                    float deathPushMultiplier = 1.8f;
                    playerRb.AddForce(pushDir * pushForce * deathPushMultiplier + Vector3.up * liftForce, ForceMode.Impulse);
                }
            }

            // slow motion on death
            TimeManager.Instance?.TriggerSlowMotion(1.5f);

            // normal impact visuals + kill
            HandleImpact();
            playerAnim?.TriggerDeath();
        }

        void OnCollisionEnter(Collision other)
        {
            if (other.collider.CompareTag("RedZone") || other.collider.CompareTag("House"))
            {
                HandleImpact();
                return;
            }

            if (other.collider.CompareTag("Player"))
            {
                HandlePlayerCollision(other.collider.gameObject, other);
                return;
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("RedZone") || other.CompareTag("House"))
            {
                HandleImpact();
                return;
            }

            if (other.CompareTag("Player"))
            {
                HandlePlayerCollision(other.gameObject, null);
                return;
            }
        }
    }