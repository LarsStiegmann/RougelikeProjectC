using UnityEngine;


[CreateAssetMenu(fileName = "NewAchievement", menuName = "Achievements/Achievement")]
public class Achievement : ScriptableObject
{
    public string id;
    public string achievementName;

    [TextArea]
    public string achievementDescription;

    public float requiredValue;
}
