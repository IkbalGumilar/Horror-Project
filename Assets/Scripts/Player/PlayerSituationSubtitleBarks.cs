using UnityEngine;

public sealed class PlayerSituationSubtitleBarks : MonoBehaviour
{
    private const int SightHitBufferSize = 32;

    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerStamina playerStamina;
    [SerializeField] private PlayerAudioController playerAudio;
    [SerializeField] private GhostNavMeshEnemy ghostEnemy;
    [SerializeField] private Transform viewOrigin;

    [Header("Ghost Sight")]
    [SerializeField] private float ghostSightDistance = 35f;
    [SerializeField, Range(-1f, 1f)] private float viewDotThreshold = 0.55f;
    [SerializeField] private LayerMask sightBlockMask = ~0;

    [Header("Cooldowns")]
    [SerializeField] private float minimumGapBetweenBarks = 2f;
    [SerializeField] private float chaseBarkCooldown = 8f;
    [SerializeField] private float damageBarkCooldown = 5f;
    [SerializeField] private float lowStaminaBarkCooldown = 6f;
    [SerializeField, Range(0f, 1f)] private float lowStaminaThreshold = 0.2f;

    [Header("Subtitle Duration")]
    [SerializeField] private float firstSightDuration = 3f;
    [SerializeField] private float chaseDuration = 2.5f;
    [SerializeField] private float damageDuration = 2.5f;
    [SerializeField] private float lowStaminaDuration = 2.5f;

    [Header("Subtitle Keys")]
    [SerializeField] private string[] firstSightKeys = { "subtitle.player_first_see_banaspati" };
    [SerializeField] private string[] chaseKeys = { "subtitle.player_chased" };
    [SerializeField] private string[] lowStaminaChaseKeys = { "subtitle.player_low_stamina_chased" };
    [SerializeField] private string[] passiveDamageBeforeSeeingKeys = { "subtitle.player_burn_hit_before_seeing_banaspati" };
    [SerializeField] private string[] passiveDamageAfterSeeingKeys = { "subtitle.player_burn_hit" };
    [SerializeField] private string[] burningAttackDamageKeys = { "subtitle.player_burning_attack_hit" };

    private bool hasSeenGhost;
    private bool deathBarkShown;
    private float nextAnyBarkTime = -999f;
    private float nextChaseBarkTime = -999f;
    private float nextDamageBarkTime = -999f;
    private float nextLowStaminaBarkTime = -999f;
    private float previousHealth;
    private Collider[] ownColliders;
    private readonly RaycastHit[] sightHits = new RaycastHit[SightHitBufferSize];

    private void Reset()
    {
        playerHealth = GetComponent<PlayerHealth>();
        playerStamina = GetComponent<PlayerStamina>();
    }

    private void Awake()
    {
        if (playerHealth == null)
        {
            playerHealth = GetComponent<PlayerHealth>();
        }

        if (playerStamina == null)
        {
            playerStamina = GetComponent<PlayerStamina>();
        }

        if (playerAudio == null)
        {
            playerAudio = GetComponent<PlayerAudioController>();
        }

        if (viewOrigin == null && Camera.main != null)
        {
            viewOrigin = Camera.main.transform;
        }

        if (ghostEnemy == null)
        {
            ghostEnemy = FindFirstObjectByType<GhostNavMeshEnemy>();
        }

        ownColliders = GetComponentsInChildren<Collider>();
        previousHealth = playerHealth != null ? playerHealth.CurrentHealth : 0f;
    }

    private void OnEnable()
    {
        if (playerHealth != null)
        {
            playerHealth.HealthChanged += HandleHealthChanged;
            playerHealth.Died += HandleDeathStarted;
        }
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.HealthChanged -= HandleHealthChanged;
            playerHealth.Died -= HandleDeathStarted;
        }
    }

    private void Update()
    {
        if (deathBarkShown || ghostEnemy == null)
        {
            playerAudio?.SetBeingChased(false);
            return;
        }

        playerAudio?.SetBeingChased(ghostEnemy.IsChasing);

        if (!hasSeenGhost && PlayerCanSeeGhost())
        {
            hasSeenGhost = true;
            ShowBark(PickRandomKey(firstSightKeys, "subtitle.player_first_see_banaspati"), firstSightDuration, true);
            playerAudio?.PlayFirstSeeGhost();
            return;
        }

        if (ghostEnemy.IsChasing
            && playerStamina != null
            && playerStamina.NormalizedStamina <= lowStaminaThreshold
            && Time.time >= nextLowStaminaBarkTime)
        {
            if (ShowBark(PickRandomKey(lowStaminaChaseKeys, "subtitle.player_low_stamina_chased"), lowStaminaDuration, false))
            {
                nextLowStaminaBarkTime = Time.time + lowStaminaBarkCooldown;
                playerAudio?.PlayLowStaminaPanic();
            }
        }

        if (ghostEnemy.IsChasing && Time.time >= nextChaseBarkTime)
        {
            if (ShowBark(PickRandomKey(chaseKeys, "subtitle.player_chased"), chaseDuration, false))
            {
                nextChaseBarkTime = Time.time + chaseBarkCooldown;
                playerAudio?.PlayChasePanic();
            }
        }
    }

    private void HandleHealthChanged(float currentHealth, float maxHealth)
    {
        float damage = previousHealth - currentHealth;
        previousHealth = currentHealth;

        if (deathBarkShown || damage <= 0f || Time.time < nextDamageBarkTime)
        {
            return;
        }

        string key = GetDamageSubtitleKey();

        if (ShowBark(key, damageDuration, false))
        {
            nextDamageBarkTime = Time.time + damageBarkCooldown;
        }
    }

    private bool ShowBark(string subtitleKey, float duration, bool ignoreGap)
    {
        if (SubtitleController.Instance == null)
        {
            return false;
        }

        if (!ignoreGap && Time.time < nextAnyBarkTime)
        {
            return false;
        }

        SubtitleController.Instance.ShowLocalized("speaker.player", subtitleKey, duration);
        nextAnyBarkTime = Time.time + minimumGapBetweenBarks;
        return true;
    }

    private string GetDamageSubtitleKey()
    {
        if (playerHealth != null && playerHealth.LastDamageType == PlayerDamageType.BurningAttack)
        {
            return PickRandomKey(burningAttackDamageKeys, "subtitle.player_burning_attack_hit");
        }

        if (!hasSeenGhost)
        {
            return PickRandomKey(passiveDamageBeforeSeeingKeys, "subtitle.player_burn_hit_before_seeing_banaspati");
        }

        return PickRandomKey(passiveDamageAfterSeeingKeys, "subtitle.player_burn_hit");
    }

    private string PickRandomKey(string[] keys, string fallbackKey)
    {
        if (keys == null || keys.Length == 0)
        {
            return fallbackKey;
        }

        for (int attempt = 0; attempt < keys.Length; attempt++)
        {
            string key = keys[Random.Range(0, keys.Length)];
            if (!string.IsNullOrWhiteSpace(key))
            {
                return key;
            }
        }

        return fallbackKey;
    }

    private void HandleDeathStarted()
    {
        deathBarkShown = true;
    }

    private bool PlayerCanSeeGhost()
    {
        if (viewOrigin == null || ghostEnemy == null)
        {
            return false;
        }

        Vector3 origin = viewOrigin.position;
        Vector3 targetPosition = ghostEnemy.transform.position;
        Vector3 direction = targetPosition - origin;
        float distance = direction.magnitude;
        if (distance <= 0.001f || distance > ghostSightDistance)
        {
            return false;
        }

        Vector3 normalizedDirection = direction / distance;
        if (Vector3.Dot(viewOrigin.forward, normalizedDirection) < viewDotThreshold)
        {
            return false;
        }

        int hitCount = Physics.RaycastNonAlloc(
            origin,
            normalizedDirection,
            sightHits,
            distance,
            sightBlockMask,
            QueryTriggerInteraction.Ignore);
        if (hitCount == 0)
        {
            return true;
        }

        Collider closestCollider = null;
        float closestDistance = float.PositiveInfinity;
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = sightHits[i];
            Collider hitCollider = hit.collider;
            if (hitCollider == null || IsOwnCollider(hitCollider) || hit.distance >= closestDistance)
            {
                continue;
            }

            closestDistance = hit.distance;
            closestCollider = hitCollider;
        }

        if (closestCollider == null)
        {
            return true;
        }

        Transform hitTransform = closestCollider.transform;
        return hitTransform == ghostEnemy.transform || hitTransform.IsChildOf(ghostEnemy.transform);
    }

    private bool IsOwnCollider(Collider hitCollider)
    {
        if (ownColliders == null)
        {
            return false;
        }

        for (int i = 0; i < ownColliders.Length; i++)
        {
            if (hitCollider == ownColliders[i])
            {
                return true;
            }
        }

        return false;
    }
}
