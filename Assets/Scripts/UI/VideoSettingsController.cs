using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class VideoSettingsController : MonoBehaviour
{
    private const string PrefsPrefix = "VideoSettings.";
    private const string GlobalBrightnessName = "_GlobalBrightness";
    private static readonly int[] CommonResolutionHeights = { 360, 480, 720, 900, 1080, 1440, 2160 };
    private static readonly int[] FrameRateValues = { 30, 60, 90, 120, -1 };

    private Slider fullScreenSlider;
    private TMP_Dropdown resolutionDropdown;
    private Slider brightnessSlider;
    private TMP_Dropdown qualityDropdown;
    private TMP_Dropdown vSyncDropdown;
    private TMP_Dropdown fpsDropdown;
    private Slider bloomSlider;
    private Slider motionBlurSlider;
    private Button defaultButton;
    private Button applyButton;

    private readonly List<Vector2Int> resolutions = new List<Vector2Int>();
    private VideoSettings initialSettings;
    private VideoSettings appliedSettings;
    private Bloom bloom;
    private MotionBlur motionBlur;
    private ColorGrading colorGrading;
    private bool initialized;
    private bool suppressCallbacks;

    private void Awake()
    {
        ResolveControls();
        if (!HasRequiredControls())
        {
            Debug.LogWarning("VideoSettingsController could not find every required control in Video Menu.", this);
            enabled = false;
            return;
        }

        ConfigureControls();
        ResolvePostProcessingEffects();
        PopulateOptions();
        initialSettings = ReadInitialSettings();
        appliedSettings = LoadSavedSettings(initialSettings);
        WriteControls(appliedSettings);
        ApplySettings(appliedSettings, save: false);
        RegisterListeners();
        initialized = true;
        RefreshApplyState();
    }

    private void OnEnable()
    {
        LocalizationManager.LanguageChanged += RefreshLocalizedOptions;
        if (initialized)
        {
            RefreshLocalizedOptions();
            RefreshApplyState();
        }
    }

    private void OnDisable()
    {
        LocalizationManager.LanguageChanged -= RefreshLocalizedOptions;
    }

    private void OnDestroy()
    {
        UnregisterListeners();
    }

    private void ResolveControls()
    {
        fullScreenSlider = FindControl<Slider>("Full Screen", "Toggle");
        resolutionDropdown = FindControl<TMP_Dropdown>("Resolution", "Dropdown");
        brightnessSlider = FindControl<Slider>("Brightness", "Slider");
        qualityDropdown = FindControl<TMP_Dropdown>("Graphics", "Dropdown");
        vSyncDropdown = FindControl<TMP_Dropdown>("V-Sync", "Dropdown");
        fpsDropdown = FindControl<TMP_Dropdown>("FPS", "Dropdown");
        bloomSlider = FindControl<Slider>("Bloom", "Toggle");
        motionBlurSlider = FindControl<Slider>("Motion Blur", "Toggle");
        defaultButton = FindDirectChild("Default")?.GetComponent<Button>();
        applyButton = FindDirectChild("Apply")?.GetComponent<Button>();
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

    private bool HasRequiredControls()
    {
        return fullScreenSlider != null
            && resolutionDropdown != null
            && brightnessSlider != null
            && qualityDropdown != null
            && vSyncDropdown != null
            && fpsDropdown != null
            && bloomSlider != null
            && motionBlurSlider != null
            && defaultButton != null
            && applyButton != null;
    }

    private void ConfigureControls()
    {
        ConfigureBinarySlider(fullScreenSlider);
        ConfigureBinarySlider(bloomSlider);
        ConfigureBinarySlider(motionBlurSlider);
        brightnessSlider.minValue = 0f;
        brightnessSlider.maxValue = 100f;
        brightnessSlider.wholeNumbers = false;
    }

    private static void ConfigureBinarySlider(Slider slider)
    {
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = true;

        BinarySliderClickToggle clickToggle = slider.GetComponent<BinarySliderClickToggle>();
        if (clickToggle == null)
        {
            clickToggle = slider.gameObject.AddComponent<BinarySliderClickToggle>();
        }

        clickToggle.Configure();
    }

    private void PopulateOptions()
    {
        BuildResolutionList();
        ReplaceOptions(resolutionDropdown, BuildResolutionLabels());
        ReplaceOptions(qualityDropdown, new List<string>(QualitySettings.names));
        ReplaceOptions(vSyncDropdown, BuildVSyncLabels());
        ReplaceOptions(fpsDropdown, BuildFrameRateLabels());
    }

    private void BuildResolutionList()
    {
        resolutions.Clear();
        int nativeWidth = Mathf.Max(Screen.currentResolution.width, Screen.width);
        int nativeHeight = Mathf.Max(Screen.currentResolution.height, Screen.height);
        float aspect = nativeHeight > 0 ? (float)nativeWidth / nativeHeight : 16f / 9f;

        Resolution[] supported = Screen.resolutions;
        for (int i = 0; i < supported.Length; i++)
        {
            Resolution resolution = supported[i];
            if (resolution.height < 360 || resolution.height > 2160 || resolution.width > 3840)
            {
                continue;
            }

            float resolutionAspect = (float)resolution.width / resolution.height;
            if (Mathf.Abs(resolutionAspect - aspect) <= 0.08f)
            {
                AddResolution(resolution.width, resolution.height);
            }
        }

        if (resolutions.Count < 2)
        {
            int maximumHeight = Mathf.Min(nativeHeight, 2160);
            for (int i = 0; i < CommonResolutionHeights.Length; i++)
            {
                int height = CommonResolutionHeights[i];
                if (height > maximumHeight)
                {
                    continue;
                }

                int width = Mathf.RoundToInt(height * aspect);
                width += width % 2;
                AddResolution(width, height);
            }
        }

        int cappedHeight = Mathf.Min(nativeHeight, 2160);
        int cappedWidth = Mathf.RoundToInt(cappedHeight * aspect);
        if (cappedWidth > 3840)
        {
            cappedWidth = 3840;
            cappedHeight = Mathf.RoundToInt(cappedWidth / aspect);
        }

        cappedWidth += cappedWidth % 2;
        cappedHeight += cappedHeight % 2;
        AddResolution(cappedWidth, cappedHeight);
        resolutions.Sort((left, right) =>
            (left.x * left.y).CompareTo(right.x * right.y));
    }

    private void AddResolution(int width, int height)
    {
        Vector2Int candidate = new Vector2Int(width, height);
        if (width > 0 && height > 0 && !resolutions.Contains(candidate))
        {
            resolutions.Add(candidate);
        }
    }

    private List<string> BuildResolutionLabels()
    {
        List<string> labels = new List<string>(resolutions.Count);
        for (int i = 0; i < resolutions.Count; i++)
        {
            Vector2Int resolution = resolutions[i];
            labels.Add($"{resolution.x} x {resolution.y}");
        }

        return labels;
    }

    private static List<string> BuildVSyncLabels()
    {
        return new List<string>
        {
            LocalizationManager.Get("ui.off"),
            LocalizationManager.Get("ui.on"),
            "G-Sync",
            "FreeSync"
        };
    }

    private static List<string> BuildFrameRateLabels()
    {
        return new List<string> { "30", "60", "90", "120", LocalizationManager.Get("settings.unlimited") };
    }

    private static void ReplaceOptions(TMP_Dropdown dropdown, List<string> options)
    {
        int selected = dropdown.value;
        dropdown.ClearOptions();
        dropdown.AddOptions(options);
        dropdown.SetValueWithoutNotify(Mathf.Clamp(selected, 0, Mathf.Max(0, options.Count - 1)));
        dropdown.RefreshShownValue();
    }

    private VideoSettings ReadInitialSettings()
    {
        int currentResolution = FindNearestResolution(Screen.width, Screen.height);
        int fpsIndex = FindFrameRateIndex(Application.targetFrameRate);
        float brightness = brightnessSlider.value;

        bool hasBloom = bloom != null;
        bool hasMotionBlur = motionBlur != null;
        bloomSlider.interactable = hasBloom;
        motionBlurSlider.interactable = hasMotionBlur;

        if (colorGrading != null)
        {
            brightness = Mathf.Clamp(colorGrading.brightness.value, 0f, 100f);
        }

        return new VideoSettings
        {
            fullScreen = Screen.fullScreen,
            resolutionIndex = currentResolution,
            brightness = brightness,
            qualityIndex = Mathf.Clamp(QualitySettings.GetQualityLevel(), 0, QualitySettings.names.Length - 1),
            vSyncMode = QualitySettings.vSyncCount > 0 ? 1 : 0,
            fpsIndex = fpsIndex,
            bloom = hasBloom && bloom.enabled.value,
            motionBlur = hasMotionBlur && motionBlur.enabled.value
        };
    }

    private VideoSettings LoadSavedSettings(VideoSettings fallback)
    {
        if (!PlayerPrefs.HasKey(PrefsPrefix + "Saved"))
        {
            return fallback;
        }

        int width = PlayerPrefs.GetInt(PrefsPrefix + "Width", Screen.width);
        int height = PlayerPrefs.GetInt(PrefsPrefix + "Height", Screen.height);
        VideoSettings loaded = new VideoSettings
        {
            fullScreen = PlayerPrefs.GetInt(PrefsPrefix + "FullScreen", fallback.fullScreen ? 1 : 0) == 1,
            resolutionIndex = FindNearestResolution(width, height),
            brightness = Mathf.Clamp(PlayerPrefs.GetFloat(PrefsPrefix + "Brightness", fallback.brightness), 0f, 100f),
            qualityIndex = Mathf.Clamp(PlayerPrefs.GetInt(PrefsPrefix + "Quality", fallback.qualityIndex), 0, QualitySettings.names.Length - 1),
            vSyncMode = Mathf.Clamp(PlayerPrefs.GetInt(PrefsPrefix + "VSync", fallback.vSyncMode), 0, 3),
            fpsIndex = Mathf.Clamp(PlayerPrefs.GetInt(PrefsPrefix + "FPS", fallback.fpsIndex), 0, FrameRateValues.Length - 1),
            bloom = PlayerPrefs.GetInt(PrefsPrefix + "Bloom", fallback.bloom ? 1 : 0) == 1,
            motionBlur = PlayerPrefs.GetInt(PrefsPrefix + "MotionBlur", fallback.motionBlur ? 1 : 0) == 1
        };

        if (bloom == null)
        {
            loaded.bloom = false;
        }

        if (motionBlur == null)
        {
            loaded.motionBlur = false;
        }

        return loaded;
    }

    private int FindNearestResolution(int width, int height)
    {
        if (resolutions.Count == 0)
        {
            return 0;
        }

        int bestIndex = 0;
        long bestDistance = long.MaxValue;
        for (int i = 0; i < resolutions.Count; i++)
        {
            long widthDifference = resolutions[i].x - width;
            long heightDifference = resolutions[i].y - height;
            long distance = widthDifference * widthDifference + heightDifference * heightDifference;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private static int FindFrameRateIndex(int frameRate)
    {
        for (int i = 0; i < FrameRateValues.Length; i++)
        {
            if (FrameRateValues[i] == frameRate)
            {
                return i;
            }
        }

        return FrameRateValues.Length - 1;
    }

    private void ResolvePostProcessingEffects()
    {
        PostProcessVolume[] volumes = FindObjectsByType<PostProcessVolume>(FindObjectsInactive.Include);

        for (int i = 0; i < volumes.Length; i++)
        {
            PostProcessProfile profile = volumes[i].profile;
            if (profile == null)
            {
                continue;
            }

            if (bloom == null)
            {
                profile.TryGetSettings(out bloom);
            }

            if (motionBlur == null)
            {
                profile.TryGetSettings(out motionBlur);
            }

            if (colorGrading == null)
            {
                profile.TryGetSettings(out colorGrading);
            }
        }
    }

    private void RegisterListeners()
    {
        fullScreenSlider.onValueChanged.AddListener(OnControlChanged);
        resolutionDropdown.onValueChanged.AddListener(OnControlChanged);
        brightnessSlider.onValueChanged.AddListener(OnControlChanged);
        qualityDropdown.onValueChanged.AddListener(OnControlChanged);
        vSyncDropdown.onValueChanged.AddListener(OnControlChanged);
        fpsDropdown.onValueChanged.AddListener(OnControlChanged);
        bloomSlider.onValueChanged.AddListener(OnControlChanged);
        motionBlurSlider.onValueChanged.AddListener(OnControlChanged);
        defaultButton.onClick.AddListener(RestoreInitialSettings);
        applyButton.onClick.AddListener(ApplyPendingSettings);
    }

    private void UnregisterListeners()
    {
        if (!initialized)
        {
            return;
        }

        fullScreenSlider.onValueChanged.RemoveListener(OnControlChanged);
        resolutionDropdown.onValueChanged.RemoveListener(OnControlChanged);
        brightnessSlider.onValueChanged.RemoveListener(OnControlChanged);
        qualityDropdown.onValueChanged.RemoveListener(OnControlChanged);
        vSyncDropdown.onValueChanged.RemoveListener(OnControlChanged);
        fpsDropdown.onValueChanged.RemoveListener(OnControlChanged);
        bloomSlider.onValueChanged.RemoveListener(OnControlChanged);
        motionBlurSlider.onValueChanged.RemoveListener(OnControlChanged);
        defaultButton.onClick.RemoveListener(RestoreInitialSettings);
        applyButton.onClick.RemoveListener(ApplyPendingSettings);
    }

    private void OnControlChanged(float _)
    {
        RefreshApplyState();
    }

    private void OnControlChanged(int _)
    {
        RefreshApplyState();
    }

    private void RestoreInitialSettings()
    {
        WriteControls(initialSettings);
        RefreshApplyState();
    }

    private void ApplyPendingSettings()
    {
        VideoSettings pending = ReadControls();
        ApplySettings(pending, save: true);
        appliedSettings = pending;
        RefreshApplyState();
    }

    private void ApplySettings(VideoSettings settings, bool save)
    {
        Vector2Int resolution = resolutions[Mathf.Clamp(settings.resolutionIndex, 0, resolutions.Count - 1)];
        FullScreenMode mode = settings.fullScreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        Screen.SetResolution(resolution.x, resolution.y, mode);

        QualitySettings.SetQualityLevel(settings.qualityIndex, applyExpensiveChanges: true);
        QualitySettings.vSyncCount = settings.vSyncMode == 1 ? 1 : 0;
        Application.targetFrameRate = FrameRateValues[settings.fpsIndex];
        Shader.SetGlobalFloat(GlobalBrightnessName, settings.brightness);

        if (colorGrading != null)
        {
            colorGrading.enabled.Override(true);
            colorGrading.brightness.Override(settings.brightness);
        }

        if (bloom != null)
        {
            bloom.enabled.Override(settings.bloom);
        }

        if (motionBlur != null)
        {
            motionBlur.enabled.Override(settings.motionBlur);
        }

        if (save)
        {
            SaveSettings(settings, resolution);
        }
    }

    private static void SaveSettings(VideoSettings settings, Vector2Int resolution)
    {
        PlayerPrefs.SetInt(PrefsPrefix + "Saved", 1);
        PlayerPrefs.SetInt(PrefsPrefix + "FullScreen", settings.fullScreen ? 1 : 0);
        PlayerPrefs.SetInt(PrefsPrefix + "Width", resolution.x);
        PlayerPrefs.SetInt(PrefsPrefix + "Height", resolution.y);
        PlayerPrefs.SetFloat(PrefsPrefix + "Brightness", settings.brightness);
        PlayerPrefs.SetInt(PrefsPrefix + "Quality", settings.qualityIndex);
        PlayerPrefs.SetInt(PrefsPrefix + "VSync", settings.vSyncMode);
        PlayerPrefs.SetInt(PrefsPrefix + "FPS", settings.fpsIndex);
        PlayerPrefs.SetInt(PrefsPrefix + "Bloom", settings.bloom ? 1 : 0);
        PlayerPrefs.SetInt(PrefsPrefix + "MotionBlur", settings.motionBlur ? 1 : 0);
        PlayerPrefs.Save();
    }

    private VideoSettings ReadControls()
    {
        return new VideoSettings
        {
            fullScreen = fullScreenSlider.value >= 0.5f,
            resolutionIndex = resolutionDropdown.value,
            brightness = brightnessSlider.value,
            qualityIndex = qualityDropdown.value,
            vSyncMode = vSyncDropdown.value,
            fpsIndex = fpsDropdown.value,
            bloom = bloom != null && bloomSlider.value >= 0.5f,
            motionBlur = motionBlur != null && motionBlurSlider.value >= 0.5f
        };
    }

    private void WriteControls(VideoSettings settings)
    {
        suppressCallbacks = true;
        fullScreenSlider.SetValueWithoutNotify(settings.fullScreen ? 1f : 0f);
        resolutionDropdown.SetValueWithoutNotify(settings.resolutionIndex);
        brightnessSlider.SetValueWithoutNotify(settings.brightness);
        qualityDropdown.SetValueWithoutNotify(settings.qualityIndex);
        vSyncDropdown.SetValueWithoutNotify(settings.vSyncMode);
        fpsDropdown.SetValueWithoutNotify(settings.fpsIndex);
        bloomSlider.SetValueWithoutNotify(settings.bloom ? 1f : 0f);
        motionBlurSlider.SetValueWithoutNotify(settings.motionBlur ? 1f : 0f);
        resolutionDropdown.RefreshShownValue();
        qualityDropdown.RefreshShownValue();
        vSyncDropdown.RefreshShownValue();
        fpsDropdown.RefreshShownValue();
        suppressCallbacks = false;
    }

    private void RefreshApplyState()
    {
        if (!initialized || suppressCallbacks)
        {
            return;
        }

        applyButton.interactable = !ReadControls().Equals(appliedSettings);
    }

    private void RefreshLocalizedOptions()
    {
        if (vSyncDropdown == null)
        {
            return;
        }

        int selected = vSyncDropdown.value;
        ReplaceOptions(vSyncDropdown, BuildVSyncLabels());
        vSyncDropdown.SetValueWithoutNotify(selected);
        vSyncDropdown.RefreshShownValue();

        selected = fpsDropdown.value;
        ReplaceOptions(fpsDropdown, BuildFrameRateLabels());
        fpsDropdown.SetValueWithoutNotify(selected);
        fpsDropdown.RefreshShownValue();
    }

    [Serializable]
    private struct VideoSettings : IEquatable<VideoSettings>
    {
        public bool fullScreen;
        public int resolutionIndex;
        public float brightness;
        public int qualityIndex;
        public int vSyncMode;
        public int fpsIndex;
        public bool bloom;
        public bool motionBlur;

        public bool Equals(VideoSettings other)
        {
            return fullScreen == other.fullScreen
                && resolutionIndex == other.resolutionIndex
                && Mathf.Approximately(brightness, other.brightness)
                && qualityIndex == other.qualityIndex
                && vSyncMode == other.vSyncMode
                && fpsIndex == other.fpsIndex
                && bloom == other.bloom
                && motionBlur == other.motionBlur;
        }
    }
}
