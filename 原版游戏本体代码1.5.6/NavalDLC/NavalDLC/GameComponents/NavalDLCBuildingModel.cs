using NavalDLC.Settlements.Building;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;

namespace NavalDLC.GameComponents;

public class NavalDLCBuildingModel : BuildingModel
{
	public override bool CanAddBuildingTypeToTown(BuildingType buildingType, Town town)
	{
		if (buildingType == NavalBuildingTypes.SettlementShipyard)
		{
			if (town.IsTown)
			{
				return town.Settlement.HasPort;
			}
			return false;
		}
		return base.BaseModel.CanAddBuildingTypeToTown(buildingType, town);
	}
}
