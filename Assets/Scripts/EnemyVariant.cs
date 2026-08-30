using UnityEngine;

/// <summary>
/// One kind of enemy. The Synty characters are a single rig carrying every mesh as a
/// child, so a "type" is really just which mesh child to switch on plus a stat block.
/// </summary>
[CreateAssetMenu(fileName = "EnemyVariant", menuName = "Roguelike/Enemy Variant")]
public class EnemyVariant : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Shown in logs and useful for identifying spawners in the Inspector.")]
    public string displayName = "Skeleton";

    [Tooltip("Exact name of the mesh child on the Enemy prefab to enable, " +
             "for example Character_Skeleton_Slave_01.")]
    public string meshChildName = "Character_Skeleton_Slave_01";

    [Header("Base stats")]
    [Tooltip("Health at the very start of a run, before time scaling.")]
    public float maxHealth = 20f;

    [Tooltip("Damage per hit, before RunTimerController's own time multiplier.")]
    public float damage = 15f;

    [Tooltip("XP granted on death.")]
    public float xpReward = 10f;

    [Tooltip("Coins dropped on death, spent on opening chests.")]
    public int coinReward = 5;

    [Header("Selection")]
    [Tooltip("Relative chance of being picked within its room. Higher is more common.")]
    public float weight = 10f;

    [Tooltip("Minutes into the run before this variant can appear. " +
             "Use it to hold heavies back until the player has some upgrades.")]
    public float unlockAtMinutes = 0f;

    [Tooltip("Scale applied to the model, for making heavies read as bigger.")]
    public float modelScale = 1f;
}
