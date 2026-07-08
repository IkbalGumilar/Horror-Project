using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public sealed class FPSPlayerController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string playerMapName = "Player";
    [SerializeField] private string moveActionName = "Move";
    [SerializeField] private string jumpActionName = "Jump";
    [SerializeField] private string sprintActionName = "Sprint";
    [SerializeField] private string crouchActionName = "Crouch";

    [Header("References")]
    [SerializeField] private FPSCameraController cameraController;
    [SerializeField] private PlayerStamina playerStamina;
    [SerializeField] private PlayerAudioController playerAudio;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 3.2f;
    [SerializeField] private float sprintSpeed = 5.2f;
    [SerializeField] private float crouchSpeed = 1.7f;
    [SerializeField] private float acceleration = 18f;
    [SerializeField] private float gravity = -24f;
    [SerializeField] private float groundedStickForce = -2f;
    [SerializeField] private float jumpHeight = 1f;

    [Header("Crouch")]
    [SerializeField] private float standingHeight = 0.5f;
    [SerializeField] private float crouchingHeight = 1.15f;
    [SerializeField] private float standingCameraHeight = 1.0f;
    [SerializeField] private float crouchingCameraHeight = 1.0f;
    [SerializeField] private float crouchLerpSpeed = 12f;

    [Header("Quick Turn")]
    [SerializeField] private bool enableQuickTurn = true;
    [SerializeField] private float backTapThreshold = -0.75f;
    [SerializeField] private float doubleBackTapWindow = 0.28f;

    private CharacterController characterController;
    private InputActionMap playerMap;
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction sprintAction;
    private InputAction crouchAction;

    private Vector3 currentHorizontalVelocity;
    private float verticalVelocity;
    private float lastBackTapTime = -999f;
    private float lastMoveY;
    private bool isCrouching;
    private bool isSprinting;

    public Vector3 HorizontalVelocity => currentHorizontalVelocity;
    public float HorizontalSpeed => currentHorizontalVelocity.magnitude;
    public bool IsGrounded => characterController != null && characterController.isGrounded;
    public bool IsCrouching => isCrouching;
    public bool IsSprinting => isSprinting;

    private void Reset()
    {
        characterController = GetComponent<CharacterController>();
        cameraController = GetComponent<FPSCameraController>();
    }

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        ResolveReferences();
        ResolveInputActions();
        ApplyControllerDimensions(standingHeight);
        cameraController?.SetCameraHeight(standingCameraHeight, true);
    }

    private void OnEnable()
    {
        moveAction?.Enable();
        jumpAction?.Enable();
        sprintAction?.Enable();
        crouchAction?.Enable();
    }

    private void OnDisable()
    {
        moveAction?.Disable();
        jumpAction?.Disable();
        sprintAction?.Disable();
        crouchAction?.Disable();
    }

    private void Update()
    {
        Vector2 moveInput = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;

        UpdateQuickTurnDetection(moveInput.y);
        UpdateMovement(moveInput);
        UpdateCrouch();
    }

    private void ResolveReferences()
    {
        if (cameraController == null)
        {
            cameraController = GetComponent<FPSCameraController>();
        }

        if (cameraController == null)
        {
            cameraController = GetComponentInChildren<FPSCameraController>();
        }

        if (playerStamina == null)
        {
            playerStamina = GetComponent<PlayerStamina>();
        }

        if (playerAudio == null)
        {
            playerAudio = GetComponent<PlayerAudioController>();
        }
    }

    private void ResolveInputActions()
    {
        if (inputActions == null)
        {
            Debug.LogWarning("FPSPlayerController needs an InputActionAsset assigned.", this);
            return;
        }

        playerMap = inputActions.FindActionMap(playerMapName, true);
        moveAction = playerMap.FindAction(moveActionName, true);
        jumpAction = playerMap.FindAction(jumpActionName, true);
        sprintAction = playerMap.FindAction(sprintActionName, true);
        crouchAction = playerMap.FindAction(crouchActionName, true);
    }

    private void UpdateMovement(Vector2 moveInput)
    {
        bool isGrounded = characterController.isGrounded;
        if (isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = groundedStickForce;
        }

        isCrouching = IsCrouchPressed();
        bool hasStamina = playerStamina == null || playerStamina.CanSprint;
        isSprinting = sprintAction != null && sprintAction.IsPressed() && moveInput.y > 0.1f && !isCrouching && hasStamina;
        float targetSpeed = isCrouching ? crouchSpeed : isSprinting ? sprintSpeed : walkSpeed;
        if (playerStamina != null)
        {
            targetSpeed *= playerStamina.MovementSpeedMultiplier;
        }

        Vector3 inputDirection = new Vector3(moveInput.x, 0f, moveInput.y);
        inputDirection = Vector3.ClampMagnitude(inputDirection, 1f);
        Vector3 targetVelocity = transform.TransformDirection(inputDirection) * targetSpeed;
        currentHorizontalVelocity = Vector3.MoveTowards(
            currentHorizontalVelocity,
            targetVelocity,
            acceleration * Time.deltaTime);

        if (jumpAction != null && jumpAction.WasPressedThisFrame() && isGrounded && !isCrouching)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        verticalVelocity += gravity * Time.deltaTime;
        Vector3 velocity = currentHorizontalVelocity + Vector3.up * verticalVelocity;
        characterController.Move(velocity * Time.deltaTime);
        playerAudio?.UpdateFootsteps(HorizontalSpeed, isGrounded, isSprinting, isCrouching);

        float normalizedStamina = playerStamina != null ? playerStamina.NormalizedStamina : 1f;
        playerAudio?.UpdateBreathing(normalizedStamina, isSprinting, false);
    }

    private void UpdateCrouch()
    {
        isCrouching = IsCrouchPressed();
        float targetHeight = isCrouching ? crouchingHeight : standingHeight;
        float newHeight = Mathf.Lerp(characterController.height, targetHeight, crouchLerpSpeed * Time.deltaTime);
        ApplyControllerDimensions(newHeight);

        float targetCameraHeight = isCrouching ? crouchingCameraHeight : standingCameraHeight;
        cameraController?.SetCameraHeight(targetCameraHeight, false);
    }

    private bool IsCrouchPressed()
    {
        return crouchAction != null && crouchAction.IsPressed();
    }

    private void ApplyControllerDimensions(float height)
    {
        if (characterController == null)
        {
            return;
        }

        characterController.height = height;
        characterController.center = Vector3.zero;
    }

    private void UpdateQuickTurnDetection(float moveY)
    {
        if (!enableQuickTurn || cameraController == null || cameraController.IsQuickTurning)
        {
            lastMoveY = moveY;
            return;
        }

        bool pressedBackThisFrame = moveY <= backTapThreshold && lastMoveY > backTapThreshold;
        if (!pressedBackThisFrame)
        {
            lastMoveY = moveY;
            return;
        }

        if (Time.time - lastBackTapTime <= doubleBackTapWindow)
        {
            cameraController.StartQuickTurn();
            lastBackTapTime = -999f;
        }
        else
        {
            lastBackTapTime = Time.time;
        }

        lastMoveY = moveY;
    }
}
