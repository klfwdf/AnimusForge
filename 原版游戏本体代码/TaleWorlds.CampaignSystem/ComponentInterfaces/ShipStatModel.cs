using System;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x0200018F RID: 399
	public abstract class ShipStatModel : MBGameModel<ShipStatModel>
	{
		// Token: 0x06001BF6 RID: 7158
		public abstract float GetShipFlagshipScore(Ship ship);
	}
}
