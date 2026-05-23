using System;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001A0 RID: 416
	public abstract class MobilePartyFoodConsumptionModel : MBGameModel<MobilePartyFoodConsumptionModel>
	{
		// Token: 0x1700070D RID: 1805
		// (get) Token: 0x06001C6A RID: 7274
		public abstract int NumberOfMenOnMapToEatOneFood { get; }

		// Token: 0x06001C6B RID: 7275
		public abstract ExplainedNumber CalculateDailyBaseFoodConsumptionf(MobileParty party, bool includeDescription = false);

		// Token: 0x06001C6C RID: 7276
		public abstract ExplainedNumber CalculateDailyFoodConsumptionf(MobileParty party, ExplainedNumber baseConsumption);

		// Token: 0x06001C6D RID: 7277
		public abstract bool DoesPartyConsumeFood(MobileParty mobileParty);
	}
}
