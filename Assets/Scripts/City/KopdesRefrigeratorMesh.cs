using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(BoxCollider))]
public sealed class KopdesRefrigeratorMesh : MonoBehaviour
{
    [Header("Dimensions")]
    [SerializeField, Min(1f)] private float width = 3f;
    [SerializeField, Min(1f)] private float height = 3.2f;
    [SerializeField, Min(0.5f)] private float depth = 1.1f;
    [SerializeField, Range(2, 6)] private int shelfLevels = 4;

    [Header("Construction")]
    [SerializeField, Min(0.03f)] private float caseThickness = 0.12f;
    [SerializeField, Min(0.02f)] private float shelfThickness = 0.06f;
    [SerializeField, Min(0.02f)] private float doorFrameThickness = 0.09f;
    [SerializeField, Min(0.02f)] private float handleThickness = 0.05f;
    [SerializeField, Range(0.2f, 0.9f)] private float handleHeightRatio = 0.55f;

    [Header("Display Stock")]
    [SerializeField] private bool populateWithSimpleProducts = true;
    [SerializeField, Min(0.15f)] private float productSpacing = 0.32f;
    [SerializeField, Min(0.1f)] private float productHeight = 0.34f;

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
        DestroyGeneratedMesh();
    }

    public void Rebuild()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        BoxCollider refrigeratorCollider = GetComponent<BoxCollider>();
        if (meshFilter == null || refrigeratorCollider == null)
        {
            return;
        }

        float safeWidth = Mathf.Max(1f, width);
        float safeHeight = Mathf.Max(1f, height);
        float safeDepth = Mathf.Max(0.5f, depth);
        float safeCase = Mathf.Min(caseThickness, Mathf.Min(safeWidth, safeHeight) * 0.2f);
        float safeShelf = Mathf.Min(shelfThickness, safeHeight * 0.1f);
        float safeFrame = Mathf.Min(doorFrameThickness, safeWidth * 0.15f);
        float safeHandle = Mathf.Min(handleThickness, safeWidth * 0.08f);

        List<Vector3> vertices = new();
        List<Vector3> normals = new();
        List<Vector2> uvs = new();
        List<int> triangles = new();

        float halfWidth = safeWidth * 0.5f;
        float halfDepth = safeDepth * 0.5f;
        float caseY = safeHeight * 0.5f;

        AddBox(vertices, normals, uvs, triangles,
            new Vector3(-halfWidth + safeCase * 0.5f, caseY, 0f),
            new Vector3(safeCase, safeHeight, safeDepth));
        AddBox(vertices, normals, uvs, triangles,
            new Vector3(halfWidth - safeCase * 0.5f, caseY, 0f),
            new Vector3(safeCase, safeHeight, safeDepth));
        AddBox(vertices, normals, uvs, triangles,
            new Vector3(0f, safeCase * 0.5f, 0f),
            new Vector3(safeWidth, safeCase, safeDepth));
        AddBox(vertices, normals, uvs, triangles,
            new Vector3(0f, safeHeight - safeCase * 0.5f, 0f),
            new Vector3(safeWidth, safeCase, safeDepth));
        AddBox(vertices, normals, uvs, triangles,
            new Vector3(0f, caseY, halfDepth - safeCase * 0.5f),
            new Vector3(safeWidth, safeHeight, safeCase));

        float interiorWidth = safeWidth - safeCase * 2f;
        float interiorDepth = safeDepth - safeCase * 1.5f;
        for (int level = 1; level < shelfLevels; level++)
        {
            float y = level * (safeHeight / shelfLevels);
            AddBox(vertices, normals, uvs, triangles,
                new Vector3(0f, y, safeCase * 0.25f),
                new Vector3(interiorWidth, safeShelf, interiorDepth));

            if (populateWithSimpleProducts)
            {
                AddDisplayProducts(
                    vertices,
                    normals,
                    uvs,
                    triangles,
                    interiorWidth,
                    safeDepth,
                    safeShelf,
                    y,
                    level);
            }
        }

        float frontZ = -halfDepth - safeFrame * 0.35f;
        AddBox(vertices, normals, uvs, triangles,
            new Vector3(0f, safeFrame * 0.5f, frontZ),
            new Vector3(safeWidth, safeFrame, safeFrame));
        AddBox(vertices, normals, uvs, triangles,
            new Vector3(0f, safeHeight - safeFrame * 0.5f, frontZ),
            new Vector3(safeWidth, safeFrame, safeFrame));
        AddBox(vertices, normals, uvs, triangles,
            new Vector3(-halfWidth + safeFrame * 0.5f, caseY, frontZ),
            new Vector3(safeFrame, safeHeight, safeFrame));
        AddBox(vertices, normals, uvs, triangles,
            new Vector3(halfWidth - safeFrame * 0.5f, caseY, frontZ),
            new Vector3(safeFrame, safeHeight, safeFrame));
        AddBox(vertices, normals, uvs, triangles,
            new Vector3(0f, caseY, frontZ),
            new Vector3(safeFrame, safeHeight, safeFrame));

        float handleHeight = safeHeight * Mathf.Clamp01(handleHeightRatio);
        float handleLength = safeHeight * 0.28f;
        float handleOffset = safeFrame * 1.25f;
        float handleZ = frontZ - safeHandle;
        AddBox(vertices, normals, uvs, triangles,
            new Vector3(-handleOffset, handleHeight, handleZ),
            new Vector3(safeHandle, handleLength, safeHandle));
        AddBox(vertices, normals, uvs, triangles,
            new Vector3(handleOffset, handleHeight, handleZ),
            new Vector3(safeHandle, handleLength, safeHandle));

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

        generatedMesh.name = $"{name} Refrigerator Mesh";
        generatedMesh.SetVertices(vertices);
        generatedMesh.SetNormals(normals);
        generatedMesh.SetUVs(0, uvs);
        generatedMesh.SetTriangles(triangles, 0);
        generatedMesh.RecalculateBounds();
        meshFilter.sharedMesh = generatedMesh;

        refrigeratorCollider.center = new Vector3(0f, safeHeight * 0.5f, 0f);
        refrigeratorCollider.size = new Vector3(safeWidth, safeHeight, safeDepth);
    }

    private void AddDisplayProducts(
        List<Vector3> vertices,
        List<Vector3> normals,
        List<Vector2> uvs,
        List<int> triangles,
        float interiorWidth,
        float safeDepth,
        float safeShelf,
        float shelfY,
        int level)
    {
        float safeProductSpacing = Mathf.Max(0.15f, productSpacing);
        int slotCount = Mathf.Max(2, Mathf.FloorToInt(interiorWidth / safeProductSpacing));
        float slotWidth = interiorWidth / slotCount;
        float packageWidth = Mathf.Min(slotWidth * 0.62f, 0.22f);
        float packageDepth = Mathf.Min(0.28f, safeDepth * 0.36f);

        for (int slot = 0; slot < slotCount; slot++)
        {
            float x = -interiorWidth * 0.5f + slotWidth * (slot + 0.5f);
            float variation = 0.86f + ((slot + level) % 3) * 0.07f;
            float packageHeight = Mathf.Max(0.1f, productHeight) * variation;
            float y = shelfY + safeShelf * 0.5f + packageHeight * 0.5f;
            AddBox(vertices, normals, uvs, triangles,
                new Vector3(x, y, -safeDepth * 0.12f),
                new Vector3(packageWidth, packageHeight, packageDepth));
        }
    }

    private void DestroyGeneratedMesh()
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

        generatedMesh = null;
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
