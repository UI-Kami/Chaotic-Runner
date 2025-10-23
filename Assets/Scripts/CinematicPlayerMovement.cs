using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
public class CinematicAutoRun : MonoBehaviour
{
    PlayerMovement pm;

    void Start()
    {
        pm = GetComponent<PlayerMovement>();
        // ensure in cinematic mode the player can't be interrupted
        GameMode.IsCinematic = true;
    }

    void Update()
    {
        // if your PlayerMovement reads horizontal input and forward speed,
        // you can simulate holding forward and slight lateral movement
        // Option A: enable PlayerMovement and leave its auto-forward logic
        // Option B: directly move the transform for a purely visual effect:
        // transform.Translate(Vector3.forward * pm.forwardSpeed * Time.deltaTime);
    }

    void OnDisable()
    {
        // when leaving cinematic, reset flag
        GameMode.IsCinematic = false;
    }
}
