using System.Collections.Generic;
using System.Linq;
using StoryMode.StoryModeObjects;
using StoryMode.StoryModePhases;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace StoryMode.Quests.TutorialPhase;

public class TravelToVillageTutorialQuest : StoryModeQuestBase
{
	private const int RefugePartyCount = 4;

	[SaveableField(1)]
	private Settlement _questVillage;

	[SaveableField(2)]
	private readonly MobileParty[] _refugeeParties;

	private TextObject _startQuestLog
	{
		get
		{
			TextObject textObject = new TextObject("{=bNqLQKQS}You are out of food. There is a village called {VILLAGE_NAME} north of here where you can buy provisions and find some help.");
			textObject.SetTextVariable("VILLAGE_NAME", _questVillage.Name);
			return textObject;
		}
	}

	private TextObject _endQuestLog => new TextObject("{=7VFLb3Qj}You have arrived at the village.");

	public override TextObject Title
	{
		get
		{
			TextObject textObject = new TextObject("{=oa4XFhve}Travel to Village {VILLAGE_NAME}");
			textObject.SetTextVariable("VILLAGE_NAME", _questVillage.Name);
			return textObject;
		}
	}

	public TravelToVillageTutorialQuest()
		: base("travel_to_village_tutorial_quest", null, CampaignTime.Never)
	{
		_questVillage = Settlement.Find("village_ES3_2");
		AddTrackedObject(_questVillage);
		Hero trackedObject = _questVillage.Notables.First((Hero x) => x.IsHeadman);
		AddTrackedObject(trackedObject);
		_refugeeParties = new MobileParty[4];
		TextObject textObject = new TextObject("{=3YHL3wpM}{BROTHER.NAME}:");
		textObject.SetCharacterProperties("BROTHER", StoryModeHeroes.ElderBrother.CharacterObject);
		InformationManager.ShowInquiry(new InquiryData(textObject.ToString(), new TextObject("{=dE2ufxte}Before we do anything else... We're low on food. There's a village north of here where we can buy provisions and find some help. You're a better rider than I am so I'll let you lead the way...").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: false, new TextObject("{=JOJ09cLW}Let's go.").ToString(), null, delegate
		{
			StoryModeEvents.Instance.OnTravelToVillageTutorialQuestStarted();
		}, null));
		SetDialogs();
		InitializeQuestOnCreation();
		AddLog(_startQuestLog);
		StoryMode.StoryModePhases.TutorialPhase.Instance.SetTutorialFocusSettlement(_questVillage);
		CreateRefugeeParties();
	}

	protected override void InitializeQuestOnGameLoad()
	{
		SetDialogs();
	}

	protected override void HourlyTick()
	{
	}

	protected override void SetDialogs()
	{
		Campaign.Current.ConversationManager.AddDialogFlow(DialogFlow.CreateDialogFlow("start", 1000010).NpcLine(new TextObject("{=MDtTC5j5}Don't hurt us![ib:nervous][if:convo_nervous]")).Condition(news_about_raiders_condition)
			.Consequence(news_about_raiders_consequence)
			.PlayerLine(new TextObject("{=pX5cx3b4}I mean you no harm. We're hunting a group of raiders who took our brother and sister."))
			.NpcLine(new TextObject("{=ajBBFq1D}Aii... Those devils. They raided our village. Took whoever they could catch. Slavers, I'll bet.[if:convo_nervous][ib:nervous2]"))
			.NpcLine(new TextObject("{=AhthUkMu}People say they're still about. We're sleeping in the woods, not going back until they're gone. You hunt them down and kill every one, you hear! Heaven protect you! Heaven guide your swords![if:convo_nervous2][ib:nervous]"))
			.CloseDialog(), this);
		Campaign.Current.ConversationManager.AddDialogFlow(DialogFlow.CreateDialogFlow("start", 1000020).NpcLine(new TextObject("{=pa9LrHln}We're here, I guess. So... We need food, and after that, maybe some men to come with us.[if:convo_thinking]")).Condition(() => Settlement.CurrentSettlement != null && Settlement.CurrentSettlement == _questVillage && Hero.OneToOneConversationHero == StoryModeHeroes.ElderBrother)
			.NpcLine(new TextObject("{=p0fmZY5r}The headman here can probably help us. Let's try to find him...[if:convo_pondering]"))
			.Consequence(talk_with_brother_consequence)
			.CloseDialog(), this);
	}

	private bool news_about_raiders_condition()
	{
		if (Settlement.CurrentSettlement == null && MobileParty.ConversationParty != null)
		{
			return _refugeeParties.Contains(MobileParty.ConversationParty);
		}
		return false;
	}

	private void news_about_raiders_consequence()
	{
		PlayerEncounter.LeaveEncounter = true;
	}

	private void talk_with_brother_consequence()
	{
		Campaign.Current.ConversationManager.ConversationEndOneShot += base.CompleteQuestWithSuccess;
	}

	protected override void RegisterEvents()
	{
		CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, OnGameMenuOpened);
		CampaignEvents.BeforeMissionOpenedEvent.AddNonSerializedListener(this, OnBeforeMissionOpened);
		StoryModeEvents.OnTravelToVillageTutorialQuestStartedEvent.AddNonSerializedListener(this, OnTravelToVillageTutorialQuestStarted);
	}

	private void OnGameMenuOpened(MenuCallbackArgs args)
	{
		if (!StoryMode.StoryModePhases.TutorialPhase.Instance.IsCompleted && Settlement.CurrentSettlement == null && PlayerEncounter.EncounteredParty != null && args.MenuContext.GameMenu.StringId != "encounter_meeting" && args.MenuContext.GameMenu.StringId != "encounter")
		{
			if (_refugeeParties.Contains(PlayerEncounter.EncounteredMobileParty))
			{
				GameMenu.SwitchToMenu("encounter_meeting");
				return;
			}
			PlayerEncounter.Finish();
			InformationManager.ShowInquiry(new InquiryData(new TextObject("{=EWD4Op6d}Notification").ToString(), new TextObject("{=pVKkclVk}Interactions are limited during tutorial phase. This interaction is disabled.").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: false, new TextObject("{=yS7PvrTD}OK").ToString(), null, null, null));
		}
	}

	private void OnBeforeMissionOpened()
	{
		if (Settlement.CurrentSettlement != null && Settlement.CurrentSettlement == Settlement.Find("village_ES3_2"))
		{
			int hitPoints = StoryModeHeroes.ElderBrother.HitPoints;
			int num = 50;
			if (hitPoints < num)
			{
				int healAmount = num - hitPoints;
				StoryModeHeroes.ElderBrother.Heal(healAmount);
			}
			LocationCharacter locationCharacterOfHero = LocationComplex.Current.GetLocationCharacterOfHero(StoryModeHeroes.ElderBrother);
			PlayerEncounter.LocationEncounter.AddAccompanyingCharacter(locationCharacterOfHero, isFollowing: true);
		}
	}

	protected override void DailyTick()
	{
		for (int i = 0; i < _refugeeParties.Length; i++)
		{
			if (_refugeeParties[i].Party.IsStarving)
			{
				_refugeeParties[i].Party.ItemRoster.AddToCounts(DefaultItems.Grain, 2);
			}
		}
	}

	private void OnTravelToVillageTutorialQuestStarted()
	{
		if (GameStateManager.Current.ActiveState is MapState mapState)
		{
			mapState.Handler.StartCameraAnimation(_questVillage.GatePosition, 1f);
		}
	}

	private void CreateRefugeeParties()
	{
		for (int i = 0; i < 4; i++)
		{
			MobileParty mobileParty = CustomPartyComponent.CreateCustomPartyWithTroopRoster(_questVillage.GatePosition, MobileParty.MainParty.SeeingRange, _questVillage, new TextObject("{=7FWF01bW}Refugees"), null, TroopRoster.CreateDummyTroopRoster(), TroopRoster.CreateDummyTroopRoster(), _questVillage.OwnerClan.Leader, "", "", 0f, avoidHostileActions: true);
			mobileParty.InitializePartyTrade(200);
			SetPartyAiAction.GetActionForPatrollingAroundSettlement(mobileParty, _questVillage, MobileParty.NavigationType.Default, isFromPort: false, isTargetingPort: false);
			mobileParty.Ai.SetDoNotMakeNewDecisions(doNotMakeNewDecisions: true);
			mobileParty.IgnoreByOtherPartiesTill(CampaignTime.Never);
			mobileParty.SetPartyUsedByQuest(isActivelyUsed: true);
			mobileParty.Party.ItemRoster.AddToCounts(DefaultItems.Grain, 2);
			CharacterObject characterObject = MBObjectManager.Instance.GetObject<CharacterObject>("storymode_quest_refugee_female");
			CharacterObject characterObject2 = MBObjectManager.Instance.GetObject<CharacterObject>("storymode_quest_refugee_male");
			int num = MBRandom.RandomInt(6, 12);
			for (int j = 0; j < num; j++)
			{
				mobileParty.MemberRoster.AddToCounts((MBRandom.RandomFloat < 0.5f) ? characterObject : characterObject2, 1);
			}
			_refugeeParties[i] = mobileParty;
		}
	}

	protected override void OnCompleteWithSuccess()
	{
		foreach (MobileParty item in _refugeeParties.ToList())
		{
			DestroyPartyAction.Apply(null, item);
		}
		AddLog(_endQuestLog);
		StoryMode.StoryModePhases.TutorialPhase.Instance.RemoveTutorialFocusSettlement();
	}

	internal static void AutoGeneratedStaticCollectObjectsTravelToVillageTutorialQuest(object o, List<object> collectedObjects)
	{
		((TravelToVillageTutorialQuest)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
	}

	protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
	{
		base.AutoGeneratedInstanceCollectObjects(collectedObjects);
		collectedObjects.Add(_questVillage);
		collectedObjects.Add(_refugeeParties);
	}

	internal static object AutoGeneratedGetMemberValue_questVillage(object o)
	{
		return ((TravelToVillageTutorialQuest)o)._questVillage;
	}

	internal static object AutoGeneratedGetMemberValue_refugeeParties(object o)
	{
		return ((TravelToVillageTutorialQuest)o)._refugeeParties;
	}
}
