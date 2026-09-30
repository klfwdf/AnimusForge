using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Domain;
namespace AnimusForge;

internal interface IWorldDiplomacyNoActionPort
{
    bool AuthorResolved { get; }
    bool TargetResolved { get; }
    bool SameParty { get; }
    bool AuthorIsPlayer { get; }
    bool AuthorEliminated { get; }
    bool TargetEliminated { get; }
    bool AuthorHasAuthority { get; }
    bool TargetHasAuthority { get; }
    string AuthorId { get; }
    string TargetId { get; }
    int MaxParticipants { get; }
    WorldDiplomacyDocument ResolveDocument(string id);
    bool IsRepresentativeFor(WorldDiplomacyDocument document);
}

internal static class WorldDiplomacyNoActionApplication
{
    internal static bool IsAllowed<TPort>(WorldDiplomacyRound round, string resultSettlementSlotId, TPort port,
        bool isRelayTurn, bool isExternalResponseOnly = false, WorldDiplomacyDocument responseSource = null)
        where TPort : IWorldDiplomacyNoActionPort

	{
		if (round == null || !port.AuthorResolved || !port.TargetResolved) return false;
		if (!WorldDiplomacyRoundLifecycleRules.IsNoActionAuthorizationEligible(
			true,
			true,
			port.SameParty,
			port.AuthorIsPlayer,
			port.AuthorEliminated,
			port.TargetEliminated,
			port.AuthorHasAuthority,
			port.TargetHasAuthority,
			WorldDiplomacyRoundLifecycleRules.IsActiveRoundState(round.State))) return false;
		WorldDiplomacyDocument root = port.ResolveDocument(round.RootDocumentId);
		bool rootReady = root?.IsReadyForPublication == true;
		bool rootActionable = rootReady && WorldDiplomacyIntentVocabulary.IsActionableDiplomacyIntent(root.Intent);
		if (isExternalResponseOnly)
		{
			bool responseGatePassed = responseSource?.IsReadyForPublication == true
				&& responseSource.IsPlayerAuthored
				&& !string.IsNullOrWhiteSpace(responseSource.DocumentId)
				&& WorldDiplomacyRoundLifecycleRules.IsRecordInRound(responseSource.RoundId, round.RoundId)
				&& string.Equals(responseSource.AuthorKingdomId, port.TargetId, StringComparison.OrdinalIgnoreCase);
			bool isPrimaryTarget = responseGatePassed && string.Equals(
				responseSource.TargetKingdomId,
				port.AuthorId,
				StringComparison.OrdinalIgnoreCase);
			bool isRepresentativeTarget = responseGatePassed
				&& port.IsRepresentativeFor(responseSource);
			bool isInAddressedList = responseGatePassed
				&& (responseSource.AddressedKingdomIds ?? new List<string>())
					.Contains(port.AuthorId, StringComparer.OrdinalIgnoreCase);
			WorldDiplomacyRoundParticipant requiredResponder = !responseGatePassed ? null
				: (round.Participants ?? new List<WorldDiplomacyRoundParticipant>())
					.FirstOrDefault(x => x != null
						&& string.Equals(x.KingdomId, port.AuthorId, StringComparison.OrdinalIgnoreCase));
			return WorldDiplomacyRoundLifecycleRules.EvaluateExternalNoActionAuthorization(
				new WorldDiplomacyExternalNoActionInput
				{
					AuthorResolved = true,
					TargetResolved = true,
					SameParty = port.SameParty,
					AuthorIsPlayer = port.AuthorIsPlayer,
					AuthorEliminated = port.AuthorEliminated,
					TargetEliminated = port.TargetEliminated,
					AuthorHasAuthority = port.AuthorHasAuthority,
					TargetHasAuthority = port.TargetHasAuthority,
					RoundActive = true,
					RootReady = rootReady,
					RootActionable = rootActionable,
					ResponseReady = responseSource?.IsReadyForPublication == true,
					ResponsePlayerAuthored = responseSource?.IsPlayerAuthored == true,
					ResponseHasDocumentId = !string.IsNullOrWhiteSpace(responseSource?.DocumentId),
					ResponseSameRound = responseSource != null
						&& WorldDiplomacyRoundLifecycleRules.IsRecordInRound(responseSource.RoundId, round.RoundId),
					ResponseAuthoredByTarget = responseSource != null
						&& string.Equals(responseSource.AuthorKingdomId, port.TargetId, StringComparison.OrdinalIgnoreCase),
					IsPrimaryTarget = isPrimaryTarget,
					IsRepresentativeTarget = isRepresentativeTarget,
					IsInAddressedList = isInAddressedList,
					ResponseRequiresResponse = responseSource?.RequiresResponse == true,
					MandatoryReplyPending = requiredResponder?.MandatoryReplyPending == true,
					LastTriggeredMatches = requiredResponder != null
						&& WorldDiplomacyRoundLifecycleRules.MatchesDocumentId(requiredResponder.LastTriggeredDocumentId, responseSource?.DocumentId),
					SettlementPending = round.ResultSettlementPending,
					RelayPlanned = round.RelayPlanned,
					IsRelayTurn = isRelayTurn,
					AuthorOnRoute = isRelayTurn && !round.ResultSettlementPending && round.RelayPlanned
						&& WorldDiplomacyStructureRules.RoundRouteContainsKingdom(round, port.AuthorId),
					TargetOnRoute = isRelayTurn && !round.ResultSettlementPending && round.RelayPlanned
						&& WorldDiplomacyStructureRules.RoundRouteContainsKingdom(round, port.TargetId)
				});
		}
		if (round.ResultSettlementPending)
		{
			string slotId = WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(resultSettlementSlotId, round.ResultSettlementCurrentSlotId);
			bool hasSlotId = !string.IsNullOrWhiteSpace(slotId);
			bool slotIdIsCurrent = hasSlotId
				&& string.Equals(round.ResultSettlementCurrentSlotId, slotId, StringComparison.OrdinalIgnoreCase);
			bool settlementTargetUsable = slotIdIsCurrent
				&& CanUseSettlementTarget(round, port);
			WorldDiplomacyResultSettlementSlot slot = settlementTargetUsable
				? (round.ResultSettlementSlots ?? new List<WorldDiplomacyResultSettlementSlot>())
					.FirstOrDefault(x => x != null
						&& string.Equals(x.SlotId, slotId, StringComparison.OrdinalIgnoreCase)
						&& string.Equals(x.KingdomId, port.AuthorId, StringComparison.OrdinalIgnoreCase))
				: null;
			bool slotHasRelatedKingdom = slot?.RelatedKingdomIds?.Any(x => !string.IsNullOrWhiteSpace(x)) == true;
			return WorldDiplomacyRoundLifecycleRules.EvaluateRelayNoActionAuthorization(
				new WorldDiplomacyRelayNoActionInput
				{
					AuthorResolved = true,
					TargetResolved = true,
					SameParty = port.SameParty,
					AuthorIsPlayer = port.AuthorIsPlayer,
					AuthorEliminated = port.AuthorEliminated,
					TargetEliminated = port.TargetEliminated,
					AuthorHasAuthority = port.AuthorHasAuthority,
					TargetHasAuthority = port.TargetHasAuthority,
					RoundActive = true,
					RootReady = rootReady,
					RootActionable = rootActionable,
					IsRelayTurn = isRelayTurn,
					SettlementPending = true,
					HasSlotId = hasSlotId,
					SlotIdIsCurrent = slotIdIsCurrent,
					SettlementTargetUsable = settlementTargetUsable,
					SlotFound = slot != null,
					SlotHasRelatedKingdom = slotHasRelatedKingdom,
					TargetInRelatedKingdoms = slotHasRelatedKingdom
						&& WorldDiplomacyRoundLifecycleRules.IsSettlementSlotRelatedTo(slot, port.TargetId),
					TargetOnRoute = slot != null && !slotHasRelatedKingdom
						&& WorldDiplomacyStructureRules.RoundRouteContainsKingdom(round, port.TargetId)
				});
		}
		List<string> route = round.RelayRouteKingdomIds ?? new List<string>();
		bool relayGatePassed = round.RelayPlanned && round.RelayWaiting
			&& string.IsNullOrWhiteSpace(resultSettlementSlotId) && isRelayTurn;
		return WorldDiplomacyRoundLifecycleRules.EvaluateRelayNoActionAuthorization(
			new WorldDiplomacyRelayNoActionInput
			{
				AuthorResolved = true,
				TargetResolved = true,
				SameParty = port.SameParty,
				AuthorIsPlayer = port.AuthorIsPlayer,
				AuthorEliminated = port.AuthorEliminated,
				TargetEliminated = port.TargetEliminated,
				AuthorHasAuthority = port.AuthorHasAuthority,
				TargetHasAuthority = port.TargetHasAuthority,
				RoundActive = true,
				RootReady = rootReady,
				RootActionable = rootActionable,
				IsRelayTurn = isRelayTurn,
				SettlementPending = false,
				HasSlotId = !string.IsNullOrWhiteSpace(resultSettlementSlotId),
				SlotIdIsCurrent = false,
				SettlementTargetUsable = false,
				SlotFound = false,
				SlotHasRelatedKingdom = false,
				TargetInRelatedKingdoms = false,
				RelayPlanned = round.RelayPlanned,
				RelayWaiting = round.RelayWaiting,
				AuthorOnRoute = relayGatePassed && WorldDiplomacyStructureRules.RoundRouteContainsKingdom(round, port.AuthorId),
				TargetOnRoute = relayGatePassed && WorldDiplomacyStructureRules.RoundRouteContainsKingdom(round, port.TargetId),
				AuthorIsCurrentCursor = relayGatePassed
					&& round.RelayCursor >= 0 && round.RelayCursor < route.Count
					&& string.Equals(route[round.RelayCursor], port.AuthorId, StringComparison.OrdinalIgnoreCase)
			});
	}
    internal static bool CanUseSettlementTarget<TPort>(WorldDiplomacyRound round, TPort port)
        where TPort : IWorldDiplomacyNoActionPort
    {
        if (round == null || !port.AuthorResolved) return false;
        return WorldDiplomacyRoundLifecycleRules.IsSettlementTargetUsable(round.ResultSettlementPending,
            port.TargetResolved, port.SameParty, port.TargetEliminated,
            port.TargetResolved && port.TargetHasAuthority,
            WorldDiplomacyStructureRules.RoundRouteContainsKingdom(round, port.TargetId),
            round.RelayRouteKingdomIds?.Count ?? 0, port.MaxParticipants);
    }
}
