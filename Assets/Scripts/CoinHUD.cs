using TMPro;
using UnityEngine;

/// <summary>
/// Shows the coin balance on the HUD. Subscribes to the currency change event rather
/// than polling, and refreshes once on enable so it is correct on the first frame.
/// </summary>
public class CoinHUD : MonoBehaviour
{
    [SerializeField] private TMP_Text label;

    [Tooltip("Text shown before the number.")]
    [SerializeField] private string prefix = "COINS: ";

    private CurrencyController wallet;

    private void Awake()
    {
        if (label == null)
        {
            label = GetComponent<TMP_Text>();
        }
    }

    private void OnEnable()
    {
        wallet = CurrencyController.Instance;
        if (wallet != null)
        {
            wallet.OnCoinsChanged += Refresh;
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (wallet != null)
        {
            wallet.OnCoinsChanged -= Refresh;
        }
    }

    private void Refresh()
    {
        if (label == null)
        {
            return;
        }

        int coins = wallet != null ? wallet.Coins : 0;
        label.text = prefix + coins;
    }
}
