using UnityEngine;
using UnityEngine.UI;

public sealed class PlayerDamageFeedback : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerAudioController playerAudio;
    [SerializeField] private Graphic hitOverlay;
    [SerializeField] private ParticleSystem fireBall;

    [Header("Hit Overlay")]
    [SerializeField, Range(0f, 1f)] private float minHitAlpha = 0.1f;
    [SerializeField, Range(0f, 1f)] private float maxHitAlpha = 0.65f;
    [SerializeField] private float damageForMaxAlpha = 30f;
    [SerializeField] private float fadeOutSpeed = 4f;

    [Header("FireBall Burn DoT")]
    [SerializeField] private float maxFireBallStartSize = 1.5f;
    [SerializeField] private float fireBallSizePerDamagePerSecond = 0.1f;
    [SerializeField] private float burnSizeFallSpeed = 0.1f;
    [SerializeField] private float lingeringGraceDuration = 0.1f;
    [SerializeField] private float minimumVisibleStartSize = 0.001f;

    [Header("Controller Vibration")]
    [SerializeField, Range(0f, 1f)] private float vibrationLowFrequency = 0.25f;
    [SerializeField, Range(0f, 1f)] private float vibrationHighFrequency = 0.6f;
    [SerializeField, Min(0f)] private float vibrationDuration = 0.12f;
    [SerializeField, Min(0f)] private float vibrationCooldown = 0.15f;

    private float previousHealth;
    private float targetAlpha;
    private float fireBallStartSize;
    private float activeBurnDamagePerSecond;
    private float lastBurnReceiveTime = float.NegativeInfinity;
    private PlayerDamageType activeBurnDamageType = PlayerDamageType.PassiveBurn;
    private GameObject activeBurnSource;
    private float nextVibrationTime;

    private void Reset()
    {
        playerHealth = GetComponent<PlayerHealth>();
        fireBall = GetComponentInChildren<ParticleSystem>(true);
    }

    private void OnValidate()
    {
        damageForMaxAlpha = Mathf.Max(0.001f, damageForMaxAlpha);
        fadeOutSpeed = Mathf.Max(0f, fadeOutSpeed);
        maxFireBallStartSize = Mathf.Max(0f, maxFireBallStartSize);
        fireBallSizePerDamagePerSecond = Mathf.Max(0f, fireBallSizePerDamagePerSecond);
        burnSizeFallSpeed = Mathf.Max(0f, burnSizeFallSpeed);
        lingeringGraceDuration = Mathf.Max(0f, lingeringGraceDuration);
        minimumVisibleStartSize = Mathf.Max(0f, minimumVisibleStartSize);
    }

    private void Awake()
    {
        if (playerHealth == null)
        {
            playerHealth = GetComponent<PlayerHealth>();
        }

        if (playerAudio == null)
        {
            playerAudio = GetComponent<PlayerAudioController>();
        }

        previousHealth = playerHealth != null ? playerHealth.CurrentHealth : 0f;
        SetOverlayAlpha(0f);
        SetFireBallStartSize(0f);
    }

    private void OnEnable()
    {
        if (playerHealth != null)
        {
            playerHealth.HealthChanged += HandleHealthChanged;
        }
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.HealthChanged -= HandleHealthChanged;
        }
    }

    private void Update()
    {
        ApplyBurnDamageOverTime();
        ReduceBurnSize();

        targetAlpha = Mathf.MoveTowards(targetAlpha, 0f, fadeOutSpeed * Time.unscaledDeltaTime);
        SetOverlayAlpha(targetAlpha);
    }

    public void ReceiveBurnDamage(float damagePerSecond, PlayerDamageType damageType, GameObject damageSource)
    {
        if (playerHealth == null || playerHealth.IsDead || damagePerSecond <= 0f)
        {
            return;
        }

        float targetStartSize = Mathf.Clamp(damagePerSecond * fireBallSizePerDamagePerSecond, 0f, maxFireBallStartSize);
        if (targetStartSize <= 0f)
        {
            return;
        }

        if (targetStartSize > fireBallStartSize)
        {
            SetFireBallStartSize(targetStartSize);
        }

        activeBurnDamagePerSecond = damagePerSecond;
        activeBurnDamageType = damageType;
        activeBurnSource = damageSource;

        lastBurnReceiveTime = Time.time;

        float normalizedDamage = Mathf.Clamp01(damagePerSecond / damageForMaxAlpha);
        targetAlpha = Mathf.Max(targetAlpha, Mathf.Lerp(minHitAlpha, maxHitAlpha, normalizedDamage));
        SetOverlayAlpha(targetAlpha);
        playerAudio?.SetBurnLoopActive(true);
    }

    private void HandleHealthChanged(float currentHealth, float maxHealth)
    {
        float damage = previousHealth - currentHealth;
        previousHealth = currentHealth;

        if (damage <= 0f)
        {
            return;
        }

        float normalizedDamage = Mathf.Clamp01(damage / Mathf.Max(0.001f, damageForMaxAlpha));
        targetAlpha = Mathf.Max(targetAlpha, Mathf.Lerp(minHitAlpha, maxHitAlpha, normalizedDamage));
        SetOverlayAlpha(targetAlpha);
        playerAudio?.PlayDamage(playerHealth != null ? playerHealth.LastDamageType : PlayerDamageType.Unknown);

        if (Time.unscaledTime >= nextVibrationTime)
        {
            GameControlSettings.PlayVibration(vibrationLowFrequency, vibrationHighFrequency, vibrationDuration);
            nextVibrationTime = Time.unscaledTime + vibrationCooldown;
        }
    }

    private void SetOverlayAlpha(float alpha)
    {
        if (hitOverlay == null)
        {
            return;
        }

        Color color = hitOverlay.color;
        color.a = alpha;
        hitOverlay.color = color;
    }

    private void ApplyBurnDamageOverTime()
    {
        if (playerHealth == null || playerHealth.IsDead || fireBallStartSize <= minimumVisibleStartSize || activeBurnDamagePerSecond <= 0f)
        {
            return;
        }

        if (Time.time - lastBurnReceiveTime <= lingeringGraceDuration)
        {
            return;
        }

        float damageRatio = Mathf.Clamp01(fireBallStartSize);
        float damage = activeBurnDamagePerSecond * damageRatio * Time.deltaTime;
        playerHealth.TakeDamage(damage, activeBurnDamageType, activeBurnSource);
    }

    private void ReduceBurnSize()
    {
        if (fireBallStartSize <= 0f || burnSizeFallSpeed <= 0f)
        {
            return;
        }

        SetFireBallStartSize(Mathf.MoveTowards(fireBallStartSize, 0f, burnSizeFallSpeed * Time.deltaTime));
        if (fireBallStartSize <= minimumVisibleStartSize)
        {
            activeBurnDamagePerSecond = 0f;
            activeBurnSource = null;
            playerAudio?.SetBurnLoopActive(false);
        }
    }

    private void SetFireBallStartSize(float value)
    {
        fireBallStartSize = Mathf.Clamp(value, 0f, maxFireBallStartSize);

        if (fireBall == null)
        {
            return;
        }

        ParticleSystem.MainModule main = fireBall.main;
        main.startSizeMultiplier = fireBallStartSize;

        bool shouldShow = fireBallStartSize > minimumVisibleStartSize;
        if (shouldShow)
        {
            fireBall.gameObject.SetActive(true);
            if (!fireBall.isPlaying)
            {
                fireBall.Play(true);
            }
        }
        else
        {
            if (fireBall.isPlaying)
            {
                fireBall.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            fireBall.gameObject.SetActive(false);
        }
    }
}
