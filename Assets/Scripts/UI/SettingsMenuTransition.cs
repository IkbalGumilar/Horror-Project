using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class SettingsMenuTransition : MonoBehaviour
{
    private static readonly Color ActiveTabColor = new Color(1f, 35f / 255f, 0f, 1f);

    [Header("Panels")]
    [SerializeField] private RectTransform mainMenu;
    [SerializeField] private RectTransform settingsMenu;

    [Header("Buttons")]
    [SerializeField] private Button settingsButton;

    [Header("Animation")]
    [SerializeField] private UIUXAnimator uiAnimator;
    [SerializeField, Min(0.01f)] private float duration = 0.35f;
    [SerializeField, Min(0.01f)] private float tabSwitchDuration = 0.25f;

    private Vector2 mainMenuHome;
    private Vector2 settingsMenuHome;
    private Coroutine transitionRoutine;
    private Coroutine tabSwitchRoutine;
    private Button[] backButtons;
    private TMP_Text[] settingTexts;
    private readonly Dictionary<TMP_Text, string> settingTextEnglish = new Dictionary<TMP_Text, string>();
    private readonly SettingsTab[] tabs =
    {
        new SettingsTab("Line Video", "Video Menu"),
        new SettingsTab("Line Audio", "Audio Menu"),
        new SettingsTab("Line Control", "Control Menu"),
        new SettingsTab("Line GamePlay", "Gameplay Menu"),
        new SettingsTab("Line Accessibility", "Accessibility Menu", "Accesibility Menu")
    };
    private int activeTabIndex = -1;
    private int highlightedTabIndex = -1;

    private void Awake()
    {
        if (mainMenu == null || settingsMenu == null || settingsButton == null)
        {
            enabled = false;
            return;
        }

        mainMenuHome = mainMenu.anchoredPosition;
        settingsMenuHome = settingsMenu.anchoredPosition;
        uiAnimator = ResolveAnimator();
        InitializeSettingsTabs();
        mainMenu.gameObject.SetActive(true);
        settingsMenu.gameObject.SetActive(false);

        settingsButton.onClick.AddListener(OpenSettings);
        backButtons = settingsMenu.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < backButtons.Length; i++)
        {
            if (backButtons[i].name == "Back")
            {
                backButtons[i].onClick.AddListener(CloseSettings);
            }
        }

        CacheSettingTexts();
        RefreshSettingTexts();
    }

    private void OnEnable()
    {
        LocalizationManager.LanguageChanged += RefreshSettingTexts;
        RefreshSettingTexts();
    }

    private void OnDisable()
    {
        LocalizationManager.LanguageChanged -= RefreshSettingTexts;
    }

    private void OnDestroy()
    {
        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveListener(OpenSettings);
        }

        if (backButtons != null)
        {
            for (int i = 0; i < backButtons.Length; i++)
            {
                if (backButtons[i] != null && backButtons[i].name == "Back")
                {
                    backButtons[i].onClick.RemoveListener(CloseSettings);
                }
            }
        }

        for (int i = 0; i < tabs.Length; i++)
        {
            if (tabs[i].button != null)
            {
                tabs[i].button.onClick.RemoveListener(tabs[i].Click);
            }
        }

        if (tabSwitchRoutine != null)
        {
            uiAnimator.StopAnimation(tabSwitchRoutine);
        }
    }

    private void CacheSettingTexts()
    {
        settingTexts = settingsMenu.GetComponentsInChildren<TMP_Text>(true);
        settingTextEnglish.Clear();
        for (int i = 0; i < settingTexts.Length; i++)
        {
            TMP_Text text = settingTexts[i];
            if (text == null || IsDropdownRuntimeLabel(text))
            {
                continue;
            }

            string englishText = text.text.Trim();
            if (string.IsNullOrEmpty(englishText))
            {
                continue;
            }

            settingTextEnglish[text] = englishText;
        }
    }

    private void RefreshSettingTexts()
    {
        if (settingTexts == null)
        {
            return;
        }

        for (int i = 0; i < settingTexts.Length; i++)
        {
            TMP_Text text = settingTexts[i];
            if (text == null || !settingTextEnglish.TryGetValue(text, out string englishText))
            {
                continue;
            }

            text.text = LocalizationManager.GetText(englishText);
        }
    }

    private static bool IsDropdownRuntimeLabel(TMP_Text text)
    {
        TMP_Dropdown dropdown = text.GetComponentInParent<TMP_Dropdown>(true);
        if (dropdown == null)
        {
            return false;
        }

        return text == dropdown.captionText || text == dropdown.itemText;
    }

    public void OpenSettings()
    {
        StartTransition(openingSettings: true);
    }

    public void CloseSettings()
    {
        StartTransition(openingSettings: false);
    }

    private void StartTransition(bool openingSettings)
    {
        if (transitionRoutine != null)
        {
            uiAnimator.StopAnimation(transitionRoutine);
        }

        RectTransform entering = openingSettings ? settingsMenu : mainMenu;
        RectTransform leaving = openingSettings ? mainMenu : settingsMenu;
        Vector2 enteringHome = openingSettings ? settingsMenuHome : mainMenuHome;
        Vector2 leavingHome = openingSettings ? mainMenuHome : settingsMenuHome;
        bool enteringFromLeft = openingSettings;

        transitionRoutine = uiAnimator.PlayHorizontalSwap(
            entering,
            leaving,
            enteringHome,
            leavingHome,
            enteringFromLeft,
            GetSlideDistance(),
            duration,
            () =>
            {
                if (openingSettings)
                {
                    ActivateDefaultSettingsTab(animate: false);
                }

                SetButtonsInteractable(false);
            },
            () =>
            {
                SetButtonsInteractable(true);
                SelectForCurrentMenu(openingSettings);
                transitionRoutine = null;
            });
    }

    private UIUXAnimator ResolveAnimator()
    {
        if (uiAnimator != null)
        {
            return uiAnimator;
        }

        UIUXAnimator found = GetComponent<UIUXAnimator>();
        return found != null ? found : gameObject.AddComponent<UIUXAnimator>();
    }

    private float GetSlideDistance()
    {
        return UIUXAnimator.GetSlideDistance(transform as RectTransform);
    }

    private void SetButtonsInteractable(bool interactable)
    {
        settingsButton.interactable = interactable;
        SetSettingsButtonsInteractable(interactable);
    }

    private void SetSettingsButtonsInteractable(bool interactable)
    {
        Button[] settingsButtons = settingsMenu.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < settingsButtons.Length; i++)
        {
            settingsButtons[i].interactable = interactable;
        }
    }

    private void SelectForCurrentMenu(bool settingsAreOpen)
    {
        GameObject selection = settingsAreOpen ? GetActiveTabSelection() : settingsButton.gameObject;
        if (selection != null)
        {
            EventSystem.current?.SetSelectedGameObject(selection);
        }
    }

    internal void SelectSettingsTab(int index)
    {
        if (!IsValidTab(index) || index == activeTabIndex)
        {
            return;
        }

        highlightedTabIndex = index;
        ApplyTabVisuals();
    }

    internal void DeselectSettingsTab(int index)
    {
        if (highlightedTabIndex == index)
        {
            highlightedTabIndex = -1;
            ApplyTabVisuals();
        }
    }

    private void Update()
    {
        if (highlightedTabIndex >= 0)
        {
            ApplyTabVisuals();
        }
    }

    private void InitializeSettingsTabs()
    {
        Transform[] children = settingsMenu.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < tabs.Length; i++)
        {
            SettingsTab tab = tabs[i];
            Transform line = FindChild(children, tab.lineName);
            if (line == null)
            {
                continue;
            }

            tab.lineGraphic = line.GetComponent<Graphic>();
            if (tab.lineGraphic == null)
            {
                tab.lineGraphic = line.gameObject.AddComponent<Image>();
            }

            tab.lineGraphic.raycastTarget = true;
            tab.button = line.GetComponent<Button>();
            if (tab.button == null)
            {
                tab.button = line.gameObject.AddComponent<Button>();
            }

            tab.button.transition = Selectable.Transition.None;
            tab.button.targetGraphic = tab.lineGraphic;
            tab.Initialize(this, i);
            tab.button.onClick.AddListener(tab.Click);
            ConfigureTabEvents(line.gameObject, tab.button, i);
            tab.CachePanel(FindFirstChildByNames(children, tab.panelNames));
        }

        ActivateDefaultSettingsTab(animate: false);
    }

    private void ConfigureTabEvents(GameObject target, Button button, int index)
    {
        EventTrigger trigger = target.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = target.AddComponent<EventTrigger>();
        }

        AddEventTrigger(trigger, EventTriggerType.Select, _ => SelectSettingsTab(index));
        AddEventTrigger(trigger, EventTriggerType.Deselect, _ => DeselectSettingsTab(index));
        AddEventTrigger(trigger, EventTriggerType.PointerEnter, _ =>
        {
            if (button != null && button.IsInteractable())
            {
                EventSystem.current?.SetSelectedGameObject(button.gameObject);
            }
        });
    }

    private static void AddEventTrigger(EventTrigger trigger, EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> callback)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry
        {
            eventID = type
        };
        entry.callback.AddListener(callback);
        trigger.triggers.Add(entry);
    }

    private void ActivateDefaultSettingsTab(bool animate)
    {
        if (activeTabIndex >= 0 && IsValidTab(activeTabIndex))
        {
            ApplyTabState(activeTabIndex, animate);
            return;
        }

        for (int i = 0; i < tabs.Length; i++)
        {
            if (tabs[i].button != null)
            {
                ApplyTabState(i, animate);
                return;
            }
        }
    }

    private void ApplyTabState(int index, bool animate)
    {
        if (!IsValidTab(index))
        {
            return;
        }

        if (!animate || activeTabIndex < 0 || activeTabIndex == index || tabs[activeTabIndex].panelRect == null || tabs[index].panelRect == null)
        {
            ApplyTabStateInstant(index);
            return;
        }

        if (tabSwitchRoutine != null)
        {
            uiAnimator.StopAnimation(tabSwitchRoutine);
        }

        tabSwitchRoutine = StartTabSwitch(activeTabIndex, index);
    }

    private void ApplyTabStateInstant(int index)
    {
        activeTabIndex = index;
        highlightedTabIndex = -1;
        for (int i = 0; i < tabs.Length; i++)
        {
            if (tabs[i].panel != null)
            {
                tabs[i].panel.SetActive(i == activeTabIndex);
            }

            if (tabs[i].panelRect != null)
            {
                tabs[i].panelRect.anchoredPosition = tabs[i].panelHome;
            }
        }

        ApplyTabVisuals();
    }

    private Coroutine StartTabSwitch(int fromIndex, int toIndex)
    {
        SettingsTab fromTab = tabs[fromIndex];
        SettingsTab toTab = tabs[toIndex];
        return uiAnimator.PlayTabSwipe(
            fromTab.panelRect,
            toTab.panelRect,
            fromTab.panelHome,
            toTab.panelHome,
            toIndex > fromIndex,
            GetSlideDistance(),
            tabSwitchDuration,
            () =>
            {
                activeTabIndex = toIndex;
                highlightedTabIndex = -1;
                ApplyTabVisuals();
                SetSettingsButtonsInteractable(false);
            },
            () =>
            {
                SetSettingsButtonsInteractable(true);
                EventSystem.current?.SetSelectedGameObject(toTab.button.gameObject);
                tabSwitchRoutine = null;
            });
    }

    private void ApplyTabVisuals()
    {
        for (int i = 0; i < tabs.Length; i++)
        {
            Graphic graphic = tabs[i].lineGraphic;
            if (graphic == null)
            {
                continue;
            }

            Color color = ActiveTabColor;
            if (i == activeTabIndex)
            {
                color.a = 1f;
            }
            else if (i == highlightedTabIndex)
            {
                float phase = Mathf.PingPong(Time.unscaledTime * 5f, 1f);
                color.a = Mathf.Lerp(0f, 50f / 255f, phase);
            }
            else
            {
                color.a = 0f;
            }

            graphic.color = color;
        }
    }

    private GameObject GetActiveTabSelection()
    {
        if (activeTabIndex >= 0 && activeTabIndex < tabs.Length && tabs[activeTabIndex].button != null)
        {
            return tabs[activeTabIndex].button.gameObject;
        }

        return FindFirstActiveButton(settingsMenu);
    }

    private bool IsValidTab(int index)
    {
        return index >= 0 && index < tabs.Length && tabs[index].button != null;
    }

    private static GameObject FindFirstActiveButton(RectTransform root)
    {
        Button[] buttons = root.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button.gameObject.activeInHierarchy && button.IsInteractable() && button.name != "Back")
            {
                return button.gameObject;
            }
        }

        return buttons.Length > 0 ? buttons[0].gameObject : null;
    }

    private static Transform FindFirstChildByNames(Transform[] children, string[] names)
    {
        for (int i = 0; i < names.Length; i++)
        {
            Transform found = FindChild(children, names[i]);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static Transform FindChild(Transform[] children, string childName)
    {
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name == childName)
            {
                return children[i];
            }
        }

        return null;
    }

    private sealed class SettingsTab
    {
        internal readonly string lineName;
        internal readonly string[] panelNames;
        internal Graphic lineGraphic;
        internal Button button;
        internal GameObject panel;
        internal RectTransform panelRect;
        internal Vector2 panelHome;
        private SettingsMenuTransition owner;
        private int index;

        internal SettingsTab(string lineObjectName, params string[] panelObjectNames)
        {
            lineName = lineObjectName;
            panelNames = panelObjectNames;
        }

        internal void Initialize(SettingsMenuTransition tabOwner, int tabIndex)
        {
            owner = tabOwner;
            index = tabIndex;
        }

        internal void CachePanel(Transform panelTransform)
        {
            if (panelTransform == null)
            {
                return;
            }

            panel = panelTransform.gameObject;
            panelRect = panelTransform as RectTransform;
            if (panelRect != null)
            {
                panelHome = panelRect.anchoredPosition;
            }
        }

        internal void Click()
        {
            owner?.ApplyTabState(index, animate: true);
        }
    }

}
