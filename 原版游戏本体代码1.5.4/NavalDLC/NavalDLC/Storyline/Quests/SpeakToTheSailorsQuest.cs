using System;
using System.Linq;
using Helpers;
using NavalDLC.Missions;
using NavalDLC.Storyline.MissionControllers;
using SandBox;
using SandBox.Missions.AgentBehaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace NavalDLC.Storyline.Quests;

public class SpeakToTheSailorsQuest : NavalStorylineQuestBase
{
	public class SpeakToTheSailorsQuestTypeDefiner : SaveableTypeDefiner
	{
		public SpeakToTheSailorsQuestTypeDefiner()
			: base(312250)
		{
		}

		protected override void DefineClassTypes()
		{
		}

		protected override void DefineEnumTypes()
		{
			AddEnumDefinition(typeof(QuestState), 100);
		}
	}

	[Flags]
	private enum QuestState
	{
		None = 0,
		TalkedToSailors = 1,
		BattleStarted = 2,
		BattleWon = 4,
		CheckpointReached = 8,
		HadEncounterWithBjolgor = 0x10
	}

	private const string SeaHoundsTemplateStringId = "storyline_act3_quest_3_sea_hounds_template";

	private const string MerchantsTemplateStringId = "storyline_act3_quest_3_merchants_template";

	private const string InterceptedMenuId = "hounds_3_intercepted";

	private const string EncounterMenuId = "quest3_encounter_invisible_menu";

	private const string BattleScene = "naval_storyline_act_3_quest_3";

	private const string ShipBallistaSlotId = "fore";

	private const string ShipSailSlotId = "sail";

	private const string BurningShipBallistaId = "fore_heavy_ballista_pot";

	private const string ExplosiveShipBallistaId = "fore_heavy_ballista_pot";

	private const string GalleySailId = "sails_lvl2";

	public const string FishingShipId = "burning_fishing_ship";

	public const string BurningTradeCogId = "burning_cog_ship";

	public const string TradeCogId = "ship_trade_cog_q3";

	private PartyTemplateObject _houndsTemplate;

	private PartyTemplateObject _merchantsTemplate;

	[SaveableField(0)]
	private Settlement _settlement;

	[SaveableField(1)]
	private MobileParty _houndsParty;

	private MobileParty _merchantParty;

	[SaveableField(2)]
	private QuestState _state;

	public override TextObject Title
	{
		get
		{
			TextObject textObject = new TextObject("{=ebFg8V9z}Speak to the Sailors in {SETTLEMENT_NAME}");
			textObject.SetTextVariable("SETTLEMENT_NAME", _settlement.Name);
			return textObject;
		}
	}

	protected override string MainPartyTemplateStringId => "storyline_act3_quest_3_main_party_template";

	public override NavalStorylineData.NavalStorylineStage Stage => NavalStorylineData.NavalStorylineStage.Act3SpeakToSailors;

	public override bool WillProgressStoryline => true;

	public SpeakToTheSailorsQuest(string questId, Settlement targetSettlement)
		: base(questId, NavalStorylineData.Gunnar, CampaignTime.Never, 0)
	{
		_settlement = targetSettlement;
	}

	protected override void InitializeQuestOnGameLoadInternal()
	{
		InitializeTemplates();
		SetDialogs();
		AddGameMenus();
	}

	protected override void SetDialogs()
	{
		AddTalkToGangradirDialogue();
		AddBjolgurDialogs();
		AddBjolgurSecondConversationDialogs();
		AddGunnarHorsebackDialogs();
		AddBjolgurDialogsEndBattle();
	}

	protected override void OnStartQuestInternal()
	{
		InitializeTemplates();
		SetDialogs();
		AddGameMenus();
		TextObject textObject = new TextObject("{=ZDDXZcMW}Gunnar has learned that the Sea Hounds will be targeting a ship that sails from the estuary near {SETTLEMENT_LINK}, bringing Sturgian silver to the Skolderbroda.");
		textObject.SetTextVariable("SETTLEMENT_LINK", _settlement.EncyclopediaLinkWithName);
		NavalStorylineData.Bjolgur.ChangeState(Hero.CharacterStates.Active);
		TeleportHeroAction.ApplyImmediateTeleportToSettlement(NavalStorylineData.Bjolgur, _settlement);
		AddLog(textObject);
		AddTrackedObject(_settlement);
		AddTrackedObject(NavalStorylineData.Bjolgur);
	}

	private void InitializeTemplates()
	{
		_houndsTemplate = Campaign.Current.ObjectManager.GetObject<PartyTemplateObject>("storyline_act3_quest_3_sea_hounds_template");
		_merchantsTemplate = Campaign.Current.ObjectManager.GetObject<PartyTemplateObject>("storyline_act3_quest_3_merchants_template");
	}

	protected override void RegisterEventsInternal()
	{
		CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, OnSettlementLeft);
		CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
		CampaignEvents.OnMissionEndedEvent.AddNonSerializedListener(this, OnMissionEnded);
		CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, OnGameMenuOpened);
	}

	private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
	{
		if (party == MobileParty.MainParty && settlement == NavalStorylineData.Act3Quest3TargetSettlement && !HadEncounterWithBjolgur())
		{
			StartConversationOnSettlementEntered();
		}
	}

	private void OnGameMenuOpened(MenuCallbackArgs args)
	{
		if (args.MenuContext.GameMenu.StringId == "naval_storyline_virtualport" && base.IsOngoing && Settlement.CurrentSettlement == _settlement)
		{
			if (!HasTalkedToSailors())
			{
				TextObject textObject = new TextObject("{=4PUz4yQv}You have arrived in {SETTLEMENT_LINK}. As you sail up the estuary into the harbor, you spot several large ships at anchor in a cove. They look like Vlandian craft, probably the pirates that Fahda told you about. They do not try to give chase, however, possibly because they saw you too late to raise sail, or perhaps because they are lying in wait for more lucrative prey.");
				textObject.SetTextVariable("SETTLEMENT_LINK", _settlement.EncyclopediaLinkWithName);
				MBTextManager.SetTextVariable("VIRTUAL_PORT_TEXT", textObject);
			}
			else
			{
				MobileParty.MainParty.SetSailAtPosition(Settlement.CurrentSettlement.PortPosition);
				PlayerEncounter.Finish();
			}
		}
	}

	private void OnMissionEnded(IMission mission)
	{
		if (PlayerEncounter.Current == null || PlayerEncounter.EncounteredParty != _houndsParty?.Party)
		{
			return;
		}
		if (PlayerEncounter.CampaignBattleResult != null && PlayerEncounter.CampaignBattleResult.BattleResolved)
		{
			if (!PlayerEncounter.CampaignBattleResult.PlayerDefeat && PlayerEncounter.CampaignBattleResult.PlayerVictory)
			{
				AddLog(new TextObject("{=bWqvK0iY}You were able to run the Sea Hound blockade."));
				AddState(QuestState.BattleWon);
			}
		}
		else if (PlayerEncounter.WinningSide != BattleSideEnum.None)
		{
			Debug.FailedAssert("unhandled case", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC\\Storyline\\Quests\\SpeakToTheSailorsQuest.cs", "OnMissionEnded", 213);
		}
	}

	private void OnSettlementLeft(MobileParty party, Settlement settlement)
	{
		if (party.IsMainParty && HasTalkedToSailors() && NavalStorylineData.IsNavalStoryLineActive() && !HasBattleStarted() && MobileParty.MainParty.IsCurrentlyAtSea)
		{
			GameMenu.ActivateGameMenu("hounds_3_intercepted");
		}
	}

	private void AddGameMenus()
	{
		AddGameMenu("hounds_3_intercepted", new TextObject("{=lbLABNVY}You row out of {SETTLEMENT_LINK} harbor, with the Sturgian merchantmen following close behind you, and make your way toward the sea. But as you reach the estuary mouth, you see several ominous squat shapes blocking your passage to the open sea. Clearly it is the Sea Hounds, and you will either have to defeat them or hold them off long enough for your allies to make good their escape."), intercepted_menu_on_init);
		AddGameMenuOption("hounds_3_intercepted", "continue", new TextObject("{=1r0tDsrR}Attack!"), intercepted_menu_on_condition, intercepted_menu_on_consequence);
		AddGameMenu("quest3_encounter_invisible_menu", new TextObject("{=!}{RETRY_DESC}"), quest3_encounter_invisible_menu_on_init);
		AddGameMenuOption("quest3_encounter_invisible_menu", "retry", new TextObject("{=YHMDy3lQ}Try again"), on_retry_condition, on_retry_consequence);
		AddGameMenuOption("quest3_encounter_invisible_menu", "retry_checkpoint", new TextObject("{=rHlzkNFL}Try again from checkpoint"), on_retry_from_checkpoint_condition, on_retry_from_checkpoint_consequence);
		AddGameMenuOption("quest3_encounter_invisible_menu", "leave", new TextObject("{=3sRdGQou}Leave"), on_leave_condition, on_leave_consequence, Isleave: true);
	}

	private void StartConversationOnSettlementEntered()
	{
		PlayerEncounter.LocationEncounter.CreateAndOpenMissionController(LocationComplex.Current.GetLocationWithId("port"), null, NavalStorylineData.Bjolgur.CharacterObject);
	}

	private void on_leave_consequence(MenuCallbackArgs args)
	{
		CompleteQuestWithCancel();
	}

	private bool on_leave_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Leave;
		return HasBattleStarted();
	}

	private bool on_retry_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Mission;
		if (HasBattleStarted())
		{
			return !CheckPointReached();
		}
		return false;
	}

	private bool on_retry_from_checkpoint_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Mission;
		if (HasBattleStarted())
		{
			return CheckPointReached();
		}
		return false;
	}

	private void on_retry_consequence(MenuCallbackArgs args)
	{
		StartBattle(fromCheckPoint: false);
	}

	private void on_retry_from_checkpoint_consequence(MenuCallbackArgs args)
	{
		StartBattle(fromCheckPoint: true);
	}

	private void quest3_encounter_invisible_menu_on_init(MenuCallbackArgs args)
	{
		MBTextManager.SetTextVariable("RETRY_DESC", new TextObject("{=etH1IHNZ}You manage to put some distance between you and your enemies, and you have a moment to consider how to proceed."));
		DestroyParty(ref _merchantParty);
		if (!HasBattleWon())
		{
			RefreshParty(_houndsParty, _houndsTemplate);
			RefreshParty(MobileParty.MainParty, base.Template);
			AddBurningTradeShipsToParties();
		}
		if (base.IsOngoing)
		{
			if (NavalStorylineData.IsNavalStoryLineActive() && HasBattleWon())
			{
				TalkToBjolgur();
			}
			else if (!HasBattleStarted())
			{
				StartBattle(fromCheckPoint: false);
			}
		}
		else
		{
			GameMenu.ExitToLast();
		}
		NavalStorylineData.OnCheckpointReached(NavalStorylineData.NavalStorylineCheckpoint.Act3Quest3EncounterMenu);
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
		HealShips(mobileParty);
	}

	private void HealShips(MobileParty mobileParty)
	{
		foreach (Ship ship in mobileParty.Ships)
		{
			ship.HitPoints = ship.MaxHitPoints;
		}
	}

	private void intercepted_menu_on_init(MenuCallbackArgs args)
	{
		MBTextManager.SetTextVariable("SETTLEMENT_LINK", _settlement.EncyclopediaLinkWithName);
		if (_houndsParty == null)
		{
			CreateHoundsParty();
		}
	}

	[GameMenuInitializationHandler("hounds_3_intercepted")]
	private static void intercepted_menu_background_on_init(MenuCallbackArgs args)
	{
		Settlement settlement = Settlement.CurrentSettlement ?? MobileParty.MainParty.LastVisitedSettlement;
		args.MenuContext.SetBackgroundMeshName(settlement.Culture.StringId + "_port");
	}

	[GameMenuInitializationHandler("quest3_encounter_invisible_menu")]
	private static void encounter_menu_background_on_init(MenuCallbackArgs args)
	{
		args.MenuContext.SetBackgroundMeshName("encounter_naval");
	}

	private void intercepted_menu_on_consequence(MenuCallbackArgs args)
	{
		GameMenu.ActivateGameMenu("quest3_encounter_invisible_menu");
	}

	private void AddBurningTradeShipsToParties()
	{
		ShipHull tradeCogHull = MBObjectManager.Instance.GetObject<ShipHull>("burning_cog_ship");
		ShipHull normalCogHull = MBObjectManager.Instance.GetObject<ShipHull>("ship_trade_cog_q3");
		ShipHull fishingShipHull = MBObjectManager.Instance.GetObject<ShipHull>("burning_fishing_ship");
		if (!MobileParty.MainParty.Ships.Any((Ship x) => x.ShipHull == normalCogHull))
		{
			Ship ship = new Ship(normalCogHull);
			ChangeShipOwnerAction.ApplyByLooting(PartyBase.MainParty, ship);
		}
		if (!MobileParty.MainParty.Ships.Any((Ship x) => x.ShipHull == fishingShipHull))
		{
			Ship ship2 = new Ship(fishingShipHull);
			ChangeShipOwnerAction.ApplyByLooting(PartyBase.MainParty, ship2);
		}
		if (!_houndsParty.Ships.Any((Ship x) => x.ShipHull == tradeCogHull))
		{
			Ship ship3 = new Ship(tradeCogHull);
			ship3.EquipUpgradePiece("fore", MBObjectManager.Instance.GetObject<ShipUpgradePiece>("fore_heavy_ballista_pot"));
			ChangeShipOwnerAction.ApplyByLooting(_houndsParty.Party, ship3);
		}
	}

	private bool intercepted_menu_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Continue;
		return true;
	}

	protected override void HourlyTick()
	{
	}

	protected override void OnFinalizeInternal()
	{
		if (IsTracked(_settlement))
		{
			RemoveTrackedObject(_settlement);
		}
		bool num = PlayerEncounter.EncounteredParty != null && PlayerEncounter.EncounteredParty == _houndsParty?.Party;
		DestroyParty(ref _houndsParty);
		DestroyParty(ref _merchantParty);
		if (NavalStorylineData.Bjolgur.IsActive)
		{
			RemoveHero(NavalStorylineData.Bjolgur);
		}
		if (num)
		{
			PlayerEncounter.Finish();
		}
		for (int num2 = MobileParty.MainParty.Ships.Count - 1; num2 >= 0; num2--)
		{
			if (MobileParty.MainParty.Ships[num2].ShipHull.StringId == "burning_fishing_ship")
			{
				DestroyShipAction.Apply(MobileParty.MainParty.Ships[num2]);
			}
		}
	}

	protected override void OnCompleteWithSuccessInternal()
	{
		GainRenownAction.Apply(Hero.MainHero, 10f);
		NavalStorylineData.OnCheckpointReached(NavalStorylineData.NavalStorylineCheckpoint.Act3Quest3Succeeded);
	}

	protected override void IsNavalQuestPartyInternal(PartyBase party, NavalStorylinePartyData data)
	{
		if (party == _houndsParty?.Party)
		{
			data.PartySize = (int)NavalDLCHelpers.GetMaxPartySizeLimitFromTemplate(_houndsTemplate).ResultNumber;
			data.Template = _houndsTemplate;
			data.IsQuestParty = true;
		}
		else if (party == _merchantParty?.Party)
		{
			data.PartySize = (int)NavalDLCHelpers.GetMaxPartySizeLimitFromTemplate(_merchantsTemplate).ResultNumber;
			data.Template = _merchantsTemplate;
			data.IsQuestParty = true;
		}
	}

	private void AddTalkToGangradirDialogue()
	{
		Campaign.Current.ConversationManager.AddDialogFlow(DialogFlow.CreateDialogFlow("start", 250).NpcLine("{=O0qBJmSS}Talk with Bjolgur when you're ready to depart.").Condition(() => base.IsOngoing && HadEncounterWithBjolgur() && Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero == NavalStorylineData.Gunnar && !HasTalkedToSailors())
			.CloseDialog(), this);
	}

	private void AddBjolgurSecondConversationDialogs()
	{
		Campaign.Current.ConversationManager.AddDialogFlow(DialogFlow.CreateDialogFlow("start", 1300).NpcLine("{=GkaEhSwJ}{PLAYER.NAME}...").Condition(() => base.IsOngoing && HadEncounterWithBjolgur() && Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero == NavalStorylineData.Bjolgur && !HasTalkedToSailors())
			.NpcLine("{=zNaWTBin}Are you ready to take command of the fireship and break the blockade?")
			.BeginPlayerOptions()
			.PlayerOption("{=anANUCFV}I am as ready as I will ever be, I suppose.")
			.Consequence(OnTalkedToSailors)
			.CloseDialog()
			.PlayerOption("{=6c2bHHHj}No, not yet.")
			.CloseDialog(), this);
	}

	private void AddBjolgurDialogs()
	{
		Campaign.Current.ConversationManager.AddDialogFlow(DialogFlow.CreateDialogFlow("start", 1300).NpcLine("{=J6QLFwbb}[if:convo_delighted][ib:hip]Welcome to {SETTLEMENT_LINK}, friend. Is that grizzled fellow with you, coming up now, is that my old comrade Gunnar of Lagshofn? A bit greyer than I remember from the days when we stood together in the shield wall facing Volbjorn's host, but, well, aren't we all…").Condition(delegate
		{
			MBTextManager.SetTextVariable("SETTLEMENT_LINK", NavalStorylineData.Act3Quest3TargetSettlement.EncyclopediaLinkWithName);
			int num;
			if (base.IsOngoing && !HadEncounterWithBjolgur() && Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero == NavalStorylineData.Bjolgur)
			{
				num = ((!HasTalkedToSailors()) ? 1 : 0);
				if (num != 0)
				{
					Agent agent = Mission.Current.Agents.FirstOrDefault((Agent x) => x.Character == NavalStorylineData.Gunnar.CharacterObject);
					if (!Campaign.Current.ConversationManager.ConversationAgents.Contains(agent))
					{
						AddGunnarToConversation(isAgentSpawned: true);
					}
					agent.TeleportToPosition(GetGunnarTeleportPosition());
				}
			}
			else
			{
				num = 0;
			}
			return (byte)num != 0;
		})
			.Consequence(delegate
			{
				AddState(QuestState.HadEncounterWithBjolgor);
			})
			.NpcLine("{=KYqqVZh1}[ib:confident]We received his letter a while back, about your run-in with Purig. Hah! That worm must have cursed like an old woman when he learned that his captives stole his ship. You two are making quite a name for yourselves.")
			.NpcLine("{=4bsY9noo}[if:convo_delighted][ib:confident3]Bjolgur of Gauksdal! Well met! Are the Skolderbroda working for the merchants of {SETTLEMENT_LINK} now?", IsGunnar, IsBjolgur)
			.Condition(delegate
			{
				MBTextManager.SetTextVariable("SETTLEMENT_LINK", NavalStorylineData.Act3Quest3TargetSettlement.EncyclopediaLinkWithName);
				return true;
			})
			.NpcLine("{=lTjvOdoX}[ib:closed]Not yet. As you know, our brotherhood does not fight before it's paid.", IsBjolgur, IsGunnar)
			.NpcLine("{=iSKIBXnj}See, the {SETTLEMENT_LINK} merchants promised us a hoard of silver to protect their ships from the Sea Hounds, but it never arrived. I was sent down to learn what was going on, and I find the silver just sitting here, loaded onto a ship in the harbor, and the Sturgians are burning through it paying their men double wages not to run off. Some Vlandian pirates were sighted in the estuary, and the Sturgians refuse to venture out.", IsBjolgur, IsMainAgent)
			.Condition(delegate
			{
				MBTextManager.SetTextVariable("SETTLEMENT_LINK", NavalStorylineData.Act3Quest3TargetSettlement.EncyclopediaLinkWithName);
				return true;
			})
			.GenerateToken(out var token)
			.BeginPlayerOptions()
			.PlayerOption("{=325GxBag}With so much wealth at stake, the Sturgians are right to be cautious.")
			.GotoDialogState(token)
			.PlayerOption("{=2YEmSZq1}Pirates are scum. Let's just sail out and crush them.")
			.GotoDialogState(token)
			.EndPlayerOptions()
			.NpcLine("{=kbug6MQB}[ib:confident2]Much as I would like to simply sail forth and bathe my sword in Sea Hound blood, my brotherhood has commanded me to do my best to ensure that the silver gets through safely.", IsBjolgur, IsMainAgent, token)
			.NpcLine("{=rlpVWadN}[ib:normal2][if:convo_thinking]Listen. I've been watching these Vlandian blockaders, and mulling over a plan. Their flagship has a lofty deck and it would be hard to board, but it doesn't seem very maneuverable. I think we can hit them with a trick that can be deadly in estuaries.", IsBjolgur, IsMainAgent)
			.NpcLine("{=K3B52zD6}We will be upstream of them. I'll have the merchants here donate some leaky old vessel that they are about to scrap. We load it up with oil and pitch. Then we steer it towards the pirates, throw a torch in the hull, and jump.", IsBjolgur, IsMainAgent)
			.NpcLine("{=8PmocyQy}[ib:normal]Good, very good. With luck, the current shall carry it right into them, and they shall all merrily blaze up like a bonfire at a midwinter feast. The silver ship will make for the open sea, while the rest of us can have it out with any surviving Sea Hounds.", IsGunnar, IsBjolgur)
			.NpcLine("{=867iaibq}[ib:closed2][if:convo_relaxed_happy]Listen, though… We need someone to steer the fireship. I'd do it myself, but my order wants me to stay close to the silver. I'd found a few volunteers who've offered to do it, but they keep sobering up.", IsBjolgur, IsMainAgent)
			.BeginPlayerOptions()
			.PlayerOption("{=ybDSa8Xr}I'll steer the fireship. Let us sail forth.")
			.Consequence(OnTalkedToSailors)
			.CloseDialog()
			.PlayerOption("{=brMsnacx}I need a little while here in port first.")
			.CloseDialog()
			.EndPlayerOptions()
			.CloseDialog(), this);
	}

	private void AddGunnarHorsebackDialogs()
	{
		Campaign.Current.ConversationManager.AddDialogFlow(DialogFlow.CreateDialogFlow("start", 1300).NpcLine("{=GkaEhSwJ}{PLAYER.NAME}...").Condition(gunnar_horseback_dialog_on_condition)
			.NpcLine("{=ypTUg9xC}There may be some Hound patrols about. Keep a wary eye.")
			.Consequence(delegate
			{
				Campaign.Current.ConversationManager.ConversationEndOneShot += gunnar_horseback_dialog_on_consequence;
			})
			.CloseDialog(), this);
	}

	private bool gunnar_horseback_dialog_on_condition()
	{
		StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter);
		if (base.IsOngoing && Mission.Current != null && Hero.OneToOneConversationHero == NavalStorylineData.Gunnar)
		{
			BlockedEstuaryMissionController missionBehavior = Mission.Current.GetMissionBehavior<BlockedEstuaryMissionController>();
			if (missionBehavior != null && missionBehavior.CurrentPhase == BlockedEstuaryMissionController.BattlePhase.Phase2)
			{
				return true;
			}
		}
		return false;
	}

	private void gunnar_horseback_dialog_on_consequence()
	{
		Mission.Current.GetMissionBehavior<BlockedEstuaryMissionController>().OnTalkedToGunnarPhase2();
	}

	private void AddBjolgurDialogsEndBattle()
	{
		Campaign.Current.ConversationManager.AddDialogFlow(DialogFlow.CreateDialogFlow("start", 1300).NpcLine("{=8OtmPWCK}[ib:hip][if:convo_delighted]So! {PLAYER.NAME}... You did well with that fireship! The silver is on its way to my order, and that bastard Purig will no doubt be much discomfitted. You helped me out there, so let me see if I can now help you.", IsBjolgur, IsMainAgent).Condition(MultiAgentConversationCondition)
			.NpcLine("{=5GMbKn4x}[ib:confident][if:convo_nonchalant]Just before I set sail for {SETTLEMENT_LINK}, my brothers and I had a visitor, a merchant named Salautas Crusas who said he was acting as an “ambassador” for Purig. He wanted us to break our contract with Balgard and ally with the Sea Hounds instead. He offered a great deal of money, too, and more - we could share in Purig's grand plan of conquest.", IsBjolgur, IsMainAgent)
			.Condition(delegate
			{
				MBTextManager.SetTextVariable("SETTLEMENT_LINK", NavalStorylineData.Act3Quest3TargetSettlement.EncyclopediaLinkWithName);
				return true;
			})
			.GenerateToken(out var token)
			.GenerateToken(out var token2)
			.GenerateToken(out var token3)
			.GenerateToken(out var token4)
			.GenerateToken(out var token5)
			.GenerateToken(out var token6)
			.BeginPlayerOptions()
			.PlayerOption("{=0EVkbp01}What grand plans?", IsBjolgur)
			.GotoDialogState(token)
			.PlayerOption("{=jce9rAAu}I'm not interested in Purig's lies, just how to find him.", IsBjolgur)
			.GotoDialogState(token2)
			.EndPlayerOptions()
			.NpcLine("{=n4bIAwNN}[ib:closed]Well, first we would join the Sea Hounds in ravaging the coasts of Sturgia and Vlandia, so that no ship would dare sail on the Byalic Sea without paying us our due. Then Purig would raise an army out of the king's old enemies and take the Nordvyg, and crown himself in Thronderlag, and shower upon us lands, and titles, and anything else we might want.", IsBjolgur, IsMainAgent, token)
			.NpcLine("{=2oEhDTjU}[if:convo_grave]Well, some of the brothers listened to him, men who had fought against Volbjorn to whom a fine meal of wealth seasoned with revenge sounded rather tasty. But the rest of us… We'd heard such promises before, and we had no wish to serve any king. Better to fight for gold… and if you want the gold to flow, you honor your contracts, even if some fancy Calradian merchant comes along offering you the riches of the seven seas.", IsBjolgur, IsMainAgent, null, token3)
			.NpcLine("{=3mxtyo2y}[if:convo_normal][ib:normal]Here's the detail that would interest you…. In addition to all the other delights that Crusas dangled before us, he also offered to build us ships. Purig was going to construct them in some northern anchorage called Angranfjord, where he had brought a large number of captives to work in a shipyard.", IsBjolgur, IsMainAgent, token2, token3)
			.NpcLine("{=GlV3EsEv}[ib:closed]This must be the slave colony that Fahda mentioned. Pirates value safe havens to build new ships. With an anchorage like that, Purig can have the Sea Hounds out of his hands.", IsGunnar, IsMainAgent, token3, token4)
			.NpcLine("{=v2664Qeo}...", IsGunnar, IsMainAgent, token4)
			.BeginPlayerOptions()
			.PlayerOption("{=WtODG7Mc}Bjolgur... you've known this for some time, you say?", IsBjolgur)
			.GotoDialogState(token5)
			.PlayerOption("{=X14bPFvN}Why didn't you tell us this before the battle?", IsBjolgur)
			.GotoDialogState(token6)
			.EndPlayerOptions()
			.NpcLine("{=7UNOf0DZ}[ib:confident][if:convo_nonchalant]Come now, I couldn't have you dash off to hunt Crusas before the silver got past the Sea Hounds. My brothers named me their emissary, you see, and we diplomats need to be crafty.", IsBjolgur, IsMainAgent, token5)
			.PlayerLine("{=l8Rbjazw}It sounds as though, if we find Crusas, we can find Purig.", IsBjolgur)
			.NpcLine("{=vhr55efV}So… I need to get this silver safely to harbor, but after that, I shall request permission from my order to fit out a ship and sail to Ostican to join your hunt. I'm not saying I owe you anything, mind you - but those bastards did try to take our money, and all Crusas' talk about gold and riches made me think that I wouldn't mind taking one of his ships and having a rummage through his holds.", IsBjolgur, IsMainAgent)
			.PlayerLine("{=JEpBDamz}We are grateful for your help. We shall meet you back in Ostican.", IsBjolgur)
			.NpcLine("{=Sl45Pmxg}[ib:hip]I shall see you shortly in Ostican, then.", IsBjolgur, IsMainAgent)
			.Consequence(delegate
			{
				Campaign.Current.ConversationManager.ConversationEndOneShot += FinishQuest;
			})
			.CloseDialog()
			.NpcLine("{=7UNOf0DZ}[ib:confident][if:convo_nonchalant]Come now, I couldn't have you dash off to hunt Crusas before the silver got past the Sea Hounds. My brothers named me their emissary, you see, and we diplomats need to be crafty.", IsBjolgur, IsMainAgent, token6)
			.PlayerLine("{=U9e7WbOS}I piloted a fireship. I think you owe us more than just information.", IsBjolgur)
			.NpcLine("{=vhr55efV}So… I need to get this silver safely to harbor, but after that, I shall request permission from my order to fit out a ship and sail to Ostican to join your hunt. I'm not saying I owe you anything, mind you - but those bastards did try to take our money, and all Crusas' talk about gold and riches made me think that I wouldn't mind taking one of his ships and having a rummage through his holds.", IsBjolgur, IsMainAgent)
			.PlayerLine("{=8zxLaxKn}You'll get your share of Crusas' ill-gained wealth, never fear.", IsBjolgur)
			.NpcLine("{=Sl45Pmxg}[ib:hip]I shall see you shortly in Ostican, then.", IsBjolgur, IsMainAgent)
			.Consequence(delegate
			{
				Campaign.Current.ConversationManager.ConversationEndOneShot += FinishQuest;
			})
			.CloseDialog()
			.CloseDialog(), this);
	}

	private void TalkToBjolgur()
	{
		Campaign.Current.CampaignMissionManager.OpenConversationMission(new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty, noHorse: true, noWeapon: true, spawnAfterFight: false, isCivilianEquipmentRequiredForLeader: false, isCivilianEquipmentRequiredForBodyGuardCharacters: false, noBodyguards: true), new ConversationCharacterData(NavalStorylineData.Bjolgur.CharacterObject, PartyBase.MainParty, noHorse: true, noWeapon: true, spawnAfterFight: false, isCivilianEquipmentRequiredForLeader: false, isCivilianEquipmentRequiredForBodyGuardCharacters: false, noBodyguards: true), "conversation_scene_sea_multi_agent", "", isMultiAgentConversation: true);
	}

	private bool MultiAgentConversationCondition()
	{
		if (base.IsOngoing && Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero == NavalStorylineData.Bjolgur && HasBattleWon() && HasTalkedToSailors())
		{
			AddGunnarToConversation(isAgentSpawned: false);
			StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter);
			return true;
		}
		return false;
	}

	private Vec3 GetGunnarTeleportPosition()
	{
		Vec3 position = Mission.Current.GetRandomPositionAroundPoint(Agent.Main.Position + Agent.Main.LookRotation.s * 3f, 1f, 1.5f);
		int num = 20;
		while (Mission.Current.Scene.GetNavigationMeshForPosition(in position) == UIntPtr.Zero && num > 0)
		{
			position = Mission.Current.GetRandomPositionAroundPoint(Agent.Main.Position + Agent.Main.LookRotation.s * 3f, 1f, 1.5f);
			num--;
		}
		return position;
	}

	private void AddGunnarToConversation(bool isAgentSpawned)
	{
		Agent item;
		if (!isAgentSpawned)
		{
			AgentBuildData agentBuildData = new AgentBuildData(NavalStorylineData.Gunnar.CharacterObject);
			agentBuildData.TroopOrigin(new SimpleAgentOrigin(agentBuildData.AgentCharacter));
			agentBuildData.InitialPosition(Mission.Current.Scene.FindEntityWithName("free_infantry_spawn_point_0").GlobalPosition);
			agentBuildData.InitialDirection(Agent.Main.LookDirection.AsVec2.Normalized());
			agentBuildData.NoHorses(noHorses: true);
			item = Mission.Current.SpawnAgent(agentBuildData);
		}
		else
		{
			item = Mission.Current.Agents.FirstOrDefault((Agent x) => IsGunnar(x));
			RemoveWalkingBehavior(NavalStorylineData.Gunnar.CharacterObject);
			RemoveWalkingBehavior(NavalStorylineData.Bjolgur.CharacterObject);
		}
		Campaign.Current.ConversationManager.AddConversationAgents(new MBList<IAgent> { item }, setActionsInstantly: true);
	}

	private void RemoveWalkingBehavior(CharacterObject character)
	{
		Agent agent = Mission.Current.Agents.FirstOrDefault((Agent x) => x.Character == character);
		CampaignAgentComponent component = agent.GetComponent<CampaignAgentComponent>();
		agent.ClearTargetFrame();
		component.AgentNavigator?.GetBehaviorGroup<DailyBehaviorGroup>()?.RemoveBehavior<WalkingBehavior>();
	}

	private void FinishQuest()
	{
		CompleteQuestWithSuccess();
	}

	private bool IsGunnar(IAgent agent)
	{
		return agent.Character == NavalStorylineData.Gunnar.CharacterObject;
	}

	private bool IsBjolgur(IAgent agent)
	{
		return agent.Character == NavalStorylineData.Bjolgur.CharacterObject;
	}

	private bool IsMainAgent(IAgent agent)
	{
		return agent == Agent.Main;
	}

	private void RemoveHero(Hero hero)
	{
		hero.ChangeState(Hero.CharacterStates.Disabled);
		LocationComplex.Current?.RemoveCharacterIfExists(hero);
		LeaveSettlementAction.ApplyForCharacterOnly(hero);
	}

	private void OnTalkedToSailors()
	{
		AddState(QuestState.TalkedToSailors);
		TextObject textObject = new TextObject("{=FOQ5YOWH}You talked to {HERO.NAME}, and agreed to pilot a fireship and help the Sturgians run the Sea Hound blockade.");
		textObject.SetCharacterProperties("HERO", NavalStorylineData.Bjolgur.CharacterObject);
		AddLog(textObject);
		Campaign.Current.ConversationManager.ConversationEndOneShot += delegate
		{
			RemoveHero(NavalStorylineData.Bjolgur);
			Mission.Current.EndMission();
		};
	}

	private void CreateHoundsParty()
	{
		CampaignVec2 position = NavigationHelper.FindPointAroundPosition(MobileParty.MainParty.Position, MobileParty.NavigationType.Naval, 3f, 1f);
		TextObject name = new TextObject("{=27QTvW27}Vlandian Pirates");
		_houndsParty = CustomPartyComponent.CreateCustomPartyWithPartyTemplate(position, 1f, _settlement, name, Clan.FindFirst((Clan x) => x.StringId == "northern_pirates"), _houndsTemplate, null);
		_houndsParty.SetPartyUsedByQuest(isActivelyUsed: true);
		_houndsParty.IsInfoHidden = true;
		_houndsParty.IgnoreByOtherPartiesTill(CampaignTime.Never);
		_houndsParty.Party.SetCustomBanner(NavalStorylineData.CorsairBanner);
		ShipUpgradePiece newUpgradePiece = MBObjectManager.Instance.GetObject<ShipUpgradePiece>("fore_heavy_ballista_pot");
		ShipUpgradePiece newUpgradePiece2 = MBObjectManager.Instance.GetObject<ShipUpgradePiece>("sails_lvl2");
		ShipUpgradePiece newUpgradePiece3 = MBObjectManager.Instance.GetObject<ShipUpgradePiece>("fore_heavy_ballista_pot");
		foreach (Ship ship in _houndsParty.Ships)
		{
			if (ship.HasSlot("fore"))
			{
				if (ship.ShipHull.StringId == "burning_cog_ship")
				{
					ship.EquipUpgradePiece("fore", newUpgradePiece3);
				}
				else
				{
					ship.EquipUpgradePiece("fore", newUpgradePiece);
				}
			}
			if (ship.HasSlot("sail") && ship.ShipHull.StringId != "burning_cog_ship")
			{
				ship.EquipUpgradePiece("sail", newUpgradePiece2);
			}
		}
	}

	private void CreateMerchantsParty()
	{
		CampaignVec2 position = NavigationHelper.FindPointAroundPosition(MobileParty.MainParty.Position, MobileParty.NavigationType.Naval, 3f, 1f);
		TextObject name = new TextObject("{=CElcGl2R}Sturgian Merchants");
		_merchantParty = CustomPartyComponent.CreateCustomPartyWithPartyTemplate(position, 3f, _settlement, name, _settlement.OwnerClan, _merchantsTemplate, null);
		_merchantParty.SetPartyUsedByQuest(isActivelyUsed: true);
		_merchantParty.IgnoreByOtherPartiesTill(CampaignTime.Never);
	}

	private void StartBattle(bool fromCheckPoint)
	{
		AddState(QuestState.BattleStarted);
		if (PartyBase.MainParty.MapEventSide == null)
		{
			PlayerEncounter.Start();
			PlayerEncounter.Current.SetupFields(_houndsParty.Party, PartyBase.MainParty);
			PlayerEncounter.StartBattle();
		}
		CreateMerchantsParty();
		_merchantParty.MapEventSide = PartyBase.MainParty.MapEventSide;
		NavalMissions.OpenBlockedEstuaryMission(GetMissionInitializerRecord(), _houndsParty, fromCheckPoint);
	}

	public void OnCheckPointReached()
	{
		AddState(QuestState.CheckpointReached);
	}

	private MissionInitializerRecord GetMissionInitializerRecord()
	{
		MissionInitializerRecord navalMissionInitializerTemplate = NavalStorylineData.GetNavalMissionInitializerTemplate("naval_storyline_act_3_quest_3");
		TerrainType faceTerrainType = Campaign.Current.MapSceneWrapper.GetFaceTerrainType(MobileParty.MainParty.CurrentNavigationFace);
		navalMissionInitializerTemplate.TerrainType = (int)faceTerrainType;
		navalMissionInitializerTemplate.NeedsRandomTerrain = false;
		navalMissionInitializerTemplate.PlayingInCampaignMode = true;
		navalMissionInitializerTemplate.RandomTerrainSeed = MBRandom.RandomInt(10000);
		navalMissionInitializerTemplate.AtmosphereOnCampaign = Campaign.Current.Models.MapWeatherModel.GetAtmosphereModel(MobileParty.MainParty.Position);
		navalMissionInitializerTemplate.SceneHasMapPatch = false;
		navalMissionInitializerTemplate.AtmosphereOnCampaign.NauticalInfo.UsesNavalSimulatedWater = 1;
		return navalMissionInitializerTemplate;
	}

	private void DestroyParty(ref MobileParty mobileParty)
	{
		if (mobileParty != null && mobileParty.IsActive)
		{
			if (mobileParty.MapEventSide != null)
			{
				mobileParty.MapEventSide = null;
			}
			DestroyPartyAction.Apply(null, mobileParty);
			mobileParty = null;
		}
	}

	private bool HasTalkedToSailors()
	{
		return (_state & QuestState.TalkedToSailors) == QuestState.TalkedToSailors;
	}

	private bool HasBattleStarted()
	{
		return (_state & QuestState.BattleStarted) == QuestState.BattleStarted;
	}

	private bool HasBattleWon()
	{
		return (_state & QuestState.BattleWon) == QuestState.BattleWon;
	}

	private bool CheckPointReached()
	{
		return (_state & QuestState.CheckpointReached) == QuestState.CheckpointReached;
	}

	private bool HadEncounterWithBjolgur()
	{
		return (_state & QuestState.HadEncounterWithBjolgor) == QuestState.HadEncounterWithBjolgor;
	}

	private void AddState(QuestState state)
	{
		_state |= state;
	}
}
