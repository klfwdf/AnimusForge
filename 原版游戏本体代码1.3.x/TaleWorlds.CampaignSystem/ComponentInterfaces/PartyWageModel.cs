using System;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001BE RID: 446
	public abstract class PartyWageModel : MBGameModel<PartyWageModel>
	{
		// Token: 0x1700074E RID: 1870
		// (get) Token: 0x06001D8C RID: 7564
		public abstract int MaxWagePaymentLimit { get; }

		// Token: 0x06001D8D RID: 7565
		public abstract int GetCharacterWage(CharacterObject character);

		// Token: 0x06001D8E RID: 7566
		public abstract ExplainedNumber GetTotalWage(MobileParty mobileParty, TroopRoster troopRoster, bool includeDescriptions = false);

		// Token: 0x06001D8F RID: 7567
		public abstract ExplainedNumber GetTroopRecruitmentCost(CharacterObject troop, Hero buyerHero, bool withoutItemCost = false);
	}
}
