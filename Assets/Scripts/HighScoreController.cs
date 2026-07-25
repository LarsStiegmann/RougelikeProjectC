using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HighScoreController : MonoBehaviour
{

    private List<HighScoreEntry> highscores = new();

    public void AddScore(string playerName, int score)
    {
        highscores.Add(new HighScoreEntry(playerName, score));

        highscores = highscores
            .OrderByDescending(HighScoreEntry => HighScoreEntry.score)
            .Take(10)
            .ToList();
    }
}
