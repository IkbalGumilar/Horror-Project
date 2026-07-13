using UnityEngine;

[CreateAssetMenu(fileName = "FPS Control Profile", menuName = "Horror Project/Player/FPS Control Profile")]
public sealed class FPSControlProfile : ScriptableObject
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 7f;
    [SerializeField] private float crouchSpeed = 2.5f;
    [SerializeField] private float acceleration = 20f;
    [SerializeField] private float gravity = -19.6f;
    [SerializeField] private float groundedStickForce = -2f;
    [SerializeField] private float jumpHeight = 2f;

    [Header("Crouch")]
    [SerializeField] private float standingHeight = 2f;
    [SerializeField] private float crouchingHeight = 1.15f;
    [SerializeField] private float standingCameraHeight = 1f;
    [SerializeField] private float crouchingCameraHeight = 0.5f;
    [SerializeField] private float crouchLerpSpeed = 12f;

    [Header("Quick Turn")]
    [SerializeField] private bool enableQuickTurn = true;
    [SerializeField] private float backTapThreshold = -0.75f;
    [SerializeField] private float doubleBackTapWindow = 0.28f;
    [SerializeField] private float quickTurnDuration = 0.18f;

    [Header("Look")]
    [SerializeField] private float mouseSensitivity = 0.12f;
    [SerializeField] private float gamepadSensitivity = 145f;
    [SerializeField] private float minPitch = -50f;
    [SerializeField] private float maxPitch = 50f;
    [SerializeField] private bool lockCursorOnEnable = true;
    [SerializeField] private float cameraHeight = 1f;
    [SerializeField] private float heightLerpSpeed = 12f;

    [Header("Head Bob")]
    [SerializeField] private Vector2 walkAmplitude = new(0f, 0.1f);
    [SerializeField] private Vector2 sprintAmplitude = new(0f, 0.1f);
    [SerializeField] private Vector2 crouchAmplitude = new(0f, 0.01f);
    [SerializeField] private float walkFrequency = 5f;
    [SerializeField] private float sprintFrequency = 6f;
    [SerializeField] private float crouchFrequency = 4f;
    [SerializeField] private float movementThreshold = 0.05f;
    [SerializeField] private float bobPositionLerpSpeed = 12f;

    [Header("Interaction")]
    [SerializeField, Min(0.1f)] private float interactionRadius = 3f;
    [SerializeField, Range(0f, 255f)] private float interactionMinimumAlpha = 150f;
    [SerializeField, Range(0f, 255f)] private float interactionMaximumAlpha = 255f;
    [SerializeField, Min(0.01f)] private float interactionBlinkCyclesPerSecond = 2.5f;

    public float WalkSpeed => walkSpeed;
    public float SprintSpeed => sprintSpeed;
    public float CrouchSpeed => crouchSpeed;
    public float Acceleration => acceleration;
    public float Gravity => gravity;
    public float GroundedStickForce => groundedStickForce;
    public float JumpHeight => jumpHeight;
    public float StandingHeight => standingHeight;
    public float CrouchingHeight => crouchingHeight;
    public float StandingCameraHeight => standingCameraHeight;
    public float CrouchingCameraHeight => crouchingCameraHeight;
    public float CrouchLerpSpeed => crouchLerpSpeed;
    public bool EnableQuickTurn => enableQuickTurn;
    public float BackTapThreshold => backTapThreshold;
    public float DoubleBackTapWindow => doubleBackTapWindow;
    public float QuickTurnDuration => quickTurnDuration;
    public float MouseSensitivity => mouseSensitivity;
    public float GamepadSensitivity => gamepadSensitivity;
    public float MinPitch => minPitch;
    public float MaxPitch => maxPitch;
    public bool LockCursorOnEnable => lockCursorOnEnable;
    public float CameraHeight => cameraHeight;
    public float HeightLerpSpeed => heightLerpSpeed;
    public Vector2 WalkAmplitude => walkAmplitude;
    public Vector2 SprintAmplitude => sprintAmplitude;
    public Vector2 CrouchAmplitude => crouchAmplitude;
    public float WalkFrequency => walkFrequency;
    public float SprintFrequency => sprintFrequency;
    public float CrouchFrequency => crouchFrequency;
    public float MovementThreshold => movementThreshold;
    public float BobPositionLerpSpeed => bobPositionLerpSpeed;
    public float InteractionRadius => interactionRadius;
    public float InteractionMinimumAlpha => interactionMinimumAlpha;
    public float InteractionMaximumAlpha => interactionMaximumAlpha;
    public float InteractionBlinkCyclesPerSecond => interactionBlinkCyclesPerSecond;
}
