using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class CampaignAdvancedStartingPlayerOptionsCampaignBehavior : CampaignBehaviorBase
{
	private enum CompanionType
	{
		Engineering,
		Tactics,
		Leadership,
		Steward,
		Trade,
		Roguery,
		Medicine,
		Smithing,
		Scouting,
		Combat,
		Sailor
	}

	private const int CompanionSkillThreshold = 20;

	private const uint KingSeed = 3324857891u;

	private const uint VassalSeed = 753149491u;

	private const uint MercenarySeed = 1160783149u;

	private const uint TraderSeed = 1374241951u;

	private const uint OutlawSeed = 4060451693u;

	private const uint BeggarSeed = 2508482539u;

	private const int RulerMinimumClanTier = 5;

	private const int RulerStartingSettlementCount = 2;

	private const int RulerLastStandStartingSettlementCount = 1;

	private const int RulerStartingCompanionCount = 4;

	private const int RulerStartingGoldMin = 100000;

	private const int RulerStartingGoldMax = 150000;

	private const int RulerStartingFoodMin = 100;

	private const int RulerStartingFoodMax = 200;

	private const int RulerStartingInfluenceMin = 75;

	private const int RulerStartingInfluenceMax = 150;

	private static readonly Dictionary<int, (int min, int max)> RulerEliteTroopCounts = new Dictionary<int, (int, int)> { 
	{
		4,
		(10, 15)
	} };

	private static readonly Dictionary<int, (int min, int max)> RulerNormalTroopCounts = new Dictionary<int, (int, int)>
	{
		{
			1,
			(40, 51)
		},
		{
			2,
			(25, 35)
		},
		{
			3,
			(30, 45)
		},
		{
			5,
			(10, 15)
		}
	};

	private const int VassalMinimumClanTier = 2;

	private const int VassalStartingCastleCount = 1;

	private const int VassalStartingCompanionCount = 2;

	private const int VassalStartingGoldMin = 25000;

	private const int VassalStartingGoldMax = 75000;

	private const int VassalStartingFoodMin = 75;

	private const int VassalStartingFoodMax = 100;

	private static readonly Dictionary<int, (int min, int max)> VassalEliteTroopCounts = new Dictionary<int, (int, int)> { 
	{
		1,
		(5, 10)
	} };

	private static readonly Dictionary<int, (int min, int max)> VassalNormalTroopCounts = new Dictionary<int, (int, int)>
	{
		{
			1,
			(15, 19)
		},
		{
			2,
			(15, 19)
		},
		{
			3,
			(5, 10)
		},
		{
			4,
			(5, 10)
		}
	};

	private const int MercenaryAwardMultiplier = 50;

	private const int MercenaryMinimumClanTier = 1;

	private const int MercenaryStartingCompanionCount = 1;

	private const int MercenaryStartingGoldMin = 10000;

	private const int MercenaryStartingGoldMax = 15000;

	private const int MercenaryStartingFoodMin = 30;

	private const int MercenaryStartingFoodMax = 50;

	private static readonly Dictionary<int, (int min, int max)> MercenaryTroopCounts = new Dictionary<int, (int, int)>
	{
		{
			1,
			(15, 20)
		},
		{
			2,
			(10, 15)
		},
		{
			3,
			(5, 9)
		}
	};

	private const int TraderClanTier = 1;

	private const int TraderStartingCompanionCount = 2;

	private const int TraderStartingGoldMin = 5000;

	private const int TraderStartingGoldMax = 10000;

	private const string MuleStringId = "mule";

	private const int TraderStartingFoodMin = 30;

	private const int TraderStartingFoodMax = 50;

	private const int TraderTradeGoodsGoldMin = 1500;

	private const int TraderTradeGoodsGoldMax = 5000;

	private const int TraderTradeGoodTypeCount = 1;

	private const int TraderMuleCountMin = 3;

	private const int TraderMuleCountMax = 5;

	private const int OutlawStartingCompanionCount = 1;

	private const int OutlawStartingGoldMin = 2000;

	private const int OutlawStartingGoldMax = 5000;

	private const int OutlawStartingFoodMin = 3;

	private const int OutlawStartingFoodMax = 7;

	private const int OutlawTroopCountMin = 10;

	private const int OutlawTroopCountMax = 15;

	private const float OutlawCrimeRating = 45f;

	private const int OutlawPlunderGoldMin = 1500;

	private const int OutlawPlunderGoldMax = 5000;

	private const int OutlawTradeGoodTypeCountMin = 3;

	private const int OutlawTradeGoodTypeCountMax = 5;

	public override void RegisterEvents()
	{
		CampaignEvents.OnCharacterCreationIsOverEvent.AddNonSerializedListener(this, OnCharacterCreationIsOver);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void OnCharacterCreationIsOver(int index)
	{
		if (index == 8)
		{
			switch (Campaign.Current.AdvancedStartData.GetStartType())
			{
			case "king":
				StartGameAsRuler(Campaign.Current.AdvancedStartData.GetKingdomId());
				break;
			case "mercenary":
				StartGameAsMercenary(Campaign.Current.AdvancedStartData.GetKingdomId());
				break;
			case "vassal":
				StartGameAsVassal(Campaign.Current.AdvancedStartData.GetKingdomId());
				break;
			case "trader":
				StartGameAsTrader();
				break;
			case "outlaw":
				StartGameAsOutlaw();
				break;
			case "beggar":
				StartGameAsBeggar();
				break;
			}
			if (GameStateManager.Current.ActiveState is MapState mapState)
			{
				mapState.Handler.ResetCamera(resetDistance: true, teleportToMainParty: true);
				mapState.Handler.TeleportCameraToMainParty();
			}
			MobileParty.MainParty.MemberRoster.UpdateVersion();
		}
	}

	private void StartGameAsRuler(string kingdomId)
	{
		MobileParty.MainParty.ItemRoster.Clear();
		EnsureMinimumClanTier(5);
		Kingdom kingdom = ResolveKingdom(kingdomId);
		Clan.PlayerClan.ShouldStayInKingdomUntil = CampaignTime.Zero;
		kingdom.RulingClan = Clan.PlayerClan;
		Clan.PlayerClan.Kingdom = kingdom;
		MBFastRandom mBFastRandom = new MBFastRandom(Campaign.Current.Options.Seed ^ 0xC62D4E23u);
		Settlement settlement = null;
		if (Campaign.Current.AdvancedStartData.GetScenario() == "LastStand")
		{
			settlement = GiveStartingFiefs(1, mBFastRandom, (Settlement settlement2) => (settlement2.IsTown || settlement2.IsCastle) && settlement2.OwnerClan.MapFaction == kingdom);
		}
		else
		{
			settlement = GiveStartingFiefs(2, mBFastRandom, (Settlement settlement2) => settlement2.IsTown && settlement2.OwnerClan.MapFaction == kingdom);
			if (Clan.PlayerClan.Settlements.Count((Settlement s) => s.IsTown) < 2)
			{
				settlement = GiveStartingFiefs(2 - Clan.PlayerClan.Settlements.Count, mBFastRandom, (Settlement settlement2) => settlement2.IsTown && settlement2.Culture == kingdom.Culture);
			}
		}
		if (settlement == null)
		{
			settlement = FindFallbackStartingTown(mBFastRandom);
		}
		UpdateMainHeroHomeSettlement(settlement);
		MobileParty.MainParty.Position = Hero.MainHero.HomeSettlement.GatePosition;
		int gold = mBFastRandom.Next(100000, 150001);
		Hero.MainHero.Gold = gold;
		AdjustStartingFood(mBFastRandom, 100, 200, kingdom.Culture);
		Equipment suitableEquipmentSet = GetSuitableEquipmentSet(Hero.MainHero, kingdom.Culture, EquipmentCategories.IsKingdomRulerTemplate, Equipment.EquipmentType.Battle, mBFastRandom);
		Equipment suitableEquipmentSet2 = GetSuitableEquipmentSet(Hero.MainHero, kingdom.Culture, EquipmentCategories.IsKingdomRulerTemplate, Equipment.EquipmentType.Civilian, mBFastRandom);
		AssignMainHeroEquipmentKeepingHorse(suitableEquipmentSet);
		EquipmentHelper.AssignHeroEquipmentFromEquipment(Hero.MainHero, suitableEquipmentSet2);
		GiveTroopsFromTree(new List<CharacterObject> { kingdom.Culture.EliteBasicTroop }, RulerEliteTroopCounts, mBFastRandom);
		GiveTroopsFromTree(new List<CharacterObject> { kingdom.Culture.BasicTroop }, RulerNormalTroopCounts, mBFastRandom);
		GiveStartingCompanions(4, mBFastRandom);
		Hero.MainHero.AddInfluenceWithKingdom(mBFastRandom.Next(75, 151));
	}

	private void StartGameAsVassal(string kingdomId)
	{
		MobileParty.MainParty.ItemRoster.Clear();
		MBFastRandom mBFastRandom = new MBFastRandom(Campaign.Current.Options.Seed ^ 0x2CE42633);
		EnsureMinimumClanTier(2);
		Kingdom kingdom = ResolveKingdom(kingdomId);
		Clan.PlayerClan.DebtToKingdom = 0;
		FactionHelper.AdjustFactionStancesForClanJoiningKingdom(Clan.PlayerClan, kingdom);
		Clan.PlayerClan.ShouldStayInKingdomUntil = default(CampaignTime);
		Clan.PlayerClan.Kingdom = kingdom;
		int gold = mBFastRandom.Next(25000, 75001);
		Hero.MainHero.Gold = gold;
		AdjustStartingFood(mBFastRandom, 75, 100, kingdom.Culture);
		Equipment suitableEquipmentSet = GetSuitableEquipmentSet(Hero.MainHero, kingdom.Culture, EquipmentCategories.IsLordTemplate, Equipment.EquipmentType.Battle, mBFastRandom);
		Equipment suitableEquipmentSet2 = GetSuitableEquipmentSet(Hero.MainHero, kingdom.Culture, EquipmentCategories.IsLordTemplate, Equipment.EquipmentType.Civilian, mBFastRandom);
		AssignMainHeroEquipmentKeepingHorse(suitableEquipmentSet);
		EquipmentHelper.AssignHeroEquipmentFromEquipment(Hero.MainHero, suitableEquipmentSet2);
		GiveTroopsFromTree(new List<CharacterObject> { kingdom.Culture.EliteBasicTroop }, VassalEliteTroopCounts, mBFastRandom);
		GiveTroopsFromTree(new List<CharacterObject> { kingdom.Culture.BasicTroop }, VassalNormalTroopCounts, mBFastRandom);
		GiveStartingCompanions(2, mBFastRandom);
		Settlement settlement = null;
		if (Campaign.Current.AdvancedStartData.GetScenario() != "LastStand")
		{
			settlement = GiveStartingFiefs(1, mBFastRandom, (Settlement settlement2) => settlement2.IsCastle && settlement2.OwnerClan.MapFaction == kingdom);
		}
		if (settlement == null)
		{
			settlement = FindFallbackStartingTown(mBFastRandom);
		}
		UpdateMainHeroHomeSettlement(settlement);
		MobileParty.MainParty.Position = settlement.GatePosition;
	}

	private void StartGameAsMercenary(string kingdomId)
	{
		MobileParty.MainParty.ItemRoster.Clear();
		MBFastRandom mBFastRandom = new MBFastRandom(Campaign.Current.Options.Seed ^ 0x4530252D);
		EnsureMinimumClanTier(1);
		Kingdom kingdom = ResolveKingdom(kingdomId);
		FactionHelper.AdjustFactionStancesForClanJoiningKingdom(Clan.PlayerClan, kingdom);
		Clan.PlayerClan.MercenaryAwardMultiplier = 50;
		Clan.PlayerClan.Kingdom = kingdom;
		Clan.PlayerClan.StartMercenaryService();
		Campaign.Current.KingdomManager.PlayerMercenaryServiceNextRenewalDay = Campaign.CurrentTime + 30f * (float)CampaignTime.HoursInDay;
		UpdateMainHeroHomeSettlement(FindFallbackStartingTown(mBFastRandom));
		Kingdom kingdom2 = FindEnemyKingdomOrDeclareWar(kingdom);
		MobileParty.MainParty.Position = GetStartingPositionForMercenary(kingdom, kingdom2, mBFastRandom);
		int gold = mBFastRandom.Next(10000, 15001);
		Hero.MainHero.Gold = gold;
		AdjustStartingFood(mBFastRandom, 30, 50, kingdom.Culture);
		GiveTroopsFromTree(new List<CharacterObject> { kingdom.BasicTroop, kingdom2.BasicTroop }, MercenaryTroopCounts, mBFastRandom);
		GiveStartingCompanions(1, mBFastRandom);
	}

	private void StartGameAsTrader()
	{
		MobileParty.MainParty.ItemRoster.Clear();
		EnsureMinimumClanTier(1);
		MBFastRandom mBFastRandom = new MBFastRandom(Campaign.Current.Options.Seed ^ 0x51E9449F);
		Town town = Town.AllTowns[mBFastRandom.Next(0, Town.AllTowns.Count)];
		Kingdom kingdom = town.Settlement.OwnerClan.Kingdom;
		MobileParty.MainParty.Position = town.Settlement.GatePosition;
		Hero.MainHero.BornSettlement = town.Settlement;
		Hero.MainHero.UpdateHomeSettlement();
		int gold = mBFastRandom.Next(5000, 10001);
		Hero.MainHero.Gold = gold;
		AdjustStartingFood(mBFastRandom, 30, 50, kingdom.Culture);
		GiveStartingCompanions(2, mBFastRandom);
		List<Workshop> list = town.Workshops.Where((Workshop w) => !w.WorkshopType.IsHidden).ToList();
		ChangeOwnerOfWorkshopAction.ApplyByFree(list[mBFastRandom.Next(0, list.Count)], Hero.MainHero);
		Hero caravanLeader = Clan.PlayerClan.Companions[mBFastRandom.Next(0, Clan.PlayerClan.Companions.Count)];
		PartyTemplateObject randomCaravanTemplate = CaravanHelper.GetRandomCaravanTemplate(kingdom.Culture, isElite: false, isLand: true);
		CaravanPartyComponent.CreateCaravanParty(Hero.MainHero, town.Settlement, randomCaravanTemplate, isInitialSpawn: true, caravanLeader);
		List<CharacterObject> list2 = kingdom.Culture.NotableTemplates.Where((CharacterObject t) => t.IsFemale == Hero.MainHero.IsFemale).ToList();
		CharacterObject characterObject = list2[mBFastRandom.Next(0, list2.Count)];
		List<Equipment> list3 = characterObject.BattleEquipments.ToList();
		List<Equipment> list4 = characterObject.CivilianEquipments.ToList();
		EquipmentHelper.AssignHeroEquipmentFromEquipment(Hero.MainHero, list3[mBFastRandom.Next(0, list3.Count)].Clone());
		EquipmentHelper.AssignHeroEquipmentFromEquipment(Hero.MainHero, list4[mBFastRandom.Next(0, list4.Count)].Clone());
		ItemObject item = MBObjectManager.Instance.GetObject<ItemObject>("mule");
		int number = mBFastRandom.Next(3, 6);
		MobileParty.MainParty.ItemRoster.AddToCounts(new EquipmentElement(item), number);
		GiveTroopsToMainPartyFromCaravanTemplate(kingdom.Culture, mBFastRandom);
		GiveTradeGoods(kingdom.Culture, 1500, 5000, 1, mBFastRandom);
	}

	private void StartGameAsOutlaw()
	{
		MobileParty.MainParty.ItemRoster.Clear();
		MBFastRandom mBFastRandom = new MBFastRandom(Campaign.Current.Options.Seed ^ 0xF205936Du);
		Town town = Town.AllTowns[mBFastRandom.Next(0, Town.AllTowns.Count)];
		MobileParty.MainParty.Position = town.Settlement.GatePosition;
		int gold = mBFastRandom.Next(2000, 5001);
		Hero.MainHero.Gold = gold;
		AdjustStartingFood(mBFastRandom, 3, 7, town.Culture);
		Settlement settlement = null;
		float num = float.MaxValue;
		foreach (Hideout item in Hideout.All)
		{
			float distance = Campaign.Current.Models.MapDistanceModel.GetDistance(town.Settlement, item.Settlement, isFromPort: false, isTargetingPort: false, MobileParty.NavigationType.Default);
			if (distance < num)
			{
				num = distance;
				settlement = item.Settlement;
			}
		}
		CultureObject culture = settlement.Culture;
		List<CharacterObject> list = new List<CharacterObject>();
		list.Add(culture.BanditBandit);
		list.Add(culture.BanditRaider);
		int num2 = mBFastRandom.Next(10, 16);
		for (int i = 0; i < num2; i++)
		{
			CharacterObject character = list[mBFastRandom.Next(0, list.Count)];
			MobileParty.MainParty.MemberRoster.AddToCounts(character, 1);
		}
		GiveStartingCompanions(1, mBFastRandom);
		foreach (Kingdom kingdom in Campaign.Current.Kingdoms)
		{
			if (!kingdom.IsEliminated)
			{
				float mainHeroCrimeRating = MBMath.ClampFloat(kingdom.MainHeroCrimeRating + 45f, 0f, Campaign.Current.Models.CrimeModel.GetMaxCrimeRating());
				kingdom.MainHeroCrimeRating = mainHeroCrimeRating;
			}
		}
		foreach (Clan clan in Campaign.Current.Clans)
		{
			if (!clan.IsEliminated && !clan.MapFaction.IsKingdomFaction && clan != Clan.PlayerClan)
			{
				float mainHeroCrimeRating2 = MBMath.ClampFloat(clan.MainHeroCrimeRating + 45f, 0f, Campaign.Current.Models.CrimeModel.GetMaxCrimeRating());
				clan.MainHeroCrimeRating = mainHeroCrimeRating2;
			}
		}
		GiveTradeGoods(Hero.MainHero.Culture, 1500, 5000, mBFastRandom.Next(3, 6), mBFastRandom);
	}

	private void StartGameAsBeggar()
	{
		MobileParty.MainParty.ItemRoster.Clear();
		MBFastRandom mBFastRandom = new MBFastRandom(Campaign.Current.Options.Seed ^ 0x958467EBu);
		List<Town> list = Town.AllTowns.Where((Town t) => t.Culture == Hero.MainHero.Culture).ToList();
		Town town = list[mBFastRandom.Next(0, list.Count)];
		MobileParty.MainParty.Position = town.Settlement.GatePosition;
		Hero.MainHero.Gold = 0;
		if (!Hero.MainHero.IsFemale)
		{
			_ = Hero.MainHero.Culture.Beggar;
		}
		else
		{
			_ = Hero.MainHero.Culture.FemaleBeggar;
		}
		for (int num = 0; num < 12; num++)
		{
			Hero.MainHero.BattleEquipment[num] = EquipmentElement.Invalid;
			Hero.MainHero.CivilianEquipment[num] = EquipmentElement.Invalid;
			Hero.MainHero.StealthEquipment[num] = EquipmentElement.Invalid;
		}
		ItemObject item = MBObjectManager.Instance.GetObject<ItemObject>("aso_beggar_robe");
		Hero.MainHero.BattleEquipment[EquipmentIndex.Body] = new EquipmentElement(item);
		Hero.MainHero.CivilianEquipment[EquipmentIndex.Body] = new EquipmentElement(item);
		Hero.MainHero.StealthEquipment[EquipmentIndex.Body] = new EquipmentElement(item);
		Hero.MainHero.BattleEquipment[EquipmentIndex.WeaponItemBeginSlot] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("aso_beggar_sword"));
		Hero.MainHero.StealthEquipment[EquipmentIndex.Weapon2] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("stealth_throwing_stone"));
	}

	public static void AssignMainHeroEquipmentKeepingHorse(Equipment sourceEquipment)
	{
		Equipment obj = (sourceEquipment.IsCivilian ? Hero.MainHero.CivilianEquipment : Hero.MainHero.BattleEquipment);
		EquipmentElement value = obj[EquipmentIndex.ArmorItemEndSlot];
		EquipmentElement value2 = obj[EquipmentIndex.HorseHarness];
		EquipmentElement rosterElement = sourceEquipment[EquipmentIndex.ArmorItemEndSlot];
		EquipmentElement rosterElement2 = sourceEquipment[EquipmentIndex.HorseHarness];
		if (!rosterElement.IsEmpty)
		{
			MobileParty.MainParty.ItemRoster.AddToCounts(rosterElement, 1);
		}
		if (!rosterElement2.IsEmpty)
		{
			MobileParty.MainParty.ItemRoster.AddToCounts(rosterElement2, 1);
		}
		EquipmentHelper.AssignHeroEquipmentFromEquipment(Hero.MainHero, sourceEquipment);
		obj[EquipmentIndex.ArmorItemEndSlot] = value;
		obj[EquipmentIndex.HorseHarness] = value2;
	}

	private static void GiveTroopsToMainPartyFromCaravanTemplate(CultureObject culture, MBFastRandom random)
	{
		List<PartyTemplateObject> list = new List<PartyTemplateObject>();
		foreach (PartyTemplateObject caravanPartyTemplate in culture.CaravanPartyTemplates)
		{
			if (caravanPartyTemplate.ShipHulls.Count == 0)
			{
				list.Add(caravanPartyTemplate);
			}
		}
		PartyTemplateObject partyTemplateObject = list[random.Next(0, list.Count)];
		List<(CharacterObject, int)> list2 = new List<(CharacterObject, int)>();
		int num = 0;
		for (int i = 0; i < partyTemplateObject.Stacks.Count; i++)
		{
			CharacterObject character = partyTemplateObject.Stacks[i].Character;
			int num2 = random.Next(partyTemplateObject.Stacks[i].MinValue, partyTemplateObject.Stacks[i].MaxValue + 1);
			list2.Add((character, num2));
			num += num2;
		}
		int num3 = (int)MobileParty.MainParty.Party.PartySizeLimitExplainer.ResultNumber - MobileParty.MainParty.Party.NumberOfAllMembers;
		if (num > num3 && num > 0 && num3 > 0)
		{
			int num4 = 0;
			for (int j = 0; j < partyTemplateObject.Stacks.Count; j++)
			{
				num4 += partyTemplateObject.Stacks[j].MinValue;
			}
			if (num3 <= num4)
			{
				float num5 = ((num4 > 0) ? ((float)num3 / (float)num4) : 0f);
				for (int k = 0; k < list2.Count; k++)
				{
					list2[k] = (list2[k].Item1, (int)((float)partyTemplateObject.Stacks[k].MinValue * num5));
				}
			}
			else
			{
				int num6 = num3 - num4;
				int num7 = num - num4;
				float num8 = (float)num6 / (float)num7;
				for (int l = 0; l < list2.Count; l++)
				{
					int num9 = list2[l].Item2 - partyTemplateObject.Stacks[l].MinValue;
					list2[l] = (list2[l].Item1, partyTemplateObject.Stacks[l].MinValue + (int)((float)num9 * num8));
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

	public static Equipment GetSuitableEquipmentSet(Hero hero, CultureObject culture, EquipmentCategories customFlags, Equipment.EquipmentType equipmentType, MBFastRandom random)
	{
		MBList<Equipment> mBList = new MBList<Equipment>();
		if (hero.IsFemale)
		{
			customFlags |= EquipmentCategories.IsFemaleTemplate;
		}
		foreach (MBEquipmentRoster item in MBEquipmentRosterExtensions.All)
		{
			if (item.EquipmentCulture != culture || item.EquipmentCategories != customFlags)
			{
				continue;
			}
			foreach (Equipment allEquipment in item.AllEquipments)
			{
				if (allEquipment.ItemEquipmentType == equipmentType)
				{
					mBList.Add(allEquipment);
				}
			}
		}
		return mBList[random.Next(0, mBList.Count)];
	}

	public static Settlement GiveStartingFiefs(int count, MBFastRandom random, Func<Settlement, bool> condition = null)
	{
		List<Settlement> list = new List<Settlement>();
		foreach (Town allFief in Town.AllFiefs)
		{
			if (condition == null || condition(allFief.Settlement))
			{
				list.Add(allFief.Settlement);
			}
		}
		if (list.Count == 0)
		{
			return null;
		}
		Settlement settlement = list[random.Next(0, list.Count)];
		GiveSettlementToMainHero(settlement);
		List<Settlement> list2 = new List<Settlement>();
		HashSet<Settlement> hashSet = new HashSet<Settlement>();
		list2.Add(settlement);
		hashSet.Add(settlement);
		for (int i = 0; i < count - 1; i++)
		{
			Settlement nextFief = GetNextFief(list2, hashSet, condition, random, 2);
			if (nextFief == null)
			{
				break;
			}
			GiveSettlementToMainHero(nextFief);
			list2.Add(nextFief);
			hashSet.Add(nextFief);
		}
		return settlement;
	}

	private static Settlement GetNextFief(List<Settlement> givenSettlements, HashSet<Settlement> excludedSettlements, Func<Settlement, bool> condition, MBFastRandom random, int searchDepth)
	{
		Dictionary<Settlement, int> dictionary = new Dictionary<Settlement, int>();
		foreach (Settlement givenSettlement in givenSettlements)
		{
			foreach (Settlement neighborFortification in givenSettlement.Town.GetNeighborFortifications(MobileParty.NavigationType.All))
			{
				if (!excludedSettlements.Contains(neighborFortification) && (condition == null || condition(neighborFortification)))
				{
					if (!dictionary.ContainsKey(neighborFortification))
					{
						dictionary[neighborFortification] = 0;
					}
					dictionary[neighborFortification]++;
				}
			}
		}
		if (dictionary.Count > 0)
		{
			int maxScore = dictionary.Values.Max();
			List<Settlement> list = (from kv in dictionary
				where kv.Value == maxScore
				select kv.Key).ToList();
			return list[random.Next(0, list.Count)];
		}
		if (searchDepth <= 0)
		{
			return null;
		}
		List<Settlement> list2 = new List<Settlement>();
		foreach (Settlement givenSettlement2 in givenSettlements)
		{
			foreach (Settlement neighborFortification2 in givenSettlement2.Town.GetNeighborFortifications(MobileParty.NavigationType.All))
			{
				if (!excludedSettlements.Contains(neighborFortification2))
				{
					list2.Add(neighborFortification2);
				}
			}
		}
		if (list2.Count == 0)
		{
			return null;
		}
		return GetNextFief(list2, excludedSettlements, condition, random, searchDepth - 1);
	}

	public static void UpdateMainHeroHomeSettlement(Settlement settlement)
	{
		Hero.MainHero.BornSettlement = settlement;
		Clan.PlayerClan.SetInitialHomeSettlement(settlement);
		Hero.MainHero.UpdateHomeSettlement();
	}

	public static Settlement FindFallbackStartingTown(MBFastRandom random, bool preferPort = false)
	{
		Settlement settlement = null;
		if (preferPort)
		{
			if (Clan.PlayerClan.Kingdom != null)
			{
				List<Town> list = Town.AllTowns.Where((Town t) => t.Settlement.OwnerClan.Kingdom == Clan.PlayerClan.Kingdom && t.Settlement.HasPort).ToList();
				if (list.Count > 0)
				{
					settlement = list[random.Next(0, list.Count)].Settlement;
				}
				if (settlement == null)
				{
					List<Town> list2 = Town.AllTowns.Where((Town t) => t.Settlement.Culture == Clan.PlayerClan.Kingdom.Culture && t.Settlement.HasPort).ToList();
					if (list2.Count > 0)
					{
						settlement = list2[random.Next(0, list2.Count)].Settlement;
					}
				}
			}
			if (settlement == null)
			{
				List<Town> list3 = Town.AllTowns.Where((Town t) => t.Settlement.Culture == Hero.MainHero.Culture && t.Settlement.HasPort).ToList();
				if (list3.Count > 0)
				{
					settlement = list3[random.Next(0, list3.Count)].Settlement;
				}
			}
			if (settlement == null)
			{
				List<Town> list4 = Town.AllTowns.Where((Town t) => t.Settlement.HasPort).ToList();
				settlement = list4[random.Next(0, list4.Count)].Settlement;
			}
		}
		else
		{
			if (Clan.PlayerClan.Kingdom != null)
			{
				List<Town> list5 = Town.AllTowns.Where((Town t) => t.Settlement.OwnerClan.Kingdom == Clan.PlayerClan.Kingdom).ToList();
				if (list5.Count > 0)
				{
					settlement = list5[random.Next(0, list5.Count)].Settlement;
				}
				if (settlement == null)
				{
					List<Town> list6 = Town.AllTowns.Where((Town t) => t.Settlement.Culture == Clan.PlayerClan.Kingdom.Culture).ToList();
					if (list6.Count > 0)
					{
						settlement = list6[random.Next(0, list6.Count)].Settlement;
					}
				}
			}
			if (settlement == null)
			{
				List<Town> list7 = Town.AllTowns.Where((Town t) => t.Settlement.Culture == Hero.MainHero.Culture).ToList();
				if (list7.Count > 0)
				{
					settlement = list7[random.Next(0, list7.Count)].Settlement;
				}
			}
			if (settlement == null)
			{
				settlement = Town.AllTowns[random.Next(0, Town.AllTowns.Count)].Settlement;
			}
		}
		return settlement;
	}

	private static void GiveSettlementToMainHero(Settlement settlement)
	{
		if (settlement.IsFortification)
		{
			settlement.Town.OwnerClan = Clan.PlayerClan;
			if (settlement.Town.Governor != null)
			{
				settlement.Town.Governor.GovernorOf.Governor = null;
				settlement.Town.Governor.GovernorOf = null;
			}
		}
		settlement.Party.SetVisualAsDirty();
		foreach (Village boundVillage in settlement.BoundVillages)
		{
			boundVillage.Settlement.Party.SetVisualAsDirty();
			if (boundVillage.VillagerPartyComponent == null)
			{
				continue;
			}
			foreach (MobileParty item in MobileParty.All)
			{
				if (item.MapEvent == null && item != MobileParty.MainParty && item.ShortTermTargetParty == boundVillage.VillagerPartyComponent.MobileParty && !item.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction))
				{
					item.SetMoveModeHold();
				}
			}
		}
	}

	private void GiveTroopsFromTree(List<CharacterObject> baseTroops, Dictionary<int, (int, int)> tierList, MBFastRandom random)
	{
		foreach (KeyValuePair<int, (int, int)> tier in tierList)
		{
			List<CharacterObject> list = CollectTroops(baseTroops, tier.Key);
			if (list.Count == 0)
			{
				list = CollectTroops(baseTroops, tier.Key + 1);
			}
			if (list.Count == 0 && tier.Key > 1)
			{
				list = CollectTroops(baseTroops, tier.Key - 1);
			}
			if (list.Count == 0)
			{
				Debug.FailedAssert("Check the troop tree for double check if troop tiers are correct.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CampaignBehaviors\\CampaignAdvancedStartingPlayerOptionsCampaignBehavior.cs", "GiveTroopsFromTree", 871);
				continue;
			}
			int num = random.Next(tier.Value.Item1, tier.Value.Item2 + 1);
			for (int i = 0; i < num; i++)
			{
				CharacterObject character = list[random.Next(0, list.Count)];
				MobileParty.MainParty.MemberRoster.AddToCounts(character, 1);
			}
		}
	}

	private static List<CharacterObject> CollectTroops(List<CharacterObject> baseTroops, int tier)
	{
		List<CharacterObject> list = new List<CharacterObject>();
		foreach (CharacterObject baseTroop in baseTroops)
		{
			list.AddRange(CharacterHelper.GetTroopTree(baseTroop, tier, tier));
		}
		return list;
	}

	private Kingdom FindEnemyKingdomOrDeclareWar(Kingdom kingdom)
	{
		foreach (Kingdom kingdom4 in Campaign.Current.Kingdoms)
		{
			if (!kingdom4.IsEliminated && kingdom4 != kingdom && FactionManager.IsAtWarAgainstFaction(kingdom, kingdom4))
			{
				return kingdom4;
			}
		}
		Kingdom kingdom2 = null;
		Dictionary<Kingdom, float> dictionary = new Dictionary<Kingdom, float>();
		foreach (Settlement settlement in kingdom.Settlements)
		{
			if (!settlement.IsTown && !settlement.IsCastle)
			{
				continue;
			}
			foreach (Settlement neighborFortification in settlement.Town.GetNeighborFortifications(MobileParty.NavigationType.All))
			{
				if (neighborFortification.MapFaction is Kingdom kingdom3 && kingdom3 != kingdom && !kingdom3.IsEliminated && !dictionary.ContainsKey(kingdom3))
				{
					TextObject reason;
					float scoreOfDeclaringWar = Campaign.Current.Models.DiplomacyModel.GetScoreOfDeclaringWar(kingdom, kingdom3, Clan.PlayerClan, out reason);
					dictionary.Add(kingdom3, scoreOfDeclaringWar);
					if (kingdom2 == null || scoreOfDeclaringWar > dictionary[kingdom2])
					{
						kingdom2 = kingdom3;
					}
				}
			}
		}
		FactionManager.DeclareWar(kingdom, kingdom2);
		return kingdom2;
	}

	private CampaignVec2 GetStartingPositionForMercenary(Kingdom kingdom, Kingdom enemyKingdom, MBFastRandom random)
	{
		Settlement settlement = null;
		Settlement settlement2 = null;
		float num = float.MaxValue;
		foreach (Town fief in kingdom.Fiefs)
		{
			foreach (Town fief2 in enemyKingdom.Fiefs)
			{
				float distance = Campaign.Current.Models.MapDistanceModel.GetDistance(fief.Settlement, fief2.Settlement, isFromPort: false, isTargetingPort: false, MobileParty.NavigationType.Default);
				if (distance < num)
				{
					num = distance;
					settlement = fief.Settlement;
					settlement2 = fief2.Settlement;
				}
			}
		}
		Vec2 vec = (settlement2.GetPosition2D - settlement.GetPosition2D).Normalized();
		return NavigationHelper.FindPointAroundPosition(new CampaignVec2(settlement.GatePosition.ToVec2() + vec * 2f, isOnLand: true), MobileParty.NavigationType.All, 2f, 0f, requirePath: false);
	}

	public static void GiveTradeGoods(CultureObject culture, int goldValueMin, int goldValueMax, int typeCount, MBFastRandom random)
	{
		List<ItemObject> list = new List<ItemObject>();
		foreach (Settlement item2 in Settlement.All)
		{
			if (!item2.IsVillage || item2.Culture != culture)
			{
				continue;
			}
			foreach (var production in item2.Village.VillageType.Productions)
			{
				ItemObject item = production.Item1;
				if (item != null && !item.IsFood && !item.IsMountable && !item.IsAnimal && !item.HasHorseComponent && item.Value > 0 && !list.Contains(item))
				{
					list.Add(item);
				}
			}
		}
		ItemObject[] array = new ItemObject[(typeCount > list.Count) ? list.Count : typeCount];
		for (int i = 0; i < array.Length; i++)
		{
			list.Remove(array[i] = list[random.Next(0, list.Count)]);
		}
		int num = random.Next(goldValueMin, goldValueMax + 1);
		float[] array2 = new float[array.Length];
		float num2 = 0f;
		for (int j = 0; j < array.Length; j++)
		{
			array2[j] = random.NextFloat();
			num2 += array2[j];
		}
		int[] array3 = new int[array.Length];
		for (int k = 0; k < array.Length; k++)
		{
			int num3 = (int)((float)num * (array2[k] / num2));
			array3[k] = num3 / array[k].Value;
		}
		MobileParty mainParty = MobileParty.MainParty;
		float num4 = (float)mainParty.InventoryCapacity - mainParty.TotalWeightCarried;
		float num5 = 0f;
		for (int l = 0; l < array.Length; l++)
		{
			num5 += (float)array3[l] * array[l].Weight;
		}
		if (num5 > num4 && num5 > 0f)
		{
			float num6 = num4 / num5;
			for (int m = 0; m < array.Length; m++)
			{
				array3[m] = (int)((float)array3[m] * num6);
			}
		}
		for (int n = 0; n < array.Length; n++)
		{
			if (array3[n] > 0)
			{
				mainParty.ItemRoster.AddToCounts(new EquipmentElement(array[n]), array3[n]);
			}
		}
	}

	public static void GiveStartingCompanions(int count, MBFastRandom random)
	{
		Dictionary<CompanionType, List<Hero>> dictionary = new Dictionary<CompanionType, List<Hero>>();
		foreach (Hero allAliveHero in Hero.AllAliveHeroes)
		{
			if (allAliveHero.IsWanderer && allAliveHero.IsActive && allAliveHero.CompanionOf == null)
			{
				CompanionType companionType = GetCompanionType(allAliveHero);
				if (!dictionary.ContainsKey(companionType))
				{
					dictionary[companionType] = new List<Hero>();
				}
				dictionary[companionType].Add(allAliveHero);
			}
		}
		List<CompanionType> list = new List<CompanionType>(dictionary.Keys);
		HashSet<CompanionType> hashSet = new HashSet<CompanionType>();
		for (int i = 0; i < count; i++)
		{
			if (list.Count == 0)
			{
				break;
			}
			if (hashSet.Count == list.Count)
			{
				hashSet.Clear();
			}
			List<CompanionType> list2 = new List<CompanionType>();
			foreach (CompanionType item in list)
			{
				if (!hashSet.Contains(item))
				{
					list2.Add(item);
				}
			}
			CompanionType companionType2 = list2[random.Next(0, list2.Count)];
			hashSet.Add(companionType2);
			List<Hero> list3 = dictionary[companionType2];
			Hero hero = list3[random.Next(0, list3.Count)];
			list3.Remove(hero);
			if (list3.Count == 0)
			{
				dictionary.Remove(companionType2);
				list.Remove(companionType2);
				hashSet.Remove(companionType2);
			}
			hero.CompanionOf = Clan.PlayerClan;
			hero.StayingInSettlement = null;
			hero.SetHasMet();
			MobileParty.MainParty.AddElementToMemberRoster(hero.CharacterObject, 1);
		}
	}

	private static CompanionType GetCompanionTypeForSkill(SkillObject skill)
	{
		if (skill == DefaultSkills.Engineering)
		{
			return CompanionType.Engineering;
		}
		if (skill == DefaultSkills.Tactics)
		{
			return CompanionType.Tactics;
		}
		if (skill == DefaultSkills.Leadership)
		{
			return CompanionType.Leadership;
		}
		if (skill == DefaultSkills.Steward)
		{
			return CompanionType.Steward;
		}
		if (skill == DefaultSkills.Trade)
		{
			return CompanionType.Trade;
		}
		if (skill == DefaultSkills.Roguery)
		{
			return CompanionType.Roguery;
		}
		if (skill == DefaultSkills.Medicine)
		{
			return CompanionType.Medicine;
		}
		if (skill == DefaultSkills.Crafting)
		{
			return CompanionType.Smithing;
		}
		if (skill == DefaultSkills.Scouting)
		{
			return CompanionType.Scouting;
		}
		return CompanionType.Combat;
	}

	private static CompanionType GetCompanionType(Hero hero)
	{
		if (hero.CharacterObject.IsMariner)
		{
			return CompanionType.Sailor;
		}
		CompanionType result = CompanionType.Combat;
		int num = 20;
		foreach (SkillObject item in Skills.All)
		{
			int skillValue = hero.GetSkillValue(item);
			if (skillValue > num)
			{
				CompanionType companionTypeForSkill = GetCompanionTypeForSkill(item);
				if (companionTypeForSkill != CompanionType.Combat)
				{
					num = skillValue;
					result = companionTypeForSkill;
				}
			}
		}
		return result;
	}

	public static void EnsureMinimumClanTier(int minimumTier)
	{
		if (Clan.PlayerClan.Tier < minimumTier)
		{
			float num = (float)Campaign.Current.Models.ClanTierModel.GetRequiredRenownForTier(minimumTier) - Clan.PlayerClan.Renown;
			if (num > 0f)
			{
				Clan.PlayerClan.AddRenown(num, shouldNotify: false);
			}
		}
	}

	public static Kingdom ResolveKingdom(string kingdomId)
	{
		Kingdom kingdom = Campaign.Current.Kingdoms.FirstOrDefaultQ((Kingdom k) => k.StringId.Equals(kingdomId));
		if (Campaign.Current.AdvancedStartData.GetScenario() == "unitedempire" && kingdom.Culture.StringId == "empire")
		{
			return Campaign.Current.Kingdoms.FirstOrDefaultQ((Kingdom k) => k.StringId.Equals("calradian_empire"));
		}
		return kingdom;
	}

	public static void AdjustStartingFood(MBFastRandom random, int minFoodAmount, int maxFoodAmount, CultureObject culture)
	{
		List<ItemObject> list = new List<ItemObject>();
		foreach (Village item2 in Village.All)
		{
			if (item2.Bound.Culture != culture)
			{
				continue;
			}
			foreach (var production in item2.VillageType.Productions)
			{
				if (production.Item1.IsFood && !list.Contains(production.Item1))
				{
					list.Add(production.Item1);
				}
			}
		}
		int num = random.Next(minFoodAmount, maxFoodAmount + 1);
		int num2 = random.Next(1, list.Count + 1);
		int num3 = num / num2;
		int num4 = TaleWorlds.Library.MathF.Max(1, num3 / 4);
		int num5 = num;
		for (int i = 0; i < num2; i++)
		{
			int num6 = ((i >= num2 - 1) ? num5 : (num3 + random.Next(-num4, num4 + 1)));
			num5 -= num6;
			if (num6 > 0)
			{
				int index = random.Next(0, list.Count);
				ItemObject item = list[index];
				list.RemoveAt(index);
				MobileParty.MainParty.ItemRoster.AddToCounts(item, num6);
			}
		}
	}
}
