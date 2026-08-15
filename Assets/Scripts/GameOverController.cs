using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Shows the Game Over panel after the player dies. Deliberately minimal: it waits
/// for the shatter to play, then fades the panel in. Kept separate from the pause
/// menu so a fuller death screen can replace it later without untangling anything.
/// </summary>
public class GameOverController : MonoBehaviour
{
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Button primaryButton;

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

        if (PlayerState.Instance != null)
        {
            PlayerState.Instance.OnPlayerDied += HandlePlayerDied;
        }
        else
        {
            // PlayerState may not have registered yet on the first frame.
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
        // Unscaled so this still works if something else has paused the game.
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

        // Hand control to the UI map so the button can be used with pad or keyboard.
        if (InputController.Instance != null && InputController.Instance.Actions != null)
        {
            InputController.Instance.Actions.Player.Disable();
            InputController.Instance.Actions.UI.Enable();
        }

        if (EventSystem.current != null && primaryButton != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            primaryButton.Select();
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

    //Highscore Namen eintragen --> möglicherweise statt BackToMenu() und Retry() entfernen

    /// <summary>Hooked up to the Game Over button.</summary>
    public void BackToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuScene);
    }

    /// <summary>Restarts the current level. Available if you want a Retry button.</summary>
    public void Retry()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    
}
