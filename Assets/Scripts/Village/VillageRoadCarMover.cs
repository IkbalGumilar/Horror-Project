using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class VillageRoadCarMover : MonoBehaviour
{
    [Header("Road Source")]
    [SerializeField] private VillageProceduralRoad proceduralRoad;
    [SerializeField] private string proceduralRoadObjectName = "Village Procedural Road";

    [Header("Motion")]
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool placeAtStartOnAwake = true;
    [SerializeField, Min(0.01f)] private float speed = 30f;
    [SerializeField] private float laneOffset = -1.6f;
    [SerializeField, Min(0.01f)] private float turnSpeed = 10f;
    [SerializeField, Min(0.1f)] private float lookAheadDistance = 40f;
    [SerializeField, Range(0.05f, 1f)] private float minTurnSpeedMultiplier = 0.1f;
    [SerializeField, Min(1f)] private float fullSlowdownTurnAngle = 10f;
    [SerializeField, Min(0.01f)] private float acceleration = 2f;
    [SerializeField] private float heightOffset = 0.75f;

    private readonly List<Vector3> path = new List<Vector3>();
    private int targetIndex = 1;
    private bool isMoving;
    private float currentSpeed;

    public bool IsMoving => isMoving;
    public bool HasFinished { get; private set; }

    private void Awake()
    {
        RebuildPath();

        if (placeAtStartOnAwake && path.Count > 0)
        {
            transform.position = GetPathPoint(0);
            FaceAlongPath(instant: true);
        }
    }

    private void Start()
    {
        if (playOnStart)
        {
            Play();
        }
    }

    private void Update()
    {
        if (!isMoving || path.Count < 2)
        {
            return;
        }

        Vector3 target = GetPathPoint(targetIndex);
        Vector3 toTarget = target - transform.position;
        float targetSpeed = speed * GetTurnSpeedMultiplier(targetIndex);
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.deltaTime);
        float step = currentSpeed * Time.deltaTime;

        if (toTarget.magnitude <= step)
        {
            transform.position = target;
            AdvanceTarget();
            return;
        }

        transform.position += toTarget.normalized * step;
        FaceAlongPath(instant: false);
    }

    public void Play()
    {
        if (path.Count < 2)
        {
            RebuildPath();
        }

        HasFinished = false;
        isMoving = path.Count >= 2;
    }

    public void Stop()
    {
        isMoving = false;
    }

    public bool HasPassedRoutePoint(int routePointIndex)
    {
        if (proceduralRoad == null
            || path.Count == 0
            || !proceduralRoad.TryGetRoutePoint(routePointIndex, out Vector3 routePoint))
        {
            return false;
        }

        int closestPathIndex = 0;
        float closestDistance = float.PositiveInfinity;
        for (int i = 0; i < path.Count; i++)
        {
            float distance = Flatten(path[i] - routePoint).sqrMagnitude;
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestPathIndex = i;
            }
        }

        return targetIndex > closestPathIndex || HasFinished;
    }

    public void ResetToStart()
    {
        RebuildPath();
        targetIndex = path.Count > 1 ? 1 : 0;
        currentSpeed = speed;
        HasFinished = false;

        if (path.Count > 0)
        {
            transform.position = GetPathPoint(0);
            FaceAlongPath(instant: true);
        }
    }

    public void RebuildPath()
    {
        path.Clear();

        VillageProceduralRoad road = proceduralRoad != null ? proceduralRoad : FindProceduralRoad();
        if (road == null)
        {
            Debug.LogWarning($"{nameof(VillageRoadCarMover)} could not find '{proceduralRoadObjectName}'.", this);
            return;
        }

        proceduralRoad = road;
        road.CopyCenterline(path);

        if (path.Count > 1)
        {
            targetIndex = 1;
            currentSpeed = speed;
            HasFinished = false;
        }
    }

    private VillageProceduralRoad FindProceduralRoad()
    {
        GameObject roadObject = GameObject.Find(proceduralRoadObjectName);
        if (roadObject == null)
        {
            return FindAnyObjectByType<VillageProceduralRoad>();
        }

        return roadObject.GetComponent<VillageProceduralRoad>();
    }

    private Vector3 WithHeightOffset(Vector3 point)
    {
        point.y += heightOffset;
        return point;
    }

    private Vector3 GetPathPoint(int index)
    {
        Vector3 point = WithHeightOffset(path[index]);
        Vector3 direction = GetPathDirection(index);
        Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;
        return point + right * laneOffset;
    }

    private Vector3 GetPathDirection(int index)
    {
        if (path.Count < 2)
        {
            return transform.forward;
        }

        if (index < path.Count - 1)
        {
            return Flatten(path[index + 1] - path[index]).normalized;
        }

        return Flatten(path[index] - path[index - 1]).normalized;
    }

    private float GetTurnSpeedMultiplier(int index)
    {
        if (path.Count < 3 || index <= 0 || index >= path.Count - 1)
        {
            return 1f;
        }

        Vector3 incoming = Flatten(path[index] - path[index - 1]).normalized;
        Vector3 outgoing = Flatten(path[index + 1] - path[index]).normalized;
        float angle = Vector3.Angle(incoming, outgoing);
        float turnAmount = Mathf.InverseLerp(0f, fullSlowdownTurnAngle, angle);
        return Mathf.Lerp(1f, minTurnSpeedMultiplier, turnAmount);
    }

    private static Vector3 Flatten(Vector3 value)
    {
        value.y = 0f;
        return value;
    }

    private void AdvanceTarget()
    {
        targetIndex++;
        if (targetIndex < path.Count)
        {
            FaceAlongPath(instant: false);
            return;
        }

        targetIndex = path.Count - 1;
        isMoving = false;
        HasFinished = true;
    }

    private void FaceAlongPath(bool instant)
    {
        if (path.Count < 2)
        {
            return;
        }

        Vector3 lookPoint = GetLookAheadPoint();
        Vector3 direction = lookPoint - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        transform.rotation = instant
            ? targetRotation
            : Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
    }

    private Vector3 GetLookAheadPoint()
    {
        if (path.Count == 0)
        {
            return transform.position + transform.forward;
        }

        float remainingDistance = lookAheadDistance;
        Vector3 previousPoint = transform.position;

        for (int i = targetIndex; i < path.Count; i++)
        {
            Vector3 point = GetPathPoint(i);
            float segmentDistance = Vector3.Distance(previousPoint, point);
            if (segmentDistance >= remainingDistance)
            {
                float t = remainingDistance / segmentDistance;
                return Vector3.Lerp(previousPoint, point, t);
            }

            remainingDistance -= segmentDistance;
            previousPoint = point;
        }

        return GetPathPoint(Mathf.Clamp(targetIndex, 0, path.Count - 1));
    }
}
