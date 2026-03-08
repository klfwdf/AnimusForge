using System;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x0200019E RID: 414
	public abstract class GenericXpModel : MBGameModel<GenericXpModel>
	{
		// Token: 0x06001C63 RID: 7267
		public abstract float GetXpMultiplier(Hero hero);
	}
}
