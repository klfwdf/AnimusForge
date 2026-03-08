using System;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Naval;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x02000153 RID: 339
	public class DefaultShipStatModel : ShipStatModel
	{
		// Token: 0x06001A3D RID: 6717 RVA: 0x000840E7 File Offset: 0x000822E7
		public override float GetShipFlagshipScore(Ship ship)
		{
			return 0f;
		}
	}
}
