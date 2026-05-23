using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Incidents;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class IncidentsCampaignBehaviour : CampaignBehaviorBase, INonReadyObjectHandler
{
	[Flags]
	public enum IncidentTrigger
	{
		LeavingVillage = 1,
		LeavingTown = 2,
		LeavingCastle = 4,
		LeavingSettlement = 8,
		LeavingEncounter = 0x10,
		LeavingBattle = 0x20,
		EnteringVillage = 0x40,
		EnteringTown = 0x80,
		EnteringCastle = 0x100,
		WaitingInSettlement = 0x200,
		DuringSiege = 0x400
	}

	public enum IncidentType
	{
		TroopSettlementRelation,
		FoodConsumption,
		PlightOfCivilians,
		PartyCampLife,
		AnimalIllness,
		Illness,
		HuntingForaging,
		PostBattle,
		HardTravel,
		Profit,
		DreamsSongsAndSigns,
		FiefManagement,
		Siege,
		Workshop
	}

	private CampaignTime _lastGlobalIncidentCooldown = CampaignTime.Zero;

	private Dictionary<Incident, CampaignTime> _incidentsOnCooldown = new Dictionary<Incident, CampaignTime>();

	private long _activeIncidentSeed;

	private bool _canInvokeSettlementEvent;

	public override void RegisterEvents()
	{
		CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
		CampaignEvents.GameMenuOptionSelectedEvent.AddNonSerializedListener(this, OnGameMenuOptionSelected);
		CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, OnGameMenuOpened);
		CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
		CampaignEvents.ConversationEnded.AddNonSerializedListener(this, ConversationEnded);
		CampaignEvents.OnHeirSelectionOverEvent.AddNonSerializedListener(this, OnHeirSelectionOver);
		CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
		CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
		CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, OnSettlementLeft);
		CampaignEvents.OnIncidentResolvedEvent.AddNonSerializedListener(this, OnIncidentResolved);
	}

	private void OnIncidentResolved(Incident incident)
	{
		_incidentsOnCooldown.Add(incident, CampaignTime.Now);
		_lastGlobalIncidentCooldown = CampaignTime.Now + GetCooldownTime();
	}

	private void OnSettlementEntered(MobileParty mobileParty, Settlement settlement, Hero hero)
	{
		if (mobileParty == MobileParty.MainParty && Campaign.Current.CurrentMenuContext == null)
		{
			_canInvokeSettlementEvent = true;
		}
	}

	private void OnSettlementLeft(MobileParty mobileParty, Settlement settlement)
	{
		if (mobileParty == MobileParty.MainParty)
		{
			_canInvokeSettlementEvent = false;
		}
	}

	private void OnNewGameCreated(CampaignGameStarter obj)
	{
		_lastGlobalIncidentCooldown = CampaignTime.Now + GetCooldownTime();
	}

	void INonReadyObjectHandler.OnBeforeNonReadyObjectsDeleted()
	{
		InitializeIncidents();
	}

	private void ConversationEnded(IEnumerable<CharacterObject> conversationCharacters)
	{
		if (PlayerEncounter.Current != null && PlayerEncounter.LeaveEncounter && MobileParty.MainParty.CurrentSettlement == null && MobileParty.MainParty.RandomFloatWithSeed((uint)CampaignTime.Now.NumTicks) < Campaign.Current.Models.IncidentModel.GetIncidentTriggerGlobalProbability())
		{
			TryInvokeIncident(IncidentTrigger.LeavingEncounter);
		}
	}

	private void OnMapEventEnded(MapEvent evt)
	{
		if (evt.IsPlayerMapEvent && !evt.IsNavalMapEvent && evt.HasWinner && evt.DefeatedSide != evt.PlayerSide && (evt.IsFieldBattle || evt.IsHideoutBattle) && !evt.AttackerSide.IsSurrendered && !evt.DefenderSide.IsSurrendered && MobileParty.MainParty.RandomFloatWithSeed((uint)CampaignTime.Now.NumTicks) < Campaign.Current.Models.IncidentModel.GetIncidentTriggerGlobalProbability())
		{
			TryInvokeIncident(IncidentTrigger.LeavingBattle);
		}
	}

	private void OnGameMenuOpened(MenuCallbackArgs args)
	{
		if (_canInvokeSettlementEvent && MobileParty.MainParty.RandomFloatWithSeed((uint)CampaignTime.Now.NumTicks) < Campaign.Current.Models.IncidentModel.GetIncidentTriggerGlobalProbability())
		{
			if (args.MenuContext.GameMenu.StringId == "town")
			{
				TryInvokeIncident(IncidentTrigger.EnteringTown);
				_canInvokeSettlementEvent = false;
			}
			if (args.MenuContext.GameMenu.StringId == "village")
			{
				TryInvokeIncident(IncidentTrigger.EnteringVillage);
				_canInvokeSettlementEvent = false;
			}
			if (args.MenuContext.GameMenu.StringId == "castle")
			{
				TryInvokeIncident(IncidentTrigger.EnteringCastle);
				_canInvokeSettlementEvent = false;
			}
		}
	}

	private void OnGameMenuOptionSelected(GameMenu gameMenu, GameMenuOption option)
	{
		if (MobileParty.MainParty.RandomFloatWithSeed((uint)CampaignTime.Now.NumTicks) < Campaign.Current.Models.IncidentModel.GetIncidentTriggerGlobalProbability())
		{
			if (gameMenu.StringId == "town" && option.IdString == "town_leave")
			{
				TryInvokeIncident(IncidentTrigger.LeavingTown | IncidentTrigger.LeavingSettlement);
			}
			if (gameMenu.StringId == "castle" && option.IdString == "leave")
			{
				TryInvokeIncident(IncidentTrigger.LeavingCastle | IncidentTrigger.LeavingSettlement);
			}
			if (gameMenu.StringId == "village" && option.IdString == "leave")
			{
				TryInvokeIncident(IncidentTrigger.LeavingVillage | IncidentTrigger.LeavingSettlement);
			}
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_incidentsOnCooldown", ref _incidentsOnCooldown);
		dataStore.SyncData("_lastGlobalIncidentCooldown", ref _lastGlobalIncidentCooldown);
		dataStore.SyncData("_activeIncidentSeed", ref _activeIncidentSeed);
	}

	private void OnHeirSelectionOver(Hero obj)
	{
		_lastGlobalIncidentCooldown = CampaignTime.Now + CampaignTime.Hours(1f);
		_incidentsOnCooldown.Clear();
	}

	private void OnHourlyTick()
	{
		float num = MobileParty.MainParty.RandomFloatWithSeed((uint)CampaignTime.Now.NumTicks);
		if (MobileParty.MainParty.SiegeEvent != null && num < Campaign.Current.Models.IncidentModel.GetIncidentTriggerProbabilityDuringSiege())
		{
			TryInvokeIncident(IncidentTrigger.DuringSiege);
		}
		else if (Campaign.Current.CurrentMenuContext != null && (Campaign.Current.CurrentMenuContext.GameMenu.StringId == "town_wait_menus" || Campaign.Current.CurrentMenuContext.GameMenu.StringId == "village_wait_menus") && num < Campaign.Current.Models.IncidentModel.GetIncidentTriggerProbabilityDuringWait())
		{
			TryInvokeIncident(IncidentTrigger.WaitingInSettlement);
		}
	}

	private void TryInvokeIncident(IncidentTrigger trigger)
	{
		if ((((trigger & IncidentTrigger.EnteringCastle) == 0 && (trigger & IncidentTrigger.EnteringTown) == 0 && (trigger & IncidentTrigger.EnteringVillage) == 0) || (MobileParty.MainParty.CurrentSettlement != null && !MobileParty.MainParty.CurrentSettlement.IsSettlementBusy(this))) && (((trigger & IncidentTrigger.LeavingTown) == 0 && (trigger & IncidentTrigger.LeavingVillage) == 0 && (trigger & IncidentTrigger.LeavingSettlement) == 0) || !MobileParty.MainParty.LastVisitedSettlement.IsSettlementBusy(this)) && !Hero.MainHero.IsPrisoner && !Campaign.Current.ConversationManager.IsConversationFlowActive && _lastGlobalIncidentCooldown.IsPast)
		{
			CheckIncidentsOnCooldown();
			_activeIncidentSeed = CampaignTime.Now.NumTicks;
			IReadOnlyList<Incident> occurableEventsForTrigger = GetOccurableEventsForTrigger(trigger);
			if (occurableEventsForTrigger.Count > 0)
			{
				Incident randomElement = occurableEventsForTrigger.GetRandomElement();
				InvokeIncident(randomElement);
			}
		}
	}

	private CampaignTime GetCooldownTime()
	{
		CampaignTime minGlobalCooldownTime = Campaign.Current.Models.IncidentModel.GetMinGlobalCooldownTime();
		CampaignTime maxGlobalCooldownTime = Campaign.Current.Models.IncidentModel.GetMaxGlobalCooldownTime();
		return CampaignTime.Hours(MBRandom.RandomFloatRanged((float)minGlobalCooldownTime.ToHours, (float)maxGlobalCooldownTime.ToHours));
	}

	private void CheckIncidentsOnCooldown()
	{
		List<Incident> list = new List<Incident>();
		foreach (KeyValuePair<Incident, CampaignTime> item in _incidentsOnCooldown)
		{
			if (item.Value + item.Key.Cooldown <= CampaignTime.Now)
			{
				list.Add(item.Key);
			}
		}
		foreach (Incident item2 in list)
		{
			_incidentsOnCooldown.Remove(item2);
		}
	}

	private IReadOnlyList<Incident> GetOccurableEventsForTrigger(IncidentTrigger trigger)
	{
		List<Incident> list = new List<Incident>();
		foreach (Incident objectType in MBObjectManager.Instance.GetObjectTypeList<Incident>())
		{
			if (!_incidentsOnCooldown.ContainsKey(objectType) && (objectType.Trigger & trigger) != 0 && objectType.CanIncidentBeInvoked())
			{
				list.Add(objectType);
			}
		}
		return list;
	}

	private void InvokeIncident(Incident incident)
	{
		MapState mapState = GameStateManager.Current.LastOrDefault<MapState>();
		if (mapState != null)
		{
			mapState.NextIncident = incident;
		}
	}

	private Incident RegisterIncident(string id, string title, string description, IncidentTrigger trigger, IncidentType type, CampaignTime cooldown, Func<TextObject, bool> condition)
	{
		Incident incident = Game.Current.ObjectManager.RegisterPresumedObject(new Incident(id));
		incident.Initialize(title, description, trigger, type, cooldown, condition);
		return incident;
	}

	private void InitializeIncidents()
	{
		Incident incident = RegisterIncident("incident_rooster_theft", "{=0EG0iB9p}Rooster theft", "{=8zFwYZeQ}As you ride off, a villager runs after you accusing one of your men of stealing a rooster. A few squawks silence the culprit's attempt to deny the crime. Honor requires you to uphold the laws of the land, but your men expect a generous {RANK} to value those who shed blood for {PRONOUN} over those who do not.", IncidentTrigger.LeavingVillage, IncidentType.TroopSettlementRelation, CampaignTime.Days(60f), delegate(TextObject description)
		{
			description.SetTextVariable("RANK", Hero.MainHero.IsLord ? "lord" : "captain");
			description.SetTextVariable("PRONOUN", Hero.MainHero.IsFemale ? "her" : "him");
			return PartyBase.MainParty.MemberRoster.TotalRegulars >= 25;
		});
		incident.AddOption("{=0k4T7dzu}Return the rooster and have your man whipped.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Honor, 100),
			IncidentEffect.TraitChange(DefaultTraits.Generosity, -50),
			IncidentEffect.MoraleChange(-5f),
			IncidentEffect.SettlementRelationChange(() => MobileParty.MainParty.LastVisitedSettlement, 10)
		});
		incident.AddOption("{=Ppzvp7Mu}Tell the peasant that the rooster is just part of the debt they owe their protectors.", new List<IncidentEffect> { IncidentEffect.SettlementRelationChange(() => MobileParty.MainParty.LastVisitedSettlement, -10) });
		incident.AddOption("{=NdcsYWrF}Throw the peasant a few silvers in compensation.", new List<IncidentEffect>
		{
			IncidentEffect.GoldChange(() => -10),
			IncidentEffect.TraitChange(DefaultTraits.Generosity, 100),
			IncidentEffect.TraitChange(DefaultTraits.Calculating, -100)
		});
		Incident incident2 = RegisterIncident("incident_suitable_boy", "{=7y4Fbx8K}A Suitable Boy", "{=jB4tmY9W}As you leave the village, a young woman approaches you carrying a months-old baby, surrounded by her relatives. She points at one of your men and says that he was the father, and that he should stay here and wed her. The lad, a strong fellow but not good with words, is too flustered to speak. You can't exactly recall if you passed through here a year ago or not.", IncidentTrigger.LeavingVillage, IncidentType.TroopSettlementRelation, CampaignTime.Years(1000f), (TextObject description) => PartyBase.MainParty.MemberRoster.TotalRegulars >= 30);
		incident2.AddOption("{=ThnC13fF}Tell the lad to search his conscience and do what is right, under the eyes of Heaven", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Honor, 100),
			IncidentEffect.Group(IncidentEffect.KillTroopsRandomly((TroopRosterElement x) => true, () => 1), IncidentEffect.SettlementRelationChange(() => MobileParty.MainParty.LastVisitedSettlement, 10)).WithChance(0.5f)
		});
		incident2.AddOption("{=bjDjwN9r}Congratulate the lad on finding a wife and throw the couple a purse of coins as a wedding gift", new List<IncidentEffect>
		{
			IncidentEffect.GoldChange(() => -50),
			IncidentEffect.TraitChange(DefaultTraits.Generosity, 100),
			IncidentEffect.KillTroopsRandomly((TroopRosterElement x) => true, () => 1)
		});
		incident2.AddOption("{=sb9zWc3I}Give your blessing to the marriage and hurl huge handfuls of coins to the villagers, declaring that this will be a day that all shall remember.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Generosity, 200),
			IncidentEffect.TraitChange(DefaultTraits.Calculating, -100),
			IncidentEffect.GoldChange(() => -1000),
			IncidentEffect.RenownChange(5f)
		});
		incident2.AddOption("{=eP8MrIEE}Tell the villagers to get themselves a plough horse instead of trying to poach one of your men", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Mercy, -100),
			IncidentEffect.MoraleChange(5f)
		});
		Incident incident3 = RegisterIncident("incident_spoiled_food_supplies", "{=5aPGeeDf}Spoiled Food Supplies", "{=RsWbJJS7}A delegation of your troops approach you. They show you a heel of bread from your stores, crawling with maggots. A piece of dried meat is streaked with mold. Your quartermaster appears to have packed the goods carelessly, allowing moisture and pests to get inside.", IncidentTrigger.LeavingVillage, IncidentType.FoodConsumption, CampaignTime.Days(60f), (TextObject description) => PartyBase.MainParty.MemberRoster.TotalRegulars >= 10);
		incident3.AddOption("{=p0gqO74k}Money is tight and you've eaten worse in your time. Tell the lads to think of it as seasoning", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Generosity, -100),
			IncidentEffect.WoundTroopsRandomly(0.05f).WithChance(0.5f)
		});
		incident3.AddOption("{=DcaV3yt9}Order your men to throw away the spoiled food", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Generosity, 100),
			IncidentEffect.ChangeItemAmount(GetSpoiledFoodItem, () => -2)
		});
		incident3.AddOption("{=YbqueBpd}Punish your quartermaster for storing it improperly, and take the money to replace it out of his pay. Discipline must prevail", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Mercy, -100),
			IncidentEffect.MoraleChange(5f)
		});
		incident3.AddOption("{=fxfInKgI}Wolf down the food yourself and smack your lips, telling the men that they are lucky to have such fine rations.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Calculating, -100),
			IncidentEffect.TraitChange(DefaultTraits.Valor, 50),
			IncidentEffect.MoraleChange(5f),
			IncidentEffect.HealthChance(-10).WithChance(0.5f)
		});
		Incident incident4 = RegisterIncident("incident_desperate_times", "{=GNdltUJj}Desperate Times", "{=CZamxmfc}As you pass through the town gates, you are thronged by a group of refugees from the countryside. They are gaunt, with sunken eyes. They say they were unable to sow their fields this year for fear of bandits, and were forced to eat their seed grain. They ask for any scraps you can offer.", IncidentTrigger.LeavingTown, IncidentType.PlightOfCivilians, CampaignTime.Days(60f), (TextObject description) => MobileParty.MainParty.LastVisitedSettlement != null && MobileParty.MainParty.LastVisitedSettlement.IsTown && MobileParty.MainParty.LastVisitedSettlement.Town.Villages.Any((Village x) => x.VillageState == Village.VillageStates.Looted));
		incident4.AddOption("{=BjUtGJg6}Break out your food and allow them to fill their bellies before your depart", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Mercy, 100),
			IncidentEffect.ChangeItemAmount(GetDesperateTimesFoodItem, () => -1)
		});
		incident4.AddOption("{=JO6D30FU}Give them sufficient money to replace their seed grain, even though the townspeople may complain that it raises prices", new List<IncidentEffect>
		{
			IncidentEffect.GoldChange(() => -200),
			IncidentEffect.TraitChange(DefaultTraits.Mercy, 200),
			IncidentEffect.SettlementRelationChange(() => MobileParty.MainParty.LastVisitedSettlement, -5)
		});
		incident4.AddOption("{=BslqBNQj}Tell them that you cannot help", new List<IncidentEffect> { IncidentEffect.TraitChange(DefaultTraits.Mercy, -100) });
		Incident incident5 = RegisterIncident("incident_no_country_for_the_afflicted", "{=rflKyqOT}No Country for the Afflicted", "{=NORgVfas}As you prepare to leave, a delegation of townspeople approach you. A group of lepers has encamped before the walls, and they beg passers-by for alms. The townspeople are worried that they might spread the disease and, at any rate, are frightening away potential customers.", IncidentTrigger.LeavingTown, IncidentType.PlightOfCivilians, CampaignTime.Days(60f), (TextObject description) => true);
		incident5.AddOption("{=yfsqkUcb}Tell the townspeople that Heaven wishes to test their compassion, and that they should treat the lepers with kindness.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Mercy, 200),
			IncidentEffect.SettlementRelationChange(() => MobileParty.MainParty.LastVisitedSettlement, -5),
			IncidentEffect.TownProsperityChange(() => MobileParty.MainParty.LastVisitedSettlement.Town, -10)
		});
		incident5.AddOption("{=z6vCG954}Give the lepers some money and food on the condition that they try to find a more welcoming town somewhere else", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Calculating, 100),
			IncidentEffect.TraitChange(DefaultTraits.Mercy, 100),
			IncidentEffect.GoldChange(() => -100)
		});
		incident5.AddOption("{=egSS7oyV}Drive the lepers away, telling them they must have sinned grievously to be so afflicted", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Mercy, -100),
			IncidentEffect.SettlementRelationChange(() => MobileParty.MainParty.LastVisitedSettlement, 10)
		});
		incident5.AddOption("{=zcMJEpHC}Don't get involved", new List<IncidentEffect> { IncidentEffect.TraitChange(DefaultTraits.Valor, -100) });
		Incident incident6 = RegisterIncident("incident_veterans_demand_privileges", "{=j8Aq3lRW}Veterans demand privileges", "{=FbKc1AwE}Several of your newer recruits approach you. Your {HIGH_TIER_TROOPS} have been demanding the freshest cuts of meat and highest quality bread from your stores, letting them make do with gristle and stale crusts. The veterans say that this is the privilege of experience, and that it will motivate your recruits to improve themselves.", IncidentTrigger.LeavingSettlement, IncidentType.PartyCampLife, CampaignTime.Years(1000f), delegate(TextObject description)
		{
			CharacterObject characterObject = GetVeteranMaxTierTroop();
			if (characterObject == null)
			{
				return false;
			}
			description.SetTextVariable("HIGH_TIER_TROOPS", characterObject.Name);
			return PartyBase.MainParty.ItemRoster.Any((ItemRosterElement x) => !x.IsEmpty && x.EquipmentElement.Item == DefaultItems.Meat);
		});
		incident6.AddOption("{=56hS9BRG}Admonish the {HIGH_TIER_TROOPS} for their indiscipline, even though they will be humiliated and may even desert", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Honor, 100),
			IncidentEffect.MoraleChange(5f),
			IncidentEffect.ChangeTroopAmount(GetVeteranMaxTierTroop, -1).WithChance(0.5f)
		}, delegate(TextObject text)
		{
			CharacterObject characterObject = GetVeteranMaxTierTroop();
			text.SetTextVariable("HIGH_TIER_TROOPS", characterObject.Name);
			return true;
		});
		incident6.AddOption("{=EAu32UBG}Tell your recruits that your veterans have indeed earned the right to the best food", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Mercy, -100),
			IncidentEffect.TraitChange(DefaultTraits.Generosity, 100)
		});
		incident6.AddOption("{=37YTPzfG}Tell your men that they have all earned the right to your esteem, and break out an extra ration of {FOOD_OR_DRINK_ITEM} to show your appreciation", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Generosity, 100),
			IncidentEffect.MoraleChange(5f),
			IncidentEffect.ChangeItemAmount(GetVeteranFoodItem, () => -1)
		}, delegate(TextObject text)
		{
			ItemObject itemObject = GetVeteranFoodItem();
			text.SetTextVariable("FOOD_OR_DRINK_ITEM", itemObject.Name);
			return true;
		});
		incident6.AddOption("{=USdND5SY}You propose a compromise - a slightly better ration for veteran troops - knowing it will avoid a fight but come across as indecisive.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Calculating, -100),
			IncidentEffect.TraitChange(DefaultTraits.Mercy, 100)
		});
		Incident incident7 = RegisterIncident("incident_an_affair_of_honor", "{=gUKUisS6}An Affair of Honor", "{=akvIjedh}Two of your cavalrymen request permission to fight a duel. Both had been wooing a merchant's daughter in {TOWN}, and one slighted the other in front of her. Some commanders encourage their men to duel as a boost to their fiery sense of pride, but others think that such fights erode their discipline as soldiers and, of course, carry the risk of injury.", IncidentTrigger.LeavingVillage, IncidentType.PartyCampLife, CampaignTime.Years(1000f), delegate(TextObject description)
		{
			if (MobileParty.MainParty.LastVisitedSettlement == null)
			{
				return false;
			}
			bool num = (from x in MobileParty.MainParty.MemberRoster.GetTroopRoster()
				where x.Character.Tier >= 4 && x.Character.HasMount() && !x.Character.IsHero
				select x).Sum((TroopRosterElement x) => x.Number) >= 5;
			bool flag = MobileParty.MainParty.MemberRoster.GetTroopRoster().Count((TroopRosterElement x) => x.Character.Tier >= 4 && x.Character.HasMount() && !x.Character.IsHero) >= 2;
			if (!num || !flag)
			{
				return false;
			}
			description.SetTextVariable("TOWN", MobileParty.MainParty.LastVisitedSettlement.Name);
			return true;
		});
		incident7.AddOption("{=24pUil64}You like that fiery spirit! Let them fight until one falls and cannot rise", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Valor, 200),
			IncidentEffect.TraitChange(DefaultTraits.Calculating, -100),
			IncidentEffect.PartyExperienceChance(200),
			IncidentEffect.WoundTroop(GetFirstNobleTroop, 1).WithChance(0.5f),
			IncidentEffect.KillTroop(GetSecondNobleTroop, 1).WithChance(0.5f)
		});
		incident7.AddOption("{=37lEhqtv}Let them fight until the first drop of blood is shed", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Valor, 100),
			IncidentEffect.PartyExperienceChance(100),
			IncidentEffect.WoundTroop(GetFirstNobleTroop, 1).WithChance(0.5f),
			IncidentEffect.WoundTroop(GetSecondNobleTroop, 1).WithChance(0.5f)
		});
		incident7.AddOption("{=cexRoZPF}You will allow no such foolishness. Save their anger for the enemy", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Valor, -100),
			IncidentEffect.TraitChange(DefaultTraits.Calculating, 100)
		});
		incident7.AddOption("{=ZYopfLDg}Tell your men to throw a die, leaving the matter to fate.", new List<IncidentEffect> { IncidentEffect.TraitChange(DefaultTraits.Calculating, -100) });
		Incident incident8 = RegisterIncident("incident_troops_fight_over_insult", "{=4mC7AIPk}Troops Fight over Insult", "{=HpkqVXQ3}A fight breaks out in your camp. A high-born {CAVALRYMAN} demanded a {FOOTMAN} water his horse, even though the {FOOTMAN} is a veteran nearly twice the {CAVALRYMAN}'s age. The two then exchanged insults, and now the {FOOTMAN} demands a duel while the {CAVALRYMAN}, who is only willing to duel his social equal, demands the {FOOTMAN} be told to watch his place.", IncidentTrigger.LeavingVillage, IncidentType.PartyCampLife, CampaignTime.Years(1000f), delegate(TextObject description)
		{
			CharacterObject characterObject = GetInsultCavalryman();
			CharacterObject characterObject2 = GetInsultFootman();
			if (characterObject2 == null || characterObject == null)
			{
				return false;
			}
			description.SetTextVariable("CAVALRYMAN", characterObject.Name);
			description.SetTextVariable("FOOTMAN", characterObject2.Name);
			return MobileParty.MainParty.MemberRoster.GetTroopRoster().Any(CavalryTroopsPredicate) && MobileParty.MainParty.MemberRoster.GetTroopRoster().Any(FootmenTroopsPredicate);
		});
		incident8.AddOption("{=QGcozvvK}Only blood can wipe out an insult! Let the two have it out, until one falls and cannot rise", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Valor, 200),
			IncidentEffect.TraitChange(DefaultTraits.Calculating, -100),
			IncidentEffect.Select(IncidentEffect.Select(IncidentEffect.KillTroopsRandomly(FootmenTroopsPredicate, () => 1), IncidentEffect.WoundTroop(GetInsultCavalryman, 1), 0.5f), IncidentEffect.Select(IncidentEffect.KillTroopsRandomly(CavalryTroopsPredicate, () => 1), IncidentEffect.WoundTroop(GetInsultFootman, 1), 0.5f), 0.5f),
			IncidentEffect.PartyExperienceChance(200)
		});
		incident8.AddOption("{=HbQDtv60}Reprimand the cavalryman, even though he is fiery and might desert", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Valor, -100),
			IncidentEffect.TraitChange(DefaultTraits.Calculating, 100),
			IncidentEffect.MoraleChange(5f),
			IncidentEffect.KillTroopsRandomly(CavalryTroopsPredicate, () => 1).WithChance(0.25f)
		});
		incident8.AddOption("{=7YCEpRij}Reprimand the footman, who you know will grumble but will have the sense to swallow his pride", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Calculating, 100),
			IncidentEffect.MoraleChange(-5f)
		});
		incident8.AddOption("{=OGDApTnl}Tell them both to stop behaving like fools", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Valor, -100),
			IncidentEffect.TraitChange(DefaultTraits.Calculating, 100)
		});
		Incident incident9 = RegisterIncident("incident_arrow_proofing", "{=JKO2Rmlr}Arrow-proofing", "{=ZCqZvb7K}As you leave the village, one of your soldiers proudly shows you a magic oil he bought from a peddlar, who told him that, if he annoints himself with it, it will prevent him from being wounded in battle. ", IncidentTrigger.LeavingVillage, IncidentType.TroopSettlementRelation, CampaignTime.Days(60f), (TextObject description) => PartyBase.MainParty.MemberRoster.TotalRegulars >= 5);
		incident9.AddOption("{=CNYKy7q3}Congratulate the soldier on his wise purchase, and tell him that you expect to see him in the thick of the fray in the next battle", new List<IncidentEffect>
		{
			IncidentEffect.MoraleChange(5f),
			IncidentEffect.TraitChange(DefaultTraits.Honor, -100),
			IncidentEffect.TraitChange(DefaultTraits.Calculating, 100)
		});
		incident9.AddOption("{=CXZxDvry}Tell your soldiers they should rely on their own strength instead of magic", new List<IncidentEffect> { IncidentEffect.TraitChange(DefaultTraits.Valor, 100) });
		incident9.AddOption("{=NHwt84uk}Chastise your man for being so gullible", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Honor, 100),
			IncidentEffect.MoraleChange(-5f)
		});
		Incident incident10 = RegisterIncident("incident_purebred_horse", "{=eWh4BDkx}Purebred Horse", "{=J9HLJpk7}As you leave the village, one of your cavalry recruits proudly presents to you a new horse he purchased. Maybe it's not the fastest beast on four legs, he says, but it's strong and spirited - he saw it fighting another of the trader's horses. You run an eye over the beast and see that your man was sold a mule, its ears cleverly cropped to resemble a horse's. The creature is strong enough but far too stubborn to serve as a cavalry mount. You know that your man will be the laughingstock of your company for weeks.", IncidentTrigger.LeavingVillage, IncidentType.TroopSettlementRelation, CampaignTime.Years(1000f), (TextObject description) => MobileParty.MainParty.ItemRoster.Any((ItemRosterElement x) => x.EquipmentElement.Item.HasHorseComponent));
		incident10.AddOption("{=DHcl6JU5}Give him one of your own horses, add the mule to your pack train, and tell him to talk to you before he makes any more purchases", new List<IncidentEffect>
		{
			IncidentEffect.ChangeItemAmount(GetPurebredHorseItem, () => -1),
			IncidentEffect.ChangeItemAmount(() => Game.Current.ObjectManager.GetObject<ItemObject>("mule"), () => 1),
			IncidentEffect.TraitChange(DefaultTraits.Generosity, 100)
		});
		incident10.AddOption("{=3uHIXyDk}Your men could use a laugh. Let the recruit show off his prize to his mates and take the humiliation, even if it's enough to drive him to desert", new List<IncidentEffect>
		{
			IncidentEffect.MoraleChange(5f),
			IncidentEffect.KillTroopsRandomly((TroopRosterElement x) => x.Character.HasMount() && !x.Character.IsHero && !x.Character.IsHero, () => 1).WithChance(0.25f)
		});
		incident10.AddOption("{=HXVVJYQr}Tell him curtly what he did and that he needs to go get his old mount back from the trader, even if it costs him a few denars", new List<IncidentEffect>
		{
			IncidentEffect.MoraleChange(-2f),
			IncidentEffect.TraitChange(DefaultTraits.Honor, 100)
		});
		Incident incident11 = RegisterIncident("incident_veteran_mentor", "{=1jmYaNxu}Veteran Mentor", "{=CObK3sDa}As you leave the village, a greybeard walks up to you carrying a cudgel. Although he's too old to fight, he says, he was a warrior once and claims to know a trick move, a way of suddenly closing with your opponent and tripping them with a spear, which he offers to teach to your troops for a price. He shows you his battle-scars, which are indeed impressive.", IncidentTrigger.LeavingVillage, IncidentType.TroopSettlementRelation, CampaignTime.Years(1000f), (TextObject description) => true);
		incident11.AddOption("{=aME3jqrO}Tell him that tricks don't win battles, but bravery does.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Valor, 100),
			IncidentEffect.MoraleChange(5f)
		});
		incident11.AddOption("{=43GeTG0K}Accept, even if it costs you time, recognizing that a warrior who survived so many battles probably knows a thing or two.", new List<IncidentEffect>
		{
			IncidentEffect.GoldChange(() => -500),
			IncidentEffect.PartyExperienceChance(200),
			IncidentEffect.DisorganizeParty()
		});
		incident11.AddOption("{=JvUb1wr4}Accept, and tell him to demonstrate his technique with a sword on one of your men, so that the lesson sticks.", new List<IncidentEffect>
		{
			IncidentEffect.GoldChange(() => -500),
			IncidentEffect.PartyExperienceChance(400),
			IncidentEffect.TraitChange(DefaultTraits.Calculating, -100),
			IncidentEffect.WoundTroopsRandomly(1)
		});
		Incident incident12 = RegisterIncident("incident_horsefly_plague", "{=S4YyXUPU}Horsefly Plague", "{=NJ0BBaaC}One of your cavalry commanders reported that some horses seem mysteriously weak. You see their listless eyes and swollen bellies and recognize it - swamp fever. This plague is spread by horseflies, and it will infect other animals if the horse is not killed", IncidentTrigger.LeavingVillage, IncidentType.AnimalIllness, CampaignTime.Days(60f), (TextObject description) => MobileParty.MainParty.ItemRoster.Where((ItemRosterElement x) => x.EquipmentElement.Item.HasHorseComponent).Sum((ItemRosterElement x) => x.Amount) >= 4);
		incident12.AddOption("{=SIVBGe7H}Isolate the infected horses immediately to prevent the spread ", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Valor, -100),
			IncidentEffect.TraitChange(DefaultTraits.Calculating, 50),
			IncidentEffect.ChangeItemsAmount(GetHorseflyPlagueHorseItems, -1),
			IncidentEffect.ChangeItemsAmount(GetHorseflyPlagueHorseItems, -3).WithChance(0.3f),
			IncidentEffect.DisorganizeParty()
		});
		incident12.AddOption("{=A4voELZr}Order the disposal of the infected horses to avoid further contamination.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Calculating, 100),
			IncidentEffect.TraitChange(DefaultTraits.Mercy, -100),
			IncidentEffect.ChangeItemsAmount(GetHorseflyPlagueHorseItems, -2)
		});
		incident12.AddOption("{=ZCWziaI4}Ignore the outbreak and continue marching", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Calculating, -100),
			IncidentEffect.ChangeItemsAmount(GetHorseflyPlagueHorseItems, -1),
			IncidentEffect.ChangeItemsAmount(GetHorseflyPlagueHorseItems, -3).WithChance(0.8f)
		});
		Incident incident13 = RegisterIncident("incident_glanders_outbreak", "{=QjWhMbL1}Glanders Outbreak", "{=xH4KUzVS}As you leave town, you notice one of your horses has a swollen head and a bloody nose. You recognize this disease. It can spread to your men, and not only this animal is at risk but every other animal that drank from the same trough.", IncidentTrigger.LeavingVillage, IncidentType.AnimalIllness, CampaignTime.Days(60f), (TextObject description) => MobileParty.MainParty.MemberRoster.GetTroopRoster().Where(TroopsToDemotePredicate).Sum((TroopRosterElement x) => x.Number) >= 5);
		incident13.AddOption("{=AIABabCI}Kill the horse and the two other horses it shared a stable with.", new List<IncidentEffect>
		{
			IncidentEffect.DemoteTroopsRandomlyWithPredicate(TroopsToDemotePredicate, TroopToDemoteToPredicate, 3),
			IncidentEffect.TraitChange(DefaultTraits.Calculating, 100),
			IncidentEffect.TraitChange(DefaultTraits.Valor, -100),
			IncidentEffect.TraitChange(DefaultTraits.Mercy, -100)
		});
		incident13.AddOption("{=IBIbetoC}Kill the horse alone, hoping for the best.", new List<IncidentEffect>
		{
			IncidentEffect.DemoteTroopsRandomlyWithPredicate(TroopsToDemotePredicate, TroopToDemoteToPredicate, 1),
			IncidentEffect.Group(IncidentEffect.DemoteTroopsRandomlyWithPredicate(TroopsToDemotePredicate, TroopToDemoteToPredicate, 3), IncidentEffect.KillTroopsRandomly((TroopRosterElement x) => true, () => 2)).WithChance(0.25f)
		});
		incident13.AddOption("{=bWz49lZu}Allow the cavalryman to care for the animal, even though it puts him and others at risk.", new List<IncidentEffect> { IncidentEffect.Select(IncidentEffect.TraitChange(DefaultTraits.Mercy, 100), IncidentEffect.Group(IncidentEffect.DemoteTroopsRandomlyWithPredicate(TroopsToDemotePredicate, TroopToDemoteToPredicate, 3), IncidentEffect.KillTroopsRandomly((TroopRosterElement x) => true, () => 2), IncidentEffect.TraitChange(DefaultTraits.Calculating, -200)).WithChance(0.25f), 0.5f) });
		Incident incident14 = RegisterIncident("incident_colicky_horses", "{=k4kbVxTL}Colicky Horses", "{=Bi8fKaGw}As you leave town, one of your cavalrymen finds his mount lying on the ground, whining in pain and refusing to get up. You're sure that he was careless and fed the animal too quickly after your arrival, as he was eager to get to the town's tavern.", IncidentTrigger.LeavingTown, IncidentType.AnimalIllness, CampaignTime.Years(1000f), (TextObject description) => MobileParty.MainParty.MemberRoster.GetTroopRoster().Where(TroopToDemotePredicate).Sum((TroopRosterElement x) => x.Number) >= 15);
		incident14.AddOption("{=bNTKEu9p}Provide what care you can, even though it will slow you down and there is no sure chance of success", new List<IncidentEffect>
		{
			IncidentEffect.DisorganizeParty(),
			IncidentEffect.DemoteTroopsRandomlyWithPredicate(TroopToDemotePredicate, TroopToDemoteToPredicate2, 1).WithChance(0.5f)
		});
		incident14.AddOption("{=9yFSbR36}Whip the responsible cavalryman for his negligence, and let him buy his own remount.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Calculating, 100),
			IncidentEffect.TraitChange(DefaultTraits.Generosity, -100)
		});
		incident14.AddOption("{=qRZpuLae}End the animal's misery quickly, and tell the horseman that you will not trust another poor beast to his care.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Mercy, 100),
			IncidentEffect.DemoteTroopsRandomlyWithPredicate(TroopToDemotePredicate, TroopToDemoteToPredicate2, 1)
		});
		Incident incident15 = RegisterIncident("incident_camp_fever", "{=l2rWAm4N}Camp Fever", "{=Q5MXehRY}As you break camp, you find that two of your men are bathed in sweat and cannot rouse themselves. They are confused, and one is starting to show pock-marks on his chest. You recognize the symptoms as camp fever, and fear it will spread quickly. Their companions want to tend to them, but you know that if you don't quarantine the men the disease is likely to spread. ", IncidentTrigger.LeavingEncounter, IncidentType.Illness, CampaignTime.Days(60f), (TextObject description) => !MobileParty.MainParty.IsCurrentlyAtSea && MobileParty.MainParty.MemberRoster.TotalRegulars >= 50);
		incident15.AddOption("{=htMJ1uiy}Load the sick onto a cart and give them water, but otherwise let their comrades have as little contact with them as possible. ", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Calculating, 100),
			IncidentEffect.MoraleChange(5f),
			IncidentEffect.WoundTroopsRandomlyWithChanceOfDeath(2, 0.25f)
		});
		incident15.AddOption("{=4v8LpVb8}Send some men to fetch a doctor and medicine, accepting that it will delay your march", new List<IncidentEffect>
		{
			IncidentEffect.GoldChange(() => -200),
			IncidentEffect.TraitChange(DefaultTraits.Generosity, 100),
			IncidentEffect.TraitChange(DefaultTraits.Valor, -100),
			IncidentEffect.DisorganizeParty()
		});
		incident15.AddOption("{=c5uy0bb9}Let your men treat the sick as you march", new List<IncidentEffect>
		{
			IncidentEffect.KillTroopsRandomly((TroopRosterElement x) => true, () => 1).WithChance(0.1f),
			IncidentEffect.WoundTroopsRandomly(2).WithChance(0.6f)
		});
		Incident incident16 = RegisterIncident("incident_bloody_flux", "{=LEHxD9ik}Bloody Flux", "{=3FwbFdbw}As you inspect your siege camp, you come across disgusting evidence that one or more of your men has contracted the bloody flux. It will pass from man to man as they share food and water, and you know what it can do to a besieging army if left unchecked.", IncidentTrigger.DuringSiege, IncidentType.Illness, CampaignTime.Years(1000f), (TextObject description) => MobileParty.MainParty.MemberRoster.TotalRegulars >= 70);
		incident16.AddOption("{=MSARXv6p}Identify the sick and make them camp outside your palisades, tending to themselves.", new List<IncidentEffect>
		{
			IncidentEffect.WoundTroopsRandomlyWithChanceOfDeath(0.05f, 0.5f),
			IncidentEffect.TraitChange(DefaultTraits.Mercy, -100),
			IncidentEffect.TraitChange(DefaultTraits.Calculating, 100)
		});
		incident16.AddOption("{=Qnb8wrL0}Treat your men but trust in strict preventative measures, like digging latrines outside camp and forcing your men to go far afield for water, that they are likely to resent.", new List<IncidentEffect>
		{
			IncidentEffect.WoundTroopsRandomlyWithChanceOfDeath(3, 0.25f),
			IncidentEffect.WoundTroopsRandomlyWithChanceOfDeath(5, 0.25f).WithChance(0.5f),
			IncidentEffect.TraitChange(DefaultTraits.Calculating, 100),
			IncidentEffect.TraitChange(DefaultTraits.Generosity, -100)
		});
		incident16.AddOption("{=zdPb0vtU}Treat the sick and make no additional demands on your men, trusting in luck to see you through this.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Calculating, -100),
			IncidentEffect.WoundTroopsRandomlyWithChanceOfDeath(0.1f, 0.3f)
		});
		Incident incident17 = RegisterIncident("incident_lice_and_fleas", "{=CX8WdvvR}Lice and Fleas", "{=b4BaOUKn}Lice are part of military life but several of your men are absolutely teaming with the vermin. Perhaps they are especially virulent in these parts, or perhaps they thrive in this weather. You fear that they will spread camp fever or other diseases.", IncidentTrigger.LeavingEncounter, IncidentType.Illness, CampaignTime.Days(60f), (TextObject description) => !MobileParty.MainParty.IsCurrentlyAtSea && MobileParty.MainParty.MemberRoster.TotalRegulars >= 45);
		incident17.AddOption("{=ybnqAVbP}Force the afflicted to shave every strand of their hair and beard", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Calculating, 100),
			IncidentEffect.MoraleChange(-5f)
		});
		incident17.AddOption("{=PAW2b2Y2}Require your men to go through the laborious process of picking clean their bodies and clothes", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Mercy, 100),
			IncidentEffect.DisorganizeParty()
		});
		incident17.AddOption("{=irLNaJ3m}Tell your men to give themselves a good scratch and keep marching", new List<IncidentEffect>
		{
			IncidentEffect.WoundTroopsRandomly(3).WithChance(0.5f),
			IncidentEffect.TraitChange(DefaultTraits.Calculating, -100)
		});
		Incident incident18 = RegisterIncident("incident_saint_of_the_woods", "{=SqpOlZhl}Saint of the Woods", "{=RrHS8JOx}As you depart the scene of your victory, a man dressed in white robes emerges from a nearby wood. He tells you that you owe your victory to the intervention of Hageos Tristamos, a holy man from a century ago who assists those who fight in a noble cause, and asks that you donate half the loot to the local shrine in gratitude, where it will be used to feed the poor.", IncidentTrigger.LeavingBattle, IncidentType.Illness, CampaignTime.Years(1000f), (TextObject description) => MobileParty.MainParty.MemberRoster.TotalWoundedRegulars >= 3);
		incident18.AddOption("{=4dIYUMkA}Donate the spoils of battle to the saint's shrine", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Mercy, 100),
			IncidentEffect.GoldChange(() => -300)
		});
		incident18.AddOption("{=Ioc2z1Ap}Declare that you owe your victory to no saint but to your men, and give them the silver", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Generosity, 200),
			IncidentEffect.TraitChange(DefaultTraits.Calculating, -100),
			IncidentEffect.MoraleChange(3f),
			IncidentEffect.GoldChange(() => -300)
		});
		incident18.AddOption("{=2auYPXJ8}Declare that you owe your victory to your own right arm, and keep the silver", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Generosity, -200),
			IncidentEffect.GoldChange(() => 300)
		});
		incident18.AddOption("{=NLbvOuFt}Fall on your knees in tears, repent of your sins, and leave a huge donation", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Calculating, -100),
			IncidentEffect.TraitChange(DefaultTraits.Mercy, 100),
			IncidentEffect.RenownChange(5f),
			IncidentEffect.GoldChange(() => -500)
		});
		Incident incident19 = RegisterIncident("incident_servants_of_mercy", "{=FYvrShj3}Servants of Mercy", "{=t0iKT1VJ}As the dust of battle settles, you see a group of women tending to the wounded. They explain that they are the Servants of Hope, mendicant sisters pledged to assist the afflicted in the hope of winning mercy from Heaven. They offer to accompany your party for a few days and tend to your men's wounds, asking nothing in return.", IncidentTrigger.LeavingBattle, IncidentType.Illness, CampaignTime.Years(1000f), (TextObject description) => MobileParty.MainParty.MemberRoster.TotalWoundedRegulars >= 3);
		incident19.AddOption("{=5ByLt1QP}Tell the Sisters that they will slow you down, and your men can heal on their own.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Valor, 100),
			IncidentEffect.TraitChange(DefaultTraits.Generosity, -100)
		});
		incident19.AddOption("{=STLwVkdO}Accept the Sisters' help", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Mercy, 100),
			IncidentEffect.DisorganizeParty(),
			IncidentEffect.HealTroopsRandomly(3)
		});
		Incident incident20 = RegisterIncident("incident_right_of_pillage", "{=p0bMRIum}Right of Pillage", "{=NWJ87xjB}As you prepare to march forth from newly captured {TOWN_NAME}, your men stop you. They shed their blood for your wars and you denied them their customary right to sack it. They say that it you will not let them enrich themselves from the enemy, then you should enrich them from your purse", IncidentTrigger.LeavingTown | IncidentTrigger.LeavingCastle, IncidentType.PartyCampLife, CampaignTime.Days(60f), delegate(TextObject description)
		{
			if (MobileParty.MainParty.LastVisitedSettlement == null)
			{
				return false;
			}
			BesiegeSettlementLogEntry besiegeSettlementLogEntry = FindLastBesiegeSettlementLogEntry();
			if (besiegeSettlementLogEntry == null)
			{
				return false;
			}
			SiegeAftermathLogEntry siegeAftermathLogEntry = FindLastSiegeAftermathLogEntry(besiegeSettlementLogEntry);
			if (siegeAftermathLogEntry == null)
			{
				return false;
			}
			if (siegeAftermathLogEntry.GameTime < besiegeSettlementLogEntry.GameTime)
			{
				return false;
			}
			description.SetTextVariable("TOWN_NAME", MobileParty.MainParty.LastVisitedSettlement.Name);
			return PartyBase.MainParty.MemberRoster.TotalRegulars >= 5;
		});
		incident20.AddOption("{=lm37mXGH}Let your men return and sack the town while it is still defenseless", new List<IncidentEffect> { IncidentEffect.Custom(null, delegate
		{
			BesiegeSettlementLogEntry besiegeSettlementLogEntry = FindLastBesiegeSettlementLogEntry();
			if (besiegeSettlementLogEntry?.OwnerClanBeforeBesiege == null || besiegeSettlementLogEntry.OwnerClanBeforeBesiege.IsEliminated)
			{
				return new List<TextObject>();
			}
			SiegeAftermathAction.ApplyAftermath(MobileParty.MainParty, MobileParty.MainParty.LastVisitedSettlement, SiegeAftermathAction.SiegeAftermath.Pillage, besiegeSettlementLogEntry.OwnerClanBeforeBesiege, new Dictionary<MobileParty, float>());
			return new List<TextObject>();
		}, (IncidentEffect effect) => new List<TextObject>
		{
			new TextObject("{=6QuPiTsY}Consequences of a town sacked")
		}) });
		incident20.AddOption("{=MLIyVwfx}Pay them yourself", new List<IncidentEffect>
		{
			IncidentEffect.GoldChange(() => -1000),
			IncidentEffect.TraitChange(DefaultTraits.Mercy, 100),
			IncidentEffect.TraitChange(DefaultTraits.Generosity, 100),
			IncidentEffect.TraitChange(DefaultTraits.Calculating, -100)
		});
		incident20.AddOption("{=HG7WWmX8}Punish the troublemakers for sewing mutiny", new List<IncidentEffect> { IncidentEffect.MoraleChange(-20f) });
		incident20.AddOption("{=tK6IkgCK}Tell your men that Heaven expects them to show mercy to the innocent", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Mercy, 200),
			IncidentEffect.TraitChange(DefaultTraits.Generosity, -100),
			IncidentEffect.MoraleChange(-10f)
		});
		Incident incident21 = RegisterIncident("incident_donative_demand", "{=p8ESONLB}Donative Demand", "{=qaGbN6wX}It has not escaped your men’s notice that you have accumulated a fair amount of money in your treasury. A delegation approaches you and suggests that this would be a good time for the traditional donative, as Calradian emperors of old were known to give their legions.", IncidentTrigger.LeavingSettlement, IncidentType.PartyCampLife, CampaignTime.Days(60f), (TextObject description) => Hero.MainHero.Gold >= 5000 && MobileParty.MainParty.MemberRoster.TotalRegulars >= 20);
		incident21.AddOption("{=Z7RAahk3}Convince your men that the money would be better spent on supplies and recruits to ensure their survival", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Calculating, 100),
			IncidentEffect.TraitChange(DefaultTraits.Generosity, -100),
			IncidentEffect.MoraleChange(-2f)
		});
		incident21.AddOption("{=YOhKn6YV}Tell your men that they can do as they wish with their wages, but you are not in the habit of subsidizing wine vendors, women of questionable virtue and gambling sharks more than necessary", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Honor, 100),
			IncidentEffect.TraitChange(DefaultTraits.Generosity, -100),
			IncidentEffect.MoraleChange(-2f)
		});
		incident21.AddOption("{=ocsX04bn}\"I am a river of wealth unto you, my brave warriors!\"", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Generosity, 100),
			IncidentEffect.MoraleChange(5f),
			IncidentEffect.GoldChange(() => -500)
		});
		incident21.AddOption("{=NrmCF6qb}Erupt in a towering rage, so they will think twice about bothering you again", new List<IncidentEffect>
		{
			IncidentEffect.MoraleChange(-5f),
			IncidentEffect.TraitChange(DefaultTraits.Calculating, -100)
		});
		Incident incident22 = RegisterIncident("incident_vedmaks_treasure", "{=6bF0LqJo}Vedmak's Treasure", "{=tfckGAIA}As you prepare to leave, one of your men says the he was approached by a villager. There is a particularly ill-tempered vedmak on the edge of town who makes his living not from healing or love-potions but from black magic, and who threatens to curse villagers unless they pay him. They asked your men to drive the vedmak away and burn his hut, and told them that they can have his ill-gotten gains.", IncidentTrigger.LeavingVillage, IncidentType.TroopSettlementRelation, CampaignTime.Years(1000f), (TextObject description) => MobileParty.MainParty.LastVisitedSettlement != null && MobileParty.MainParty.LastVisitedSettlement.MapFaction.StringId == "sturgia" && MobileParty.MainParty.LastVisitedSettlement.IsVillage && PartyBase.MainParty.MemberRoster.TotalRegulars >= 5);
		incident22.AddOption("{=byocyGXf}Convince your men that those who anger with vedmaks are likely to see their nose shrivel up or their fingers fall off", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Calculating, 200),
			IncidentEffect.TraitChange(DefaultTraits.Honor, -100)
		});
		incident22.AddOption("{=8gEBPabt}Let them go and demand they split their haul with you", new List<IncidentEffect>
		{
			IncidentEffect.SettlementRelationChange(() => MobileParty.MainParty.LastVisitedSettlement, 5),
			IncidentEffect.MoraleChange(5f),
			IncidentEffect.TraitChange(DefaultTraits.Mercy, -100),
			IncidentEffect.GoldChange(() => 200)
		});
		incident22.AddOption("{=3zP80NBJ}Tell them that village disputes are a matter for a proper inquest by the authorities", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Honor, 100),
			IncidentEffect.MoraleChange(-5f)
		});
		Incident incident23 = RegisterIncident("incident_the_deer_departed", "{=IG19KaLK}The Deer Departed", "{=oxBlBLWF}You noticed that the deer around this Khuzait village are unusually unafraid of humans. One of your men manages to shoot one. The villagers approach him afterwards and beg him not to eat the animal. Their former shaman transformed himself into a deer some years ago, they say, and while they don't know if the animal your man killed is him or not, they want to perform the burial rights properly, just to be sure.", IncidentTrigger.LeavingVillage, IncidentType.HuntingForaging, CampaignTime.Years(1000f), (TextObject description) => MobileParty.MainParty.LastVisitedSettlement?.MapFaction.StringId == "khuzait" && MobileParty.MainParty.MemberRoster.GetTroopRoster().Any((TroopRosterElement x) => !x.Character.IsHero && x.Character.IsRanged));
		incident23.AddOption("{=xJ0FKCtA}Let the villagers take the deer, even though your men scoff at your superstition", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Mercy, 100),
			IncidentEffect.TraitChange(DefaultTraits.Calculating, -100),
			IncidentEffect.MoraleChange(-5f),
			IncidentEffect.SettlementRelationChange(() => MobileParty.MainParty.LastVisitedSettlement, 5)
		});
		incident23.AddOption("{=p8yBnCG2}Let your men enjoy their meal, but commend the villagers for making a good attempt to get their hands on a piece of perfectly good meat", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Mercy, -100),
			IncidentEffect.TraitChange(DefaultTraits.Calculating, 100),
			IncidentEffect.MoraleChange(5f),
			IncidentEffect.ChangeItemAmount(() => Game.Current.ObjectManager.GetObject<ItemObject>("meat"), () => 3),
			IncidentEffect.SettlementRelationChange(() => MobileParty.MainParty.LastVisitedSettlement, -5)
		});
		incident23.AddOption("{=0wRiev5G}Show genuine remorse and participate in the burial rites for the deer, just in case, even if your troops make comments behind your back", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Calculating, -100),
			IncidentEffect.TraitChange(DefaultTraits.Mercy, 100),
			IncidentEffect.MoraleChange(-5f),
			IncidentEffect.SettlementRelationChange(() => MobileParty.MainParty.LastVisitedSettlement, 20)
		});
		Incident incident24 = RegisterIncident("incident_wedding_celebration", "{=G16Nlaaq}Wedding Celebration", "{=dkwLMXDz}As you break camp, several of your veterans come forward, somewhat sheepishly. They push one of their number forward and say that he married a young lass at the last village that you visited. They ask if they can share one of the amphorae of wine that they've noticed you are carrying in celebration.", IncidentTrigger.LeavingEncounter, IncidentType.TroopSettlementRelation, CampaignTime.Years(1000f), (TextObject description) => !MobileParty.MainParty.IsCurrentlyAtSea && MobileParty.MainParty.MemberRoster.TotalRegulars >= 10);
		incident24.AddOption("{=Edn4syjZ}Indulge your men, even though you have little doubt that the wedding is an invention", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Generosity, 200),
			IncidentEffect.TraitChange(DefaultTraits.Calculating, -100),
			IncidentEffect.MoraleChange(5f),
			IncidentEffect.ChangeItemAmount(() => Game.Current.ObjectManager.GetObject<ItemObject>("wine"), () => -1)
		});
		incident24.AddOption("{=u514BSH6}Tell them that you will not reward deceit", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Honor, 200),
			IncidentEffect.TraitChange(DefaultTraits.Generosity, -100)
		});
		incident24.AddOption("{=axNOvMcw}Pour all your wine onto the ground, telling them that you cannot tolerate the presence of anything that erodes their morals and discipline", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Honor, 100),
			IncidentEffect.TraitChange(DefaultTraits.Calculating, -100),
			IncidentEffect.MoraleChange(5f)
		});
		Incident incident25 = RegisterIncident("incident_kannic_splendors", "{=msK9baMp}Kannic Splendors", "{=a1cuuevH}One of your men comes up to you excitedly. A peddler in the marketplace sold him a map to a treasure buried by a long-dead Kannic sorceror in a wadi near here. He wants a bit of time to check it out and promises you a fifth share of anything he finds. You've heard tales of gold found in ancient tombs near here, but in this case you'd give a hundred to one odds that this leads to either a grave that was pillaged centuries ago or someone's underground irrigation cistern, and at any rate the locals won't appreciate your man smashing things up with pick and shovel.", IncidentTrigger.LeavingTown, IncidentType.TroopSettlementRelation, CampaignTime.Years(1000f), (TextObject description) => MobileParty.MainParty.LastVisitedSettlement?.MapFaction.StringId == "aserai" && PartyBase.MainParty.MemberRoster.TotalRegulars >= 5);
		incident25.AddOption("{=12bIzNkB}Tell him that no one sells maps to treasures that they could just as easily dig up themselves", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Honor, 100),
			IncidentEffect.MoraleChange(-5f)
		});
		incident25.AddOption("{=bi8UfCdg}Indulge your man, and wait for his to complete his errand", new List<IncidentEffect>
		{
			IncidentEffect.DisorganizeParty(),
			IncidentEffect.TraitChange(DefaultTraits.Generosity, 100),
			IncidentEffect.SettlementRelationChange(() => MobileParty.MainParty.LastVisitedSettlement, -10),
			IncidentEffect.GoldChange(() => 1000).WithChance(0.01f)
		});
		incident25.AddOption("{=KQhA1bQq}Tell him that the Kannic sorcerors sealed jinn into their treasure chambers with the power to turn a man's skin inside-out, and you couldn't possibly let him go", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Calculating, 200),
			IncidentEffect.TraitChange(DefaultTraits.Honor, -100)
		});
		Incident incident26 = RegisterIncident("incident_solider_is_wanted_criminal", "{=eN5vqUuF}Soldier is Wanted Criminal", "{=z4RgNBkq}On your way out of the village, a delegation led by the elder approaches you. They point to a {TROOP_TYPE} in your ranks - a good warrior, but none too outgoing. They say he used to farm in this village, but fled after he killed his neighbor in a quarrel over boundary stones. The accused snaps back with a sudden vehemence - his victim was a cheat, and deserved what he got. Under the law, murder requires the death penalty, though custom allows blood money to be paid instead.", IncidentTrigger.LeavingVillage, IncidentType.TroopSettlementRelation, CampaignTime.Years(1000f), delegate(TextObject description)
		{
			if (MobileParty.MainParty.LastVisitedSettlement == null)
			{
				return false;
			}
			CharacterObject characterObject = GetWantedCriminalTroop();
			if (characterObject == null)
			{
				return false;
			}
			description.SetTextVariable("TROOP_TYPE", characterObject.Name);
			return (MobileParty.MainParty.LastVisitedSettlement.MapFaction.StringId == "empire" || MobileParty.MainParty.LastVisitedSettlement.MapFaction.StringId == "vlandia") && MobileParty.MainParty.MemberRoster.TotalRegulars >= 40;
		});
		incident26.AddOption("{=HXj3itpQ}Your {TROOP_TYPE} confessed. There is no option but to let the law take its course", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Honor, 300),
			IncidentEffect.TraitChange(DefaultTraits.Generosity, -100),
			IncidentEffect.KillTroopsRandomly((TroopRosterElement x) => x.Character == GetWantedCriminalTroop(), () => 1),
			IncidentEffect.SettlementRelationChange(() => MobileParty.MainParty.LastVisitedSettlement, 10)
		}, delegate(TextObject text)
		{
			CharacterObject characterObject = GetWantedCriminalTroop();
			text.SetTextVariable("TROOP_TYPE", characterObject.Name);
			return true;
		});
		incident26.AddOption("{=XvgESxLA}Tell the villagers that they will have to accept blood money, even though they would prefer vengeance", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Honor, -100),
			IncidentEffect.TraitChange(DefaultTraits.Mercy, 100),
			IncidentEffect.TraitChange(DefaultTraits.Generosity, 200),
			IncidentEffect.SettlementRelationChange(() => MobileParty.MainParty.LastVisitedSettlement, -5),
			IncidentEffect.GoldChange(() => -300)
		});
		incident26.AddOption("{=OjPetQa7}Tell the villagers that they will need to wait until the next life for justice, because you need your fighter in this one", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Generosity, 200),
			IncidentEffect.TraitChange(DefaultTraits.Honor, -100),
			IncidentEffect.SettlementRelationChange(() => MobileParty.MainParty.LastVisitedSettlement, -10)
		});
		Incident incident27 = RegisterIncident("incident_soldier_in_debt", "{=GDHHAyQ5}Soldier in Debt", "{=brV8zgmJ}As you leave town, a merchant approaches you. He shows you a document indicating that one of your {TROOP_TYPE} took out a loan, to be repaid on departure with a quarter part as interest. Your man shrugs, calls the merchant a thief for loaning at such ruinous rates, and says he lost the money gambling anyway.", IncidentTrigger.LeavingTown, IncidentType.TroopSettlementRelation, CampaignTime.Days(60f), delegate(TextObject description)
		{
			CharacterObject characterObject = GetSoldierInDebtTroop();
			if (characterObject == null)
			{
				return false;
			}
			description.SetTextVariable("TROOP_TYPE", characterObject.Name);
			return Hero.MainHero.Clan.Kingdom != null && !Hero.MainHero.Clan.IsUnderMercenaryService;
		});
		incident27.AddOption("{=z0yA6wmV}Force your soldier to repay the loan with full interest", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Honor, 200),
			IncidentEffect.TraitChange(DefaultTraits.Generosity, -100),
			IncidentEffect.MoraleChange(-5f)
		});
		incident27.AddOption("{=qeWTPZge}Tell the merchant that he is lucky not to be chased out of camp for gouging your men", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Honor, -100),
			IncidentEffect.TraitChange(DefaultTraits.Generosity, 100),
			IncidentEffect.SettlementRelationChange(() => MobileParty.MainParty.LastVisitedSettlement, -5)
		});
		incident27.AddOption("{=KMb0CHWI}Pay back the loan out of your own purse, even if you might get a reputation for being easily manipulated", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Generosity, 200),
			IncidentEffect.TraitChange(DefaultTraits.Calculating, -100),
			IncidentEffect.GoldChange(() => -100),
			IncidentEffect.MoraleChange(5f)
		});
		Incident incident28 = RegisterIncident("incident_apples_from_heaven", "{=DCy85lqp}Apples from Heaven", "{=OBdQ2q3d}As you enter the village, some of your men tell you that they've found a group of what look like wild apple trees in full bloom by a stream. They'd fetch a fine price in the village market. It doesn't look like anyone owns the land, but you know that once they're discovered a half-dozen claimants will press title.", IncidentTrigger.EnteringVillage, IncidentType.HuntingForaging, CampaignTime.Days(60f), (TextObject description) => MobileParty.MainParty.MemberRoster.TotalRegulars >= 5);
		incident28.AddOption("{=n6gqNNJ0}Tell your men to help themselves.", new List<IncidentEffect>
		{
			IncidentEffect.MoraleChange(5f),
			IncidentEffect.TraitChange(DefaultTraits.Generosity, 100)
		});
		incident28.AddOption("{=mezu0Pej}Inform the villagers of your discovery", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Honor, 100),
			IncidentEffect.MoraleChange(-5f)
		});
		Incident incident29 = RegisterIncident("incident_tempting_berries", "{=o7kYGTjF}Tempting Berries", "{=OmYT9wnV}Your party stumbles upon some bushes with ripe berries near a village, allowing for a refreshing break during the journey. You haven't seen berries quite like these before, but they do look tasty.", IncidentTrigger.LeavingVillage, IncidentType.HuntingForaging, CampaignTime.Days(60f), (TextObject description) => MobileParty.MainParty.MemberRoster.TotalRegulars >= 5);
		incident29.AddOption("{=xSoKTeLL}Allow your men to collect some berries from the bushes", new List<IncidentEffect> { IncidentEffect.Select(IncidentEffect.Group(IncidentEffect.TraitChange(DefaultTraits.Generosity, 100), IncidentEffect.MoraleChange(5f)), IncidentEffect.WoundTroopsRandomly(3), 0.2f) });
		incident29.AddOption("{=E8IpjMAw}The bushes may belong to someone. Leave them to the local farmers", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Honor, 100),
			IncidentEffect.MoraleChange(-5f)
		});
		incident29.AddOption("{=y0Ior5zV}If they are ripe why aren't they are already eaten by animals or collected? They might be poisonous. Avoid them", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Valor, -100),
			IncidentEffect.TraitChange(DefaultTraits.Calculating, 100),
			IncidentEffect.MoraleChange(-1f)
		});
		Incident incident30 = RegisterIncident("incident_hunters_paradise", "{=zQ2rtAQE}Hunter's Paradise", "{=fjWw8D1x}Your men spot several deer on the outskirts of the village, a rare thing in a land where meat and hides are valuable. The villagers tell you that there was an unusual crop of acorns this year, while fear of bandits has prevented them from hunting as they normally would. They invite you to shoot a few deer, as there will be plenty to go around anyway and the beasts are trampling the crops.", IncidentTrigger.EnteringVillage, IncidentType.HuntingForaging, CampaignTime.Years(1000f), (TextObject description) => PartyBase.MainParty.MemberRoster.TotalRegulars >= 5);
		incident30.AddOption("{=E659QFHp}Free meat, a bit of fun and practice for your men. Sound the hunting horn.", new List<IncidentEffect>
		{
			IncidentEffect.DisorganizeParty(),
			IncidentEffect.TraitChange(DefaultTraits.Generosity, 100),
			IncidentEffect.ChangeItemAmount(() => Game.Current.ObjectManager.GetObject<ItemObject>("meat"), () => 3),
			IncidentEffect.PartyExperienceChance(200),
			IncidentEffect.WoundTroopsRandomly(2).WithChance(0.5f)
		});
		incident30.AddOption("{=jON8A2UY}A bunch of loud amateur hunters will scare the game and probably injure themselves. Let your scout bag an animal or two, while the rest of your party marches ahead.", new List<IncidentEffect>
		{
			IncidentEffect.ChangeItemAmount(() => Game.Current.ObjectManager.GetObject<ItemObject>("meat"), () => 2),
			IncidentEffect.DisorganizeParty()
		});
		incident30.AddOption("{=5rjUJaW4}Hunting requires patience. You've got real battles to fight.", new List<IncidentEffect> { IncidentEffect.TraitChange(DefaultTraits.Valor, 100) });
		Incident incident31 = RegisterIncident("incident_boar_in_a_thicket", "{=Fa0J7a6M}Boar in a Thicket", "{=sQA7Z3Pp}As you set out from the village, a local hunter tells you that he can lead you to a nest made by a particularly large boar in a nearby thicket. The animal is a threat to the village and too fierce for him, he says. It already killed two of his dogs. Your men could probably get it and claim a meal of fresh pork, though not without risk.", IncidentTrigger.LeavingVillage, IncidentType.HuntingForaging, CampaignTime.Days(60f), (TextObject description) => PartyBase.MainParty.MemberRoster.TotalRegulars >= 5);
		incident31.AddOption("{=chZrxxe1}The hunt is on! Surround the thicket with spearmen and drive the boar out", new List<IncidentEffect> { IncidentEffect.Select(IncidentEffect.Group(IncidentEffect.MoraleChange(5f), IncidentEffect.ChangeItemAmount(() => Game.Current.ObjectManager.GetObject<ItemObject>("meat"), () => 3), IncidentEffect.TraitChange(DefaultTraits.Calculating, -100)), IncidentEffect.KillTroopsRandomly((TroopRosterElement x) => true, () => 1), 0.75f) });
		incident31.AddOption("{=Hl2EsIob}Leave the animal be", new List<IncidentEffect> { IncidentEffect.TraitChange(DefaultTraits.Valor, 100) });
		Incident incident32 = RegisterIncident("incident_broken_wagon", "{=8FobMTCH}Broken-down Wagon", "{=FJzoOKIU}You come across a trader whose wagon has gotten stuck in the road after his draught horse went lame. He is desperate to sell his wares before they spoil, and offers them at a bargain price.", IncidentTrigger.LeavingEncounter, IncidentType.TroopSettlementRelation, CampaignTime.Days(60f), (TextObject description) => !MobileParty.MainParty.IsCurrentlyAtSea && MobileParty.MainParty.ItemRoster.Any((ItemRosterElement x) => x.EquipmentElement.Item.HasHorseComponent || x.EquipmentElement.Item.StringId == "mule"));
		incident32.AddOption("{=ywaqOBG6}Give some draught animal to him so that he can continue on his way.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Generosity, 200),
			IncidentEffect.ChangeItemAmount(GetBrokenWagonDraughtAnimalItem, () => -1)
		});
		incident32.AddOption("{=1bobtmxl}Sell him your cheapest horse so that he can continue.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Calculating, 100),
			IncidentEffect.TraitChange(DefaultTraits.Mercy, -100),
			IncidentEffect.GoldChange(() => 200),
			IncidentEffect.ChangeItemAmount(GetBrokenWagonCheapestHorseItem, () => -1)
		});
		incident32.AddOption("{=buCYa33Q}Buy his wares at bargain price.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Calculating, 100),
			IncidentEffect.TraitChange(DefaultTraits.Mercy, -100),
			IncidentEffect.ChangeItemAmount(() => Game.Current.ObjectManager.GetObject<ItemObject>("cotton"), () => 1),
			IncidentEffect.ChangeItemAmount(() => Game.Current.ObjectManager.GetObject<ItemObject>("wine"), () => 1),
			IncidentEffect.GoldChange(() => -100)
		});
		incident32.AddOption("{=wIakrAb}Explain to him that you cannot help.", new List<IncidentEffect>());
		Incident incident33 = RegisterIncident("incident_honor_the_slain_foe", "{=bGxsp5Le}Honor the Slain Foe", "{=lFaBQPxk}Your {CULTURE_NAME} troops ask if they can take charge of the {CULTURE_BURIAL_STYLE} of the {CULTURE_NAME} in the enemy's ranks, declaring that they feel a duty to their kin even if they fought in a bad cause.", IncidentTrigger.LeavingBattle, IncidentType.PostBattle, CampaignTime.Days(60f), delegate(TextObject description)
		{
			TroopRosterElement troopRosterElement = GetHonorTheSlainFoeRandomTroop();
			if (troopRosterElement.Character == null)
			{
				return false;
			}
			description.SetTextVariable("CULTURE_NAME", troopRosterElement.Character.Culture.Name);
			switch (troopRosterElement.Character.Culture.StringId)
			{
			case "nord":
			case "battania":
			case "khuzait":
				description.SetTextVariable("CULTURE_BURIAL_STYLE", "{=LntPXkbN}cremation");
				break;
			default:
				description.SetTextVariable("CULTURE_BURIAL_STYLE", "{=AbdmPy2k}burial");
				break;
			}
			return true;
		});
		incident33.AddOption("{=9mHMjIqd}Refuse, knowing that separate rituals will breed suspicion among the different cultures in your ranks", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Generosity, -100),
			IncidentEffect.MoraleChange(-5f)
		});
		incident33.AddOption("{=ssAwOnse}Grant their request, as there is no shame in honoring a valiant foe", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Generosity, 100),
			IncidentEffect.TraitChange(DefaultTraits.Valor, 100),
			IncidentEffect.MoraleChange(10f)
		});
		Incident incident34 = RegisterIncident("incident_local_hero", "{=lfYEujfb}Local Hero", "{=9ab6HwCS}As you prepare to leave the village, the elder approaches you. His nephew is to be married, and it would be a great honor if a warrior of your renown would bless the couple. Your men suppress grins and jostle each other, and you know they are looking forward to the chance to get royally drunk.", IncidentTrigger.LeavingVillage, IncidentType.TroopSettlementRelation, CampaignTime.Days(60f), (TextObject description) => Hero.MainHero.Clan.Renown >= 500f && PartyBase.MainParty.MemberRoster.TotalRegulars >= 5);
		incident34.AddOption("{=Huu4DySE}Accept the invitation and let the men have their fun", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Generosity, 100),
			IncidentEffect.MoraleChange(5f),
			IncidentEffect.DisorganizeParty()
		});
		incident34.AddOption("{=LamSdYjF}Accept the invitation, offering a handsome gift to the couple", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Generosity, 200),
			IncidentEffect.GoldChange(() => -100),
			IncidentEffect.MoraleChange(10f),
			IncidentEffect.DisorganizeParty()
		});
		incident34.AddOption("{=Pw3qq9nw}Apologize, even though it will disappoint your men", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Generosity, -100),
			IncidentEffect.MoraleChange(-5f)
		});
		Incident incident35 = RegisterIncident("incident_false_money", "{=Cba9zsDh}False Money", "{=FKkG15p7}As you leave the village, a trader comes up to you and says the \"silver\" denars that some of your men just gave him are just polished iron. You can indeed see some specks of rust. He demands that you pay him in full. It's entirely possible that your men tried to pass off some false coinage, but the trader's \"evidence\" could equally easily have come from his own stock.", IncidentTrigger.LeavingTown, IncidentType.TroopSettlementRelation, CampaignTime.Years(1000f), (TextObject description) => MobileParty.MainParty.MemberRoster.TotalRegulars >= 25);
		incident35.AddOption("{=ZZXWjvQG}Have your men turn out their pockets. If there are any other tin coins there, assume they are at fault.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Honor, 100),
			IncidentEffect.TraitChange(DefaultTraits.Calculating, 100),
			IncidentEffect.TraitChange(DefaultTraits.Generosity, -100),
			IncidentEffect.MoraleChange(-5f)
		});
		incident35.AddOption("{=lnF2ci30}Tell the merchant that once he accepts currency, it is his fault and his responsibility", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Honor, -100),
			IncidentEffect.TraitChange(DefaultTraits.Calculating, 100)
		});
		Incident incident36 = RegisterIncident("incident_coin_clipping", "{=IFtXJbfK}Coin Clipping", "{=7fpGI4Jb}Shortly after the loot is distributed, you come across several of your men rubbing the edges of the coins that you paid them against a rock. You recognize what they're doing - clipping, or shaving off a bit of the silver to melt down in the hope that no one will notice.", IncidentTrigger.LeavingBattle, IncidentType.PartyCampLife, CampaignTime.Days(60f), (TextObject description) => Hero.MainHero.GetSkillValue(DefaultSkills.Roguery) >= 40 && PartyBase.MainParty.MemberRoster.TotalRegulars >= 5);
		incident36.AddOption("{=D750cNUH}Turn a blind eye, even though the practice is considered little better than counterfeiting", new List<IncidentEffect> { IncidentEffect.TraitChange(DefaultTraits.Honor, -100) });
		incident36.AddOption("{=EM8bWb1b}Demand that your men stop, and inform any merchants in the next town that they should look very carefully at whatever coins your men give them.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Honor, 200),
			IncidentEffect.TraitChange(DefaultTraits.Generosity, -100),
			IncidentEffect.MoraleChange(-5f)
		});
		incident36.AddOption("{=rRVEjUWH}Show them a few tricks from your rogue's repertoire", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Honor, -200),
			IncidentEffect.TraitChange(DefaultTraits.Generosity, 100),
			IncidentEffect.TraitChange(DefaultTraits.Calculating, 100),
			IncidentEffect.GoldChange(() => 200),
			IncidentEffect.SkillChange(DefaultSkills.Roguery, 100f)
		});
		Incident incident37 = RegisterIncident("incident_ice_march", "{=5LOG5afj}Ice March", "{=rm4ApCIW}Now that the fighting has ended you notice that the weather has turned bitterly cold, and a wind is picking up. Soon after you return to the march, you notice that several of your men have fallen behind. Your men are shivering in the wind, however, and are in no mood to wait. You can march on and hope they catch up to you, or set a slower pace.", IncidentTrigger.LeavingEncounter, IncidentType.HardTravel, CampaignTime.Days(60f), delegate
		{
			MapWeatherModel.WeatherEvent weatherEventInPosition = Campaign.Current.Models.MapWeatherModel.GetWeatherEventInPosition(MobileParty.MainParty.Position.ToVec2());
			return !MobileParty.MainParty.IsCurrentlyAtSea && MobileParty.MainParty.MemberRoster.TotalRegulars >= 30 && (weatherEventInPosition == MapWeatherModel.WeatherEvent.HeavyRain || weatherEventInPosition == MapWeatherModel.WeatherEvent.LightRain);
		});
		incident37.AddOption("{=bXoYv9a2}Continue at a normal marching pace", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Valor, 100),
			IncidentEffect.KillTroopsRandomly((TroopRosterElement x) => !x.Character.IsHero, () => (int)(0.05f * (float)MobileParty.MainParty.MemberRoster.TotalRegulars)).WithChance(0.5f)
		});
		incident37.AddOption("{=gxGlWxVC}Slow down until the stragglers can catch up", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Generosity, 200),
			IncidentEffect.MoraleChange(-2f),
			IncidentEffect.DisorganizeParty()
		});
		Incident incident38 = RegisterIncident("incident_sandstorm_warning", "{=cSa3IFw5}Sandstorm Warning", "{=yJMpjiP7}As you set out again after the battle, you feel a few scattered drops of rain. Then there is a rush of wind, and a wall of dust engulfs you. The blowing grains sting your face, and you can see but a few arms' lengths ahead. You know that it is a simple matter for men to get lost in the notorious sandstorms of the Nahhas, only to be found later dead of thirst or buried in a dune.", IncidentTrigger.LeavingEncounter, IncidentType.HardTravel, CampaignTime.Days(60f), delegate
		{
			if (!MobileParty.MainParty.Position.IsValid())
			{
				return false;
			}
			return !MobileParty.MainParty.IsCurrentlyAtSea && MobileParty.MainParty.MemberRoster.TotalRegulars >= 20 && Campaign.Current.MapSceneWrapper.GetFaceTerrainType(MobileParty.MainParty.Position.Face) == TerrainType.Desert;
		});
		incident38.AddOption("{=vzOfEiKx}Wait briefly in the lee of a dune until the wind eases up, then press cautiously ahead", new List<IncidentEffect>
		{
			IncidentEffect.DisorganizeParty(),
			IncidentEffect.TraitChange(DefaultTraits.Valor, -100)
		});
		incident38.AddOption("{=HZwt5rVy}Keep marching, even though there is a good chance some of your men might be separated.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Valor, 100),
			IncidentEffect.KillTroopsRandomly((TroopRosterElement x) => !x.Character.IsHero, () => (int)(0.05f * (float)MobileParty.MainParty.MemberRoster.TotalRegulars)).WithChance(0.5f)
		});
		Incident incident39 = RegisterIncident("incident_vintage_of_victory", "{=1cgOQB5K}Vintage of Victory", "{=ZoIXaxj9}Among the loot from your last battle you discover a vat of fine wine from {WINE_SETTLEMENT_NAME}. You know that you could sell it for a considerable sum, but your men just risked their lives and don't seem inclined to pocket silver tomorrow for a drink today.", IncidentTrigger.LeavingBattle, IncidentType.PostBattle, CampaignTime.Days(60f), delegate(TextObject description)
		{
			Village seededRandomElement = IncidentHelper.GetSeededRandomElement(Village.All.Where((Village x) => x.VillageType == DefaultVillageTypes.VineYard).ToList(), _activeIncidentSeed);
			if (seededRandomElement == null)
			{
				return false;
			}
			description.SetTextVariable("WINE_SETTLEMENT_NAME", seededRandomElement.Name);
			return MobileParty.MainParty.MemberRoster.TotalManCount >= 20;
		});
		incident39.AddOption("{=hj27QkT3}Keep it to sell, telling your men that the money will be spent on keeping them safe", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Generosity, -100),
			IncidentEffect.TraitChange(DefaultTraits.Calculating, 100),
			IncidentEffect.ChangeItemAmount(() => Game.Current.ObjectManager.GetObject<ItemObject>("wine"), () => 1)
		});
		incident39.AddOption("{=SdQEKdeo}Smash the top off the amphorae and let them drink their fill.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Generosity, 200),
			IncidentEffect.TraitChange(DefaultTraits.Calculating, -100),
			IncidentEffect.MoraleChange(2f),
			IncidentEffect.DisorganizeParty()
		});
		Incident incident40 = RegisterIncident("incident_no_time_to_mourn", "{=GL0jmbbJ}No Time to Mourn", "{=Qrj5hN2k}It is time to bury your dead, but the ground is frozen solid and the wood from nearby trees is too wet to make a proper pyre. You can give the bodies a hasty burial in a nearby gully, even though wolves or other animals may find them, or take the bodies with you to bury them as soon as you can.", IncidentTrigger.LeavingBattle, IncidentType.PostBattle, CampaignTime.Days(60f), delegate
		{
			PlayerBattleEndedLogEntry playerBattleEndedLogEntry = Campaign.Current.LogEntryHistory.FindLastGameActionLog((PlayerBattleEndedLogEntry x) => true);
			return playerBattleEndedLogEntry != null && playerBattleEndedLogEntry.PlayerCasualties > 0 && PartyBase.MainParty.MemberRoster.TotalRegulars >= 5;
		});
		incident40.AddOption("{=2GMj6ZaW}Carry the bodies with you until you find dry wood or softer ground", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Generosity, 100),
			IncidentEffect.MoraleChange(2f),
			IncidentEffect.DisorganizeParty()
		});
		incident40.AddOption("{=0CCgjMTG}Cover the bodies as best you can and hope for the best", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Generosity, -100),
			IncidentEffect.MoraleChange(-2f)
		});
		Incident incident41 = RegisterIncident("incident_no_mood_for_mercy", "{=X8dgb937}No Mood for Mercy", "{=uXendo01}It was a hard-fought battle, and among your fallen is a young good-natured {TROOP_TYPE} who kept his comrades' spirits up during hard times. Your men’s faces are grim as they go out to collect the wounded of both sides. You can tell that, if they have their way, not many injured enemies will be brought back to your camp still alive as prisoners.", IncidentTrigger.LeavingBattle, IncidentType.PostBattle, CampaignTime.Years(1000f), delegate(TextObject description)
		{
			CharacterObject characterObject = GetNoMoodForMercyRandomTroop();
			if (characterObject == null)
			{
				return false;
			}
			description.SetTextVariable("TROOP_TYPE", characterObject.Name);
			PlayerBattleEndedLogEntry playerBattleEndedLogEntry = Campaign.Current.LogEntryHistory.FindLastGameActionLog((PlayerBattleEndedLogEntry x) => true);
			return playerBattleEndedLogEntry != null && playerBattleEndedLogEntry.PlayerCasualties > 0;
		});
		incident41.AddOption("{=5SWqBZSh}Demand that your men respect the customs of war and tend to those who gave quarter", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Mercy, 100),
			IncidentEffect.TraitChange(DefaultTraits.Honor, 100),
			IncidentEffect.TraitChange(DefaultTraits.Generosity, 100),
			IncidentEffect.MoraleChange(-15f)
		});
		incident41.AddOption("{=3CaDxb8R}Remind your men that war is war, the {TROOP_TYPE} knew the risks, and prisoners bring ransoms", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Calculating, 100),
			IncidentEffect.TraitChange(DefaultTraits.Generosity, 100),
			IncidentEffect.MoraleChange(-5f)
		}, delegate(TextObject text)
		{
			CharacterObject characterObject = GetNoMoodForMercyRandomTroop();
			text.SetTextVariable("TROOP_TYPE", characterObject.Name);
			return true;
		});
		incident41.AddOption("{=mBkCqpEj}Tell your men that they can avenge their comrade as they see fit", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Mercy, -100),
			IncidentEffect.MoraleChange(10f),
			IncidentEffect.RemovePrisonersRandomlyWithPredicate((TroopRosterElement x) => true, 3)
		});
		Incident incident42 = RegisterIncident("incident_misplaced_vengeance", "{=fiesVorG}Misplaced Vengeance", "{=i0SwEBdf}Throughout your visit to {VILLAGE}, the villagers were worried that the enemy who sacked {LOOTED_VILLAGE} might be sending patrols their way. As you leave, you find out that their fears were not misplaced. The first thing you see is a flight of ravens in the sky. Your men begin to mutter to themselves, but soon they stumble across a sight that silences them - a pile of burned corpses, stacked by a crossroad, left by the {ENEMY} as their calling card. You press on, but you soon notice that your men are casting an eye towards your prisoners. They may not have been directly involved in what happened in {LOOTED_VILLAGE}, but you doubt your men care much.", IncidentTrigger.LeavingVillage, IncidentType.PostBattle, CampaignTime.Years(1000f), delegate(TextObject description)
		{
			if (MobileParty.MainParty.LastVisitedSettlement == null)
			{
				return false;
			}
			if (!MobileParty.MainParty.LastVisitedSettlement.IsVillage)
			{
				return false;
			}
			Village village = MobileParty.MainParty.LastVisitedSettlement.Village;
			Village lootedVillage = GetMisplacedVengeanceLootedVillage();
			if (lootedVillage == null)
			{
				return false;
			}
			description.SetTextVariable("VILLAGE", village.Name);
			description.SetTextVariable("LOOTED_VILLAGE", lootedVillage.Name);
			description.SetTextVariable("ENEMY", lootedVillage.Settlement.MapFaction.Name);
			return MobileParty.MainParty.LastVisitedSettlement.MapFaction == Hero.MainHero.MapFaction && PartyBase.MainParty.MemberRoster.TotalRegulars >= 5 && (from x in MobileParty.MainParty.PrisonRoster.GetTroopRoster()
				where x.Character.Culture == lootedVillage.Settlement.Culture && !x.Character.IsHero
				select x).Sum((TroopRosterElement x) => x.Number) > 5;
		});
		incident42.AddOption("{=SfO3uwdX}Take a brief walk to clear your head, and when you come back to find the prisoners gone, do not ask questions", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Mercy, -100),
			IncidentEffect.MoraleChange(5f),
			IncidentEffect.Custom(null, delegate
			{
				Village lootedVillage = GetMisplacedVengeanceLootedVillage();
				IncidentEffect.RemovePrisonersRandomlyWithPredicate((TroopRosterElement x) => x.Character.Culture == lootedVillage?.Settlement.Culture, 5).Consequence();
				TextObject item = new TextObject("{=vyH9slae}Lost {AMOUNT} {?AMOUNT > 1}prisoners{?}prisoner{\\?} of {CULTURE} culture").SetTextVariable("AMOUNT", 5).SetTextVariable("CULTURE", lootedVillage?.Settlement.Culture.Name);
				return new List<TextObject> { item };
			}, delegate
			{
				Village village = GetMisplacedVengeanceLootedVillage();
				return new List<TextObject> { new TextObject("{=gejYla80}Lose {AMOUNT} {?AMOUNT > 1}prisoners{?}prisoner{\\?} of {CULTURE} culture").SetTextVariable("AMOUNT", 5).SetTextVariable("CULTURE", village?.Settlement.Culture.Name) };
			})
		});
		incident42.AddOption("{=qljKyPrR}Tell your men that the prisoners must be kept safe and that you will hold them responsible for anything that happens", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Honor, 100),
			IncidentEffect.MoraleChange(-2f),
			IncidentEffect.DisorganizeParty()
		});
		Incident incident43 = RegisterIncident("incident_sleeping_sentry", "{=qFa4Oik0}Sleeping Sentry", "{=gQigaCkD}One of your {RECRUIT} fell asleep on guard. He was new to your company and not popular, and your men are angry. He put their lives at risk, and they demand he be turned over to them to face the ancient Calradic penalty - to run the gauntlet of men with sticks who will beat him, an ordeal that frequently ends in death.", IncidentTrigger.LeavingEncounter, IncidentType.PartyCampLife, CampaignTime.Days(60f), delegate(TextObject description)
		{
			CharacterObject characterObject = GetSleepingSentryTroop();
			if (characterObject == null)
			{
				return false;
			}
			description.SetTextVariable("RECRUIT", characterObject.Name);
			return !MobileParty.MainParty.IsCurrentlyAtSea && MobileParty.MainParty.MemberRoster.TotalRegulars >= 35;
		});
		incident43.AddOption("{=oIknbpZl}Force the recruit to run the gauntlet", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Mercy, -100),
			IncidentEffect.TraitChange(DefaultTraits.Valor, 100),
			IncidentEffect.MoraleChange(5f),
			IncidentEffect.Select(IncidentEffect.WoundTroop(GetSleepingSentryTroop, 1), IncidentEffect.KillTroopsRandomly((TroopRosterElement x) => x.Character.Tier <= 2, () => 1), 0.5f)
		});
		incident43.AddOption("{=0lqAKHfU}Tell your men to be forgiving, as they were once recruits themselves", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Mercy, 100),
			IncidentEffect.MoraleChange(-10f)
		});
		Incident incident44 = RegisterIncident("incident_trade_proposal", "{=IaUhqDw6}Trade Proposal", "{=sU4t3v9r}A passing trader offers to buy {GOOD} from your party at well over the price that it would fetch in the nearest town. No doubt some other merchant has fed him bad information on prices, either to put a rival out of business or as a joke.", IncidentTrigger.LeavingEncounter, IncidentType.Profit, CampaignTime.Days(60f), delegate(TextObject description)
		{
			ItemRosterElement itemRosterElement = GetTradeProposalGood();
			if (itemRosterElement.EquipmentElement.Item == null)
			{
				return false;
			}
			description.SetTextVariable("GOOD", itemRosterElement.EquipmentElement.Item.Name);
			return !MobileParty.MainParty.IsCurrentlyAtSea && MobileParty.MainParty.ItemRoster.Any((ItemRosterElement x) => x.Amount > 0);
		});
		incident44.AddOption("{=b9S5bNbq}As a merchant he should know his trade. Keep silent and take the offer", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Calculating, 100),
			IncidentEffect.Custom(null, delegate
			{
				ItemRosterElement itemRosterElement = GetTradeProposalGood();
				MobileParty.MainParty.ItemRoster.AddToCounts(itemRosterElement.EquipmentElement, -1);
				GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, itemRosterElement.EquipmentElement.ItemValue * 2);
				TextObject textObject = new TextObject("{=JmG1KrTw}Lost 1 {GOOD} and gained {AMOUNT}{GOLD_ICON}");
				textObject.SetTextVariable("GOOD", itemRosterElement.EquipmentElement.Item.Name);
				textObject.SetTextVariable("AMOUNT", itemRosterElement.EquipmentElement.ItemValue * 2);
				return new List<TextObject> { textObject };
			}, delegate
			{
				ItemRosterElement itemRosterElement = GetTradeProposalGood();
				TextObject textObject = new TextObject("{=baqpnA0F}Lose 1 {GOOD}, gain {AMOUNT}{GOLD_ICON}");
				textObject.SetTextVariable("GOOD", itemRosterElement.EquipmentElement.Item.Name);
				textObject.SetTextVariable("AMOUNT", itemRosterElement.EquipmentElement.ItemValue * 2);
				return new List<TextObject> { textObject };
			})
		});
		incident44.AddOption("{=0Wwwa77J}Tell the trader that you have no wish to profit from his ignorance", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Honor, 100),
			IncidentEffect.TraitChange(DefaultTraits.Mercy, 100)
		});
		incident44.AddOption("{=QWHwvRtC}As a fool and his money are soon parted, you'll do him a favor by saving him some time. Haggle for more", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Honor, -100),
			IncidentEffect.Custom(null, delegate
			{
				ItemRosterElement itemRosterElement = GetTradeProposalGood();
				MobileParty.MainParty.ItemRoster.AddToCounts(itemRosterElement.EquipmentElement, -1);
				GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, itemRosterElement.EquipmentElement.ItemValue * 3);
				TextObject textObject = new TextObject("{=JmG1KrTw}Lost 1 {GOOD} and gained {AMOUNT}{GOLD_ICON}");
				textObject.SetTextVariable("GOOD", itemRosterElement.EquipmentElement.Item.Name);
				textObject.SetTextVariable("AMOUNT", itemRosterElement.EquipmentElement.ItemValue * 3);
				return new List<TextObject> { textObject };
			}, delegate
			{
				ItemRosterElement itemRosterElement = GetTradeProposalGood();
				TextObject textObject = new TextObject("{=baqpnA0F}Lose 1 {GOOD}, gain {AMOUNT}{GOLD_ICON}");
				textObject.SetTextVariable("GOOD", itemRosterElement.EquipmentElement.Item.Name);
				textObject.SetTextVariable("AMOUNT", itemRosterElement.EquipmentElement.ItemValue * 3);
				return new List<TextObject> { textObject };
			})
		});
		Incident incident45 = RegisterIncident("incident_singing_for_supper", "{=9uaAUCas}Singing for Supper", "{=iV57q3Tm}A strolling minstrel offers to compose an epic ballad about your heroic deeds, potentially increasing your renown for a little amount of coin. Do you accept his offer, and if so, do you offer him any guidance on what to sing?", IncidentTrigger.LeavingTown, IncidentType.DreamsSongsAndSigns, CampaignTime.Days(60f), (TextObject description) => true);
		incident45.AddOption("{=AxGaZNry}Let him do it and reward him generously, but tell him to describe your losses and faults as well", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Honor, 200),
			IncidentEffect.GoldChange(() => -500),
			IncidentEffect.RenownChange(20f),
			IncidentEffect.InfluenceChange(-100f)
		});
		incident45.AddOption("{=m35ezdZG}You don't need a bard to recount your heroic deeds", new List<IncidentEffect> { IncidentEffect.TraitChange(DefaultTraits.Generosity, -100) });
		incident45.AddOption("{=bJBLAnrc}Shower him with gold and tell him that he should feel free to embellish your story as he sees fit", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Calculating, 100),
			IncidentEffect.TraitChange(DefaultTraits.Honor, -100),
			IncidentEffect.GoldChange(() => -1000),
			IncidentEffect.RenownChange(20f),
			IncidentEffect.InfluenceChange(200f)
		});
		incident45.AddOption("{=HbOFGhTG}Tragic tales are always the most popular. Ensure the bard does not leave out the daemon who sits on your shoulder, your bouts of madness, and the witch who foretold your terrible but heroic death", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Calculating, -300),
			IncidentEffect.RenownChange(30f),
			IncidentEffect.GoldChange(() => -200)
		});
		Incident incident46 = RegisterIncident("incident_love_marriage", "{=QpkXLmjE}Love Marriage", "{=Tq4O8vic}One of your {KNIGHT} asks you for a favor. His daughter plans to marry a man from the back alleys of this town with a reputation as a no-good layabout. He has refused permission but asks you to do what you can to insure the two do not elope.", IncidentTrigger.LeavingTown, IncidentType.FiefManagement, CampaignTime.Years(1000f), delegate(TextObject description)
		{
			CharacterObject character = IncidentHelper.GetSeededRandomElement(MobileParty.MainParty.MemberRoster.GetTroopRoster().Where(LoveMarriageTroopPredicate).ToList(), _activeIncidentSeed).Character;
			if (character == null)
			{
				return false;
			}
			description.SetTextVariable("KNIGHT", character.Name);
			return MobileParty.MainParty.LastVisitedSettlement.IsFortification && MobileParty.MainParty.LastVisitedSettlement.OwnerClan == Hero.MainHero.Clan;
		});
		incident46.AddOption("{=uppCchb0}Let them marry. Love must triumph over a father's pride", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Mercy, 200),
			IncidentEffect.TraitChange(DefaultTraits.Calculating, -100),
			IncidentEffect.KillTroopsRandomly(LoveMarriageTroopPredicate, () => 1).WithChance(0.5f)
		});
		incident46.AddOption("{=oQmuxCSk}Do your best to explain to the girl that marriages are affairs of the head, not of the heart, and she had better think of her future and that of her children and not elope.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Mercy, -100),
			IncidentEffect.TraitChange(DefaultTraits.Calculating, 200).WithChance(0.5f)
		});
		incident46.AddOption("{=GMQROhTM}Order your troops to give the upstart a sound beating. Who does he think he is, the hero of some foolish romantic poem?", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Mercy, -100),
			IncidentEffect.TraitChange(DefaultTraits.Calculating, 50),
			IncidentEffect.MoraleChange(5f)
		});
		Incident incident47 = RegisterIncident("incident_riddles_on_the_road", "{=bBd0jAvo}Riddles on the Road", "{=S2PV4Qeb}As you ride out of the village, an old man in rags comes up to you and grasps the halter of your horse. \"You must tell me!\" he barks. \"What is the tale told by the sword?\" Your men gather in to see if you will humor this seemingly mad hermit.", IncidentTrigger.LeavingVillage, IncidentType.DreamsSongsAndSigns, CampaignTime.Years(1000f), (TextObject description) => PartyBase.MainParty.MemberRoster.TotalRegulars >= 10);
		incident47.AddOption("{=2dW3cBQy}That steel is forged from iron, and long hard marches breed tough warriors.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Honor, 200),
			IncidentEffect.MoraleChange(-5f)
		});
		incident47.AddOption("{=PHo98vjr}That a sword is all you need, because if you own one, you can take whatever else you need from those without.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Calculating, 200),
			IncidentEffect.RenownChange(10f)
		});
		incident47.AddOption("{=xhuGmF9r}That the force of the blade lies only in the hand that wields it.", new List<IncidentEffect> { IncidentEffect.TraitChange(DefaultTraits.Valor, 100) });
		incident47.AddOption("{=yMoBOgxx}\"You tell me,\" you say, with a mad laugh, as you deal him a slice that draws blood.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Calculating, -200),
			IncidentEffect.TraitChange(DefaultTraits.Mercy, -100),
			IncidentEffect.RenownChange(20f)
		});
		Incident incident48 = RegisterIncident("incident_deadly_reputation", "{=XeX5x8jw}A Deadly Reputation", "{=GpUBP10Q}As you leave the village, a merchant who clearly wants your approval shouts out, \"{LOCAL_KING} has slain his thousands, but {PLAYER} his tens of thousands!\"", IncidentTrigger.LeavingVillage, IncidentType.DreamsSongsAndSigns, CampaignTime.Years(1000f), delegate(TextObject description)
		{
			if (MobileParty.MainParty.LastVisitedSettlement == null || MobileParty.MainParty.LastVisitedSettlement.MapFaction.Leader == null)
			{
				return false;
			}
			description.SetTextVariable("LOCAL_KING", MobileParty.MainParty.LastVisitedSettlement.MapFaction.Leader.Name);
			description.SetTextVariable("PLAYER", Hero.MainHero.Name);
			return Hero.MainHero.Clan.Kingdom != null && Hero.MainHero.Clan.Kingdom.Leader != Hero.MainHero;
		});
		incident48.AddOption("{=sxH5NHba}\"I have, and it is but an appetite-whetter before the main meal.\"", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Mercy, -100),
			IncidentEffect.RenownChange(20f),
			IncidentEffect.HeroRelationChange(() => Hero.MainHero.Clan.Kingdom.Leader, -1)
		});
		incident48.AddOption("{=acBdTQxp}\"All I have done has been in the service of {KING}. I am but his will made flesh.\"", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Calculating, 100),
			IncidentEffect.HeroRelationChange(() => Hero.MainHero.Clan.Kingdom.Leader, 5)
		}, delegate(TextObject text)
		{
			text.SetTextVariable("KING", Hero.MainHero.Clan.Kingdom.Leader.Name);
			return true;
		});
		incident48.AddOption("{=naVH8uSO}\"Any blood I have shed may have been necessary but I hope that Heaven will forgive me.\"", new List<IncidentEffect> { IncidentEffect.TraitChange(DefaultTraits.Mercy, 100) });
		incident48.AddOption("{=iR3ZuTSW}Acknowledge him by howling like an animal and swirling your weapon about, as mad behavior makes for good stories.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Calculating, -200),
			IncidentEffect.RenownChange(20f)
		});
		Incident incident49 = RegisterIncident("incident_intriguing_rumors", "{=iYfbN2yb}Intriguing Rumors", "{=aqZyQ3Ca}A man, dressed as a nobleman's servant, approaches you. \"I hear that you and {RIVALLORD.NAME} aren't on the best of terms. Well, what can you expect from a man who can't manage his own family? I was recently employed at his household, and know that his wife was unusually close to her groom... I can spread the word, if you make it worth my while.\"", IncidentTrigger.LeavingTown, IncidentType.TroopSettlementRelation, CampaignTime.Years(1000f), delegate(TextObject description)
		{
			Hero hero = GetIntriguingRumorsRivalLord();
			if (hero == null)
			{
				return false;
			}
			description.SetCharacterProperties("RIVALLORD", hero.CharacterObject);
			return true;
		});
		incident49.AddOption("{=Gjd6nP29}You will not sully the good name of anyone with an obviously false rumor", new List<IncidentEffect> { IncidentEffect.TraitChange(DefaultTraits.Honor, 100) });
		incident49.AddOption("{=jE9aKaYd}People will believe a noble's former servant. This man's favor is worth a purse of silver to you.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Calculating, 100),
			IncidentEffect.TraitChange(DefaultTraits.Honor, -100),
			IncidentEffect.GoldChange(() => -200),
			IncidentEffect.InfluenceChange(10f),
			IncidentEffect.Custom(null, delegate
			{
				Hero hero = GetIntriguingRumorsRivalLord();
				if (hero != null)
				{
					ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, hero, -5);
					hero.AddInfluenceWithKingdom(-10f);
					TextObject textObject = new TextObject("{=WkIylUOa}{RIVALLORD.NAME} lost {AMOUNT} influence.");
					textObject.SetCharacterProperties("RIVALLORD", hero.CharacterObject);
					textObject.SetTextVariable("AMOUNT", 10);
					return new List<TextObject> { textObject };
				}
				return new List<TextObject>();
			}, delegate
			{
				Hero hero = GetIntriguingRumorsRivalLord();
				TextObject textObject = new TextObject("{=NQKDUIGg}{RIVALLORD.NAME} loses {AMOUNT} influence");
				textObject.SetCharacterProperties("RIVALLORD", hero.CharacterObject);
				textObject.SetTextVariable("AMOUNT", 10);
				return new List<TextObject> { textObject };
			})
		});
		incident49.AddOption("{=bZUMOAjU}Crack a few jokes to your men at {RIVALLORD.NAME}'s expense, but otherwise do nothing", new List<IncidentEffect> { IncidentEffect.MoraleChange(5f) }, delegate(TextObject text)
		{
			Hero hero = GetIntriguingRumorsRivalLord();
			text.SetCharacterProperties("RIVALLORD", hero.CharacterObject);
			return true;
		});
		Incident incident50 = RegisterIncident("incident_charitable_acts_recognition", "{=pjy9Z4bt}Charitable Acts Recognition", "{=vaez5WXH}As you leave town, you see a much larger number of beggars gathered than you would expect. Some of their limps look unconvincing. Your reputation for compassion and generosity may have gotten around.", IncidentTrigger.LeavingTown, IncidentType.PlightOfCivilians, CampaignTime.Days(60f), (TextObject description) => Hero.MainHero.GetTraitLevel(DefaultTraits.Generosity) > 1 || Hero.MainHero.GetTraitLevel(DefaultTraits.Mercy) > 1);
		incident50.AddOption("{=DFIPsN1W}If they come to you in need, it is not for you to question them. Have your men distribute alms as usual.", new List<IncidentEffect>
		{
			IncidentEffect.GoldChange(() => -200),
			IncidentEffect.TraitChange(DefaultTraits.Mercy, 200),
			IncidentEffect.TraitChange(DefaultTraits.Calculating, -100)
		});
		incident50.AddOption("{=CKKIJZcB}Have some of your men boil up a soup. Keep a watchful eye on all the beggars, especially those who move too vigorously to the front of the line, and serve only those who seem genuinely too frail to work.", new List<IncidentEffect>
		{
			IncidentEffect.GoldChange(() => -200),
			IncidentEffect.TraitChange(DefaultTraits.Calculating, 100),
			IncidentEffect.TraitChange(DefaultTraits.Mercy, 100),
			IncidentEffect.DisorganizeParty()
		});
		incident50.AddOption("{=T1pwrLN9}Tell them that you are no chicken to be plucked. You will reward neither liars nor those who keep company with liars.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Mercy, -100),
			IncidentEffect.TraitChange(DefaultTraits.Calculating, 100)
		});
		Incident incident51 = RegisterIncident("incident_the_pavillon", "{=rG4b9Tft}The Pavillon", "{=qBsa5KC5}As you leave town you come across an encampment of traveling entertainers. You see tattered but colorful tents, you hear wailing but enticing music, and smell the scent of cheap incense and meat that is probably slightly spoiled but spiced so that you would never notice. A group of women in some noble's thrown-away silks, their faces made up skillfully with charcoal and sheep's blood, beckon to your men. What do you do?", IncidentTrigger.LeavingTown, IncidentType.TroopSettlementRelation, CampaignTime.Days(60f), delegate
		{
			if (MobileParty.MainParty.LastVisitedSettlement == null)
			{
				return false;
			}
			CultureObject culture = MobileParty.MainParty.LastVisitedSettlement.Culture;
			return PartyBase.MainParty.MemberRoster.TotalRegulars >= 5 && culture != null && (culture.StringId == "aserai" || culture.StringId == "khuzait" || culture.StringId == "empire_s");
		});
		incident51.AddOption("{=lPAC4x1e}Tell your men to steer clear of such dubious enticements", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Honor, 100),
			IncidentEffect.MoraleChange(-5f)
		});
		incident51.AddOption("{=xYuNxo3v}Let your men have some fun, but tell them to watch their purses and stay out of fights", new List<IncidentEffect>
		{
			IncidentEffect.MoraleChange(5f),
			IncidentEffect.TraitChange(DefaultTraits.Generosity, 100),
			IncidentEffect.WoundTroopsRandomly(1).WithChance(0.5f),
			IncidentEffect.DisorganizeParty()
		});
		incident51.AddOption("{=mmlk2tXh}The fun is on you! Hurl a fistful of silver at the entertainers and tell them your men deserve nothing but the best", new List<IncidentEffect>
		{
			IncidentEffect.MoraleChange(5f),
			IncidentEffect.TraitChange(DefaultTraits.Generosity, 200),
			IncidentEffect.GoldChange(() => -100)
		});
		incident51.AddOption("{=lzK4PmYL}Rob the entertainers. No doubt their wealth is ill-gotten, and to whom will they complain?", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Mercy, -100),
			IncidentEffect.GoldChange(() => 200)
		});
		Incident incident52 = RegisterIncident("incident_abundance_of_troublemakers", "{=GFGuSmN9}Abundance of troublemakers", "{=mZir8Ogn}As you prepare to leave, the village elder approaches you. There's a gang of four lads who are constantly picking fights with the other villagers, he says. Even their own families don't want them around for fear they'll end up killing someone and cause a blood feud. He begs you to take them into your ranks, where they can learn a bit of discipline.", IncidentTrigger.LeavingVillage, IncidentType.TroopSettlementRelation, CampaignTime.Days(60f), (TextObject description) => MobileParty.MainParty.LastVisitedSettlement != null);
		incident52.AddOption("{=ePstZcmu}Accept the recruits. The first chance you get, find fault with their gear and give them a sound thrashing, so they'll know to mend their ways.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Mercy, -100),
			IncidentEffect.ChangeTroopAmount(() => MobileParty.MainParty.LastVisitedSettlement.Culture.BasicTroop, 4)
		});
		incident52.AddOption("{=Dogg1b9a}Take the recruits. Try not to be too hard on them at first, even though they'll probably corrode your party's discipline.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Mercy, 100),
			IncidentEffect.MoraleChange(-10f),
			IncidentEffect.ChangeTroopAmount(() => MobileParty.MainParty.LastVisitedSettlement.Culture.BasicTroop, 4)
		});
		incident52.AddOption("{=EIbmlJRP}Tell the elder he'll need to find some other way to handle his troublemakers.", new List<IncidentEffect>());
		Incident incident53 = RegisterIncident("incident_ennoble_a_fighter", "{=wLozeWS9}Ennoble a fighter", "{=FPnUWAj1}After the battle, one of your {TROOPS} from your party approaches you, dreamed of riding with your cavalry as a {NOBLE_TIER}. He has the money to buy his own horse, and he certainly distinguished himself in the fight. You know however that this will go down badly - with the high-born cavalry, who prefer to mingle with their own class, but probably even more so with his comrades, who will think he scorns them.", IncidentTrigger.LeavingBattle, IncidentType.PartyCampLife, CampaignTime.Years(1000f), delegate(TextObject description)
		{
			CharacterObject characterObject = GetRegularTroop();
			if (characterObject == null)
			{
				return false;
			}
			CharacterObject characterObject2 = GetNobleTroop(characterObject.Culture);
			if (characterObject2 == null)
			{
				return false;
			}
			description.SetTextVariable("TROOPS", characterObject.Name);
			description.SetTextVariable("NOBLE_TIER", characterObject2.Name);
			return true;
		});
		incident53.AddOption("{=HpAbeiKP}Courage and skill deserve a reward, even if the result is not what you get", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Generosity, 200),
			IncidentEffect.MoraleChange(-10f),
			IncidentEffect.UpgradeTroop(GetRegularTroop, GetNobleTroopForUpgrade, 1, () => _activeIncidentSeed)
		});
		incident53.AddOption("{=oL1qlaxH}Explain to him your reservations, even though you can tell he won't like to hear them, and let him make up his own mind", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Honor, 100),
			IncidentEffect.TraitChange(DefaultTraits.Generosity, -100),
			IncidentEffect.Select(IncidentEffect.Group(IncidentEffect.MoraleChange(-5f), IncidentEffect.UpgradeTroop(GetRegularTroop, GetNobleTroopForUpgrade, 1, () => _activeIncidentSeed)), IncidentEffect.Custom(null, () => new List<TextObject>(), (IncidentEffect effect) => new List<TextObject>
			{
				new TextObject("{=lobJVVWT}Nothing happens")
			}), 0.5f)
		});
		incident53.AddOption("{=uJYOhEx8}Commend him, but tell him to stick with his comrades", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Generosity, -200),
			IncidentEffect.TraitChange(DefaultTraits.Calculating, 100)
		});
		Incident incident54 = RegisterIncident("incident_family_claims_bandit", "{=6IA1S2XU}Family Claims Bandit", "{=BG8Ph5La}As you leave {VILLAGE_NAME}, a woman approaches you and grasps your horse's halter. Among your prisoners is her son, she says. You captured him when he was part of a gang of bandits, but she tells you tearfully that he is a good boy who fell in with bad company. She swears she'll ensure that he doesn't stray again from the correct path.", IncidentTrigger.LeavingVillage, IncidentType.TroopSettlementRelation, CampaignTime.Years(1000f), delegate(TextObject description)
		{
			if (MobileParty.MainParty.LastVisitedSettlement == null)
			{
				return false;
			}
			description.SetTextVariable("VILLAGE_NAME", MobileParty.MainParty.LastVisitedSettlement.Name);
			return MobileParty.MainParty.PrisonRoster.TotalRegulars > 0 && MobileParty.MainParty.PrisonRoster.GetTroopRoster().Any(BanditPrisonerPredicate);
		});
		incident54.AddOption("{=QDbl4lB7}Release your captive, even if some will mutter that you have a soft heart and a soft head", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Mercy, 200),
			IncidentEffect.TraitChange(DefaultTraits.Calculating, -100),
			IncidentEffect.RemovePrisonersRandomlyWithPredicate(BanditPrisonerPredicate, 1)
		});
		incident54.AddOption("{=9yrWWbHF}Tell the woman that her son shall face the same penalties as any other bandit you caught", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Honor, 100),
			IncidentEffect.TraitChange(DefaultTraits.Mercy, -100)
		});
		incident54.AddOption("{=6sAI5uOm}Tell the village that they must pay the lad's ransom, so that he will know there is no profit in banditry", new List<IncidentEffect>
		{
			IncidentEffect.GoldChange(() => 100),
			IncidentEffect.RemovePrisonersRandomlyWithPredicate(BanditPrisonerPredicate, 1),
			IncidentEffect.TraitChange(DefaultTraits.Mercy, -100),
			IncidentEffect.TraitChange(DefaultTraits.Calculating, 50)
		});
		Incident incident55 = RegisterIncident("incident_turn_pike", "{=kl13BJZB}Turnpike", "{=VauaVUNS}You notice a footpath leading through your estate which could be quickly improved to be used by wagons and be a shortcut between the main trade roads. You could charge tolls and turn it into a turnpike, though the locals would resent the increased traffic and fees on their customary route.", IncidentTrigger.LeavingTown | IncidentTrigger.LeavingCastle, IncidentType.FiefManagement, CampaignTime.Years(1000f), delegate
		{
			if (MobileParty.MainParty.LastVisitedSettlement == null || !MobileParty.MainParty.LastVisitedSettlement.IsFortification || MobileParty.MainParty.LastVisitedSettlement.OwnerClan != Clan.PlayerClan)
			{
				return false;
			}
			Building building = GetTurnPikeBuilding();
			return building != null && building.CurrentLevel == building.BuildingType.StartLevel;
		});
		incident55.AddOption("{=aF4Q5TJc}Widen the road for wagons but charge tolls to use it", new List<IncidentEffect>
		{
			IncidentEffect.BuildingLevelChange(GetTurnPikeBuilding, () => 1),
			IncidentEffect.TownBoundVillageRelationChange(() => MobileParty.MainParty.LastVisitedSettlement.Town, -10),
			IncidentEffect.TraitChange(DefaultTraits.Generosity, -100)
		});
		incident55.AddOption("{=xxVE2bw1}Improve the road, but charge no tolls", new List<IncidentEffect>
		{
			IncidentEffect.BuildingLevelChange(GetTurnPikeBuilding, () => 1),
			IncidentEffect.TownBoundVillageRelationChange(() => MobileParty.MainParty.LastVisitedSettlement.Town, 10),
			IncidentEffect.TraitChange(DefaultTraits.Generosity, 100),
			IncidentEffect.GoldChange(() => -1000)
		});
		incident55.AddOption("{=wcbbBjcD}Let it be", new List<IncidentEffect>());
		Incident incident56 = RegisterIncident("incident_endebted_farmers", "{=Vbs9aocS}Endebted Farmers", "{=cRMxKTa8}Several freeholding farmers in your fief have fallen into debt through what appears to be a combination of bad luck and risky investments - a plough horse that died, a crop ruined by frost. They are unable to pay their taxes, and ask for a reprieve for several years.", IncidentTrigger.LeavingTown | IncidentTrigger.LeavingCastle, IncidentType.FiefManagement, CampaignTime.Years(1000f), delegate
		{
			if (MobileParty.MainParty.LastVisitedSettlement == null || !MobileParty.MainParty.LastVisitedSettlement.IsFortification || MobileParty.MainParty.LastVisitedSettlement.OwnerClan != Clan.PlayerClan)
			{
				return false;
			}
			Building building = GetEndebtedFarmersBuilding();
			return building != null && building.CurrentLevel == building.BuildingType.StartLevel;
		});
		incident56.AddOption("{=OM6tkdD7}You know an opportunity when you see it. Seize their lands. They can farm as your tenants", new List<IncidentEffect>
		{
			IncidentEffect.BuildingLevelChange(GetEndebtedFarmersBuilding, () => 1),
			IncidentEffect.TraitChange(DefaultTraits.Mercy, -100),
			IncidentEffect.TraitChange(DefaultTraits.Calculating, 100)
		});
		incident56.AddOption("{=jeWayyGY}A reprieve is fine, but you tack on a bit of interest so that you're not overwhelmed with hard luck stories", new List<IncidentEffect> { IncidentEffect.TraitChange(DefaultTraits.Generosity, 100) });
		incident56.AddOption("{=WHWokYrT}Call in a favor with a moneylender in town to reduce their debts so they can pay their taxes", new List<IncidentEffect>
		{
			IncidentEffect.GoldChange(() => 1000),
			IncidentEffect.InfluenceChange(-20f)
		});
		incident56.AddOption("{=3QobDImA}Help them get back on their feet, even if some will call say that you are too trusting", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Mercy, 200),
			IncidentEffect.TraitChange(DefaultTraits.Calculating, -100),
			IncidentEffect.GoldChange(() => -200)
		});
		Incident incident57 = RegisterIncident("incident_fertility_festival", "{=o5LpNEIa}Fertility Festival", "{=lfJ6OHgX}The village has seen an unexpected rise in pregnancies. Some of your villagers ask your permission to hold a festival in honor of Agalea, protectress of motherhood. Agalea has never been recognized as a saint by the imperial Senate, and many consider her veneration to be a primitive Palaic holder. Her festivals tend to be rather raucous, befitting the theme of fertility, and also rather expensive. It would certainly set tongues wagging in the district. Do you grant permission?", IncidentTrigger.LeavingVillage, IncidentType.FiefManagement, CampaignTime.Years(1000f), delegate
		{
			if (MobileParty.MainParty.LastVisitedSettlement == null)
			{
				return false;
			}
			return MobileParty.MainParty.LastVisitedSettlement.IsVillage && MobileParty.MainParty.LastVisitedSettlement.OwnerClan == Hero.MainHero.Clan && MobileParty.MainParty.LastVisitedSettlement.Culture.StringId == "empire";
		});
		incident57.AddOption("{=EFUNx0cv}A vat of wine for you to dance in honor of the lady Agalea!", new List<IncidentEffect>
		{
			IncidentEffect.GoldChange(() => -200),
			IncidentEffect.TraitChange(DefaultTraits.Generosity, 100),
			IncidentEffect.TraitChange(DefaultTraits.Calculating, -100),
			IncidentEffect.SettlementRelationChange(() => MobileParty.MainParty.LastVisitedSettlement, 10),
			IncidentEffect.InfluenceChange(-100f),
			IncidentEffect.VillageHearthChange(() => MobileParty.MainParty.LastVisitedSettlement.Village, 20),
			IncidentEffect.TownProsperityChange(() => MobileParty.MainParty.LastVisitedSettlement.Village.Bound.Town, -10)
		});
		incident57.AddOption("{=NWNIVZF0}Give the villagers a stern lecture on the importance of sound doctrine and lawful worship", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Generosity, -100),
			IncidentEffect.TraitChange(DefaultTraits.Honor, 100),
			IncidentEffect.SettlementRelationChange(() => MobileParty.MainParty.LastVisitedSettlement, -10),
			IncidentEffect.InfluenceChange(100f),
			IncidentEffect.VillageHearthChange(() => MobileParty.MainParty.LastVisitedSettlement.Village, 5)
		});
		incident57.AddOption("{=J9BsFeSS}Tell the villagers to keep things discrete", new List<IncidentEffect>
		{
			IncidentEffect.VillageHearthChange(() => MobileParty.MainParty.LastVisitedSettlement.Village, 10),
			IncidentEffect.TownProsperityChange(() => MobileParty.MainParty.LastVisitedSettlement.Village.Bound.Town, -5)
		});
		Incident incident58 = RegisterIncident("incident_migrant_influx", "{=OY5LLcxv}Migrant Influx", "{=2KaXj3Qa}The depredations of the {NEARBY_ENEMY_FACTION} has driven many farmers from their homes. Those who have land usually return to it, but many of the poorer are seeking a new place to live. You speak to a few encamped on the outskirts of your village. There is no land for them. They seem eager to work as tenants, but there is always a possibility of tensions with the more established village families.", IncidentTrigger.LeavingVillage, IncidentType.FiefManagement, CampaignTime.Days(60f), delegate(TextObject description)
		{
			if (MobileParty.MainParty.LastVisitedSettlement == null)
			{
				return false;
			}
			if (MobileParty.MainParty.LastVisitedSettlement.OwnerClan != Hero.MainHero.Clan)
			{
				return false;
			}
			Kingdom kingdom = Kingdom.All.FirstOrDefault((Kingdom k) => k.IsAtWarWith(Hero.MainHero.MapFaction));
			if (kingdom == null)
			{
				return false;
			}
			description.SetTextVariable("NEARBY_ENEMY_FACTION", kingdom.Name);
			return MobileParty.MainParty.LastVisitedSettlement.IsVillage && MobileParty.MainParty.LastVisitedSettlement.OwnerClan == Hero.MainHero.Clan;
		});
		incident58.AddOption("{=8JqCDWRd}Welcome them, and give them a donation to purchase small plots of land.", new List<IncidentEffect>
		{
			IncidentEffect.GoldChange(() => -500),
			IncidentEffect.TraitChange(DefaultTraits.Generosity, 100),
			IncidentEffect.VillageHearthChange(() => MobileParty.MainParty.LastVisitedSettlement.Village, 20),
			IncidentEffect.TownSecurityChange(() => MobileParty.MainParty.LastVisitedSettlement.Village.Bound.Town, 10).WithChance(0.25f)
		});
		incident58.AddOption("{=U4UJn9X4}Let them stay. Tell both newcomers and longtime residents to get along, and hope for the best.", new List<IncidentEffect>
		{
			IncidentEffect.VillageHearthChange(() => MobileParty.MainParty.LastVisitedSettlement.Village, 25),
			IncidentEffect.TownSecurityChange(() => MobileParty.MainParty.LastVisitedSettlement.Village.Bound.Town, -10).WithChance(0.5f)
		});
		incident58.AddOption("{=o4aQ2UEL}Tell them to make their homes someplace else.", new List<IncidentEffect> { IncidentEffect.TraitChange(DefaultTraits.Mercy, -100) });
		Incident incident59 = RegisterIncident("incident_nomads_wish_to_settle", "{=5vN2NZ8R}Nomads Wish to Settle", "{=U4Ybz08z}A desperate band of nomads has arrived at your village, seeking a place to settle down. Their herds were ravaged by an epidemic, they say, and they are ready to take up farming or work as laborers. Otherwise, they must starve or give themselves to another tribe as servants. The villagers are skeptical. Nomads are too lazy to be farmers, they say, preferring horse theft and feuds.", IncidentTrigger.LeavingVillage, IncidentType.FiefManagement, CampaignTime.Years(1000f), delegate
		{
			if (MobileParty.MainParty.LastVisitedSettlement == null)
			{
				return false;
			}
			Village village = MobileParty.MainParty.LastVisitedSettlement.Village;
			bool num = new string[3] { "khuzait", "aserai", "empire_s" }.Contains(village.Settlement.Culture.StringId);
			bool flag = Hero.MainHero.Clan.Fiefs.Contains(village.Bound.Town);
			return num && flag;
		});
		incident59.AddOption("{=iSZCkpt3}Welcome them with open arms and a small donation to buy plots of land for their homes", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Generosity, 100),
			IncidentEffect.GoldChange(() => -500),
			IncidentEffect.VillageHearthChange(() => MobileParty.MainParty.LastVisitedSettlement.Village, 20),
			IncidentEffect.TownSecurityChange(() => MobileParty.MainParty.LastVisitedSettlement.Village.Bound.Town, -10).WithChance(0.25f)
		});
		incident59.AddOption("{=taVbaFFW}Sternly instruct them to respect the traditions of settled life, even though the cold welcome might scare some away", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Calculating, 100),
			IncidentEffect.TraitChange(DefaultTraits.Honor, 100),
			IncidentEffect.VillageHearthChange(() => MobileParty.MainParty.LastVisitedSettlement.Village, 10)
		});
		incident59.AddOption("{=jUlHTiSU}Agree with the villagers that nomads are more trouble than they are worth.", new List<IncidentEffect> { IncidentEffect.TraitChange(DefaultTraits.Mercy, -100) });
		Incident incident60 = RegisterIncident("incident_conflict_over_commons_both_yours", "{=uqB1fKFY}Conflict Over Commons", "{=raLLq10c}Grazing land has grown scarce near {TOWN_OR_CASTLE_NAME}, and a heath between {POORER_VILLAGE} and {RICHER_VILLAGE}, covered with thistle and formerly of little value, has become a point of contention. {POORER_VILLAGE} claims that they have been taking goats there for generations, but {RICHER_VILLAGE} has produced a document showing legal title.", IncidentTrigger.LeavingTown | IncidentTrigger.LeavingCastle, IncidentType.FiefManagement, CampaignTime.Years(1000f), delegate(TextObject description)
		{
			if (!MobileParty.MainParty.LastVisitedSettlement.IsFortification || MobileParty.MainParty.LastVisitedSettlement.OwnerClan != Hero.MainHero.Clan)
			{
				return false;
			}
			List<Village> source = MobileParty.MainParty.LastVisitedSettlement.Town.Villages.OrderBy((Village x) => x.Hearth).ToList();
			Village village = source.LastOrDefault();
			Village village2 = source.FirstOrDefault();
			if (village == null || village2 == null || village == village2)
			{
				return false;
			}
			float num = Village.All.Sum((Village x) => x.Hearth) / (float)Village.All.Count;
			if (village.Hearth < num || village2.Hearth < num)
			{
				return false;
			}
			description.SetTextVariable("TOWN_OR_CASTLE_NAME", MobileParty.MainParty.LastVisitedSettlement.Name);
			description.SetTextVariable("RICHER_VILLAGE", village.Name);
			description.SetTextVariable("POORER_VILLAGE", village2.Name);
			return true;
		});
		incident60.AddOption("{=cVzpV96W}The laws of the realm are clear: no matter how many years go by, the original owner retains the land unless it is legally sold or transferred.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Honor, 200),
			IncidentEffect.VillageHearthChange(() => MobileParty.MainParty.LastVisitedSettlement.Town.Villages.OrderBy((Village x) => x.Hearth).LastOrDefault(), 5),
			IncidentEffect.VillageHearthChange(() => MobileParty.MainParty.LastVisitedSettlement.Town.Villages.OrderBy((Village x) => x.Hearth).FirstOrDefault(), -10)
		});
		incident60.AddOption("{=3zEtGAII}You agree with the ancient Palaic principle: the lands belong to the spirits of the earth, not to humans, and go to those who make the best use of them.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Generosity, 100),
			IncidentEffect.TraitChange(DefaultTraits.Honor, -100),
			IncidentEffect.VillageHearthChange(() => MobileParty.MainParty.LastVisitedSettlement.Town.Villages.OrderBy((Village x) => x.Hearth).LastOrDefault(), -5),
			IncidentEffect.VillageHearthChange(() => MobileParty.MainParty.LastVisitedSettlement.Town.Villages.OrderBy((Village x) => x.Hearth).FirstOrDefault(), 10)
		});
		incident60.AddOption("{=wlVCpQHS}Have them split it.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Honor, -50),
			IncidentEffect.VillageHearthChange(() => MobileParty.MainParty.LastVisitedSettlement.Town.Villages.OrderBy((Village x) => x.Hearth).FirstOrDefault(), 5)
		});
		incident60.AddOption("{=YWZFOX4S}Let them know that your decision is for sale at a price", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Honor, -100),
			IncidentEffect.GoldChange(() => 1000),
			IncidentEffect.VillageHearthChange(() => MobileParty.MainParty.LastVisitedSettlement.Town.Villages.OrderBy((Village x) => x.Hearth).LastOrDefault(), 5),
			IncidentEffect.VillageHearthChange(() => MobileParty.MainParty.LastVisitedSettlement.Town.Villages.OrderBy((Village x) => x.Hearth).FirstOrDefault(), -10)
		});
		Incident incident61 = RegisterIncident("incident_conflict_over_commons", "{=uqB1fKFY}Conflict Over Commons", "{=jGafCYMe}Villagers from your fief of {VILLAGE} have started going to a marsh on the road to {VILLAGE2} to gather reeds for baskets, supplementing their earnings. Hunters from {VILLAGE2}, owned by {LORD.NAME}, say they've been disturbing the local waterfowl. Neither of you has title to the marsh, but {VILLAGE2} says that its people started going there first. Your tenants say it's not their problem if they've figured out a better way to make use of the resources.", IncidentTrigger.LeavingTown | IncidentTrigger.LeavingCastle, IncidentType.FiefManagement, CampaignTime.Years(1000f), delegate(TextObject description)
		{
			if (!MobileParty.MainParty.LastVisitedSettlement.IsFortification || MobileParty.MainParty.LastVisitedSettlement.OwnerClan != Hero.MainHero.Clan)
			{
				return false;
			}
			Village village = GetVillageOne();
			Village village2 = GetOtherVillage();
			if (village2?.Settlement.Owner == null)
			{
				return false;
			}
			description.SetTextVariable("VILLAGE", village.Name);
			description.SetTextVariable("VILLAGE2", village2.Name);
			description.SetCharacterProperties("LORD", village2.Settlement.Owner.CharacterObject);
			return true;
		});
		incident61.AddOption("{=8gTYsXZM}Continue to send your reed gatherers there, and have them bring staves and slings to defend their rights", new List<IncidentEffect>
		{
			IncidentEffect.SettlementRelationChange(() => GetOtherVillage().Settlement, -10),
			IncidentEffect.HeroRelationChange(() => GetOtherVillage().Settlement.OwnerClan.Leader, -10),
			IncidentEffect.VillageHearthChange(GetVillageOne, -5).WithChance(0.25f),
			IncidentEffect.TownProsperityChange(() => MobileParty.MainParty.LastVisitedSettlement.Town, 20),
			IncidentEffect.TraitChange(DefaultTraits.Valor, 100)
		});
		incident61.AddOption("{=0xiAZr96}Tell {NOBLE.NAME} that your people will get their reeds elsewhere and stop frightening off the game.", new List<IncidentEffect>
		{
			IncidentEffect.TownProsperityChange(() => MobileParty.MainParty.LastVisitedSettlement.Town, 5),
			IncidentEffect.TraitChange(DefaultTraits.Valor, -100),
			IncidentEffect.HeroRelationChange(() => GetOtherVillage().Settlement.OwnerClan.Leader, 20)
		}, delegate(TextObject text)
		{
			Village village = GetOtherVillage();
			text.SetCharacterProperties("NOBLE", village.Settlement.OwnerClan.Leader.CharacterObject);
			return true;
		});
		Incident incident62 = RegisterIncident("incident_educational_advancements", "{=RZJ0f8qo}Educational Advancements", "{=2OQlwt7x}Villagers say that a wanderer has recently arrived and offered to teach their children to read. He is not only well-educated, but he has a gift for holding the attention of restless 10-year-olds. However, he seems to have spent time with one of the rebel bands that were common around here ten years back, and occasionally speaks of a coming reign of justice brought by Heaven, in a way that doesn't seem like one of his ironic jokes.", IncidentTrigger.LeavingVillage, IncidentType.FiefManagement, CampaignTime.Years(1000f), (TextObject description) => MobileParty.MainParty.LastVisitedSettlement != null && Hero.MainHero.Clan.Fiefs.Contains(MobileParty.MainParty.LastVisitedSettlement.Village.Bound.Town));
		incident62.AddOption("{=Wuga4ZqG}Let him stay. It's good for children to read, and it's not like they take their elders seriously anyway", new List<IncidentEffect>
		{
			IncidentEffect.TownSecurityChange(() => MobileParty.MainParty.LastVisitedSettlement.Village.Bound.Town, -5),
			IncidentEffect.TownProsperityChange(() => MobileParty.MainParty.LastVisitedSettlement.Village.Bound.Town, 10),
			IncidentEffect.InfluenceChange(20f)
		});
		incident62.AddOption("{=zEPpgYaf}Reading won't help a farmboy plough, but might goad him to do foolish things that get him killed", new List<IncidentEffect> { IncidentEffect.TraitChange(DefaultTraits.Calculating, 100) });
		incident62.AddOption("{=3OerG5Ik}Take the teacher aside, and tell him the elder will pay him a handsome stipend that will stop abruptly if he breathes a word about politics or theology.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Calculating, 100),
			IncidentEffect.TownProsperityChange(() => MobileParty.MainParty.LastVisitedSettlement.Village.Bound.Town, 10),
			IncidentEffect.GoldChange(() => -200)
		});
		Incident incident63 = RegisterIncident("incident_gold_rush", "{=8jG2vlOu}Gold Rush", "{=eQz8b4TH}The village is buzzing with a poorly kept secret. Following a rare but fierce rainstorm in the dry hills, fishermen have begun to find small flakes of gold in a local stream. If word gets out, there will be a huge influx of fortune-seekers into the area. The gold will last a few months, and it's not clear how much of it will benefit the village or its owner.", IncidentTrigger.LeavingVillage, IncidentType.FiefManagement, CampaignTime.Years(1000f), (TextObject description) => MobileParty.MainParty.LastVisitedSettlement != null && Hero.MainHero.Clan.Fiefs.Contains(MobileParty.MainParty.LastVisitedSettlement.Village.Bound.Town));
		incident63.AddOption("{=mEqk8UCG}This region could use more people. Let word get out.", new List<IncidentEffect>
		{
			IncidentEffect.VillageHearthChange(() => MobileParty.MainParty.LastVisitedSettlement.Village, 20),
			IncidentEffect.TownSecurityChange(() => MobileParty.MainParty.LastVisitedSettlement.Village.Bound.Town, -10),
			IncidentEffect.TraitChange(DefaultTraits.Calculating, 100),
			IncidentEffect.InfestNearbyHideout(() => MobileParty.MainParty.LastVisitedSettlement)
		});
		incident63.AddOption("{=aNGQJJkM}As liege of the village, the gold belongs to you. Post guards along the river to stop unauthorized prospecting", new List<IncidentEffect>
		{
			IncidentEffect.GoldChange(() => 1000),
			IncidentEffect.SettlementRelationChange(() => MobileParty.MainParty.LastVisitedSettlement, -10),
			IncidentEffect.TraitChange(DefaultTraits.Generosity, -200)
		});
		incident63.AddOption("{=cECwyvjY}Encourage the villagers to mine it stealthily, ensuring their cooperation by allowing them to keep it", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Generosity, 200),
			IncidentEffect.TownProsperityChange(() => MobileParty.MainParty.LastVisitedSettlement.Village.Bound.Town, 100),
			IncidentEffect.TownSecurityChange(() => MobileParty.MainParty.LastVisitedSettlement.Village.Bound.Town, -5)
		});
		Incident incident64 = RegisterIncident("incident_wages_of_war_and_diseases", "{=7q53TLg7}Wages of war and diseases", "{=FZXYHC7s}Between the wars and an epidemic, the villages near {FIEF_NAME} are experiencing a shortage of people, especially young men. Fields are going fallow for lack of hands to plough them, especially those cleared only in recent generations where the title is in dispute. As liege, you can redistribute this unused land, but you will need to find labor.", IncidentTrigger.LeavingTown | IncidentTrigger.LeavingCastle, IncidentType.FiefManagement, CampaignTime.Days(60f), delegate(TextObject description)
		{
			if (MobileParty.MainParty.LastVisitedSettlement == null || !MobileParty.MainParty.LastVisitedSettlement.IsFortification || MobileParty.MainParty.LastVisitedSettlement.OwnerClan != Clan.PlayerClan)
			{
				return false;
			}
			Building building = GetWagesOfWarAndDiseasesBuilding();
			if (building == null)
			{
				return false;
			}
			float num = Village.All.Average((Village x) => x.Hearth);
			if (MobileParty.MainParty.LastVisitedSettlement.BoundVillages.Average((Village x) => x.Hearth) > num)
			{
				return false;
			}
			description.SetTextVariable("FIEF_NAME", MobileParty.MainParty.LastVisitedSettlement.Name);
			return building.CurrentLevel == building.BuildingType.StartLevel;
		});
		incident64.AddOption("{=wnvUsbSs}Seize the land as your own, attracting desperate peasants from surrounding districts with exploitative sharecropping agreements", new List<IncidentEffect>
		{
			IncidentEffect.BuildingLevelChange(GetWagesOfWarAndDiseasesBuilding, () => 1),
			IncidentEffect.TraitChange(DefaultTraits.Mercy, -100),
			IncidentEffect.TownBoundVillageHearthChange(() => MobileParty.MainParty.LastVisitedSettlement.Town, 10)
		});
		incident64.AddOption("{=8jCcqBSV}Distribute the land to some of your veterans as a reward for their service", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Generosity, 200),
			IncidentEffect.KillTroopsRandomly((TroopRosterElement x) => !x.Character.IsHero, () => 10),
			IncidentEffect.TownBoundVillageHearthChange(() => MobileParty.MainParty.LastVisitedSettlement.Town, 20)
		});
		incident64.AddOption("{=QSEZfwVk}Distribute money to the region's farmers so they can hire agricultural laborers or provide attractive dowries for their daughters ", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Mercy, 200),
			IncidentEffect.TownBoundVillageHearthChange(() => MobileParty.MainParty.LastVisitedSettlement.Town, 30),
			IncidentEffect.GoldChange(() => -500),
			IncidentEffect.TownProsperityChange(() => MobileParty.MainParty.LastVisitedSettlement.Town, 10)
		});
		Incident incident65 = RegisterIncident("incident_successful_harvest", "{=MaaFv4Po}Successful Harvest", "{=evcKUaab}An early thaw and a warm spring has ensured a bumper crop, which the prayers of the villagers and the efforts of the local cats have protected from rats and birds. Such bounties occur rarely. What will you do?", IncidentTrigger.LeavingTown | IncidentTrigger.LeavingCastle, IncidentType.FiefManagement, CampaignTime.Days(60f), delegate
		{
			if (MobileParty.MainParty.LastVisitedSettlement == null)
			{
				return false;
			}
			return MobileParty.MainParty.LastVisitedSettlement.OwnerClan == Hero.MainHero.Clan && (CampaignTime.Now.GetSeasonOfYear == CampaignTime.Seasons.Summer || CampaignTime.Now.GetSeasonOfYear == CampaignTime.Seasons.Autumn);
		});
		incident65.AddOption("{=HT1mQuCI}Feast! Thank Heaven and the spirits of the land, and make merry", new List<IncidentEffect>
		{
			IncidentEffect.MoraleChange(10f),
			IncidentEffect.TownBoundVillageRelationChange(() => MobileParty.MainParty.LastVisitedSettlement.Town, 5),
			IncidentEffect.TownBoundVillageHearthChange(() => MobileParty.MainParty.LastVisitedSettlement.Town, 10),
			IncidentEffect.TownProsperityChange(() => MobileParty.MainParty.LastVisitedSettlement.Town, 5),
			IncidentEffect.TraitChange(DefaultTraits.Generosity, 200),
			IncidentEffect.TraitChange(DefaultTraits.Calculating, -100)
		});
		incident65.AddOption("{=c5vobNeb}Encourage the farmers to sell the crops and make much-needed improvements to irrigation and roads", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Calculating, 100),
			IncidentEffect.TownProsperityChange(() => MobileParty.MainParty.LastVisitedSettlement.Town, 20)
		});
		incident65.AddOption("{=ZaBiC5XV}Go through your accounts to see who might owe you back taxes", new List<IncidentEffect>
		{
			IncidentEffect.GoldChange(() => 1000),
			IncidentEffect.TraitChange(DefaultTraits.Generosity, -200)
		});
		Incident incident66 = RegisterIncident("incident_haunted_dreams", "{=99w595qn}Haunted Dreams", "{=yXXbY1bb}You lie down for a well-deserved nap, but you slip into an uneasy dream. You are sailing on a wind-tossed sea. A wave crashes on your bow, sending spray across the decks. You look closely, and see that it is not water but blood sloshing around across your feet. You stare out into the sea, and the breakers take on the forms of men you have slain in battle.", IncidentTrigger.WaitingInSettlement, IncidentType.DreamsSongsAndSigns, CampaignTime.Years(1000f), (TextObject description) => Hero.MainHero.Clan.Renown >= 200f && Campaign.Current.GetCampaignBehavior<IStatisticsCampaignBehavior>().GetNumberOfTroopsKnockedOrKilledByPlayer() > 0);
		incident66.AddOption("{=sxrBpIUk}Have I killed without good reason? Heaven wishes me to repent! I shall choose the peaceful path. When there is one, that is...", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Mercy, 200),
			IncidentEffect.TraitChange(DefaultTraits.Calculating, -100)
		});
		incident66.AddOption("{=a0MWe8GO}To where do we sail on this sea of blood? To glory, that is where!", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Valor, 200),
			IncidentEffect.TraitChange(DefaultTraits.Calculating, -100)
		});
		incident66.AddOption("{=B1cyEaQA}Dreams are but a temporary madness. Heaven does not speak to us in this way.", new List<IncidentEffect> { IncidentEffect.TraitChange(DefaultTraits.Calculating, 100) });
		Incident incident67 = RegisterIncident("incident_market_manipulation", "{=tGwbM59L}Market Manipulation", "{=5OuDl2Po}As you come into town, the merchant {MERCHANT_NAME} makes you an offer. Local merchants are on edge awaiting an incoming caravan from {PLACE}. If you claim that you found its wreckage, looted by bandits, the prices of goods would rise considerably. He offers to pay you 1000 denars, in addition to whatever extra you can make by selling your own wares once the prices rise.", IncidentTrigger.EnteringTown, IncidentType.Profit, CampaignTime.Years(1000f), delegate(TextObject description)
		{
			if (MobileParty.MainParty.CurrentSettlement == null)
			{
				return false;
			}
			Town randomElement = Town.AllTowns.GetRandomElement();
			Hero hero = GetMarketManipulationMerchant();
			if (hero == null)
			{
				return false;
			}
			description.SetTextVariable("MERCHANT_NAME", hero.Name);
			description.SetTextVariable("PLACE", randomElement.Name);
			return Hero.MainHero.GetSkillValue(DefaultSkills.Trade) >= 20;
		});
		incident67.AddOption("{=SLASexva}Accept, but leave enough room for doubt in your account that it won't look too bad if the caravan turns up.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Calculating, 100),
			IncidentEffect.TraitChange(DefaultTraits.Honor, -100),
			IncidentEffect.GoldChange(() => 1000),
			IncidentEffect.SkillChange(DefaultSkills.Trade, 50f),
			IncidentEffect.Custom(null, delegate
			{
				TownMarketData marketData = Settlement.CurrentSettlement.Town.MarketData;
				foreach (ItemCategory item2 in ItemCategories.All.Where((ItemCategory x) => x.IsTradeGood))
				{
					marketData.SetDemand(item2, marketData.GetDemand(item2) * 1.1f);
				}
				return new List<TextObject> { new TextObject("{=OI4jkYuY}Demands of trade goods raised by {AMOUNT}% in {TOWN}").SetTextVariable("AMOUNT", 10).SetTextVariable("TOWN", Settlement.CurrentSettlement.Name) };
			}, (IncidentEffect effect) => new List<TextObject> { new TextObject("{=uiomHbfS}Raise demand of trade goods by {AMOUNT}% in {TOWN}").SetTextVariable("AMOUNT", 10).SetTextVariable("TOWN", Settlement.CurrentSettlement.Name) })
		});
		incident67.AddOption("{=EyHD7gjy}Say you'll swear up and down that you saw the wreckage, damaging your reputation, but demand 2000 denars instead.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Calculating, -200),
			IncidentEffect.TraitChange(DefaultTraits.Honor, -200),
			IncidentEffect.GoldChange(() => 2000),
			IncidentEffect.SkillChange(DefaultSkills.Trade, 100f),
			IncidentEffect.Custom(null, delegate
			{
				TownMarketData marketData = Settlement.CurrentSettlement.Town.MarketData;
				foreach (ItemCategory item3 in ItemCategories.All.Where((ItemCategory x) => x.IsTradeGood))
				{
					marketData.SetDemand(item3, marketData.GetDemand(item3) * 1.1f);
				}
				return new List<TextObject> { new TextObject("{=OI4jkYuY}Demands of trade goods raised by {AMOUNT}% in {TOWN}").SetTextVariable("AMOUNT", 10).SetTextVariable("TOWN", Settlement.CurrentSettlement.Name) };
			}, (IncidentEffect effect) => new List<TextObject> { new TextObject("{=uiomHbfS}Raise demand of trade goods by {AMOUNT}% in {TOWN}").SetTextVariable("AMOUNT", 10).SetTextVariable("TOWN", Settlement.CurrentSettlement.Name) })
		});
		incident67.AddOption("{=aMMxVIpa}Erupt in anger for all to see, and curse {MERCHANT} for taking you for a base liar.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Calculating, -100),
			IncidentEffect.TraitChange(DefaultTraits.Honor, 100),
			IncidentEffect.SettlementRelationChange(() => Settlement.CurrentSettlement, 10),
			IncidentEffect.HeroRelationChange(GetMarketManipulationMerchant, -20)
		}, delegate(TextObject text)
		{
			Hero hero = GetMarketManipulationMerchant();
			text.SetTextVariable("MERCHANT", hero.Name);
			return true;
		});
		incident67.AddOption("{=UbZMlyk6}Take a pass on the matter.", new List<IncidentEffect>());
		Incident incident68 = RegisterIncident("incident_gnostic_boys", "{=hewpozXg}Gnostic Boys", "{=H7Co1wqC}Two of your {NOBLE_TIER} have lately been causing a stir in the camp with their irreverent religious preaching. They propound that this world is a illusion, created by a being called the Demiurge for its own amusement, and humans are but game-pieces. You think this may be the Marcorionist doctrine, declared heretical by the imperial Senate some years ago. It is clearly disturbing your more traditional-minded troops and is probably also not good for your party's reputation.", IncidentTrigger.LeavingVillage, IncidentType.DreamsSongsAndSigns, CampaignTime.Years(1000f), delegate(TextObject description)
		{
			if (MobileParty.MainParty.LastVisitedSettlement == null || !MobileParty.MainParty.LastVisitedSettlement.IsVillage || MobileParty.MainParty.LastVisitedSettlement.Village.Bound.IsCastle)
			{
				return false;
			}
			CharacterObject characterObject = GetGnosticBoysNobleTroop();
			if (characterObject == null)
			{
				return false;
			}
			description.SetTextVariable("NOBLE_TIER", characterObject.Name);
			return true;
		});
		incident68.AddOption("{=WyuxrGqC}Tell them, in a towering fury, that if they do not wish to be burned then they had best either leave your party or cease this sacrilege", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Calculating, -100),
			IncidentEffect.MoraleChange(20f),
			IncidentEffect.InfluenceChange(50f),
			IncidentEffect.ChangeTroopAmount(GetGnosticBoysNobleTroop, -2)
		});
		incident68.AddOption("{=4WZ5CWm8}Take them aside and tell them that while nihilism is fine for a boozy salon in {TOWN_NAME}, it's a bit much for men who must every day put their lives on the line.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Calculating, 100),
			IncidentEffect.MoraleChange(-20f).WithChance(0.5f)
		}, delegate(TextObject text)
		{
			text.SetTextVariable("TOWN_NAME", MobileParty.MainParty.LastVisitedSettlement.Village.Bound.Name);
			return true;
		});
		incident68.AddOption("{=vzFoybv0}Tell your party that, if this is true, then men are free to choose their own purpose in this world - and anyone who stands with you should make his purpose to help those in need.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Mercy, 100),
			IncidentEffect.InfluenceChange(-50f)
		});
		incident68.AddOption("{=e07rQCcq}Tell the men that this is splendid news for brave men like themselves, as surely nothing would amuse the Demiurge more than a good fight.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Valor, 200),
			IncidentEffect.TraitChange(DefaultTraits.Calculating, -100),
			IncidentEffect.MoraleChange(20f),
			IncidentEffect.InfluenceChange(-50f)
		});
		Incident incident69 = RegisterIncident("incident_speculative_investment", "{=12bmA5qD}Speculative Investment", "{=vPNDzUAF}You strike up a conversation with a trader in town, who is desperate for news of the wars. When you tell him about the looting of {VILLAGE_NAME}, he makes a proposition. The price of grain will almost certainly rise, and he intends to buy up food now to sell it later when the hunger hits. He needs money, and he will sell you {OTHER_GOOD} at far below their market value - 1000 denars. He asks you also to keep this information to yourself.", IncidentTrigger.LeavingTown, IncidentType.Profit, CampaignTime.Years(1000f), delegate(TextObject description)
		{
			VillageStateChangedLogEntry villageStateChangedLogEntry = Campaign.Current.LogEntryHistory.FindLastGameActionLog((VillageStateChangedLogEntry x) => x.NewState == Village.VillageStates.Looted && x.Village.IsProducing(DefaultItems.Grain));
			if (villageStateChangedLogEntry == null)
			{
				return false;
			}
			description.SetTextVariable("VILLAGE_NAME", villageStateChangedLogEntry.Village.Name);
			ItemObject itemObject = GetSpeculativeInvestmentOtherGood();
			description.SetTextVariable("OTHER_GOOD", itemObject.Name);
			return Hero.MainHero.GetSkillValue(DefaultSkills.Trade) >= 20;
		});
		incident69.AddOption("{=aUui6FYp}All's fair in trade and war. You accept the deal.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Calculating, 50),
			IncidentEffect.TraitChange(DefaultTraits.Mercy, -50),
			IncidentEffect.GoldChange(() => -500),
			IncidentEffect.ChangeItemAmount(GetSpeculativeInvestmentOtherGood, delegate
			{
				ItemObject itemObject = GetSpeculativeInvestmentOtherGood();
				return (int)Math.Round(1000f / (float)itemObject.Value);
			})
		});
		incident69.AddOption("{=2xvqOUJh}Tell him quietly that you will not profit off of the misery of others.", new List<IncidentEffect> { IncidentEffect.TraitChange(DefaultTraits.Mercy, 100) });
		incident69.AddOption("{=hnMUxRyt}Denounce him angrily in public as a food hoarder for all to hear, so that word gets around of your compassion and mercy.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Mercy, 100),
			IncidentEffect.TraitChange(DefaultTraits.Calculating, -150),
			IncidentEffect.RenownChange(5f)
		});
		Incident incident70 = RegisterIncident("incident_mad_dog", "{=iuAMSSTY}Mad Dog", "{=nyZpbVbv}Your men are very proud of their leader - perhaps too proud. Every time they go into town, they spread exaggerated tales of your recklessness in battle. You imagine that this could win you a few admiring recruits, but more and more you've caught a look that suggests fear or distrust, as though you're a wild animal or a demon.", IncidentTrigger.LeavingVillage, IncidentType.TroopSettlementRelation, CampaignTime.Years(1000f), (TextObject description) => Hero.MainHero.Clan.Renown >= 300f && PartyBase.MainParty.MemberRoster.TotalRegulars >= 5);
		incident70.AddOption("{=bbH4PqtD}Let the tales spread! Sanity and caution do not inspire legend", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Calculating, -300),
			IncidentEffect.RenownChange(10f),
			IncidentEffect.TraitChange(DefaultTraits.Valor, 100),
			IncidentEffect.ChangeTroopAmount(GetMadDogTroop, 1)
		});
		incident70.AddOption("{=Qbdym8vH}In a towering rage, tell your men that you will not tolerate lies of any sort.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Calculating, -100),
			IncidentEffect.TraitChange(DefaultTraits.Honor, 100)
		});
		incident70.AddOption("{=EzpB6hJG}Try to play down the image of a madman by being as friendly as possible on your next visit to town.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Valor, -100),
			IncidentEffect.TraitChange(DefaultTraits.Generosity, 100)
		});
		Incident incident71 = RegisterIncident("incident_besiegers_blues", "{=uTsdJCSU}Besiegers' Blues", "{=IxgJotDk}At first this siege gave your men a welcome respite from the hardships of the march, but now inactivity and boredom has started to take a toll. Even dice and gossip have lost their charms, and you're starting to see quarrels and lapses in discipline.", IncidentTrigger.DuringSiege, IncidentType.Siege, CampaignTime.Years(1000f), (TextObject description) => MobileParty.MainParty.MemberRoster.TotalRegulars >= 40 && PlayerSiege.PlayerSiegeEvent?.BesiegerCamp.SiegeEngines?.SiegePreparations != null);
		incident71.AddOption("{=LUap9uP0}Drill your men for the upcoming assault, even though the ever-present risk of injury might put several out of commission when they're needed most.", new List<IncidentEffect>
		{
			IncidentEffect.PartyExperienceChance(100),
			IncidentEffect.WoundTroopsRandomly(0.05f).WithChance(0.5f)
		});
		incident71.AddOption("{=GTPPtara}March about the camp, searching for specks of rust on swords or sentries who aren't fully attentive to their watch. Give any violators hard, pointless tasks to teach them to keep their standards up.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Calculating, -100),
			IncidentEffect.SiegeProgressChange(() => PlayerSiege.PlayerSiegeEvent.BesiegerCamp.SiegeEngines.SiegePreparations.Progress * 0.2f)
		});
		incident71.AddOption("{=bp8kKb1A}Let the men enjoy their rest.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Generosity, 100),
			IncidentEffect.MoraleChange(-20f)
		});
		Incident incident72 = RegisterIncident("incident_swaggering_shield_brothers", "{=KtJYTHVi}Swaggering Shield-Brothers", "{=YCPF4qyz}You are approached by a pair of Skolderbroda. They are anxious to fight for you and ask only that you pay them a token sum. They say you that a warlord like you deserves the services of men such as themselves - praise to you, but a slight to the rest of your party. One of your troops tells you that he's sure that these men must have broken their vows to their company, or why else would they be so eager to join you?", IncidentTrigger.LeavingVillage, IncidentType.PartyCampLife, CampaignTime.Years(1000f), (TextObject description) => Hero.MainHero.Clan.Renown >= 150f && PartyBase.MainParty.MemberRoster.TotalRegulars >= 5);
		incident72.AddOption("{=Kqr9pVCU}Take them into your company. Your troops will need to bear any insult.", new List<IncidentEffect>
		{
			IncidentEffect.ChangeTroopAmount(GetSkolderbrotvaTroop, 2),
			IncidentEffect.TraitChange(DefaultTraits.Generosity, -200),
			IncidentEffect.MoraleChange(-5f)
		});
		incident72.AddOption("{=78VrrDRM}Tell them that you believe them to be oathbreakers, and as such have no place in your ranks.", new List<IncidentEffect> { IncidentEffect.TraitChange(DefaultTraits.Honor, 100) });
		incident72.AddOption("{=1b9oYu6v}Tell them they can join you and be as arrogant as they like - so long as they beat one of your men in a duel to first blood.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Valor, 100),
			IncidentEffect.Group(IncidentEffect.ChangeTroopAmount(GetSkolderbrotvaTroop, 2), IncidentEffect.WoundTroop(() => (from x in MobileParty.MainParty.MemberRoster.GetTroopRoster()
				orderby x.Character.Tier descending
				select x).FirstOrDefault((TroopRosterElement x) => !x.Character.IsHero).Character, 1)).WithChance(0.5f)
		});
		Incident incident73 = RegisterIncident("incident_ill_conceived_plans", "{=VSLbywBT}Ill-Conceived Plans", "{=K8DJmY4L}Several of your {TROOP_TYPE} have an idea for the next battle. They suggest that they be hidden in a supply wagon and left for the enemy to discover and drag into camp, whereupon they will leap out and wreak havoc. They are very proud of their scheme, even though you can think of half a dozen reasons it won't work.", IncidentTrigger.LeavingVillage, IncidentType.PartyCampLife, CampaignTime.Years(1000f), delegate(TextObject description)
		{
			if (MobileParty.MainParty.MemberRoster.TotalRegulars < 10)
			{
				return false;
			}
			CharacterObject character = IncidentHelper.GetSeededRandomElement((from x in MobileParty.MainParty.MemberRoster.GetTroopRoster()
				where !x.Character.IsHero
				select x).ToList(), _activeIncidentSeed).Character;
			if (character == null)
			{
				return false;
			}
			description.SetTextVariable("TROOP_TYPE", character.Name);
			return true;
		});
		incident73.AddOption("{=X68PZrYv}Tell them that every people in Calradia has its own legend about warriors concealed in unlikely places, and you can't imagine them getting away with it.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Calculating, 100),
			IncidentEffect.TraitChange(DefaultTraits.Generosity, -100)
		});
		incident73.AddOption("{=Gsazqfhj}Let them test their plan. Buy a hide, sew the men into it with their weapons, and gather the company to watch in amusement as they struggle to free themselves.", new List<IncidentEffect>
		{
			IncidentEffect.GoldChange(() => -50),
			IncidentEffect.TraitChange(DefaultTraits.Calculating, 50),
			IncidentEffect.DisorganizeParty(),
			IncidentEffect.PartyExperienceChance(100)
		});
		incident73.AddOption("{=YhxsQjGM}Nod quickly as though you weren't really listening and move on to some other task.", new List<IncidentEffect> { IncidentEffect.TraitChange(DefaultTraits.Calculating, -50) });
		Incident incident74 = RegisterIncident("incident_heat_and_dust", "{=HoNCyyfW}Heat and Dust", "{=42bGMh4X}Your men were not expecting to spend so much time in the desert. They complain that baggage that they might gladly carry themselves in normal times becomes a crushing burden when the blazing sun is high in the sky. They have located a number of pack animals in the markets and ask you to buy them to lighten their own loads.", IncidentTrigger.LeavingVillage, IncidentType.HardTravel, CampaignTime.Years(1000f), (TextObject description) => Campaign.Current.MapSceneWrapper.GetFaceTerrainType(MobileParty.MainParty.Position.Face) == TerrainType.Desert && PartyBase.MainParty.MemberRoster.TotalRegulars >= 5);
		incident74.AddOption("{=ACzeec4e}Agree to purchase the camels.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Generosity, 50),
			IncidentEffect.ChangeItemAmount(() => Game.Current.ObjectManager.GetObject<ItemObject>("camel"), () => 4),
			IncidentEffect.GoldChange(() => -1000)
		});
		incident74.AddOption("{=uD8su80f}Tell them that you need the money for other purposes.", new List<IncidentEffect> { IncidentEffect.TraitChange(DefaultTraits.Generosity, -200) });
		Incident incident75 = RegisterIncident("incident_preemptive_work", "{=Zd3jBBun}Preemptive Work", "{=3IphvZTK}In your absence, several families in your district have been hard at work draining a marsh and turning it into a field. The survey records in your treasury however indicate clearly that the marsh is part of your domain. They almost certainly did not know this, but you are entitled to claim the field or charge them for working it.", IncidentTrigger.EnteringTown | IncidentTrigger.EnteringCastle, IncidentType.FiefManagement, CampaignTime.Years(1000f), delegate
		{
			if (MobileParty.MainParty.LastVisitedSettlement == null || !MobileParty.MainParty.LastVisitedSettlement.IsFortification || MobileParty.MainParty.LastVisitedSettlement.OwnerClan != Clan.PlayerClan)
			{
				return false;
			}
			Settlement lastVisitedSettlement = MobileParty.MainParty.LastVisitedSettlement;
			bool flag = false;
			for (float num = -3f; num <= 3f; num += 0.25f)
			{
				for (float num2 = -3f; num2 <= 3f; num2 += 0.25f)
				{
					if (num * num + num2 * num2 <= 9f)
					{
						CampaignVec2 campaignVec = lastVisitedSettlement.Position + new Vec2(num, num2);
						if (campaignVec.Face.IsValid() && Campaign.Current.MapSceneWrapper.GetFaceTerrainType(campaignVec.Face) == TerrainType.Desert)
						{
							flag = true;
							break;
						}
					}
				}
				if (flag)
				{
					break;
				}
			}
			if (!flag)
			{
				return false;
			}
			Building building = GetPreemptiveWorkBuilding();
			return building != null && building.CurrentLevel == building.BuildingType.StartLevel;
		});
		incident75.AddOption("{=9e8I99t3}Transfer ownership to the farmers to encourage such enterprise, even if others may take advantage of you in the future.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Generosity, 100),
			IncidentEffect.TraitChange(DefaultTraits.Calculating, -50),
			IncidentEffect.TownProsperityChange(() => MobileParty.MainParty.LastVisitedSettlement.Town, 20)
		});
		incident75.AddOption("{=j0HmgDOA}Maintain your claim to the land, but allow the farmers to make use of it for only a token rent.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Generosity, -100),
			IncidentEffect.TraitChange(DefaultTraits.Calculating, 50),
			IncidentEffect.TownProsperityChange(() => MobileParty.MainParty.LastVisitedSettlement.Town, 15),
			IncidentEffect.GoldChange(() => 500)
		});
		incident75.AddOption("{=xcPJPtaT}Demand the land for yourself.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Generosity, -100),
			IncidentEffect.BuildingLevelChange(GetPreemptiveWorkBuilding, () => 1),
			IncidentEffect.TownBoundVillageRelationChange(() => MobileParty.MainParty.LastVisitedSettlement.Town, -20)
		});
		Incident incident76 = RegisterIncident("incident_through_proper_channels", "{=S12S4pmL}Through the proper channels", "{=3eOOT66E}One of your tenants has a proposal for you. He proposes to dig a channel known as a qanah into a hillside, where the water table is high, which will allow him to flood some low-lying ground and turn it into an oasis garden. The barren hillside is your property, though at present it yields no income of any kind.", IncidentTrigger.EnteringTown | IncidentTrigger.EnteringCastle, IncidentType.FiefManagement, CampaignTime.Years(1000f), delegate
		{
			if (MobileParty.MainParty.CurrentSettlement == null)
			{
				return false;
			}
			Settlement currentSettlement = MobileParty.MainParty.CurrentSettlement;
			bool flag = false;
			for (float num = -3f; num <= 3f; num += 0.25f)
			{
				for (float num2 = -3f; num2 <= 3f; num2 += 0.25f)
				{
					if (num * num + num2 * num2 <= 9f)
					{
						CampaignVec2 campaignVec = currentSettlement.Position + new Vec2(num, num2);
						if (campaignVec.Face.IsValid() && Campaign.Current.MapSceneWrapper.GetFaceTerrainType(campaignVec.Face) == TerrainType.Desert)
						{
							flag = true;
							break;
						}
					}
				}
				if (flag)
				{
					break;
				}
			}
			if (!flag)
			{
				return false;
			}
			return MobileParty.MainParty.CurrentSettlement.IsFortification && Hero.MainHero.Clan == MobileParty.MainParty.CurrentSettlement.OwnerClan;
		});
		incident76.AddOption("{=7f06ovrO}Applaud his plan, and tell him that you will charge him nothing to sink a channel through your property.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Generosity, 100),
			IncidentEffect.TownProsperityChange(() => MobileParty.MainParty.CurrentSettlement.Town, 20)
		});
		incident76.AddOption("{=iB2g7XBt}Insist he purchase the hillside from your at market rate.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Generosity, -100),
			IncidentEffect.TownProsperityChange(() => MobileParty.MainParty.CurrentSettlement.Town, 15),
			IncidentEffect.GoldChange(() => 300)
		});
		incident76.AddOption("{=P8W5OQB9}Wait until he digs the channel and clears the fields, then charge him heavily to use the water without which his effort will be for nothing.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Generosity, -200),
			IncidentEffect.TraitChange(DefaultTraits.Calculating, 50),
			IncidentEffect.TownProsperityChange(() => MobileParty.MainParty.CurrentSettlement.Town, 10),
			IncidentEffect.GoldChange(() => 500)
		});
		Incident incident77 = RegisterIncident("incident_dodgy_spice", "{=5OzX71s3}Dodgy Spice", "{=QEP3ntdb}Merchants are savvy to most attempts to pass off most types of shoddy goods as top quality, but one of your {TROOP.NAME} knows a few tricks that aren't in the usual rogue's repetoire. He suggests buying a selection of spices from the market, then sprinkling a bit of pomegranate rind in with the saffron, some cassia bark with the cinnamon, and a lead compound known as litharge with the pepper.", IncidentTrigger.LeavingTown, IncidentType.Profit, CampaignTime.Years(1000f), delegate(TextObject description)
		{
			if (Hero.MainHero.GetSkillValue(DefaultSkills.Trade) < 10)
			{
				return false;
			}
			TroopRosterElement troopRosterElement = GetRandomTroop();
			if (troopRosterElement.Character == null)
			{
				return false;
			}
			description.SetCharacterProperties("TROOP", troopRosterElement.Character);
			return true;
		});
		incident77.AddOption("{=lrysxcAE}A bit of lead never hurt anyone! Try them all, and split the profits with your {TROOP.NAME}.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Generosity, 100),
			IncidentEffect.TraitChange(DefaultTraits.Honor, -300),
			IncidentEffect.GoldChange(() => -100),
			IncidentEffect.ChangeItemAmount(() => Game.Current.ObjectManager.GetObject<ItemObject>("salt"), () => 1)
		}, delegate(TextObject text)
		{
			text.SetCharacterProperties("TROOP", GetRandomTroop().Character);
			return true;
		});
		incident77.AddOption("{=tDK8b1J8}You vaguely recall hearing that lead might be poisonous, but you'll try the others.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Honor, -100),
			IncidentEffect.GoldChange(() => -150),
			IncidentEffect.ChangeItemAmount(() => Game.Current.ObjectManager.GetObject<ItemObject>("salt"), () => 1)
		});
		incident77.AddOption("{=MfbK2Sqa}Tell your man to save his ingenuity for battle, not trade.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Honor, 100),
			IncidentEffect.TraitChange(DefaultTraits.Generosity, -50)
		});
		incident77.AddOption("{=a5gLRsa4}Thank your man bruskly and try his trick, but pay him nothing.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Honor, -200),
			IncidentEffect.TraitChange(DefaultTraits.Generosity, -100),
			IncidentEffect.ChangeItemAmount(() => Game.Current.ObjectManager.GetObject<ItemObject>("salt"), () => 1)
		});
		Incident incident78 = RegisterIncident("incident_sorrows_of_war", "{=FL8rUSWo}Sorrows of War", "{=a939QfLJ}As you leave {TOWN_NAME}, a young woman throws herself in front of your horse. She raises her face to the sky, tears at her hair, and wails that her young husband signed up to fight for you and was slain in battle. \"You took him from me!\" she screams. It is not clear what she wants.", IncidentTrigger.LeavingTown, IncidentType.TroopSettlementRelation, CampaignTime.Years(1000f), delegate(TextObject description)
		{
			if (MobileParty.MainParty.LastVisitedSettlement == null)
			{
				return false;
			}
			PlayerBattleEndedLogEntry playerBattleEndedLogEntry = Campaign.Current.LogEntryHistory.FindLastGameActionLog((PlayerBattleEndedLogEntry x) => true);
			if (playerBattleEndedLogEntry == null)
			{
				return false;
			}
			if (!playerBattleEndedLogEntry.HasHeavyCausality)
			{
				return false;
			}
			description.SetTextVariable("TOWN_NAME", MobileParty.MainParty.LastVisitedSettlement.Name);
			return true;
		});
		incident78.AddOption("{=8uabQDxp}Tell her, as soberly as you can, that her husband gave his life for a noble cause. Hand her a purse of 100 denars, and tell her that you are sure that he would be happy if she marries again, as long as he remains in her memory.", new List<IncidentEffect>
		{
			IncidentEffect.GoldChange(() => -100),
			IncidentEffect.TraitChange(DefaultTraits.Valor, 50)
		});
		incident78.AddOption("{=kZJvgtE3}Dismount, burst into tears, take her hands in yours and tell her that every life lost in your service tears at your heart, and that you want nothing more but for these wars to end.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Calculating, -200),
			IncidentEffect.TraitChange(DefaultTraits.Mercy, 100)
		});
		incident78.AddOption("{=52l7VuWI}Tell her that Heaven expects her to bear her fate with dignity.", new List<IncidentEffect> { IncidentEffect.TraitChange(DefaultTraits.Generosity, -100) });
		incident78.AddOption("{=Sz28bZOI}Have your purser speak to her at length about what she might need, as you keep your distance. ", new List<IncidentEffect>
		{
			IncidentEffect.DisorganizeParty(),
			IncidentEffect.GoldChange(() => -50)
		});
		Incident incident79 = RegisterIncident("incident_souvenirs", "{=LAMmAUvt}Souvenirs", "{=PqzLYt6i}Some time recently - you're not sure exactly when - your men started a new post-battle tradition - going about the field collecting grisly trophies from slain enemies. Now you can't help noticing these prizes dangling from their belts or the halters of their horses.", IncidentTrigger.LeavingBattle, IncidentType.PostBattle, CampaignTime.Years(1000f), (TextObject description) => Settlement.CurrentSettlement != null && PartyBase.MainParty.MemberRoster.TotalRegulars >= 5);
		incident79.AddOption("{=1zsWhG2N}Tell your men as best you can that, while you respect their spirit, this will frighten local farmers who don't understand warriors' ways.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Calculating, 50),
			IncidentEffect.TraitChange(DefaultTraits.Generosity, -100),
			IncidentEffect.MoraleChange(-20f)
		});
		incident79.AddOption("{=VHlwd8na}Take a lively interest in each of these ornaments, asking your men at which battle and from which fallen foe they were taken.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Valor, 100),
			IncidentEffect.TraitChange(DefaultTraits.Calculating, -100),
			IncidentEffect.MoraleChange(20f),
			IncidentEffect.Custom(null, delegate
			{
				List<Settlement> list = GetNearbyVillages();
				List<TextObject> list2 = new List<TextObject>();
				foreach (Settlement item4 in list)
				{
					ChangeRelationAction.ApplyPlayerRelation(item4.Notables.FirstOrDefault(), -5);
					TextObject textObject = new TextObject("{=8IzNumMa}{?AMOUNT > 0}Increased{?}Decreased{\\?} relationship with {SETTLEMENT} by {ABS(AMOUNT)}.");
					textObject.SetTextVariable("AMOUNT", -5);
					textObject.SetTextVariable("SETTLEMENT", item4.Name);
					list2.Add(textObject);
				}
				return list2;
			}, (IncidentEffect effect) => new List<TextObject>
			{
				new TextObject("{=4o7R829M}-5 relations with three surrounding villages")
			})
		});
		incident79.AddOption("{=xoH5LaY0}Harangue your men for their barbarism, tell them to honor rather than mock their fallen foes.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Honor, 100),
			IncidentEffect.TraitChange(DefaultTraits.Generosity, -100)
		});
		incident79.AddOption("{=SLAFtiET}Pretend you saw nothing.", new List<IncidentEffect> { IncidentEffect.Custom(null, delegate
		{
			List<Settlement> list = GetNearbyVillages();
			List<TextObject> list2 = new List<TextObject>();
			foreach (Settlement item5 in list)
			{
				ChangeRelationAction.ApplyPlayerRelation(item5.Notables.FirstOrDefault(), -5);
				TextObject textObject = new TextObject("{=8IzNumMa}{?AMOUNT > 0}Increased{?}Decreased{\\?} relationship with {SETTLEMENT} by {ABS(AMOUNT)}.");
				textObject.SetTextVariable("AMOUNT", -5);
				textObject.SetTextVariable("SETTLEMENT", item5.Name);
				list2.Add(textObject);
			}
			return list2;
		}, (IncidentEffect effect) => new List<TextObject>
		{
			new TextObject("{=4o7R829M}-5 relations with three surrounding villages")
		}) });
		Incident incident80 = RegisterIncident("incident_back_taxes", "{=NFwuiSCw}Back taxes", "{=YAnTtrLs}Your reputation as a skilled trader generally does you good, but there is always a downside. When you arrive in {TOWN}, the market inspector descends from his tower to present you with a list of fees that you supposedly accumulated over the years, based on decrees that go back several centuries. The decrees may be genuine, but in all your talk with merchants, you never heard of anyone ever paying them.", IncidentTrigger.EnteringTown, IncidentType.Profit, CampaignTime.Years(1000f), delegate(TextObject description)
		{
			if (MobileParty.MainParty.CurrentSettlement == null)
			{
				return false;
			}
			description.SetTextVariable("TOWN", MobileParty.MainParty.CurrentSettlement.Name);
			return Hero.MainHero.GetSkillValue(DefaultSkills.Trade) >= 40;
		});
		incident80.AddOption("{=RPyFwR6I}Erupt in fury, telling the inspector that there is no way you will be fleeced in this way, legally or illegally.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Valor, 100),
			IncidentEffect.TraitChange(DefaultTraits.Calculating, -100),
			IncidentEffect.RenownChange(5f),
			IncidentEffect.CrimeRatingChange(() => MobileParty.MainParty.CurrentSettlement.MapFaction, -10f)
		});
		incident80.AddOption("{=vzA2ZJog}Pay, but say for all to hear that selective application of obscure laws is little better than theft.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Honor, 100),
			IncidentEffect.GoldChange(() => -300)
		});
		incident80.AddOption("{=ogP9E5Hx}Reason with the inspector over a splendid dinner and an amphora of the finest wine that is of course on you.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Honor, -50),
			IncidentEffect.InfluenceChange(50f),
			IncidentEffect.GoldChange(() => -100)
		});
		incident80.AddOption("{=9aYLFpC9}Just hand over the money to make the problem go away.", new List<IncidentEffect>
		{
			IncidentEffect.GoldChange(() => -200),
			IncidentEffect.TraitChange(DefaultTraits.Valor, -100)
		});
		Incident incident81 = RegisterIncident("incident_third_party_arbitration", "{=trfafbJW}Third-Party Arbitration", "{=XQU3Tobe}Knowing your reputation as a trader, two merchants in {TOWN_NAME} are asking you to arbitrate a dispute between them. The two had drawn up a contract wherein each partner had a half-share in an incoming shipment of wine. When it arrived, however, the sea captain demanded twice the price. The wealthier of the two partners was at the docks. He took the initiative and purchased the goods, deciding to take a loss for the sake of his reputation as a reliable supplier to the local taverns, but the second is poorer and wants out of the deal.", IncidentTrigger.EnteringTown, IncidentType.Profit, CampaignTime.Years(1000f), delegate(TextObject description)
		{
			if (MobileParty.MainParty.CurrentSettlement == null)
			{
				return false;
			}
			description.SetTextVariable("TOWN_NAME", MobileParty.MainParty.CurrentSettlement.Name);
			return Hero.MainHero.GetSkillValue(DefaultSkills.Trade) >= 35;
		});
		incident81.AddOption("{=nbshAaHo}The letter of the contract would favor the wealthier partner. Merchants should be careful what they sign.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Honor, 100),
			IncidentEffect.TraitChange(DefaultTraits.Generosity, -50),
			IncidentEffect.InfluenceChange(50f)
		});
		incident81.AddOption("{=bsC5zEoI}Never mind the letter of the contract, the reasonable, decent move for the richer merchant would have been to consult with his partner.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Generosity, 100),
			IncidentEffect.TraitChange(DefaultTraits.Honor, -50),
			IncidentEffect.InfluenceChange(50f)
		});
		incident81.AddOption("{=Ib04QQJY}Tell them that the whole thing gives you a headache.", new List<IncidentEffect>());
		incident81.AddOption("{=0ZP5fDBX}Let the wealthier merchant know that your decision is for sale.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Honor, -300),
			IncidentEffect.GoldChange(() => 1000)
		});
		Incident incident82 = RegisterIncident("incident_tipping_the_scales", "{=OXzJbvd8}Tipping the scales", "{=fhjyEgld}Your troops are taking a bit longer than usual to get ready, and you've had time to relax a bit and watch goings-on at the market. One commodity vendor in particular has caught your attention. He always uses one set of weights for the sacks that he is buying from local farmers, and another for goods that he sells. You have little doubt that one or both of these sets is a false measure, allowing him to defraud his customers.", IncidentTrigger.LeavingTown, IncidentType.Profit, CampaignTime.Years(1000f), (TextObject description) => Hero.MainHero.GetSkillValue(DefaultSkills.Trade) >= 30);
		incident82.AddOption("{=wjpmx20I}This isn't your affair, and you don't know if other merchants or corrupt officials may be in on it. Walk away.", new List<IncidentEffect>());
		incident82.AddOption("{=Uidhqph1}Denounce the merchant loudly, even if you run the risk of making enemies among the town elite.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Honor, 100),
			IncidentEffect.SettlementRelationChange(() => MobileParty.MainParty.LastVisitedSettlement, -20).WithChance(0.5f)
		});
		incident82.AddOption("{=MGfXReQC}Walk up to the merchant, and insist he use the lighter weights to sell you one measure of his goods. Then come back later and return them, insisting he use the heavier weights.", new List<IncidentEffect>
		{
			IncidentEffect.GoldChange(() => 200),
			IncidentEffect.TraitChange(DefaultTraits.Honor, -50),
			IncidentEffect.SkillChange(DefaultSkills.Roguery, 50f)
		});
		Incident incident83 = RegisterIncident("incident_injury_accident", "{=m9MbuMlw}Injury Accident", "{=1jAyaivZ}As you pass through the village streets, a dog dashes out from among some spectators and bites at the heels of one of your {CAVALRYMAN}'s horses, panicking it. The mount rears and gallops down the street, trampling a vegetable vendor who was unable to get out of its way. She will probably walk with a limp hereafter, and her family demands compensation.", IncidentTrigger.LeavingVillage, IncidentType.TroopSettlementRelation, CampaignTime.Years(1000f), delegate(TextObject description)
		{
			TroopRosterElement troopRosterElement = GetRandomCavalryTroop();
			if (troopRosterElement.Character == null)
			{
				return false;
			}
			description.SetTextVariable("CAVALRYMAN", troopRosterElement.Character.Name);
			return MobileParty.MainParty.MemberRoster.GetTroopRoster().Where(CavalryTroopPredicate).Sum((TroopRosterElement x) => x.Number) >= 10;
		});
		incident83.AddOption("{=jwsexTeh}No horseman can control a mount that is under attack from a dog. Refuse.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Mercy, -100),
			IncidentEffect.SettlementRelationChange(() => MobileParty.MainParty.LastVisitedSettlement, -20)
		});
		incident83.AddOption("{=5HsJkbZz}You brought your men into their village, and you must take responsibility.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Generosity, 100),
			IncidentEffect.GoldChange(() => -100)
		});
		incident83.AddOption("{=te1A19Xe}Tell your {CAVALRYMAN} that he can sort it out.", new List<IncidentEffect> { IncidentEffect.TraitChange(DefaultTraits.Generosity, -100) }, delegate(TextObject text)
		{
			text.SetTextVariable("CAVALRYMAN", GetRandomCavalryTroop().Character.Name);
			return true;
		});
		Incident incident84 = RegisterIncident("incident_water_supplies", "{=xYjLaJxA}Water supplies", "{=EbNZg7jp}In hot climes, often the most vulnerable part of a fortress is its water supply. Most depend on wells but those can run dry. After days spent in front of {FORTRESS_NAME}, you've caught glimpses of the defenders slipping into the hills. You explore where they've gone, and eventually you come across a rudimentary cistern. You suspect that they must be refreshing their water here.", IncidentTrigger.DuringSiege, IncidentType.Siege, CampaignTime.Years(1000f), delegate(TextObject description)
		{
			if (MobileParty.MainParty.BesiegedSettlement == null)
			{
				return false;
			}
			description.SetTextVariable("FORTRESS_NAME", MobileParty.MainParty.BesiegedSettlement.Name);
			if (!MobileParty.MainParty.Position.IsValid())
			{
				return false;
			}
			TerrainType faceTerrainType = Campaign.Current.MapSceneWrapper.GetFaceTerrainType(MobileParty.MainParty.Position.Face);
			return faceTerrainType == TerrainType.Steppe || faceTerrainType == TerrainType.Desert;
		});
		incident84.AddOption("{=57VuY9ln}Poison it! Some women and children of {FORTRESS_NAME} may well perish along with the defenders, but such is war.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Mercy, -100),
			IncidentEffect.Custom(null, delegate
			{
				MobileParty garrisonParty = MobileParty.MainParty.BesiegedSettlement.Town.GarrisonParty;
				int num = (int)((float)garrisonParty.MemberRoster.TotalRegulars * 0.1f);
				List<TroopRosterElement> list = (from x in garrisonParty.MemberRoster.GetTroopRoster()
					where !x.Character.IsHero && x.Number > 0
					select x).ToList();
				while (num > 0 && list.Count > 0)
				{
					int index = MBRandom.RandomInt(list.Count);
					TroopRosterElement value = list[index];
					int num2 = MBRandom.RandomInt(1, Math.Min(num, value.Number) + 1);
					garrisonParty.MemberRoster.AddToCounts(value.Character, -num2);
					value.Number -= num2;
					list[index] = value;
					if (value.Number == 0)
					{
						list.RemoveAt(index);
					}
					num -= num2;
				}
				return new List<TextObject>
				{
					new TextObject("{=pS3PKbYB}10% of the garrison perished.")
				};
			}, (IncidentEffect effect) => new List<TextObject>
			{
				new TextObject("{=gyRuGBMF}10% of the garrison perishes")
			}),
			IncidentEffect.TownProsperityChange(() => MobileParty.MainParty.BesiegedSettlement.Town, -20)
		}, delegate(TextObject text)
		{
			text.SetTextVariable("FORTRESS_NAME", MobileParty.MainParty.BesiegedSettlement.Name);
			return true;
		});
		incident84.AddOption("{=bGaD3LoG}The best blows are struck not to the body but to the spirit. Let the defenders know you found it by diverting it to a pool where your men can bathe on the hottest, thirstiest days of the siege.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Calculating, 100),
			IncidentEffect.TraitChange(DefaultTraits.Generosity, 100),
			IncidentEffect.SiegeProgressChange(() => 0.1f)
		});
		incident84.AddOption("{=iiwhVNBb}You respect the defenders' cunning. Simply observe how they slip out of the fortress, by ropes or hidden gates, and learn from it.", new List<IncidentEffect> { IncidentEffect.SkillChange(DefaultSkills.Engineering, 100f) });
		Incident incident85 = RegisterIncident("incident_mining", "{=O4iUPSXs}Mining", "{=0DSopB7F}After observing the terrain around {FORTRESS_NAME}, you think conditions might be appropriate for mining - digging a tunnel under a wall, then collapsing it. This can open a breach but is time-consuming and risky. If you encounter rock underground, you may find all your efforts go to waste. If the ground is too soft, the tunnel may collapse and bury your men. Most troops are reluctant to spend time in dark, confined tunnels unless they are paid extra, or forced.", IncidentTrigger.DuringSiege, IncidentType.Siege, CampaignTime.Years(1000f), delegate(TextObject description)
		{
			if (MobileParty.MainParty.BesiegedSettlement == null)
			{
				return false;
			}
			description.SetTextVariable("FORTRESS_NAME", MobileParty.MainParty.BesiegedSettlement.Name);
			return MobileParty.MainParty.BesiegedSettlement != null && Hero.MainHero.GetSkillValue(DefaultSkills.Engineering) >= 30;
		});
		incident85.AddOption("{=SR6inO3d}Double the pay of any troops willing to dig the mine, and tunnel carefully.", new List<IncidentEffect>
		{
			IncidentEffect.GoldChange(() => -500),
			IncidentEffect.Select(IncidentEffect.Group(IncidentEffect.BreachSiegeWall(1), IncidentEffect.MoraleChange(5f), IncidentEffect.TraitChange(DefaultTraits.Calculating, 100), IncidentEffect.RenownChange(5f), IncidentEffect.SkillChange(DefaultSkills.Engineering, 50f)), IncidentEffect.Group(IncidentEffect.TraitChange(DefaultTraits.Valor, -100), IncidentEffect.SiegeProgressChange(() => -0.2f)), 0.6f)
		});
		incident85.AddOption("{=kntd1zYo}Tunnel quickly, threatening to execute anyone who refuses for insubordination", new List<IncidentEffect> { IncidentEffect.Select(IncidentEffect.Group(IncidentEffect.SiegeProgressChange(() => 0.2f), IncidentEffect.TraitChange(DefaultTraits.Generosity, -100), IncidentEffect.RenownChange(5f), IncidentEffect.SkillChange(DefaultSkills.Engineering, 50f)), IncidentEffect.Group(IncidentEffect.TraitChange(DefaultTraits.Calculating, -100), IncidentEffect.SiegeProgressChange(() => -0.1f), IncidentEffect.KillTroopsRandomly((TroopRosterElement x) => true, () => 10)), 0.6f) });
		incident85.AddOption("{=lCumAw9j}Rely on the usual methods", new List<IncidentEffect>());
		Incident incident86 = RegisterIncident("incident_at_the_breach", "{=OaCU1zfR}At the Breach", "{=rpvUQUmq}You've been studying the walls of {FORTRESS_NAME} for days, and you see what appears to be a crack in one place - possibly the result of an old earthquake, or just negligence. It's hard to hit with your catapults and the ground is too rocky to mine, but you reckon troops with pickaxes by themselves could bring it down - assuming they were willing to brave the storm of arrows and rocks that would no doubt come pouring from the battlements above.", IncidentTrigger.DuringSiege, IncidentType.Siege, CampaignTime.Years(1000f), delegate(TextObject description)
		{
			if (MobileParty.MainParty.BesiegedSettlement == null)
			{
				return false;
			}
			description.SetTextVariable("FORTRESS_NAME", MobileParty.MainParty.BesiegedSettlement.Name);
			return MobileParty.MainParty.BesiegedSettlement != null && Hero.MainHero.GetSkillValue(DefaultSkills.Engineering) >= 20;
		});
		incident86.AddOption("{=PJYgbG2h}\"A purse of silver for every stone brought to me from that wall!\"", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Valor, 100),
			IncidentEffect.TraitChange(DefaultTraits.Calculating, -100),
			IncidentEffect.KillTroopsRandomly((TroopRosterElement x) => true, () => 10),
			IncidentEffect.GoldChange(() => -500),
			IncidentEffect.SiegeProgressChange(() => 0.2f).WithChance(0.8f)
		});
		incident86.AddOption("{=ffJrKMFm}Order your recruits to swarm the base of the wall and bring it down. They can be replaced.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Generosity, -100),
			IncidentEffect.KillTroopsRandomlyOrderedByTier((TroopRosterElement x) => true, () => 10),
			IncidentEffect.BreachSiegeWall(1)
		});
		incident86.AddOption("{=wJ3Nh230}A breach is not worth the losses.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Valor, -100),
			IncidentEffect.TraitChange(DefaultTraits.Mercy, 100)
		});
		Incident incident87 = RegisterIncident("incident_failed_inspection", "{=KBh8fb89}Failed Inspection", "{=jUBTrdbE}You take a quick look over your men before you ride out, and you are shocked to notice the state of their equipment. In particular, several of the men have frayed bridles and shield straps that look like they might snap in battle. They are supposed to oil and replace their leather at their own expense, and clearly have not been doing so.", IncidentTrigger.LeavingSettlement, IncidentType.PartyCampLife, CampaignTime.Years(1000f), (TextObject description) => MobileParty.MainParty.MemberRoster.TotalRegulars >= 40);
		incident87.AddOption("{=fyJ6aDOQ}Burst into a righteous fury and hand out extra punishments, so that no one will ever again dare neglect their equipment", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Calculating, -200),
			IncidentEffect.PartyExperienceChance(100)
		});
		incident87.AddOption("{=PZTyrsag}Sternly lecture them the risks of a strap snapping in battle. Have them do all their preparations for march over again, from the beginning.", new List<IncidentEffect>
		{
			IncidentEffect.PartyExperienceChance(20),
			IncidentEffect.DisorganizeParty(),
			IncidentEffect.TraitChange(DefaultTraits.Calculating, 50)
		});
		incident87.AddOption("{=fQscCIxs}Buy them the best equipment you can from the market to replace their wares.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Generosity, 200),
			IncidentEffect.GoldChange(() => -200)
		});
		incident87.AddOption("{=SLAFtiET}Pretend you saw nothing.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Generosity, 100),
			IncidentEffect.DemoteTroopsRandomlyWithPredicate((TroopRosterElement x) => !x.Character.IsHero, (CharacterObject x) => true, 2, specifyUnitTypeOnHint: false)
		});
		Incident incident88 = RegisterIncident("incident_the_quiet_life", "{=3M54uDKZ}The Quiet Life", "{=IaOfWrNF}As you pass through your lands, you see several of your {TROOP_NAME} casting envious eyes over the farmers in the fields. They are tired of wandering from place to place, they say, and hope to put down roots somewhere and start a family. Their contract of enlistment is not yet up but it occurs to you that if they settled in your estates, it would be a boon to your local militia.", IncidentTrigger.EnteringTown | IncidentTrigger.EnteringCastle, IncidentType.FiefManagement, CampaignTime.Years(1000f), delegate(TextObject description)
		{
			if (MobileParty.MainParty.CurrentSettlement == null)
			{
				return false;
			}
			CharacterObject characterObject = GetTheQuietLifeTroop();
			if (characterObject == null)
			{
				return false;
			}
			description.SetTextVariable("TROOP_NAME", characterObject.Name);
			return MobileParty.MainParty.CurrentSettlement.IsFortification && Hero.MainHero.Clan == MobileParty.MainParty.CurrentSettlement.OwnerClan && MobileParty.MainParty.MemberRoster.TotalRegulars >= 60;
		});
		incident88.AddOption("{=SFUrdvHo}Grant them their wish, and encourage them to purchase land near {FORTRESS_NAME}.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Generosity, 100),
			IncidentEffect.ChangeTroopAmount(GetTheQuietLifeTroop, -3),
			IncidentEffect.SettlementMilitiaChange(() => MobileParty.MainParty.CurrentSettlement, 10)
		}, delegate(TextObject text)
		{
			text.SetTextVariable("FORTRESS_NAME", MobileParty.MainParty.CurrentSettlement.Name);
			return true;
		});
		incident88.AddOption("{=3WFuf7OC}Give them money to purchase land near your fief in {VILLAGE_NAME}, so long as they take charge of training the local militia.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Generosity, 200),
			IncidentEffect.GoldChange(() => -300),
			IncidentEffect.ChangeTroopAmount(GetTheQuietLifeTroop, -3),
			IncidentEffect.SettlementMilitiaChange(() => MobileParty.MainParty.CurrentSettlement, 15)
		}, delegate(TextObject text)
		{
			Village randomElement = MobileParty.MainParty.CurrentSettlement.Town.Villages.GetRandomElement();
			text.SetTextVariable("VILLAGE_NAME", randomElement.Name);
			return true;
		});
		incident88.AddOption("{=YIsU82Jd}Tell them they can go, but only if they forfeit back pay as compensation to you for losing them.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Generosity, -100),
			IncidentEffect.ChangeTroopAmount(GetTheQuietLifeTroop, -3),
			IncidentEffect.GoldChange(() => 300)
		});
		incident88.AddOption("{=KUb0DRxw}Refuse them permission.", new List<IncidentEffect> { IncidentEffect.TraitChange(DefaultTraits.Generosity, -100) });
		Incident incident89 = RegisterIncident("incident_job_offer", "{=xEktDm2z}Job Offer", "{=guEjfYhO}One of your {TROOP_NAME} had some drinks with the arena master in {TOWN} and left the tavern with a job offer - work as a trainer and occasional combatant. The signing bonus is generous. He is reluctant to leave you, and you'd be reluctant to lose a valuable soldier, but he'd be in a fine position to sing your praises.", IncidentTrigger.LeavingTown, IncidentType.PartyCampLife, CampaignTime.Years(1000f), delegate(TextObject description)
		{
			if (MobileParty.MainParty.LastVisitedSettlement == null)
			{
				return false;
			}
			CharacterObject characterObject = GetJobOfferTroop();
			if (characterObject == null)
			{
				return false;
			}
			description.SetTextVariable("TROOP_NAME", characterObject.Name);
			description.SetTextVariable("TOWN", MobileParty.MainParty.LastVisitedSettlement.Name);
			return MobileParty.MainParty.MemberRoster.TotalRegulars >= 30 && Hero.MainHero.Clan.Renown >= 200f;
		});
		incident89.AddOption("{=0awbcZag}Give him your blessing and encourage him to bore everyone to tears with his old war stories.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Generosity, 100),
			IncidentEffect.RenownChange(10f),
			IncidentEffect.ChangeTroopAmount(GetJobOfferTroop, -1)
		});
		incident89.AddOption("{=Qb3axqdZ}Persuade him to stay. The arena is fine for practice but true glory is not won with blunted weapons.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Valor, 100),
			IncidentEffect.TraitChange(DefaultTraits.Generosity, -100)
		});
		incident89.AddOption("{=klVc5afL}Let him go if he splits his signing bonus with you.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Generosity, -100),
			IncidentEffect.RenownChange(5f),
			IncidentEffect.ChangeTroopAmount(GetJobOfferTroop, -1),
			IncidentEffect.GoldChange(() => 200)
		});
		Incident incident90 = RegisterIncident("incident_secession_of_the_plebs", "{=VvwnbRx4}Secession of the Plebs", "{=GgYzaakh}Trade in {TOWN_NAME} has been booming, and prices and wages have been going up. The workers at your {WORKSHOP} have taken notice. Your superviser reports that they have all refused to come in to work until they receive a pay increase. He notes that there remain plenty of hungry mouths and willing hands in town, but that the current workers might cause trouble if they are replaced.", IncidentTrigger.EnteringTown, IncidentType.Workshop, CampaignTime.Years(1000f), delegate(TextObject description)
		{
			Workshop workshop = GetSecessionOfThePlebsWorkshop();
			if (workshop == null)
			{
				return false;
			}
			description.SetTextVariable("TOWN_NAME", workshop.Settlement.Name);
			description.SetTextVariable("WORKSHOP", workshop.Name);
			return true;
		});
		incident90.AddOption("{=5krpAZu6}Chide them gently for walking off the job before coming to you first, because of course you'll up their wages.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Generosity, 200),
			IncidentEffect.WorkshopProfitabilityChange(GetSecessionOfThePlebsWorkshop, -0.1f)
		});
		incident90.AddOption("{=zM2vbhrt}Sit down with them and negotiate a mutual understanding.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Generosity, 100),
			IncidentEffect.WorkshopProfitabilityChange(GetSecessionOfThePlebsWorkshop, -0.05f)
		});
		incident90.AddOption("{=aTU2CgIU}Fire the lot of them, hire different workers, and post some troops outside to protect your shop.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Mercy, -100),
			IncidentEffect.TraitChange(DefaultTraits.Calculating, 100),
			IncidentEffect.KillTroopsRandomly((TroopRosterElement x) => !x.Character.IsHero, () => 2)
		});
		Incident incident91 = RegisterIncident("incident_occupational_safety", "{=l8rpMldr}Occupational Safety", "{=Eih6lrcN}Business is good at your {WORKSHOP} in {TOWN}, but in the rush to keep up with demand the normal precautions are not being taken. {INJURY_TEXT} He will live, but is unlikely to be able to do skilled work, and the rest of the workers are worried about future accidents.", IncidentTrigger.EnteringTown, IncidentType.Workshop, CampaignTime.Years(1000f), delegate(TextObject description)
		{
			if (!MobileParty.MainParty.CurrentSettlement.IsTown)
			{
				return false;
			}
			WorkshopType workshopType = WorkshopType.Find("brewery");
			WorkshopType workshopType2 = WorkshopType.Find("smithy");
			Workshop workshop = GetOccupationalSafetyWorkshop();
			if (workshop == null)
			{
				return false;
			}
			description.SetTextVariable("TOWN", workshop.Settlement.Name);
			description.SetTextVariable("WORKSHOP", workshop.Name);
			string variable = string.Empty;
			if (workshop.WorkshopType == workshopType)
			{
				variable = "{=JcoSd4oi}One man burned his hand in the oven.";
			}
			else if (workshop.WorkshopType == workshopType2)
			{
				variable = "{=lVnw8K2N}One man crushed his hand in the forge.";
			}
			description.SetTextVariable("INJURY_TEXT", variable);
			return true;
		});
		incident91.AddOption("{=JdlbkM8C}Pay him a pension and order your supervisors to take more care, even if it slows down production.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Generosity, 100),
			IncidentEffect.TraitChange(DefaultTraits.Mercy, 100),
			IncidentEffect.GoldChange(() => -100),
			IncidentEffect.WorkshopProfitabilityChange(GetOccupationalSafetyWorkshop, -0.1f)
		});
		incident91.AddOption("{=OlvD0u1O}You're not going to pay for another man's clumsiness. Let work recommence at the same pace.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Generosity, -100),
			IncidentEffect.TownSecurityChange(() => MobileParty.MainParty.CurrentSettlement.Town, -10)
		});
		incident91.AddOption("{=k4aNfNHg}As on the battlefield, so on the workshop floor. Let each team work at its own pace, receiving bonuses for output, but also taking responsibiliy for injuries.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Calculating, 100),
			IncidentEffect.TraitChange(DefaultTraits.Mercy, -200),
			IncidentEffect.WorkshopProfitabilityChange(GetOccupationalSafetyWorkshop, 0.05f)
		});
		Incident incident92 = RegisterIncident("incident_hand_in_the_strongbox", "{=OFuTQ6t8}Hand in the Strongbox", "{=82Fno8Vr}Your clerk at your {WORKSHOP} in {TOWN} has caught one of your foremen skimming from the funds you use to purchase raw materials. Luckily, he did not take much, and he readily confessed. How do you prevent this from happening in the future?", IncidentTrigger.EnteringTown, IncidentType.Workshop, CampaignTime.Years(1000f), delegate(TextObject description)
		{
			Workshop workshop = GetHandInTheStrongboxWorkshop();
			if (workshop == null)
			{
				return false;
			}
			description.SetTextVariable("TOWN", workshop.Settlement.Name);
			description.SetTextVariable("WORKSHOP", workshop.Name);
			return true;
		});
		incident92.AddOption("{=47hUs9yg}You make an example of the foreman. You hand him over to the town authorities and encourage them to apply the full penalty - branding as a thief and exile from the town.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Mercy, -100),
			IncidentEffect.TraitChange(DefaultTraits.Honor, 100),
			IncidentEffect.WorkshopProfitabilityChange(GetHandInTheStrongboxWorkshop, -0.1f).WithChance(0.3f)
		});
		incident92.AddOption("{=n2mOvIPt}You appeal to your men's better natures. You plea for leniency for your foreman, and beg them not to take advantage.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Mercy, 100),
			IncidentEffect.WorkshopProfitabilityChange(GetHandInTheStrongboxWorkshop, -0.05f)
		});
		incident92.AddOption("{=cyZG4qHm}You institute a complex scheme of bookkeeping, even though it will make purchases more onerous and slow down production.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Calculating, 100),
			IncidentEffect.WorkshopProfitabilityChange(GetHandInTheStrongboxWorkshop, -0.05f)
		});
		incident92.AddOption("{=G8eaX10B}You treat all your workers as potential thieves, having them searched coming and going from the workshop.", new List<IncidentEffect> { IncidentEffect.TraitChange(DefaultTraits.Generosity, -100) });
		Incident incident93 = RegisterIncident("incident_jobs_for_the_lads", "{=0avcvATJ}Jobs for the Lads", "{=cJdoDJ4M}One prominent resident of {TOWN}, {ARTISAN_NAME}, approaches you. Your {WORKSHOP} has been hiring workers who have recently migrated in from the countryside. He requests that you fire them and take on some local boys from the back alleys. If you refuse, he won't be responsible if the carts carrying your shipments get swarmed by traffic going through tight gates, slowing down production.", IncidentTrigger.EnteringTown, IncidentType.Workshop, CampaignTime.Years(1000f), delegate(TextObject description)
		{
			if (!MobileParty.MainParty.CurrentSettlement.IsTown)
			{
				return false;
			}
			Workshop workshop = GetJobsForTheLadsWorkshop();
			if (workshop == null)
			{
				return false;
			}
			Hero hero = GetJobsForTheLadsArtisan();
			if (hero == null)
			{
				return false;
			}
			description.SetTextVariable("TOWN", workshop.Settlement.Name);
			description.SetTextVariable("WORKSHOP", workshop.Name);
			description.SetTextVariable("ARTISAN_NAME", hero.Name);
			return true;
		});
		incident93.AddOption("{=bdUzdzIe}Sadly bid farewell to the migrants and wish them well in future endeavors.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Generosity, -100),
			IncidentEffect.HeroRelationChange(GetJobsForTheLadsArtisan, 10)
		});
		incident93.AddOption("{=qoSObD62}Tell {ARTISAN_NAME} to do his worst - you're not firing men who've served you well.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Generosity, 100),
			IncidentEffect.WorkshopProfitabilityChange(GetJobsForTheLadsWorkshop, -0.05f)
		}, delegate(TextObject text)
		{
			Hero hero = GetJobsForTheLadsArtisan();
			text.SetTextVariable("ARTISAN_NAME", hero.Name);
			return true;
		});
		incident93.AddOption("{=TbF5Kc4x}Ask {ARTISAN_NAME} if you can make a sizeable contribution to his guild's feast and funeral fund.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Honor, -100),
			IncidentEffect.TraitChange(DefaultTraits.Calculating, 100),
			IncidentEffect.GoldChange(() => -200),
			IncidentEffect.HeroRelationChange(GetJobsForTheLadsArtisan, 10)
		}, delegate(TextObject text)
		{
			Hero hero = GetJobsForTheLadsArtisan();
			text.SetTextVariable("ARTISAN_NAME", hero.Name);
			return true;
		});
		Incident incident94 = RegisterIncident("incident_respect_for_the_slain", "{=EilA5dlq}Respect for the Slain", "{=pNVKWAKo}Throughout your visit to {VILLAGE}, one of your older {TROOP_TYPE} seems moody and downcast. As you march out, he suddenly stops, stares at a roadside stall, then breaks ranks from your party and starts shouting at the vendor. The story quickly comes tumbling out. He served in one of the Empire's old legions in a battle here against the {NEAREST_OTHER_CULTURE}, and the vendor is selling scraps of metal that were clearly looted from the graves of the dead.", IncidentTrigger.LeavingVillage, IncidentType.TroopSettlementRelation, CampaignTime.Years(1000f), delegate(TextObject description)
		{
			if (MobileParty.MainParty.LastVisitedSettlement == null)
			{
				return false;
			}
			if (MobileParty.MainParty.MemberRoster.GetTroopRoster().Where(HighTierTroopsPredicate).ToList()
				.Sum((TroopRosterElement x) => x.Number) < 10)
			{
				return false;
			}
			CharacterObject characterObject = GetRespectForTheSlainSelectedTroop();
			if (characterObject == null)
			{
				return false;
			}
			CultureObject cultureObject = GetRespectForTheSlainNearestOtherCulture();
			if (cultureObject == null)
			{
				return false;
			}
			description.SetTextVariable("VILLAGE", MobileParty.MainParty.LastVisitedSettlement.Name);
			description.SetTextVariable("TROOP_TYPE", characterObject.Name);
			description.SetTextVariable("NEAREST_OTHER_CULTURE", cultureObject.Name);
			return true;
		});
		incident94.AddOption("{=N2Sb9nS6}Purchase what old helmets and shield bosses you can and allow your man to build a small memorial on the battlefield, even though you know it will soon be looted afresh.", new List<IncidentEffect>
		{
			IncidentEffect.GoldChange(() => -200),
			IncidentEffect.TraitChange(DefaultTraits.Generosity, 100),
			IncidentEffect.DisorganizeParty()
		});
		incident94.AddOption("{=fqBRonK9}Do you not also not loot whatever you can from the dead at your victories? Tell your man to save his anger for wrongs done to the living.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Generosity, -100),
			IncidentEffect.TraitChange(DefaultTraits.Calculating, 100)
		});
		incident94.AddOption("{=WGmKib15}Erupt in fury at the vendor and accuse him of grave desecration. It's unlikely that the thefts will stop, but your men will be pleased.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Calculating, -100),
			IncidentEffect.TraitChange(DefaultTraits.Generosity, 50),
			IncidentEffect.RenownChange(2f),
			IncidentEffect.SettlementRelationChange(() => MobileParty.MainParty.LastVisitedSettlement, -5)
		});
		Incident incident95 = RegisterIncident("incident_just_rewards", "{=YwstWeDQ}Just Rewards", "{=Jo5OjwAU}Your men fought truly heroically in the last battle. As they help their wounded comrades back to camp and gather the slain, their weary faces sometimes look to you with expectation. If ever there was a time to give them some extra recognition of their valor and sacrifice, this would be it.", IncidentTrigger.LeavingBattle, IncidentType.PartyCampLife, CampaignTime.Years(1000f), delegate
		{
			PlayerBattleEndedLogEntry playerBattleEndedLogEntry = Campaign.Current.LogEntryHistory.FindLastGameActionLog((PlayerBattleEndedLogEntry x) => true);
			return playerBattleEndedLogEntry != null && playerBattleEndedLogEntry.IsAgainstGreatOdds && Hero.MainHero.Gold >= 2000 && PartyBase.MainParty.MemberRoster.TotalRegulars >= 5;
		});
		incident95.AddOption("{=Wenvx1OE}Reach into your treasury and grab great fistfuls of silver denars, hurling it to your men and praising them by name for their deeds.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Calculating, -100),
			IncidentEffect.TraitChange(DefaultTraits.Generosity, 200),
			IncidentEffect.GoldChange(() => -(int)((float)Hero.MainHero.Gold * 0.5f)),
			IncidentEffect.RenownChange(5f)
		});
		incident95.AddOption("{=w3a39PVT}Give them an extra week's pay as a bonus, telling them they deserve far more but it is all you can afford.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Generosity, 100),
			IncidentEffect.GoldChange(() => MobileParty.MainParty.TotalWage * -7)
		});
		incident95.AddOption("{=WnlD7awi}Gruffly tell them that you were pleased they did their duty.", new List<IncidentEffect> { IncidentEffect.TraitChange(DefaultTraits.Generosity, -100) });
		Incident incident96 = RegisterIncident("incident_letter_of_reference", "{=q0HIXEal}Letter of Reference", "{=D7ZcX8XR}As you pass through the market, {NOTABLE.NAME} requests a minute of your time. {?NOTABLE.GENDER}Her{?}His{\\?} cousin is planning on doing some business in {TOWN}, and {?NOTABLE.GENDER}she{?}he{\\?} would like a letter of reference from you. You know next to nothing about the lad, although {NOTABLE.NAME} swears that he is honest and conscientious. You find you are receiving a number of these types of requests as your fame spreads.", IncidentTrigger.LeavingTown, IncidentType.Profit, CampaignTime.Years(1000f), delegate(TextObject description)
		{
			if (MobileParty.MainParty.LastVisitedSettlement == null)
			{
				return false;
			}
			Hero hero = GetLetterOfReferenceNotable();
			if (hero == null || Hero.MainHero.Clan.Renown < 200f)
			{
				return false;
			}
			description.SetCharacterProperties("NOTABLE", hero.CharacterObject);
			description.SetTextVariable("TOWN", hero.CurrentSettlement.Name);
			return true;
		});
		incident96.AddOption("{=z15Ux7sh}Write truthfully that you don't know much about the bearer of the letter, but he comes from good family.", new List<IncidentEffect> { IncidentEffect.TraitChange(DefaultTraits.Honor, 100) });
		incident96.AddOption("{=aZwaFhA6}Write effusively about the lad's qualities.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Generosity, 100),
			IncidentEffect.TraitChange(DefaultTraits.Honor, -100),
			IncidentEffect.HeroRelationChange(GetLetterOfReferenceNotable, 20)
		});
		incident96.AddOption("{=8rEX8rjv}Tell {NOTABLE.NAME} that you will write whatever {?NOTABLE.GENDER}she{?}he{\\?} wants, for the proper price.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Honor, -100),
			IncidentEffect.GoldChange(() => 300)
		}, delegate(TextObject text)
		{
			Hero hero = GetLetterOfReferenceNotable();
			text.SetCharacterProperties("NOTABLE", hero.CharacterObject);
			return true;
		});
		incident96.AddOption("{=DOPdMPla}Upbraid {NOTABLE.NAME} in the street for all to hear for daring to presume that you would recommend anyone that you do not know.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Honor, 200),
			IncidentEffect.TraitChange(DefaultTraits.Calculating, -100),
			IncidentEffect.HeroRelationChange(GetLetterOfReferenceNotable, -10)
		}, delegate(TextObject text)
		{
			Hero hero = GetLetterOfReferenceNotable();
			text.SetCharacterProperties("NOTABLE", hero.CharacterObject);
			return true;
		});
		Incident incident97 = RegisterIncident("incident_tiller_and_wanderer", "{=OqlihYNW}The Tiller and the Wanderer", "{=nuqL3OAF}As you are leaving your fief you stumble across an altercation. A group of nomads, who pass by annually on their migration from summer to winter pastures, have allowed their herds to graze in your tenants' crops. Your troops far outnumber the nomads and your tenants are urging you to teach them a lesson, but of course their kin might retaliate at a later date.", IncidentTrigger.LeavingTown | IncidentTrigger.LeavingCastle, IncidentType.FiefManagement, CampaignTime.Years(1000f), delegate
		{
			if (MobileParty.MainParty.LastVisitedSettlement == null)
			{
				return false;
			}
			return MobileParty.MainParty.LastVisitedSettlement.IsFortification && MobileParty.MainParty.LastVisitedSettlement.OwnerClan == Hero.MainHero.Clan && (MobileParty.MainParty.LastVisitedSettlement.Culture.StringId == "aserai" || MobileParty.MainParty.LastVisitedSettlement.Culture.StringId == "khuzait");
		});
		incident97.AddOption("{=fI8Bd468}Seize the nomads' herds to compensate you and your tenants for their crops.", new List<IncidentEffect>
		{
			IncidentEffect.ChangeItemAmount(GetTillerAndWandererHorseItem, () => 3),
			IncidentEffect.TraitChange(DefaultTraits.Valor, 100),
			IncidentEffect.TraitChange(DefaultTraits.Mercy, -100),
			IncidentEffect.TownProsperityChange(() => MobileParty.MainParty.LastVisitedSettlement.Town, 10),
			IncidentEffect.SettlementRelationChange(() => MobileParty.MainParty.LastVisitedSettlement, 20),
			IncidentEffect.TownSecurityChange(() => MobileParty.MainParty.LastVisitedSettlement.Town, -10),
			IncidentEffect.Custom(null, delegate
			{
				Clan clan = GetTillerAndWandererBanditPartyClan();
				for (int i = 0; i < 3; i++)
				{
					Hideout hideout = SettlementHelper.FindNearestHideoutToSettlement(MobileParty.MainParty.LastVisitedSettlement, MobileParty.NavigationType.Default);
					CampaignVec2 initialPosition = NavigationHelper.FindPointAroundPosition(MobileParty.MainParty.LastVisitedSettlement.GatePosition, MobileParty.NavigationType.Default, 5f);
					MobileParty mobileParty = BanditPartyComponent.CreateBanditParty("incident_tiller_and_wanderer_bandit_revenge_" + i, clan, hideout, isBossParty: false, null, initialPosition);
					mobileParty.MemberRoster.AddToCounts(clan.Culture.BanditBandit, MobileParty.MainParty.RandomIntWithSeed((uint)_activeIncidentSeed, 5, 17));
					mobileParty.InitializePartyTrade(200);
					mobileParty.SetCustomHomeSettlement(hideout.Settlement);
					mobileParty.Party.SetVisualAsDirty();
				}
				return new List<TextObject>
				{
					new TextObject("{=qxE0YAmo}3 bandit parties spawned nearby.")
				};
			}, (IncidentEffect effect) => new List<TextObject>
			{
				new TextObject("{=ayfKQixq}3 bandit parties spawn nearby")
			})
		});
		incident97.AddOption("{=ciQPfxEC}Demand that the nomads pay fair compensation for damaged crops, even though you know this will not settle the problem for long.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Honor, 100),
			IncidentEffect.TownSecurityChange(() => MobileParty.MainParty.LastVisitedSettlement.Town, -5),
			IncidentEffect.SettlementRelationChange(() => MobileParty.MainParty.LastVisitedSettlement, 10)
		});
		incident97.AddOption("{=fkQj3TIP}Tell the nomads that proud warriors such as themselves need not worry what these dirt-diggers think. Share a meal with them, and see if some will join you.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Generosity, -100),
			IncidentEffect.TraitChange(DefaultTraits.Valor, 100),
			IncidentEffect.ChangeTroopAmount(GetTillerAndWandererTroop, 4),
			IncidentEffect.SettlementRelationChange(() => MobileParty.MainParty.LastVisitedSettlement, -10),
			IncidentEffect.TownProsperityChange(() => MobileParty.MainParty.LastVisitedSettlement.Town, -10)
		});
		Incident incident98 = RegisterIncident("incident_cries_in_the_mist", "{=ELXomm1C}Cries in the Mist", "{=SAXnFQLq}The people of {VILLAGE_NAME} warned you to take care outside their village. When the mist gathers, The woods will be haunted by spirits whose shriek foretells death. Now the gloom is gathering, and you hear mutterings from your men. You are certain that they will soon be hearing wailing and seeing apparitions, real or imagined. The horses are becoming twitchy too, picking up on the mood of the men, and accidents are likely to happen.", IncidentTrigger.LeavingVillage, IncidentType.DreamsSongsAndSigns, CampaignTime.Years(1000f), delegate(TextObject description)
		{
			if (MobileParty.MainParty.LastVisitedSettlement == null || !MobileParty.MainParty.LastVisitedSettlement.IsVillage)
			{
				return false;
			}
			Village village = MobileParty.MainParty.LastVisitedSettlement.Village;
			bool flag = false;
			for (float num = -3f; num <= 3f; num += 0.25f)
			{
				for (float num2 = -3f; num2 <= 3f; num2 += 0.25f)
				{
					if (num * num + num2 * num2 <= 9f)
					{
						CampaignVec2 campaignVec = new CampaignVec2(village.Settlement.Position.ToVec2() + new Vec2(num, num2), isOnLand: true);
						if (campaignVec.Face.IsValid() && Campaign.Current.MapSceneWrapper.GetFaceTerrainType(campaignVec.Face) == TerrainType.Forest)
						{
							flag = true;
							break;
						}
					}
				}
				if (flag)
				{
					break;
				}
			}
			if (!flag)
			{
				return false;
			}
			description.SetTextVariable("VILLAGE_NAME", village.Name);
			return true;
		});
		incident98.AddOption("{=uD05twgi}Tell your men to ignore the superstitions of villagers, and press on.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Calculating, 100),
			IncidentEffect.TraitChange(DefaultTraits.Valor, 100),
			IncidentEffect.TraitChange(DefaultTraits.Generosity, -100),
			IncidentEffect.MoraleChange(-20f),
			IncidentEffect.WoundTroopsRandomly((TroopRosterElement x) => x.Character.IsMounted, () => Math.Min(MobileParty.MainParty.RandomIntWithSeed((uint)_activeIncidentSeed, 1, 6), (from x in MobileParty.MainParty.MemberRoster.GetTroopRoster()
				where x.Character.IsMounted
				select x).Sum((TroopRosterElement x) => x.Number))).WithChance(0.5f)
		});
		incident98.AddOption("{=BxpJKMYq}Dispel the spirits in the approved orthodox Calradian way, by loudly calling on Heaven to ward off the ghosts of Below.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Valor, 100),
			IncidentEffect.TraitChange(DefaultTraits.Calculating, -100)
		});
		incident98.AddOption("{=4Ef7xG1o}Propitiate the spirits as the villagers advised, by slaughtering a black cockerel that they sold one of your men.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Generosity, 100),
			IncidentEffect.TraitChange(DefaultTraits.Calculating, -100)
		});
		Incident incident99 = RegisterIncident("incident_feeling_the_bite", "{=0yKJ0J0N}Feeling the Bite", "{=D9T64ejr}As you leave {VILLAGE_NAME}, several of the villagers approach you. They warn you that stagnant ponds from the last rain have yet to fully dry and have bred mosquitos. They offer to sell you a concoction of oils including rosemary, catmint and other herbs that they say will ward off all flying vermin.", IncidentTrigger.LeavingVillage, IncidentType.HardTravel, CampaignTime.Years(1000f), delegate(TextObject description)
		{
			if (MobileParty.MainParty.LastVisitedSettlement != null)
			{
				CampaignTime.Seasons getSeasonOfYear = CampaignTime.Now.GetSeasonOfYear;
				bool result = getSeasonOfYear == CampaignTime.Seasons.Spring || getSeasonOfYear == CampaignTime.Seasons.Summer;
				description.SetTextVariable("VILLAGE_NAME", MobileParty.MainParty.LastVisitedSettlement.Name);
				return result;
			}
			return false;
		});
		incident99.AddOption("{=zGVNqFnh}Buy vials of ointment for every member of your party, at the risk of looking gullible.", new List<IncidentEffect>
		{
			IncidentEffect.GoldChange(() => -100),
			IncidentEffect.TraitChange(DefaultTraits.Calculating, -50),
			IncidentEffect.TraitChange(DefaultTraits.Generosity, 100)
		});
		incident99.AddOption("{=qCoqaa70}Have your men wrap themselves up as best they can and press on.", new List<IncidentEffect> { IncidentEffect.TraitChange(DefaultTraits.Generosity, -50) });
		incident99.AddOption("{=VccCV5dX}Take circuitous routs to avoid the ponds.", new List<IncidentEffect>
		{
			IncidentEffect.DisorganizeParty(),
			IncidentEffect.TraitChange(DefaultTraits.Calculating, 100)
		});
		Incident incident100 = RegisterIncident("incident_nearly_a_gift_horse", "{=N19m9qmd}Nearly a Gift Horse", "{=WR7NCetE}As you leave {VILLAGE_NAME}, a scraggly young man waves to attract your attention. He leads a magnificent {RARE_HORSE}, and says that he will sell it to you for a mere 500 denars. He claims he found it wandering on a heath, almost dead from exhaustion and neglect, and he nursed it back to health. He won't go into town because he says he would be unfairly accused of theft.", IncidentTrigger.LeavingVillage, IncidentType.TroopSettlementRelation, CampaignTime.Years(1000f), delegate(TextObject description)
		{
			if (MobileParty.MainParty.LastVisitedSettlement == null || !MobileParty.MainParty.LastVisitedSettlement.IsVillage)
			{
				return false;
			}
			ItemObject itemObject = GetNearlyAGiftHorseHorse();
			if (itemObject == null)
			{
				return false;
			}
			description.SetTextVariable("VILLAGE_NAME", MobileParty.MainParty.LastVisitedSettlement.Name);
			description.SetTextVariable("RARE_HORSE", itemObject.Name);
			description.SetTextVariable("TOWN_NAME", MobileParty.MainParty.LastVisitedSettlement.Village.Bound.Name);
			return true;
		});
		incident100.AddOption("{=YVETzHXK}Plausible enough, and you have no time to check the provenance of every bargain you find. Buy the horse!", new List<IncidentEffect>
		{
			IncidentEffect.GoldChange(() => -500),
			IncidentEffect.TraitChange(DefaultTraits.Honor, -50),
			IncidentEffect.ChangeItemAmount(GetNearlyAGiftHorseHorse, () => 1)
		});
		incident100.AddOption("{=B9HD9PNw}A likely story... Place the man under arrest and send word to the authorities of {TOWN_NAME}, who have the time to sort things out.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Honor, 100),
			IncidentEffect.TraitChange(DefaultTraits.Mercy, -50),
			IncidentEffect.DisorganizeParty()
		}, delegate(TextObject text)
		{
			Settlement lastVisitedSettlement = MobileParty.MainParty.LastVisitedSettlement;
			text.SetTextVariable("TOWN_NAME", lastVisitedSettlement.Village.Bound.Name);
			return true;
		});
		incident100.AddOption("{=CyM5bAdB}You're not sure you trust the seller, but you're not handing over to officials who might just hang him and seize the horse themselves. Go on your way.", new List<IncidentEffect> { IncidentEffect.TraitChange(DefaultTraits.Mercy, 100) });
		incident100.AddOption("{=AwBJzppu}Take the horse without paying. Tell the seller that thieves have no right to complain of theft.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Mercy, -100),
			IncidentEffect.TraitChange(DefaultTraits.Honor, -100),
			IncidentEffect.ChangeItemAmount(GetNearlyAGiftHorseHorse, () => 1)
		});
		Incident incident101 = RegisterIncident("incident_hammer_of_the_sun", "{=R80p9Ywt}Hammer of the Sun", "{=th5MgyAZ}As you enter {TOWN_NAME}, one of your {ASERAI_TROOPS} approaches you. Many of his comrades are not used to the heat of the desert, he says. They march after drinking wine through the evening but are not careful to properly take water throughout the day, as the sun beats down on them. He has seen men collapse and die from such carelessness and ignorance.", IncidentTrigger.EnteringTown, IncidentType.HardTravel, CampaignTime.Years(1000f), delegate(TextObject description)
		{
			if (!MobileParty.MainParty.Position.IsValid())
			{
				return false;
			}
			if (Campaign.Current.MapSceneWrapper.GetFaceTerrainType(MobileParty.MainParty.Position.Face) != TerrainType.Desert || MobileParty.MainParty.MemberRoster.TotalRegulars < 50)
			{
				return false;
			}
			Settlement currentSettlement = MobileParty.MainParty.CurrentSettlement;
			MBList<TroopRosterElement> troopRoster = MobileParty.MainParty.MemberRoster.GetTroopRoster();
			if (troopRoster.Count((TroopRosterElement x) => x.Character.Culture.StringId != "aserai" && !x.Character.IsHero) < 10)
			{
				return false;
			}
			CharacterObject character = IncidentHelper.GetSeededRandomElement(troopRoster.Where((TroopRosterElement x) => x.Character.Culture.StringId == "aserai" && !x.Character.IsHero).ToList(), _activeIncidentSeed).Character;
			if (character == null)
			{
				return false;
			}
			description.SetTextVariable("TOWN_NAME", currentSettlement.Name);
			description.SetTextVariable("ASERAI_TROOPS", character.Name);
			return true;
		});
		incident101.AddOption("{=lbSsRg8w}Your men will resent any curbs on their celebrations, even for their own health. Warn them but take no other precautions.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Generosity, 100),
			IncidentEffect.KillTroopsRandomlyByChance(0.01f)
		});
		incident101.AddOption("{=3Uub2e17}Tell your men that there shall be no wine tonight, and none henceforth until they learn how to handle themselves in the desert.", new List<IncidentEffect> { IncidentEffect.TraitChange(DefaultTraits.Generosity, -100) });
		incident101.AddOption("{=cG9pkbqo}Let them men celebrate, but move slowly in the morning, pausing to make sure that each man is drinking from his water-skin.", new List<IncidentEffect>
		{
			IncidentEffect.TraitChange(DefaultTraits.Generosity, 50),
			IncidentEffect.TraitChange(DefaultTraits.Calculating, 50),
			IncidentEffect.DisorganizeParty(),
			IncidentEffect.WoundTroopsRandomlyByChance(0.01f)
		});
		static bool BanditPrisonerPredicate(TroopRosterElement x)
		{
			if (x.Character.Culture.IsBandit)
			{
				return !x.Character.IsHero;
			}
			return false;
		}
		static bool CavalryTroopPredicate(TroopRosterElement x)
		{
			if (x.Character.IsMounted)
			{
				return !x.Character.IsHero;
			}
			return false;
		}
		static bool CavalryTroopsPredicate(TroopRosterElement x)
		{
			if (x.Character.Tier >= 4 && x.Character.HasMount())
			{
				return !x.Character.IsHero;
			}
			return false;
		}
		static BesiegeSettlementLogEntry FindLastBesiegeSettlementLogEntry()
		{
			return Campaign.Current.LogEntryHistory.FindLastGameActionLog((BesiegeSettlementLogEntry x) => x.BesiegerHero == Hero.MainHero && x.Settlement == MobileParty.MainParty.LastVisitedSettlement);
		}
		static SiegeAftermathLogEntry FindLastSiegeAftermathLogEntry(BesiegeSettlementLogEntry siegeLog)
		{
			return Campaign.Current.LogEntryHistory.FindLastGameActionLog((SiegeAftermathLogEntry x) => x.PlayerWasInvolved && x.SiegeAftermath == SiegeAftermathAction.SiegeAftermath.ShowMercy && x.CapturedSettlement == siegeLog.Settlement);
		}
		static bool FootmenTroopsPredicate(TroopRosterElement x)
		{
			if (x.Character.Tier < 4)
			{
				return !x.Character.IsHero;
			}
			return false;
		}
		ItemObject GetBrokenWagonCheapestHorseItem()
		{
			List<ItemRosterElement> list = MobileParty.MainParty.ItemRoster.Where((ItemRosterElement x) => x.EquipmentElement.Item.HasHorseComponent).ToList();
			return list[MobileParty.MainParty.RandomIntWithSeed((uint)_activeIncidentSeed, list.Count)].EquipmentElement.Item;
		}
		ItemObject GetBrokenWagonDraughtAnimalItem()
		{
			List<ItemRosterElement> list = MobileParty.MainParty.ItemRoster.Where((ItemRosterElement x) => x.EquipmentElement.Item.HasHorseComponent || x.EquipmentElement.Item.StringId == "mule" || x.EquipmentElement.Item.StringId == "mule_unmountable").ToList();
			return list[MobileParty.MainParty.RandomIntWithSeed((uint)_activeIncidentSeed, list.Count)].EquipmentElement.Item;
		}
		ItemObject GetDesperateTimesFoodItem()
		{
			return IncidentHelper.GetSeededRandomElement(MobileParty.MainParty.ItemRoster.Where((ItemRosterElement x) => x.EquipmentElement.Item.IsFood && x.Amount >= 1).ToList(), _activeIncidentSeed).EquipmentElement.Item;
		}
		static Building GetEndebtedFarmersBuilding()
		{
			return MobileParty.MainParty.LastVisitedSettlement.Town.Buildings.FirstOrDefault((Building x) => x.BuildingType == DefaultBuildingTypes.CastleFarmlands);
		}
		CharacterObject GetFirstNobleTroop()
		{
			return IncidentHelper.GetSeededRandomElement((from x in MobileParty.MainParty.MemberRoster.GetTroopRoster()
				where x.Character.Tier >= 4 && x.Character.HasMount() && !x.Character.IsHero
				select x).ToList(), _activeIncidentSeed).Character;
		}
		CharacterObject GetGnosticBoysNobleTroop()
		{
			return IncidentHelper.GetSeededRandomElement((from x in MobileParty.MainParty.MemberRoster.GetTroopRoster()
				where !x.Character.IsHero && x.Character.Culture.StringId == "empire" && x.Character.Tier >= 3 && x.Number >= 2
				select x).ToList(), _activeIncidentSeed).Character;
		}
		Workshop GetHandInTheStrongboxWorkshop()
		{
			Town seededRandomElement = IncidentHelper.GetSeededRandomElement(Town.AllTowns.Where((Town x) => x.Workshops.Any((Workshop y) => y.Owner == Hero.MainHero)).ToList(), _activeIncidentSeed);
			if (seededRandomElement == null)
			{
				return null;
			}
			return IncidentHelper.GetSeededRandomElement(seededRandomElement.Workshops.Where((Workshop w) => w.Owner == Hero.MainHero).ToList(), _activeIncidentSeed);
		}
		TroopRosterElement GetHonorTheSlainFoeRandomTroop()
		{
			return IncidentHelper.GetSeededRandomElement((from x in MobileParty.MainParty.MemberRoster.GetTroopRoster()
				where !x.Character.IsHero
				select x).ToList(), _activeIncidentSeed);
		}
		static List<ItemObject> GetHorseflyPlagueHorseItems()
		{
			return (from x in MobileParty.MainParty.ItemRoster
				where x.EquipmentElement.Item.HasHorseComponent
				select x.EquipmentElement.Item).ToList();
		}
		CharacterObject GetInsultCavalryman()
		{
			return IncidentHelper.GetSeededRandomElement(MobileParty.MainParty.MemberRoster.GetTroopRoster().Where(CavalryTroopsPredicate).ToList(), _activeIncidentSeed).Character;
		}
		CharacterObject GetInsultFootman()
		{
			return IncidentHelper.GetSeededRandomElement(MobileParty.MainParty.MemberRoster.GetTroopRoster().Where(FootmenTroopsPredicate).ToList(), _activeIncidentSeed).Character;
		}
		Hero GetIntriguingRumorsRivalLord()
		{
			return IncidentHelper.GetSeededRandomElement(Hero.MainHero.Clan.Kingdom?.AliveLords.Where((Hero lord) => lord != Hero.MainHero && Hero.MainHero.GetRelation(lord) < 0).ToList(), _activeIncidentSeed);
		}
		CharacterObject GetJobOfferTroop()
		{
			return IncidentHelper.GetSeededRandomElement((from x in MobileParty.MainParty.MemberRoster.GetTroopRoster()
				where !x.Character.IsHero && x.Character.Tier >= 5
				select x).ToList(), _activeIncidentSeed).Character;
		}
		Hero GetJobsForTheLadsArtisan()
		{
			return IncidentHelper.GetSeededRandomElement(MobileParty.MainParty.CurrentSettlement.Town.Settlement.Notables.Where((Hero n) => n.GetTraitLevel(DefaultTraits.Honor) <= 0 && Hero.MainHero.GetRelation(n) < 10 && n.Occupation == Occupation.Artisan).ToList(), _activeIncidentSeed);
		}
		Workshop GetJobsForTheLadsWorkshop()
		{
			return IncidentHelper.GetSeededRandomElement(MobileParty.MainParty.CurrentSettlement.Town.Workshops.Where((Workshop w) => w.Owner == Hero.MainHero).ToList(), _activeIncidentSeed);
		}
		Hero GetLetterOfReferenceNotable()
		{
			return IncidentHelper.GetSeededRandomElement(MobileParty.MainParty.LastVisitedSettlement.Town.Settlement.Notables.Where((Hero x) => x.Occupation == Occupation.Merchant && x.GetTraitLevel(DefaultTraits.Honor) <= 0).ToList(), _activeIncidentSeed);
		}
		CharacterObject GetMadDogTroop()
		{
			List<CharacterObject> collection = CharacterHelper.GetTroopTree(MobileParty.MainParty.LastVisitedSettlement.Culture.BasicTroop, 5f, 5f).ToList();
			List<CharacterObject> collection2 = CharacterHelper.GetTroopTree(MobileParty.MainParty.LastVisitedSettlement.Culture.EliteBasicTroop, 5f, 5f).ToList();
			List<CharacterObject> list = new List<CharacterObject>();
			list.AddRange(collection);
			list.AddRange(collection2);
			return IncidentHelper.GetSeededRandomElement(list, _activeIncidentSeed);
		}
		Hero GetMarketManipulationMerchant()
		{
			return IncidentHelper.GetSeededRandomElement(MobileParty.MainParty.CurrentSettlement.Notables.Where((Hero x) => x.Occupation == Occupation.Merchant).ToList(), _activeIncidentSeed);
		}
		static Village GetMisplacedVengeanceLootedVillage()
		{
			Village result = null;
			float num = float.MaxValue;
			foreach (Village item6 in Village.All)
			{
				if (item6.VillageState == Village.VillageStates.Looted && Hero.MainHero.MapFaction.IsAtWarWith(item6.Settlement.MapFaction))
				{
					float distance = Campaign.Current.Models.MapDistanceModel.GetDistance(MobileParty.MainParty.LastVisitedSettlement.Village.Settlement, item6.Settlement, isFromPort: false, isTargetingPort: false, MobileParty.NavigationType.All);
					if (distance <= num && distance <= 1000f)
					{
						result = item6;
						num = distance;
					}
				}
			}
			return result;
		}
		static List<Settlement> GetNearbyVillages()
		{
			float estimatedLandRatio;
			return (from s in Settlement.All
				where s.IsVillage
				orderby Campaign.Current.Models.MapDistanceModel.GetDistance(MobileParty.MainParty, s, isTargetingPort: false, MobileParty.NavigationType.All, out estimatedLandRatio)
				select s).Take(3).ToList();
		}
		ItemObject GetNearlyAGiftHorseHorse()
		{
			return IncidentHelper.GetSeededRandomElement(Campaign.Current.AllItems.Where((ItemObject x) => x.ItemCategory == DefaultItemCategories.Horse && x.Culture == MobileParty.MainParty.LastVisitedSettlement.Culture && x.Value > 500).ToList(), _activeIncidentSeed);
		}
		CharacterObject GetNoMoodForMercyRandomTroop()
		{
			return IncidentHelper.GetSeededRandomElement((from x in MobileParty.MainParty.MemberRoster.GetTroopRoster()
				where !x.Character.IsHero
				select x).ToList(), _activeIncidentSeed).Character;
		}
		CharacterObject GetNobleTroop(CultureObject culture)
		{
			List<TroopRosterElement> list = (from x in MobileParty.MainParty.MemberRoster.GetTroopRoster()
				where !x.Character.IsHero && x.Character.Tier >= 4 && x.Character.IsMounted && x.Number > 0 && x.Character.Culture == culture && CharacterHelper.GetTroopTree(culture.EliteBasicTroop).Contains(x.Character)
				select x).ToList();
			if (list.Count == 0)
			{
				return null;
			}
			return IncidentHelper.GetSeededRandomElement(list, _activeIncidentSeed).Character;
		}
		CharacterObject GetNobleTroopForUpgrade()
		{
			CharacterObject characterObject = GetRegularTroop();
			if (characterObject == null)
			{
				return null;
			}
			return GetNobleTroop(characterObject.Culture);
		}
		Workshop GetOccupationalSafetyWorkshop()
		{
			WorkshopType breweryWorkshopType = WorkshopType.Find("brewery");
			WorkshopType smithyWorkshopType = WorkshopType.Find("smithy");
			return IncidentHelper.GetSeededRandomElement(MobileParty.MainParty.CurrentSettlement.Town.Workshops.Where((Workshop w) => w.Owner == Hero.MainHero && (w.WorkshopType == breweryWorkshopType || w.WorkshopType == smithyWorkshopType)).ToList(), _activeIncidentSeed);
		}
		Village GetOtherVillage()
		{
			MBReadOnlyList<Village> villages = MobileParty.MainParty.LastVisitedSettlement.Town.Villages;
			Village village = GetVillageOne();
			Village result = null;
			float num = float.MaxValue;
			foreach (Village item7 in Village.All)
			{
				if (!villages.Contains(item7))
				{
					float distance = Campaign.Current.Models.MapDistanceModel.GetDistance(village.Settlement, item7.Settlement, isFromPort: false, isTargetingPort: false, MobileParty.NavigationType.Default);
					if (distance < num && item7.Settlement.OwnerClan != Clan.PlayerClan)
					{
						result = item7;
						num = distance;
					}
				}
			}
			return result;
		}
		static Building GetPreemptiveWorkBuilding()
		{
			return MobileParty.MainParty.LastVisitedSettlement.Town.Buildings.FirstOrDefault((Building x) => x.BuildingType == DefaultBuildingTypes.CastleFarmlands);
		}
		ItemObject GetPurebredHorseItem()
		{
			return IncidentHelper.GetSeededRandomElement(MobileParty.MainParty.ItemRoster.Where((ItemRosterElement x) => x.EquipmentElement.Item.HasHorseComponent && x.Amount >= 1).ToList(), _activeIncidentSeed).EquipmentElement.Item;
		}
		TroopRosterElement GetRandomCavalryTroop()
		{
			return IncidentHelper.GetSeededRandomElement(MobileParty.MainParty.MemberRoster.GetTroopRoster().Where(CavalryTroopPredicate).ToList(), _activeIncidentSeed);
		}
		TroopRosterElement GetRandomTroop()
		{
			return IncidentHelper.GetSeededRandomElement((from x in MobileParty.MainParty.MemberRoster.GetTroopRoster()
				where !x.Character.IsHero
				select x).ToList(), _activeIncidentSeed);
		}
		CharacterObject GetRegularTroop()
		{
			List<TroopRosterElement> list = (from x in MobileParty.MainParty.MemberRoster.GetTroopRoster()
				where !x.Character.IsHero && x.Character.Tier < 4 && x.Character.Tier > 1 && !x.Character.IsMounted && x.Number > 0 && CharacterHelper.GetTroopTree(x.Character.Culture.BasicTroop).Contains(x.Character) && GetNobleTroop(x.Character.Culture) != null
				select x).ToList();
			if (list.Count == 0)
			{
				return null;
			}
			return IncidentHelper.GetSeededRandomElement(list, _activeIncidentSeed).Character;
		}
		static CultureObject GetRespectForTheSlainNearestOtherCulture()
		{
			return (from s in Settlement.All
				where s.Culture != MobileParty.MainParty.LastVisitedSettlement.Culture
				orderby Campaign.Current.Models.MapDistanceModel.GetDistance(MobileParty.MainParty.LastVisitedSettlement, s, isFromPort: false, isTargetingPort: false, MobileParty.NavigationType.All)
				select s).FirstOrDefault()?.Culture;
		}
		CharacterObject GetRespectForTheSlainSelectedTroop()
		{
			return IncidentHelper.GetSeededRandomElement(MobileParty.MainParty.MemberRoster.GetTroopRoster().Where(HighTierTroopsPredicate).ToList(), _activeIncidentSeed).Character;
		}
		Workshop GetSecessionOfThePlebsWorkshop()
		{
			List<Town> list = Town.AllTowns.Where((Town x) => x.Workshops.Any((Workshop y) => y.Owner == Hero.MainHero)).ToList();
			if (list.Count == 0)
			{
				return null;
			}
			return IncidentHelper.GetSeededRandomElement(IncidentHelper.GetSeededRandomElement(list, _activeIncidentSeed).Workshops.Where((Workshop w) => w.Owner == Hero.MainHero).ToList(), _activeIncidentSeed);
		}
		CharacterObject GetSecondNobleTroop()
		{
			return IncidentHelper.GetSeededRandomElement((from x in MobileParty.MainParty.MemberRoster.GetTroopRoster()
				where x.Character.Tier >= 4 && x.Character.HasMount() && !x.Character.IsHero && x.Character != GetFirstNobleTroop()
				select x).ToList(), _activeIncidentSeed).Character;
		}
		static CharacterObject GetSkolderbrotvaTroop()
		{
			return CharacterObject.FindFirst((CharacterObject x) => x.StringId == "skolderbrotva_tier_3");
		}
		CharacterObject GetSleepingSentryTroop()
		{
			List<TroopRosterElement> list = (from x in MobileParty.MainParty.MemberRoster.GetTroopRoster()
				where x.Character.Tier <= 2 && !x.Character.IsHero
				select x).ToList();
			if (list.Count == 0)
			{
				return null;
			}
			return IncidentHelper.GetSeededRandomElement(list, _activeIncidentSeed).Character;
		}
		static CharacterObject GetSoldierInDebtTroop()
		{
			return MobileParty.MainParty.MemberRoster.GetTroopRoster().GetRandomElementWithPredicate((TroopRosterElement x) => !x.Character.IsHero).Character;
		}
		ItemObject GetSpeculativeInvestmentOtherGood()
		{
			return IncidentHelper.GetSeededRandomElement(new List<ItemObject>
			{
				Game.Current.ObjectManager.GetObject<ItemObject>("wine"),
				Game.Current.ObjectManager.GetObject<ItemObject>("cotton"),
				Game.Current.ObjectManager.GetObject<ItemObject>("fur")
			}, _activeIncidentSeed);
		}
		ItemObject GetSpoiledFoodItem()
		{
			return IncidentHelper.GetSeededRandomElement(MobileParty.MainParty.ItemRoster.Where((ItemRosterElement x) => x.EquipmentElement.Item.IsFood && x.Amount >= 2).ToList(), _activeIncidentSeed).EquipmentElement.Item;
		}
		CharacterObject GetTheQuietLifeTroop()
		{
			return IncidentHelper.GetSeededRandomElement((from x in MobileParty.MainParty.MemberRoster.GetTroopRoster()
				where !x.Character.IsHero && x.Character.Tier >= 4 && x.Number >= 3
				select x).ToList(), _activeIncidentSeed).Character;
		}
		Clan GetTillerAndWandererBanditPartyClan()
		{
			if (!(MobileParty.MainParty.RandomFloatWithSeed((uint)_activeIncidentSeed) < 0.5f))
			{
				return Clan.BanditFactions.FirstOrDefault((Clan x) => x.StringId == "steppe_bandits");
			}
			return Clan.BanditFactions.FirstOrDefault((Clan x) => x.StringId == "desert_bandits");
		}
		static ItemObject GetTillerAndWandererHorseItem()
		{
			if (!(MobileParty.MainParty.LastVisitedSettlement.Culture.StringId == "aserai"))
			{
				return Game.Current.ObjectManager.GetObject<ItemObject>("khuzait_horse");
			}
			return Game.Current.ObjectManager.GetObject<ItemObject>("aserai_horse");
		}
		static CharacterObject GetTillerAndWandererTroop()
		{
			return (from x in CharacterHelper.GetTroopTree(MobileParty.MainParty.LastVisitedSettlement.Culture.BasicTroop, 4f, 4f)
				where x.IsMounted
				select x).ToList().GetRandomElement();
		}
		ItemRosterElement GetTradeProposalGood()
		{
			return IncidentHelper.GetSeededRandomElement(MobileParty.MainParty.ItemRoster.Where((ItemRosterElement x) => x.EquipmentElement.Item.IsTradeGood).ToList(), _activeIncidentSeed);
		}
		static Building GetTurnPikeBuilding()
		{
			return MobileParty.MainParty.LastVisitedSettlement.Town.Buildings.FirstOrDefault((Building x) => x.BuildingType == DefaultBuildingTypes.CastleRoadsAndPaths || x.BuildingType == DefaultBuildingTypes.SettlementRoadsAndPaths);
		}
		ItemObject GetVeteranFoodItem()
		{
			return IncidentHelper.GetSeededRandomElement(MobileParty.MainParty.ItemRoster.Where((ItemRosterElement x) => x.EquipmentElement.Item.IsFood && x.Amount >= 1).ToList(), _activeIncidentSeed).EquipmentElement.Item;
		}
		static CharacterObject GetVeteranMaxTierTroop()
		{
			List<TroopRosterElement> list = (from x in MobileParty.MainParty.MemberRoster.GetTroopRoster()
				where !x.Character.IsHero
				select x).ToList();
			if (list.Count == 0)
			{
				return null;
			}
			return TaleWorlds.Core.Extensions.MaxBy(list, (TroopRosterElement x) => x.Character.Tier).Character;
		}
		Village GetVillageOne()
		{
			return IncidentHelper.GetSeededRandomElement(MobileParty.MainParty.LastVisitedSettlement.Town.Villages, _activeIncidentSeed);
		}
		static Building GetWagesOfWarAndDiseasesBuilding()
		{
			return MobileParty.MainParty.LastVisitedSettlement.Town.Buildings.FirstOrDefault((Building x) => x.BuildingType == DefaultBuildingTypes.CastleFarmlands);
		}
		CharacterObject GetWantedCriminalTroop()
		{
			List<TroopRosterElement> list = (from x in MobileParty.MainParty.MemberRoster.GetTroopRoster()
				where !x.Character.IsHero
				select x).ToList();
			if (list.Count == 0)
			{
				return null;
			}
			return IncidentHelper.GetSeededRandomElement(list, _activeIncidentSeed).Character;
		}
		static bool HighTierTroopsPredicate(TroopRosterElement x)
		{
			if (!x.Character.IsHero)
			{
				return x.Character.Tier >= 4;
			}
			return false;
		}
		static bool LoveMarriageTroopPredicate(TroopRosterElement x)
		{
			if (!x.Character.IsHero && x.Character.Tier >= 4)
			{
				return x.Character.IsMounted;
			}
			return false;
		}
		static bool TroopToDemotePredicate(TroopRosterElement x)
		{
			if (x.Character.HasMount())
			{
				return !x.Character.IsHero;
			}
			return false;
		}
		static bool TroopToDemoteToPredicate(CharacterObject x)
		{
			return !x.HasMount();
		}
		static bool TroopToDemoteToPredicate2(CharacterObject x)
		{
			return !x.HasMount();
		}
		static bool TroopsToDemotePredicate(TroopRosterElement x)
		{
			if (x.Character.HasMount())
			{
				return !x.Character.IsHero;
			}
			return false;
		}
	}
}
