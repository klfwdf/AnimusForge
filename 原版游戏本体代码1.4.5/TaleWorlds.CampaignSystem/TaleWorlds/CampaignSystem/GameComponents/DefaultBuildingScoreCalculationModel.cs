using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.Core;
using TaleWorlds.LinQuick;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultBuildingScoreCalculationModel : BuildingScoreCalculationModel
{
	public override Building GetNextDailyBuilding(Town town)
	{
		return town.Buildings.GetRandomElementWithPredicate((Building b) => b.BuildingType.IsDailyProject);
	}

	public override Building GetNextBuilding(Town town)
	{
		return town.Buildings.WhereQ((Building x) => !x.BuildingType.IsDailyProject && x.CurrentLevel < 3 && !town.BuildingsInProgress.Contains(x)).GetRandomElementInefficiently();
	}
}
