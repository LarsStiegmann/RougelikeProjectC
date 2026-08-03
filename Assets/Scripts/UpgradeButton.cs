using TMPro;
using UnityEngine;

public class UpgradeButton : MonoBehaviour
{
    private StatUpgrade upgrade;

    [SerializeField] private TMP_Text nameText;
    //[SerializeField] private TMP_Text descriptionText;

    public void Setup(StatUpgrade newUpgrade)
    {
        upgrade = newUpgrade;

        nameText.text = upgrade.upgradeName;
        //descriptionText.text = upgrade.description;
    }

    public void Select()
    {
        PlayerState.Instance.ApplyUpgrade(upgrade);
        Debug.Log("Upgrade gewählt: " + upgrade.upgradeName);
    }
}
