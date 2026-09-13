using UnityEngine;


[RequireComponent(typeof(TreasureChest))]
public class ChestPrice : MonoBehaviour
{
    [Tooltip("Coins needed for the first opening.")]
    [SerializeField] private int baseCost = 25;

    [Tooltip("The price is multiplied by this after every opening.")]
    [SerializeField] private float costMultiplier = 2f;

    [Tooltip("Ceiling so a long run cannot produce an absurd number.")]
    [SerializeField] private int maxCost = 100000;

    [Tooltip("Added to every chest's base price for each chest opened anywhere this run, so ability power is paced by total spend rather than by hopping between fresh chests.")]
    [SerializeField] private int globalStepPerOpen = 10;

    private int timesPaid;

    private static int globalOpens;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void HookSceneLoads()
    {
        globalOpens = 0;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if (mode == UnityEngine.SceneManagement.LoadSceneMode.Single)
        {
            globalOpens = 0;
        }
    }

    public static int GlobalOpens => globalOpens;

    public int CurrentCost
    {
        get
        {
            float cost = (baseCost + globalStepPerOpen * globalOpens) * Mathf.Pow(costMultiplier, timesPaid);
            return Mathf.Clamp(Mathf.RoundToInt(cost), 0, maxCost);
        }
    }

    public int TimesPaid => timesPaid;

    public bool CanAfford()
    {
        return CurrencyController.Instance != null
            && CurrencyController.Instance.CanAfford(CurrentCost);
    }

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
        globalOpens++;
        return true;
    }
}
