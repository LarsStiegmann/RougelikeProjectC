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

[CreateAssetMenu(menuName = "Upgrades/Stat Upgrade")]
public class StatUpgrade : ScriptableObject
{
    public string upgradeName;
    [TextArea]
    public string description;

    public UpgradeType upgradeType;
    public float value;
}
