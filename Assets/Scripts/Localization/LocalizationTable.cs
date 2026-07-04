using System;
using System.Collections.Generic;
using UnityEngine;

public enum GameLanguage
{
    English,
    Indonesian
}

[Serializable]
public sealed class LocalizedTextEntry
{
    public string key;
    [TextArea(1, 4)] public string english;
    [TextArea(1, 4)] public string indonesian;
}

[CreateAssetMenu(fileName = "Localization Table", menuName = "Localization/Localization Table")]
public sealed class LocalizationTable : ScriptableObject
{
    public GameLanguage defaultLanguage = GameLanguage.Indonesian;
    public LocalizedTextEntry[] entries;

    private Dictionary<string, LocalizedTextEntry> lookup;
    private Dictionary<string, LocalizedTextEntry> englishLookup;

    public string GetText(string key, GameLanguage language)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return string.Empty;
        }

        LocalizedTextEntry entry = GetEntry(key);
        if (entry == null)
        {
            return key;
        }

        string text = language == GameLanguage.Indonesian ? entry.indonesian : entry.english;
        if (string.IsNullOrEmpty(text))
        {
            text = language == GameLanguage.Indonesian ? entry.english : entry.indonesian;
        }

        return string.IsNullOrEmpty(text) ? key : text;
    }

    public string GetTextByEnglish(string englishText, GameLanguage language)
    {
        if (string.IsNullOrWhiteSpace(englishText))
        {
            return string.Empty;
        }

        EnsureLookup();
        if (!englishLookup.TryGetValue(englishText, out LocalizedTextEntry entry))
        {
            return englishText;
        }

        string text = language == GameLanguage.Indonesian ? entry.indonesian : entry.english;
        return string.IsNullOrEmpty(text) ? englishText : text;
    }

    public void ClearLookupCache()
    {
        lookup = null;
        englishLookup = null;
    }

    private LocalizedTextEntry GetEntry(string key)
    {
        EnsureLookup();
        lookup.TryGetValue(key, out LocalizedTextEntry entry);
        return entry;
    }

    private void EnsureLookup()
    {
        if (lookup != null)
        {
            return;
        }

        lookup = new Dictionary<string, LocalizedTextEntry>();
        englishLookup = new Dictionary<string, LocalizedTextEntry>();
        if (entries == null)
        {
            return;
        }

        for (int i = 0; i < entries.Length; i++)
        {
            LocalizedTextEntry entry = entries[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.key))
            {
                continue;
            }

            if (lookup.ContainsKey(entry.key))
            {
                Debug.LogWarning(
                    $"Duplicate localization key '{entry.key}' found in {name}. The last runtime entry with this key will override earlier entries.",
                    this);
            }

            lookup[entry.key] = entry;
            if (!string.IsNullOrWhiteSpace(entry.english) && !englishLookup.ContainsKey(entry.english))
            {
                englishLookup[entry.english] = entry;
            }
        }
    }

    private void OnValidate()
    {
        ClearLookupCache();
        WarnDuplicateKeys();
    }

    private void WarnDuplicateKeys()
    {
        if (entries == null)
        {
            return;
        }

        Dictionary<string, int> firstIndexByKey = new Dictionary<string, int>();
        for (int i = 0; i < entries.Length; i++)
        {
            LocalizedTextEntry entry = entries[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.key))
            {
                continue;
            }

            if (firstIndexByKey.TryGetValue(entry.key, out int firstIndex))
            {
                Debug.LogWarning(
                    $"Duplicate localization key '{entry.key}' found in {name}. First index: {firstIndex}, duplicate index: {i}. The last runtime entry with this key can override earlier entries.",
                    this);
                continue;
            }

            firstIndexByKey.Add(entry.key, i);
        }
    }
}
