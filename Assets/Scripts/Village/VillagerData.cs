using System;
using UnityEngine;

public enum VillagerDialogueSpeaker
{
    Player,
    Villager
}

public enum VillagerDialogueEmotion
{
    Neutral,
    Friendly,
    Worried,
    Afraid,
    Angry,
    Sad,
    Whispering
}

[Serializable]
public sealed class VillagerDialogueLine
{
    [SerializeField] private VillagerDialogueSpeaker speaker;
    [SerializeField] private string localizationKey;
    [SerializeField, Min(0.1f)] private float duration = 3.5f;
    [SerializeField, Min(0f)] private float pauseAfter = 0.15f;
    [SerializeField] private AudioClip voiceClip;
    [SerializeField] private VillagerDialogueEmotion emotion;

    public VillagerDialogueSpeaker Speaker => speaker;
    public string LocalizationKey => localizationKey;
    public float Duration => duration;
    public float PauseAfter => pauseAfter;
    public AudioClip VoiceClip => voiceClip;
    public VillagerDialogueEmotion Emotion => emotion;
}

[Serializable]
public sealed class VillagerConversationSequence
{
    [SerializeField] private VillagerDialogueLine[] lines = Array.Empty<VillagerDialogueLine>();

    public VillagerDialogueLine[] Lines => lines;
    public bool HasLines => lines != null && lines.Length > 0;
}

[CreateAssetMenu(fileName = "Villager Data", menuName = "Horror Game/Village/Villager Data")]
public sealed class VillagerData : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string villagerId;
    [SerializeField] private string displayNameKey;
    [SerializeField] private string occupationKey;
    [SerializeField] private string descriptionKey;
    [SerializeField, Min(0)] private int age;
    [SerializeField] private Sprite portrait;

    [Header("Gameplay")]
    [SerializeField] private bool storyCritical;
    [SerializeField] private bool canTrade;
    [SerializeField] private string shopId;
    [SerializeField, Range(-100, 100)] private int startingAffinity;

    [Header("Dialogue")]
    [SerializeField] private bool randomizeSingleLineConversation;
    [SerializeField] private VillagerConversationSequence firstConversation = new VillagerConversationSequence();
    [SerializeField] private VillagerConversationSequence repeatConversation = new VillagerConversationSequence();

    [Header("Voice")]
    [SerializeField] private Vector2 voicePitchRange = new Vector2(0.95f, 1.05f);

    public string VillagerId => villagerId;
    public string DisplayNameKey => displayNameKey;
    public string OccupationKey => occupationKey;
    public string DescriptionKey => descriptionKey;
    public int Age => age;
    public Sprite Portrait => portrait;
    public bool StoryCritical => storyCritical;
    public bool CanTrade => canTrade;
    public string ShopId => shopId;
    public int StartingAffinity => startingAffinity;
    public bool RandomizeSingleLineConversation => randomizeSingleLineConversation;
    public VillagerConversationSequence FirstConversation => firstConversation;
    public VillagerConversationSequence RepeatConversation => repeatConversation;
    public Vector2 VoicePitchRange => voicePitchRange;

    private void OnValidate()
    {
        if (voicePitchRange.x > voicePitchRange.y)
        {
            voicePitchRange = new Vector2(voicePitchRange.y, voicePitchRange.x);
        }
    }
}
