using UnityEngine;

public sealed class PlasmaBurnAttack : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GhostNavMeshEnemy ghostEnemy;
    [SerializeField] private PlayerHealth targetHealth;
    [SerializeField] private string targetTag = "Player";

    [Header("Passive Burn")]
    [SerializeField] private float passiveRange = 8f;
    [SerializeField] private float minDamagePerSecond = 1f;
    [SerializeField] private float maxDamagePerSecond = 8f;

    [Header("Burning Attack")]
    [SerializeField] private bool burningAttackActive;
    [SerializeField] private bool activateWhenChasing = true;
    [SerializeField] private float burningAttackMultiplier = 2.5f;
    [SerializeField] private float prolongedChaseDeathThreshold = 5f;

    public bool IsBurningAttackActive => burningAttackActive || (activateWhenChasing && ghostEnemy != null && ghostEnemy.IsChasing);
    public float EffectiveRange => passiveRange * (IsBurningAttackActive ? burningAttackMultiplier : 1f);
    public float CurrentDamagePerSecond { get; private set; }

    private void OnValidate()
    {
        passiveRange = Mathf.Max(0f, passiveRange);
        minDamagePerSecond = Mathf.Max(0f, minDamagePerSecond);
        maxDamagePerSecond = Mathf.Max(minDamagePerSecond, maxDamagePerSecond);
        burningAttackMultiplier = Mathf.Max(1f, burningAttackMultiplier);
    }

    private void Reset()
    {
        ghostEnemy = GetComponent<GhostNavMeshEnemy>();
    }

    private void Awake()
    {
        if (ghostEnemy == null)
        {
            ghostEnemy = GetComponent<GhostNavMeshEnemy>();
        }
    }

    private void Start()
    {
        ResolveTarget();
    }

    private void Update()
    {
        if (targetHealth == null)
        {
            ResolveTarget();
        }

        if (targetHealth == null || targetHealth.IsDead)
        {
            CurrentDamagePerSecond = 0f;
            return;
        }

        float range = Mathf.Max(0.001f, EffectiveRange);
        float distance = Vector3.Distance(transform.position, targetHealth.transform.position);
        if (distance > range)
        {
            CurrentDamagePerSecond = 0f;
            return;
        }

        if (IsBurningAttackActive && ghostEnemy != null && !ghostEnemy.HasLineOfSightToTarget)
        {
            CurrentDamagePerSecond = 0f;
            return;
        }

        float closeness = 1f - Mathf.Clamp01(distance / range);
        CurrentDamagePerSecond = Mathf.Lerp(minDamagePerSecond, maxDamagePerSecond, closeness);

        if (IsBurningAttackActive)
        {
            CurrentDamagePerSecond *= burningAttackMultiplier;
        }

        targetHealth.TakeDamage(CurrentDamagePerSecond * Time.deltaTime, GetDamageType(), gameObject);
    }

    public void SetBurningAttackActive(bool active)
    {
        burningAttackActive = active;
    }

    public void ActivateBurningAttack()
    {
        SetBurningAttackActive(true);
    }

    public void DeactivateBurningAttack()
    {
        SetBurningAttackActive(false);
    }

    private PlayerDamageType GetDamageType()
    {
        if (ghostEnemy != null && ghostEnemy.ChaseDuration >= prolongedChaseDeathThreshold)
        {
            return PlayerDamageType.ProlongedChaseBurn;
        }

        return IsBurningAttackActive ? PlayerDamageType.BurningAttack : PlayerDamageType.PassiveBurn;
    }

    private void ResolveTarget()
    {
        if (targetHealth != null || string.IsNullOrWhiteSpace(targetTag))
        {
            return;
        }

        GameObject targetObject = GameObject.FindGameObjectWithTag(targetTag);
        if (targetObject != null)
        {
            targetHealth = targetObject.GetComponent<PlayerHealth>();
        }
    }
}
