using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class PlayerDeathController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Transform playerCamera;
    [SerializeField] private MonoBehaviour[] disableOnDeath;
    [SerializeField] private CharacterController characterController;

    [Header("Death Timing")]
    [SerializeField] private float deathDuration = 2f;
    [SerializeField] private bool resetTimeScaleOnAwake = true;

    [Header("Death UI")]
    [SerializeField] private GameObject deathPanel;
    [SerializeField] private Graphic[] deathPanelFadeGraphics;
    [SerializeField] private Graphic pressAnyButtonGraphic;
    [SerializeField] private float deathPanelFadeDuration = 0.5f;
    [SerializeField] private float pressAnyButtonDelay = 3f;
    [SerializeField] private float autoQuitDelay = 5f;
    [SerializeField] private float pressAnyButtonBlinkSpeed = 16f;
    [SerializeField, Range(0f, 1f)] private float pressAnyButtonMinAlpha = 0.03921569f;
    [SerializeField, Range(0f, 1f)] private float pressAnyButtonMaxAlpha = 1f;

    [Header("Passive Burn Death")]
    [SerializeField] private float fallAngle = 90f;
    [SerializeField] private float rollDegreesPerSecond = 720f;

    [Header("Burning Attack Death")]
    [SerializeField] private float carriedSpeed = 6f;
    [SerializeField] private Vector3 carriedDirection = Vector3.forward;

    [Header("Prolonged Chase Death")]
    [SerializeField] private float liftTargetHeight = 10f;
    [SerializeField] private float liftLookAtSpeed = 12f;

    private bool deathStarted;
    private float[] deathPanelVisibleAlphas;

    private void Reset()
    {
        playerHealth = GetComponent<PlayerHealth>();
        characterController = GetComponent<CharacterController>();
        disableOnDeath = GetComponents<MonoBehaviour>();
    }

    private void Awake()
    {
        if (resetTimeScaleOnAwake)
        {
            Time.timeScale = 1f;
        }

        if (playerHealth == null)
        {
            playerHealth = GetComponent<PlayerHealth>();
        }

        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }

        CacheDeathPanelAlphas();
        HideDeathPanel();
    }

    private void OnEnable()
    {
        if (playerHealth != null)
        {
            playerHealth.Died += HandleDeath;
        }
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.Died -= HandleDeath;
        }
    }

    private void HandleDeath()
    {
        if (deathStarted)
        {
            return;
        }

        deathStarted = true;
        DisablePlayerControl();

        PlayerDamageType damageType = playerHealth != null ? playerHealth.LastDamageType : PlayerDamageType.Unknown;
        switch (damageType)
        {
            case PlayerDamageType.BurningAttack:
                StartCoroutine(BurningAttackDeath());
                break;
            case PlayerDamageType.ProlongedChaseBurn:
                StartCoroutine(ProlongedChaseDeath());
                break;
            default:
                StartCoroutine(PassiveBurnDeath());
                break;
        }
    }

    private IEnumerator PassiveBurnDeath()
    {
        Quaternion startRotation = transform.rotation;
        Quaternion fallRotation = startRotation * Quaternion.AngleAxis(fallAngle, Vector3.forward);
        float elapsed = 0f;

        while (elapsed < deathDuration)
        {
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.001f, deathDuration));
            Quaternion rolling = Quaternion.AngleAxis(rollDegreesPerSecond * elapsed, Vector3.right);
            transform.rotation = Quaternion.Slerp(startRotation, fallRotation, t) * rolling;
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        yield return ShowDeathPanel();
    }

    private IEnumerator BurningAttackDeath()
    {
        DetachCamera();
        Vector3 worldDirection = transform.TransformDirection(carriedDirection.normalized);
        if (worldDirection.sqrMagnitude < 0.001f)
        {
            worldDirection = transform.forward;
        }

        float elapsed = 0f;
        while (elapsed < deathDuration)
        {
            transform.position += worldDirection * carriedSpeed * Time.unscaledDeltaTime;
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        yield return ShowDeathPanel();
    }

    private IEnumerator ProlongedChaseDeath()
    {
        DetachCamera();
        Vector3 startPosition = transform.position;
        Vector3 targetPosition = new Vector3(startPosition.x, liftTargetHeight, startPosition.z);
        float elapsed = 0f;

        while (elapsed < deathDuration)
        {
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.001f, deathDuration));
            transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            LookCameraAtPlayer();
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        transform.position = targetPosition;
        LookCameraAtPlayer();
        yield return ShowDeathPanel();
    }

    private void DisablePlayerControl()
    {
        if (disableOnDeath != null)
        {
            for (int i = 0; i < disableOnDeath.Length; i++)
            {
                MonoBehaviour behaviour = disableOnDeath[i];
                if (behaviour != null && behaviour != this)
                {
                    behaviour.enabled = false;
                }
            }
        }

        if (characterController != null)
        {
            characterController.enabled = false;
        }
    }

    private void DetachCamera()
    {
        if (playerCamera != null && playerCamera.parent != null)
        {
            playerCamera.SetParent(null, true);
        }
    }

    private void LookCameraAtPlayer()
    {
        if (playerCamera == null)
        {
            return;
        }

        Vector3 direction = transform.position - playerCamera.position;
        if (direction.sqrMagnitude < 0.001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        playerCamera.rotation = Quaternion.Slerp(playerCamera.rotation, targetRotation, liftLookAtSpeed * Time.unscaledDeltaTime);
    }

    private void HideDeathPanel()
    {
        SetGraphicsAlpha01(deathPanelFadeGraphics, 0f);
        SetGraphicAlpha(pressAnyButtonGraphic, 0f);

        if (deathPanel != null)
        {
            deathPanel.SetActive(false);
        }
    }

    private IEnumerator ShowDeathPanel()
    {
        if (deathPanel == null)
        {
            yield break;
        }

        deathPanel.SetActive(true);
        SetGraphicsAlpha01(deathPanelFadeGraphics, 0f);
        SetGraphicAlpha(pressAnyButtonGraphic, 0f);

        float fadeDuration = Mathf.Max(0.001f, deathPanelFadeDuration);
        float fadeElapsed = 0f;
        while (fadeElapsed < fadeDuration)
        {
            float fade = Mathf.Clamp01(fadeElapsed / fadeDuration);
            fade = fade * fade * (3f - 2f * fade);
            SetGraphicsAlpha01(deathPanelFadeGraphics, fade);
            fadeElapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        SetGraphicsAlpha01(deathPanelFadeGraphics, 1f);

        float delayElapsed = 0f;
        while (delayElapsed < pressAnyButtonDelay)
        {
            SetGraphicAlpha(pressAnyButtonGraphic, 0f);
            delayElapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        float quitElapsed = 0f;
        while (quitElapsed < autoQuitDelay)
        {
            float blink = Mathf.PingPong(Time.unscaledTime * pressAnyButtonBlinkSpeed, 1f);
            SetGraphicAlpha(pressAnyButtonGraphic, Mathf.Lerp(pressAnyButtonMinAlpha, pressAnyButtonMaxAlpha, blink));
            if (WasAnyInputPressedThisFrame())
            {
                QuitGame();
                yield break;
            }

            quitElapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        QuitGame();
    }

    private void CacheDeathPanelAlphas()
    {
        if (deathPanelFadeGraphics == null)
        {
            deathPanelVisibleAlphas = null;
            return;
        }

        deathPanelVisibleAlphas = new float[deathPanelFadeGraphics.Length];
        for (int i = 0; i < deathPanelFadeGraphics.Length; i++)
        {
            deathPanelVisibleAlphas[i] = deathPanelFadeGraphics[i] != null ? deathPanelFadeGraphics[i].color.a : 1f;
        }
    }

    private void SetGraphicsAlpha01(Graphic[] graphics, float alpha01)
    {
        if (graphics == null)
        {
            return;
        }

        for (int i = 0; i < graphics.Length; i++)
        {
            float visibleAlpha = deathPanelVisibleAlphas != null && i < deathPanelVisibleAlphas.Length
                ? deathPanelVisibleAlphas[i]
                : 1f;
            SetGraphicAlpha(graphics[i], visibleAlpha * alpha01);
        }
    }

    private void SetGraphicAlpha(Graphic graphic, float alpha)
    {
        if (graphic == null)
        {
            return;
        }

        Color color = graphic.color;
        color.a = alpha;
        graphic.color = color;
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private bool WasAnyInputPressedThisFrame()
    {
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
        {
            return true;
        }

        if (Mouse.current != null
            && (Mouse.current.leftButton.wasPressedThisFrame
                || Mouse.current.rightButton.wasPressedThisFrame
                || Mouse.current.middleButton.wasPressedThisFrame))
        {
            return true;
        }

        if (Gamepad.current != null
            && (Gamepad.current.buttonSouth.wasPressedThisFrame
                || Gamepad.current.buttonNorth.wasPressedThisFrame
                || Gamepad.current.buttonEast.wasPressedThisFrame
                || Gamepad.current.buttonWest.wasPressedThisFrame
                || Gamepad.current.startButton.wasPressedThisFrame
                || Gamepad.current.selectButton.wasPressedThisFrame))
        {
            return true;
        }

        return false;
    }
}
