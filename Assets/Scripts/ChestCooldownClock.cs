using UnityEngine;
using UnityEngine.UI;

public class ChestCooldownClock : MonoBehaviour
{
    [Header("Placement")]
    [Tooltip("Offset from the chest, in world units.")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.15f, 0f);

    [Tooltip("Diameter of the clock face in world units.")]
    [SerializeField] private float size = 0.85f;

    [Header("Visibility")]
    [Tooltip("The player must be within this distance for the clock to show.")]
    [SerializeField] private float visibleRange = 8f;

    [Tooltip("Seconds to fade in and out.")]
    [SerializeField] private float fadeDuration = 0.25f;

    [Header("Colour")]
    [SerializeField] private Color fillColor = new Color(0.88f, 0.15f, 0.15f, 1f);
    [SerializeField] private Color backingColor = new Color(0.04f, 0.04f, 0.06f, 0.8f);
 
    [SerializeField] private Color rimColor = new Color(0.7f, 0.7f, 0.7f, 1f);

    [Header("Pie cuts")]
    [Tooltip("How many wedges the face is divided into. 0 disables the cuts.")]
    [SerializeField] private int segmentCount = 0;

    [Tooltip("Colour of the dividing lines.")]
    [SerializeField] private Color segmentColor = Color.white;

    [Tooltip("Thickness of each dividing line, in texture pixels.")]
    [SerializeField] private float segmentLineWidth = 1f;

    private Canvas canvas;
    private CanvasGroup group;
    private Image fillImage;
    private Transform player;
    private Camera cam;

    private bool active;
    private float progress = 1f;

    private static Sprite discSprite;
    private static Sprite ringSprite;
    private Sprite spokesSprite;

    private void Awake()
    {
        Build();
        SetShown(false, true);
    }

    public void Activate()
    {
        active = true;
    }

    /// <summary>Hides the clock.</summary>
    public void Deactivate()
    {
        active = false;
    }

    public void SetProgress(float remaining01)
    {
        progress = Mathf.Clamp01(remaining01);
        if (fillImage != null)
        {
            fillImage.fillAmount = progress;
        }
    }

    private void LateUpdate()
    {
        if (canvas == null)
        {
            return;
        }

        canvas.transform.position = transform.position + worldOffset;

        if (cam == null)
        {
            cam = Camera.main;
        }

        if (cam != null)
        {
            canvas.transform.rotation = Quaternion.LookRotation(
                canvas.transform.position - cam.transform.position, Vector3.up);
        }

        bool inRange = false;
        if (active)
        {
            if (player == null && PlayerState.Instance != null)
            {
                player = PlayerState.Instance.transform;
            }

            if (player != null)
            {
                inRange = (player.position - transform.position).sqrMagnitude <= visibleRange * visibleRange;
            }
        }

        SetShown(active && inRange, false);
    }

    private void SetShown(bool shown, bool instant)
    {
        if (group == null)
        {
            return;
        }

        float target = shown ? 1f : 0f;

        if (instant || fadeDuration <= 0f)
        {
            group.alpha = target;
            return;
        }

        group.alpha = Mathf.MoveTowards(group.alpha, target, Time.deltaTime / fadeDuration);
    }

    private void Build()
    {
        EnsureSprites();

        GameObject canvasGO = new GameObject("CooldownClock");
        canvasGO.transform.SetParent(transform, false);

        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        RectTransform crt = canvas.GetComponent<RectTransform>();
        crt.sizeDelta = new Vector2(100f, 100f);
        // 100 units of canvas maps onto `size` world units.
        crt.localScale = Vector3.one * (size / 100f);

        group = canvasGO.AddComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;

        MakeImage("Backing", crt, discSprite, backingColor, 108f, Image.Type.Simple);

        fillImage = MakeImage("Fill", crt, discSprite, fillColor, 88f, Image.Type.Filled);
        fillImage.fillMethod = Image.FillMethod.Radial360;
        fillImage.fillOrigin = (int)Image.Origin360.Top;
        fillImage.fillClockwise = true;
        fillImage.fillAmount = 1f;

        if (segmentCount > 1)
        {
            spokesSprite = BuildSpokesSprite(segmentCount, segmentLineWidth);
            MakeImage("Cuts", crt, spokesSprite, segmentColor, 108f, Image.Type.Simple);
        }

        MakeImage("Rim", crt, ringSprite, rimColor, 108f, Image.Type.Simple);
    }

    private Image MakeImage(string name, RectTransform parent, Sprite sprite, Color color, float px, Image.Type type)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(px, px);
        rt.anchoredPosition = Vector2.zero;

        Image img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.type = type;
        img.raycastTarget = false;
        return img;
    }

    private static Sprite BuildSpokesSprite(int count, float lineWidth)
    {
        const int res = 128;
        float centre = (res - 1) * 0.5f;
        float outer = centre - 1f;
        float step = Mathf.PI * 2f / count;

        Texture2D tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float dx = x - centre;
                float dy = y - centre;
                float d = Mathf.Sqrt(dx * dx + dy * dy);

                if (d > outer || d < 0.5f)
                {
                    tex.SetPixel(x, y, Color.clear);
                    continue;
                }

                float angle = Mathf.Atan2(dx, dy);
                if (angle < 0f) angle += Mathf.PI * 2f;

                float nearest = Mathf.Repeat(angle + step * 0.5f, step) - step * 0.5f;
                float pixelsFromLine = Mathf.Abs(nearest) * d;

                float scaledWidth = lineWidth * (res / 64f);
                tex.SetPixel(x, y, pixelsFromLine <= scaledWidth * 0.5f ? Color.white : Color.clear);
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0f, 0f, res, res), new Vector2(0.5f, 0.5f), 100f);
    }

    private static void EnsureSprites()
    {
        if (discSprite != null && ringSprite != null)
        {
            return;
        }

        const int res = 64;
        float centre = (res - 1) * 0.5f;
        float outer = centre - 1f;
        float inner = outer * 0.90f;

        Texture2D disc = new Texture2D(res, res, TextureFormat.RGBA32, false);
        Texture2D ring = new Texture2D(res, res, TextureFormat.RGBA32, false);
        disc.filterMode = FilterMode.Point;
        ring.filterMode = FilterMode.Point;
        disc.wrapMode = TextureWrapMode.Clamp;
        ring.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float dx = x - centre;
                float dy = y - centre;
                float d = Mathf.Sqrt(dx * dx + dy * dy);

                disc.SetPixel(x, y, d <= outer ? Color.white : Color.clear);
                ring.SetPixel(x, y, (d <= outer && d >= inner) ? Color.white : Color.clear);
            }
        }

        disc.Apply();
        ring.Apply();

        Rect r = new Rect(0f, 0f, res, res);
        Vector2 pivot = new Vector2(0.5f, 0.5f);
        discSprite = Sprite.Create(disc, r, pivot, 100f);
        ringSprite = Sprite.Create(ring, r, pivot, 100f);
    }
}
