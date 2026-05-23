using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.Library;

namespace Helpers
{
	// Token: 0x0200001D RID: 29
	public static class BuildingHelper
	{
		// Token: 0x06000102 RID: 258 RVA: 0x0000D018 File Offset: 0x0000B218
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
					building.BuildingProgress = (float)building.GetConstructionCost();
				}
				building.Town.BuildingsInProgress.Dequeue();
			}
		}

		// Token: 0x06000103 RID: 259 RVA: 0x0000D06C File Offset: 0x0000B26C
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

		// Token: 0x06000104 RID: 260 RVA: 0x0000D0D4 File Offset: 0x0000B2D4
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
					Debug.FailedAssert("DefaultProject in building queue", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Helpers.cs", "ChangeCurrentBuildingQueue", 7129);
				}
			}
		}

		// Token: 0x06000105 RID: 261 RVA: 0x0000D15C File Offset: 0x0000B35C
		public static float GetProgressOfBuilding(Building building, Town town)
		{
			using (List<Building>.Enumerator enumerator = town.Buildings.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (enumerator.Current == building)
					{
						return building.BuildingProgress / (float)building.GetConstructionCost();
					}
				}
			}
			Debug.FailedAssert(building.Name + "is not a project of" + town.Name, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Helpers.cs", "GetProgressOfBuilding", 7144);
			return 0f;
		}

		// Token: 0x06000106 RID: 262 RVA: 0x0000D1EC File Offset: 0x0000B3EC
		public static int GetDaysToComplete(Building building, Town town)
		{
			BuildingConstructionModel buildingConstructionModel = Campaign.Current.Models.BuildingConstructionModel;
			using (List<Building>.Enumerator enumerator = town.Buildings.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (enumerator.Current == building)
					{
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
				}
			}
			Debug.FailedAssert(building.Name + "is not a project of" + town.Name, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Helpers.cs", "GetDaysToComplete", 7186);
			return 0;
		}

		// Token: 0x06000107 RID: 263 RVA: 0x0000D31C File Offset: 0x0000B51C
		public static int GetTierOfBuilding(BuildingType buildingType, Town town)
		{
			foreach (Building building in town.Buildings)
			{
				if (building.BuildingType == buildingType)
				{
					return building.CurrentLevel;
				}
			}
			Debug.FailedAssert(buildingType.Name + "is not a project of" + town.Name, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Helpers.cs", "GetTierOfBuilding", 7200);
			return 0;
		}

		// Token: 0x06000108 RID: 264 RVA: 0x0000D3A8 File Offset: 0x0000B5A8
		public static void BoostBuildingProcessWithGold(int gold, Town town)
		{
			if (gold < town.BoostBuildingProcess)
			{
				GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, town.BoostBuildingProcess - gold, false);
			}
			else if (gold > town.BoostBuildingProcess)
			{
				GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, gold - town.BoostBuildingProcess, false);
			}
			town.BoostBuildingProcess = gold;
		}
	}
}
