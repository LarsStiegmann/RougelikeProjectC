using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private Button primaryMenuButton;
    [SerializeField] private Button primarySettingsButton;
    [SerializeField] private Button primaryCollectionButton;
    [SerializeField] private Button primaryAchievementButton;
    [SerializeField] private Button primaryAudioSettingsButton;
    [SerializeField] private Button primarySensitivitySettingsButton;

    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject collectionPanel;
    [SerializeField] private GameObject achievementPanel;
    [SerializeField] private GameObject audioSettingsPanel;
    [SerializeField] private GameObject sensitivitySettingsPanel;

    [SerializeField] private AudioClip[] buttonSounds;

    private void Awake()
    {
        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.Load();
        }
    }

    private void Start()
    {
        settingsPanel.SetActive(false);
        collectionPanel.SetActive(false);
        achievementPanel.SetActive(false);
        audioSettingsPanel.SetActive(false);
        sensitivitySettingsPanel.SetActive(false);

        primaryMenuButton.Select();
    }

    private void PLaySound()
    {
        AudioController.Instance.PlayRandomAudio(buttonSounds, transform, 1f, false);
    }

    public void StartGame()
    {
        //StartGameAfterDelay();
        PLaySound();
        SceneManager.LoadScene("Level1");
    }

    public void OpenSettings()
    {
        PLaySound();
        settingsPanel.SetActive(true);
        primarySettingsButton.Select();
    }

    public void OpenAudioSettings()
    {
        PLaySound();
        audioSettingsPanel.SetActive(true);
        primaryAudioSettingsButton.Select();
    }

    public void CloseAudioSettings()
    {
        PLaySound();
        audioSettingsPanel.SetActive(false);
        primarySettingsButton.Select();
    }

    public void OpenSensitivitySettings()
    {
        PLaySound();
        sensitivitySettingsPanel.SetActive(true);
        primarySensitivitySettingsButton.Select();
    }

    public void CloseSensitivitySettings()
    {
        PLaySound();
        sensitivitySettingsPanel.SetActive(false);
        primarySettingsButton.Select();
    }

    public void CloseSettings()
    {
        PLaySound();
        settingsPanel.SetActive(false);
        primaryMenuButton.Select();
    }

    public void OpenCollection()
    {
        PLaySound();
        collectionPanel.SetActive(true);
        primaryCollectionButton.Select();
    }

    public void CloseCollection()
    {
        PLaySound();
        collectionPanel.SetActive(false);
        primaryMenuButton.Select();
    }

    public void OpenAchievements()
    {
        PLaySound();
        achievementPanel.SetActive(true);
        primaryAchievementButton.Select();
    }

    public void CloseAchievements()
    {
        PLaySound();
        achievementPanel.SetActive(false);
        primaryMenuButton.Select();
    }

    public void QuitGame()
    {
        PLaySound();
        Application.Quit();
        Debug.Log("Application quit");
    }
}
