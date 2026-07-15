using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class CityNewGameController : MonoBehaviour
{
    [Header("Menu")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject difficultyPanel;
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button easyButton;
    [SerializeField] private Button normalButton;
    [SerializeField] private Button hardButton;

    [Header("Opening Conversation")]
    [SerializeField] private LocalizedConversationData openingConversation;
    [SerializeField] private LocalizedConversationData roadConversation;
    [SerializeField] private SubtitleController subtitleController;
    [SerializeField, Min(0f)] private float conversationStartDelay = 0.5f;
    [SerializeField, Min(0f)] private float arrivalLineDuration = 4f;

    [Header("Opening Camera")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float cameraTargetWorldY = 10f;
    [SerializeField, Min(0.01f)] private float cameraRiseDuration = 4f;

    [Header("Title And Fade UI")]
    [Tooltip("Assign the title container after it has been added to the Canvas.")]
    [SerializeField] private CanvasGroup titleCanvasGroup;
    [SerializeField, Min(0f)] private float titleFadeDuration = 0.5f;
    [SerializeField, Min(0f)] private float titleVisibleDuration = 5f;
    [Tooltip("Use the full-screen black Image on Safe Area. Only its color alpha is animated.")]
    [SerializeField] private Graphic blackFadeGraphic;
    [SerializeField, Min(0.01f)] private float fadeToBlackDuration = 1f;
    [SerializeField, Min(0f)] private float blackHoldDuration = 0.25f;
    [SerializeField, Min(0.01f)] private float fadeFromBlackDuration = 1.5f;

    [Header("Road Handoff")]
    [SerializeField] private CityRoadVehicleMover vehicleMover;
    [SerializeField] private CityProceduralRoad gameplayRoad;
    [SerializeField, Min(1)] private int gameplayRoadExitControlPoint = 1;
    [SerializeField] private CityProceduralRoad gasStationRoad;
    [SerializeField, Min(1)] private int gasStationRoadExitControlPoint = 1;
    [SerializeField] private Transform[] gasStationWaypoints;
    [Tooltip("One-based gas station waypoint number used when the road conversation is skipped.")]
    [SerializeField, Min(1)] private int roadSkipWaypointNumber = 2;
    [SerializeField, Min(0.01f)] private float gameplayTurnSpeed = 10f;
    [SerializeField, Min(0.01f)] private float gasStationTurnSpeed = 5f;
    [SerializeField, Min(0.01f)] private float finalStopDeceleration = 2f;

    private Coroutine conversationRoutine;
    private Coroutine roadConversationRoutine;
    private bool gameStarted;

    private void Awake()
    {
        if (difficultyPanel != null)
        {
            difficultyPanel.SetActive(false);
        }

        SetCanvasGroupImmediate(titleCanvasGroup, 0f);
        SetGraphicAlphaImmediate(blackFadeGraphic, 0f);

        newGameButton?.onClick.AddListener(OpenDifficultySelection);
        easyButton?.onClick.AddListener(StartEasyGame);
        normalButton?.onClick.AddListener(StartNormalGame);
        hardButton?.onClick.AddListener(StartHardGame);
    }

    private void OnDestroy()
    {
        subtitleController?.EndSkippableSequence(this);
        if (roadConversationRoutine != null)
        {
            StopCoroutine(roadConversationRoutine);
        }
        newGameButton?.onClick.RemoveListener(OpenDifficultySelection);
        easyButton?.onClick.RemoveListener(StartEasyGame);
        normalButton?.onClick.RemoveListener(StartNormalGame);
        hardButton?.onClick.RemoveListener(StartHardGame);
    }

    private void OpenDifficultySelection()
    {
        if (gameStarted || difficultyPanel == null)
        {
            return;
        }

        mainMenuPanel?.SetActive(false);
        difficultyPanel.SetActive(true);
        SelectButton(easyButton);
    }

    private void StartEasyGame()
    {
        BeginGame(GameplayDifficulty.Easy);
    }

    private void StartNormalGame()
    {
        BeginGame(GameplayDifficulty.Normal);
    }

    private void StartHardGame()
    {
        BeginGame(GameplayDifficulty.Hard);
    }

    private void BeginGame(GameplayDifficulty difficulty)
    {
        if (gameStarted)
        {
            return;
        }

        gameStarted = true;
        GameGameplaySettings.Apply(
            difficulty,
            GameGameplaySettings.CrosshairEnabled,
            GameGameplaySettings.TutorialEnabled,
            GameGameplaySettings.AutosaveEnabled,
            GameGameplaySettings.CameraShakeEnabled,
            GameGameplaySettings.SubtitleBackgroundEnabled,
            GameGameplaySettings.HeadBobIntensity,
            false);

        mainMenuPanel?.SetActive(false);
        difficultyPanel?.SetActive(false);
        EventSystem.current?.SetSelectedGameObject(null);
        conversationRoutine = StartCoroutine(PlayOpeningConversation());
    }

    private IEnumerator PlayOpeningConversation()
    {
        if (conversationStartDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(conversationStartDelay);
        }

        if (subtitleController == null)
        {
            Debug.LogError(
                $"{nameof(CityNewGameController)} requires a scene SubtitleController. No subtitle UI will be created automatically.",
                this);
            conversationRoutine = null;
            yield break;
        }

        LocalizedConversationLine[] lines = openingConversation != null
            ? openingConversation.Lines
            : null;
        if (subtitleController == null || lines == null)
        {
            conversationRoutine = null;
            yield break;
        }

        subtitleController.BeginSkippableSequence(this, SkipOpeningConversation);

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
        yield return PlayOpeningTransition();
        conversationRoutine = null;
    }

    private void SkipOpeningConversation()
    {
        if (conversationRoutine != null)
        {
            StopCoroutine(conversationRoutine);
        }

        subtitleController?.EndSkippableSequence(this);
        subtitleController?.Hide();
        conversationRoutine = StartCoroutine(PlayOpeningTransition());
    }

    private IEnumerator PlayOpeningTransition()
    {
        if (playerTransform == null || cameraTransform == null)
        {
            Debug.LogError(
                $"{nameof(CityNewGameController)} requires Player and Camera references for the opening transition.",
                this);
            yield break;
        }

        Transform originalCameraParent = cameraTransform.parent;
        Vector3 originalCameraLocalPosition = cameraTransform.localPosition;
        Quaternion originalCameraLocalRotation = cameraTransform.localRotation;

        cameraTransform.SetParent(null, true);
        Vector3 riseStart = cameraTransform.position;
        Vector3 riseTarget = new Vector3(riseStart.x, cameraTargetWorldY, riseStart.z);
        yield return MoveCamera(riseStart, riseTarget, cameraRiseDuration);

        if (titleCanvasGroup != null)
        {
            yield return FadeCanvasGroup(titleCanvasGroup, 0f, 1f, titleFadeDuration);
            if (titleVisibleDuration > 0f)
            {
                yield return new WaitForSecondsRealtime(titleVisibleDuration);
            }
        }
        if (blackFadeGraphic == null)
        {
            Debug.LogWarning(
                $"{nameof(CityNewGameController)} requires the Safe Area Image as its black fade Graphic. The road handoff was skipped.",
                this);
            RestoreCamera(
                originalCameraParent,
                originalCameraLocalPosition,
                originalCameraLocalRotation);
            yield break;
        }

        yield return FadeGraphicAlpha(blackFadeGraphic, 0f, 1f, fadeToBlackDuration);
        SetCanvasGroupImmediate(titleCanvasGroup, 0f);

        RestoreCamera(
            originalCameraParent,
            originalCameraLocalPosition,
            originalCameraLocalRotation);

        if (vehicleMover == null || gameplayRoad == null || gasStationRoad == null)
        {
            Debug.LogError(
                $"{nameof(CityNewGameController)} requires the Car mover, Gameplay Road, and Gas Station Road for the road handoff.",
                this);
            yield return FadeGraphicAlpha(blackFadeGraphic, 1f, 0f, fadeFromBlackDuration);
            yield break;
        }

        if (!vehicleMover.SwitchToConnectedRoadAndWaypoints(
                gameplayRoad,
                gameplayRoadExitControlPoint,
                gasStationRoad,
                gasStationRoadExitControlPoint,
                gasStationWaypoints,
                gameplayTurnSpeed,
                gasStationTurnSpeed,
                finalStopDeceleration,
                shouldLoop: false,
                placeAtStart: true,
                startMoving: true))
        {
            Debug.LogError("The car could not build the connected route to the gas station.", this);
            yield return FadeGraphicAlpha(blackFadeGraphic, 1f, 0f, fadeFromBlackDuration);
            yield break;
        }

        if (blackHoldDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(blackHoldDuration);
        }

        yield return FadeGraphicAlpha(blackFadeGraphic, 1f, 0f, fadeFromBlackDuration);
        roadConversationRoutine = StartCoroutine(PlayRoadConversation());
    }

    private IEnumerator PlayRoadConversation()
    {
        LocalizedConversationLine[] lines = roadConversation != null
            ? roadConversation.Lines
            : null;

        if (subtitleController != null && lines != null)
        {
            subtitleController.BeginSkippableSequence(this, SkipRoadConversation);
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
        }

        yield return WaitForRoadArrivalAndReaction();
        roadConversationRoutine = null;
    }

    private void SkipRoadConversation()
    {
        if (roadConversationRoutine != null)
        {
            StopCoroutine(roadConversationRoutine);
        }

        subtitleController?.EndSkippableSequence(this);
        subtitleController?.Hide();
        roadConversationRoutine = StartCoroutine(SkipRoadConversationTransition());
    }

    private IEnumerator SkipRoadConversationTransition()
    {
        if (blackFadeGraphic != null)
        {
            yield return FadeGraphicAlpha(
                blackFadeGraphic,
                blackFadeGraphic.color.a,
                1f,
                fadeToBlackDuration);
        }

        int waypointIndex = Mathf.Max(1, roadSkipWaypointNumber) - 1;
        if (vehicleMover == null || !vehicleMover.TryTeleportToDirectWaypoint(waypointIndex))
        {
            Debug.LogWarning(
                $"{nameof(CityNewGameController)} could not skip the car to gas station waypoint {roadSkipWaypointNumber}.",
                this);
        }

        if (blackHoldDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(blackHoldDuration);
        }

        if (blackFadeGraphic != null)
        {
            yield return FadeGraphicAlpha(
                blackFadeGraphic,
                blackFadeGraphic.color.a,
                0f,
                fadeFromBlackDuration);
        }

        yield return WaitForRoadArrivalAndReaction();
    }

    private IEnumerator WaitForRoadArrivalAndReaction()
    {
        while (vehicleMover != null && !vehicleMover.HasFinished)
        {
            yield return null;
        }

        if (subtitleController != null)
        {
            subtitleController.ShowLocalized(
                "speaker.player",
                "city.road.arrival",
                arrivalLineDuration);
        }

        roadConversationRoutine = null;
    }

    private void RestoreCamera(
        Transform originalParent,
        Vector3 originalLocalPosition,
        Quaternion originalLocalRotation)
    {
        cameraTransform.SetParent(originalParent != null ? originalParent : playerTransform, false);
        cameraTransform.localPosition = originalLocalPosition;
        cameraTransform.localRotation = originalLocalRotation;
    }

    private IEnumerator MoveCamera(Vector3 start, Vector3 target, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float smoothProgress = progress * progress * (3f - (2f * progress));
            cameraTransform.position = Vector3.LerpUnclamped(start, target, smoothProgress);
            yield return null;
        }

        cameraTransform.position = target;
    }

    private static IEnumerator FadeCanvasGroup(
        CanvasGroup canvasGroup,
        float from,
        float to,
        float duration)
    {
        if (canvasGroup == null)
        {
            yield break;
        }

        canvasGroup.gameObject.SetActive(true);
        canvasGroup.alpha = from;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        canvasGroup.alpha = to;
        canvasGroup.blocksRaycasts = to > 0.99f;
    }

    private static IEnumerator FadeGraphicAlpha(
        Graphic graphic,
        float from,
        float to,
        float duration)
    {
        if (graphic == null)
        {
            yield break;
        }

        SetGraphicAlphaImmediate(graphic, from);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetGraphicAlphaImmediate(
                graphic,
                Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration)));
            yield return null;
        }

        SetGraphicAlphaImmediate(graphic, to);
    }

    private static void SetCanvasGroupImmediate(CanvasGroup canvasGroup, float alpha)
    {
        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.alpha = alpha;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = alpha > 0.99f;
    }

    private static void SetGraphicAlphaImmediate(Graphic graphic, float alpha)
    {
        if (graphic == null)
        {
            return;
        }

        Color color = graphic.color;
        color.a = Mathf.Clamp01(alpha);
        graphic.color = color;
    }

    private static void SelectButton(Button button)
    {
        if (button != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(button.gameObject);
        }
    }
}
