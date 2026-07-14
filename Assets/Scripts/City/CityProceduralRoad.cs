using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public sealed class CityProceduralRoad : MonoBehaviour
{
    [SerializeField, Min(1f)] private float roadWidth = 12f;
    [SerializeField] private float surfaceY = 0.52f;
    [SerializeField, Range(4, 32)] private int samplesPerSegment = 18;
    [SerializeField] private bool closedLoop = true;
    [SerializeField] private List<Vector3> controlPoints = new List<Vector3>();

    private readonly List<Vector3> centerline = new List<Vector3>();
    private Mesh generatedMesh;

    private static readonly Vector3[] DefaultLayout =
    {
        // Mandalika-inspired loop adapted from the referenced SVG track outline.
        new Vector3(-247f, 0f, 266f),
        new Vector3(-90f, 0f, 306f),
        new Vector3(75f, 0f, 345f),
        new Vector3(229f, 0f, 384f),
        new Vector3(277f, 0f, 358f),
        new Vector3(282f, 0f, 216f),
        new Vector3(257f, 0f, 198f),
        new Vector3(210f, 0f, 189f),
        new Vector3(221f, 0f, 136f),
        new Vector3(304f, 0f, 70f),
        new Vector3(320f, 0f, 14f),
        new Vector3(320f, 0f, -66f),
        new Vector3(292f, 0f, -110f),
        new Vector3(225f, 0f, -178f),
        new Vector3(173f, 0f, -194f),
        new Vector3(63f, 0f, -246f),
        new Vector3(-29f, 0f, -312f),
        new Vector3(-176f, 0f, -384f),
        new Vector3(-232f, 0f, -345f),
        new Vector3(-227f, 0f, -238f),
        new Vector3(-169f, 0f, -214f),
        new Vector3(-77f, 0f, -120f),
        new Vector3(-92f, 0f, 3f),
        new Vector3(-160f, 0f, 51f),
        new Vector3(-259f, 0f, 104f),
        new Vector3(-311f, 0f, 221f),
        new Vector3(-320f, 0f, 309f),
        new Vector3(-272f, 0f, 358f),
        new Vector3(-257f, 0f, 291f),
        new Vector3(-266f, 0f, 226f),
    };

    private void OnEnable()
    {
        EnsureDefaultLayout();
        Rebuild();
    }

    private void OnValidate()
    {
        EnsureDefaultLayout();
        Rebuild();
    }

    public void CopyCenterline(List<Vector3> results)
    {
        if (centerline.Count < 2)
        {
            Rebuild();
        }

        results.AddRange(centerline);
    }

    public bool CopyCenterlineThroughControlPoint(int controlPointIndex, List<Vector3> results)
    {
        if (results == null || controlPoints.Count < 2)
        {
            return false;
        }

        if (centerline.Count < 2)
        {
            Rebuild();
        }

        int clampedControlPoint = Mathf.Clamp(controlPointIndex, 0, controlPoints.Count - 1);
        int lastCenterlineIndex = clampedControlPoint >= controlPoints.Count - 1
            ? centerline.Count - 1
            : Mathf.Min(clampedControlPoint * samplesPerSegment, centerline.Count - 1);

        for (int i = 0; i <= lastCenterlineIndex; i++)
        {
            results.Add(centerline[i]);
        }

        return results.Count >= 2;
    }

    public bool CopyCenterlineFromControlPoint(int controlPointIndex, List<Vector3> results)
    {
        if (results == null || controlPoints.Count < 2)
        {
            return false;
        }

        if (centerline.Count < 2)
        {
            Rebuild();
        }

        int clampedControlPoint = Mathf.Clamp(controlPointIndex, 0, controlPoints.Count - 1);
        int firstCenterlineIndex = clampedControlPoint >= controlPoints.Count - 1
            ? centerline.Count - 1
            : Mathf.Min(clampedControlPoint * samplesPerSegment, centerline.Count - 1);

        for (int i = firstCenterlineIndex; i < centerline.Count; i++)
        {
            results.Add(centerline[i]);
        }

        return results.Count >= 2;
    }

    public bool CopyCenterlineReversed(List<Vector3> results)
    {
        if (results == null)
        {
            return false;
        }

        if (centerline.Count < 2)
        {
            Rebuild();
        }

        for (int i = centerline.Count - 1; i >= 0; i--)
        {
            results.Add(centerline[i]);
        }

        return results.Count >= 2;
    }

    public bool IsPointInsideRoad(Vector3 worldPoint, float extraClearance)
    {
        if (centerline.Count < 2)
        {
            Rebuild();
        }

        float maxDistance = roadWidth * 0.5f + Mathf.Max(0f, extraClearance);
        return GetDistanceToCenterline(worldPoint) <= maxDistance;
    }

    public float GetDistanceToCenterline(Vector3 worldPoint)
    {
        if (centerline.Count < 2)
        {
            Rebuild();
        }

        float closestSqrDistance = float.PositiveInfinity;
        Vector2 point = new Vector2(worldPoint.x, worldPoint.z);

        for (int i = 0; i < centerline.Count - 1; i++)
        {
            Vector2 start = new Vector2(centerline[i].x, centerline[i].z);
            Vector2 end = new Vector2(centerline[i + 1].x, centerline[i + 1].z);
            Vector2 projected = ProjectPointToSegment(point, start, end);
            float sqrDistance = (point - projected).sqrMagnitude;
            if (sqrDistance < closestSqrDistance)
            {
                closestSqrDistance = sqrDistance;
            }
        }

        return Mathf.Sqrt(closestSqrDistance);
    }

    private void EnsureDefaultLayout()
    {
        if (controlPoints.Count > 1)
        {
            return;
        }

        controlPoints.Clear();
        controlPoints.AddRange(DefaultLayout);
    }

    private void Rebuild()
    {
        if (controlPoints.Count < 2)
        {
            return;
        }

        BuildCenterline();
        BuildMesh();
    }

    private void BuildCenterline()
    {
        centerline.Clear();

        int segmentCount = closedLoop ? controlPoints.Count : controlPoints.Count - 1;
        for (int segment = 0; segment < segmentCount; segment++)
        {
            Vector3 p0 = GetControlPoint(segment - 1);
            Vector3 p1 = GetControlPoint(segment);
            Vector3 p2 = GetControlPoint(segment + 1);
            Vector3 p3 = GetControlPoint(segment + 2);

            for (int sample = 0; sample < samplesPerSegment; sample++)
            {
                float t = sample / (float)samplesPerSegment;
                Vector3 point = CatmullRom(p0, p1, p2, p3, t);
                point.y = surfaceY;
                centerline.Add(point);
            }
        }

        Vector3 endPoint = closedLoop ? centerline[0] : controlPoints[controlPoints.Count - 1];
        endPoint.y = surfaceY;
        centerline.Add(endPoint);
    }

    private Vector3 GetControlPoint(int index)
    {
        if (closedLoop)
        {
            int wrapped = (index % controlPoints.Count + controlPoints.Count) % controlPoints.Count;
            return controlPoints[wrapped];
        }

        return controlPoints[Mathf.Clamp(index, 0, controlPoints.Count - 1)];
    }

    private void BuildMesh()
    {
        if (centerline.Count < 2)
        {
            return;
        }

        if (generatedMesh == null)
        {
            generatedMesh = new Mesh { name = "City Procedural Road Mesh" };
        }
        else
        {
            generatedMesh.Clear();
        }

        int vertexCount = centerline.Count * 2;
        Vector3[] vertices = new Vector3[vertexCount];
        Vector2[] uvs = new Vector2[vertexCount];
        int[] triangles = new int[(centerline.Count - 1) * 6];

        float distance = 0f;
        for (int i = 0; i < centerline.Count; i++)
        {
            if (i > 0)
            {
                distance += Vector3.Distance(centerline[i - 1], centerline[i]);
            }

            Vector3 direction = GetDirection(i);
            Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;
            Vector3 leftPoint = centerline[i] - right * (roadWidth * 0.5f);
            Vector3 rightPoint = centerline[i] + right * (roadWidth * 0.5f);

            vertices[i * 2] = transform.InverseTransformPoint(leftPoint);
            vertices[i * 2 + 1] = transform.InverseTransformPoint(rightPoint);
            uvs[i * 2] = new Vector2(0f, distance / roadWidth);
            uvs[i * 2 + 1] = new Vector2(1f, distance / roadWidth);
        }

        int triangleIndex = 0;
        for (int i = 0; i < centerline.Count - 1; i++)
        {
            int start = i * 2;
            triangles[triangleIndex++] = start;
            triangles[triangleIndex++] = start + 2;
            triangles[triangleIndex++] = start + 1;
            triangles[triangleIndex++] = start + 1;
            triangles[triangleIndex++] = start + 2;
            triangles[triangleIndex++] = start + 3;
        }

        generatedMesh.vertices = vertices;
        generatedMesh.uv = uvs;
        generatedMesh.triangles = triangles;
        generatedMesh.RecalculateNormals();
        generatedMesh.RecalculateBounds();

        MeshFilter meshFilter = GetComponent<MeshFilter>();
        MeshCollider meshCollider = GetComponent<MeshCollider>();
        meshFilter.sharedMesh = generatedMesh;
        meshCollider.sharedMesh = generatedMesh;
    }

    private void OnDrawGizmosSelected()
    {
        if (controlPoints.Count < 2)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        for (int i = 0; i < controlPoints.Count; i++)
        {
            Vector3 current = controlPoints[i];
            current.y = surfaceY + 1f;
            Gizmos.DrawSphere(current, 4f);

            int nextIndex = i + 1;
            if (nextIndex >= controlPoints.Count)
            {
                if (!closedLoop)
                {
                    continue;
                }

                nextIndex = 0;
            }

            Vector3 next = controlPoints[nextIndex];
            next.y = surfaceY + 1f;
            Gizmos.DrawLine(current, next);
        }
    }

    private Vector3 GetDirection(int index)
    {
        Vector3 direction;
        if (index <= 0)
        {
            direction = centerline[1] - centerline[0];
        }
        else if (index >= centerline.Count - 1)
        {
            direction = centerline[index] - centerline[index - 1];
        }
        else
        {
            direction = centerline[index + 1] - centerline[index - 1];
        }

        direction.y = 0f;
        return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
    }

    private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        return 0.5f * (
            (2f * p1) +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
    }

    private static Vector2 ProjectPointToSegment(Vector2 point, Vector2 start, Vector2 end)
    {
        Vector2 segment = end - start;
        float lengthSqr = segment.sqrMagnitude;
        if (lengthSqr < 0.0001f)
        {
            return start;
        }

        float t = Vector2.Dot(point - start, segment) / lengthSqr;
        return start + segment * Mathf.Clamp01(t);
    }
}
