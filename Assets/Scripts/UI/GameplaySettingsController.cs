using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class GameplaySettingsController : MonoBehaviour
{
    private TMP_Dropdown languageDropdown;
    private TMP_Dropdown difficultyDropdown;
    private TMP_Dropdown crosshairDropdown;
    private TMP_Dropdown tutorialDropdown;
    private TMP_Dropdown autosaveDropdown;
    private TMP_Dropdown cameraShakeDropdown;
    private TMP_Dropdown subtitleBackgroundDropdown;
    private Slider headBobIntensitySlider;
    private TMP_Text headBobValueText;
    private Button defaultButton;
    private Button applyButton;

    private GameplayValues initialValues;
    private GameplayValues appliedValues;
    private bool initialized;
    private bool suppressCallbacks;

    private void Awake()
    {
        ResolveControls();
        if (!HasRequiredControls())
        {
            Debug.LogWarning("GameplaySettingsController could not find every required control in Gameplay Menu.", this);
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
            WriteControls(ReadControls());
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

        languageDropdown.onValueChanged.RemoveListener(OnDropdownChanged);
        difficultyDropdown.onValueChanged.RemoveListener(OnDropdownChanged);
        crosshairDropdown.onValueChanged.RemoveListener(OnDropdownChanged);
        tutorialDropdown.onValueChanged.RemoveListener(OnDropdownChanged);
        autosaveDropdown.onValueChanged.RemoveListener(OnDropdownChanged);
        if (cameraShakeDropdown != null)
        {
            cameraShakeDropdown.onValueChanged.RemoveListener(OnDropdownChanged);
        }

        if (subtitleBackgroundDropdown != null)
        {
            subtitleBackgroundDropdown.onValueChanged.RemoveListener(OnDropdownChanged);
        }
        headBobIntensitySlider.onValueChanged.RemoveListener(OnHeadBobSliderChanged);
        defaultButton.onClick.RemoveListener(RestoreInitialValues);
        applyButton.onClick.RemoveListener(ApplyPendingValues);
    }

    private void ResolveControls()
    {
        languageDropdown = FindControl<TMP_Dropdown>("Language", "Dropdown");
        difficultyDropdown = FindControl<TMP_Dropdown>("Difficulty", "Dropdown")
            ?? FindControl<TMP_Dropdown>("Dificulty", "Dropdown");
        crosshairDropdown = FindControl<TMP_Dropdown>("Crosshair", "Dropdown");
        tutorialDropdown = FindControl<TMP_Dropdown>("Tutorial", "Dropdown");
        autosaveDropdown = FindControl<TMP_Dropdown>("Autosave", "Dropdown")
            ?? FindControl<TMP_Dropdown>("AutoSave", "Dropdown");
        cameraShakeDropdown = FindControl<TMP_Dropdown>("Camera Shake", "Dropdown");
        subtitleBackgroundDropdown = FindControl<TMP_Dropdown>("Subtitle Background", "Dropdown");
        headBobIntensitySlider = FindControl<Slider>("Intensity Head Bob", "Slider")
            ?? FindControl<Slider>("HeadBob", "Slider");
        headBobValueText = FindValueText(headBobIntensitySlider);
        defaultButton = FindDirectChild("Default")?.GetComponent<Button>();
        applyButton = FindDirectChild("Apply")?.GetComponent<Button>();
    }

    private bool HasRequiredControls()
    {
        return languageDropdown != null
            && difficultyDropdown != null
            && crosshairDropdown != null
            && tutorialDropdown != null
            && autosaveDropdown != null
            && headBobIntensitySlider != null
            && defaultButton != null
            && applyButton != null;
    }

    private void ConfigureControls()
    {
        headBobIntensitySlider.minValue = 0f;
        headBobIntensitySlider.maxValue = 2f;
        headBobIntensitySlider.wholeNumbers = false;
        RefreshDropdownLabels();
    }

    private void RefreshDropdownLabels()
    {
        if (languageDropdown == null)
        {
            return;
        }

        ReplaceOptions(languageDropdown, BuildLanguageLabels());
        ReplaceOptions(difficultyDropdown, new List<string>
        {
            LocalizationManager.Get("settings.difficulty_easy"),
            LocalizationManager.Get("settings.difficulty_normal"),
            LocalizationManager.Get("settings.difficulty_hard")
        });

        List<string> offOn = new List<string>
        {
            LocalizationManager.Get("ui.off"),
            LocalizationManager.Get("ui.on")
        };
        ReplaceOptions(crosshairDropdown, offOn);
        ReplaceOptions(tutorialDropdown, offOn);
        ReplaceOptions(autosaveDropdown, offOn);
        if (cameraShakeDropdown != null)
        {
            ReplaceOptions(cameraShakeDropdown, offOn);
        }

        if (subtitleBackgroundDropdown != null)
        {
            ReplaceOptions(subtitleBackgroundDropdown, offOn);
        }
    }

    private static List<string> BuildLanguageLabels()
    {
        List<string> labels = new List<string>(LocalizationManager.LanguageCount);
        for (int i = 0; i < LocalizationManager.LanguageCount; i++)
        {
            GameLanguage language = LocalizationManager.IndexToLanguage(i);
            labels.Add(language == GameLanguage.Indonesian ? "Indonesia" : "English");
        }

        return labels;
    }

    private static void ReplaceOptions(TMP_Dropdown dropdown, List<string> options)
    {
        int selected = dropdown.value;
        dropdown.ClearOptions();
        dropdown.AddOptions(options);
        dropdown.SetValueWithoutNotify(Mathf.Clamp(selected, 0, Mathf.Max(0, options.Count - 1)));
        dropdown.RefreshShownValue();
    }

    private void RegisterListeners()
    {
        languageDropdown.onValueChanged.AddListener(OnDropdownChanged);
        difficultyDropdown.onValueChanged.AddListener(OnDropdownChanged);
        crosshairDropdown.onValueChanged.AddListener(OnDropdownChanged);
        tutorialDropdown.onValueChanged.AddListener(OnDropdownChanged);
        autosaveDropdown.onValueChanged.AddListener(OnDropdownChanged);
        if (cameraShakeDropdown != null)
        {
            cameraShakeDropdown.onValueChanged.AddListener(OnDropdownChanged);
        }

        if (subtitleBackgroundDropdown != null)
        {
            subtitleBackgroundDropdown.onValueChanged.AddListener(OnDropdownChanged);
        }
        headBobIntensitySlider.onValueChanged.AddListener(OnHeadBobSliderChanged);
        defaultButton.onClick.AddListener(RestoreInitialValues);
        applyButton.onClick.AddListener(ApplyPendingValues);
    }

    private static GameplayValues ReadCurrentValues()
    {
        return new GameplayValues
        {
            language = LocalizationManager.CurrentLanguage,
            difficulty = GameGameplaySettings.Difficulty,
            crosshair = GameGameplaySettings.CrosshairEnabled,
            tutorial = GameGameplaySettings.TutorialEnabled,
            autosave = GameGameplaySettings.AutosaveEnabled,
            cameraShake = GameGameplaySettings.CameraShakeEnabled,
            subtitleBackground = GameGameplaySettings.SubtitleBackgroundEnabled,
            headBobIntensity = GameGameplaySettings.HeadBobIntensity
        };
    }

    private GameplayValues ReadControls()
    {
        return new GameplayValues
        {
            language = LocalizationManager.IndexToLanguage(languageDropdown.value),
            difficulty = (GameplayDifficulty)Mathf.Clamp(difficultyDropdown.value, 0, 2),
            crosshair = crosshairDropdown.value == 1,
            tutorial = tutorialDropdown.value == 1,
            autosave = autosaveDropdown.value == 1,
            cameraShake = cameraShakeDropdown == null ? appliedValues.cameraShake : cameraShakeDropdown.value == 1,
            subtitleBackground = subtitleBackgroundDropdown == null ? appliedValues.subtitleBackground : subtitleBackgroundDropdown.value == 1,
            headBobIntensity = headBobIntensitySlider.value
        };
    }

    private void WriteControls(GameplayValues values)
    {
        suppressCallbacks = true;
        languageDropdown.SetValueWithoutNotify((int)values.language);
        difficultyDropdown.SetValueWithoutNotify((int)values.difficulty);
        crosshairDropdown.SetValueWithoutNotify(values.crosshair ? 1 : 0);
        tutorialDropdown.SetValueWithoutNotify(values.tutorial ? 1 : 0);
        autosaveDropdown.SetValueWithoutNotify(values.autosave ? 1 : 0);
        if (cameraShakeDropdown != null)
        {
            cameraShakeDropdown.SetValueWithoutNotify(values.cameraShake ? 1 : 0);
        }

        if (subtitleBackgroundDropdown != null)
        {
            subtitleBackgroundDropdown.SetValueWithoutNotify(values.subtitleBackground ? 1 : 0);
        }
        headBobIntensitySlider.SetValueWithoutNotify(values.headBobIntensity);
        languageDropdown.RefreshShownValue();
        difficultyDropdown.RefreshShownValue();
        crosshairDropdown.RefreshShownValue();
        tutorialDropdown.RefreshShownValue();
        autosaveDropdown.RefreshShownValue();
        if (cameraShakeDropdown != null)
        {
            cameraShakeDropdown.RefreshShownValue();
        }

        if (subtitleBackgroundDropdown != null)
        {
            subtitleBackgroundDropdown.RefreshShownValue();
        }
        RefreshHeadBobLabel();
        suppressCallbacks = false;
    }

    private void OnDropdownChanged(int _)
    {
        RefreshApplyState();
    }

    private void OnHeadBobSliderChanged(float _)
    {
        RefreshHeadBobLabel();
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
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.SetLanguage(appliedValues.language);
        }

        GameGameplaySettings.Apply(
            appliedValues.difficulty,
            appliedValues.crosshair,
            appliedValues.tutorial,
            appliedValues.autosave,
            appliedValues.cameraShake,
            appliedValues.subtitleBackground,
            appliedValues.headBobIntensity,
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

    private void RefreshHeadBobLabel()
    {
        if (headBobValueText != null)
        {
            headBobValueText.text = $"{Mathf.RoundToInt(headBobIntensitySlider.value * 100f)}%";
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

    private struct GameplayValues
    {
        public GameLanguage language;
        public GameplayDifficulty difficulty;
        public bool crosshair;
        public bool tutorial;
        public bool autosave;
        public bool cameraShake;
        public bool subtitleBackground;
        public float headBobIntensity;

        public bool ApproximatelyEquals(GameplayValues other)
        {
            return language == other.language
                && difficulty == other.difficulty
                && crosshair == other.crosshair
                && tutorial == other.tutorial
                && autosave == other.autosave
                && cameraShake == other.cameraShake
                && subtitleBackground == other.subtitleBackground
                && Mathf.Approximately(headBobIntensity, other.headBobIntensity);
        }
    }
}
