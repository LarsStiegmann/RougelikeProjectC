using UnityEngine;

/// <summary>
/// Tracks what this chest costs to open. Each chest keeps its own price, and that
/// price doubles every time it is looted, so the third open is a real decision
/// rather than a formality.
/// </summary>
[RequireComponent(typeof(TreasureChest))]
public class ChestPrice : MonoBehaviour
{
    [Tooltip("Coins needed for the first opening.")]
    [SerializeField] private int baseCost = 25;

    [Tooltip("The price is multiplied by this after every opening.")]
    [SerializeField] private float costMultiplier = 2f;

    [Tooltip("Ceiling so a long run cannot produce an absurd number.")]
    [SerializeField] private int maxCost = 100000;

    private int timesPaid;

    /// <summary>What it costs to open right now.</summary>
    public int CurrentCost
    {
        get
        {
            float cost = baseCost * Mathf.Pow(costMultiplier, timesPaid);
            return Mathf.Clamp(Mathf.RoundToInt(cost), 0, maxCost);
        }
    }

    /// <summary>How many times this chest has been paid for.</summary>
    public int TimesPaid => timesPaid;

    /// <summary>True if the player can currently afford this chest.</summary>
    public bool CanAfford()
    {
        return CurrencyController.Instance != null
            && CurrencyController.Instance.CanAfford(CurrentCost);
    }

    /// <summary>
    /// Charges the player. Returns false and takes nothing if they cannot pay, which
    /// is the caller's cue to leave the chest shut.
    /// </summary>
    public bool TryPay()
    {
        CurrencyController wallet = CurrencyController.Instance;
        if (wallet == null)
        {
            return true;
        }

        if (!wallet.TrySpend(CurrentCost))
        {
            return false;
        }

        timesPaid++;
        return true;
    }
}
