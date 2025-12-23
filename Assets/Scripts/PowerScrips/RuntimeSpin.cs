using UnityEngine;

/// <summary>
/// Small helper used at runtime to give a visual spin when the prefab has no animator or rotation.
/// </summary>
public class RuntimeSpin : MonoBehaviour
{
    public Vector3 degreesPerSec = new Vector3(0f, 180f, 0f);

    void Update()
    {
        transform.Rotate(degreesPerSec * Time.deltaTime, Space.Self);
    }
}
