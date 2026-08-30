using TMPro;
using UnityEngine;

public class AchievementEntry : MonoBehaviour
{
    [SerializeField] private TMP_Text achievementName;
    [SerializeField] private TMP_Text description;
    [SerializeField] private TMP_Text status;

    public void Setup(Achievement achievement, bool unlocked)
    {
        achievementName.text = achievement.achievementName;
        description.text = achievement.achievementDescription;

        if (unlocked)
        {
            status.text = "Done";
        }
        else
        {
            status.text = "Locked";
        }
    }
}
