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
    [SerializeField] private float capturedOffset = 0.5f;
    [SerializeField] private float draggedDistance = 3f;
    [SerializeField] private float burningSceneDuration = 3f;
    [SerializeField] private Vector3 carriedDirection = Vector3.forward;

    [Header("Prolonged Chase Death")]
    [SerializeField] private float liftTargetHeight = 20f;
    [SerializeField] private float liftLookAtSpeed = 12f;
    [SerializeField] private float prolongedFireScaleMultiplier = 3f;
    [SerializeField] private float prolongedBurnDuration = 3f;
    [SerializeField] private float prolongedFallDuration = 3f;
    [SerializeField] private Color deathFireColor = new Color(0.1f, 0.45f, 1f, 1f);
    [SerializeField] private float deathFireStartSpeed = 3f;
    [SerializeField] private float deathFireEndSpeed = 6f;

    [Header("Death Subtitles")]
    [SerializeField] private string deathSubtitleSpeakerKey = "speaker.player";
    [SerializeField] private float deathScreamDuration = 0.75f;
    [SerializeField] private float deathBigScreamDuration = 1.4f;
    [SerializeField] private float deathBigSubtitleScale = 1.65f;

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
        bool firstScreamShown = false;
        bool secondScreamShown = false;

        while (elapsed < deathDuration)
        {
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.001f, deathDuration));
            if (!firstScreamShown && t >= 0.2f)
            {
                firstScreamShown = true;
                ShowDeathSubtitle("subtitle.player_scream_short", deathScreamDuration, 1f);
            }

            if (!secondScreamShown && t >= 0.6f)
            {
                secondScreamShown = true;
                ShowDeathSubtitle("subtitle.player_scream_short", deathScreamDuration, 1f);
            }

            Quaternion rolling = Quaternion.AngleAxis(rollDegreesPerSecond * elapsed, Vector3.right);
            transform.rotation = Quaternion.Slerp(startRotation, fallRotation, t) * rolling;
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        ShowDeathSubtitle("subtitle.player_scream_final_large", deathBigScreamDuration, deathBigSubtitleScale);
        yield return new WaitForSecondsRealtime(deathBigScreamDuration);
        yield return ShowDeathPanel();
    }

    private IEnumerator BurningAttackDeath()
    {
        DetachCamera();
        Transform damageSource = GetDamageSourceTransform();
        DisableDamageSourceControl(damageSource);
        ParticleSystem[] fireParticles = PrepareDeathFire(damageSource, false);
        Vector3 dragDirection = GetCarryDirection(damageSource).normalized;
        Vector3 startGhostPosition = GetCapturedGhostPosition(damageSource, dragDirection);
        Vector3 targetGhostPosition = startGhostPosition + dragDirection * Mathf.Max(0f, draggedDistance);
        Vector3 sourceToPlayerOffset = -dragDirection * Mathf.Max(0f, capturedOffset);
        float sceneDuration = Mathf.Max(0.001f, burningSceneDuration);
        float elapsed = 0f;
        bool firstScreamShown = false;
        bool secondScreamShown = false;

        while (elapsed < sceneDuration)
        {
            float t = Mathf.Clamp01(elapsed / sceneDuration);
            if (!firstScreamShown && t >= 0.2f)
            {
                firstScreamShown = true;
                ShowDeathSubtitle("subtitle.player_scream_short", deathScreamDuration, 1f);
            }

            if (!secondScreamShown && t >= 0.55f)
            {
                secondScreamShown = true;
                ShowDeathSubtitle("subtitle.player_scream_short", deathScreamDuration, 1f);
            }

            Vector3 sourcePosition = Vector3.Lerp(startGhostPosition, targetGhostPosition, t);
            if (damageSource != null)
            {
                damageSource.position = sourcePosition;
            }

            transform.position = sourcePosition + sourceToPlayerOffset;
            SetDeathFireSpeed(fireParticles, Mathf.Lerp(deathFireStartSpeed, deathFireEndSpeed, t));
            LookCameraAtPlayer();
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (damageSource != null)
        {
            damageSource.position = targetGhostPosition;
        }

        transform.position = targetGhostPosition + sourceToPlayerOffset;
        LookCameraAtPlayer();
        ShowDeathSubtitle("subtitle.player_scream_cutoff", deathScreamDuration, 1f);
        yield return new WaitForSecondsRealtime(deathScreamDuration);
        yield return ShowDeathPanel();
    }

    private IEnumerator ProlongedChaseDeath()
    {
        Transform damageSource = GetDamageSourceTransform();
        DisableDamageSourceControl(damageSource);
        ParticleSystem[] fireParticles = PrepareDeathFire(damageSource, true);

        Vector3 startPosition = transform.position;
        Vector3 targetPosition = new Vector3(startPosition.x, liftTargetHeight, startPosition.z);
        float burnDuration = Mathf.Max(0.001f, prolongedBurnDuration);
        float elapsed = 0f;
        int liftScreamsShown = 0;

        while (elapsed < burnDuration)
        {
            float t = Mathf.Clamp01(elapsed / burnDuration);
            if (liftScreamsShown < 3 && t >= (liftScreamsShown + 1) * 0.25f)
            {
                liftScreamsShown++;
                ShowDeathSubtitle("subtitle.player_scream_short", deathScreamDuration, 1f);
            }

            transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            CarryDamageSourceNearPlayer(damageSource, transform.position);
            SetDeathFireSpeed(fireParticles, Mathf.Lerp(deathFireStartSpeed, deathFireEndSpeed, t));
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        transform.position = targetPosition;
        CarryDamageSourceNearPlayer(damageSource, transform.position);
        ShowDeathSubtitle("subtitle.player_scream_air_burn", deathBigScreamDuration, deathBigSubtitleScale);
        DetachCamera();
        LookCameraAtPlayer();

        Vector3 fallStartPosition = transform.position;
        Vector3 fallTargetPosition = new Vector3(fallStartPosition.x, 0f, fallStartPosition.z);
        float fallDuration = Mathf.Max(0.001f, prolongedFallDuration);
        elapsed = 0f;

        while (elapsed < fallDuration)
        {
            float t = Mathf.Clamp01(elapsed / fallDuration);
            float eased = t * t * (3f - 2f * t);
            transform.position = Vector3.Lerp(fallStartPosition, fallTargetPosition, eased);
            CarryDamageSourceNearPlayer(damageSource, transform.position);
            SetDeathFireSpeed(fireParticles, deathFireEndSpeed);
            LookCameraAtPlayer();
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        transform.position = fallTargetPosition;
        CarryDamageSourceNearPlayer(damageSource, transform.position);
        LookCameraAtPlayer();
        yield return ShowDeathPanel();
    }

    private void ShowDeathSubtitle(string subtitleKey, float duration, float subtitleScale)
    {
        if (SubtitleController.Instance == null)
        {
            return;
        }

        SubtitleController.Instance.ShowLocalized(deathSubtitleSpeakerKey, subtitleKey, duration, subtitleScale);
    }

    private Transform GetDamageSourceTransform()
    {
        if (playerHealth == null || playerHealth.LastDamageSource == null)
        {
            return null;
        }

        return playerHealth.LastDamageSource.transform;
    }

    private void DisableDamageSourceControl(Transform damageSource)
    {
        if (damageSource == null)
        {
            return;
        }

        GhostNavMeshEnemy ghostEnemy = damageSource.GetComponent<GhostNavMeshEnemy>();
        if (ghostEnemy != null)
        {
            ghostEnemy.enabled = false;
        }

        PlasmaBurnAttack burnAttack = damageSource.GetComponent<PlasmaBurnAttack>();
        if (burnAttack != null)
        {
            burnAttack.enabled = false;
        }
    }

    private Vector3 GetCapturedGhostPosition(Transform damageSource, Vector3 dragDirection)
    {
        if (damageSource != null)
        {
            return transform.position + dragDirection * Mathf.Max(0f, capturedOffset);
        }

        return transform.position;
    }

    private void CarryDamageSourceNearPlayer(Transform damageSource, Vector3 playerPosition)
    {
        if (damageSource == null)
        {
            return;
        }

        Vector3 direction = damageSource.position - playerPosition;
        if (direction.sqrMagnitude < 0.001f)
        {
            direction = transform.forward;
        }

        direction.Normalize();
        damageSource.position = playerPosition + direction * Mathf.Max(0f, capturedOffset);
    }

    private Vector3 GetCarryDirection(Transform damageSource)
    {
        if (damageSource != null)
        {
            Vector3 directionFromSource = transform.position - damageSource.position;
            directionFromSource.y = 0f;
            if (directionFromSource.sqrMagnitude > 0.001f)
            {
                return directionFromSource.normalized;
            }

            Vector3 sourceForward = damageSource.forward;
            sourceForward.y = 0f;
            if (sourceForward.sqrMagnitude > 0.001f)
            {
                return sourceForward.normalized;
            }
        }

        Vector3 fallbackDirection = transform.TransformDirection(carriedDirection.normalized);
        fallbackDirection.y = 0f;
        if (fallbackDirection.sqrMagnitude > 0.001f)
        {
            return fallbackDirection.normalized;
        }

        return transform.forward.normalized;
    }

    private ParticleSystem[] PrepareDeathFire(Transform damageSource, bool scaleRootParticles)
    {
        if (damageSource == null)
        {
            return null;
        }

        ParticleSystem[] particleSystems = damageSource.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem particleSystem = particleSystems[i];
            if (particleSystem == null)
            {
                continue;
            }

            ParticleSystem.MainModule main = particleSystem.main;
            main.startColor = deathFireColor;
            main.simulationSpeed = Mathf.Max(0.01f, deathFireStartSpeed);

            ParticleSystemRenderer particleRenderer = particleSystem.GetComponent<ParticleSystemRenderer>();
            if (particleRenderer != null)
            {
                Material material = particleRenderer.material;
                if (material != null)
                {
                    if (material.HasProperty("_BaseColor"))
                    {
                        material.SetColor("_BaseColor", deathFireColor);
                    }
                    else if (material.HasProperty("_Color"))
                    {
                        material.SetColor("_Color", deathFireColor);
                    }
                }
            }

            if (scaleRootParticles && !HasParticleSystemAncestor(particleSystem.transform, damageSource))
            {
                particleSystem.transform.localScale *= Mathf.Max(0.01f, prolongedFireScaleMultiplier);
            }
        }

        return particleSystems;
    }

    private void SetDeathFireSpeed(ParticleSystem[] particleSystems, float speed)
    {
        if (particleSystems == null)
        {
            return;
        }

        float safeSpeed = Mathf.Max(0.01f, speed);
        for (int i = 0; i < particleSystems.Length; i++)
        {
            if (particleSystems[i] == null)
            {
                continue;
            }

            ParticleSystem.MainModule main = particleSystems[i].main;
            main.simulationSpeed = safeSpeed;
        }
    }

    private bool HasParticleSystemAncestor(Transform particleTransform, Transform root)
    {
        Transform parent = particleTransform.parent;
        while (parent != null && parent != root)
        {
            if (parent.GetComponent<ParticleSystem>() != null)
            {
                return true;
            }

            parent = parent.parent;
        }

        return false;
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
