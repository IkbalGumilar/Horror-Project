using System.Collections;
using UnityEngine;

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

    private Coroutine conversationRoutine;
    private Coroutine arrivalRoutine;

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

        subtitleController?.EndSkippableSequence(this);
        subtitleController?.Hide();
    }

    private IEnumerator WaitForArrival()
    {
        while (carMover != null && !carMover.HasPassedRoutePoint(arrivalControlPointIndex))
        {
            yield return null;
        }

        if (subtitleController != null)
        {
            subtitleController.ShowLocalized(
                arrivalSpeakerKey,
                arrivalTextKey,
                arrivalLineDuration);
        }

        arrivalRoutine = null;
    }
}
