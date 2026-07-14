using System;
using UnityEngine;

[Serializable]
public sealed class LocalizedConversationLine
{
    [SerializeField] private string speakerKey;
    [SerializeField] private string textKey;
    [SerializeField, Min(0f)] private float duration = 5f;

    public string SpeakerKey => speakerKey;
    public string TextKey => textKey;
    public float Duration => duration;
}

[CreateAssetMenu(fileName = "Localized Conversation", menuName = "Dialogue/Localized Conversation")]
public sealed class LocalizedConversationData : ScriptableObject
{
    [SerializeField] private LocalizedConversationLine[] lines = Array.Empty<LocalizedConversationLine>();

    public LocalizedConversationLine[] Lines => lines;
}
