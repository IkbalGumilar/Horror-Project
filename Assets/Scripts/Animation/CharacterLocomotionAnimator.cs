using UnityEngine;

[System.Serializable]
public sealed class CharacterLocomotionSettings
{
    [Header("Speed Thresholds")]
    [SerializeField, Min(0f)] private float movementThreshold = 0.05f;
    [SerializeField, Min(0f)] private float fastWalkThreshold = 2.4f;
    [SerializeField, Min(0f)] private float runThreshold = 4.2f;

    [Header("Playback")]
    [SerializeField, Min(0f)] private float crossFadeDuration = 0.12f;
    [SerializeField] private bool matchPlaybackToMovementSpeed = true;
    [SerializeField, Min(0.01f)] private float walkReferenceSpeed = 1.7f;
    [SerializeField, Min(0.01f)] private float fastWalkReferenceSpeed = 3.2f;
    [SerializeField, Min(0.01f)] private float runReferenceSpeed = 5.2f;
    [SerializeField] private Vector2 playbackSpeedRange = new Vector2(0.65f, 1.5f);

    public float MovementThreshold => Mathf.Max(0f, movementThreshold);
    public float FastWalkThreshold => Mathf.Max(MovementThreshold, fastWalkThreshold);
    public float RunThreshold => Mathf.Max(FastWalkThreshold, runThreshold);
    public float CrossFadeDuration => Mathf.Max(0f, crossFadeDuration);
    public bool MatchPlaybackToMovementSpeed => matchPlaybackToMovementSpeed;
    public float WalkReferenceSpeed => Mathf.Max(0.01f, walkReferenceSpeed);
    public float FastWalkReferenceSpeed => Mathf.Max(0.01f, fastWalkReferenceSpeed);
    public float RunReferenceSpeed => Mathf.Max(0.01f, runReferenceSpeed);
    public Vector2 PlaybackSpeedRange => playbackSpeedRange;
}

[DisallowMultipleComponent]
public sealed class CharacterLocomotionAnimator : MonoBehaviour
{
    private static readonly int WalkState = Animator.StringToHash("Base Layer.Walk");
    private static readonly int FastWalkState = Animator.StringToHash("Base Layer.FastWalk");
    private static readonly int RunState = Animator.StringToHash("Base Layer.Run");

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform movementRoot;
    [SerializeField] private FPSPlayerController playerController;
    [SerializeField] private CharacterLocomotionSettings settings = new CharacterLocomotionSettings();

    private Vector3 previousPosition;
    private LocomotionState currentState = LocomotionState.None;
    private float currentSpeed;
    private AnimatorCullingMode cullingMode = AnimatorCullingMode.CullUpdateTransforms;

    public Animator TargetAnimator => animator;
    public float CurrentSpeed => currentSpeed;
    public string CurrentStateName => currentState.ToString();

    public void Initialize(
        Animator targetAnimator,
        Transform trackedMovementRoot,
        FPSPlayerController fpsPlayerController = null,
        CharacterLocomotionSettings locomotionSettings = null,
        AnimatorCullingMode targetCullingMode = AnimatorCullingMode.CullUpdateTransforms)
    {
        animator = targetAnimator;
        movementRoot = trackedMovementRoot != null ? trackedMovementRoot : transform;
        playerController = fpsPlayerController;
        settings = locomotionSettings ?? settings ?? new CharacterLocomotionSettings();
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
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            return;
        }

        currentSpeed = ReadHorizontalSpeed();
        if (currentSpeed <= settings.MovementThreshold)
        {
            animator.speed = 0f;
            return;
        }

        LocomotionState targetState = SelectState(currentSpeed);
        int targetStateHash = GetStateHash(targetState);
        if (targetState != currentState)
        {
            animator.speed = 1f;
            animator.CrossFade(targetStateHash, settings.CrossFadeDuration, 0);
            currentState = targetState;
        }

        animator.speed = GetPlaybackSpeed(targetState, currentSpeed);
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
    }

    private void PrepareAnimator()
    {
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            return;
        }

        animator.applyRootMotion = false;
        animator.cullingMode = cullingMode;
        if (currentState == LocomotionState.None && animator.HasState(0, WalkState))
        {
            animator.speed = 1f;
            animator.Play(WalkState, 0, 0f);
            animator.Update(0f);
            animator.speed = 0f;
            currentState = LocomotionState.Walk;
        }
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

    private LocomotionState SelectState(float speed)
    {
        if (playerController != null && playerController.IsSprinting)
        {
            return LocomotionState.Run;
        }

        if (speed >= settings.RunThreshold)
        {
            return LocomotionState.Run;
        }

        return speed >= settings.FastWalkThreshold
            ? LocomotionState.FastWalk
            : LocomotionState.Walk;
    }

    private float GetPlaybackSpeed(LocomotionState state, float speed)
    {
        if (!settings.MatchPlaybackToMovementSpeed)
        {
            return 1f;
        }

        float referenceSpeed = state switch
        {
            LocomotionState.FastWalk => settings.FastWalkReferenceSpeed,
            LocomotionState.Run => settings.RunReferenceSpeed,
            _ => settings.WalkReferenceSpeed
        };
        Vector2 playbackSpeedRange = settings.PlaybackSpeedRange;
        float minimum = Mathf.Min(playbackSpeedRange.x, playbackSpeedRange.y);
        float maximum = Mathf.Max(playbackSpeedRange.x, playbackSpeedRange.y);
        return Mathf.Clamp(speed / referenceSpeed, minimum, maximum);
    }

    private static int GetStateHash(LocomotionState state)
    {
        return state switch
        {
            LocomotionState.FastWalk => FastWalkState,
            LocomotionState.Run => RunState,
            _ => WalkState
        };
    }

    private enum LocomotionState
    {
        None,
        Walk,
        FastWalk,
        Run
    }
}
