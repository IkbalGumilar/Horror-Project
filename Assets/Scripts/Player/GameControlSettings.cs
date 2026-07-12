using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public enum RunInputMode
{
    Hold,
    Toggle
}

[DisallowMultipleComponent]
public sealed class GameControlSettings : MonoBehaviour
{
    private const string PrefsPrefix = "ControlSettings.";

    private static GameControlSettings instance;
    private Coroutine vibrationRoutine;

    public static float YawSensitivityMultiplier { get; private set; } = 1f;
    public static float PitchSensitivityMultiplier { get; private set; } = 1f;
    public static bool ReverseMouse { get; private set; }
    public static bool VibrationEnabled { get; private set; } = true;
    public static RunInputMode RunMode { get; private set; } = RunInputMode.Hold;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null)
        {
            return;
        }

        GameObject settingsObject = new GameObject("Control Settings");
        instance = settingsObject.AddComponent<GameControlSettings>();
        DontDestroyOnLoad(settingsObject);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        Load();
    }

    private void OnDisable()
    {
        StopVibration();
    }

    public static void Apply(
        float yawMultiplier,
        float pitchMultiplier,
        bool reverseMouse,
        bool vibrationEnabled,
        RunInputMode runMode,
        bool save)
    {
        YawSensitivityMultiplier = Mathf.Clamp(yawMultiplier, 0f, 2f);
        PitchSensitivityMultiplier = Mathf.Clamp(pitchMultiplier, 0f, 2f);
        ReverseMouse = reverseMouse;
        VibrationEnabled = vibrationEnabled;
        RunMode = runMode == RunInputMode.Toggle ? RunInputMode.Toggle : RunInputMode.Hold;

        if (!VibrationEnabled)
        {
            instance?.StopVibration();
        }

        if (!save)
        {
            return;
        }

        PlayerPrefs.SetFloat(PrefsPrefix + "Yaw", YawSensitivityMultiplier);
        PlayerPrefs.SetFloat(PrefsPrefix + "Pitch", PitchSensitivityMultiplier);
        PlayerPrefs.SetInt(PrefsPrefix + "ReverseMouse", ReverseMouse ? 1 : 0);
        PlayerPrefs.SetInt(PrefsPrefix + "Vibration", VibrationEnabled ? 1 : 0);
        PlayerPrefs.SetInt(PrefsPrefix + "RunMode", (int)RunMode);
        PlayerPrefs.SetInt(PrefsPrefix + "Saved", 1);
        PlayerPrefs.Save();
    }

    public static void PlayVibration(float lowFrequency, float highFrequency, float duration)
    {
        if (!VibrationEnabled || instance == null || Gamepad.current == null || duration <= 0f)
        {
            return;
        }

        if (instance.vibrationRoutine != null)
        {
            instance.StopCoroutine(instance.vibrationRoutine);
        }

        instance.vibrationRoutine = instance.StartCoroutine(instance.Vibrate(
            Mathf.Clamp01(lowFrequency),
            Mathf.Clamp01(highFrequency),
            duration));
    }

    private void Load()
    {
        if (PlayerPrefs.GetInt(PrefsPrefix + "Saved", 0) != 1)
        {
            return;
        }

        Apply(
            PlayerPrefs.GetFloat(PrefsPrefix + "Yaw", 1f),
            PlayerPrefs.GetFloat(PrefsPrefix + "Pitch", 1f),
            PlayerPrefs.GetInt(PrefsPrefix + "ReverseMouse", 0) == 1,
            PlayerPrefs.GetInt(PrefsPrefix + "Vibration", 1) == 1,
            (RunInputMode)PlayerPrefs.GetInt(PrefsPrefix + "RunMode", (int)RunInputMode.Hold),
            save: false);
    }

    private IEnumerator Vibrate(float lowFrequency, float highFrequency, float duration)
    {
        Gamepad gamepad = Gamepad.current;
        gamepad.SetMotorSpeeds(lowFrequency, highFrequency);
        yield return new WaitForSecondsRealtime(duration);

        if (gamepad != null && gamepad.added)
        {
            gamepad.SetMotorSpeeds(0f, 0f);
        }

        vibrationRoutine = null;
    }

    private void StopVibration()
    {
        if (vibrationRoutine != null)
        {
            StopCoroutine(vibrationRoutine);
            vibrationRoutine = null;
        }

        Gamepad.current?.SetMotorSpeeds(0f, 0f);
    }
}
