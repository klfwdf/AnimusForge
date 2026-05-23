using System;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001A5 RID: 421
	public abstract class AllianceModel : MBGameModel<AllianceModel>
	{
		// Token: 0x17000715 RID: 1813
		// (get) Token: 0x06001C87 RID: 7303
		public abstract CampaignTime MaxDurationOfAlliance { get; }

		// Token: 0x17000716 RID: 1814
		// (get) Token: 0x06001C88 RID: 7304
		public abstract CampaignTime MaxDurationOfWarParticipation { get; }

		// Token: 0x17000717 RID: 1815
		// (get) Token: 0x06001C89 RID: 7305
		public abstract int MaxNumberOfAlliances { get; }

		// Token: 0x17000718 RID: 1816
		// (get) Token: 0x06001C8A RID: 7306
		public abstract CampaignTime DurationForOffers { get; }

		// Token: 0x06001C8B RID: 7307
		public abstract int GetCallToWarCost(Kingdom callingKingdom, Kingdom calledKingdom, Kingdom kingdomToCallToWarAgainst);

		// Token: 0x06001C8C RID: 7308
		public abstract ExplainedNumber GetScoreOfStartingAlliance(Kingdom kingdomDeclaresAlliance, Kingdom kingdomDeclaredAlliance, IFaction evaluatingFaction, out TextObject explanation, bool includeDescription = false);

		// Token: 0x06001C8D RID: 7309
		public abstract float GetScoreOfCallingToWar(Kingdom callingKingdom, Kingdom calledKingdom, Kingdom kingdomToCallToWarAgainst, IFaction evaluatingFaction, out TextObject reason);

		// Token: 0x06001C8E RID: 7310
		public abstract float GetScoreOfJoiningWar(Kingdom offeringKingdom, Kingdom kingdomToOfferToJoinWarWith, Kingdom kingdomToOfferToJoinWarAgainst, IFaction evaluatingFaction, out TextObject reason);

		// Token: 0x06001C8F RID: 7311
		public abstract int GetInfluenceCostOfProposingStartingAlliance(Clan proposingClan);

		// Token: 0x06001C90 RID: 7312
		public abstract int GetInfluenceCostOfCallingToWar(Clan proposingClan);
	}
}
