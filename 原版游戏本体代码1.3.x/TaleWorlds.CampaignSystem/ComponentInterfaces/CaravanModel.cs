using System;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x02000194 RID: 404
	public abstract class CaravanModel : MBGameModel<CaravanModel>
	{
		// Token: 0x17000707 RID: 1799
		// (get) Token: 0x06001C18 RID: 7192
		public abstract int MaxNumberOfItemsToBuyFromSingleCategory { get; }

		// Token: 0x06001C19 RID: 7193
		public abstract int GetMaxGoldToSpendOnOneItemCategory(MobileParty caravan, ItemCategory itemCategory);

		// Token: 0x06001C1A RID: 7194
		public abstract int GetInitialTradeGold(Hero owner, bool isNavalCaravan, bool eliteCaravan);

		// Token: 0x06001C1B RID: 7195
		public abstract int GetCaravanFormingCost(bool eliteCaravan, bool navalCaravan);

		// Token: 0x06001C1C RID: 7196
		public abstract int GetPowerChangeAfterCaravanCreation(Hero hero, MobileParty caravanParty);

		// Token: 0x06001C1D RID: 7197
		public abstract bool CanHeroCreateCaravan(Hero hero);

		// Token: 0x06001C1E RID: 7198
		public abstract float GetEliteCaravanSpawnChance(Hero hero);
	}
}
