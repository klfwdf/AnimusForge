using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class BuildingScoreCalculationModel : MBGameModel<BuildingScoreCalculationModel>
{
	public abstract Building GetNextBuilding(Town town);

	public abstract Building GetNextDailyBuilding(Town town);
}
