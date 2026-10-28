using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace AnimusForge;

internal sealed partial class WorldDiplomacyOrchestration
{
    private bool HasPublishedPlayerOpening(WorldDiplomacyRound round) => round != null
        && Storage.Documents.Any(x => x.RoundId == round.RoundId && x.IsPlayerAuthored && x.IsReadyForPublication);

    private bool HasEventFoundation(WorldDiplomacyRound round) =>
        !string.IsNullOrWhiteSpace(round.EventBackground)
        && round.Participants.Where(x => x.State != "observer" && !_host.IsPlayerParty(x.KingdomId)).All(x =>
            !string.IsNullOrWhiteSpace(x.Agenda) && !string.IsNullOrWhiteSpace(x.PreferredOutcome)
            && !string.IsNullOrWhiteSpace(x.ExpectedCost) && !string.IsNullOrWhiteSpace(x.RedLine)
            && !string.IsNullOrWhiteSpace(x.Alternative));

    private bool TryReadOpeningFoundation(WorldDiplomacyRound round, string stage, JObject draft,
        out string background, out Dictionary<string, WorldDiplomacyRoundParticipant> positions)
        => TryReadOpeningFoundation(round, stage, draft, out background, out positions, out _);

    private bool TryReadOpeningFoundation(WorldDiplomacyRound round, string stage, JObject draft,
        out string background, out Dictionary<string, WorldDiplomacyRoundParticipant> positions, out string reason)
    {
        background = null; positions = null; reason = "";
        if (stage == "started" && round.EventOpeningOrigin == "player_declaration_v2"
            && (string.IsNullOrEmpty(round.EventPartyReview)
                || !round.Participants.Any(x => x.State == "active" && !_host.IsPlayerParty(x.KingdomId))))
        { reason = "player_opening_plan_and_npc_positions_required"; return false; }
        if (stage != "started" || HasEventFoundation(round)) return true;
        reason = "opening_background_missing_or_invalid";
        background = draft["background"]?.Type == JTokenType.String ? (string)draft["background"] : "";
        if (string.IsNullOrWhiteSpace(background) || background.Length > 6000) return false;
        reason = "opening_positions_missing_or_invalid";
        if (draft["positions"] is not JArray rows) return false;
        var required = new HashSet<string>(round.Participants.Where(x => x.State != "observer" && !_host.IsPlayerParty(x.KingdomId)).Select(x => x.KingdomId), StringComparer.Ordinal);
        reason = "opening_positions_must_match_required_npc_participants";
        // A unilateral player declaration can open before any NPC has joined.
        // Its empty NPC position list is valid; never invent the human's agenda.
        if (rows.Count != required.Count || required.Count == 0 && !HasPublishedPlayerOpening(round)) return false;
        positions = new(StringComparer.Ordinal);
        foreach (var row in rows.OfType<JObject>())
        {
            string id = row["kingdom"]?.Type == JTokenType.String ? (string)row["kingdom"] : "";
            reason = "opening_positions_must_match_required_npc_participants";
            if (string.IsNullOrEmpty(id) || !required.Contains(id) || positions.ContainsKey(id)) return false;
            reason = "opening_positions_requires_agenda_gain_cost_red_line_alternative";
            if (!TryReadEventPosition(row, out var position)) return false;
            positions.Add(id, position);
        }
        if (positions.Count != required.Count) { reason = "opening_positions_must_match_required_npc_participants"; return false; }
        reason = ""; return true;
    }

    private static void ApplyOpeningFoundation(WorldDiplomacyRound round, string background,
        Dictionary<string, WorldDiplomacyRoundParticipant> positions)
    {
        if (positions == null) return;
        round.EventBackground = background;
        foreach (var party in round.Participants)
            if (positions.TryGetValue(party.KingdomId, out var position))
            {
                party.Agenda = position.Agenda; party.PreferredOutcome = position.PreferredOutcome;
                party.ExpectedCost = position.ExpectedCost; party.RedLine = position.RedLine;
                party.Alternative = position.Alternative;
            }
    }
}
