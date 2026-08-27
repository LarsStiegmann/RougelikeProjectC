using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SaveData
{
    //public int waehrung;
    public List<HighScoreEntry> highscores;
    public float mouseSensitivity = 0.25f;
    public float controllerSensitivity = 180f;
    public float volume = 1f;
}
