using NavalDLC.Missions;
using NavalDLC.Storyline.Quests;
using StoryMode;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace NavalDLC.Storyline;

public class NavalStorylineFirstActCampaignBehavior : CampaignBehaviorBase
{
	private enum PortFightState
	{
		None,
		FightMissionStarted,
		FightMissionWon,
		FightShouldContinue,
		ReadyToBeFinalized
	}

	public class NavalStorylineFirstActCampaignBehaviorTypeDefiner : SaveableTypeDefiner
	{
		public NavalStorylineFirstActCampaignBehaviorTypeDefiner()
			: base(370000)
		{
		}

		protected override void DefineEnumTypes()
		{
			AddEnumDefinition(typeof(PortFightState), 1);
		}
	}

	private const string PortFightEnemyTroopStringId = "gangster_3";

	private PortFightState _portFightState;

	private bool _initialPortFightSuccessDialogPlayerOption1Selected;

	private bool _initialPortFightSuccessDialogPlayerOption2Selected;

	private bool _initialPortFightSuccessDialogPlayerOption4Selected;

	public override void RegisterEvents()
	{
		if (!NavalStorylineData.IsNavalStorylineCanceled())
		{
			CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
			CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, OnGameMenuOpened);
			CampaignEvents.OnAfterSessionLaunchedEvent.AddNonSerializedListener(this, OnAfterSessionLaunched);
			CampaignEvents.OnMissionEndedEvent.AddNonSerializedListener(this, OnMissionEnded);
			NavalDLCEvents.OnNavalStorylineCanceledEvent.AddNonSerializedListener(this, OnNavalStorylineCanceled);
		}
	}

	private void OnNavalStorylineCanceled(NavalStorylineData.StorylineCancelDetail detail)
	{
		CampaignEventDispatcher.Instance.RemoveListeners(this);
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_portFightState", ref _portFightState);
	}

	private void OnNewGameCreated(CampaignGameStarter campaignGameStarter)
	{
		if (StoryModeManager.Current == null)
		{
			_portFightState = PortFightState.ReadyToBeFinalized;
		}
	}

	private void OnGameMenuOpened(MenuCallbackArgs args)
	{
		if (_portFightState != PortFightState.ReadyToBeFinalized && args.MenuContext.GameMenu.StringId == "port_menu" && Settlement.CurrentSettlement == NavalStorylineData.HomeSettlement && Campaign.Current.QuestManager.IsThereActiveQuestWithType(typeof(InquireAtOstican)))
		{
			if (_portFightState == PortFightState.FightMissionWon)
			{
				GameMenu.ActivateGameMenu("naval_storyline_after_port_fight");
			}
			else
			{
				GameMenu.ActivateGameMenu("naval_storyline_port_fight");
			}
		}
	}

	private void OnAfterSessionLaunched(CampaignGameStarter campaignGameStarter)
	{
		AddGameMenus(campaignGameStarter);
		AddPortFightOnSuccessDialogFlow(campaignGameStarter);
	}

	private void OnMissionEnded(IMission mission)
	{
		if (_portFightState == PortFightState.FightMissionStarted)
		{
			MissionResult missionResult = (mission as Mission).MissionResult;
			if (missionResult != null && missionResult.PlayerVictory)
			{
				_portFightState = PortFightState.FightMissionWon;
			}
			else
			{
				_portFightState = PortFightState.FightShouldContinue;
			}
		}
	}

	private void AddGameMenus(CampaignGameStarter campaignGameStarter)
	{
		campaignGameStarter.AddGameMenu("naval_storyline_port_fight", "{=GhTjvwpl}You're strolling through {SETTLEMENT}{.o} streets when you hear raised voices coming from a side alley. You turn to look, and see three rough-looking men accosting an older man in a cloak. His gaze shifts quickly from one to the other and his body is tensed, as though he is going to spring into action. You sense a fight is about to start.", port_fight_on_init);
		campaignGameStarter.AddGameMenuOption("naval_storyline_port_fight", "continue", "{=DM6luo3c}Continue", port_fight_condition, port_fight_consequence);
		campaignGameStarter.AddGameMenu("naval_storyline_after_port_fight", "{=!}{AFTER_PORT_FIGHT_MENU_TEXT}", after_port_fight_on_init);
		campaignGameStarter.AddGameMenuOption("naval_storyline_after_port_fight", "continue_to_dialog", "{=DM6luo3c}Continue", naval_storyline_after_port_fight_continue_to_dialog_on_condition, naval_storyline_after_port_fight_continue_to_dialog_on_consequence);
		campaignGameStarter.AddGameMenuOption("naval_storyline_after_port_fight", "return_to_fight", "{=inC6Ia5s}Return to the fight", naval_storyline_after_port_fight_return_to_fight_on_condition, naval_storyline_after_port_fight_return_to_fight_on_consequence);
		campaignGameStarter.AddGameMenuOption("naval_storyline_after_port_fight", "escape", "{=qqjRkMy9}Make good your escape", naval_storyline_after_port_fight_escape_on_condition, naval_storyline_after_port_fight_escape_on_consequence, isLeave: true);
	}

	[GameMenuInitializationHandler("naval_storyline_port_fight")]
	[GameMenuInitializationHandler("naval_storyline_after_port_fight")]
	public static void port_menu_on_init(MenuCallbackArgs args)
	{
		string backgroundMeshName = Settlement.CurrentSettlement.Culture.StringId + "_port";
		args.MenuContext.SetBackgroundMeshName(backgroundMeshName);
		args.MenuContext.SetAmbientSound("event:/map/ambient/node/settlements/2d/port");
	}

	private void port_fight_on_init(MenuCallbackArgs args)
	{
		NavalStorylineData.OnCheckpointReached(NavalStorylineData.NavalStorylineCheckpoint.Act1PortMenu);
		MBTextManager.SetTextVariable("SETTLEMENT", NavalStorylineData.HomeSettlement.EncyclopediaLinkWithName);
	}

	private bool port_fight_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Mission;
		return true;
	}

	private void port_fight_consequence(MenuCallbackArgs args)
	{
		TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
		troopRoster.AddToCounts(CharacterObject.PlayerCharacter, 1, insertAtFront: true);
		troopRoster.AddToCounts(NavalStorylineData.Gunnar.CharacterObject, 1);
		TroopRoster troopRoster2 = TroopRoster.CreateDummyTroopRoster();
		CharacterObject character = MBObjectManager.Instance.GetObject<CharacterObject>("gangster_3");
		troopRoster2.AddToCounts(character, 3);
		int wallLevel = Settlement.CurrentSettlement.Town.GetWallLevel();
		Settlement.CurrentSettlement.LocationComplex.GetScene("center", wallLevel);
		LocationComplex.Current.GetLocationWithId("center");
		GameMenu.ActivateGameMenu("naval_storyline_after_port_fight");
		_portFightState = PortFightState.FightMissionStarted;
		MissionInitializerRecord navalMissionInitializerTemplate = NavalStorylineData.GetNavalMissionInitializerTemplate("storyline_shipyard_alley");
		TerrainType faceTerrainType = Campaign.Current.MapSceneWrapper.GetFaceTerrainType(MobileParty.MainParty.CurrentNavigationFace);
		navalMissionInitializerTemplate.TerrainType = (int)faceTerrainType;
		navalMissionInitializerTemplate.NeedsRandomTerrain = false;
		navalMissionInitializerTemplate.PlayingInCampaignMode = true;
		navalMissionInitializerTemplate.RandomTerrainSeed = MBRandom.RandomInt(10000);
		navalMissionInitializerTemplate.AtmosphereOnCampaign = Campaign.Current.Models.MapWeatherModel.GetAtmosphereModel(MobileParty.MainParty.Position);
		navalMissionInitializerTemplate.SceneHasMapPatch = false;
		navalMissionInitializerTemplate.AtmosphereOnCampaign.NauticalInfo.UsesNavalSimulatedWater = 1;
		NavalMissions.OpenNavalStorylineAlleyFightMission(navalMissionInitializerTemplate);
	}

	private void after_port_fight_on_init(MenuCallbackArgs args)
	{
		if (_portFightState == PortFightState.None)
		{
			return;
		}
		if (_portFightState == PortFightState.FightMissionWon)
		{
			if (NavalStorylineData.Gunnar.IsWounded)
			{
				MBTextManager.SetTextVariable("AFTER_PORT_FIGHT_MENU_TEXT", new TextObject("{=3V80vvSz}You make quick work of the alley thugs, and help their victim to his feet. He seems dazed, but grateful."));
			}
			else if (Hero.MainHero.IsWounded)
			{
				MBTextManager.SetTextVariable("AFTER_PORT_FIGHT_MENU_TEXT", new TextObject("{=5NoZgdqr}The alley thugs are too many for you, and knock you to the ground. Before they can finish you off, however, you hear a rush of feet and cries of alarm. The town watch must have heard the commotion, and your assailants make a quick retreat. The watch helps you to your feet and tells you to be more careful. The thugs' victim, dazed but apparently unhurt, introduces himself."));
			}
			else
			{
				OnFightMissionFinalized();
			}
		}
		else if (_portFightState == PortFightState.FightShouldContinue)
		{
			MBTextManager.SetTextVariable("AFTER_PORT_FIGHT_MENU_TEXT", new TextObject("{=7C4JYwZp}You back out of the alley. You could easily escape, but you sense that the thugs will kill the old man."));
		}
		else
		{
			OnFightMissionFinalized();
		}
	}

	private void OnFightMissionFinalized()
	{
		_portFightState = PortFightState.ReadyToBeFinalized;
		NavalStorylineData.OnCheckpointReached(NavalStorylineData.NavalStorylineCheckpoint.Act1PortFightSucceeded);
		GameMenu.SwitchToMenu("town");
	}

	private void OpenConversationWithGunnar()
	{
		SpawnPortQuestGiver();
		PlayerEncounter.LocationEncounter.CreateAndOpenMissionController(LocationComplex.Current.GetLocationOfCharacter(NavalStorylineData.Gunnar), null, NavalStorylineData.Gunnar.CharacterObject);
	}

	private bool naval_storyline_after_port_fight_continue_to_dialog_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.HostileAction;
		return _portFightState == PortFightState.FightMissionWon;
	}

	private void naval_storyline_after_port_fight_continue_to_dialog_on_consequence(MenuCallbackArgs args)
	{
		OpenConversationWithGunnar();
	}

	private bool naval_storyline_after_port_fight_return_to_fight_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.HostileAction;
		return _portFightState == PortFightState.FightShouldContinue;
	}

	private void naval_storyline_after_port_fight_return_to_fight_on_consequence(MenuCallbackArgs args)
	{
		port_fight_consequence(args);
	}

	private bool naval_storyline_after_port_fight_escape_on_condition(MenuCallbackArgs args)
	{
		args.Tooltip = new TextObject("{=SpZEO1Rx}This option will abandon the storyline.");
		args.optionLeaveType = GameMenuOption.LeaveType.Leave;
		return _portFightState == PortFightState.FightShouldContinue;
	}

	private void naval_storyline_after_port_fight_escape_on_consequence(MenuCallbackArgs args)
	{
		_portFightState = PortFightState.ReadyToBeFinalized;
		NavalDLCEvents.Instance.OnNavalStorylineCanceled(NavalStorylineData.StorylineCancelDetail.ByDialogue);
		GameMenu.SwitchToMenu("town_outside");
	}

	private void AddPortFightOnSuccessDialogFlow(CampaignGameStarter campaignGameStarter)
	{
		campaignGameStarter.AddDialogLine("initial_port_fight_success_dialog_start", "start", "gunnar_introduction_1", "{=!}{START_LINE}", initial_port_fight_success_dialog_start_on_condition, null, 50000);
		campaignGameStarter.AddDialogLine("gunnar_introduction_1_line", "gunnar_introduction_1", "gunnar_introduction_2", "{=rbpBs3bZ}[ib:confident2]I am Gunnar of Lagshofn, from the Nordvyg lands.", null, null);
		campaignGameStarter.AddDialogLine("gunnar_introduction_2_line", "gunnar_introduction_2", "gunnar_introduction_3", "{=8kUr3LUi}[ib:normal]I've come to this port seeking warriors and a ship. These men we fought were allies of a pirate gang who call themselves the Sea Hounds. They have been raiding and slaving along the Nordvyg’s shores, and I intend to go to war with them.", null, null);
		campaignGameStarter.AddDialogLine("gunnar_introduction_3_line", "gunnar_introduction_3", "initial_port_fight_success_dialog_player_options", "{=enXch5l7}[if:convo_stern][ib:hip2]The Sea Hounds and I have history, and nowadays they hate my guts as fiercely as I hate theirs. Somebody must have sent word of my whereabouts to their local friends as these lowlifes had a mind to do me in. Again, you have my thanks for evening the odds.", null, null);
		campaignGameStarter.AddPlayerLine("initial_port_fight_success_dialog_player_options_1", "initial_port_fight_success_dialog_player_options", "initial_port_fight_success_dialog_player_options_1_answer", "{=Z39CjlP7}Did you say slave raids? My brother and sister were taken in one.", initial_port_fight_success_dialog_player_options_1_condition, initial_port_fight_success_dialog_player_options_1_on_consequence, 100, initial_port_fight_success_dialog_player_options_1_clickable_condition);
		campaignGameStarter.AddPlayerLine("initial_port_fight_success_dialog_player_options_2", "initial_port_fight_success_dialog_player_options", "initial_port_fight_success_dialog_player_options_2_answer", "{=tIxXxFQU}Who are these Sea Hounds?", initial_port_fight_success_dialog_player_options_2_condition, initial_port_fight_success_dialog_player_options_2_on_consequence, 100, initial_port_fight_success_dialog_player_options_2_clickable_condition);
		campaignGameStarter.AddPlayerLine("initial_port_fight_success_dialog_player_options_3", "initial_port_fight_success_dialog_player_options", "initial_port_fight_success_dialog_player_options_3_answer", "{=XP7g0Kiq}Why do you risk so much to hunt them?", initial_port_fight_success_dialog_player_options_3_condition, initial_port_fight_success_dialog_player_options_3_on_consequence, 100, initial_port_fight_success_dialog_player_options_3_clickable_condition);
		campaignGameStarter.AddPlayerLine("initial_port_fight_success_dialog_player_options_4", "initial_port_fight_success_dialog_player_options", "initial_port_fight_success_dialog_player_options_4_answer", "{=ac5oq0pt}What are you doing now?", initial_port_fight_success_dialog_continue_condition, null);
		campaignGameStarter.AddDialogLine("initial_port_fight_success_dialog_player_options_1_answer_line", "initial_port_fight_success_dialog_player_options_1_answer", "initial_port_fight_success_dialog_player_options", "{=zTr3dBd7}[if:convo_focused_voice][ib:normal]I know what it's like to lose family to slavers. If you're still searching, look to the Sea Hounds. They've got their hands in most of the slaving that happens along these coasts.", null, null);
		campaignGameStarter.AddDialogLine("initial_port_fight_success_dialog_player_options_2_answer_line", "initial_port_fight_success_dialog_player_options_2_answer", "initial_port_fight_success_dialog_player_options", "{=Vs5cNhfI}[if:convo_empathic_voice]It’s hard to believe now, but they were once my brothers-in-arms. Years ago we fought side-by-side in the last great rebellion in the north. Most of the clans and many freemen like myself refused to bow to Volbjorn the usurper, as he was then called. But Volbjorn knew how to speak to men’s desires. He won over the bigger clans with promises of land and silver, and when summer came and he brought a fleet to give us battle, he had with him so many long ships that their sails covered the horizon. We still fought them of course, but their numbers were too many to beat.", null, null);
		campaignGameStarter.AddDialogLine("initial_port_fight_success_dialog_player_options_3_answer_line", "initial_port_fight_success_dialog_player_options_3_answer", "initial_port_fight_success_dialog_player_options", "{=lIpAlkH2}[if:convo_stern][ib:closed]They dishonor what we fought for. I'm no stranger to battle - I'll kill when I must. But they murder for pleasure, thinking the All-Father rewards bloodthirst. He wants warriors, not hounds.", null, null);
		campaignGameStarter.AddDialogLine("initial_port_fight_success_dialog_player_options_4_answer_line", "initial_port_fight_success_dialog_player_options_4_answer", "next_move_explanation_1", "{=RQ0qIqGH}I mean to gather up with some of my kin and friends to go against the Sea Hounds. Just a few days ago, I ran into an old comrade of mine here in Ostican. He is called Purig and he happens to own a fast ship. He promised to help me capture a Sea Hound ship and put together a crew.", null, null);
		campaignGameStarter.AddDialogLine("next_move_explanation_1_line", "next_move_explanation_1", "next_move_explanation_player_options", "{=okfrRTb4}[ib:confident][if:convo_normal]So, I'm going to make you a proposal. Perhaps you'd like to come with us? I can't guarantee we'll find your kin, but I can promise a good fight and, if we win, a bit of fine loot. And, well, if you'd ever had an interest in learning how to handle a ship, you won't find any better school than these northern seas.", null, null);
		campaignGameStarter.AddPlayerLine("next_move_explanation_player_option_1_line", "next_move_explanation_player_options", "player_joins_gunnar_answer", "{=9buEaTHt}I will join you, and we can hunt together.", null, null);
		campaignGameStarter.AddPlayerLine("next_move_explanation_player_option_2_line", "next_move_explanation_player_options", "player_waits_answer", "{=qFFYyNeR}Let me think this over.", null, null);
		campaignGameStarter.AddPlayerLine("next_move_explanation_player_option_3_line", "next_move_explanation_player_options", "player_skips_tutorial", "{=JAuDUFkG}I have other obligations, and I already know how to handle a ship. (Skip tutorial)", null, null);
		campaignGameStarter.AddDialogLine("player_joins_gunnar_answer_line", "player_joins_gunnar_answer", "close_window", "{=nu5vuTvX}[if:convo_calm_friendly]You can find Purig in the tavern and introduce yourself. I should go get myself cleaned up and get ready to travel.", null, delegate
		{
			Campaign.Current.ConversationManager.ConversationEndOneShot += OnQuestGiverSaved;
		});
		campaignGameStarter.AddDialogLine("player_waits_answer_line", "player_waits_answer", "close_window", "{=nyQhfz0B}The decision is of course yours. I expect you can find Purig in the tavern for the next few days, if you change your mind.", null, delegate
		{
			Campaign.Current.ConversationManager.ConversationEndOneShot += OnQuestGiverSaved;
		});
		campaignGameStarter.AddDialogLine("player_skips_tutorial_line", "player_skips_tutorial", "skip_naval_tutorial_confirmation", "{=2biaAIpM}[if:convo_empathic_voice]Very well. I hope you find your kin some day. Listen, whatever I manage to do near Hvalvik, I will return here and try to find other warriors to help me. If you ever reconsider, look for me here in Ostican.", null, null);
		campaignGameStarter.AddPlayerLine("skip_tutorial_confirmation_option_1_line", "skip_naval_tutorial_confirmation", "player_joins_gunnar_answer", "{=58CsRmug}Wait, I changed my mind.", null, null);
		campaignGameStarter.AddPlayerLine("skip_tutorial_confirmation_option_2_line", "skip_naval_tutorial_confirmation", "close_window", "{=1zleX968}Farewell to you too, and good luck.", null, delegate
		{
			Campaign.Current.ConversationManager.ConversationEndOneShot += OnNavalTutorialSkipped;
		});
	}

	private bool initial_port_fight_success_dialog_start_on_condition()
	{
		int num;
		if (Hero.OneToOneConversationHero == NavalStorylineData.Gunnar)
		{
			num = ((!NavalStorylineData.Gunnar.HasMet) ? 1 : 0);
			if (num != 0)
			{
				TextObject textObject = (Hero.MainHero.IsWounded ? new TextObject("{=h46iGLj0}[ib:hip]Are you all right? One on three aren't the worst odds I've faced, but even so, that could have gone either way. I owe you my thanks.") : new TextObject("{=CvcV0DWt}[ib:hip][if:convo_empathic_voice]By my blood… Damn, that hurts. I think I'm all right, though. Thank you."));
				textObject.SetCharacterProperties("QUEST_GIVER", NavalStorylineData.Gunnar.CharacterObject);
				textObject.SetCharacterProperties("PLAYER", Hero.MainHero.CharacterObject);
				MBTextManager.SetTextVariable("START_LINE", textObject);
			}
		}
		else
		{
			num = 0;
		}
		return (byte)num != 0;
	}

	private bool initial_port_fight_success_dialog_player_options_1_condition()
	{
		return true;
	}

	private bool initial_port_fight_success_dialog_player_options_1_clickable_condition(out TextObject explanation)
	{
		explanation = TextObject.GetEmpty();
		return !_initialPortFightSuccessDialogPlayerOption1Selected;
	}

	private void initial_port_fight_success_dialog_player_options_1_on_consequence()
	{
		_initialPortFightSuccessDialogPlayerOption1Selected = true;
	}

	private bool initial_port_fight_success_dialog_player_options_2_condition()
	{
		return true;
	}

	private bool initial_port_fight_success_dialog_player_options_2_clickable_condition(out TextObject explanation)
	{
		explanation = TextObject.GetEmpty();
		return !_initialPortFightSuccessDialogPlayerOption2Selected;
	}

	private void initial_port_fight_success_dialog_player_options_2_on_consequence()
	{
		_initialPortFightSuccessDialogPlayerOption2Selected = true;
	}

	private bool initial_port_fight_success_dialog_player_options_3_condition()
	{
		return _initialPortFightSuccessDialogPlayerOption2Selected;
	}

	private bool initial_port_fight_success_dialog_player_options_3_clickable_condition(out TextObject explanation)
	{
		explanation = TextObject.GetEmpty();
		return !_initialPortFightSuccessDialogPlayerOption4Selected;
	}

	private void initial_port_fight_success_dialog_player_options_3_on_consequence()
	{
		_initialPortFightSuccessDialogPlayerOption4Selected = true;
	}

	private bool initial_port_fight_success_dialog_continue_condition()
	{
		if (_initialPortFightSuccessDialogPlayerOption1Selected && _initialPortFightSuccessDialogPlayerOption2Selected)
		{
			return _initialPortFightSuccessDialogPlayerOption4Selected;
		}
		return false;
	}

	private void SpawnPortQuestGiver()
	{
		Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(NavalStorylineData.Gunnar.CharacterObject.Race, "_settlement");
		LocationCharacter locationCharacter = new LocationCharacter(new AgentData(new SimpleAgentOrigin(NavalStorylineData.Gunnar.CharacterObject)).Monster(monsterWithSuffix), SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors, "npc_common", fixedLocation: true, LocationCharacter.CharacterRelations.Neutral, null, useCivilianEquipment: true);
		LocationComplex.Current.GetLocationWithId("center").AddCharacter(locationCharacter);
	}

	private void OnQuestGiverSaved()
	{
		Mission.Current.GetMissionBehavior<NavalStorylineAlleyFightMissionController>().OnConversationEnded();
		LocationComplex.Current.RemoveCharacterIfExists(NavalStorylineData.Gunnar);
		NavalDLCEvents.Instance.OnGunnarSaved();
		NavalStorylineData.Gunnar.SetHasMet();
		OnFightMissionFinalized();
	}

	private void OnNavalTutorialSkipped()
	{
		Mission.Current?.GetMissionBehavior<NavalStorylineAlleyFightMissionController>()?.OnConversationEnded();
		NavalStorylineData.Gunnar.SetHasMet();
		OnFightMissionFinalized();
		NavalDLCEvents.Instance.OnNavalStorylineTutorialSkipped();
		Settlement currentSettlement = Settlement.CurrentSettlement;
		if (currentSettlement != null && currentSettlement == NavalStorylineData.HomeSettlement && currentSettlement.HasPort && currentSettlement.LocationComplex.GetLocationOfCharacter(NavalStorylineData.Gunnar) == null)
		{
			Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(NavalStorylineData.Gunnar.CharacterObject.Race, "_settlement");
			LocationCharacter locationCharacter = new LocationCharacter(new AgentData(new SimpleAgentOrigin(NavalStorylineData.Gunnar.CharacterObject)).Monster(monsterWithSuffix), SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors, "npc_common", fixedLocation: true, LocationCharacter.CharacterRelations.Neutral, null, useCivilianEquipment: true);
			LocationComplex.Current.GetLocationWithId("port").AddCharacter(locationCharacter);
		}
	}
}
