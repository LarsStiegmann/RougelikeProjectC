using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverController : MonoBehaviour
{
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject gameOverButtonsPanel;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private GameObject enterNamePanel;
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private Button primaryGameOverButton;

    [Tooltip("Delay after death before the panel appears, so the shatter can play.")]
    [SerializeField] private float showDelay = 1.6f;

    [Tooltip("How long the panel takes to fade in.")]
    [SerializeField] private float fadeDuration = 0.6f;

    [Tooltip("Scene loaded by the menu button.")]
    [SerializeField] private string mainMenuScene = "MainMenu";

    private bool shown;

    private void Start()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
        if (enterNamePanel != null)
        {
            enterNamePanel.SetActive(false);
        }

        if (gameOverButtonsPanel != null)
        {
            gameOverButtonsPanel.SetActive(false);
        }

        if (PlayerState.Instance != null)
        {
            PlayerState.Instance.OnPlayerDied += HandlePlayerDied;
        }
        else
        {
            StartCoroutine(SubscribeWhenReady());
        }
    }

    private IEnumerator SubscribeWhenReady()
    {
        float t = 0f;
        while (PlayerState.Instance == null && t < 5f)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        if (PlayerState.Instance != null)
        {
            PlayerState.Instance.OnPlayerDied += HandlePlayerDied;
        }
    }

    private void OnDestroy()
    {
        if (PlayerState.Instance != null)
        {
            PlayerState.Instance.OnPlayerDied -= HandlePlayerDied;
        }
    }

    private void HandlePlayerDied()
    {
        if (shown)
        {
            return;
        }
        shown = true;
        StartCoroutine(ShowRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        float t = 0f;
        while (t < showDelay)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        if (enterNamePanel != null)
        {
            enterNamePanel.SetActive(true);
        }

        CursorController.Instance.SetMenuCursor();

        if (InputController.Instance != null && InputController.Instance.Actions != null)
        {
            InputController.Instance.Actions.Player.Disable();
            InputController.Instance.Actions.UI.Enable();
        }

        if (EventSystem.current != null && nameInput != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            nameInput.Select();
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            float f = 0f;
            while (f < fadeDuration)
            {
                f += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Clamp01(f / fadeDuration);
                yield return null;
            }
            canvasGroup.alpha = 1f;
        }
    }

    
    public void ConfirmName()
    {
        Time.timeScale = 1f;

        //__________________________________________________________________________________________________
        //Quelle: Christina Creates Games auf Youtube
        //Titel: TextMeshPro Input Field in Unity 6: Basics to Pro Features
        //URL: https://www.youtube.com/watch?v=zahrwl1125k&t=139s
        //Datum: 23.05.2023
        //*Diese Quelle wurde weniger für Code, sondern hauptsächlich für Einstellungen im Inspektor verwendet

        string playerName = nameInput.text.Trim();

        //__________________________________________________________________________________________________

        if (string.IsNullOrEmpty(playerName))
        {
            Debug.Log("leerer Name");
            return;
        }

        if (HighScoreController.Instance != null && StatCounter.Instance != null)
        {
            HighScoreController.Instance.AddScore(playerName, StatCounter.Instance.score);
            Debug.Log("eingetragen");
        }

        if (gameOverButtonsPanel != null)
        {
            gameOverButtonsPanel.SetActive(true);
        }

        enterNamePanel.SetActive(false);

        if (EventSystem.current != null && primaryGameOverButton != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            primaryGameOverButton.Select();
        }
    }
    

    public void BackToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuScene);
    }

    public void Retry()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    
}
