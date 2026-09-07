using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SaveData
{
    public List<HighScoreEntry> highscores;

    public float mouseSensitivity = 0.25f;
    public float controllerSensitivity = 180f;
    public float masterVolume = 1f;
    public float soundFXVolume = 1;
    public float musicVolume = 1;

    public List<string> unlockedAchievements = new List<string>();

    public int allTimeKills;
    public int allTimeOpenedChests;
    public int deaths;
    public int allTimeBossKills;

    public List<AchievementProgress> achievementProgress = new List<AchievementProgress>();
}
