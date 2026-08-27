using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class CharacterMovement : MonoBehaviour
{
    private Animator anim;

    private const float lookThreshold = 0.01f;

        [Tooltip("How long the character must be airborne before the falling animation kicks in. Filters out brief ungrounded blips from stairs/small bumps.")]
    [SerializeField] private float fallAnimationBuffer = 0.15f;
    [Tooltip("Extra distance below the capsule to check for ground, to smooth out stairs and small bumps that would otherwise briefly report as not-grounded.")]
    [SerializeField] private float groundCheckDistance = 0.3f;
    [SerializeField] private float gravity = -9.81f;

    [SerializeField] private Transform cameraTarget;
    [SerializeField] private float topClamp = 70f;
    [SerializeField] private float bottomClamp = -30f;
    
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] footstepClips;
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] private float stepDistance = 2f;
    [SerializeField] private float footstepVolume = 0.7f;
    [SerializeField] private float jumpVolume = 0.8f;
    [SerializeField] private Vector2 footstepPitchRange = new Vector2(0.95f, 1.05f);
    //[Tooltip("Gamepad look speed in degrees per second (right stick).")]
    //[SerializeField] private float lookSpeed = 50f;
    //[Tooltip("Mouse look sensitivity. Applied to raw pointer delta, so it is frame-rate independent.")]
    //[SerializeField] private float mouseLookSensitivity = 0.8f;
    [Tooltip("How quickly the character rotates to face the direction of movement. Higher is snappier.")]
    [SerializeField] private float turnSpeed = 20f;
    
    private int groundLayerMask;
    private CharacterController characterController;
    //private InputSystem_Actions inputActions;

    private bool lookFromGamepad;
    private Vector2 look;
    private Vector2 moveInput;

    private float yaw;
    private float pitch;

    private float ungroundedTime;
    private float verticalVelocity;
    
    private float distanceSinceLastStep;
    private Vector3 currentVelocity;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        //inputActions = new InputSystem_Actions();
        anim = GetComponent<Animator>();

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        groundLayerMask = ~(1 << gameObject.layer);

        yaw = transform.eulerAngles.y;
        pitch = 20f;
    }


    private bool IsGrounded()
    {
        if (characterController.isGrounded)
        {
            return true;
        }

        Vector3 origin = transform.position + characterController.center;
        float radius = Mathf.Max(0.01f, characterController.radius - characterController.skinWidth);

        return Physics.SphereCast(
            origin,
            radius,
            Vector3.down,
            out _,
            (characterController.height * 0.5f) + groundCheckDistance,
            groundLayerMask,
            QueryTriggerInteraction.Ignore
        );
    }


    public void ResetGroundedState()
    {
        ungroundedTime = 0f;

        if (characterController != null && characterController.isGrounded && verticalVelocity < 0)
        {
            verticalVelocity = -2f;
        }
    }



    private void OnEnable()
    {
        InputController input = InputController.Instance;
        if (input == null || input.Actions == null)
        {
            return;
        }

        input.Actions.Player.Move.performed += OnMove;
        input.Actions.Player.Move.canceled += OnMove;

        input.Actions.Player.Jump.performed += OnJump;

        input.Actions.Player.Look.performed += OnLook;
        input.Actions.Player.Look.canceled += OnLook;
    }

    private void OnDisable()
    {
        InputController input = InputController.Instance;
        if (input == null || input.Actions == null)
        {
            return;
        }

        input.Actions.Player.Move.performed -= OnMove;
        input.Actions.Player.Move.canceled -= OnMove;

        input.Actions.Player.Jump.performed -= OnJump;

        input.Actions.Player.Look.performed -= OnLook;
        input.Actions.Player.Look.canceled -= OnLook;
    }

    private void Update()
    {
        MovePlayer();
        ApplyGravity();
        UpdateFootsteps();
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
        if (IsGrounded())
        {
            verticalVelocity = Mathf.Sqrt(PlayerState.Instance.currentJumpForce * -2f * gravity);
            PlayJumpSound();
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
                turnSpeed * Time.deltaTime);
        }
    }

    private void ApplyGravity()
    {
        bool grounded = IsGrounded();

        if (grounded)
        {
            ungroundedTime = 0f;

            if (verticalVelocity < 0)
            {
                verticalVelocity = -2f;
            }
        }
        else
        {
            ungroundedTime += Time.deltaTime;
        }

        verticalVelocity += gravity * Time.deltaTime;

        characterController.Move(Vector3.up * verticalVelocity * Time.deltaTime);
    }

    private void OnLook(InputAction.CallbackContext context)
    {
        look = context.ReadValue<Vector2>();

        // Remember which kind of device produced this input. Stick input is an
        // absolute amount that must be scaled by time, whereas pointer input is
        // already a per-frame delta and must not be.
        if (context.control != null)
        {
            lookFromGamepad = context.control.device is Gamepad || context.control.device is Joystick;
        }
    }

    private void Look()
    {

        if(look.sqrMagnitude >= lookThreshold)
        {
            float multiplier = lookFromGamepad
                ? Time.deltaTime * SaveSystem.Instance.controllerSensitivity
                : SaveSystem.Instance.mouseSensitivity;

            yaw += look.x * multiplier;
            pitch -= look.y * multiplier;
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
        // Treat brief ungrounded moments (stairs, small bumps, resuming from pause) as
        // still grounded for animation purposes, so only a genuine fall triggers the
        // falling animation.
        bool isAnimGrounded = IsGrounded() || ungroundedTime < fallAnimationBuffer;

        Vector3 horizontalVelocity = currentVelocity;
        horizontalVelocity.y = 0;

        float speed = horizontalVelocity.magnitude;

        anim.SetFloat("Speed", speed);

        if (!isAnimGrounded)
        {
            anim.SetFloat("Speed", 0);
        }

        anim.SetBool("Grounded", isAnimGrounded);

        anim.SetFloat("VerticalVelocity", verticalVelocity);
    }

    private void UpdateFootsteps()
    {
        if (!IsGrounded())
        {
            distanceSinceLastStep = 0f;
            return;
        }

        Vector3 horizontalVelocity = currentVelocity;
        horizontalVelocity.y = 0f;

        if (horizontalVelocity.magnitude < 0.1f)
        {
            return;
        }

        distanceSinceLastStep += horizontalVelocity.magnitude * Time.deltaTime;

        if (distanceSinceLastStep >= stepDistance)
        {
            distanceSinceLastStep = 0f;
            PlayFootstepSound();
        }
    }

    private void PlayFootstepSound()
    {
        if (audioSource == null || footstepClips == null || footstepClips.Length == 0)
        {
            return;
        }

        AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];
        audioSource.pitch = Random.Range(footstepPitchRange.x, footstepPitchRange.y);
        audioSource.PlayOneShot(clip, footstepVolume);
    }

    private void PlayJumpSound()
    {
        if (audioSource == null || jumpClip == null)
        {
            return;
        }

        audioSource.pitch = 1f;
        audioSource.PlayOneShot(jumpClip, jumpVolume);
    }

}
