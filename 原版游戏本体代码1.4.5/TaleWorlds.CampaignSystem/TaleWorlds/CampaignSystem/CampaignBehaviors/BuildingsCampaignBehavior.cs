using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class BuildingsCampaignBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnSettlementOwnerChanged);
		CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
		CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, DailyTickSettlement);
		CampaignEvents.OnBuildingLevelChangedEvent.AddNonSerializedListener(this, OnBuildingLevelChanged);
	}

	private void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
	{
		if (settlement.Town != null && newOwner.Clan != Clan.PlayerClan)
		{
			settlement.Town.BuildingsInProgress.Clear();
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void OnNewGameCreated(CampaignGameStarter starter)
	{
		BuildDevelopmentsAtGameStart();
	}

	private static void DecideDailyProject(Town town)
	{
		Building nextDailyBuilding = Campaign.Current.Models.BuildingScoreCalculationModel.GetNextDailyBuilding(town);
		if (nextDailyBuilding != null && nextDailyBuilding != town.CurrentDefaultBuilding)
		{
			BuildingHelper.ChangeDefaultBuilding(nextDailyBuilding, town);
		}
	}

	private static void DecideBuildingQueue(Town town)
	{
		if (town.BuildingsInProgress.IsEmpty())
		{
			Building nextBuilding = Campaign.Current.Models.BuildingScoreCalculationModel.GetNextBuilding(town);
			if (nextBuilding != null)
			{
				town.BuildingsInProgress.Enqueue(nextBuilding);
			}
		}
	}

	private void DailyTickSettlement(Settlement settlement)
	{
		if (!settlement.IsFortification)
		{
			return;
		}
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
				DecideBuildingQueue(town);
			}
			if (MBRandom.RandomFloat < 0.01f)
			{
				DecideDailyProject(town);
			}
		}
		if (!town.CurrentBuilding.BuildingType.IsDailyProject)
		{
			TickCurrentBuildingForTown(town);
		}
		else if (town.Governor != null && town.Governor.GetPerkValue(DefaultPerks.Charm.Virile) && MBRandom.RandomFloat <= DefaultPerks.Charm.Virile.SecondaryBonus)
		{
			Hero randomElement = settlement.Notables.GetRandomElement();
			if (randomElement != null)
			{
				ChangeRelationAction.ApplyRelationChangeBetweenHeroes(town.Governor.Clan.Leader, randomElement, 1, showQuickNotification: false);
			}
		}
	}

	private void TickCurrentBuildingForTown(Town town)
	{
		if (town.BuildingsInProgress.Peek().CurrentLevel == 3)
		{
			town.BuildingsInProgress.Dequeue();
		}
		if (town.Owner.Settlement.IsUnderSiege || town.BuildingsInProgress.IsEmpty())
		{
			return;
		}
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

	private void OnBuildingLevelChanged(Town town, Building building, int levelChange)
	{
		if (building.BuildingType.HasEffect(BuildingEffectEnum.GarrisonCapacity))
		{
			building.Town.Settlement.Town.GarrisonParty?.Party.MemberRoster.UpdateVersion();
		}
		if (building.BuildingType.HasEffect(BuildingEffectEnum.PrisonCapacity))
		{
			building.Town.Settlement.Party.PrisonRoster.UpdateVersion();
		}
		if (levelChange <= 0)
		{
			return;
		}
		if (town.Governor != null)
		{
			if ((town.IsTown || town.IsCastle) && town.Governor.GetPerkValue(DefaultPerks.Charm.MoralLeader))
			{
				foreach (Hero notable in town.Settlement.Notables)
				{
					ChangeRelationAction.ApplyRelationChangeBetweenHeroes(town.Settlement.OwnerClan.Leader, notable, MathF.Round(DefaultPerks.Charm.MoralLeader.SecondaryBonus));
				}
			}
			if (town.Governor.GetPerkValue(DefaultPerks.Engineering.Foreman))
			{
				town.Prosperity += DefaultPerks.Engineering.Foreman.SecondaryBonus;
			}
		}
		SkillLevelingManager.OnSettlementProjectFinished(town.Settlement);
	}

	private static void BuildDevelopmentsAtGameStart()
	{
		foreach (Settlement item in Settlement.All)
		{
			if (!item.IsFortification)
			{
				continue;
			}
			Town town = item.Town;
			foreach (BuildingType buildingType in BuildingType.All)
			{
				if (town.Buildings.All((Building b) => b.BuildingType != buildingType) && Campaign.Current.Models.BuildingModel.CanAddBuildingTypeToTown(buildingType, town))
				{
					town.Buildings.Add(new Building(buildingType, town, 0f, buildingType.StartLevel));
				}
			}
			foreach (Building building in town.Buildings)
			{
				BuildingType buildingType2 = building.BuildingType;
				if (building.CurrentLevel < 3 && item.RandomFloat(1f) < buildingType2.VarianceChance)
				{
					Debug.Print("Building variance roll success! SettlementId: " + item.StringId + " BuildingId: " + buildingType2.StringId + "Level: " + building.CurrentLevel);
					building.LevelUp();
					Debug.Print("Building level increased to " + building.CurrentLevel + ".");
				}
			}
			DecideDailyProject(town);
			DecideBuildingQueue(town);
		}
	}
}
