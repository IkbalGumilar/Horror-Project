using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Graphic))]
public sealed class VillageSceneEntryFade : MonoBehaviour
{
    [SerializeField] private Graphic fadeGraphic;
    [SerializeField] private GameObject loadingTextRoot;
    [SerializeField] private bool runWhenSceneStartsDirectly = true;
    [SerializeField, Min(0f)] private float minimumLoadingDisplayDuration = 0.5f;
    [SerializeField, Min(0.01f)] private float fadeFromBlackDuration = 1.5f;

    private static bool transitionPending;

    public static bool IsTransitioning { get; private set; }

    public static void PrepareForSceneLoad()
    {
        transitionPending = true;
    }

    private void Awake()
    {
        if (fadeGraphic == null)
        {
            fadeGraphic = GetComponent<Graphic>();
        }

        bool shouldRun = transitionPending || runWhenSceneStartsDirectly;
        transitionPending = false;
        if (!shouldRun || fadeGraphic == null)
        {
            return;
        }

        SetBlackAlpha(1f);
        IsTransitioning = true;
        loadingTextRoot?.SetActive(true);
        StartCoroutine(CompleteLoadingAndFade());
    }

    private IEnumerator CompleteLoadingAndFade()
    {
        yield return null;

        if (minimumLoadingDisplayDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(minimumLoadingDisplayDuration);
        }

        loadingTextRoot?.SetActive(false);

        float elapsed = 0f;
        while (elapsed < fadeFromBlackDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetBlackAlpha(1f - Mathf.Clamp01(elapsed / fadeFromBlackDuration));
            yield return null;
        }

        SetBlackAlpha(0f);
        IsTransitioning = false;
    }

    private void SetBlackAlpha(float alpha)
    {
        fadeGraphic.color = new Color(0f, 0f, 0f, Mathf.Clamp01(alpha));
    }
}
