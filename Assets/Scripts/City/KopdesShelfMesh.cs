using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(BoxCollider))]
public sealed class KopdesShelfMesh : MonoBehaviour
{
    [Header("Dimensions")]
    [SerializeField, Min(0.5f)] private float width = 9f;
    [SerializeField, Min(0.5f)] private float height = 2.1f;
    [SerializeField, Min(0.3f)] private float depth = 1.4f;
    [SerializeField, Range(2, 6)] private int shelfLevels = 4;

    [Header("Construction")]
    [SerializeField, Min(0.02f)] private float boardThickness = 0.08f;
    [SerializeField, Min(0.02f)] private float uprightThickness = 0.12f;
    [SerializeField, Min(0.02f)] private float centerPanelThickness = 0.08f;
    [SerializeField, Min(0f)] private float endInset = 0.08f;

    [Header("Store Display")]
    [SerializeField] private bool populateWithSimpleProducts = true;
    [SerializeField, Min(0.25f)] private float productSpacing = 0.6f;
    [SerializeField, Min(0.1f)] private float productHeight = 0.42f;
    [SerializeField, Min(0.08f)] private float productDepth = 0.24f;

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
        BoxCollider shelfCollider = GetComponent<BoxCollider>();
        if (meshFilter == null || shelfCollider == null)
        {
            return;
        }

        float safeWidth = Mathf.Max(0.5f, width);
        float safeHeight = Mathf.Max(0.5f, height);
        float safeDepth = Mathf.Max(0.3f, depth);
        float safeBoardThickness = Mathf.Min(boardThickness, safeHeight * 0.2f);
        float safeUprightThickness = Mathf.Min(uprightThickness, safeWidth * 0.2f);
        float safePanelThickness = Mathf.Min(centerPanelThickness, safeDepth * 0.5f);

        List<Vector3> vertices = new();
        List<Vector3> normals = new();
        List<Vector2> uvs = new();
        List<int> triangles = new();

        float usableWidth = Mathf.Max(0.1f, safeWidth - (endInset * 2f));
        for (int level = 0; level < shelfLevels; level++)
        {
            float t = shelfLevels > 1 ? level / (float)(shelfLevels - 1) : 0f;
            float y = Mathf.Lerp(safeBoardThickness * 0.5f, safeHeight - safeBoardThickness * 0.5f, t);
            AddBox(vertices, normals, uvs, triangles,
                new Vector3(0f, y, 0f),
                new Vector3(usableWidth, safeBoardThickness, safeDepth));
        }

        float uprightY = safeHeight * 0.5f;
        float uprightX = safeWidth * 0.5f - safeUprightThickness * 0.5f;
        AddBox(vertices, normals, uvs, triangles,
            new Vector3(-uprightX, uprightY, 0f),
            new Vector3(safeUprightThickness, safeHeight, safeDepth));
        AddBox(vertices, normals, uvs, triangles,
            new Vector3(uprightX, uprightY, 0f),
            new Vector3(safeUprightThickness, safeHeight, safeDepth));
        AddBox(vertices, normals, uvs, triangles,
            new Vector3(0f, uprightY, 0f),
            new Vector3(usableWidth, safeHeight, safePanelThickness));

        if (populateWithSimpleProducts)
        {
            AddDisplayProducts(
                vertices,
                normals,
                uvs,
                triangles,
                usableWidth,
                safeHeight,
                safeDepth,
                safeBoardThickness);
        }

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

        generatedMesh.name = $"{name} Shelf Mesh";
        generatedMesh.SetVertices(vertices);
        generatedMesh.SetNormals(normals);
        generatedMesh.SetUVs(0, uvs);
        generatedMesh.SetTriangles(triangles, 0);
        generatedMesh.RecalculateBounds();
        meshFilter.sharedMesh = generatedMesh;

        shelfCollider.center = new Vector3(0f, safeHeight * 0.5f, 0f);
        shelfCollider.size = new Vector3(safeWidth, safeHeight, safeDepth);
    }

    private void AddDisplayProducts(
        List<Vector3> vertices,
        List<Vector3> normals,
        List<Vector2> uvs,
        List<int> triangles,
        float usableWidth,
        float safeHeight,
        float safeDepth,
        float safeBoardThickness)
    {
        float safeProductSpacing = Mathf.Max(0.25f, productSpacing);
        int slotCount = Mathf.Max(1, Mathf.FloorToInt(usableWidth / safeProductSpacing));
        float slotWidth = usableWidth / slotCount;
        float packageWidth = Mathf.Min(slotWidth * 0.72f, 0.48f);
        float packageDepth = Mathf.Min(Mathf.Max(0.08f, productDepth), safeDepth * 0.38f);
        float sideOffset = safeDepth * 0.5f - packageDepth * 0.55f;

        for (int level = 0; level < shelfLevels - 1; level++)
        {
            float levelT = shelfLevels > 1 ? level / (float)(shelfLevels - 1) : 0f;
            float shelfY = Mathf.Lerp(
                safeBoardThickness * 0.5f,
                safeHeight - safeBoardThickness * 0.5f,
                levelT);

            for (int slot = 0; slot < slotCount; slot++)
            {
                float x = -usableWidth * 0.5f + slotWidth * (slot + 0.5f);
                float variation = 0.82f + ((slot + level) % 3) * 0.09f;
                float packageHeight = Mathf.Max(0.1f, productHeight) * variation;
                float packageY = shelfY + safeBoardThickness * 0.5f + packageHeight * 0.5f;
                Vector3 packageSize = new(packageWidth, packageHeight, packageDepth);

                AddBox(vertices, normals, uvs, triangles,
                    new Vector3(x, packageY, -sideOffset),
                    packageSize);
                AddBox(vertices, normals, uvs, triangles,
                    new Vector3(x, packageY, sideOffset),
                    packageSize);
            }
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
