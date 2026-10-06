using System.Collections.Generic;
using System.Linq;
using Helpers;
using NavalDLC.Missions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.SaveSystem;

namespace NavalDLC.Storyline.Quests;

public class DefeatThePiratesQuest : NavalStorylineQuestBase
{
	private const string EncounterMenuId = "quest3_encounter_menu";

	private const string RetryMenuId = "quest3_retry_menu";

	private const string PiratePartyTemplateStringId = "storyline_act_2_sea_hounds_template";

	private const string PirateConversationCharacterId = "sea_hounds";

	public const string PlayerPartySailPatternId = "generated_square__h4_09";

	public const string PiratePartySailPatternId = "generated_square_l1_h4_10";

	private static readonly Dictionary<string, string> PlayerShipUpgradePieces = new Dictionary<string, string> { { "sail", "sails_lvl2" } };

	private static readonly Dictionary<string, string> PirateShipUpgradePieces = new Dictionary<string, string> { { "sail", "sails_lvl2" } };

	[SaveableField(1)]
	private MobileParty _pirateParty;

	[SaveableField(2)]
	private bool _battleWon;

	[SaveableField(3)]
	private bool _battleFinished;

	private PartyTemplateObject _pirateTemplate;

	public override NavalStorylineData.NavalStorylineStage Stage => NavalStorylineData.NavalStorylineStage.Act2;

	public override bool WillProgressStoryline => true;

	protected override string MainPartyTemplateStringId => "storyline_act_2_main_party_template";

	public int PirateTroopCount => _pirateTemplate.Stacks.Sum((PartyTemplateStack t) => t.MaxValue);

	public override TextObject Title => new TextObject("{=wKBtraSp}Defeat the Sea Hounds");

	private TextObject _descriptionLogText => new TextObject("{=VWK3jIqG}Defeat the two Sea Hound vessels that are lying in wait outside of Ostican.");

	public DefeatThePiratesQuest(string questId, Hero questGiver)
		: base(questId, questGiver, CampaignTime.Never, 0)
	{
		_pirateTemplate = Campaign.Current.ObjectManager.GetObject<PartyTemplateObject>("storyline_act_2_sea_hounds_template");
		AddLog(_descriptionLogText);
	}

	protected override void SetDialogs()
	{
		Campaign.Current.ConversationManager.AddDialogFlow(DialogFlow.CreateDialogFlow("start", 1200).NpcLine("{=NW5vE1xa}That's one Sea Hound defeated, but the other can't be too far away. We've captured a second ship, though. It's a snekkja - it should be quick and nimble. How about you cross over and take the helm? I'll keep command of our old knarr.").Condition(delegate
		{
			PirateBattleMissionController pirateBattleMissionController = Mission.Current?.GetMissionBehavior<PirateBattleMissionController>();
			return Hero.OneToOneConversationHero == NavalStorylineData.Gunnar && pirateBattleMissionController != null && pirateBattleMissionController.IsFirstShipCleared;
		})
			.BeginPlayerOptions()
			.PlayerOption("{=alDwmQtB}I'll go do that.")
			.Consequence(delegate
			{
				PirateBattleMissionController missionBehavior = Mission.Current.GetMissionBehavior<PirateBattleMissionController>();
				Campaign.Current.ConversationManager.ConversationEndOneShot += missionBehavior.OnPlayerSelectedSecondShipToCommand;
			})
			.NpcLine("{=qauwgx3r}[if:convo_huge_smile][ib:hip]Splendid. Let's go chase down that second Sea Hound.")
			.CloseDialog()
			.PlayerOption("{=cnjTiMmv}Very good. I'll keep command of our old knarr. You captain this agile snekkja.")
			.Consequence(delegate
			{
				PirateBattleMissionController missionBehavior = Mission.Current.GetMissionBehavior<PirateBattleMissionController>();
				Campaign.Current.ConversationManager.ConversationEndOneShot += missionBehavior.OnPlayerSelectedFirstShipToCommand;
			})
			.NpcLine("{=qauwgx3r}[if:convo_huge_smile][ib:hip]Splendid. Let's go chase down that second Sea Hound.")
			.CloseDialog()
			.EndPlayerOptions()
			.CloseDialog(), this);
		string token = "";
		Campaign.Current.ConversationManager.AddDialogFlow(DialogFlow.CreateDialogFlow("start", 1200).NpcLine("{=dF7jeK5a}[ib:weary][if:convo_beaten]I'm new at this, my {?PLAYER.GENDER}lady{?}lord{\\?}! I'm just a farmer who fell on hard times. I signed on with this ship in Varcheg a month ago. They told me we'd be trading grain and ivory across the Byalic. I didn't know we'd be attacking honest folk like yourselves!", IsPirate, IsMainHero).Condition(delegate
		{
			StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter);
			return CharacterObject.OneToOneConversationCharacter == ConversationHelper.GetConversationCharacterPartyLeader(_pirateParty?.Party);
		})
			.Consequence(delegate
			{
				AgentBuildData agentBuildData = new AgentBuildData(NavalStorylineData.Gunnar.CharacterObject);
				agentBuildData.TroopOrigin(new SimpleAgentOrigin(agentBuildData.AgentCharacter));
				agentBuildData.InitialPosition(Mission.Current.Scene.FindEntityWithName("free_infantry_spawn_point_0").GlobalPosition);
				agentBuildData.InitialDirection(Agent.Main.LookDirection.AsVec2.Normalized());
				if (Mission.Current != null)
				{
					Agent item = Mission.Current.SpawnAgent(agentBuildData);
					Campaign.Current.ConversationManager.AddConversationAgents(new List<IAgent> { item }, setActionsInstantly: true);
				}
			})
			.NpcLine("{=GsPj9ptT}[if:convo_nervous2]Listen - these Sea Hounds are trolls and demons, not men! I want no part of this any more! Spare me, and I promise I'll go back to my old life.", IsPirate, IsMainHero)
			.BeginPlayerOptions()
			.PlayerOption("{=LBoq4sXI}Tell me the truth, and I'll let you live.", IsPirate, null, token)
			.PlayerOption("{=wTEbf3gc}I am looking for my sister. Let me know how to find her, and we will spare your life.", IsPirate, null, token)
			.EndPlayerOptions()
			.GenerateToken(out token)
			.NpcLine("{=Q3bpobtL}[if:convo_nervous]We purchased some slaves from some bandits in Ostican. We were planning on selling them onward to another buyer further south along the coast. Perhaps your sister was one of them? Will you spare me?", IsPirate)
			.NpcLine("{=b1saAIdA}[ib:hip]Are you really a farmer, now? Callouses such as those on your hands are made by oars, not ploughs. And I see a scar on your sword-arm that doesn't look like it came from the kick of a mule. Indeed, I might even recall your name. Hralgar Eel-Nose, is it not?", IsGunnar, IsPirate)
			.NpcLine("{=tiHQafDb}[if:convo_predatory][ib:aggressive]Gunnar of Langshofn… Three of your old shipmates have we visited while reeving. One died well. The others… It's said that your people are mean and stingy hosts, but those two gave us some fine entertainment.", IsPirate, IsGunnar)
			.NpcLine("{=yhEKOBfT}[ib:warrior]As for you, friend of Gunnar... I told you where to seek your sister. Best rescue her quick, or she may take a liking to one of our brave lads and give you a litter of Sea Puppies. So there you have it… I fulfilled my end of the bargain. Put me ashore.", IsPirate, IsMainHero)
			.BeginPlayerOptions()
			.PlayerOption("{=00iNZpwG}You lied. The bargain is void. Gunnar, do what you will with him.")
			.Consequence(delegate
			{
				Campaign.Current.ConversationManager.ConversationEndOneShot += OnOption1Chosen;
			})
			.CloseDialog()
			.PlayerOption("{=RSBjrwHG}We will spare your life, but the sea may have other plans for you. Over the side you go.", IsPirate)
			.Consequence(delegate
			{
				Campaign.Current.ConversationManager.ConversationEndOneShot += OnOption2Chosen;
			})
			.CloseDialog()
			.PlayerOption("{=KfQHGUID}I keep my bargains, however loathsome they may be. We shall put you ashore.")
			.Consequence(delegate
			{
				Campaign.Current.ConversationManager.ConversationEndOneShot += OnOption3Chosen;
			})
			.CloseDialog()
			.EndPlayerOptions()
			.CloseDialog(), this);
	}

	private void AddGameMenus()
	{
		AddGameMenu("quest3_retry_menu", new TextObject("{=etH1IHNZ}You manage to put some distance between you and your enemies, and you have a moment to consider how to proceed."), retry_menu_on_init);
		AddGameMenuOption("quest3_retry_menu", "try_again_option", new TextObject("{=YHMDy3lQ}Try again"), retry_menu_try_again_on_condition, retry_menu_try_again_on_consequence);
		AddGameMenuOption("quest3_retry_menu", "leave_option", new TextObject("{=3sRdGQou}Leave"), leave_on_condition, leave_on_consequence, Isleave: true);
		AddGameMenu("quest3_encounter_menu", new TextObject("{=Mv2qMTmx}As you sail out of Ostican harbor you spot a single ship, anchored just offshore. As soon as it sights you it runs out its oars and steers to intercept your course. It is not waiting for its partner, and is probably not expecting you to put up much of a fight."), encounter_menu_on_init);
		AddGameMenuOption("quest3_encounter_menu", "fight_option", new TextObject("{=Ky03jg94}Fight"), encounter_menu_attack_on_condition, encounter_menu_attack_on_consequence);
	}

	private bool retry_menu_try_again_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.HostileAction;
		if (_battleFinished)
		{
			return !_battleWon;
		}
		return false;
	}

	private void retry_menu_try_again_on_consequence(MenuCallbackArgs args)
	{
		OnRetry();
	}

	private bool leave_on_condition(MenuCallbackArgs args)
	{
		args.Tooltip = new TextObject("{=wmTjX28f}This will exit story mode and return you to the Sandbox. You can continue the storyline later by talking to Gunnar in the port again.");
		args.optionLeaveType = GameMenuOption.LeaveType.Leave;
		return true;
	}

	private void leave_on_consequence(MenuCallbackArgs args)
	{
		CompleteQuestWithCancel();
		NavalStorylineData.DeactivateNavalStoryline();
	}

	private void retry_menu_on_init(MenuCallbackArgs args)
	{
		args.MenuContext.SetBackgroundMeshName("encounter_naval");
		if (_battleFinished && _battleWon)
		{
			OnPlayerWon();
		}
	}

	private void encounter_menu_on_init(MenuCallbackArgs args)
	{
		args.MenuContext.SetBackgroundMeshName("encounter_naval");
		NavalStorylineData.OnCheckpointReached(NavalStorylineData.NavalStorylineCheckpoint.Act2EncounterMenu);
		MobileParty.MainParty.SetMoveModeHold();
		_pirateParty?.SetMoveModeHold();
	}

	private bool encounter_menu_attack_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.HostileAction;
		return true;
	}

	private void encounter_menu_attack_on_consequence(MenuCallbackArgs args)
	{
		StartBattle();
	}

	private bool IsGunnar(IAgent agent)
	{
		return agent.Character == NavalStorylineData.Gunnar.CharacterObject;
	}

	private bool IsPirate(IAgent agent)
	{
		return agent.Character.StringId == "sea_hounds";
	}

	private bool IsMainHero(IAgent agent)
	{
		return agent.Character == CharacterObject.PlayerCharacter;
	}

	private void OnOption1Chosen()
	{
		GainRenownAction.Apply(Hero.MainHero, 10f);
		TraitLevelingHelper.OnIssueSolvedThroughQuest(Hero.MainHero, DefaultTraits.Honor, -5);
		CompleteQuestWithSuccess();
	}

	private void OnOption2Chosen()
	{
		GainRenownAction.Apply(Hero.MainHero, 5f);
		CompleteQuestWithSuccess();
	}

	private void OnOption3Chosen()
	{
		TraitLevelingHelper.OnIssueSolvedThroughQuest(Hero.MainHero, DefaultTraits.Honor, 20);
		CompleteQuestWithSuccess();
	}

	protected override void InitializeQuestOnGameLoadInternal()
	{
		_pirateTemplate = Campaign.Current.ObjectManager.GetObject<PartyTemplateObject>("storyline_act_2_sea_hounds_template");
		AddGameMenus();
		SetDialogs();
		if (MobileParty.MainParty.IsActive)
		{
			NavalDLCHelpers.SetCustomSailPatternOfPartyShips(MobileParty.MainParty, "generated_square__h4_09");
		}
		if (_pirateParty != null && _pirateParty.IsActive)
		{
			NavalDLCHelpers.SetCustomSailPatternOfPartyShips(_pirateParty, "generated_square_l1_h4_10");
		}
	}

	protected override void OnStartQuestInternal()
	{
		SetDialogs();
		AddGameMenus();
		SpawnPirates(NavalStorylineData.HomeSettlement);
		MobileParty.MainParty.IgnoreByOtherPartiesTill(base.QuestDueTime);
		NavalDLCHelpers.SetCustomSailPatternOfPartyShips(MobileParty.MainParty, "generated_square__h4_09");
		NavalDLCHelpers.AddUpgradePiecesToPartyShips(MobileParty.MainParty, PlayerShipUpgradePieces);
	}

	protected override void HourlyTick()
	{
		if (_pirateParty != null && MobileParty.MainParty.Position.DistanceSquared(_pirateParty.Position) <= Campaign.Current.Models.EncounterModel.GetEncounterJoiningRadius * Campaign.Current.Models.EncounterModel.GetEncounterJoiningRadius * 1.5f)
		{
			GameMenu.ActivateGameMenu("quest3_encounter_menu");
		}
	}

	private void StartBattle()
	{
		foreach (Ship ship in _pirateParty.Ships)
		{
			ship.IsInvulnerable = false;
		}
		PlayerEncounter.RestartPlayerEncounter(PartyBase.MainParty, _pirateParty.Party, forcePlayerOutFromSettlement: false);
		PlayerEncounter.StartBattle();
		GameMenu.ActivateGameMenu("quest3_retry_menu");
		OpenPirateBattleMission();
	}

	private void OpenPirateBattleMission()
	{
		MissionInitializerRecord navalMissionInitializerTemplate = NavalStorylineData.GetNavalMissionInitializerTemplate("naval_storyline_act_2_tutorial");
		navalMissionInitializerTemplate.PlayingInCampaignMode = true;
		navalMissionInitializerTemplate.AtmosphereOnCampaign = Campaign.Current.Models.MapWeatherModel.GetAtmosphereModel(MobileParty.MainParty.Position);
		NavalMissions.OpenNavalStorylinePirateBattleMission(navalMissionInitializerTemplate, _pirateParty, PirateTroopCount);
	}

	protected override void RegisterEventsInternal()
	{
		CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
		CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, OnSettlementLeft);
		CampaignEvents.OnMissionEndedEvent.AddNonSerializedListener(this, OnMissionEnded);
	}

	private void OnSettlementLeft(MobileParty party, Settlement settlement)
	{
		if (party == MobileParty.MainParty && _pirateParty != null)
		{
			_pirateParty.Ai.SetDoNotMakeNewDecisions(doNotMakeNewDecisions: false);
			_pirateParty.SetMoveEngageParty(MobileParty.MainParty, MobileParty.NavigationType.Naval);
			_pirateParty.Ai.SetDoNotMakeNewDecisions(doNotMakeNewDecisions: true);
		}
	}

	private void OnMissionEnded(IMission mission)
	{
		if (PlayerEncounter.Current != null && PlayerEncounter.EncounteredParty == _pirateParty?.Party)
		{
			_battleFinished = true;
			_battleWon = false;
			if (PlayerEncounter.Battle != null && PlayerEncounter.BattleState == BattleState.DefenderVictory)
			{
				_battleWon = true;
			}
			Hero.MainHero.Heal(Hero.MainHero.MaxHitPoints);
		}
	}

	private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
	{
		if (party == MobileParty.MainParty && _pirateParty != null)
		{
			_pirateParty.Ai.SetDoNotMakeNewDecisions(doNotMakeNewDecisions: false);
			_pirateParty.SetMovePatrolAroundPoint(settlement.PortPosition, MobileParty.NavigationType.Naval);
			_pirateParty.Ai.SetDoNotMakeNewDecisions(doNotMakeNewDecisions: true);
		}
	}

	protected override void OnFinalizeInternal()
	{
		if (PlayerEncounter.Battle != null && PlayerEncounter.Battle.InvolvedParties.Contains(_pirateParty.Party))
		{
			PlayerEncounter.Finish();
		}
		if (_pirateParty != null)
		{
			if (_pirateParty.IsActive)
			{
				_pirateParty.Ai.DisableAi();
				DestroyPartyAction.Apply(null, _pirateParty);
			}
			_pirateParty = null;
		}
		MobileParty.MainParty.IgnoreByOtherPartiesTill(CampaignTime.Now);
	}

	protected override void OnCompleteWithSuccessInternal()
	{
		NavalStorylineData.OnCheckpointReached(NavalStorylineData.NavalStorylineCheckpoint.Act2Finalized);
	}

	private void OnPlayerWon()
	{
		StartConversationWithPirate();
	}

	private void StartConversationWithPirate()
	{
		CharacterObject element = Campaign.Current.ObjectManager.GetObject<CharacterObject>("sea_hounds");
		_pirateParty.Party.AddElementToMemberRoster(element, 1);
		CharacterObject conversationCharacterPartyLeader = ConversationHelper.GetConversationCharacterPartyLeader(_pirateParty.Party);
		ConversationCharacterData playerCharacterData = new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty, noHorse: true, noWeapon: false, spawnAfterFight: false, isCivilianEquipmentRequiredForLeader: false, isCivilianEquipmentRequiredForBodyGuardCharacters: false, noBodyguards: true);
		ConversationCharacterData conversationPartnerData = new ConversationCharacterData(conversationCharacterPartyLeader, _pirateParty.Party, noHorse: true, noWeapon: false, spawnAfterFight: false, isCivilianEquipmentRequiredForLeader: false, isCivilianEquipmentRequiredForBodyGuardCharacters: false, noBodyguards: true);
		CampaignMission.OpenConversationMission(playerCharacterData, conversationPartnerData, "conversation_scene_sea_multi_agent", "", isMultiAgentConversation: true);
	}

	private void OnRetry()
	{
		RefreshPiratePartyForces();
		_battleFinished = false;
		_battleWon = false;
		OpenPirateBattleMission();
	}

	private void SpawnPirates(Settlement settlement)
	{
		Clan clan = Clan.All.FirstOrDefault((Clan t) => t.StringId == "northern_pirates");
		CampaignVec2 position = NavigationHelper.FindReachablePointAroundPosition(settlement.PortPosition, MobileParty.NavigationType.Naval, 20f, 10f);
		TextObject textObject = new TextObject("{=SKC3FeGR}Sea Hounds");
		_pirateParty = CustomPartyComponent.CreateCustomPartyWithPartyTemplate(position, 0.5f, SettlementHelper.FindRandomHideout((Settlement t) => t.IsHideout), textObject, clan, _pirateTemplate, NavalStorylineData.Purig);
		_pirateParty.Party.SetCustomName(textObject);
		_pirateParty.InitializeMobilePartyAtPosition(position);
		_pirateParty.SetLandNavigationAccess(access: false);
		_pirateParty.Party.SetVisualAsDirty();
		_pirateParty.ActualClan = clan;
		_pirateParty.SetPartyUsedByQuest(isActivelyUsed: true);
		_pirateParty.Party.SetCustomBanner(NavalStorylineData.CorsairBanner);
		(_pirateParty.PartyComponent as CustomPartyComponent).SetBaseSpeed(2.5f);
		_pirateParty.IgnoreByOtherPartiesTill(CampaignTime.Never);
		_pirateParty.SetMoveEngageParty(MobileParty.MainParty, MobileParty.NavigationType.Naval);
		_pirateParty.Ai.SetDoNotMakeNewDecisions(doNotMakeNewDecisions: true);
		NavalDLCHelpers.AddUpgradePiecesToPartyShips(_pirateParty, PirateShipUpgradePieces);
		NavalDLCHelpers.SetCustomSailPatternOfPartyShips(_pirateParty, "generated_square_l1_h4_10");
	}

	private void RefreshPiratePartyForces()
	{
		_pirateParty.MemberRoster.Clear();
		CharacterObject element = Campaign.Current.ObjectManager.GetObject<CharacterObject>("sea_hounds");
		_pirateParty.AddElementToMemberRoster(element, PirateTroopCount * 2);
		foreach (Ship item in _pirateParty.Ships.ToList())
		{
			item.Owner = null;
		}
		using List<ShipTemplateStack>.Enumerator enumerator2 = _pirateTemplate.ShipHulls.GetEnumerator();
		while (enumerator2.MoveNext())
		{
			new Ship(enumerator2.Current.ShipHull)
			{
				Owner = _pirateParty.Party,
				IsInvulnerable = true
			};
		}
	}

	public bool IsPiratePartyVisible()
	{
		if (_pirateParty != null && _pirateParty.IsActive)
		{
			return _pirateParty.IsVisible;
		}
		return false;
	}
}
