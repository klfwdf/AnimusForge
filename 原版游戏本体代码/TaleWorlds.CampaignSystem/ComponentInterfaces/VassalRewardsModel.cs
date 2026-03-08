using System;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001CF RID: 463
	public abstract class VassalRewardsModel : MBGameModel<VassalRewardsModel>
	{
		// Token: 0x1700077F RID: 1919
		// (get) Token: 0x06001E0C RID: 7692
		public abstract float InfluenceReward { get; }

		// Token: 0x17000780 RID: 1920
		// (get) Token: 0x06001E0D RID: 7693
		public abstract int RelationRewardWithLeader { get; }

		// Token: 0x06001E0E RID: 7694
		public abstract TroopRoster GetTroopRewardsForJoiningKingdom(Kingdom kingdom);

		// Token: 0x06001E0F RID: 7695
		public abstract ItemRoster GetEquipmentRewardsForJoiningKingdom(Kingdom kingdom);
	}
}
