using Unity.Cinemachine;
using System.Collections;
using UnityEngine;

public class CinematicCamSwitcher : MonoBehaviour
{
    public CinemachineCamera[] cams;
    public float switchInterval = 7f;

    int idx = 0;
    void Start()
    {
        StartCoroutine(LoopCams());
    }

    IEnumerator LoopCams()
    {
        while (true)
        {
            for (int i = 0; i < cams.Length; i++)
                cams[i].Priority = 0;
            cams[idx].Priority = 10;
            idx = (idx + 1) % cams.Length;
            yield return new WaitForSeconds(switchInterval);
        }
    }
}
