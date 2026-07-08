using UnityEngine;

public sealed class PlasmaBurnAttack : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GhostNavMeshEnemy ghostEnemy;
    [SerializeField] private PlayerHealth targetHealth;
    [SerializeField] private PlayerDamageFeedback targetDamageFeedback;
    [SerializeField] private string targetTag = "Player";

    [Header("Passive Burn")]
    [SerializeField] private float passiveRange = 8f;
    [SerializeField] private float minDamagePerSecond = 1f;
    [SerializeField] private float maxDamagePerSecond = 8f;

    [Header("Burning Attack")]
    [SerializeField] private bool burningAttackActive;
    [SerializeField] private bool activateWhenChasing = true;
    [SerializeField] private float burningAttackMultiplier = 2.5f;
    [SerializeField] private float prolongedChaseDeathThreshold = 3f;
    [SerializeField] private bool currentBurningAttackActive;
    [SerializeField] private bool currentBurningDamageBoostActive;
    [SerializeField] private float currentAppliedDamageMultiplier = 1f;

    [Header("Death Scene Trigger")]
    [SerializeField] private Vector3 directDeathMaxOffset = Vector3.one;

    public bool IsBurningAttackActive => currentBurningAttackActive;
    public float EffectiveRange => GetEffectiveRange(IsBurningAttackActive);
    public float CurrentDamagePerSecond { get; private set; }

    private void OnValidate()
    {
        passiveRange = Mathf.Max(0f, passiveRange);
        minDamagePerSecond = Mathf.Max(0f, minDamagePerSecond);
        maxDamagePerSecond = Mathf.Max(minDamagePerSecond, maxDamagePerSecond);
        burningAttackMultiplier = Mathf.Max(1f, burningAttackMultiplier);
        directDeathMaxOffset.x = Mathf.Max(0f, directDeathMaxOffset.x);
        directDeathMaxOffset.y = Mathf.Max(0f, directDeathMaxOffset.y);
        directDeathMaxOffset.z = Mathf.Max(0f, directDeathMaxOffset.z);
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
        if (targetHealth == null || targetDamageFeedback == null)
        {
            ResolveTarget();
        }

        if (targetHealth == null || targetHealth.IsDead)
        {
            currentBurningAttackActive = false;
            currentBurningDamageBoostActive = false;
            currentAppliedDamageMultiplier = 1f;
            CurrentDamagePerSecond = 0f;
            return;
        }

        UpdateBurningAttackState();
        bool burningDamageBoostActive = IsBurningAttackActive;
        bool canApplyDirectBurningAttack = CanApplyDirectBurningAttack();
        currentBurningDamageBoostActive = burningDamageBoostActive;
        currentAppliedDamageMultiplier = burningDamageBoostActive ? burningAttackMultiplier : 1f;

        float range = Mathf.Max(0.001f, GetEffectiveRange(burningDamageBoostActive));
        float distance = Vector3.Distance(transform.position, targetHealth.transform.position);
        if (distance > range)
        {
            CurrentDamagePerSecond = 0f;
            return;
        }

        float closeness = 1f - Mathf.Clamp01(distance / range);
        CurrentDamagePerSecond = Mathf.Lerp(minDamagePerSecond, maxDamagePerSecond, closeness);

        if (burningDamageBoostActive)
        {
            CurrentDamagePerSecond *= currentAppliedDamageMultiplier;
        }

        PlayerDamageType damageType = GetDamageType(canApplyDirectBurningAttack);
        if (targetDamageFeedback != null)
        {
            targetDamageFeedback.ReceiveBurnDamage(CurrentDamagePerSecond, damageType, gameObject);
        }

        targetHealth.TakeDamage(CurrentDamagePerSecond * Time.deltaTime, damageType, gameObject);
    }

    public void SetBurningAttackActive(bool active)
    {
        burningAttackActive = active;
        UpdateBurningAttackState();
    }

    public void ActivateBurningAttack()
    {
        SetBurningAttackActive(true);
    }

    public void DeactivateBurningAttack()
    {
        SetBurningAttackActive(false);
    }

    private float GetEffectiveRange(bool canApplyBurningAttack)
    {
        return passiveRange * (canApplyBurningAttack ? burningAttackMultiplier : 1f);
    }

    private bool CanApplyDirectBurningAttack()
    {
        if (!IsBurningAttackActive)
        {
            return false;
        }

        return ghostEnemy == null || ghostEnemy.HasLineOfSightToTarget;
    }

    private void UpdateBurningAttackState()
    {
        currentBurningAttackActive = burningAttackActive || (activateWhenChasing && ghostEnemy != null && ghostEnemy.IsChasing);
    }

    private PlayerDamageType GetDamageType(bool canApplyBurningAttack)
    {
        bool canTriggerDirectDeathScene = IsWithinDirectDeathOffset();
        if (canTriggerDirectDeathScene && ghostEnemy != null && ghostEnemy.ChaseDuration >= prolongedChaseDeathThreshold)
        {
            return PlayerDamageType.ProlongedChaseBurn;
        }

        return canTriggerDirectDeathScene && canApplyBurningAttack ? PlayerDamageType.BurningAttack : PlayerDamageType.PassiveBurn;
    }

    private bool IsWithinDirectDeathOffset()
    {
        if (targetHealth == null)
        {
            return false;
        }

        Vector3 offset = targetHealth.transform.position - transform.position;
        return Mathf.Abs(offset.x) <= directDeathMaxOffset.x
            && Mathf.Abs(offset.y) <= directDeathMaxOffset.y
            && Mathf.Abs(offset.z) <= directDeathMaxOffset.z;
    }

    private void ResolveTarget()
    {
        if (targetHealth != null && targetDamageFeedback == null)
        {
            targetDamageFeedback = targetHealth.GetComponent<PlayerDamageFeedback>();
        }

        if ((targetHealth != null && targetDamageFeedback != null) || string.IsNullOrWhiteSpace(targetTag))
        {
            return;
        }

        GameObject targetObject = GameObject.FindGameObjectWithTag(targetTag);
        if (targetObject != null)
        {
            if (targetHealth == null)
            {
                targetHealth = targetObject.GetComponent<PlayerHealth>();
            }

            if (targetDamageFeedback == null)
            {
                targetDamageFeedback = targetObject.GetComponent<PlayerDamageFeedback>();
            }
        }
    }
}
