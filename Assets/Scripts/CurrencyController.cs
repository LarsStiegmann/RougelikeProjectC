using System;
using UnityEngine;


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

    public int Coins { get; private set; }

    public event Action OnCoinsChanged;

    private void Awake()
    {
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

    public void Add(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        Coins += amount;
        OnCoinsChanged?.Invoke();
    }

    public bool CanAfford(int cost)
    {
        return Coins >= cost;
    }

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

    public void ResetCoins()
    {
        Coins = startingCoins;
        OnCoinsChanged?.Invoke();
    }
}
