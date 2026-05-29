using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace AnimusForge;

public sealed class CourierMobilePartyAIModel : MobilePartyAIModel
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
#if BANNERLORD_1_4_OR_GREATER
	public override float FortificationPortPatrolDistanceAsDays => _inner.FortificationPortPatrolDistanceAsDays;
#endif
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
		if (CourierDeliveryBehavior.IsCourierParty(party))
		{
			return false;
		}
		return _inner.ShouldConsiderAttacking(party, targetParty);
	}

	public override float GetPatrolRadius(MobileParty mobileParty, CampaignVec2 patrolPoint)
	{
		return _inner.GetPatrolRadius(mobileParty, patrolPoint);
	}

#if BANNERLORD_1_4_OR_GREATER
	public override float GetSettlementNearbyThreatAndAllyCheckRadius(Settlement settlement, bool isSpotting)
	{
		return _inner.GetSettlementNearbyThreatAndAllyCheckRadius(settlement, isSpotting);
	}

#endif
	public override bool ShouldPartyCheckInitiativeBehavior(MobileParty mobileParty)
	{
		return _inner.ShouldPartyCheckInitiativeBehavior(mobileParty);
	}

	public override void GetBestInitiativeBehavior(MobileParty mobileParty, out AiBehavior bestInitiativeBehavior, out MobileParty bestInitiativeTargetParty, out float bestInitiativeBehaviorScore, out Vec2 averageEnemyVec)
	{
		_inner.GetBestInitiativeBehavior(mobileParty, out bestInitiativeBehavior, out bestInitiativeTargetParty, out bestInitiativeBehaviorScore, out averageEnemyVec);
		if (CourierDeliveryBehavior.IsCourierParty(mobileParty) && bestInitiativeBehavior == AiBehavior.EngageParty)
		{
			bestInitiativeBehavior = AiBehavior.None;
			bestInitiativeTargetParty = null;
			bestInitiativeBehaviorScore = 0f;
			Logger.LogVerbose("CourierDelivery", "initiative_attack_suppressed:" + (mobileParty?.StringId ?? ""), () => "initiative attack suppressed party=" + (mobileParty?.StringId ?? ""), 10.0);
		}
	}
}
