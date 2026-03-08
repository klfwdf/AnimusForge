using System;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001B2 RID: 434
	public abstract class NotableSpawnModel : MBGameModel<NotableSpawnModel>
	{
		// Token: 0x06001D21 RID: 7457
		public abstract int GetTargetNotableCountForSettlement(Settlement settlement, Occupation occupation);
	}
}
