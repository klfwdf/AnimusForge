using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace NavalDLC.CampaignBehaviors;

public class NavalAdvancedStartingPlayerOptionsCampaignBehavior : CampaignBehaviorBase
{
	private const uint FleetAdmiralSeed = 2311779433u;

	private const uint MerchantVenturerSeed = 4042565843u;

	private const uint PersonalShipSeed = 2125681889u;

	private const int FleetAdmiralMinimumClanTier = 2;

	private const int FleetAdmiralStartingCompanionCount = 2;

	private const int FleetAdmiralCastleCount = 1;

	private const int FleetAdmiralStartingGoldMin = 25000;

	private const int FleetAdmiralStartingGoldMax = 75000;

	private const int FleetAdmiralStartingFoodMin = 75;

	private const int FleetAdmiralStartingFoodMax = 100;

	private const int FleetAdmiralMediumShipCount = 2;

	private const int FleetAdmiralLightShipCount = 1;

	private static readonly Dictionary<int, (int min, int max)> FleetAdmiralMarinerTroopCounts = new Dictionary<int, (int, int)>
	{
		{
			3,
			(15, 19)
		},
		{
			4,
			(5, 10)
		},
		{
			5,
			(5, 10)
		}
	};

	private static readonly Dictionary<int, (int min, int max)> FleetAdmiralNormalTroopCounts = new Dictionary<int, (int, int)> { 
	{
		1,
		(15, 29)
	} };

	private const int MerchantVenturerClanTier = 1;

	private const int MerchantVenturerStartingCompanionCount = 1;

	private const int MerchantVenturerStartingGoldMin = 5000;

	private const int MerchantVenturerStartingGoldMax = 10000;

	private const int MerchantVenturerStartingFoodMin = 30;

	private const int MerchantVenturerStartingFoodMax = 50;

	private const int MerchantVenturerTradeGoodsGoldMin = 1500;

	private const int MerchantVenturerTradeGoodsGoldMax = 5000;

	private const int MerchantVenturerTradeGoodTypeCount = 1;

	private const int MerchantVenturerMaximumTroopCount = 30;

	private const string MerchantVenturerShipId = "eastern_trade_ship";

	private static readonly Dictionary<int, (int min, int max)> MerchantVenturerTroopCounts = new Dictionary<int, (int, int)>
	{
		{
			0,
			(12, 15)
		},
		{
			1,
			(5, 9)
		},
		{
			2,
			(3, 4)
		}
	};

	private static readonly string[] PersonalShipHullIds = new string[2] { "eastern_trade_ship", "northern_trade_ship" };

	public override void RegisterEvents()
	{
		CampaignEvents.OnCharacterCreationIsOverEvent.AddNonSerializedListener(this, OnCharacterCreationIsOver);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void OnCharacterCreationIsOver(int index)
	{
		if (index != 8)
		{
			return;
		}
		string startType = Campaign.Current.AdvancedStartData.GetStartType();
		if (!(startType == "fleetadmiral"))
		{
			if (startType == "merchantventurer")
			{
				StartGameAsMerchantVenturer();
			}
		}
		else
		{
			StartGameAsFleetAdmiral(Campaign.Current.AdvancedStartData.GetKingdomId());
		}
		if (Campaign.Current.AdvancedStartData.IsPersonalShipEnabled())
		{
			GivePersonalShip();
		}
		MobileParty.MainParty.MemberRoster.UpdateVersion();
	}

	private void StartGameAsFleetAdmiral(string kingdomId)
	{
		MobileParty.MainParty.ItemRoster.Clear();
		MBFastRandom mBFastRandom = new MBFastRandom(Campaign.Current.Options.Seed ^ 0x89CAF469u);
		CampaignAdvancedStartingPlayerOptionsCampaignBehavior.EnsureMinimumClanTier(2);
		Kingdom kingdom = CampaignAdvancedStartingPlayerOptionsCampaignBehavior.ResolveKingdom(kingdomId);
		ChangeKingdomAction.ApplyByJoinToKingdom(Clan.PlayerClan, kingdom, default(CampaignTime), showNotification: false);
		int gold = mBFastRandom.Next(25000, 75001);
		Hero.MainHero.Gold = gold;
		CampaignAdvancedStartingPlayerOptionsCampaignBehavior.AdjustStartingFood(mBFastRandom, 75, 100, kingdom.Culture);
		Equipment suitableEquipmentSet = CampaignAdvancedStartingPlayerOptionsCampaignBehavior.GetSuitableEquipmentSet(Hero.MainHero, kingdom.Culture, EquipmentCategories.IsLordTemplate, Equipment.EquipmentType.Battle, mBFastRandom);
		Equipment suitableEquipmentSet2 = CampaignAdvancedStartingPlayerOptionsCampaignBehavior.GetSuitableEquipmentSet(Hero.MainHero, kingdom.Culture, EquipmentCategories.IsLordTemplate, Equipment.EquipmentType.Civilian, mBFastRandom);
		CampaignAdvancedStartingPlayerOptionsCampaignBehavior.AssignMainHeroEquipmentKeepingHorse(suitableEquipmentSet);
		EquipmentHelper.AssignHeroEquipmentFromEquipment(Hero.MainHero, suitableEquipmentSet2);
		GiveTroopsFromTree(new List<CharacterObject> { kingdom.BasicTroop }, FleetAdmiralNormalTroopCounts, giveMarinerTroops: false, mBFastRandom);
		GiveTroopsFromTree(new List<CharacterObject> { kingdom.BasicTroop }, FleetAdmiralMarinerTroopCounts, giveMarinerTroops: true, mBFastRandom);
		CampaignAdvancedStartingPlayerOptionsCampaignBehavior.GiveStartingCompanions(2, mBFastRandom);
		Settlement settlement = null;
		if (Campaign.Current.AdvancedStartData.GetScenario() != "LastStand")
		{
			settlement = CampaignAdvancedStartingPlayerOptionsCampaignBehavior.GiveStartingFiefs(1, mBFastRandom, (Settlement settlement3) => settlement3.IsCastle && settlement3.OwnerClan.MapFaction == kingdom && settlement3.HasPort);
		}
		if (settlement == null)
		{
			settlement = CampaignAdvancedStartingPlayerOptionsCampaignBehavior.GiveStartingFiefs(1, mBFastRandom, (Settlement settlement3) => settlement3.IsCastle && settlement3.OwnerClan.MapFaction == kingdom);
		}
		if (settlement == null)
		{
			settlement = CampaignAdvancedStartingPlayerOptionsCampaignBehavior.FindFallbackStartingTown(mBFastRandom);
		}
		CampaignAdvancedStartingPlayerOptionsCampaignBehavior.UpdateMainHeroHomeSettlement(settlement);
		GiveRandomShipsByType(2, ShipHull.ShipType.Medium, kingdom.Culture, mBFastRandom);
		GiveRandomShipsByType(1, ShipHull.ShipType.Light, kingdom.Culture, mBFastRandom);
		if (settlement.HasPort)
		{
			MobileParty.MainParty.SetSailAtPosition(settlement.PortPosition);
			return;
		}
		Settlement settlement2 = SettlementHelper.FindNearestSettlementToPoint(settlement.GatePosition, (Settlement s) => s.HasPort && s.OwnerClan.MapFaction == kingdom);
		if (settlement2 != null)
		{
			MobileParty.MainParty.SetSailAtPosition(settlement2.PortPosition);
		}
		else
		{
			MobileParty.MainParty.Position = settlement.GatePosition;
		}
	}

	private void StartGameAsMerchantVenturer()
	{
		MobileParty.MainParty.ItemRoster.Clear();
		CampaignAdvancedStartingPlayerOptionsCampaignBehavior.EnsureMinimumClanTier(1);
		MBFastRandom mBFastRandom = new MBFastRandom(Campaign.Current.Options.Seed ^ 0xF0F4A8D3u);
		List<Town> list = Town.AllTowns.Where((Town s) => s.Settlement.HasPort).ToList();
		Town town = list[mBFastRandom.Next(0, list.Count)];
		Kingdom kingdom = town.Settlement.OwnerClan.Kingdom;
		Hero.MainHero.BornSettlement = town.Settlement;
		Hero.MainHero.UpdateHomeSettlement();
		int gold = mBFastRandom.Next(5000, 10001);
		Hero.MainHero.Gold = gold;
		CampaignAdvancedStartingPlayerOptionsCampaignBehavior.AdjustStartingFood(mBFastRandom, 30, 50, kingdom.Culture);
		List<Workshop> list2 = town.Workshops.Where((Workshop w) => !w.WorkshopType.IsHidden).ToList();
		ChangeOwnerOfWorkshopAction.ApplyByFree(list2[mBFastRandom.Next(0, list2.Count)], Hero.MainHero);
		CampaignAdvancedStartingPlayerOptionsCampaignBehavior.GiveStartingCompanions(1, mBFastRandom);
		Hero caravanLeader = Clan.PlayerClan.Companions[mBFastRandom.Next(0, Clan.PlayerClan.Companions.Count)];
		PartyTemplateObject randomCaravanTemplate = CaravanHelper.GetRandomCaravanTemplate(Hero.MainHero.Culture, isElite: false, isLand: false);
		CaravanPartyComponent.CreateCaravanParty(Hero.MainHero, town.Settlement, randomCaravanTemplate, isInitialSpawn: true, caravanLeader);
		List<CharacterObject> list3 = kingdom.Culture.NotableTemplates.Where((CharacterObject t) => t.IsFemale == Hero.MainHero.IsFemale).ToList();
		CharacterObject characterObject = list3[mBFastRandom.Next(0, list3.Count)];
		List<Equipment> list4 = characterObject.BattleEquipments.ToList();
		List<Equipment> list5 = characterObject.CivilianEquipments.ToList();
		EquipmentHelper.AssignHeroEquipmentFromEquipment(Hero.MainHero, list4[mBFastRandom.Next(0, list4.Count)].Clone());
		EquipmentHelper.AssignHeroEquipmentFromEquipment(Hero.MainHero, list5[mBFastRandom.Next(0, list5.Count)].Clone());
		GiveTroopsToMainPartyFromCaravanTemplate(kingdom.Culture, mBFastRandom);
		ShipHull shipHull = MBObjectManager.Instance.GetObject<ShipHull>("eastern_trade_ship");
		ChangeShipOwnerAction.ApplyByProduction(ship: new Ship(shipHull), newOwner: PartyBase.MainParty);
		Ship ship = new Ship(shipHull);
		ChangeShipOwnerAction.ApplyByProduction(PartyBase.MainParty, ship);
		PartyBase.MainParty?.MobileParty?.SetNavalVisualAsDirty();
		MobileParty.MainParty.SetSailAtPosition(town.Settlement.PortPosition);
		CampaignAdvancedStartingPlayerOptionsCampaignBehavior.GiveTradeGoods(kingdom.Culture, 1500, 5000, 1, mBFastRandom);
	}

	private void GivePersonalShip()
	{
		MBFastRandom mBFastRandom = new MBFastRandom(Campaign.Current.Options.Seed ^ 0x7EB354E1);
		string objectName = PersonalShipHullIds[mBFastRandom.Next(0, PersonalShipHullIds.Length)];
		Ship ship = new Ship(MBObjectManager.Instance.GetObject<ShipHull>(objectName));
		ChangeShipOwnerAction.ApplyByProduction(PartyBase.MainParty, ship);
		MobileParty.MainParty.SetNavalVisualAsDirty();
	}

	private void GiveRandomShipsByType(int count, ShipHull.ShipType shipType, CultureObject culture, MBFastRandom random)
	{
		List<ShipHull> list = new List<ShipHull>();
		foreach (ShipHull availableShipHull in culture.AvailableShipHulls)
		{
			if (availableShipHull.Type == shipType)
			{
				list.Add(availableShipHull);
			}
		}
		if (list.Count == 0)
		{
			list.AddRange(culture.AvailableShipHulls);
		}
		for (int i = 0; i < count; i++)
		{
			Ship ship = new Ship(list[random.Next(0, list.Count)]);
			ChangeShipOwnerAction.ApplyByProduction(PartyBase.MainParty, ship);
			MobileParty.MainParty.SetNavalVisualAsDirty();
		}
	}

	private void GiveTroopsFromTree(List<CharacterObject> baseTroops, Dictionary<int, (int, int)> tierList, bool giveMarinerTroops, MBFastRandom random)
	{
		foreach (KeyValuePair<int, (int, int)> tier in tierList)
		{
			List<CharacterObject> list = CollectTroops(baseTroops, tier.Key, giveMarinerTroops);
			if (list.Count == 0)
			{
				list = CollectTroops(baseTroops, tier.Key + 1, giveMarinerTroops);
			}
			if (list.Count == 0 && tier.Key > 1)
			{
				list = CollectTroops(baseTroops, tier.Key - 1, giveMarinerTroops);
			}
			if (list.Count != 0)
			{
				int num = random.Next(tier.Value.Item1, tier.Value.Item2 + 1);
				for (int i = 0; i < num; i++)
				{
					CharacterObject character = list[random.Next(0, list.Count)];
					MobileParty.MainParty.MemberRoster.AddToCounts(character, 1);
				}
			}
		}
	}

	private List<CharacterObject> CollectTroops(List<CharacterObject> baseTroops, int tier, bool giveMarinerTroops)
	{
		List<CharacterObject> list = new List<CharacterObject>();
		List<CharacterObject> list2 = new List<CharacterObject>();
		foreach (CharacterObject baseTroop in baseTroops)
		{
			foreach (CharacterObject item in CharacterHelper.GetTroopTree(baseTroop, tier, tier))
			{
				if (giveMarinerTroops)
				{
					if (item.IsMariner)
					{
						list.Add(item);
					}
					else
					{
						list2.Add(item);
					}
				}
				else if (!item.IsMariner)
				{
					list.Add(item);
				}
				else
				{
					list2.Add(item);
				}
			}
		}
		if (list.Count <= 0)
		{
			return list2;
		}
		return list;
	}

	private static void GiveTroopsToMainPartyFromCaravanTemplate(CultureObject culture, MBFastRandom random)
	{
		List<PartyTemplateObject> list = new List<PartyTemplateObject>();
		foreach (PartyTemplateObject caravanPartyTemplate in culture.CaravanPartyTemplates)
		{
			if (caravanPartyTemplate.ShipHulls.Count > 0)
			{
				list.Add(caravanPartyTemplate);
			}
		}
		PartyTemplateObject partyTemplateObject = list[random.Next(0, list.Count)];
		List<(CharacterObject, int)> list2 = new List<(CharacterObject, int)>();
		int num = 0;
		foreach (KeyValuePair<int, (int, int)> merchantVenturerTroopCount in MerchantVenturerTroopCounts)
		{
			CharacterObject character = partyTemplateObject.Stacks[merchantVenturerTroopCount.Key].Character;
			int num2 = random.Next(merchantVenturerTroopCount.Value.Item1, merchantVenturerTroopCount.Value.Item2 + 1);
			list2.Add((character, num2));
			num += num2;
		}
		int num3 = 30 - MobileParty.MainParty.Party.NumberOfAllMembers;
		if (num > num3 && num > 0 && num3 > 0)
		{
			int num4 = 0;
			for (int i = 0; i < partyTemplateObject.Stacks.Count; i++)
			{
				num4 += partyTemplateObject.Stacks[i].MinValue;
			}
			if (num3 <= num4)
			{
				float num5 = ((num4 > 0) ? ((float)num3 / (float)num4) : 0f);
				for (int j = 0; j < list2.Count; j++)
				{
					list2[j] = (list2[j].Item1, (int)((float)partyTemplateObject.Stacks[j].MinValue * num5));
				}
			}
			else
			{
				int num6 = num3 - num4;
				int num7 = num - num4;
				float num8 = (float)num6 / (float)num7;
				for (int k = 0; k < list2.Count; k++)
				{
					int num9 = list2[k].Item2 - partyTemplateObject.Stacks[k].MinValue;
					list2[k] = (list2[k].Item1, partyTemplateObject.Stacks[k].MinValue + (int)((float)num9 * num8));
				}
			}
		}
		foreach (var (character2, num10) in list2)
		{
			if (num10 > 0)
			{
				MobileParty.MainParty.MemberRoster.AddToCounts(character2, num10);
			}
		}
	}
}
