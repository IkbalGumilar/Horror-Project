using UnityEngine;

public enum GameplayDifficulty
{
    Easy,
    Normal,
    Hard
}

public sealed class GameGameplaySettings : MonoBehaviour
{
    private const string DifficultyKey = "GameplaySettings.Difficulty";
    private const string CrosshairKey = "GameplaySettings.Crosshair";
    private const string TutorialKey = "GameplaySettings.Tutorial";
    private const string AutosaveKey = "GameplaySettings.Autosave";
    private const string CameraShakeKey = "GameplaySettings.CameraShake";
    private const string SubtitleBackgroundKey = "GameplaySettings.SubtitleBackground";
    private const string HeadBobIntensityKey = "GameplaySettings.HeadBobIntensity";

    private static GameGameplaySettings instance;

    [SerializeField] private GameplayDifficulty difficulty = GameplayDifficulty.Normal;
    [SerializeField] private bool crosshairEnabled = true;
    [SerializeField] private bool tutorialEnabled = true;
    [SerializeField] private bool autosaveEnabled = true;
    [SerializeField] private bool cameraShakeEnabled = true;
    [SerializeField] private bool subtitleBackgroundEnabled = true;
    [SerializeField, Range(0f, 2f)] private float headBobIntensity = 1f;

    public static GameplayDifficulty Difficulty
    {
        get
        {
            EnsureInstance();
            return instance.difficulty;
        }
    }

    public static bool CrosshairEnabled
    {
        get
        {
            EnsureInstance();
            return instance.crosshairEnabled;
        }
    }

    public static bool TutorialEnabled
    {
        get
        {
            EnsureInstance();
            return instance.tutorialEnabled;
        }
    }

    public static bool AutosaveEnabled
    {
        get
        {
            EnsureInstance();
            return instance.autosaveEnabled;
        }
    }

    public static bool CameraShakeEnabled
    {
        get
        {
            EnsureInstance();
            return instance.cameraShakeEnabled;
        }
    }

    public static bool SubtitleBackgroundEnabled
    {
        get
        {
            EnsureInstance();
            return instance.subtitleBackgroundEnabled;
        }
    }

    public static float HeadBobIntensity
    {
        get
        {
            EnsureInstance();
            return instance.headBobIntensity;
        }
    }

    public static void Apply(
        GameplayDifficulty difficulty,
        bool crosshairEnabled,
        bool tutorialEnabled,
        bool autosaveEnabled,
        bool cameraShakeEnabled,
        bool subtitleBackgroundEnabled,
        float headBobIntensity,
        bool save)
    {
        EnsureInstance();
        instance.difficulty = difficulty;
        instance.crosshairEnabled = crosshairEnabled;
        instance.tutorialEnabled = tutorialEnabled;
        instance.autosaveEnabled = autosaveEnabled;
        instance.cameraShakeEnabled = cameraShakeEnabled;
        instance.subtitleBackgroundEnabled = subtitleBackgroundEnabled;
        instance.headBobIntensity = Mathf.Clamp(headBobIntensity, 0f, 2f);

        if (save)
        {
            instance.Save();
        }
    }

    private static void EnsureInstance()
    {
        if (instance != null)
        {
            return;
        }

        instance = FindAnyObjectByType<GameGameplaySettings>();
        if (instance == null)
        {
            GameObject settingsObject = new GameObject(nameof(GameGameplaySettings));
            instance = settingsObject.AddComponent<GameGameplaySettings>();
        }

        instance.Load();
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

    private void Load()
    {
        difficulty = (GameplayDifficulty)Mathf.Clamp(
            PlayerPrefs.GetInt(DifficultyKey, (int)GameplayDifficulty.Normal),
            0,
            System.Enum.GetValues(typeof(GameplayDifficulty)).Length - 1);
        crosshairEnabled = PlayerPrefs.GetInt(CrosshairKey, 1) == 1;
        tutorialEnabled = PlayerPrefs.GetInt(TutorialKey, 1) == 1;
        autosaveEnabled = PlayerPrefs.GetInt(AutosaveKey, 1) == 1;
        cameraShakeEnabled = PlayerPrefs.GetInt(CameraShakeKey, 1) == 1;
        subtitleBackgroundEnabled = PlayerPrefs.GetInt(SubtitleBackgroundKey, 1) == 1;
        headBobIntensity = Mathf.Clamp(PlayerPrefs.GetFloat(HeadBobIntensityKey, 1f), 0f, 2f);
    }

    private void Save()
    {
        PlayerPrefs.SetInt(DifficultyKey, (int)difficulty);
        PlayerPrefs.SetInt(CrosshairKey, crosshairEnabled ? 1 : 0);
        PlayerPrefs.SetInt(TutorialKey, tutorialEnabled ? 1 : 0);
        PlayerPrefs.SetInt(AutosaveKey, autosaveEnabled ? 1 : 0);
        PlayerPrefs.SetInt(CameraShakeKey, cameraShakeEnabled ? 1 : 0);
        PlayerPrefs.SetInt(SubtitleBackgroundKey, subtitleBackgroundEnabled ? 1 : 0);
        PlayerPrefs.SetFloat(HeadBobIntensityKey, headBobIntensity);
        PlayerPrefs.Save();
    }
}
