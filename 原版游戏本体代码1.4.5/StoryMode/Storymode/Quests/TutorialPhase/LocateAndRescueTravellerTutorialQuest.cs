using System.Collections.Generic;
using System.Linq;
using Helpers;
using StoryMode.StoryModeObjects;
using StoryMode.StoryModePhases;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace StoryMode.Quests.TutorialPhase;

public class LocateAndRescueTravellerTutorialQuest : StoryModeQuestBase
{
	private const int MainPartyHealHitPointLimit = 50;

	private const int PlayerPartySizeMinLimitToSpawnRaiders = 4;

	private const int RaiderPartySize = 6;

	private const int RaiderPartyCount = 3;

	private const string RaiderPartyStringId = "locate_and_rescue_traveller_quest_raider_party_";

	[SaveableField(1)]
	private int _raiderPartyCount;

	[SaveableField(2)]
	private readonly List<MobileParty> _raiderParties;

	[SaveableField(3)]
	private int _defeatedRaiderPartyCount;

	[SaveableField(4)]
	private readonly JournalLog _startQuestLog;

	private TextObject _startQuestLogText => new TextObject("{=JJo0i8an}Look around the village to find the party that captured the traveller whom the headman told you about.");

	public override TextObject Title => new TextObject("{=ACyYhA2s}Locate and Rescue Traveller");

	public LocateAndRescueTravellerTutorialQuest()
		: base("locate_and_rescue_traveler_tutorial_quest", null, CampaignTime.Never)
	{
		_raiderParties = new List<MobileParty>();
		_defeatedRaiderPartyCount = 0;
		SetDialogs();
		AddGameMenus();
		InitializeQuestOnCreation();
		_raiderPartyCount = 0;
		_startQuestLog = AddDiscreteLog(_startQuestLogText, new TextObject("{=UkNUuyr1}Defeated Parties"), _defeatedRaiderPartyCount, 3);
		if (MobileParty.MainParty.MemberRoster.TotalManCount >= 4)
		{
			SpawnRaiderParties();
		}
		StoryMode.StoryModePhases.TutorialPhase.Instance.SetTutorialFocusSettlement(Settlement.Find("village_ES3_2"));
	}

	protected override void RegisterEvents()
	{
		CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, OnGameMenuOpened);
		CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, OnSettlementLeft);
		CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
		CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, OnMobilePartyDestroyed);
	}

	protected override void InitializeQuestOnGameLoad()
	{
		SetDialogs();
		AddGameMenus();
	}

	private MobileParty CreateRaiderParty()
	{
		Settlement settlement = SettlementHelper.FindNearestHideoutToMobileParty(MobileParty.MainParty, MobileParty.NavigationType.All, (Settlement x) => x.IsActive).Settlement;
		Settlement settlement2 = MBObjectManager.Instance.GetObject<Settlement>("village_ES3_2");
		CampaignVec2 initialPosition = NavigationHelper.FindReachablePointAroundPosition(settlement2.GatePosition, MobileParty.NavigationType.Default, MobileParty.MainParty.SeeingRange * 0.75f, 1f);
		MobileParty mobileParty = BanditPartyComponent.CreateBanditParty("locate_and_rescue_traveller_quest_raider_party_" + _raiderPartyCount, settlement.OwnerClan, settlement.Hideout, isBossParty: false, null, initialPosition);
		CharacterObject character = Campaign.Current.ObjectManager.GetObject<CharacterObject>("storymode_quest_raider");
		mobileParty.MemberRoster.AddToCounts(character, 6);
		CharacterObject character2 = MBObjectManager.Instance.GetObject<CharacterObject>("tutorial_placeholder_volunteer");
		mobileParty.PrisonRoster.AddToCounts(character2, (MBRandom.RandomFloat >= 0.5f) ? 1 : 2);
		mobileParty.Party.SetCustomName(new TextObject("{=u1Pkt4HC}Raiders"));
		mobileParty.InitializePartyTrade(200);
		mobileParty.ActualClan = settlement.OwnerClan;
		SetPartyAiAction.GetActionForPatrollingAroundSettlement(mobileParty, settlement2, MobileParty.NavigationType.Default, isFromPort: false, isTargetingPort: false);
		mobileParty.Ai.SetDoNotMakeNewDecisions(doNotMakeNewDecisions: true);
		mobileParty.IgnoreByOtherPartiesTill(CampaignTime.Never);
		mobileParty.Party.SetVisualAsDirty();
		AddTrackedObject(mobileParty);
		mobileParty.IsActive = true;
		_raiderPartyCount++;
		mobileParty.SetPartyUsedByQuest(isActivelyUsed: true);
		return mobileParty;
	}

	private void DespawnRaiderParties()
	{
		if (_raiderParties.IsEmpty())
		{
			return;
		}
		foreach (MobileParty item in _raiderParties.ToList())
		{
			RemoveTrackedObject(item);
			DestroyPartyAction.Apply(null, item);
		}
		_raiderParties.Clear();
	}

	private void SpawnRaiderParties()
	{
		if (_raiderParties.IsEmpty())
		{
			for (int i = _defeatedRaiderPartyCount; i < 3; i++)
			{
				_raiderParties.Add(CreateRaiderParty());
			}
		}
	}

	protected override void SetDialogs()
	{
		Campaign.Current.ConversationManager.AddDialogFlow(DialogFlow.CreateDialogFlow("start", 1000010).NpcLine(new TextObject("{=BdYaRvhm}I don't know who you are, but I'm in your debt. These brigands would've marched us to our deaths.[ib:nervous2][if:convo_uncomfortable_voice]")).Condition(meeting_tacitus_on_condition)
			.NpcLine(new TextObject("{=9VxUSDQ7}My name's Tacteos. I'm a doctor by trade. I was on, well, a bit of a quest, but now I'm thinking I'm not really made for this kind of thing.[ib:nervous][if:convo_pondering]"))
			.NpcLine(new TextObject("{=5LJTeOBT}I was with a caravan and they just came out of the brush. We were surrounded and outnumbered, so we gave up. I figured they'd keep us alive, if just for the ransom. But then they started flogging us along at top speed, without any water, and I was just about ready to drop.[ib:nervous2]"))
			.NpcLine(new TextObject("{=XdDQdSsW}I could feel the signs of heat-stroke creeping up and I told them but they just flogged me more... If your group hadn't come along... Maybe I have a way to thank you properly.[ib:normal][if:convo_thinking]"))
			.PlayerLine(new TextObject("{=bkZFbCRx}We're looking for two children captured by the raiders. Can you tell us anything?"))
			.NpcLine(new TextObject("{=ehnbi5yD}I am afraid I haven't seen any children. But after our caravan was attacked, the chief of the raiders, the one they call Radagos, took and rode off with our more valuable belongings, including a chest that I had.[ib:closed][if:convo_empathic_voice]"))
			.NpcLine(new TextObject("{=RF3NoR3d}He seemed to be controlling more than one band raiding around this area. If this lot has your kin, then I think he'd be the one to know.[if:convo_pondering]"))
			.NpcLine(new TextObject("{=K75sH3vW}And since I have nothing of value left to repay your help, I'll tell you this. If you do catch up with and defeat that ruffian, you may be able to recover my chest. It contains a valuable ornament which I was told could be of great value, if you knew where to sell it.[if:convo_pondering]"))
			.NpcLine(new TextObject("{=8GCW5IRO}I was trying to find out more about it, but, as I say, I've had all my urge for travelling flogged out of me. Right now I don't think I'd venture more than 20 paces from a well as long as I live.[ib:closed2][if:convo_shocked]"))
			.PlayerLine(new TextObject("{=Zyn5FrTR}We'll keep that in mind."))
			.NpcLine(new TextObject("{=vJyTsFdU}It doesn't look like much and I suspect this lot would give it away for a few coins, but I got it from a mercenary whom I treated once, and swore it was related to 'Neretzes's Folly'. I don't know what that means, except that Neretzes was, of course, the emperor who died in battle some years back. Maybe you can find out its true value.[if:convo_calm_friendly]"))
			.NpcLine(new TextObject("{=tsjQtWsO}Thanks for saving me again. I hope our paths will cross again![ib:normal2][if:convo_calm_friendly]"))
			.Consequence(meeting_tacitus_on_consequence)
			.CloseDialog(), this);
		Campaign.Current.ConversationManager.AddDialogFlow(DialogFlow.CreateDialogFlow("start", 1000010).NpcLine(new TextObject("{=!}Start encounter.")).Condition(meeting_with_raider_party_on_condition)
			.CloseDialog(), this);
	}

	private bool meeting_tacitus_on_condition()
	{
		if (Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero == StoryModeHeroes.Tacitus)
		{
			return !Hero.OneToOneConversationHero.HasMet;
		}
		return false;
	}

	private void meeting_tacitus_on_consequence()
	{
		foreach (MobileParty raiderParty in _raiderParties)
		{
			if (raiderParty.IsActive)
			{
				DestroyPartyAction.Apply(null, raiderParty);
			}
		}
		DisableHeroAction.Apply(StoryModeHeroes.Tacitus);
		CompleteQuestWithSuccess();
	}

	private bool meeting_with_raider_party_on_condition()
	{
		return _raiderParties.Any((MobileParty p) => ConversationHelper.GetConversationCharacterPartyLeader(p.Party) == CharacterObject.OneToOneConversationCharacter);
	}

	private void OnGameMenuOpened(MenuCallbackArgs args)
	{
		if (Settlement.CurrentSettlement == null && PlayerEncounter.EncounteredMobileParty != null && _raiderParties.Any((MobileParty p) => p == PlayerEncounter.EncounteredMobileParty) && args.MenuContext.GameMenu.StringId != "encounter_meeting" && args.MenuContext.GameMenu.StringId != "encounter" && args.MenuContext.GameMenu.StringId != "encounter_raiders_quest")
		{
			GameMenu.SwitchToMenu("encounter_raiders_quest");
		}
		if (Hero.MainHero.HitPoints < 50)
		{
			Hero.MainHero.Heal(50 - Hero.MainHero.HitPoints);
		}
		Hero elderBrother = StoryModeHeroes.ElderBrother;
		if (elderBrother.HitPoints < 50)
		{
			elderBrother.Heal(50 - elderBrother.HitPoints);
		}
		if (!Hero.MainHero.IsPrisoner)
		{
			return;
		}
		EndCaptivityAction.ApplyByPeace(Hero.MainHero);
		if (elderBrother.IsPrisoner)
		{
			EndCaptivityAction.ApplyByPeace(elderBrother);
		}
		if (elderBrother.PartyBelongedTo != MobileParty.MainParty)
		{
			if (elderBrother.HeroState == Hero.CharacterStates.Fugitive || elderBrother.HeroState == Hero.CharacterStates.Released)
			{
				elderBrother.ChangeState(Hero.CharacterStates.Active);
			}
			AddHeroToPartyAction.Apply(elderBrother, MobileParty.MainParty, showNotification: false);
		}
		DisableHeroAction.Apply(StoryModeHeroes.Tacitus);
		TextObject textObject = new TextObject("{=ORnjaMlM}You were defeated by the raiders, but your brother saved you. It doesn't look like they're going anywhere, though, so you should attack again once you're ready.{newline}You must have at least {NUMBER} members in your party. If you don't, go back to the village and recruit some more troops.");
		textObject.SetTextVariable("NUMBER", 4);
		InformationManager.ShowInquiry(new InquiryData(new TextObject("{=FPhWhjq7}Defeated").ToString(), textObject.ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: false, new TextObject("{=lmG7uRK2}Okay").ToString(), null, delegate
		{
			PartyBase mainParty = PartyBase.MainParty;
			if (mainParty != null && mainParty.MemberRoster.TotalManCount >= 4)
			{
				SpawnRaiderParties();
			}
			else
			{
				Campaign.Current?.VisualTrackerManager.RegisterObject(MBObjectManager.Instance.GetObject<Settlement>("village_ES3_2"));
			}
		}, null));
		DespawnRaiderParties();
	}

	private void AddGameMenus()
	{
		AddGameMenu("encounter_raiders_quest", new TextObject("{=mU1bC1mp}You encountered the raider party."), game_menu_encounter_on_init, GameMenu.MenuOverlayType.Encounter);
		AddGameMenuOption("encounter_raiders_quest", "encounter_raiders_quest_attack", new TextObject("{=1r0tDsrR}Attack!"), game_menu_encounter_attack_on_condition, game_menu_encounter_attack_on_consequence);
		AddGameMenuOption("encounter_raiders_quest", "encounter_raiders_quest_send_troops", new TextObject("{=z3VamNrX}Send in your troops."), game_menu_encounter_send_troops_on_condition, null);
		AddGameMenuOption("encounter_raiders_quest", "encounter_raiders_quest_leave", new TextObject("{=2YYRyrOO}Leave..."), game_menu_encounter_leave_on_condition, game_menu_encounter_leave_on_consequence, Isleave: true);
	}

	private void game_menu_encounter_on_init(MenuCallbackArgs args)
	{
		if (PlayerEncounter.Battle == null)
		{
			PlayerEncounter.StartBattle();
		}
		PlayerEncounter.Update();
	}

	private bool game_menu_encounter_leave_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Leave;
		return true;
	}

	private void game_menu_encounter_leave_on_consequence(MenuCallbackArgs args)
	{
		MenuHelper.EncounterLeaveConsequence();
	}

	private bool game_menu_encounter_attack_on_condition(MenuCallbackArgs args)
	{
		if (PartyBase.MainParty.MemberRoster.TotalManCount < 4)
		{
			args.IsEnabled = false;
			args.Tooltip = new TextObject("{=DyE3luNM}You need to have at least {NUMBER} member in your party to deal with the raider party. Go back to village to recruit more troops.");
			args.Tooltip.SetTextVariable("NUMBER", 4);
		}
		return MenuHelper.EncounterAttackCondition(args);
	}

	internal void game_menu_encounter_attack_on_consequence(MenuCallbackArgs args)
	{
		MenuHelper.EncounterAttackConsequence(args);
	}

	private bool game_menu_encounter_send_troops_on_condition(MenuCallbackArgs args)
	{
		args.IsEnabled = false;
		args.Tooltip = new TextObject("{=hnFkhPhp}This option is disabled during tutorial stage.");
		args.optionLeaveType = GameMenuOption.LeaveType.OrderTroopsToAttack;
		return true;
	}

	[GameMenuInitializationHandler("encounter_raiders_quest")]
	private static void game_menu_encounter_on_init_background(MenuCallbackArgs args)
	{
		args.MenuContext.SetBackgroundMeshName("encounter_looter");
	}

	private void OnSettlementLeft(MobileParty party, Settlement settlement)
	{
		if (party == MobileParty.MainParty)
		{
			if (4 > MobileParty.MainParty.MemberRoster.TotalManCount)
			{
				DespawnRaiderParties();
				OpenRecruitMoreTroopsPopUp();
			}
			else
			{
				SpawnRaiderParties();
			}
		}
	}

	private void OpenRecruitMoreTroopsPopUp()
	{
		InformationManager.ShowInquiry(new InquiryData(new TextObject("{=y3fn2vWY}Recruit Troops").ToString(), new TextObject("{=taOCFKtZ}You need to recruit more troops to deal with the raider party. Go back to village to recruit more troops.").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: false, new TextObject("{=yS7PvrTD}OK").ToString(), null, null, null));
	}

	private void OnMapEventEnded(MapEvent mapEvent)
	{
		if (!mapEvent.IsPlayerMapEvent)
		{
			return;
		}
		if (mapEvent.PlayerSide == mapEvent.WinningSide)
		{
			foreach (MobileParty party in _raiderParties.ToList())
			{
				if (mapEvent.InvolvedParties.Any((PartyBase p) => p == party.Party))
				{
					_defeatedRaiderPartyCount++;
					_startQuestLog.UpdateCurrentProgress(_defeatedRaiderPartyCount);
					party.MemberRoster.Clear();
					if (_raiderParties.Count > 1)
					{
						_raiderParties.Remove(party);
					}
				}
				if (party.MemberRoster.TotalManCount == 0 && _raiderParties.Count > 1)
				{
					_raiderParties.Remove(party);
				}
			}
			if (_defeatedRaiderPartyCount >= 3)
			{
				MobileParty mobileParty = _raiderParties[0];
				TakePrisonerAction.Apply(prisonerCharacter: StoryModeHeroes.Tacitus, capturerParty: mobileParty.Party);
				mobileParty.PrisonRoster.AddToCounts(Campaign.Current.ObjectManager.GetObject<CharacterObject>("villager_empire"), 2);
				InformationManager.ShowInquiry(new InquiryData(new TextObject("{=EWD4Op6d}Notification").ToString(), new TextObject("{=OMrnTIe0}You rescue several prisoners that the raiders had been dragging along. They look parched and exhausted. You give them a bit of water and bread, and after a short while one staggers to his feet and comes over to you.").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: false, new TextObject("{=lmG7uRK2}Okay").ToString(), null, delegate
				{
					CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter, null, noHorse: true, noWeapon: true), new ConversationCharacterData(StoryModeHeroes.Tacitus.CharacterObject, null, noHorse: true, noWeapon: true));
				}, null));
			}
		}
		if (4 > MobileParty.MainParty.MemberRoster.TotalManCount)
		{
			DespawnRaiderParties();
			OpenRecruitMoreTroopsPopUp();
		}
	}

	protected override void HourlyTick()
	{
		if (4 > MobileParty.MainParty.MemberRoster.TotalManCount && MathF.Floor(Campaign.Current.Models.CampaignTimeModel.CampaignStartTime.ElapsedHoursUntilNow) % 12 == 0)
		{
			DespawnRaiderParties();
			OpenRecruitMoreTroopsPopUp();
			Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
		}
	}

	private void OnMobilePartyDestroyed(MobileParty mobileParty, PartyBase destroyerParty)
	{
		if (_raiderParties.Contains(mobileParty))
		{
			_raiderParties.Remove(mobileParty);
		}
	}

	protected override void OnCompleteWithSuccess()
	{
		StoryMode.StoryModePhases.TutorialPhase.Instance.RemoveTutorialFocusSettlement();
		StoryMode.StoryModePhases.TutorialPhase.Instance.RemoveTutorialFocusMobileParty();
	}

	internal static void AutoGeneratedStaticCollectObjectsLocateAndRescueTravellerTutorialQuest(object o, List<object> collectedObjects)
	{
		((LocateAndRescueTravellerTutorialQuest)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
	}

	protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
	{
		base.AutoGeneratedInstanceCollectObjects(collectedObjects);
		collectedObjects.Add(_raiderParties);
		collectedObjects.Add(_startQuestLog);
	}

	internal static object AutoGeneratedGetMemberValue_raiderPartyCount(object o)
	{
		return ((LocateAndRescueTravellerTutorialQuest)o)._raiderPartyCount;
	}

	internal static object AutoGeneratedGetMemberValue_raiderParties(object o)
	{
		return ((LocateAndRescueTravellerTutorialQuest)o)._raiderParties;
	}

	internal static object AutoGeneratedGetMemberValue_defeatedRaiderPartyCount(object o)
	{
		return ((LocateAndRescueTravellerTutorialQuest)o)._defeatedRaiderPartyCount;
	}

	internal static object AutoGeneratedGetMemberValue_startQuestLog(object o)
	{
		return ((LocateAndRescueTravellerTutorialQuest)o)._startQuestLog;
	}
}
