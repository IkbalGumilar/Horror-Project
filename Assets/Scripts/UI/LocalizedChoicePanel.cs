using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class LocalizedChoicePanel : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text choiceText;
    [SerializeField] private Color selectedColor = Color.white;
    [SerializeField] private Color normalColor = new Color(0.68f, 0.68f, 0.68f, 1f);

    private readonly List<string> choiceKeys = new();
    private Action<int> selectionCallback;
    private int selectedIndex;
    private bool acceptingInput;

    public bool IsOpen => acceptingInput;

    private void Awake()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (choiceText == null)
        {
            choiceText = GetComponentInChildren<TMP_Text>(true);
        }

        HideImmediate();
    }

    private void OnEnable()
    {
        LocalizationManager.LanguageChanged += RefreshText;
    }

    private void OnDisable()
    {
        LocalizationManager.LanguageChanged -= RefreshText;
    }

    private void Update()
    {
        if (!acceptingInput || choiceKeys.Count == 0)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        Gamepad gamepad = Gamepad.current;

        bool previous = keyboard != null && (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame);
        bool next = keyboard != null && (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame);
        bool submit = keyboard != null && (
            keyboard.enterKey.wasPressedThisFrame
            || keyboard.spaceKey.wasPressedThisFrame
            || keyboard.eKey.wasPressedThisFrame);
        bool cancel = keyboard != null && keyboard.escapeKey.wasPressedThisFrame;

        if (gamepad != null)
        {
            previous |= gamepad.dpad.up.wasPressedThisFrame;
            next |= gamepad.dpad.down.wasPressedThisFrame;
            submit |= gamepad.buttonSouth.wasPressedThisFrame;
            cancel |= gamepad.buttonEast.wasPressedThisFrame;
        }

        if (previous)
        {
            SetSelectedIndex(selectedIndex - 1);
        }
        else if (next)
        {
            SetSelectedIndex(selectedIndex + 1);
        }
        else if (submit)
        {
            Submit(selectedIndex);
        }
        else if (cancel)
        {
            Cancel();
        }
    }

    public void Show(IReadOnlyList<string> localizedChoiceKeys, Action<int> onSelected, int initialIndex = 0)
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        choiceKeys.Clear();
        if (localizedChoiceKeys != null)
        {
            for (int i = 0; i < localizedChoiceKeys.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(localizedChoiceKeys[i]))
                {
                    choiceKeys.Add(localizedChoiceKeys[i]);
                }
            }
        }

        selectionCallback = onSelected;
        selectedIndex = choiceKeys.Count > 0 ? Mathf.Clamp(initialIndex, 0, choiceKeys.Count - 1) : 0;
        acceptingInput = choiceKeys.Count > 0;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = acceptingInput ? 1f : 0f;
            canvasGroup.interactable = acceptingInput;
            canvasGroup.blocksRaycasts = acceptingInput;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        RefreshText();
    }

    public void HideImmediate()
    {
        acceptingInput = false;
        selectionCallback = null;
        choiceKeys.Clear();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!acceptingInput || choiceText == null)
        {
            return;
        }

        int linkIndex = TMP_TextUtilities.FindIntersectingLink(
            choiceText,
            eventData.position,
            eventData.pressEventCamera);
        if (linkIndex < 0)
        {
            return;
        }

        TMP_LinkInfo link = choiceText.textInfo.linkInfo[linkIndex];
        if (int.TryParse(link.GetLinkID(), out int choiceIndex))
        {
            Submit(choiceIndex);
        }
    }

    private void SetSelectedIndex(int index)
    {
        selectedIndex = (index % choiceKeys.Count + choiceKeys.Count) % choiceKeys.Count;
        RefreshText();
    }

    private void Submit(int index)
    {
        if (!acceptingInput || index < 0 || index >= choiceKeys.Count)
        {
            return;
        }

        Action<int> callback = selectionCallback;
        HideImmediate();
        callback?.Invoke(index);
    }

    private void Cancel()
    {
        Action<int> callback = selectionCallback;
        HideImmediate();
        callback?.Invoke(-1);
    }

    private void RefreshText()
    {
        if (choiceText == null)
        {
            return;
        }

        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < choiceKeys.Count; i++)
        {
            if (i > 0)
            {
                builder.AppendLine();
            }

            Color color = i == selectedIndex ? selectedColor : normalColor;
            builder.Append("<link=\"").Append(i).Append("\"><color=#")
                .Append(ColorUtility.ToHtmlStringRGBA(color)).Append(">")
                .Append(i == selectedIndex ? "> " : "  ")
                .Append(LocalizationManager.Get(choiceKeys[i]))
                .Append("</color></link>");
        }

        choiceText.text = builder.ToString();
    }
}
