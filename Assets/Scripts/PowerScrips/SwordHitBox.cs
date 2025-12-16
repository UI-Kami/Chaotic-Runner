using UnityEngine;

public class SwordHitBox : MonoBehaviour
{
    [Tooltip("If set, this will be used to filter which objects can be hit.")]
    public string[] hittableTags = new string[] { "Fence", "Billboard", "LargeBillboard","WoodenFence" };

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) return;

        // If this is a pooled obstacle, use its cleanup handler
        var cleanup = other.GetComponentInParent<RoadObstacles.ObstacleCleanup>();
        if (cleanup != null)
        {
            cleanup.HandleSlashed();
            return;
        }

        // Otherwise, check tags
        foreach (var t in hittableTags)
        {
            if (other.CompareTag(t))
            {
                Destroy(other.gameObject);
                return;
            }
        }
    }
}
