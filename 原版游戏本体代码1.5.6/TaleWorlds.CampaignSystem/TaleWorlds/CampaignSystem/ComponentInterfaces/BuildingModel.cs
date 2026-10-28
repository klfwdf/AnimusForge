using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class BuildingModel : MBGameModel<BuildingModel>
{
	public abstract bool CanAddBuildingTypeToTown(BuildingType buildingType, Town town);
}
