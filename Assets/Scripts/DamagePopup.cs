using UnityEngine;
using TMPro;

/// <summary>
/// A single floating damage number. Rises, fades out, and always faces the camera.
/// Destroys itself when finished.
/// </summary>
public class DamagePopup : MonoBehaviour
{
    [SerializeField] private TextMeshPro label;

    [Header("Motion")]
    [Tooltip("How long the number stays on screen, in seconds.")]
    [SerializeField] private float lifetime = 0.85f;

    [Tooltip("How fast the number drifts upward, in units per second.")]
    [SerializeField] private float riseSpeed = 1.6f;

    [Tooltip("Upward drift is scaled down over the lifetime so it eases to a stop.")]
    [SerializeField] private AnimationCurve riseFalloff = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    [Header("Appearance")]
    [Tooltip("Extra scale punch at the moment the number appears.")]
    [SerializeField] private float popScale = 1.35f;

    [Tooltip("Fraction of the lifetime spent on the initial pop.")]
    [SerializeField] private float popDuration = 0.18f;

    private float elapsed;
    private Vector3 baseScale;
    private Camera cam;
    private Color startColor;

    private void Awake()
    {
        if (label == null)
        {
            label = GetComponentInChildren<TextMeshPro>();
        }

        baseScale = transform.localScale;

        if (label != null)
        {
            startColor = label.color;
        }
    }

    /// <summary>
    /// Sets arbitrary text, for popups that are not plain damage numbers.
    /// </summary>
    public void Setup(string content, Color color)
    {
        if (label == null)
        {
            label = GetComponentInChildren<TextMeshPro>();
        }

        if (label != null)
        {
            label.text = content;
            label.color = color;
            startColor = color;
        }
    }

    /// <summary>
    /// Sets the number shown. Call immediately after spawning.
    /// </summary>
    public void Setup(float amount, Color color)
    {
        if (label == null)
        {
            label = GetComponentInChildren<TextMeshPro>();
        }

        if (label != null)
        {
            label.text = Mathf.Max(1, Mathf.RoundToInt(amount)).ToString();
            label.color = color;
            startColor = color;
        }
    }

    private void Update()
    {
        if (cam == null)
        {
            cam = Camera.main;
        }

        elapsed += Time.deltaTime;
        float t = lifetime > 0f ? Mathf.Clamp01(elapsed / lifetime) : 1f;

        // Drift upward, easing out.
        transform.position += Vector3.up * (riseSpeed * riseFalloff.Evaluate(t) * Time.deltaTime);

        // Face the camera so the number is readable from any angle.
        if (cam != null)
        {
            transform.rotation = cam.transform.rotation;
        }

        // Quick pop on appear, then settle back to normal size.
        float scaleMultiplier = 1f;
        if (popDuration > 0f && elapsed < popDuration)
        {
            float k = 1f - (elapsed / popDuration);
            scaleMultiplier = Mathf.Lerp(1f, popScale, k);
        }
        transform.localScale = baseScale * scaleMultiplier;

        // Fade out over the back half of the life.
        if (label != null)
        {
            float alpha = Mathf.Clamp01(1f - Mathf.InverseLerp(0.45f, 1f, t));
            label.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
        }

        if (t >= 1f)
        {
            Destroy(gameObject);
        }
    }
}
