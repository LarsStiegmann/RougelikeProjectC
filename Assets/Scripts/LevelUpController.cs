using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class LevelUpController : MonoBehaviour
{
    [SerializeField] private List<StatUpgrade> availableUpgrades;

    [SerializeField] private GameObject levelUpPanel;
    [SerializeField] private UpgradeButton[] statButtons;
    [SerializeField] private Button primaryLevelUpButton;

    private void Start()
    {
        if (levelUpPanel != null)
        {
            levelUpPanel.SetActive(false);
        }
    }

    public List<StatUpgrade> GetRandomUpgrades(int amount)
    {
        return availableUpgrades
            .OrderBy(x => Random.value)
            .Take(amount)
            .ToList();
    }

public void Open()
    {
        if (levelUpPanel == null || availableUpgrades == null || availableUpgrades.Count == 0)
        {
            return;
        }

        int slots = statButtons != null ? statButtons.Length : 0;
        if (slots == 0)
        {
            return;
        }

        List<StatUpgrade> randomUpgrades = GetRandomUpgrades(slots);

        levelUpPanel.SetActive(true);
        Time.timeScale = 0f;

        // Hand control to the UI map so the choice can be made with pad or keyboard.
        if (InputController.Instance != null && InputController.Instance.Actions != null)
        {
            InputController.Instance.Actions.Player.Disable();
            InputController.Instance.Actions.UI.Enable();
        }

        for (int i = 0; i < slots; i++)
        {
            if (statButtons[i] == null)
            {
                continue;
            }

            if (i < randomUpgrades.Count)
            {
                statButtons[i].gameObject.SetActive(true);
                statButtons[i].Setup(randomUpgrades[i]);
            }
            else
            {
                // fewer upgrades available than slots
                statButtons[i].gameObject.SetActive(false);
            }
        }

        if (EventSystem.current != null && primaryLevelUpButton != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            primaryLevelUpButton.Select();
        }
    }

    /// <summary>
    /// Closes the menu and resumes play. Must run after an upgrade is chosen,
    /// otherwise Time.timeScale stays at 0 and the game is frozen for good.
    /// </summary>
    public void Close()
    {
        if (levelUpPanel != null)
        {
            levelUpPanel.SetActive(false);
        }

        Time.timeScale = 1f;

        if (InputController.Instance != null && InputController.Instance.Actions != null)
        {
            InputController.Instance.Actions.UI.Disable();
            InputController.Instance.Actions.Player.Enable();
        }

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }
}
