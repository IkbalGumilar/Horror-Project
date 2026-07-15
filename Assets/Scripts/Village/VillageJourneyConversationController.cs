using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class VillageJourneyConversationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private VillageRoadCarMover carMover;
    [SerializeField] private LocalizedConversationData journeyConversation;
    [SerializeField] private SubtitleController subtitleController;

    [Header("Arrival")]
    [SerializeField, Min(0)] private int arrivalControlPointIndex = 6;
    [SerializeField] private string arrivalSpeakerKey = "speaker.player";
    [SerializeField] private string arrivalTextKey = "village.journey.arrival";
    [SerializeField, Min(0f)] private float arrivalLineDuration = 6f;

    [Header("Skip Transition")]
    [SerializeField] private Graphic blackFadeGraphic;
    [SerializeField, Min(0.01f)] private float fadeToBlackDuration = 1f;
    [SerializeField, Min(0.01f)] private float fadeFromBlackDuration = 1.5f;

    private Coroutine conversationRoutine;
    private Coroutine arrivalRoutine;
    private bool arrivalLineStarted;

    private void Awake()
    {
        if (carMover == null)
        {
            carMover = GetComponent<VillageRoadCarMover>();
        }
    }

    private void Start()
    {
        if (subtitleController == null)
        {
            subtitleController = SubtitleController.Instance;
        }

        conversationRoutine = StartCoroutine(PlayJourneyConversation());
        arrivalRoutine = StartCoroutine(WaitForArrival());
    }

    private void OnDestroy()
    {
        subtitleController?.EndSkippableSequence(this);
    }

    private IEnumerator PlayJourneyConversation()
    {
        while (VillageSceneEntryFade.IsTransitioning)
        {
            yield return null;
        }

        LocalizedConversationLine[] lines = journeyConversation != null
            ? journeyConversation.Lines
            : null;
        if (subtitleController == null || lines == null)
        {
            Debug.LogError(
                $"{nameof(VillageJourneyConversationController)} requires a journey conversation and SubtitleController.",
                this);
            conversationRoutine = null;
            yield break;
        }

        subtitleController.BeginSkippableSequence(this, SkipJourneyConversation);
        for (int i = 0; i < lines.Length; i++)
        {
            LocalizedConversationLine line = lines[i];
            if (line == null || string.IsNullOrWhiteSpace(line.TextKey))
            {
                continue;
            }

            float duration = Mathf.Max(0f, line.Duration);
            subtitleController.ShowLocalized(line.SpeakerKey, line.TextKey, duration);
            if (duration > 0f)
            {
                yield return new WaitForSecondsRealtime(duration);
            }
        }

        subtitleController.EndSkippableSequence(this);
        subtitleController.Hide();
        conversationRoutine = null;
    }

    private void SkipJourneyConversation()
    {
        if (conversationRoutine != null)
        {
            StopCoroutine(conversationRoutine);
            conversationRoutine = null;
        }

        if (arrivalRoutine != null)
        {
            StopCoroutine(arrivalRoutine);
            arrivalRoutine = null;
        }

        subtitleController?.EndSkippableSequence(this);
        subtitleController?.Hide();
        conversationRoutine = StartCoroutine(SkipToArrival());
    }

    private IEnumerator SkipToArrival()
    {
        if (blackFadeGraphic == null)
        {
            Debug.LogWarning(
                $"{nameof(VillageJourneyConversationController)} cannot hide the journey skip because no Safe Area fade Graphic is assigned.",
                this);
        }
        else
        {
            yield return FadeGraphicAlpha(
                blackFadeGraphic,
                blackFadeGraphic.color.a,
                1f,
                fadeToBlackDuration);
        }

        if (carMover == null || !carMover.TryTeleportToRoutePoint(arrivalControlPointIndex))
        {
            Debug.LogWarning(
                $"{nameof(VillageJourneyConversationController)} could not skip the car to village route point {arrivalControlPointIndex}.",
                this);
        }

        if (blackFadeGraphic != null)
        {
            yield return FadeGraphicAlpha(
                blackFadeGraphic,
                blackFadeGraphic.color.a,
                0f,
                fadeFromBlackDuration);
        }

        conversationRoutine = null;
        ShowArrivalLine();
    }

    private IEnumerator WaitForArrival()
    {
        while (carMover != null && !carMover.HasPassedRoutePoint(arrivalControlPointIndex))
        {
            yield return null;
        }

        if (conversationRoutine != null)
        {
            StopCoroutine(conversationRoutine);
            conversationRoutine = null;
        }

        arrivalRoutine = null;
        ShowArrivalLine();
    }

    private void ShowArrivalLine()
    {
        if (arrivalLineStarted)
        {
            return;
        }

        arrivalLineStarted = true;
        subtitleController?.EndSkippableSequence(this);
        if (subtitleController != null)
        {
            subtitleController.ShowLocalized(
                arrivalSpeakerKey,
                arrivalTextKey,
                arrivalLineDuration);
        }
    }

    private static IEnumerator FadeGraphicAlpha(
        Graphic graphic,
        float from,
        float to,
        float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetGraphicAlpha(graphic, Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration)));
            yield return null;
        }

        SetGraphicAlpha(graphic, to);
    }

    private static void SetGraphicAlpha(Graphic graphic, float alpha)
    {
        Color color = graphic.color;
        color.a = Mathf.Clamp01(alpha);
        graphic.color = color;
    }
}
