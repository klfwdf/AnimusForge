using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;

namespace AnimusForge;

public sealed class CourierMobilePartyAIModel : DefaultMobilePartyAIModel
{
	private readonly MobilePartyAIModel _inner;

	public CourierMobilePartyAIModel(MobilePartyAIModel inner)
	{
		_inner = inner ?? new DefaultMobilePartyAIModel();
	}

	public override float AiCheckInterval => _inner.AiCheckInterval;
	public override float FleeToNearbyPartyRadius => _inner.FleeToNearbyPartyRadius;
	public override float FleeToNearbySettlementRadius => _inner.FleeToNearbySettlementRadius;
	public override float HideoutPatrolDistanceAsDays => _inner.HideoutPatrolDistanceAsDays;
	public override float FortificationPatrolDistanceAsDays => _inner.FortificationPatrolDistanceAsDays;
	public override float VillagePatrolDistanceAsDays => _inner.VillagePatrolDistanceAsDays;
	public override float SettlementDefendingNearbyPartyCheckRadius => _inner.SettlementDefendingNearbyPartyCheckRadius;
	public override float SettlementDefendingWaitingPositionRadius => _inner.SettlementDefendingWaitingPositionRadius;
	public override float NeededFoodsInDaysThresholdForSiege => _inner.NeededFoodsInDaysThresholdForSiege;
	public override float NeededFoodsInDaysThresholdForRaid => _inner.NeededFoodsInDaysThresholdForRaid;

	public override bool ShouldConsiderAvoiding(MobileParty party, MobileParty targetParty)
	{
		return _inner.ShouldConsiderAvoiding(party, targetParty);
	}

	public override bool ShouldConsiderAttacking(MobileParty party, MobileParty targetParty)
	{
		if (NobleGatheringBehavior.IsTemporaryGatheringParty(party))
		{
			return false;
		}
		bool hasPotentialActiveCouriers = CourierDeliveryBehavior.HasPotentialActiveCourierPartiesForAi();
		bool partyIsCourier = CourierDeliveryBehavior.HasCourierPartyIdPrefix(party)
			|| (hasPotentialActiveCouriers && CourierDeliveryBehavior.IsCourierParty(party));
		if (partyIsCourier)
		{
			return false;
		}
		if (NobleGatheringBehavior.IsTemporaryGatheringParty(targetParty) && !CourierDeliveryBehavior.IsBanditOrOutlawParty(party))
		{
			return false;
		}
		bool targetIsCourier = CourierDeliveryBehavior.HasCourierPartyIdPrefix(targetParty)
			|| (hasPotentialActiveCouriers && CourierDeliveryBehavior.IsCourierParty(targetParty));
		if (targetIsCourier
			&& (CourierDeliveryBehavior.IsNpcIssuedCourierParty(targetParty) || !CourierDeliveryBehavior.IsBanditOrOutlawParty(party)))
		{
			return false;
		}
		if (targetParty == MobileParty.MainParty && ProactiveNpcRequestBehavior.IsProactiveRequestParty(party))
		{
			return false;
		}
		return _inner.ShouldConsiderAttacking(party, targetParty);
	}

	public override float GetPatrolRadius(MobileParty mobileParty, CampaignVec2 patrolPoint)
	{
		return _inner.GetPatrolRadius(mobileParty, patrolPoint);
	}

	public override bool ShouldPartyCheckInitiativeBehavior(MobileParty mobileParty)
	{
		return _inner.ShouldPartyCheckInitiativeBehavior(mobileParty);
	}

	public override void GetBestInitiativeBehavior(MobileParty mobileParty, out AiBehavior bestInitiativeBehavior, out MobileParty bestInitiativeTargetParty, out float bestInitiativeBehaviorScore, out Vec2 averageEnemyVec)
	{
		_inner.GetBestInitiativeBehavior(mobileParty, out bestInitiativeBehavior, out bestInitiativeTargetParty, out bestInitiativeBehaviorScore, out averageEnemyVec);
		// The custom rules below only ever change an EngageParty result. Most AI evaluations
		// are not engagements, so avoid all special-party checks on that hot path.
		if (bestInitiativeBehavior != AiBehavior.EngageParty)
		{
			return;
		}
		if (ProactiveNpcRequestBehavior.IsProactiveRequestParty(mobileParty) && bestInitiativeTargetParty == MobileParty.MainParty)
		{
			bestInitiativeBehavior = AiBehavior.None;
			bestInitiativeTargetParty = null;
			bestInitiativeBehaviorScore = 0f;
			Logger.LogVerbose("ProactiveNpcRequest", "initiative_attack_suppressed:" + (mobileParty?.StringId ?? ""), () => "initiative attack suppressed party=" + (mobileParty?.StringId ?? ""), 10.0);
			return;
		}
		if (NobleGatheringBehavior.IsTemporaryGatheringParty(mobileParty))
		{
			bestInitiativeBehavior = AiBehavior.None;
			bestInitiativeTargetParty = null;
			bestInitiativeBehaviorScore = 0f;
			Logger.LogVerbose("NobleGathering", "initiative_attack_suppressed:" + (mobileParty?.StringId ?? ""), () => "initiative attack suppressed party=" + (mobileParty?.StringId ?? ""), 10.0);
			return;
		}
		if (NobleGatheringBehavior.IsTemporaryGatheringParty(bestInitiativeTargetParty) && !CourierDeliveryBehavior.IsBanditOrOutlawParty(mobileParty))
		{
			bestInitiativeBehavior = AiBehavior.None;
			bestInitiativeTargetParty = null;
			bestInitiativeBehaviorScore = 0f;
			Logger.LogVerbose("NobleGathering", "non_bandit_temporary_party_attack_suppressed:" + (mobileParty?.StringId ?? ""), () => "non-bandit temporary gathering party attack suppressed party=" + (mobileParty?.StringId ?? ""), 10.0);
			return;
		}
		bool hasPotentialActiveCouriers = CourierDeliveryBehavior.HasPotentialActiveCourierPartiesForAi();
		bool partyIsCourier = CourierDeliveryBehavior.HasCourierPartyIdPrefix(mobileParty)
			|| (hasPotentialActiveCouriers && CourierDeliveryBehavior.IsCourierParty(mobileParty));
		if (partyIsCourier)
		{
			bestInitiativeBehavior = AiBehavior.None;
			bestInitiativeTargetParty = null;
			bestInitiativeBehaviorScore = 0f;
			Logger.LogVerbose("CourierDelivery", "initiative_attack_suppressed:" + (mobileParty?.StringId ?? ""), () => "initiative attack suppressed party=" + (mobileParty?.StringId ?? ""), 10.0);
			return;
		}
		bool targetIsCourier = CourierDeliveryBehavior.HasCourierPartyIdPrefix(bestInitiativeTargetParty)
			|| (hasPotentialActiveCouriers && CourierDeliveryBehavior.IsCourierParty(bestInitiativeTargetParty));
		if (!targetIsCourier)
		{
			return;
		}
		bool protectedNpcCourier = CourierDeliveryBehavior.IsNpcIssuedCourierParty(bestInitiativeTargetParty);
		if (!protectedNpcCourier && CourierDeliveryBehavior.IsBanditOrOutlawParty(mobileParty))
		{
			return;
		}
		string partyId = mobileParty?.StringId ?? "";
		string courierId = bestInitiativeTargetParty?.StringId ?? "";
		bestInitiativeBehavior = AiBehavior.None;
		bestInitiativeTargetParty = null;
		bestInitiativeBehaviorScore = 0f;
		string suppressionKind = protectedNpcCourier ? "npc_courier" : "non_bandit_courier";
		Logger.LogVerbose("CourierDelivery", suppressionKind + "_attack_suppressed:" + partyId + ":" + courierId, () => suppressionKind + " attack suppressed party=" + partyId + " courier=" + courierId, 10.0);
	}
}
