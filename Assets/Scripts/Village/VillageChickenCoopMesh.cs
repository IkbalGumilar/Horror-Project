using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public sealed class VillageChickenCoopMesh : MonoBehaviour
{
    private const int WoodMaterial = 0;
    private const int RoofMaterial = 1;
    private const int WireMaterial = 2;
    private const int DarkMaterial = 3;
    private const int MaterialCount = 4;

    [Header("Walk-in Dimensions")]
    [SerializeField, Min(6f)] private float width = 8.5f;
    [SerializeField, Min(4f)] private float depth = 5.2f;
    [SerializeField, Min(2.4f)] private float wallHeight = 2.7f;
    [SerializeField, Min(0.25f)] private float roofRise = 0.65f;
    [SerializeField, Min(0.1f)] private float roofOverhang = 0.38f;

    [Header("Structure")]
    [SerializeField, Min(0.08f)] private float frameThickness = 0.14f;
    [SerializeField, Min(1.8f)] private float enclosedHouseWidth = 2.6f;
    [SerializeField, Min(0.35f)] private float enclosedFloorHeight = 0.78f;
    [SerializeField, Min(0.2f)] private float wireSpacing = 0.45f;
    [SerializeField, Min(0.008f)] private float wireThickness = 0.018f;

    [Header("Player Entrance")]
    [SerializeField, Min(1f)] private float entranceWidth = 1.35f;
    [SerializeField, Min(2f)] private float entranceHeight = 2.25f;
    [SerializeField, Range(35f, 110f)] private float openedDoorAngle = 72f;

    private Mesh visualMesh;
    private Mesh collisionMesh;

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
        DestroyMesh(visualMesh);
        DestroyMesh(collisionMesh);
    }

    public void Rebuild()
    {
        MeshFilter filter = GetComponent<MeshFilter>();
        MeshCollider meshCollider = GetComponent<MeshCollider>();
        if (filter == null || meshCollider == null)
        {
            return;
        }

        float safeHouseWidth = Mathf.Clamp(enclosedHouseWidth, 1.8f, width * 0.42f);
        float runWidth = width - safeHouseWidth;
        float safeEntranceWidth = Mathf.Min(entranceWidth, runWidth - frameThickness * 4f);
        float safeEntranceHeight = Mathf.Min(entranceHeight, wallHeight - frameThickness * 1.5f);

        MeshData visual = new(MaterialCount);
        BuildFrame(visual, safeHouseWidth);
        BuildWireRun(visual, safeHouseWidth, safeEntranceWidth, safeEntranceHeight);
        BuildOpenedDoor(visual, safeHouseWidth, safeEntranceWidth, safeEntranceHeight);
        BuildEnclosedHouse(visual, safeHouseWidth);
        BuildRoof(visual);
        BuildInteriorDetails(visual, safeHouseWidth);

        visualMesh = ApplyMesh(visualMesh, visual, $"{name} Visual Mesh");
        filter.sharedMesh = visualMesh;

        MeshData collision = new(1);
        BuildCollision(collision, safeHouseWidth, safeEntranceWidth);
        collisionMesh = ApplyMesh(collisionMesh, collision, $"{name} Collision Mesh");
        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = collisionMesh;
    }

    private void BuildFrame(MeshData mesh, float houseWidth)
    {
        float halfWidth = width * 0.5f;
        float halfDepth = depth * 0.5f;
        float partitionX = halfWidth - houseWidth;
        float postY = wallHeight * 0.5f;
        Vector3 postSize = new(frameThickness, wallHeight, frameThickness);

        float[] xPosts =
        {
            -halfWidth,
            -halfWidth + (partitionX + halfWidth) * 0.5f,
            partitionX,
            halfWidth
        };

        for (int i = 0; i < xPosts.Length; i++)
        {
            mesh.AddBox(new Vector3(xPosts[i], postY, -halfDepth), postSize, Quaternion.identity, WoodMaterial);
            mesh.AddBox(new Vector3(xPosts[i], postY, halfDepth), postSize, Quaternion.identity, WoodMaterial);
        }

        mesh.AddBox(new Vector3(-halfWidth, postY, 0f), new Vector3(frameThickness, wallHeight, depth), Quaternion.identity, WoodMaterial);
        mesh.AddBox(new Vector3(halfWidth, postY, 0f), new Vector3(frameThickness, wallHeight, depth), Quaternion.identity, WoodMaterial);

        float beamY = frameThickness * 0.5f;
        float topBeamY = wallHeight - frameThickness * 0.5f;
        Vector3 longBeam = new(width, frameThickness, frameThickness);
        Vector3 sideBeam = new(frameThickness, frameThickness, depth);
        mesh.AddBox(new Vector3(0f, beamY, -halfDepth), longBeam, Quaternion.identity, WoodMaterial);
        mesh.AddBox(new Vector3(0f, beamY, halfDepth), longBeam, Quaternion.identity, WoodMaterial);
        mesh.AddBox(new Vector3(0f, topBeamY, -halfDepth), longBeam, Quaternion.identity, WoodMaterial);
        mesh.AddBox(new Vector3(0f, topBeamY, halfDepth), longBeam, Quaternion.identity, WoodMaterial);
        mesh.AddBox(new Vector3(-halfWidth, beamY, 0f), sideBeam, Quaternion.identity, WoodMaterial);
        mesh.AddBox(new Vector3(halfWidth, beamY, 0f), sideBeam, Quaternion.identity, WoodMaterial);
        mesh.AddBox(new Vector3(-halfWidth, topBeamY, 0f), sideBeam, Quaternion.identity, WoodMaterial);
        mesh.AddBox(new Vector3(halfWidth, topBeamY, 0f), sideBeam, Quaternion.identity, WoodMaterial);
    }

    private void BuildWireRun(MeshData mesh, float houseWidth, float doorWidth, float doorHeight)
    {
        float halfWidth = width * 0.5f;
        float halfDepth = depth * 0.5f;
        float partitionX = halfWidth - houseWidth;
        float runWidth = partitionX + halfWidth;
        float doorCenter = -halfWidth + runWidth * 0.58f;

        AddWirePanelZ(mesh, -halfDepth + frameThickness * 0.22f,
            -halfWidth, partitionX, doorCenter, doorWidth, doorHeight);
        AddWirePanelZ(mesh, halfDepth - frameThickness * 0.22f,
            -halfWidth, partitionX, float.PositiveInfinity, 0f, 0f);
        AddWirePanelX(mesh, -halfWidth + frameThickness * 0.22f, -halfDepth, halfDepth);

        float jambY = doorHeight * 0.5f;
        mesh.AddBox(new Vector3(doorCenter - doorWidth * 0.5f, jambY, -halfDepth),
            new Vector3(frameThickness, doorHeight, frameThickness), Quaternion.identity, WoodMaterial);
        mesh.AddBox(new Vector3(doorCenter + doorWidth * 0.5f, jambY, -halfDepth),
            new Vector3(frameThickness, doorHeight, frameThickness), Quaternion.identity, WoodMaterial);
        mesh.AddBox(new Vector3(doorCenter, doorHeight, -halfDepth),
            new Vector3(doorWidth + frameThickness, frameThickness, frameThickness), Quaternion.identity, WoodMaterial);
    }

    private void AddWirePanelZ(
        MeshData mesh,
        float z,
        float xMin,
        float xMax,
        float openingCenter,
        float openingWidth,
        float openingHeight)
    {
        float lowerY = frameThickness;
        float upperY = wallHeight - frameThickness;
        float openingMin = openingCenter - openingWidth * 0.5f;
        float openingMax = openingCenter + openingWidth * 0.5f;

        int verticalCount = Mathf.Max(1, Mathf.CeilToInt((xMax - xMin) / wireSpacing));
        for (int i = 0; i <= verticalCount; i++)
        {
            float x = Mathf.Lerp(xMin, xMax, i / (float)verticalCount);
            bool crossesOpening = x > openingMin && x < openingMax;
            float startY = crossesOpening ? openingHeight : lowerY;
            float height = upperY - startY;
            if (height > 0.01f)
            {
                mesh.AddBox(new Vector3(x, startY + height * 0.5f, z),
                    new Vector3(wireThickness, height, wireThickness), Quaternion.identity, WireMaterial);
            }
        }

        int horizontalCount = Mathf.Max(1, Mathf.CeilToInt((upperY - lowerY) / wireSpacing));
        for (int i = 0; i <= horizontalCount; i++)
        {
            float y = Mathf.Lerp(lowerY, upperY, i / (float)horizontalCount);
            if (y >= openingHeight || openingWidth <= 0f)
            {
                mesh.AddBox(new Vector3((xMin + xMax) * 0.5f, y, z),
                    new Vector3(xMax - xMin, wireThickness, wireThickness), Quaternion.identity, WireMaterial);
                continue;
            }

            AddHorizontalWireSegment(mesh, z, y, xMin, openingMin);
            AddHorizontalWireSegment(mesh, z, y, openingMax, xMax);
        }
    }

    private void AddHorizontalWireSegment(MeshData mesh, float z, float y, float fromX, float toX)
    {
        float length = toX - fromX;
        if (length <= 0.01f)
        {
            return;
        }

        mesh.AddBox(new Vector3((fromX + toX) * 0.5f, y, z),
            new Vector3(length, wireThickness, wireThickness), Quaternion.identity, WireMaterial);
    }

    private void AddWirePanelX(MeshData mesh, float x, float zMin, float zMax)
    {
        float lowerY = frameThickness;
        float upperY = wallHeight - frameThickness;
        int verticalCount = Mathf.Max(1, Mathf.CeilToInt((zMax - zMin) / wireSpacing));
        for (int i = 0; i <= verticalCount; i++)
        {
            float z = Mathf.Lerp(zMin, zMax, i / (float)verticalCount);
            mesh.AddBox(new Vector3(x, (lowerY + upperY) * 0.5f, z),
                new Vector3(wireThickness, upperY - lowerY, wireThickness), Quaternion.identity, WireMaterial);
        }

        int horizontalCount = Mathf.Max(1, Mathf.CeilToInt((upperY - lowerY) / wireSpacing));
        for (int i = 0; i <= horizontalCount; i++)
        {
            float y = Mathf.Lerp(lowerY, upperY, i / (float)horizontalCount);
            mesh.AddBox(new Vector3(x, y, (zMin + zMax) * 0.5f),
                new Vector3(wireThickness, wireThickness, zMax - zMin), Quaternion.identity, WireMaterial);
        }
    }

    private void BuildOpenedDoor(MeshData mesh, float houseWidth, float doorWidth, float doorHeight)
    {
        float halfWidth = width * 0.5f;
        float halfDepth = depth * 0.5f;
        float partitionX = halfWidth - houseWidth;
        float runWidth = partitionX + halfWidth;
        float doorCenter = -halfWidth + runWidth * 0.58f;
        Vector3 hinge = new(doorCenter - doorWidth * 0.5f, 0f, -halfDepth);
        Quaternion rotation = Quaternion.Euler(0f, -openedDoorAngle, 0f);

        AddDoorBar(mesh, hinge, rotation,
            new Vector3(frameThickness * 0.5f, doorHeight * 0.5f, 0f),
            new Vector3(frameThickness, doorHeight, frameThickness));
        AddDoorBar(mesh, hinge, rotation,
            new Vector3(doorWidth - frameThickness * 0.5f, doorHeight * 0.5f, 0f),
            new Vector3(frameThickness, doorHeight, frameThickness));
        AddDoorBar(mesh, hinge, rotation,
            new Vector3(doorWidth * 0.5f, frameThickness * 0.5f, 0f),
            new Vector3(doorWidth, frameThickness, frameThickness));
        AddDoorBar(mesh, hinge, rotation,
            new Vector3(doorWidth * 0.5f, doorHeight - frameThickness * 0.5f, 0f),
            new Vector3(doorWidth, frameThickness, frameThickness));

        int verticalCount = Mathf.Max(2, Mathf.CeilToInt(doorWidth / wireSpacing));
        for (int i = 1; i < verticalCount; i++)
        {
            float localX = doorWidth * i / verticalCount;
            AddDoorWire(mesh, hinge, rotation,
                new Vector3(localX, doorHeight * 0.5f, 0f),
                new Vector3(wireThickness, doorHeight - frameThickness * 2f, wireThickness));
        }

        int horizontalCount = Mathf.Max(2, Mathf.CeilToInt(doorHeight / wireSpacing));
        for (int i = 1; i < horizontalCount; i++)
        {
            float localY = doorHeight * i / horizontalCount;
            AddDoorWire(mesh, hinge, rotation,
                new Vector3(doorWidth * 0.5f, localY, 0f),
                new Vector3(doorWidth - frameThickness * 2f, wireThickness, wireThickness));
        }
    }

    private static void AddDoorBar(MeshData mesh, Vector3 hinge, Quaternion rotation, Vector3 localCenter, Vector3 size)
    {
        mesh.AddBox(hinge + rotation * localCenter, size, rotation, WoodMaterial);
    }

    private static void AddDoorWire(MeshData mesh, Vector3 hinge, Quaternion rotation, Vector3 localCenter, Vector3 size)
    {
        mesh.AddBox(hinge + rotation * localCenter, size, rotation, WireMaterial);
    }

    private void BuildEnclosedHouse(MeshData mesh, float houseWidth)
    {
        float halfWidth = width * 0.5f;
        float halfDepth = depth * 0.5f;
        float partitionX = halfWidth - houseWidth;
        float houseCenterX = (partitionX + halfWidth) * 0.5f;
        float panelBottom = enclosedFloorHeight;
        float panelHeight = wallHeight - panelBottom;
        float panelY = panelBottom + panelHeight * 0.5f;
        float panelThickness = 0.1f;

        mesh.AddBox(new Vector3(houseCenterX, enclosedFloorHeight, 0f),
            new Vector3(houseWidth, 0.14f, depth), Quaternion.identity, WoodMaterial);
        mesh.AddBox(new Vector3(houseCenterX, panelY, halfDepth - panelThickness * 0.5f),
            new Vector3(houseWidth, panelHeight, panelThickness), Quaternion.identity, WoodMaterial);
        mesh.AddBox(new Vector3(halfWidth - panelThickness * 0.5f, panelY, 0f),
            new Vector3(panelThickness, panelHeight, depth), Quaternion.identity, WoodMaterial);
        mesh.AddBox(new Vector3(partitionX + panelThickness * 0.5f, panelY, 0f),
            new Vector3(panelThickness, panelHeight, depth), Quaternion.identity, WoodMaterial);

        float windowWidth = Mathf.Min(0.9f, houseWidth * 0.42f);
        float windowHeight = 0.72f;
        float windowY = enclosedFloorHeight + panelHeight * 0.58f;
        AddFrontWallAroundWindow(mesh, houseCenterX, houseWidth, -halfDepth, panelBottom,
            wallHeight, windowWidth, windowHeight, windowY, panelThickness);

        mesh.AddBox(new Vector3(houseCenterX, windowY, -halfDepth - 0.012f),
            new Vector3(windowWidth, windowHeight, 0.025f), Quaternion.identity, DarkMaterial);
        for (int i = -1; i <= 1; i++)
        {
            mesh.AddBox(new Vector3(houseCenterX + i * windowWidth * 0.32f, windowY, -halfDepth - 0.025f),
                new Vector3(wireThickness * 2f, windowHeight, wireThickness * 2f), Quaternion.identity, WireMaterial);
        }

        AddHouseBattens(mesh, partitionX, halfWidth, halfDepth, panelBottom, wallHeight);
        AddHouseSupports(mesh, partitionX, halfWidth, halfDepth);
        AddNestBox(mesh, houseCenterX, houseWidth, halfDepth);
        AddChickenRamp(mesh, partitionX);
    }

    private void AddFrontWallAroundWindow(
        MeshData mesh,
        float centerX,
        float panelWidth,
        float z,
        float bottom,
        float top,
        float windowWidth,
        float windowHeight,
        float windowY,
        float thickness)
    {
        float left = centerX - panelWidth * 0.5f;
        float right = centerX + panelWidth * 0.5f;
        float windowLeft = centerX - windowWidth * 0.5f;
        float windowRight = centerX + windowWidth * 0.5f;
        float windowBottom = windowY - windowHeight * 0.5f;
        float windowTop = windowY + windowHeight * 0.5f;

        mesh.AddBox(new Vector3((left + windowLeft) * 0.5f, (bottom + top) * 0.5f, z),
            new Vector3(windowLeft - left, top - bottom, thickness), Quaternion.identity, WoodMaterial);
        mesh.AddBox(new Vector3((windowRight + right) * 0.5f, (bottom + top) * 0.5f, z),
            new Vector3(right - windowRight, top - bottom, thickness), Quaternion.identity, WoodMaterial);
        mesh.AddBox(new Vector3(centerX, (bottom + windowBottom) * 0.5f, z),
            new Vector3(windowWidth, windowBottom - bottom, thickness), Quaternion.identity, WoodMaterial);
        mesh.AddBox(new Vector3(centerX, (windowTop + top) * 0.5f, z),
            new Vector3(windowWidth, top - windowTop, thickness), Quaternion.identity, WoodMaterial);

        float trim = 0.08f;
        mesh.AddBox(new Vector3(windowLeft, windowY, z - thickness),
            new Vector3(trim, windowHeight + trim, trim), Quaternion.identity, DarkMaterial);
        mesh.AddBox(new Vector3(windowRight, windowY, z - thickness),
            new Vector3(trim, windowHeight + trim, trim), Quaternion.identity, DarkMaterial);
        mesh.AddBox(new Vector3(centerX, windowBottom, z - thickness),
            new Vector3(windowWidth + trim, trim, trim), Quaternion.identity, DarkMaterial);
        mesh.AddBox(new Vector3(centerX, windowTop, z - thickness),
            new Vector3(windowWidth + trim, trim, trim), Quaternion.identity, DarkMaterial);
    }

    private void AddHouseBattens(MeshData mesh, float partitionX, float halfWidth, float halfDepth, float bottom, float top)
    {
        float height = top - bottom;
        int count = Mathf.Max(2, Mathf.CeilToInt((halfWidth - partitionX) / 0.55f));
        for (int i = 0; i <= count; i++)
        {
            float x = Mathf.Lerp(partitionX, halfWidth, i / (float)count);
            mesh.AddBox(new Vector3(x, bottom + height * 0.5f, -halfDepth - 0.055f),
                new Vector3(0.025f, height, 0.025f), Quaternion.identity, DarkMaterial);
            mesh.AddBox(new Vector3(x, bottom + height * 0.5f, halfDepth + 0.055f),
                new Vector3(0.025f, height, 0.025f), Quaternion.identity, DarkMaterial);
        }
    }

    private void AddHouseSupports(MeshData mesh, float partitionX, float halfWidth, float halfDepth)
    {
        float legY = enclosedFloorHeight * 0.5f;
        float inset = 0.22f;
        float[] xs = { partitionX + inset, halfWidth - inset };
        float[] zs = { -halfDepth + inset, halfDepth - inset };
        for (int x = 0; x < xs.Length; x++)
        {
            for (int z = 0; z < zs.Length; z++)
            {
                mesh.AddBox(new Vector3(xs[x], legY, zs[z]),
                    new Vector3(frameThickness, enclosedFloorHeight, frameThickness), Quaternion.identity, WoodMaterial);
            }
        }
    }

    private void AddNestBox(MeshData mesh, float houseCenterX, float houseWidth, float halfDepth)
    {
        float boxWidth = Mathf.Min(1.8f, houseWidth * 0.72f);
        float boxHeight = 0.62f;
        float boxDepth = 0.72f;
        float centerY = enclosedFloorHeight + 0.42f;
        float centerZ = -halfDepth - boxDepth * 0.5f;
        mesh.AddBox(new Vector3(houseCenterX, centerY, centerZ),
            new Vector3(boxWidth, boxHeight, boxDepth), Quaternion.identity, WoodMaterial);
        mesh.AddBox(new Vector3(houseCenterX, centerY, centerZ - boxDepth * 0.51f),
            new Vector3(boxWidth * 0.78f, boxHeight * 0.48f, 0.025f), Quaternion.identity, DarkMaterial);
        mesh.AddBox(new Vector3(houseCenterX, centerY + boxHeight * 0.57f, centerZ),
            new Vector3(boxWidth + 0.18f, 0.09f, boxDepth + 0.18f),
            Quaternion.Euler(-10f, 0f, 0f), RoofMaterial);
    }

    private void AddChickenRamp(MeshData mesh, float partitionX)
    {
        float run = 1.55f;
        float rise = enclosedFloorHeight - 0.12f;
        float length = Mathf.Sqrt(run * run + rise * rise);
        float angle = Mathf.Atan2(rise, run) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.Euler(0f, 0f, angle);
        Vector3 lowPoint = new(partitionX - run, 0.12f, 0.65f);
        Vector3 highPoint = new(partitionX, enclosedFloorHeight, 0.65f);
        Vector3 center = (lowPoint + highPoint) * 0.5f;
        mesh.AddBox(center, new Vector3(length, 0.08f, 0.72f), rotation, WoodMaterial);

        for (int i = 1; i < 6; i++)
        {
            Vector3 local = new(Mathf.Lerp(-length * 0.42f, length * 0.42f, i / 6f), 0.07f, 0f);
            mesh.AddBox(center + rotation * local,
                new Vector3(0.055f, 0.055f, 0.75f), rotation, DarkMaterial);
        }

        mesh.AddBox(new Vector3(partitionX - 0.055f, enclosedFloorHeight + 0.3f, 0.65f),
            new Vector3(0.025f, 0.52f, 0.62f), Quaternion.identity, DarkMaterial);
    }

    private void BuildRoof(MeshData mesh)
    {
        float halfDepth = depth * 0.5f;
        float horizontalRun = halfDepth + roofOverhang;
        float slopeLength = Mathf.Sqrt(horizontalRun * horizontalRun + roofRise * roofRise);
        float angle = Mathf.Atan2(roofRise, horizontalRun) * Mathf.Rad2Deg;
        float roofWidth = width + roofOverhang * 2f;
        float centerY = wallHeight + roofRise * 0.5f;

        Quaternion frontRotation = Quaternion.Euler(-angle, 0f, 0f);
        Quaternion backRotation = Quaternion.Euler(angle, 0f, 0f);
        Vector3 frontCenter = new(0f, centerY, -horizontalRun * 0.5f);
        Vector3 backCenter = new(0f, centerY, horizontalRun * 0.5f);
        Vector3 panelSize = new(roofWidth, 0.1f, slopeLength);
        mesh.AddBox(frontCenter, panelSize, frontRotation, RoofMaterial);
        mesh.AddBox(backCenter, panelSize, backRotation, RoofMaterial);

        const int ridgeCount = 12;
        for (int i = 0; i <= ridgeCount; i++)
        {
            float localZ = Mathf.Lerp(-slopeLength * 0.47f, slopeLength * 0.47f, i / (float)ridgeCount);
            Vector3 frontRidge = frontCenter + frontRotation * new Vector3(0f, 0.065f, localZ);
            Vector3 backRidge = backCenter + backRotation * new Vector3(0f, 0.065f, localZ);
            Vector3 ridgeSize = new(roofWidth, 0.035f, 0.035f);
            mesh.AddBox(frontRidge, ridgeSize, frontRotation, RoofMaterial);
            mesh.AddBox(backRidge, ridgeSize, backRotation, RoofMaterial);
        }

        mesh.AddBox(new Vector3(0f, wallHeight + roofRise + 0.04f, 0f),
            new Vector3(roofWidth + 0.08f, 0.12f, 0.18f), Quaternion.identity, RoofMaterial);
    }

    private void BuildInteriorDetails(MeshData mesh, float houseWidth)
    {
        float halfWidth = width * 0.5f;
        float partitionX = halfWidth - houseWidth;
        float runCenterX = (-halfWidth + partitionX) * 0.5f;

        mesh.AddBox(new Vector3(runCenterX, 0.86f, 1.45f),
            new Vector3(partitionX + halfWidth - 1.1f, 0.09f, 0.09f), Quaternion.identity, WoodMaterial);
        mesh.AddBox(new Vector3(runCenterX, 0.43f, 1.45f),
            new Vector3(0.1f, 0.86f, 0.1f), Quaternion.identity, WoodMaterial);

        AddFeeder(mesh, new Vector3(runCenterX - 1.1f, 0f, -0.55f));
        AddFeeder(mesh, new Vector3(runCenterX + 0.35f, 0f, 0.2f));
    }

    private static void AddFeeder(MeshData mesh, Vector3 position)
    {
        mesh.AddBox(position + new Vector3(0f, 0.1f, 0f),
            new Vector3(0.72f, 0.2f, 0.72f), Quaternion.identity, RoofMaterial);
        mesh.AddBox(position + new Vector3(0f, 0.48f, 0f),
            new Vector3(0.42f, 0.65f, 0.42f), Quaternion.identity, WireMaterial);
        mesh.AddBox(position + new Vector3(0f, 0.83f, 0f),
            new Vector3(0.52f, 0.08f, 0.52f), Quaternion.identity, RoofMaterial);
    }

    private void BuildCollision(MeshData mesh, float houseWidth, float doorWidth)
    {
        float halfWidth = width * 0.5f;
        float halfDepth = depth * 0.5f;
        float partitionX = halfWidth - houseWidth;
        float runWidth = partitionX + halfWidth;
        float doorCenter = -halfWidth + runWidth * 0.58f;
        float doorLeft = doorCenter - doorWidth * 0.5f;
        float doorRight = doorCenter + doorWidth * 0.5f;
        float thickness = Mathf.Max(0.1f, frameThickness);
        float centerY = wallHeight * 0.5f;

        mesh.AddBox(new Vector3(0f, centerY, halfDepth),
            new Vector3(width, wallHeight, thickness), Quaternion.identity, 0);
        mesh.AddBox(new Vector3(-halfWidth, centerY, 0f),
            new Vector3(thickness, wallHeight, depth), Quaternion.identity, 0);
        mesh.AddBox(new Vector3(halfWidth, centerY, 0f),
            new Vector3(thickness, wallHeight, depth), Quaternion.identity, 0);

        float leftLength = doorLeft + halfWidth;
        if (leftLength > 0.01f)
        {
            mesh.AddBox(new Vector3(-halfWidth + leftLength * 0.5f, centerY, -halfDepth),
                new Vector3(leftLength, wallHeight, thickness), Quaternion.identity, 0);
        }

        float rightLength = halfWidth - doorRight;
        if (rightLength > 0.01f)
        {
            mesh.AddBox(new Vector3(doorRight + rightLength * 0.5f, centerY, -halfDepth),
                new Vector3(rightLength, wallHeight, thickness), Quaternion.identity, 0);
        }

        mesh.AddBox(new Vector3((partitionX + halfWidth) * 0.5f, centerY, 0f),
            new Vector3(houseWidth, wallHeight, depth), Quaternion.identity, 0);
    }

    private static Mesh ApplyMesh(Mesh target, MeshData data, string meshName)
    {
        if (target == null)
        {
            target = new Mesh { hideFlags = HideFlags.DontSave };
        }
        else
        {
            target.Clear();
        }

        target.name = meshName;
        target.indexFormat = data.Vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
        target.SetVertices(data.Vertices);
        target.SetNormals(data.Normals);
        target.SetUVs(0, data.Uvs);
        target.subMeshCount = data.Triangles.Length;
        for (int i = 0; i < data.Triangles.Length; i++)
        {
            target.SetTriangles(data.Triangles[i], i, false);
        }

        target.RecalculateBounds();
        return target;
    }

    private static void DestroyMesh(Mesh mesh)
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
        public readonly List<int>[] Triangles;

        public MeshData(int materialCount)
        {
            Triangles = new List<int>[materialCount];
            for (int i = 0; i < materialCount; i++)
            {
                Triangles[i] = new List<int>();
            }
        }

        public void AddBox(Vector3 center, Vector3 size, Quaternion rotation, int material)
        {
            Vector3 half = size * 0.5f;
            AddFace(center, rotation,
                new Vector3(-half.x, -half.y, half.z), new Vector3(half.x, -half.y, half.z),
                new Vector3(half.x, half.y, half.z), new Vector3(-half.x, half.y, half.z),
                Vector3.forward, material);
            AddFace(center, rotation,
                new Vector3(half.x, -half.y, -half.z), new Vector3(-half.x, -half.y, -half.z),
                new Vector3(-half.x, half.y, -half.z), new Vector3(half.x, half.y, -half.z),
                Vector3.back, material);
            AddFace(center, rotation,
                new Vector3(-half.x, -half.y, -half.z), new Vector3(-half.x, -half.y, half.z),
                new Vector3(-half.x, half.y, half.z), new Vector3(-half.x, half.y, -half.z),
                Vector3.left, material);
            AddFace(center, rotation,
                new Vector3(half.x, -half.y, half.z), new Vector3(half.x, -half.y, -half.z),
                new Vector3(half.x, half.y, -half.z), new Vector3(half.x, half.y, half.z),
                Vector3.right, material);
            AddFace(center, rotation,
                new Vector3(-half.x, half.y, half.z), new Vector3(half.x, half.y, half.z),
                new Vector3(half.x, half.y, -half.z), new Vector3(-half.x, half.y, -half.z),
                Vector3.up, material);
            AddFace(center, rotation,
                new Vector3(-half.x, -half.y, -half.z), new Vector3(half.x, -half.y, -half.z),
                new Vector3(half.x, -half.y, half.z), new Vector3(-half.x, -half.y, half.z),
                Vector3.down, material);
        }

        private void AddFace(
            Vector3 center,
            Quaternion rotation,
            Vector3 a,
            Vector3 b,
            Vector3 c,
            Vector3 d,
            Vector3 localNormal,
            int material)
        {
            int start = Vertices.Count;
            Vertices.Add(center + rotation * a);
            Vertices.Add(center + rotation * b);
            Vertices.Add(center + rotation * c);
            Vertices.Add(center + rotation * d);

            Vector3 normal = rotation * localNormal;
            Normals.Add(normal);
            Normals.Add(normal);
            Normals.Add(normal);
            Normals.Add(normal);
            Uvs.Add(new Vector2(0f, 0f));
            Uvs.Add(new Vector2(1f, 0f));
            Uvs.Add(new Vector2(1f, 1f));
            Uvs.Add(new Vector2(0f, 1f));

            List<int> triangles = Triangles[Mathf.Clamp(material, 0, Triangles.Length - 1)];
            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
            triangles.Add(start);
            triangles.Add(start + 2);
            triangles.Add(start + 3);
        }
    }
}
