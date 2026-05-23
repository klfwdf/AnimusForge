using System;
using System.Collections.Generic;
using Helpers;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x02000420 RID: 1056
	public class ParleyCampaignBehavior : CampaignBehaviorBase, IParleyCampaignBehavior
	{
		// Token: 0x060042CF RID: 17103 RVA: 0x001427B0 File Offset: 0x001409B0
		public override void RegisterEvents()
		{
			CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnSessionLaunched));
		}

		// Token: 0x060042D0 RID: 17104 RVA: 0x001427C9 File Offset: 0x001409C9
		public override void SyncData(IDataStore dataStore)
		{
			dataStore.SyncData<PartyBase>("_parleyedParty", ref this._parleyedParty);
		}

		// Token: 0x060042D1 RID: 17105 RVA: 0x001427DD File Offset: 0x001409DD
		public void StartParley(PartyBase partyBase)
		{
			if (partyBase.IsSettlement)
			{
				this._parleyedParty = partyBase;
				GameMenu.ActivateGameMenu("request_meeting_parley");
				return;
			}
			Debug.FailedAssert("MobileParty parley not implemented yet!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CampaignBehaviors\\ParleyCampaignBehavior.cs", "StartParley", 35);
		}

		// Token: 0x060042D2 RID: 17106 RVA: 0x0014280F File Offset: 0x00140A0F
		private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
		{
			this.AddMenus(campaignGameStarter);
		}

		// Token: 0x060042D3 RID: 17107 RVA: 0x00142818 File Offset: 0x00140A18
		private void AddMenus(CampaignGameStarter campaignGameStarter)
		{
			campaignGameStarter.AddGameMenu("request_meeting_parley", "{=pBAx7jTM}With whom do you want to meet?", new OnInitDelegate(this.game_menu_town_menu_request_meeting_on_init), GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
			campaignGameStarter.AddGameMenuOption("request_meeting_parley", "request_meeting_with", "{=!}{HERO_TO_MEET.LINK}", new GameMenuOption.OnConditionDelegate(this.game_menu_request_meeting_with_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_request_meeting_with_on_consequence), false, -1, true, null);
			campaignGameStarter.AddGameMenuOption("request_meeting_parley", "meeting_town_leave", "{=3sRdGQou}Leave", new GameMenuOption.OnConditionDelegate(this.game_meeting_town_leave_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_request_meeting_town_leave_on_consequence), true, -1, false, null);
			campaignGameStarter.AddGameMenuOption("request_meeting_parley", "meeting_castle_leave", "{=3sRdGQou}Leave", new GameMenuOption.OnConditionDelegate(this.game_meeting_castle_leave_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_request_meeting_castle_leave_on_consequence), true, -1, false, null);
		}

		// Token: 0x060042D4 RID: 17108 RVA: 0x001428D8 File Offset: 0x00140AD8
		private void game_menu_town_menu_request_meeting_on_init(MenuCallbackArgs args)
		{
			List<Hero> heroesToMeetInTown = TownHelpers.GetHeroesToMeetInTown(this._parleyedParty.Settlement);
			args.MenuContext.SetRepeatObjectList(heroesToMeetInTown);
			args.MenuContext.SetBackgroundMeshName(this._parleyedParty.Settlement.SettlementComponent.WaitMeshName);
		}

		// Token: 0x060042D5 RID: 17109 RVA: 0x00142924 File Offset: 0x00140B24
		private bool game_menu_request_meeting_with_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Conversation;
			Hero hero = args.MenuContext.GetCurrentRepeatableObject() as Hero;
			if (this._parleyedParty != null && hero != null)
			{
				StringHelpers.SetCharacterProperties("HERO_TO_MEET", hero.CharacterObject, null, false);
				MenuHelper.SetIssueAndQuestDataForHero(args, hero);
				return true;
			}
			return false;
		}

		// Token: 0x060042D6 RID: 17110 RVA: 0x00142972 File Offset: 0x00140B72
		private void game_menu_request_meeting_town_leave_on_consequence(MenuCallbackArgs args)
		{
			this.SettlementMenuLeaveConsequenceCommon();
		}

		// Token: 0x060042D7 RID: 17111 RVA: 0x0014297A File Offset: 0x00140B7A
		private void game_menu_request_meeting_castle_leave_on_consequence(MenuCallbackArgs args)
		{
			this.SettlementMenuLeaveConsequenceCommon();
		}

		// Token: 0x060042D8 RID: 17112 RVA: 0x00142982 File Offset: 0x00140B82
		private void SettlementMenuLeaveConsequenceCommon()
		{
			GameMenu.ExitToLast();
			this._parleyedParty = null;
		}

		// Token: 0x060042D9 RID: 17113 RVA: 0x00142990 File Offset: 0x00140B90
		private void game_menu_request_meeting_with_on_consequence(MenuCallbackArgs args)
		{
			string sceneLevels;
			string meetingScene = this.GetMeetingScene(out sceneLevels);
			Hero hero = (Hero)args.MenuContext.GetSelectedObject();
			ConversationCharacterData playerCharacterData = new ConversationCharacterData(Hero.MainHero.CharacterObject, PartyBase.MainParty, false, false, false, false, false, false);
			CharacterObject characterObject = hero.CharacterObject;
			MobileParty partyBelongedTo = hero.PartyBelongedTo;
			ConversationCharacterData conversationPartnerData = new ConversationCharacterData(characterObject, (partyBelongedTo != null) ? partyBelongedTo.Party : null, true, false, false, false, false, false);
			CampaignMission.OpenConversationMission(playerCharacterData, conversationPartnerData, meetingScene, sceneLevels, false);
		}

		// Token: 0x060042DA RID: 17114 RVA: 0x00142A00 File Offset: 0x00140C00
		private string GetMeetingScene(out string sceneLevel)
		{
			string sceneID = GameSceneDataManager.Instance.MeetingScenes.GetRandomElementWithPredicate((MeetingSceneData x) => x.Culture == this._parleyedParty.Settlement.Culture).SceneID;
			if (string.IsNullOrEmpty(sceneID))
			{
				sceneID = GameSceneDataManager.Instance.MeetingScenes.GetRandomElement<MeetingSceneData>().SceneID;
			}
			sceneLevel = "";
			if (this._parleyedParty.Settlement.IsFortification)
			{
				sceneLevel = Campaign.Current.Models.LocationModel.GetUpgradeLevelTag(this._parleyedParty.Settlement.Town.GetWallLevel());
			}
			return sceneID;
		}

		// Token: 0x060042DB RID: 17115 RVA: 0x00142A96 File Offset: 0x00140C96
		private bool game_meeting_town_leave_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Leave;
			return this._parleyedParty.Settlement.IsTown;
		}

		// Token: 0x060042DC RID: 17116 RVA: 0x00142AB0 File Offset: 0x00140CB0
		private bool game_meeting_castle_leave_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Leave;
			return this._parleyedParty.Settlement.IsCastle;
		}

		// Token: 0x04001311 RID: 4881
		private PartyBase _parleyedParty;
	}
}
