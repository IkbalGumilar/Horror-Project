using UnityEngine;

[DisallowMultipleComponent]
public sealed class VillagerWanderController : MonoBehaviour
{
    private const int GroundRaycastHeight = 50;
    private const int GroundRaycastDistance = 100;

    private readonly Collider[] overlapResults = new Collider[12];
    private readonly RaycastHit[] castResults = new RaycastHit[12];

    private VillagerConversation conversation;
    private Collider ownCollider;
    private Vector3 homePosition;
    private Vector3 destination;
    private Vector2 idleDurationRange;
    private LayerMask obstacleMask;
    private float wanderRadius;
    private float movementSpeed;
    private float rotationSpeed;
    private float obstacleRadius;
    private float obstacleCheckDistance;
    private float groundOffset;
    private float idleUntil;
    private bool hasDestination;
    private bool wasMovementLocked;

    public void Initialize(
        VillagerConversation villagerConversation,
        float radius,
        float speed,
        float turnSpeed,
        Vector2 idleRange,
        float avoidanceRadius,
        float avoidanceDistance,
        LayerMask blockingLayers)
    {
        conversation = villagerConversation;
        wanderRadius = Mathf.Max(0f, radius);
        movementSpeed = Mathf.Max(0f, speed);
        rotationSpeed = Mathf.Max(0f, turnSpeed);
        idleDurationRange = new Vector2(
            Mathf.Max(0f, Mathf.Min(idleRange.x, idleRange.y)),
            Mathf.Max(0f, Mathf.Max(idleRange.x, idleRange.y)));
        obstacleRadius = Mathf.Max(0f, avoidanceRadius);
        obstacleCheckDistance = Mathf.Max(0f, avoidanceDistance);
        obstacleMask = blockingLayers;
        homePosition = transform.position;
        CacheGroundOffset();
        WaitBeforeNextDestination();
    }

    private void Awake()
    {
        ownCollider = GetComponent<Collider>();
        if (conversation == null)
        {
            conversation = GetComponent<VillagerConversation>();
        }

        homePosition = transform.position;
        CacheGroundOffset();
    }

    private void Update()
    {
        bool movementLocked = conversation != null && conversation.MovementLocked;
        if (movementLocked)
        {
            hasDestination = false;
            wasMovementLocked = true;
            return;
        }

        if (wasMovementLocked)
        {
            wasMovementLocked = false;
            WaitBeforeNextDestination();
        }

        if (Time.time < idleUntil)
        {
            return;
        }

        if (!hasDestination && !TryChooseDestination())
        {
            WaitBeforeNextDestination();
            return;
        }

        MoveToDestination();
    }

    private void MoveToDestination()
    {
        Vector3 horizontalDirection = destination - transform.position;
        horizontalDirection.y = 0f;
        float remainingDistance = horizontalDirection.magnitude;
        if (remainingDistance <= 0.05f)
        {
            transform.position = destination;
            WaitBeforeNextDestination();
            return;
        }

        Vector3 direction = horizontalDirection / remainingDistance;
        float moveDistance = Mathf.Min(movementSpeed * Time.deltaTime, remainingDistance);
        if (HasObstacleAhead(direction, moveDistance))
        {
            WaitBeforeNextDestination();
            return;
        }

        if (direction.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime);
        }

        Vector3 nextPosition = transform.position + direction * moveDistance;
        if (TryGetGroundHeight(nextPosition, out float groundHeight))
        {
            nextPosition.y = groundHeight + groundOffset;
        }

        transform.position = nextPosition;
    }

    private bool TryChooseDestination()
    {
        for (int attempt = 0; attempt < 12; attempt++)
        {
            Vector2 randomOffset = Random.insideUnitCircle * wanderRadius;
            Vector3 candidate = homePosition + new Vector3(randomOffset.x, 0f, randomOffset.y);
            if (TryGetGroundHeight(candidate, out float groundHeight))
            {
                candidate.y = groundHeight + groundOffset;
            }

            if (IsPositionBlocked(candidate))
            {
                continue;
            }

            destination = candidate;
            hasDestination = true;
            return true;
        }

        return false;
    }

    private bool HasObstacleAhead(Vector3 direction, float moveDistance)
    {
        float checkDistance = Mathf.Max(moveDistance, obstacleCheckDistance);
        if (obstacleRadius <= 0f || checkDistance <= 0f)
        {
            return false;
        }

        Vector3 origin = transform.position + Vector3.up * obstacleRadius;
        int hitCount = Physics.SphereCastNonAlloc(
            origin,
            obstacleRadius,
            direction,
            castResults,
            checkDistance,
            obstacleMask,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hitCount; i++)
        {
            Collider hitCollider = castResults[i].collider;
            if (IsBlockingCollider(hitCollider))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsPositionBlocked(Vector3 position)
    {
        if (obstacleRadius <= 0f)
        {
            return false;
        }

        Vector3 center = position + Vector3.up * obstacleRadius;
        int overlapCount = Physics.OverlapSphereNonAlloc(
            center,
            obstacleRadius,
            overlapResults,
            obstacleMask,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < overlapCount; i++)
        {
            if (IsBlockingCollider(overlapResults[i]))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsBlockingCollider(Collider candidate)
    {
        if (candidate == null || candidate == ownCollider || candidate is TerrainCollider)
        {
            return false;
        }

        bool belongsToThisVillager = candidate.transform == transform
            || candidate.transform.IsChildOf(transform)
            || transform.IsChildOf(candidate.transform);
        return !belongsToThisVillager;
    }

    private void CacheGroundOffset()
    {
        groundOffset = TryGetGroundHeight(transform.position, out float groundHeight)
            ? transform.position.y - groundHeight
            : 0f;
    }

    private void WaitBeforeNextDestination()
    {
        hasDestination = false;
        idleUntil = Time.time + Random.Range(idleDurationRange.x, idleDurationRange.y);
    }

    private static bool TryGetGroundHeight(Vector3 position, out float height)
    {
        Terrain[] terrains = Terrain.activeTerrains;
        for (int i = 0; i < terrains.Length; i++)
        {
            Terrain terrain = terrains[i];
            Vector3 origin = terrain.transform.position;
            Vector3 size = terrain.terrainData.size;
            if (position.x < origin.x || position.x > origin.x + size.x
                || position.z < origin.z || position.z > origin.z + size.z)
            {
                continue;
            }

            height = terrain.SampleHeight(position) + origin.y;
            return true;
        }

        Vector3 rayOrigin = position + Vector3.up * GroundRaycastHeight;
        if (Physics.Raycast(
            rayOrigin,
            Vector3.down,
            out RaycastHit hit,
            GroundRaycastDistance,
            ~0,
            QueryTriggerInteraction.Ignore))
        {
            height = hit.point.y;
            return true;
        }

        height = position.y;
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 center = Application.isPlaying ? homePosition : transform.position;
        Gizmos.color = new Color(0.2f, 0.75f, 1f, 0.7f);
        Gizmos.DrawWireSphere(center, wanderRadius);
    }
}
