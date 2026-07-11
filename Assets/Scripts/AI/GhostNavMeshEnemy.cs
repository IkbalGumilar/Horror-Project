using UnityEngine;

public sealed class GhostNavMeshEnemy : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private PlayerHealth targetHealth;
    [SerializeField] private string targetTag = "Player";
    [SerializeField] private float detectionRange = 5f;
    [SerializeField] private LayerMask detectionMask = ~0;
    [SerializeField] private float attackDistance = 1.5f;

    [Header("Patrol")]
    [SerializeField] private Vector3 patrolRange = new Vector3(10f, 10f, 10f);
    [SerializeField] private float minimumHeight = 0.5f;
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float waypointReachDistance = 0.35f;

    [Header("Chase")]
    [SerializeField] private float chaseSpeed = 5f;
    [SerializeField] private float turnSpeed = 8f;
    [SerializeField] private float loseTargetDistance = 35f;
    [SerializeField] private float stuckForgetDuration = 2f;
    [SerializeField] private float stuckMoveThreshold = 0.05f;
    [SerializeField] private bool hasSpottedTarget;
    [SerializeField] private float stuckTimer;

    [Header("Line Of Sight")]
    [SerializeField] private bool requireLineOfSight = true;
    [SerializeField] private bool requireLineOfSightForChase;
    [SerializeField] private LayerMask lineOfSightMask = ~0;
    [SerializeField] private Vector3 lineOfSightOffset = new Vector3(0f, 0.5f, 0f);
    [SerializeField] private float closeSightDistance = 1f;

    [Header("Obstacle Avoidance")]
    [SerializeField] private bool avoidObstacles = true;
    [SerializeField] private LayerMask obstacleMask = ~0;
    [SerializeField] private float obstacleProbeRadius = 0.6f;
    [SerializeField] private float obstacleProbeDistance = 2f;

    private Vector3 startPosition;
    private Vector3 patrolDestination;
    private Collider[] ownColliders;
    private Collider[] targetColliders;
    private readonly Collider[] detectionHits = new Collider[16];
    private readonly RaycastHit[] lineOfSightHits = new RaycastHit[32];
    private readonly RaycastHit[] obstacleHits = new RaycastHit[32];
    private readonly Vector3[] avoidanceCandidates = new Vector3[9];
    private int lastLineOfSightFrame = -1;
    private bool cachedLineOfSight;

    public bool IsChasing { get; private set; }
    public bool IsInAttackRange { get; private set; }
    public float ChaseDuration { get; private set; }
    public bool HasSpottedTarget => hasSpottedTarget;
    public bool HasLineOfSightToTarget => GetLineOfSightToTarget();

    private void Awake()
    {
        startPosition = transform.position;
        startPosition.y = Mathf.Max(startPosition.y, minimumHeight);
        patrolDestination = startPosition;
        ownColliders = GetComponentsInChildren<Collider>();
    }

    private void Start()
    {
        ResolveTarget();
    }

    private void Update()
    {
        if (target == null || targetHealth == null)
        {
            ResolveTarget();
        }

        if (target != null && (targetHealth == null || !targetHealth.IsDead))
        {
            float distanceToTarget = Vector3.Distance(transform.position, target.position);
            bool targetInDetectionSphere = TargetIsInDetectionSphere(distanceToTarget);
            bool canSeeTarget = GetLineOfSightToTarget();

            if (!hasSpottedTarget && targetInDetectionSphere && (!requireLineOfSightForChase || canSeeTarget))
            {
                hasSpottedTarget = true;
                stuckTimer = 0f;
            }

            if (hasSpottedTarget && distanceToTarget > loseTargetDistance)
            {
                ForgetTargetMemory();
            }

            IsChasing = hasSpottedTarget;
            IsInAttackRange = hasSpottedTarget && distanceToTarget <= attackDistance && canSeeTarget;
        }
        else
        {
            ForgetTargetMemory();
        }

        ChaseDuration = IsChasing ? ChaseDuration + Time.deltaTime : 0f;

        if (IsChasing)
        {
            float movedDistance = MoveDirect(target.position, chaseSpeed);
            UpdateStuckMemory(movedDistance);
            FaceTarget();
        }
        else
        {
            stuckTimer = 0f;
            Patrol();
        }
    }

    private void Patrol()
    {
        if ((transform.position - patrolDestination).sqrMagnitude <= waypointReachDistance * waypointReachDistance)
            patrolDestination = PickPatrolDestination();

        MoveDirect(patrolDestination, patrolSpeed);
        FacePosition(patrolDestination);
    }

    private Vector3 PickPatrolDestination()
    {
        float x = Random.Range(-patrolRange.x, patrolRange.x);
        float y = Random.Range(-patrolRange.y, patrolRange.y);
        float z = Random.Range(-patrolRange.z, patrolRange.z);
        return ClampAboveGround(startPosition + new Vector3(x, y, z));
    }

    private float MoveDirect(Vector3 destination, float speed)
    {
        destination = ClampAboveGround(destination);
        Vector3 direction = destination - transform.position;
        float distance = direction.magnitude;
        if (distance <= 0.001f)
        {
            return 0f;
        }

        direction /= distance;
        if (avoidObstacles)
        {
            direction = FindOpenDirection(direction, Mathf.Min(distance, obstacleProbeDistance));
        }

        Vector3 previousPosition = transform.position;
        Vector3 nextPosition = transform.position + direction * Mathf.Min(speed * Time.deltaTime, distance);
        transform.position = ClampAboveGround(nextPosition);
        return Vector3.Distance(previousPosition, transform.position);
    }

    private void UpdateStuckMemory(float movedDistance)
    {
        if (target == null)
        {
            ForgetTargetMemory();
            return;
        }

        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        if (distanceToTarget <= attackDistance)
        {
            stuckTimer = 0f;
            return;
        }

        float minimumExpectedMove = Mathf.Max(0f, stuckMoveThreshold) * Time.deltaTime;
        if (movedDistance <= minimumExpectedMove)
        {
            stuckTimer += Time.deltaTime;
        }
        else
        {
            stuckTimer = 0f;
        }

        if (stuckTimer >= stuckForgetDuration)
        {
            ForgetTargetMemory();
        }
    }

    private void ForgetTargetMemory()
    {
        hasSpottedTarget = false;
        IsChasing = false;
        IsInAttackRange = false;
        stuckTimer = 0f;
    }

    private bool GetLineOfSightToTarget()
    {
        if (lastLineOfSightFrame == Time.frameCount)
        {
            return cachedLineOfSight;
        }

        lastLineOfSightFrame = Time.frameCount;
        cachedLineOfSight = TargetCanBeSeen();
        return cachedLineOfSight;
    }

    private Vector3 ClampAboveGround(Vector3 position)
    {
        position.y = Mathf.Max(position.y, minimumHeight);
        return position;
    }

    private void FaceTarget()
    {
        Vector3 direction = target.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
    }

    private void FacePosition(Vector3 position)
    {
        Vector3 direction = position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
    }

    private void ResolveTarget()
    {
        if (target != null && targetHealth != null)
        {
            CacheTargetColliders();
            return;
        }

        GameObject targetObject = null;
        if (target != null)
        {
            targetObject = target.gameObject;
        }
        else if (!string.IsNullOrWhiteSpace(targetTag))
        {
            targetObject = GameObject.FindGameObjectWithTag(targetTag);
        }

        if (targetObject == null)
        {
            return;
        }

        target = targetObject.transform;
        targetHealth = targetObject.GetComponent<PlayerHealth>();
        CacheTargetColliders();
    }

    private void CacheTargetColliders()
    {
        if (target == null)
        {
            targetColliders = null;
            return;
        }

        if (targetColliders == null || targetColliders.Length == 0)
        {
            targetColliders = target.GetComponentsInChildren<Collider>();
        }
    }

    private bool TargetIsInDetectionSphere(float distanceToTarget)
    {
        if (distanceToTarget <= closeSightDistance)
        {
            return true;
        }

        int hitCount = Physics.OverlapSphereNonAlloc(
            transform.position,
            detectionRange,
            detectionHits,
            detectionMask,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hitCount; i++)
        {
            Collider hitCollider = detectionHits[i];
            if (hitCollider != null && IsTargetCollider(hitCollider))
            {
                return true;
            }
        }

        return false;
    }

    private bool TargetCanBeSeen()
    {
        if (!requireLineOfSight || target == null)
        {
            return true;
        }

        Vector3 origin = transform.position + lineOfSightOffset;
        Vector3 targetPosition = target.position + lineOfSightOffset;
        Vector3 direction = targetPosition - origin;
        float distance = direction.magnitude;
        if (distance <= 0.001f)
        {
            return true;
        }

        if (distance <= closeSightDistance)
        {
            return true;
        }

        int hitCount = Physics.RaycastNonAlloc(
            origin,
            direction / distance,
            lineOfSightHits,
            distance,
            lineOfSightMask,
            QueryTriggerInteraction.Ignore);
        if (hitCount == 0)
        {
            return true;
        }

        Collider closestCollider = null;
        float closestDistance = float.PositiveInfinity;
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = lineOfSightHits[i];
            Collider hitCollider = hit.collider;
            if (hitCollider == null || IsOwnCollider(hitCollider) || hit.distance >= closestDistance)
            {
                continue;
            }

            closestDistance = hit.distance;
            closestCollider = hitCollider;
        }

        return closestCollider == null || IsTargetCollider(closestCollider);
    }

    private Vector3 FindOpenDirection(Vector3 desiredDirection, float checkDistance)
    {
        if (!IsMovementBlocked(desiredDirection, checkDistance))
        {
            return desiredDirection;
        }

        Vector3 right = Vector3.Cross(Vector3.up, desiredDirection);
        if (right.sqrMagnitude < 0.001f)
        {
            right = transform.right;
        }

        right.Normalize();
        Vector3 up = Vector3.up;
        avoidanceCandidates[0] = (desiredDirection + right).normalized;
        avoidanceCandidates[1] = (desiredDirection - right).normalized;
        avoidanceCandidates[2] = (desiredDirection + up).normalized;
        avoidanceCandidates[3] = (desiredDirection - up).normalized;
        avoidanceCandidates[4] = (desiredDirection + right + up).normalized;
        avoidanceCandidates[5] = (desiredDirection - right + up).normalized;
        avoidanceCandidates[6] = right;
        avoidanceCandidates[7] = -right;
        avoidanceCandidates[8] = up;

        Vector3 bestDirection = Vector3.zero;
        float bestScore = -1f;
        for (int i = 0; i < avoidanceCandidates.Length; i++)
        {
            Vector3 candidate = avoidanceCandidates[i];
            if (candidate.sqrMagnitude < 0.001f || IsMovementBlocked(candidate, checkDistance))
            {
                continue;
            }

            float score = Vector3.Dot(desiredDirection, candidate);
            if (score > bestScore)
            {
                bestScore = score;
                bestDirection = candidate;
            }
        }

        return bestDirection.sqrMagnitude > 0.001f ? bestDirection : Vector3.zero;
    }

    private bool IsMovementBlocked(Vector3 direction, float checkDistance)
    {
        if (direction.sqrMagnitude < 0.001f || checkDistance <= 0f)
        {
            return false;
        }

        int hitCount = Physics.SphereCastNonAlloc(
            transform.position,
            Mathf.Max(0.01f, obstacleProbeRadius),
            direction.normalized,
            obstacleHits,
            checkDistance,
            obstacleMask,
            QueryTriggerInteraction.Ignore);

        if (hitCount == 0)
        {
            return false;
        }

        for (int i = 0; i < hitCount; i++)
        {
            Collider hitCollider = obstacleHits[i].collider;
            if (hitCollider == null || IsOwnCollider(hitCollider) || IsTargetCollider(hitCollider))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private bool IsOwnCollider(Collider candidate)
    {
        if (ownColliders == null)
        {
            return false;
        }

        for (int i = 0; i < ownColliders.Length; i++)
        {
            if (ownColliders[i] == candidate)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsTargetCollider(Collider candidate)
    {
        if (targetColliders == null)
        {
            return false;
        }

        for (int i = 0; i < targetColliders.Length; i++)
        {
            if (targetColliders[i] == candidate)
            {
                return true;
            }
        }

        return false;
    }
}
