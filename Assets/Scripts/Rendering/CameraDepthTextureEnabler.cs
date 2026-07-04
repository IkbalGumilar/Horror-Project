using UnityEngine;

[RequireComponent(typeof(Camera))]
public sealed class CameraDepthTextureEnabler : MonoBehaviour
{
    [SerializeField] private DepthTextureMode depthTextureMode = DepthTextureMode.Depth;

    private Camera targetCamera;

    private void OnEnable()
    {
        targetCamera = GetComponent<Camera>();
        targetCamera.depthTextureMode |= depthTextureMode;
    }
}
