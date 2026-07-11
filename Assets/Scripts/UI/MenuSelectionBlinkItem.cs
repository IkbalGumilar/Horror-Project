using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class MenuSelectionBlinkItem : MonoBehaviour,
    ISelectHandler,
    IDeselectHandler,
    IPointerEnterHandler
{
    private MenuSelectionBlink owner;
    private Selectable selectable;

    internal void Initialize(MenuSelectionBlink blinkOwner, Selectable target)
    {
        owner = blinkOwner;
        selectable = target;
    }

    public void OnSelect(BaseEventData eventData)
    {
        owner?.SetSelected(selectable);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        owner?.ClearSelected(selectable);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (selectable == null || !selectable.IsInteractable())
        {
            return;
        }

        EventSystem.current?.SetSelectedGameObject(selectable.gameObject);
    }
}
