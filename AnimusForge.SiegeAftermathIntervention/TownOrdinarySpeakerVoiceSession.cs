using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Owns scene-local voice variation for ordinary town civilians and soldiers.
/// It never creates an AI request and never persists beyond the active town scene.
/// </summary>
public sealed class TownOrdinarySpeakerVoiceSession
{
    public const float RecommendedReplyTemperature = 0.62f;
    public const int DefaultRecentUtteranceCapacity = 3;

    private const int MaximumSourceDescriptionLength = 240;
    private const int MaximumRecentUtteranceLength = 96;

    private static readonly string[] CivilianTemperamentKeys =
    {
        "civilian_temperament_cautious",
        "civilian_temperament_practical",
        "civilian_temperament_defiant",
        "civilian_temperament_compassionate",
        "civilian_temperament_suspicious",
        "civilian_temperament_resigned",
    };

    private static readonly string[] SoldierTemperamentKeys =
    {
        "soldier_temperament_disciplined",
        "soldier_temperament_hotheaded",
        "soldier_temperament_wary",
        "soldier_temperament_mercenary",
        "soldier_temperament_zealous",
        "soldier_temperament_stoic",
    };

    private static readonly string[] RhythmKeys =
    {
        "rhythm_terse",
        "rhythm_hesitant",
        "rhythm_plainspoken",
        "rhythm_indirect",
        "rhythm_vivid",
        "rhythm_formal",
    };

    private static readonly string[] CivilianConcernKeys =
    {
        "civilian_concern_family",
        "civilian_concern_livelihood",
        "civilian_concern_property",
        "civilian_concern_safety",
        "civilian_concern_local_order",
    };

    private static readonly string[] SoldierConcernKeys =
    {
        "soldier_concern_discipline",
        "soldier_concern_reward",
        "soldier_concern_honor",
        "soldier_concern_comrades",
        "soldier_concern_faith",
        "soldier_concern_occupation_order",
    };

    private readonly object _sync = new object();
    private readonly int _recentUtteranceCapacity;
    private readonly Dictionary<string, TownOrdinarySpeakerVoiceProfile> _profiles =
        new Dictionary<string, TownOrdinarySpeakerVoiceProfile>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Queue<string>> _recentUtterances =
        new Dictionary<string, Queue<string>>(StringComparer.OrdinalIgnoreCase);

    private string _settlementId = string.Empty;

    public TownOrdinarySpeakerVoiceSession(int recentUtteranceCapacity = DefaultRecentUtteranceCapacity)
    {
        _recentUtteranceCapacity = Math.Max(1, recentUtteranceCapacity);
    }

    public bool IsActive
    {
        get
        {
            lock (_sync)
            {
                return _settlementId.Length > 0;
            }
        }
    }

    public int ProfileCount
    {
        get
        {
            lock (_sync)
            {
                return _profiles.Count;
            }
        }
    }

    public bool Begin(string settlementId)
    {
        string normalizedSettlementId = NormalizeKey(settlementId);
        lock (_sync)
        {
            ClearInternal();
            if (normalizedSettlementId.Length == 0)
            {
                return false;
            }

            _settlementId = normalizedSettlementId;
            return true;
        }
    }

    public bool EndScene()
    {
        lock (_sync)
        {
            bool wasActive = _settlementId.Length > 0;
            ClearInternal();
            return wasActive;
        }
    }

    public void Reset()
    {
        lock (_sync)
        {
            ClearInternal();
        }
    }

    public string BuildPromptContext(
        TownOrdinarySpeakerVoiceFacts facts,
        TownPromptTextCatalog textCatalog)
    {
        if (facts == null || !IsSupportedRole(facts.Role))
        {
            return string.Empty;
        }

        TownPromptTextCatalog text = TownPromptTextCatalog.Resolve(textCatalog);
        lock (_sync)
        {
            string settlementId = NormalizeKey(facts.SettlementId);
            string speakerKey = NormalizeKey(facts.SpeakerKey);
            if (_settlementId.Length == 0
                || settlementId.Length == 0
                || speakerKey.Length == 0
                || !string.Equals(_settlementId, settlementId, StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }

            string scopedSpeakerKey = BuildScopedSpeakerKey(settlementId, speakerKey, facts.Role);
            if (!_profiles.TryGetValue(scopedSpeakerKey, out TownOrdinarySpeakerVoiceProfile profile))
            {
                profile = CreateProfile(scopedSpeakerKey, facts, text);
                _profiles[scopedSpeakerKey] = profile;
            }

            return BuildPrompt(profile, scopedSpeakerKey, facts, text);
        }
    }

    public bool RecordUtterance(
        string settlementId,
        string speakerKey,
        TownDialogueRole role,
        string utterance)
    {
        if (!IsSupportedRole(role))
        {
            return false;
        }

        string normalizedSettlementId = NormalizeKey(settlementId);
        string normalizedSpeakerKey = NormalizeKey(speakerKey);
        string normalizedUtterance = NormalizePromptValue(utterance, MaximumRecentUtteranceLength);
        lock (_sync)
        {
            if (_settlementId.Length == 0
                || normalizedSettlementId.Length == 0
                || normalizedSpeakerKey.Length == 0
                || normalizedUtterance.Length == 0
                || !string.Equals(_settlementId, normalizedSettlementId, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string scopedSpeakerKey = BuildScopedSpeakerKey(normalizedSettlementId, normalizedSpeakerKey, role);
            if (!_recentUtterances.TryGetValue(scopedSpeakerKey, out Queue<string> recent))
            {
                recent = new Queue<string>();
                _recentUtterances[scopedSpeakerKey] = recent;
            }

            if (recent.Any(item => string.Equals(item, normalizedUtterance, StringComparison.Ordinal)))
            {
                return false;
            }

            recent.Enqueue(normalizedUtterance);
            while (recent.Count > _recentUtteranceCapacity)
            {
                recent.Dequeue();
            }
            return true;
        }
    }

    public int GetRecentUtteranceCount(string settlementId, string speakerKey, TownDialogueRole role)
    {
        string scopedSpeakerKey = BuildScopedSpeakerKey(
            NormalizeKey(settlementId),
            NormalizeKey(speakerKey),
            role);
        lock (_sync)
        {
            return _recentUtterances.TryGetValue(scopedSpeakerKey, out Queue<string> recent)
                ? recent.Count
                : 0;
        }
    }

    private static TownOrdinarySpeakerVoiceProfile CreateProfile(
        string scopedSpeakerKey,
        TownOrdinarySpeakerVoiceFacts facts,
        TownPromptTextCatalog text)
    {
        string[] temperamentKeys = facts.Role == TownDialogueRole.OrdinarySoldier
            ? SoldierTemperamentKeys
            : CivilianTemperamentKeys;
        string[] concernKeys = facts.Role == TownDialogueRole.OrdinarySoldier
            ? SoldierConcernKeys
            : CivilianConcernKeys;

        string sourcePersonality = NormalizePromptValue(facts.SourcePersonality, MaximumSourceDescriptionLength);
        string sourceBackground = NormalizePromptValue(facts.SourceBackground, MaximumSourceDescriptionLength);
        string temperament = sourcePersonality.Length > 0
            ? sourcePersonality
            : ResolveFragment(text, SelectKey(scopedSpeakerKey, "temperament", temperamentKeys));
        string rhythm = ResolveFragment(text, SelectKey(scopedSpeakerKey, "rhythm", RhythmKeys));
        string concern = ResolveFragment(text, SelectKey(scopedSpeakerKey, "concern", concernKeys));
        return new TownOrdinarySpeakerVoiceProfile(
            temperament,
            rhythm,
            concern,
            sourcePersonality,
            sourceBackground);
    }

    private string BuildPrompt(
        TownOrdinarySpeakerVoiceProfile profile,
        string scopedSpeakerKey,
        TownOrdinarySpeakerVoiceFacts facts,
        TownPromptTextCatalog text)
    {
        var lines = new List<string>();
        AddIfPresent(lines, text.SpeakerVoiceSectionTitle);

        if (profile.SourcePersonality.Length > 0 || profile.SourceBackground.Length > 0)
        {
            string sourceLine = ApplyTemplate(text.SpeakerVoiceSourceTemplate, "personality", profile.SourcePersonality);
            sourceLine = ApplyTemplate(sourceLine, "background", profile.SourceBackground);
            AddIfPresent(lines, sourceLine);
        }

        string profileLine = ApplyTemplate(text.SpeakerVoiceProfileTemplate, "role", facts.Role.ToString());
        profileLine = ApplyTemplate(profileLine, "temperament", profile.Temperament);
        profileLine = ApplyTemplate(profileLine, "rhythm", profile.Rhythm);
        profileLine = ApplyTemplate(profileLine, "concern", profile.Concern);
        AddIfPresent(lines, profileLine);

        if (_recentUtterances.TryGetValue(scopedSpeakerKey, out Queue<string> recent) && recent.Count > 0)
        {
            string recentLine = ApplyTemplate(
                text.SpeakerVoiceRecentTemplate,
                "recent",
                string.Join(" | ", recent));
            AddIfPresent(lines, recentLine);
        }

        AddIfPresent(lines, text.SpeakerVoiceInstruction);
        return string.Join(Environment.NewLine, lines);
    }

    private static string ResolveFragment(TownPromptTextCatalog text, string key)
    {
        if (text?.SpeakerVoiceFragments != null
            && text.SpeakerVoiceFragments.TryGetValue(key ?? string.Empty, out string value)
            && !string.IsNullOrWhiteSpace(value))
        {
            return value.Trim();
        }
        return key ?? string.Empty;
    }

    private static string SelectKey(string seed, string salt, string[] keys)
    {
        if (keys == null || keys.Length == 0)
        {
            return string.Empty;
        }
        uint hash = ComputeStableHash((seed ?? string.Empty) + "|" + (salt ?? string.Empty));
        return keys[hash % keys.Length];
    }

    private static uint ComputeStableHash(string value)
    {
        unchecked
        {
            uint hash = 2166136261;
            foreach (char character in value ?? string.Empty)
            {
                hash ^= character;
                hash *= 16777619;
            }
            return hash;
        }
    }

    private static string BuildScopedSpeakerKey(
        string settlementId,
        string speakerKey,
        TownDialogueRole role)
    {
        return (settlementId ?? string.Empty)
            + "|"
            + (speakerKey ?? string.Empty)
            + "|"
            + role;
    }

    private static bool IsSupportedRole(TownDialogueRole role)
    {
        return role == TownDialogueRole.OrdinaryCivilian
            || role == TownDialogueRole.OrdinarySoldier;
    }

    private static string NormalizeKey(string value)
    {
        return (value ?? string.Empty).Replace("\r", string.Empty).Replace("\n", string.Empty).Trim().ToLowerInvariant();
    }

    private static string NormalizePromptValue(string value, int maximumLength)
    {
        string normalized = (value ?? string.Empty)
            .Replace("\r", " ")
            .Replace("\n", " ")
            .Trim();
        while (normalized.IndexOf("  ", StringComparison.Ordinal) >= 0)
        {
            normalized = normalized.Replace("  ", " ");
        }
        return normalized.Length <= maximumLength
            ? normalized
            : normalized.Substring(0, maximumLength).TrimEnd();
    }

    private static string ApplyTemplate(string template, string key, string value)
    {
        return (template ?? string.Empty).Replace("{" + key + "}", value ?? string.Empty);
    }

    private static void AddIfPresent(List<string> lines, string value)
    {
        string normalized = (value ?? string.Empty).Trim();
        if (normalized.Length > 0)
        {
            lines.Add(normalized);
        }
    }

    private void ClearInternal()
    {
        _settlementId = string.Empty;
        _profiles.Clear();
        _recentUtterances.Clear();
    }
}

public sealed class TownOrdinarySpeakerVoiceFacts
{
    public TownOrdinarySpeakerVoiceFacts(
        string settlementId,
        string speakerKey,
        TownDialogueRole role,
        string sourcePersonality,
        string sourceBackground)
    {
        SettlementId = settlementId ?? string.Empty;
        SpeakerKey = speakerKey ?? string.Empty;
        Role = role;
        SourcePersonality = sourcePersonality ?? string.Empty;
        SourceBackground = sourceBackground ?? string.Empty;
    }

    public string SettlementId { get; }

    public string SpeakerKey { get; }

    public TownDialogueRole Role { get; }

    public string SourcePersonality { get; }

    public string SourceBackground { get; }
}

public sealed class TownOrdinarySpeakerVoiceProfile
{
    public TownOrdinarySpeakerVoiceProfile(
        string temperament,
        string rhythm,
        string concern,
        string sourcePersonality,
        string sourceBackground)
    {
        Temperament = temperament ?? string.Empty;
        Rhythm = rhythm ?? string.Empty;
        Concern = concern ?? string.Empty;
        SourcePersonality = sourcePersonality ?? string.Empty;
        SourceBackground = sourceBackground ?? string.Empty;
    }

    public string Temperament { get; }

    public string Rhythm { get; }

    public string Concern { get; }

    public string SourcePersonality { get; }

    public string SourceBackground { get; }
}
