using UnityEngine;

public class AchievementUI : MonoBehaviour
{
    [SerializeField] private AchievementEntry achievementPrefab;
    [SerializeField] private Transform content;
    [SerializeField] private Achievement[] achievements;

    private void Start()
    {
        foreach (Achievement achievement in achievements)
        {
            AchievementEntry entry = Instantiate(achievementPrefab, content);

            bool unlocked = AchievementController.Instance.IsUnlocked(achievement);

            entry.Setup(achievement, unlocked);
        }
    }
}
