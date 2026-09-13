using TMPro;
using UnityEngine;

    //_________________________________________________________________________________________________________
    //Quelle: KI (Claude Opus 5)
    //Prompt: Generate a Coin HUD for the Player HUD
    //Datum: 01.09.2026
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
    //_________________________________________________________________________________________________________
