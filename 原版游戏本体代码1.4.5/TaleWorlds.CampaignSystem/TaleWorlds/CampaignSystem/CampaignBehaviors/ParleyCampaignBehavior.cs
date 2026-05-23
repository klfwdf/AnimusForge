using System.Collections.Generic;
using Helpers;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class ParleyCampaignBehavior : CampaignBehaviorBase, IParleyCampaignBehavior
{
	private PartyBase _parleyedParty;

	public override void RegisterEvents()
	{
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_parleyedParty", ref _parleyedParty);
	}

	public void StartParley(PartyBase partyBase)
	{
		if (partyBase.IsSettlement)
		{
			_parleyedParty = partyBase;
			GameMenu.ActivateGameMenu("request_meeting_parley");
		}
		else
		{
			Debug.FailedAssert("MobileParty parley not implemented yet!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CampaignBehaviors\\ParleyCampaignBehavior.cs", "StartParley", 35);
		}
	}

	private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
	{
		AddMenus(campaignGameStarter);
	}

	private void AddMenus(CampaignGameStarter campaignGameStarter)
	{
		campaignGameStarter.AddGameMenu("request_meeting_parley", "{=pBAx7jTM}With whom do you want to meet?", game_menu_town_menu_request_meeting_on_init);
		campaignGameStarter.AddGameMenuOption("request_meeting_parley", "request_meeting_with", "{=!}{HERO_TO_MEET.LINK}", game_menu_request_meeting_with_on_condition, game_menu_request_meeting_with_on_consequence, isLeave: false, -1, isRepeatable: true);
		campaignGameStarter.AddGameMenuOption("request_meeting_parley", "meeting_town_leave", "{=3sRdGQou}Leave", game_meeting_town_leave_on_condition, game_menu_request_meeting_town_leave_on_consequence, isLeave: true);
		campaignGameStarter.AddGameMenuOption("request_meeting_parley", "meeting_castle_leave", "{=3sRdGQou}Leave", game_meeting_castle_leave_on_condition, game_menu_request_meeting_castle_leave_on_consequence, isLeave: true);
	}

	private void game_menu_town_menu_request_meeting_on_init(MenuCallbackArgs args)
	{
		List<Hero> heroesToMeetInTown = TownHelpers.GetHeroesToMeetInTown(_parleyedParty.Settlement);
		args.MenuContext.SetRepeatObjectList(heroesToMeetInTown);
		args.MenuContext.SetBackgroundMeshName(_parleyedParty.Settlement.SettlementComponent.WaitMeshName);
	}

	private bool game_menu_request_meeting_with_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Conversation;
		Hero hero = args.MenuContext.GetCurrentRepeatableObject() as Hero;
		if (_parleyedParty != null && hero != null)
		{
			StringHelpers.SetCharacterProperties("HERO_TO_MEET", hero.CharacterObject);
			MenuHelper.SetIssueAndQuestDataForHero(args, hero);
			return true;
		}
		return false;
	}

	private void game_menu_request_meeting_town_leave_on_consequence(MenuCallbackArgs args)
	{
		SettlementMenuLeaveConsequenceCommon();
	}

	private void game_menu_request_meeting_castle_leave_on_consequence(MenuCallbackArgs args)
	{
		SettlementMenuLeaveConsequenceCommon();
	}

	private void SettlementMenuLeaveConsequenceCommon()
	{
		GameMenu.ExitToLast();
		_parleyedParty = null;
	}

	private void game_menu_request_meeting_with_on_consequence(MenuCallbackArgs args)
	{
		string sceneLevel;
		string meetingScene = GetMeetingScene(out sceneLevel);
		Hero hero = (Hero)args.MenuContext.GetSelectedObject();
		ConversationCharacterData playerCharacterData = new ConversationCharacterData(Hero.MainHero.CharacterObject, PartyBase.MainParty);
		ConversationCharacterData conversationPartnerData = new ConversationCharacterData(hero.CharacterObject, hero.PartyBelongedTo?.Party, noHorse: true);
		CampaignMission.OpenConversationMission(playerCharacterData, conversationPartnerData, meetingScene, sceneLevel);
	}

	private string GetMeetingScene(out string sceneLevel)
	{
		string sceneID = GameSceneDataManager.Instance.MeetingScenes.GetRandomElementWithPredicate((MeetingSceneData x) => x.Culture == _parleyedParty.Settlement.Culture).SceneID;
		if (string.IsNullOrEmpty(sceneID))
		{
			sceneID = GameSceneDataManager.Instance.MeetingScenes.GetRandomElement().SceneID;
		}
		sceneLevel = "";
		if (_parleyedParty.Settlement.IsFortification)
		{
			sceneLevel = Campaign.Current.Models.LocationModel.GetUpgradeLevelTag(_parleyedParty.Settlement.Town.GetWallLevel());
		}
		return sceneID;
	}

	private bool game_meeting_town_leave_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Leave;
		return _parleyedParty.Settlement.IsTown;
	}

	private bool game_meeting_castle_leave_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Leave;
		return _parleyedParty.Settlement.IsCastle;
	}

	public PartyBase GetParleyedParty()
	{
		return _parleyedParty;
	}
}
