using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class AudioSettingsController : MonoBehaviour
{
    private const float MinimumDecibels = -80f;
    private const float MaximumDecibels = 0f;

    private Slider masterSlider;
    private Slider musicSlider;
    private Slider sfxSlider;
    private Slider ambientSlider;
    private TMP_Dropdown dynamicRangeDropdown;
    private TMP_Dropdown outputDropdown;
    private Button defaultButton;
    private Button applyButton;
    private TMP_Text masterDbText;
    private TMP_Text musicDbText;
    private TMP_Text sfxDbText;
    private TMP_Text ambientDbText;

    private AudioValues initialValues;
    private AudioValues appliedValues;
    private bool initialized;
    private bool suppressCallbacks;

    private void Awake()
    {
        ResolveControls();
        if (!HasRequiredControls())
        {
            Debug.LogWarning("AudioSettingsController could not find every required control in Audio Menu.", this);
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

        masterSlider.onValueChanged.RemoveListener(OnControlChanged);
        musicSlider.onValueChanged.RemoveListener(OnControlChanged);
        sfxSlider.onValueChanged.RemoveListener(OnControlChanged);
        ambientSlider.onValueChanged.RemoveListener(OnControlChanged);
        dynamicRangeDropdown.onValueChanged.RemoveListener(OnControlChanged);
        outputDropdown.onValueChanged.RemoveListener(OnControlChanged);
        defaultButton.onClick.RemoveListener(RestoreInitialValues);
        applyButton.onClick.RemoveListener(ApplyPendingValues);
    }

    private void ResolveControls()
    {
        masterSlider = FindControl<Slider>("Master Volume", "Slider");
        musicSlider = FindControl<Slider>("Music Volume", "Slider");
        sfxSlider = FindControl<Slider>("SFX Volume", "Slider");
        ambientSlider = FindControl<Slider>("Ambient Volume", "Slider");
        dynamicRangeDropdown = FindControl<TMP_Dropdown>("Dynamic Range", "Dropdown");
        outputDropdown = FindControl<TMP_Dropdown>("Audio Output", "Dropdown");
        defaultButton = FindDirectChild("Default")?.GetComponent<Button>();
        applyButton = FindDirectChild("Apply")?.GetComponent<Button>();
        masterDbText = FindDbText(masterSlider);
        musicDbText = FindDbText(musicSlider);
        sfxDbText = FindDbText(sfxSlider);
        ambientDbText = FindDbText(ambientSlider);
    }

    private bool HasRequiredControls()
    {
        return masterSlider != null && musicSlider != null && sfxSlider != null && ambientSlider != null
            && dynamicRangeDropdown != null && outputDropdown != null && defaultButton != null && applyButton != null;
    }

    private void ConfigureControls()
    {
        ConfigureVolumeSlider(masterSlider);
        ConfigureVolumeSlider(musicSlider);
        ConfigureVolumeSlider(sfxSlider);
        ConfigureVolumeSlider(ambientSlider);

        RefreshDropdownLabels();
        dynamicRangeDropdown.SetValueWithoutNotify(1);
        dynamicRangeDropdown.interactable = false;
        PopulateOutputOptions();
        outputDropdown.interactable = true;
    }

    private static void ConfigureVolumeSlider(Slider slider)
    {
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
    }

    private void RefreshDropdownLabels()
    {
        if (dynamicRangeDropdown == null)
        {
            return;
        }

        int selected = dynamicRangeDropdown.value;
        dynamicRangeDropdown.ClearOptions();
        dynamicRangeDropdown.AddOptions(new List<string>
        {
            LocalizationManager.Get("settings.dynamic_night"),
            LocalizationManager.Get("settings.dynamic_standard"),
            LocalizationManager.Get("settings.dynamic_wide")
        });
        dynamicRangeDropdown.SetValueWithoutNotify(Mathf.Clamp(selected, 0, 2));
        dynamicRangeDropdown.RefreshShownValue();

        if (outputDropdown != null)
        {
            PopulateOutputOptions();
        }
    }

    private void PopulateOutputOptions()
    {
        int selected = outputDropdown.value;
        outputDropdown.ClearOptions();
        outputDropdown.AddOptions(new List<string>
        {
            LocalizationManager.Get("settings.output_mono"),
            LocalizationManager.Get("settings.output_stereo")
        });
        outputDropdown.SetValueWithoutNotify(Mathf.Clamp(selected, 0, 1));
        outputDropdown.RefreshShownValue();
    }

    private void RegisterListeners()
    {
        masterSlider.onValueChanged.AddListener(OnControlChanged);
        musicSlider.onValueChanged.AddListener(OnControlChanged);
        sfxSlider.onValueChanged.AddListener(OnControlChanged);
        ambientSlider.onValueChanged.AddListener(OnControlChanged);
        dynamicRangeDropdown.onValueChanged.AddListener(OnControlChanged);
        outputDropdown.onValueChanged.AddListener(OnControlChanged);
        defaultButton.onClick.AddListener(RestoreInitialValues);
        applyButton.onClick.AddListener(ApplyPendingValues);
    }

    private AudioValues ReadCurrentValues()
    {
        return new AudioValues
        {
            master = GameAudioManager.MasterVolume,
            music = GameAudioManager.MusicVolume,
            sfx = GameAudioManager.SfxVolume,
            ambient = GameAudioManager.AmbientVolume,
            dynamicRange = 1,
            outputMode = SpeakerModeToIndex(GameAudioManager.OutputMode)
        };
    }

    private AudioValues ReadControls()
    {
        return new AudioValues
        {
            master = SliderToLinearGain(masterSlider.value),
            music = SliderToLinearGain(musicSlider.value),
            sfx = SliderToLinearGain(sfxSlider.value),
            ambient = SliderToLinearGain(ambientSlider.value),
            dynamicRange = dynamicRangeDropdown.value,
            outputMode = outputDropdown.value
        };
    }

    private void WriteControls(AudioValues values)
    {
        suppressCallbacks = true;
        masterSlider.SetValueWithoutNotify(LinearGainToSlider(values.master));
        musicSlider.SetValueWithoutNotify(LinearGainToSlider(values.music));
        sfxSlider.SetValueWithoutNotify(LinearGainToSlider(values.sfx));
        ambientSlider.SetValueWithoutNotify(LinearGainToSlider(values.ambient));
        dynamicRangeDropdown.SetValueWithoutNotify(values.dynamicRange);
        dynamicRangeDropdown.RefreshShownValue();
        outputDropdown.SetValueWithoutNotify(values.outputMode);
        outputDropdown.RefreshShownValue();
        RefreshDecibelLabels();
        suppressCallbacks = false;
    }

    private void OnControlChanged(float _)
    {
        RefreshDecibelLabels();
        RefreshApplyState();
    }

    private void OnControlChanged(int _)
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
        GameAudioManager.ApplyVolumes(
            appliedValues.master,
            appliedValues.music,
            appliedValues.sfx,
            appliedValues.ambient,
            save: true);
        if (!GameAudioManager.ApplyOutputMode(IndexToSpeakerMode(appliedValues.outputMode), save: true))
        {
            appliedValues.outputMode = SpeakerModeToIndex(GameAudioManager.OutputMode);
            outputDropdown.SetValueWithoutNotify(appliedValues.outputMode);
            outputDropdown.RefreshShownValue();
        }
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

    private static TMP_Text FindDbText(Slider slider)
    {
        if (slider == null)
        {
            return null;
        }

        Transform dbContainer = slider.transform.Find("dB");
        return dbContainer != null ? dbContainer.GetComponentInChildren<TMP_Text>(true) : null;
    }

    private void RefreshDecibelLabels()
    {
        SetDecibelText(masterDbText, masterSlider.value);
        SetDecibelText(musicDbText, musicSlider.value);
        SetDecibelText(sfxDbText, sfxSlider.value);
        SetDecibelText(ambientDbText, ambientSlider.value);
    }

    private static void SetDecibelText(TMP_Text label, float sliderValue)
    {
        if (label == null)
        {
            return;
        }

        float decibels = sliderValue <= 0f
            ? MinimumDecibels
            : Mathf.Lerp(MinimumDecibels, MaximumDecibels, sliderValue);
        label.text = $"{decibels:0} dB";
    }

    private static float SliderToLinearGain(float sliderValue)
    {
        if (sliderValue <= 0f)
        {
            return 0f;
        }

        float decibels = Mathf.Lerp(MinimumDecibels, MaximumDecibels, sliderValue);
        return Mathf.Pow(10f, decibels / 20f);
    }

    private static float LinearGainToSlider(float linearGain)
    {
        if (linearGain <= 0f)
        {
            return 0f;
        }

        float decibels = 20f * Mathf.Log10(Mathf.Clamp01(linearGain));
        return Mathf.InverseLerp(
            MinimumDecibels,
            MaximumDecibels,
            Mathf.Clamp(decibels, MinimumDecibels, MaximumDecibels));
    }

    private static AudioSpeakerMode IndexToSpeakerMode(int index)
    {
        return index == 0 ? AudioSpeakerMode.Mono : AudioSpeakerMode.Stereo;
    }

    private static int SpeakerModeToIndex(AudioSpeakerMode speakerMode)
    {
        return speakerMode == AudioSpeakerMode.Mono ? 0 : 1;
    }

    private struct AudioValues
    {
        public float master;
        public float music;
        public float sfx;
        public float ambient;
        public int dynamicRange;
        public int outputMode;

        public bool ApproximatelyEquals(AudioValues other)
        {
            return Mathf.Approximately(master, other.master)
                && Mathf.Approximately(music, other.music)
                && Mathf.Approximately(sfx, other.sfx)
                && Mathf.Approximately(ambient, other.ambient)
                && dynamicRange == other.dynamicRange
                && outputMode == other.outputMode;
        }
    }
}
