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
    IReadOnlyList<WorldDiplomacyThreat> Threats { get; }
    int LastFailedRoundDay(WorldDiplomacyOfferCooldownKey key);
    int CooldownDays();
    int CurrentDay();
    WorldDiplomacyDocument ResolveDocument(string id);
    IWorldDiplomacyWarAdmissionPort CaptureWarAdmission(string first, string second);
    IWorldDiplomacyNoActionPort CaptureNoActionPort(string author, string target);
}
internal sealed class WorldDiplomacyActionSelectionApplication
{
    private readonly IWorldDiplomacyActionSelectionPort _port;
    internal WorldDiplomacyActionSelectionApplication(IWorldDiplomacyActionSelectionPort port) { _port = port; }

    internal List<string> BuildPotentialDiplomaticActionIntents(string first, string second)
	{
		List<string> actions = new List<string>();
		if (first == null || second == null || first == second) return actions;
        if (_port.CaptureWarAdmission(first, second).InvolvesPlayer) return PlayerPotentialIntents();
        WorldDiplomacyPairFacts facts = _port.CapturePair(first, second);
        if (_port.HasAuthority(first) && _port.HasAuthority(second))
            actions.AddRange(new[] { "propose_annexation", "propose_tributary", "propose_garrison", "propose_vassal" });
        bool atWar = facts.AtWar;
        bool allied = facts.Allied;
        bool trading = facts.Trading;
		if (atWar)
		{
			actions.Add("propose_peace");
			return actions;
		}
		WorldDiplomacyThreat incoming = WorldDiplomacyRoundLifecycleRules.SelectOpenThreatBetween(_port.Threats, second, first);
		if (WorldDiplomacyRoundLifecycleRules.IsThreatDecisionPending(incoming)) actions.Add("comply_ultimatum");
		WorldDiplomacyThreat outbound = WorldDiplomacyRoundLifecycleRules.SelectOpenThreatIssuedBy(_port.Threats, first);
        IWorldDiplomacyWarAdmissionPort warAdmission = _port.CaptureWarAdmission(first, second);
		bool canIssueWarThreat = WorldDiplomacyWarAdmissionApplication.CanIssueWarThreat(ref warAdmission, out _);
		if (canIssueWarThreat && outbound == null)
		{
			actions.Add("warning");
			actions.Add("ultimatum");
		}
		else if (canIssueWarThreat
			&& WorldDiplomacyRoundLifecycleRules.IsEscalatableWarningThreat(outbound, second))
		{
			actions.Add("ultimatum");
		}
		bool enforcingRejectedUltimatum = WorldDiplomacyRoundLifecycleRules.IsEnforcingRejectedUltimatum(_port.Threats, first, second);
		bool canDeclareWar = WorldDiplomacyWarAdmissionApplication.CanDeclareWar(ref warAdmission, out _, enforcingRejectedUltimatum);
		if (canDeclareWar) actions.Add("declare_war");
		if (facts.HasAlliance)
		{
			if (allied) actions.Add("break_alliance");
			else if (!WorldDiplomacyOfferCooldownRules.IsTradeAllianceProposalCoolingDown(_port.LastFailedRoundDay, first, second, "propose_alliance", _port.CooldownDays(), _port.CurrentDay())) actions.Add("propose_alliance");
		}
		if (facts.HasTrade)
		{
			if (trading) actions.Add("cancel_trade");
			else if (!WorldDiplomacyOfferCooldownRules.IsTradeAllianceProposalCoolingDown(_port.LastFailedRoundDay, first, second, "propose_trade", _port.CooldownDays(), _port.CurrentDay())) actions.Add("propose_trade");
		}
		return WorldDiplomacyRoundLifecycleRules.NormalizeIdListPreserveOrder(actions);
	}

    private static List<string> PlayerPotentialIntents() => new() {
        "statement", "condemn", "apology", "concession", "warning", "ultimatum", "declare_war",
        "propose_peace", "propose_alliance", "break_alliance", "propose_trade", "cancel_trade",
        "propose_annexation", "propose_tributary", "propose_garrison", "propose_vassal"
    };

    internal List<string> BuildLegalDiplomaticActionIntents(
		WorldDiplomacyRound round,
		string author,
		string target, bool playerContext = false)
	{
        bool playerDiplomacy = playerContext || _port.CaptureWarAdmission(author, target).InvolvesPlayer;
		return WorldDiplomacyRoundLifecycleRules.BuildLegalDiplomaticActionIntents(
			round, author, target,
			() => playerDiplomacy ? PlayerPotentialIntents() : BuildPotentialDiplomaticActionIntents(author, target), _port.ResolveDocument, playerDiplomacy);
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
		List<string> intents = BuildLegalDiplomaticActionIntents(round, author, target, playerContext);
		bool mustAnswerPeaceOffer = WorldDiplomacyOfferContractRules.IsExclusivePeaceOfferResponseSet(intents);
		if (!mustAnswerPeaceOffer && WorldDiplomacyNoActionApplication.IsAllowed(
			round,
			resultSettlementSlotId,
			_port.CaptureNoActionPort(author, target),
			isRelayTurn,
			isExternalResponseOnly,
			responseSource))
		{
			intents.Add("statement");
		}
		return WorldDiplomacyRoundLifecycleRules.NormalizeIdListPreserveOrder(intents);
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

    internal List<string> GetRoundPlanActionableParticipants(string author, WorldDiplomacyRound round)
	{
		if (author == null) return new List<string>();
		return _port.KingdomIds()
			.Where(x => x != null && x != author && !_port.IsEliminated(x) && _port.HasAuthority(x))
			.Where(x => BuildLegalDiplomaticActionIntents(round, author, x).Count > 0
				|| BuildLegalDiplomaticActionIntents(round, x, author).Any(intent =>
					string.Equals(intent, "comply_ultimatum", StringComparison.OrdinalIgnoreCase)
					|| !string.IsNullOrWhiteSpace(WorldDiplomacyIntentVocabulary.ResponseIntentToProposalIntent(intent))))
			.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
			.ToList();
	}

    internal List<string> GetResultSettlementActionableTargets(WorldDiplomacyRound round, string author)
	{
		if (round == null || author == null) return new List<string>();
		return _port.KingdomIds()
			.Where(x => WorldDiplomacyNoActionApplication.CanUseSettlementTarget(round, _port.CaptureNoActionPort(author, x)))
			.Where(x => BuildLegalDiplomaticDeclarationIntents(
				round, author, x, isRelayTurn: true,
				resultSettlementSlotId: round.ResultSettlementCurrentSlotId).Count > 0)
			.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
			.ToList();
	}
}
