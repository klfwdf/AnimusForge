using System;
using System.Collections.Generic;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001C4 RID: 452
	public abstract class SettlementMenuOverlayModel : MBGameModel<SettlementMenuOverlayModel>
	{
		// Token: 0x06001DAE RID: 7598
		public abstract Dictionary<Hero, bool> GetOverlayHeroes();
	}
}
