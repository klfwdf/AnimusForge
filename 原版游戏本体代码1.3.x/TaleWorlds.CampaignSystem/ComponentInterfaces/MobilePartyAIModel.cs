using System;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001AF RID: 431
	public abstract class MobilePartyAIModel : MBGameModel<MobilePartyAIModel>
	{
		// Token: 0x1700072C RID: 1836
		// (get) Token: 0x06001CFE RID: 7422
		public abstract float AiCheckInterval { get; }

		// Token: 0x1700072D RID: 1837
		// (get) Token: 0x06001CFF RID: 7423
		public abstract float FleeToNearbyPartyRadius { get; }

		// Token: 0x1700072E RID: 1838
		// (get) Token: 0x06001D00 RID: 7424
		public abstract float FleeToNearbySettlementRadius { get; }

		// Token: 0x1700072F RID: 1839
		// (get) Token: 0x06001D01 RID: 7425
		public abstract float HideoutPatrolDistanceAsDays { get; }

		// Token: 0x17000730 RID: 1840
		// (get) Token: 0x06001D02 RID: 7426
		public abstract float FortificationPatrolDistanceAsDays { get; }

		// Token: 0x17000731 RID: 1841
		// (get) Token: 0x06001D03 RID: 7427
		public abstract float VillagePatrolDistanceAsDays { get; }

		// Token: 0x17000732 RID: 1842
		// (get) Token: 0x06001D04 RID: 7428
		public abstract float SettlementDefendingNearbyPartyCheckRadius { get; }

		// Token: 0x17000733 RID: 1843
		// (get) Token: 0x06001D05 RID: 7429
		public abstract float SettlementDefendingWaitingPositionRadius { get; }

		// Token: 0x17000734 RID: 1844
		// (get) Token: 0x06001D06 RID: 7430
		public abstract float NeededFoodsInDaysThresholdForSiege { get; }

		// Token: 0x17000735 RID: 1845
		// (get) Token: 0x06001D07 RID: 7431
		public abstract float NeededFoodsInDaysThresholdForRaid { get; }

		// Token: 0x06001D08 RID: 7432
		public abstract bool ShouldConsiderAvoiding(MobileParty party, MobileParty targetParty);

		// Token: 0x06001D09 RID: 7433
		public abstract bool ShouldConsiderAttacking(MobileParty party, MobileParty targetParty);

		// Token: 0x06001D0A RID: 7434
		public abstract float GetPatrolRadius(MobileParty mobileParty, CampaignVec2 patrolPoint);

		// Token: 0x06001D0B RID: 7435
		public abstract bool ShouldPartyCheckInitiativeBehavior(MobileParty mobileParty);

		// Token: 0x06001D0C RID: 7436
		public abstract void GetBestInitiativeBehavior(MobileParty mobileParty, out AiBehavior bestInitiativeBehavior, out MobileParty bestInitiativeTargetParty, out float bestInitiativeBehaviorScore, out Vec2 averageEnemyVec);
	}
}
