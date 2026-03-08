using System;
using TaleWorlds.CampaignSystem.ComponentInterfaces;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x0200015A RID: 346
	public class DefaultTavernMercenaryTroopsModel : TavernMercenaryTroopsModel
	{
		// Token: 0x170006D3 RID: 1747
		// (get) Token: 0x06001A91 RID: 6801 RVA: 0x00087B34 File Offset: 0x00085D34
		public override float RegularMercenariesSpawnChance
		{
			get
			{
				return 0.7f;
			}
		}
	}
}
