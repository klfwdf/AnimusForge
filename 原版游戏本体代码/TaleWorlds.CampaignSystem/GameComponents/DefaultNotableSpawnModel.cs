using System;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x0200012E RID: 302
	public class DefaultNotableSpawnModel : NotableSpawnModel
	{
		// Token: 0x060018D4 RID: 6356 RVA: 0x00079BE4 File Offset: 0x00077DE4
		public override int GetTargetNotableCountForSettlement(Settlement settlement, Occupation occupation)
		{
			int result = 0;
			if (settlement.IsTown)
			{
				if (occupation == Occupation.Merchant)
				{
					result = 2;
				}
				else if (occupation == Occupation.GangLeader)
				{
					result = 2;
				}
				else if (occupation == Occupation.Artisan)
				{
					result = 1;
				}
				else
				{
					result = 0;
				}
			}
			else if (settlement.IsVillage)
			{
				if (occupation == Occupation.Headman)
				{
					result = 1;
				}
				else if (occupation == Occupation.RuralNotable)
				{
					result = 2;
				}
			}
			return result;
		}
	}
}
