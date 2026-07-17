using UnityEngine;

[DisallowMultipleComponent]
public sealed class VillageChickenController : MonoBehaviour
{
    private static readonly int SpeedParameter = Animator.StringToHash("Speed");
    private static readonly int IncubatingParameter = Animator.StringToHash("IsIncubating");

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private bool isIncubating;

    [Header("Patrol Area")]
    [SerializeField] private Transform patrolSpace;
    [SerializeField] private Vector3 patrolLocalCenter;
    [SerializeField] private Vector2 patrolSize = new(5f, 3.6f);

    [Header("Movement")]
    [SerializeField, Min(0f)] private float walkSpeed = 0.48f;
    [SerializeField, Min(0f)] private float turnSpeed = 5f;
    [SerializeField, Min(0.01f)] private float stoppingDistance = 0.12f;
    [SerializeField] private Vector2 idleDurationRange = new(1.5f, 4.5f);
    [SerializeField, Min(0f)] private float obstacleProbeDistance = 0.24f;
    [SerializeField, Min(0f)] private float obstacleProbeRadius = 0.07f;
    [SerializeField] private LayerMask obstacleLayers = ~0;

    private Vector3 targetLocalPosition;
    private float idleTimer;
    private bool isWalking;

    public bool IsIncubating => isIncubating;

    private void Awake()
    {
        ResolveAnimator();
    }

    private void OnEnable()
    {
        ResolveAnimator();
        ApplyAnimationState();
        BeginIdle();
    }

    private void Update()
    {
        if (isIncubating || patrolSpace == null)
        {
            SetWalking(false);
            return;
        }

        if (!isWalking)
        {
            idleTimer -= Time.deltaTime;
            if (idleTimer <= 0f)
            {
                SelectPatrolTarget();
            }

            return;
        }

        Vector3 targetWorldPosition = patrolSpace.TransformPoint(targetLocalPosition);
        targetWorldPosition.y = transform.position.y;
        Vector3 direction = targetWorldPosition - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= stoppingDistance * stoppingDistance)
        {
            BeginIdle();
            return;
        }

        direction.Normalize();
        Vector3 probeOrigin = transform.position + patrolSpace.up * 0.12f;
        if (obstacleProbeDistance > 0f && Physics.SphereCast(
                probeOrigin,
                obstacleProbeRadius,
                direction,
                out _,
                obstacleProbeDistance,
                obstacleLayers,
                QueryTriggerInteraction.Ignore))
        {
            BeginIdle(0.25f);
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction, patrolSpace.up);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            turnSpeed * Time.deltaTime);

        Vector3 nextPosition = transform.position + direction * (walkSpeed * Time.deltaTime);
        Vector3 nextLocalPosition = patrolSpace.InverseTransformPoint(nextPosition);
        Vector2 halfSize = patrolSize * 0.5f;
        nextLocalPosition.x = Mathf.Clamp(
            nextLocalPosition.x,
            patrolLocalCenter.x - halfSize.x,
            patrolLocalCenter.x + halfSize.x);
        nextLocalPosition.z = Mathf.Clamp(
            nextLocalPosition.z,
            patrolLocalCenter.z - halfSize.y,
            patrolLocalCenter.z + halfSize.y);
        nextLocalPosition.y = patrolSpace.InverseTransformPoint(transform.position).y;
        transform.position = patrolSpace.TransformPoint(nextLocalPosition);
    }

    public void Configure(
        Transform areaSpace,
        Vector3 localCenter,
        Vector2 localSize,
        bool incubating)
    {
        patrolSpace = areaSpace;
        patrolLocalCenter = localCenter;
        patrolSize = localSize;
        isIncubating = incubating;
    }

    public void AssignAnimator(Animator value)
    {
        animator = value;
    }

    public void SetIncubating(bool value)
    {
        isIncubating = value;
        if (!isActiveAndEnabled)
        {
            return;
        }

        ApplyAnimationState();
        if (value)
        {
            SetWalking(false);
        }
        else
        {
            BeginIdle();
        }
    }

    private void ResolveAnimator()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    private void ApplyAnimationState()
    {
        if (animator == null)
        {
            return;
        }

        animator.SetBool(IncubatingParameter, isIncubating);
        animator.SetFloat(SpeedParameter, 0f);
    }

    private void SelectPatrolTarget()
    {
        Vector2 halfSize = patrolSize * 0.5f;
        targetLocalPosition = new Vector3(
            patrolLocalCenter.x + Random.Range(-halfSize.x, halfSize.x),
            0f,
            patrolLocalCenter.z + Random.Range(-halfSize.y, halfSize.y));
        SetWalking(true);
    }

    private void BeginIdle(float minimumDelay = 0f)
    {
        float minimum = Mathf.Min(idleDurationRange.x, idleDurationRange.y);
        float maximum = Mathf.Max(idleDurationRange.x, idleDurationRange.y);
        idleTimer = Mathf.Max(minimumDelay, Random.Range(minimum, maximum));
        SetWalking(false);
    }

    private void SetWalking(bool value)
    {
        isWalking = value;
        if (animator != null)
        {
            animator.SetFloat(SpeedParameter, value ? 1f : 0f, 0.12f, Time.deltaTime);
        }
    }
}
