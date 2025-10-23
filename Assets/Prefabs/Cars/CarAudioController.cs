using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class CarAudioController : MonoBehaviour
{
    public Transform player;
    public float activationDistance = 50f;
    private AudioSource carAudio;
    private bool isPlaying = false;

    void Start()
    {
        carAudio = GetComponent<AudioSource>();
        carAudio.spatialBlend = 1f; // 3D sound
        carAudio.loop = true;
        carAudio.playOnAwake = false;

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }
    }

    void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance < activationDistance)
        {
            if (!isPlaying)
            {
                carAudio.Play();
                isPlaying = true;
            }
        }
        else
        {
            if (isPlaying)
            {
                carAudio.Stop();
                isPlaying = false;
            }
        }
    }
}
