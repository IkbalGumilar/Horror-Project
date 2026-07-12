using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class ControlSettingsController : MonoBehaviour
{
    private Slider pitchSlider;
    private Slider yawSlider;
    private TMP_Dropdown reverseMouseDropdown;
    private TMP_Dropdown vibrationDropdown;
    private TMP_Dropdown runDropdown;
    private Button defaultButton;
    private Button applyButton;
    private TMP_Text pitchValueText;
    private TMP_Text yawValueText;

    private ControlValues initialValues;
    private ControlValues appliedValues;
    private bool initialized;
    private bool suppressCallbacks;

    private void Awake()
    {
        ResolveControls();
        if (!HasRequiredControls())
        {
            Debug.LogWarning("ControlSettingsController could not find every required control in Control Menu.", this);
            enabled = false;
            return;
        }

        ConfigureControls();
        initialValues = ReadCurrentValues();
        appliedValues = initialValues;
        WriteControls(appliedValues);
        RegisterListeners();
        initialized = true;
        RefreshApplyState();
    }

    private void OnEnable()
    {
        LocalizationManager.LanguageChanged += RefreshDropdownLabels;
        if (initialized)
        {
            RefreshDropdownLabels();
            RefreshApplyState();
        }
    }

    private void OnDisable()
    {
        LocalizationManager.LanguageChanged -= RefreshDropdownLabels;
    }

    private void OnDestroy()
    {
        if (!initialized)
        {
            return;
        }

        pitchSlider.onValueChanged.RemoveListener(OnSliderChanged);
        yawSlider.onValueChanged.RemoveListener(OnSliderChanged);
        reverseMouseDropdown.onValueChanged.RemoveListener(OnDropdownChanged);
        vibrationDropdown.onValueChanged.RemoveListener(OnDropdownChanged);
        runDropdown.onValueChanged.RemoveListener(OnDropdownChanged);
        defaultButton.onClick.RemoveListener(RestoreInitialValues);
        applyButton.onClick.RemoveListener(ApplyPendingValues);
    }

    private void ResolveControls()
    {
        pitchSlider = FindControl<Slider>("Sensitivity Pitch", "Slider");
        yawSlider = FindControl<Slider>("Sensitivity Yaw", "Slider");
        reverseMouseDropdown = FindControl<TMP_Dropdown>("Reverst Mouse", "Dropdown");
        vibrationDropdown = FindControl<TMP_Dropdown>("Vibrate", "Dropdown");
        runDropdown = FindControl<TMP_Dropdown>("Run", "Dropdown");
        defaultButton = FindDirectChild("Default")?.GetComponent<Button>();
        applyButton = FindDirectChild("Apply")?.GetComponent<Button>();
        pitchValueText = FindValueText(pitchSlider);
        yawValueText = FindValueText(yawSlider);
    }

    private bool HasRequiredControls()
    {
        return pitchSlider != null && yawSlider != null && reverseMouseDropdown != null
            && vibrationDropdown != null && runDropdown != null && defaultButton != null && applyButton != null;
    }

    private void ConfigureControls()
    {
        ConfigureSensitivitySlider(pitchSlider);
        ConfigureSensitivitySlider(yawSlider);
        RefreshDropdownLabels();
    }

    private static void ConfigureSensitivitySlider(Slider slider)
    {
        slider.minValue = 0f;
        slider.maxValue = 2f;
        slider.wholeNumbers = false;
    }

    private void RefreshDropdownLabels()
    {
        if (reverseMouseDropdown == null)
        {
            return;
        }

        ReplaceOptions(reverseMouseDropdown, new List<string>
        {
            LocalizationManager.Get("ui.off"),
            LocalizationManager.Get("ui.on")
        });
        ReplaceOptions(vibrationDropdown, new List<string>
        {
            LocalizationManager.Get("ui.off"),
            LocalizationManager.Get("ui.on")
        });
        ReplaceOptions(runDropdown, new List<string>
        {
            LocalizationManager.Get("settings.run_hold"),
            LocalizationManager.Get("settings.run_toggle")
        });
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
        pitchSlider.onValueChanged.AddListener(OnSliderChanged);
        yawSlider.onValueChanged.AddListener(OnSliderChanged);
        reverseMouseDropdown.onValueChanged.AddListener(OnDropdownChanged);
        vibrationDropdown.onValueChanged.AddListener(OnDropdownChanged);
        runDropdown.onValueChanged.AddListener(OnDropdownChanged);
        defaultButton.onClick.AddListener(RestoreInitialValues);
        applyButton.onClick.AddListener(ApplyPendingValues);
    }

    private static ControlValues ReadCurrentValues()
    {
        return new ControlValues
        {
            pitch = GameControlSettings.PitchSensitivityMultiplier,
            yaw = GameControlSettings.YawSensitivityMultiplier,
            reverseMouse = GameControlSettings.ReverseMouse,
            vibration = GameControlSettings.VibrationEnabled,
            runMode = GameControlSettings.RunMode
        };
    }

    private ControlValues ReadControls()
    {
        return new ControlValues
        {
            pitch = pitchSlider.value,
            yaw = yawSlider.value,
            reverseMouse = reverseMouseDropdown.value == 1,
            vibration = vibrationDropdown.value == 1,
            runMode = runDropdown.value == 1 ? RunInputMode.Toggle : RunInputMode.Hold
        };
    }

    private void WriteControls(ControlValues values)
    {
        suppressCallbacks = true;
        pitchSlider.SetValueWithoutNotify(values.pitch);
        yawSlider.SetValueWithoutNotify(values.yaw);
        reverseMouseDropdown.SetValueWithoutNotify(values.reverseMouse ? 1 : 0);
        vibrationDropdown.SetValueWithoutNotify(values.vibration ? 1 : 0);
        runDropdown.SetValueWithoutNotify(values.runMode == RunInputMode.Toggle ? 1 : 0);
        reverseMouseDropdown.RefreshShownValue();
        vibrationDropdown.RefreshShownValue();
        runDropdown.RefreshShownValue();
        RefreshSensitivityLabels();
        suppressCallbacks = false;
    }

    private void OnSliderChanged(float _)
    {
        RefreshSensitivityLabels();
        RefreshApplyState();
    }

    private void OnDropdownChanged(int _)
    {
        RefreshApplyState();
    }

    private void RestoreInitialValues()
    {
        WriteControls(initialValues);
        RefreshApplyState();
    }

    private void ApplyPendingValues()
    {
        appliedValues = ReadControls();
        GameControlSettings.Apply(
            appliedValues.yaw,
            appliedValues.pitch,
            appliedValues.reverseMouse,
            appliedValues.vibration,
            appliedValues.runMode,
            save: true);
        RefreshApplyState();
    }

    private void RefreshApplyState()
    {
        if (!initialized || suppressCallbacks)
        {
            return;
        }

        applyButton.interactable = !ReadControls().ApproximatelyEquals(appliedValues);
    }

    private void RefreshSensitivityLabels()
    {
        SetSensitivityText(pitchValueText, pitchSlider.value);
        SetSensitivityText(yawValueText, yawSlider.value);
    }

    private static void SetSensitivityText(TMP_Text label, float multiplier)
    {
        if (label != null)
        {
            label.text = $"{Mathf.RoundToInt(multiplier * 100f)}%";
        }
    }

    private static TMP_Text FindValueText(Slider slider)
    {
        Transform valueContainer = slider != null ? slider.transform.Find("Value") : null;
        return valueContainer != null ? valueContainer.GetComponentInChildren<TMP_Text>(true) : null;
    }

    private T FindControl<T>(string groupName, string controlName) where T : Component
    {
        Transform group = FindDirectChild(groupName);
        Transform control = group != null ? group.Find(controlName) : null;
        return control != null ? control.GetComponent<T>() : null;
    }

    private Transform FindDirectChild(string childName)
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.name == childName)
            {
                return child;
            }
        }

        return null;
    }

    private struct ControlValues
    {
        public float pitch;
        public float yaw;
        public bool reverseMouse;
        public bool vibration;
        public RunInputMode runMode;

        public bool ApproximatelyEquals(ControlValues other)
        {
            return Mathf.Approximately(pitch, other.pitch)
                && Mathf.Approximately(yaw, other.yaw)
                && reverseMouse == other.reverseMouse
                && vibration == other.vibration
                && runMode == other.runMode;
        }
    }
}
