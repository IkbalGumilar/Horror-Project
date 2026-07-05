using System;
using UnityEngine;

public enum PlayerDamageType
{
    Unknown,
    PassiveBurn,
    BurningAttack,
    ProlongedChaseBurn
}

public sealed class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = 100f;
    [SerializeField] private bool startFull = true;

    [Header("Passive Healing")]
    [SerializeField, Range(0f, 1f)] private float passiveHealPercentPerSecond = 0.001f;

    public event Action<float, float> HealthChanged;
    public event Action Died;

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public float NormalizedHealth => maxHealth > 0f ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;
    public bool IsDead => currentHealth <= 0f;
    public PlayerDamageType LastDamageType { get; private set; }
    public GameObject LastDamageSource { get; private set; }

    private void OnValidate()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
    }

    private void Awake()
    {
        if (startFull)
        {
            currentHealth = maxHealth;
        }

        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
    }

    private void Update()
    {
        if (IsDead || currentHealth >= maxHealth || passiveHealPercentPerSecond <= 0f)
        {
            return;
        }

        Heal(maxHealth * passiveHealPercentPerSecond * Time.deltaTime);
    }

    public void TakeDamage(float amount)
    {
        TakeDamage(amount, PlayerDamageType.Unknown, null);
    }

    public void TakeDamage(float amount, PlayerDamageType damageType, GameObject damageSource)
    {
        if (amount <= 0f || IsDead)
        {
            return;
        }

        LastDamageType = damageType;
        LastDamageSource = damageSource;
        SetHealth(currentHealth - amount);
    }

    public void Heal(float amount)
    {
        if (amount <= 0f || IsDead)
        {
            return;
        }

        SetHealth(currentHealth + amount);
    }

    public void RestoreFullHealth()
    {
        SetHealth(maxHealth);
    }

    public void SetHealth(float value)
    {
        float previousHealth = currentHealth;
        currentHealth = Mathf.Clamp(value, 0f, maxHealth);

        if (Mathf.Approximately(previousHealth, currentHealth))
        {
            return;
        }

        HealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0f)
        {
            Died?.Invoke();
        }
    }
}
