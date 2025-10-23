using UnityEngine;

public class SkyDarkener_Builtin : MonoBehaviour
{
    [Header("References")]
    public Material skyboxMaterial;
    public Light sunLight;

    [Header("Color Settings")]
    public Color normalSkyColor = new Color(0.6f, 0.8f, 1f);
    public Color darkSkyColor = new Color(0.05f, 0.05f, 0.1f);
    public Color normalFogColor = new Color(0.7f, 0.8f, 1f);
    public Color darkFogColor = new Color(0.1f, 0.1f, 0.15f);

    [Header("Transition Settings")]
    public float transitionSpeed = 1.5f;
    public float ambientDarkMultiplier = 0.2f;

    private bool darkening;
    private float progress;
    private int activeMeteors = 0;
    private Color baseAmbient;

    void Start()
    {
        if (skyboxMaterial == null)
            skyboxMaterial = RenderSettings.skybox;

        baseAmbient = RenderSettings.ambientLight;
        if (skyboxMaterial.HasProperty("_Tint"))
            skyboxMaterial.SetColor("_Tint", normalSkyColor);

        RenderSettings.fog = true;
        RenderSettings.fogColor = normalFogColor;
    }

    void Update()
    {
        float target = darkening ? 1f : 0f;
        progress = Mathf.MoveTowards(progress, target, Time.deltaTime * transitionSpeed);

        if (skyboxMaterial && skyboxMaterial.HasProperty("_Tint"))
        {
            skyboxMaterial.SetColor("_Tint", Color.Lerp(normalSkyColor, darkSkyColor, progress));
        }

        RenderSettings.fogColor = Color.Lerp(normalFogColor, darkFogColor, progress);
        RenderSettings.ambientLight = Color.Lerp(baseAmbient, baseAmbient * ambientDarkMultiplier, progress);
    }

    public void DarkenSky() => darkening = true;

    public void RestoreSky()
    {
        if (activeMeteors <= 0)
            darkening = false;
    }

    public void RegisterMeteor()
    {
        activeMeteors++;
        darkening = true;
    }

    public void UnregisterMeteor()
    {
        activeMeteors = Mathf.Max(0, activeMeteors - 1);
        if (activeMeteors == 0)
            darkening = false;
    }
}
