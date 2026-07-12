using System.Collections;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class SubtitleController : MonoBehaviour
{
    private enum SubtitleSource
    {
        RawText,
        LocalizationKeys,
        EnglishLookup
    }

    public static SubtitleController Instance { get; private set; }

    [Header("References")]
    [SerializeField] private LocalizationTable fallbackLocalizationTable;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private TMP_Text subtitleText;
    [SerializeField] private Graphic backgroundGraphic;

    [Header("Accessibility Size Multipliers")]
    [SerializeField] private float verySmallSize = 0.65f;
    [SerializeField] private float smallSize = 0.8f;
    [SerializeField] private float mediumSize = 1f;
    [SerializeField] private float largeSize = 1.25f;
    [SerializeField] private float veryLargeSize = 1.5f;

    [Header("Timing")]
    [SerializeField] private float defaultDuration = 3.5f;
    [SerializeField] private float fadeDuration = 0.2f;
    [SerializeField] private bool hideOnAwake = true;

    [Header("Profanity Filter")]
    [SerializeField] private bool censorProfanity;
    [SerializeField] private string[] censoredWords = { "anjing", "fuck" };

    private Coroutine activeRoutine;
    private string currentSpeakerKey;
    private string currentTextKey;
    private string currentSpeakerText;
    private string currentSubtitleText;
    private SubtitleSource currentSource;
    private float defaultSpeakerFontSize;
    private float defaultSubtitleFontSize;
    private float currentSubtitleFontScale = 1f;
    private float defaultBackgroundAlpha = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        if (speakerNameText != null)
        {
            defaultSpeakerFontSize = speakerNameText.fontSize;
        }

        if (subtitleText != null)
        {
            defaultSubtitleFontSize = subtitleText.fontSize;
        }

        if (backgroundGraphic == null)
        {
            Transform background = FindDescendant(transform, "Background");
            backgroundGraphic = background != null ? background.GetComponent<Graphic>() : null;
        }

        if (backgroundGraphic != null)
        {
            defaultBackgroundAlpha = backgroundGraphic.color.a;
        }

        ApplyAccessibilitySettings();

        if (hideOnAwake)
        {
            HideImmediate();
        }
    }

    private void OnEnable()
    {
        LocalizationManager.LanguageChanged += RefreshLocalizedText;
        GameAccessibilitySettings.Changed += ApplyAccessibilitySettings;
        ApplyAccessibilitySettings();
    }

    private void OnDisable()
    {
        LocalizationManager.LanguageChanged -= RefreshLocalizedText;
        GameAccessibilitySettings.Changed -= ApplyAccessibilitySettings;
    }

    public void Show(string speakerName, string text)
    {
        Show(speakerName, text, defaultDuration);
    }

    public void Show(string speakerName, string text, float duration)
    {
        currentSpeakerText = speakerName;
        currentSubtitleText = text;
        currentSource = SubtitleSource.RawText;
        currentSubtitleFontScale = 1f;
        SetText(speakerName, text);
        StartSubtitleRoutine(duration);
    }

    public void ShowEnglishText(string speakerEnglishText, string subtitleEnglishText)
    {
        ShowEnglishText(speakerEnglishText, subtitleEnglishText, defaultDuration);
    }

    public void ShowEnglishText(string speakerEnglishText, string subtitleEnglishText, float duration)
    {
        currentSpeakerText = speakerEnglishText;
        currentSubtitleText = subtitleEnglishText;
        currentSource = SubtitleSource.EnglishLookup;
        currentSubtitleFontScale = 1f;
        SetText(GetEnglishText(speakerEnglishText), GetEnglishText(subtitleEnglishText));
        StartSubtitleRoutine(duration);
    }

    public void ShowLocalized(string speakerKey, string textKey)
    {
        ShowLocalized(speakerKey, textKey, defaultDuration);
    }

    public void ShowLocalized(string speakerKey, string textKey, float duration)
    {
        ShowLocalized(speakerKey, textKey, duration, 1f);
    }

    public void ShowLocalized(string speakerKey, string textKey, float duration, float subtitleFontScale)
    {
        currentSpeakerKey = speakerKey;
        currentTextKey = textKey;
        currentSource = SubtitleSource.LocalizationKeys;
        currentSubtitleFontScale = Mathf.Max(0.1f, subtitleFontScale);
        SetText(GetLocalizedText(speakerKey), GetLocalizedText(textKey));
        StartSubtitleRoutine(duration);
    }

    public void Hide()
    {
        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
        }

        activeRoutine = StartCoroutine(FadeTo(0f, fadeDuration, true));
    }

    public void HideImmediate()
    {
        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
            activeRoutine = null;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    private void StartSubtitleRoutine(float duration)
    {
        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
        }

        if (!GameAccessibilitySettings.SubtitlesEnabled)
        {
            activeRoutine = null;
            HideImmediate();
            return;
        }

        activeRoutine = StartCoroutine(ShowRoutine(Mathf.Max(0f, duration)));
    }

    private IEnumerator ShowRoutine(float duration)
    {
        yield return FadeTo(1f, fadeDuration, false);

        if (duration > 0f)
        {
            yield return new WaitForSecondsRealtime(duration);
            yield return FadeTo(0f, fadeDuration, true);
        }

        activeRoutine = null;
    }

    private IEnumerator FadeTo(float targetAlpha, float duration, bool disableRaycasts)
    {
        if (canvasGroup == null)
        {
            yield break;
        }

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        float startAlpha = canvasGroup.alpha;
        if (duration <= 0f)
        {
            canvasGroup.alpha = targetAlpha;
        }
        else
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;
        }

        if (disableRaycasts)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    private void SetText(string speakerName, string text)
    {
        float accessibilityScale = GetAccessibilityFontScale();
        if (speakerNameText != null)
        {
            if (defaultSpeakerFontSize > 0f)
            {
                speakerNameText.fontSize = defaultSpeakerFontSize * accessibilityScale;
            }

            speakerNameText.text = speakerName;
            speakerNameText.gameObject.SetActive(GameAccessibilitySettings.SpeakerNameEnabled);
        }

        if (subtitleText != null)
        {
            if (defaultSubtitleFontSize > 0f)
            {
                subtitleText.fontSize = defaultSubtitleFontSize * currentSubtitleFontScale * accessibilityScale;
            }

            subtitleText.text = ApplyProfanityFilter(text);
        }
    }

    private void ApplyAccessibilitySettings()
    {
        if (!GameAccessibilitySettings.SubtitlesEnabled)
        {
            HideImmediate();
        }

        if (speakerNameText != null)
        {
            speakerNameText.gameObject.SetActive(GameAccessibilitySettings.SpeakerNameEnabled);
        }

        if (backgroundGraphic != null)
        {
            Color color = backgroundGraphic.color;
            color.a = GameAccessibilitySettings.SubtitleBackgroundEnabled ? defaultBackgroundAlpha : 0f;
            backgroundGraphic.color = color;
        }

        RefreshLocalizedText();
    }

    private float GetAccessibilityFontScale()
    {
        switch (GameAccessibilitySettings.SubtitleSize)
        {
            case 0: return Mathf.Max(0.01f, verySmallSize);
            case 1: return Mathf.Max(0.01f, smallSize);
            case 3: return Mathf.Max(0.01f, largeSize);
            case 4: return Mathf.Max(0.01f, veryLargeSize);
            default: return Mathf.Max(0.01f, mediumSize);
        }
    }

    private static Transform FindDescendant(Transform root, string objectName)
    {
        Transform[] descendants = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < descendants.Length; i++)
        {
            if (descendants[i].name == objectName)
            {
                return descendants[i];
            }
        }

        return null;
    }

    private void RefreshLocalizedText()
    {
        switch (currentSource)
        {
            case SubtitleSource.LocalizationKeys:
                SetText(GetLocalizedText(currentSpeakerKey), GetLocalizedText(currentTextKey));
                break;
            case SubtitleSource.EnglishLookup:
                SetText(GetEnglishText(currentSpeakerText), GetEnglishText(currentSubtitleText));
                break;
            default:
                SetText(currentSpeakerText, currentSubtitleText);
                break;
        }
    }

    private string GetLocalizedText(string key)
    {
        string text = LocalizationManager.Get(key);
        if (text != key || fallbackLocalizationTable == null)
        {
            return text;
        }

        return fallbackLocalizationTable.GetText(key, GetFallbackLanguage());
    }

    private string GetEnglishText(string englishText)
    {
        string text = LocalizationManager.GetText(englishText);
        if (text != englishText || fallbackLocalizationTable == null)
        {
            return text;
        }

        return fallbackLocalizationTable.GetTextByEnglish(englishText, GetFallbackLanguage());
    }

    private GameLanguage GetFallbackLanguage()
    {
        if (LocalizationManager.Instance != null)
        {
            return LocalizationManager.CurrentLanguage;
        }

        return fallbackLocalizationTable != null ? fallbackLocalizationTable.defaultLanguage : GameLanguage.Indonesian;
    }

    private string ApplyProfanityFilter(string text)
    {
        if (!censorProfanity || string.IsNullOrEmpty(text) || censoredWords == null)
        {
            return text;
        }

        string filteredText = text;
        for (int i = 0; i < censoredWords.Length; i++)
        {
            string word = censoredWords[i];
            if (string.IsNullOrWhiteSpace(word))
            {
                continue;
            }

            string pattern = $@"(?<!\p{{L}}){Regex.Escape(word)}(?!\p{{L}})";
            filteredText = Regex.Replace(
                filteredText,
                pattern,
                match => CensorMatchedWord(match.Value),
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        return filteredText;
    }

    private string CensorMatchedWord(string word)
    {
        if (string.IsNullOrEmpty(word))
        {
            return string.Empty;
        }

        if (word.Length <= 2)
        {
            return new string('*', word.Length);
        }

        if (word.Length == 3)
        {
            return word[0] + new string('*', word.Length - 1);
        }

        if (word.Length == 4)
        {
            return word[0] + new string('*', 2) + word[3];
        }

        int visibleEdgeLength = word.Length >= 6 ? 2 : 1;
        int hiddenLength = Mathf.Max(1, word.Length - visibleEdgeLength * 2);
        return word.Substring(0, visibleEdgeLength)
            + new string('*', hiddenLength)
            + word.Substring(word.Length - visibleEdgeLength, visibleEdgeLength);
    }
}
