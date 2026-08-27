using UnityEngine;
using UnityEngine.InputSystem;

public class CursorController : MonoBehaviour
{
    public static CursorController Instance { get; private set; }

    private bool usingGamepad = false;

    private bool inGameplay = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        SetMenuCursor();
    }

    //_______________________________________________________________________________________
    //KI unterstützt
    //Tool: ChatGPT (OpenAI, GPT-5.5)
    //Prompt: ist es möglich in Unity zu unterscheiden ob gerade mit Maus und Tastatur oder mit Controller gespielt wird?

    private void Update()
    {
        if (inGameplay)
        {
            return;
        }

        if (Mouse.current != null && Mouse.current.delta.ReadValue().sqrMagnitude > 0.01f)
        {
            if (usingGamepad)
            {
                usingGamepad = false;
                SetMenuCursor();
                Debug.Log("using Mouse");
            }
        }

        if (Gamepad.current != null)
        {
            if (Gamepad.current.leftStick.ReadValue().sqrMagnitude > 0.01f || 
                Gamepad.current.dpad.ReadValue().sqrMagnitude > 0.01f || 
                Gamepad.current.buttonSouth.wasPressedThisFrame)
            {
                if (!usingGamepad)
                {
                    usingGamepad = true;
                    SetMenuCursor();
                    Debug.Log("using Controller");
                }
            }
        }
    }

    //_______________________________________________________________________________________

    public void SetGameplayCursor()
    {
        inGameplay = true;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void SetMenuCursor()
    {
        inGameplay = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = !usingGamepad;
    }
}
