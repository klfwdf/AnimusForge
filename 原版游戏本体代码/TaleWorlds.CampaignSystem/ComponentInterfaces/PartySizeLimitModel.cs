using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001BB RID: 443
	public abstract class PartySizeLimitModel : MBGameModel<PartySizeLimitModel>
	{
		// Token: 0x06001D78 RID: 7544
		public abstract ExplainedNumber GetPartyMemberSizeLimit(PartyBase party, bool includeDescriptions = false);

		// Token: 0x06001D79 RID: 7545
		public abstract ExplainedNumber GetPartyPrisonerSizeLimit(PartyBase party, bool includeDescriptions = false);

		// Token: 0x06001D7A RID: 7546
		public abstract ExplainedNumber CalculateGarrisonPartySizeLimit(Settlement settlement, bool includeDescriptions = false);

		// Token: 0x06001D7B RID: 7547
		public abstract int GetClanTierPartySizeEffectForHero(Hero hero);

		// Token: 0x06001D7C RID: 7548
		public abstract int GetNextClanTierPartySizeEffectChangeForHero(Hero hero);

		// Token: 0x06001D7D RID: 7549
		public abstract int GetAssumedPartySizeForLordParty(Hero leaderHero, IFaction partyMapFaction, Clan actualClan);

		// Token: 0x1700074D RID: 1869
		// (get) Token: 0x06001D7E RID: 7550
		public abstract int MinimumNumberOfVillagersAtVillagerParty { get; }

		// Token: 0x06001D7F RID: 7551
		public abstract int GetIdealVillagerPartySize(Village village);

		// Token: 0x06001D80 RID: 7552
		public abstract TroopRoster FindAppropriateInitialRosterForMobileParty(MobileParty party, PartyTemplateObject partyTemplate);

		// Token: 0x06001D81 RID: 7553
		public abstract List<Ship> FindAppropriateInitialShipsForMobileParty(MobileParty party, PartyTemplateObject partyTemplate);
	}
}
