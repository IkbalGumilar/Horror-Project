using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class CityCarDepartureController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CityRoadVehicleMover carMover;
    [SerializeField] private CityCarExitController carExitController;
    [SerializeField] private WorldInteractable carInteractable;

    [Header("Entry")]
    [SerializeField] private Vector3 seatLocalPosition = new Vector3(0f, 0.67f, 0.947f);
    [SerializeField] private Vector3 seatLocalEulerAngles;
    [SerializeField, Min(0.01f)] private float entryDuration = 1.25f;

    [Header("Departure Route")]
    [SerializeField] private Transform[] exitWaypoints;
    [SerializeField] private CityProceduralRoad dirtRoad;
    [SerializeField] private CityProceduralRoad mainRoad;
    [SerializeField, Min(0)] private int mainRoadStartControlPoint = 2;
    [SerializeField, Min(0.01f)] private float waypointMaxSpeed = 5f;
    [SerializeField, Min(0.01f)] private float dirtMaxSpeed = 10f;
    [SerializeField, Min(0.01f)] private float mainRoadSpeed = 15f;

    [Header("Departure Conversation")]
    [SerializeField] private LocalizedConversationData departureConversation;
    [SerializeField] private SubtitleController subtitleController;

    [Header("Village Transition")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Graphic blackFadeGraphic;
    [SerializeField] private float cameraTargetWorldY = 10f;
    [SerializeField, Min(0.01f)] private float cameraRiseDuration = 4f;
    [SerializeField, Min(0f)] private float cameraTopHoldDuration = 2f;
    [SerializeField, Min(0.01f)] private float fadeToBlackDuration = 1f;
    [SerializeField] private string villageSceneName = "VilageScene";

    private bool purchaseConfirmed;
    private bool enteringCar;
    private bool hasCapturedSeatPose;
    private Vector3 capturedSeatLocalPosition;
    private Quaternion capturedSeatLocalRotation;
    private Coroutine departureSequenceRoutine;

    private void Awake()
    {
        if (carMover == null)
        {
            carMover = GetComponent<CityRoadVehicleMover>();
        }

        if (carExitController == null)
        {
            carExitController = GetComponent<CityCarExitController>();
        }

        if (carInteractable == null)
        {
            carInteractable = GetComponent<WorldInteractable>();
        }

        Transform playerRoot = carExitController != null ? carExitController.PlayerRoot : null;
        if (playerRoot != null && playerRoot.parent == transform)
        {
            capturedSeatLocalPosition = playerRoot.localPosition;
            capturedSeatLocalRotation = playerRoot.localRotation;
            hasCapturedSeatPose = true;
        }

        if (carInteractable != null)
        {
            carInteractable.Triggered += BeginCarEntry;
            carInteractable.enabled = false;
        }
    }

    private void OnDestroy()
    {
        subtitleController?.EndSkippableSequence(this);

        if (carInteractable != null)
        {
            carInteractable.Triggered -= BeginCarEntry;
        }
    }

    public void UnlockAfterPurchase()
    {
        if (purchaseConfirmed || enteringCar)
        {
            return;
        }

        purchaseConfirmed = true;
        if (carInteractable != null)
        {
            carInteractable.enabled = true;
            carInteractable.ResetInteraction();
        }
    }

    private void BeginCarEntry()
    {
        if (!purchaseConfirmed || enteringCar)
        {
            return;
        }

        enteringCar = true;
        if (carInteractable != null)
        {
            carInteractable.enabled = false;
        }

        StartCoroutine(EnterAndDepart());
    }

    private IEnumerator EnterAndDepart()
    {
        Transform playerRoot = carExitController != null ? carExitController.PlayerRoot : null;
        if (playerRoot == null || carMover == null)
        {
            enteringCar = false;
            yield break;
        }

        carExitController.SetPlayerControlEnabled(false);

        Vector3 targetLocalPosition = hasCapturedSeatPose
            ? capturedSeatLocalPosition
            : seatLocalPosition;
        Quaternion targetLocalRotation = hasCapturedSeatPose
            ? capturedSeatLocalRotation
            : Quaternion.Euler(seatLocalEulerAngles);
        Vector3 startPosition = playerRoot.position;
        Quaternion startRotation = playerRoot.rotation;
        Vector3 targetPosition = transform.TransformPoint(targetLocalPosition);
        Quaternion targetRotation = transform.rotation * targetLocalRotation;
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, entryDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float smoothProgress = progress * progress * (3f - (2f * progress));
            playerRoot.SetPositionAndRotation(
                Vector3.Lerp(startPosition, targetPosition, smoothProgress),
                Quaternion.Slerp(startRotation, targetRotation, smoothProgress));
            yield return null;
        }

        playerRoot.SetParent(transform, false);
        playerRoot.localPosition = targetLocalPosition;
        playerRoot.localRotation = targetLocalRotation;

        if (!carMover.SwitchToDepartureRoute(
                exitWaypoints,
                dirtRoad,
                mainRoad,
                mainRoadStartControlPoint,
                waypointMaxSpeed,
                dirtMaxSpeed,
                mainRoadSpeed))
        {
            Debug.LogError("The car could not build its departure route from the gas station.", this);
            carExitController.SetPlayerControlEnabled(true);
            playerRoot.SetParent(null, true);
            enteringCar = false;
            yield break;
        }

        departureSequenceRoutine = StartCoroutine(PlayDepartureSequence());
    }

    private IEnumerator PlayDepartureSequence()
    {
        LocalizedConversationLine[] lines = departureConversation != null
            ? departureConversation.Lines
            : null;

        if (subtitleController == null || lines == null)
        {
            Debug.LogError(
                $"{nameof(CityCarDepartureController)} requires the departure conversation and SubtitleController.",
                this);
        }
        else
        {
            subtitleController.BeginSkippableSequence(this, SkipDepartureConversation);

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

        yield return TransitionToVillage();
        departureSequenceRoutine = null;
    }

    private void SkipDepartureConversation()
    {
        if (departureSequenceRoutine != null)
        {
            StopCoroutine(departureSequenceRoutine);
        }

        subtitleController?.EndSkippableSequence(this);
        subtitleController?.Hide();
        departureSequenceRoutine = StartCoroutine(TransitionToVillage());
    }

    private IEnumerator TransitionToVillage()
    {
        if (cameraTransform == null || blackFadeGraphic == null)
        {
            Debug.LogError(
                $"{nameof(CityCarDepartureController)} requires Camera and Safe Area fade references.",
                this);
            departureSequenceRoutine = null;
            yield break;
        }

        cameraTransform.SetParent(null, true);
        Vector3 riseStart = cameraTransform.position;
        Vector3 riseTarget = new Vector3(riseStart.x, cameraTargetWorldY, riseStart.z);
        yield return MoveCamera(riseStart, riseTarget, cameraRiseDuration);

        if (cameraTopHoldDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(cameraTopHoldDuration);
        }

        float startAlpha = blackFadeGraphic.color.a;
        yield return FadeGraphicAlpha(
            blackFadeGraphic,
            startAlpha,
            1f,
            fadeToBlackDuration);

        if (string.IsNullOrWhiteSpace(villageSceneName) ||
            !Application.CanStreamedLevelBeLoaded(villageSceneName))
        {
            Debug.LogError(
                $"Scene '{villageSceneName}' is not available in Build Settings.",
                this);
            departureSequenceRoutine = null;
            yield break;
        }

        VillageSceneEntryFade.PrepareForSceneLoad();
        SceneManager.LoadScene(villageSceneName);
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
