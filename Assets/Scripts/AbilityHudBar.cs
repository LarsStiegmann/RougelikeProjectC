using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

 //_________________________________________________________________________________________________________
    //Quelle: KI (Claude Opus 5)
    //Prompt: Generate an ability hud bar for the Player UI
    //Datum: 02.09.2026
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

    [Tooltip("Frame colour for a slot the player has not filled yet.")]
    [SerializeField] private Color emptyFrameColour = new Color(0.09f, 0.10f, 0.14f, 0.4f);

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

        for (int i = slots.Count; i < PlayerAbilities.MaxAbilities; i++)
        {
            slots.Add(BuildSlot(i));
        }

        for (int i = 0; i < slots.Count; i++)
        {
            Slot slot = slots[i];

            if (i >= owned.Count)
            {
                ShowEmpty(slot);
                continue;
            }

            PlayerAbilities.OwnedAbility ability = owned[i];

      
            if (slot.definition != ability.definition)
            {
                Assign(slot, ability.definition);
            }

            slot.frame.color = frameColour;

            float charge = ability.Charge;

           
            slot.cooldownOverlay.fillAmount = 1f - charge;


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

            slot.icon.color = charge >= 1f ? Color.white : new Color(0.75f, 0.75f, 0.75f, 1f);

            if (slot.levelLabel != null)
            {
                slot.levelLabel.text = "Lv" + ability.level;
            }
        }
    }

    private void ShowEmpty(Slot slot)
    {
        slot.definition = null;
        slot.frame.color = emptyFrameColour;
        slot.icon.sprite = null;
        slot.icon.color = Color.clear;
        slot.cooldownOverlay.fillAmount = 0f;
        slot.readyFlash.color = Color.clear;
        slot.lastCharge = 0f;
        slot.flashTimer = 0f;

        if (slot.levelLabel != null)
        {
            slot.levelLabel.text = string.Empty;
        }
    }

    private void Assign(Slot slot, AbilityDefinition definition)
    {
        slot.definition = definition;

        if (definition != null && definition.icon != null)
        {
            slot.icon.sprite = definition.icon;
            slot.icon.color = Color.white;
        }
        else
        {
            slot.icon.color = Color.clear;
        }

        if (slot.root != null)
        {
            slot.root.name = "AbilitySlot_" + (definition != null ? definition.displayName : "Empty");
        }
    }

    private Slot BuildSlot(int index)
    {
        var slot = new Slot();

        GameObject go = new GameObject("AbilitySlot_Empty_" + index);
        slot.root = go.AddComponent<RectTransform>();
        slot.root.SetParent(transform, false);
        slot.root.anchorMin = new Vector2(0f, 0f);
        slot.root.anchorMax = new Vector2(0f, 0f);
        slot.root.pivot = new Vector2(0.5f, 0.5f);
        slot.root.sizeDelta = new Vector2(slotSize, slotSize);
        slot.root.anchoredPosition = firstSlotPosition + new Vector2(index * (slotSize + slotSpacing), 0f);

        slot.frame = CreateImage(slot.root, "Frame", emptyFrameColour);
        StretchFull(slot.frame.rectTransform);

        slot.icon = CreateImage(slot.root, "Icon", Color.clear);
        StretchFull(slot.icon.rectTransform, 8f);
        slot.icon.sprite = null;
        slot.icon.preserveAspect = true;

        slot.cooldownOverlay = CreateImage(slot.root, "Cooldown", cooldownColour);
        StretchFull(slot.cooldownOverlay.rectTransform, 8f);
        slot.cooldownOverlay.type = Image.Type.Filled;
        slot.cooldownOverlay.fillMethod = Image.FillMethod.Radial360;
        slot.cooldownOverlay.fillOrigin = (int)Image.Origin360.Top;
        slot.cooldownOverlay.fillClockwise = true;
        slot.cooldownOverlay.fillAmount = 0f;

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

//_________________________________________________________________________________________________________
