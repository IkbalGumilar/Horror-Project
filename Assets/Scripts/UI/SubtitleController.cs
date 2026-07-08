using System.Collections;
using TMPro;
using UnityEngine;

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

    [Header("Timing")]
    [SerializeField] private float defaultDuration = 3.5f;
    [SerializeField] private float fadeDuration = 0.2f;
    [SerializeField] private bool hideOnAwake = true;

    private Coroutine activeRoutine;
    private string currentSpeakerKey;
    private string currentTextKey;
    private string currentSpeakerText;
    private string currentSubtitleText;
    private SubtitleSource currentSource;
    private float defaultSpeakerFontSize;
    private float defaultSubtitleFontSize;
    private float currentSubtitleFontScale = 1f;

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

        if (hideOnAwake)
        {
            HideImmediate();
        }
    }

    private void OnEnable()
    {
        LocalizationManager.LanguageChanged += RefreshLocalizedText;
    }

    private void OnDisable()
    {
        LocalizationManager.LanguageChanged -= RefreshLocalizedText;
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
        if (speakerNameText != null)
        {
            if (defaultSpeakerFontSize > 0f)
            {
                speakerNameText.fontSize = defaultSpeakerFontSize;
            }

            speakerNameText.text = speakerName;
        }

        if (subtitleText != null)
        {
            if (defaultSubtitleFontSize > 0f)
            {
                subtitleText.fontSize = defaultSubtitleFontSize * currentSubtitleFontScale;
            }

            subtitleText.text = text;
        }
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
}
