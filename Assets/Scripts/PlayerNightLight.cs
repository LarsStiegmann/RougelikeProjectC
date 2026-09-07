using UnityEngine;

/// <summary>
/// Keeps a light on the player so the character stays readable after dark.
/// Intensity follows how dark the world actually is (read from the ambient sky
/// colour the day-night cycle drives), so it glows at night and all but
/// disappears in daylight - no coupling to the cycle script itself.
/// </summary>
[RequireComponent(typeof(Light))]
public class PlayerNightLight : MonoBehaviour
{
    [Header("Intensity")]
    [SerializeField] private float dayIntensity = 0.35f;
    [SerializeField] private float nightIntensity = 3.4f;

    [Header("Ambient thresholds (sky colour brightness)")]
    [Tooltip("At or above this ambient brightness the light is at its day value.")]
    [SerializeField] private float brightAmbient = 0.50f;

    [Tooltip("At or below this ambient brightness the light is at its night value.")]
    [SerializeField] private float darkAmbient = 0.16f;

    [Tooltip("How quickly the light adapts, in intensity units per second.")]
    [SerializeField] private float adaptSpeed = 2.5f;

    private Light lightSource;

    private void Awake()
    {
        lightSource = GetComponent<Light>();
    }

    private void Start()
    {
        lightSource.intensity = TargetIntensity();
    }

    private void Update()
    {
        lightSource.intensity = Mathf.MoveTowards(
            lightSource.intensity,
            TargetIntensity(),
            adaptSpeed * Time.unscaledDeltaTime);
    }

    private float TargetIntensity()
    {
        float ambient = RenderSettings.ambientSkyColor.grayscale;
        float darkness = Mathf.Clamp01(Mathf.InverseLerp(brightAmbient, darkAmbient, ambient));
        return Mathf.Lerp(dayIntensity, nightIntensity, darkness);
    }
}
