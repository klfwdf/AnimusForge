using System;
using System.Linq;
using Helpers;
using StoryMode.Quests.FirstPhase;
using StoryMode.Quests.PlayerClanQuests;
using StoryMode.Quests.TutorialPhase;
using StoryMode.StoryModeObjects;
using StoryMode.StoryModePhases;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.SceneInformationPopupTypes;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace StoryMode.GameComponents.CampaignBehaviors;

public class FirstPhaseCampaignBehavior : CampaignBehaviorBase
{
	private Location _imperialMentorHouse;

	private Location _antiImperialMentorHouse;

	private bool _popUpShowed;

	public override void RegisterEvents()
	{
		CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
		CampaignEvents.OnQuestCompletedEvent.AddNonSerializedListener(this, OnQuestCompleted);
		CampaignEvents.OnNewGameCreatedPartialFollowUpEndEvent.AddNonSerializedListener(this, OnNewGameCreatedPartialFollowUpEnd);
		CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, OnGameMenuOpened);
		CampaignEvents.BeforeMissionOpenedEvent.AddNonSerializedListener(this, OnBeforeMissionOpened);
		CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, OnSettlementLeft);
		StoryModeEvents.OnBannerPieceCollectedEvent.AddNonSerializedListener(this, OnBannerPieceCollected);
		StoryModeEvents.OnStoryModeTutorialEndedEvent.AddNonSerializedListener(this, OnStoryModeTutorialEnded);
		StoryModeEvents.OnMainStoryLineSideChosenEvent.AddNonSerializedListener(this, OnMainStoryLineSideChosen);
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_imperialMentorHouse", ref _imperialMentorHouse);
		dataStore.SyncData("_antiImperialMentorHouse", ref _antiImperialMentorHouse);
		dataStore.SyncData("_popUpShowed", ref _popUpShowed);
	}

	private void OnGameLoaded(CampaignGameStarter campaignGameStarter)
	{
		SpawnMentorsIfNeeded();
	}

	private void OnNewGameCreatedPartialFollowUpEnd(CampaignGameStarter campaignGameStarter)
	{
		Settlement settlement = SettlementHelper.FindRandomSettlement((Settlement s) => s.IsTown && !s.IsUnderSiege && s.Culture.StringId == "empire");
		_imperialMentorHouse = ReserveHouseForMentor(StoryModeHeroes.ImperialMentor, settlement);
		Settlement settlement2 = SettlementHelper.FindRandomSettlement((Settlement s) => s.IsTown && !s.IsUnderSiege && s.Culture.StringId == "battania");
		_antiImperialMentorHouse = ReserveHouseForMentor(StoryModeHeroes.AntiImperialMentor, settlement2);
		StoryModeManager.Current.MainStoryLine.SetMentorSettlements(settlement, settlement2);
	}

	private void OnQuestCompleted(QuestBase quest, QuestBase.QuestCompleteDetails detail)
	{
		if (detail == QuestBase.QuestCompleteDetails.Success)
		{
			if (quest is BannerInvestigationQuest)
			{
				new MeetWithIstianaQuest(StoryModeManager.Current.MainStoryLine.ImperialMentorSettlement).StartQuest();
				new MeetWithArzagosQuest(StoryModeManager.Current.MainStoryLine.AntiImperialMentorSettlement).StartQuest();
			}
			else if (quest is MeetWithIstianaQuest)
			{
				Hero imperialMentor = StoryModeHeroes.ImperialMentor;
				new IstianasBannerPieceQuest(imperialMentor, FindSuitableHideout(imperialMentor)).StartQuest();
			}
			else if (quest is MeetWithArzagosQuest)
			{
				Hero antiImperialMentor = StoryModeHeroes.AntiImperialMentor;
				new ArzagosBannerPieceQuest(antiImperialMentor, FindSuitableHideout(antiImperialMentor)).StartQuest();
			}
		}
	}

	private void OnGameMenuOpened(MenuCallbackArgs args)
	{
		SpawnMentorsIfNeeded();
	}

	private void OnBeforeMissionOpened()
	{
		SpawnMentorsIfNeeded();
	}

	private void SpawnMentorsIfNeeded()
	{
		if (_imperialMentorHouse != null && _antiImperialMentorHouse != null && Settlement.CurrentSettlement != null && (StoryModeHeroes.ImperialMentor.CurrentSettlement == Settlement.CurrentSettlement || StoryModeHeroes.AntiImperialMentor.CurrentSettlement == Settlement.CurrentSettlement))
		{
			SpawnMentorInHouse(Settlement.CurrentSettlement);
		}
	}

	private void OnSettlementLeft(MobileParty party, Settlement settlement)
	{
		if (settlement.StringId == "tutorial_training_field" && party == MobileParty.MainParty && TutorialPhase.Instance.TutorialQuestPhase == TutorialQuestPhase.Finalized && !_popUpShowed && TutorialPhase.Instance.IsSkipped)
		{
			InformationManager.ShowInquiry(new InquiryData(new TextObject("{=EWD4Op6d}Notification").ToString(), GameTexts.FindText("main_storyline_skip_tutorial_notification_text").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: false, new TextObject("{=yS7PvrTD}OK").ToString(), null, delegate
			{
				_popUpShowed = true;
				CampaignEventDispatcher.Instance.RemoveListeners(Campaign.Current.GetCampaignBehavior<TutorialPhaseCampaignBehavior>());
				MBInformationManager.ShowSceneNotification(new FindingFirstBannerPieceSceneNotificationItem(Hero.MainHero, OnPieceFoundAction));
			}, null));
		}
	}

	private void ShowStealthTutorialInquiry()
	{
		TextObject textObject = new TextObject("{=DhMge68x}Stealth Tutorial");
		InformationManager.ShowInquiry(new InquiryData(text: new TextObject("{=bU88a6lW}You and your brother part ways. As he rides over the crest of a hill, he lifts his arm in salute, then disappears from view. A few days ago you were a family of six. Now, you are alone, and you realize that despite your courage and determination you and your brother may never see each other again.{newline}However, you are not left long in your solitude. As you make the final preparations to set out, a young boy staggers into your camp. Once he regains his breath, he tells you that a small group of bandits raided his village and seized the headman as a hostage. The villagers saw you riding through the countryside, and thought you might be able to help them.").ToString(), titleText: textObject.ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: false, affirmativeText: GameTexts.FindText("str_continue").ToString(), negativeText: string.Empty, affirmativeAction: StartStealthTutorial, negativeAction: null), pauseGameActiveState: true);
	}

	private void StartStealthTutorial()
	{
		new VillagersInNeed().StartQuest();
		StoryModeEvents.Instance.OnStealthTutorialActivated();
	}

	private void OnPieceFoundAction()
	{
		SelectClanName();
	}

	private void OnStoryModeTutorialEnded()
	{
		new RebuildPlayerClanQuest().StartQuest();
		new BannerInvestigationQuest().StartQuest();
	}

	private void OnBannerPieceCollected()
	{
		TextObject textObject = new TextObject("{=Pus87ZW2}You've found the {BANNER_PIECE_COUNT} banner piece!");
		if (FirstPhase.Instance == null || FirstPhase.Instance.CollectedBannerPieceCount == 1)
		{
			textObject.SetTextVariable("BANNER_PIECE_COUNT", new TextObject("{=oAoTaAWg}first"));
		}
		else if (FirstPhase.Instance.CollectedBannerPieceCount == 2)
		{
			textObject.SetTextVariable("BANNER_PIECE_COUNT", new TextObject("{=9ZyXl25X}second"));
		}
		else if (FirstPhase.Instance.CollectedBannerPieceCount == 3)
		{
			textObject.SetTextVariable("BANNER_PIECE_COUNT", new TextObject("{=4cw169Kb}third and the final"));
		}
		MBInformationManager.AddQuickInformation(textObject);
	}

	private void OnMainStoryLineSideChosen(MainStoryLineSide side)
	{
		_imperialMentorHouse.RemoveReservation();
		_imperialMentorHouse = null;
		_antiImperialMentorHouse.RemoveReservation();
		_antiImperialMentorHouse = null;
	}

	private void SelectClanName()
	{
		InformationManager.ShowTextInquiry(new TextInquiryData(new TextObject("{=JJiKk4ow}Select your family name: ").ToString(), string.Empty, isAffirmativeOptionShown: true, isNegativeOptionShown: false, GameTexts.FindText("str_done").ToString(), null, OnChangeClanNameDone, null, shouldInputBeObfuscated: false, FactionHelper.IsClanNameApplicable, "", Clan.PlayerClan.Name.ToString()));
	}

	private void OnChangeClanNameDone(string newClanName)
	{
		TextObject textObject = GameTexts.FindText("str_generic_clan_name");
		textObject.SetTextVariable("CLAN_NAME", new TextObject(newClanName));
		Clan.PlayerClan.ChangeClanName(textObject, textObject);
		OpenBannerSelectionScreen(ShowStealthTutorialInquiry);
	}

	private void OpenBannerSelectionScreen(Action endAction)
	{
		Game.Current.GameStateManager.PushState(Game.Current.GameStateManager.CreateState<BannerEditorState>(new object[1] { endAction }));
	}

	private Settlement FindSuitableHideout(Hero questGiver)
	{
		Settlement result = null;
		float num = float.MaxValue;
		foreach (Hideout item in Hideout.All)
		{
			if (!item.Settlement.IsSettlementBusy(this))
			{
				float distance = Campaign.Current.Models.MapDistanceModel.GetDistance(item.Settlement, questGiver.CurrentSettlement, isFromPort: false, isTargetingPort: false, MobileParty.NavigationType.Default);
				if (distance < num)
				{
					num = distance;
					result = item.Settlement;
				}
			}
		}
		return result;
	}

	private void SpawnMentorInHouse(Settlement settlement)
	{
		Hero obj = ((StoryModeHeroes.ImperialMentor.CurrentSettlement == settlement) ? StoryModeHeroes.ImperialMentor : StoryModeHeroes.AntiImperialMentor);
		Location location = ((StoryModeHeroes.ImperialMentor.CurrentSettlement == settlement) ? _imperialMentorHouse : _antiImperialMentorHouse);
		CharacterObject characterObject = obj.CharacterObject;
		Monster monsterWithSuffix = FaceGen.GetMonsterWithSuffix(characterObject.Race, "_settlement");
		LocationCharacter locationCharacter = new LocationCharacter(new AgentData(new SimpleAgentOrigin(characterObject)).Monster(monsterWithSuffix), SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors, "npc_common", fixedLocation: true, LocationCharacter.CharacterRelations.Neutral, null, useCivilianEquipment: true);
		location.AddCharacter(locationCharacter);
	}

	private Location ReserveHouseForMentor(Hero mentor, Settlement settlement)
	{
		if (settlement == null)
		{
			Debug.Print("There is null settlement in ReserveHouseForMentor");
		}
		MBList<Location> mBList = new MBList<Location>();
		mBList.Add(settlement.LocationComplex.GetLocationWithId("house_1"));
		mBList.Add(settlement.LocationComplex.GetLocationWithId("house_2"));
		mBList.Add(settlement.LocationComplex.GetLocationWithId("house_3"));
		Location obj = mBList.First((Location h) => !h.IsReserved) ?? mBList.GetRandomElement();
		TextObject textObject = new TextObject("{=EZ19JOGj}{MENTOR.NAME}'s House");
		StringHelpers.SetCharacterProperties("MENTOR", mentor.CharacterObject, textObject);
		obj.ReserveLocation(textObject, textObject);
		return obj;
	}
}
