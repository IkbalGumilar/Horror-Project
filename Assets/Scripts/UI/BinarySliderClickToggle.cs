using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Slider))]
public sealed class BinarySliderClickToggle : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler,
    ISubmitHandler,
    ICancelHandler
{
    private Slider slider;
    private bool pointerHeld;
    private bool internalChange;
    private float lockedValue;

    private void Awake()
    {
        slider = GetComponent<Slider>();
        ConfigureSlider();
        lockedValue = Snap(slider.value);
        slider.SetValueWithoutNotify(lockedValue);
        slider.onValueChanged.AddListener(OnSliderValueChanged);
    }

    private void OnDestroy()
    {
        if (slider != null)
        {
            slider.onValueChanged.RemoveListener(OnSliderValueChanged);
        }
    }

    private void LateUpdate()
    {
        if (slider == null || internalChange)
        {
            return;
        }

        float snapped = Snap(slider.value);
        if (pointerHeld || !Mathf.Approximately(snapped, lockedValue))
        {
            SetValueWithoutNotify(lockedValue);
        }
    }

    public void Configure()
    {
        if (slider == null)
        {
            slider = GetComponent<Slider>();
        }

        ConfigureSlider();
        lockedValue = Snap(slider.value);
        slider.SetValueWithoutNotify(lockedValue);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!CanToggle())
        {
            return;
        }

        pointerHeld = true;
        Toggle();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        pointerHeld = false;
        SetValueWithoutNotify(lockedValue);
    }

    public void OnSubmit(BaseEventData eventData)
    {
        if (CanToggle())
        {
            Toggle();
        }
    }

    public void OnCancel(BaseEventData eventData)
    {
        pointerHeld = false;
        SetValueWithoutNotify(lockedValue);
    }

    private void ConfigureSlider()
    {
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = true;
    }

    private bool CanToggle()
    {
        return slider != null && slider.IsActive() && slider.IsInteractable();
    }

    private void Toggle()
    {
        lockedValue = lockedValue >= 0.5f ? 0f : 1f;
        SetValue(lockedValue);
    }

    private void OnSliderValueChanged(float value)
    {
        if (internalChange)
        {
            return;
        }

        SetValueWithoutNotify(lockedValue);
    }

    private void SetValue(float value)
    {
        internalChange = true;
        slider.value = Snap(value);
        internalChange = false;
    }

    private void SetValueWithoutNotify(float value)
    {
        internalChange = true;
        slider.SetValueWithoutNotify(Snap(value));
        internalChange = false;
    }

    private static float Snap(float value)
    {
        return value >= 0.5f ? 1f : 0f;
    }
}
