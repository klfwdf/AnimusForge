using System;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001D0 RID: 464
	public abstract class ClanTierModel : MBGameModel<ClanTierModel>
	{
		// Token: 0x17000781 RID: 1921
		// (get) Token: 0x06001E11 RID: 7697
		public abstract int MinClanTier { get; }

		// Token: 0x17000782 RID: 1922
		// (get) Token: 0x06001E12 RID: 7698
		public abstract int MaxClanTier { get; }

		// Token: 0x17000783 RID: 1923
		// (get) Token: 0x06001E13 RID: 7699
		public abstract int MercenaryEligibleTier { get; }

		// Token: 0x17000784 RID: 1924
		// (get) Token: 0x06001E14 RID: 7700
		public abstract int VassalEligibleTier { get; }

		// Token: 0x17000785 RID: 1925
		// (get) Token: 0x06001E15 RID: 7701
		public abstract int BannerEligibleTier { get; }

		// Token: 0x17000786 RID: 1926
		// (get) Token: 0x06001E16 RID: 7702
		public abstract int RebelClanStartingTier { get; }

		// Token: 0x17000787 RID: 1927
		// (get) Token: 0x06001E17 RID: 7703
		public abstract int CompanionToLordClanStartingTier { get; }

		// Token: 0x06001E18 RID: 7704
		public abstract int CalculateInitialRenown(Clan clan);

		// Token: 0x06001E19 RID: 7705
		public abstract int CalculateInitialInfluence(Clan clan);

		// Token: 0x06001E1A RID: 7706
		public abstract int CalculateTier(Clan clan);

		// Token: 0x06001E1B RID: 7707
		public abstract ValueTuple<ExplainedNumber, bool> HasUpcomingTier(Clan clan, out TextObject extraExplanation, bool includeDescriptions = false);

		// Token: 0x06001E1C RID: 7708
		public abstract int GetRequiredRenownForTier(int tier);

		// Token: 0x06001E1D RID: 7709
		public abstract int GetPartyLimitForTier(Clan clan, int clanTierToCheck);

		// Token: 0x06001E1E RID: 7710
		public abstract int GetCompanionLimit(Clan clan);
	}
}
