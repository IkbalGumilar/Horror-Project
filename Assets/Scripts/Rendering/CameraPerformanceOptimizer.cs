using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public sealed class CameraPerformanceOptimizer : MonoBehaviour
{
    [Header("Camera Culling")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private bool applyCameraCulling = true;
    [SerializeField] private float farClipPlane = 325f;
    [SerializeField] private bool useOcclusionCulling = true;
    [SerializeField] private bool useLayerCullDistance = false;
    [SerializeField] private float layerCullDistance = 325f;
    [SerializeField] private bool sphericalLayerCulling = true;

    [Header("Texture Streaming")]
    [SerializeField] private bool applyTextureStreaming = true;
    [SerializeField] private bool applyGlobalTextureLimit = true;
    [SerializeField, Range(0, 3)] private int globalTextureMipmapLimit = 2;
    [SerializeField] private float streamingMipmapsMemoryBudget = 256f;
    [SerializeField] private int streamingMipmapsRenderersPerFrame = 128;

    [Header("Terrain")]
    [SerializeField] private bool applyTerrainSettings = true;
    [SerializeField] private Terrain[] terrains;
    [SerializeField] private float treeDistance = 80f;
    [SerializeField] private float treeBillboardDistance = 20f;
    [SerializeField] private int treeMaximumFullLODCount = 20;
    [SerializeField] private float detailObjectDistance = 22.5f;
    [SerializeField] private float terrainPixelError = 12f;
    [SerializeField] private float terrainBaseMapDistance = 60f;
    [SerializeField] private bool drawTerrainInstanced = true;

    private void Reset()
    {
        targetCamera = GetComponent<Camera>();
        terrains = FindObjectsByType<Terrain>(FindObjectsSortMode.None);
    }

    private void OnEnable()
    {
        Apply();
    }

    private void Awake()
    {
        Apply();
    }

    [ContextMenu("Apply Performance Settings")]
    public void Apply()
    {
        if (targetCamera == null)
        {
            targetCamera = GetComponent<Camera>();
        }

        if (applyCameraCulling)
        {
            ApplyCameraSettings();
        }

        if (applyTextureStreaming)
        {
            ApplyTextureStreamingSettings();
        }

        if (applyTerrainSettings)
        {
            ApplyTerrainSettings();
        }
    }

    private void ApplyCameraSettings()
    {
        if (targetCamera == null)
        {
            return;
        }

        targetCamera.useOcclusionCulling = useOcclusionCulling;
        targetCamera.farClipPlane = Mathf.Max(targetCamera.nearClipPlane + 1f, farClipPlane);
        targetCamera.layerCullSpherical = sphericalLayerCulling;

        float[] distances = targetCamera.layerCullDistances;
        if (distances == null || distances.Length != 32)
        {
            distances = new float[32];
        }

        float distance = useLayerCullDistance ? Mathf.Max(0f, layerCullDistance) : 0f;
        for (int i = 0; i < distances.Length; i++)
        {
            distances[i] = distance;
        }

        targetCamera.layerCullDistances = distances;
    }

    private void ApplyTextureStreamingSettings()
    {
        if (applyGlobalTextureLimit)
        {
            QualitySettings.globalTextureMipmapLimit = globalTextureMipmapLimit;
        }

        QualitySettings.streamingMipmapsActive = true;
        QualitySettings.streamingMipmapsAddAllCameras = true;
        QualitySettings.streamingMipmapsMemoryBudget = Mathf.Max(16f, streamingMipmapsMemoryBudget);
        QualitySettings.streamingMipmapsRenderersPerFrame = Mathf.Max(1, streamingMipmapsRenderersPerFrame);
    }

    private void ApplyTerrainSettings()
    {
        Terrain[] targetTerrains = GetTerrains();
        for (int i = 0; i < targetTerrains.Length; i++)
        {
            Terrain terrain = targetTerrains[i];
            if (terrain == null)
            {
                continue;
            }

            terrain.treeDistance = Mathf.Max(0f, treeDistance);
            terrain.treeBillboardDistance = Mathf.Max(0f, treeBillboardDistance);
            terrain.treeMaximumFullLODCount = Mathf.Max(0, treeMaximumFullLODCount);
            terrain.detailObjectDistance = Mathf.Max(0f, detailObjectDistance);
            terrain.heightmapPixelError = Mathf.Max(1f, terrainPixelError);
            terrain.basemapDistance = Mathf.Max(0f, terrainBaseMapDistance);
            terrain.drawInstanced = drawTerrainInstanced;
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
}
