using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.BarterSystem;
using TaleWorlds.CampaignSystem.BarterSystem.Barterables;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Conversation.Persuasion;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x02000414 RID: 1044
	public class LordDefectionCampaignBehavior : CampaignBehaviorBase
	{
		// Token: 0x0600421A RID: 16922 RVA: 0x0013C825 File Offset: 0x0013AA25
		public LordDefectionCampaignBehavior()
		{
			this._previousDefectionPersuasionAttempts = new List<PersuasionAttempt>();
		}

		// Token: 0x0600421B RID: 16923 RVA: 0x0013C859 File Offset: 0x0013AA59
		public override void RegisterEvents()
		{
			CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnSessionLaunched));
			CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, new Action(this.OnDailyTick));
		}

		// Token: 0x0600421C RID: 16924 RVA: 0x0013C889 File Offset: 0x0013AA89
		public override void SyncData(IDataStore dataStore)
		{
			dataStore.SyncData<List<PersuasionAttempt>>("previousPersuasionAttempts", ref this._previousDefectionPersuasionAttempts);
		}

		// Token: 0x0600421D RID: 16925 RVA: 0x0013C89D File Offset: 0x0013AA9D
		public void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
		{
			this.AddDialogs(campaignGameStarter);
		}

		// Token: 0x0600421E RID: 16926 RVA: 0x0013C8A6 File Offset: 0x0013AAA6
		public void ClearPersuasion()
		{
			this._previousDefectionPersuasionAttempts.Clear();
		}

		// Token: 0x0600421F RID: 16927 RVA: 0x0013C8B4 File Offset: 0x0013AAB4
		private PersuasionTask GetFailedPersuasionTask(LordDefectionCampaignBehavior.DefectionReservationType reservationType)
		{
			foreach (PersuasionTask persuasionTask in this._allReservations)
			{
				if (persuasionTask.ReservationType == (int)reservationType && !this.CanAttemptToPersuade(Hero.OneToOneConversationHero, (int)reservationType))
				{
					return persuasionTask;
				}
			}
			return null;
		}

		// Token: 0x06004220 RID: 16928 RVA: 0x0013C920 File Offset: 0x0013AB20
		private PersuasionTask GetAnyFailedPersuasionTask()
		{
			foreach (PersuasionTask persuasionTask in this._allReservations)
			{
				if (!this.CanAttemptToPersuade(Hero.OneToOneConversationHero, persuasionTask.ReservationType))
				{
					return persuasionTask;
				}
			}
			return null;
		}

		// Token: 0x06004221 RID: 16929 RVA: 0x0013C988 File Offset: 0x0013AB88
		private PersuasionTask GetCurrentPersuasionTask()
		{
			foreach (PersuasionTask persuasionTask in this._allReservations)
			{
				if (!persuasionTask.Options.All((PersuasionOptionArgs x) => x.IsBlocked))
				{
					return persuasionTask;
				}
			}
			return this._allReservations[this._allReservations.Count - 1];
		}

		// Token: 0x06004222 RID: 16930 RVA: 0x0013CA24 File Offset: 0x0013AC24
		protected void AddDialogs(CampaignGameStarter starter)
		{
			this.AddLordDefectionPersuasionOptions(starter);
			string id = "lord_ask_recruit_argument_1";
			string inputToken = "lord_ask_recruit_persuasion";
			string outputToken = "lord_defection_reaction";
			string text = "{=!}{DEFECTION_PERSUADE_ATTEMPT_1}";
			ConversationSentence.OnConditionDelegate conditionDelegate = new ConversationSentence.OnConditionDelegate(this.conversation_lord_recruit_1_persuade_option_on_condition);
			ConversationSentence.OnConsequenceDelegate consequenceDelegate = new ConversationSentence.OnConsequenceDelegate(this.conversation_lord_recruit_1_persuade_option_on_consequence);
			int priority = 100;
			ConversationSentence.OnPersuasionOptionDelegate persuasionOptionDelegate = new ConversationSentence.OnPersuasionOptionDelegate(this.SetupDefectionPersuasionOption1);
			starter.AddPlayerLine(id, inputToken, outputToken, text, conditionDelegate, consequenceDelegate, priority, new ConversationSentence.OnClickableConditionDelegate(this.DefectionPersuasionOption1ClickableOnCondition1), persuasionOptionDelegate);
			string id2 = "lord_ask_recruit_argument_2";
			string inputToken2 = "lord_ask_recruit_persuasion";
			string outputToken2 = "lord_defection_reaction";
			string text2 = "{=!}{DEFECTION_PERSUADE_ATTEMPT_2}";
			ConversationSentence.OnConditionDelegate conditionDelegate2 = new ConversationSentence.OnConditionDelegate(this.conversation_lord_recruit_2_persuade_option_on_condition);
			ConversationSentence.OnConsequenceDelegate consequenceDelegate2 = new ConversationSentence.OnConsequenceDelegate(this.conversation_lord_recruit_2_persuade_option_on_consequence);
			int priority2 = 100;
			persuasionOptionDelegate = new ConversationSentence.OnPersuasionOptionDelegate(this.SetupDefectionPersuasionOption2);
			starter.AddPlayerLine(id2, inputToken2, outputToken2, text2, conditionDelegate2, consequenceDelegate2, priority2, new ConversationSentence.OnClickableConditionDelegate(this.DefectionPersuasionOption2ClickableOnCondition2), persuasionOptionDelegate);
			string id3 = "lord_ask_recruit_argument_3";
			string inputToken3 = "lord_ask_recruit_persuasion";
			string outputToken3 = "lord_defection_reaction";
			string text3 = "{=!}{DEFECTION_PERSUADE_ATTEMPT_3}";
			ConversationSentence.OnConditionDelegate conditionDelegate3 = new ConversationSentence.OnConditionDelegate(this.conversation_lord_recruit_3_persuade_option_on_condition);
			ConversationSentence.OnConsequenceDelegate consequenceDelegate3 = new ConversationSentence.OnConsequenceDelegate(this.conversation_lord_recruit_3_persuade_option_on_consequence);
			int priority3 = 100;
			persuasionOptionDelegate = new ConversationSentence.OnPersuasionOptionDelegate(this.SetupDefectionPersuasionOption3);
			starter.AddPlayerLine(id3, inputToken3, outputToken3, text3, conditionDelegate3, consequenceDelegate3, priority3, new ConversationSentence.OnClickableConditionDelegate(this.DefectionPersuasionOption3ClickableOnCondition3), persuasionOptionDelegate);
			string id4 = "lord_ask_recruit_argument_4";
			string inputToken4 = "lord_ask_recruit_persuasion";
			string outputToken4 = "lord_defection_reaction";
			string text4 = "{=!}{DEFECTION_PERSUADE_ATTEMPT_4}";
			ConversationSentence.OnConditionDelegate conditionDelegate4 = new ConversationSentence.OnConditionDelegate(this.conversation_lord_recruit_4_persuade_option_on_condition);
			ConversationSentence.OnConsequenceDelegate consequenceDelegate4 = new ConversationSentence.OnConsequenceDelegate(this.conversation_lord_recruit_4_persuade_option_on_consequence);
			int priority4 = 100;
			persuasionOptionDelegate = new ConversationSentence.OnPersuasionOptionDelegate(this.SetupDefectionPersuasionOption4);
			starter.AddPlayerLine(id4, inputToken4, outputToken4, text4, conditionDelegate4, consequenceDelegate4, priority4, new ConversationSentence.OnClickableConditionDelegate(this.DefectionPersuasionOption4ClickableOnCondition4), persuasionOptionDelegate);
			starter.AddPlayerLine("lord_ask_recruit_argument_no_answer", "lord_ask_recruit_persuasion", "lord_pretalk", "{=0eAtiZbL}I have no answer to that.", null, new ConversationSentence.OnConsequenceDelegate(this.conversation_on_end_persuasion_on_consequence), 100, null, null);
			starter.AddDialogLine("lord_ask_recruit_argument_reaction", "lord_defection_reaction", "lord_defection_next_reservation", "{=!}{PERSUASION_REACTION}", new ConversationSentence.OnConditionDelegate(this.conversation_lord_persuade_option_reaction_on_condition), new ConversationSentence.OnConsequenceDelegate(this.conversation_lord_persuade_option_reaction_on_consequence), 100, null);
		}

		// Token: 0x06004223 RID: 16931 RVA: 0x0013CBD8 File Offset: 0x0013ADD8
		private void AddLordDefectionPersuasionOptions(CampaignGameStarter starter)
		{
			starter.AddPlayerLine("player_is_requesting_enemy_change_sides", "lord_talk_speak_diplomacy_2", "persuasion_leave_faction_npc", "{=5a0NhbOA}Your liege, {FIRST_NAME}, is not worth of your loyalty.", new ConversationSentence.OnConditionDelegate(this.conversation_player_is_asking_to_recruit_enemy_on_condition), null, 100, null, null);
			starter.AddPlayerLine("player_is_requesting_neutral_change_sides", "lord_talk_speak_diplomacy_2", "persuasion_leave_faction_npc", "{=3gbgjJfZ}Candidly, what do you think of your liege, {FIRST_NAME}?", new ConversationSentence.OnConditionDelegate(this.conversation_player_is_asking_to_recruit_neutral_on_condition), null, 100, null, null);
			starter.AddPlayerLine("player_suggesting_treason", "lord_talk_speak_diplomacy_2", "persuasion_leave_faction_npc", "{=bKsb7tcr}Candidly, what do you think of our liege, {FIRST_NAME}?", new ConversationSentence.OnConditionDelegate(this.conversation_suggest_treason_on_condition), null, 100, null, null);
			starter.AddPlayerLine("persuasion_leave_faction_player_cheat", "lord_talk_speak_diplomacy_2", "start", "{=Cd405TC7}Clear past persuasion attempts (CHEAT)", delegate
			{
				if (Game.Current.IsDevelopmentMode)
				{
					return this._previousDefectionPersuasionAttempts.Any((PersuasionAttempt x) => x.PersuadedHero == Hero.OneToOneConversationHero);
				}
				return false;
			}, new ConversationSentence.OnConsequenceDelegate(this.conversation_clear_persuasion_on_consequence), 100, null, null);
			starter.AddPlayerLine("player_prisoner_talk", "hero_main_options", "persuasion_leave_faction_npc", "{=wNSH1JdJ}I have an offer for you: join us, and be set free.", new ConversationSentence.OnConditionDelegate(this.conversation_player_start_defection_with_prisoner_on_condition), null, 100, null, null);
			starter.AddDialogLine("player_prisoner_talk_pre_barter", "player_prisoner_defection", "persuasion_leave_faction_npc", "{=DRkWMe5X}Even now, I am not sure that's in my best interests...", null, null, 100, null);
			starter.AddDialogLine("persuasion_leave_faction_npc_refuse", "persuasion_leave_faction_npc", "lord_pretalk", "{=!}{LIEGE_IS_RELATIVE}", new ConversationSentence.OnConditionDelegate(this.conversation_lord_from_ruling_clan_on_condition), null, 100, null);
			starter.AddDialogLine("persuasion_leave_faction_npc_refuse_high_negative_score", "persuasion_leave_faction_npc", "lord_pretalk", "{=ZYUHljOa}I am happy with my current liege. Neither your purse nor our relationship is deep enough to change that.", new ConversationSentence.OnConditionDelegate(this.conversation_lord_persuade_option_reaction_pre_reject_on_condition), null, 100, null);
			starter.AddDialogLine("persuasion_leave_faction_npc_redirected", "persuasion_leave_faction_npc", "lord_pretalk", "{=UW1roOES}You should discuss this issue with {REDIRECT_HERO_RELATIONSHIP}, who speaks for our family.", new ConversationSentence.OnConditionDelegate(this.conversation_lord_redirects_to_clan_leader_on_condition), new ConversationSentence.OnConsequenceDelegate(this.persuasion_redierect_player_finish_on_consequece), 100, null);
			starter.AddDialogLine("persuasion_leave_faction_npc_answer", "persuasion_leave_faction_npc", "persuasion_leave_faction_player", "{=yub5GWVq}What are you saying, exactly?[if:convo_thinking]", () => !this.conversation_lord_redirects_to_clan_leader_on_condition(), null, 100, null);
			starter.AddPlayerLine("persuasion_leave_faction_player_start", "persuasion_leave_faction_player", "lord_defection_next_reservation", "{=!}{RECRUIT_START}", new ConversationSentence.OnConditionDelegate(this.conversation_lord_can_recruit_on_condition), new ConversationSentence.OnConsequenceDelegate(this.start_lord_defection_persuasion_on_consequence), 100, null, null);
			starter.AddDialogLine("lord_ask_recruit_next_reservation_fail", "lord_defection_next_reservation", "lord_pretalk", "{=!}{FAILED_PERSUASION_LINE}", new ConversationSentence.OnConditionDelegate(this.conversation_lord_player_has_failed_in_defection_on_condition), new ConversationSentence.OnConsequenceDelegate(this.conversation_on_end_persuasion_on_consequence), 100, null);
			starter.AddDialogLine("lord_ask_recruit_next_reservation_attempt", "lord_defection_next_reservation", "lord_ask_recruit_persuasion", "{=!}{PERSUASION_TASK_LINE}[if:convo_thinking]", new ConversationSentence.OnConditionDelegate(this.conversation_lord_recruit_check_if_reservations_met_on_condition), null, 100, null);
			starter.AddDialogLine("lord_ask_recruit_next_reservation_success_without_barter", "lord_defection_next_reservation", "close_window", "{=!}{DEFECTION_AGREE_WITHOUT_BARTER}", new ConversationSentence.OnConditionDelegate(this.conversation_lord_check_if_ready_to_join_faction_without_barter_on_condition), new ConversationSentence.OnConsequenceDelegate(this.conversation_lord_defect_to_clan_without_barter_on_consequence), 100, null);
			starter.AddDialogLine("lord_ask_recruit_next_reservation_success_with_barter", "lord_defection_next_reservation", "lord_ask_recruit_ai_argument_reaction", "{=BeYbp6M2}Very well. You've convinced me that this is something I can consider.", new ConversationSentence.OnConditionDelegate(this.conversation_lord_check_if_ready_to_join_faction_with_barter_on_condition), null, 100, null);
			starter.AddDialogLine("persuasion_leave_faction_npc_result_success_2", "lord_ask_recruit_ai_argument_reaction", "lord_defection_barter_line", "{=0dY1xyyK}This is a dangerous step, however, and I'm putting my life and the lives of my people at risk. I need some sort of support from you before I can change my allegiance.[if:convo_stern]", null, null, 100, null);
			starter.AddDialogLine("persuasion_leave_faction_npc_result_success_open_barter", "lord_defection_barter_line", "lord_defection_post_barter", "{=!}BARTER LINE - Covered by barter interface. Please do not remove these lines!", null, new ConversationSentence.OnConsequenceDelegate(this.conversation_leave_faction_barter_consequence), 100, null);
			starter.AddDialogLine("lord_defection_post_barter_s", "lord_defection_post_barter", "close_window", "{=9aZgTNiU}Very well. This is a great step to take, but it must be done.[if:convo_calm_friendly][ib:confident]", new ConversationSentence.OnConditionDelegate(this.defection_barter_successful_on_condition), new ConversationSentence.OnConsequenceDelegate(this.defection_successful_on_consequence), 100, null);
			starter.AddDialogLine("lord_defection_post_barter_f", "lord_defection_post_barter", "close_window", "{=BO9QV55x}I cannot do what you ask.[if:convo_grave]", () => !this.defection_barter_successful_on_condition(), null, 100, null);
		}

		// Token: 0x06004224 RID: 16932 RVA: 0x0013CF48 File Offset: 0x0013B148
		private bool defection_barter_successful_on_condition()
		{
			return Campaign.Current.BarterManager.LastBarterIsAccepted;
		}

		// Token: 0x06004225 RID: 16933 RVA: 0x0013CF5C File Offset: 0x0013B15C
		private void defection_successful_on_consequence()
		{
			TraitLevelingHelper.OnPersuasionDefection(Hero.OneToOneConversationHero);
			if (PlayerEncounter.Current != null)
			{
				PlayerEncounter.LeaveEncounter = true;
			}
			foreach (PersuasionAttempt persuasionAttempt in this._previousDefectionPersuasionAttempts)
			{
				if (persuasionAttempt.PersuadedHero == Hero.OneToOneConversationHero)
				{
					PersuasionOptionResult result = persuasionAttempt.Result;
					if (result != PersuasionOptionResult.Success)
					{
						if (result == PersuasionOptionResult.CriticalSuccess)
						{
							int num = ((persuasionAttempt.Args.ArgumentStrength < PersuasionArgumentStrength.Normal) ? (MathF.Abs((int)persuasionAttempt.Args.ArgumentStrength) * 50) : 50);
							SkillLevelingManager.OnPersuasionSucceeded(Hero.MainHero, persuasionAttempt.Args.SkillUsed, PersuasionDifficulty.Medium, 2 * num);
						}
					}
					else
					{
						int num = ((persuasionAttempt.Args.ArgumentStrength < PersuasionArgumentStrength.Normal) ? (MathF.Abs((int)persuasionAttempt.Args.ArgumentStrength) * 50) : 50);
						SkillLevelingManager.OnPersuasionSucceeded(Hero.MainHero, persuasionAttempt.Args.SkillUsed, PersuasionDifficulty.Medium, num);
					}
				}
			}
			IStatisticsCampaignBehavior behavior = Campaign.Current.CampaignBehaviorManager.GetBehavior<IStatisticsCampaignBehavior>();
			if (behavior != null)
			{
				behavior.OnDefectionPersuasionSucess();
			}
		}

		// Token: 0x06004226 RID: 16934 RVA: 0x0013D084 File Offset: 0x0013B284
		private bool conversation_lord_recruit_1_persuade_option_on_condition()
		{
			PersuasionTask currentPersuasionTask = this.GetCurrentPersuasionTask();
			if (currentPersuasionTask.Options.Count > 0)
			{
				TextObject textObject = new TextObject("{=bSo9hKwr}{PERSUASION_OPTION_LINE} {SUCCESS_CHANCE}", null);
				textObject.SetTextVariable("SUCCESS_CHANCE", PersuasionHelper.ShowSuccess(currentPersuasionTask.Options.ElementAt(0), true));
				textObject.SetTextVariable("PERSUASION_OPTION_LINE", currentPersuasionTask.Options.ElementAt(0).Line);
				MBTextManager.SetTextVariable("DEFECTION_PERSUADE_ATTEMPT_1", textObject, false);
				return true;
			}
			return false;
		}

		// Token: 0x06004227 RID: 16935 RVA: 0x0013D0FC File Offset: 0x0013B2FC
		private void conversation_lord_recruit_1_persuade_option_on_consequence()
		{
			PersuasionTask currentPersuasionTask = this.GetCurrentPersuasionTask();
			if (currentPersuasionTask.Options.Count > 0)
			{
				currentPersuasionTask.Options[0].BlockTheOption(true);
			}
		}

		// Token: 0x06004228 RID: 16936 RVA: 0x0013D130 File Offset: 0x0013B330
		private void conversation_lord_recruit_2_persuade_option_on_consequence()
		{
			PersuasionTask currentPersuasionTask = this.GetCurrentPersuasionTask();
			if (currentPersuasionTask.Options.Count > 1)
			{
				currentPersuasionTask.Options[1].BlockTheOption(true);
			}
		}

		// Token: 0x06004229 RID: 16937 RVA: 0x0013D164 File Offset: 0x0013B364
		private void conversation_lord_recruit_3_persuade_option_on_consequence()
		{
			PersuasionTask currentPersuasionTask = this.GetCurrentPersuasionTask();
			if (currentPersuasionTask.Options.Count > 2)
			{
				currentPersuasionTask.Options[2].BlockTheOption(true);
			}
		}

		// Token: 0x0600422A RID: 16938 RVA: 0x0013D198 File Offset: 0x0013B398
		private void conversation_lord_recruit_4_persuade_option_on_consequence()
		{
			PersuasionTask currentPersuasionTask = this.GetCurrentPersuasionTask();
			if (currentPersuasionTask.Options.Count > 3)
			{
				currentPersuasionTask.Options[3].BlockTheOption(true);
			}
		}

		// Token: 0x0600422B RID: 16939 RVA: 0x0013D1CC File Offset: 0x0013B3CC
		private bool DefectionPersuasionOption1ClickableOnCondition1(out TextObject hintText)
		{
			PersuasionTask currentPersuasionTask = this.GetCurrentPersuasionTask();
			if (currentPersuasionTask.Options.Count > 0)
			{
				hintText = null;
				return !currentPersuasionTask.Options.ElementAt(0).IsBlocked;
			}
			hintText = new TextObject("{=9ACJsI6S}Blocked", null);
			return false;
		}

		// Token: 0x0600422C RID: 16940 RVA: 0x0013D214 File Offset: 0x0013B414
		private bool DefectionPersuasionOption2ClickableOnCondition2(out TextObject hintText)
		{
			PersuasionTask currentPersuasionTask = this.GetCurrentPersuasionTask();
			if (currentPersuasionTask.Options.Count > 1)
			{
				hintText = null;
				return !currentPersuasionTask.Options.ElementAt(1).IsBlocked;
			}
			hintText = new TextObject("{=9ACJsI6S}Blocked", null);
			return false;
		}

		// Token: 0x0600422D RID: 16941 RVA: 0x0013D25C File Offset: 0x0013B45C
		private bool DefectionPersuasionOption3ClickableOnCondition3(out TextObject hintText)
		{
			PersuasionTask currentPersuasionTask = this.GetCurrentPersuasionTask();
			if (currentPersuasionTask.Options.Count > 2)
			{
				hintText = null;
				return !currentPersuasionTask.Options.ElementAt(2).IsBlocked;
			}
			hintText = new TextObject("{=9ACJsI6S}Blocked", null);
			return false;
		}

		// Token: 0x0600422E RID: 16942 RVA: 0x0013D2A4 File Offset: 0x0013B4A4
		private bool DefectionPersuasionOption4ClickableOnCondition4(out TextObject hintText)
		{
			PersuasionTask currentPersuasionTask = this.GetCurrentPersuasionTask();
			if (currentPersuasionTask.Options.Count > 3)
			{
				hintText = null;
				return !currentPersuasionTask.Options.ElementAt(3).IsBlocked;
			}
			hintText = new TextObject("{=9ACJsI6S}Blocked", null);
			return false;
		}

		// Token: 0x0600422F RID: 16943 RVA: 0x0013D2EC File Offset: 0x0013B4EC
		private bool conversation_lord_recruit_2_persuade_option_on_condition()
		{
			PersuasionTask currentPersuasionTask = this.GetCurrentPersuasionTask();
			if (currentPersuasionTask.Options.Count > 1)
			{
				TextObject textObject = new TextObject("{=bSo9hKwr}{PERSUASION_OPTION_LINE} {SUCCESS_CHANCE}", null);
				textObject.SetTextVariable("SUCCESS_CHANCE", PersuasionHelper.ShowSuccess(currentPersuasionTask.Options.ElementAt(1), true));
				textObject.SetTextVariable("PERSUASION_OPTION_LINE", currentPersuasionTask.Options.ElementAt(1).Line);
				MBTextManager.SetTextVariable("DEFECTION_PERSUADE_ATTEMPT_2", textObject, false);
				return true;
			}
			return false;
		}

		// Token: 0x06004230 RID: 16944 RVA: 0x0013D364 File Offset: 0x0013B564
		private bool conversation_lord_recruit_3_persuade_option_on_condition()
		{
			PersuasionTask currentPersuasionTask = this.GetCurrentPersuasionTask();
			if (currentPersuasionTask.Options.Count > 2)
			{
				TextObject textObject = new TextObject("{=bSo9hKwr}{PERSUASION_OPTION_LINE} {SUCCESS_CHANCE}", null);
				textObject.SetTextVariable("SUCCESS_CHANCE", PersuasionHelper.ShowSuccess(currentPersuasionTask.Options.ElementAt(2), true));
				textObject.SetTextVariable("PERSUASION_OPTION_LINE", currentPersuasionTask.Options.ElementAt(2).Line);
				MBTextManager.SetTextVariable("DEFECTION_PERSUADE_ATTEMPT_3", textObject, false);
				return true;
			}
			return false;
		}

		// Token: 0x06004231 RID: 16945 RVA: 0x0013D3DC File Offset: 0x0013B5DC
		private bool conversation_lord_recruit_4_persuade_option_on_condition()
		{
			PersuasionTask currentPersuasionTask = this.GetCurrentPersuasionTask();
			if (currentPersuasionTask.Options.Count > 3)
			{
				TextObject textObject = new TextObject("{=bSo9hKwr}{PERSUASION_OPTION_LINE} {SUCCESS_CHANCE}", null);
				textObject.SetTextVariable("SUCCESS_CHANCE", PersuasionHelper.ShowSuccess(currentPersuasionTask.Options.ElementAt(3), true));
				textObject.SetTextVariable("PERSUASION_OPTION_LINE", currentPersuasionTask.Options.ElementAt(3).Line);
				MBTextManager.SetTextVariable("DEFECTION_PERSUADE_ATTEMPT_4", textObject, false);
				return true;
			}
			return false;
		}

		// Token: 0x06004232 RID: 16946 RVA: 0x0013D454 File Offset: 0x0013B654
		private PersuasionOptionArgs SetupDefectionPersuasionOption1()
		{
			return this.GetCurrentPersuasionTask().Options.ElementAt(0);
		}

		// Token: 0x06004233 RID: 16947 RVA: 0x0013D467 File Offset: 0x0013B667
		private PersuasionOptionArgs SetupDefectionPersuasionOption2()
		{
			return this.GetCurrentPersuasionTask().Options.ElementAt(1);
		}

		// Token: 0x06004234 RID: 16948 RVA: 0x0013D47A File Offset: 0x0013B67A
		private PersuasionOptionArgs SetupDefectionPersuasionOption3()
		{
			return this.GetCurrentPersuasionTask().Options.ElementAt(2);
		}

		// Token: 0x06004235 RID: 16949 RVA: 0x0013D48D File Offset: 0x0013B68D
		private PersuasionOptionArgs SetupDefectionPersuasionOption4()
		{
			return this.GetCurrentPersuasionTask().Options.ElementAt(3);
		}

		// Token: 0x06004236 RID: 16950 RVA: 0x0013D4A0 File Offset: 0x0013B6A0
		private bool conversation_player_start_defection_with_prisoner_on_condition()
		{
			if (Hero.OneToOneConversationHero != null && Clan.PlayerClan.Kingdom != null && Hero.MainHero.IsKingdomLeader)
			{
				Clan clan = Hero.OneToOneConversationHero.Clan;
				if (((clan != null) ? clan.Leader : null) == Hero.OneToOneConversationHero && Hero.OneToOneConversationHero.HeroState == Hero.CharacterStates.Prisoner && Campaign.Current.CurrentConversationContext != ConversationContext.CapturedLord && Campaign.Current.CurrentConversationContext != ConversationContext.FreeOrCapturePrisonerHero && Hero.OneToOneConversationHero.Clan != Hero.OneToOneConversationHero.MapFaction.Leader.Clan)
				{
					return (Hero.OneToOneConversationHero.PartyBelongedToAsPrisoner != null && Hero.OneToOneConversationHero.PartyBelongedToAsPrisoner == PartyBase.MainParty) || (Hero.OneToOneConversationHero.CurrentSettlement != null && Hero.OneToOneConversationHero.CurrentSettlement.OwnerClan == Clan.PlayerClan);
				}
			}
			return false;
		}

		// Token: 0x06004237 RID: 16951 RVA: 0x0013D584 File Offset: 0x0013B784
		private bool conversation_lord_persuade_option_reaction_pre_reject_on_condition()
		{
			return Hero.OneToOneConversationHero.Clan.Leader == Hero.OneToOneConversationHero && (float)new JoinKingdomAsClanBarterable(Hero.OneToOneConversationHero, (Kingdom)Hero.MainHero.MapFaction, true).GetValueForFaction(Hero.OneToOneConversationHero.Clan) < -MathF.Min(2000000f, MathF.Max(500000f, 250000f + (float)Hero.MainHero.Gold / 3f));
		}

		// Token: 0x06004238 RID: 16952 RVA: 0x0013D604 File Offset: 0x0013B804
		private bool conversation_lord_persuade_option_reaction_on_condition()
		{
			PersuasionOptionResult item = ConversationManager.GetPersuasionChosenOptions().Last<Tuple<PersuasionOptionArgs, PersuasionOptionResult>>().Item2;
			PersuasionTask currentPersuasionTask = this.GetCurrentPersuasionTask();
			if (item == PersuasionOptionResult.Failure)
			{
				MBTextManager.SetTextVariable("IMMEDIATE_FAILURE_LINE", ((currentPersuasionTask != null) ? currentPersuasionTask.ImmediateFailLine : null) ?? TextObject.GetEmpty(), false);
				MBTextManager.SetTextVariable("PERSUASION_REACTION", "{=18xOURG4}Hmm.. No... {IMMEDIATE_FAILURE_LINE}", false);
			}
			else
			{
				if (item == PersuasionOptionResult.CriticalFailure)
				{
					MBTextManager.SetTextVariable("PERSUASION_REACTION", "{=Lj5Lghww}What? No...", false);
					TextObject text = ((currentPersuasionTask != null) ? currentPersuasionTask.ImmediateFailLine : null) ?? TextObject.GetEmpty();
					MBTextManager.SetTextVariable("IMMEDIATE_FAILURE_LINE", text, false);
					MBTextManager.SetTextVariable("PERSUASION_REACTION", "{=18xOURG4}Hmm.. No... {IMMEDIATE_FAILURE_LINE}", false);
					using (List<PersuasionTask>.Enumerator enumerator = this._allReservations.GetEnumerator())
					{
						while (enumerator.MoveNext())
						{
							PersuasionTask persuasionTask = enumerator.Current;
							persuasionTask.BlockAllOptions();
						}
						return true;
					}
				}
				MBTextManager.SetTextVariable("PERSUASION_REACTION", PersuasionHelper.GetDefaultPersuasionOptionReaction(item), false);
			}
			return true;
		}

		// Token: 0x06004239 RID: 16953 RVA: 0x0013D6FC File Offset: 0x0013B8FC
		private void conversation_lord_persuade_option_reaction_on_consequence()
		{
			Tuple<PersuasionOptionArgs, PersuasionOptionResult> tuple = ConversationManager.GetPersuasionChosenOptions().Last<Tuple<PersuasionOptionArgs, PersuasionOptionResult>>();
			float difficulty = Campaign.Current.Models.PersuasionModel.GetDifficulty(PersuasionDifficulty.Medium);
			float moveToNextStageChance;
			float blockRandomOptionChance;
			Campaign.Current.Models.PersuasionModel.GetEffectChances(tuple.Item1, out moveToNextStageChance, out blockRandomOptionChance, difficulty);
			PersuasionTask persuasionTask = this.FindTaskOfOption(tuple.Item1);
			persuasionTask.ApplyEffects(moveToNextStageChance, blockRandomOptionChance);
			PersuasionAttempt item = new PersuasionAttempt(Hero.OneToOneConversationHero, CampaignTime.Now, tuple.Item1, tuple.Item2, persuasionTask.ReservationType);
			this._previousDefectionPersuasionAttempts.Add(item);
		}

		// Token: 0x0600423A RID: 16954 RVA: 0x0013D794 File Offset: 0x0013B994
		private PersuasionTask FindTaskOfOption(PersuasionOptionArgs optionChosenWithLine)
		{
			foreach (PersuasionTask persuasionTask in this._allReservations)
			{
				using (List<PersuasionOptionArgs>.Enumerator enumerator2 = persuasionTask.Options.GetEnumerator())
				{
					while (enumerator2.MoveNext())
					{
						if (enumerator2.Current.Line == optionChosenWithLine.Line)
						{
							return persuasionTask;
						}
					}
				}
			}
			return null;
		}

		// Token: 0x0600423B RID: 16955 RVA: 0x0013D834 File Offset: 0x0013BA34
		private void conversation_on_end_persuasion_on_consequence()
		{
			this._allReservations = null;
			ConversationManager.EndPersuasion();
		}

		// Token: 0x0600423C RID: 16956 RVA: 0x0013D844 File Offset: 0x0013BA44
		public bool conversation_lord_player_has_failed_in_defection_on_condition()
		{
			if (this.GetCurrentPersuasionTask().Options.All((PersuasionOptionArgs x) => x.IsBlocked) && !ConversationManager.GetPersuasionProgressSatisfied())
			{
				PersuasionTask anyFailedPersuasionTask = this.GetAnyFailedPersuasionTask();
				if (anyFailedPersuasionTask != null)
				{
					MBTextManager.SetTextVariable("FAILED_PERSUASION_LINE", anyFailedPersuasionTask.FinalFailLine, false);
				}
				return true;
			}
			return false;
		}

		// Token: 0x0600423D RID: 16957 RVA: 0x0013D8A8 File Offset: 0x0013BAA8
		public bool conversation_lord_recruit_check_if_reservations_met_on_condition()
		{
			PersuasionTask currentPersuasionTask = this.GetCurrentPersuasionTask();
			if (currentPersuasionTask == this._allReservations[this._allReservations.Count - 1])
			{
				if (currentPersuasionTask.Options.All((PersuasionOptionArgs x) => x.IsBlocked))
				{
					return false;
				}
			}
			if (!ConversationManager.GetPersuasionProgressSatisfied())
			{
				MBTextManager.SetTextVariable("PERSUASION_TASK_LINE", currentPersuasionTask.SpokenLine, false);
				return true;
			}
			return false;
		}

		// Token: 0x0600423E RID: 16958 RVA: 0x0013D91E File Offset: 0x0013BB1E
		public bool conversation_lord_check_if_ready_to_join_faction_without_barter_on_condition()
		{
			return false;
		}

		// Token: 0x0600423F RID: 16959 RVA: 0x0013D924 File Offset: 0x0013BB24
		public void conversation_lord_defect_to_clan_without_barter_on_consequence()
		{
			Kingdom kingdom = Hero.MainHero.Clan.Kingdom;
			new JoinKingdomAsClanBarterable(Hero.OneToOneConversationHero, kingdom, true).Apply();
			this.defection_successful_on_consequence();
			ConversationManager.EndPersuasion();
		}

		// Token: 0x06004240 RID: 16960 RVA: 0x0013D95D File Offset: 0x0013BB5D
		public bool conversation_lord_check_if_ready_to_join_faction_with_barter_on_condition()
		{
			return ConversationManager.GetPersuasionProgressSatisfied();
		}

		// Token: 0x06004241 RID: 16961 RVA: 0x0013D964 File Offset: 0x0013BB64
		private List<PersuasionTask> GetPersuasionTasksForDefection(Hero forLord, Hero newLiege)
		{
			Hero currentLiege = forLord.MapFaction.Leader;
			LogEntry logEntry = null;
			LogEntry logEntry2 = null;
			LogEntry logEntry3 = null;
			List<LogEntry> gameActionLogs = Campaign.Current.LogEntryHistory.GameActionLogs;
			bool flag = forLord.GetTraitLevel(DefaultTraits.Honor) + forLord.GetTraitLevel(DefaultTraits.Mercy) < 0;
			foreach (LogEntry logEntry4 in gameActionLogs)
			{
				if (logEntry4.GetValueAsPoliticsAbuseOfPower(forLord, currentLiege) > 0 && (logEntry == null || logEntry4.GetValueAsPoliticsAbuseOfPower(forLord, currentLiege) > logEntry.GetValueAsPoliticsAbuseOfPower(forLord, currentLiege)))
				{
					logEntry = logEntry4;
				}
				if (logEntry4.GetValueAsPoliticsShowedWeakness(forLord, currentLiege) > 0 && (logEntry2 == null || logEntry4.GetValueAsPoliticsShowedWeakness(forLord, currentLiege) > logEntry2.GetValueAsPoliticsSlightedClan(forLord, currentLiege)))
				{
					logEntry2 = logEntry4;
				}
				if (logEntry4.GetValueAsPoliticsSlightedClan(forLord, currentLiege) > 0 && (logEntry3 == null || logEntry4.GetValueAsPoliticsSlightedClan(forLord, currentLiege) > logEntry3.GetValueAsPoliticsSlightedClan(forLord, currentLiege)))
				{
					logEntry3 = logEntry4;
				}
			}
			List<PersuasionTask> list = new List<PersuasionTask>();
			StringHelpers.SetCharacterProperties("CURRENT_LIEGE", forLord.MapFaction.Leader.CharacterObject, null, false);
			StringHelpers.SetCharacterProperties("NEW_LIEGE", newLiege.CharacterObject, null, false);
			PersuasionTask persuasionTask = new PersuasionTask(0);
			persuasionTask.SpokenLine = new TextObject("{=PtWQ789Z}I'm not sure I trust you people.", null);
			persuasionTask.ImmediateFailLine = new TextObject("{=u3eGQRn8}I am not entirely comfortable discussing this with you.", null);
			persuasionTask.FinalFailLine = new TextObject("{=yxeyl4LW}I am simply not comfortable discussing this with you.", null);
			float unmodifiedClanLeaderRelationshipWithPlayer = Hero.OneToOneConversationHero.GetUnmodifiedClanLeaderRelationshipWithPlayer();
			PersuasionArgumentStrength persuasionArgumentStrength;
			if (unmodifiedClanLeaderRelationshipWithPlayer <= -10f)
			{
				persuasionTask.SpokenLine = new TextObject("{=GtIpsut6}I don't even like you. You expect me to discuss something like this with you?", null);
				persuasionArgumentStrength = PersuasionArgumentStrength.VeryHard;
			}
			else if (unmodifiedClanLeaderRelationshipWithPlayer <= 0f)
			{
				persuasionTask.SpokenLine = new TextObject("{=Owa28Kpr}I barely know you, and you're asking me to talk treason?", null);
				persuasionArgumentStrength = PersuasionArgumentStrength.Hard;
			}
			else if (unmodifiedClanLeaderRelationshipWithPlayer >= 20f)
			{
				persuasionTask.SpokenLine = new TextObject("{=HM7auUMA}You are my friend, but even so, this is a risky conversation to have.", null);
				persuasionArgumentStrength = PersuasionArgumentStrength.Easy;
			}
			else
			{
				persuasionTask.SpokenLine = new TextObject("{=arBQHbWv}I'm not sure I know you well enough to discuss something like this.", null);
				persuasionArgumentStrength = PersuasionArgumentStrength.Normal;
			}
			if (unmodifiedClanLeaderRelationshipWithPlayer >= 20f)
			{
				PersuasionOptionArgs option = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Honor, TraitEffect.Positive, persuasionArgumentStrength, true, new TextObject("{=qsnh0KGS}As your friend, I give you my word that I won't breathe a word to anyone else about this conversation.", null), null, false, true, false);
				persuasionTask.AddOptionToTask(option);
			}
			else if (forLord.GetTraitLevel(DefaultTraits.Honor) > 0)
			{
				TextObject textObject = new TextObject("{=yZWBDAG0}You are known as a {?LORD.GENDER}woman{?}man{\\?} of honor. You may know me as one as well.", null);
				PersuasionOptionArgs option2 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Honor, TraitEffect.Positive, persuasionArgumentStrength, true, textObject, null, false, true, false);
				StringHelpers.SetCharacterProperties("LORD", forLord.CharacterObject, textObject, false);
				persuasionTask.AddOptionToTask(option2);
			}
			else if (forLord.GetTraitLevel(DefaultTraits.Honor) == 0)
			{
				TextObject textObject2 = new TextObject("{=0cMibkQO}You may know me as a {?PLAYER.GENDER}woman{?}man{\\?} of honor.", null);
				PersuasionOptionArgs option3 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Honor, TraitEffect.Positive, persuasionArgumentStrength, true, textObject2, null, false, true, false);
				StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject2, false);
				persuasionTask.AddOptionToTask(option3);
			}
			if (Hero.OneToOneConversationHero.GetTraitLevel(DefaultTraits.Mercy) > 0 && unmodifiedClanLeaderRelationshipWithPlayer < 20f)
			{
				PersuasionOptionArgs option4 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Mercy, TraitEffect.Positive, persuasionArgumentStrength, false, new TextObject("{=ch6zCk2w}You know me as someone who seeks to avoid bloodshed.", null), null, false, true, false);
				persuasionTask.AddOptionToTask(option4);
			}
			else if (Hero.OneToOneConversationHero.GetTraitLevel(DefaultTraits.Valor) > 0 && unmodifiedClanLeaderRelationshipWithPlayer < 20f)
			{
				PersuasionOptionArgs option5 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Valor, TraitEffect.Positive, persuasionArgumentStrength - 1, true, new TextObject("{=I5f6Xg3a}You must have heard of my deeds. I speak to you as one warrior to another.", null), null, false, true, false);
				persuasionTask.AddOptionToTask(option5);
			}
			if (unmodifiedClanLeaderRelationshipWithPlayer >= 20f)
			{
				PersuasionOptionArgs option6 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Calculating, TraitEffect.Positive, persuasionArgumentStrength, false, new TextObject("{=8wUfQc4W}You know me. I'll be careful not to get this get back to the wrong ears.", null), null, false, true, false);
				persuasionTask.AddOptionToTask(option6);
			}
			else
			{
				PersuasionOptionArgs option7 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Calculating, TraitEffect.Positive, persuasionArgumentStrength - 1, false, new TextObject("{=VA8BTMBR}You must know of my reputation. You know that it's not in my interest to betray your trust.", null), null, false, true, false);
				persuasionTask.AddOptionToTask(option7);
			}
			list.Add(persuasionTask);
			PersuasionTask persuasionTask2 = new PersuasionTask(1);
			persuasionTask2.ImmediateFailLine = new TextObject("{=VnECJbmq}That is not enough.", null);
			persuasionTask2.FinalFailLine = new TextObject("{=KbQQV5rI}My oath is my oath. I cannot break it. (Oath persuasion failed.)", null);
			PersuasionArgumentStrength persuasionArgumentStrength2;
			if (forLord.IsEnemy(currentLiege) && logEntry != null)
			{
				persuasionTask2.SpokenLine = new TextObject("{=QY55NgWl}I gave an oath to {CURRENT_LIEGE.LINK} - though I'm not sure he's always kept his oath to me.", null);
				persuasionArgumentStrength2 = PersuasionArgumentStrength.Easy;
			}
			else if (forLord.GetTraitLevel(DefaultTraits.Honor) > 0)
			{
				persuasionTask2.SpokenLine = new TextObject("{=4HWFvX8M}I gave an oath to my liege. To break it, even for a good reason, would be a great stain on my honor.", null);
				persuasionArgumentStrength2 = PersuasionArgumentStrength.VeryHard;
			}
			else if (flag && logEntry2 != null)
			{
				persuasionTask2.SpokenLine = new TextObject("{=wOKF17ta}I gave an oath to {CURRENT_LIEGE.LINK} - though no oath binds me to serve a weak leader who'll take us all down.", null);
				persuasionArgumentStrength2 = PersuasionArgumentStrength.Easy;
			}
			else if (forLord.GetTraitLevel(DefaultTraits.Mercy) > 0 && newLiege.GetTraitLevel(DefaultTraits.Mercy) < 0)
			{
				persuasionTask2.SpokenLine = new TextObject("{=GlRZN1J5}I gave an oath to {CURRENT_LIEGE.LINK} - though no oath binds me to serve a weak leader who is too softhearted to rule.", null);
				persuasionArgumentStrength2 = PersuasionArgumentStrength.Easy;
			}
			else if ((forLord.GetTraitLevel(DefaultTraits.Egalitarian) > 0 && newLiege.GetTraitLevel(DefaultTraits.Oligarchic) > 0) || newLiege.GetTraitLevel(DefaultTraits.Authoritarian) > 0)
			{
				persuasionTask2.SpokenLine = new TextObject("{=CymOFgzv}I gave an oath to {CURRENT_LIEGE.LINK} - but {?CURRENT_LIEGE.GENDER}her{?}his{\\?} disregard for the common people of this realm does give me pause.", null);
				persuasionArgumentStrength2 = PersuasionArgumentStrength.Easy;
			}
			else if ((forLord.GetTraitLevel(DefaultTraits.Oligarchic) > 0 && newLiege.GetTraitLevel(DefaultTraits.Egalitarian) > 0) || newLiege.GetTraitLevel(DefaultTraits.Authoritarian) > 0)
			{
				persuasionTask2.SpokenLine = new TextObject("{=EYQI9HJv}I gave an oath to {CURRENT_LIEGE.LINK} - but {?CURRENT_LIEGE.GENDER}her{?}his{\\?} disregard for the laws of this realm does give me pause.", null);
				persuasionArgumentStrength2 = PersuasionArgumentStrength.Easy;
			}
			else
			{
				persuasionTask2.SpokenLine = new TextObject("{=VJoCtAvz}I gave an oath to my liege.", null);
				persuasionArgumentStrength2 = PersuasionArgumentStrength.Normal;
			}
			if (currentLiege.GetTraitLevel(DefaultTraits.Honor) + currentLiege.GetTraitLevel(DefaultTraits.Mercy) < 0)
			{
				PersuasionOptionArgs option8 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Mercy, TraitEffect.Positive, PersuasionArgumentStrength.Normal, false, new TextObject("{=ITqVF9i4}You know {CURRENT_LIEGE.NAME} asks you to do dishonorable things, and no oath binds you to doing evil.", null), null, false, true, false);
				persuasionTask2.AddOptionToTask(option8);
			}
			if (currentLiege.GetTraitLevel(DefaultTraits.Honor) < 0)
			{
				PersuasionOptionArgs option9 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Honor, TraitEffect.Positive, persuasionArgumentStrength2 + 1, false, new TextObject("{=5lq4HNU5}{CURRENT_LIEGE.NAME} is not known for keeping his word.", null), null, false, true, false);
				persuasionTask2.AddOptionToTask(option9);
			}
			if (logEntry != null || currentLiege.GetTraitLevel(DefaultTraits.Honor) <= 0)
			{
				PersuasionOptionArgs option10 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Honor, TraitEffect.Positive, persuasionArgumentStrength2 + 1, false, new TextObject("{=nQStXojH}If {?CURRENT_LIEGE.GENDER}she{?}he{\\?} ever violated {?CURRENT_LIEGE.GENDER}her{?}his{\\?} oath to you, it absolves you of your duty to {?CURRENT_LIEGE.GENDER}her{?}him{\\?}.", null), null, false, true, false);
				persuasionTask2.AddOptionToTask(option10);
			}
			PersuasionOptionArgs option11 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Calculating, TraitEffect.Positive, PersuasionArgumentStrength.Hard, false, new TextObject("{=lhnuawq3}You know very well that in politics oaths are easily made, and just as easily broken.", null), null, false, true, false);
			persuasionTask2.AddOptionToTask(option11);
			list.Add(persuasionTask2);
			PersuasionTask persuasionTask3 = new PersuasionTask(2);
			persuasionTask3.FinalFailLine = new TextObject("{=5E2bIcGb}I will not betray my liege. (Loyalty persuasion failed.)", null);
			persuasionTask3.SpokenLine = new TextObject("{=ttpan5jp}{CURRENT_LIEGE.LINK} and I have been through a great deal together.", null);
			PersuasionArgumentStrength persuasionArgumentStrength3;
			if (logEntry3 != null)
			{
				persuasionTask3.SpokenLine = new TextObject("{=IoaAvgRD}You know {NEW_LIEGE.LINK} have had our differences. You expect me to change sides for him?", null);
				persuasionArgumentStrength3 = PersuasionArgumentStrength.Hard;
			}
			else if (forLord.IsEnemy(newLiege))
			{
				persuasionTask3.SpokenLine = new TextObject("{=awaFsZ5l}I have always stood by {CURRENT_LIEGE.LINK}. Whether {CURRENT_LIEGE.LINK} has stood by me or not is another question...", null);
				persuasionArgumentStrength3 = PersuasionArgumentStrength.VeryEasy;
			}
			else if (forLord.IsFriend(currentLiege))
			{
				persuasionTask3.SpokenLine = new TextObject("{=PGkFvo77}{CURRENT_LIEGE.LINK} is a friend of mine. I cannot imagine betraying {?CURRENT_LIEGE.GENDER}her{?}him{\\?}.", null);
				persuasionArgumentStrength3 = PersuasionArgumentStrength.VeryHard;
			}
			else if ((forLord.GetTraitLevel(DefaultTraits.Egalitarian) > 0 && currentLiege.GetTraitLevel(DefaultTraits.Egalitarian) > 0) || (forLord.GetTraitLevel(DefaultTraits.Oligarchic) > 0 && currentLiege.GetTraitLevel(DefaultTraits.Oligarchic) > 0) || (forLord.GetTraitLevel(DefaultTraits.Authoritarian) > 0 && currentLiege.GetTraitLevel(DefaultTraits.Authoritarian) > 0))
			{
				persuasionTask3.SpokenLine = new TextObject("{=Xlb7Xxyl}{CURRENT_LIEGE.LINK} stands for what I believe in.", null);
				persuasionArgumentStrength3 = PersuasionArgumentStrength.Hard;
			}
			else if ((forLord.GetTraitLevel(DefaultTraits.Mercy) > 0 && currentLiege.GetTraitLevel(DefaultTraits.Mercy) > 0) || (forLord.GetTraitLevel(DefaultTraits.Honor) > 0 && currentLiege.GetTraitLevel(DefaultTraits.Honor) > 0))
			{
				persuasionTask3.SpokenLine = new TextObject("{=LtDqAAk4}I consider {CURRENT_LIEGE.LINK} to be an upright ruler. {NEW_LIEGE.LINK} is not.", null);
				persuasionArgumentStrength3 = PersuasionArgumentStrength.Hard;
			}
			else
			{
				persuasionArgumentStrength3 = PersuasionArgumentStrength.Normal;
			}
			CultureObject culture = Hero.MainHero.Culture;
			if (Hero.MainHero.Clan.Kingdom != null)
			{
				culture = Hero.MainHero.Clan.Kingdom.Culture;
			}
			if (forLord.Culture != culture && persuasionArgumentStrength3 >= PersuasionArgumentStrength.Normal)
			{
				TextObject textObject3 = new TextObject("{=6lbjddM8}{PRIOR_LINE} We have been together in many wars. Including many against your {?IS_SAME_CULTURE}people{?}allies{\\?}, the {ETHNIC_TERM}, I should add.", null);
				textObject3.SetTextVariable("PRIOR_LINE", persuasionTask3.SpokenLine);
				textObject3.SetTextVariable("ETHNIC_TERM", GameTexts.FindText("str_neutral_term_for_culture", culture.StringId));
				textObject3.SetTextVariable("IS_SAME_CULTURE", (Hero.MainHero.Culture == culture) ? 1 : 0);
				persuasionTask3.SpokenLine = textObject3;
			}
			if (currentLiege.IsEnemy(forLord))
			{
				PersuasionOptionArgs option12 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Generosity, TraitEffect.Positive, persuasionArgumentStrength3, false, new TextObject("{=z5cLVzC8}It's well known that you and {CURRENT_LIEGE.NAME} loathe each other.", null), null, false, true, false);
				persuasionTask3.AddOptionToTask(option12);
			}
			else if (currentLiege.GetTraitLevel(DefaultTraits.Generosity) < 0)
			{
				PersuasionOptionArgs option13 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Generosity, TraitEffect.Positive, persuasionArgumentStrength3, false, new TextObject("{=ZzR9VTU0}{CURRENT_LIEGE.NAME} isn't known for his sense of loyalty. Why do you feel so much to him?", null), null, false, true, false);
				persuasionTask3.AddOptionToTask(option13);
			}
			else
			{
				PersuasionOptionArgs option14 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Valor, TraitEffect.Positive, persuasionArgumentStrength3 - 1, false, new TextObject("{=abkmGhLH}Has {CURRENT_LIEGE.NAME} really repaid you for your service as you deserve?", null), null, false, true, false);
				persuasionTask3.AddOptionToTask(option14);
			}
			if (HeroHelper.NPCPoliticalDifferencesWithNPC(forLord, newLiege) && !HeroHelper.NPCPoliticalDifferencesWithNPC(forLord, currentLiege))
			{
				PersuasionOptionArgs option15 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Valor, TraitEffect.Positive, persuasionArgumentStrength3 + 1, false, new TextObject("{=OdS0e6Sb}{NEW_LIEGE.NAME} stands up for what you believe in, while {CURRENT_LIEGE.NAME} does not.", null), null, false, true, false);
				persuasionTask3.AddOptionToTask(option15);
			}
			if (forLord.GetTraitLevel(DefaultTraits.Mercy) > 0 && currentLiege.GetTraitLevel(DefaultTraits.Mercy) < 0 && newLiege.GetTraitLevel(DefaultTraits.Mercy) >= 0)
			{
				PersuasionOptionArgs option16 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Mercy, TraitEffect.Positive, persuasionArgumentStrength3, true, new TextObject("{=9cZeHcAC}The cruelty of {CURRENT_LIEGE.NAME} is legendary. Who cares what he stands for if the realm is drenched in blood?", null), null, false, true, false);
				persuasionTask3.AddOptionToTask(option16);
			}
			PersuasionOptionArgs option17 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Calculating, TraitEffect.Positive, persuasionArgumentStrength3, false, new TextObject("{=y3xguaCc}Put your interests and the good of the realm first. There's too much at stake for that.", null), null, false, true, false);
			persuasionTask3.AddOptionToTask(option17);
			list.Add(persuasionTask3);
			PersuasionTask persuasionTask4 = new PersuasionTask(4);
			persuasionTask4.FinalFailLine = new TextObject("{=2P9mMbrq}It is not in my interest to change sides. (Self-interest persuasion failed.)", null);
			float renown = newLiege.Clan.Renown;
			List<Settlement> list2 = (from x in Settlement.All
				where x.MapFaction == newLiege.MapFaction
				select x).ToList<Settlement>();
			List<Settlement> list3 = (from x in Settlement.All
				where x.MapFaction == currentLiege.MapFaction
				select x).ToList<Settlement>();
			PersuasionArgumentStrength argumentStrength;
			if (renown < 1000f && newLiege == Hero.MainHero)
			{
				persuasionTask4.SpokenLine = new TextObject("{=p2rTaKo8}You have no claim to the throne. Even in the unlikely case that others follow you, another usurper will just rise up to defeat you.", null);
				argumentStrength = PersuasionArgumentStrength.VeryHard;
			}
			else if (list2.Count * 3 < list3.Count)
			{
				persuasionTask4.SpokenLine = new TextObject("{=A6E74QyR}You are badly outnumbered. A rebellion should at least have a chance of success.", null);
				argumentStrength = PersuasionArgumentStrength.VeryHard;
			}
			else if (list2.Count < list3.Count)
			{
				persuasionTask4.SpokenLine = new TextObject("{=ZQa7tXdK}You are somewhat outnumbered. Even if I agree with you, it would be wise of me to wait.", null);
				argumentStrength = PersuasionArgumentStrength.Hard;
			}
			else
			{
				persuasionTask4.SpokenLine = new TextObject("{=GEBRuVcZ}Why change sides now? Once one declares oneself a rebel, there is usually no going back.", null);
				argumentStrength = PersuasionArgumentStrength.Normal;
			}
			if (forLord.GetTraitLevel(DefaultTraits.Valor) > 0)
			{
				PersuasionOptionArgs option18 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Valor, TraitEffect.Positive, PersuasionArgumentStrength.Normal, true, new TextObject("{=XFzbzt3W}You are known for your valor. Fortune favors the bold. Together, we will win this war quickly.", null), null, false, true, false);
				persuasionTask4.AddOptionToTask(option18);
			}
			else if (forLord.GetTraitLevel(DefaultTraits.Valor) > 0)
			{
				PersuasionOptionArgs option19 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Valor, TraitEffect.Positive, PersuasionArgumentStrength.Hard, true, new TextObject("{=7QdKwOhY}Fortune favors the bold. With you with us, we will win this war quickly.", null), null, false, true, false);
				persuasionTask4.AddOptionToTask(option19);
			}
			PersuasionOptionArgs option20 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Calculating, TraitEffect.Positive, PersuasionArgumentStrength.Normal, false, new TextObject("{=dGkJi1yb}I have a strategy to win. And my strategies always work, eventually.", null), null, false, true, false);
			persuasionTask4.AddOptionToTask(option20);
			if (forLord.GetTraitLevel(DefaultTraits.Honor) + forLord.GetTraitLevel(DefaultTraits.Valor) < 0)
			{
				PersuasionOptionArgs option21 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Honor, TraitEffect.Positive, PersuasionArgumentStrength.Normal, false, new TextObject("{=IpnQP7A1}Better to die fighting for a just ruler than to live under an unjust one.", null), null, false, true, false);
				persuasionTask4.AddOptionToTask(option21);
			}
			if (Hero.MainHero == newLiege)
			{
				PersuasionOptionArgs option22 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Generosity, TraitEffect.Positive, argumentStrength, false, new TextObject("{=a37zTVVe}Believe me, I'll be generous to those who came to me early. Perhaps not as generous to those who came late.", null), null, false, true, false);
				persuasionTask4.AddOptionToTask(option22);
			}
			else
			{
				PersuasionOptionArgs option23 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Honor, TraitEffect.Positive, PersuasionArgumentStrength.Normal, false, new TextObject("{=aMICdOjq}{NEW_LIEGE.NAME} will be grateful to those who backed {?NEW_LIEGE.GENDER}her{?}him{\\?} before {?NEW_LIEGE.GENDER}her{?}his{\\?} victory was assured. Not so much after it's assured.", null), null, false, true, false);
				persuasionTask4.AddOptionToTask(option23);
			}
			list.Add(persuasionTask4);
			return list;
		}

		// Token: 0x06004242 RID: 16962 RVA: 0x0013E608 File Offset: 0x0013C808
		public bool conversation_player_is_asking_to_recruit_enemy_on_condition()
		{
			Kingdom kingdom = Clan.PlayerClan.Kingdom;
			if (kingdom != null && Campaign.Current.Models.DefectionModel.CanHeroDefectToFaction(Hero.OneToOneConversationHero, kingdom) && FactionManager.IsAtWarAgainstFaction(Hero.OneToOneConversationHero.MapFaction, Hero.MainHero.MapFaction))
			{
				Hero.OneToOneConversationHero.MapFaction.Leader.SetTextVariables();
				MBTextManager.SetTextVariable("FACTION_NAME", Hero.MainHero.MapFaction.Name, false);
				return true;
			}
			return false;
		}

		// Token: 0x06004243 RID: 16963 RVA: 0x0013E68C File Offset: 0x0013C88C
		public bool conversation_player_is_asking_to_recruit_neutral_on_condition()
		{
			Kingdom kingdom = Clan.PlayerClan.Kingdom;
			if (kingdom != null && Campaign.Current.Models.DefectionModel.CanHeroDefectToFaction(Hero.OneToOneConversationHero, kingdom) && !FactionManager.IsAtWarAgainstFaction(Hero.OneToOneConversationHero.MapFaction, Hero.MainHero.MapFaction))
			{
				Hero.OneToOneConversationHero.MapFaction.Leader.SetTextVariables();
				MBTextManager.SetTextVariable("FACTION_NAME", Hero.MainHero.MapFaction.Name, false);
				return true;
			}
			return false;
		}

		// Token: 0x06004244 RID: 16964 RVA: 0x0013E70F File Offset: 0x0013C90F
		private bool conversation_suggest_treason_on_condition()
		{
			return false;
		}

		// Token: 0x06004245 RID: 16965 RVA: 0x0013E714 File Offset: 0x0013C914
		public bool conversation_lord_from_ruling_clan_on_condition()
		{
			float num = 0f;
			this._allReservations = this.GetPersuasionTasksForDefection(Hero.OneToOneConversationHero, Hero.MainHero.MapFaction.Leader);
			foreach (PersuasionTask persuasionTask in this._allReservations)
			{
				foreach (PersuasionAttempt persuasionAttempt in this._previousDefectionPersuasionAttempts)
				{
					if (persuasionAttempt.Matches(Hero.OneToOneConversationHero, persuasionTask.ReservationType))
					{
						switch (persuasionAttempt.Result)
						{
						case PersuasionOptionResult.CriticalFailure:
							num -= this._criticalFailValue;
							break;
						case PersuasionOptionResult.Failure:
							num -= 0f;
							break;
						case PersuasionOptionResult.Success:
							num += this._successValue;
							break;
						case PersuasionOptionResult.CriticalSuccess:
							num += this._criticalSuccessValue;
							break;
						}
					}
				}
			}
			if (this._maximumScoreCap > num)
			{
				if (this._previousDefectionPersuasionAttempts.Any((PersuasionAttempt x) => x.PersuadedHero == Hero.OneToOneConversationHero))
				{
					MBTextManager.SetTextVariable("LIEGE_IS_RELATIVE", new TextObject("{=03lc5R2t}You have tried to persuade me before. I will not stand your words again.", null), false);
					return true;
				}
			}
			if (Hero.OneToOneConversationHero.Clan.IsMapFaction)
			{
				return false;
			}
			if (Hero.OneToOneConversationHero.Clan == Hero.OneToOneConversationHero.MapFaction.Leader.Clan)
			{
				TextObject textObject = new TextObject("{=jF4Nl8Au}{NPC_LIEGE.NAME}, {LIEGE_RELATIONSHIP}? Long may {?NPC_LIEGE.GENDER}she{?}he{\\?} live.", null);
				StringHelpers.SetCharacterProperties("NPC_LIEGE", Hero.OneToOneConversationHero.Clan.Leader.CharacterObject, textObject, false);
				textObject.SetTextVariable("LIEGE_RELATIONSHIP", ConversationHelper.HeroRefersToHero(Hero.OneToOneConversationHero, Hero.OneToOneConversationHero.Clan.Leader, true));
				MBTextManager.SetTextVariable("LIEGE_IS_RELATIVE", textObject, false);
				return true;
			}
			if (Hero.OneToOneConversationHero.PartyBelongedTo != null && Hero.OneToOneConversationHero.PartyBelongedTo.Army != null && (Hero.OneToOneConversationHero.PartyBelongedTo.Army.LeaderParty == Hero.OneToOneConversationHero.PartyBelongedTo || Hero.OneToOneConversationHero.PartyBelongedTo.AttachedTo != null))
			{
				MBTextManager.SetTextVariable("LIEGE_IS_RELATIVE", new TextObject("{=MalIalPA}I will not listen to such matters while I'm in an army.", null), false);
				return true;
			}
			return false;
		}

		// Token: 0x06004246 RID: 16966 RVA: 0x0013E974 File Offset: 0x0013CB74
		public bool conversation_lord_redirects_to_clan_leader_on_condition()
		{
			if (Hero.OneToOneConversationHero.Clan.IsMapFaction)
			{
				return false;
			}
			MBTextManager.SetTextVariable("REDIRECT_HERO_RELATIONSHIP", ConversationHelper.HeroRefersToHero(Hero.OneToOneConversationHero, Hero.OneToOneConversationHero.Clan.Leader, true), false);
			return !Hero.OneToOneConversationHero.Clan.IsMapFaction && Hero.OneToOneConversationHero.Clan.Leader != Hero.OneToOneConversationHero;
		}

		// Token: 0x06004247 RID: 16967 RVA: 0x0013E9E5 File Offset: 0x0013CBE5
		private void persuasion_redierect_player_finish_on_consequece()
		{
		}

		// Token: 0x06004248 RID: 16968 RVA: 0x0013E9E8 File Offset: 0x0013CBE8
		private bool conversation_lord_can_recruit_on_condition()
		{
			if (Hero.MainHero.MapFaction.Leader == Hero.MainHero)
			{
				MBTextManager.SetTextVariable("RECRUIT_START", new TextObject("{=Fr7wzk97}I am the rightful ruler of this land. I would like your support.", null), false);
			}
			else if (Hero.MainHero.MapFaction == Hero.OneToOneConversationHero.MapFaction)
			{
				StringHelpers.SetCharacterProperties("CURRENT_LIEGE", Hero.OneToOneConversationHero.MapFaction.Leader.CharacterObject, null, false);
				MBTextManager.SetTextVariable("RECRUIT_START", "{=V7qF7uas}I should lead our people, not {CURRENT_LIEGE.NAME}.", false);
			}
			else
			{
				StringHelpers.SetCharacterProperties("NEW_LIEGE", Hero.MainHero.MapFaction.Leader.CharacterObject, null, false);
				MBTextManager.SetTextVariable("RECRUIT_START", new TextObject("{=UwPs3wmj}My liege {NEW_LIEGE.NAME} would welcome your support. Join us!", null), false);
			}
			return true;
		}

		// Token: 0x06004249 RID: 16969 RVA: 0x0013EAA8 File Offset: 0x0013CCA8
		private void start_lord_defection_persuasion_on_consequence()
		{
			Hero newLiege = Hero.MainHero.MapFaction.Leader;
			if (Hero.MainHero.MapFaction == Hero.OneToOneConversationHero.MapFaction)
			{
				newLiege = Hero.MainHero;
			}
			this._allReservations = this.GetPersuasionTasksForDefection(Hero.OneToOneConversationHero, newLiege);
			this._maximumScoreCap = (float)this._allReservations.Count * 1f;
			float num = 0f;
			foreach (PersuasionTask persuasionTask in this._allReservations)
			{
				foreach (PersuasionAttempt persuasionAttempt in this._previousDefectionPersuasionAttempts)
				{
					if (persuasionAttempt.Matches(Hero.OneToOneConversationHero, persuasionTask.ReservationType))
					{
						switch (persuasionAttempt.Result)
						{
						case PersuasionOptionResult.CriticalFailure:
							num -= this._criticalFailValue;
							break;
						case PersuasionOptionResult.Failure:
							num -= 0f;
							break;
						case PersuasionOptionResult.Success:
							num += this._successValue;
							break;
						case PersuasionOptionResult.CriticalSuccess:
							num += this._criticalSuccessValue;
							break;
						}
					}
				}
			}
			ConversationManager.StartPersuasion(this._maximumScoreCap, this._successValue, this._failValue, this._criticalSuccessValue, this._criticalFailValue, num, PersuasionDifficulty.Medium);
		}

		// Token: 0x0600424A RID: 16970 RVA: 0x0013EC18 File Offset: 0x0013CE18
		private void OnDailyTick()
		{
			this.RemoveOldAttempts();
		}

		// Token: 0x0600424B RID: 16971 RVA: 0x0013EC20 File Offset: 0x0013CE20
		private void conversation_clear_persuasion_on_consequence()
		{
			this.ClearPersuasion();
		}

		// Token: 0x0600424C RID: 16972 RVA: 0x0013EC28 File Offset: 0x0013CE28
		private void conversation_leave_faction_barter_consequence()
		{
			BarterManager instance = BarterManager.Instance;
			Hero mainHero = Hero.MainHero;
			Hero oneToOneConversationHero = Hero.OneToOneConversationHero;
			PartyBase mainParty = PartyBase.MainParty;
			MobileParty partyBelongedTo = Hero.OneToOneConversationHero.PartyBelongedTo;
			instance.StartBarterOffer(mainHero, oneToOneConversationHero, mainParty, (partyBelongedTo != null) ? partyBelongedTo.Party : null, null, new BarterManager.BarterContextInitializer(BarterManager.Instance.InitializeJoinFactionBarterContext), 0, false, new Barterable[]
			{
				new JoinKingdomAsClanBarterable(Hero.OneToOneConversationHero, Clan.PlayerClan.Kingdom, true)
			});
			this._allReservations = null;
			ConversationManager.EndPersuasion();
			if (Hero.OneToOneConversationHero.PartyBelongedTo != null && !Hero.OneToOneConversationHero.PartyBelongedTo.MapFaction.IsAtWarWith(MobileParty.MainParty.MapFaction))
			{
				PlayerEncounter.LeaveEncounter = true;
			}
		}

		// Token: 0x0600424D RID: 16973 RVA: 0x0013ECD4 File Offset: 0x0013CED4
		private bool CanAttemptToPersuade(Hero targetHero, int reservationType)
		{
			foreach (PersuasionAttempt persuasionAttempt in this._previousDefectionPersuasionAttempts)
			{
				if (persuasionAttempt.Matches(targetHero, reservationType) && !persuasionAttempt.IsSuccesful() && persuasionAttempt.GameTime.ElapsedWeeksUntilNow < 1f)
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x0600424E RID: 16974 RVA: 0x0013ED50 File Offset: 0x0013CF50
		private void RemoveOldAttempts()
		{
			for (int i = this._previousDefectionPersuasionAttempts.Count - 1; i >= 0; i--)
			{
				if (this._previousDefectionPersuasionAttempts[i].GameTime.ElapsedYearsUntilNow > 1f)
				{
					this._previousDefectionPersuasionAttempts.RemoveAt(i);
				}
			}
		}

		// Token: 0x040012E7 RID: 4839
		private const PersuasionDifficulty _difficulty = PersuasionDifficulty.Medium;

		// Token: 0x040012E8 RID: 4840
		private List<PersuasionTask> _allReservations;

		// Token: 0x040012E9 RID: 4841
		[SaveableField(1)]
		private List<PersuasionAttempt> _previousDefectionPersuasionAttempts;

		// Token: 0x040012EA RID: 4842
		private float _maximumScoreCap;

		// Token: 0x040012EB RID: 4843
		private float _successValue = 1f;

		// Token: 0x040012EC RID: 4844
		private float _criticalSuccessValue = 2f;

		// Token: 0x040012ED RID: 4845
		private float _criticalFailValue = 2f;

		// Token: 0x040012EE RID: 4846
		private float _failValue;

		// Token: 0x0200081A RID: 2074
		public class LordDefectionCampaignBehaviorTypeDefiner : SaveableTypeDefiner
		{
			// Token: 0x0600661B RID: 26139 RVA: 0x001C1F70 File Offset: 0x001C0170
			public LordDefectionCampaignBehaviorTypeDefiner()
				: base(100000)
			{
			}

			// Token: 0x0600661C RID: 26140 RVA: 0x001C1F7D File Offset: 0x001C017D
			protected override void DefineEnumTypes()
			{
				base.AddEnumDefinition(typeof(LordDefectionCampaignBehavior.DefectionReservationType), 1, null);
			}
		}

		// Token: 0x0200081B RID: 2075
		private enum DefectionReservationType
		{
			// Token: 0x04002279 RID: 8825
			LordDefectionPlayerTrust,
			// Token: 0x0400227A RID: 8826
			LordDefectionOathToLiege,
			// Token: 0x0400227B RID: 8827
			LordDefectionLoyalty,
			// Token: 0x0400227C RID: 8828
			LordDefectionPolicy,
			// Token: 0x0400227D RID: 8829
			LordDefectionSelfinterest
		}
	}
}
