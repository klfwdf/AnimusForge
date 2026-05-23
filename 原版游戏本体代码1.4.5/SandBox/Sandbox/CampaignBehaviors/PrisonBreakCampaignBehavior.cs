using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;

namespace SandBox.CampaignBehaviors;

public class PrisonBreakCampaignBehavior : CampaignBehaviorBase
{
	private const int CoolDownInDays = 7;

	private const int PrisonBreakDialogPriority = 120;

	private const string DefaultPrisonGuardWeaponId = "battania_mace_1_t2";

	private Dictionary<Settlement, CampaignTime> _coolDownData = new Dictionary<Settlement, CampaignTime>();

	private Hero _prisonerHero;

	private bool _launchingPrisonBreakMission;

	private int _bribeCost;

	private string _previousMenuId;

	public override void RegisterEvents()
	{
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
		CampaignEvents.CanHeroDieEvent.AddNonSerializedListener(this, CanHeroDie);
		CampaignEvents.LocationCharactersAreReadyToSpawnEvent.AddNonSerializedListener(this, LocationCharactersAreReadyToSpawn);
	}

	private void LocationCharactersAreReadyToSpawn(Dictionary<string, int> availableSpawnPoints)
	{
		if (_launchingPrisonBreakMission)
		{
			_launchingPrisonBreakMission = false;
			int num = 8;
			Location locationWithId = LocationComplex.Current.GetLocationWithId("prison");
			locationWithId.RemoveAllCharacters();
			locationWithId.AddCharacter(CreatePrisonBreakPrisoner());
			for (int i = 0; i < num; i++)
			{
				LocationCharacter locationCharacter = CreatePrisonBreakGuard();
				locationWithId.AddCharacter(locationCharacter);
			}
		}
	}

	private LocationCharacter CreatePrisonBreakPrisoner()
	{
		AgentData agentData = new AgentData(new SimpleAgentOrigin(_prisonerHero.CharacterObject)).Age((int)_prisonerHero.CharacterObject.Age).NoHorses(noHorses: true);
		return new LocationCharacter(agentData, SandBoxManager.Instance.AgentBehaviorManager.AddCompanionBehaviors, "sp_prison_break_prisoner", fixedLocation: true, LocationCharacter.CharacterRelations.Friendly, ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, agentData.AgentIsFemale, "_guard"), useCivilianEquipment: true);
	}

	public LocationCharacter CreatePrisonBreakGuard()
	{
		AgentData prisonGuardAgentData = GetPrisonGuardAgentData();
		Campaign.Current.Models.AgeModel.GetAgeLimitForLocation((CharacterObject)prisonGuardAgentData.AgentCharacter, out var minimumAge, out var maximumAge);
		prisonGuardAgentData.Age(MBRandom.RandomInt(minimumAge, maximumAge));
		return new LocationCharacter(prisonGuardAgentData, SandBoxManager.Instance.AgentBehaviorManager.AddStealthAgentBehaviors, "stealth_agent", fixedLocation: true, LocationCharacter.CharacterRelations.Enemy, ActionSetCode.GenerateActionSetNameWithSuffix(prisonGuardAgentData.AgentMonster, prisonGuardAgentData.AgentIsFemale, "_guard"), useCivilianEquipment: false);
	}

	private AgentData GetPrisonGuardAgentData()
	{
		List<CharacterObject> list = CharacterHelper.GetTroopTree(Settlement.CurrentSettlement.Owner.Culture.BasicTroop.Culture.BasicTroop, 2f, 3f).ToListQ();
		CharacterObject obj = (list.AnyQ((CharacterObject x) => !x.IsRanged) ? list.Where((CharacterObject x) => !x.IsRanged).GetRandomElementInefficiently() : list.GetRandomElementInefficiently());
		Equipment equipment = obj.Equipment.Clone(cloneWithoutWeapons: true);
		equipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.WeaponItemBeginSlot, new EquipmentElement(Game.Current.ObjectManager.GetObject<ItemObject>("battania_mace_1_t2")));
		return new AgentData(new SimpleAgentOrigin(obj)).Equipment(equipment);
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_prisonerHero", ref _prisonerHero);
		dataStore.SyncData("_coolDownData", ref _coolDownData);
		dataStore.SyncData("_previousMenuId", ref _previousMenuId);
	}

	private void CanHeroDie(Hero hero, KillCharacterAction.KillCharacterActionDetail detail, ref bool result)
	{
		if (detail == KillCharacterAction.KillCharacterActionDetail.DiedInBattle && hero == Hero.MainHero && _prisonerHero != null && CampaignMission.Current != null && CampaignMission.Current.Location == Settlement.CurrentSettlement?.LocationComplex?.GetLocationWithId("prison"))
		{
			result = false;
		}
	}

	private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
	{
		AddGameMenus(campaignGameStarter);
		AddDialogs(campaignGameStarter);
	}

	private void AddGameMenus(CampaignGameStarter campaignGameStarter)
	{
		campaignGameStarter.AddGameMenuOption("town_keep_dungeon", "town_prison_break", "{=lc0YIqby}Stage a prison break", game_menu_stage_prison_break_on_condition, game_menu_castle_prison_break_from_dungeon_on_consequence, isLeave: false, 2);
		campaignGameStarter.AddGameMenuOption("castle_dungeon", "town_prison_break", "{=lc0YIqby}Stage a prison break", game_menu_stage_prison_break_on_condition, game_menu_castle_prison_break_from_castle_dungeon_on_consequence, isLeave: false, 2);
		campaignGameStarter.AddGameMenuOption("town_enemy_town_keep", "town_prison_break", "{=lc0YIqby}Stage a prison break", game_menu_stage_prison_break_on_condition, game_menu_castle_prison_break_from_enemy_keep_on_consequence, isLeave: false, 0);
		campaignGameStarter.AddGameMenu("start_prison_break", "{=aZaujaHb}The guard accepts your offer. He is ready to help you break {PRISONER.NAME} out, if you're willing to pay.", start_prison_break_on_init);
		campaignGameStarter.AddGameMenuOption("start_prison_break", "start", "{=N6UeziT8}Start ({COST}{GOLD_ICON})", game_menu_castle_prison_break_on_condition, delegate
		{
			OpenPrisonBreakMission();
		});
		campaignGameStarter.AddGameMenuOption("start_prison_break", "leave", "{=3sRdGQou}Leave", game_menu_leave_on_condition, game_menu_cancel_prison_break, isLeave: true);
		campaignGameStarter.AddGameMenu("prison_break_cool_down", "{=cGSXFJ3N}Because of a recent breakout attempt in this settlement it is on high alert. The guard won't even be seen talking to you.", null);
		campaignGameStarter.AddGameMenuOption("prison_break_cool_down", "leave", "{=3sRdGQou}Leave", game_menu_leave_on_condition, game_menu_cancel_prison_break, isLeave: true);
		campaignGameStarter.AddGameMenu("settlement_prison_break_success", "{=TazumJGN}You emerge into the streets. No one is yet aware of what happened in the dungeons, and you hustle {PRISONER.NAME} towards the gates.{newline}You may now leave the {?SETTLEMENT_TYPE}settlement{?}castle{\\?}.", settlement_prison_break_success_on_init);
		campaignGameStarter.AddGameMenuOption("settlement_prison_break_success", "continue", "{=DM6luo3c}Continue", game_menu_continue_on_condition, settlement_prison_break_success_continue_on_consequence);
		campaignGameStarter.AddGameMenu("settlement_prison_break_fail_player_unconscious", "{=svuD2vBo}You were knocked unconscious while trying to break {PRISONER.NAME} out of the dungeon.{newline}The guards caught you both and threw you in a cell.", settlement_prison_break_fail_on_init, GameMenu.MenuOverlayType.SettlementWithBoth);
		campaignGameStarter.AddGameMenuOption("settlement_prison_break_fail_player_unconscious", "continue", "{=DM6luo3c}Continue", game_menu_continue_on_condition, settlement_prison_break_fail_player_unconscious_continue_on_consequence);
		campaignGameStarter.AddGameMenu("settlement_prison_break_fail_prisoner_unconscious", "{=eKy1II3h}You made your way out but {PRISONER.NAME} was badly wounded during the escape. You had no choice but to leave {?PRISONER.GENDER}her{?}him{\\?} behind as you disappeared into the back streets and sneaked out the gate.{INFORMATION_IF_PRISONER_DEAD}", settlement_prison_break_fail_prisoner_injured_on_init, GameMenu.MenuOverlayType.SettlementWithBoth);
		campaignGameStarter.AddGameMenuOption("settlement_prison_break_fail_prisoner_unconscious", "continue", "{=DM6luo3c}Continue", game_menu_continue_on_condition, settlement_prison_break_fail_prisoner_unconscious_continue_on_consequence);
	}

	private void AddDialogs(CampaignGameStarter campaignGameStarter)
	{
		campaignGameStarter.AddDialogLine("prison_break_start_1", "start", "prison_break_end_already_met", "{=5RDF3aZN}{SALUTATION}... You came for me!", prison_break_end_with_success_clan_member, null, 120);
		campaignGameStarter.AddDialogLine("prison_break_start_2", "start", "prison_break_end_already_met", "{=PRadDFN5}{SALUTATION}... Well, I hadn't expected this, but I'm very grateful.", prison_break_end_with_success_player_already_met, null, 120);
		campaignGameStarter.AddDialogLine("prison_break_start_3", "start", "prison_break_end_meet", "{=zbPRul7h}Well.. I don't know you, but I'm very grateful.", prison_break_end_with_success_other_on_condition, null, 120);
		campaignGameStarter.AddPlayerLine("prison_break_player_ask", "prison_break_end_already_met", "prison_break_next_move", "{=qFoMsPIf}I'm glad we made it out safe. What will you do now?", null, null);
		campaignGameStarter.AddPlayerLine("prison_break_player_meet", "prison_break_end_meet", "prison_break_next_move", "{=nMn63bV1}I am {PLAYER.NAME}. All I ask is that you remember that name, and what I did.{newline}Tell me, what will you do now?", null, null);
		campaignGameStarter.AddDialogLine("prison_break_next_companion", "prison_break_next_move", "prison_break_next_move_player_companion", "{=aoJHP3Ud}I'm ready to rejoin you. I'm in your debt.", () => _prisonerHero.CompanionOf == Clan.PlayerClan, null);
		campaignGameStarter.AddDialogLine("prison_break_next_commander", "prison_break_next_move", "prison_break_next_move_player", "{=xADZi2bK}I'll go and find my men. I will remember your help...", () => _prisonerHero.IsCommander, null);
		campaignGameStarter.AddDialogLine("prison_break_next_noble", "prison_break_next_move", "prison_break_next_move_player", "{=W2vV5jzj}I'll go back to my family. I will remember your help...", () => _prisonerHero.IsLord, null);
		campaignGameStarter.AddDialogLine("prison_break_next_notable", "prison_break_next_move", "prison_break_next_move_player", "{=efdCZPw4}I'll go back to my work. I will remember your help...", () => _prisonerHero.IsNotable, null);
		campaignGameStarter.AddDialogLine("prison_break_next_other", "prison_break_next_move", "prison_break_next_move_player_other", "{=TWZ4abt5}I'll keep wandering about, as I've done before. I can make a living. No need to worry.", null, null);
		campaignGameStarter.AddPlayerLine("prison_break_end_dialog_3", "prison_break_next_move_player_companion", "close_window", "{=ncvB4XRL}You could join me.", null, prison_break_end_with_success_companion);
		campaignGameStarter.AddPlayerLine("prison_break_end_dialog_1", "prison_break_next_move_player", "close_window", "{=rlAec9CM}Very well. Keep safe.", null, prison_break_end_with_success_on_consequence);
		campaignGameStarter.AddPlayerLine("prison_break_end_dialog_2", "prison_break_next_move_player_other", "close_window", "{=dzXaXKaC}Very well.", null, prison_break_end_with_success_on_consequence);
	}

	[GameMenuInitializationHandler("start_prison_break")]
	[GameMenuInitializationHandler("prison_break_cool_down")]
	[GameMenuInitializationHandler("settlement_prison_break_success")]
	[GameMenuInitializationHandler("settlement_prison_break_fail_player_unconscious")]
	[GameMenuInitializationHandler("settlement_prison_break_fail_prisoner_unconscious")]
	public static void game_menu_prison_menu_on_init(MenuCallbackArgs args)
	{
		args.MenuContext.SetBackgroundMeshName(Settlement.CurrentSettlement.SettlementComponent.WaitMeshName);
	}

	private bool prison_break_end_with_success_clan_member()
	{
		int num;
		if (_prisonerHero != null && _prisonerHero.CharacterObject == CharacterObject.OneToOneConversationCharacter)
		{
			if (_prisonerHero.CompanionOf != Clan.PlayerClan)
			{
				num = ((_prisonerHero.Clan == Clan.PlayerClan) ? 1 : 0);
				if (num == 0)
				{
					goto IL_006b;
				}
			}
			else
			{
				num = 1;
			}
			MBTextManager.SetTextVariable("SALUTATION", Campaign.Current.ConversationManager.FindMatchingTextOrNull("str_salutation", CharacterObject.OneToOneConversationCharacter));
		}
		else
		{
			num = 0;
		}
		goto IL_006b;
		IL_006b:
		return (byte)num != 0;
	}

	private bool prison_break_end_with_success_player_already_met()
	{
		int num;
		if (_prisonerHero != null && _prisonerHero.CharacterObject == CharacterObject.OneToOneConversationCharacter)
		{
			num = (_prisonerHero.HasMet ? 1 : 0);
			if (num != 0)
			{
				MBTextManager.SetTextVariable("SALUTATION", Campaign.Current.ConversationManager.FindMatchingTextOrNull("str_salutation", CharacterObject.OneToOneConversationCharacter));
			}
		}
		else
		{
			num = 0;
		}
		return (byte)num != 0;
	}

	private bool prison_break_end_with_success_other_on_condition()
	{
		if (_prisonerHero != null)
		{
			return _prisonerHero.CharacterObject == CharacterObject.OneToOneConversationCharacter;
		}
		return false;
	}

	private void PrisonBreakEndedInternal()
	{
		ChangeRelationAction.ApplyPlayerRelation(_prisonerHero, Campaign.Current.Models.PrisonBreakModel.GetRelationRewardOnPrisonBreak(_prisonerHero));
		SkillLevelingManager.OnPrisonBreakEnd(_prisonerHero, isSucceeded: true);
	}

	private void prison_break_end_with_success_on_consequence()
	{
		PrisonBreakEndedInternal();
		EndCaptivityAction.ApplyByEscape(_prisonerHero, Hero.MainHero);
		_prisonerHero = null;
	}

	private void prison_break_end_with_success_companion()
	{
		PrisonBreakEndedInternal();
		EndCaptivityAction.ApplyByEscape(_prisonerHero, Hero.MainHero);
		_prisonerHero.ChangeState(Hero.CharacterStates.Active);
		AddHeroToPartyAction.Apply(_prisonerHero, MobileParty.MainParty);
		_prisonerHero = null;
	}

	private bool game_menu_castle_prison_break_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Mission;
		_bribeCost = Campaign.Current.Models.PrisonBreakModel.GetPrisonBreakStartCost(_prisonerHero);
		MBTextManager.SetTextVariable("COST", _bribeCost);
		return true;
	}

	private void AddCoolDownForPrisonBreak(Settlement settlement)
	{
		CampaignTime value = CampaignTime.DaysFromNow(7f);
		if (_coolDownData.ContainsKey(settlement))
		{
			_coolDownData[settlement] = value;
		}
		else
		{
			_coolDownData.Add(settlement, value);
		}
	}

	private bool CanPlayerStartPrisonBreak(Settlement settlement)
	{
		bool flag = true;
		if (_coolDownData.TryGetValue(settlement, out var value))
		{
			flag = value.IsPast;
			if (flag)
			{
				_coolDownData.Remove(settlement);
			}
		}
		return flag;
	}

	private bool game_menu_stage_prison_break_on_condition(MenuCallbackArgs args)
	{
		bool result = false;
		if (Campaign.Current.Models.PrisonBreakModel.CanPlayerStagePrisonBreak(Settlement.CurrentSettlement))
		{
			args.optionLeaveType = GameMenuOption.LeaveType.StagePrisonBreak;
			if (Hero.MainHero.IsWounded)
			{
				args.IsEnabled = false;
				args.Tooltip = new TextObject("{=yNMrF2QF}You are wounded");
			}
			result = true;
		}
		return result;
	}

	private void game_menu_castle_prison_break_from_dungeon_on_consequence(MenuCallbackArgs args)
	{
		_previousMenuId = "town_keep_dungeon";
		game_menu_castle_prison_break_on_consequence(args);
	}

	private void game_menu_castle_prison_break_from_castle_dungeon_on_consequence(MenuCallbackArgs args)
	{
		_previousMenuId = "castle_dungeon";
		game_menu_castle_prison_break_on_consequence(args);
	}

	private void game_menu_castle_prison_break_from_enemy_keep_on_consequence(MenuCallbackArgs args)
	{
		_previousMenuId = "town_enemy_town_keep";
		game_menu_castle_prison_break_on_consequence(args);
	}

	private void game_menu_castle_prison_break_on_consequence(MenuCallbackArgs args)
	{
		if (CanPlayerStartPrisonBreak(Settlement.CurrentSettlement))
		{
			FlattenedTroopRoster flattenedTroopRoster = Settlement.CurrentSettlement.Party.PrisonRoster.ToFlattenedRoster();
			if (Settlement.CurrentSettlement.Town.GarrisonParty != null)
			{
				flattenedTroopRoster.Add(Settlement.CurrentSettlement.Town.GarrisonParty.PrisonRoster.GetTroopRoster());
			}
			flattenedTroopRoster.RemoveIf((FlattenedTroopRosterElement x) => !x.Troop.IsHero);
			List<InquiryElement> list = new List<InquiryElement>();
			foreach (FlattenedTroopRosterElement item in flattenedTroopRoster)
			{
				TextObject textObject = null;
				bool flag = false;
				TextObject textObject2;
				if (FactionManager.IsAtWarAgainstFaction(Clan.PlayerClan.MapFaction, item.Troop.HeroObject.MapFaction))
				{
					textObject2 = new TextObject("{=!}{HERO.NAME}");
					StringHelpers.SetCharacterProperties("HERO", item.Troop, textObject2);
					textObject = new TextObject("{=VM1SGrla}{HERO.NAME} is your enemy.");
					textObject.SetCharacterProperties("HERO", item.Troop);
					flag = true;
				}
				else
				{
					int prisonBreakStartCost = Campaign.Current.Models.PrisonBreakModel.GetPrisonBreakStartCost(item.Troop.HeroObject);
					flag = Hero.MainHero.Gold < prisonBreakStartCost;
					textObject2 = new TextObject("{=!}{HERO.NAME}");
					StringHelpers.SetCharacterProperties("HERO", item.Troop, textObject2);
					textObject = new TextObject("{=I4SjNT6Y}This will cost you {BRIBE_COST}{GOLD_ICON}.{?ENOUGH_GOLD}{?} You don't have enough money.{\\?}");
					textObject.SetTextVariable("BRIBE_COST", prisonBreakStartCost);
					textObject.SetTextVariable("ENOUGH_GOLD", (!flag) ? 1 : 0);
				}
				list.Add(new InquiryElement(item.Troop, textObject2.ToString(), new CharacterImageIdentifier(CharacterCode.CreateFrom(item.Troop)), !flag, textObject.ToString()));
			}
			MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(new TextObject("{=oQjsShmH}PRISONERS").ToString(), new TextObject("{=abpzOR0D}Choose a prisoner to break out").ToString(), list, isExitShown: true, 1, 1, GameTexts.FindText("str_done").ToString(), string.Empty, StartPrisonBreak, null));
		}
		else
		{
			GameMenu.SwitchToMenu("prison_break_cool_down");
		}
	}

	private void StartPrisonBreak(List<InquiryElement> prisonerList)
	{
		if (prisonerList.Count > 0)
		{
			_prisonerHero = ((CharacterObject)prisonerList[0].Identifier).HeroObject;
			GameMenu.SwitchToMenu("start_prison_break");
		}
		else
		{
			_prisonerHero = null;
		}
	}

	private void OpenPrisonBreakMission()
	{
		GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, _bribeCost);
		AddCoolDownForPrisonBreak(Settlement.CurrentSettlement);
		_launchingPrisonBreakMission = true;
		Location locationWithId = LocationComplex.Current.GetLocationWithId("prison");
		CampaignMission.OpenPrisonBreakMission(locationWithId.GetSceneName(Settlement.CurrentSettlement.Town.GetWallLevel()), locationWithId, _prisonerHero.CharacterObject);
	}

	private bool game_menu_leave_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Leave;
		return true;
	}

	private bool game_menu_continue_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Continue;
		return true;
	}

	private void game_menu_cancel_prison_break(MenuCallbackArgs args)
	{
		_prisonerHero = null;
		GameMenu.SwitchToMenu(_previousMenuId);
	}

	private void start_prison_break_on_init(MenuCallbackArgs args)
	{
		StringHelpers.SetCharacterProperties("PRISONER", _prisonerHero.CharacterObject);
	}

	private void settlement_prison_break_success_on_init(MenuCallbackArgs args)
	{
		StringHelpers.SetCharacterProperties("PRISONER", _prisonerHero.CharacterObject);
		MBTextManager.SetTextVariable("SETTLEMENT_TYPE", Settlement.CurrentSettlement.IsTown ? 1 : 0);
	}

	private void settlement_prison_break_success_continue_on_consequence(MenuCallbackArgs args)
	{
		PlayerEncounter.LeaveSettlement();
		PlayerEncounter.Finish();
		CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter), new ConversationCharacterData(_prisonerHero.CharacterObject));
	}

	private void settlement_prison_break_fail_prisoner_injured_on_init(MenuCallbackArgs args)
	{
		if (_prisonerHero.IsDead)
		{
			TextObject textObject = new TextObject("{=GkwOyJn9}{newline}You later learn that {?PRISONER.GENDER}she{?}he{\\?} died from {?PRISONER.GENDER}her{?}his{\\?} injuries.");
			StringHelpers.SetCharacterProperties("PRISONER", _prisonerHero.CharacterObject, textObject);
			MBTextManager.SetTextVariable("INFORMATION_IF_PRISONER_DEAD", textObject);
		}
		StringHelpers.SetCharacterProperties("PRISONER", _prisonerHero.CharacterObject);
	}

	private void settlement_prison_break_fail_on_init(MenuCallbackArgs args)
	{
		StringHelpers.SetCharacterProperties("PRISONER", _prisonerHero.CharacterObject);
	}

	private void settlement_prison_break_fail_player_unconscious_continue_on_consequence(MenuCallbackArgs args)
	{
		SkillLevelingManager.OnPrisonBreakEnd(_prisonerHero, isSucceeded: false);
		Settlement currentSettlement = Settlement.CurrentSettlement;
		PlayerEncounter.LeaveSettlement();
		PlayerEncounter.Finish();
		TakePrisonerAction.Apply(currentSettlement.Party, Hero.MainHero);
		_prisonerHero = null;
	}

	private void settlement_prison_break_fail_prisoner_unconscious_continue_on_consequence(MenuCallbackArgs args)
	{
		SkillLevelingManager.OnPrisonBreakEnd(_prisonerHero, isSucceeded: false);
		_prisonerHero = null;
		PlayerEncounter.LeaveSettlement();
		PlayerEncounter.Finish();
	}
}
