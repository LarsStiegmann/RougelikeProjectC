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
    CritDamage
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
}
