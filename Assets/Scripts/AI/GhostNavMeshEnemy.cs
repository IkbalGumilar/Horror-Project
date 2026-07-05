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

    private Vector3 startPosition;
    private Vector3 patrolDestination;

    public bool IsChasing { get; private set; }
    public bool IsInAttackRange { get; private set; }
    public float ChaseDuration { get; private set; }

    private void Awake()
    {
        startPosition = transform.position;
        startPosition.y = Mathf.Max(startPosition.y, minimumHeight);
        patrolDestination = startPosition;
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
            IsChasing = distanceToTarget <= detectionRange;
            IsInAttackRange = distanceToTarget <= attackDistance;
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
        Vector3 nextPosition = Vector3.MoveTowards(transform.position, destination, speed * Time.deltaTime);
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
    }
}
