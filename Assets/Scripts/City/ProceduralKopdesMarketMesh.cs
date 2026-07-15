using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public sealed class ProceduralKopdesMarketMesh : MonoBehaviour
{
    private const int FrontLeftWall = 0;
    private const int FrontRightWall = 1;
    private const int LeftWall = 2;
    private const int RightWall = 3;
    private const int BackWall = 4;

    [Header("Building")]
    [SerializeField, Min(12f)] private float buildingWidth = 38f;
    [SerializeField, Min(16f)] private float buildingLength = 44f;
    [SerializeField, Min(3f)] private float wallHeight = 5f;
    [SerializeField, Min(0.1f)] private float wallThickness = 0.35f;
    [SerializeField, Min(0.05f)] private float floorThickness = 0.2f;
    [SerializeField, Min(0.1f)] private float roofThickness = 0.35f;

    [Header("Entrance")]
    [SerializeField, Min(2f)] private float entranceWidth = 5f;
    [SerializeField, Min(2f)] private float entranceHeight = 3.2f;
    [SerializeField, Min(0.05f)] private float doorFrameThickness = 0.12f;
    [SerializeField, Min(0.5f)] private float awningDepth = 2.4f;
    [SerializeField, Min(0.1f)] private float awningThickness = 0.2f;

    [Header("Checkout")]
    [SerializeField] private Vector2 firstCheckoutPosition = new(9.5f, 0.5f);
    [SerializeField] private Vector2 secondCheckoutPosition = new(14.5f, 0.5f);
    [SerializeField, Min(0.5f)] private float checkoutWidth = 1.2f;
    [SerializeField, Min(1f)] private float checkoutLength = 5f;
    [SerializeField, Min(0.5f)] private float checkoutHeight = 1f;

    [Header("Colliders")]
    [SerializeField] private BoxCollider floorCollider;
    [SerializeField] private BoxCollider[] wallColliders = new BoxCollider[5];
    [SerializeField] private BoxCollider[] checkoutColliders = new BoxCollider[2];

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

        float frontZ = -6f;
        float backZ = frontZ + buildingLength;
        float centerZ = (frontZ + backZ) * 0.5f;
        float floorTop = floorThickness;
        float wallY = floorTop + wallHeight * 0.5f;
        float halfWidth = buildingWidth * 0.5f;

        Vector3 floorCenter = new(0f, floorThickness * 0.5f, centerZ);
        Vector3 floorSize = new(buildingWidth, floorThickness, buildingLength);
        AddBox(vertices, normals, uvs, triangles, floorCenter, floorSize);
        UpdateCollider(floorCollider, floorCenter, floorSize);

        AddWall(
            vertices, normals, uvs, triangles,
            new Vector3(-halfWidth, wallY, centerZ),
            new Vector3(wallThickness, wallHeight, buildingLength),
            LeftWall);
        AddWall(
            vertices, normals, uvs, triangles,
            new Vector3(halfWidth, wallY, centerZ),
            new Vector3(wallThickness, wallHeight, buildingLength),
            RightWall);
        AddWall(
            vertices, normals, uvs, triangles,
            new Vector3(0f, wallY, backZ),
            new Vector3(buildingWidth, wallHeight, wallThickness),
            BackWall);

        float frontSegmentWidth = (buildingWidth - entranceWidth) * 0.5f;
        float frontSegmentOffset = entranceWidth * 0.5f + frontSegmentWidth * 0.5f;
        AddWall(
            vertices, normals, uvs, triangles,
            new Vector3(-frontSegmentOffset, wallY, frontZ),
            new Vector3(frontSegmentWidth, wallHeight, wallThickness),
            FrontLeftWall);
        AddWall(
            vertices, normals, uvs, triangles,
            new Vector3(frontSegmentOffset, wallY, frontZ),
            new Vector3(frontSegmentWidth, wallHeight, wallThickness),
            FrontRightWall);

        float headerHeight = Mathf.Max(0.2f, wallHeight - entranceHeight);
        AddBox(vertices, normals, uvs, triangles,
            new Vector3(0f, floorTop + entranceHeight + headerHeight * 0.5f, frontZ),
            new Vector3(entranceWidth, headerHeight, wallThickness));

        AddEntranceDetails(vertices, normals, uvs, triangles, frontZ, floorTop);
        AddFacadeDetails(vertices, normals, uvs, triangles, frontZ, floorTop);

        AddBox(vertices, normals, uvs, triangles,
            new Vector3(0f, floorTop + wallHeight + roofThickness * 0.5f, centerZ),
            new Vector3(buildingWidth + 0.5f, roofThickness, buildingLength + 0.5f));

        AddCheckout(vertices, normals, uvs, triangles, firstCheckoutPosition, 0, floorTop);
        AddCheckout(vertices, normals, uvs, triangles, secondCheckoutPosition, 1, floorTop);
        AddCeilingFixtures(vertices, normals, uvs, triangles, floorTop + wallHeight - 0.08f);
        AddWallSkirting(vertices, normals, uvs, triangles, centerZ, backZ, floorTop);

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

        generatedMesh.name = $"{name} Supermarket Mesh";
        generatedMesh.SetVertices(vertices);
        generatedMesh.SetNormals(normals);
        generatedMesh.SetUVs(0, uvs);
        generatedMesh.SetTriangles(triangles, 0);
        generatedMesh.RecalculateBounds();
        meshFilter.sharedMesh = generatedMesh;
    }

    private void AddWall(
        List<Vector3> vertices,
        List<Vector3> normals,
        List<Vector2> uvs,
        List<int> triangles,
        Vector3 center,
        Vector3 size,
        int colliderIndex)
    {
        AddBox(vertices, normals, uvs, triangles, center, size);
        if (wallColliders != null && colliderIndex >= 0 && colliderIndex < wallColliders.Length)
        {
            UpdateCollider(wallColliders[colliderIndex], center, size);
        }
    }

    private void AddEntranceDetails(
        List<Vector3> vertices,
        List<Vector3> normals,
        List<Vector2> uvs,
        List<int> triangles,
        float frontZ,
        float floorTop)
    {
        float frameY = floorTop + entranceHeight * 0.5f;
        float frameZ = frontZ - wallThickness * 0.6f;
        float sideX = entranceWidth * 0.5f - doorFrameThickness * 0.5f;
        AddBox(vertices, normals, uvs, triangles,
            new Vector3(-sideX, frameY, frameZ),
            new Vector3(doorFrameThickness, entranceHeight, wallThickness * 0.65f));
        AddBox(vertices, normals, uvs, triangles,
            new Vector3(sideX, frameY, frameZ),
            new Vector3(doorFrameThickness, entranceHeight, wallThickness * 0.65f));
        AddBox(vertices, normals, uvs, triangles,
            new Vector3(0f, floorTop + entranceHeight - doorFrameThickness * 0.5f, frameZ),
            new Vector3(entranceWidth, doorFrameThickness, wallThickness * 0.65f));
        AddBox(vertices, normals, uvs, triangles,
            new Vector3(0f, frameY, frameZ),
            new Vector3(doorFrameThickness, entranceHeight, wallThickness * 0.5f));

        float awningY = floorTop + entranceHeight + 0.35f;
        float awningZ = frontZ - awningDepth * 0.5f;
        AddBox(vertices, normals, uvs, triangles,
            new Vector3(0f, awningY, awningZ),
            new Vector3(entranceWidth + 3f, awningThickness, awningDepth));
        AddBox(vertices, normals, uvs, triangles,
            new Vector3(-(entranceWidth * 0.5f + 1f), awningY * 0.5f, frontZ - awningDepth + 0.2f),
            new Vector3(0.16f, awningY, 0.16f));
        AddBox(vertices, normals, uvs, triangles,
            new Vector3(entranceWidth * 0.5f + 1f, awningY * 0.5f, frontZ - awningDepth + 0.2f),
            new Vector3(0.16f, awningY, 0.16f));
    }

    private void AddFacadeDetails(
        List<Vector3> vertices,
        List<Vector3> normals,
        List<Vector2> uvs,
        List<int> triangles,
        float frontZ,
        float floorTop)
    {
        float signY = floorTop + wallHeight + 0.28f;
        AddBox(vertices, normals, uvs, triangles,
            new Vector3(0f, signY, frontZ - 0.35f),
            new Vector3(13f, 1.25f, 0.22f));

        float windowY = floorTop + 1.65f;
        float windowZ = frontZ - wallThickness * 0.62f;
        AddWindowFrame(vertices, normals, uvs, triangles, -10.5f, windowY, windowZ);
        AddWindowFrame(vertices, normals, uvs, triangles, 10.5f, windowY, windowZ);
    }

    private static void AddWindowFrame(
        List<Vector3> vertices,
        List<Vector3> normals,
        List<Vector2> uvs,
        List<int> triangles,
        float x,
        float y,
        float z)
    {
        const float width = 5.5f;
        const float height = 2.4f;
        const float frame = 0.1f;
        const float depth = 0.08f;
        AddBox(vertices, normals, uvs, triangles, new Vector3(x, y - height * 0.5f, z), new Vector3(width, frame, depth));
        AddBox(vertices, normals, uvs, triangles, new Vector3(x, y + height * 0.5f, z), new Vector3(width, frame, depth));
        AddBox(vertices, normals, uvs, triangles, new Vector3(x - width * 0.5f, y, z), new Vector3(frame, height, depth));
        AddBox(vertices, normals, uvs, triangles, new Vector3(x + width * 0.5f, y, z), new Vector3(frame, height, depth));
        AddBox(vertices, normals, uvs, triangles, new Vector3(x, y, z), new Vector3(frame, height, depth));
    }

    private void AddCheckout(
        List<Vector3> vertices,
        List<Vector3> normals,
        List<Vector2> uvs,
        List<int> triangles,
        Vector2 position,
        int colliderIndex,
        float floorTop)
    {
        Vector3 center = new(position.x, floorTop + checkoutHeight * 0.5f, position.y);
        Vector3 size = new(checkoutWidth, checkoutHeight, checkoutLength);
        AddBox(vertices, normals, uvs, triangles, center, size);
        AddBox(vertices, normals, uvs, triangles,
            new Vector3(position.x, floorTop + checkoutHeight + 0.12f, position.y - checkoutLength * 0.1f),
            new Vector3(checkoutWidth + 0.14f, 0.24f, checkoutLength * 0.62f));
        AddBox(vertices, normals, uvs, triangles,
            new Vector3(position.x, floorTop + checkoutHeight + 0.45f, position.y + checkoutLength * 0.34f),
            new Vector3(checkoutWidth * 0.8f, 0.9f, checkoutLength * 0.22f));

        if (checkoutColliders != null && colliderIndex >= 0 && colliderIndex < checkoutColliders.Length)
        {
            UpdateCollider(checkoutColliders[colliderIndex], center, size);
        }
    }

    private void AddCeilingFixtures(
        List<Vector3> vertices,
        List<Vector3> normals,
        List<Vector2> uvs,
        List<int> triangles,
        float y)
    {
        float[] xPositions = { -11f, 0f, 11f };
        float[] zPositions = { 1f, 9f, 17f, 25f, 33f };
        for (int xIndex = 0; xIndex < xPositions.Length; xIndex++)
        {
            for (int zIndex = 0; zIndex < zPositions.Length; zIndex++)
            {
                AddBox(vertices, normals, uvs, triangles,
                    new Vector3(xPositions[xIndex], y, zPositions[zIndex]),
                    new Vector3(3.8f, 0.08f, 0.55f));
            }
        }
    }

    private void AddWallSkirting(
        List<Vector3> vertices,
        List<Vector3> normals,
        List<Vector2> uvs,
        List<int> triangles,
        float centerZ,
        float backZ,
        float floorTop)
    {
        float y = floorTop + 0.16f;
        float halfWidth = buildingWidth * 0.5f;
        AddBox(vertices, normals, uvs, triangles, new Vector3(-halfWidth + wallThickness, y, centerZ), new Vector3(0.12f, 0.32f, buildingLength));
        AddBox(vertices, normals, uvs, triangles, new Vector3(halfWidth - wallThickness, y, centerZ), new Vector3(0.12f, 0.32f, buildingLength));
        AddBox(vertices, normals, uvs, triangles, new Vector3(0f, y, backZ - wallThickness), new Vector3(buildingWidth, 0.32f, 0.12f));
    }

    private static void UpdateCollider(BoxCollider target, Vector3 center, Vector3 size)
    {
        if (target == null)
        {
            return;
        }

        target.center = center;
        target.size = size;
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
