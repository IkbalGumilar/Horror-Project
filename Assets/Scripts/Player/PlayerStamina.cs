using UnityEngine;
using UnityEngine.UI;

public sealed class PlayerStamina : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FPSPlayerController playerController;
    [SerializeField] private Slider staminaSlider;
    [SerializeField] private CenteredStaminaBar centeredStaminaBar;

    [Header("Stamina")]
    [SerializeField] private float maxStamina = 1f;
    [SerializeField] private float currentStamina = 1f;
    [SerializeField] private bool startFull = true;
    [SerializeField] private float sprintDrainPerSecond = 0.2f;
    [SerializeField] private float recoveryPerSecond = 0.15f;
    [SerializeField] private float recoveryDelay = 0.5f;

    [Header("Exhaustion")]
    [SerializeField] private float exhaustedRegenLockDuration = 2f;
    [SerializeField, Range(0f, 1f)] private float exhaustedRecoveryThreshold = 0.5f;
    [SerializeField, Range(0f, 1f)] private float exhaustedMoveSpeedMultiplier = 0.7f;

    [Header("HUD")]
    [SerializeField] private Graphic[] staminaGraphics;
    [SerializeField] private CanvasGroup staminaCanvasGroup;
    [SerializeField] private bool hideOnAwake = true;
    [SerializeField] private float visibleAlpha = 1f;
    [SerializeField] private float hiddenAlpha = 0f;
    [SerializeField] private float delayBeforeFade = 1f;
    [SerializeField] private float fadeDuration = 0.25f;
    [SerializeField, Range(0f, 1f)] private float hideOnlyAfterNormalized = 0.5f;
    [SerializeField, Range(0f, 1f)] private float lowStaminaBlinkThreshold = 0.2f;
    [SerializeField, Range(0f, 1f)] private float blinkMinAlpha = 0.03921569f;
    [SerializeField] private float blinkSpeed = 16f;

    private float lastDrainTime = -999f;
    private float exhaustedStartTime = -999f;
    private float lastHudShowTime = -999f;
    private float hudFadeStartTime = -999f;
    private bool hudFadeStarted;
    private bool hudHasBeenShown;
    private bool exhausted;

    public float CurrentStamina => currentStamina;
    public float MaxStamina => maxStamina;
    public float NormalizedStamina => maxStamina > 0f ? Mathf.Clamp01(currentStamina / maxStamina) : 0f;
    public bool CanSprint => !exhausted && currentStamina > 0f;
    public bool IsExhausted => exhausted;
    public float MovementSpeedMultiplier => exhausted ? exhaustedMoveSpeedMultiplier : 1f;

    public void ShowControlReadyIndicator()
    {
        RefreshUI();
        ShowHud();
    }

    private void Reset()
    {
        playerController = GetComponent<FPSPlayerController>();
    }

    private void Awake()
    {
        if (playerController == null)
        {
            playerController = GetComponent<FPSPlayerController>();
        }

        if ((staminaGraphics == null || staminaGraphics.Length == 0) && staminaSlider != null)
        {
            staminaGraphics = staminaSlider.GetComponentsInChildren<Graphic>(true);
        }

        if (staminaCanvasGroup == null && staminaSlider != null)
        {
            staminaCanvasGroup = staminaSlider.GetComponent<CanvasGroup>();
        }

        if (startFull)
        {
            currentStamina = maxStamina;
        }

        currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
        exhausted = currentStamina <= 0f;
        if (exhausted)
        {
            exhaustedStartTime = Time.time;
        }

        SetHudAlpha(hideOnAwake ? hiddenAlpha : visibleAlpha);
    }

    private void Start()
    {
        RefreshUI();
    }

    private void Update()
    {
        if (playerController != null && playerController.IsSprinting)
        {
            Drain(Time.deltaTime);
        }
        else
        {
            Recover(Time.deltaTime);
        }

        RefreshUI();
        UpdateHud();
    }

    private void Drain(float deltaTime)
    {
        currentStamina = Mathf.Max(0f, currentStamina - sprintDrainPerSecond * deltaTime);
        lastDrainTime = Time.time;
        ShowHud();

        if (currentStamina <= 0f)
        {
            EnterExhausted();
        }
    }

    private void Recover(float deltaTime)
    {
        if (exhausted && Time.time - exhaustedStartTime < exhaustedRegenLockDuration)
        {
            ShowHud();
            return;
        }

        if (Time.time - lastDrainTime < recoveryDelay)
        {
            return;
        }

        currentStamina = Mathf.Min(maxStamina, currentStamina + recoveryPerSecond * deltaTime);
        if (exhausted)
        {
            ShowHud();
        }

        if (exhausted && NormalizedStamina >= exhaustedRecoveryThreshold)
        {
            exhausted = false;
            ShowHud();
        }
    }

    private void RefreshUI()
    {
        float normalized = NormalizedStamina;

        if (staminaSlider != null && centeredStaminaBar == null)
        {
            staminaSlider.SetValueWithoutNotify(normalized);
        }

        if (centeredStaminaBar != null)
        {
            centeredStaminaBar.SetValue(normalized);
        }
    }

    private void EnterExhausted()
    {
        if (exhausted)
        {
            return;
        }

        exhausted = true;
        exhaustedStartTime = Time.time;
        ShowHud();
    }

    private void ShowHud()
    {
        hudHasBeenShown = true;
        lastHudShowTime = Time.time;
        hudFadeStarted = false;
        hudFadeStartTime = -999f;
        SetHudAlpha(visibleAlpha);
    }

    private void UpdateHud()
    {
        float normalized = NormalizedStamina;
        if (!hudHasBeenShown && normalized >= hideOnlyAfterNormalized && !exhausted)
        {
            SetHudAlpha(hiddenAlpha);
            return;
        }

        if (normalized <= lowStaminaBlinkThreshold)
        {
            float blink = Mathf.PingPong(Time.time * blinkSpeed, 1f);
            SetHudAlpha(Mathf.Lerp(blinkMinAlpha, visibleAlpha, blink));
            return;
        }

        if (exhausted || normalized < hideOnlyAfterNormalized)
        {
            SetHudAlpha(visibleAlpha);
            return;
        }

        if (Time.time - lastHudShowTime < delayBeforeFade)
        {
            SetHudAlpha(visibleAlpha);
            return;
        }

        if (!hudFadeStarted)
        {
            hudFadeStarted = true;
            hudFadeStartTime = Time.time;
        }

        float duration = Mathf.Max(0.001f, fadeDuration);
        float fade = Mathf.Clamp01((Time.time - hudFadeStartTime) / duration);
        fade = fade * fade * (3f - 2f * fade);
        SetHudAlpha(Mathf.Lerp(visibleAlpha, hiddenAlpha, fade));
    }

    private void SetHudAlpha(float alpha)
    {
        if (staminaCanvasGroup != null)
        {
            staminaCanvasGroup.alpha = alpha;
        }

        if (staminaGraphics == null)
        {
            return;
        }

        for (int i = 0; i < staminaGraphics.Length; i++)
        {
            Graphic graphic = staminaGraphics[i];
            if (graphic == null)
            {
                continue;
            }

            Color color = graphic.color;
            color.a = alpha;
            graphic.color = color;
        }
    }
}
