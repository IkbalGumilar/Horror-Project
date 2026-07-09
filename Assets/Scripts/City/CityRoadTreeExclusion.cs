using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class CityRoadTreeExclusion : MonoBehaviour
{
    [SerializeField] private CityProceduralRoad road;
    [SerializeField] private Terrain[] terrains;
    [SerializeField, Min(0f)] private float extraClearance = 8f;
    [SerializeField] private bool clearOnPlay = true;

    private void Reset()
    {
        road = GetComponent<CityProceduralRoad>();
        terrains = FindObjectsByType<Terrain>(FindObjectsSortMode.None);
    }

    private void Awake()
    {
        if (Application.isPlaying && clearOnPlay)
        {
            ClearTreesOnRoad();
        }
    }

    [ContextMenu("Clear Trees On Road")]
    public void ClearTreesOnRoad()
    {
        CityProceduralRoad targetRoad = road != null ? road : GetComponent<CityProceduralRoad>();
        if (targetRoad == null)
        {
            Debug.LogWarning($"{nameof(CityRoadTreeExclusion)} needs a CityProceduralRoad reference.", this);
            return;
        }

        Terrain[] targetTerrains = GetTerrains();
        for (int i = 0; i < targetTerrains.Length; i++)
        {
            ClearTerrainTrees(targetTerrains[i], targetRoad);
        }
    }

    private Terrain[] GetTerrains()
    {
        if (terrains != null && terrains.Length > 0)
        {
            return terrains;
        }

        terrains = FindObjectsByType<Terrain>(FindObjectsSortMode.None);
        return terrains;
    }

    private void ClearTerrainTrees(Terrain terrain, CityProceduralRoad targetRoad)
    {
        if (terrain == null || terrain.terrainData == null)
        {
            return;
        }

        TerrainData terrainData = terrain.terrainData;
        TreeInstance[] existingTrees = terrainData.treeInstances;
        List<TreeInstance> keptTrees = new List<TreeInstance>(existingTrees.Length);
        Vector3 terrainPosition = terrain.transform.position;
        Vector3 terrainSize = terrainData.size;

        for (int i = 0; i < existingTrees.Length; i++)
        {
            TreeInstance tree = existingTrees[i];
            Vector3 worldPosition = terrainPosition + Vector3.Scale(tree.position, terrainSize);
            if (!targetRoad.IsPointInsideRoad(worldPosition, extraClearance))
            {
                keptTrees.Add(tree);
            }
        }

        if (keptTrees.Count == existingTrees.Length)
        {
            return;
        }

        terrainData.treeInstances = keptTrees.ToArray();
        terrain.Flush();
    }
}
