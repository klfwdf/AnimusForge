using System;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x02000124 RID: 292
	public class DefaultLocationModel : LocationModel
	{
		// Token: 0x06001839 RID: 6201 RVA: 0x00074204 File Offset: 0x00072404
		public override int GetSettlementUpgradeLevel(LocationEncounter locationEncounter)
		{
			return locationEncounter.Settlement.Town.GetWallLevel();
		}

		// Token: 0x0600183A RID: 6202 RVA: 0x00074218 File Offset: 0x00072418
		public override string GetCivilianSceneLevel(Settlement settlement)
		{
			string text = "civilian";
			if (settlement.IsFortification)
			{
				string upgradeLevelTag = this.GetUpgradeLevelTag(settlement.Town.GetWallLevel());
				if (!upgradeLevelTag.IsEmpty<char>())
				{
					text = text + " " + upgradeLevelTag;
				}
			}
			return text;
		}

		// Token: 0x0600183B RID: 6203 RVA: 0x0007425C File Offset: 0x0007245C
		public override string GetCivilianUpgradeLevelTag(int upgradeLevel)
		{
			if (upgradeLevel == 0)
			{
				return "";
			}
			string text = "civilian";
			string upgradeLevelTag = this.GetUpgradeLevelTag(upgradeLevel);
			if (!upgradeLevelTag.IsEmpty<char>())
			{
				text = text + " " + upgradeLevelTag;
			}
			return text;
		}

		// Token: 0x0600183C RID: 6204 RVA: 0x00074296 File Offset: 0x00072496
		public override string GetUpgradeLevelTag(int upgradeLevel)
		{
			switch (upgradeLevel)
			{
			case 1:
				return "level_1";
			case 2:
				return "level_2";
			case 3:
				return "level_3";
			default:
				return "";
			}
		}
	}
}
