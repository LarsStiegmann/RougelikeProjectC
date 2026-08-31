using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class AchievementController : MonoBehaviour
{
    public static AchievementController Instance { get; private set; }

    [SerializeField] private List<Achievement> achievements;

    [SerializeField] private Achievement platin;

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

        CheckAchievements();
    }

    public void UpdateAchievement(Achievement achievement, float currentValue)
    {
        if (IsUnlocked(achievement))
        {
            return;
        }

        if (currentValue >= achievement.requiredValue)
        {
            UnlockAchievement(achievement);
        }
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
}
