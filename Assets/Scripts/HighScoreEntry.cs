using UnityEngine;

[System.Serializable]
public class HighScoreEntry
{
    public string playerName;
    public int score;

    public HighScoreEntry(string playerName, int score)
    {
        this.playerName = playerName;
        this.score = score;
    }
}
