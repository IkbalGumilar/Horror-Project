using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class GameAudioManager : MonoBehaviour
{
    private const string PrefsPrefix = "AudioSettings.";

    private static GameAudioManager instance;

    private AudioSource musicSource;
    private AudioSource ambientSource;

    public static float MasterVolume { get; private set; } = 1f;
    public static float MusicVolume { get; private set; } = 1f;
    public static float SfxVolume { get; private set; } = 1f;
    public static float AmbientVolume { get; private set; } = 1f;
    public static AudioSpeakerMode OutputMode { get; private set; } = AudioSpeakerMode.Stereo;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null)
        {
            return;
        }

        GameObject managerObject = new GameObject("Game Audio");
        instance = managerObject.AddComponent<GameAudioManager>();
        DontDestroyOnLoad(managerObject);
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
        musicSource = CreateLoopSource("Music");
        ambientSource = CreateLoopSource("Ambient");
        LoadSavedVolumes();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            instance = null;
        }
    }

    public static void ApplyVolumes(float master, float music, float sfx, float ambient, bool save)
    {
        MasterVolume = Mathf.Clamp01(master);
        MusicVolume = Mathf.Clamp01(music);
        SfxVolume = Mathf.Clamp01(sfx);
        AmbientVolume = Mathf.Clamp01(ambient);

        AudioListener.volume = MasterVolume;
        if (instance != null)
        {
            instance.RefreshSourceVolumes();
        }

        if (!save)
        {
            return;
        }

        PlayerPrefs.SetFloat(PrefsPrefix + "Master", MasterVolume);
        PlayerPrefs.SetFloat(PrefsPrefix + "Music", MusicVolume);
        PlayerPrefs.SetFloat(PrefsPrefix + "SFX", SfxVolume);
        PlayerPrefs.SetFloat(PrefsPrefix + "Ambient", AmbientVolume);
        PlayerPrefs.SetInt(PrefsPrefix + "Saved", 1);
        PlayerPrefs.Save();
    }

    public static bool ApplyOutputMode(AudioSpeakerMode outputMode, bool save)
    {
        if (outputMode != AudioSpeakerMode.Mono && outputMode != AudioSpeakerMode.Stereo)
        {
            outputMode = AudioSpeakerMode.Stereo;
        }

        AudioConfiguration configuration = AudioSettings.GetConfiguration();
        if (configuration.speakerMode != outputMode)
        {
            configuration.speakerMode = outputMode;
            if (!AudioSettings.Reset(configuration))
            {
                Debug.LogWarning($"Audio output mode {outputMode} is not supported by the current audio driver.");
                OutputMode = AudioSettings.GetConfiguration().speakerMode;
                return false;
            }
        }

        OutputMode = outputMode;
        if (save)
        {
            PlayerPrefs.SetInt(PrefsPrefix + "OutputMode", (int)OutputMode);
            PlayerPrefs.Save();
        }

        return true;
    }

    public static void ApplySceneProfile(SceneAudioProfile profile)
    {
        if (instance == null || profile == null)
        {
            return;
        }

        instance.SetLoop(instance.musicSource, profile.MusicClip, profile.LoopMusic);
        instance.SetLoop(instance.ambientSource, profile.AmbientClip, profile.LoopAmbient);
    }

    private void LoadSavedVolumes()
    {
        if (PlayerPrefs.GetInt(PrefsPrefix + "Saved", 0) == 1)
        {
            MasterVolume = PlayerPrefs.GetFloat(PrefsPrefix + "Master", MasterVolume);
            MusicVolume = PlayerPrefs.GetFloat(PrefsPrefix + "Music", MusicVolume);
            SfxVolume = PlayerPrefs.GetFloat(PrefsPrefix + "SFX", SfxVolume);
            AmbientVolume = PlayerPrefs.GetFloat(PrefsPrefix + "Ambient", AmbientVolume);
        }

        AudioSpeakerMode savedOutputMode = (AudioSpeakerMode)PlayerPrefs.GetInt(
            PrefsPrefix + "OutputMode",
            (int)AudioSettings.GetConfiguration().speakerMode);
        ApplyOutputMode(savedOutputMode, save: false);

        ApplyVolumes(MasterVolume, MusicVolume, SfxVolume, AmbientVolume, save: false);
    }

    private void OnSceneLoaded(Scene _, LoadSceneMode __)
    {
        SceneAudioProfile profile = FindAnyObjectByType<SceneAudioProfile>();
        if (profile == null)
        {
            SetLoop(musicSource, null, true);
            SetLoop(ambientSource, null, true);
            return;
        }

        ApplySceneProfile(profile);
    }

    private AudioSource CreateLoopSource(string sourceName)
    {
        GameObject sourceObject = new GameObject(sourceName);
        sourceObject.transform.SetParent(transform, false);
        AudioSource source = sourceObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        return source;
    }

    private void SetLoop(AudioSource source, AudioClip clip, bool loop)
    {
        if (source.clip == clip && source.loop == loop)
        {
            if (clip != null && !source.isPlaying)
            {
                source.Play();
            }

            return;
        }

        source.Stop();
        source.clip = clip;
        source.loop = loop;
        if (clip != null)
        {
            source.Play();
        }
    }

    private void RefreshSourceVolumes()
    {
        musicSource.volume = MusicVolume;
        ambientSource.volume = AmbientVolume;
    }
}
