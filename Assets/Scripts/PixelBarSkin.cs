using UnityEngine;
using UnityEngine.UI;



[ExecuteAlways]
public class PixelBarSkin : MonoBehaviour
{
    [System.Serializable]
    public class Bar
    {
        [Tooltip("The Slider whose Background/Fill should be pixel-skinned.")]
        public Slider slider;

        [Tooltip("Main colour of the filled portion.")]
        public Color fillColour = new Color(0.894f, 0.231f, 0.267f);

        [Tooltip("Colour of the empty track behind the fill.")]
        public Color trackColour = new Color(0.231f, 0.090f, 0.145f);

        [Tooltip("Width in pixels of one block. Lower = more, finer notches.")]
        public float notchSpacing = 14f;
    }

    [SerializeField] private Bar[] bars = new Bar[0];

    [Header("Sprites")]
    [SerializeField] private Sprite fillSprite;
    [SerializeField] private Sprite frameSprite;
    [SerializeField] private Sprite notchSprite;

    [Header("Frame")]
    [Tooltip("Tint of the hard outline drawn around each bar.")]
    [SerializeField] private Color frameColour = Color.white;

    private void OnEnable()
    {
        Apply();
    }

    private void OnValidate()
    {
#if UNITY_EDITOR

        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this != null)
            {
                Apply();
            }
        };
#endif
    }


    public void Apply()
    {
        if (bars == null)
        {
            return;
        }

        foreach (Bar bar in bars)
        {
            if (bar == null || bar.slider == null)
            {
                continue;
            }

            SkinOne(bar);
        }
    }

    private void SkinOne(Bar bar)
    {
        Transform root = bar.slider.transform;

        Transform bg = root.Find("Background");
        if (bg != null)
        {
            Image bgImage = bg.GetComponent<Image>();
            if (bgImage != null)
            {
                bgImage.sprite = fillSprite;
                bgImage.type = Image.Type.Simple;
                bgImage.color = bar.trackColour;
            }
        }

        Image fill = bar.slider.fillRect != null ? bar.slider.fillRect.GetComponent<Image>() : null;
        if (fill != null)
        {

            fill.sprite = fillSprite;
            fill.type = Image.Type.Simple;
            fill.color = bar.fillColour;

            AddOverlay(fill.rectTransform, "PixelNotches", notchSprite, Color.white, bar.notchSpacing);
        }

        AddOverlay(root as RectTransform, "PixelFrame", frameSprite, frameColour, 0f);
    }

    private void AddOverlay(RectTransform parent, string overlayName, Sprite sprite, Color colour, float tileWidth)
    {
        if (parent == null || sprite == null)
        {
            return;
        }

        Transform existing = parent.Find(overlayName);
        RectTransform rt;

        if (existing != null)
        {
            rt = existing as RectTransform;
        }
        else
        {
            GameObject go = new GameObject(overlayName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.hideFlags = HideFlags.DontSave;
            rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
        }

        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image image = rt.GetComponent<Image>();
        image.sprite = sprite;
        image.color = colour;
        image.raycastTarget = false;

        if (tileWidth > 0f)
        {
            image.type = Image.Type.Tiled;

            image.pixelsPerUnitMultiplier = 8f / Mathf.Max(1f, tileWidth);
        }
        else
        {
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 0.5f;
        }
    }
}
