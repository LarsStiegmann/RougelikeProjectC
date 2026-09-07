using UnityEngine;

/// <summary>
/// The shape an ability takes when it fires. Each kind is handled by
/// PlayerAbilities.Execute.
/// </summary>
public enum AbilityKind
{
    FrostNova,
    OrbitingOrbs,
    HomingBolts
}

/// <summary>
/// Tuning data for one auto-cast ability.
///
/// Abilities are granted through the normal reward pipeline: a StatUpgrade of
/// type Ability points at one of these, so a potion (or any other upgrade
/// source) can hand it to the player without any new plumbing.
///
/// Rolling the same ability again levels it up instead of being wasted.
/// </summary>
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

    [Header("Progression")]
    public int maxLevel = 5;

    [Header("Look")]
    [Tooltip("Tint used for the burst particles and flash light.")]
    public Color effectColour = new Color(0.45f, 0.85f, 1f);

    public float CooldownAt(int level)
    {
        float reduction = 1f - cooldownReductionPerLevel * (level - 1);
        return Mathf.Max(minimumCooldown, baseCooldown * Mathf.Max(0.1f, reduction));
    }

    public float DamageMultiplierAt(int level)
    {
        return baseDamageMultiplier + damageGrowthPerLevel * (level - 1);
    }

    public float RadiusAt(int level)
    {
        return baseRadius + radiusGrowthPerLevel * (level - 1);
    }

    public int CountAt(int level)
    {
        return baseCount + (level - 1) / 2;
    }
}
