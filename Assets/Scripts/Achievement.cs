using UnityEngine;

public enum AchievementProgressType
{
    Run,
    AllTime
}

[CreateAssetMenu(fileName = "NewAchievement", menuName = "Achievements/Achievement")]
public class Achievement : ScriptableObject
{
    public string id;
    public string achievementName;

    [TextArea]
    public string achievementDescription;

    public AchievementProgressType progressType;

    public float requiredValue;

    public bool isPlatinAchievement;
}
