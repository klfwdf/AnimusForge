using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.Refactor.Domain;
namespace AnimusForge;

internal readonly struct WorldDiplomacyPairFacts
{
    internal readonly bool AtWar, HasAlliance, HasTrade, Allied, Trading;
    internal WorldDiplomacyPairFacts(bool atWar, bool hasAlliance, bool hasTrade, bool allied, bool trading)
    { AtWar = atWar; HasAlliance = hasAlliance; HasTrade = hasTrade; Allied = allied; Trading = trading; }
}
internal interface IWorldDiplomacyActionSelectionPort
{
    IEnumerable<string> KingdomIds();
    bool HasAuthority(string id);
    bool IsEliminated(string id);
    WorldDiplomacyPairFacts CapturePair(string first, string second);
    WorldDiplomacyDocument ResolveDocument(string id);
    IWorldDiplomacyWarAdmissionPort CaptureWarAdmission(string first, string second);
}
internal sealed class WorldDiplomacyActionSelectionApplication
{
    private readonly IWorldDiplomacyActionSelectionPort _port;
    internal WorldDiplomacyActionSelectionApplication(IWorldDiplomacyActionSelectionPort port) { _port = port; }

    internal List<string> BuildPotentialDiplomaticActionIntents(string first, string second)
    {
        var actions = new List<string>();
        if (string.IsNullOrWhiteSpace(first) || string.IsNullOrWhiteSpace(second) || first == second
            || _port.IsEliminated(first) || _port.IsEliminated(second) || !_port.HasAuthority(first) || !_port.HasAuthority(second)) return actions;
        actions.AddRange(new[] { "statement", "condemn", "apology", "concession", "warning", "ultimatum" });
        var pair = _port.CapturePair(first, second);
        if (pair.AtWar) actions.Add("propose_peace");
        else
        {
            var war = _port.CaptureWarAdmission(first, second);
            if (WorldDiplomacyWarAdmissionApplication.CanDeclareWar(ref war, out _)) actions.Add("declare_war");
            if (pair.HasAlliance) actions.Add(pair.Allied ? "break_alliance" : "propose_alliance");
            if (pair.HasTrade) actions.Add(pair.Trading ? "cancel_trade" : "propose_trade");
            actions.AddRange(new[] { "propose_annexation", "propose_tributary", "propose_garrison", "propose_vassal" });
        }
        return actions;
    }

    internal List<string> BuildLegalDiplomaticActionIntents(WorldDiplomacyRound round,
        string author, string target, bool playerContext = false)
    {
        return WorldDiplomacyRoundLifecycleRules.BuildLegalDiplomaticActionIntents(round, author, target,
            () => BuildPotentialDiplomaticActionIntents(author, target), _port.ResolveDocument);
    }

    internal List<string> BuildLegalDiplomaticDeclarationIntents(
		WorldDiplomacyRound round,
		string author,
		string target,
		bool isRelayTurn,
		string resultSettlementSlotId = null,
		bool isExternalResponseOnly = false,
		WorldDiplomacyDocument responseSource = null, bool playerContext = false)
	{

        return BuildLegalDiplomaticActionIntents(round, author, target);
	}

    internal List<string> GetActionableDiplomaticTargets(string author, WorldDiplomacyRound round = null)
	{
		if (author == null) return new List<string>();
		return _port.KingdomIds()
			.Where(x => x != null && x != author && !_port.IsEliminated(x) && _port.HasAuthority(x))
			.Where(x => BuildLegalDiplomaticActionIntents(round, author, x).Count > 0)
			.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
			.ToList();
	}




}
