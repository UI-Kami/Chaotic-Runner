using UnityEngine;

public class SprintPower : MonoBehaviour

{
    public float sprintDuration = 3f;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerAnimation anim = other.GetComponent<PlayerAnimation>();
            if (anim != null)
            {
                anim.ActivateSprintBoost(sprintDuration);
            }

            // TODO: Add explosion VFX here later
            Destroy(gameObject);
        }
    }
}
