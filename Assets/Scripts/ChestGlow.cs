using UnityEngine;

/// <summary>
/// Makes an unopened chest shimmer: golden pixel sparkles drift up around it and
/// twinkle in and out, with a faint steady light underneath so it still reads in
/// the dark. Everything is built at runtime, and it shuts off once the chest is
/// opened so a shimmering chest always means "not looted yet".
/// </summary>
[RequireComponent(typeof(TreasureChest))]
public class ChestGlow : MonoBehaviour
{
    [Header("Sparkles")]
    [SerializeField] private Material sparkleMaterial;

    [Tooltip("Sparkles emitted per second.")]
    [SerializeField] private float emissionRate = 9f;

    [Tooltip("Size range of each sparkle.")]
    [SerializeField] private Vector2 sizeRange = new Vector2(0.05f, 0.13f);

    [Tooltip("How long each sparkle lives.")]
    [SerializeField] private Vector2 lifetimeRange = new Vector2(0.9f, 1.8f);

    [Tooltip("How fast sparkles drift upward.")]
    [SerializeField] private Vector2 riseSpeedRange = new Vector2(0.25f, 0.7f);

    [Tooltip("Horizontal area sparkles spawn within, roughly the chest footprint.")]
    [SerializeField] private Vector3 spawnArea = new Vector3(0.9f, 0.35f, 0.7f);

    [Tooltip("Height above the chest base that sparkles spawn around.")]
    [SerializeField] private float spawnHeight = 0.45f;

    [Header("Colour")]
    [SerializeField] private Color sparkleWarm = new Color(1f, 0.85f, 0.45f, 1f);
    [SerializeField] private Color sparkleBright = new Color(1f, 0.97f, 0.75f, 1f);

    [Header("Light")]
    [Tooltip("A faint steady light so the chest is still lit. Set to 0 for sparkles only.")]
    [SerializeField] private float lightIntensity = 0.9f;

    [SerializeField] private float lightRange = 3.5f;
    [SerializeField] private Color lightColor = new Color(1f, 0.78f, 0.35f, 1f);

    private TreasureChest chest;
    private ParticleSystem sparkles;
    private Light glowLight;
    private bool stopped;

    private void Awake()
    {
        chest = GetComponent<TreasureChest>();
        BuildSparkles();
        BuildLight();
    }

    private void Update()
    {
        if (stopped || chest == null || !chest.IsOpened)
        {
            return;
        }

        stopped = true;

        // Stop emitting but let the sparkles already in the air finish.
        if (sparkles != null)
        {
            sparkles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
        if (glowLight != null)
        {
            glowLight.enabled = false;
        }
    }

    private void BuildSparkles()
    {
        GameObject go = new GameObject("ChestSparkles");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, spawnHeight, 0f);

        sparkles = go.AddComponent<ParticleSystem>();

        var main = sparkles.main;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetimeRange.x, lifetimeRange.y);
        main.startSpeed = new ParticleSystem.MinMaxCurve(riseSpeedRange.x, riseSpeedRange.y);
        main.startSize = new ParticleSystem.MinMaxCurve(sizeRange.x, sizeRange.y);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.gravityModifier = new ParticleSystem.MinMaxCurve(-0.02f);   // slight lift
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 40;

        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(sparkleWarm, 0f), new GradientColorKey(sparkleBright, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
        main.startColor = new ParticleSystem.MinMaxGradient(grad);

        var em = sparkles.emission;
        em.rateOverTime = emissionRate;

        // spawn across the chest rather than from a single point
        var shape = sparkles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = spawnArea;

        // drift sideways a touch so they do not rise in straight lines
        var vel = sparkles.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.Local;
        // All three axes must use the same MinMaxCurve mode, so Y is set explicitly
        // as a two-constant curve too even though it contributes nothing.
        vel.x = new ParticleSystem.MinMaxCurve(-0.12f, 0.12f);
        vel.y = new ParticleSystem.MinMaxCurve(0f, 0f);
        vel.z = new ParticleSystem.MinMaxCurve(-0.12f, 0.12f);

        // the shimmer itself: each sparkle swells and shrinks several times
        var sol = sparkles.sizeOverLifetime;
        sol.enabled = true;
        var keys = new Keyframe[24];
        for (int i = 0; i < keys.Length; i++)
        {
            float t = i / (float)(keys.Length - 1);
            float envelope = Mathf.Sin(Mathf.PI * t);                 // fade in and out overall
            float flicker = 0.55f + 0.45f * Mathf.Sin(t * Mathf.PI * 8f);
            keys[i] = new Keyframe(t, Mathf.Clamp01(envelope * flicker));
        }
        sol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(keys));

        var col = sparkles.colorOverLifetime;
        col.enabled = true;
        var fade = new Gradient();
        fade.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f),
                    new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
        col.color = new ParticleSystem.MinMaxGradient(fade);

        var rend = go.GetComponent<ParticleSystemRenderer>();
        rend.material = sparkleMaterial;
        rend.renderMode = ParticleSystemRenderMode.Billboard;
        rend.alignment = ParticleSystemRenderSpace.View;
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rend.receiveShadows = false;
        rend.sortingFudge = -2f;
    }

    private void BuildLight()
    {
        if (lightIntensity <= 0f)
        {
            return;
        }

        GameObject go = new GameObject("ChestGlowLight");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, spawnHeight, 0f);

        glowLight = go.AddComponent<Light>();
        glowLight.type = LightType.Point;
        glowLight.color = lightColor;
        glowLight.range = lightRange;
        glowLight.intensity = lightIntensity;   // steady, no pulsing
        glowLight.shadows = LightShadows.None;
        glowLight.renderMode = LightRenderMode.ForceVertex;
    }
}
