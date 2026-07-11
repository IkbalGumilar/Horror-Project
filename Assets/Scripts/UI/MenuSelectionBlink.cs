using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class MenuSelectionBlink : MonoBehaviour
{
    [SerializeField, Range(0f, 255f)] private float minimumAlpha;
    [SerializeField, Range(0f, 255f)] private float maximumAlpha = 50f;
    [SerializeField, Min(0.01f)] private float blinkCyclesPerSecond = 2.5f;
    [SerializeField] private Selectable firstSelected;

    private Selectable[] selectables;
    private Selectable selected;

    private void Awake()
    {
        selectables = GetComponentsInChildren<Selectable>(true);
        for (int i = 0; i < selectables.Length; i++)
        {
            Selectable selectable = selectables[i];
            selectable.transition = Selectable.Transition.None;

            MenuSelectionBlinkItem item = selectable.GetComponent<MenuSelectionBlinkItem>();
            if (item == null)
            {
                item = selectable.gameObject.AddComponent<MenuSelectionBlinkItem>();
            }

            item.Initialize(this, selectable);
            SetGraphicAlpha(selectable, minimumAlpha);
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
        SetGraphicAlpha(selected, Mathf.Lerp(minimumAlpha, maximumAlpha, phase));
    }

    internal void SetSelected(Selectable next)
    {
        if (selected == next)
        {
            return;
        }

        if (selected != null)
        {
            SetGraphicAlpha(selected, minimumAlpha);
        }

        selected = next;
    }

    internal void ClearSelected(Selectable item)
    {
        if (selected != item)
        {
            return;
        }

        SetGraphicAlpha(selected, minimumAlpha);
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

    private static void SetGraphicAlpha(Selectable selectable, float alpha255)
    {
        if (selectable == null || selectable.targetGraphic == null)
        {
            return;
        }

        Color color = selectable.targetGraphic.color;
        color.a = Mathf.Clamp(alpha255, 0f, 255f) / 255f;
        selectable.targetGraphic.color = color;
    }
}
