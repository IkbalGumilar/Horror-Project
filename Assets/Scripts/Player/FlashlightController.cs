using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class FlashlightController : MonoBehaviour
{
    [Header("Follow")]
    [SerializeField] private FPSCameraController cameraController;
    [SerializeField] private Transform pitchFollowTarget;
    [SerializeField] private bool parentFlashlightToPitchTargetOnAwake = true;
    [SerializeField] private bool keepWorldTransformWhenParenting = true;

    [Header("Input")]
    [SerializeField] private Key toggleKey = Key.F;
    [SerializeField] private float scrollSensitivity = 1f;

    [Header("Light")]
    [SerializeField] private Light flashlight;
    [SerializeField] private bool isOn = true;
    [SerializeField] private float narrowSpotAngle = 10f;
    [SerializeField] private float wideSpotAngle = 45f;
    [SerializeField] private float narrowIntensity = 2f;
    [SerializeField] private float wideIntensity = 0.5f;

    [Header("UI")]
    [SerializeField] private Image flashlightStatusImage;
    [SerializeField] private HUDFadeAfterShow flashlightStatusFade;
    [SerializeField] private Sprite[] statusIcons = new Sprite[2];
    [SerializeField] private int onIconIndex;
    [SerializeField] private int offIconIndex = 1;

    private float focus01;

    private void Reset()
    {
        flashlight = GetComponentInChildren<Light>();
    }

    private void Awake()
    {
        ResolveFollowTarget();
        if (flashlight == null)
        {
            flashlight = GetComponentInChildren<Light>();
        }

        ParentFlashlightToPitchTarget();
        focus01 = GetFocusFromCurrentLight();
        ApplyLightState();
        RefreshStatusIcon();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard[toggleKey].wasPressedThisFrame)
        {
            Toggle();
        }

        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            return;
        }

        float scrollY = mouse.scroll.ReadValue().y;
        if (Mathf.Approximately(scrollY, 0f))
        {
            return;
        }

        focus01 = Mathf.Clamp01(focus01 + Mathf.Sign(scrollY) * scrollSensitivity);
        ApplyLightShape();
    }

    public void Toggle()
    {
        isOn = !isOn;
        ApplyLightState();
        RefreshStatusIcon();
        flashlightStatusFade?.Show();
    }

    private void ResolveFollowTarget()
    {
        if (cameraController == null)
        {
            cameraController = GetComponent<FPSCameraController>();
        }

        if (cameraController == null)
        {
            cameraController = GetComponentInChildren<FPSCameraController>();
        }

        if (pitchFollowTarget == null && cameraController != null)
        {
            pitchFollowTarget = cameraController.CameraPivot;
        }
    }

    private void ParentFlashlightToPitchTarget()
    {
        if (!parentFlashlightToPitchTargetOnAwake || flashlight == null || pitchFollowTarget == null)
        {
            return;
        }

        Transform flashlightTransform = flashlight.transform;
        if (flashlightTransform.parent == pitchFollowTarget)
        {
            return;
        }

        flashlightTransform.SetParent(pitchFollowTarget, keepWorldTransformWhenParenting);
    }

    private float GetFocusFromCurrentLight()
    {
        if (flashlight == null || Mathf.Approximately(wideSpotAngle, narrowSpotAngle))
        {
            return 1f;
        }

        return Mathf.InverseLerp(wideSpotAngle, narrowSpotAngle, flashlight.spotAngle);
    }

    private void ApplyLightState()
    {
        if (flashlight != null)
        {
            flashlight.enabled = isOn;
            ApplyLightShape();
        }
    }

    private void ApplyLightShape()
    {
        if (flashlight == null)
        {
            return;
        }

        flashlight.spotAngle = Mathf.Lerp(wideSpotAngle, narrowSpotAngle, focus01);
        flashlight.intensity = Mathf.Lerp(wideIntensity, narrowIntensity, focus01);
    }

    private void RefreshStatusIcon()
    {
        if (flashlightStatusImage == null || statusIcons == null || statusIcons.Length == 0)
        {
            return;
        }

        if (flashlightStatusFade == null)
        {
            flashlightStatusFade = flashlightStatusImage.GetComponent<HUDFadeAfterShow>();
        }

        int iconIndex = isOn ? onIconIndex : offIconIndex;
        if (iconIndex < 0 || iconIndex >= statusIcons.Length || statusIcons[iconIndex] == null)
        {
            return;
        }

        flashlightStatusImage.sprite = statusIcons[iconIndex];
    }
}
