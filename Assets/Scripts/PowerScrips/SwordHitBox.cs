using UnityEngine;

public class SwordHitBox : MonoBehaviour
{
    [Tooltip("If set, this will be used to filter which objects can be hit.")]
    public string[] hittableTags = new string[] { "Fence", "Billboard", "LargeBillboard","WoodenFence", "Car", "DrunkCar" };

    [Tooltip("If true this hitbox will be active immediately on Start. Defaults to false so world instances are inert.")]
    public bool activeOnStart = false;

    private bool isActive = false;

    private void Start()
    {
        isActive = activeOnStart;
    }

    /// <summary>
    /// Explicitly mark this hitbox as active for the player's ring instance.
    /// </summary>
    public void ActivateForPlayer()
    {
        isActive = true;
    }

    private void HandleHit(Collider other)
    {
        if (other == null) return;

        if (other.CompareTag("Player")) return;

        // Debug hit for tracing
        Debug.Log($"SwordHitBox: Hit object={other.name} tag={other.tag} active={isActive}");

        // If this is a pooled obstacle, use its cleanup handler (it will spawn plasma and handle slow-motion)
        var cleanup = other.GetComponentInParent<RoadObstacles.ObstacleCleanup>();
        if (cleanup != null)
        {
            cleanup.HandleSlashed();
            return;
        }

        // Cars (tagged or with CarObstacle component) -> spawn plasma explosion + destroy car
        if (other.CompareTag("Car") || other.CompareTag("DrunkCar") || other.GetComponentInParent<CarObstacle>() != null)
        {
            var carObj = other.GetComponentInParent<CarObstacle>()?.gameObject ?? other.gameObject;
            ExplosionManager.Instance?.SpawnPlasmaExplosion(carObj.transform.position);
            Destroy(carObj);
            return;
        }

        // Otherwise, check tags -> spawn plasma + destroy
        foreach (var t in hittableTags)
        {
            if (other.CompareTag(t))
            {
                ExplosionManager.Instance?.SpawnPlasmaExplosion(other.transform.position);
                Destroy(other.gameObject);
                return;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Ignore hits unless this hitbox is active (owned by player) or is parented to a player
        if (!isActive)
        {
            if (GetComponentInParent<PlayerSword>() == null)
                return;
            // if parented to player, auto-activate so we don't rely on explicit call
            isActive = true;
        }

        HandleHit(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // collisions may hit non-trigger colliders — forward to the same logic
        // Only process if active or parented to a player
        if (!isActive)
        {
            if (GetComponentInParent<PlayerSword>() == null)
                return;
            isActive = true;
        }

        HandleHit(collision.collider);
    }
}
