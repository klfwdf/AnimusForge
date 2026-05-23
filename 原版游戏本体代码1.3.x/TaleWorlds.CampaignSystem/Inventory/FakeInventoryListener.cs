using System;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.Inventory
{
	// Token: 0x020000D6 RID: 214
	public class FakeInventoryListener : InventoryListener
	{
		// Token: 0x0600146F RID: 5231 RVA: 0x0005E557 File Offset: 0x0005C757
		public override int GetGold()
		{
			return 0;
		}

		// Token: 0x06001470 RID: 5232 RVA: 0x0005E55A File Offset: 0x0005C75A
		public override TextObject GetTraderName()
		{
			return TextObject.GetEmpty();
		}

		// Token: 0x06001471 RID: 5233 RVA: 0x0005E561 File Offset: 0x0005C761
		public override void SetGold(int gold)
		{
		}

		// Token: 0x06001472 RID: 5234 RVA: 0x0005E563 File Offset: 0x0005C763
		public override void OnTransaction()
		{
		}

		// Token: 0x06001473 RID: 5235 RVA: 0x0005E565 File Offset: 0x0005C765
		public override PartyBase GetOppositeParty()
		{
			return null;
		}
	}
}
