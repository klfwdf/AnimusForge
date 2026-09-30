using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AnimusForge.Refactor.Domain;
using AnimusForge.Refactor.Contracts;

namespace AnimusForge;

// Synchronous settlement over canonical records. Live objects never cross the port.
internal static class WorldDiplomacyThreatSettlementApplication
{

	internal static bool TryApplyUltimatumComplianceDomesticPenalty(WorldDiplomacyStorage _storage, IWorldDiplomacyThreatSettlementPort _port, IWorldDiplomacyOrchestration _orchestration,
		WorldDiplomacyThreat threat,
		string compliantKingdom,
		out int affectedClanCount)
	{
		affectedClanCount = 0;
		int newlyAppliedClanCount = 0;
		if (compliantKingdom == null
			|| !WorldDiplomacyRoundLifecycleRules.IsThreatDomesticPenaltyEligible(threat, compliantKingdom))
		{
			return false;
		}

		threat.DomesticPenaltyEligibleClanIds ??= new List<string>();
		threat.DomesticPenaltyAppliedClanIds ??= new List<string>();
		threat.DomesticPenaltySkippedClanIds ??= new List<string>();
		if (threat.DomesticPenaltyCompleted)
		{
			affectedClanCount = WorldDiplomacyRoundLifecycleRules.CountThreatConsequenceAppliedClans(
				threat.DomesticPenaltyAppliedClanIds);
			return true;
		}

		if (!threat.DomesticPenaltySnapshotCaptured)
		{
			WorldDiplomacyConsequenceSnapshot snapshot = _port.CaptureConsequenceSnapshot(compliantKingdom);
			if (snapshot == null || string.IsNullOrWhiteSpace(snapshot.RulingClanId)) return false;
			WorldDiplomacyRoundLifecycleRules.CaptureThreatDomesticPenaltySnapshot(
				threat, snapshot.RulingClanId, snapshot.EligibleClanIds);
		}

		string rulingClanId = (threat.DomesticPenaltyRulingClanId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(rulingClanId) || !_port.CampaignAvailable)
		{
			return false;
		}

		HashSet<string> eligibleIds = new HashSet<string>(
			threat.DomesticPenaltyEligibleClanIds.Where(x => !string.IsNullOrWhiteSpace(x)),
			StringComparer.OrdinalIgnoreCase);
		HashSet<string> appliedIds = new HashSet<string>(
			threat.DomesticPenaltyAppliedClanIds.Where(x => !string.IsNullOrWhiteSpace(x)),
			StringComparer.OrdinalIgnoreCase);
		HashSet<string> skippedIds = new HashSet<string>(
			threat.DomesticPenaltySkippedClanIds.Where(x => !string.IsNullOrWhiteSpace(x)),
			StringComparer.OrdinalIgnoreCase);
		if (eligibleIds.Count == 0)
		{
			WorldDiplomacyRoundLifecycleRules.CompleteThreatDomesticPenaltyWithoutEligible(threat);
			return true;
		}

		HashSet<string> requiredClanIds = new HashSet<string>(eligibleIds, StringComparer.OrdinalIgnoreCase)
		{
			rulingClanId
		};
		_port.PrepareClans(requiredClanIds);
		WorldDiplomacyConsequenceClan rulingClan = _port.ReadClan(rulingClanId);
		if (rulingClan == null || rulingClan.IsEliminated)
		{
			foreach (string unresolvedId in WorldDiplomacyRoundLifecycleRules.SelectUnresolvedClanIds(eligibleIds, appliedIds))
			{
				skippedIds.Add(unresolvedId);
			}
			WorldDiplomacyRoundLifecycleRules.CompleteThreatDomesticPenaltyAsSkipped(threat, skippedIds);
			affectedClanCount = appliedIds.Count;
			return true;
		}
		if (rulingClan.LeaderId == null)
		{
			return false;
		}

		foreach (string eligibleClanId in eligibleIds)
		{
			if (WorldDiplomacyRoundLifecycleRules.IsThreatConsequenceClanSettled(eligibleClanId, appliedIds, skippedIds))
			{
				continue;
			}
			WorldDiplomacyConsequenceClan vassalClan = _port.ReadClan(eligibleClanId);
			if (vassalClan == null || vassalClan.IsEliminated)
			{
				skippedIds.Add(eligibleClanId);
				continue;
			}
			if (vassalClan.LeaderId == null) continue;
			if (vassalClan.LeaderId == rulingClan.LeaderId)
			{
				skippedIds.Add(eligibleClanId);
				continue;
			}

			int expectedRelation = int.MinValue;
			try
			{
				int relationBefore = _port.ReadRelation(eligibleClanId, rulingClanId);
				expectedRelation = WorldDiplomacyRoundLifecycleRules.ComputeThreatConsequenceRelationTarget(
					relationBefore, _port.UltimatumComplianceRoyalRelationPenalty);
				if (WorldDiplomacyRoundLifecycleRules.ShouldApplyThreatRelationPenalty(relationBefore))
				{
					_port.ChangeRelation(
						eligibleClanId,
						rulingClanId,
						_port.UltimatumComplianceRoyalRelationPenalty);
				}
				int relationAfter = _port.ReadRelation(eligibleClanId, rulingClanId);
				if (relationAfter != expectedRelation)
				{
					_port.Log("ultimatum compliance domestic penalty deferred threat=" + threat.ThreatId
						+ " clan=" + eligibleClanId
						+ " before=" + relationBefore.ToString(CultureInfo.InvariantCulture)
						+ " after=" + relationAfter.ToString(CultureInfo.InvariantCulture)
						+ " expected=" + expectedRelation.ToString(CultureInfo.InvariantCulture));
					continue;
				}
				appliedIds.Add(eligibleClanId);
				newlyAppliedClanCount++;
			}
			catch (Exception ex)
			{
				bool appliedDespiteException = false;
				if (expectedRelation != int.MinValue)
				{
					try
					{
						int relationAfterException = _port.ReadRelation(eligibleClanId, rulingClanId);
						if (WorldDiplomacyRoundLifecycleRules.IsThreatRelationPenaltyApplied(relationAfterException, expectedRelation))
						{
							appliedIds.Add(eligibleClanId);
							newlyAppliedClanCount++;
							appliedDespiteException = true;
						}
					}
					catch
					{
					}
				}
				_port.Log("ultimatum compliance domestic penalty failed threat=" + threat.ThreatId
					+ " clan=" + eligibleClanId + " applied_despite_exception=" + appliedDespiteException
					+ " error=" + ex.Message);
			}
		}

		threat.DomesticPenaltyAppliedClanIds = WorldDiplomacyRoundLifecycleRules.OrderThreatConsequenceClanIds(appliedIds);
		threat.DomesticPenaltySkippedClanIds = WorldDiplomacyRoundLifecycleRules.OrderThreatConsequenceClanIds(skippedIds);
		threat.DomesticPenaltyCompleted = WorldDiplomacyRoundLifecycleRules.IsThreatConsequenceSettled(
			eligibleIds, appliedIds, skippedIds);
		affectedClanCount = appliedIds.Count;
		_port.Log("ultimatum compliance domestic penalty threat=" + threat.ThreatId
			+ " kingdom=" + compliantKingdom
			+ " ruling_clan=" + rulingClanId
			+ " newly_applied=" + newlyAppliedClanCount.ToString(CultureInfo.InvariantCulture)
			+ " applied=" + appliedIds.Count.ToString(CultureInfo.InvariantCulture)
			+ " skipped=" + skippedIds.Count.ToString(CultureInfo.InvariantCulture)
			+ "/" + eligibleIds.Count.ToString(CultureInfo.InvariantCulture)
			+ " completed=" + threat.DomesticPenaltyCompleted);
		return threat.DomesticPenaltyCompleted;
	}

	internal static bool TryApplyDiplomaticThreatPolicyConditionCancellation(WorldDiplomacyStorage _storage, IWorldDiplomacyThreatSettlementPort _port, IWorldDiplomacyOrchestration _orchestration, WorldDiplomacyThreat threat)
	{
		WorldDiplomacyPolicyCancellationDispatch dispatch =
			WorldDiplomacyRoundLifecycleRules.EvaluateThreatPolicyCancellationDispatch(threat);
		if (dispatch == WorldDiplomacyPolicyCancellationDispatch.Reject) return false;
		if (dispatch == WorldDiplomacyPolicyCancellationDispatch.AlreadyComplete) return true;
		if (dispatch == WorldDiplomacyPolicyCancellationDispatch.MarkNotBound)
		{
			threat.PolicyConditionCancellationCompleted = true;
			threat.PolicyConditionCancellationStatus = "not_bound";
			return true;
		}

		bool completed = _port.CancelPolicy(
			threat.PolicyConditionPolicyId,
			threat.PolicyConditionOwnerKingdomId,
			"澶栦氦濞佹厬閫€璁╋細" + threat.ThreatId,
			out string policyName,
			out string result);
		if (!completed)
		{
			_port.Log("diplomatic threat policy cancellation deferred threat=" + threat.ThreatId
				+ " policy=" + threat.PolicyConditionPolicyId + " result=" + (result ?? ""));
			return false;
		}
		if (!string.IsNullOrWhiteSpace(policyName)) threat.PolicyConditionPolicyName = WorldDiplomacyTextRules.Limit(policyName.Trim(), 80);
		threat.PolicyConditionCancellationCompleted = true;
		threat.PolicyConditionCancellationStatus = WorldDiplomacyRoundLifecycleRules.ResolvePolicyCancellationStatus(result);
		threat.PolicyConditionCancellationDay = _port.CurrentDay();
		threat.UpdatedDay = Math.Max(threat.UpdatedDay, threat.PolicyConditionCancellationDay);
		_storage?.PendingPolicySignals?.RemoveAll(signal => WorldDiplomacyRoundLifecycleRules.IsPolicySignalBoundTo(
			signal, threat.PolicyConditionPolicyId, threat.PolicyConditionOwnerKingdomId));
		WorldDiplomacyRoundLifecycleRules.RemoveSettledPolicySignalContextFromActiveRound(
			_storage?.ActiveRound, threat.PolicyConditionPolicyId,
			threat.PolicyConditionOwnerKingdomId);
		WorldDiplomacyRoundLifecycleRules.InvalidateOtherThreatsBoundToSettledPolicy(
			threat, _storage?.DiplomaticThreats, _port.CurrentDay(), _port.Log);
		_port.Log("diplomatic threat policy cancellation settled threat=" + threat.ThreatId
			+ " policy=" + threat.PolicyConditionPolicyId
			+ " owner=" + threat.PolicyConditionOwnerKingdomId
			+ " result=" + threat.PolicyConditionCancellationStatus);
		return true;
	}

	internal static bool TryApplyDiplomaticThreatIssuerRelationReward(WorldDiplomacyStorage _storage, IWorldDiplomacyThreatSettlementPort _port, IWorldDiplomacyOrchestration _orchestration,
		WorldDiplomacyThreat threat,
		string issuerKingdom,
		out int affectedClanCount)
	{
		affectedClanCount = 0;
		int newlyAppliedClanCount = 0;
		if (issuerKingdom == null
			|| !WorldDiplomacyRoundLifecycleRules.IsThreatIssuerRewardEligible(threat, issuerKingdom))
		{
			return false;
		}

		threat.IssuerRewardEligibleClanIds ??= new List<string>();
		threat.IssuerRewardAppliedClanIds ??= new List<string>();
		threat.IssuerRewardSkippedClanIds ??= new List<string>();
		if (threat.IssuerRewardCompleted)
		{
			affectedClanCount = WorldDiplomacyRoundLifecycleRules.CountThreatConsequenceAppliedClans(
				threat.IssuerRewardAppliedClanIds);
			return true;
		}

		if (!threat.IssuerRewardSnapshotCaptured)
		{
			int rewardAmount = _port.GetThreatComplianceIssuerRelationReward();
			threat.IssuerRewardAmount = rewardAmount;
			if (rewardAmount <= 0)
			{
				WorldDiplomacyRoundLifecycleRules.CompleteThreatIssuerRewardWithoutAmount(threat);
				return true;
			}
			WorldDiplomacyConsequenceSnapshot snapshot = _port.CaptureConsequenceSnapshot(issuerKingdom);
			if (snapshot == null || string.IsNullOrWhiteSpace(snapshot.RulingClanId)) return false;
			WorldDiplomacyRoundLifecycleRules.CaptureThreatIssuerRewardSnapshot(
				threat, snapshot.RulingClanId, snapshot.EligibleClanIds);
		}

		string rulingClanId = (threat.IssuerRewardRulingClanId ?? "").Trim();
		int amount = WorldDiplomacyRoundLifecycleRules.ClampThreatIssuerRewardAmount(
			threat.IssuerRewardAmount, _port.IssuerRelationRewardMax);
		if (amount <= 0)
		{
			WorldDiplomacyRoundLifecycleRules.CompleteThreatIssuerRewardWithoutEligible(threat);
			return true;
		}
		if (rulingClanId.Length == 0 || !_port.CampaignAvailable) return false;

		HashSet<string> eligibleIds = new HashSet<string>(threat.IssuerRewardEligibleClanIds, StringComparer.OrdinalIgnoreCase);
		HashSet<string> appliedIds = new HashSet<string>(threat.IssuerRewardAppliedClanIds, StringComparer.OrdinalIgnoreCase);
		HashSet<string> skippedIds = new HashSet<string>(threat.IssuerRewardSkippedClanIds, StringComparer.OrdinalIgnoreCase);
		if (eligibleIds.Count == 0)
		{
			WorldDiplomacyRoundLifecycleRules.CompleteThreatIssuerRewardWithoutEligible(threat);
			return true;
		}

		HashSet<string> requiredClanIds = new HashSet<string>(eligibleIds, StringComparer.OrdinalIgnoreCase) { rulingClanId };
		_port.PrepareClans(requiredClanIds);
		WorldDiplomacyConsequenceClan rulingClan = _port.ReadClan(rulingClanId);
		if (rulingClan == null || rulingClan.IsEliminated)
		{
			foreach (string unresolvedId in WorldDiplomacyRoundLifecycleRules.SelectUnresolvedClanIds(eligibleIds, appliedIds))
			{
				skippedIds.Add(unresolvedId);
			}
			WorldDiplomacyRoundLifecycleRules.CompleteThreatIssuerRewardAsSkipped(threat, skippedIds);
			affectedClanCount = appliedIds.Count;
			return true;
		}
		if (rulingClan.LeaderId == null) return false;

		foreach (string eligibleClanId in eligibleIds)
		{
			if (WorldDiplomacyRoundLifecycleRules.IsThreatConsequenceClanSettled(eligibleClanId, appliedIds, skippedIds)) continue;
			WorldDiplomacyConsequenceClan vassalClan = _port.ReadClan(eligibleClanId);
			if (vassalClan == null || vassalClan.IsEliminated)
			{
				skippedIds.Add(eligibleClanId);
				continue;
			}
			if (vassalClan.LeaderId == null) continue;
			if (vassalClan.LeaderId == rulingClan.LeaderId)
			{
				skippedIds.Add(eligibleClanId);
				continue;
			}

			int expectedRelation = int.MinValue;
			try
			{
				int relationBefore = _port.ReadRelation(eligibleClanId, rulingClanId);
				expectedRelation = WorldDiplomacyRoundLifecycleRules.ComputeThreatConsequenceRelationTarget(
					relationBefore, amount);
				if (WorldDiplomacyRoundLifecycleRules.ShouldApplyThreatRelationReward(relationBefore))
				{
					_port.ChangeRelation(
						eligibleClanId,
						rulingClanId,
						amount);
				}
				int relationAfter = _port.ReadRelation(eligibleClanId, rulingClanId);
				if (relationAfter != expectedRelation)
				{
					_port.Log("diplomatic threat issuer relation reward deferred threat=" + threat.ThreatId
						+ " clan=" + eligibleClanId
						+ " before=" + relationBefore.ToString(CultureInfo.InvariantCulture)
						+ " after=" + relationAfter.ToString(CultureInfo.InvariantCulture)
						+ " expected=" + expectedRelation.ToString(CultureInfo.InvariantCulture));
					continue;
				}
				appliedIds.Add(eligibleClanId);
				newlyAppliedClanCount++;
			}
			catch (Exception ex)
			{
				bool appliedDespiteException = false;
				if (expectedRelation != int.MinValue)
				{
					try
					{
						int relationAfterException = _port.ReadRelation(eligibleClanId, rulingClanId);
						if (WorldDiplomacyRoundLifecycleRules.IsThreatRelationRewardApplied(relationAfterException, expectedRelation))
						{
							appliedIds.Add(eligibleClanId);
							newlyAppliedClanCount++;
							appliedDespiteException = true;
						}
					}
					catch
					{
					}
				}
				_port.Log("diplomatic threat issuer relation reward failed threat=" + threat.ThreatId
					+ " clan=" + eligibleClanId + " applied_despite_exception=" + appliedDespiteException
					+ " error=" + ex.Message);
			}
		}

		threat.IssuerRewardAppliedClanIds = WorldDiplomacyRoundLifecycleRules.OrderThreatConsequenceClanIds(appliedIds);
		threat.IssuerRewardSkippedClanIds = WorldDiplomacyRoundLifecycleRules.OrderThreatConsequenceClanIds(skippedIds);
		threat.IssuerRewardCompleted = WorldDiplomacyRoundLifecycleRules.IsThreatConsequenceSettled(
			eligibleIds, appliedIds, skippedIds);
		affectedClanCount = appliedIds.Count;
		_port.Log("diplomatic threat issuer relation reward threat=" + threat.ThreatId
			+ " kingdom=" + issuerKingdom
			+ " ruling_clan=" + rulingClanId
			+ " amount=" + amount.ToString(CultureInfo.InvariantCulture)
			+ " newly_applied=" + newlyAppliedClanCount.ToString(CultureInfo.InvariantCulture)
			+ " applied=" + appliedIds.Count.ToString(CultureInfo.InvariantCulture)
			+ " skipped=" + skippedIds.Count.ToString(CultureInfo.InvariantCulture)
			+ "/" + eligibleIds.Count.ToString(CultureInfo.InvariantCulture)
			+ " completed=" + threat.IssuerRewardCompleted);
		return threat.IssuerRewardCompleted;
	}

	internal static bool ResolveDiplomaticThreatCompliance(WorldDiplomacyStorage _storage, IWorldDiplomacyThreatSettlementPort _port, IWorldDiplomacyOrchestration _orchestration, WorldDiplomacyDocument document, string compliantKingdom, string issuer)
	{
		if (document == null || compliantKingdom == null || issuer == null) return false;
		WorldDiplomacyThreat threat = WorldDiplomacyRoundLifecycleRules.SelectOpenThreatBetween(_storage?.DiplomaticThreats, issuer, compliantKingdom)
			?? WorldDiplomacyRoundLifecycleRules.SelectComplianceRecordedThreat(
				_storage.DiplomaticThreats, issuer, compliantKingdom,
				document.DocumentId, document.ProcessingActionId);
		if (WorldDiplomacyRoundLifecycleRules.IsThreatComplianceAlreadyRecorded(threat, document.DocumentId))
		{
			document.ChangedDiplomaticState = true;
			document.MechanicalResult = "已明确服从阶段=" + WorldDiplomacyRoundLifecycleRules.DescribeThreatStage(threat.Stage);
			return true;
		}
		if (!WorldDiplomacyRoundLifecycleRules.IsThreatComplianceStageMatch(
			threat, document.RespondingToThreatDocumentId, document.RespondingToThreatActionId)) return false;

		WorldDiplomacyRoundLifecycleRules.ApplyThreatComplianceResolution(threat, document, _port.CurrentDay());
		int prestigeChange = WorldDiplomacyRoundLifecycleRules.ResolveCompliancePrestigeDelta(
			threat.Stage, _port.UltimatumCompliancePrestigeChange, _port.WarningCompliancePrestigeChange);
		_orchestration.ApplyNationalPrestigeDelta(issuer, prestigeChange, document,
			"迫使" + _port.KingdomName(compliantKingdom) + "服从" + WorldDiplomacyRoundLifecycleRules.DescribeThreatStageDiplomaticLabel(threat.Stage));
		_orchestration.ApplyNationalPrestigeDelta(compliantKingdom, -prestigeChange, document,
			"在压力下服从" + _port.KingdomName(issuer) + "的" + WorldDiplomacyRoundLifecycleRules.DescribeThreatStageDiplomaticLabel(threat.Stage));
		bool domesticPenaltyCompleted = TryApplyUltimatumComplianceDomesticPenalty(_storage, _port, _orchestration, threat, compliantKingdom, out int affectedClanCount);
		bool policyCancellationCompleted = TryApplyDiplomaticThreatPolicyConditionCancellation(_storage, _port, _orchestration, threat);
		bool issuerRewardCompleted = TryApplyDiplomaticThreatIssuerRelationReward(_storage, _port, _orchestration, threat, issuer, out int rewardedClanCount);
		document.ChangedDiplomaticState = true;
		document.MechanicalResult = "已明确服从阶段=" + WorldDiplomacyRoundLifecycleRules.DescribeThreatStage(threat.Stage)
			+ (WorldDiplomacyRoundLifecycleRules.IsThreatCancellationStatusCancelled(threat.PolicyConditionCancellationStatus)
			? "；附带政策《" + WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(threat.PolicyConditionPolicyName, threat.PolicyConditionPolicyId) + "》已取消"
				: "");
		_port.Log("diplomatic threat complied threat=" + threat.ThreatId
			+ " issuer=" + issuer + " target=" + compliantKingdom
			+ " domestic_penalty_completed=" + domesticPenaltyCompleted
			+ " domestic_penalty_applied_clans=" + affectedClanCount.ToString(CultureInfo.InvariantCulture)
			+ " policy_cancellation_completed=" + policyCancellationCompleted
			+ " policy=" + threat.PolicyConditionPolicyId
			+ " issuer_reward_completed=" + issuerRewardCompleted
			+ " issuer_reward_applied_clans=" + rewardedClanCount.ToString(CultureInfo.InvariantCulture));
		return true;
	}

	internal static void ApplyDiplomaticThreatReputationPenalty(WorldDiplomacyStorage _storage, IWorldDiplomacyThreatSettlementPort _port, IWorldDiplomacyOrchestration _orchestration,
		WorldDiplomacyThreat threat,
		WorldDiplomacyDocument document)
	{
		if (!WorldDiplomacyRoundLifecycleRules.IsThreatBreachPenaltyApplicable(threat)) return;
		int penalty = WorldDiplomacyRoundLifecycleRules.ResolveThreatBreachPrestigePenalty(
			threat.Stage, _port.UltimatumFollowThroughPrestigePenalty, _port.WarningFollowThroughPrestigePenalty);
		int before = WorldDiplomacyReputationRules.GetNationalPrestige(_storage?.NationalPrestigeByKingdom, threat.IssuerKingdomId);
		int after = _orchestration.ApplyNationalPrestigeDelta(threat.IssuerKingdomId, -penalty, document,
			WorldDiplomacyRoundLifecycleRules.DescribeThreatBreachPrestigeReason(threat.Stage));
		if (before == 0)
		{
			_orchestration.ApplyZeroPrestigeBreachRelationPenalty(
				threat.IssuerKingdomId,
				WorldDiplomacyRoundLifecycleRules.ResolveThreatBreachRelationPenalty(
					threat.Stage, _port.ZeroPrestigeUltimatumBreachRelationPenalty, _port.ZeroPrestigeWarningBreachRelationPenalty));
		}
		WorldDiplomacyRoundLifecycleRules.ApplyThreatBreachSettlement(
			threat, before - after, document?.RoundId, document?.DocumentId, _port.CurrentDay());
		_port.Log("national prestige penalty threat=" + threat.ThreatId
			+ " issuer=" + threat.IssuerKingdomId + " target=" + threat.TargetKingdomId
			+ " stage=" + threat.Stage + " penalty=" + threat.ReputationPenaltyAmount.ToString(CultureInfo.InvariantCulture)
			+ " prestige=" + after.ToString(CultureInfo.InvariantCulture));
	}

	internal static void RetryDiplomaticThreatDomesticPenalties(WorldDiplomacyStorage _storage, IWorldDiplomacyThreatSettlementPort _port, IWorldDiplomacyOrchestration _orchestration)
	{
		foreach (WorldDiplomacyThreat threat in WorldDiplomacyRoundLifecycleRules.SelectThreatHistoryRetryBatch(
			_storage?.DiplomaticThreats, WorldDiplomacyRoundLifecycleRules.NeedsDomesticPenaltySettlementRetry, 8))
		{
			threat.UpdatedDay = _port.CurrentDay();
			WorldDiplomacyConsequenceParty target = _port.ReadParty(threat.TargetKingdomId);
			string compliantKingdom = target?.Id;
			bool cannotCaptureEliminatedKingdomSnapshot = WorldDiplomacyRoundLifecycleRules.CannotCaptureThreatConsequenceSnapshot(
				target?.IsEliminated == true, target?.HasRulingClan != true,
				threat.DomesticPenaltySnapshotCaptured);
			bool completed = compliantKingdom != null && !cannotCaptureEliminatedKingdomSnapshot
				? TryApplyUltimatumComplianceDomesticPenalty(_storage, _port, _orchestration, threat, compliantKingdom, out int affectedClanCount)
				: WorldDiplomacyRoundLifecycleRules.CompleteUnresolvableDiplomaticThreatDomesticPenalty(
				threat, out affectedClanCount, _port.CurrentDay(), _port.Log);
			if (!completed) continue;
			WorldDiplomacyDocument document = _port.ResolveDocument(threat.ComplianceDocumentId);
			if (document != null)
			{
				WorldDiplomacyRoundLifecycleRules.UpdateDiplomaticThreatComplianceDocumentResult(threat, _port.ResolveDocument);
				try
				{
					_orchestration.AppendCanonicalDocumentEvents(document);
					WorldDiplomacyThreatHistoryApplication.FinalizeDiplomaticThreatHistoryAfterDocument(document,
			_storage?.DiplomaticThreats, _orchestration.TryAppendDiplomaticThreatHistoryResult,
			_orchestration.TryAppendDiplomaticThreatDomesticPenaltyHistoryResult,
			_orchestration.TryAppendDiplomaticThreatIssuerRewardHistoryResult);
				}
				catch (Exception ex)
				{
					_orchestration.ScheduleDeferredCanonicalHistoryRetry(document.DocumentId);
					_port.Log("compliance history append deferred threat=" + threat.ThreatId + " error=" + ex.Message);
				}
			}
			_orchestration.TryAppendDiplomaticThreatDomesticPenaltyHistoryResult(threat);
		}
	}

	internal static void RetryDiplomaticThreatComplianceConsequences(WorldDiplomacyStorage _storage, IWorldDiplomacyThreatSettlementPort _port, IWorldDiplomacyOrchestration _orchestration)
	{
		foreach (WorldDiplomacyThreat threat in WorldDiplomacyRoundLifecycleRules.SelectThreatHistoryRetryBatch(
			_storage?.DiplomaticThreats, WorldDiplomacyRoundLifecycleRules.NeedsPolicyCancellationRetry, 8))
		{
			threat.UpdatedDay = _port.CurrentDay();
			TryApplyDiplomaticThreatPolicyConditionCancellation(_storage, _port, _orchestration, threat);
			WorldDiplomacyRoundLifecycleRules.UpdateDiplomaticThreatComplianceDocumentResult(threat, _port.ResolveDocument);
		}

		foreach (WorldDiplomacyThreat threat in WorldDiplomacyRoundLifecycleRules.SelectThreatHistoryRetryBatch(
			_storage?.DiplomaticThreats, WorldDiplomacyRoundLifecycleRules.NeedsIssuerRewardSettlementRetry, 8))
		{
			threat.UpdatedDay = _port.CurrentDay();
			WorldDiplomacyConsequenceParty issuerParty = _port.ReadParty(threat.IssuerKingdomId);
			string issuer = issuerParty?.Id;
			bool cannotCaptureEliminatedKingdomSnapshot = WorldDiplomacyRoundLifecycleRules.CannotCaptureThreatConsequenceSnapshot(
				issuerParty?.IsEliminated == true, issuerParty?.HasRulingClan != true,
				threat.IssuerRewardSnapshotCaptured);
			bool completed = issuer != null && !cannotCaptureEliminatedKingdomSnapshot
				? TryApplyDiplomaticThreatIssuerRelationReward(_storage, _port, _orchestration, threat, issuer, out int affectedClanCount)
				: WorldDiplomacyRoundLifecycleRules.CompleteUnresolvableDiplomaticThreatIssuerRelationReward(
				threat, out affectedClanCount, _port.GetThreatComplianceIssuerRelationReward(), _port.CurrentDay(), _port.Log);
			if (!completed) continue;
			_orchestration.TryAppendDiplomaticThreatIssuerRewardHistoryResult(threat);
		}
	}

	internal static void RetryDiplomaticThreatHistoryResults(WorldDiplomacyStorage _storage, IWorldDiplomacyThreatSettlementPort _port, IWorldDiplomacyOrchestration _orchestration)
	{
		foreach (WorldDiplomacyThreat threat in WorldDiplomacyRoundLifecycleRules.SelectThreatHistoryRetryBatch(
			_storage?.DiplomaticThreats, WorldDiplomacyRoundLifecycleRules.NeedsNonComplianceHistoryRetry, 8))
		{
			WorldDiplomacyThreatHistoryApplication.TryAppendDiplomaticThreatNonComplianceHistoryResult(threat, _orchestration.TryAppendDiplomaticThreatNonComplianceHistoryResult);
		}
		foreach (WorldDiplomacyThreat threat in WorldDiplomacyRoundLifecycleRules.SelectThreatHistoryRetryBatch(
			_storage?.DiplomaticThreats, WorldDiplomacyRoundLifecycleRules.NeedsThreatResultHistoryRetry, 8))
		{
			threat.UpdatedDay = _port.CurrentDay();
			if (string.Equals(threat.Status, "breached", StringComparison.OrdinalIgnoreCase))
			{
				_orchestration.TryAppendDiplomaticThreatHistoryResult(threat);
				continue;
			}
			WorldDiplomacyDocument source = _port.ResolveDocument(WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(threat.ComplianceDocumentId, threat.ResolutionDocumentId));
			if (source == null || !source.ChangedDiplomaticState) continue;
			try
			{
				_orchestration.AppendCanonicalDocumentEvents(source);
				WorldDiplomacyThreatHistoryApplication.FinalizeDiplomaticThreatHistoryAfterDocument(source,
			_storage?.DiplomaticThreats, _orchestration.TryAppendDiplomaticThreatHistoryResult,
			_orchestration.TryAppendDiplomaticThreatDomesticPenaltyHistoryResult,
			_orchestration.TryAppendDiplomaticThreatIssuerRewardHistoryResult);
			}
			catch (Exception ex)
			{
				_port.Log("threat history retry failed threat=" + threat.ThreatId + " error=" + ex.Message);
			}
		}
		foreach (WorldDiplomacyThreat threat in WorldDiplomacyRoundLifecycleRules.SelectThreatHistoryRetryBatch(
			_storage?.DiplomaticThreats, WorldDiplomacyRoundLifecycleRules.NeedsDomesticPenaltyHistoryRetry, 8))
		{
			_orchestration.TryAppendDiplomaticThreatDomesticPenaltyHistoryResult(threat);
		}
		foreach (WorldDiplomacyThreat threat in WorldDiplomacyRoundLifecycleRules.SelectThreatHistoryRetryBatch(
			_storage?.DiplomaticThreats, WorldDiplomacyRoundLifecycleRules.NeedsIssuerRewardHistoryRetry, 8))
		{
			_orchestration.TryAppendDiplomaticThreatIssuerRewardHistoryResult(threat);
		}
	}
}
