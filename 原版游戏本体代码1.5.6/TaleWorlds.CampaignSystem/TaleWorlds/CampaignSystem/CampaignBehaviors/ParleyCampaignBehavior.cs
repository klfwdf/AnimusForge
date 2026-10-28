using System.Collections.Generic;
using Helpers;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
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
		else if (partyBase.IsMobile)
		{
			_parleyedParty = partyBase;
			List<Hero> heroesToMeetInParty = GetHeroesToMeetInParty(partyBase.MobileParty);
			if (heroesToMeetInParty.Count == 1)
			{
				StartFieldParleyConversation(heroesToMeetInParty[0]);
			}
			else
			{
				GameMenu.ActivateGameMenu("request_meeting_field_parley");
			}
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
		campaignGameStarter.AddGameMenu("request_meeting_field_parley", "{=pBAx7jTM}With whom do you want to meet?", game_menu_field_parley_on_init);
		campaignGameStarter.AddGameMenuOption("request_meeting_field_parley", "request_meeting_with_field", "{=!}{HERO_TO_MEET.LINK}", game_menu_request_meeting_with_field_on_condition, game_menu_request_meeting_with_field_on_consequence, isLeave: false, -1, isRepeatable: true);
		campaignGameStarter.AddGameMenuOption("request_meeting_field_parley", "meeting_field_leave", "{=3sRdGQou}Leave", game_meeting_field_leave_on_condition, game_menu_request_meeting_field_leave_on_consequence, isLeave: true);
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
		string meetingScene = GetMeetingScene(out var sceneLevel);
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

	private void game_menu_field_parley_on_init(MenuCallbackArgs args)
	{
		List<Hero> heroesToMeetInParty = GetHeroesToMeetInParty(_parleyedParty.MobileParty);
		args.MenuContext.SetRepeatObjectList(heroesToMeetInParty);
		string text = (_parleyedParty.MapFaction?.Culture)?.EncounterBackgroundMesh;
		if (!string.IsNullOrEmpty(text))
		{
			if (MobileParty.MainParty.IsCurrentlyAtSea || _parleyedParty.MobileParty.IsCurrentlyAtSea)
			{
				text += "_naval";
			}
			args.MenuContext.SetBackgroundMeshName(text);
		}
	}

	private static List<Hero> GetHeroesToMeetInParty(MobileParty mobileParty)
	{
		List<Hero> list = new List<Hero>();
		if (mobileParty == null)
		{
			Debug.FailedAssert("Mobile party should not be null", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CampaignBehaviors\\ParleyCampaignBehavior.cs", "GetHeroesToMeetInParty", 172);
			return list;
		}
		if (mobileParty.Army != null && mobileParty.Army.LeaderParty != null)
		{
			AddHeroesFromParty(mobileParty.Army.LeaderParty, list);
			foreach (MobileParty attachedParty in mobileParty.Army.LeaderParty.AttachedParties)
			{
				AddHeroesFromParty(attachedParty, list);
			}
		}
		else
		{
			AddHeroesFromParty(mobileParty, list);
		}
		return list;
	}

	private static void AddHeroesFromParty(MobileParty mobileParty, List<Hero> meetingList)
	{
		if (mobileParty == null)
		{
			Debug.FailedAssert("Mobile party should not be null", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CampaignBehaviors\\ParleyCampaignBehavior.cs", "AddHeroesFromParty", 196);
			return;
		}
		foreach (TroopRosterElement item in mobileParty.MemberRoster.GetTroopRoster())
		{
			if (item.Character.IsHero && item.Character.HeroObject != null && !item.Character.HeroObject.IsPrisoner && !item.Character.HeroObject.IsDead && !meetingList.Contains(item.Character.HeroObject))
			{
				meetingList.Add(item.Character.HeroObject);
			}
		}
	}

	private bool game_menu_request_meeting_with_field_on_condition(MenuCallbackArgs args)
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

	private void game_menu_request_meeting_with_field_on_consequence(MenuCallbackArgs args)
	{
		if (!(args.MenuContext.GetSelectedObject() is Hero heroToMeet))
		{
			_parleyedParty = null;
			return;
		}
		GameMenu.ExitToLast();
		StartFieldParleyConversation(heroToMeet);
	}

	private void StartFieldParleyConversation(Hero heroToMeet)
	{
		Campaign.Current.CurrentConversationContext = ConversationContext.Default;
		PartyBase party = heroToMeet.PartyBelongedTo?.Party ?? _parleyedParty;
		ConversationCharacterData playerCharacterData = new ConversationCharacterData(Hero.MainHero.CharacterObject, PartyBase.MainParty);
		ConversationCharacterData conversationPartnerData = new ConversationCharacterData(heroToMeet.CharacterObject, party, noHorse: true);
		_parleyedParty = null;
		if (MobileParty.MainParty.IsCurrentlyAtSea)
		{
			CampaignMission.OpenConversationMission(playerCharacterData, conversationPartnerData);
		}
		else
		{
			CampaignMapConversation.OpenConversation(playerCharacterData, conversationPartnerData);
		}
	}

	private bool game_meeting_field_leave_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Leave;
		if (_parleyedParty != null)
		{
			return _parleyedParty.IsMobile;
		}
		return false;
	}

	private void game_menu_request_meeting_field_leave_on_consequence(MenuCallbackArgs args)
	{
		GameMenu.ExitToLast();
		_parleyedParty = null;
	}

	public PartyBase GetParleyedParty()
	{
		return _parleyedParty;
	}
}
