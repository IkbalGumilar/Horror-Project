using System;
using UnityEngine;

public enum AccessibilityLevel
{
    Low,
    Medium,
    High
}

public static class GameAccessibilitySettings
{
    private const string Prefix = "AccessibilitySettings.";

    public static event Action Changed;

    public static bool SubtitlesEnabled { get; private set; } = true;
    public static int SubtitleSize { get; private set; } = 2;
    public static bool SubtitleBackgroundEnabled { get; private set; } = true;
    public static bool SpeakerNameEnabled { get; private set; } = true;
    public static AccessibilityLevel CameraShake { get; private set; } = AccessibilityLevel.Medium;
    public static AccessibilityLevel BlinkIntensity { get; private set; } = AccessibilityLevel.Medium;
    public static float Contrast { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Load()
    {
        SubtitlesEnabled = PlayerPrefs.GetInt(Prefix + "Subtitles", 1) == 1;
        SubtitleSize = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "SubtitleSize", 2), 0, 4);
        SubtitleBackgroundEnabled = PlayerPrefs.GetInt(Prefix + "SubtitleBackground", 1) == 1;
        SpeakerNameEnabled = PlayerPrefs.GetInt(Prefix + "SpeakerName", 1) == 1;
        CameraShake = (AccessibilityLevel)Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "CameraShake", 1), 0, 2);
        BlinkIntensity = (AccessibilityLevel)Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "BlinkIntensity", 1), 0, 2);
        Contrast = Mathf.Clamp(PlayerPrefs.GetFloat(Prefix + "Contrast", 0f), 0f, 100f);
    }

    public static void Apply(
        bool subtitlesEnabled,
        int subtitleSize,
        bool subtitleBackgroundEnabled,
        bool speakerNameEnabled,
        AccessibilityLevel cameraShake,
        AccessibilityLevel blinkIntensity,
        float contrast,
        bool save)
    {
        SubtitlesEnabled = subtitlesEnabled;
        SubtitleSize = Mathf.Clamp(subtitleSize, 0, 4);
        SubtitleBackgroundEnabled = subtitleBackgroundEnabled;
        SpeakerNameEnabled = speakerNameEnabled;
        CameraShake = cameraShake;
        BlinkIntensity = blinkIntensity;
        Contrast = Mathf.Clamp(contrast, 0f, 100f);

        Shader.SetGlobalFloat("_GlobalContrast", Contrast);
        Changed?.Invoke();

        if (!save)
        {
            return;
        }

        PlayerPrefs.SetInt(Prefix + "Subtitles", SubtitlesEnabled ? 1 : 0);
        PlayerPrefs.SetInt(Prefix + "SubtitleSize", SubtitleSize);
        PlayerPrefs.SetInt(Prefix + "SubtitleBackground", SubtitleBackgroundEnabled ? 1 : 0);
        PlayerPrefs.SetInt(Prefix + "SpeakerName", SpeakerNameEnabled ? 1 : 0);
        PlayerPrefs.SetInt(Prefix + "CameraShake", (int)CameraShake);
        PlayerPrefs.SetInt(Prefix + "BlinkIntensity", (int)BlinkIntensity);
        PlayerPrefs.SetFloat(Prefix + "Contrast", Contrast);
        PlayerPrefs.Save();
    }
}
