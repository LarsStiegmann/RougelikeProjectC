using System.Collections;
using TMPro;
using UnityEngine;

public class AchievementNotification : MonoBehaviour
{
    public static AchievementNotification Instance { get; private set; }

    [SerializeField] private GameObject notificationPanel;
    [SerializeField] private TMP_Text achievementName;

    [SerializeField] private AudioClip[] achievementAudios;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        notificationPanel.SetActive(false);
    }

    public void ShowNotification(Achievement achievement)
    {
        StartCoroutine(NotificationAppearAndDissapper(achievement));
    }

    private IEnumerator NotificationAppearAndDissapper(Achievement achievement)
    {
        achievementName.text = achievement.achievementName;

        AudioController.Instance.PlayRandomAudio(achievementAudios, transform, 0.5f, false);

        notificationPanel.SetActive(true);

        yield return new WaitForSeconds(3f);

        notificationPanel.SetActive(false);

        CheckForAllAchievementsNotification(achievement);
    }

    private void CheckForAllAchievementsNotification(Achievement achievement)
    {
        if (achievement.isPlatinAchievement)
        {
            return;
        }

        if (AchievementController.Instance.AllAchievementsUnlocked())
        {
            Achievement platinAchievement = AchievementController.Instance.GetPlatinAchievement();

            if (platinAchievement != null)
            {
                ShowNotification(platinAchievement);
            }
        }
    }
}
