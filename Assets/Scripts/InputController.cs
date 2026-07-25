using UnityEngine;

public class InputController : MonoBehaviour
{
    public static InputController Instance { get; private set; }

    public InputSystem_Actions Actions { get; private set; }

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
            Actions = new InputSystem_Actions();

            Actions.Disable();
            Actions.Player.Enable();

            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        Actions.Disable();
    }
}
