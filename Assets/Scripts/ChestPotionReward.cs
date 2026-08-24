using UnityEngine;

/// <summary>
/// Bridges an opened chest to the potion lottery wheel.
///
/// Wire this component's GiveReward() into the chest's existing onOpened UnityEvent
/// in the Inspector. That keeps TreasureChest itself untouched: the event field is
/// already there and documented as "Hook rewards up here".
///
/// As a safety net, if the event was never wired the component also watches the
/// chest's public IsOpened flag and fires once on the frame it flips.
/// </summary>
[RequireComponent(typeof(TreasureChest))]
public class ChestPotionReward : MonoBehaviour
{
    [Tooltip("Where the 3D potion pops out. Defaults to this chest's transform.")]
    [SerializeField] private Transform spawnAnchor;

    [Tooltip("Fallback: poll the chest's IsOpened flag in case the onOpened event " +
             "was not wired in the Inspector. Harmless to leave on.")]
    [SerializeField] private bool pollAsFallback = true;

    private TreasureChest chest;
    private bool rewardGiven;

    private void Awake()
    {
        chest = GetComponent<TreasureChest>();
        if (spawnAnchor == null)
        {
            spawnAnchor = transform;
        }
    }

    private void Update()
    {
        if (!pollAsFallback || rewardGiven || chest == null)
        {
            return;
        }

        if (chest.IsOpened)
        {
            GiveReward();
        }
    }

    /// <summary>
    /// Re-arms this chest so the next open grants another potion. Called by
    /// ChestCooldown once the chest has closed again.
    /// </summary>
    public void ResetReward()
    {
        rewardGiven = false;
    }

    /// <summary>
    /// Hook this into TreasureChest.onOpened. Safe to call more than once.
    /// </summary>
    public void GiveReward()
    {
        if (rewardGiven)
        {
            return;
        }

        if (PotionWheelController.Instance == null)
        {
            Debug.LogWarning("ChestPotionReward: no PotionWheelController in the scene.", this);
            return;
        }

        rewardGiven = true;
        PotionWheelController.Instance.Roll(spawnAnchor != null ? spawnAnchor : transform);
    }
}
