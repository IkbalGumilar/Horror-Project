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
    private int targetIndex = 1;
    private bool isMoving;
    private float currentSpeed;

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

        isMoving = path.Count >= 2;
    }

    public void Stop()
    {
        isMoving = false;
    }

    public void RebuildPath()
    {
        path.Clear();

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
            return FindFirstObjectByType<CityProceduralRoad>();
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

        if (loop)
        {
            targetIndex = 0;
            return;
        }

        targetIndex = path.Count - 1;
        isMoving = false;
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
