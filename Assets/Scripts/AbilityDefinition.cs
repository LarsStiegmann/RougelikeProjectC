using UnityEngine;

public enum AbilityKind
{
    FrostNova,
    OrbitingOrbs,
    HomingBolts
}

[CreateAssetMenu(fileName = "Ability", menuName = "Roguelike/Ability Definition")]
public class AbilityDefinition : ScriptableObject
{
    [Header("Presentation")]
    public string displayName = "Ability";

    [TextArea(2, 4)]
    public string description = "";

    public Sprite icon;

    [Header("Behaviour")]
    [Tooltip("Which effect this ability runs when it fires.")]
    public AbilityKind kind = AbilityKind.FrostNova;

    [Header("Timing")]
    [Tooltip("Seconds between casts at level 1.")]
    public float baseCooldown = 4f;

    [Tooltip("Fraction of the cooldown removed per level above 1.")]
    [Range(0f, 0.5f)]
    public float cooldownReductionPerLevel = 0.08f;

    [Tooltip("Cooldown can never drop below this.")]
    public float minimumCooldown = 0.75f;

    [Header("Damage")]
    [Tooltip("Multiplier applied to the player's current damage at level 1.")]
    public float baseDamageMultiplier = 1.2f;

    [Tooltip("Extra damage multiplier added per level above 1.")]
    public float damageGrowthPerLevel = 0.25f;

    [Header("Shape")]
    [Tooltip("Radius / reach in metres at level 1.")]
    public float baseRadius = 6f;

    [Tooltip("Extra radius per level above 1.")]
    public float radiusGrowthPerLevel = 0.4f;

    [Tooltip("Projectile or orbiter count at level 1, for abilities that use it.")]
    public int baseCount = 3;

    [Tooltip("Levels needed to gain one more projectile / orbiter.")]
    public int levelsPerExtraCount = 2;

    [Tooltip("Hard ceiling on projectiles / orbiters, so a maxed ability stays readable and cheap.")]
    public int maxCount = 8;

    [Header("Progression")]
    public int maxLevel = 20;

    [Header("Look")]
    [Tooltip("Tint used for the burst particles and flash light.")]
    public Color effectColour = new Color(0.45f, 0.85f, 1f);

    private int Steps(int level)
    {
        return Mathf.Clamp(level, 1, Mathf.Max(1, maxLevel)) - 1;
    }

    public float CooldownAt(int level)
    {
        float reduction = 1f - cooldownReductionPerLevel * Steps(level);
        return Mathf.Max(minimumCooldown, baseCooldown * Mathf.Max(0.05f, reduction));
    }

    public float DamageMultiplierAt(int level)
    {
        return baseDamageMultiplier + damageGrowthPerLevel * Steps(level);
    }

    public float RadiusAt(int level)
    {
        return baseRadius + radiusGrowthPerLevel * Steps(level);
    }

    public int CountAt(int level)
    {
        int extra = Steps(level) / Mathf.Max(1, levelsPerExtraCount);
        return Mathf.Min(Mathf.Max(1, maxCount), baseCount + extra);
    }
}
