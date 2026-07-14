using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class CityRoadVehicleMover : MonoBehaviour
{
    [Header("Road Source")]
    [SerializeField] private CityProceduralRoad proceduralRoad;
    [SerializeField] private string proceduralRoadObjectName = "City Procedural Road";
    [SerializeField] private Component roadComponent;
    [SerializeField] private string roadObjectName = "road_0001";
    [SerializeField] private bool useRoadTransform = false;

    [Header("Motion")]
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool placeAtStartOnAwake = true;
    [SerializeField, Min(0.01f)] private float speed = 8f;
    [SerializeField] private float laneOffset = -2f;
    [SerializeField, Min(0.01f)] private float turnSpeed = 6f;
    [SerializeField, Min(0.1f)] private float lookAheadDistance = 14f;
    [SerializeField, Range(0.05f, 1f)] private float minTurnSpeedMultiplier = 0.45f;
    [SerializeField, Min(1f)] private float fullSlowdownTurnAngle = 60f;
    [SerializeField, Min(0.01f)] private float acceleration = 8f;
    [SerializeField] private float heightOffset = 0.75f;
    [SerializeField] private bool loop = false;

    private readonly List<Vector3> path = new List<Vector3>();
    private readonly List<float> explicitTargetSpeeds = new List<float>();
    private int targetIndex = 1;
    private int directWaypointStartIndex = int.MaxValue;
    private bool isMoving;
    private float currentSpeed;
    private bool useRouteSpeedProfile;
    private int firstTurnStartIndex = int.MaxValue;
    private int gasStationTurnStartIndex = int.MaxValue;
    private int finalStopTargetIndex = int.MaxValue;
    private float firstTurnSpeed;
    private float gasStationTurnSpeed;
    private float finalStopDeceleration;
    private bool useExplicitSpeedProfile;
    private bool useDepartureLaneProfile;
    private int departureRoadStartIndex = int.MaxValue;

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
        float targetSpeed = GetTargetSpeed(targetIndex);
        float speedChangeRate = useRouteSpeedProfile && targetIndex >= finalStopTargetIndex
            ? finalStopDeceleration
            : acceleration;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, speedChangeRate * Time.deltaTime);
        float step = currentSpeed * Time.deltaTime;

        if (toTarget.magnitude <= Mathf.Max(step, 0.01f))
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

    public bool SwitchToRoad(
        CityProceduralRoad targetRoad,
        bool shouldLoop,
        bool placeAtStart,
        bool startMoving)
    {
        Stop();
        proceduralRoad = targetRoad;
        proceduralRoadObjectName = targetRoad != null ? targetRoad.name : string.Empty;
        roadComponent = null;
        loop = shouldLoop;
        ResetRouteSpeedProfile();
        RebuildPath();

        if (path.Count < 2)
        {
            return false;
        }

        targetIndex = 1;
        currentSpeed = speed;
        if (placeAtStart)
        {
            transform.position = GetPathPoint(0);
            FaceAlongPath(instant: true);
        }

        if (startMoving)
        {
            Play();
        }

        return true;
    }

    public bool SwitchToConnectedRoad(
        CityProceduralRoad entryRoad,
        int exitControlPointIndex,
        CityProceduralRoad continuationRoad,
        bool shouldLoop,
        bool placeAtStart,
        bool startMoving)
    {
        Stop();
        proceduralRoad = entryRoad;
        proceduralRoadObjectName = entryRoad != null ? entryRoad.name : string.Empty;
        roadComponent = null;
        loop = shouldLoop;
        path.Clear();
        directWaypointStartIndex = int.MaxValue;
        ResetRouteSpeedProfile();

        if (entryRoad == null
            || continuationRoad == null
            || !entryRoad.CopyCenterlineThroughControlPoint(exitControlPointIndex, path))
        {
            return false;
        }

        List<Vector3> continuation = new List<Vector3>();
        continuationRoad.CopyCenterline(continuation);
        AppendPath(continuation);
        if (path.Count < 2)
        {
            return false;
        }

        targetIndex = 1;
        currentSpeed = speed;
        if (placeAtStart)
        {
            transform.position = GetPathPoint(0);
            FaceAlongPath(instant: true);
        }

        if (startMoving)
        {
            Play();
        }

        return true;
    }

    public bool SwitchToConnectedRoadAndWaypoints(
        CityProceduralRoad entryRoad,
        int entryExitControlPointIndex,
        CityProceduralRoad continuationRoad,
        int continuationExitControlPointIndex,
        IReadOnlyList<Transform> waypoints,
        float roadTurnSpeed,
        float stationTurnSpeed,
        float stopDeceleration,
        bool shouldLoop,
        bool placeAtStart,
        bool startMoving)
    {
        Stop();
        proceduralRoad = entryRoad;
        proceduralRoadObjectName = entryRoad != null ? entryRoad.name : string.Empty;
        roadComponent = null;
        loop = shouldLoop;
        path.Clear();
        directWaypointStartIndex = int.MaxValue;
        ResetRouteSpeedProfile();

        if (entryRoad == null
            || continuationRoad == null
            || !entryRoad.CopyCenterlineThroughControlPoint(entryExitControlPointIndex, path))
        {
            return false;
        }

        firstTurnStartIndex = Mathf.Max(1, path.Count - 1);

        List<Vector3> continuation = new List<Vector3>();
        if (!continuationRoad.CopyCenterlineThroughControlPoint(
                continuationExitControlPointIndex,
                continuation))
        {
            return false;
        }

        AppendPath(continuation);
        gasStationTurnStartIndex = Mathf.Max(1, path.Count - 1);
        directWaypointStartIndex = path.Count;
        AppendWaypoints(waypoints);
        if (path.Count < 2 || directWaypointStartIndex >= path.Count)
        {
            return false;
        }


        useRouteSpeedProfile = true;
        firstTurnSpeed = Mathf.Max(0.01f, roadTurnSpeed);
        gasStationTurnSpeed = Mathf.Max(0.01f, stationTurnSpeed);
        finalStopDeceleration = Mathf.Max(0.01f, stopDeceleration);
        finalStopTargetIndex = path.Count - 1;

        targetIndex = 1;
        currentSpeed = speed;
        if (placeAtStart)
        {
            transform.position = GetPathPoint(0);
            FaceAlongPath(instant: true);
        }

        if (startMoving)
        {
            Play();
        }

        return true;
    }

    public bool SwitchToDepartureRoute(
        IReadOnlyList<Transform> exitWaypoints,
        CityProceduralRoad dirtRoad,
        CityProceduralRoad mainRoad,
        int mainRoadStartControlPoint,
        float waypointMaxSpeed,
        float dirtMaxSpeed,
        float mainRoadSpeed)
    {
        Stop();
        loop = false;
        path.Clear();
        ResetRouteSpeedProfile();

        float surfaceY = transform.position.y - heightOffset;
        AppendDeparturePoint(new Vector3(transform.position.x, surfaceY, transform.position.z), 0f);

        if (exitWaypoints != null)
        {
            for (int i = 0; i < exitWaypoints.Count; i++)
            {
                Transform waypoint = exitWaypoints[i];
                if (waypoint == null)
                {
                    continue;
                }

                Vector3 point = waypoint.position;
                point.y = surfaceY;
                AppendDeparturePoint(point, waypointMaxSpeed);
            }
        }

        List<Vector3> dirtPoints = new List<Vector3>();
        if (dirtRoad == null || !dirtRoad.CopyCenterlineReversed(dirtPoints))
        {
            return false;
        }

        departureRoadStartIndex = path.Count;
        AppendDepartureSection(dirtPoints, waypointMaxSpeed, dirtMaxSpeed);

        List<Vector3> mainRoadPoints = new List<Vector3>();
        if (mainRoad == null
            || !mainRoad.CopyCenterlineFromControlPoint(mainRoadStartControlPoint, mainRoadPoints))
        {
            return false;
        }

        AppendDepartureSection(mainRoadPoints, dirtMaxSpeed, mainRoadSpeed);
        if (path.Count < 2 || explicitTargetSpeeds.Count != path.Count)
        {
            return false;
        }

        useExplicitSpeedProfile = true;
        useDepartureLaneProfile = true;
        targetIndex = 1;
        currentSpeed = Mathf.Max(0.01f, waypointMaxSpeed);
        Play();
        return true;
    }

    private void AppendDepartureSection(
        IReadOnlyList<Vector3> points,
        float startSpeed,
        float endSpeed)
    {
        for (int i = 0; i < points.Count; i++)
        {
            float progress = (i + 1f) / Mathf.Max(1f, points.Count);
            AppendDeparturePoint(points[i], Mathf.Lerp(startSpeed, endSpeed, progress));
        }
    }

    private void AppendDeparturePoint(Vector3 point, float targetSpeed)
    {
        if (path.Count > 0 && Vector3.SqrMagnitude(path[path.Count - 1] - point) < 0.0001f)
        {
            explicitTargetSpeeds[explicitTargetSpeeds.Count - 1] = Mathf.Max(0f, targetSpeed);
            return;
        }

        path.Add(point);
        explicitTargetSpeeds.Add(Mathf.Max(0f, targetSpeed));
    }

    private void AppendPath(List<Vector3> points)
    {
        for (int i = 0; i < points.Count; i++)
        {
            if (path.Count > 0 && Vector3.SqrMagnitude(path[path.Count - 1] - points[i]) < 0.0001f)
            {
                continue;
            }

            path.Add(points[i]);
        }
    }

    private void AppendWaypoints(IReadOnlyList<Transform> waypoints)
    {
        if (waypoints == null || path.Count == 0)
        {
            return;
        }

        float pathSurfaceY = path[path.Count - 1].y;
        for (int i = 0; i < waypoints.Count; i++)
        {
            Transform waypoint = waypoints[i];
            if (waypoint == null)
            {
                continue;
            }

            Vector3 point = waypoint.position;
            point.y = pathSurfaceY;
            if (Vector3.SqrMagnitude(path[path.Count - 1] - point) >= 0.0001f)
            {
                path.Add(point);
            }
        }
    }

    public void RebuildPath()
    {
        path.Clear();
        directWaypointStartIndex = int.MaxValue;
        ResetRouteSpeedProfile();

        CityProceduralRoad cityRoad = proceduralRoad != null ? proceduralRoad : FindProceduralRoad();
        if (cityRoad != null)
        {
            proceduralRoad = cityRoad;
            cityRoad.CopyCenterline(path);
        }

        if (path.Count > 1)
        {
            targetIndex = 1;
            currentSpeed = speed;
            return;
        }

        Component source = roadComponent != null ? roadComponent : FindRoadComponent();
        if (source == null)
        {
            Debug.LogWarning($"{nameof(CityRoadVehicleMover)} could not find a procedural road or EasyRoads road '{roadObjectName}'.", this);
            return;
        }

        roadComponent = source;

        if (!TryReadSplinePoints(source, path))
        {
            Debug.LogWarning($"{nameof(CityRoadVehicleMover)} found '{source.name}' but could not read splinePoints.", this);
            return;
        }

        if (path.Count > 1)
        {
            targetIndex = 1;
            currentSpeed = speed;
        }
    }

    private CityProceduralRoad FindProceduralRoad()
    {
        GameObject roadObject = GameObject.Find(proceduralRoadObjectName);
        if (roadObject == null)
        {
            return FindAnyObjectByType<CityProceduralRoad>();
        }

        return roadObject.GetComponent<CityProceduralRoad>();
    }

    private Component FindRoadComponent()
    {
        GameObject roadObject = GameObject.Find(roadObjectName);
        if (roadObject == null)
        {
            return null;
        }

        Component[] components = roadObject.GetComponents<Component>();
        foreach (Component component in components)
        {
            if (component == null)
            {
                continue;
            }

            if (HasSplinePointsField(component.GetType()))
            {
                return component;
            }
        }

        return null;
    }

    private bool TryReadSplinePoints(Component source, List<Vector3> results)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        FieldInfo field = GetField(source.GetType(), "splinePoints", flags);
        if (field == null)
        {
            return false;
        }

        object value = field.GetValue(source);
        IEnumerable enumerable = value as IEnumerable;
        if (enumerable == null)
        {
            return false;
        }

        Transform roadTransform = source.transform;
        foreach (object item in enumerable)
        {
            if (!(item is Vector3))
            {
                continue;
            }

            Vector3 point = (Vector3)item;
            results.Add(useRoadTransform && roadTransform != null ? roadTransform.TransformPoint(point) : point);
        }

        return results.Count > 1;
    }

    private static bool HasSplinePointsField(Type type)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        return GetField(type, "splinePoints", flags) != null;
    }

    private static FieldInfo GetField(Type type, string fieldName, BindingFlags flags)
    {
        while (type != null)
        {
            FieldInfo field = type.GetField(fieldName, flags);
            if (field != null)
            {
                return field;
            }

            type = type.BaseType;
        }

        return null;
    }

    private Vector3 WithHeightOffset(Vector3 point)
    {
        point.y += heightOffset;
        return point;
    }

    private Vector3 GetPathPoint(int index)
    {
        Vector3 point = WithHeightOffset(path[index]);
        bool applyLaneOffset = useDepartureLaneProfile
            ? index >= departureRoadStartIndex
            : index < directWaypointStartIndex;
        if (!applyLaneOffset)
        {
            return point;
        }

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

    private float GetTargetSpeed(int index)
    {
        if (useExplicitSpeedProfile && explicitTargetSpeeds.Count == path.Count)
        {
            return explicitTargetSpeeds[index];
        }

        if (!useRouteSpeedProfile)
        {
            return speed * GetTurnSpeedMultiplier(index);
        }

        float targetSpeed = speed;
        if (index >= firstTurnStartIndex)
        {
            targetSpeed = firstTurnSpeed;
        }

        if (index >= gasStationTurnStartIndex)
        {
            targetSpeed = gasStationTurnSpeed;
        }

        if (index >= finalStopTargetIndex)
        {
            float remainingDistance = Vector3.Distance(transform.position, GetPathPoint(finalStopTargetIndex));
            float stoppingSpeed = Mathf.Sqrt(2f * finalStopDeceleration * remainingDistance);
            targetSpeed = Mathf.Min(targetSpeed, stoppingSpeed);
        }

        return targetSpeed;
    }

    private void ResetRouteSpeedProfile()
    {
        useRouteSpeedProfile = false;
        firstTurnStartIndex = int.MaxValue;
        gasStationTurnStartIndex = int.MaxValue;
        finalStopTargetIndex = int.MaxValue;
        firstTurnSpeed = 0f;
        gasStationTurnSpeed = 0f;
        finalStopDeceleration = acceleration;
        explicitTargetSpeeds.Clear();
        useExplicitSpeedProfile = false;
        useDepartureLaneProfile = false;
        departureRoadStartIndex = int.MaxValue;
        HasFinished = false;
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

        if (loop)
        {
            targetIndex = 0;
            return;
        }

        targetIndex = path.Count - 1;
        isMoving = false;
        currentSpeed = 0f;
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

        if (loop)
        {
            for (int i = 0; i < targetIndex; i++)
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
        }

        return GetPathPoint(Mathf.Clamp(targetIndex, 0, path.Count - 1));
    }
}
