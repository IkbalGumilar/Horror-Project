using UnityEngine;

public sealed class GhostAudioController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GhostNavMeshEnemy ghostEnemy;
    [SerializeField] private PlasmaBurnAttack burnAttack;
    [SerializeField] private AudioSource loopSource;
    [SerializeField] private AudioSource oneShotSource;

    [Header("Banaspati Voice")]
    [SerializeField] private AudioClip idleLoop;
    [SerializeField] private AudioClip chaseLoop;
    [SerializeField] private AudioClip burningAttackLoop;
    [SerializeField] private AudioClip spottedClip;
    [SerializeField] private AudioClip lostTargetClip;
    [SerializeField] private AudioClip closeAttackClip;
    [SerializeField, Range(0f, 1f)] private float loopVolume = 0.7f;
    [SerializeField, Range(0f, 1f)] private float oneShotVolume = 0.85f;
    [SerializeField] private float closeAttackCooldown = 2f;

    private bool wasChasing;
    private bool wasInAttackRange;
    private float nextCloseAttackTime;

    private void Reset()
    {
        ghostEnemy = GetComponent<GhostNavMeshEnemy>();
        burnAttack = GetComponent<PlasmaBurnAttack>();
    }

    private void Awake()
    {
        if (ghostEnemy == null)
        {
            ghostEnemy = GetComponent<GhostNavMeshEnemy>();
        }

        if (burnAttack == null)
        {
            burnAttack = GetComponent<PlasmaBurnAttack>();
        }

        loopSource = EnsureAudioSource(loopSource, "Ghost Loop Audio", true);
        oneShotSource = EnsureAudioSource(oneShotSource, "Ghost One Shot Audio", false);
    }

    private void Update()
    {
        bool isChasing = ghostEnemy != null && ghostEnemy.IsChasing;
        bool isInAttackRange = ghostEnemy != null && ghostEnemy.IsInAttackRange;
        bool isBurningAttackActive = burnAttack != null && burnAttack.IsBurningAttackActive;

        if (isChasing && !wasChasing)
        {
            PlayOneShot(spottedClip);
        }
        else if (!isChasing && wasChasing)
        {
            PlayOneShot(lostTargetClip);
        }

        if (isInAttackRange && (!wasInAttackRange || Time.time >= nextCloseAttackTime))
        {
            PlayOneShot(closeAttackClip);
            nextCloseAttackTime = Time.time + Mathf.Max(0f, closeAttackCooldown);
        }

        AudioClip targetLoop = idleLoop;
        if (isBurningAttackActive && burningAttackLoop != null)
        {
            targetLoop = burningAttackLoop;
        }
        else if (isChasing && chaseLoop != null)
        {
            targetLoop = chaseLoop;
        }

        SetLoop(targetLoop);
        wasChasing = isChasing;
        wasInAttackRange = isInAttackRange;
    }

    private void SetLoop(AudioClip clip)
    {
        if (loopSource == null)
        {
            return;
        }

        if (clip == null)
        {
            loopSource.Stop();
            loopSource.clip = null;
            return;
        }

        if (loopSource.clip != clip)
        {
            loopSource.clip = clip;
            loopSource.loop = true;
            loopSource.volume = loopVolume;
            loopSource.Play();
            return;
        }

        if (!loopSource.isPlaying)
        {
            loopSource.Play();
        }

        loopSource.volume = loopVolume;
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (clip == null || oneShotSource == null)
        {
            return;
        }

        oneShotSource.PlayOneShot(clip, oneShotVolume);
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
        newSource.spatialBlend = 1f;
        return newSource;
    }
}
