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
		if (NobleGatheringBehavior.IsTemporaryGatheringParty(targetParty) && !CourierDeliveryBehavior.IsBanditOrOutlawParty(party))
		{
			return false;
		}
		if (CourierDeliveryBehavior.IsCourierParty(party))
		{
			return false;
		}
		if (CourierDeliveryBehavior.IsCourierParty(targetParty) && !CourierDeliveryBehavior.IsBanditOrOutlawParty(party))
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
		using (PerfProbe.Scope("CourierMobilePartyAIModel.GetBestInitiativeBehavior"))
		{
			_inner.GetBestInitiativeBehavior(mobileParty, out bestInitiativeBehavior, out bestInitiativeTargetParty, out bestInitiativeBehaviorScore, out averageEnemyVec);
			if (ProactiveNpcRequestBehavior.IsProactiveRequestParty(mobileParty) && bestInitiativeBehavior == AiBehavior.EngageParty && bestInitiativeTargetParty == MobileParty.MainParty)
			{
				bestInitiativeBehavior = AiBehavior.None;
				bestInitiativeTargetParty = null;
				bestInitiativeBehaviorScore = 0f;
				Logger.LogVerbose("ProactiveNpcRequest", "initiative_attack_suppressed:" + (mobileParty?.StringId ?? ""), () => "initiative attack suppressed party=" + (mobileParty?.StringId ?? ""), 10.0);
			}
			if (NobleGatheringBehavior.IsTemporaryGatheringParty(mobileParty) && bestInitiativeBehavior == AiBehavior.EngageParty)
			{
				bestInitiativeBehavior = AiBehavior.None;
				bestInitiativeTargetParty = null;
				bestInitiativeBehaviorScore = 0f;
				Logger.LogVerbose("NobleGathering", "initiative_attack_suppressed:" + (mobileParty?.StringId ?? ""), () => "initiative attack suppressed party=" + (mobileParty?.StringId ?? ""), 10.0);
			}
			if (bestInitiativeBehavior == AiBehavior.EngageParty && NobleGatheringBehavior.IsTemporaryGatheringParty(bestInitiativeTargetParty) && !CourierDeliveryBehavior.IsBanditOrOutlawParty(mobileParty))
			{
				bestInitiativeBehavior = AiBehavior.None;
				bestInitiativeTargetParty = null;
				bestInitiativeBehaviorScore = 0f;
				Logger.LogVerbose("NobleGathering", "non_bandit_temporary_party_attack_suppressed:" + (mobileParty?.StringId ?? ""), () => "non-bandit temporary gathering party attack suppressed party=" + (mobileParty?.StringId ?? ""), 10.0);
			}
			if (CourierDeliveryBehavior.IsCourierParty(mobileParty) && bestInitiativeBehavior == AiBehavior.EngageParty)
			{
				bestInitiativeBehavior = AiBehavior.None;
				bestInitiativeTargetParty = null;
				bestInitiativeBehaviorScore = 0f;
				Logger.LogVerbose("CourierDelivery", "initiative_attack_suppressed:" + (mobileParty?.StringId ?? ""), () => "initiative attack suppressed party=" + (mobileParty?.StringId ?? ""), 10.0);
			}
			if (bestInitiativeBehavior == AiBehavior.EngageParty && CourierDeliveryBehavior.IsCourierParty(bestInitiativeTargetParty) && !CourierDeliveryBehavior.IsBanditOrOutlawParty(mobileParty))
			{
				string partyId = mobileParty?.StringId ?? "";
				string courierId = bestInitiativeTargetParty?.StringId ?? "";
				bestInitiativeBehavior = AiBehavior.None;
				bestInitiativeTargetParty = null;
				bestInitiativeBehaviorScore = 0f;
				Logger.LogVerbose("CourierDelivery", "non_bandit_courier_attack_suppressed:" + partyId + ":" + courierId, () => "non-bandit courier attack suppressed party=" + partyId + " courier=" + courierId, 10.0);
			}
		}
	}
}
