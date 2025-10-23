    using UnityEngine;

public class MapLifetime : MonoBehaviour
{
    public float Lifetime = 10f;
    private float timer = 0f;
    public bool ShouldDestroy => timer >= Lifetime;

    void Update()
    {
        timer += Time.deltaTime;
    }
}
