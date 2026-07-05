using UnityEngine;
using UnityEngine.UI;

public sealed class PlayerDamageFeedback : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Graphic hitOverlay;

    [Header("Feedback")]
    [SerializeField, Range(0f, 1f)] private float minHitAlpha = 0.1f;
    [SerializeField, Range(0f, 1f)] private float maxHitAlpha = 0.65f;
    [SerializeField] private float damageForMaxAlpha = 30f;
    [SerializeField] private float fadeOutSpeed = 4f;

    private float previousHealth;
    private float targetAlpha;

    private void Reset()
    {
        playerHealth = GetComponent<PlayerHealth>();
    }

    private void Awake()
    {
        if (playerHealth == null)
        {
            playerHealth = GetComponent<PlayerHealth>();
        }

        previousHealth = playerHealth != null ? playerHealth.CurrentHealth : 0f;
        SetOverlayAlpha(0f);
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
        if (hitOverlay == null)
        {
            return;
        }

        targetAlpha = Mathf.MoveTowards(targetAlpha, 0f, fadeOutSpeed * Time.unscaledDeltaTime);
        SetOverlayAlpha(targetAlpha);
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
}
