using UnityEngine;

public class StatCounter : MonoBehaviour
{
    public static StatCounter Instance { get; private set; }

    public int kills { get; private set; }

    public int chests { get; private set; }

    [SerializeField] private Achievement killsAchievement1;
    [SerializeField] private Achievement killsAchievement2;

    [SerializeField] private Achievement killsAchievementAllTime1;
    [SerializeField] private Achievement killsAchievementAllTime2;

    [SerializeField] private Achievement chestAchievement;
    [SerializeField] private Achievement chestAllTimeAchievement;

    [SerializeField] private Achievement firstDeathAchievement;
    [SerializeField] private Achievement deathsAchievement;

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
    }

    public void AddKill()
    {
        kills++;

        SaveSystem.Instance.allTimeKills++;

        SaveSystem.Instance.Save();

        AchievementController.Instance.UpdateAchievement(killsAchievement1, kills);
        AchievementController.Instance.UpdateAchievement(killsAchievement2, kills);

        AchievementController.Instance.UpdateAchievement(killsAchievementAllTime1, SaveSystem.Instance.allTimeKills);
        AchievementController.Instance.UpdateAchievement(killsAchievementAllTime2, SaveSystem.Instance.allTimeKills);
    }

    public void AddChest()
    {
        chests++;

        SaveSystem.Instance.allTimeOpenedChests++;

        SaveSystem.Instance.Save();

        AchievementController.Instance.UpdateAchievement(chestAchievement, chests);
        AchievementController.Instance.UpdateAchievement(chestAllTimeAchievement, SaveSystem.Instance.allTimeOpenedChests);
    }

    public void AddDeath()
    {
        SaveSystem.Instance.deaths++;

        SaveSystem.Instance.Save();

        AchievementController.Instance.UpdateAchievement(firstDeathAchievement, SaveSystem.Instance.deaths);
        AchievementController.Instance.UpdateAchievement(deathsAchievement, SaveSystem.Instance.deaths);
    }
}
