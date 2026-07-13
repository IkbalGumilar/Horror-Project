using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class VillageVillagerSpawnEntry
{
    [SerializeField] private Vector3 localPosition;
    [SerializeField] private VillagerData data;

    public Vector3 LocalPosition => localPosition;
    public VillagerData Data => data;
}

[DisallowMultipleComponent]
public sealed class VillagePopulationSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject villagerTemplate;
    [SerializeField] private Transform populationParent;

    [Header("Placement")]
    [SerializeField] private bool placeOnTerrain = true;
    [SerializeField] private float groundOffset = 1f;
    [SerializeField] private bool randomizeYaw = true;
    [SerializeField] private int randomSeed = 173;
    [SerializeField] private List<VillageVillagerSpawnEntry> villagers = new List<VillageVillagerSpawnEntry>();

    private readonly List<GameObject> spawnedVillagers = new List<GameObject>();

    private void Awake()
    {
        SpawnPopulation();
    }

    public void SpawnPopulation()
    {
        ClearPopulation();

        if (villagerTemplate == null)
        {
            Debug.LogWarning($"{nameof(VillagePopulationSpawner)} needs a villager template.", this);
            return;
        }

        Transform parent = populationParent != null ? populationParent : transform;
        bool templateWasActive = villagerTemplate.activeSelf;
        villagerTemplate.SetActive(false);
        System.Random random = new System.Random(randomSeed);

        for (int i = 0; i < villagers.Count; i++)
        {
            VillageVillagerSpawnEntry entry = villagers[i];
            if (entry == null)
            {
                continue;
            }

            GameObject villager = Instantiate(villagerTemplate, parent);
            villager.name = $"Villager {i + 1:00}";

            Vector3 worldPosition = transform.TransformPoint(entry.LocalPosition);
            if (placeOnTerrain && TryGetTerrainHeight(worldPosition, out float terrainHeight))
            {
                worldPosition.y = terrainHeight + groundOffset;
            }

            villager.transform.SetPositionAndRotation(
                worldPosition,
                randomizeYaw
                    ? Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f)
                    : transform.rotation);

            VillagerConversation conversation = villager.GetComponent<VillagerConversation>();
            if (conversation == null)
            {
                conversation = villager.AddComponent<VillagerConversation>();
            }

            conversation.Initialize(entry.Data);
            villager.SetActive(templateWasActive);
            spawnedVillagers.Add(villager);
        }
    }

    public void ClearPopulation()
    {
        for (int i = spawnedVillagers.Count - 1; i >= 0; i--)
        {
            if (spawnedVillagers[i] != null)
            {
                Destroy(spawnedVillagers[i]);
            }
        }

        spawnedVillagers.Clear();
    }

    private static bool TryGetTerrainHeight(Vector3 worldPosition, out float height)
    {
        Terrain[] terrains = Terrain.activeTerrains;
        for (int i = 0; i < terrains.Length; i++)
        {
            Terrain terrain = terrains[i];
            Vector3 origin = terrain.transform.position;
            Vector3 size = terrain.terrainData.size;
            if (worldPosition.x < origin.x || worldPosition.x > origin.x + size.x
                || worldPosition.z < origin.z || worldPosition.z > origin.z + size.z)
            {
                continue;
            }

            height = terrain.SampleHeight(worldPosition) + origin.y;
            return true;
        }

        height = worldPosition.y;
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.25f, 0.8f, 0.45f, 0.8f);
        for (int i = 0; i < villagers.Count; i++)
        {
            VillageVillagerSpawnEntry entry = villagers[i];
            if (entry == null)
            {
                continue;
            }

            Vector3 position = transform.TransformPoint(entry.LocalPosition);
            if (placeOnTerrain && TryGetTerrainHeight(position, out float terrainHeight))
            {
                position.y = terrainHeight + groundOffset;
            }

            Gizmos.DrawWireSphere(position, 0.5f);
            Gizmos.DrawLine(position - Vector3.up, position + Vector3.up);
        }
    }
}
