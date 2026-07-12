using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class AccessibilitySettingsController : MonoBehaviour
{
    private TMP_Dropdown subtitleDropdown;
    private TMP_Dropdown subtitleSizeDropdown;
    private TMP_Dropdown subtitleBackgroundDropdown;
    private TMP_Dropdown speakerNameDropdown;
    private TMP_Dropdown cameraShakeDropdown;
    private TMP_Dropdown blinkDropdown;
    private Slider contrastSlider;
    private Button defaultButton;
    private Button applyButton;
    private ColorGrading colorGrading;
    private Values initialValues;
    private Values appliedValues;
    private bool initialized;
    private bool suppressCallbacks;

    private void Awake()
    {
        ResolveControls();
        if (!HasRequiredControls())
        {
            Debug.LogWarning("AccessibilitySettingsController could not find every required control in Accessibility Menu.", this);
            enabled = false;
            return;
        }

        ResolveColorGrading();
        ConfigureControls();
        initialValues = ReadCurrentValues();
        appliedValues = initialValues;
        WriteControls(appliedValues);
        ApplyValues(appliedValues, false);
        RegisterListeners();
        initialized = true;
        RefreshState();
    }

    private void OnEnable()
    {
        LocalizationManager.LanguageChanged += RefreshOptions;
        if (initialized)
        {
            RefreshOptions();
            WriteControls(ReadControls());
            RefreshState();
        }
    }

    private void OnDisable()
    {
        LocalizationManager.LanguageChanged -= RefreshOptions;
    }

    private void OnDestroy()
    {
        if (!initialized) return;
        subtitleDropdown.onValueChanged.RemoveListener(OnControlChanged);
        subtitleSizeDropdown.onValueChanged.RemoveListener(OnControlChanged);
        subtitleBackgroundDropdown.onValueChanged.RemoveListener(OnControlChanged);
        speakerNameDropdown.onValueChanged.RemoveListener(OnControlChanged);
        cameraShakeDropdown.onValueChanged.RemoveListener(OnControlChanged);
        blinkDropdown.onValueChanged.RemoveListener(OnControlChanged);
        contrastSlider.onValueChanged.RemoveListener(OnSliderChanged);
        defaultButton.onClick.RemoveListener(RestoreDefaults);
        applyButton.onClick.RemoveListener(ApplyPending);
    }

    private void ResolveControls()
    {
        subtitleDropdown = FindControl<TMP_Dropdown>("Subtitle");
        subtitleSizeDropdown = FindControl<TMP_Dropdown>("Subtitle Size");
        subtitleBackgroundDropdown = FindControl<TMP_Dropdown>("Subtitle Background");
        speakerNameDropdown = FindControl<TMP_Dropdown>("Name Subtitle");
        cameraShakeDropdown = FindControl<TMP_Dropdown>("Camera Shake");
        blinkDropdown = FindControl<TMP_Dropdown>("Blink blink", "Dynamic Range");
        contrastSlider = FindControl<Slider>("Contrast", "Dynamic Range (1)");
        defaultButton = FindDirectChild("Default")?.GetComponent<Button>();
        applyButton = FindDirectChild("Apply")?.GetComponent<Button>();
    }

    private bool HasRequiredControls()
    {
        return subtitleDropdown != null && subtitleSizeDropdown != null
            && subtitleBackgroundDropdown != null && speakerNameDropdown != null
            && cameraShakeDropdown != null && blinkDropdown != null
            && contrastSlider != null && defaultButton != null && applyButton != null;
    }

    private T FindControl<T>(params string[] groupNames) where T : Component
    {
        for (int i = 0; i < groupNames.Length; i++)
        {
            Transform group = FindDirectChild(groupNames[i]);
            T result = group != null ? group.GetComponentInChildren<T>(true) : null;
            if (result != null) return result;
        }
        return null;
    }

    private Transform FindDirectChild(string childName)
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.name == childName) return child;
        }
        return null;
    }

    private void ConfigureControls()
    {
        contrastSlider.minValue = 0f;
        contrastSlider.maxValue = 100f;
        contrastSlider.wholeNumbers = false;
        RefreshOptions();
    }

    private void RefreshOptions()
    {
        if (subtitleDropdown == null) return;
        List<string> offOn = new List<string> { LocalizationManager.Get("ui.off"), LocalizationManager.Get("ui.on") };
        ReplaceOptions(subtitleDropdown, offOn);
        ReplaceOptions(subtitleBackgroundDropdown, offOn);
        ReplaceOptions(speakerNameDropdown, offOn);
        ReplaceOptions(subtitleSizeDropdown, new List<string>
        {
            LocalizationManager.Get("settings.size_very_small"),
            LocalizationManager.Get("settings.size_small"),
            LocalizationManager.Get("settings.size_medium"),
            LocalizationManager.Get("settings.size_large"),
            LocalizationManager.Get("settings.size_very_large")
        });
        List<string> levels = new List<string>
        {
            LocalizationManager.Get("settings.level_low"),
            LocalizationManager.Get("settings.level_medium"),
            LocalizationManager.Get("settings.level_high")
        };
        ReplaceOptions(cameraShakeDropdown, levels);
        ReplaceOptions(blinkDropdown, levels);
    }

    private static void ReplaceOptions(TMP_Dropdown dropdown, List<string> options)
    {
        int selected = dropdown.value;
        dropdown.ClearOptions();
        dropdown.AddOptions(options);
        dropdown.SetValueWithoutNotify(Mathf.Clamp(selected, 0, options.Count - 1));
        dropdown.RefreshShownValue();
    }

    private void RegisterListeners()
    {
        subtitleDropdown.onValueChanged.AddListener(OnControlChanged);
        subtitleSizeDropdown.onValueChanged.AddListener(OnControlChanged);
        subtitleBackgroundDropdown.onValueChanged.AddListener(OnControlChanged);
        speakerNameDropdown.onValueChanged.AddListener(OnControlChanged);
        cameraShakeDropdown.onValueChanged.AddListener(OnControlChanged);
        blinkDropdown.onValueChanged.AddListener(OnControlChanged);
        contrastSlider.onValueChanged.AddListener(OnSliderChanged);
        defaultButton.onClick.AddListener(RestoreDefaults);
        applyButton.onClick.AddListener(ApplyPending);
    }

    private static Values ReadCurrentValues()
    {
        return new Values
        {
            subtitles = GameAccessibilitySettings.SubtitlesEnabled,
            subtitleSize = GameAccessibilitySettings.SubtitleSize,
            subtitleBackground = GameAccessibilitySettings.SubtitleBackgroundEnabled,
            speakerName = GameAccessibilitySettings.SpeakerNameEnabled,
            cameraShake = GameAccessibilitySettings.CameraShake,
            blink = GameAccessibilitySettings.BlinkIntensity,
            contrast = GameAccessibilitySettings.Contrast
        };
    }

    private Values ReadControls()
    {
        return new Values
        {
            subtitles = subtitleDropdown.value == 1,
            subtitleSize = subtitleSizeDropdown.value,
            subtitleBackground = subtitleBackgroundDropdown.value == 1,
            speakerName = speakerNameDropdown.value == 1,
            cameraShake = (AccessibilityLevel)cameraShakeDropdown.value,
            blink = (AccessibilityLevel)blinkDropdown.value,
            contrast = contrastSlider.value
        };
    }

    private void WriteControls(Values values)
    {
        suppressCallbacks = true;
        subtitleDropdown.SetValueWithoutNotify(values.subtitles ? 1 : 0);
        subtitleSizeDropdown.SetValueWithoutNotify(values.subtitleSize);
        subtitleBackgroundDropdown.SetValueWithoutNotify(values.subtitleBackground ? 1 : 0);
        speakerNameDropdown.SetValueWithoutNotify(values.speakerName ? 1 : 0);
        cameraShakeDropdown.SetValueWithoutNotify((int)values.cameraShake);
        blinkDropdown.SetValueWithoutNotify((int)values.blink);
        contrastSlider.SetValueWithoutNotify(values.contrast);
        subtitleDropdown.RefreshShownValue();
        subtitleSizeDropdown.RefreshShownValue();
        subtitleBackgroundDropdown.RefreshShownValue();
        speakerNameDropdown.RefreshShownValue();
        cameraShakeDropdown.RefreshShownValue();
        blinkDropdown.RefreshShownValue();
        suppressCallbacks = false;
    }

    private void OnControlChanged(int _)
    {
        RefreshState();
    }

    private void OnSliderChanged(float _)
    {
        RefreshState();
    }

    private void RefreshState()
    {
        if (!initialized && suppressCallbacks) return;
        bool subtitlesOn = subtitleDropdown.value == 1;
        subtitleSizeDropdown.interactable = subtitlesOn;
        subtitleBackgroundDropdown.interactable = subtitlesOn;
        speakerNameDropdown.interactable = subtitlesOn;
        SetGroupAlpha(FindDirectChild("Subtitle Size"), subtitlesOn ? 1f : 0.45f);
        applyButton.interactable = !ReadControls().Equals(appliedValues);
    }

    private static void SetGroupAlpha(Transform group, float alpha)
    {
        if (group == null) return;
        CanvasGroup canvasGroup = group.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = group.gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = alpha;
    }

    private void RestoreDefaults()
    {
        WriteControls(initialValues);
        RefreshState();
    }

    private void ApplyPending()
    {
        Values values = ReadControls();
        ApplyValues(values, true);
        appliedValues = values;
        RefreshState();
    }

    private void ApplyValues(Values values, bool save)
    {
        GameAccessibilitySettings.Apply(values.subtitles, values.subtitleSize, values.subtitleBackground,
            values.speakerName, values.cameraShake, values.blink, values.contrast, save);
        if (colorGrading != null)
        {
            colorGrading.enabled.Override(true);
            colorGrading.contrast.Override(values.contrast);
        }
    }

    private void ResolveColorGrading()
    {
        PostProcessVolume[] volumes = FindObjectsByType<PostProcessVolume>();
        for (int i = 0; i < volumes.Length; i++)
        {
            if (volumes[i].profile != null && volumes[i].profile.TryGetSettings(out colorGrading)) return;
        }
    }

    [Serializable]
    private struct Values : IEquatable<Values>
    {
        public bool subtitles;
        public int subtitleSize;
        public bool subtitleBackground;
        public bool speakerName;
        public AccessibilityLevel cameraShake;
        public AccessibilityLevel blink;
        public float contrast;

        public bool Equals(Values other)
        {
            return subtitles == other.subtitles && subtitleSize == other.subtitleSize
                && subtitleBackground == other.subtitleBackground && speakerName == other.speakerName
                && cameraShake == other.cameraShake && blink == other.blink
                && Mathf.Approximately(contrast, other.contrast);
        }
    }
}
