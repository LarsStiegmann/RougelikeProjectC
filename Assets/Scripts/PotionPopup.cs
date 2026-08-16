using System.Collections;
using UnityEngine;

/// <summary>
/// Pops the won potion's Synty model out of the chest: it scales in, rises while
/// spinning, hangs for a beat, then shrinks away.
///
/// Everything runs on unscaled time because the wheel pauses the game while this
/// plays. Scaling is used instead of alpha fading so the Synty opaque materials
/// are left untouched.
/// </summary>
public class PotionPopup : MonoBehaviour
{
    [SerializeField] private float riseHeight = 1.5f;
    [SerializeField] private float riseDuration = 1.1f;
    [SerializeField] private float spinSpeed = 110f;
    [SerializeField] private float growDuration = 0.28f;
    [SerializeField] private float hangDuration = 0.7f;
    [SerializeField] private float shrinkDuration = 0.35f;
    [SerializeField] private float modelScale = 1.6f;
    [SerializeField] private float lightIntensity = 2.2f;
    [SerializeField] private float lightRange = 4.5f;

    private static readonly Vector3 DefaultOffset = new Vector3(0f, 0.55f, 0f);

    /// <summary>
    /// Creates the popup for a won potion above the given anchor (the chest).
    /// </summary>
    public static void Spawn(PotionDefinition potion, Transform anchor)
    {
        if (potion == null || potion.modelPrefab == null || anchor == null)
        {
            return;
        }

        GameObject host = new GameObject("PotionPopup_" + potion.name);
        host.transform.position = anchor.position + DefaultOffset;

        PotionPopup popup = host.AddComponent<PotionPopup>();
        popup.Begin(potion);
    }

    private void Begin(PotionDefinition potion)
    {
        GameObject model = Instantiate(potion.modelPrefab, transform);
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        model.transform.localScale = Vector3.one * modelScale;

        // The prefab may ship with colliders; a floating reward should not block
        // the player or trip the trap overlap checks.
        foreach (Collider col in model.GetComponentsInChildren<Collider>(true))
        {
            col.enabled = false;
        }

        Color tint = RarityTint(potion.rarity);

        GameObject lightHost = new GameObject("Glow");
        lightHost.transform.SetParent(transform, false);
        Light glow = lightHost.AddComponent<Light>();
        glow.type = LightType.Point;
        glow.color = tint;
        glow.intensity = lightIntensity;
        glow.range = lightRange;
        glow.shadows = LightShadows.None;

        StartCoroutine(Animate(model.transform, glow));
    }

    private static Color RarityTint(PotionRarity rarity)
    {
        switch (rarity)
        {
            case PotionRarity.Epic: return new Color(0.78f, 0.45f, 1f);
            case PotionRarity.Rare: return new Color(0.45f, 0.7f, 1f);
            default: return new Color(1f, 0.92f, 0.65f);
        }
    }

    private IEnumerator Animate(Transform model, Light glow)
    {
        Vector3 basePos = transform.position;
        Vector3 fullScale = Vector3.one * modelScale;
        float total = 0f;

        // Grow in.
        float t = 0f;
        while (t < growDuration)
        {
            t += Time.unscaledDeltaTime;
            total += Time.unscaledDeltaTime;
            float n = Mathf.Clamp01(t / growDuration);
            // Overshoot slightly then settle, so it "pops".
            float s = Mathf.Sin(n * Mathf.PI * 0.5f) * 1.12f;
            model.localScale = fullScale * Mathf.Min(s, 1.12f);
            model.Rotate(Vector3.up, spinSpeed * Time.unscaledDeltaTime, Space.World);
            transform.position = basePos + Vector3.up * (riseHeight * (total / riseDuration));
            yield return null;
        }

        model.localScale = fullScale;

        // Keep rising and spinning, then hang.
        float hangTimer = 0f;
        while (hangTimer < hangDuration)
        {
            hangTimer += Time.unscaledDeltaTime;
            total += Time.unscaledDeltaTime;
            model.Rotate(Vector3.up, spinSpeed * Time.unscaledDeltaTime, Space.World);
            float riseN = Mathf.Clamp01(total / riseDuration);
            transform.position = basePos + Vector3.up * (riseHeight * Mathf.Sin(riseN * Mathf.PI * 0.5f));
            yield return null;
        }

        // Shrink away.
        t = 0f;
        float startIntensity = glow != null ? glow.intensity : 0f;
        while (t < shrinkDuration)
        {
            t += Time.unscaledDeltaTime;
            float n = Mathf.Clamp01(t / shrinkDuration);
            model.localScale = fullScale * (1f - n);
            model.Rotate(Vector3.up, spinSpeed * 2f * Time.unscaledDeltaTime, Space.World);
            transform.position += Vector3.up * (0.6f * Time.unscaledDeltaTime);
            if (glow != null)
            {
                glow.intensity = startIntensity * (1f - n);
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}
