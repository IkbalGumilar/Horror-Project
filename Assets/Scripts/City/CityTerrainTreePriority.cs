using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Terrain))]
public sealed class CityTerrainTreePriority : MonoBehaviour
{
    [SerializeField] private Terrain targetTerrain;
    [SerializeField] private bool rebalanceOnPlay = true;
    [SerializeField] private int randomSeed = 1207;

    [Header("Weights")]
    [SerializeField, Min(0f)] private float smallWeight = 70f;
    [SerializeField, Min(0f)] private float mediumWeight = 20f;
    [SerializeField, Min(0f)] private float tallWeight = 8f;
    [SerializeField, Min(0f)] private float bareWeight = 2f;
    [SerializeField, Min(0f)] private float fallbackWeight = 1f;

    private void Reset()
    {
        targetTerrain = GetComponent<Terrain>();
    }

    private void Awake()
    {
        if (Application.isPlaying && rebalanceOnPlay)
        {
            RebalanceTrees();
        }
    }

    [ContextMenu("Rebalance Tree Priority")]
    public void RebalanceTrees()
    {
        Terrain terrain = targetTerrain != null ? targetTerrain : GetComponent<Terrain>();
        if (terrain == null || terrain.terrainData == null)
        {
            return;
        }

        TerrainData terrainData = terrain.terrainData;
        TreePrototype[] prototypes = terrainData.treePrototypes;
        TreeInstance[] trees = terrainData.treeInstances;
        if (prototypes == null || prototypes.Length == 0 || trees == null || trees.Length == 0)
        {
            return;
        }

        float[] weights = BuildWeights(prototypes);
        float totalWeight = 0f;
        for (int i = 0; i < weights.Length; i++)
        {
            totalWeight += weights[i];
        }

        if (totalWeight <= 0f)
        {
            return;
        }

        System.Random random = new System.Random(randomSeed);
        for (int i = 0; i < trees.Length; i++)
        {
            TreeInstance tree = trees[i];
            tree.prototypeIndex = PickPrototypeIndex(weights, totalWeight, random);
            trees[i] = tree;
        }

        terrainData.treeInstances = trees;
        terrain.Flush();
    }

    private float[] BuildWeights(TreePrototype[] prototypes)
    {
        float[] weights = new float[prototypes.Length];
        for (int i = 0; i < prototypes.Length; i++)
        {
            string prefabName = prototypes[i].prefab != null ? prototypes[i].prefab.name : string.Empty;
            weights[i] = GetWeight(prefabName);
        }

        return weights;
    }

    private float GetWeight(string prefabName)
    {
        if (prefabName.IndexOf("Small", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return smallWeight;
        }

        if (prefabName.IndexOf("Medium", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return mediumWeight;
        }

        if (prefabName.IndexOf("Tall", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return tallWeight;
        }

        if (prefabName.IndexOf("Bare", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return bareWeight;
        }

        return fallbackWeight;
    }

    private static int PickPrototypeIndex(float[] weights, float totalWeight, System.Random random)
    {
        double roll = random.NextDouble() * totalWeight;
        float current = 0f;

        for (int i = 0; i < weights.Length; i++)
        {
            current += weights[i];
            if (roll <= current)
            {
                return i;
            }
        }

        return weights.Length - 1;
    }
}
