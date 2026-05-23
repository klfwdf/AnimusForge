using System.Collections.Generic;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class RebellionsCampaignBehavior : CampaignBehaviorBase
{
	private const int UpdateClanAfterDays = 30;

	private const int LoyaltyAfterRebellion = 100;

	private const int InitialRelationPenalty = -80;

	private const int InitialRelationBoostWithOtherFactions = 10;

	private const int InitialRelationBoost = 60;

	private const int InitialRelationBetweenRebelHeroes = 10;

	private const int RebelClanStartingRenownMin = 200;

	private const int RebelClanStartingRenownMax = 300;

	private const int RebelHeroAgeMin = 25;

	private const int RebelHeroAgeMax = 40;

	private const float MilitiaGarrisonRatio = 1.4f;

	private const float ThrowGarrisonTroopToPrisonPercentage = 0.5f;

	private const float ThrowMilitiaTroopToGarrisonPercentage = 0.6f;

	private const float DailyRebellionCheckChance = 0.25f;

	private Dictionary<Clan, int> _rebelClansAndDaysPassedAfterCreation;

	private Dictionary<CultureObject, Dictionary<int, int>> _cultureIconIdAndFrequencies;

	private bool _rebellionEnabled = true;

	public RebellionsCampaignBehavior()
	{
		_rebelClansAndDaysPassedAfterCreation = new Dictionary<Clan, int>();
		_cultureIconIdAndFrequencies = new Dictionary<CultureObject, Dictionary<int, int>>();
	}

	public override void RegisterEvents()
	{
		CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, DailyTickSettlement);
		CampaignEvents.DailyTickClanEvent.AddNonSerializedListener(this, DailyTickClan);
		CampaignEvents.OnNewGameCreatedPartialFollowUpEndEvent.AddNonSerializedListener(this, OnNewGameCreatedPartialFollowUpEnd);
		CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, OnGameLoaded);
		CampaignEvents.OnClanDestroyedEvent.AddNonSerializedListener(this, OnClanDestroyed);
		CampaignEvents.OnSiegeEventStartedEvent.AddNonSerializedListener(this, OnSiegeStarted);
	}

	private void OnSiegeStarted(SiegeEvent siegeEvent)
	{
		if (siegeEvent.BesiegedSettlement.IsTown)
		{
			CheckAndSetTownRebelliousState(siegeEvent.BesiegedSettlement);
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_rebelClansAndDaysPassedAfterCreation", ref _rebelClansAndDaysPassedAfterCreation);
		dataStore.SyncData("_iconIdAndFrequency", ref _cultureIconIdAndFrequencies);
	}

	private void OnNewGameCreatedPartialFollowUpEnd(CampaignGameStarter starter)
	{
		InitializeIconIdAndFrequencies();
	}

	private void OnGameLoaded()
	{
		InitializeIconIdAndFrequencies();
		if (!MBSaveLoad.IsUpdatingGameVersion || !(MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("e1.7.3.0")))
		{
			return;
		}
		foreach (Settlement item in Settlement.All)
		{
			if (!item.IsTown && item.InRebelliousState)
			{
				item.Town.InRebelliousState = false;
				CampaignEventDispatcher.Instance.TownRebelliousStateChanged(item.Town, rebelliousState: false);
			}
		}
	}

	private void DailyTickSettlement(Settlement settlement)
	{
		if (_rebellionEnabled && settlement.IsTown && settlement.Party.MapEvent == null && settlement.Party.SiegeEvent == null && !settlement.OwnerClan.IsRebelClan && Settlement.CurrentSettlement != settlement)
		{
			CheckAndSetTownRebelliousState(settlement);
			if (MBRandom.RandomFloat < 0.25f && CheckRebellionEvent(settlement))
			{
				StartRebellionEvent(settlement);
			}
		}
		if (settlement.IsTown && settlement.OwnerClan.IsRebelClan)
		{
			float num = MBMath.Map(_rebelClansAndDaysPassedAfterCreation[settlement.OwnerClan] - 1, 0f, 30f, Campaign.Current.Models.SettlementLoyaltyModel.LoyaltyBoostAfterRebellionStartValue, 0f);
			settlement.Town.Loyalty += num;
		}
	}

	private void CheckAndSetTownRebelliousState(Settlement settlement)
	{
		bool inRebelliousState = settlement.Town.InRebelliousState;
		settlement.Town.InRebelliousState = settlement.Town.Loyalty <= (float)Campaign.Current.Models.SettlementLoyaltyModel.RebelliousStateStartLoyaltyThreshold;
		if (inRebelliousState != settlement.Town.InRebelliousState)
		{
			CampaignEventDispatcher.Instance.TownRebelliousStateChanged(settlement.Town, settlement.Town.InRebelliousState);
		}
	}

	private void OnClanDestroyed(Clan destroyedClan)
	{
		if (_rebelClansAndDaysPassedAfterCreation.ContainsKey(destroyedClan))
		{
			_rebelClansAndDaysPassedAfterCreation.Remove(destroyedClan);
		}
		if (destroyedClan.IsRebelClan)
		{
			for (int num = destroyedClan.Heroes.Count - 1; num >= 0; num--)
			{
				Hero hero = destroyedClan.Heroes[num];
				Campaign.Current.CampaignObjectManager.UnregisterDeadHero(hero);
			}
		}
	}

	private void DailyTickClan(Clan clan)
	{
		if (_rebelClansAndDaysPassedAfterCreation.ContainsKey(clan))
		{
			_rebelClansAndDaysPassedAfterCreation[clan]++;
			if (_rebelClansAndDaysPassedAfterCreation[clan] >= 30 && clan.Leader != null && clan.Settlements.Count > 0)
			{
				TextObject textObject = new TextObject("{=aKaGaOQx}{CLAN_LEADER.NAME}{.o} Clan");
				StringHelpers.SetCharacterProperties("CLAN_LEADER", clan.Leader.CharacterObject, textObject);
				clan.ChangeClanName(textObject, textObject);
				clan.IsRebelClan = false;
				_rebelClansAndDaysPassedAfterCreation.Remove(clan);
				CampaignEventDispatcher.Instance.OnRebelliousClanDisbandedAtSettlement(clan.HomeSettlement, clan);
			}
		}
		if (!clan.IsRebelClan || clan.Settlements.Count != 0 || clan.Heroes.Count <= 0 || clan.IsEliminated)
		{
			return;
		}
		for (int num = clan.Heroes.Count - 1; num >= 0; num--)
		{
			Hero hero = clan.Heroes[num];
			if (hero.IsAlive)
			{
				if (hero.IsPrisoner && hero.PartyBelongedToAsPrisoner != null && hero.PartyBelongedToAsPrisoner != PartyBase.MainParty && hero.PartyBelongedToAsPrisoner.LeaderHero != null)
				{
					KillCharacterAction.ApplyByExecution(hero, hero.PartyBelongedToAsPrisoner.LeaderHero, showNotification: true, isForced: true);
				}
				else if (hero.PartyBelongedTo == null)
				{
					KillCharacterAction.ApplyByRemove(hero);
				}
				else if (_rebelClansAndDaysPassedAfterCreation[clan] > 90 && hero.PartyBelongedTo != null && hero.PartyBelongedTo.MapEvent == null)
				{
					KillCharacterAction.ApplyByRemove(hero);
				}
			}
		}
	}

	private static bool CheckRebellionEvent(Settlement settlement)
	{
		if (settlement.Town.Loyalty <= (float)Campaign.Current.Models.SettlementLoyaltyModel.RebellionStartLoyaltyThreshold)
		{
			float militia = settlement.Militia;
			float num = settlement.Town.GarrisonParty?.Party.CalculateCurrentStrength() ?? 0f;
			foreach (MobileParty party in settlement.Parties)
			{
				if (party.IsLordParty && DiplomacyHelper.IsSameFactionAndNotEliminated(party.MapFaction, settlement.MapFaction))
				{
					num += party.Party.CalculateCurrentStrength();
				}
			}
			return militia >= num * 1.4f;
		}
		return false;
	}

	public void StartRebellionEvent(Settlement settlement)
	{
		Clan ownerClan = settlement.OwnerClan;
		CreateRebelPartyAndClan(settlement);
		ApplyRebellionConsequencesToSettlement(settlement);
		CampaignEventDispatcher.Instance.OnRebellionFinished(settlement, ownerClan);
		settlement.Town.FoodStocks = settlement.Town.FoodStocksUpperLimit();
		settlement.Militia = 100f;
	}

	private void ApplyRebellionConsequencesToSettlement(Settlement settlement)
	{
		Dictionary<TroopRosterElement, int> dictionary = new Dictionary<TroopRosterElement, int>();
		foreach (TroopRosterElement item in settlement.Town.GarrisonParty.MemberRoster.GetTroopRoster())
		{
			for (int i = 0; i < item.Number; i++)
			{
				if (MBRandom.RandomFloat < 0.5f)
				{
					if (dictionary.ContainsKey(item))
					{
						dictionary[item]++;
					}
					else
					{
						dictionary.Add(item, 1);
					}
				}
			}
		}
		settlement.Town.GarrisonParty.MemberRoster.Clear();
		foreach (KeyValuePair<TroopRosterElement, int> item2 in dictionary)
		{
			settlement.Party.AddPrisoner(item2.Key.Character, item2.Value);
		}
		settlement.Town.GarrisonParty.AddElementToMemberRoster(settlement.Culture.RangedMilitiaTroop, (int)(settlement.Militia * (MBRandom.RandomFloatRanged(-0.1f, 0.1f) + 0.6f)));
		settlement.Militia = 0f;
		if (settlement.MilitiaPartyComponent != null)
		{
			DestroyPartyAction.Apply(null, settlement.MilitiaPartyComponent.MobileParty);
		}
		settlement.Town.GarrisonParty.MemberRoster.AddToCounts(settlement.OwnerClan.Culture.BasicTroop, 50);
		settlement.Town.GarrisonParty.MemberRoster.AddToCounts((settlement.OwnerClan.Culture.BasicTroop.UpgradeTargets.Length != 0) ? settlement.OwnerClan.Culture.BasicTroop.UpgradeTargets.GetRandomElement() : settlement.OwnerClan.Culture.BasicTroop, 25);
		settlement.Town.Loyalty = 100f;
		settlement.Town.InRebelliousState = false;
	}

	private void CreateRebelPartyAndClan(Settlement settlement)
	{
		MBReadOnlyList<CharacterObject> rebelliousHeroTemplates = settlement.Culture.RebelliousHeroTemplates;
		List<Hero> list = new List<Hero>
		{
			CreateRebelLeader(rebelliousHeroTemplates.GetRandomElement(), settlement),
			CreateRebelGovernor(rebelliousHeroTemplates.GetRandomElement(), settlement),
			CreateRebelSupporterHero(rebelliousHeroTemplates.GetRandomElement(), settlement),
			CreateRebelSupporterHero(rebelliousHeroTemplates.GetRandomElement(), settlement)
		};
		int clanIdForNewRebelClan = GetClanIdForNewRebelClan(settlement.Culture);
		Clan clan = Clan.CreateSettlementRebelClan(settlement, list[0], clanIdForNewRebelClan);
		clan.IsNoble = true;
		clan.AddRenown(MBRandom.RandomInt(200, 300));
		foreach (Hero item in list)
		{
			item.Clan = clan;
		}
		_rebelClansAndDaysPassedAfterCreation.Add(clan, 1);
		foreach (Hero item2 in list)
		{
			item2.ChangeState(Hero.CharacterStates.Active);
		}
		MobileParty mobileParty = MobilePartyHelper.SpawnLordParty(list[0], settlement);
		MobilePartyHelper.SpawnLordParty(list[2], settlement);
		MobilePartyHelper.SpawnLordParty(list[3], settlement);
		IFaction mapFaction = settlement.MapFaction;
		DeclareWarAction.ApplyByRebellion(clan, mapFaction);
		foreach (Hero item3 in list)
		{
			ChangeRelationAction.ApplyRelationChangeBetweenHeroes(mapFaction.Leader, item3, MBRandom.RandomInt(-85, -75));
			foreach (Kingdom item4 in Kingdom.All)
			{
				if (item4.IsEliminated || item4.Culture == mapFaction.Culture)
				{
					continue;
				}
				int num = 0;
				foreach (Town fief in item4.Fiefs)
				{
					num += ((!fief.IsTown) ? 1 : 2);
				}
				int num2 = (int)(MBRandom.RandomFloat * MBRandom.RandomFloat * 30f - (float)num);
				int value = ((item4.Culture == clan.Culture) ? (num2 + MBRandom.RandomInt(55, 65)) : num2);
				item4.Leader.SetPersonalRelation(item3, value);
			}
			foreach (Hero item5 in list)
			{
				if (item3 != item5)
				{
					ChangeRelationAction.ApplyRelationChangeBetweenHeroes(item3, item5, MBRandom.RandomInt(5, 15));
				}
			}
		}
		ChangeOwnerOfSettlementAction.ApplyByRebellion(mobileParty.LeaderHero, settlement);
		ChangeGovernorAction.Apply(settlement.Town, list[1]);
		EnterSettlementAction.ApplyForParty(mobileParty, settlement);
		mobileParty.Ai.DisableForHours(5);
		list[0].ChangeHeroGold(50000);
		CampaignEventDispatcher.Instance.OnClanCreated(clan, isCompanion: false);
	}

	private Hero CreateRebelLeader(CharacterObject templateCharacter, Settlement settlement)
	{
		return CreateRebelHeroInternal(templateCharacter, settlement, new Dictionary<SkillObject, int>
		{
			{
				DefaultSkills.Steward,
				MBRandom.RandomInt(100, 175)
			},
			{
				DefaultSkills.Leadership,
				MBRandom.RandomInt(125, 175)
			},
			{
				DefaultSkills.OneHanded,
				MBRandom.RandomInt(125, 175)
			}
		});
	}

	private Hero CreateRebelGovernor(CharacterObject templateCharacter, Settlement settlement)
	{
		return CreateRebelHeroInternal(templateCharacter, settlement, new Dictionary<SkillObject, int>
		{
			{
				DefaultSkills.Steward,
				MBRandom.RandomInt(125, 200)
			},
			{
				DefaultSkills.Leadership,
				MBRandom.RandomInt(100, 125)
			},
			{
				DefaultSkills.OneHanded,
				MBRandom.RandomInt(60, 90)
			}
		});
	}

	private Hero CreateRebelSupporterHero(CharacterObject templateCharacter, Settlement settlement)
	{
		return CreateRebelHeroInternal(templateCharacter, settlement, new Dictionary<SkillObject, int>
		{
			{
				DefaultSkills.Steward,
				MBRandom.RandomInt(100, 175)
			},
			{
				DefaultSkills.Leadership,
				MBRandom.RandomInt(100, 175)
			},
			{
				DefaultSkills.OneHanded,
				MBRandom.RandomInt(125, 175)
			}
		});
	}

	private Hero CreateRebelHeroInternal(CharacterObject templateCharacter, Settlement settlement, Dictionary<SkillObject, int> startingSkills)
	{
		Hero hero = HeroCreator.CreateSpecialHero(templateCharacter, settlement, null, null, MBRandom.RandomInt(25, 40));
		foreach (KeyValuePair<SkillObject, int> startingSkill in startingSkills)
		{
			hero.HeroDeveloper.SetInitialSkillLevel(startingSkill.Key, startingSkill.Value);
		}
		foreach (PerkObject allPerk in Campaign.Current.AllPerks)
		{
			if (hero.GetPerkValue(allPerk) && (float)hero.GetSkillValue(allPerk.Skill) < allPerk.RequiredSkillValue)
			{
				hero.SetPerkValueInternal(allPerk, value: false);
			}
		}
		return hero;
	}

	private int GetClanIdForNewRebelClan(CultureObject culture)
	{
		int num = 0;
		int num2 = int.MaxValue;
		int num3 = int.MaxValue;
		if (!_cultureIconIdAndFrequencies.TryGetValue(culture, out var value))
		{
			value = new Dictionary<int, int>();
			_cultureIconIdAndFrequencies.Add(culture, value);
		}
		if (culture.PossibleClanBannerIconsIDs != null)
		{
			MBList<int> mBList = culture.PossibleClanBannerIconsIDs.ToMBList();
			mBList.Shuffle();
			foreach (int item in mBList)
			{
				if (!value.TryGetValue(item, out var value2))
				{
					value2 = 0;
					value.Add(item, value2);
				}
				if (value2 < num3)
				{
					num2 = item;
					num3 = value2;
				}
			}
		}
		if (num2 == int.MaxValue)
		{
			foreach (KeyValuePair<CultureObject, Dictionary<int, int>> cultureIconIdAndFrequency in _cultureIconIdAndFrequencies)
			{
				foreach (KeyValuePair<int, int> item2 in cultureIconIdAndFrequency.Value)
				{
					if (item2.Value < num3)
					{
						num2 = item2.Key;
						num3 = item2.Value;
					}
				}
			}
		}
		num = num2;
		if (_cultureIconIdAndFrequencies[culture].TryGetValue(num, out var value3))
		{
			_cultureIconIdAndFrequencies[culture][num] = value3 + 1;
		}
		else
		{
			_cultureIconIdAndFrequencies[culture].Add(num, 1);
		}
		return num;
	}

	private void InitializeIconIdAndFrequencies()
	{
		if (_cultureIconIdAndFrequencies == null)
		{
			_cultureIconIdAndFrequencies = new Dictionary<CultureObject, Dictionary<int, int>>();
		}
		foreach (Kingdom item in Kingdom.All)
		{
			if (!_cultureIconIdAndFrequencies.ContainsKey(item.Culture))
			{
				_cultureIconIdAndFrequencies.Add(item.Culture, new Dictionary<int, int>());
			}
		}
		foreach (CultureObject objectType in MBObjectManager.Instance.GetObjectTypeList<CultureObject>())
		{
			if (!_cultureIconIdAndFrequencies.ContainsKey(objectType))
			{
				_cultureIconIdAndFrequencies.Add(objectType, new Dictionary<int, int>());
			}
		}
		foreach (CultureObject key in _cultureIconIdAndFrequencies.Keys)
		{
			if (key.PossibleClanBannerIconsIDs == null)
			{
				continue;
			}
			foreach (int possibleClanBannerIconsID in key.PossibleClanBannerIconsIDs)
			{
				if (!_cultureIconIdAndFrequencies[key].ContainsKey(possibleClanBannerIconsID))
				{
					_cultureIconIdAndFrequencies[key].Add(possibleClanBannerIconsID, 0);
				}
			}
		}
	}
}
