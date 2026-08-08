using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private Button primaryMenuButton;
    [SerializeField] private Button primarySettingsButton;
    [SerializeField] private Button primaryCollectionButton;
    [SerializeField] private Button primaryAchievementButton;

    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject collectionPanel;
    [SerializeField] private GameObject achievementPanel;

    private void Awake()
    {
        SaveSystem.Instance.Load();
    }

    private void Start()
    {
        settingsPanel.SetActive(false);
        collectionPanel.SetActive(false);
        achievementPanel.SetActive(false);

        primaryMenuButton.Select();
    }

    public void StartGame()
    {
        Debug.Log("Starting game");
        SceneManager.LoadScene("Level1");
    }

    public void OpenSettings()
    {
        settingsPanel.SetActive(true);
        primarySettingsButton.Select();
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
        primaryMenuButton.Select();
    }

    public void OpenCollection()
    {
        collectionPanel.SetActive(true);
        primaryCollectionButton.Select();
    }

    public void CloseCollection()
    {
        collectionPanel.SetActive(false);
        primaryMenuButton.Select();
    }

    public void OpenAchievements()
    {
        achievementPanel.SetActive(true);
        primaryAchievementButton.Select();
    }

    public void CloseAchievements()
    {
        achievementPanel.SetActive(false);
        primaryMenuButton.Select();
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Application quit");
    }
}
