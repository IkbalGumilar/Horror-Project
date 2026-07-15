using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public sealed class ProceduralGasStationMesh : MonoBehaviour
{
    private const int LaneCount = 3;

    [Header("Forecourt")]
    [SerializeField, Min(10f)] private float forecourtWidth = 26f;
    [SerializeField, Min(10f)] private float forecourtLength = 24f;
    [SerializeField, Min(0.05f)] private float forecourtThickness = 0.15f;

    [Header("Canopy")]
    [SerializeField, Min(8f)] private float canopyWidth = 22f;
    [SerializeField, Min(8f)] private float canopyLength = 18f;
    [SerializeField, Min(2.5f)] private float canopyHeight = 5.2f;
    [SerializeField, Min(0.1f)] private float canopyThickness = 0.55f;
    [SerializeField, Min(0.15f)] private float columnThickness = 0.45f;
    [SerializeField, Min(0.5f)] private float columnInset = 3.5f;

    [Header("Three Fuel Lanes")]
    [SerializeField, Min(2f)] private float laneSpacing = 6f;
    [SerializeField, Min(0.5f)] private float islandWidth = 1.2f;
    [SerializeField, Min(2f)] private float islandLength = 6f;
    [SerializeField, Min(0.05f)] private float islandHeight = 0.25f;
    [SerializeField, Min(0.5f)] private float pumpWidth = 0.9f;
    [SerializeField, Min(1f)] private float pumpHeight = 2.3f;
    [SerializeField, Min(0.3f)] private float pumpDepth = 1.05f;

    [Header("Price Sign")]
    [SerializeField, Min(1f)] private float signHeight = 5.5f;
    [SerializeField, Min(0.5f)] private float signWidth = 2.8f;
    [SerializeField, Min(0.5f)] private float signPanelHeight = 2.2f;

    [Header("Colliders")]
    [SerializeField] private BoxCollider[] islandColliders = new BoxCollider[LaneCount];
    [SerializeField] private BoxCollider[] columnColliders = new BoxCollider[4];

    private Mesh generatedMesh;

    private void OnEnable()
    {
        Rebuild();
    }

    private void OnValidate()
    {
        Rebuild();
    }

    private void OnDestroy()
    {
        if (generatedMesh == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(generatedMesh);
        }
        else
        {
            DestroyImmediate(generatedMesh);
        }
    }

    public void Rebuild()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            return;
        }

        List<Vector3> vertices = new();
        List<Vector3> normals = new();
        List<Vector2> uvs = new();
        List<int> triangles = new();

        float slabY = forecourtThickness * 0.5f;
        AddBox(vertices, normals, uvs, triangles,
            new Vector3(0f, slabY, 0f),
            new Vector3(forecourtWidth, forecourtThickness, forecourtLength));

        float canopyY = canopyHeight + canopyThickness * 0.5f;
        AddBox(vertices, normals, uvs, triangles,
            new Vector3(0f, canopyY, 0f),
            new Vector3(canopyWidth, canopyThickness, canopyLength));

        float fasciaHeight = canopyThickness * 1.35f;
        AddBox(vertices, normals, uvs, triangles,
            new Vector3(0f, canopyHeight + fasciaHeight * 0.5f, -canopyLength * 0.5f),
            new Vector3(canopyWidth, fasciaHeight, 0.18f));
        AddBox(vertices, normals, uvs, triangles,
            new Vector3(0f, canopyHeight + fasciaHeight * 0.5f, canopyLength * 0.5f),
            new Vector3(canopyWidth, fasciaHeight, 0.18f));

        Vector3[] columnPositions = GetColumnPositions();
        for (int i = 0; i < columnPositions.Length; i++)
        {
            Vector3 position = columnPositions[i];
            position.y = canopyHeight * 0.5f;
            AddBox(vertices, normals, uvs, triangles,
                position,
                new Vector3(columnThickness, canopyHeight, columnThickness));
            UpdateCollider(
                columnColliders,
                i,
                position,
                new Vector3(columnThickness, canopyHeight, columnThickness));
        }

        for (int lane = 0; lane < LaneCount; lane++)
        {
            float x = (lane - 1) * laneSpacing;
            Vector3 islandPosition = new(x, forecourtThickness + islandHeight * 0.5f, 0f);
            Vector3 islandSize = new(islandWidth, islandHeight, islandLength);
            AddBox(vertices, normals, uvs, triangles, islandPosition, islandSize);
            UpdateCollider(islandColliders, lane, islandPosition, islandSize);

            AddFuelPump(vertices, normals, uvs, triangles, x);
            AddBollardPair(vertices, normals, uvs, triangles, x);
        }

        AddLaneMarkings(vertices, normals, uvs, triangles);
        AddPriceSign(vertices, normals, uvs, triangles);
        AddCanopyFixtures(vertices, normals, uvs, triangles);

        if (generatedMesh == null)
        {
            generatedMesh = new Mesh
            {
                hideFlags = HideFlags.DontSave
            };
        }
        else
        {
            generatedMesh.Clear();
        }

        generatedMesh.name = $"{name} Mesh";
        generatedMesh.SetVertices(vertices);
        generatedMesh.SetNormals(normals);
        generatedMesh.SetUVs(0, uvs);
        generatedMesh.SetTriangles(triangles, 0);
        generatedMesh.RecalculateBounds();
        meshFilter.sharedMesh = generatedMesh;
    }

    private void AddFuelPump(
        List<Vector3> vertices,
        List<Vector3> normals,
        List<Vector2> uvs,
        List<int> triangles,
        float x)
    {
        float baseY = forecourtThickness + islandHeight;
        AddBox(vertices, normals, uvs, triangles,
            new Vector3(x, baseY + 0.12f, 0f),
            new Vector3(pumpWidth + 0.2f, 0.24f, pumpDepth + 0.2f));
        AddBox(vertices, normals, uvs, triangles,
            new Vector3(x, baseY + pumpHeight * 0.5f, 0f),
            new Vector3(pumpWidth, pumpHeight, pumpDepth));
        AddBox(vertices, normals, uvs, triangles,
            new Vector3(x, baseY + pumpHeight * 0.7f, -pumpDepth * 0.52f),
            new Vector3(pumpWidth * 0.7f, pumpHeight * 0.28f, 0.08f));
        AddBox(vertices, normals, uvs, triangles,
            new Vector3(x, baseY + pumpHeight + 0.12f, 0f),
            new Vector3(pumpWidth + 0.18f, 0.24f, pumpDepth + 0.14f));
    }

    private void AddBollardPair(
        List<Vector3> vertices,
        List<Vector3> normals,
        List<Vector2> uvs,
        List<int> triangles,
        float x)
    {
        float bollardY = forecourtThickness + 0.45f;
        float offsetZ = islandLength * 0.5f - 0.45f;
        Vector3 size = new(0.18f, 0.9f, 0.18f);
        AddBox(vertices, normals, uvs, triangles, new Vector3(x, bollardY, -offsetZ), size);
        AddBox(vertices, normals, uvs, triangles, new Vector3(x, bollardY, offsetZ), size);
    }

    private void AddLaneMarkings(
        List<Vector3> vertices,
        List<Vector3> normals,
        List<Vector2> uvs,
        List<int> triangles)
    {
        float markingY = forecourtThickness + 0.012f;
        float outsideOffset = laneSpacing * 1.5f;
        for (int i = 0; i < 4; i++)
        {
            float x = -outsideOffset + i * laneSpacing;
            AddBox(vertices, normals, uvs, triangles,
                new Vector3(x, markingY, 0f),
                new Vector3(0.08f, 0.024f, islandLength + 5f));
        }
    }

    private void AddPriceSign(
        List<Vector3> vertices,
        List<Vector3> normals,
        List<Vector2> uvs,
        List<int> triangles)
    {
        float x = canopyWidth * 0.5f + 1.2f;
        float z = -canopyLength * 0.5f + 1.2f;
        AddBox(vertices, normals, uvs, triangles,
            new Vector3(x, signHeight * 0.5f, z),
            new Vector3(0.18f, signHeight, 0.18f));
        AddBox(vertices, normals, uvs, triangles,
            new Vector3(x, signHeight - signPanelHeight * 0.5f, z),
            new Vector3(signWidth, signPanelHeight, 0.22f));
    }

    private void AddCanopyFixtures(
        List<Vector3> vertices,
        List<Vector3> normals,
        List<Vector2> uvs,
        List<int> triangles)
    {
        float fixtureY = canopyHeight - 0.05f;
        for (int lane = 0; lane < LaneCount; lane++)
        {
            float x = (lane - 1) * laneSpacing;
            for (int row = -1; row <= 1; row += 2)
            {
                AddBox(vertices, normals, uvs, triangles,
                    new Vector3(x, fixtureY, row * canopyLength * 0.28f),
                    new Vector3(1.2f, 0.08f, 0.55f));
            }
        }
    }

    private Vector3[] GetColumnPositions()
    {
        float x = canopyWidth * 0.5f - columnInset;
        float z = canopyLength * 0.5f - columnInset;
        return new[]
        {
            new Vector3(-x, 0f, -z),
            new Vector3(x, 0f, -z),
            new Vector3(-x, 0f, z),
            new Vector3(x, 0f, z)
        };
    }

    private static void UpdateCollider(
        BoxCollider[] colliders,
        int index,
        Vector3 center,
        Vector3 size)
    {
        if (colliders == null || index < 0 || index >= colliders.Length || colliders[index] == null)
        {
            return;
        }

        colliders[index].center = center;
        colliders[index].size = size;
    }

    private static void AddBox(
        List<Vector3> vertices,
        List<Vector3> normals,
        List<Vector2> uvs,
        List<int> triangles,
        Vector3 center,
        Vector3 size)
    {
        Vector3 half = size * 0.5f;
        AddFace(vertices, normals, uvs, triangles, center,
            new Vector3(-half.x, -half.y, half.z), new Vector3(half.x, -half.y, half.z),
            new Vector3(half.x, half.y, half.z), new Vector3(-half.x, half.y, half.z), Vector3.forward);
        AddFace(vertices, normals, uvs, triangles, center,
            new Vector3(half.x, -half.y, -half.z), new Vector3(-half.x, -half.y, -half.z),
            new Vector3(-half.x, half.y, -half.z), new Vector3(half.x, half.y, -half.z), Vector3.back);
        AddFace(vertices, normals, uvs, triangles, center,
            new Vector3(-half.x, -half.y, -half.z), new Vector3(-half.x, -half.y, half.z),
            new Vector3(-half.x, half.y, half.z), new Vector3(-half.x, half.y, -half.z), Vector3.left);
        AddFace(vertices, normals, uvs, triangles, center,
            new Vector3(half.x, -half.y, half.z), new Vector3(half.x, -half.y, -half.z),
            new Vector3(half.x, half.y, -half.z), new Vector3(half.x, half.y, half.z), Vector3.right);
        AddFace(vertices, normals, uvs, triangles, center,
            new Vector3(-half.x, half.y, half.z), new Vector3(half.x, half.y, half.z),
            new Vector3(half.x, half.y, -half.z), new Vector3(-half.x, half.y, -half.z), Vector3.up);
        AddFace(vertices, normals, uvs, triangles, center,
            new Vector3(-half.x, -half.y, -half.z), new Vector3(half.x, -half.y, -half.z),
            new Vector3(half.x, -half.y, half.z), new Vector3(-half.x, -half.y, half.z), Vector3.down);
    }

    private static void AddFace(
        List<Vector3> vertices,
        List<Vector3> normals,
        List<Vector2> uvs,
        List<int> triangles,
        Vector3 center,
        Vector3 a,
        Vector3 b,
        Vector3 c,
        Vector3 d,
        Vector3 normal)
    {
        int start = vertices.Count;
        vertices.Add(center + a);
        vertices.Add(center + b);
        vertices.Add(center + c);
        vertices.Add(center + d);
        normals.Add(normal);
        normals.Add(normal);
        normals.Add(normal);
        normals.Add(normal);
        uvs.Add(new Vector2(0f, 0f));
        uvs.Add(new Vector2(1f, 0f));
        uvs.Add(new Vector2(1f, 1f));
        uvs.Add(new Vector2(0f, 1f));
        triangles.Add(start);
        triangles.Add(start + 1);
        triangles.Add(start + 2);
        triangles.Add(start);
        triangles.Add(start + 2);
        triangles.Add(start + 3);
    }
}
