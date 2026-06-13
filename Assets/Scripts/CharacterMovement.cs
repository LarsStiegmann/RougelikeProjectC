using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class CharacterMovement : MonoBehaviour
{
    private const float lookThreshold = 0.01f;

    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 2f;
    [SerializeField] private float gravity = -9.81f;

    [SerializeField] private Transform cameraTarget;
    [SerializeField] private float topClamp = 70f;
    [SerializeField] private float bottomClamp = -30f;
    [SerializeField] private float lookSpeed = 50f;
    
    private CharacterController characterController;
    private InputSystem_Actions inputActions;

    private Vector2 look;
    private Vector2 moveInput;

    private float yaw;
    private float pitch;

    private float verticalVelocity;


    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        inputActions = new InputSystem_Actions();

        yaw = transform.eulerAngles.y;
        pitch = 20f;
    }

    private void OnEnable()
    {
        inputActions.Enable();

        inputActions.Player.Move.performed += OnMove;
        inputActions.Player.Move.canceled += OnMove;

        inputActions.Player.Jump.performed += OnJump;

        inputActions.Player.Look.performed += OnLook;
        inputActions.Player.Look.canceled += OnLook;
    }

    private void OnDisable()
    {
        inputActions.Player.Move.performed -= OnMove;
        inputActions.Player.Move.canceled -= OnMove;

        inputActions.Player.Jump.performed -= OnJump;

        inputActions.Player.Look.performed -= OnLook;
        inputActions.Player.Look.canceled -= OnLook;

        inputActions.Disable();
    }

    private void Update()
    {
        MovePlayer();
        ApplyGravity();
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
            verticalVelocity = Mathf.Sqrt(jumpForce * -2f * gravity);
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

        characterController.Move(move * moveSpeed * Time.deltaTime);

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

}
