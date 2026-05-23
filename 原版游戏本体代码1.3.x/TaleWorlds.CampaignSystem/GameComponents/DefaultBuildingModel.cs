using System;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x020000FA RID: 250
	public class DefaultBuildingModel : BuildingModel
	{
		// Token: 0x0600167D RID: 5757 RVA: 0x00067298 File Offset: 0x00065498
		public override bool CanAddBuildingTypeToTown(BuildingType buildingType, Town town)
		{
			if (buildingType == DefaultBuildingTypes.SettlementFortifications || buildingType == DefaultBuildingTypes.SettlementBarracks || buildingType == DefaultBuildingTypes.SettlementTrainingFields || buildingType == DefaultBuildingTypes.SettlementGuardHouse || buildingType == DefaultBuildingTypes.SettlementSiegeWorkshop || buildingType == DefaultBuildingTypes.SettlementTaxOffice || buildingType == DefaultBuildingTypes.SettlementMarketplace || buildingType == DefaultBuildingTypes.SettlementWarehouse || buildingType == DefaultBuildingTypes.SettlementMason || buildingType == DefaultBuildingTypes.SettlementWaterworks || buildingType == DefaultBuildingTypes.SettlementCourthouse || buildingType == DefaultBuildingTypes.SettlementRoadsAndPaths)
			{
				return town.IsTown;
			}
			if (buildingType == DefaultBuildingTypes.CastleFortifications || buildingType == DefaultBuildingTypes.CastleBarracks || buildingType == DefaultBuildingTypes.CastleTrainingFields || buildingType == DefaultBuildingTypes.CastleGuardHouse || buildingType == DefaultBuildingTypes.CastleSiegeWorkshop || buildingType == DefaultBuildingTypes.CastleCastallansOffice || buildingType == DefaultBuildingTypes.CastleGranary || buildingType == DefaultBuildingTypes.CastleCraftmansQuarters || buildingType == DefaultBuildingTypes.CastleFarmlands || buildingType == DefaultBuildingTypes.CastleMason || buildingType == DefaultBuildingTypes.CastleRoadsAndPaths)
			{
				return town.IsCastle;
			}
			if (buildingType == DefaultBuildingTypes.SettlementDailyHousing || buildingType == DefaultBuildingTypes.SettlementDailyTrainMilitia || buildingType == DefaultBuildingTypes.SettlementDailyFestivalAndGames || buildingType == DefaultBuildingTypes.SettlementDailyIrrigation)
			{
				return town.IsTown;
			}
			return (buildingType != DefaultBuildingTypes.CastleDailySlackenGarrison && buildingType != DefaultBuildingTypes.CastleDailyRaiseTroops && buildingType != DefaultBuildingTypes.CastleDailyDrills && buildingType != DefaultBuildingTypes.CastleDailyIrrigation) || town.IsCastle;
		}
	}
}
