using System.Collections.Generic;
using System.Linq;
using Helpers;
using NavalDLC.Missions;
using SandBox;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Objects;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace NavalDLC.Storyline.Quests;

public class HuntDownTheEmiraAlFahdaAndTheCorsairsQuest : NavalStorylineQuestBase
{
	private const int NumberOfCorsairParties = 2;

	private const int GoldReward = 1000;

	private const int RelationshipReward = 10;

	private const int CorsairShipAiDisableTime = 3;

	private const string QuestSetPieceEncounterMenuId = "naval_storyline_act_3_quest_2_encounter_menu";

	private const string QuestSetPieceRetryMenuId = "naval_storyline_act_3_quest_2_retry_menu";

	private const string Act3Quest2CorsairPartyTemplateStringIdBase = "storyline_act3_quest_2_corsair_generic_template_";

	private const string Act3Quest2BossCorsairPartyTemplateStringId = "storyline_act3_quest_2_boss_corsair_template";

	private const string FahdaShipHullId = "ship_meditheavy_storyline";

	private const string MediumReinforcementShipHullId = "ship_liburna_storyline";

	private const string LightReinforcementShipHullId = "ship_meditlight_storyline";

	private static readonly Dictionary<string, string> FahdaShipUpgradePieces = new Dictionary<string, string> { { "side", "side_southern_shields_lvl2" } };

	private static readonly Dictionary<string, string> MediumReinforcementShipUpgradePieces = new Dictionary<string, string>
	{
		{ "side", "side_southern_shields_lvl2" },
		{ "sail", "sails_lvl2" }
	};

	private static readonly Dictionary<string, string> FirstLightReinforcementShipUpgradePieces = new Dictionary<string, string>
	{
		{ "side", "side_southern_shields_lvl2" },
		{ "sail", "sails_lvl2" }
	};

	private static readonly Dictionary<string, string> SecondLightReinforcementShipUpgradePieces = new Dictionary<string, string>
	{
		{ "side", "side_southern_shields_lvl2" },
		{ "sail", "sails_lvl2" }
	};

	private const string LaharShipHullId = "ship_liburna_q2_storyline";

	private static readonly Dictionary<string, string> LaharShipUpgradePieces = new Dictionary<string, string>
	{
		{ "side", "side_southern_shields_lvl3" },
		{ "sail", "sails_lvl2" },
		{ "bow", "bow_northern_reinforced_ram_lvl3" }
	};

	private const string GunnarShipHullId = "northern_medium_ship";

	private static readonly Dictionary<string, string> GunnarShipUpgradePieces = new Dictionary<string, string>
	{
		{ "side", "side_southern_shields_lvl2" },
		{ "sail", "sails_lvl2" }
	};

	private GameEntity _stormEntity;

	[SaveableField(1)]
	private List<MobileParty> _corsairParties;

	[SaveableField(2)]
	private JournalLog _playerStartsQuestLog;

	[SaveableField(3)]
	private CampaignVec2 _corsairSpawnPosition;

	[SaveableField(4)]
	private int _numberOfDefeatedCorsairParties;

	[SaveableField(5)]
	private MobileParty _bossCorsairParty;

	[SaveableField(6)]
	private bool _battleWon;

	[SaveableField(7)]
	private bool _willProgressStoryline;

	[SaveableField(8)]
	private bool _battleStarted;

	[SaveableField(9)]
	private readonly MapMarker _corsairHuntingGroundMarker;

	public override bool WillProgressStoryline => _willProgressStoryline;

	public override TextObject Title
	{
		get
		{
			TextObject textObject = new TextObject("{=kEyCQWh1}Hunt Down {HERO.NAME}");
			textObject.SetCharacterProperties("HERO", NavalStorylineData.EmiraAlFahda.CharacterObject);
			return textObject;
		}
	}

	private TextObject DescriptionLogText
	{
		get
		{
			TextObject textObject = new TextObject("{=ezctGj6M}Find the corsair {HERO.NAME} and defeat her.");
			textObject.SetCharacterProperties("HERO", NavalStorylineData.EmiraAlFahda.CharacterObject);
			return textObject;
		}
	}

	private TextObject MainCorsairShipSpawnedLogText
	{
		get
		{
			TextObject textObject = new TextObject("{=BKlHaMZ6}Overtake and defeat {HERO.NAME} and her fleet.");
			textObject.SetCharacterProperties("HERO", NavalStorylineData.EmiraAlFahda.CharacterObject);
			return textObject;
		}
	}

	private TextObject QuestSucceededWithRansomLogText
	{
		get
		{
			TextObject textObject = new TextObject("{=UvFN0bf1}You decided to accept {HERO.NAME}'s ransom money. ({GOLD_REWARD}{GOLD_ICON}).");
			textObject.SetCharacterProperties("HERO", NavalStorylineData.EmiraAlFahda.CharacterObject);
			textObject.SetTextVariable("GOLD_REWARD", 1000);
			return textObject;
		}
	}

	private TextObject QuestSucceededWithReturnOfEmiraLogText
	{
		get
		{
			TextObject textObject = new TextObject("{=DKA4tOwq}You decided to return {HERO.NAME} to her uncles alive.(+{RELATIONSHIP_REWARD} relationship with all notables in {SETTLEMENT_LINK}).");
			textObject.SetCharacterProperties("HERO", NavalStorylineData.EmiraAlFahda.CharacterObject);
			textObject.SetTextVariable("RELATIONSHIP_REWARD", 10);
			textObject.SetTextVariable("SETTLEMENT_LINK", NavalStorylineData.Act3Quest2TargetSettlement.EncyclopediaLinkWithName);
			return textObject;
		}
	}

	private TextObject PlayerStartsQuestLogText
	{
		get
		{
			TextObject textObject = new TextObject("{=pfIWdGnV}The corsairs appear to be scattered. Find them and take them, until you sight {HERO.NAME}.");
			textObject.SetCharacterProperties("HERO", NavalStorylineData.EmiraAlFahda.CharacterObject);
			return textObject;
		}
	}

	public override NavalStorylineData.NavalStorylineStage Stage => NavalStorylineData.NavalStorylineStage.Act3Quest2;

	protected override string MainPartyTemplateStringId => "storyline_act3_quest_2_main_party_template";

	public HuntDownTheEmiraAlFahdaAndTheCorsairsQuest(string questId, Hero questGiver, CampaignVec2 corsairSpawnPosition)
		: base(questId, questGiver, CampaignTime.Never, 0)
	{
		_willProgressStoryline = false;
		_numberOfDefeatedCorsairParties = 0;
		_corsairParties = new List<MobileParty>();
		_bossCorsairParty = null;
		_corsairSpawnPosition = corsairSpawnPosition;
		_corsairHuntingGroundMarker = Campaign.Current.MapTrackerManager.CreateMapMarker(NavalStorylineData.CorsairBanner, new TextObject("{=QLrwlirp}Corsair Hunting Grounds"), _corsairSpawnPosition.AsVec3(), isVisibleOnMap: false, base.StringId);
		AddLog(DescriptionLogText);
	}

	protected override void OnFinalizeInternal()
	{
		_playerStartsQuestLog = null;
		DestroyCorsairParties();
		Scene scene = ((MapScene)Campaign.Current.MapSceneWrapper).Scene;
		List<GameEntity> entities = new List<GameEntity>();
		scene.GetAllEntitiesWithScriptComponent<CampaignMapAmbientOccluder>(ref entities);
		foreach (GameEntity item in entities)
		{
			item.GetFirstScriptOfType<CampaignMapAmbientOccluder>().UnregisterQuestStorm(_stormEntity);
		}
		_stormEntity?.Remove(111);
	}

	protected override void InitializeQuestOnGameLoadInternal()
	{
		SetDialogs();
		AddGameMenus();
		if (_numberOfDefeatedCorsairParties == 2)
		{
			SpawnStormEntity();
		}
	}

	protected override void SetDialogs()
	{
		AddDialogsForFinalFight();
	}

	protected override void OnStartQuestInternal()
	{
		SetDialogs();
		AddGameMenus();
		_numberOfDefeatedCorsairParties = 2;
		SpawnMainCorsairParty();
		SpawnStormEntity();
		_willProgressStoryline = true;
		AddTrackedObject(_corsairHuntingGroundMarker);
	}

	protected override void HourlyTick()
	{
		if (_corsairHuntingGroundMarker.Position.Distance(MobileParty.MainParty.Position.AsVec3()) > 15f)
		{
			_corsairHuntingGroundMarker.IsVisibleOnMap = true;
		}
		else
		{
			_corsairHuntingGroundMarker.IsVisibleOnMap = false;
		}
		foreach (MobileParty corsairParty in _corsairParties)
		{
			if (MBRandom.RandomFloat < 0.25f && corsairParty.IsActive && !corsairParty.IsMoving && !corsairParty.Ai.IsDisabled)
			{
				CampaignVec2 point = NavigationHelper.FindReachablePointAroundPosition(_corsairSpawnPosition, MobileParty.NavigationType.Naval, 10f, 3f);
				corsairParty.SetMoveGoToPoint(point, MobileParty.NavigationType.Naval);
			}
		}
	}

	protected override void IsNavalQuestPartyInternal(PartyBase party, NavalStorylinePartyData data)
	{
		if (_corsairParties.Any((MobileParty c) => c.Party == party))
		{
			PartyTemplateObject templateObject = Campaign.Current.ObjectManager.GetObject<PartyTemplateObject>("storyline_act3_quest_2_corsair_generic_template_" + ((!party.Id.Contains("0")) ? 1 : 0));
			data.PartySize = (int)NavalDLCHelpers.GetMaxPartySizeLimitFromTemplate(templateObject).ResultNumber;
			data.IsQuestParty = true;
		}
		else if (_bossCorsairParty != null && _bossCorsairParty.Party == party)
		{
			PartyTemplateObject templateObject2 = Campaign.Current.ObjectManager.GetObject<PartyTemplateObject>("storyline_act3_quest_2_boss_corsair_template");
			data.PartySize = (int)NavalDLCHelpers.GetMaxPartySizeLimitFromTemplate(templateObject2).ResultNumber + 1;
			data.IsQuestParty = true;
		}
		if (party == PartyBase.MainParty)
		{
			data.PartySize++;
		}
	}

	protected override void OnCompleteWithSuccessInternal()
	{
		MobileParty.MainParty.MemberRoster.RemoveTroop(NavalStorylineData.Lahar.CharacterObject);
		NavalStorylineData.Lahar.ChangeState(Hero.CharacterStates.Disabled);
		GainRenownAction.Apply(Hero.MainHero, 10f);
		NavalStorylineData.OnCheckpointReached(NavalStorylineData.NavalStorylineCheckpoint.Act3Quest2Succeeded);
	}

	protected override void OnFailedInternal()
	{
		MobileParty.MainParty.MemberRoster.RemoveTroop(NavalStorylineData.Lahar.CharacterObject);
		NavalStorylineData.Lahar.ChangeState(Hero.CharacterStates.Disabled);
	}

	protected override void RegisterEventsInternal()
	{
		CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, OnMobilePartyDestroyed);
		CampaignEvents.MapEventStarted.AddNonSerializedListener(this, OnMapEventStarted);
		CampaignEvents.OnMissionEndedEvent.AddNonSerializedListener(this, OnMissionEnded);
		CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, OnGameMenuOpened);
		CampaignEvents.OnShipOwnerChangedEvent.AddNonSerializedListener(this, OnShipOwnerChanged);
		CampaignEvents.BeforeGameMenuOpenedEvent.AddNonSerializedListener(this, OnBeforeGameMenuOpened);
	}

	private void OnMapEventStarted(MapEvent mapEvent, PartyBase partyBase1, PartyBase partyBase2)
	{
		if (partyBase1.IsNavalStorylineQuestParty())
		{
			foreach (Ship ship in partyBase1.Ships)
			{
				ship.IsInvulnerable = false;
			}
		}
		if (!partyBase2.IsNavalStorylineQuestParty())
		{
			return;
		}
		foreach (Ship ship2 in partyBase2.Ships)
		{
			ship2.IsInvulnerable = false;
		}
	}

	private void OnShipOwnerChanged(Ship ship, PartyBase partyBase, ChangeShipOwnerAction.ShipOwnerChangeDetail detail)
	{
		if (partyBase == PartyBase.MainParty && ship.IsInvulnerable)
		{
			ship.IsInvulnerable = false;
		}
	}

	private void AddGameMenus()
	{
		AddGameMenu("naval_storyline_act_3_quest_2_encounter_menu", new TextObject("{=YjcPI4pT}An east wind sweeps across the sea, bearing desert dust, and briefly obscures your vision. Soon after it lifts, you hear your lookouts shouting excitedly to you. They have spotted Fahda’s fleet, which appears to have been damaged by the gale. If you attack now, you may be able to sink the flagship before it can escape."), naval_storyline_act_3_quest_2_set_piece_encounter_menu_on_init, GameMenu.MenuOverlayType.Encounter);
		AddGameMenuOption("naval_storyline_act_3_quest_2_encounter_menu", "naval_storyline_act_3_quest_2_encounter_menu_continue", new TextObject("{=1r0tDsrR}Attack!"), naval_storyline_act_3_quest_2_set_piece_encounter_menu_attack_on_condition, naval_storyline_act_3_quest_2_set_piece_encounter_menu_attack_on_consequence);
		AddGameMenu("naval_storyline_act_3_quest_2_retry_menu", new TextObject("{=etH1IHNZ}You manage to put some distance between you and your enemies, and you have a moment to consider how to proceed."), naval_storyline_act_3_quest_2_set_piece_retry_menu_on_init);
		AddGameMenuOption("naval_storyline_act_3_quest_2_retry_menu", "try_again_option", new TextObject("{=YHMDy3lQ}Try again"), naval_storyline_act_3_quest_2_set_piece_retry_menu_retry_on_condition, naval_storyline_act_3_quest_2_set_piece_retry_menu_retry_on_consequence);
		AddGameMenuOption("naval_storyline_act_3_quest_2_retry_menu", "leave_option", new TextObject("{=3sRdGQou}Leave"), naval_storyline_act_3_quest_2_set_piece_retry_menu_leave_on_condition, naval_storyline_act_3_quest_2_set_piece_retry_menu_leave_on_consequence, Isleave: true);
	}

	private void naval_storyline_act_3_quest_2_set_piece_retry_menu_leave_on_consequence(MenuCallbackArgs args)
	{
		CompleteQuestWithCancel();
		NavalStorylineData.DeactivateNavalStoryline();
	}

	private bool naval_storyline_act_3_quest_2_set_piece_retry_menu_retry_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Mission;
		if (!_battleWon)
		{
			return _battleStarted;
		}
		return false;
	}

	private bool naval_storyline_act_3_quest_2_set_piece_encounter_menu_attack_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Continue;
		if (!_battleStarted)
		{
			return !_battleWon;
		}
		return false;
	}

	private void OnBeforeGameMenuOpened(MenuCallbackArgs args)
	{
		if (args.MenuContext?.GameMenu?.StringId == "naval_storyline_encounter_meeting" && NavalStorylineData.IsNavalStoryLineActive() && PlayerEncounter.EncounteredParty != null && PlayerEncounter.EncounteredParty.IsNavalStorylineQuestParty())
		{
			PlayerEncounter.SetMeetingDone();
		}
	}

	private bool naval_storyline_act_3_quest_2_set_piece_retry_menu_leave_on_condition(MenuCallbackArgs args)
	{
		args.Tooltip = new TextObject("{=wmTjX28f}This will exit story mode and return you to the Sandbox. You can continue the storyline later by talking to Gunnar in the port again.");
		args.optionLeaveType = GameMenuOption.LeaveType.Leave;
		if (!_battleWon)
		{
			return _battleStarted;
		}
		return false;
	}

	private void naval_storyline_act_3_quest_2_set_piece_encounter_menu_on_init(MenuCallbackArgs args)
	{
		args.MenuContext.SetBackgroundMeshName("encounter_naval");
		NavalStorylineData.OnCheckpointReached(NavalStorylineData.NavalStorylineCheckpoint.Act3Quest2EncounterMenu);
	}

	private void naval_storyline_act_3_quest_2_set_piece_retry_menu_on_init(MenuCallbackArgs args)
	{
		args.MenuContext.SetBackgroundMeshName("encounter_naval");
		if (_battleWon)
		{
			PlayerEncounter.Finish();
			RefreshShips(MobileParty.MainParty, Campaign.Current.ObjectManager.GetObject<PartyTemplateObject>(MainPartyTemplateStringId));
			AddShipUpgradesForMainParty();
			NavalStorylineData.EmiraAlFahda.SetHasMet();
			NavalStorylineData.EmiraAlFahda.MakeWounded();
			ConversationCharacterData playerCharacterData = new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty, noHorse: true, noWeapon: false, spawnAfterFight: false, isCivilianEquipmentRequiredForLeader: false, isCivilianEquipmentRequiredForBodyGuardCharacters: false, noBodyguards: true);
			ConversationCharacterData conversationPartnerData = new ConversationCharacterData(NavalStorylineData.EmiraAlFahda.CharacterObject, null, noHorse: true, noWeapon: true, spawnAfterFight: true, isCivilianEquipmentRequiredForLeader: false, isCivilianEquipmentRequiredForBodyGuardCharacters: false, noBodyguards: true);
			CampaignMission.OpenConversationMission(playerCharacterData, conversationPartnerData, "conversation_scene_sea_multi_agent", "", isMultiAgentConversation: true);
		}
		else
		{
			RefreshParty(_bossCorsairParty, Campaign.Current.ObjectManager.GetObject<PartyTemplateObject>("storyline_act3_quest_2_boss_corsair_template"));
			AddShipUpgradesForMainCorsairParty();
			RefreshParty(MobileParty.MainParty, Campaign.Current.ObjectManager.GetObject<PartyTemplateObject>(MainPartyTemplateStringId));
			AddShipUpgradesForMainParty();
		}
	}

	private void naval_storyline_act_3_quest_2_set_piece_encounter_menu_attack_on_consequence(MenuCallbackArgs args)
	{
		StartBattle();
	}

	private void naval_storyline_act_3_quest_2_set_piece_retry_menu_retry_on_consequence(MenuCallbackArgs args)
	{
		StartBattle();
	}

	private void OnGameMenuOpened(MenuCallbackArgs args)
	{
		if (!NavalStorylineData.IsNavalStoryLineActive() || PlayerEncounter.EncounteredParty == null || !PlayerEncounter.EncounteredParty.IsNavalStorylineQuestParty())
		{
			return;
		}
		string text = args.MenuContext?.GameMenu?.StringId;
		if (_bossCorsairParty?.Party == PlayerEncounter.EncounteredParty)
		{
			if (text == "naval_storyline_encounter")
			{
				GameMenu.ActivateGameMenu("naval_storyline_act_3_quest_2_encounter_menu");
			}
		}
		else
		{
			MBTextManager.SetTextVariable("ENCOUNTER_TEXT", new TextObject("{=XVCdua8m}One of your sharper-eyed sailors thinks he sees a ship. You stare at the horizon, and though at first it's hard to make out shapes against the choppy waves of the gulf, you eventually distinguish the unmistakable outline of a lateen sail. It's a corsair, and it's heading directly towards you. "), sendClients: true);
		}
	}

	private void OnMissionEnded(IMission mission)
	{
		if (!Mission.Current.IsNavalBattle || PlayerEncounter.Current == null || PlayerEncounter.EncounteredParty == null || _bossCorsairParty?.Party != PlayerEncounter.EncounteredParty)
		{
			return;
		}
		if (PlayerEncounter.CampaignBattleResult != null && PlayerEncounter.CampaignBattleResult.BattleResolved)
		{
			if (PlayerEncounter.CampaignBattleResult.PlayerDefeat)
			{
				_battleWon = false;
			}
			else if (PlayerEncounter.CampaignBattleResult.PlayerVictory && _bossCorsairParty?.Party == PlayerEncounter.EncounteredParty)
			{
				_battleWon = true;
			}
		}
		else if (PlayerEncounter.WinningSide == BattleSideEnum.None)
		{
			_battleWon = false;
		}
		else
		{
			Debug.FailedAssert("unhandled case", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC\\Storyline\\Quests\\HuntDownTheEmiraAlFahdaAndTheCorsairsQuest.cs", "OnMissionEnded", 477);
		}
	}

	private void OnMobilePartyDestroyed(MobileParty party, PartyBase partyBase)
	{
		if (NavalStorylineData.IsNavalStoryLineActive() && _playerStartsQuestLog != null && _corsairParties.Contains(party) && partyBase == PartyBase.MainParty)
		{
			MBInformationManager.AddQuickInformation(new TextObject("{=MRX4gImP}So far so good, but there are still enemies about."), 0, NavalStorylineData.Gunnar.CharacterObject);
			_numberOfDefeatedCorsairParties++;
			_corsairParties.Remove(party);
			UpdateQuestTaskStage(_playerStartsQuestLog, _numberOfDefeatedCorsairParties);
			if (2 == _numberOfDefeatedCorsairParties)
			{
				SpawnStormEntity();
				SpawnMainCorsairParty();
				AddLog(MainCorsairShipSpawnedLogText);
			}
		}
	}

	private void AddDialogsForFinalFight()
	{
		Campaign.Current.ConversationManager.AddDialogFlow(DialogFlow.CreateDialogFlow("start", 1200).NpcLine(new TextObject("{=unOIbuqz}[ib:warrior][if:convo_shocked]What have you done? Do you know who I am? I have allies who'll unthread your entrails from your guts and hang you with them from your own yardarm. I am queen of these waters, you fools, and those who practice piracy here without my permission end up chum to attract the sharks."), IsEmiraAlFahda, IsLahar).Condition(MultiAgentConversationCondition)
			.GenerateToken(out var token)
			.GenerateToken(out var token2)
			.GenerateToken(out var token3)
			.GenerateToken(out var token4)
			.GenerateToken(out var token5)
			.NpcLine(new TextObject("{=xQunuNT9}[ib:closed][if:convo_huge_smile]My lady, we are not pirates. Rather, I am a man who has done many services for families such as your own in Quyaz. At present I am working for your uncles. I do not know what they intend to do with you, although I do not expect that a town that lives on trade will deal leniently with piracy."), IsLahar, IsEmiraAlFahda)
			.NpcLine(new TextObject("{=nyOUdUQI}[ib:hip][if:convo_merry]Before we sail, however, I would like you to have a chat with my friend here."), IsLahar, IsEmiraAlFahda)
			.NpcLine(new TextObject("{=LFLn7SJc}[ib:weary]So you are on contract to deliver me alive to Quyaz, are you? I can tell you this, then - my lineage goes back to the founding of that city, and if you spill so much as a drop of my blood, your own shall be drained from your body like that of a horse-fish. As for the Sea Hounds, they are my allies and servants, and I shall not betray them to you."), IsEmiraAlFahda, IsLahar)
			.NpcLine(new TextObject("{=C88poDCA}[ib:normal][if:convo_pondering]How much are my uncles paying you, anyway? I have a chest of silver set aside for occasions such as this, and I suspect I could pay you more than they will. They are stingy men."), IsEmiraAlFahda, IsLahar)
			.NpcLine(new TextObject("{=v2664Qeo}..."), IsEmiraAlFahda, IsLahar)
			.GotoDialogState(token)
			.BeginPlayerOptions(token, optionUsedOnce: true)
			.PlayerOption(new TextObject("{=q3uOXLEO}I am here too, and I have no contract to deliver you anywhere alive."), IsEmiraAlFahda)
			.GotoDialogState(token2)
			.PlayerOption(new TextObject("{=XfIbjoH8}You tell me all you know about the Sea Hounds and their dealings in slaves."), IsEmiraAlFahda)
			.GotoDialogState(token2)
			.EndPlayerOptions()
			.NpcLine(new TextObject("{=06AGZvSg}[ib:nervous]Are you threatening me? You won't get a single coin from my uncles if you harm me."), IsEmiraAlFahda, IsMainHero, token2)
			.NpcLine(new TextObject("{=T0a3QpjV}[if:convo_grave]Unlike Lahar, here, we have not shed our blood today merely for a part-share of a ransom, or to boost our standing with the merchants of Quyaz. You are an ally of the Sea Hounds, and it serves us well to make an example of you. Your life is forfeit unless you tell us something we can use."), IsGunnar, IsEmiraAlFahda)
			.NpcLine(new TextObject("{=IPq1hnUG}[ib:nervous2][if:convo_nervous]How do I know that telling you about the Sea Hounds will save my life?"), IsEmiraAlFahda, IsMainHero, null, token3)
			.BeginPlayerOptions(token3)
			.PlayerOption(new TextObject("{=Su0h3ZMC}If you speak truthfully, you will live."), IsEmiraAlFahda)
			.GotoDialogState(token4)
			.PlayerOption(new TextObject("{=9tmYkhb1}You'll just have to try and see."), IsEmiraAlFahda)
			.GotoDialogState(token4)
			.EndPlayerOptions()
			.NpcLine(new TextObject("{=tlXQV9mO}[ib:normal]I can tell you this – I don’t have your sister. I used to buy captives from the Sea Hounds. But now they have this new leader named Purig, who keeps them all for his own purposes. Apparently he has some anchorage up in the north, where he intends to use slaves to build larger and stronger ships."), IsEmiraAlFahda, IsMainHero, token4)
			.NpcLine(new TextObject("{=w5GbjHDG}[ib:closed]Purig, a leader among the Sea Hounds! I'll speak straight here - it gnaws at my gut to hear that he is prospering from his treachery."), IsGunnar, IsMainHero)
			.NpcLine(new TextObject("{=C2OtgWn0}[ib:demure]He acts as though the Sea Hounds have already crowned him their king. He demanded that I hunt for captives here in the south and sell them to him, promising to pay me with a huge store of silver that some new partners of his, a vile-looking gang of Vlandian pirates, hoped to steal from the merchants of Omor."), IsEmiraAlFahda, IsMainHero)
			.NpcLine(new TextObject("{=Ex5CzHBt}We should get more information on this Omor silver. If we can stop these Vlandians it would deal a great blow to Purig, and we could possibly find out more about this northern anchorage, his captives, and maybe your sister."), IsGunnar, IsMainHero)
			.NpcLine(new TextObject("{=2bzElv6k}So that information is worth something to you, is it not? If we add in that ransom I mentioned, is it enough to buy my life and my freedom?"), IsEmiraAlFahda, IsMainHero)
			.NpcLine(new TextObject("{=M24S1pEI}You know my preference, {PLAYER.NAME}. If I bring her back to Quyaz, I will ensure that you get some of the credit, but perhaps you prefer good cold silver to goodwill."), IsLahar, IsMainHero, null, token5)
			.BeginPlayerOptions(token5)
			.PlayerOption(new TextObject("{=VHbGnf4W}Return her to her uncles alive, as per your original understanding."), IsLahar)
			.NpcLine(new TextObject("{=7f9yXAvI}I accept your decision. Very well then…"), IsLahar, IsMainHero)
			.Consequence(delegate
			{
				OnPlayerSelectsOption1();
			})
			.CloseDialog()
			.PlayerOption(new TextObject("{=xpz9JFGK}The lady offers a fair ransom. Let us accept."), IsLahar)
			.NpcLine(new TextObject("{=7f9yXAvI}I accept your decision. Very well then…"), IsLahar, IsMainHero)
			.NpcLine(new TextObject("{=cxG2qhbv}Listen. I have enjoyed our excursion, and hunting pirates is always good business. Though I must depart now to Quyaz, I would like to go hunting with you again. Gunnar tells me that you will be sailing from Ostican once you locate your next quarry. Hopefully I will see you there soon."), IsLahar, IsMainHero)
			.Consequence(delegate
			{
				OnPlayerSelectsOption2();
			})
			.CloseDialog()
			.EndPlayerOptions()
			.CloseDialog(), this);
	}

	private void OnPlayerSelectsOption1()
	{
		foreach (Hero notable in NavalStorylineData.Act3Quest2TargetSettlement.Notables)
		{
			ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, notable, 10);
		}
		AddLog(QuestSucceededWithReturnOfEmiraLogText);
		CompleteQuestWithSuccess();
	}

	private void OnPlayerSelectsOption2()
	{
		GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, 1000);
		AddLog(QuestSucceededWithRansomLogText);
		CompleteQuestWithSuccess();
	}

	private bool IsLahar(IAgent agent)
	{
		return agent.Character == NavalStorylineData.Lahar.CharacterObject;
	}

	private bool IsGunnar(IAgent agent)
	{
		return agent.Character == NavalStorylineData.Gunnar.CharacterObject;
	}

	private bool IsMainHero(IAgent agent)
	{
		return agent.Character == CharacterObject.PlayerCharacter;
	}

	private bool IsEmiraAlFahda(IAgent agent)
	{
		return agent.Character == NavalStorylineData.EmiraAlFahda.CharacterObject;
	}

	private Agent SpawnGunnar()
	{
		AgentBuildData agentBuildData = new AgentBuildData(NavalStorylineData.Gunnar.CharacterObject);
		agentBuildData.TroopOrigin(new SimpleAgentOrigin(agentBuildData.AgentCharacter));
		agentBuildData.InitialPosition(Mission.Current.Scene.FindEntityWithName("free_infantry_spawn_point_1").GlobalPosition);
		agentBuildData.InitialDirection(Agent.Main.LookDirection.AsVec2.Normalized());
		agentBuildData.NoHorses(noHorses: true);
		return Mission.Current.SpawnAgent(agentBuildData);
	}

	private bool MultiAgentConversationCondition()
	{
		if (Hero.OneToOneConversationHero == NavalStorylineData.EmiraAlFahda && MobileParty.MainParty.IsCurrentlyAtSea && Mission.Current != null)
		{
			Agent item = SpawnLahar();
			Agent item2 = SpawnGunnar();
			Campaign.Current.ConversationManager.AddConversationAgents(new List<Agent> { item, item2 }, setActionsInstantly: true);
			return true;
		}
		return false;
	}

	private Agent SpawnLahar()
	{
		AgentBuildData agentBuildData = new AgentBuildData(NavalStorylineData.Lahar.CharacterObject);
		agentBuildData.TroopOrigin(new SimpleAgentOrigin(agentBuildData.AgentCharacter));
		agentBuildData.InitialPosition(Mission.Current.Scene.FindEntityWithName("free_infantry_spawn_point_0").GlobalPosition);
		agentBuildData.InitialDirection(Agent.Main.LookDirection.AsVec2.Normalized());
		agentBuildData.NoHorses(noHorses: true);
		return Mission.Current.SpawnAgent(agentBuildData);
	}

	private void StartBattle()
	{
		_battleWon = false;
		_battleStarted = true;
		foreach (TroopRosterElement item in from troop in PartyBase.MainParty.MemberRoster.GetTroopRoster()
			where troop.Character.IsHero && troop.Character.HeroObject.IsWounded
			select troop)
		{
			item.Character.HeroObject.Heal(item.Character.HeroObject.WoundedHealthLimit - item.Character.HeroObject.HitPoints + 1);
		}
		PlayerEncounter.Finish();
		PlayerEncounter.Start();
		PlayerEncounter.Current.SetupFields(_bossCorsairParty.Party, PartyBase.MainParty);
		PlayerEncounter.StartBattle();
		MissionInitializerRecord navalMissionInitializerTemplate = NavalStorylineData.GetNavalMissionInitializerTemplate("naval_storyline_act_3_quest_2");
		navalMissionInitializerTemplate.NeedsRandomTerrain = false;
		navalMissionInitializerTemplate.PlayingInCampaignMode = false;
		navalMissionInitializerTemplate.SceneHasMapPatch = false;
		navalMissionInitializerTemplate.AtmosphereOnCampaign.NauticalInfo.UsesNavalSimulatedWater = 1;
		NavalMissions.OpenNavalStorylineWoundedBeastBattleMission(navalMissionInitializerTemplate);
		GameMenu.ActivateGameMenu("naval_storyline_act_3_quest_2_retry_menu");
	}

	private void SpawnMainCorsairParty()
	{
		NavalStorylineData.EmiraAlFahda.ChangeState(Hero.CharacterStates.Active);
		_bossCorsairParty = CustomPartyComponent.CreateCustomPartyWithPartyTemplate(_corsairSpawnPosition, 1f, NavalStorylineData.HomeSettlement, new TextObject("{=j7h8QfsE}Fahda's Corsairs"), Clan.BanditFactions.FirstOrDefault((Clan x) => x.StringId == "southern_pirates"), Campaign.Current.ObjectManager.GetObject<PartyTemplateObject>("storyline_act3_quest_2_boss_corsair_template"), NavalStorylineData.EmiraAlFahda, NavalStorylineData.EmiraAlFahda, "", "", 1f);
		AddShipUpgradesForMainCorsairParty();
		SetupCorsairParty(_bossCorsairParty);
		_bossCorsairParty.IsInfoHidden = true;
	}

	private void AddShipUpgradesForMainCorsairParty()
	{
		bool flag = false;
		foreach (Ship ship in _bossCorsairParty.Ships)
		{
			if (ship.ShipHull.StringId == "ship_meditheavy_storyline")
			{
				ship.ChangeFigurehead(DefaultFigureheads.Viper);
				AddShipUpgradePieces(ship, FahdaShipUpgradePieces);
			}
			else if (ship.ShipHull.StringId == "ship_liburna_storyline")
			{
				ship.ChangeFigurehead(DefaultFigureheads.Hawk);
				AddShipUpgradePieces(ship, MediumReinforcementShipUpgradePieces);
			}
			else if (ship.ShipHull.StringId == "ship_meditlight_storyline")
			{
				if (flag)
				{
					AddShipUpgradePieces(ship, SecondLightReinforcementShipUpgradePieces);
					continue;
				}
				AddShipUpgradePieces(ship, FirstLightReinforcementShipUpgradePieces);
				flag = true;
			}
		}
	}

	private void AddShipUpgradesForMainParty()
	{
		foreach (Ship ship in MobileParty.MainParty.Ships)
		{
			if (ship.ShipHull.StringId == "ship_liburna_q2_storyline")
			{
				ship.ChangeFigurehead(DefaultFigureheads.Hawk);
				AddShipUpgradePieces(ship, LaharShipUpgradePieces);
			}
			else if (ship.ShipHull.StringId == "northern_medium_ship")
			{
				ship.ChangeFigurehead(DefaultFigureheads.Dragon);
				AddShipUpgradePieces(ship, GunnarShipUpgradePieces);
			}
		}
	}

	private void SetupCorsairParty(MobileParty corsairParty)
	{
		corsairParty.SetPartyUsedByQuest(isActivelyUsed: true);
		AddTrackedObject(corsairParty);
		corsairParty.IsCurrentlyAtSea = true;
		corsairParty.IsVisible = true;
		corsairParty.Party.SetCustomBanner(NavalStorylineData.CorsairBanner);
		foreach (Ship ship in corsairParty.Ships)
		{
			ship.IsInvulnerable = true;
		}
		corsairParty.Ai.SetDoNotMakeNewDecisions(doNotMakeNewDecisions: true);
		corsairParty.Ai.DisableForHours(3);
		corsairParty.IgnoreByOtherPartiesTill(CampaignTime.Never);
		corsairParty.Party.SetVisualAsDirty();
	}

	private void DestroyCorsairParties()
	{
		foreach (MobileParty item in _corsairParties.ToList())
		{
			if (item != null && item.IsActive)
			{
				DestroyPartyAction.Apply(null, item);
			}
		}
		if (_bossCorsairParty != null && _bossCorsairParty.IsActive)
		{
			DestroyPartyAction.Apply(null, _bossCorsairParty);
		}
	}

	private void SpawnStormEntity()
	{
		if (_stormEntity == null)
		{
			MatrixFrame identity = MatrixFrame.Identity;
			Scene scene = ((MapScene)Campaign.Current.MapSceneWrapper).Scene;
			List<GameEntity> entities = new List<GameEntity>();
			identity.origin = new Vec3(_corsairSpawnPosition.X, _corsairSpawnPosition.Y);
			_stormEntity = GameEntity.Instantiate(scene, "psys_mapicon_darkclouds", identity);
			scene.GetAllEntitiesWithScriptComponent<CampaignMapAmbientOccluder>(ref entities);
			for (int i = 0; i < entities.Count; i++)
			{
				entities[i].GetFirstScriptOfType<CampaignMapAmbientOccluder>().RegisterQuestStorm(_stormEntity);
			}
		}
	}

	private void RefreshParty(MobileParty mobileParty, PartyTemplateObject pt)
	{
		MBList<TroopRosterElement> troopRoster = mobileParty.MemberRoster.GetTroopRoster();
		for (int i = 0; i < troopRoster.Count; i++)
		{
			if (troopRoster[i].Character.IsHero)
			{
				troopRoster[i].Character.HeroObject.Heal(troopRoster[i].Character.HeroObject.MaxHitPoints);
			}
			else
			{
				mobileParty.MemberRoster.RemoveTroop(troopRoster[i].Character, troopRoster[i].Number);
			}
		}
		TroopRoster troopRoster2 = Campaign.Current.Models.PartySizeLimitModel.FindAppropriateInitialRosterForMobileParty(mobileParty, pt);
		mobileParty.MemberRoster.Add(troopRoster2);
		RefreshShips(mobileParty, pt);
	}

	private void RefreshShips(MobileParty mobileParty, PartyTemplateObject pt)
	{
		foreach (Ship ship3 in mobileParty.Ships)
		{
			ship3.HitPoints = ship3.MaxHitPoints;
		}
		List<Ship> list = Campaign.Current.Models.PartySizeLimitModel.FindAppropriateInitialShipsForMobileParty(mobileParty, pt);
		if (mobileParty.Ships.Count == list.Count)
		{
			return;
		}
		foreach (Ship ship in mobileParty.Ships)
		{
			Ship ship2 = list.FirstOrDefault((Ship x) => x.ShipHull == ship.ShipHull);
			if (ship2 != null)
			{
				list.Remove(ship2);
			}
		}
		if (list.Count <= 0)
		{
			return;
		}
		foreach (Ship item in list)
		{
			ChangeShipOwnerAction.ApplyByMobilePartyCreation(mobileParty.Party, item);
			if (mobileParty != MobileParty.MainParty)
			{
				item.IsInvulnerable = true;
			}
		}
	}

	private void AddShipUpgradePieces(Ship ship, Dictionary<string, string> upgradePieces)
	{
		foreach (KeyValuePair<string, string> kv in upgradePieces)
		{
			ShipUpgradePiece newUpgradePiece = MBObjectManager.Instance.GetObject<ShipUpgradePiece>(kv.Value);
			if (ship.ShipHull.AvailableSlots.Any((KeyValuePair<string, ShipSlot> slot) => slot.Key == kv.Key))
			{
				ship.EquipUpgradePiece(kv.Key, newUpgradePiece);
			}
		}
	}

	public bool IsFahdaVisible()
	{
		if (_bossCorsairParty != null && _bossCorsairParty.IsActive)
		{
			return _bossCorsairParty.IsVisible;
		}
		return false;
	}
}
