using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

[DisallowMultipleComponent]
public sealed class MenuSelectionBlink : MonoBehaviour
{
    private const float SettingsNormalAlpha = 255f;
    private const float SettingsSelectedAlpha = 100f;
    private const float SelectedRgbBoost = 20f;

    [SerializeField, Range(0f, 255f)] private float minimumAlpha;
    [SerializeField, Range(0f, 255f)] private float maximumAlpha = 50f;
    [SerializeField, Min(0.01f)] private float blinkCyclesPerSecond = 2.5f;
    [SerializeField] private Selectable firstSelected;

    private Selectable[] selectables;
    private Selectable selected;
    private readonly Dictionary<Graphic, Color> baseColors = new Dictionary<Graphic, Color>();

    private void Awake()
    {
        selectables = GetComponentsInChildren<Selectable>(true);
        for (int i = 0; i < selectables.Length; i++)
        {
            Selectable selectable = selectables[i];
            if (IsSettingsTab(selectable))
            {
                continue;
            }

            selectable.transition = Selectable.Transition.None;
            CacheBaseColor(selectable);

            MenuSelectionBlinkItem item = selectable.GetComponent<MenuSelectionBlinkItem>();
            if (item == null)
            {
                item = selectable.gameObject.AddComponent<MenuSelectionBlinkItem>();
            }

            item.Initialize(this, selectable);
            SetGraphicVisual(selectable, GetRestingAlpha(selectable), 0f);
        }
    }

    private void Start()
    {
        Selectable initial = firstSelected != null ? firstSelected : FindFirstInteractable();
        if (initial == null)
        {
            return;
        }

        EventSystem.current?.SetSelectedGameObject(initial.gameObject);
        SetSelected(initial);
    }

    private void Update()
    {
        if (selected == null)
        {
            return;
        }

        float phase = Mathf.PingPong(Time.unscaledTime * blinkCyclesPerSecond * 2f, 1f);
        float fromAlpha = IsSettingsControl(selected) ? SettingsNormalAlpha : minimumAlpha;
        float toAlpha = IsSettingsControl(selected) ? SettingsSelectedAlpha : maximumAlpha;
        SetGraphicVisual(selected, Mathf.Lerp(fromAlpha, toAlpha, phase), Mathf.Lerp(0f, SelectedRgbBoost, phase));
    }

    internal void SetSelected(Selectable next)
    {
        if (selected == next)
        {
            return;
        }

        if (selected != null)
        {
            SetGraphicVisual(selected, GetRestingAlpha(selected), 0f);
        }

        selected = next;
    }

    internal void ClearSelected(Selectable item)
    {
        if (selected != item)
        {
            return;
        }

        SetGraphicVisual(selected, GetRestingAlpha(selected), 0f);
        selected = null;
    }

    private Selectable FindFirstInteractable()
    {
        for (int i = 0; i < selectables.Length; i++)
        {
            if (selectables[i].IsInteractable())
            {
                return selectables[i];
            }
        }

        return null;
    }

    private void CacheBaseColor(Selectable selectable)
    {
        if (selectable == null || selectable.targetGraphic == null)
        {
            return;
        }

        if (!baseColors.ContainsKey(selectable.targetGraphic))
        {
            baseColors.Add(selectable.targetGraphic, selectable.targetGraphic.color);
        }
    }

    private void SetGraphicVisual(Selectable selectable, float alpha255, float rgbBoost255)
    {
        if (selectable == null || selectable.targetGraphic == null)
        {
            return;
        }

        CacheBaseColor(selectable);

        Color color = baseColors.TryGetValue(selectable.targetGraphic, out Color baseColor)
            ? baseColor
            : selectable.targetGraphic.color;
        float boost = Mathf.Clamp(rgbBoost255, 0f, 255f) / 255f;
        color.r = Mathf.Clamp01(color.r + boost);
        color.g = Mathf.Clamp01(color.g + boost);
        color.b = Mathf.Clamp01(color.b + boost);
        color.a = Mathf.Clamp(alpha255, 0f, 255f) / 255f;
        selectable.targetGraphic.color = color;
    }

    private float GetRestingAlpha(Selectable selectable)
    {
        return IsSettingsControl(selectable) ? SettingsNormalAlpha : minimumAlpha;
    }

    private static bool IsSettingsControl(Selectable selectable)
    {
        if (selectable == null || IsSettingsTab(selectable))
        {
            return false;
        }

        Transform current = selectable.transform;
        while (current != null)
        {
            if (current.name == "SettingMenu")
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private static bool IsSettingsTab(Selectable selectable)
    {
        return selectable != null && selectable.name.StartsWith("Line ", System.StringComparison.Ordinal);
    }
}
