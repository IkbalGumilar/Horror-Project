using UnityEngine;

public sealed class GhostNavMeshEnemy : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private PlayerHealth targetHealth;
    [SerializeField] private string targetTag = "Player";
    [SerializeField] private float detectionRange = 5f;
    [SerializeField] private float attackDistance = 1.5f;

    [Header("Patrol")]
    [SerializeField] private Vector3 patrolRange = new Vector3(10f, 10f, 10f);
    [SerializeField] private float minimumHeight = 0.5f;
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float waypointReachDistance = 0.35f;

    [Header("Chase")]
    [SerializeField] private float chaseSpeed = 5f;
    [SerializeField] private float turnSpeed = 8f;

    [Header("Line Of Sight")]
    [SerializeField] private bool requireLineOfSight = true;
    [SerializeField] private LayerMask lineOfSightMask = ~0;
    [SerializeField] private Vector3 lineOfSightOffset = new Vector3(0f, 0.5f, 0f);

    [Header("Obstacle Avoidance")]
    [SerializeField] private bool avoidObstacles = true;
    [SerializeField] private LayerMask obstacleMask = ~0;
    [SerializeField] private float obstacleProbeRadius = 0.6f;
    [SerializeField] private float obstacleProbeDistance = 2f;

    private Vector3 startPosition;
    private Vector3 patrolDestination;
    private Collider[] ownColliders;
    private Collider[] targetColliders;

    public bool IsChasing { get; private set; }
    public bool IsInAttackRange { get; private set; }
    public float ChaseDuration { get; private set; }
    public bool HasLineOfSightToTarget => TargetCanBeSeen();

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
            bool canSeeTarget = TargetCanBeSeen();
            IsChasing = distanceToTarget <= detectionRange && canSeeTarget;
            IsInAttackRange = distanceToTarget <= attackDistance && canSeeTarget;
        }
        else
        {
            IsChasing = false;
            IsInAttackRange = false;
        }

        ChaseDuration = IsChasing ? ChaseDuration + Time.deltaTime : 0f;

        if (IsChasing)
        {
            MoveDirect(target.position, chaseSpeed);
            FaceTarget();
        }
        else
        {
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

    private void MoveDirect(Vector3 destination, float speed)
    {
        destination = ClampAboveGround(destination);
        Vector3 direction = destination - transform.position;
        float distance = direction.magnitude;
        if (distance <= 0.001f)
        {
            return;
        }

        direction /= distance;
        if (avoidObstacles)
        {
            direction = FindOpenDirection(direction, Mathf.Min(distance, obstacleProbeDistance));
        }

        Vector3 nextPosition = transform.position + direction * Mathf.Min(speed * Time.deltaTime, distance);
        transform.position = ClampAboveGround(nextPosition);
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
        targetColliders = targetObject.GetComponentsInChildren<Collider>();
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

        RaycastHit[] hits = Physics.RaycastAll(origin, direction / distance, distance, lineOfSightMask, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0)
        {
            return true;
        }

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        for (int i = 0; i < hits.Length; i++)
        {
            Collider hitCollider = hits[i].collider;
            if (hitCollider == null || IsOwnCollider(hitCollider))
            {
                continue;
            }

            return IsTargetCollider(hitCollider);
        }

        return true;
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
        Vector3[] candidates =
        {
            (desiredDirection + right).normalized,
            (desiredDirection - right).normalized,
            (desiredDirection + up).normalized,
            (desiredDirection - up).normalized,
            (desiredDirection + right + up).normalized,
            (desiredDirection - right + up).normalized,
            right,
            -right,
            up
        };

        Vector3 bestDirection = Vector3.zero;
        float bestScore = -1f;
        for (int i = 0; i < candidates.Length; i++)
        {
            Vector3 candidate = candidates[i];
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

        RaycastHit[] hits = Physics.SphereCastAll(
            transform.position,
            Mathf.Max(0.01f, obstacleProbeRadius),
            direction.normalized,
            checkDistance,
            obstacleMask,
            QueryTriggerInteraction.Ignore);

        if (hits == null || hits.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hitCollider = hits[i].collider;
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
