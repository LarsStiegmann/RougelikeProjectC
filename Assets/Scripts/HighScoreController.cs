using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class HighScoreController : MonoBehaviour
{
    public static HighScoreController Instance { get; private set; }

    private List<HighScoreEntry> highscores = new();
    public IReadOnlyList<HighScoreEntry> Highscores => highscores;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    //________________________________________________________________________________
    //KI unterstützt: siehe "HighScoreEntry" Zeile 10
    public void AddScore(string playerName, int score)
    {
        highscores.Add(new HighScoreEntry(playerName, score));

        highscores = highscores
            .OrderByDescending(HighScoreEntry => HighScoreEntry.score)
            .Take(10)
            .ToList();

        SaveSystem.Instance.Save();
    }

    //________________________________________________________________________________

    public void SetHighScores(List<HighScoreEntry> newHighScores)
    {
        highscores = newHighScores;
    }
}
