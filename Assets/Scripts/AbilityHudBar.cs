using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows one slot per ability the player owns: the potion icon, a radial sweep
/// that empties as the ability recharges, and its level.
///
/// Slots are built at runtime from PlayerAbilities, so gaining an ability makes
/// a slot appear on its own. This lives on its own object under the HUD canvas
/// rather than inside PlayerHUDController.
/// </summary>
public class AbilityHudBar : MonoBehaviour
{
    [Header("Layout")]
    [SerializeField] private Vector2 firstSlotPosition = new Vector2(120f, 120f);
    [SerializeField] private float slotSize = 92f;
    [SerializeField] private float slotSpacing = 14f;

    [Header("Look")]
    [SerializeField] private Color frameColour = new Color(0.09f, 0.10f, 0.14f, 0.85f);
    [SerializeField] private Color cooldownColour = new Color(0f, 0f, 0f, 0.72f);
    [SerializeField] private Color readyFlashColour = new Color(1f, 1f, 1f, 0.55f);
    [SerializeField] private TMP_FontAsset font;

    private class Slot
    {
        public AbilityDefinition definition;
        public RectTransform root;
        public Image frame;
        public Image icon;
        public Image cooldownOverlay;
        public Image readyFlash;
        public TextMeshProUGUI levelLabel;
        public float lastCharge;
        public float flashTimer;
    }

    private readonly List<Slot> slots = new List<Slot>();
    private static Sprite whiteSprite;

    private void Update()
    {
        PlayerAbilities abilities = PlayerAbilities.Instance;
        if (abilities == null)
        {
            return;
        }

        IReadOnlyList<PlayerAbilities.OwnedAbility> owned = abilities.Owned;

        // Add a slot for anything newly acquired.
        for (int i = slots.Count; i < owned.Count; i++)
        {
            slots.Add(BuildSlot(owned[i].definition, i));
        }

        for (int i = 0; i < slots.Count && i < owned.Count; i++)
        {
            Slot slot = slots[i];
            PlayerAbilities.OwnedAbility ability = owned[i];

            float charge = ability.Charge;

            // Radial sweep: full dark at 0 charge, gone when ready.
            slot.cooldownOverlay.fillAmount = 1f - charge;

            // Flash the frame the instant it comes off cooldown.
            if (charge < slot.lastCharge - 0.2f)
            {
                slot.flashTimer = 0.35f;
            }
            slot.lastCharge = charge;

            if (slot.flashTimer > 0f)
            {
                slot.flashTimer -= Time.unscaledDeltaTime;
                Color c = readyFlashColour;
                c.a = readyFlashColour.a * Mathf.Clamp01(slot.flashTimer / 0.35f);
                slot.readyFlash.color = c;
            }
            else
            {
                slot.readyFlash.color = Color.clear;
            }

            // Icon dims slightly while recharging so ready-ness reads at a glance.
            slot.icon.color = charge >= 1f ? Color.white : new Color(0.75f, 0.75f, 0.75f, 1f);

            if (slot.levelLabel != null)
            {
                slot.levelLabel.text = "Lv" + ability.level;
            }
        }
    }

    private Slot BuildSlot(AbilityDefinition definition, int index)
    {
        var slot = new Slot { definition = definition };

        GameObject go = new GameObject("AbilitySlot_" + (definition != null ? definition.displayName : index.ToString()));
        slot.root = go.AddComponent<RectTransform>();
        slot.root.SetParent(transform, false);
        slot.root.anchorMin = new Vector2(0f, 0f);
        slot.root.anchorMax = new Vector2(0f, 0f);
        slot.root.pivot = new Vector2(0.5f, 0.5f);
        slot.root.sizeDelta = new Vector2(slotSize, slotSize);
        slot.root.anchoredPosition = firstSlotPosition + new Vector2(index * (slotSize + slotSpacing), 0f);

        slot.frame = CreateImage(slot.root, "Frame", frameColour);
        StretchFull(slot.frame.rectTransform);

        slot.icon = CreateImage(slot.root, "Icon", Color.white);
        StretchFull(slot.icon.rectTransform, 8f);
        if (definition != null && definition.icon != null)
        {
            slot.icon.sprite = definition.icon;
        }
        else
        {
            // No art yet: show the frame rather than a blank white square.
            slot.icon.color = Color.clear;
        }
        slot.icon.preserveAspect = true;

        slot.cooldownOverlay = CreateImage(slot.root, "Cooldown", cooldownColour);
        StretchFull(slot.cooldownOverlay.rectTransform, 8f);
        slot.cooldownOverlay.type = Image.Type.Filled;
        slot.cooldownOverlay.fillMethod = Image.FillMethod.Radial360;
        slot.cooldownOverlay.fillOrigin = (int)Image.Origin360.Top;
        slot.cooldownOverlay.fillClockwise = true;
        slot.cooldownOverlay.fillAmount = 1f;

        slot.readyFlash = CreateImage(slot.root, "ReadyFlash", Color.clear);
        StretchFull(slot.readyFlash.rectTransform);
        slot.readyFlash.raycastTarget = false;

        GameObject labelGo = new GameObject("Level");
        RectTransform labelRt = labelGo.AddComponent<RectTransform>();
        labelRt.SetParent(slot.root, false);
        labelRt.anchorMin = new Vector2(0f, 0f);
        labelRt.anchorMax = new Vector2(1f, 0f);
        labelRt.pivot = new Vector2(0.5f, 0f);
        labelRt.offsetMin = new Vector2(0f, 4f);
        labelRt.offsetMax = new Vector2(0f, 26f);
        slot.levelLabel = labelGo.AddComponent<TextMeshProUGUI>();
        slot.levelLabel.alignment = TextAlignmentOptions.Center;
        slot.levelLabel.fontSize = 20f;
        slot.levelLabel.color = new Color(1f, 0.85f, 0.35f);
        slot.levelLabel.raycastTarget = false;
        if (font != null)
        {
            slot.levelLabel.font = font;
        }

        return slot;
    }

    private Image CreateImage(Transform parent, string name, Color colour)
    {
        GameObject go = new GameObject(name);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        Image image = go.AddComponent<Image>();
        image.color = colour;
        image.sprite = GetWhiteSprite();
        image.raycastTarget = false;
        return image;
    }

    private static void StretchFull(RectTransform rt, float inset = 0f)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(inset, inset);
        rt.offsetMax = new Vector2(-inset, -inset);
    }

    private static Sprite GetWhiteSprite()
    {
        if (whiteSprite != null)
        {
            return whiteSprite;
        }

        Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[16];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.white;
        }
        tex.SetPixels(pixels);
        tex.Apply();

        whiteSprite = Sprite.Create(tex, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f));
        whiteSprite.name = "AbilityHudWhite";
        return whiteSprite;
    }
}
