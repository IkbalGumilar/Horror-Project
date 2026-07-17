using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public sealed class FPSPlayerController : MonoBehaviour
{
    [Header("Shared Profile")]
    [SerializeField] private FPSControlProfile controlProfile;

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
    private bool sprintToggled;
    private bool initialized;
    private bool movementSuppressed;
    private bool hasModelCameraHeights;
    private float modelStandingCameraHeight;
    private float modelCrouchingCameraHeight;

    public Vector3 HorizontalVelocity => currentHorizontalVelocity;
    public float HorizontalSpeed => currentHorizontalVelocity.magnitude;
    public float MaximumMovementSpeed => Mathf.Max(walkSpeed, sprintSpeed, crouchSpeed);
    public float NormalizedHorizontalSpeed => MaximumMovementSpeed > 0f
        ? Mathf.Clamp01(HorizontalSpeed / MaximumMovementSpeed)
        : 0f;
    public float ConfiguredStandingHeight => controlProfile != null
        ? controlProfile.StandingHeight
        : standingHeight;
    public bool IsGrounded => characterController != null && characterController.isGrounded;
    public bool IsCrouching => isCrouching;
    public bool IsSprinting => isSprinting;
    public InputActionAsset InputActions => inputActions;

    private void Reset()
    {
        characterController = GetComponent<CharacterController>();
        cameraController = GetComponent<FPSCameraController>();
    }

    private void Awake()
    {
        if (enabled)
        {
            Initialize();
        }
    }

    private void Initialize()
    {
        if (initialized)
        {
            return;
        }

        ApplyControlProfile();
        characterController = GetComponent<CharacterController>();
        ResolveReferences();
        ResolveInputActions();
        ApplyControllerDimensions(standingHeight);
        cameraController?.SetCameraHeight(standingCameraHeight, true);
        initialized = true;
    }

    private void ApplyControlProfile()
    {
        if (controlProfile != null)
        {
            walkSpeed = controlProfile.WalkSpeed;
            sprintSpeed = controlProfile.SprintSpeed;
            crouchSpeed = controlProfile.CrouchSpeed;
            acceleration = controlProfile.Acceleration;
            gravity = controlProfile.Gravity;
            groundedStickForce = controlProfile.GroundedStickForce;
            jumpHeight = controlProfile.JumpHeight;
            standingHeight = controlProfile.StandingHeight;
            crouchingHeight = controlProfile.CrouchingHeight;
            standingCameraHeight = controlProfile.StandingCameraHeight;
            crouchingCameraHeight = controlProfile.CrouchingCameraHeight;
            crouchLerpSpeed = controlProfile.CrouchLerpSpeed;
            enableQuickTurn = controlProfile.EnableQuickTurn;
            backTapThreshold = controlProfile.BackTapThreshold;
            doubleBackTapWindow = controlProfile.DoubleBackTapWindow;
        }

        if (hasModelCameraHeights)
        {
            standingCameraHeight = modelStandingCameraHeight;
            crouchingCameraHeight = modelCrouchingCameraHeight;
        }
    }

    private void OnEnable()
    {
        Initialize();
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
        if (movementSuppressed)
        {
            UpdateSuppressedMovement();
            return;
        }

        Vector2 moveInput = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;

        UpdateQuickTurnDetection(moveInput.y);
        UpdateMovement(moveInput);
        UpdateCrouch();
    }

    public void SetMovementSuppressed(bool suppressed)
    {
        movementSuppressed = suppressed;
        if (!suppressed)
        {
            return;
        }

        currentHorizontalVelocity = Vector3.zero;
        isSprinting = false;
        sprintToggled = false;
        lastMoveY = 0f;
    }

    public void SetModelCameraHeight(float standingViewHeight, bool immediate)
    {
        float configuredStandingCameraHeight = controlProfile != null
            ? controlProfile.StandingCameraHeight
            : standingCameraHeight;
        float configuredCrouchingCameraHeight = controlProfile != null
            ? controlProfile.CrouchingCameraHeight
            : crouchingCameraHeight;
        float crouchDrop = Mathf.Max(
            0f,
            configuredStandingCameraHeight - configuredCrouchingCameraHeight);

        hasModelCameraHeights = true;
        modelStandingCameraHeight = standingViewHeight;
        modelCrouchingCameraHeight = Mathf.Max(0f, standingViewHeight - crouchDrop);
        standingCameraHeight = modelStandingCameraHeight;
        crouchingCameraHeight = modelCrouchingCameraHeight;

        if (cameraController == null)
        {
            cameraController = GetComponent<FPSCameraController>();
        }

        cameraController?.SetModelStandingHeight(modelStandingCameraHeight, immediate);
        cameraController?.SetCameraHeight(
            isCrouching ? modelCrouchingCameraHeight : modelStandingCameraHeight,
            immediate);
    }

    private void UpdateSuppressedMovement()
    {
        currentHorizontalVelocity = Vector3.zero;
        isSprinting = false;

        if (characterController == null || !characterController.enabled)
        {
            return;
        }

        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = groundedStickForce;
        }

        verticalVelocity += gravity * Time.deltaTime;
        characterController.Move(Vector3.up * verticalVelocity * Time.deltaTime);

        float normalizedStamina = playerStamina != null ? playerStamina.NormalizedStamina : 1f;
        playerAudio?.UpdateBreathing(normalizedStamina, false, false);
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
        bool sprintRequested = ReadSprintRequested(hasStamina);
        isSprinting = sprintRequested && moveInput.y > 0.1f && !isCrouching && hasStamina;
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

    private bool ReadSprintRequested(bool hasStamina)
    {
        if (sprintAction == null)
        {
            return false;
        }

        if (GameControlSettings.RunMode == RunInputMode.Hold)
        {
            sprintToggled = false;
            return sprintAction.IsPressed();
        }

        if (sprintAction.WasPressedThisFrame())
        {
            sprintToggled = !sprintToggled;
        }

        if (!hasStamina || isCrouching)
        {
            sprintToggled = false;
        }

        return sprintToggled;
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
