using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class HUDFadeAfterShow : MonoBehaviour
{
    [SerializeField] private Graphic[] graphics;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private bool hideOnAwake = true;
    [SerializeField] private float visibleAlpha = 1f;
    [SerializeField] private float hiddenAlpha = 0f;
    [SerializeField] private float delayBeforeFade = 1f;
    [SerializeField] private float fadeDuration = 0.25f;
    [SerializeField] private bool useUnscaledTime;

    private Coroutine fadeRoutine;

    private void Reset()
    {
        CollectGraphics();
    }

    private void Awake()
    {
        if ((graphics == null || graphics.Length == 0) && canvasGroup == null)
        {
            CollectGraphics();
        }

        if (hideOnAwake)
        {
            SetAlpha(hiddenAlpha);
        }
    }

    public void Show()
    {
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
        }

        SetAlpha(visibleAlpha);
        fadeRoutine = StartCoroutine(FadeAfterDelay());
    }

    public void HideImmediate()
    {
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        SetAlpha(hiddenAlpha);
    }

    private IEnumerator FadeAfterDelay()
    {
        float delayTimer = 0f;
        while (delayTimer < delayBeforeFade)
        {
            delayTimer += GetDeltaTime();
            yield return null;
        }

        float duration = Mathf.Max(0.001f, fadeDuration);
        float fadeTimer = 0f;
        while (fadeTimer < duration)
        {
            fadeTimer += GetDeltaTime();
            float t = Mathf.Clamp01(fadeTimer / duration);
            t = t * t * (3f - 2f * t);
            SetAlpha(Mathf.Lerp(visibleAlpha, hiddenAlpha, t));
            yield return null;
        }

        SetAlpha(hiddenAlpha);
        fadeRoutine = null;
    }

    private float GetDeltaTime()
    {
        return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    }

    private void SetAlpha(float alpha)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = alpha;
        }

        if (graphics == null)
        {
            return;
        }

        for (int i = 0; i < graphics.Length; i++)
        {
            Graphic graphic = graphics[i];
            if (graphic == null)
            {
                continue;
            }

            Color color = graphic.color;
            color.a = alpha;
            graphic.color = color;
        }
    }

    private void CollectGraphics()
    {
        graphics = GetComponentsInChildren<Graphic>(true);
        canvasGroup = GetComponent<CanvasGroup>();
    }
}
