using UnityEngine.SceneManagement;
using UnityEngine;

public class InputController : MonoBehaviour
{
    public static InputController Instance { get; private set; }

    public InputSystem_Actions Actions { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Actions = new InputSystem_Actions();

        Actions.Disable();
        Actions.Player.Enable();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (Actions == null)
        {
            return;
        }

        // Make sure the correct action map is always active for the scene we just
        // landed in, regardless of what state it was left in (e.g. paused) before
        // the scene change. Prevents gameplay input from staying disabled after
        // returning from the main menu.
        if (scene.name == "MainMenu")
        {
            Actions.Player.Disable();
            Actions.UI.Enable();
        }
        else
        {
            Actions.UI.Disable();
            Actions.Player.Enable();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Actions?.Disable();
            Instance = null;
        }
    }
}
