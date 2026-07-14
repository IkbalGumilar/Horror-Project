using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class PlayerVillagerInteraction : MonoBehaviour
{
    [Header("Shared Profile")]
    [SerializeField] private FPSControlProfile controlProfile;

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string playerMapName = "Player";
    [SerializeField] private string interactActionName = "Interact";

    [Header("Detection")]
    [SerializeField] private Transform detectionOrigin;
    [SerializeField, Min(0.1f)] private float interactionRadius = 3f;

    [Header("Prompt")]
    [SerializeField] private GameObject promptRoot;
    [SerializeField, Range(0f, 255f)] private float minimumAlpha = 150f;
    [SerializeField, Range(0f, 255f)] private float maximumAlpha = 255f;
    [SerializeField, Min(0.01f)] private float blinkCyclesPerSecond = 2.5f;

    [Header("Conversation Control")]
    [SerializeField] private FPSPlayerController playerController;
    [SerializeField] private FPSCameraController cameraController;
    [SerializeField] private PlayerStamina playerStamina;
    [SerializeField, Min(0f)] private float postConversationDelay = 0.5f;
    [SerializeField, Min(0f)] private float faceNpcTurnSpeed = 360f;
    [SerializeField, Min(0.1f)] private float conversationDistance = 3f;
    [SerializeField, Min(0f)] private float approachMoveSpeed = 2f;
    [SerializeField, Min(0f)] private float distanceTolerance = 0.05f;
    [SerializeField, Range(0f, 45f)] private float facingTolerance = 2f;

    private InputAction interactAction;
    private CanvasGroup promptCanvasGroup;
    private VillagerConversation nearbyVillager;
    private VillagerConversation talkingVillager;
    private Coroutine releaseControlsRoutine;
    private bool preparingConversation;

    private void Awake()
    {
        ApplyControlProfile();
        if (detectionOrigin == null)
        {
            detectionOrigin = transform;
        }

        ResolvePlayerReferences();
        PreparePrompt();
        SetPromptAlpha(0f);
    }

    private void ApplyControlProfile()
    {
        if (controlProfile == null)
        {
            return;
        }

        interactionRadius = controlProfile.InteractionRadius;
        minimumAlpha = controlProfile.InteractionMinimumAlpha;
        maximumAlpha = controlProfile.InteractionMaximumAlpha;
        blinkCyclesPerSecond = controlProfile.InteractionBlinkCyclesPerSecond;
    }

    private void OnEnable()
    {
        ResolvePlayerReferences();
        if (promptCanvasGroup == null)
        {
            PreparePrompt();
        }

        BindInput();
    }

    private void OnDisable()
    {
        if (releaseControlsRoutine != null)
        {
            StopCoroutine(releaseControlsRoutine);
            releaseControlsRoutine = null;
        }

        UnbindInput();
        UnsubscribeFromTalkingVillager();
        SetConversationControlsLocked(false);
        nearbyVillager = null;
        SetPromptAlpha(0f);
    }

    private void Update()
    {
        if (talkingVillager != null)
        {
            if (preparingConversation)
            {
                UpdateConversationAlignment();
            }
            else
            {
                FaceTalkingVillager();
            }

            SetPromptAlpha(0f);
            return;
        }

        nearbyVillager = FindNearestVillager();
        if (nearbyVillager == null)
        {
            SetPromptAlpha(0f);
            return;
        }

        float phase = Mathf.PingPong(Time.unscaledTime * blinkCyclesPerSecond * 2f, 1f);
        SetPromptAlpha(Mathf.Lerp(minimumAlpha, maximumAlpha, phase) / 255f);
    }

    public bool TryInteract()
    {
        if (talkingVillager != null || nearbyVillager == null)
        {
            return false;
        }

        talkingVillager = nearbyVillager;
        preparingConversation = true;
        SetConversationControlsLocked(true);
        nearbyVillager = null;
        SetPromptAlpha(0f);
        return true;
    }

    private VillagerConversation FindNearestVillager()
    {
        Vector3 origin = detectionOrigin != null ? detectionOrigin.position : transform.position;
        return VillagerConversation.FindNearest(origin, interactionRadius);
    }

    private void BindInput()
    {
        ResolveInputActionsReference();
        if (inputActions == null)
        {
            Debug.LogWarning($"{nameof(PlayerVillagerInteraction)} needs an InputActionAsset.", this);
            return;
        }

        InputActionMap playerMap = inputActions.FindActionMap(playerMapName, false);
        interactAction = playerMap != null ? playerMap.FindAction(interactActionName, false) : null;
        if (interactAction == null)
        {
            Debug.LogWarning(
                $"Could not find input action '{playerMapName}/{interactActionName}'.",
                this);
            return;
        }

        interactAction.started += OnInteractStarted;
        interactAction.Enable();
    }

    private void UnbindInput()
    {
        if (interactAction == null)
        {
            return;
        }

        interactAction.started -= OnInteractStarted;
        interactAction.Disable();
        interactAction = null;
    }

    private void OnInteractStarted(InputAction.CallbackContext context)
    {
        TryInteract();
    }

    private void OnConversationCompleted(VillagerConversation conversation)
    {
        if (conversation != talkingVillager)
        {
            return;
        }

        if (releaseControlsRoutine == null)
        {
            releaseControlsRoutine = StartCoroutine(ReleaseControlsAfterDelay());
        }
    }

    private IEnumerator ReleaseControlsAfterDelay()
    {
        if (postConversationDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(postConversationDelay);
        }

        UnsubscribeFromTalkingVillager();
        SetConversationControlsLocked(false);
        playerStamina?.ShowControlReadyIndicator();
        releaseControlsRoutine = null;
    }

    private void SetConversationControlsLocked(bool locked)
    {
        playerController?.SetMovementSuppressed(locked);
        cameraController?.SetLookSuppressed(locked);
    }

    private void UpdateConversationAlignment()
    {
        if (talkingVillager == null)
        {
            CancelConversationPreparation();
            return;
        }

        Vector3 npcPosition = talkingVillager.transform.position;
        Vector3 awayFromNpc = transform.position - npcPosition;
        awayFromNpc.y = 0f;
        if (awayFromNpc.sqrMagnitude <= 0.0001f)
        {
            awayFromNpc = -talkingVillager.transform.forward;
            awayFromNpc.y = 0f;
        }

        Vector3 targetPosition = npcPosition + awayFromNpc.normalized * conversationDistance;
        targetPosition.y = transform.position.y;
        Vector3 nextPosition = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            approachMoveSpeed * Time.unscaledDeltaTime);

        CharacterController characterController = GetComponent<CharacterController>();
        if (characterController != null && characterController.enabled)
        {
            characterController.Move(nextPosition - transform.position);
        }
        else
        {
            transform.position = nextPosition;
        }

        FaceTalkingVillager();

        Vector3 horizontalOffset = transform.position - npcPosition;
        horizontalOffset.y = 0f;
        Vector3 directionToNpc = -horizontalOffset;
        float distanceError = Mathf.Abs(horizontalOffset.magnitude - conversationDistance);
        float facingError = directionToNpc.sqrMagnitude > 0.0001f
            ? Vector3.Angle(transform.forward, directionToNpc.normalized)
            : 0f;
        float lookError = cameraController != null
            ? cameraController.GetLookAngleTo(talkingVillager.ConversationLookPosition)
            : facingError;

        if (distanceError <= distanceTolerance
            && facingError <= facingTolerance
            && lookError <= facingTolerance)
        {
            StartAlignedConversation();
        }
    }

    private void StartAlignedConversation()
    {
        preparingConversation = false;
        if (talkingVillager != null && talkingVillager.TryStartConversation())
        {
            talkingVillager.Completed += OnConversationCompleted;
            return;
        }

        CancelConversationPreparation();
    }

    private void CancelConversationPreparation()
    {
        preparingConversation = false;
        UnsubscribeFromTalkingVillager();
        SetConversationControlsLocked(false);
    }

    private void FaceTalkingVillager()
    {
        if (talkingVillager == null)
        {
            return;
        }

        Vector3 direction = talkingVillager.transform.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        Quaternion nextRotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            faceNpcTurnSpeed * Time.unscaledDeltaTime);

        if (cameraController != null)
        {
            cameraController.LookAtPoint(
                talkingVillager.ConversationLookPosition,
                faceNpcTurnSpeed * Time.unscaledDeltaTime);
        }
        else
        {
            transform.rotation = nextRotation;
        }
    }

    private void UnsubscribeFromTalkingVillager()
    {
        if (talkingVillager == null)
        {
            return;
        }

        talkingVillager.Completed -= OnConversationCompleted;
        talkingVillager = null;
        preparingConversation = false;
    }

    private void PreparePrompt()
    {
        if (promptRoot == null)
        {
            GameObject interactObject = GameObject.Find("Interact");
            if (interactObject != null && interactObject.transform is RectTransform)
            {
                promptRoot = interactObject;
            }
        }

        if (promptRoot == null)
        {
            return;
        }

        if (!promptRoot.activeSelf)
        {
            promptRoot.SetActive(true);
        }

        promptCanvasGroup = promptRoot.GetComponent<CanvasGroup>();
        if (promptCanvasGroup == null)
        {
            promptCanvasGroup = promptRoot.AddComponent<CanvasGroup>();
        }

        promptCanvasGroup.interactable = false;
        promptCanvasGroup.blocksRaycasts = false;
    }

    private void ResolveInputActionsReference()
    {
        if (inputActions != null)
        {
            return;
        }

        FPSPlayerController playerController = GetComponent<FPSPlayerController>();
        if (playerController != null)
        {
            inputActions = playerController.InputActions;
        }
    }

    private void ResolvePlayerReferences()
    {
        if (playerController == null)
        {
            playerController = GetComponent<FPSPlayerController>();
        }

        if (cameraController == null)
        {
            cameraController = GetComponent<FPSCameraController>();
        }

        if (playerStamina == null)
        {
            playerStamina = GetComponent<PlayerStamina>();
        }
    }

    private void SetPromptAlpha(float alpha)
    {
        if (promptCanvasGroup != null)
        {
            promptCanvasGroup.alpha = Mathf.Clamp01(alpha);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Transform origin = detectionOrigin != null ? detectionOrigin : transform;
        Gizmos.color = new Color(0.2f, 0.85f, 0.45f, 0.7f);
        Gizmos.DrawWireSphere(origin.position, interactionRadius);
    }
}
