using System;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001AC RID: 428
	public abstract class TargetScoreCalculatingModel : MBGameModel<TargetScoreCalculatingModel>
	{
		// Token: 0x17000726 RID: 1830
		// (get) Token: 0x06001CEB RID: 7403
		public abstract float TravelingToAssignmentFactor { get; }

		// Token: 0x17000727 RID: 1831
		// (get) Token: 0x06001CEC RID: 7404
		public abstract float BesiegingFactor { get; }

		// Token: 0x17000728 RID: 1832
		// (get) Token: 0x06001CED RID: 7405
		public abstract float AssaultingTownFactor { get; }

		// Token: 0x17000729 RID: 1833
		// (get) Token: 0x06001CEE RID: 7406
		public abstract float RaidingFactor { get; }

		// Token: 0x1700072A RID: 1834
		// (get) Token: 0x06001CEF RID: 7407
		public abstract float DefendingFactor { get; }

		// Token: 0x06001CF0 RID: 7408
		public abstract float GetPatrollingFactor(bool isNavalPatrolling);

		// Token: 0x06001CF1 RID: 7409
		public abstract float GetTargetScoreForFaction(Settlement targetSettlement, Army.ArmyTypes missionType, MobileParty mobileParty, float ourStrength);

		// Token: 0x06001CF2 RID: 7410
		public abstract float CalculatePatrollingScoreForSettlement(Settlement settlement, bool isFromPort, MobileParty mobileParty);

		// Token: 0x06001CF3 RID: 7411
		public abstract float CurrentObjectiveValue(MobileParty mobileParty);
	}
}
