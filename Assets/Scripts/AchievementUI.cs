using System;
using UnityEngine;
using UnityEngine.UI;

public class AchievementUI : MonoBehaviour
{
    [SerializeField] private AchievementEntry achievementPrefab;
    [SerializeField] private Transform content;
    [SerializeField] private Achievement[] achievements;

    private int index;

    private void Start()
    {
        foreach (Achievement achievement in achievements)
        {
            AchievementEntry entry = Instantiate(achievementPrefab, content);

            Image background = entry.transform.Find("AchievementEntryBackground").GetComponent<Image>();

            if (index % 2 == 1)
            {
                Color color = background.color;
                color.a = 0;
                background.color = color;
            }

            bool unlocked = AchievementController.Instance.IsUnlocked(achievement);

            entry.Setup(achievement, unlocked);

            index++;
        }
    }
}
