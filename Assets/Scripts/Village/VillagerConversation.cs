using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public interface ICustomVillagerConversation
{
    bool TryBeginConversation(VillagerConversation owner);
    void CancelConversation();
}

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
    private ICustomVillagerConversation customConversation;
    private bool customConversationActive;

    public VillagerData Data => data;
    public bool IsTalking => conversationRoutine != null || customConversationActive;
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
        if (IsTalking || data == null)
        {
            return false;
        }

        ResolveCustomConversation();
        if (customConversation != null)
        {
            customConversationActive = true;
            Started?.Invoke(this);
            conversationStarted?.Invoke();
            if (customConversation.TryBeginConversation(this))
            {
                return true;
            }

            customConversationActive = false;
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
        if (customConversationActive)
        {
            customConversation?.CancelConversation();
            customConversationActive = false;
        }

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
            SubtitleController.Instance.EndSkippableSequence(this);
            SubtitleController.Instance.Hide();
        }
    }

    public void ResetConversationProgress()
    {
        completedConversationCount = 0;
    }

    public void CompleteCustomConversation(ICustomVillagerConversation source)
    {
        if (!customConversationActive || !ReferenceEquals(customConversation, source))
        {
            return;
        }

        customConversationActive = false;
        completedConversationCount++;
        Completed?.Invoke(this);
        conversationCompleted?.Invoke();
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
        SubtitleController.Instance?.BeginSkippableSequence(this, SkipConversation);

        VillagerDialogueLine[] lines = sequence.Lines;
        if (data.RandomizeSingleLineConversation)
        {
            VillagerDialogueLine randomLine = GetRandomValidLine(lines);
            if (randomLine != null)
            {
                yield return PlayLine(randomLine);
            }

            CompleteConversation();
            yield break;
        }

        for (int i = 0; i < lines.Length; i++)
        {
            VillagerDialogueLine line = lines[i];
            if (line == null || string.IsNullOrWhiteSpace(line.LocalizationKey))
            {
                continue;
            }

            yield return PlayLine(line);
        }

        CompleteConversation();
    }

    private IEnumerator PlayLine(VillagerDialogueLine line)
    {
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

    private static VillagerDialogueLine GetRandomValidLine(VillagerDialogueLine[] lines)
    {
        if (lines == null || lines.Length == 0)
        {
            return null;
        }

        int startIndex = UnityEngine.Random.Range(0, lines.Length);
        for (int offset = 0; offset < lines.Length; offset++)
        {
            VillagerDialogueLine line = lines[(startIndex + offset) % lines.Length];
            if (line != null && !string.IsNullOrWhiteSpace(line.LocalizationKey))
            {
                return line;
            }
        }

        return null;
    }

    private void SkipConversation()
    {
        if (conversationRoutine != null)
        {
            StopCoroutine(conversationRoutine);
        }

        CompleteConversation();
    }

    private void CompleteConversation()
    {
        SubtitleController.Instance?.EndSkippableSequence(this);
        SubtitleController.Instance?.Hide();

        if (voiceSource != null)
        {
            voiceSource.Stop();
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
        ResolveCustomConversation();
        RefreshIdentityName();
    }

    private void ResolveCustomConversation()
    {
        if (customConversation == null)
        {
            customConversation = GetComponent<ICustomVillagerConversation>();
        }
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
