using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public enum UIUXEase
{
    Linear,
    SmoothStep,
    EaseIn,
    EaseOut,
    EaseInOut,
    BackOut
}

[DisallowMultipleComponent]
public sealed class UIUXAnimator : MonoBehaviour
{
    public Coroutine PlayHorizontalSwap(
        RectTransform entering,
        RectTransform leaving,
        Vector2 enteringHome,
        Vector2 leavingHome,
        bool enteringFromLeft,
        float distance,
        float duration,
        Action onBeforeMove,
        Action onComplete)
    {
        return StartCoroutine(AnimateHorizontalSwap(
            entering,
            leaving,
            enteringHome,
            leavingHome,
            enteringFromLeft,
            distance,
            duration,
            onBeforeMove,
            onComplete));
    }

    public Coroutine PlayTabSwipe(
        RectTransform currentPanel,
        RectTransform nextPanel,
        Vector2 currentHome,
        Vector2 nextHome,
        bool nextIsToRight,
        float distance,
        float duration,
        Action onBeforeMove,
        Action onComplete)
    {
        return StartCoroutine(AnimateTabSwipe(
            currentPanel,
            nextPanel,
            currentHome,
            nextHome,
            nextIsToRight,
            distance,
            duration,
            onBeforeMove,
            onComplete));
    }

    public Coroutine PlayAnchoredMove(
        RectTransform target,
        Vector2 from,
        Vector2 to,
        float duration,
        Action onBeforeMove = null,
        Action onComplete = null,
        UIUXEase ease = UIUXEase.SmoothStep)
    {
        return StartCoroutine(AnimateValue(
            duration,
            ease,
            value => target.anchoredPosition = Vector2.LerpUnclamped(from, to, value),
            onBeforeMove,
            onComplete));
    }

    public Coroutine PlayAnchoredMoveTo(
        RectTransform target,
        Vector2 to,
        float duration,
        Action onBeforeMove = null,
        Action onComplete = null,
        UIUXEase ease = UIUXEase.SmoothStep)
    {
        return PlayAnchoredMove(target, target.anchoredPosition, to, duration, onBeforeMove, onComplete, ease);
    }

    public Coroutine PlayFade(
        CanvasGroup target,
        float from,
        float to,
        float duration,
        Action onBeforeMove = null,
        Action onComplete = null,
        UIUXEase ease = UIUXEase.SmoothStep)
    {
        return StartCoroutine(AnimateValue(
            duration,
            ease,
            value => target.alpha = Mathf.LerpUnclamped(from, to, value),
            onBeforeMove,
            onComplete));
    }

    public Coroutine PlayFadeTo(
        CanvasGroup target,
        float to,
        float duration,
        Action onBeforeMove = null,
        Action onComplete = null,
        UIUXEase ease = UIUXEase.SmoothStep)
    {
        return PlayFade(target, target.alpha, to, duration, onBeforeMove, onComplete, ease);
    }

    public Coroutine PlayCanvasGroupVisibility(
        CanvasGroup target,
        bool visible,
        float duration,
        bool blockRaycastsWhenVisible,
        Action onComplete = null,
        UIUXEase ease = UIUXEase.SmoothStep)
    {
        target.gameObject.SetActive(true);
        target.interactable = visible;
        target.blocksRaycasts = visible && blockRaycastsWhenVisible;

        return PlayFadeTo(
            target,
            visible ? 1f : 0f,
            duration,
            null,
            () =>
            {
                target.interactable = visible;
                target.blocksRaycasts = visible && blockRaycastsWhenVisible;
                target.gameObject.SetActive(visible);
                onComplete?.Invoke();
            },
            ease);
    }

    public Coroutine PlayGraphicFade(
        Graphic target,
        float from,
        float to,
        float duration,
        Action onBeforeMove = null,
        Action onComplete = null,
        UIUXEase ease = UIUXEase.SmoothStep)
    {
        return StartCoroutine(AnimateValue(
            duration,
            ease,
            value =>
            {
                Color color = target.color;
                color.a = Mathf.LerpUnclamped(from, to, value);
                target.color = color;
            },
            onBeforeMove,
            onComplete));
    }

    public Coroutine PlayGraphicFadeTo(
        Graphic target,
        float to,
        float duration,
        Action onBeforeMove = null,
        Action onComplete = null,
        UIUXEase ease = UIUXEase.SmoothStep)
    {
        return PlayGraphicFade(target, target.color.a, to, duration, onBeforeMove, onComplete, ease);
    }

    public Coroutine PlayGraphicColor(
        Graphic target,
        Color from,
        Color to,
        float duration,
        Action onBeforeMove = null,
        Action onComplete = null,
        UIUXEase ease = UIUXEase.SmoothStep)
    {
        return StartCoroutine(AnimateValue(
            duration,
            ease,
            value => target.color = Color.LerpUnclamped(from, to, value),
            onBeforeMove,
            onComplete));
    }

    public Coroutine PlayGraphicColorTo(
        Graphic target,
        Color to,
        float duration,
        Action onBeforeMove = null,
        Action onComplete = null,
        UIUXEase ease = UIUXEase.SmoothStep)
    {
        return PlayGraphicColor(target, target.color, to, duration, onBeforeMove, onComplete, ease);
    }

    public Coroutine PlayLocalScale(
        Transform target,
        Vector3 from,
        Vector3 to,
        float duration,
        Action onBeforeMove = null,
        Action onComplete = null,
        UIUXEase ease = UIUXEase.SmoothStep)
    {
        return StartCoroutine(AnimateValue(
            duration,
            ease,
            value => target.localScale = Vector3.LerpUnclamped(from, to, value),
            onBeforeMove,
            onComplete));
    }

    public Coroutine PlayLocalScaleTo(
        Transform target,
        Vector3 to,
        float duration,
        Action onBeforeMove = null,
        Action onComplete = null,
        UIUXEase ease = UIUXEase.SmoothStep)
    {
        return PlayLocalScale(target, target.localScale, to, duration, onBeforeMove, onComplete, ease);
    }

    public Coroutine PlayPunchScale(
        Transform target,
        Vector3 baseScale,
        float multiplier,
        float duration,
        Action onBeforeMove = null,
        Action onComplete = null)
    {
        return StartCoroutine(AnimatePunchScale(target, baseScale, multiplier, duration, onBeforeMove, onComplete));
    }

    public Coroutine PlayLocalRotation(
        RectTransform target,
        Vector3 fromEuler,
        Vector3 toEuler,
        float duration,
        Action onBeforeMove = null,
        Action onComplete = null,
        UIUXEase ease = UIUXEase.SmoothStep)
    {
        return StartCoroutine(AnimateValue(
            duration,
            ease,
            value => target.localEulerAngles = Vector3.LerpUnclamped(fromEuler, toEuler, value),
            onBeforeMove,
            onComplete));
    }

    public Coroutine PlayLocalRotationTo(
        RectTransform target,
        Vector3 toEuler,
        float duration,
        Action onBeforeMove = null,
        Action onComplete = null,
        UIUXEase ease = UIUXEase.SmoothStep)
    {
        return PlayLocalRotation(target, target.localEulerAngles, toEuler, duration, onBeforeMove, onComplete, ease);
    }

    public Coroutine PlayShake(
        RectTransform target,
        Vector2 home,
        float strength,
        float duration,
        int frequency,
        Action onBeforeMove = null,
        Action onComplete = null)
    {
        return StartCoroutine(AnimateShake(target, home, strength, duration, frequency, onBeforeMove, onComplete));
    }

    public Coroutine PlayGraphicBlink(
        Graphic target,
        float minAlpha,
        float maxAlpha,
        float duration,
        int cycles,
        Action onBeforeMove = null,
        Action onComplete = null)
    {
        return StartCoroutine(AnimateBlink(
            duration,
            cycles,
            value =>
            {
                Color color = target.color;
                color.a = Mathf.LerpUnclamped(minAlpha, maxAlpha, value);
                target.color = color;
            },
            onBeforeMove,
            onComplete));
    }

    public Coroutine PlayCanvasGroupBlink(
        CanvasGroup target,
        float minAlpha,
        float maxAlpha,
        float duration,
        int cycles,
        Action onBeforeMove = null,
        Action onComplete = null)
    {
        return StartCoroutine(AnimateBlink(
            duration,
            cycles,
            value => target.alpha = Mathf.LerpUnclamped(minAlpha, maxAlpha, value),
            onBeforeMove,
            onComplete));
    }

    public void StopAnimation(Coroutine routine)
    {
        if (routine != null)
        {
            StopCoroutine(routine);
        }
    }

    public static float GetSlideDistance(RectTransform scope)
    {
        float width = scope != null ? scope.rect.width : 0f;
        return Mathf.Max(1f, width > 0f ? width : Screen.width);
    }

    private static IEnumerator AnimateHorizontalSwap(
        RectTransform entering,
        RectTransform leaving,
        Vector2 enteringHome,
        Vector2 leavingHome,
        bool enteringFromLeft,
        float distance,
        float duration,
        Action onBeforeMove,
        Action onComplete)
    {
        float direction = enteringFromLeft ? -1f : 1f;
        Vector2 enteringStart = enteringHome + Vector2.right * direction * distance;
        Vector2 leavingEnd = leavingHome - Vector2.right * direction * distance;

        entering.gameObject.SetActive(true);
        leaving.gameObject.SetActive(true);
        onBeforeMove?.Invoke();

        entering.anchoredPosition = enteringStart;
        leaving.anchoredPosition = leavingHome;

        yield return AnimatePair(
            entering,
            leaving,
            enteringStart,
            enteringHome,
            leavingHome,
            leavingEnd,
            duration);

        entering.anchoredPosition = enteringHome;
        leaving.anchoredPosition = leavingHome;
        leaving.gameObject.SetActive(false);
        onComplete?.Invoke();
    }

    private static IEnumerator AnimateTabSwipe(
        RectTransform currentPanel,
        RectTransform nextPanel,
        Vector2 currentHome,
        Vector2 nextHome,
        bool nextIsToRight,
        float distance,
        float duration,
        Action onBeforeMove,
        Action onComplete)
    {
        float direction = nextIsToRight ? 1f : -1f;
        Vector2 currentEnd = currentHome + Vector2.left * direction * distance;
        Vector2 nextStart = nextHome + Vector2.right * direction * distance;

        currentPanel.gameObject.SetActive(true);
        nextPanel.gameObject.SetActive(true);
        onBeforeMove?.Invoke();

        currentPanel.anchoredPosition = currentHome;
        nextPanel.anchoredPosition = nextStart;

        yield return AnimatePair(
            nextPanel,
            currentPanel,
            nextStart,
            nextHome,
            currentHome,
            currentEnd,
            duration);

        currentPanel.anchoredPosition = currentHome;
        nextPanel.anchoredPosition = nextHome;
        currentPanel.gameObject.SetActive(false);
        nextPanel.gameObject.SetActive(true);
        onComplete?.Invoke();
    }

    private static IEnumerator AnimatePair(
        RectTransform entering,
        RectTransform leaving,
        Vector2 enteringStart,
        Vector2 enteringEnd,
        Vector2 leavingStart,
        Vector2 leavingEnd,
        float duration)
    {
        float elapsed = 0f;
        duration = Mathf.Max(0.01f, duration);
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = Ease(t, UIUXEase.SmoothStep);
            entering.anchoredPosition = Vector2.LerpUnclamped(enteringStart, enteringEnd, eased);
            leaving.anchoredPosition = Vector2.LerpUnclamped(leavingStart, leavingEnd, eased);
            yield return null;
        }
    }

    private static IEnumerator AnimateValue(
        float duration,
        UIUXEase ease,
        Action<float> apply,
        Action onBeforeMove,
        Action onComplete)
    {
        onBeforeMove?.Invoke();
        float elapsed = 0f;
        duration = Mathf.Max(0.01f, duration);
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            apply(Ease(t, ease));
            yield return null;
        }

        apply(1f);
        onComplete?.Invoke();
    }

    private static IEnumerator AnimatePunchScale(
        Transform target,
        Vector3 baseScale,
        float multiplier,
        float duration,
        Action onBeforeMove,
        Action onComplete)
    {
        onBeforeMove?.Invoke();
        Vector3 punchScale = baseScale * Mathf.Max(0f, multiplier);
        float halfDuration = Mathf.Max(0.01f, duration * 0.5f);

        yield return AnimateValue(
            halfDuration,
            UIUXEase.BackOut,
            value => target.localScale = Vector3.LerpUnclamped(baseScale, punchScale, value),
            null,
            null);

        yield return AnimateValue(
            halfDuration,
            UIUXEase.SmoothStep,
            value => target.localScale = Vector3.LerpUnclamped(punchScale, baseScale, value),
            null,
            null);

        target.localScale = baseScale;
        onComplete?.Invoke();
    }

    private static IEnumerator AnimateShake(
        RectTransform target,
        Vector2 home,
        float strength,
        float duration,
        int frequency,
        Action onBeforeMove,
        Action onComplete)
    {
        onBeforeMove?.Invoke();
        float elapsed = 0f;
        duration = Mathf.Max(0.01f, duration);
        frequency = Mathf.Max(1, frequency);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float falloff = 1f - t;
            float angle = elapsed * frequency * Mathf.PI * 2f;
            Vector2 offset = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle * 1.37f)) * strength * falloff;
            target.anchoredPosition = home + offset;
            yield return null;
        }

        target.anchoredPosition = home;
        onComplete?.Invoke();
    }

    private static IEnumerator AnimateBlink(
        float duration,
        int cycles,
        Action<float> apply,
        Action onBeforeMove,
        Action onComplete)
    {
        onBeforeMove?.Invoke();
        float elapsed = 0f;
        duration = Mathf.Max(0.01f, duration);
        cycles = Mathf.Max(1, cycles);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float blink = Mathf.PingPong(t * cycles * 2f, 1f);
            apply(blink);
            yield return null;
        }

        apply(0f);
        onComplete?.Invoke();
    }

    private static float Ease(float value, UIUXEase ease)
    {
        value = Mathf.Clamp01(value);
        switch (ease)
        {
            case UIUXEase.Linear:
                return value;
            case UIUXEase.EaseIn:
                return value * value;
            case UIUXEase.EaseOut:
                return 1f - (1f - value) * (1f - value);
            case UIUXEase.EaseInOut:
                return value < 0.5f ? 2f * value * value : 1f - Mathf.Pow(-2f * value + 2f, 2f) * 0.5f;
            case UIUXEase.BackOut:
                return BackOut(value);
            case UIUXEase.SmoothStep:
            default:
                return SmoothStep(value);
        }
    }

    private static float SmoothStep(float value)
    {
        return value * value * (3f - 2f * value);
    }

    private static float BackOut(float value)
    {
        const float overshoot = 1.70158f;
        float shifted = value - 1f;
        return 1f + (overshoot + 1f) * shifted * shifted * shifted + overshoot * shifted * shifted;
    }
}
