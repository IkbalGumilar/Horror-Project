using UnityEngine;
using UnityEngine.InputSystem;

public sealed class FPSCameraController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string playerMapName = "Player";
    [SerializeField] private string lookActionName = "Look";

    [Header("References")]
    [SerializeField] private Transform yawRoot;
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private bool parentCameraToPivotOnAwake = true;
    [SerializeField] private bool resetCameraLocalTransformOnParent = true;

    [Header("Look")]
    [SerializeField] private float mouseSensitivity = 0.12f;
    [SerializeField] private float gamepadSensitivity = 145f;
    [SerializeField] private float minPitch = -82f;
    [SerializeField] private float maxPitch = 82f;
    [SerializeField] private bool lockCursorOnEnable = true;

    [Header("Camera Height")]
    [SerializeField] private float cameraHeight = 1;
    [SerializeField] private float heightLerpSpeed = 12f;

    [Header("Quick Turn")]
    [SerializeField] private float quickTurnDuration = 0.18f;

    private InputActionMap playerMap;
    private InputAction lookAction;
    private float yaw;
    private float pitch;
    private float turnStartYaw;
    private float turnTargetYaw;
    private float turnElapsed;
    private bool isQuickTurning;

    public bool IsQuickTurning => isQuickTurning;
    public Transform CameraPivot => cameraPivot;
    public Camera PlayerCamera => playerCamera;

    private void Reset()
    {
        yawRoot = transform;
        playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera != null)
        {
            cameraPivot = playerCamera.transform.parent != null ? playerCamera.transform.parent : playerCamera.transform;
        }
    }

    private void Awake()
    {
        ResolveReferences();
        ResolveInputActions();

        yaw = yawRoot.eulerAngles.y;
        pitch = cameraPivot != null ? NormalizeAngle(cameraPivot.localEulerAngles.x) : 0f;
        SetCameraHeight(cameraHeight, true);
    }

    private void OnEnable()
    {
        lookAction?.Enable();
        if (lockCursorOnEnable)
        {
            LockCursor();
        }
    }

    private void OnDisable()
    {
        lookAction?.Disable();
        UnlockCursor();
    }

    private void Update()
    {
        Vector2 lookInput = lookAction != null ? lookAction.ReadValue<Vector2>() : Vector2.zero;
        UpdateLook(lookInput);
    }

    public void StartQuickTurn()
    {
        if (isQuickTurning)
        {
            return;
        }

        isQuickTurning = true;
        turnElapsed = 0f;
        turnStartYaw = yaw;
        turnTargetYaw = yaw + 180f;
    }

    public void SetCameraHeight(float height, bool immediate)
    {
        cameraHeight = height;
        if (cameraPivot == null)
        {
            return;
        }

        Vector3 localPosition = cameraPivot.localPosition;
        localPosition.y = immediate
            ? cameraHeight
            : Mathf.Lerp(localPosition.y, cameraHeight, heightLerpSpeed * Time.deltaTime);
        cameraPivot.localPosition = localPosition;
    }

    public void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void ResolveReferences()
    {
        if (yawRoot == null)
        {
            yawRoot = transform;
        }

        if (cameraPivot == null)
        {
            Transform existingPivot = transform.Find("CameraPivot");
            if (existingPivot != null)
            {
                cameraPivot = existingPivot;
            }
        }

        if (cameraPivot == null)
        {
            GameObject pivotObject = new GameObject("CameraPivot");
            cameraPivot = pivotObject.transform;
            cameraPivot.SetParent(yawRoot, false);
        }

        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
        }

        if (playerCamera == null && Camera.main != null)
        {
            playerCamera = Camera.main;
        }

        if (parentCameraToPivotOnAwake && playerCamera != null && playerCamera.transform.parent != cameraPivot)
        {
            playerCamera.transform.SetParent(cameraPivot, false);
            if (resetCameraLocalTransformOnParent)
            {
                playerCamera.transform.localPosition = Vector3.zero;
                playerCamera.transform.localRotation = Quaternion.identity;
            }
        }
    }

    private void ResolveInputActions()
    {
        if (inputActions == null)
        {
            Debug.LogWarning("FPSCameraController needs an InputActionAsset assigned.", this);
            return;
        }

        playerMap = inputActions.FindActionMap(playerMapName, true);
        lookAction = playerMap.FindAction(lookActionName, true);
    }

    private void UpdateLook(Vector2 lookInput)
    {
        bool usingMouse = lookAction != null && lookAction.activeControl != null && lookAction.activeControl.device is Mouse;
        float sensitivity = usingMouse ? mouseSensitivity : gamepadSensitivity * Time.deltaTime;

        if (!isQuickTurning)
        {
            yaw += lookInput.x * sensitivity;
        }

        pitch = Mathf.Clamp(pitch - lookInput.y * sensitivity, minPitch, maxPitch);
        if (cameraPivot != null)
        {
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        UpdateQuickTurnRotation();
        yawRoot.rotation = Quaternion.Euler(0f, yaw, 0f);
    }

    private void UpdateQuickTurnRotation()
    {
        if (!isQuickTurning)
        {
            return;
        }

        turnElapsed += Time.deltaTime;
        float duration = Mathf.Max(0.01f, quickTurnDuration);
        float t = Mathf.Clamp01(turnElapsed / duration);
        t = t * t * (3f - 2f * t);
        yaw = Mathf.LerpAngle(turnStartYaw, turnTargetYaw, t);

        if (turnElapsed >= duration)
        {
            yaw = turnTargetYaw;
            isQuickTurning = false;
        }
    }

    private static float NormalizeAngle(float angle)
    {
        angle %= 360f;
        return angle > 180f ? angle - 360f : angle;
    }
}
