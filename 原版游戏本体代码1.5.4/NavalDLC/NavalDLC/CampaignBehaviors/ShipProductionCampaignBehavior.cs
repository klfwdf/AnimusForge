using System.Collections.Generic;
using System.Linq;
using Helpers;
using NavalDLC.CharacterDevelopment;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace NavalDLC.CampaignBehaviors;

public class ShipProductionCampaignBehavior : CampaignBehaviorBase
{
	private const float ShipGenerationChance = 0.5f;

	private const float ShipGenerationUpgradePieceAddingChance = 0.5f;

	private const int ShipGenerationDailyCount = 10;

	public static bool DebugShipyards;

	public override void RegisterEvents()
	{
		CampaignEvents.OnNewGameCreatedPartialFollowUpEvent.AddNonSerializedListener(this, OnNewGameCreatedPartialFollowUp);
		CampaignEvents.DailyTickTownEvent.AddNonSerializedListener(this, DailyTickTown);
		CampaignEvents.OnShipCreatedEvent.AddNonSerializedListener(this, OnShipCreated);
		CampaignEvents.OnShipDestroyedEvent.AddNonSerializedListener(this, OnShipDestroyed);
		CampaignEvents.OnShipOwnerChangedEvent.AddNonSerializedListener(this, OnShipOwnerChanged);
		CampaignEvents.TickEvent.AddNonSerializedListener(this, Tick);
	}

	private void Tick(float obj)
	{
		if (!DebugShipyards)
		{
			return;
		}
		foreach (Town allTown in Town.AllTowns)
		{
			if (!allTown.Settlement.HasPort)
			{
				continue;
			}
			Vec3 vec = allTown.Settlement.Position.AsVec3() + Vec3.Up * 3.75f;
			vec.x -= 1f;
			Building shipyard = allTown.GetShipyard();
			string text = $"Ship Count: {allTown.AvailableShips.Count}\nShipyard level: {shipyard.CurrentLevel}";
			foreach (Ship availableShip in allTown.AvailableShips)
			{
				text = text + "\n" + availableShip.ShipHull.Name.ToString();
				vec = new Vec3(vec.x, vec.y + 0.25f, vec.z);
			}
		}
		MBList<MobileParty> mBList = MobileParty.AllLordParties.OrderByDescending((MobileParty x) => x.Ships.Count).Take(5).ToMBList();
		int num = 140;
		foreach (MobileParty item in mBList)
		{
			_ = item;
			num += 20;
		}
	}

	private void DailyTickTown(Town town)
	{
		if (!town.IsTown || town.IsUnderSiege || town.Settlement.Party.MapEvent != null || !town.Settlement.HasPort)
		{
			return;
		}
		ExplainedNumber bonuses = new ExplainedNumber(0.5f);
		PerkHelper.AddPerkBonusForTown(NavalPerks.Boatswain.StreamlinedOperations, town, isPrimaryBonus: false, ref bonuses);
		int maxShipCountForTown = GetMaxShipCountForTown(town);
		if (town.AvailableShips.Count < maxShipCountForTown && MBRandom.RandomFloat < bonuses.ResultNumber)
		{
			for (int i = 0; i < 10; i++)
			{
				if (town.AvailableShips.Count >= maxShipCountForTown)
				{
					break;
				}
				CreateShip(town);
			}
		}
		int idealShipCountForTown = GetIdealShipCountForTown(town);
		if (town.AvailableShips.Count >= idealShipCountForTown)
		{
			TryRemoveExcessShipsFromTown(town);
		}
	}

	private static void TryRemoveExcessShipsFromTown(Town town)
	{
		int idealShipCountForTown = GetIdealShipCountForTown(town);
		int num = town.AvailableShips.Count - idealShipCountForTown;
		if (num <= 0)
		{
			return;
		}
		List<Ship> shipsOfOtherCulture = town.AvailableShips.Where((Ship x) => !town.Culture.AvailableShipHulls.Contains(x.ShipHull)).ToList();
		foreach (Ship item in shipsOfOtherCulture)
		{
			if (MBRandom.RandomFloat < 0.7f)
			{
				DestroyShipAction.Apply(item);
				num--;
				if (num < 0)
				{
					break;
				}
			}
		}
		if (num <= 0)
		{
			return;
		}
		foreach (Ship item2 in town.AvailableShips.Where((Ship x) => !shipsOfOtherCulture.Contains(x)).ToList())
		{
			if (MBRandom.RandomFloat < 0.3f)
			{
				DestroyShipAction.Apply(item2);
				num--;
				if (num < 0)
				{
					break;
				}
			}
		}
	}

	private void CreateShip(Town town)
	{
		ShipHull randomShipHull = GetRandomShipHull(town);
		if (randomShipHull == null)
		{
			return;
		}
		Ship ship = new Ship(randomShipHull);
		List<ShipUpgradePiece> availableShipUpgradePieces = town.GetAvailableShipUpgradePieces();
		availableShipUpgradePieces.Shuffle();
		foreach (KeyValuePair<string, ShipSlot> availableSlot in ship.ShipHull.AvailableSlots)
		{
			if (!(MBRandom.RandomFloat > 0.5f))
			{
				continue;
			}
			int num = MBRandom.RandomInt(availableShipUpgradePieces.Count);
			for (int i = 0; i < availableShipUpgradePieces.Count; i++)
			{
				ShipUpgradePiece shipUpgradePiece = availableShipUpgradePieces[(i + num) % availableShipUpgradePieces.Count];
				if (shipUpgradePiece.DoesPieceMatchSlot(availableSlot.Value))
				{
					ship.EquipUpgradePiece(availableSlot.Key, shipUpgradePiece);
					break;
				}
			}
		}
		ChangeShipOwnerAction.ApplyByProduction(town.Settlement.Party, ship);
		CampaignEventDispatcher.Instance.OnShipCreated(ship, town.Settlement);
	}

	private void OnShipOwnerChanged(Ship ship, PartyBase oldOwner, ChangeShipOwnerAction.ShipOwnerChangeDetail changeDetail)
	{
		PartyBase owner = ship.Owner;
		if (owner != null && owner.IsSettlement)
		{
			RepairShipAction.ApplyForFree(ship);
		}
	}

	private void OnNewGameCreatedPartialFollowUp(CampaignGameStarter starter, int index)
	{
		foreach (Town allTown in Town.AllTowns)
		{
			DailyTickTown(allTown);
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void OnShipCreated(Ship ship, Settlement settlement)
	{
		if (settlement.IsFortification && settlement.Town.Governor != null && settlement.Town.Governor.GetPerkValue(NavalPerks.Boatswain.MerchantFleet))
		{
			float secondaryBonus = NavalPerks.Boatswain.MerchantFleet.SecondaryBonus;
			if (secondaryBonus > 0f)
			{
				GainKingdomInfluenceAction.ApplyForDefault(settlement.Owner, secondaryBonus);
			}
		}
	}

	private void OnShipDestroyed(PartyBase party, Ship ship, DestroyShipAction.ShipDestroyDetail detail)
	{
		Hero perkOwnerHero = null;
		if (detail == DestroyShipAction.ShipDestroyDetail.ApplyByDiscard && party.IsMobile && party.MobileParty.HasPerk(NavalPerks.Boatswain.Salvage, out perkOwnerHero))
		{
			float num = ship.HitPoints * 0.01f;
			if (num > 0f)
			{
				GainKingdomInfluenceAction.ApplyForDefault(party.LeaderHero, num);
			}
		}
	}

	private ShipHull GetRandomShipHull(Town town)
	{
		MBList<(ShipHull, float)> availableShipHullsForTown = GetAvailableShipHullsForTown(town);
		if (availableShipHullsForTown.Count == 0)
		{
			Debug.FailedAssert("Could not find ships to create.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC\\CampaignBehaviors\\ShipProductionCampaignBehavior.cs", "GetRandomShipHull", 242);
		}
		return MBRandom.ChooseWeighted(availableShipHullsForTown);
	}

	private MBList<(ShipHull, float)> GetAvailableShipHullsForTown(Town town)
	{
		MBList<(ShipHull, float)> mBList = new MBList<(ShipHull, float)>();
		foreach (ShipHull availableShipHull in town.Culture.AvailableShipHulls)
		{
			if (CanTownCreateShipFromHull(town, availableShipHull))
			{
				mBList.Add((availableShipHull, availableShipHull.ProductionBuildWeight));
			}
		}
		return mBList;
	}

	private bool CanTownCreateShipFromHull(Town town, ShipHull shipHull)
	{
		return shipHull.Type switch
		{
			ShipHull.ShipType.Light => town.GetShipyard().CurrentLevel > 0, 
			ShipHull.ShipType.Medium => town.GetShipyard().CurrentLevel > 1, 
			ShipHull.ShipType.Heavy => town.GetShipyard().CurrentLevel == 3, 
			_ => false, 
		};
	}

	private static int GetMaxShipCountForTown(Town town)
	{
		ExplainedNumber result = default(ExplainedNumber);
		town.AddEffectOfBuildings(BuildingEffectEnum.MaximumShipCount, ref result);
		return (int)result.ResultNumber;
	}

	private static int GetIdealShipCountForTown(Town town)
	{
		return MathF.Max(GetMaxShipCountForTown(town) - 2, 0);
	}
}
