using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuController : MonoBehaviour
{
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private Button primaryPauseMenuButton;

    [SerializeField] private GameObject settingsMenuPanel;
    [SerializeField] private Button primarySettingsButton;

    private void Start()
    {
        pauseMenuPanel.SetActive(false);
        settingsMenuPanel.SetActive(false);
    }

private void OnEnable()
    {
        InputController input = InputController.Instance;
        if (input == null || input.Actions == null)
        {
            return;
        }

        input.Actions.Player.Pause.performed += OnPause;
        input.Actions.UI.Cancel.performed += OnResume;
    }

private void OnDisable()
    {
        InputController input = InputController.Instance;
        if (input == null || input.Actions == null)
        {
            return;
        }

        input.Actions.Player.Pause.performed -= OnPause;
        input.Actions.UI.Cancel.performed -= OnResume;
    }

    public void OnPause(InputAction.CallbackContext context)
    {
        PauseGame();
    }

    public void OnResume(InputAction.CallbackContext context)
    {
        ResumeGame();
    }

    private void PauseGame()
    {
        pauseMenuPanel.SetActive(true);

        Time.timeScale = 0f;

        InputController.Instance.Actions.Player.Disable();
        InputController.Instance.Actions.UI.Enable();
        Debug.Log("UI");

        EventSystem.current.SetSelectedGameObject(null);
        primaryPauseMenuButton.Select();
    }

public void ResumeGame()
    {
        pauseMenuPanel.SetActive(false);

        Time.timeScale = 1f;

        InputController.Instance.Actions.UI.Disable();
        InputController.Instance.Actions.Player.Enable();
        Debug.Log("Player");

        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            CharacterMovement movement = player.GetComponent<CharacterMovement>();
            if (movement != null)
            {
                movement.ResetGroundedState();
            }
        }
    }

    public void OpenSettings()
    {
        settingsMenuPanel.SetActive(true);

        primarySettingsButton.Select();
    }

    public void CloseSettings()
    {
        settingsMenuPanel.SetActive(false);
        primaryPauseMenuButton.Select();
    }

public void BackToMenu()
    {
        Time.timeScale = 1f;
        pauseMenuPanel.SetActive(false);

        InputController input = InputController.Instance;
        if (input != null && input.Actions != null)
        {
            input.Actions.UI.Disable();
            input.Actions.Player.Enable();
        }

        // HighScoreController and SaveSystem are DontDestroyOnLoad singletons that
        // live in the MainMenu scene. Entering play directly in Level1 skips that
        // scene, so they do not exist and this would throw.
        if (HighScoreController.Instance != null && SaveSystem.Instance != null)
        {
            HighScoreController.Instance.AddScore("Lars", 1);
        }

        SceneManager.LoadScene("MainMenu");
    }
}
