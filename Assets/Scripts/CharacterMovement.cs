using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class CharacterMovement : MonoBehaviour
{
    private Animator anim;

    private const float lookThreshold = 0.01f;

    [SerializeField] private float gravity = -9.81f;

    [SerializeField] private Transform cameraTarget;
    [SerializeField] private float topClamp = 70f;
    [SerializeField] private float bottomClamp = -30f;
    [SerializeField] private float lookSpeed = 50f;
    
    private CharacterController characterController;
    //private InputSystem_Actions inputActions;

    private Vector2 look;
    private Vector2 moveInput;

    private float yaw;
    private float pitch;

    private float verticalVelocity;
    private Vector3 currentVelocity;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        //inputActions = new InputSystem_Actions();
        anim = GetComponent<Animator>();

        yaw = transform.eulerAngles.y;
        pitch = 20f;
    }

    private void OnEnable()
    {
        //inputActions.Enable();

        InputController.Instance.Actions.Player.Move.performed += OnMove;
        InputController.Instance.Actions.Player.Move.canceled += OnMove;

        InputController.Instance.Actions.Player.Jump.performed += OnJump;

        InputController.Instance.Actions.Player.Look.performed += OnLook;
        InputController.Instance.Actions.Player.Look.canceled += OnLook;
    }

    private void OnDisable()
    {
        InputController.Instance.Actions.Player.Move.performed -= OnMove;
        InputController.Instance.Actions.Player.Move.canceled -= OnMove;

        InputController.Instance.Actions.Player.Jump.performed -= OnJump;

        InputController.Instance.Actions.Player.Look.performed -= OnLook;
        InputController.Instance.Actions.Player.Look.canceled -= OnLook;

        //inputActions.Disable();
    }

    private void Update()
    {
        MovePlayer();
        ApplyGravity();
        UpdateAnimator();
    }

    private void LateUpdate()
    {
        Look();
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        if (characterController.isGrounded)
        {
            verticalVelocity = Mathf.Sqrt(PlayerState.Instance.currentJumpForce * -2f * gravity);
        }
    }

    private void MovePlayer()
    {
        Vector3 forward = Camera.main.transform.forward;
        Vector3 right = Camera.main.transform.right;

        forward.y = 0;
        right.y = 0;

        forward.Normalize();
        right.Normalize();

        Vector3 move = right * moveInput.x + forward * moveInput.y;

        currentVelocity = move * PlayerState.Instance.currentSpeed;

        characterController.Move(move * PlayerState.Instance.currentSpeed * Time.deltaTime);

        if (move.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(move);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                10f * Time.deltaTime);
        }
    }

    private void ApplyGravity()
    {
        if (characterController.isGrounded && verticalVelocity < 0)
        {
            verticalVelocity = -2f;
        }

        verticalVelocity += gravity * Time.deltaTime;

        characterController.Move(Vector3.up * verticalVelocity * Time.deltaTime);
    }

    private void OnLook(InputAction.CallbackContext context)
    {
        look = context.ReadValue<Vector2>();
    }

    private void Look()
    {
        if(look.sqrMagnitude >= lookThreshold)
        {
            float deltaTimeMultiplier = Time.deltaTime * lookSpeed;
            yaw += look.x * deltaTimeMultiplier;
            pitch -= look.y * deltaTimeMultiplier;
        }

        yaw = ClampAngle(yaw, float.MinValue, float.MaxValue);
        pitch = ClampAngle(pitch, bottomClamp, topClamp);

        cameraTarget.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    private float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f)
        {
            lfAngle += 360f;
        }

        if (lfAngle > 360f)
        {
            lfAngle -= 360f;
        }

        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }

    private void UpdateAnimator()
    {
        Vector3 horizontalVelocity = currentVelocity;
        horizontalVelocity.y = 0;

        float speed = horizontalVelocity.magnitude;

        anim.SetFloat("Speed", speed);

        if (!characterController.isGrounded)
        {
            anim.SetFloat("Speed", 0);
        }

        anim.SetBool("Grounded", characterController.isGrounded);

        anim.SetFloat("VerticalVelocity", verticalVelocity);
    }
}
