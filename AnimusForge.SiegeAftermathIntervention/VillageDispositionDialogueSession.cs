using System;
using System.Collections.Generic;

namespace AnimusForge.SiegeAftermathIntervention;

public enum VillageDispositionSpeakerRole
{
    Unknown, Villager, VillageNotable, PlayerTroop, PlayerCompanion, Other,
}

public static class VillageDispositionSpeakerPolicy
{
    // Flags are captured from the actual current mission Agent, never from model text.
    public static VillageDispositionSpeakerRole Resolve(bool validSpeaker, bool isHero,
        bool isResidentNotable, bool isPlayerPartyMember, bool isCivilian)
    {
        if (!validSpeaker) return VillageDispositionSpeakerRole.Unknown;
        if (isPlayerPartyMember) return isHero
            ? VillageDispositionSpeakerRole.PlayerCompanion : VillageDispositionSpeakerRole.PlayerTroop;
        if (isHero && isResidentNotable) return VillageDispositionSpeakerRole.VillageNotable;
        return !isHero && isCivilian ? VillageDispositionSpeakerRole.Villager : VillageDispositionSpeakerRole.Other;
    }

    public static bool CanAuthorize(VillageDispositionActionSpec action,
        VillageDispositionSpeakerRole role, bool directPlayerResponse)
    {
        return action != null && directPlayerResponse && role != VillageDispositionSpeakerRole.Unknown
            && Enum.IsDefined(typeof(VillageDispositionSpeakerRole), role)
            && ((action.OutcomeLevel <= 0 && action.Kind != VillageDispositionActionKind.VillagerApproval)
                || role == VillageDispositionSpeakerRole.Villager || role == VillageDispositionSpeakerRole.VillageNotable)
            && (!action.RequiresNotable || role == VillageDispositionSpeakerRole.VillageNotable);
    }
}

public sealed class VillageDispositionSpeakerProfile
{
    internal VillageDispositionSpeakerProfile(string key, string name, string personality,
        string background, string rhythm, string concern)
    {
        Key = key;
        Name = name;
        Personality = personality;
        Background = background;
        Rhythm = rhythm;
        Concern = concern;
    }

    public string Key { get; }
    public string Name { get; }
    public string Personality { get; }
    public string Background { get; }
    public string Rhythm { get; }
    public string Concern { get; }
}

/// <summary>One scene, one detached persona per real speaker. No LLM requests or save data.</summary>
public sealed class VillageDispositionDialogueSession
{
    private readonly object _sync = new object();
    private readonly Dictionary<string, VillageDispositionSpeakerProfile> _profiles =
        new Dictionary<string, VillageDispositionSpeakerProfile>(StringComparer.Ordinal);
    private readonly TownOrdinarySpeakerVoiceSession _ordinaryVoices = new TownOrdinarySpeakerVoiceSession();
    private string _villageId = string.Empty;
    private int _sceneGeneration;
    private TownPromptTextCatalog _textCatalog;
    private TownPromptTextCatalog _ordinaryCatalog;

    public int ProfileCount { get { lock (_sync) return _profiles.Count; } }

    public void Begin(string villageId, bool english = false)
        => Begin(villageId, english, null);

    public void Begin(string villageId, bool english, TownPromptTextCatalog text)
    {
        lock (_sync)
        {
            _profiles.Clear();
            _sceneGeneration++;
            _ordinaryVoices.EndScene();
            _villageId = (villageId ?? string.Empty).Trim();
            _textCatalog = _villageId.Length == 0 ? null : text ?? TownPromptCatalogResources.Get(english);
            _ordinaryCatalog = null;
            if (_textCatalog != null)
            {
                _ordinaryVoices.Begin(_villageId);
                // Copy only small voice fragments once per admission. Do not mutate the town catalog.
                var fragments = new Dictionary<string, string>(_textCatalog.SpeakerVoiceFragments);
                fragments["civilian_concern_family"] = _textCatalog.GetRuntimeText("VillageScene_ConcernFamily");
                fragments["civilian_concern_local_order"] = _textCatalog.GetRuntimeText("VillageScene_ConcernOrder");
                fragments["soldier_concern_occupation_order"] = _textCatalog.GetRuntimeText("VillageScene_ConcernSoldierOrder");
                fragments["soldier_concern_honor"] = _textCatalog.GetRuntimeText("VillageScene_ConcernHonor");
                _ordinaryCatalog = new TownPromptTextCatalog { SpeakerVoiceFragments = fragments };
            }
        }
    }

    public void End() => Begin(string.Empty);

    public bool TryGet(string villageId, string speakerKey, out VillageDispositionSpeakerProfile profile)
    {
        lock (_sync)
        {
            profile = null;
            return Matches(villageId, speakerKey) && _profiles.TryGetValue(speakerKey, out profile);
        }
    }

    public VillageDispositionSpeakerProfile GetOrCreate(string villageId, string speakerKey,
        string name, VillageDispositionSpeakerRole role, string sourcePersonality, string sourceBackground)
    {
        lock (_sync)
        {
            if (!Matches(villageId, speakerKey) || role == VillageDispositionSpeakerRole.Unknown
                || !Enum.IsDefined(typeof(VillageDispositionSpeakerRole), role)) return null;
            if (_profiles.TryGetValue(speakerKey, out VillageDispositionSpeakerProfile existing)) return existing;
            string personality = VillageDispositionPromptProfile.CleanValue(sourcePersonality, 240);
            string background = VillageDispositionPromptProfile.CleanValue(sourceBackground, 240);
            string rhythm = string.Empty, concern = string.Empty;
            // Ordinary villagers/soldiers use the exact town generator, but a village-owned cache.
            // Notables/heroes keep supplied AF data; missing history/personality stays unknown.
            if (role == VillageDispositionSpeakerRole.Villager || role == VillageDispositionSpeakerRole.PlayerTroop)
            {
                var townRole = role == VillageDispositionSpeakerRole.PlayerTroop ? TownDialogueRole.OrdinarySoldier : TownDialogueRole.OrdinaryCivilian;
                var facts = new TownOrdinarySpeakerVoiceFacts(_villageId, speakerKey + "|scene:" + _sceneGeneration, townRole, personality, background);
                var generated = _ordinaryVoices.GetOrCreateProfile(facts, _ordinaryCatalog);
                personality = generated.Temperament; rhythm = generated.Rhythm; concern = generated.Concern;
            }
            var profile = new VillageDispositionSpeakerProfile(speakerKey,
                VillageDispositionPromptProfile.CleanValue(name, 64), personality, background, rhythm, concern);
            _profiles.Add(speakerKey, profile);
            return profile;
        }
    }

    public string BuildSpeakerPrompt(VillageDispositionSpeakerProfile profile, VillageDispositionSpeakerRole currentRole)
        => BuildSpeakerPrompt(profile, currentRole, null);

    public string BuildSpeakerPrompt(VillageDispositionSpeakerProfile profile, VillageDispositionSpeakerRole currentRole, TownPromptTextCatalog text)
    {
        lock (_sync)
        {
            // Reject detached results from a retired scene, even if the same village is re-entered.
            if (profile == null || _villageId.Length == 0 || currentRole == VillageDispositionSpeakerRole.Unknown
                || !_profiles.TryGetValue(profile.Key, out var current) || !ReferenceEquals(current, profile)) return string.Empty;
            return VillageDispositionPromptProfile.BuildSpeakerPrompt(profile, currentRole, text ?? _textCatalog);
        }
    }

    private bool Matches(string villageId, string speakerKey) => _villageId.Length > 0
        && !string.IsNullOrWhiteSpace(speakerKey) && string.Equals(_villageId, villageId, StringComparison.Ordinal);

}
