using UnityEngine;

public sealed class FPSHeadBob : MonoBehaviour
{
    [Header("Shared Profile")]
    [SerializeField] private FPSControlProfile controlProfile;

    [Header("References")]
    [SerializeField] private FPSPlayerController playerController;
    [SerializeField] private FPSCameraController cameraController;
    [SerializeField] private Transform bobTarget;

    [Header("Bob Amount")]
    [SerializeField] private Vector2 walkAmplitude = Vector2.zero;
    [SerializeField] private Vector2 sprintAmplitude = Vector2.zero;
    [SerializeField] private Vector2 crouchAmplitude = Vector2.zero;

    [Header("Bob Frequency")]
    [SerializeField] private float walkFrequency;
    [SerializeField] private float sprintFrequency;
    [SerializeField] private float crouchFrequency;

    [Header("Blend")]
    [SerializeField] private float movementThreshold = 0.05f;
    [SerializeField] private float positionLerpSpeed = 12f;

    private Vector3 baseLocalPosition;
    private bool hasBaseLocalPosition;
    private float bobTimer;

    private void Awake()
    {
        ApplyControlProfile();
        ResolveReferences();
        CacheBaseLocalPosition();
    }

    private void ApplyControlProfile()
    {
        if (controlProfile == null)
        {
            return;
        }

        walkAmplitude = controlProfile.WalkAmplitude;
        sprintAmplitude = controlProfile.SprintAmplitude;
        crouchAmplitude = controlProfile.CrouchAmplitude;
        walkFrequency = controlProfile.WalkFrequency;
        sprintFrequency = controlProfile.SprintFrequency;
        crouchFrequency = controlProfile.CrouchFrequency;
        movementThreshold = controlProfile.MovementThreshold;
        positionLerpSpeed = controlProfile.BobPositionLerpSpeed;
    }

    private void OnEnable()
    {
        ApplyControlProfile();
        ResolveReferences();
        CacheBaseLocalPosition();
    }

    private void LateUpdate()
    {
        ResolveReferences();
        if (bobTarget == null || playerController == null)
        {
            return;
        }

        if (!hasBaseLocalPosition)
        {
            CacheBaseLocalPosition();
        }

        Vector3 targetLocalPosition = baseLocalPosition;
        bool shouldBob = playerController.IsGrounded && playerController.HorizontalSpeed > movementThreshold;

        if (shouldBob)
        {
            Vector2 amplitude = GetCurrentAmplitude() * GameGameplaySettings.HeadBobIntensity;
            float frequency = GetCurrentFrequency();
            bobTimer += Time.deltaTime * frequency;

            targetLocalPosition += new Vector3(
                Mathf.Sin(bobTimer) * amplitude.x,
                Mathf.Cos(bobTimer * 2f) * amplitude.y,
                0f);
        }
        else
        {
            bobTimer = 0f;
        }

        bobTarget.localPosition = Vector3.Lerp(
            bobTarget.localPosition,
            targetLocalPosition,
            positionLerpSpeed * Time.deltaTime);
    }

    public void ResetBaseLocalPosition()
    {
        CacheBaseLocalPosition();
    }

    private void ResolveReferences()
    {
        if (playerController == null)
        {
            playerController = GetComponentInParent<FPSPlayerController>();
        }

        if (cameraController == null)
        {
            cameraController = GetComponentInParent<FPSCameraController>();
        }

        if (bobTarget == null && cameraController != null && cameraController.PlayerCamera != null)
        {
            bobTarget = cameraController.PlayerCamera.transform;
        }
    }

    private void CacheBaseLocalPosition()
    {
        if (bobTarget == null)
        {
            return;
        }

        baseLocalPosition = bobTarget.localPosition;
        hasBaseLocalPosition = true;
    }

    private Vector2 GetCurrentAmplitude()
    {
        if (playerController.IsCrouching)
        {
            return crouchAmplitude;
        }

        return playerController.IsSprinting ? sprintAmplitude : walkAmplitude;
    }

    private float GetCurrentFrequency()
    {
        if (playerController.IsCrouching)
        {
            return crouchFrequency;
        }

        return playerController.IsSprinting ? sprintFrequency : walkFrequency;
    }
}
