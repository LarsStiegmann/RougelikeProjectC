using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UpgradeButton : MonoBehaviour
{
    private StatUpgrade upgrade;

    [SerializeField] private TMP_Text nameText;
    //[SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Image buttonBackground;

    [SerializeField] private Color commonColor;
    [SerializeField] private Color rareColor;
    [SerializeField] private Color epicColor;

    Color rarityColor = Color.white;

    [SerializeField] private AudioClip[] buttonSounds;

    public void Setup(StatUpgrade newUpgrade)
    {
        upgrade = newUpgrade;

        nameText.text = upgrade.upgradeName;
        //descriptionText.text = upgrade.description;

        SetRarityColor();
    }

    private void SetRarityColor()
    {
        switch (upgrade.rarity)
        {
            case UpgradeRarity.Common:
                rarityColor = commonColor;
                break;
            case UpgradeRarity.Rare:
                rarityColor = rareColor;
                break;
            case UpgradeRarity.Epic:
                rarityColor = epicColor;
                break;
        }

        buttonBackground.color = rarityColor;
    }

    public void Select()
    {
        AudioController.Instance.PlayRandomAudio(buttonSounds, transform, 0.5f, false);
        PlayerState.Instance.ApplyUpgrade(upgrade);
        Debug.Log("Upgrade gewählt: " + upgrade.upgradeName);
    }
}
