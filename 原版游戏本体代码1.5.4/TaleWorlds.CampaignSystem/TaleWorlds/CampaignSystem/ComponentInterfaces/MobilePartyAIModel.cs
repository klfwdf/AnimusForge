using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class MobilePartyAIModel : MBGameModel<MobilePartyAIModel>
{
	public abstract float AiCheckInterval { get; }

	public abstract float FleeToNearbyPartyRadius { get; }

	public abstract float FleeToNearbySettlementRadius { get; }

	public abstract float HideoutPatrolDistanceAsDays { get; }

	public abstract float FortificationPatrolDistanceAsDays { get; }

	public abstract float FortificationPortPatrolDistanceAsDays { get; }

	public abstract float VillagePatrolDistanceAsDays { get; }

	public abstract float SettlementDefendingNearbyPartyCheckRadius { get; }

	public abstract float SettlementDefendingWaitingPositionRadius { get; }

	public abstract float NeededFoodsInDaysThresholdForSiege { get; }

	public abstract float NeededFoodsInDaysThresholdForRaid { get; }

	public abstract bool ShouldConsiderAvoiding(MobileParty party, MobileParty targetParty);

	public abstract bool ShouldConsiderAttacking(MobileParty party, MobileParty targetParty);

	public abstract float GetPatrolRadius(MobileParty mobileParty, CampaignVec2 patrolPoint);

	public abstract float GetSettlementNearbyThreatAndAllyCheckRadius(Settlement settlement, bool isPort);

	public abstract bool ShouldPartyCheckInitiativeBehavior(MobileParty mobileParty);

	public abstract void GetBestInitiativeBehavior(MobileParty mobileParty, out AiBehavior bestInitiativeBehavior, out MobileParty bestInitiativeTargetParty, out float bestInitiativeBehaviorScore, out Vec2 averageEnemyVec);
}
