using UnityEngine.SceneManagement;
using UnityEngine;

public class InputController : MonoBehaviour
{
    private static InputController instance;

    /// <summary>
    /// Resolves the controller even if this object's Awake has not run yet, which
    /// happens after a domain reload or when another component's OnEnable runs
    /// first. Without this, callers would hit a null Instance.
    /// </summary>
    public static InputController Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<InputController>();
            }

            if (instance != null)
            {
                instance.EnsureInitialized();
            }

            return instance;
        }
    }

    
public InputSystem_Actions Actions { get; private set; }

private void Awake()
    {
        if (instance == null || instance == this)
        {
            instance = this;
            EnsureInitialized();
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
<<<<<<< HEAD

private void EnsureInitialized()
    {
        // Keyed off Actions itself rather than a bool flag: Unity's domain reload
        // restores plain private fields but cannot restore the Actions object, so a
        // flag would survive as 'true' while Actions came back null.
        if (Actions != null)
        {
            return;
        }

        Actions = new InputSystem_Actions();
        Actions.Disable();
        Actions.Player.Enable();

        SceneManager.sceneLoaded -= OnSceneLoaded;
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
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Actions?.Disable();
            instance = null;
        }
    }
=======
>>>>>>> refs/remotes/origin/main
}
