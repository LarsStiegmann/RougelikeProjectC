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

    /// <summary>
    /// 0 in full daylight, 1 at deep night. Read from the same curve that drives
    /// the torches, so anything that lights up after dark stays in step with them.
    /// Static so per-enemy effects can read it without a scene lookup every frame.
    /// </summary>
    public static float Darkness { get; private set; } = 1f;

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

        if (smoothCurveTangents)
        {
            SmoothAll();
        }

        if (starField != null)
        {
            starRenderer = starField.GetComponentInChildren<ParticleSystemRenderer>(true);
            starBlock = new MaterialPropertyBlock();
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

    [Header("Phases")]
    [Tooltip("When enabled, the cycle loops golden afternoon -> dusk -> night -> dawn and never reaches bright midday.")]
    [SerializeField] private bool skipDay = true;

    [Header("Sun / moon handover")]
    [Tooltip("Degrees of sun elevation either side of the horizon spent blending between the sun angle and the moon angle. Larger = slower, softer handover.")]
    [SerializeField] private float handoverBand = 14f;

    [Tooltip("How far the directional light dips at the midpoint of the handover, so the swing between angles happens while it is barely casting. 0 = no dip.")]
    [Range(0f, 1f)]
    [SerializeField] private float handoverDip = 0.7f;

    [Tooltip("Round off the corners of every curve at startup so phases ease in and out instead of changing rate abruptly at each keyframe.")]
    [SerializeField] private bool smoothCurveTangents = true;

    private ParticleSystemRenderer starRenderer;
    private MaterialPropertyBlock starBlock;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int TintColorId = Shader.PropertyToID("_Color");

    private void Apply()
    {
        // With skipDay the accumulated phase maps onto t in [0.45 .. 1.08]:
        // late golden light, sunset, a long night, then dawn - midday never happens.
        float t = skipDay ? Mathf.Repeat(0.45f + timeOfDay * 0.63f, 1f) : timeOfDay;
        float elevation = Mathf.Sin(t * Mathf.PI * 2f) * 72f;

        // The light used to flip from the sun angle to a fixed moon angle the
        // instant elevation crossed the horizon, swinging every shadow in the
        // scene through 130 degrees in one frame. Now it eases across a band
        // either side of the horizon instead.
        float band = Mathf.Max(0.01f, handoverBand);
        float nightBlend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(band, -band, elevation));
        bool night = nightBlend > 0.5f;

        if (sun != null)
        {
            Quaternion sunAngle = Quaternion.Euler(Mathf.Max(elevation, -band), 330f, 0f);
            Quaternion moonAngle = Quaternion.Euler(48f, 150f, 0f);
            sun.transform.rotation = Quaternion.Slerp(sunAngle, moonAngle, nightBlend);
            sun.color = sunColor.Evaluate(t);

            // Fade the directional light down through the middle of the handover,
            // so the angle change happens while it is casting least. 0 and 1 are
            // the ends of the blend; 0.5 is the midpoint where the dip is deepest.
            float midpoint = 1f - Mathf.Abs(nightBlend * 2f - 1f);
            sun.intensity = sunIntensity.Evaluate(t) * (1f - handoverDip * midpoint);
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

        Darkness = torchFactor != null ? Mathf.Clamp01(torchFactor.Evaluate(t)) : 1f;

        if (torchLights != null)
        {
            float f = Mathf.Clamp01(torchFactor.Evaluate(t));
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

        UpdateStars(nightBlend);
    }

    /// <summary>
    /// Stars used to pop on and off with SetActive at the exact horizon crossing.
    /// They now ride the same blend as the sun-moon handover, fading in over the
    /// whole dusk band.
    /// </summary>
    private void UpdateStars(float nightBlend)
    {
        if (starField == null)
        {
            return;
        }

        bool wanted = nightBlend > 0.001f;
        if (starField.activeSelf != wanted)
        {
            starField.SetActive(wanted);
        }

        if (!wanted || starRenderer == null)
        {
            return;
        }

        // Ease the fade so stars appear gently rather than ramping in linearly.
        float a = Mathf.SmoothStep(0f, 1f, nightBlend);
        starRenderer.GetPropertyBlock(starBlock);
        starBlock.SetColor(BaseColorId, new Color(1f, 1f, 1f, a));
        starBlock.SetColor(TintColorId, new Color(1f, 1f, 1f, a));
        starRenderer.SetPropertyBlock(starBlock);
    }

    /// <summary>Rounds off the corner at every keyframe of every curve.</summary>
    private void SmoothAll()
    {
        AnimationCurve[] all =
        {
            sunIntensity, fogStartDistance, fogEndDistance, skyExposure,
            atmosphereThickness, torchFactor, bloomIntensity, bloomThreshold, postExposure
        };

        foreach (AnimationCurve c in all)
        {
            if (c == null)
            {
                continue;
            }

            for (int i = 0; i < c.length; i++)
            {
                c.SmoothTangents(i, 0f);
            }
        }
    }
}
