using System;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001A1 RID: 417
	public abstract class PartyMoraleModel : MBGameModel<PartyMoraleModel>
	{
		// Token: 0x1700070E RID: 1806
		// (get) Token: 0x06001C6F RID: 7279
		public abstract float HighMoraleValue { get; }

		// Token: 0x06001C70 RID: 7280
		public abstract int GetDailyStarvationMoralePenalty(PartyBase party);

		// Token: 0x06001C71 RID: 7281
		public abstract int GetDailyNoWageMoralePenalty(MobileParty party);

		// Token: 0x06001C72 RID: 7282
		public abstract float GetStandardBaseMorale(PartyBase party);

		// Token: 0x06001C73 RID: 7283
		public abstract float GetVictoryMoraleChange(PartyBase party);

		// Token: 0x06001C74 RID: 7284
		public abstract float GetDefeatMoraleChange(PartyBase party);

		// Token: 0x06001C75 RID: 7285
		public abstract ExplainedNumber GetEffectivePartyMorale(MobileParty party, bool includeDescription = false);
	}
}
