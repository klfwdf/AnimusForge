using System;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001DA RID: 474
	public abstract class PlayerProgressionModel : MBGameModel<PlayerProgressionModel>
	{
		// Token: 0x06001E52 RID: 7762
		public abstract float GetPlayerProgress();
	}
}
