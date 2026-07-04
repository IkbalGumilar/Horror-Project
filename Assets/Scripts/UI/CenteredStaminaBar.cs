using UnityEngine;

public sealed class CenteredStaminaBar : MonoBehaviour
{
    [SerializeField] private RectTransform fill;
    [SerializeField, Range(0f, 1f)] private float value = 1f;

    private void Reset()
    {
        Transform fillTransform = transform.Find("Fill Area/Fill");
        if (fillTransform != null)
        {
            fill = fillTransform as RectTransform;
        }
    }

    private void Start()
    {
        Apply();
    }

    private void OnValidate()
    {
        value = Mathf.Clamp01(value);
    }

    public void SetValue(float normalizedValue)
    {
        value = Mathf.Clamp01(normalizedValue);
        Apply();
    }

    private void Apply()
    {
        if (fill == null)
        {
            return;
        }

        fill.anchorMin = Vector2.zero;
        fill.anchorMax = Vector2.one;
        fill.anchoredPosition = Vector2.zero;
        fill.sizeDelta = Vector2.zero;
        fill.pivot = new Vector2(0.5f, fill.pivot.y);
        Vector3 scale = fill.localScale;
        scale.x = value;
        fill.localScale = scale;
    }
}
