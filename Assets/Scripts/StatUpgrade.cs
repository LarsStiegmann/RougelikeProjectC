using UnityEngine;

public enum UpgradeType
{
    MaxHealth,
    Damage,
    AttackSpeed,
    HealthRegeneration,
    Speed,
    JumpForce,
    Armor,
    LifeSteal,
    Range,
    CritChance,
    CritDamage,
    Ability
}

public enum UpgradeRarity
{
    Common,
    Rare,
    Epic
}

[CreateAssetMenu(menuName = "Upgrades/Stat Upgrade")]
public class StatUpgrade : ScriptableObject
{
    public string upgradeName;
    [TextArea]
    public string description;

    public UpgradeType upgradeType;
    public UpgradeRarity rarity;

    public float value;

    [Tooltip("Only used when upgradeType is Ability: the ability to grant or level up.")]
    public AbilityDefinition ability;
}
