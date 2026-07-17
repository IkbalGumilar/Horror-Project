using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public sealed class PlayerCharacterVisual : MonoBehaviour
{
    [Header("Model")]
    [SerializeField] private GameObject modelPrefab;

    [Header("Locomotion")]
    [SerializeField] private RuntimeAnimatorController locomotionController;
    [SerializeField] private CharacterLocomotionSettings locomotionSettings =
        new CharacterLocomotionSettings();

    [Header("Placement")]
    [SerializeField] private Vector3 localPosition;
    [SerializeField] private Vector3 localEulerAngles;
    [SerializeField] private Vector3 localScale = Vector3.one;
    [SerializeField] private bool alignFeetToCharacterController = true;
    [SerializeField] private bool hideLegacyRenderer = true;

    [Header("First Person")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private bool hideFromAttachedCamera;
    [SerializeField] private bool keepFirstPersonShadow = true;

    [Header("View Alignment")]
    [SerializeField] private bool alignCameraToViewAnchor = true;
    [SerializeField] private string viewAnchorName = "ViewAnchor";
    [SerializeField] private float cameraHeightOffset;

    private GameObject modelInstance;
    private Renderer[] modelRenderers;
    private bool[] rendererEnabledStates;
    private ShadowCastingMode[] shadowCastingModes;
    private bool? lastFirstPersonState;

    public GameObject ModelInstance => modelInstance;
    public Animator ModelAnimator { get; private set; }
    public Transform ViewAnchor { get; private set; }
    public float StandingViewHeight { get; private set; }

    private void Awake()
    {
        CreateModel();
        ResolveCamera();
        AlignCameraToViewAnchor();
        UpdateVisibility(true);
    }

    private void LateUpdate()
    {
        ResolveCamera();
        UpdateVisibility(false);
    }

    private void CreateModel()
    {
        if (modelPrefab == null || modelInstance != null)
        {
            return;
        }

        modelInstance = Instantiate(modelPrefab, transform);
        modelInstance.name = $"{modelPrefab.name} Visual";

        Vector3 offset = localPosition;
        if (alignFeetToCharacterController)
        {
            CharacterController controller = GetComponent<CharacterController>();
            if (controller != null)
            {
                FPSPlayerController playerController = GetComponent<FPSPlayerController>();
                float standingHeight = playerController != null
                    ? playerController.ConfiguredStandingHeight
                    : controller.height;
                offset.y += controller.center.y - standingHeight * 0.5f;
            }
            else if (TryGetComponent(out CapsuleCollider capsule))
            {
                offset.y += capsule.center.y - capsule.height * 0.5f;
            }
        }

        modelInstance.transform.SetLocalPositionAndRotation(
            offset,
            Quaternion.Euler(localEulerAngles));
        modelInstance.transform.localScale = localScale;
        ConfigureAnimation();

        modelRenderers = modelInstance.GetComponentsInChildren<Renderer>(true);
        rendererEnabledStates = new bool[modelRenderers.Length];
        shadowCastingModes = new ShadowCastingMode[modelRenderers.Length];
        for (int i = 0; i < modelRenderers.Length; i++)
        {
            rendererEnabledStates[i] = modelRenderers[i].enabled;
            shadowCastingModes[i] = modelRenderers[i].shadowCastingMode;
        }

        if (hideLegacyRenderer)
        {
            Renderer[] legacyRenderers = GetComponents<Renderer>();
            for (int i = 0; i < legacyRenderers.Length; i++)
            {
                legacyRenderers[i].enabled = false;
            }
        }
    }

    private void AlignCameraToViewAnchor()
    {
        if (!alignCameraToViewAnchor || modelInstance == null)
        {
            return;
        }

        ViewAnchor = FindModelTransform(viewAnchorName);
        if (ViewAnchor == null)
        {
            Debug.LogWarning(
                $"{nameof(PlayerCharacterVisual)} could not find view anchor " +
                $"'{viewAnchorName}' on {modelInstance.name}.",
                this);
            return;
        }

        StandingViewHeight = transform.InverseTransformPoint(ViewAnchor.position).y
            + cameraHeightOffset;

        FPSPlayerController playerController = GetComponent<FPSPlayerController>();
        if (playerController != null)
        {
            playerController.SetModelCameraHeight(StandingViewHeight, true);
            return;
        }

        FPSCameraController cameraController = GetComponent<FPSCameraController>();
        cameraController?.SetModelStandingHeight(StandingViewHeight, true);
    }

    private Transform FindModelTransform(string transformName)
    {
        if (string.IsNullOrWhiteSpace(transformName))
        {
            return null;
        }

        Transform[] transforms = modelInstance.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i].name == transformName)
            {
                return transforms[i];
            }
        }

        return null;
    }

    private void ConfigureAnimation()
    {
        ModelAnimator = modelInstance.GetComponentInChildren<Animator>(true);
        if (ModelAnimator == null || locomotionController == null)
        {
            return;
        }

        ModelAnimator.runtimeAnimatorController = locomotionController;
        ModelAnimator.applyRootMotion = false;

        CharacterLocomotionAnimator locomotion = GetComponent<CharacterLocomotionAnimator>();
        if (locomotion == null)
        {
            locomotion = gameObject.AddComponent<CharacterLocomotionAnimator>();
        }

        locomotion.Initialize(
            ModelAnimator,
            transform,
            GetComponent<FPSPlayerController>(),
            locomotionSettings: locomotionSettings,
            targetMaximumBlendValue: 1f,
            targetCullingMode: AnimatorCullingMode.AlwaysAnimate);
    }

    private void ResolveCamera()
    {
        if (playerCamera != null)
        {
            return;
        }

        FPSCameraController cameraController = GetComponent<FPSCameraController>();
        if (cameraController != null)
        {
            playerCamera = cameraController.PlayerCamera;
        }

        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>(true);
        }
    }

    private void UpdateVisibility(bool force)
    {
        if (modelRenderers == null || !hideFromAttachedCamera)
        {
            return;
        }

        bool isFirstPerson = playerCamera != null && playerCamera.transform.IsChildOf(transform);
        if (!force && lastFirstPersonState == isFirstPerson)
        {
            return;
        }

        lastFirstPersonState = isFirstPerson;
        for (int i = 0; i < modelRenderers.Length; i++)
        {
            Renderer modelRenderer = modelRenderers[i];
            if (modelRenderer == null)
            {
                continue;
            }

            if (!isFirstPerson)
            {
                modelRenderer.enabled = rendererEnabledStates[i];
                modelRenderer.shadowCastingMode = shadowCastingModes[i];
                continue;
            }

            if (keepFirstPersonShadow && rendererEnabledStates[i])
            {
                modelRenderer.enabled = true;
                modelRenderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            }
            else
            {
                modelRenderer.enabled = false;
            }
        }
    }
}
