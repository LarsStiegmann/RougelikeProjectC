using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class LevelUpController : MonoBehaviour
{

    [SerializeField] private List<StatUpgrade> availableUpgrades;

    [SerializeField] private GameObject levelUpPanel;
    [SerializeField] private UpgradeButton[] statButtons;
    [SerializeField] private Button primaryLevelUpButton;

    private void Start()
    {
        levelUpPanel.SetActive(false);
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
        List<StatUpgrade> randomUpgrades = GetRandomUpgrades(3);
        levelUpPanel.SetActive(true);
        primaryLevelUpButton.Select();
        Time.timeScale = 0f;

        for (int i = 0; i < 3; i++)
        {
            statButtons[i].Setup(randomUpgrades[i]);
        }
    }
}
