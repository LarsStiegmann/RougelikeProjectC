using UnityEngine;
using UnityEngine.UI;


public class PixelEnemyBar : MonoBehaviour
{
    [Tooltip("Ramp sprite used for the filled portion.")]
    [SerializeField] private Sprite fillSprite;

    [Tooltip("Nine-sliced hard outline drawn around the bar.")]
    [SerializeField] private Sprite frameSprite;

    [Tooltip("Tiled sprite that chops the bar into blocks.")]
    [SerializeField] private Sprite notchSprite;

    [Header("Palette")]
    [SerializeField] private Color fillColour = new Color(0.851f, 0.255f, 0.290f);
    [SerializeField] private Color trackColour = new Color(0.075f, 0.043f, 0.063f, 0.85f);
    [SerializeField] private Color frameColour = new Color(1f, 1f, 1f, 0.9f);

    [Tooltip("Canvas pixels per block. The bar canvas is 200 wide, so 25 gives 8 blocks.")]
    [SerializeField] private float notchSpacing = 25f;

    private bool styled;

    private void OnEnable()
    {
        
        styled = false;
    }

    private void LateUpdate()
    {
        if (styled)
        {
            return;
        }


        Transform canvas = transform.Find("HealthBarCanvas");
        if (canvas == null)
        {
            return;
        }

        Transform bg = canvas.Find("Background");
        Transform fill = canvas.Find("Fill");
        if (bg == null || fill == null)
        {
            return;
        }

        Image bgImage = bg.GetComponent<Image>();
        if (bgImage != null)
        {
            bgImage.sprite = fillSprite;
            bgImage.type = Image.Type.Simple;
            bgImage.color = trackColour;
        }

        Image fillImage = fill.GetComponent<Image>();
        if (fillImage != null)
        {
            fillImage.sprite = fillSprite;
            fillImage.color = fillColour;

            Overlay(fillImage.rectTransform, "PixelNotches", notchSprite, Color.white, notchSpacing);
        }

        Overlay(canvas as RectTransform, "PixelFrame", frameSprite, frameColour, 0f);

        styled = true;
    }

    private static void Overlay(RectTransform parent, string overlayName, Sprite sprite, Color colour, float tileWidth)
    {
        if (parent == null || sprite == null || parent.Find(overlayName) != null)
        {
            return;
        }

        GameObject go = new GameObject(overlayName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image image = go.GetComponent<Image>();
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
            image.pixelsPerUnitMultiplier = 1f;
        }
    }
}
