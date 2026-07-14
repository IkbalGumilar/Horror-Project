using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public sealed class CityCashierConversation : MonoBehaviour, ICustomVillagerConversation
{
    private static readonly string[] TopicChoiceKeys =
    {
        "city.kopdes.choice.banaspati",
        "city.kopdes.choice.reason",
        "city.kopdes.choice.forest",
        "city.kopdes.choice.distance",
        "city.kopdes.choice.price",
        "city.kopdes.choice.leave"
    };

    private static readonly string[] ConfirmationChoiceKeys =
    {
        "ui.yes",
        "ui.no"
    };

    [SerializeField] private LocalizedChoicePanel topicChoicePanel;
    [SerializeField] private LocalizedChoicePanel confirmationChoicePanel;
    [SerializeField] private string cashierSpeakerKey = "city.kopdes.cashier.name";
    [SerializeField, Min(0.1f)] private float lineDuration = 3.5f;
    [SerializeField, Min(0f)] private float pauseBetweenLines = 0.15f;
    [SerializeField] private UnityEvent purchaseConfirmed;

    private VillagerConversation owner;
    private Coroutine conversationRoutine;
    private int pendingSelection = int.MinValue;

    public bool HasConfirmedPurchase { get; private set; }

    public bool TryBeginConversation(VillagerConversation conversationOwner)
    {
        if (conversationRoutine != null
            || conversationOwner == null
            || topicChoicePanel == null
            || confirmationChoicePanel == null)
        {
            return false;
        }

        owner = conversationOwner;
        conversationRoutine = StartCoroutine(ConversationRoutine());
        return true;
    }

    public void CancelConversation()
    {
        if (conversationRoutine != null)
        {
            StopCoroutine(conversationRoutine);
            conversationRoutine = null;
        }

        HideChoicePanels();
        SubtitleController.Instance?.EndChoiceOverlay();
        owner = null;
        RestoreCursor();
    }

    private IEnumerator ConversationRoutine()
    {
        yield return ShowLine(cashierSpeakerKey, "city.kopdes.greeting");

        bool keepTalking = true;
        while (keepTalking)
        {
            int topic = int.MinValue;
            yield return WaitForChoice(topicChoicePanel, TopicChoiceKeys, value => topic = value);

            if (topic < 0 || topic == TopicChoiceKeys.Length - 1)
            {
                keepTalking = false;
                continue;
            }

            switch (topic)
            {
                case 0:
                    yield return ShowLine("speaker.player", "city.kopdes.question.banaspati");
                    yield return ShowLine(cashierSpeakerKey, "city.kopdes.answer.banaspati.1");
                    yield return ShowLine(cashierSpeakerKey, "city.kopdes.answer.banaspati.2");
                    break;
                case 1:
                    yield return ShowLine("speaker.player", "city.kopdes.question.reason");
                    yield return ShowLine(cashierSpeakerKey, "city.kopdes.answer.reason.joke");
                    yield return ShowLine(cashierSpeakerKey, "city.kopdes.answer.reason.real");
                    break;
                case 2:
                    yield return ShowLine("speaker.player", "city.kopdes.question.forest");
                    yield return ShowLine(cashierSpeakerKey, "city.kopdes.answer.forest.1");
                    yield return ShowLine(cashierSpeakerKey, "city.kopdes.answer.forest.2");
                    break;
                case 3:
                    yield return ShowLine("speaker.player", "city.kopdes.question.distance");
                    yield return ShowLine(cashierSpeakerKey, "city.kopdes.answer.distance");
                    break;
                case 4:
                    yield return ShowLine("speaker.player", "city.kopdes.question.price");
                    yield return ShowLine(cashierSpeakerKey, "city.kopdes.answer.price");

                    int confirmation = int.MinValue;
                    yield return WaitForChoice(
                        confirmationChoicePanel,
                        ConfirmationChoiceKeys,
                        value => confirmation = value);
                    if (confirmation == 0)
                    {
                        HasConfirmedPurchase = true;
                        purchaseConfirmed?.Invoke();
                        yield return ShowLine(cashierSpeakerKey, "city.kopdes.purchase.yes");
                        keepTalking = false;
                    }
                    else if (confirmation == 1)
                    {
                        yield return ShowLine(cashierSpeakerKey, "city.kopdes.purchase.no");
                    }
                    break;
            }
        }

        FinishConversation();
    }

    private IEnumerator ShowLine(string speakerKey, string textKey)
    {
        HideChoicePanels();
        SubtitleController.Instance?.EndChoiceOverlay();
        RestoreCursor();
        SubtitleController.Instance?.ShowLocalized(speakerKey, textKey, lineDuration);
        yield return new WaitForSecondsRealtime(lineDuration + pauseBetweenLines);
    }

    private IEnumerator WaitForChoice(
        LocalizedChoicePanel panel,
        string[] choiceKeys,
        System.Action<int> callback)
    {
        HideChoicePanels();
        SubtitleController.Instance?.BeginChoiceOverlay();
        pendingSelection = int.MinValue;
        panel.Show(choiceKeys, value => pendingSelection = value);

        while (pendingSelection == int.MinValue)
        {
            yield return null;
        }

        int selected = pendingSelection;
        pendingSelection = int.MinValue;
        panel.HideImmediate();
        SubtitleController.Instance?.EndChoiceOverlay();
        callback(selected);
    }

    private void FinishConversation()
    {
        HideChoicePanels();
        SubtitleController.Instance?.EndChoiceOverlay();
        SubtitleController.Instance?.Hide();
        RestoreCursor();

        VillagerConversation completedOwner = owner;
        owner = null;
        conversationRoutine = null;
        completedOwner?.CompleteCustomConversation(this);
    }

    private static void RestoreCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void HideChoicePanels()
    {
        topicChoicePanel?.HideImmediate();
        confirmationChoicePanel?.HideImmediate();
    }

    private void OnDisable()
    {
        CancelConversation();
    }
}
