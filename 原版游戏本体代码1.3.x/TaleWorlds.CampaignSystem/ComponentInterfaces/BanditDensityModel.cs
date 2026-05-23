using System;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001B1 RID: 433
	public abstract class BanditDensityModel : MBGameModel<BanditDensityModel>
	{
		// Token: 0x06001D13 RID: 7443
		public abstract int GetMaxSupportedNumberOfLootersForClan(Clan clan);

		// Token: 0x17000739 RID: 1849
		// (get) Token: 0x06001D14 RID: 7444
		public abstract int NumberOfMinimumBanditPartiesInAHideoutToInfestIt { get; }

		// Token: 0x1700073A RID: 1850
		// (get) Token: 0x06001D15 RID: 7445
		public abstract int NumberOfMaximumBanditPartiesInEachHideout { get; }

		// Token: 0x1700073B RID: 1851
		// (get) Token: 0x06001D16 RID: 7446
		public abstract int NumberOfMaximumBanditPartiesAroundEachHideout { get; }

		// Token: 0x1700073C RID: 1852
		// (get) Token: 0x06001D17 RID: 7447
		public abstract int NumberOfMaximumHideoutsAtEachBanditFaction { get; }

		// Token: 0x1700073D RID: 1853
		// (get) Token: 0x06001D18 RID: 7448
		public abstract int NumberOfInitialHideoutsAtEachBanditFaction { get; }

		// Token: 0x1700073E RID: 1854
		// (get) Token: 0x06001D19 RID: 7449
		public abstract int NumberOfMinimumBanditTroopsInHideoutMission { get; }

		// Token: 0x1700073F RID: 1855
		// (get) Token: 0x06001D1A RID: 7450
		public abstract int NumberOfMaximumTroopCountForFirstFightInHideout { get; }

		// Token: 0x17000740 RID: 1856
		// (get) Token: 0x06001D1B RID: 7451
		public abstract int NumberOfMaximumTroopCountForBossFightInHideout { get; }

		// Token: 0x17000741 RID: 1857
		// (get) Token: 0x06001D1C RID: 7452
		public abstract float SpawnPercentageForFirstFightInHideoutMission { get; }

		// Token: 0x06001D1D RID: 7453
		public abstract int GetMinimumTroopCountForHideoutMission(MobileParty party, bool isAssault);

		// Token: 0x06001D1E RID: 7454
		public abstract int GetMaximumTroopCountForHideoutMission(MobileParty party, bool isAssault);

		// Token: 0x06001D1F RID: 7455
		public abstract bool IsPositionInsideNavalSafeZone(CampaignVec2 position);
	}
}
