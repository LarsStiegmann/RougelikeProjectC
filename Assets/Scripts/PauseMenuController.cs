using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuController : MonoBehaviour
{
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private Button primaryPauseMenuButton;

    [SerializeField] private GameObject settingsMenuPanel;
    [SerializeField] private Button primarySettingsButton;

    [SerializeField] private TMP_Text healthAmountText;
    [SerializeField] private TMP_Text healthRegenerationAmountText;
    [SerializeField] private TMP_Text lifeStealAmountText;
    [SerializeField] private TMP_Text armorAmountText;
    [SerializeField] private TMP_Text damageAmountText;
    [SerializeField] private TMP_Text attackSpeedAmountText;
    [SerializeField] private TMP_Text rangeAmountText;
    [SerializeField] private TMP_Text critChanceAmountText;
    [SerializeField] private TMP_Text critDamageAmountText;
    [SerializeField] private TMP_Text speedAmountText;
    [SerializeField] private TMP_Text jumpHeightAmountText;

    private void Start()
    {
        pauseMenuPanel.SetActive(false);
        settingsMenuPanel.SetActive(false);

        if(CursorController.Instance != null)
        {
            CursorController.Instance.SetGameplayCursor();
        }
    }

    private void OnEnable()
    {
        InputController input = InputController.Instance;
        if (input == null || input.Actions == null)
        {
            return;
        }

        input.Actions.Player.Pause.performed += OnPause;
        input.Actions.UI.Cancel.performed += OnResume;
    }

    private void OnDisable()
    {
        InputController input = InputController.Instance;
        if (input == null || input.Actions == null)
        {
            return;
        }

        input.Actions.Player.Pause.performed -= OnPause;
        input.Actions.UI.Cancel.performed -= OnResume;
    }

    public void OnPause(InputAction.CallbackContext context)
    {
        PauseGame();
    }

    public void OnResume(InputAction.CallbackContext context)
    {
        ResumeGame();
    }

    private void PauseGame()
    {
        pauseMenuPanel.SetActive(true);

        ShowPlayerStats();

        Time.timeScale = 0f;

        InputController.Instance.Actions.Player.Disable();
        InputController.Instance.Actions.UI.Enable();
        Debug.Log("UI");

        CursorController.Instance.SetMenuCursor();

        EventSystem.current.SetSelectedGameObject(null);
        primaryPauseMenuButton.Select();
    }

    public void ResumeGame()
    {
        pauseMenuPanel.SetActive(false);

        Time.timeScale = 1f;

        InputController.Instance.Actions.UI.Disable();
        InputController.Instance.Actions.Player.Enable();
        Debug.Log("Player");

        CursorController.Instance.SetGameplayCursor();

        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            CharacterMovement movement = player.GetComponent<CharacterMovement>();
            if (movement != null)
            {
                movement.ResetGroundedState();
            }
        }
    }

    public void OpenSettings()
    {
        settingsMenuPanel.SetActive(true);

        primarySettingsButton.Select();
    }

    public void CloseSettings()
    {
        settingsMenuPanel.SetActive(false);
        primaryPauseMenuButton.Select();
    }

    public void BackToMenu()
    {
        Time.timeScale = 1f;
        pauseMenuPanel.SetActive(false);

        InputController input = InputController.Instance;
        if (input != null && input.Actions != null)
        {
            input.Actions.UI.Disable();
            input.Actions.Player.Enable();
        }

        SceneManager.LoadScene("MainMenu");
    }

    private void ShowPlayerStats()
    {
        healthAmountText.text = PlayerState.Instance.currentMaxHealth.ToString();
        healthRegenerationAmountText.text = PlayerState.Instance.currentHealthRegeneration.ToString();
        lifeStealAmountText.text = PlayerState.Instance.currentLifeSteal.ToString();
        armorAmountText.text = PlayerState.Instance.currentArmor.ToString();
        damageAmountText.text = PlayerState.Instance.currentDamage.ToString();
        attackSpeedAmountText.text = PlayerState.Instance.bonusAttackSpeedPercentage.ToString() + "%";
        rangeAmountText.text = PlayerState.Instance.currentAttackRange.ToString();
        critChanceAmountText.text = PlayerState.Instance.currentCritChance.ToString() + "%";
        critDamageAmountText.text = PlayerState.Instance.currentCritMultiplier.ToString();
        speedAmountText.text = PlayerState.Instance.currentSpeed.ToString();
        jumpHeightAmountText.text = PlayerState.Instance.currentJumpForce.ToString();
    }
}
