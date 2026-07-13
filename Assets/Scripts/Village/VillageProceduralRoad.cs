using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public sealed class VillageProceduralRoad : MonoBehaviour
{
    [Header("Shape")]
    [SerializeField, Min(1f)] private float roadWidth = 12f;
    [SerializeField, Min(0f)] private float cornerRadius = 20f;
    [SerializeField, Min(0.25f)] private float sampleSpacing = 3f;
    [SerializeField, Range(2, 24)] private int cornerSamples = 10;

    [Header("Ground")]
    [SerializeField] private bool followTerrain = true;
    [SerializeField] private float surfaceOffset = 0.05f;

    [Header("Route")]
    [SerializeField] private List<Vector3> routePoints = new List<Vector3>
    {
        new Vector3(-400f, 0f, -300f),
        new Vector3(100f, 0f, -300f),
        new Vector3(100f, 0f, 0f),
        new Vector3(0f, 0f, 0f)
    };

    private readonly List<Vector3> centerline = new List<Vector3>();
    private Mesh generatedMesh;

    private void OnEnable()
    {
        Rebuild();
    }

    private void OnValidate()
    {
        Rebuild();
    }

    public void Rebuild()
    {
        if (routePoints == null || routePoints.Count < 2)
        {
            return;
        }

        BuildCenterline();
        BuildMesh();
    }

    public void CopyCenterline(List<Vector3> results)
    {
        if (results == null)
        {
            return;
        }

        if (centerline.Count < 2)
        {
            Rebuild();
        }

        results.AddRange(centerline);
    }

    private void BuildCenterline()
    {
        centerline.Clear();
        AddCenterPoint(routePoints[0]);

        for (int i = 1; i < routePoints.Count - 1; i++)
        {
            Vector3 previous = routePoints[i - 1];
            Vector3 corner = routePoints[i];
            Vector3 next = routePoints[i + 1];
            Vector3 incoming = Flatten(corner - previous).normalized;
            Vector3 outgoing = Flatten(next - corner).normalized;

            if (incoming.sqrMagnitude < 0.5f || outgoing.sqrMagnitude < 0.5f)
            {
                AppendLine(corner);
                continue;
            }

            float incomingLength = Flatten(corner - previous).magnitude;
            float outgoingLength = Flatten(next - corner).magnitude;
            float trim = Mathf.Min(cornerRadius, incomingLength * 0.45f, outgoingLength * 0.45f);
            Vector3 entry = corner - incoming * trim;
            Vector3 exit = corner + outgoing * trim;

            AppendLine(entry);
            for (int sample = 1; sample <= cornerSamples; sample++)
            {
                float t = sample / (float)cornerSamples;
                float inverse = 1f - t;
                Vector3 point = inverse * inverse * entry
                    + 2f * inverse * t * corner
                    + t * t * exit;
                AddCenterPoint(point);
            }
        }

        AppendLine(routePoints[routePoints.Count - 1]);
    }

    private void AppendLine(Vector3 target)
    {
        Vector3 start = centerline.Count > 0 ? centerline[centerline.Count - 1] : target;
        Vector3 flatStart = new Vector3(start.x, 0f, start.z);
        Vector3 flatTarget = new Vector3(target.x, 0f, target.z);
        float distance = Vector3.Distance(flatStart, flatTarget);
        int steps = Mathf.Max(1, Mathf.CeilToInt(distance / Mathf.Max(0.25f, sampleSpacing)));

        for (int step = 1; step <= steps; step++)
        {
            AddCenterPoint(Vector3.Lerp(start, target, step / (float)steps));
        }
    }

    private void AddCenterPoint(Vector3 point)
    {
        point.y = GetSurfaceHeight(point);
        centerline.Add(point);
    }

    private void BuildMesh()
    {
        if (centerline.Count < 2)
        {
            return;
        }

        if (generatedMesh == null)
        {
            generatedMesh = new Mesh { name = "Village Procedural Road Mesh" };
        }
        else
        {
            generatedMesh.Clear();
        }

        Vector3[] vertices = new Vector3[centerline.Count * 2];
        Vector2[] uvs = new Vector2[vertices.Length];
        Color32[] colors = new Color32[vertices.Length];
        int[] triangles = new int[(centerline.Count - 1) * 6];
        float travelledDistance = 0f;

        for (int i = 0; i < centerline.Count; i++)
        {
            if (i > 0)
            {
                travelledDistance += Vector3.Distance(centerline[i - 1], centerline[i]);
            }

            Vector3 direction = GetDirection(i);
            Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;
            Vector3 leftPoint = centerline[i] - right * (roadWidth * 0.5f);
            Vector3 rightPoint = centerline[i] + right * (roadWidth * 0.5f);
            leftPoint.y = GetSurfaceHeight(leftPoint);
            rightPoint.y = GetSurfaceHeight(rightPoint);

            vertices[i * 2] = transform.InverseTransformPoint(leftPoint);
            vertices[i * 2 + 1] = transform.InverseTransformPoint(rightPoint);
            uvs[i * 2] = new Vector2(0f, travelledDistance / Mathf.Max(1f, roadWidth));
            uvs[i * 2 + 1] = new Vector2(1f, travelledDistance / Mathf.Max(1f, roadWidth));
            colors[i * 2] = Color.white;
            colors[i * 2 + 1] = Color.white;
        }

        int triangle = 0;
        for (int i = 0; i < centerline.Count - 1; i++)
        {
            int vertex = i * 2;
            triangles[triangle++] = vertex;
            triangles[triangle++] = vertex + 2;
            triangles[triangle++] = vertex + 1;
            triangles[triangle++] = vertex + 1;
            triangles[triangle++] = vertex + 2;
            triangles[triangle++] = vertex + 3;
        }

        generatedMesh.vertices = vertices;
        generatedMesh.uv = uvs;
        generatedMesh.colors32 = colors;
        generatedMesh.triangles = triangles;
        generatedMesh.RecalculateNormals();
        generatedMesh.RecalculateTangents();
        generatedMesh.RecalculateBounds();

        MeshFilter meshFilter = GetComponent<MeshFilter>();
        MeshCollider meshCollider = GetComponent<MeshCollider>();
        meshFilter.sharedMesh = generatedMesh;
        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = generatedMesh;
    }

    private float GetSurfaceHeight(Vector3 worldPoint)
    {
        if (!followTerrain)
        {
            return worldPoint.y + surfaceOffset;
        }

        Terrain[] terrains = Terrain.activeTerrains;
        for (int i = 0; i < terrains.Length; i++)
        {
            Terrain terrain = terrains[i];
            Vector3 origin = terrain.transform.position;
            Vector3 size = terrain.terrainData.size;
            if (worldPoint.x < origin.x || worldPoint.x > origin.x + size.x
                || worldPoint.z < origin.z || worldPoint.z > origin.z + size.z)
            {
                continue;
            }

            return terrain.SampleHeight(worldPoint) + origin.y + surfaceOffset;
        }

        return worldPoint.y + surfaceOffset;
    }

    private Vector3 GetDirection(int index)
    {
        Vector3 direction = index <= 0
            ? centerline[1] - centerline[0]
            : index >= centerline.Count - 1
                ? centerline[index] - centerline[index - 1]
                : centerline[index + 1] - centerline[index - 1];
        return Flatten(direction).normalized;
    }

    private static Vector3 Flatten(Vector3 value)
    {
        value.y = 0f;
        return value;
    }

    private void OnDrawGizmosSelected()
    {
        if (routePoints == null)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        for (int i = 0; i < routePoints.Count; i++)
        {
            Vector3 point = routePoints[i];
            point.y = GetSurfaceHeight(point) + 0.5f;
            Gizmos.DrawSphere(point, 2f);
        }
    }
}
