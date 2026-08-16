using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Megabonk-style horizontal lottery reel. A strip of potion cells scrolls past a
/// fixed centre marker, decelerates, and lands on a weighted-random winner. The
/// winning potion's StatUpgrade is then handed to PlayerState.ApplyUpgrade.
///
/// This builds its own reel cells at runtime, so the scene only needs an empty
/// panel, a viewport and a content transform.
/// </summary>
public class PotionWheelController : MonoBehaviour
{
    public static PotionWheelController Instance { get; private set; }

    [Header("Potion pool")]
    [Tooltip("Every potion the wheel can roll.")]
    [SerializeField] private List<PotionDefinition> potions = new List<PotionDefinition>();

    [Header("UI references")]
    [SerializeField] private GameObject panel;
    [SerializeField] private RectTransform viewport;
    [SerializeField] private RectTransform reelContent;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text resultNameText;
    [SerializeField] private TMP_Text resultDescText;
    [SerializeField] private TMP_FontAsset font;

    [Header("Reel layout")]
    [SerializeField] private float cellWidth = 190f;
    [SerializeField] private float cellSpacing = 14f;
    [Tooltip("How many cells are generated in the strip. More means a longer visible scroll.")]
    [SerializeField] private int stripLength = 44;
    [Tooltip("How far from the end of the strip the winning cell sits.")]
    [SerializeField] private int winIndexFromEnd = 5;

    [Header("Timing (all unscaled, the game is paused)")]
    [SerializeField] private float spinDuration = 3.2f;
    [SerializeField] private float resultHold = 1.8f;
    [SerializeField] private float openFade = 0.25f;

    [Header("Rarity colours")]
    [SerializeField] private Color commonColour = new Color(0.55f, 0.60f, 0.66f);
    [SerializeField] private Color rareColour = new Color(0.25f, 0.55f, 0.90f);
    [SerializeField] private Color epicColour = new Color(0.68f, 0.35f, 0.90f);

    private readonly List<Image> cellFrames = new List<Image>();
    private readonly List<PotionDefinition> cellPotions = new List<PotionDefinition>();
    private bool isSpinning;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (panel != null)
        {
            canvasGroup = panel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = panel.AddComponent<CanvasGroup>();
            }

            panel.SetActive(false);
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
    /// True while a roll is in progress, so chests opened back to back queue up
    /// rather than fighting over the panel.
    /// </summary>
    public bool IsSpinning => isSpinning;

    /// <summary>
    /// Rolls a potion and shows the reel. <paramref name="spawnAnchor"/> is where the
    /// 3D potion model pops out, normally the chest that was just opened.
    /// </summary>
    public void Roll(Transform spawnAnchor)
    {
        if (isSpinning || panel == null || potions == null || potions.Count == 0)
        {
            return;
        }

        if (PlayerState.Instance != null && PlayerState.Instance.IsDead)
        {
            return;
        }

        StartCoroutine(RollRoutine(spawnAnchor));
    }

    private PotionDefinition PickWeighted()
    {
        float total = 0f;
        foreach (PotionDefinition p in potions)
        {
            if (p != null)
            {
                total += Mathf.Max(0f, p.weight);
            }
        }

        if (total <= 0f)
        {
            return potions[Random.Range(0, potions.Count)];
        }

        float roll = Random.Range(0f, total);
        foreach (PotionDefinition p in potions)
        {
            if (p == null)
            {
                continue;
            }

            roll -= Mathf.Max(0f, p.weight);
            if (roll <= 0f)
            {
                return p;
            }
        }

        return potions[potions.Count - 1];
    }

    private Color ColourFor(PotionRarity rarity)
    {
        switch (rarity)
        {
            case PotionRarity.Epic: return epicColour;
            case PotionRarity.Rare: return rareColour;
            default: return commonColour;
        }
    }

    private IEnumerator RollRoutine(Transform spawnAnchor)
    {
        isSpinning = true;

        PotionDefinition winner = PickWeighted();
        int winIndex = Mathf.Clamp(stripLength - 1 - winIndexFromEnd, 0, stripLength - 1);

        BuildStrip(winner, winIndex);

        // Pause the game and hand control to the UI map, mirroring LevelUpController.
        Time.timeScale = 0f;
        if (InputController.Instance != null && InputController.Instance.Actions != null)
        {
            InputController.Instance.Actions.Player.Disable();
            InputController.Instance.Actions.UI.Enable();
        }

        panel.SetActive(true);
        if (titleText != null)
        {
            titleText.text = "POTION FOUND";
        }

        if (resultNameText != null)
        {
            resultNameText.text = string.Empty;
        }

        if (resultDescText != null)
        {
            resultDescText.text = string.Empty;
        }

        // Fade the panel in.
        float t = 0f;
        while (t < openFade)
        {
            t += Time.unscaledDeltaTime;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Clamp01(t / openFade);
            }

            yield return null;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }

        float step = cellWidth + cellSpacing;
        float viewportWidth = viewport != null ? viewport.rect.width : 800f;

        float startX = (viewportWidth * 0.5f) - (cellWidth * 0.5f);
        // Land with a little jitter so the winner is not always dead centre.
        float jitter = Random.Range(-cellWidth * 0.28f, cellWidth * 0.28f);
        float endX = (viewportWidth * 0.5f) - (winIndex * step) - (cellWidth * 0.5f) + jitter;

        int lastCell = -1;
        t = 0f;
        while (t < spinDuration)
        {
            t += Time.unscaledDeltaTime;
            float n = Mathf.Clamp01(t / spinDuration);

            // Ease-out quart: fast launch, long slow settle onto the winner.
            float eased = 1f - Mathf.Pow(1f - n, 4f);
            float x = Mathf.Lerp(startX, endX, eased);
            reelContent.anchoredPosition = new Vector2(x, 0f);

            // Pop whichever cell is currently under the centre marker.
            int centred = Mathf.RoundToInt(((viewportWidth * 0.5f) - x - (cellWidth * 0.5f)) / step);
            if (centred != lastCell)
            {
                lastCell = centred;
                HighlightCell(centred);
            }

            yield return null;
        }

        reelContent.anchoredPosition = new Vector2(endX, 0f);
        HighlightCell(winIndex);

        // Reveal the result.
        if (resultNameText != null)
        {
            resultNameText.text = winner.displayName;
            resultNameText.color = ColourFor(winner.rarity);
        }

        if (resultDescText != null)
        {
            resultDescText.text = string.IsNullOrEmpty(winner.description)
                ? DescribeUpgrade(winner.upgrade)
                : winner.description;
        }

        // Apply the stat change through the existing public API.
        if (winner.upgrade != null && PlayerState.Instance != null)
        {
            PlayerState.Instance.ApplyUpgrade(winner.upgrade);
        }

        // Pop the 3D model out of the chest while the banner is up.
        if (winner.modelPrefab != null && spawnAnchor != null)
        {
            PotionPopup.Spawn(winner, spawnAnchor);
        }

        float hold = 0f;
        while (hold < resultHold)
        {
            hold += Time.unscaledDeltaTime;
            yield return null;
        }

        // Fade out and hand control back.
        t = 0f;
        while (t < openFade)
        {
            t += Time.unscaledDeltaTime;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f - Mathf.Clamp01(t / openFade);
            }

            yield return null;
        }

        panel.SetActive(false);
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }

        Time.timeScale = 1f;
        if (InputController.Instance != null && InputController.Instance.Actions != null)
        {
            InputController.Instance.Actions.UI.Disable();
            InputController.Instance.Actions.Player.Enable();
        }

        isSpinning = false;
    }

    private string DescribeUpgrade(StatUpgrade upgrade)
    {
        return upgrade != null ? upgrade.upgradeName : string.Empty;
    }

    private void HighlightCell(int index)
    {
        for (int i = 0; i < cellFrames.Count; i++)
        {
            if (cellFrames[i] == null)
            {
                continue;
            }

            bool centred = i == index;
            cellFrames[i].transform.localScale = centred ? Vector3.one * 1.08f : Vector3.one;
        }
    }

    private void BuildStrip(PotionDefinition winner, int winIndex)
    {
        for (int i = reelContent.childCount - 1; i >= 0; i--)
        {
            Destroy(reelContent.GetChild(i).gameObject);
        }

        cellFrames.Clear();
        cellPotions.Clear();

        float step = cellWidth + cellSpacing;
        reelContent.sizeDelta = new Vector2(stripLength * step, 0f);

        for (int i = 0; i < stripLength; i++)
        {
            PotionDefinition p = i == winIndex ? winner : PickWeighted();
            cellPotions.Add(p);
            cellFrames.Add(BuildCell(p, i, step));
        }
    }

    private Image BuildCell(PotionDefinition potion, int index, float step)
    {
        GameObject cell = new GameObject("Cell_" + index, typeof(RectTransform), typeof(Image));
        RectTransform rt = cell.GetComponent<RectTransform>();
        rt.SetParent(reelContent, false);
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.sizeDelta = new Vector2(cellWidth, cellWidth * 1.15f);
        rt.anchoredPosition = new Vector2(index * step, 0f);

        Image frame = cell.GetComponent<Image>();
        Color c = ColourFor(potion != null ? potion.rarity : PotionRarity.Common);
        frame.color = new Color(c.r * 0.35f, c.g * 0.35f, c.b * 0.35f, 0.92f);

        // Rarity edge.
        GameObject edge = new GameObject("Edge", typeof(RectTransform), typeof(Image));
        RectTransform ert = edge.GetComponent<RectTransform>();
        ert.SetParent(rt, false);
        ert.anchorMin = new Vector2(0f, 0f);
        ert.anchorMax = new Vector2(1f, 0f);
        ert.pivot = new Vector2(0.5f, 0f);
        ert.sizeDelta = new Vector2(0f, 8f);
        ert.anchoredPosition = Vector2.zero;
        edge.GetComponent<Image>().color = c;

        // Icon.
        if (potion != null && potion.icon != null)
        {
            GameObject icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            RectTransform irt = icon.GetComponent<RectTransform>();
            irt.SetParent(rt, false);
            irt.anchorMin = new Vector2(0.5f, 1f);
            irt.anchorMax = new Vector2(0.5f, 1f);
            irt.pivot = new Vector2(0.5f, 1f);
            irt.sizeDelta = new Vector2(cellWidth * 0.78f, cellWidth * 0.78f);
            irt.anchoredPosition = new Vector2(0f, -10f);

            Image img = icon.GetComponent<Image>();
            img.sprite = potion.icon;
            img.preserveAspect = true;
        }

        // Name.
        GameObject label = new GameObject("Label", typeof(RectTransform));
        RectTransform lrt = label.GetComponent<RectTransform>();
        lrt.SetParent(rt, false);
        lrt.anchorMin = new Vector2(0f, 0f);
        lrt.anchorMax = new Vector2(1f, 0f);
        lrt.pivot = new Vector2(0.5f, 0f);
        lrt.sizeDelta = new Vector2(-12f, 46f);
        lrt.anchoredPosition = new Vector2(0f, 12f);

        TextMeshProUGUI text = label.AddComponent<TextMeshProUGUI>();
        if (font != null)
        {
            text.font = font;
        }

        text.text = potion != null ? potion.displayName : "?";
        text.fontSize = 22f;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = true;
        text.color = Color.white;

        return frame;
    }
}
