using static AnimusForge.Refactor.Domain.WorldDiplomacyStructureRules;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace AnimusForge;

internal sealed partial class WorldDiplomacyOrchestration
{
    private WorldDiplomacyStorage _hostilityStorage;
    private readonly Dictionary<string, string> _hostilityEvents = new(StringComparer.Ordinal);

    // Called only after the native war notification confirms a real player act.
    // No action executor or fabricated player declaration is involved.
    internal WorldDiplomacyRound CapturePlayerHostility(string first, string second, string warIdentity)
    {
        if (!_persistenceHealthy || !_host.WorldDiplomacyEnabled() || string.IsNullOrEmpty(warIdentity)
            || first == second || !_host.PartiesAtWar(first, second)) return null;
        string player = _host.PlayerKingdomId();
        if (string.IsNullOrEmpty(player) || (player != first && player != second)) return null;
        if (!ReferenceEquals(_hostilityStorage, Storage))
        {
            _hostilityStorage = Storage; _hostilityEvents.Clear();
            foreach (var r in GetLiveRounds().Concat(Storage.CompletedRounds).Where(x => x?.EventSourceType == "player_hostility"))
                foreach (string key in r.ExternalSignalKeys ?? new()) _hostilityEvents[key] = r.RoundId;
        }
        string target = player == first ? second : first;
        string identity = "player_hostility|" + player + "|" + target + "|" + warIdentity;
        if (_hostilityEvents.TryGetValue(identity, out string previous)) return ResolveRound(previous);
        var round = CreateIndependentEventRound(player, target, "", "player_hostility", true);
        round.IsPlayerInsertion = true;
        round.EventOpeningOrigin = "actual_native_player_hostility";
        round.ExternalSignalKeys.Add(identity); _hostilityEvents[identity] = round.RoundId;
        round.RoundTopic = DiplomacyPromptTexts.Format("Event_PlayerHostilityTitle", "War following {PLAYER}'s hostilities", "PLAYER", _host.PartyNameOrEmpty(player));
        round.ExternalOpeningContext = JsonConvert.SerializeObject(new {
            source = "native_war_declared", war_identity = warIdentity, player_kingdom = player, other_kingdom = target,
            fact = DiplomacyPromptTexts.Format("Event_PlayerHostilitySource",
                "An actual hostile act by the player has already caused war between {PLAYER} and {OTHER}. This is a confirmed occurrence, not a prior diplomatic declaration. Do not invent a prior declaration, consent or specific unrecorded acts; do not execute war again. Current rulers retain authority over subsequent negotiations.",
                "PLAYER", _host.PartyNameOrEmpty(player), "OTHER", _host.PartyNameOrEmpty(target)) });
        EnsureRoundParticipant(round, target, "active", false);
        EnsureEventBoundary(round, "started");
        EnqueueGeneration(target, player, null, false, null, 100, roundId: round.RoundId);
        MarkEventDirty(round);
        return round;
    }
}
