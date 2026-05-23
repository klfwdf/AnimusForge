using System;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.Core;
using TaleWorlds.LinQuick;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x020000FB RID: 251
	public class DefaultBuildingScoreCalculationModel : BuildingScoreCalculationModel
	{
		// Token: 0x0600167F RID: 5759 RVA: 0x000673C2 File Offset: 0x000655C2
		public override Building GetNextDailyBuilding(Town town)
		{
			return town.Buildings.GetRandomElementWithPredicate((Building b) => b.BuildingType.IsDailyProject);
		}

		// Token: 0x06001680 RID: 5760 RVA: 0x000673F0 File Offset: 0x000655F0
		public override Building GetNextBuilding(Town town)
		{
			return town.Buildings.WhereQ((Building x) => !x.BuildingType.IsDailyProject && x.CurrentLevel < 3 && !town.BuildingsInProgress.Contains(x)).GetRandomElementInefficiently<Building>();
		}
	}
}
