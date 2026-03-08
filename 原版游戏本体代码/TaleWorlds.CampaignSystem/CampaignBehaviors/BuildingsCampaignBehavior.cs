using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x020003D3 RID: 979
	public class BuildingsCampaignBehavior : CampaignBehaviorBase
	{
		// Token: 0x06003A4F RID: 14927 RVA: 0x000F0EFC File Offset: 0x000EF0FC
		public override void RegisterEvents()
		{
			CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, new Action<Settlement, bool, Hero, Hero, Hero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail>(this.OnSettlementOwnerChanged));
			CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnNewGameCreated));
			CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, new Action<Settlement>(this.DailyTickSettlement));
			CampaignEvents.OnBuildingLevelChangedEvent.AddNonSerializedListener(this, new Action<Town, Building, int>(this.OnBuildingLevelChanged));
		}

		// Token: 0x06003A50 RID: 14928 RVA: 0x000F0F65 File Offset: 0x000EF165
		private void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
		{
			if (settlement.Town != null && newOwner.Clan != Clan.PlayerClan)
			{
				settlement.Town.BuildingsInProgress.Clear();
			}
		}

		// Token: 0x06003A51 RID: 14929 RVA: 0x000F0F8C File Offset: 0x000EF18C
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x06003A52 RID: 14930 RVA: 0x000F0F8E File Offset: 0x000EF18E
		private void OnNewGameCreated(CampaignGameStarter starter)
		{
			BuildingsCampaignBehavior.BuildDevelopmentsAtGameStart();
		}

		// Token: 0x06003A53 RID: 14931 RVA: 0x000F0F98 File Offset: 0x000EF198
		private static void DecideDailyProject(Town town)
		{
			Building nextDailyBuilding = Campaign.Current.Models.BuildingScoreCalculationModel.GetNextDailyBuilding(town);
			if (nextDailyBuilding != null && nextDailyBuilding != town.CurrentDefaultBuilding)
			{
				BuildingHelper.ChangeDefaultBuilding(nextDailyBuilding, town);
			}
		}

		// Token: 0x06003A54 RID: 14932 RVA: 0x000F0FD0 File Offset: 0x000EF1D0
		private static void DecideBuildingQueue(Town town)
		{
			if (town.BuildingsInProgress.IsEmpty<Building>())
			{
				Building nextBuilding = Campaign.Current.Models.BuildingScoreCalculationModel.GetNextBuilding(town);
				if (nextBuilding != null)
				{
					town.BuildingsInProgress.Enqueue(nextBuilding);
				}
			}
		}

		// Token: 0x06003A55 RID: 14933 RVA: 0x000F1010 File Offset: 0x000EF210
		private void DailyTickSettlement(Settlement settlement)
		{
			if (settlement.IsFortification)
			{
				Town town = settlement.Town;
				foreach (Building building in town.Buildings)
				{
					if (town.Owner.Settlement.SiegeEvent == null)
					{
						building.HitPointChanged(10f);
					}
				}
				if (town.Owner.Settlement.OwnerClan != Clan.PlayerClan)
				{
					if (MBRandom.RandomFloat < 0.1f)
					{
						BuildingsCampaignBehavior.DecideBuildingQueue(town);
					}
					if (MBRandom.RandomFloat < 0.01f)
					{
						BuildingsCampaignBehavior.DecideDailyProject(town);
					}
				}
				if (!town.CurrentBuilding.BuildingType.IsDailyProject)
				{
					this.TickCurrentBuildingForTown(town);
					return;
				}
				if (town.Governor != null && town.Governor.GetPerkValue(DefaultPerks.Charm.Virile) && MBRandom.RandomFloat <= DefaultPerks.Charm.Virile.SecondaryBonus)
				{
					Hero randomElement = settlement.Notables.GetRandomElement<Hero>();
					if (randomElement != null)
					{
						ChangeRelationAction.ApplyRelationChangeBetweenHeroes(town.Governor.Clan.Leader, randomElement, 1, false);
					}
				}
			}
		}

		// Token: 0x06003A56 RID: 14934 RVA: 0x000F1134 File Offset: 0x000EF334
		private void TickCurrentBuildingForTown(Town town)
		{
			if (town.BuildingsInProgress.Peek().CurrentLevel == 3)
			{
				town.BuildingsInProgress.Dequeue();
			}
			if (!town.Owner.Settlement.IsUnderSiege && !town.BuildingsInProgress.IsEmpty<Building>())
			{
				BuildingConstructionModel buildingConstructionModel = Campaign.Current.Models.BuildingConstructionModel;
				Building building = town.BuildingsInProgress.Peek();
				building.BuildingProgress += town.Construction;
				int num = (town.IsCastle ? buildingConstructionModel.CastleBoostCost : buildingConstructionModel.TownBoostCost);
				if (town.BoostBuildingProcess > 0)
				{
					town.BoostBuildingProcess -= num;
					if (town.BoostBuildingProcess < 0)
					{
						town.BoostBuildingProcess = 0;
					}
				}
				BuildingHelper.CheckIfBuildingIsComplete(building);
			}
		}

		// Token: 0x06003A57 RID: 14935 RVA: 0x000F11F0 File Offset: 0x000EF3F0
		private void OnBuildingLevelChanged(Town town, Building building, int levelChange)
		{
			if (building.BuildingType.HasEffect(BuildingEffectEnum.PrisonCapacity))
			{
				building.Town.Settlement.Party.PrisonRoster.UpdateVersion();
			}
			if (levelChange > 0)
			{
				if (town.Governor != null)
				{
					if ((town.IsTown || town.IsCastle) && town.Governor.GetPerkValue(DefaultPerks.Charm.MoralLeader))
					{
						foreach (Hero gainedRelationWith in town.Settlement.Notables)
						{
							ChangeRelationAction.ApplyRelationChangeBetweenHeroes(town.Settlement.OwnerClan.Leader, gainedRelationWith, MathF.Round(DefaultPerks.Charm.MoralLeader.SecondaryBonus), true);
						}
					}
					if (town.Governor.GetPerkValue(DefaultPerks.Engineering.Foreman))
					{
						town.Prosperity += DefaultPerks.Engineering.Foreman.SecondaryBonus;
					}
				}
				SkillLevelingManager.OnSettlementProjectFinished(town.Settlement);
			}
		}

		// Token: 0x06003A58 RID: 14936 RVA: 0x000F12F8 File Offset: 0x000EF4F8
		private static void BuildDevelopmentsAtGameStart()
		{
			foreach (Settlement settlement in Settlement.All)
			{
				if (settlement.IsFortification)
				{
					Town town = settlement.Town;
					using (List<BuildingType>.Enumerator enumerator2 = BuildingType.All.GetEnumerator())
					{
						while (enumerator2.MoveNext())
						{
							BuildingType buildingType = enumerator2.Current;
							if (town.Buildings.All((Building b) => b.BuildingType != buildingType) && Campaign.Current.Models.BuildingModel.CanAddBuildingTypeToTown(buildingType, town))
							{
								town.Buildings.Add(new Building(buildingType, town, 0f, buildingType.StartLevel));
							}
						}
					}
					foreach (Building building in town.Buildings)
					{
						BuildingType buildingType2 = building.BuildingType;
						if (building.CurrentLevel < 3 && settlement.RandomFloat(1f) < buildingType2.VarianceChance)
						{
							Debug.Print(string.Concat(new object[] { "Building variance roll success! SettlementId: ", settlement.StringId, " BuildingId: ", buildingType2.StringId, "Level: ", building.CurrentLevel }), 0, Debug.DebugColor.White, 17592186044416UL);
							building.LevelUp();
							Debug.Print("Building level increased to " + building.CurrentLevel + ".", 0, Debug.DebugColor.White, 17592186044416UL);
						}
					}
					BuildingsCampaignBehavior.DecideDailyProject(town);
					BuildingsCampaignBehavior.DecideBuildingQueue(town);
				}
			}
		}
	}
}
