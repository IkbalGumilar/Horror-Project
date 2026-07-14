using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public sealed class VillagerConversation : MonoBehaviour
{
    private static readonly HashSet<VillagerConversation> ActiveVillagers = new();

    [Header("Data")]
    [SerializeField] private VillagerData data;
    [SerializeField] private AudioSource voiceSource;

    [Header("Interaction")]
    [SerializeField] private Transform conversationLookTarget;

    [Header("Events")]
    [SerializeField] private UnityEvent conversationStarted;
    [SerializeField] private UnityEvent conversationCompleted;

    private Coroutine conversationRoutine;
    private int completedConversationCount;
    private bool movementLocked;

    public VillagerData Data => data;
    public bool IsTalking => conversationRoutine != null;
    public bool MovementLocked => movementLocked || IsTalking || (data != null && data.CanTrade);
    public bool HasCompletedFirstConversation => completedConversationCount > 0;
    public string DisplayName => data != null ? LocalizationManager.Get(data.DisplayNameKey) : string.Empty;
    public string Occupation => data != null ? LocalizationManager.Get(data.OccupationKey) : string.Empty;
    public Vector3 ConversationLookPosition
    {
        get
        {
            if (conversationLookTarget != null)
            {
                return conversationLookTarget.position;
            }

            Renderer visual = GetComponentInChildren<Renderer>();
            return visual != null ? visual.bounds.center : transform.position;
        }
    }

    public event Action<VillagerConversation> Started;
    public event Action<VillagerConversation> Completed;

    public static VillagerConversation FindNearest(Vector3 origin, float radius)
    {
        VillagerConversation nearest = null;
        float nearestSqrDistance = radius * radius;

        foreach (VillagerConversation candidate in ActiveVillagers)
        {
            if (candidate == null || candidate.data == null || candidate.IsTalking)
            {
                continue;
            }

            float sqrDistance = (candidate.transform.position - origin).sqrMagnitude;
            if (sqrDistance > nearestSqrDistance)
            {
                continue;
            }

            nearest = candidate;
            nearestSqrDistance = sqrDistance;
        }

        return nearest;
    }

    public void Initialize(VillagerData villagerData)
    {
        data = villagerData;
        completedConversationCount = 0;
        RefreshIdentityName();
    }

    public bool TryStartConversation()
    {
        if (conversationRoutine != null || data == null)
        {
            return false;
        }

        VillagerConversationSequence sequence = GetCurrentSequence();
        if (sequence == null || !sequence.HasLines)
        {
            return false;
        }

        conversationRoutine = StartCoroutine(PlayConversation(sequence));
        return true;
    }

    public void SetMovementLocked(bool locked)
    {
        movementLocked = locked;
    }

    public void StopConversation()
    {
        if (conversationRoutine == null)
        {
            return;
        }

        StopCoroutine(conversationRoutine);
        conversationRoutine = null;

        if (voiceSource != null)
        {
            voiceSource.Stop();
        }

        if (SubtitleController.Instance != null)
        {
            SubtitleController.Instance.Hide();
        }
    }

    public void ResetConversationProgress()
    {
        completedConversationCount = 0;
    }

    private VillagerConversationSequence GetCurrentSequence()
    {
        if (completedConversationCount <= 0 && data.FirstConversation != null
            && data.FirstConversation.HasLines)
        {
            return data.FirstConversation;
        }

        if (data.RepeatConversation != null && data.RepeatConversation.HasLines)
        {
            return data.RepeatConversation;
        }

        return data.FirstConversation;
    }

    private IEnumerator PlayConversation(VillagerConversationSequence sequence)
    {
        Started?.Invoke(this);
        conversationStarted?.Invoke();

        VillagerDialogueLine[] lines = sequence.Lines;
        for (int i = 0; i < lines.Length; i++)
        {
            VillagerDialogueLine line = lines[i];
            if (line == null || string.IsNullOrWhiteSpace(line.LocalizationKey))
            {
                continue;
            }

            PlayVoice(line.VoiceClip);

            if (SubtitleController.Instance != null)
            {
                string speakerKey = line.Speaker == VillagerDialogueSpeaker.Player
                    ? "speaker.player"
                    : data.DisplayNameKey;
                SubtitleController.Instance.ShowLocalized(speakerKey, line.LocalizationKey, line.Duration);
            }

            yield return new WaitForSecondsRealtime(line.Duration + line.PauseAfter);
        }

        completedConversationCount++;
        conversationRoutine = null;
        Completed?.Invoke(this);
        conversationCompleted?.Invoke();
    }

    private void PlayVoice(AudioClip clip)
    {
        if (clip == null)
        {
            return;
        }

        if (voiceSource == null)
        {
            voiceSource = GetComponent<AudioSource>();
        }

        if (voiceSource == null)
        {
            voiceSource = gameObject.AddComponent<AudioSource>();
            voiceSource.playOnAwake = false;
            voiceSource.spatialBlend = 1f;
        }

        Vector2 pitchRange = data.VoicePitchRange;
        voiceSource.pitch = UnityEngine.Random.Range(pitchRange.x, pitchRange.y);
        voiceSource.clip = clip;
        voiceSource.Play();
    }

    private void OnDisable()
    {
        ActiveVillagers.Remove(this);
        LocalizationManager.LanguageChanged -= RefreshIdentityName;
        StopConversation();
    }

    private void OnEnable()
    {
        ActiveVillagers.Add(this);
        LocalizationManager.LanguageChanged += RefreshIdentityName;
        RefreshIdentityName();
    }

    private void RefreshIdentityName()
    {
        if (data == null || string.IsNullOrWhiteSpace(data.DisplayNameKey))
        {
            return;
        }

        string localizedName = LocalizationManager.Get(data.DisplayNameKey);
        if (!string.IsNullOrWhiteSpace(localizedName) && localizedName != data.DisplayNameKey)
        {
            gameObject.name = localizedName;
        }
    }
}
