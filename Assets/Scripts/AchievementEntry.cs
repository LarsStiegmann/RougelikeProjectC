using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AchievementEntry : MonoBehaviour
{
    [SerializeField] private TMP_Text achievementName;
    [SerializeField] private TMP_Text description;
    [SerializeField] private TMP_Text progress;
    [SerializeField] private Image status;

    [SerializeField] private Sprite trophySprite;
    [SerializeField] private Sprite lockSprite;

    //_______________________________________________________________________________________________________
    //KI unterstützt
    //siehe AchievementController Zeile 34
    //*die grundsätzliche Ausgabe von Erfolgen über ein Prefab,
    //welches in AchievementUI generiert wird, stammt von KI.
    public void Setup(Achievement achievement, bool unlocked)
    {
        achievementName.text = achievement.achievementName;
        description.text = achievement.achievementDescription;

        if (unlocked)
        {
            status.sprite = trophySprite;
        }
        else
        {
            status.sprite = lockSprite;
        }
    //_______________________________________________________________________________________________________


        //_________________________________________________________________________________________________
        //KI unterstützt
        //siehe AchievementController Zeile 78
        //*Die genauen Ausgabewerte wurden selbst gewählt.*
        if (achievement.isPlatinAchievement)
        {
            int total = AchievementController.Instance.GetTotalAchievementCount();
            int unlockedCount = AchievementController.Instance.GetPlatinAchievementProgress();

            progress.text = $"{unlockedCount} / {total}";

        }
        else if(achievement.progressType == AchievementProgressType.AllTime)
        {
            if (unlocked)
            {
                progress.text = $"{achievement.requiredValue} / {achievement.requiredValue}";
            }
            else
            {
                float currentValue = AchievementController.Instance.GetCurrentProgress(achievement);

                progress.text = $"{currentValue} / {achievement.requiredValue}";
            }
        }
        else
        {
            if (unlocked)
            {
                progress.text = "1 / 1";
            }
            else
            {
                progress.text = "0 / 1";
            }
        }
        //_________________________________________________________________________________________________
    }
}
