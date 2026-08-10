using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveSystem : MonoBehaviour
{
    public static SaveSystem Instance { get; private set; }

    private string path;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            path = Path.Combine(Application.persistentDataPath, "save.json");
        }
        else
        {
            Destroy(gameObject);
        }
    }

public void Save()
    {
        if (HighScoreController.Instance == null)
        {
            return;
        }

        SaveData data = new SaveData();
        data.highscores = new List<HighScoreEntry>(HighScoreController.Instance.Highscores);

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(path, json);
    }

public void Load()
    {
        if (!File.Exists(path))
        {
            return;
        }

        string json = File.ReadAllText(path);
        SaveData data = JsonUtility.FromJson<SaveData>(json);

        if (data == null || data.highscores == null)
        {
            return;
        }

        if (HighScoreController.Instance != null)
        {
            HighScoreController.Instance.SetHighScores(data.highscores);
        }
    }
}
