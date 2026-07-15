using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class VillageStiltHouseSettlement : MonoBehaviour
{
    private enum HouseStyle
    {
        Compact,
        Family,
        SideVeranda,
        BroadVeranda,
        VillageHead
    }

    [Header("House References")]
    [SerializeField] private Transform[] houses = Array.Empty<Transform>();
    [SerializeField] private Material houseMaterial;
    [SerializeField] private int variationSeed = 173;
    [SerializeField] private HouseStyle[] stylePattern =
    {
        HouseStyle.Compact,
        HouseStyle.Family,
        HouseStyle.SideVeranda,
        HouseStyle.BroadVeranda
    };

    [Header("Shared Proportions")]
    [SerializeField, Range(-0.3f, 0f)] private float floorLevel = -0.14f;
    [SerializeField, Range(0.08f, 0.35f)] private float eaveHeight = 0.22f;
    [SerializeField, Range(0.3f, 0.7f)] private float ridgeHeight = 0.48f;
    [SerializeField, Range(0.015f, 0.08f)] private float postThickness = 0.035f;
    [SerializeField, Range(0.015f, 0.1f)] private float floorThickness = 0.045f;
    [SerializeField, Range(0.01f, 0.07f)] private float wallThickness = 0.025f;
    [SerializeField, Range(0.01f, 0.08f)] private float roofThickness = 0.03f;
    [SerializeField, Range(0.01f, 0.15f)] private float roofOverhang = 0.055f;
    [SerializeField, Range(2, 7)] private int stairSteps = 4;

    [Header("Wood Palettes")]
    [SerializeField] private Color[] wallColors =
    {
        new(0.42f, 0.27f, 0.14f),
        new(0.31f, 0.34f, 0.22f),
        new(0.38f, 0.20f, 0.13f),
        new(0.33f, 0.31f, 0.27f),
        new(0.47f, 0.32f, 0.18f)
    };
    [SerializeField] private Color[] roofColors =
    {
        new(0.15f, 0.12f, 0.10f),
        new(0.17f, 0.18f, 0.15f),
        new(0.19f, 0.13f, 0.11f),
        new(0.16f, 0.16f, 0.17f),
        new(0.13f, 0.10f, 0.08f)
    };
    [SerializeField] private Color trimColor = new(0.62f, 0.48f, 0.29f);
    [SerializeField] private Color windowColor = new(0.055f, 0.085f, 0.08f);
    [SerializeField] private Color foundationColor = new(0.25f, 0.24f, 0.22f);

    private readonly Dictionary<MeshFilter, Mesh> generatedMeshes = new();
    private readonly Dictionary<MeshFilter, Mesh> originalMeshes = new();

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
        foreach (KeyValuePair<MeshFilter, Mesh> pair in generatedMeshes)
        {
            MeshFilter filter = pair.Key;
            Mesh mesh = pair.Value;
            if (filter != null && filter.sharedMesh == mesh && originalMeshes.TryGetValue(filter, out Mesh original))
            {
                filter.sharedMesh = original;
            }

            DestroyGeneratedMesh(mesh);
        }

        generatedMeshes.Clear();
        originalMeshes.Clear();
    }

    [ContextMenu("Rebuild Stilt Houses")]
    public void Rebuild()
    {
        if (houses == null)
        {
            return;
        }

        for (int index = 0; index < houses.Length; index++)
        {
            Transform house = houses[index];
            if (house == null)
            {
                continue;
            }

            MeshFilter meshFilter = house.GetComponent<MeshFilter>();
            MeshRenderer meshRenderer = house.GetComponent<MeshRenderer>();
            if (meshFilter == null || meshRenderer == null)
            {
                continue;
            }

            if (!originalMeshes.ContainsKey(meshFilter))
            {
                originalMeshes.Add(meshFilter, meshFilter.sharedMesh);
            }

            bool isVillageHead = house.name.IndexOf("Head", StringComparison.OrdinalIgnoreCase) >= 0;
            HouseStyle style = GetStyle(index, isVillageHead);
            int paletteIndex = GetPaletteIndex(index, isVillageHead);
            BuildHouse(meshFilter, meshRenderer, house.GetComponent<BoxCollider>(), style, paletteIndex, index);
        }
    }

    private HouseStyle GetStyle(int index, bool isVillageHead)
    {
        if (isVillageHead)
        {
            return HouseStyle.VillageHead;
        }

        if (stylePattern == null || stylePattern.Length == 0)
        {
            return (HouseStyle)PositiveModulo(index + variationSeed, 4);
        }

        HouseStyle style = stylePattern[PositiveModulo(index + variationSeed, stylePattern.Length)];
        return style == HouseStyle.VillageHead ? HouseStyle.Family : style;
    }

    private int GetPaletteIndex(int index, bool isVillageHead)
    {
        if (isVillageHead)
        {
            return 4;
        }

        int mixed = unchecked(index * 1103515245 + variationSeed * 12345);
        return PositiveModulo(mixed, Mathf.Max(1, wallColors == null ? 0 : wallColors.Length));
    }

    private void BuildHouse(
        MeshFilter meshFilter,
        MeshRenderer meshRenderer,
        BoxCollider bodyCollider,
        HouseStyle style,
        int paletteIndex,
        int houseIndex)
    {
        MeshData data = new();
        HouseDimensions dimensions = GetDimensions(style);
        HousePalette palette = GetPalette(paletteIndex, style);

        AddFoundation(data, dimensions, palette, style);
        AddFloorsAndBeams(data, dimensions, palette, style);
        AddWalls(data, dimensions, palette, style, houseIndex);
        AddPorch(data, dimensions, palette, style, houseIndex);
        AddRoof(data, dimensions, palette, style);

        if (!generatedMeshes.TryGetValue(meshFilter, out Mesh mesh) || mesh == null)
        {
            mesh = new Mesh
            {
                hideFlags = HideFlags.DontSave,
                name = $"{meshFilter.name} Stilt House Mesh"
            };
            generatedMeshes[meshFilter] = mesh;
        }
        else
        {
            mesh.Clear();
        }

        mesh.SetVertices(data.Vertices);
        mesh.SetNormals(data.Normals);
        mesh.SetUVs(0, data.Uvs);
        mesh.SetColors(data.Colors);
        mesh.SetTriangles(data.Triangles, 0);
        mesh.RecalculateBounds();
        meshFilter.sharedMesh = mesh;

        if (houseMaterial != null)
        {
            meshRenderer.sharedMaterial = houseMaterial;
        }

        if (bodyCollider != null)
        {
            float wallBase = floorLevel + floorThickness * 0.45f;
            bodyCollider.center = new Vector3(
                0f,
                (wallBase + eaveHeight) * 0.5f,
                (dimensions.BodyFront + dimensions.BodyBack) * 0.5f);
            bodyCollider.size = new Vector3(
                dimensions.BodyWidth,
                eaveHeight - wallBase + floorThickness,
                dimensions.BodyDepth);
        }
    }

    private HouseDimensions GetDimensions(HouseStyle style)
    {
        return style switch
        {
            HouseStyle.Compact => new HouseDimensions(0.72f, 0.62f, 0.38f, -0.43f, 0.2f, 0f),
            HouseStyle.Family => new HouseDimensions(0.82f, 0.66f, 0.39f, -0.45f, 0.26f, 0f),
            HouseStyle.SideVeranda => new HouseDimensions(0.76f, 0.64f, 0.38f, -0.44f, 0.22f, 0.16f),
            HouseStyle.BroadVeranda => new HouseDimensions(0.88f, 0.6f, 0.38f, -0.47f, 0.34f, 0f),
            HouseStyle.VillageHead => new HouseDimensions(0.9f, 0.66f, 0.39f, -0.49f, 0.44f, 0.14f),
            _ => new HouseDimensions(0.8f, 0.64f, 0.38f, -0.45f, 0.26f, 0f)
        };
    }

    private HousePalette GetPalette(int index, HouseStyle style)
    {
        Color wall = GetColor(wallColors, index, new Color(0.4f, 0.27f, 0.15f));
        Color roof = GetColor(roofColors, index, new Color(0.16f, 0.13f, 0.1f));
        float variation = style == HouseStyle.VillageHead ? 1.08f : 0.94f + PositiveModulo(index, 4) * 0.035f;
        return new HousePalette(
            ToColor32(Multiply(wall, variation)),
            ToColor32(Multiply(wall, variation * 0.72f)),
            ToColor32(Multiply(trimColor, variation)),
            ToColor32(Multiply(roof, style == HouseStyle.VillageHead ? 0.88f : 1f)),
            ToColor32(windowColor),
            ToColor32(foundationColor));
    }

    private void AddFoundation(MeshData data, HouseDimensions d, HousePalette palette, HouseStyle style)
    {
        float bottom = -0.5f;
        float postTop = floorLevel - floorThickness * 0.45f;
        float postHeight = Mathf.Max(0.05f, postTop - bottom);
        float x = d.BodyWidth * 0.42f;
        float frontZ = d.BodyFront + 0.055f;
        float backZ = d.BodyBack - 0.055f;
        float[] xPositions = style == HouseStyle.VillageHead || style == HouseStyle.BroadVeranda
            ? new[] { -x, 0f, x }
            : new[] { -x, x };

        for (int xi = 0; xi < xPositions.Length; xi++)
        {
            AddPost(data, xPositions[xi], frontZ, bottom, postHeight, palette.DarkWood, palette.Foundation);
            AddPost(data, xPositions[xi], backZ, bottom, postHeight, palette.DarkWood, palette.Foundation);
        }

        float porchX = d.BodyWidth * 0.45f;
        AddPost(data, -porchX, d.PorchFront + 0.04f, bottom, postHeight, palette.DarkWood, palette.Foundation);
        AddPost(data, porchX, d.PorchFront + 0.04f, bottom, postHeight, palette.DarkWood, palette.Foundation);

        if (style != HouseStyle.Compact)
        {
            Vector3 leftLow = new(-x, bottom + 0.06f, backZ);
            Vector3 leftHigh = new(-x, postTop - 0.04f, frontZ);
            Vector3 rightLow = new(x, bottom + 0.06f, frontZ);
            Vector3 rightHigh = new(x, postTop - 0.04f, backZ);
            AddBeamBetween(data, leftLow, leftHigh, postThickness * 0.55f, palette.DarkWood);
            AddBeamBetween(data, rightLow, rightHigh, postThickness * 0.55f, palette.DarkWood);
        }
    }

    private void AddPost(
        MeshData data,
        float x,
        float z,
        float bottom,
        float height,
        Color32 wood,
        Color32 foundation)
    {
        AddBox(data, new Vector3(x, bottom + height * 0.5f, z), new Vector3(postThickness, height, postThickness), Quaternion.identity, wood);
        AddBox(data, new Vector3(x, bottom + 0.012f, z), new Vector3(postThickness * 1.65f, 0.024f, postThickness * 1.65f), Quaternion.identity, foundation);
    }

    private void AddFloorsAndBeams(MeshData data, HouseDimensions d, HousePalette palette, HouseStyle style)
    {
        AddBox(data,
            new Vector3(0f, floorLevel, (d.BodyFront + d.BodyBack) * 0.5f),
            new Vector3(d.BodyWidth, floorThickness, d.BodyDepth),
            Quaternion.identity,
            palette.Wood);

        float porchDepth = d.BodyFront - d.PorchFront;
        AddBox(data,
            new Vector3(0f, floorLevel, d.PorchFront + porchDepth * 0.5f),
            new Vector3(d.BodyWidth + 0.05f, floorThickness * 0.8f, porchDepth),
            Quaternion.identity,
            palette.Trim);

        float beamY = floorLevel - floorThickness * 0.65f;
        AddBox(data, new Vector3(-d.BodyWidth * 0.46f, beamY, (d.BodyFront + d.BodyBack) * 0.5f), new Vector3(postThickness * 0.8f, postThickness, d.BodyDepth), Quaternion.identity, palette.DarkWood);
        AddBox(data, new Vector3(d.BodyWidth * 0.46f, beamY, (d.BodyFront + d.BodyBack) * 0.5f), new Vector3(postThickness * 0.8f, postThickness, d.BodyDepth), Quaternion.identity, palette.DarkWood);

        if (style == HouseStyle.SideVeranda || style == HouseStyle.VillageHead)
        {
            int side = style == HouseStyle.VillageHead ? -1 : 1;
            float extension = d.SideVerandaWidth;
            float outerX = side * (d.BodyWidth * 0.5f + extension * 0.5f);
            float verandaDepth = d.BodyDepth * 0.75f;
            float z = d.BodyFront + verandaDepth * 0.5f;
            AddBox(data, new Vector3(outerX, floorLevel, z), new Vector3(extension, floorThickness * 0.8f, verandaDepth), Quaternion.identity, palette.Trim);
        }
    }

    private void AddWalls(MeshData data, HouseDimensions d, HousePalette palette, HouseStyle style, int houseIndex)
    {
        float wallBase = floorLevel + floorThickness * 0.5f;
        float wallHeight = Mathf.Max(0.08f, eaveHeight - wallBase);
        float wallY = wallBase + wallHeight * 0.5f;
        float centerZ = (d.BodyFront + d.BodyBack) * 0.5f;

        AddBox(data, new Vector3(0f, wallY, d.BodyFront), new Vector3(d.BodyWidth, wallHeight, wallThickness), Quaternion.identity, palette.Wood);
        AddBox(data, new Vector3(0f, wallY, d.BodyBack), new Vector3(d.BodyWidth, wallHeight, wallThickness), Quaternion.identity, palette.Wood);
        AddBox(data, new Vector3(-d.BodyWidth * 0.5f, wallY, centerZ), new Vector3(wallThickness, wallHeight, d.BodyDepth), Quaternion.identity, palette.Wood);
        AddBox(data, new Vector3(d.BodyWidth * 0.5f, wallY, centerZ), new Vector3(wallThickness, wallHeight, d.BodyDepth), Quaternion.identity, palette.Wood);

        AddWallBattens(data, d, palette, wallBase, wallHeight);

        float doorX = style switch
        {
            HouseStyle.SideVeranda => 0.17f,
            HouseStyle.BroadVeranda => -0.12f,
            _ => 0f
        };
        float doorWidth = style == HouseStyle.VillageHead ? 0.19f : 0.15f;
        float doorHeight = wallHeight * 0.78f;
        float frontSurface = d.BodyFront - wallThickness * 0.7f;
        AddBox(data,
            new Vector3(doorX, wallBase + doorHeight * 0.5f, frontSurface),
            new Vector3(doorWidth, doorHeight, wallThickness * 0.45f),
            Quaternion.identity,
            palette.DarkWood);
        AddDoorFrame(data, doorX, wallBase, doorWidth, doorHeight, frontSurface - wallThickness * 0.3f, palette.Trim);

        if (style == HouseStyle.Compact)
        {
            AddFrontWindow(data, -0.22f, wallBase, wallHeight, frontSurface, palette);
        }
        else if (style == HouseStyle.SideVeranda)
        {
            AddFrontWindow(data, -0.2f, wallBase, wallHeight, frontSurface, palette);
            AddSideWindow(data, -1, centerZ + 0.08f, wallBase, wallHeight, d, palette);
        }
        else
        {
            AddFrontWindow(data, -d.BodyWidth * 0.3f, wallBase, wallHeight, frontSurface, palette);
            AddFrontWindow(data, d.BodyWidth * 0.3f, wallBase, wallHeight, frontSurface, palette);
            AddSideWindow(data, houseIndex % 2 == 0 ? -1 : 1, centerZ + 0.06f, wallBase, wallHeight, d, palette);
        }

        AddGableSlats(data, d, palette, d.BodyFront - wallThickness * 0.25f);
        AddGableSlats(data, d, palette, d.BodyBack + wallThickness * 0.25f);
    }

    private void AddWallBattens(MeshData data, HouseDimensions d, HousePalette palette, float wallBase, float wallHeight)
    {
        float frontZ = d.BodyFront - wallThickness * 0.65f;
        float backZ = d.BodyBack + wallThickness * 0.65f;
        float y = wallBase + wallHeight * 0.5f;
        const int frontSegments = 7;
        for (int segment = 1; segment < frontSegments; segment++)
        {
            float x = Mathf.Lerp(-d.BodyWidth * 0.5f, d.BodyWidth * 0.5f, segment / (float)frontSegments);
            AddBox(data, new Vector3(x, y, frontZ), new Vector3(0.008f, wallHeight, 0.008f), Quaternion.identity, palette.Trim);
            AddBox(data, new Vector3(x, y, backZ), new Vector3(0.008f, wallHeight, 0.008f), Quaternion.identity, palette.Trim);
        }

        for (int segment = 1; segment < 5; segment++)
        {
            float z = Mathf.Lerp(d.BodyFront, d.BodyBack, segment / 5f);
            AddBox(data, new Vector3(-d.BodyWidth * 0.5f - wallThickness * 0.65f, y, z), new Vector3(0.008f, wallHeight, 0.008f), Quaternion.identity, palette.Trim);
            AddBox(data, new Vector3(d.BodyWidth * 0.5f + wallThickness * 0.65f, y, z), new Vector3(0.008f, wallHeight, 0.008f), Quaternion.identity, palette.Trim);
        }
    }

    private void AddDoorFrame(MeshData data, float x, float bottom, float width, float height, float z, Color32 color)
    {
        float frame = 0.014f;
        AddBox(data, new Vector3(x - width * 0.5f, bottom + height * 0.5f, z), new Vector3(frame, height + frame, frame), Quaternion.identity, color);
        AddBox(data, new Vector3(x + width * 0.5f, bottom + height * 0.5f, z), new Vector3(frame, height + frame, frame), Quaternion.identity, color);
        AddBox(data, new Vector3(x, bottom + height, z), new Vector3(width + frame, frame, frame), Quaternion.identity, color);
        AddBox(data, new Vector3(x + width * 0.32f, bottom + height * 0.48f, z - frame), new Vector3(0.012f, 0.012f, 0.012f), Quaternion.identity, color);
    }

    private void AddFrontWindow(MeshData data, float x, float wallBase, float wallHeight, float z, HousePalette palette)
    {
        float width = 0.145f;
        float height = wallHeight * 0.42f;
        float y = wallBase + wallHeight * 0.57f;
        AddBox(data, new Vector3(x, y, z - wallThickness * 0.35f), new Vector3(width, height, 0.012f), Quaternion.identity, palette.Window);
        AddWindowFrameFront(data, x, y, z - wallThickness * 0.65f, width, height, palette.Trim);
    }

    private void AddWindowFrameFront(MeshData data, float x, float y, float z, float width, float height, Color32 color)
    {
        const float frame = 0.012f;
        AddBox(data, new Vector3(x, y - height * 0.5f, z), new Vector3(width + frame, frame, frame), Quaternion.identity, color);
        AddBox(data, new Vector3(x, y + height * 0.5f, z), new Vector3(width + frame, frame, frame), Quaternion.identity, color);
        AddBox(data, new Vector3(x - width * 0.5f, y, z), new Vector3(frame, height, frame), Quaternion.identity, color);
        AddBox(data, new Vector3(x + width * 0.5f, y, z), new Vector3(frame, height, frame), Quaternion.identity, color);
        AddBox(data, new Vector3(x, y, z), new Vector3(frame, height, frame), Quaternion.identity, color);
    }

    private void AddSideWindow(MeshData data, int side, float z, float wallBase, float wallHeight, HouseDimensions d, HousePalette palette)
    {
        float width = 0.16f;
        float height = wallHeight * 0.4f;
        float y = wallBase + wallHeight * 0.58f;
        float x = side * (d.BodyWidth * 0.5f + wallThickness * 0.7f);
        AddBox(data, new Vector3(x, y, z), new Vector3(0.012f, height, width), Quaternion.identity, palette.Window);
        const float frame = 0.012f;
        float outerX = x + side * frame * 0.35f;
        AddBox(data, new Vector3(outerX, y - height * 0.5f, z), new Vector3(frame, frame, width + frame), Quaternion.identity, palette.Trim);
        AddBox(data, new Vector3(outerX, y + height * 0.5f, z), new Vector3(frame, frame, width + frame), Quaternion.identity, palette.Trim);
        AddBox(data, new Vector3(outerX, y, z - width * 0.5f), new Vector3(frame, height, frame), Quaternion.identity, palette.Trim);
        AddBox(data, new Vector3(outerX, y, z + width * 0.5f), new Vector3(frame, height, frame), Quaternion.identity, palette.Trim);
    }

    private void AddGableSlats(MeshData data, HouseDimensions d, HousePalette palette, float z)
    {
        const int slats = 9;
        float width = d.BodyWidth / slats;
        float halfWidth = d.BodyWidth * 0.5f;
        float ridge = Mathf.Max(eaveHeight + 0.05f, ridgeHeight);
        for (int index = 0; index < slats; index++)
        {
            float x = -halfWidth + width * (index + 0.5f);
            float normalized = 1f - Mathf.Abs(x) / Mathf.Max(0.01f, halfWidth);
            float top = Mathf.Lerp(eaveHeight, ridge, normalized);
            float height = Mathf.Max(0.01f, top - eaveHeight);
            AddBox(data, new Vector3(x, eaveHeight + height * 0.5f, z), new Vector3(width * 0.9f, height, wallThickness), Quaternion.identity, palette.Wood);
        }
    }

    private void AddPorch(MeshData data, HouseDimensions d, HousePalette palette, HouseStyle style, int houseIndex)
    {
        float stairWidth = d.StairWidth;
        float stairX = style switch
        {
            HouseStyle.SideVeranda => 0.17f,
            HouseStyle.BroadVeranda => -0.12f,
            _ => 0f
        };
        int steps = Mathf.Max(2, stairSteps + (style == HouseStyle.VillageHead ? 1 : style == HouseStyle.Compact ? -1 : 0));
        AddStairs(data, d.PorchFront, stairX, stairWidth, steps, palette);

        if (style != HouseStyle.Compact)
        {
            AddPorchRails(data, d, stairX, stairWidth, palette, style == HouseStyle.VillageHead);
        }

        if (style == HouseStyle.SideVeranda || style == HouseStyle.VillageHead)
        {
            int side = style == HouseStyle.VillageHead ? -1 : houseIndex % 2 == 0 ? -1 : 1;
            AddSideVeranda(data, d, side, palette);
        }

        if (style == HouseStyle.BroadVeranda || style == HouseStyle.VillageHead)
        {
            float columnHeight = eaveHeight - floorLevel;
            float columnY = floorLevel + columnHeight * 0.5f;
            float x = d.BodyWidth * 0.4f;
            float z = d.PorchFront + 0.045f;
            AddBox(data, new Vector3(-x, columnY, z), new Vector3(postThickness * 0.75f, columnHeight, postThickness * 0.75f), Quaternion.identity, palette.Trim);
            AddBox(data, new Vector3(x, columnY, z), new Vector3(postThickness * 0.75f, columnHeight, postThickness * 0.75f), Quaternion.identity, palette.Trim);
        }
    }

    private void AddStairs(MeshData data, float porchFront, float x, float width, int steps, HousePalette palette)
    {
        float bottom = -0.5f;
        float totalRise = Mathf.Max(0.08f, floorLevel - bottom);
        float stepRise = totalRise / steps;
        float run = Mathf.Clamp(totalRise * 0.52f, 0.15f, 0.23f);
        float stepDepth = run / steps;
        float stairFront = porchFront - run;

        for (int step = 0; step < steps; step++)
        {
            float top = bottom + stepRise * (step + 1);
            float z = stairFront + stepDepth * (step + 0.5f);
            AddBox(data,
                new Vector3(x, bottom + (top - bottom) * 0.5f, z),
                new Vector3(width, top - bottom, stepDepth * 1.08f),
                Quaternion.identity,
                step % 2 == 0 ? palette.Trim : palette.Wood);
        }
    }

    private void AddPorchRails(MeshData data, HouseDimensions d, float stairX, float stairWidth, HousePalette palette, bool ornate)
    {
        float railHeight = ornate ? 0.19f : 0.15f;
        float railY = floorLevel + railHeight * 0.58f;
        float z = d.PorchFront + 0.025f;
        float leftEdge = -d.BodyWidth * 0.48f;
        float rightEdge = d.BodyWidth * 0.48f;
        float gapLeft = stairX - stairWidth * 0.58f;
        float gapRight = stairX + stairWidth * 0.58f;

        AddRailSegment(data, leftEdge, gapLeft, z, railY, railHeight, palette.Trim, ornate);
        AddRailSegment(data, gapRight, rightEdge, z, railY, railHeight, palette.Trim, ornate);

        float sideDepth = d.BodyFront - d.PorchFront;
        float sideZ = d.PorchFront + sideDepth * 0.5f;
        AddBox(data, new Vector3(leftEdge, railY, sideZ), new Vector3(0.012f, 0.012f, sideDepth), Quaternion.identity, palette.Trim);
        AddBox(data, new Vector3(rightEdge, railY, sideZ), new Vector3(0.012f, 0.012f, sideDepth), Quaternion.identity, palette.Trim);
    }

    private void AddRailSegment(MeshData data, float fromX, float toX, float z, float y, float height, Color32 color, bool ornate)
    {
        if (toX - fromX < 0.025f)
        {
            return;
        }

        float center = (fromX + toX) * 0.5f;
        float width = toX - fromX;
        AddBox(data, new Vector3(center, y, z), new Vector3(width, 0.012f, 0.012f), Quaternion.identity, color);
        AddBox(data, new Vector3(center, y - height * 0.35f, z), new Vector3(width, 0.009f, 0.009f), Quaternion.identity, color);

        int posts = ornate ? 4 : 2;
        for (int index = 0; index <= posts; index++)
        {
            float x = Mathf.Lerp(fromX, toX, index / (float)posts);
            AddBox(data, new Vector3(x, floorLevel + height * 0.5f, z), new Vector3(0.011f, height, 0.011f), Quaternion.identity, color);
        }
    }

    private void AddSideVeranda(MeshData data, HouseDimensions d, int side, HousePalette palette)
    {
        float outerX = side * (d.BodyWidth * 0.5f + d.SideVerandaWidth);
        float zStart = d.BodyFront + 0.03f;
        float zEnd = d.BodyBack - d.BodyDepth * 0.2f;
        float railY = floorLevel + 0.09f;
        AddBox(data, new Vector3(outerX, railY, (zStart + zEnd) * 0.5f), new Vector3(0.012f, 0.012f, zEnd - zStart), Quaternion.identity, palette.Trim);

        for (int index = 0; index < 4; index++)
        {
            float z = Mathf.Lerp(zStart, zEnd, index / 3f);
            AddBox(data, new Vector3(outerX, floorLevel + 0.075f, z), new Vector3(0.012f, 0.15f, 0.012f), Quaternion.identity, palette.Trim);
        }

        float roofHalfSpan = d.SideVerandaWidth * 0.75f;
        float roofRise = 0.055f;
        float panelLength = Mathf.Sqrt(roofHalfSpan * roofHalfSpan + roofRise * roofRise);
        float roofCenterX = side * (d.BodyWidth * 0.5f + roofHalfSpan * 0.5f);
        float roofCenterY = eaveHeight - roofRise * 0.35f;
        float angle = Mathf.Atan2(roofRise, roofHalfSpan) * Mathf.Rad2Deg;
        AddBox(data,
            new Vector3(roofCenterX, roofCenterY, (zStart + zEnd) * 0.5f),
            new Vector3(panelLength, roofThickness, zEnd - zStart + 0.08f),
            Quaternion.Euler(0f, 0f, -side * angle),
            palette.Roof);
    }

    private void AddRoof(MeshData data, HouseDimensions d, HousePalette palette, HouseStyle style)
    {
        float roofFront = d.PorchFront - roofOverhang * 0.55f;
        float roofBack = d.BodyBack + roofOverhang;
        float roofLength = roofBack - roofFront;
        float roofCenterZ = (roofFront + roofBack) * 0.5f;
        float halfSpan = d.BodyWidth * 0.5f + roofOverhang;
        float ridge = Mathf.Max(eaveHeight + 0.06f, ridgeHeight + (style == HouseStyle.VillageHead ? 0.035f : 0f));
        AddRoofPair(data, halfSpan, eaveHeight, ridge, roofCenterZ, roofLength, palette.Roof);

        AddBox(data,
            new Vector3(0f, ridge + roofThickness * 0.2f, roofCenterZ),
            new Vector3(roofThickness * 1.45f, roofThickness * 1.45f, roofLength + 0.025f),
            Quaternion.Euler(0f, 0f, 45f),
            palette.Roof);

        if (style == HouseStyle.VillageHead)
        {
            float upperEave = ridge + 0.01f;
            float upperRidge = ridge + 0.12f;
            float upperHalfSpan = halfSpan * 0.48f;
            float upperLength = roofLength * 0.68f;
            AddBox(data,
                new Vector3(0f, upperEave - 0.025f, roofCenterZ),
                new Vector3(upperHalfSpan * 1.25f, 0.065f, upperLength * 0.82f),
                Quaternion.identity,
                palette.DarkWood);
            AddRoofPair(data, upperHalfSpan, upperEave, upperRidge, roofCenterZ, upperLength, palette.Roof);
            AddBox(data,
                new Vector3(0f, upperRidge, roofCenterZ),
                new Vector3(roofThickness * 1.35f, roofThickness * 1.35f, upperLength + 0.02f),
                Quaternion.Euler(0f, 0f, 45f),
                palette.Trim);
        }
    }

    private void AddRoofPair(MeshData data, float halfSpan, float eave, float ridge, float centerZ, float length, Color32 color)
    {
        float rise = ridge - eave;
        float panelLength = Mathf.Sqrt(halfSpan * halfSpan + rise * rise);
        float pitch = Mathf.Atan2(rise, halfSpan) * Mathf.Rad2Deg;
        float centerY = (eave + ridge) * 0.5f;

        AddBox(data,
            new Vector3(-halfSpan * 0.5f, centerY, centerZ),
            new Vector3(panelLength, roofThickness, length),
            Quaternion.Euler(0f, 0f, pitch),
            color);
        AddBox(data,
            new Vector3(halfSpan * 0.5f, centerY, centerZ),
            new Vector3(panelLength, roofThickness, length),
            Quaternion.Euler(0f, 0f, -pitch),
            color);
    }

    private static void AddBeamBetween(MeshData data, Vector3 from, Vector3 to, float thickness, Color32 color)
    {
        Vector3 direction = to - from;
        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        AddBox(
            data,
            (from + to) * 0.5f,
            new Vector3(thickness, direction.magnitude, thickness),
            Quaternion.FromToRotation(Vector3.up, direction.normalized),
            color);
    }

    private static void AddBox(MeshData data, Vector3 center, Vector3 size, Quaternion rotation, Color32 color)
    {
        Vector3 half = size * 0.5f;
        AddFace(data, center, rotation,
            new Vector3(-half.x, -half.y, half.z), new Vector3(half.x, -half.y, half.z),
            new Vector3(half.x, half.y, half.z), new Vector3(-half.x, half.y, half.z), Vector3.forward, color);
        AddFace(data, center, rotation,
            new Vector3(half.x, -half.y, -half.z), new Vector3(-half.x, -half.y, -half.z),
            new Vector3(-half.x, half.y, -half.z), new Vector3(half.x, half.y, -half.z), Vector3.back, color);
        AddFace(data, center, rotation,
            new Vector3(-half.x, -half.y, -half.z), new Vector3(-half.x, -half.y, half.z),
            new Vector3(-half.x, half.y, half.z), new Vector3(-half.x, half.y, -half.z), Vector3.left, color);
        AddFace(data, center, rotation,
            new Vector3(half.x, -half.y, half.z), new Vector3(half.x, -half.y, -half.z),
            new Vector3(half.x, half.y, -half.z), new Vector3(half.x, half.y, half.z), Vector3.right, color);
        AddFace(data, center, rotation,
            new Vector3(-half.x, half.y, half.z), new Vector3(half.x, half.y, half.z),
            new Vector3(half.x, half.y, -half.z), new Vector3(-half.x, half.y, -half.z), Vector3.up, color);
        AddFace(data, center, rotation,
            new Vector3(-half.x, -half.y, -half.z), new Vector3(half.x, -half.y, -half.z),
            new Vector3(half.x, -half.y, half.z), new Vector3(-half.x, -half.y, half.z), Vector3.down, color);
    }

    private static void AddFace(
        MeshData data,
        Vector3 center,
        Quaternion rotation,
        Vector3 a,
        Vector3 b,
        Vector3 c,
        Vector3 d,
        Vector3 normal,
        Color32 color)
    {
        int start = data.Vertices.Count;
        data.Vertices.Add(center + rotation * a);
        data.Vertices.Add(center + rotation * b);
        data.Vertices.Add(center + rotation * c);
        data.Vertices.Add(center + rotation * d);
        Vector3 rotatedNormal = rotation * normal;
        data.Normals.Add(rotatedNormal);
        data.Normals.Add(rotatedNormal);
        data.Normals.Add(rotatedNormal);
        data.Normals.Add(rotatedNormal);
        data.Uvs.Add(new Vector2(0f, 0f));
        data.Uvs.Add(new Vector2(1f, 0f));
        data.Uvs.Add(new Vector2(1f, 1f));
        data.Uvs.Add(new Vector2(0f, 1f));
        data.Colors.Add(color);
        data.Colors.Add(color);
        data.Colors.Add(color);
        data.Colors.Add(color);
        data.Triangles.Add(start);
        data.Triangles.Add(start + 1);
        data.Triangles.Add(start + 2);
        data.Triangles.Add(start);
        data.Triangles.Add(start + 2);
        data.Triangles.Add(start + 3);
    }

    private static int PositiveModulo(int value, int divisor)
    {
        if (divisor <= 0)
        {
            return 0;
        }

        int result = value % divisor;
        return result < 0 ? result + divisor : result;
    }

    private static Color GetColor(Color[] colors, int index, Color fallback)
    {
        return colors == null || colors.Length == 0 ? fallback : colors[PositiveModulo(index, colors.Length)];
    }

    private static Color Multiply(Color color, float multiplier)
    {
        return new Color(
            Mathf.Clamp01(color.r * multiplier),
            Mathf.Clamp01(color.g * multiplier),
            Mathf.Clamp01(color.b * multiplier),
            color.a);
    }

    private static Color32 ToColor32(Color color)
    {
        return color;
    }

    private static void DestroyGeneratedMesh(Mesh mesh)
    {
        if (mesh == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(mesh);
        }
        else
        {
            DestroyImmediate(mesh);
        }
    }

    private sealed class MeshData
    {
        public readonly List<Vector3> Vertices = new();
        public readonly List<Vector3> Normals = new();
        public readonly List<Vector2> Uvs = new();
        public readonly List<Color32> Colors = new();
        public readonly List<int> Triangles = new();
    }

    private readonly struct HouseDimensions
    {
        public HouseDimensions(float bodyWidth, float bodyDepth, float bodyBack, float porchFront, float stairWidth, float sideVerandaWidth)
        {
            BodyWidth = bodyWidth;
            BodyDepth = bodyDepth;
            BodyBack = bodyBack;
            PorchFront = porchFront;
            StairWidth = stairWidth;
            SideVerandaWidth = sideVerandaWidth;
        }

        public float BodyWidth { get; }
        public float BodyDepth { get; }
        public float BodyBack { get; }
        public float BodyFront => BodyBack - BodyDepth;
        public float PorchFront { get; }
        public float StairWidth { get; }
        public float SideVerandaWidth { get; }
    }

    private readonly struct HousePalette
    {
        public HousePalette(Color32 wood, Color32 darkWood, Color32 trim, Color32 roof, Color32 window, Color32 foundation)
        {
            Wood = wood;
            DarkWood = darkWood;
            Trim = trim;
            Roof = roof;
            Window = window;
            Foundation = foundation;
        }

        public Color32 Wood { get; }
        public Color32 DarkWood { get; }
        public Color32 Trim { get; }
        public Color32 Roof { get; }
        public Color32 Window { get; }
        public Color32 Foundation { get; }
    }
}
