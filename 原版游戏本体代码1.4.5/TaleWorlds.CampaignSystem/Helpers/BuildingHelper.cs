using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.Library;

namespace Helpers;

public static class BuildingHelper
{
	public static void CheckIfBuildingIsComplete(Building building)
	{
		if ((float)building.GetConstructionCost() <= building.BuildingProgress)
		{
			if (building.CurrentLevel < 3)
			{
				building.LevelUp();
			}
			if (building.CurrentLevel == 3)
			{
				building.BuildingProgress = building.GetConstructionCost();
			}
			building.Town.BuildingsInProgress.Dequeue();
		}
	}

	public static void ChangeDefaultBuilding(Building newDefault, Town town)
	{
		foreach (Building building in town.Buildings)
		{
			if (building.IsCurrentlyDefault)
			{
				building.IsCurrentlyDefault = false;
			}
			if (building == newDefault)
			{
				building.IsCurrentlyDefault = true;
			}
		}
	}

	public static void ChangeCurrentBuildingQueue(List<Building> buildings, Town town)
	{
		town.BuildingsInProgress.Clear();
		foreach (Building building in buildings)
		{
			if (!building.BuildingType.IsDailyProject)
			{
				town.BuildingsInProgress.Enqueue(building);
			}
			else
			{
				Debug.FailedAssert("DefaultProject in building queue", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Helpers.cs", "ChangeCurrentBuildingQueue", 7222);
			}
		}
	}

	public static float GetProgressOfBuilding(Building building, Town town)
	{
		foreach (Building building2 in town.Buildings)
		{
			if (building2 == building)
			{
				return building.BuildingProgress / (float)building.GetConstructionCost();
			}
		}
		Debug.FailedAssert(string.Concat(building.Name, "is not a project of", town.Name), "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Helpers.cs", "GetProgressOfBuilding", 7237);
		return 0f;
	}

	public static int GetDaysToComplete(Building building, Town town)
	{
		BuildingConstructionModel buildingConstructionModel = Campaign.Current.Models.BuildingConstructionModel;
		foreach (Building building2 in town.Buildings)
		{
			if (building2 != building)
			{
				continue;
			}
			float num = (float)building.GetConstructionCost() - building.BuildingProgress;
			int num2 = (int)town.Construction;
			if (num2 != 0)
			{
				int num3 = (int)(num / (float)num2);
				int num4 = (town.IsCastle ? buildingConstructionModel.CastleBoostCost : buildingConstructionModel.TownBoostCost);
				if (town.BoostBuildingProcess >= num4)
				{
					int num5 = town.BoostBuildingProcess / num4;
					if (num3 > num5)
					{
						int num6 = num5 * num2;
						int num7 = Campaign.Current.Models.BuildingConstructionModel.CalculateDailyConstructionPowerWithoutBoost(town);
						return num5 + MathF.Max((int)((num - (float)num6) / (float)num7), 1);
					}
				}
				return MathF.Max(num3, 1);
			}
			return -1;
		}
		Debug.FailedAssert(string.Concat(building.Name, "is not a project of", town.Name), "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Helpers.cs", "GetDaysToComplete", 7279);
		return 0;
	}

	public static int GetTierOfBuilding(BuildingType buildingType, Town town)
	{
		foreach (Building building in town.Buildings)
		{
			if (building.BuildingType == buildingType)
			{
				return building.CurrentLevel;
			}
		}
		Debug.FailedAssert(string.Concat(buildingType.Name, "is not a project of", town.Name), "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Helpers.cs", "GetTierOfBuilding", 7293);
		return 0;
	}

	public static void BoostBuildingProcessWithGold(int gold, Town town)
	{
		if (gold < town.BoostBuildingProcess)
		{
			GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, town.BoostBuildingProcess - gold);
		}
		else if (gold > town.BoostBuildingProcess)
		{
			GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, gold - town.BoostBuildingProcess);
		}
		town.BoostBuildingProcess = gold;
	}
}
