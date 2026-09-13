using UnityEngine;


public enum PotionRarity
{
    Common,
    Rare,
    Epic
}

[CreateAssetMenu(fileName = "Potion", menuName = "Roguelike/Potion Definition")]
public class PotionDefinition : ScriptableObject
{
    [Header("Presentation")]
    [Tooltip("Name shown on the reel cell and in the result banner.")]
    public string displayName = "Potion";

    [Tooltip("One-line explanation of what the potion does.")]
    [TextArea(2, 4)]
    public string description = "";

    [Tooltip("Drives the reel cell colour and the default roll weight.")]
    public PotionRarity rarity = PotionRarity.Common;

    [Tooltip("Baked icon shown on the reel cell.")]
    public Sprite icon;

    [Header("Reward")]
    [Tooltip("Applied via PlayerState.ApplyUpgrade when this potion is won.")]
    public StatUpgrade upgrade;

    [Header("World model")]
    [Tooltip("Synty potion prefab that rises out of the chest when won.")]
    public GameObject modelPrefab;

    [Header("Odds")]
    [Tooltip("Relative chance of being rolled. Higher means more common. " +
             "Weights are summed across all potions, so these are ratios, not percentages.")]
    public float weight = 30f;
}
