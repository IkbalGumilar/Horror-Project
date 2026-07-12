using UnityEngine;

public sealed class PlayerAudioController : MonoBehaviour
{
    [Header("Sources")]
    [SerializeField] private AudioSource oneShotSource;
    [SerializeField] private AudioSource breathSource;
    [SerializeField] private AudioSource burnLoopSource;

    [Header("Footsteps")]
    [SerializeField] private AudioClip[] walkFootstepClips;
    [SerializeField] private AudioClip[] sprintFootstepClips;
    [SerializeField] private AudioClip[] crouchFootstepClips;
    [SerializeField] private float walkStepInterval = 0.55f;
    [SerializeField] private float sprintStepInterval = 0.36f;
    [SerializeField] private float crouchStepInterval = 0.75f;
    [SerializeField, Range(0f, 1f)] private float footstepVolume = 0.75f;

    [Header("Breathing")]
    [SerializeField] private AudioClip normalBreathLoop;
    [SerializeField] private AudioClip tiredBreathLoop;
    [SerializeField] private float tiredStaminaThreshold = 0.3f;
    [SerializeField] private float breathFadeSpeed = 6f;
    [SerializeField, Range(0f, 1f)] private float normalBreathVolume = 0.18f;
    [SerializeField, Range(0f, 1f)] private float tiredBreathVolume = 0.45f;

    [Header("Damage")]
    [SerializeField] private AudioClip passiveDamageClip;
    [SerializeField] private AudioClip burningAttackDamageClip;
    [SerializeField] private AudioClip burnDamageLoop;
    [SerializeField] private float damageClipCooldown = 1f;
    [SerializeField, Range(0f, 1f)] private float damageClipVolume = 0.8f;
    [SerializeField, Range(0f, 1f)] private float burnLoopVolume = 0.45f;

    [Header("Fear Reactions")]
    [SerializeField] private AudioClip firstSeeGhostClip;
    [SerializeField] private AudioClip chasePanicClip;
    [SerializeField] private AudioClip lowStaminaPanicClip;
    [SerializeField, Range(0f, 1f)] private float fearReactionVolume = 0.75f;

    [Header("Death Screams")]
    [SerializeField] private AudioClip shortScreamClip;
    [SerializeField] private AudioClip finalScreamClip;
    [SerializeField] private AudioClip cutoffScreamClip;
    [SerializeField] private AudioClip airBurnScreamClip;
    [SerializeField, Range(0f, 1f)] private float screamVolume = 1f;

    private float nextStepTime;
    private float nextDamageClipTime;
    private bool isBeingChased;

    private void Awake()
    {
        oneShotSource = EnsureAudioSource(oneShotSource, "Player One Shot Audio", false);
        breathSource = EnsureAudioSource(breathSource, "Player Breath Audio", true);
        burnLoopSource = EnsureAudioSource(burnLoopSource, "Player Burn Audio", true);
    }

    private void Update()
    {
        UpdateBreathFade();
    }

    public void UpdateFootsteps(float horizontalSpeed, bool isGrounded, bool isSprinting, bool isCrouching)
    {
        if (!isGrounded || horizontalSpeed <= 0.1f || Time.time < nextStepTime)
        {
            return;
        }

        AudioClip[] clips = isCrouching ? crouchFootstepClips : isSprinting ? sprintFootstepClips : walkFootstepClips;
        PlayRandomOneShot(clips, footstepVolume);

        float interval = isCrouching ? crouchStepInterval : isSprinting ? sprintStepInterval : walkStepInterval;
        nextStepTime = Time.time + Mathf.Max(0.05f, interval);
    }

    public void UpdateBreathing(float normalizedStamina, bool isSprinting, bool isChasing)
    {
        if (breathSource == null)
        {
            return;
        }

        bool useTiredBreath = isBeingChased || isChasing || isSprinting || normalizedStamina <= tiredStaminaThreshold;
        AudioClip targetClip = useTiredBreath && tiredBreathLoop != null ? tiredBreathLoop : normalBreathLoop;
        if (targetClip == null)
        {
            if (breathSource.isPlaying)
            {
                breathSource.Stop();
            }

            breathSource.clip = null;
            return;
        }

        if (targetClip != null && breathSource.clip != targetClip)
        {
            breathSource.clip = targetClip;
            breathSource.loop = true;
            breathSource.Play();
        }
    }

    public void PlayDamage(PlayerDamageType damageType)
    {
        if (Time.time < nextDamageClipTime)
        {
            return;
        }

        AudioClip clip = damageType == PlayerDamageType.BurningAttack ? burningAttackDamageClip : passiveDamageClip;
        PlayOneShot(clip, damageClipVolume);
        nextDamageClipTime = Time.time + Mathf.Max(0f, damageClipCooldown);
    }

    public void SetBeingChased(bool value)
    {
        isBeingChased = value;
    }

    public void PlayFirstSeeGhost()
    {
        PlayOneShot(firstSeeGhostClip, fearReactionVolume);
    }

    public void PlayChasePanic()
    {
        PlayOneShot(chasePanicClip, fearReactionVolume);
    }

    public void PlayLowStaminaPanic()
    {
        PlayOneShot(lowStaminaPanicClip, fearReactionVolume);
    }

    public void SetBurnLoopActive(bool active)
    {
        if (burnLoopSource == null)
        {
            return;
        }

        if (active && burnDamageLoop != null)
        {
            if (burnLoopSource.clip != burnDamageLoop)
            {
                burnLoopSource.clip = burnDamageLoop;
                burnLoopSource.loop = true;
            }

            if (!burnLoopSource.isPlaying)
            {
                burnLoopSource.Play();
            }

            burnLoopSource.volume = burnLoopVolume * GameAudioManager.SfxVolume;
        }
        else if (burnLoopSource.isPlaying)
        {
            burnLoopSource.Stop();
        }
    }

    public void PlayShortScream()
    {
        PlayOneShot(shortScreamClip, screamVolume);
    }

    public void PlayFinalScream()
    {
        PlayOneShot(finalScreamClip, screamVolume);
    }

    public void PlayCutoffScream()
    {
        PlayOneShot(cutoffScreamClip, screamVolume);
    }

    public void PlayAirBurnScream()
    {
        PlayOneShot(airBurnScreamClip, screamVolume);
    }

    private void UpdateBreathFade()
    {
        if (breathSource == null)
        {
            return;
        }

        float targetVolume = breathSource.clip == tiredBreathLoop ? tiredBreathVolume : normalBreathVolume;
        if (breathSource.clip == null)
        {
            targetVolume = 0f;
        }

        targetVolume *= GameAudioManager.SfxVolume;
        breathSource.volume = Mathf.MoveTowards(breathSource.volume, targetVolume, breathFadeSpeed * Time.unscaledDeltaTime);
    }

    private void PlayRandomOneShot(AudioClip[] clips, float volume)
    {
        if (clips == null || clips.Length == 0)
        {
            return;
        }

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        PlayOneShot(clip, volume);
    }

    private void PlayOneShot(AudioClip clip, float volume)
    {
        if (clip == null || oneShotSource == null)
        {
            return;
        }

        oneShotSource.PlayOneShot(clip, volume * GameAudioManager.SfxVolume);
    }

    private AudioSource EnsureAudioSource(AudioSource source, string sourceName, bool loop)
    {
        if (source != null)
        {
            source.loop = loop;
            source.playOnAwake = false;
            return source;
        }

        GameObject sourceObject = new GameObject(sourceName);
        sourceObject.transform.SetParent(transform, false);
        AudioSource newSource = sourceObject.AddComponent<AudioSource>();
        newSource.playOnAwake = false;
        newSource.loop = loop;
        newSource.spatialBlend = 0f;
        return newSource;
    }
}
