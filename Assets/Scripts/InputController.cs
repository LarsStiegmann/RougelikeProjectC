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

    //______________________________________________________________
    //KI unterstützt
    //Tool: ChatGPT (OpenAI, GPT-5.5)
    //Prompt: wie würde ein InputManager aussehen?
    //*Dieses Skript wurde durch verschiedenste Probleme
    //mehrfach komplett überarbeitet und dieser Prompt ist
    //lediglich der Anfang.*
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (Actions == null)
        {
            return;
        }

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
    //______________________________________________________________

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
