//using Cinemachine;
using System.Collections;
using UnityEngine;

public class CinematicCamSwitcher : MonoBehaviour
{
    public MonoBehaviour[] cams; // use MonoBehaviour so this compiles even if Cinemachine assembly isn't available
    [Tooltip("Seconds between camera switches (uses real time so Time.timeScale won't pause it)")]
    public float switchInterval = 7f;

    [Tooltip("Priority assigned to the active camera (if using Cinemachine Virtual Cameras)")]
    public int activePriority = 10;
    [Tooltip("Priority assigned to inactive cameras (if using Cinemachine Virtual Cameras)")]
    public int inactivePriority = 0;

    int idx = 0;

    void Start()
    {
        if (cams == null || cams.Length == 0)
        {
            Debug.LogWarning("[CinematicCamSwitcher] No cameras assigned.");
            return;
        }
        Debug.Log($"[CinematicCamSwitcher] Starting. Cameras={cams.Length}, Interval={switchInterval}");
        StartCoroutine(LoopCams());
    }

    IEnumerator LoopCams()
    {
        while (true)
        {
            for (int i = 0; i < cams.Length; i++)
            {
                var c = cams[i];
                if (c == null) continue;

                // Try to set a 'Priority' property (Cinemachine virtual camera).
                var prop = c.GetType().GetProperty("Priority");
                if (prop != null && prop.PropertyType == typeof(int))
                {
                    prop.SetValue(c, inactivePriority, null);
                }
                else
                {
                    // Fallback: enable/disable the GameObject so non-Cinemachine setups still work.
                    c.gameObject.SetActive(false);
                }
            }

            var active = cams[idx];
            if (active != null)
            {
                var propActive = active.GetType().GetProperty("Priority");
                if (propActive != null && propActive.PropertyType == typeof(int))
                    propActive.SetValue(active, activePriority, null);
                else
                    active.gameObject.SetActive(true);
            }

            idx = (idx + 1) % cams.Length;
            yield return new WaitForSecondsRealtime(switchInterval);
        }
    }
}
