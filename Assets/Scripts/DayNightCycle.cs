using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Simple full-scene day-night cycle for the arena. Drives the directional light
/// (sun by day, moon by night), trilight ambient, fog, the procedural skybox and
/// the torch/candle point lights. Everything is keyed off a normalised time of day:
/// 0 = dawn, 0.25 = noon, 0.5 = dusk, 0.75 = midnight.
/// </summary>
public class DayNightCycle : MonoBehaviour
{
    [Header("Timing")]
    [Tooltip("Real seconds for one full day.")]
    [SerializeField] private float dayLengthSeconds = 240f;

    [Range(0f, 1f)]
    [Tooltip("Time of day the run starts at (0 dawn, 0.25 noon, 0.5 dusk, 0.75 midnight).")]
    [SerializeField] private float startTime = 0.2f;

    [Header("Scene references")]
    [SerializeField] private Light sun;
    [SerializeField] private Material skyboxMaterial;
    [SerializeField] private GameObject starField;

    [Header("Sun / moon")]
    [SerializeField] private Gradient sunColor;
    [SerializeField] private AnimationCurve sunIntensity;

    [Header("Ambient (trilight)")]
    [SerializeField] private Gradient ambientSky;
    [SerializeField] private Gradient ambientEquator;
    [SerializeField] private Gradient ambientGround;

    [Header("Fog")]
    [SerializeField] private Gradient fogColor;
    [SerializeField] private AnimationCurve fogStartDistance;
    [SerializeField] private AnimationCurve fogEndDistance;

    [Header("Skybox")]
    [SerializeField] private AnimationCurve skyExposure;
    [SerializeField] private AnimationCurve atmosphereThickness;

    [Header("Torches")]
    [Tooltip("How strongly the arena point lights burn at midday, as a fraction of their night value.")]
    [SerializeField] private AnimationCurve torchFactor;

    [Header("Post processing")]
    [SerializeField] private UnityEngine.Rendering.Volume postVolume;
    [SerializeField] private AnimationCurve bloomIntensity;
    [SerializeField] private AnimationCurve bloomThreshold;
    [SerializeField] private AnimationCurve postExposure;

    private UnityEngine.Rendering.Universal.Bloom bloom;
    private UnityEngine.Rendering.Universal.ColorAdjustments colorAdjust;

    private float timeOfDay;
    private Light[] torchLights;
    private float[] torchBase;

    public float TimeOfDay => timeOfDay;

    private void Start()
    {
        timeOfDay = startTime;

        // Gather the arena point lights once; their serialized intensities are the night values.
        Transform lightsRoot = sun != null ? sun.transform.parent : null;
        if (lightsRoot != null)
        {
            var pts = new System.Collections.Generic.List<Light>();
            foreach (Light l in lightsRoot.GetComponentsInChildren<Light>())
            {
                if (l.type == LightType.Point)
                {
                    pts.Add(l);
                }
            }

            torchLights = pts.ToArray();
            torchBase = new float[torchLights.Length];
            for (int i = 0; i < torchLights.Length; i++)
            {
                torchBase[i] = torchLights[i].intensity;
            }
        }

        if (skyboxMaterial != null)
        {
            RenderSettings.skybox = skyboxMaterial;
        }

        if (postVolume != null)
        {
            var profile = postVolume.profile;   // runtime instance, does not touch the asset
            profile.TryGet(out bloom);
            profile.TryGet(out colorAdjust);
        }

        RenderSettings.sun = sun;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;

        Apply();
    }

    private void Update()
    {
        timeOfDay = Mathf.Repeat(timeOfDay + Time.deltaTime / dayLengthSeconds, 1f);
        Apply();
    }

    /// <summary>Jump straight to a given time of day (0..1). Handy for testing.</summary>
    public void SetTimeOfDay(float t)
    {
        timeOfDay = Mathf.Repeat(t, 1f);
        Apply();
    }

    private void Apply()
    {
        float t = timeOfDay;
        float elevation = Mathf.Sin(t * Mathf.PI * 2f) * 72f;
        bool night = elevation < 2f;

        if (sun != null)
        {
            // Below the horizon the same light flips role and becomes a fixed moon.
            sun.transform.rotation = night
                ? Quaternion.Euler(48f, 150f, 0f)
                : Quaternion.Euler(elevation, 330f, 0f);
            sun.color = sunColor.Evaluate(t);
            sun.intensity = sunIntensity.Evaluate(t);
        }

        RenderSettings.ambientSkyColor = ambientSky.Evaluate(t);
        RenderSettings.ambientEquatorColor = ambientEquator.Evaluate(t);
        RenderSettings.ambientGroundColor = ambientGround.Evaluate(t);

        RenderSettings.fogColor = fogColor.Evaluate(t);
        RenderSettings.fogStartDistance = fogStartDistance.Evaluate(t);
        RenderSettings.fogEndDistance = fogEndDistance.Evaluate(t);

        if (skyboxMaterial != null)
        {
            skyboxMaterial.SetFloat("_Exposure", skyExposure.Evaluate(t));
            skyboxMaterial.SetFloat("_AtmosphereThickness", atmosphereThickness.Evaluate(t));
        }

        if (torchLights != null)
        {
            float f = torchFactor.Evaluate(t);
            for (int i = 0; i < torchLights.Length; i++)
            {
                if (torchLights[i] != null)
                {
                    torchLights[i].intensity = torchBase[i] * f;
                }
            }
        }

        if (bloom != null)
        {
            bloom.intensity.value = bloomIntensity.Evaluate(t);
            bloom.threshold.value = bloomThreshold.Evaluate(t);
        }

        if (colorAdjust != null)
        {
            colorAdjust.postExposure.value = postExposure.Evaluate(t);
        }

        if (starField != null && starField.activeSelf != night)
        {
            starField.SetActive(night);
        }
    }
}
