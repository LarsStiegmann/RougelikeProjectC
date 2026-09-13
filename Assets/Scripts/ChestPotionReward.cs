using UnityEngine;

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


    public void ResetReward()
    {
        rewardGiven = false;
    }


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
