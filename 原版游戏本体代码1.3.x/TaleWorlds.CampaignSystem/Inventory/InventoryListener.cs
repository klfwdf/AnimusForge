using System;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.Inventory
{
	// Token: 0x020000D5 RID: 213
	public abstract class InventoryListener
	{
		// Token: 0x06001469 RID: 5225
		public abstract int GetGold();

		// Token: 0x0600146A RID: 5226
		public abstract TextObject GetTraderName();

		// Token: 0x0600146B RID: 5227
		public abstract void SetGold(int gold);

		// Token: 0x0600146C RID: 5228
		public abstract PartyBase GetOppositeParty();

		// Token: 0x0600146D RID: 5229
		public abstract void OnTransaction();
	}
}
