using UnityEngine;

/// <summary>
/// Drives a single crescent slash: it snaps in, sweeps a little, then fades out
/// quickly. Deliberately short and punchy rather than a lingering particle effect.
/// Destroys itself when finished.
/// </summary>
public class SlashEffect : MonoBehaviour
{
    [Header("Timing")]
    [Tooltip("Total time the slash is visible, in seconds. Short reads as snappy.")]
    [SerializeField] private float lifetime = 0.22f;

    [Header("Sweep")]
    [Tooltip("Degrees the arc rotates over its life, giving a sense of the blade travelling.")]
    [SerializeField] private float sweepDegrees = 34f;

    [Tooltip("Where in the sweep the arc starts, as a fraction of sweepDegrees before centre.")]
    [SerializeField] private float sweepStartBias = 0.55f;

    [Header("Scale")]
    [SerializeField] private float startScale = 0.86f;
    [SerializeField] private float endScale = 1.08f;

    [Header("Fade")]
    [Tooltip("Fraction of the lifetime held at full brightness before fading.")]
    [SerializeField, Range(0f, 1f)] private float holdFraction = 0.25f;

    [Header("Cleanup")]
    [Tooltip("Extra time kept alive after the blade fades, so child particles can finish before the object is destroyed.")]
    [SerializeField] private float childLinger = 0.7f;

    private MeshRenderer meshRenderer;
    private MaterialPropertyBlock propertyBlock;
    private float elapsed;
    private Quaternion baseRotation;
    private Vector3 baseScale;
    private Color baseColor = Color.white;
    private bool started;
    private bool destroyScheduled;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

private void Start()
    {
        // Captured in Start rather than Awake: the effect is parented to the player
        // immediately after being instantiated, and localRotation is only meaningful
        // once that parenting has happened.
        meshRenderer = GetComponentInChildren<MeshRenderer>();
        propertyBlock = new MaterialPropertyBlock();

        baseRotation = transform.localRotation;
        baseScale = transform.localScale;

        if (meshRenderer != null && meshRenderer.sharedMaterial != null)
        {
            if (meshRenderer.sharedMaterial.HasProperty(BaseColorId))
            {
                baseColor = meshRenderer.sharedMaterial.GetColor(BaseColorId);
            }
            else if (meshRenderer.sharedMaterial.HasProperty(ColorId))
            {
                baseColor = meshRenderer.sharedMaterial.GetColor(ColorId);
            }
        }

        started = true;
        Apply(0f);
    }

private void Update()
    {
        if (!started)
        {
            return;
        }

        elapsed += Time.deltaTime;
        float t = lifetime > 0f ? Mathf.Clamp01(elapsed / lifetime) : 1f;

        Apply(t);

        if (t >= 1f && !destroyScheduled)
        {
            destroyScheduled = true;

            // The blade is invisible by now, but child particle systems may still be
            // playing, so give them time rather than cutting them off.
            Destroy(gameObject, childLinger);
        }
    }

private void Apply(float t)
    {
        // Ease out so most of the travel happens immediately.
        float eased = 1f - Mathf.Pow(1f - t, 3f);

        transform.localRotation = baseRotation * Quaternion.Euler(
            0f,
            Mathf.Lerp(-sweepDegrees * sweepStartBias, sweepDegrees * (1f - sweepStartBias), eased),
            0f
        );

        transform.localScale = baseScale * Mathf.Lerp(startScale, endScale, eased);

        // Hold briefly at full brightness, then fade off.
        float alpha = t <= holdFraction
            ? 1f
            : 1f - Mathf.InverseLerp(holdFraction, 1f, t);
        alpha = Mathf.Clamp01(alpha);
        alpha *= alpha; // sharper tail

        Color c = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * alpha);

        if (meshRenderer == null)
        {
            return;
        }

        meshRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(BaseColorId, c);
        propertyBlock.SetColor(ColorId, c);
        meshRenderer.SetPropertyBlock(propertyBlock);
    }
}
