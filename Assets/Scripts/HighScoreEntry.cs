using UnityEngine;

[System.Serializable]
public class HighScoreEntry
{
    public string playerName;
    public int score;

    //_____________________________________________________
    //KI unterstützt
    //Tool: (OpenAI, GPT-5.5)
    //Prompt: ich möchte ein Highscoreboard machen auf dem die besten 10 Scores auftauchen,
    //welcher Datentyp ist für die Speicherung am besten?
    public HighScoreEntry(string playerName, int score)
    {
        this.playerName = playerName;
        this.score = score;
    }
    //_____________________________________________________
}
