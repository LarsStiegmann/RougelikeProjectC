using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class AchievementController : MonoBehaviour
{
    public static AchievementController Instance { get; private set; }

    [SerializeField] private List<Achievement> achievements;

    [SerializeField] private Achievement platin;

    private List<AchievementProgress> runAchievementProgress = new List<AchievementProgress>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public bool IsUnlocked(Achievement achievement)
    {
        return SaveSystem.Instance.unlockedAchievements.Contains(achievement.id);
    }

    public void UnlockAchievement(Achievement achievement)
    {
        if (IsUnlocked(achievement))
        {
            return;
        }

        SaveSystem.Instance.unlockedAchievements.Add(achievement.id);

        SaveSystem.Instance.Save();

        AchievementNotification.Instance.ShowNotification(achievement);

        CheckAchievements();
    }

    public void UpdateAchievement(Achievement achievement, float currentValue)
    {
        if (IsUnlocked(achievement))
        {
            return;
        }

        AchievementProgress progress = GetProgress(achievement);

        progress.currentValue = currentValue;

        if (currentValue >= achievement.requiredValue)
        {
            UnlockAchievement(achievement);
        }
    }

    private AchievementProgress GetProgress(Achievement achievement)
    {
        List<AchievementProgress> list;

        if (achievement.progressType == AchievementProgressType.Run)
        {
            list = runAchievementProgress;
        }
        else
        {
            list = SaveSystem.Instance.achievementProgress;
        }

        AchievementProgress progress = list.Find(x => x.id == achievement.id);

        if (progress == null)
        {
            progress = new AchievementProgress
            {
                id = achievement.id,
                currentValue = 0
            };
            list.Add(progress);
        }

        return progress;
    }

    public float GetCurrentProgress(Achievement achievement)
    {
        return GetProgress(achievement).currentValue;
    }

    private void CheckAchievements()
    {
        foreach (var achievement in achievements)
        {
            if (achievement == platin)
            {
                continue;
            }

            if (!IsUnlocked(achievement))
            {
                return;
            }
        }

        UnlockAchievement(platin);
    }

    public int GetPlatinAchievementProgress()
    {
        return SaveSystem.Instance.unlockedAchievements.Count;
    }

    public int GetTotalAchievementCount()
    {
        return achievements.Count - 1;
    }

    public bool AllAchievementsUnlocked()
    {
        int totalAchievements = achievements.Count;

        return SaveSystem.Instance.unlockedAchievements.Count >= totalAchievements;
    }

    public Achievement GetPlatinAchievement()
    {
        return achievements.Find(x => x.isPlatinAchievement);
    }
}
