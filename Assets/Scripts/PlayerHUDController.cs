using UnityEngine;
using UnityEngine.UI;
using TMPro;

    //_________________________________________________________________________________________________________
    //Quelle: KI (Claude Opus 5)
    //Prompt: Generate a Player HUD for the game
    //Datum: 02.08.2026

public class PlayerHUDController : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private Slider xpSlider;
    [SerializeField] private TMP_Text xpText;
    [SerializeField] private TMP_Text levelText;

    private void Start()
    {
        // Try to bind to player events
        if (PlayerState.Instance != null)
        {
            PlayerState.Instance.OnHealthChanged += UpdateHealthHUD;
            PlayerState.Instance.OnXPChanged += UpdateXPHUD;
            PlayerState.Instance.OnLevelUp += UpdateLevelHUD;

            // Initial updates
            UpdateHealthHUD();
            UpdateXPHUD();
            UpdateLevelHUD();
        }
        else
        {
            Debug.LogWarning("[PlayerHUDController] PlayerState.Instance not found! Retrying in 0.5s...");
            Invoke(nameof(RetryInit), 0.5f);
        }
    }

    private void RetryInit()
    {
        if (PlayerState.Instance != null)
        {
            PlayerState.Instance.OnHealthChanged += UpdateHealthHUD;
            PlayerState.Instance.OnXPChanged += UpdateXPHUD;
            PlayerState.Instance.OnLevelUp += UpdateLevelHUD;

            UpdateHealthHUD();
            UpdateXPHUD();
            UpdateLevelHUD();
        }
    }

    private void OnDestroy()
    {
        if (PlayerState.Instance != null)
        {
            PlayerState.Instance.OnHealthChanged -= UpdateHealthHUD;
            PlayerState.Instance.OnXPChanged -= UpdateXPHUD;
            PlayerState.Instance.OnLevelUp -= UpdateLevelHUD;
        }
    }

    private void UpdateHealthHUD()
    {
        if (PlayerState.Instance == null) return;

        float currentHP = PlayerState.Instance.currentHealth;
        float maxHP = PlayerState.Instance.currentMaxHealth;

        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHP;
            healthSlider.value = currentHP;
        }

        if (healthText != null)
        {
            healthText.text = $"HP: {Mathf.CeilToInt(currentHP)} / {Mathf.CeilToInt(maxHP)}";
        }
    }

    private void UpdateXPHUD()
    {
        if (PlayerState.Instance == null) return;

        float currentXP = PlayerState.Instance.currentXP;
        float maxXP = PlayerState.Instance.currentMaxXP;

        if (xpSlider != null)
        {
            xpSlider.maxValue = maxXP;
            xpSlider.value = currentXP;
        }

        if (xpText != null)
        {
            xpText.text = $"XP: {Mathf.RoundToInt(currentXP)} / {Mathf.RoundToInt(maxXP)}";
        }
    }

    private void UpdateLevelHUD()
    {
        if (PlayerState.Instance == null) return;

        if (levelText != null)
        {
            levelText.text = $"LVL: {PlayerState.Instance.currentLevel}";
        }
    }
}
    //_________________________________________________________________________________________________________
