using UnityEngine;

[System.Serializable]
public sealed class CharacterLocomotionSettings
{
    [SerializeField] private string speedParameter = "Speed";
    [SerializeField] private string locomotionLayer = "Locomotion";
    [SerializeField, Min(0f)] private float movementThreshold = 0.05f;
    [SerializeField, Min(0f)] private float dampTime = 0.12f;

    public string SpeedParameter => string.IsNullOrWhiteSpace(speedParameter)
        ? "Speed"
        : speedParameter;
    public string LocomotionLayer => locomotionLayer;
    public float MovementThreshold => Mathf.Max(0f, movementThreshold);
    public float DampTime => Mathf.Max(0f, dampTime);
}

[DisallowMultipleComponent]
public sealed class CharacterLocomotionAnimator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform movementRoot;
    [SerializeField] private FPSPlayerController playerController;
    [SerializeField] private CharacterLocomotionSettings settings =
        new CharacterLocomotionSettings();

    [Header("Blend Tree Range")]
    [SerializeField, Range(0f, 1f)] private float maximumBlendValue = 1f;
    [SerializeField, Min(0.01f)] private float referenceMovementSpeed = 1f;

    private Vector3 previousPosition;
    private AnimatorCullingMode cullingMode = AnimatorCullingMode.CullUpdateTransforms;
    private int speedParameterHash;
    private int locomotionLayerIndex = -1;
    private bool hasSpeedParameter;
    private bool animatorPrepared;
    private float currentMovementSpeed;
    private float currentBlendValue;

    public Animator TargetAnimator => animator;
    public float CurrentSpeed => currentMovementSpeed;
    public float CurrentBlendValue => currentBlendValue;

    public void Initialize(
        Animator targetAnimator,
        Transform trackedMovementRoot,
        FPSPlayerController fpsPlayerController = null,
        CharacterLocomotionSettings locomotionSettings = null,
        float targetMaximumBlendValue = 1f,
        float targetReferenceMovementSpeed = 1f,
        AnimatorCullingMode targetCullingMode = AnimatorCullingMode.CullUpdateTransforms)
    {
        animator = targetAnimator;
        movementRoot = trackedMovementRoot != null ? trackedMovementRoot : transform;
        playerController = fpsPlayerController;
        settings = locomotionSettings ?? settings ?? new CharacterLocomotionSettings();
        maximumBlendValue = Mathf.Clamp01(targetMaximumBlendValue);
        referenceMovementSpeed = Mathf.Max(0.01f, targetReferenceMovementSpeed);
        cullingMode = targetCullingMode;
        previousPosition = movementRoot.position;
        PrepareAnimator();
    }

    private void Awake()
    {
        ResolveReferences();
        previousPosition = movementRoot.position;
        PrepareAnimator();
    }

    private void OnEnable()
    {
        ResolveReferences();
        previousPosition = movementRoot.position;
        PrepareAnimator();
    }

    private void LateUpdate()
    {
        ResolveReferences();
        if (!animatorPrepared)
        {
            PrepareAnimator();
        }

        if (!animatorPrepared || !hasSpeedParameter)
        {
            return;
        }

        currentMovementSpeed = ReadHorizontalSpeed();
        currentBlendValue = CalculateBlendValue(currentMovementSpeed);
        if (settings.DampTime > 0f && Time.deltaTime > 0f)
        {
            animator.SetFloat(
                speedParameterHash,
                currentBlendValue,
                settings.DampTime,
                Time.deltaTime);
        }
        else
        {
            animator.SetFloat(speedParameterHash, currentBlendValue);
        }
    }

    private void ResolveReferences()
    {
        if (movementRoot == null)
        {
            movementRoot = transform;
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>(true);
        }

        if (playerController == null)
        {
            playerController = GetComponent<FPSPlayerController>();
        }

        settings ??= new CharacterLocomotionSettings();
    }

    private void PrepareAnimator()
    {
        animatorPrepared = false;
        hasSpeedParameter = false;
        if (animator == null
            || animator.runtimeAnimatorController == null
            || !animator.isActiveAndEnabled)
        {
            return;
        }

        animator.applyRootMotion = false;
        animator.cullingMode = cullingMode;
        animator.speed = 1f;

        speedParameterHash = Animator.StringToHash(settings.SpeedParameter);
        hasSpeedParameter = HasFloatParameter(speedParameterHash);
        locomotionLayerIndex = string.IsNullOrWhiteSpace(settings.LocomotionLayer)
            ? -1
            : animator.GetLayerIndex(settings.LocomotionLayer);
        if (locomotionLayerIndex >= 0)
        {
            animator.SetLayerWeight(locomotionLayerIndex, 1f);
        }

        currentBlendValue = 0f;
        if (hasSpeedParameter)
        {
            animator.SetFloat(speedParameterHash, 0f);
        }

        animatorPrepared = true;
    }

    private bool HasFloatParameter(int parameterHash)
    {
        AnimatorControllerParameter[] parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].nameHash == parameterHash
                && parameters[i].type == AnimatorControllerParameterType.Float)
            {
                return true;
            }
        }

        Debug.LogWarning(
            $"{nameof(CharacterLocomotionAnimator)} could not find float parameter " +
            $"'{settings.SpeedParameter}' on {animator.name}.",
            this);
        return false;
    }

    private float ReadHorizontalSpeed()
    {
        Vector3 position = movementRoot.position;
        float measuredSpeed = 0f;

        if (playerController != null)
        {
            measuredSpeed = playerController.enabled ? playerController.HorizontalSpeed : 0f;
        }
        else if (Time.deltaTime > 0f)
        {
            Vector3 displacement = position - previousPosition;
            displacement.y = 0f;
            measuredSpeed = displacement.magnitude / Time.deltaTime;
        }

        previousPosition = position;
        return measuredSpeed;
    }

    private float CalculateBlendValue(float movementSpeed)
    {
        if (movementSpeed <= settings.MovementThreshold)
        {
            return 0f;
        }

        float normalizedSpeed = playerController != null
            ? playerController.NormalizedHorizontalSpeed
            : Mathf.Clamp01(movementSpeed / referenceMovementSpeed);
        return Mathf.Min(normalizedSpeed, maximumBlendValue);
    }
}
