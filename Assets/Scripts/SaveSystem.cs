using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveSystem : MonoBehaviour
{
    public static SaveSystem Instance { get; private set; }

    private string path;

    public float mouseSensitivity;
    public float controllerSensitivity;
    public float masterVolume;
    public float soundFXVolume;
    public float musicVolume;

    public List<string> unlockedAchievements = new List<string>();

    public int allTimeKills;
    public int allTimeOpenedChests;
    public int deaths;

    public List<AchievementProgress> achievementProgress = new List<AchievementProgress>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            path = Path.Combine(Application.persistentDataPath, "save.json");
            EnsureDefaults();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void EnsureDefaults()
    {
        if (mouseSensitivity == 0f && controllerSensitivity == 0f
            && masterVolume == 0f && soundFXVolume == 0f && musicVolume == 0f)
        {
            mouseSensitivity = 0.25f;
            controllerSensitivity = 180f;
            masterVolume = 1f;
            soundFXVolume = 1f;
            musicVolume = 1f;
        }
    }

    //____________________________________________________________________________________________
    //KI unterst�tzt
    //Tool: ChatGPT (OpenAI, GPT-5.5)
    //Prompt: wie kann ich mein Spiel in Unity so erweitern,
    //dass Spieldaten permanent f�r den Spieler gespeichert werden?
    //*Grundprinzip stammt von KI, Umsetzung wurde �berarbeitet*

    public void Save()
    {
        SaveData data = new SaveData();

        if (HighScoreController.Instance != null)
        {
            data.highscores = new List<HighScoreEntry>(HighScoreController.Instance.Highscores);
        }

        data.mouseSensitivity = mouseSensitivity;
        data.controllerSensitivity = controllerSensitivity;
        data.masterVolume = masterVolume;
        data.soundFXVolume = soundFXVolume;
        data.musicVolume = musicVolume;

        data.unlockedAchievements = new List<string>(unlockedAchievements);

        data.allTimeKills = allTimeKills;
        data.allTimeOpenedChests = allTimeOpenedChests;
        data.deaths = deaths;

        data.achievementProgress = new List<AchievementProgress>(achievementProgress);

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

        if (data == null)
        {
            return;
        }

        mouseSensitivity = data.mouseSensitivity;
        controllerSensitivity = data.controllerSensitivity;
        masterVolume = data.masterVolume;
        soundFXVolume = data.soundFXVolume;
        musicVolume = data.musicVolume;

        EnsureDefaults();

        unlockedAchievements = data.unlockedAchievements ?? new List<string>();

        allTimeKills = data.allTimeKills;
        allTimeOpenedChests = data.allTimeOpenedChests;
        deaths = data.deaths;

        achievementProgress = data.achievementProgress ?? new List<AchievementProgress>();

        if (HighScoreController.Instance != null)
        {
            HighScoreController.Instance.SetHighScores(data.highscores);
        }
    }

    //____________________________________________________________________________________________
}
