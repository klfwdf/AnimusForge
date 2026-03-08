using System;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001EA RID: 490
	public abstract class TavernMercenaryTroopsModel : MBGameModel<TavernMercenaryTroopsModel>
	{
		// Token: 0x170007A8 RID: 1960
		// (get) Token: 0x06001EC9 RID: 7881
		public abstract float RegularMercenariesSpawnChance { get; }
	}
}
