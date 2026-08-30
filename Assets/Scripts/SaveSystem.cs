using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveSystem : MonoBehaviour
{
    private static SaveSystem instance;

    /// <summary>
    /// Finds the scene instance, or creates one with default settings so that scenes
    /// started directly in the editor (without the MainMenu) still have working
    /// sensitivity and volume values.
    /// </summary>
    public static SaveSystem Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<SaveSystem>();
            }

            if (instance == null)
            {
                GameObject go = new GameObject("SaveSystem (auto)");
                instance = go.AddComponent<SaveSystem>();
            }

            return instance;
        }
    }

    private string path;

    public float mouseSensitivity;
    public float controllerSensitivity;
    public float masterVolume;
    public float soundFXVolume;
    public float musicVolume;

    private void Awake()
    {
        // Compare against the backing field, not the property: the property getter
        // would find this very component and then destroy it as a "duplicate".
        if (instance == null || instance == this)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            path = Path.Combine(Application.persistentDataPath, "save.json");
            EnsureDefaults();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// An all-zero profile is unplayable (frozen camera, muted game) and only occurs
    /// when the values were never initialised - a fresh scene object, or a save.json
    /// written before the settings system existed. Replace it with sane defaults.
    /// </summary>
    private void EnsureDefaults()
    {
        if (mouseSensitivity == 0f && controllerSensitivity == 0f
            && masterVolume == 0f && soundFXVolume == 0f && musicVolume == 0f)
        {
            // Midpoint of the settings slider's 0-0.5 range.
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

        // Old save files predate these fields and load them all as zero.
        EnsureDefaults();

        AudioListener.volume = masterVolume;

        if (HighScoreController.Instance != null)
        {
            HighScoreController.Instance.SetHighScores(data.highscores);
        }
    }

    //____________________________________________________________________________________________
}
