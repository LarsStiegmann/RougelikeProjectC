using System;
using UnityEngine;

/// <summary>
/// Holds the run's coin balance. Coins drop from kills and are spent opening chests.
///
/// The instance is found or created on demand, so a scene started directly in the
/// editor works without the object being placed by hand.
/// </summary>
public class CurrencyController : MonoBehaviour
{
    private static CurrencyController instance;

    public static CurrencyController Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<CurrencyController>();
            }

            if (instance == null)
            {
                GameObject go = new GameObject("CurrencyController (auto)");
                instance = go.AddComponent<CurrencyController>();
            }

            return instance;
        }
    }

    [Tooltip("Coins the player starts a run with.")]
    [SerializeField] private int startingCoins = 0;

    /// <summary>Current coin balance.</summary>
    public int Coins { get; private set; }

    /// <summary>Raised whenever the balance changes, for the HUD to listen to.</summary>
    public event Action OnCoinsChanged;

    private void Awake()
    {
        // Compare against the backing field: the property getter would find this very
        // component and then destroy it as a duplicate.
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        Coins = startingCoins;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    /// <summary>Grants coins. Negative or zero amounts are ignored.</summary>
    public void Add(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        Coins += amount;
        OnCoinsChanged?.Invoke();
    }

    /// <summary>True if the balance covers the given cost.</summary>
    public bool CanAfford(int cost)
    {
        return Coins >= cost;
    }

    /// <summary>
    /// Deducts the cost and returns true, or leaves the balance alone and returns
    /// false if it cannot be paid.
    /// </summary>
    public bool TrySpend(int cost)
    {
        if (cost <= 0)
        {
            return true;
        }

        if (Coins < cost)
        {
            return false;
        }

        Coins -= cost;
        OnCoinsChanged?.Invoke();
        return true;
    }

    /// <summary>Resets to the starting balance, for a fresh run.</summary>
    public void ResetCoins()
    {
        Coins = startingCoins;
        OnCoinsChanged?.Invoke();
    }
}
