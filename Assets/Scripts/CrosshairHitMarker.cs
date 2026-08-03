using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sits on the centre-screen crosshair dot and gives feedback when the player
/// actually lands a hit: the dot briefly punches up in scale and flashes colour.
/// </summary>
public class CrosshairHitMarker : MonoBehaviour
{
    public static CrosshairHitMarker Instance { get; private set; }

    [Header("Appearance")]
    [SerializeField] private Image dot;
    [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.85f);
    [SerializeField] private Color hitColor = new Color(1f, 0.25f, 0.2f, 1f);

    [Header("Feedback")]
    [Tooltip("How much larger the dot punches on a hit.")]
    [SerializeField] private float hitScale = 2f;

    [Tooltip("How long the hit flash lasts, in seconds.")]
    [SerializeField] private float hitDuration = 0.18f;

    private RectTransform rectTransform;
    private Vector3 baseScale;
    private Coroutine flashRoutine;

    private void Awake()
    {
        Instance = this;

        rectTransform = GetComponent<RectTransform>();
        baseScale = rectTransform.localScale;

        if (dot == null)
        {
            dot = GetComponent<Image>();
        }

        if (dot != null)
        {
            dot.color = normalColor;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// Plays the hit-marker flash. Safe to call repeatedly; restarts the effect.
    /// </summary>
    public void Flash()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
        }

        flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        float elapsed = 0f;

        while (elapsed < hitDuration)
        {
            // Unscaled so the flash still resolves correctly regardless of timeScale.
            elapsed += Time.unscaledDeltaTime;
            float k = 1f - Mathf.Clamp01(elapsed / hitDuration);

            rectTransform.localScale = baseScale * Mathf.Lerp(1f, hitScale, k);

            if (dot != null)
            {
                dot.color = Color.Lerp(normalColor, hitColor, k);
            }

            yield return null;
        }

        rectTransform.localScale = baseScale;

        if (dot != null)
        {
            dot.color = normalColor;
        }

        flashRoutine = null;
    }
}
