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

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class LordDefectionCampaignBehavior : CampaignBehaviorBase
{
	public class LordDefectionCampaignBehaviorTypeDefiner : SaveableTypeDefiner
	{
		public LordDefectionCampaignBehaviorTypeDefiner()
			: base(100000)
		{
		}

		protected override void DefineEnumTypes()
		{
			AddEnumDefinition(typeof(DefectionReservationType), 1);
		}
	}

	private enum DefectionReservationType
	{
		LordDefectionPlayerTrust,
		LordDefectionOathToLiege,
		LordDefectionLoyalty,
		LordDefectionPolicy,
		LordDefectionSelfinterest
	}

	private const PersuasionDifficulty _difficulty = PersuasionDifficulty.Medium;

	private List<PersuasionTask> _allReservations;

	[SaveableField(1)]
	private List<PersuasionAttempt> _previousDefectionPersuasionAttempts;

	private float _maximumScoreCap;

	private float _successValue = 1f;

	private float _criticalSuccessValue = 2f;

	private float _criticalFailValue = 2f;

	private float _failValue;

	public LordDefectionCampaignBehavior()
	{
		_previousDefectionPersuasionAttempts = new List<PersuasionAttempt>();
	}

	public override void RegisterEvents()
	{
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
		CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("previousPersuasionAttempts", ref _previousDefectionPersuasionAttempts);
	}

	public void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
	{
		AddDialogs(campaignGameStarter);
	}

	public void ClearPersuasion()
	{
		_previousDefectionPersuasionAttempts.Clear();
	}

	private PersuasionTask GetFailedPersuasionTask(DefectionReservationType reservationType)
	{
		foreach (PersuasionTask allReservation in _allReservations)
		{
			if (allReservation.ReservationType == (int)reservationType && !CanAttemptToPersuade(Hero.OneToOneConversationHero, (int)reservationType))
			{
				return allReservation;
			}
		}
		return null;
	}

	private PersuasionTask GetAnyFailedPersuasionTask()
	{
		foreach (PersuasionTask allReservation in _allReservations)
		{
			if (!CanAttemptToPersuade(Hero.OneToOneConversationHero, allReservation.ReservationType))
			{
				return allReservation;
			}
		}
		return null;
	}

	private PersuasionTask GetCurrentPersuasionTask()
	{
		foreach (PersuasionTask allReservation in _allReservations)
		{
			if (!allReservation.Options.All((PersuasionOptionArgs x) => x.IsBlocked))
			{
				return allReservation;
			}
		}
		return _allReservations[_allReservations.Count - 1];
	}

	protected void AddDialogs(CampaignGameStarter starter)
	{
		AddLordDefectionPersuasionOptions(starter);
		starter.AddPlayerLine("lord_ask_recruit_argument_1", "lord_ask_recruit_persuasion", "lord_defection_reaction", "{=!}{DEFECTION_PERSUADE_ATTEMPT_1}", conversation_lord_recruit_1_persuade_option_on_condition, conversation_lord_recruit_1_persuade_option_on_consequence, 100, persuasionOptionDelegate: SetupDefectionPersuasionOption1, clickableConditionDelegate: DefectionPersuasionOption1ClickableOnCondition1);
		starter.AddPlayerLine("lord_ask_recruit_argument_2", "lord_ask_recruit_persuasion", "lord_defection_reaction", "{=!}{DEFECTION_PERSUADE_ATTEMPT_2}", conversation_lord_recruit_2_persuade_option_on_condition, conversation_lord_recruit_2_persuade_option_on_consequence, 100, persuasionOptionDelegate: SetupDefectionPersuasionOption2, clickableConditionDelegate: DefectionPersuasionOption2ClickableOnCondition2);
		starter.AddPlayerLine("lord_ask_recruit_argument_3", "lord_ask_recruit_persuasion", "lord_defection_reaction", "{=!}{DEFECTION_PERSUADE_ATTEMPT_3}", conversation_lord_recruit_3_persuade_option_on_condition, conversation_lord_recruit_3_persuade_option_on_consequence, 100, persuasionOptionDelegate: SetupDefectionPersuasionOption3, clickableConditionDelegate: DefectionPersuasionOption3ClickableOnCondition3);
		starter.AddPlayerLine("lord_ask_recruit_argument_4", "lord_ask_recruit_persuasion", "lord_defection_reaction", "{=!}{DEFECTION_PERSUADE_ATTEMPT_4}", conversation_lord_recruit_4_persuade_option_on_condition, conversation_lord_recruit_4_persuade_option_on_consequence, 100, persuasionOptionDelegate: SetupDefectionPersuasionOption4, clickableConditionDelegate: DefectionPersuasionOption4ClickableOnCondition4);
		starter.AddPlayerLine("lord_ask_recruit_argument_no_answer", "lord_ask_recruit_persuasion", "lord_pretalk", "{=0eAtiZbL}I have no answer to that.", null, conversation_on_end_persuasion_on_consequence);
		starter.AddDialogLine("lord_ask_recruit_argument_reaction", "lord_defection_reaction", "lord_defection_next_reservation", "{=!}{PERSUASION_REACTION}", conversation_lord_persuade_option_reaction_on_condition, conversation_lord_persuade_option_reaction_on_consequence);
	}

	private void AddLordDefectionPersuasionOptions(CampaignGameStarter starter)
	{
		starter.AddPlayerLine("player_is_requesting_enemy_change_sides", "lord_talk_speak_diplomacy_2", "persuasion_leave_faction_npc", "{=5a0NhbOA}Your liege, {FIRST_NAME}, is not worth of your loyalty.", conversation_player_is_asking_to_recruit_enemy_on_condition, null);
		starter.AddPlayerLine("player_is_requesting_neutral_change_sides", "lord_talk_speak_diplomacy_2", "persuasion_leave_faction_npc", "{=3gbgjJfZ}Candidly, what do you think of your liege, {FIRST_NAME}?", conversation_player_is_asking_to_recruit_neutral_on_condition, null);
		starter.AddPlayerLine("player_suggesting_treason", "lord_talk_speak_diplomacy_2", "persuasion_leave_faction_npc", "{=bKsb7tcr}Candidly, what do you think of our liege, {FIRST_NAME}?", conversation_suggest_treason_on_condition, null);
		starter.AddPlayerLine("persuasion_leave_faction_player_cheat", "lord_talk_speak_diplomacy_2", "start", "{=Cd405TC7}Clear past persuasion attempts (CHEAT)", () => Game.Current.IsDevelopmentMode && _previousDefectionPersuasionAttempts.Any((PersuasionAttempt x) => x.PersuadedHero == Hero.OneToOneConversationHero), conversation_clear_persuasion_on_consequence);
		starter.AddPlayerLine("player_prisoner_talk", "hero_main_options", "persuasion_leave_faction_npc", "{=wNSH1JdJ}I have an offer for you: join us, and be set free.", conversation_player_start_defection_with_prisoner_on_condition, null);
		starter.AddDialogLine("player_prisoner_talk_pre_barter", "player_prisoner_defection", "persuasion_leave_faction_npc", "{=DRkWMe5X}Even now, I am not sure that's in my best interests...", null, null);
		starter.AddDialogLine("persuasion_leave_faction_npc_refuse", "persuasion_leave_faction_npc", "lord_pretalk", "{=!}{LIEGE_IS_RELATIVE}", conversation_lord_from_ruling_clan_on_condition, null);
		starter.AddDialogLine("persuasion_leave_faction_npc_refuse_high_negative_score", "persuasion_leave_faction_npc", "lord_pretalk", "{=ZYUHljOa}I am happy with my current liege. Neither your purse nor our relationship is deep enough to change that.", conversation_lord_persuade_option_reaction_pre_reject_on_condition, null);
		starter.AddDialogLine("persuasion_leave_faction_npc_redirected", "persuasion_leave_faction_npc", "lord_pretalk", "{=UW1roOES}You should discuss this issue with {REDIRECT_HERO_RELATIONSHIP}, who speaks for our family.", conversation_lord_redirects_to_clan_leader_on_condition, persuasion_redierect_player_finish_on_consequece);
		starter.AddDialogLine("persuasion_leave_faction_npc_answer", "persuasion_leave_faction_npc", "persuasion_leave_faction_player", "{=yub5GWVq}What are you saying, exactly?[if:convo_thinking]", () => !conversation_lord_redirects_to_clan_leader_on_condition(), null);
		starter.AddPlayerLine("persuasion_leave_faction_player_start", "persuasion_leave_faction_player", "lord_defection_next_reservation", "{=!}{RECRUIT_START}", conversation_lord_can_recruit_on_condition, start_lord_defection_persuasion_on_consequence);
		starter.AddDialogLine("lord_ask_recruit_next_reservation_fail", "lord_defection_next_reservation", "lord_pretalk", "{=!}{FAILED_PERSUASION_LINE}", conversation_lord_player_has_failed_in_defection_on_condition, conversation_on_end_persuasion_on_consequence);
		starter.AddDialogLine("lord_ask_recruit_next_reservation_attempt", "lord_defection_next_reservation", "lord_ask_recruit_persuasion", "{=!}{PERSUASION_TASK_LINE}[if:convo_thinking]", conversation_lord_recruit_check_if_reservations_met_on_condition, null);
		starter.AddDialogLine("lord_ask_recruit_next_reservation_success_without_barter", "lord_defection_next_reservation", "close_window", "{=!}{DEFECTION_AGREE_WITHOUT_BARTER}", conversation_lord_check_if_ready_to_join_faction_without_barter_on_condition, conversation_lord_defect_to_clan_without_barter_on_consequence);
		starter.AddDialogLine("lord_ask_recruit_next_reservation_success_with_barter", "lord_defection_next_reservation", "lord_ask_recruit_ai_argument_reaction", "{=BeYbp6M2}Very well. You've convinced me that this is something I can consider.", conversation_lord_check_if_ready_to_join_faction_with_barter_on_condition, null);
		starter.AddDialogLine("persuasion_leave_faction_npc_result_success_2", "lord_ask_recruit_ai_argument_reaction", "lord_defection_barter_line", "{=0dY1xyyK}This is a dangerous step, however, and I'm putting my life and the lives of my people at risk. I need some sort of support from you before I can change my allegiance.[if:convo_stern]", null, null);
		starter.AddDialogLine("persuasion_leave_faction_npc_result_success_open_barter", "lord_defection_barter_line", "lord_defection_post_barter", "{=!}BARTER LINE - Covered by barter interface. Please do not remove these lines!", null, conversation_leave_faction_barter_consequence);
		starter.AddDialogLine("lord_defection_post_barter_s", "lord_defection_post_barter", "close_window", "{=9aZgTNiU}Very well. This is a great step to take, but it must be done.[if:convo_calm_friendly][ib:confident]", defection_barter_successful_on_condition, defection_successful_on_consequence);
		starter.AddDialogLine("lord_defection_post_barter_f", "lord_defection_post_barter", "close_window", "{=BO9QV55x}I cannot do what you ask.[if:convo_grave]", () => !defection_barter_successful_on_condition(), null);
	}

	private bool defection_barter_successful_on_condition()
	{
		return Campaign.Current.BarterManager.LastBarterIsAccepted;
	}

	private void defection_successful_on_consequence()
	{
		TraitLevelingHelper.OnPersuasionDefection(Hero.OneToOneConversationHero);
		if (PlayerEncounter.Current != null)
		{
			PlayerEncounter.LeaveEncounter = true;
		}
		foreach (PersuasionAttempt previousDefectionPersuasionAttempt in _previousDefectionPersuasionAttempts)
		{
			if (previousDefectionPersuasionAttempt.PersuadedHero == Hero.OneToOneConversationHero)
			{
				switch (previousDefectionPersuasionAttempt.Result)
				{
				case PersuasionOptionResult.Success:
				{
					int num = ((previousDefectionPersuasionAttempt.Args.ArgumentStrength < PersuasionArgumentStrength.Normal) ? (TaleWorlds.Library.MathF.Abs((int)previousDefectionPersuasionAttempt.Args.ArgumentStrength) * 50) : 50);
					SkillLevelingManager.OnPersuasionSucceeded(Hero.MainHero, previousDefectionPersuasionAttempt.Args.SkillUsed, PersuasionDifficulty.Medium, num);
					break;
				}
				case PersuasionOptionResult.CriticalSuccess:
				{
					int num = ((previousDefectionPersuasionAttempt.Args.ArgumentStrength < PersuasionArgumentStrength.Normal) ? (TaleWorlds.Library.MathF.Abs((int)previousDefectionPersuasionAttempt.Args.ArgumentStrength) * 50) : 50);
					SkillLevelingManager.OnPersuasionSucceeded(Hero.MainHero, previousDefectionPersuasionAttempt.Args.SkillUsed, PersuasionDifficulty.Medium, 2 * num);
					break;
				}
				}
			}
		}
		Campaign.Current.CampaignBehaviorManager.GetBehavior<IStatisticsCampaignBehavior>()?.OnDefectionPersuasionSucess();
	}

	private bool conversation_lord_recruit_1_persuade_option_on_condition()
	{
		PersuasionTask currentPersuasionTask = GetCurrentPersuasionTask();
		if (currentPersuasionTask.Options.Count > 0)
		{
			TextObject textObject = new TextObject("{=bSo9hKwr}{PERSUASION_OPTION_LINE} {SUCCESS_CHANCE}");
			textObject.SetTextVariable("SUCCESS_CHANCE", PersuasionHelper.ShowSuccess(currentPersuasionTask.Options.ElementAt(0)));
			textObject.SetTextVariable("PERSUASION_OPTION_LINE", currentPersuasionTask.Options.ElementAt(0).Line);
			MBTextManager.SetTextVariable("DEFECTION_PERSUADE_ATTEMPT_1", textObject);
			return true;
		}
		return false;
	}

	private void conversation_lord_recruit_1_persuade_option_on_consequence()
	{
		PersuasionTask currentPersuasionTask = GetCurrentPersuasionTask();
		if (currentPersuasionTask.Options.Count > 0)
		{
			currentPersuasionTask.Options[0].BlockTheOption(isBlocked: true);
		}
	}

	private void conversation_lord_recruit_2_persuade_option_on_consequence()
	{
		PersuasionTask currentPersuasionTask = GetCurrentPersuasionTask();
		if (currentPersuasionTask.Options.Count > 1)
		{
			currentPersuasionTask.Options[1].BlockTheOption(isBlocked: true);
		}
	}

	private void conversation_lord_recruit_3_persuade_option_on_consequence()
	{
		PersuasionTask currentPersuasionTask = GetCurrentPersuasionTask();
		if (currentPersuasionTask.Options.Count > 2)
		{
			currentPersuasionTask.Options[2].BlockTheOption(isBlocked: true);
		}
	}

	private void conversation_lord_recruit_4_persuade_option_on_consequence()
	{
		PersuasionTask currentPersuasionTask = GetCurrentPersuasionTask();
		if (currentPersuasionTask.Options.Count > 3)
		{
			currentPersuasionTask.Options[3].BlockTheOption(isBlocked: true);
		}
	}

	private bool DefectionPersuasionOption1ClickableOnCondition1(out TextObject hintText)
	{
		PersuasionTask currentPersuasionTask = GetCurrentPersuasionTask();
		if (currentPersuasionTask.Options.Count > 0)
		{
			hintText = null;
			return !currentPersuasionTask.Options.ElementAt(0).IsBlocked;
		}
		hintText = new TextObject("{=9ACJsI6S}Blocked");
		return false;
	}

	private bool DefectionPersuasionOption2ClickableOnCondition2(out TextObject hintText)
	{
		PersuasionTask currentPersuasionTask = GetCurrentPersuasionTask();
		if (currentPersuasionTask.Options.Count > 1)
		{
			hintText = null;
			return !currentPersuasionTask.Options.ElementAt(1).IsBlocked;
		}
		hintText = new TextObject("{=9ACJsI6S}Blocked");
		return false;
	}

	private bool DefectionPersuasionOption3ClickableOnCondition3(out TextObject hintText)
	{
		PersuasionTask currentPersuasionTask = GetCurrentPersuasionTask();
		if (currentPersuasionTask.Options.Count > 2)
		{
			hintText = null;
			return !currentPersuasionTask.Options.ElementAt(2).IsBlocked;
		}
		hintText = new TextObject("{=9ACJsI6S}Blocked");
		return false;
	}

	private bool DefectionPersuasionOption4ClickableOnCondition4(out TextObject hintText)
	{
		PersuasionTask currentPersuasionTask = GetCurrentPersuasionTask();
		if (currentPersuasionTask.Options.Count > 3)
		{
			hintText = null;
			return !currentPersuasionTask.Options.ElementAt(3).IsBlocked;
		}
		hintText = new TextObject("{=9ACJsI6S}Blocked");
		return false;
	}

	private bool conversation_lord_recruit_2_persuade_option_on_condition()
	{
		PersuasionTask currentPersuasionTask = GetCurrentPersuasionTask();
		if (currentPersuasionTask.Options.Count > 1)
		{
			TextObject textObject = new TextObject("{=bSo9hKwr}{PERSUASION_OPTION_LINE} {SUCCESS_CHANCE}");
			textObject.SetTextVariable("SUCCESS_CHANCE", PersuasionHelper.ShowSuccess(currentPersuasionTask.Options.ElementAt(1)));
			textObject.SetTextVariable("PERSUASION_OPTION_LINE", currentPersuasionTask.Options.ElementAt(1).Line);
			MBTextManager.SetTextVariable("DEFECTION_PERSUADE_ATTEMPT_2", textObject);
			return true;
		}
		return false;
	}

	private bool conversation_lord_recruit_3_persuade_option_on_condition()
	{
		PersuasionTask currentPersuasionTask = GetCurrentPersuasionTask();
		if (currentPersuasionTask.Options.Count > 2)
		{
			TextObject textObject = new TextObject("{=bSo9hKwr}{PERSUASION_OPTION_LINE} {SUCCESS_CHANCE}");
			textObject.SetTextVariable("SUCCESS_CHANCE", PersuasionHelper.ShowSuccess(currentPersuasionTask.Options.ElementAt(2)));
			textObject.SetTextVariable("PERSUASION_OPTION_LINE", currentPersuasionTask.Options.ElementAt(2).Line);
			MBTextManager.SetTextVariable("DEFECTION_PERSUADE_ATTEMPT_3", textObject);
			return true;
		}
		return false;
	}

	private bool conversation_lord_recruit_4_persuade_option_on_condition()
	{
		PersuasionTask currentPersuasionTask = GetCurrentPersuasionTask();
		if (currentPersuasionTask.Options.Count > 3)
		{
			TextObject textObject = new TextObject("{=bSo9hKwr}{PERSUASION_OPTION_LINE} {SUCCESS_CHANCE}");
			textObject.SetTextVariable("SUCCESS_CHANCE", PersuasionHelper.ShowSuccess(currentPersuasionTask.Options.ElementAt(3)));
			textObject.SetTextVariable("PERSUASION_OPTION_LINE", currentPersuasionTask.Options.ElementAt(3).Line);
			MBTextManager.SetTextVariable("DEFECTION_PERSUADE_ATTEMPT_4", textObject);
			return true;
		}
		return false;
	}

	private PersuasionOptionArgs SetupDefectionPersuasionOption1()
	{
		return GetCurrentPersuasionTask().Options.ElementAt(0);
	}

	private PersuasionOptionArgs SetupDefectionPersuasionOption2()
	{
		return GetCurrentPersuasionTask().Options.ElementAt(1);
	}

	private PersuasionOptionArgs SetupDefectionPersuasionOption3()
	{
		return GetCurrentPersuasionTask().Options.ElementAt(2);
	}

	private PersuasionOptionArgs SetupDefectionPersuasionOption4()
	{
		return GetCurrentPersuasionTask().Options.ElementAt(3);
	}

	private bool conversation_player_start_defection_with_prisoner_on_condition()
	{
		if (Hero.OneToOneConversationHero != null && Clan.PlayerClan.Kingdom != null && Hero.MainHero.IsKingdomLeader && Hero.OneToOneConversationHero.Clan?.Leader == Hero.OneToOneConversationHero && Hero.OneToOneConversationHero.HeroState == Hero.CharacterStates.Prisoner && Campaign.Current.CurrentConversationContext != ConversationContext.CapturedLord && Campaign.Current.CurrentConversationContext != ConversationContext.FreeOrCapturePrisonerHero && Hero.OneToOneConversationHero.Clan != Hero.OneToOneConversationHero.MapFaction.Leader.Clan)
		{
			if (Hero.OneToOneConversationHero.PartyBelongedToAsPrisoner == null || Hero.OneToOneConversationHero.PartyBelongedToAsPrisoner != PartyBase.MainParty)
			{
				if (Hero.OneToOneConversationHero.CurrentSettlement != null)
				{
					return Hero.OneToOneConversationHero.CurrentSettlement.OwnerClan == Clan.PlayerClan;
				}
				return false;
			}
			return true;
		}
		return false;
	}

	private bool conversation_lord_persuade_option_reaction_pre_reject_on_condition()
	{
		if (Hero.OneToOneConversationHero.Clan.Leader != Hero.OneToOneConversationHero)
		{
			return false;
		}
		return (float)new JoinKingdomAsClanBarterable(Hero.OneToOneConversationHero, (Kingdom)Hero.MainHero.MapFaction, isDefecting: true).GetValueForFaction(Hero.OneToOneConversationHero.Clan) < 0f - TaleWorlds.Library.MathF.Min(2000000f, TaleWorlds.Library.MathF.Max(500000f, 250000f + (float)Hero.MainHero.Gold / 3f));
	}

	private bool conversation_lord_persuade_option_reaction_on_condition()
	{
		PersuasionOptionResult item = ConversationManager.GetPersuasionChosenOptions().Last().Item2;
		PersuasionTask currentPersuasionTask = GetCurrentPersuasionTask();
		switch (item)
		{
		case PersuasionOptionResult.Failure:
			MBTextManager.SetTextVariable("IMMEDIATE_FAILURE_LINE", currentPersuasionTask?.ImmediateFailLine ?? TextObject.GetEmpty());
			MBTextManager.SetTextVariable("PERSUASION_REACTION", "{=18xOURG4}Hmm.. No... {IMMEDIATE_FAILURE_LINE}");
			break;
		case PersuasionOptionResult.CriticalFailure:
		{
			MBTextManager.SetTextVariable("PERSUASION_REACTION", "{=Lj5Lghww}What? No...");
			TextObject text = currentPersuasionTask?.ImmediateFailLine ?? TextObject.GetEmpty();
			MBTextManager.SetTextVariable("IMMEDIATE_FAILURE_LINE", text);
			MBTextManager.SetTextVariable("PERSUASION_REACTION", "{=18xOURG4}Hmm.. No... {IMMEDIATE_FAILURE_LINE}");
			foreach (PersuasionTask allReservation in _allReservations)
			{
				allReservation.BlockAllOptions();
			}
			break;
		}
		default:
			MBTextManager.SetTextVariable("PERSUASION_REACTION", PersuasionHelper.GetDefaultPersuasionOptionReaction(item));
			break;
		}
		return true;
	}

	private void conversation_lord_persuade_option_reaction_on_consequence()
	{
		Tuple<PersuasionOptionArgs, PersuasionOptionResult> tuple = ConversationManager.GetPersuasionChosenOptions().Last();
		float difficulty = Campaign.Current.Models.PersuasionModel.GetDifficulty(PersuasionDifficulty.Medium);
		Campaign.Current.Models.PersuasionModel.GetEffectChances(tuple.Item1, out var moveToNextStageChance, out var blockRandomOptionChance, difficulty);
		PersuasionTask persuasionTask = FindTaskOfOption(tuple.Item1);
		persuasionTask.ApplyEffects(moveToNextStageChance, blockRandomOptionChance);
		PersuasionAttempt item = new PersuasionAttempt(Hero.OneToOneConversationHero, CampaignTime.Now, tuple.Item1, tuple.Item2, persuasionTask.ReservationType);
		_previousDefectionPersuasionAttempts.Add(item);
	}

	private PersuasionTask FindTaskOfOption(PersuasionOptionArgs optionChosenWithLine)
	{
		foreach (PersuasionTask allReservation in _allReservations)
		{
			foreach (PersuasionOptionArgs option in allReservation.Options)
			{
				if (option.Line == optionChosenWithLine.Line)
				{
					return allReservation;
				}
			}
		}
		return null;
	}

	private void conversation_on_end_persuasion_on_consequence()
	{
		_allReservations = null;
		ConversationManager.EndPersuasion();
	}

	public bool conversation_lord_player_has_failed_in_defection_on_condition()
	{
		if (GetCurrentPersuasionTask().Options.All((PersuasionOptionArgs x) => x.IsBlocked) && !ConversationManager.GetPersuasionProgressSatisfied())
		{
			PersuasionTask anyFailedPersuasionTask = GetAnyFailedPersuasionTask();
			if (anyFailedPersuasionTask != null)
			{
				MBTextManager.SetTextVariable("FAILED_PERSUASION_LINE", anyFailedPersuasionTask.FinalFailLine);
			}
			return true;
		}
		return false;
	}

	public bool conversation_lord_recruit_check_if_reservations_met_on_condition()
	{
		PersuasionTask currentPersuasionTask = GetCurrentPersuasionTask();
		if ((currentPersuasionTask == _allReservations[_allReservations.Count - 1] && currentPersuasionTask.Options.All((PersuasionOptionArgs x) => x.IsBlocked)) || ConversationManager.GetPersuasionProgressSatisfied())
		{
			return false;
		}
		MBTextManager.SetTextVariable("PERSUASION_TASK_LINE", currentPersuasionTask.SpokenLine);
		return true;
	}

	public bool conversation_lord_check_if_ready_to_join_faction_without_barter_on_condition()
	{
		return false;
	}

	public void conversation_lord_defect_to_clan_without_barter_on_consequence()
	{
		Kingdom kingdom = Hero.MainHero.Clan.Kingdom;
		new JoinKingdomAsClanBarterable(Hero.OneToOneConversationHero, kingdom, isDefecting: true).Apply();
		defection_successful_on_consequence();
		ConversationManager.EndPersuasion();
	}

	public bool conversation_lord_check_if_ready_to_join_faction_with_barter_on_condition()
	{
		return ConversationManager.GetPersuasionProgressSatisfied();
	}

	private List<PersuasionTask> GetPersuasionTasksForDefection(Hero forLord, Hero newLiege)
	{
		Hero currentLiege = forLord.MapFaction.Leader;
		LogEntry logEntry = null;
		LogEntry logEntry2 = null;
		LogEntry logEntry3 = null;
		MBReadOnlyList<LogEntry> gameActionLogs = Campaign.Current.LogEntryHistory.GameActionLogs;
		bool flag = forLord.GetTraitLevel(DefaultTraits.Honor) + forLord.GetTraitLevel(DefaultTraits.Mercy) < 0;
		foreach (LogEntry item in gameActionLogs)
		{
			if (item.GetValueAsPoliticsAbuseOfPower(forLord, currentLiege) > 0 && (logEntry == null || item.GetValueAsPoliticsAbuseOfPower(forLord, currentLiege) > logEntry.GetValueAsPoliticsAbuseOfPower(forLord, currentLiege)))
			{
				logEntry = item;
			}
			if (item.GetValueAsPoliticsShowedWeakness(forLord, currentLiege) > 0 && (logEntry2 == null || item.GetValueAsPoliticsShowedWeakness(forLord, currentLiege) > logEntry2.GetValueAsPoliticsSlightedClan(forLord, currentLiege)))
			{
				logEntry2 = item;
			}
			if (item.GetValueAsPoliticsSlightedClan(forLord, currentLiege) > 0 && (logEntry3 == null || item.GetValueAsPoliticsSlightedClan(forLord, currentLiege) > logEntry3.GetValueAsPoliticsSlightedClan(forLord, currentLiege)))
			{
				logEntry3 = item;
			}
		}
		List<PersuasionTask> list = new List<PersuasionTask>();
		StringHelpers.SetCharacterProperties("CURRENT_LIEGE", forLord.MapFaction.Leader.CharacterObject);
		StringHelpers.SetCharacterProperties("NEW_LIEGE", newLiege.CharacterObject);
		PersuasionTask persuasionTask = new PersuasionTask(0)
		{
			SpokenLine = new TextObject("{=PtWQ789Z}I'm not sure I trust you people."),
			ImmediateFailLine = new TextObject("{=u3eGQRn8}I am not entirely comfortable discussing this with you."),
			FinalFailLine = new TextObject("{=yxeyl4LW}I am simply not comfortable discussing this with you.")
		};
		float unmodifiedClanLeaderRelationshipWithPlayer = Hero.OneToOneConversationHero.GetUnmodifiedClanLeaderRelationshipWithPlayer();
		PersuasionArgumentStrength persuasionArgumentStrength = PersuasionArgumentStrength.Normal;
		if (unmodifiedClanLeaderRelationshipWithPlayer <= -10f)
		{
			persuasionTask.SpokenLine = new TextObject("{=GtIpsut6}I don't even like you. You expect me to discuss something like this with you?");
			persuasionArgumentStrength = PersuasionArgumentStrength.VeryHard;
		}
		else if (unmodifiedClanLeaderRelationshipWithPlayer <= 0f)
		{
			persuasionTask.SpokenLine = new TextObject("{=Owa28Kpr}I barely know you, and you're asking me to talk treason?");
			persuasionArgumentStrength = PersuasionArgumentStrength.Hard;
		}
		else if (unmodifiedClanLeaderRelationshipWithPlayer >= 20f)
		{
			persuasionTask.SpokenLine = new TextObject("{=HM7auUMA}You are my friend, but even so, this is a risky conversation to have.");
			persuasionArgumentStrength = PersuasionArgumentStrength.Easy;
		}
		else
		{
			persuasionTask.SpokenLine = new TextObject("{=arBQHbWv}I'm not sure I know you well enough to discuss something like this.");
			persuasionArgumentStrength = PersuasionArgumentStrength.Normal;
		}
		if (unmodifiedClanLeaderRelationshipWithPlayer >= 20f)
		{
			PersuasionOptionArgs option = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Honor, TraitEffect.Positive, persuasionArgumentStrength, givesCriticalSuccess: true, new TextObject("{=qsnh0KGS}As your friend, I give you my word that I won't breathe a word to anyone else about this conversation."), null, canBlockOtherOption: false, canMoveToTheNextReservation: true);
			persuasionTask.AddOptionToTask(option);
		}
		else if (forLord.GetTraitLevel(DefaultTraits.Honor) > 0)
		{
			TextObject textObject = new TextObject("{=yZWBDAG0}You are known as a {?LORD.GENDER}woman{?}man{\\?} of honor. You may know me as one as well.");
			PersuasionOptionArgs option2 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Honor, TraitEffect.Positive, persuasionArgumentStrength, givesCriticalSuccess: true, textObject, null, canBlockOtherOption: false, canMoveToTheNextReservation: true);
			StringHelpers.SetCharacterProperties("LORD", forLord.CharacterObject, textObject);
			persuasionTask.AddOptionToTask(option2);
		}
		else if (forLord.GetTraitLevel(DefaultTraits.Honor) == 0)
		{
			TextObject textObject2 = new TextObject("{=0cMibkQO}You may know me as a {?PLAYER.GENDER}woman{?}man{\\?} of honor.");
			PersuasionOptionArgs option3 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Honor, TraitEffect.Positive, persuasionArgumentStrength, givesCriticalSuccess: true, textObject2, null, canBlockOtherOption: false, canMoveToTheNextReservation: true);
			StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject2);
			persuasionTask.AddOptionToTask(option3);
		}
		if (Hero.OneToOneConversationHero.GetTraitLevel(DefaultTraits.Mercy) > 0 && unmodifiedClanLeaderRelationshipWithPlayer < 20f)
		{
			PersuasionOptionArgs option4 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Mercy, TraitEffect.Positive, persuasionArgumentStrength, givesCriticalSuccess: false, new TextObject("{=ch6zCk2w}You know me as someone who seeks to avoid bloodshed."), null, canBlockOtherOption: false, canMoveToTheNextReservation: true);
			persuasionTask.AddOptionToTask(option4);
		}
		else if (Hero.OneToOneConversationHero.GetTraitLevel(DefaultTraits.Valor) > 0 && unmodifiedClanLeaderRelationshipWithPlayer < 20f)
		{
			PersuasionOptionArgs option5 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Valor, TraitEffect.Positive, persuasionArgumentStrength - 1, givesCriticalSuccess: true, new TextObject("{=I5f6Xg3a}You must have heard of my deeds. I speak to you as one warrior to another."), null, canBlockOtherOption: false, canMoveToTheNextReservation: true);
			persuasionTask.AddOptionToTask(option5);
		}
		if (unmodifiedClanLeaderRelationshipWithPlayer >= 20f)
		{
			PersuasionOptionArgs option6 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Calculating, TraitEffect.Positive, persuasionArgumentStrength, givesCriticalSuccess: false, new TextObject("{=8wUfQc4W}You know me. I'll be careful not to get this get back to the wrong ears."), null, canBlockOtherOption: false, canMoveToTheNextReservation: true);
			persuasionTask.AddOptionToTask(option6);
		}
		else
		{
			PersuasionOptionArgs option7 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Calculating, TraitEffect.Positive, persuasionArgumentStrength - 1, givesCriticalSuccess: false, new TextObject("{=VA8BTMBR}You must know of my reputation. You know that it's not in my interest to betray your trust."), null, canBlockOtherOption: false, canMoveToTheNextReservation: true);
			persuasionTask.AddOptionToTask(option7);
		}
		list.Add(persuasionTask);
		PersuasionTask persuasionTask2 = new PersuasionTask(1)
		{
			ImmediateFailLine = new TextObject("{=VnECJbmq}That is not enough."),
			FinalFailLine = new TextObject("{=KbQQV5rI}My oath is my oath. I cannot break it. (Oath persuasion failed.)")
		};
		PersuasionArgumentStrength persuasionArgumentStrength2;
		if (forLord.IsEnemy(currentLiege) && logEntry != null)
		{
			persuasionTask2.SpokenLine = new TextObject("{=QY55NgWl}I gave an oath to {CURRENT_LIEGE.LINK} - though I'm not sure he's always kept his oath to me.");
			persuasionArgumentStrength2 = PersuasionArgumentStrength.Easy;
		}
		else if (forLord.GetTraitLevel(DefaultTraits.Honor) > 0)
		{
			persuasionTask2.SpokenLine = new TextObject("{=4HWFvX8M}I gave an oath to my liege. To break it, even for a good reason, would be a great stain on my honor.");
			persuasionArgumentStrength2 = PersuasionArgumentStrength.VeryHard;
		}
		else if (flag && logEntry2 != null)
		{
			persuasionTask2.SpokenLine = new TextObject("{=wOKF17ta}I gave an oath to {CURRENT_LIEGE.LINK} - though no oath binds me to serve a weak leader who'll take us all down.");
			persuasionArgumentStrength2 = PersuasionArgumentStrength.Easy;
		}
		else if (forLord.GetTraitLevel(DefaultTraits.Mercy) > 0 && newLiege.GetTraitLevel(DefaultTraits.Mercy) < 0)
		{
			persuasionTask2.SpokenLine = new TextObject("{=GlRZN1J5}I gave an oath to {CURRENT_LIEGE.LINK} - though no oath binds me to serve a weak leader who is too softhearted to rule.");
			persuasionArgumentStrength2 = PersuasionArgumentStrength.Easy;
		}
		else if ((forLord.GetTraitLevel(DefaultTraits.Egalitarian) > 0 && newLiege.GetTraitLevel(DefaultTraits.Oligarchic) > 0) || newLiege.GetTraitLevel(DefaultTraits.Authoritarian) > 0)
		{
			persuasionTask2.SpokenLine = new TextObject("{=CymOFgzv}I gave an oath to {CURRENT_LIEGE.LINK} - but {?CURRENT_LIEGE.GENDER}her{?}his{\\?} disregard for the common people of this realm does give me pause.");
			persuasionArgumentStrength2 = PersuasionArgumentStrength.Easy;
		}
		else if ((forLord.GetTraitLevel(DefaultTraits.Oligarchic) > 0 && newLiege.GetTraitLevel(DefaultTraits.Egalitarian) > 0) || newLiege.GetTraitLevel(DefaultTraits.Authoritarian) > 0)
		{
			persuasionTask2.SpokenLine = new TextObject("{=EYQI9HJv}I gave an oath to {CURRENT_LIEGE.LINK} - but {?CURRENT_LIEGE.GENDER}her{?}his{\\?} disregard for the laws of this realm does give me pause.");
			persuasionArgumentStrength2 = PersuasionArgumentStrength.Easy;
		}
		else
		{
			persuasionTask2.SpokenLine = new TextObject("{=VJoCtAvz}I gave an oath to my liege.");
			persuasionArgumentStrength2 = PersuasionArgumentStrength.Normal;
		}
		if (currentLiege.GetTraitLevel(DefaultTraits.Honor) + currentLiege.GetTraitLevel(DefaultTraits.Mercy) < 0)
		{
			PersuasionOptionArgs option8 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Mercy, TraitEffect.Positive, PersuasionArgumentStrength.Normal, givesCriticalSuccess: false, new TextObject("{=ITqVF9i4}You know {CURRENT_LIEGE.NAME} asks you to do dishonorable things, and no oath binds you to doing evil."), null, canBlockOtherOption: false, canMoveToTheNextReservation: true);
			persuasionTask2.AddOptionToTask(option8);
		}
		if (currentLiege.GetTraitLevel(DefaultTraits.Honor) < 0)
		{
			PersuasionOptionArgs option9 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Honor, TraitEffect.Positive, persuasionArgumentStrength2 + 1, givesCriticalSuccess: false, new TextObject("{=5lq4HNU5}{CURRENT_LIEGE.NAME} is not known for keeping his word."), null, canBlockOtherOption: false, canMoveToTheNextReservation: true);
			persuasionTask2.AddOptionToTask(option9);
		}
		if (logEntry != null || currentLiege.GetTraitLevel(DefaultTraits.Honor) <= 0)
		{
			PersuasionOptionArgs option10 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Honor, TraitEffect.Positive, persuasionArgumentStrength2 + 1, givesCriticalSuccess: false, new TextObject("{=nQStXojH}If {?CURRENT_LIEGE.GENDER}she{?}he{\\?} ever violated {?CURRENT_LIEGE.GENDER}her{?}his{\\?} oath to you, it absolves you of your duty to {?CURRENT_LIEGE.GENDER}her{?}him{\\?}."), null, canBlockOtherOption: false, canMoveToTheNextReservation: true);
			persuasionTask2.AddOptionToTask(option10);
		}
		PersuasionOptionArgs option11 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Calculating, TraitEffect.Positive, PersuasionArgumentStrength.Hard, givesCriticalSuccess: false, new TextObject("{=lhnuawq3}You know very well that in politics oaths are easily made, and just as easily broken."), null, canBlockOtherOption: false, canMoveToTheNextReservation: true);
		persuasionTask2.AddOptionToTask(option11);
		list.Add(persuasionTask2);
		PersuasionTask persuasionTask3 = new PersuasionTask(2)
		{
			FinalFailLine = new TextObject("{=5E2bIcGb}I will not betray my liege. (Loyalty persuasion failed.)"),
			SpokenLine = new TextObject("{=ttpan5jp}{CURRENT_LIEGE.LINK} and I have been through a great deal together.")
		};
		PersuasionArgumentStrength persuasionArgumentStrength3;
		if (logEntry3 != null)
		{
			persuasionTask3.SpokenLine = new TextObject("{=IoaAvgRD}You know {NEW_LIEGE.LINK} have had our differences. You expect me to change sides for him?");
			persuasionArgumentStrength3 = PersuasionArgumentStrength.Hard;
		}
		else if (forLord.IsEnemy(newLiege))
		{
			persuasionTask3.SpokenLine = new TextObject("{=awaFsZ5l}I have always stood by {CURRENT_LIEGE.LINK}. Whether {CURRENT_LIEGE.LINK} has stood by me or not is another question...");
			persuasionArgumentStrength3 = PersuasionArgumentStrength.VeryEasy;
		}
		else if (forLord.IsFriend(currentLiege))
		{
			persuasionTask3.SpokenLine = new TextObject("{=PGkFvo77}{CURRENT_LIEGE.LINK} is a friend of mine. I cannot imagine betraying {?CURRENT_LIEGE.GENDER}her{?}him{\\?}.");
			persuasionArgumentStrength3 = PersuasionArgumentStrength.VeryHard;
		}
		else if ((forLord.GetTraitLevel(DefaultTraits.Egalitarian) > 0 && currentLiege.GetTraitLevel(DefaultTraits.Egalitarian) > 0) || (forLord.GetTraitLevel(DefaultTraits.Oligarchic) > 0 && currentLiege.GetTraitLevel(DefaultTraits.Oligarchic) > 0) || (forLord.GetTraitLevel(DefaultTraits.Authoritarian) > 0 && currentLiege.GetTraitLevel(DefaultTraits.Authoritarian) > 0))
		{
			persuasionTask3.SpokenLine = new TextObject("{=Xlb7Xxyl}{CURRENT_LIEGE.LINK} stands for what I believe in.");
			persuasionArgumentStrength3 = PersuasionArgumentStrength.Hard;
		}
		else if ((forLord.GetTraitLevel(DefaultTraits.Mercy) > 0 && currentLiege.GetTraitLevel(DefaultTraits.Mercy) > 0) || (forLord.GetTraitLevel(DefaultTraits.Honor) > 0 && currentLiege.GetTraitLevel(DefaultTraits.Honor) > 0))
		{
			persuasionTask3.SpokenLine = new TextObject("{=LtDqAAk4}I consider {CURRENT_LIEGE.LINK} to be an upright ruler. {NEW_LIEGE.LINK} is not.");
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
			TextObject textObject3 = new TextObject("{=6lbjddM8}{PRIOR_LINE} We have been together in many wars. Including many against your {?IS_SAME_CULTURE}people{?}allies{\\?}, the {ETHNIC_TERM}, I should add.");
			textObject3.SetTextVariable("PRIOR_LINE", persuasionTask3.SpokenLine);
			textObject3.SetTextVariable("ETHNIC_TERM", GameTexts.FindText("str_neutral_term_for_culture", culture.StringId));
			textObject3.SetTextVariable("IS_SAME_CULTURE", (Hero.MainHero.Culture == culture) ? 1 : 0);
			persuasionTask3.SpokenLine = textObject3;
		}
		if (currentLiege.IsEnemy(forLord))
		{
			PersuasionOptionArgs option12 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Generosity, TraitEffect.Positive, persuasionArgumentStrength3, givesCriticalSuccess: false, new TextObject("{=z5cLVzC8}It's well known that you and {CURRENT_LIEGE.NAME} loathe each other."), null, canBlockOtherOption: false, canMoveToTheNextReservation: true);
			persuasionTask3.AddOptionToTask(option12);
		}
		else if (currentLiege.GetTraitLevel(DefaultTraits.Generosity) < 0)
		{
			PersuasionOptionArgs option13 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Generosity, TraitEffect.Positive, persuasionArgumentStrength3, givesCriticalSuccess: false, new TextObject("{=ZzR9VTU0}{CURRENT_LIEGE.NAME} isn't known for his sense of loyalty. Why do you feel so much to him?"), null, canBlockOtherOption: false, canMoveToTheNextReservation: true);
			persuasionTask3.AddOptionToTask(option13);
		}
		else
		{
			PersuasionOptionArgs option14 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Valor, TraitEffect.Positive, persuasionArgumentStrength3 - 1, givesCriticalSuccess: false, new TextObject("{=abkmGhLH}Has {CURRENT_LIEGE.NAME} really repaid you for your service as you deserve?"), null, canBlockOtherOption: false, canMoveToTheNextReservation: true);
			persuasionTask3.AddOptionToTask(option14);
		}
		if (HeroHelper.NPCPoliticalDifferencesWithNPC(forLord, newLiege) && !HeroHelper.NPCPoliticalDifferencesWithNPC(forLord, currentLiege))
		{
			PersuasionOptionArgs option15 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Valor, TraitEffect.Positive, persuasionArgumentStrength3 + 1, givesCriticalSuccess: false, new TextObject("{=OdS0e6Sb}{NEW_LIEGE.NAME} stands up for what you believe in, while {CURRENT_LIEGE.NAME} does not."), null, canBlockOtherOption: false, canMoveToTheNextReservation: true);
			persuasionTask3.AddOptionToTask(option15);
		}
		if (forLord.GetTraitLevel(DefaultTraits.Mercy) > 0 && currentLiege.GetTraitLevel(DefaultTraits.Mercy) < 0 && newLiege.GetTraitLevel(DefaultTraits.Mercy) >= 0)
		{
			PersuasionOptionArgs option16 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Mercy, TraitEffect.Positive, persuasionArgumentStrength3, givesCriticalSuccess: true, new TextObject("{=9cZeHcAC}The cruelty of {CURRENT_LIEGE.NAME} is legendary. Who cares what he stands for if the realm is drenched in blood?"), null, canBlockOtherOption: false, canMoveToTheNextReservation: true);
			persuasionTask3.AddOptionToTask(option16);
		}
		PersuasionOptionArgs option17 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Calculating, TraitEffect.Positive, persuasionArgumentStrength3, givesCriticalSuccess: false, new TextObject("{=y3xguaCc}Put your interests and the good of the realm first. There's too much at stake for that."), null, canBlockOtherOption: false, canMoveToTheNextReservation: true);
		persuasionTask3.AddOptionToTask(option17);
		list.Add(persuasionTask3);
		PersuasionTask persuasionTask4 = new PersuasionTask(4)
		{
			FinalFailLine = new TextObject("{=2P9mMbrq}It is not in my interest to change sides. (Self-interest persuasion failed.)")
		};
		float renown = newLiege.Clan.Renown;
		List<Settlement> list2 = Settlement.All.Where((Settlement x) => x.MapFaction == newLiege.MapFaction).ToList();
		List<Settlement> list3 = Settlement.All.Where((Settlement x) => x.MapFaction == currentLiege.MapFaction).ToList();
		PersuasionArgumentStrength argumentStrength;
		if (renown < 1000f && newLiege == Hero.MainHero)
		{
			persuasionTask4.SpokenLine = new TextObject("{=p2rTaKo8}You have no claim to the throne. Even in the unlikely case that others follow you, another usurper will just rise up to defeat you.");
			argumentStrength = PersuasionArgumentStrength.VeryHard;
		}
		else if (list2.Count * 3 < list3.Count)
		{
			persuasionTask4.SpokenLine = new TextObject("{=A6E74QyR}You are badly outnumbered. A rebellion should at least have a chance of success.");
			argumentStrength = PersuasionArgumentStrength.VeryHard;
		}
		else if (list2.Count < list3.Count)
		{
			persuasionTask4.SpokenLine = new TextObject("{=ZQa7tXdK}You are somewhat outnumbered. Even if I agree with you, it would be wise of me to wait.");
			argumentStrength = PersuasionArgumentStrength.Hard;
		}
		else
		{
			persuasionTask4.SpokenLine = new TextObject("{=GEBRuVcZ}Why change sides now? Once one declares oneself a rebel, there is usually no going back.");
			argumentStrength = PersuasionArgumentStrength.Normal;
		}
		if (forLord.GetTraitLevel(DefaultTraits.Valor) > 0)
		{
			PersuasionOptionArgs option18 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Valor, TraitEffect.Positive, PersuasionArgumentStrength.Normal, givesCriticalSuccess: true, new TextObject("{=XFzbzt3W}You are known for your valor. Fortune favors the bold. Together, we will win this war quickly."), null, canBlockOtherOption: false, canMoveToTheNextReservation: true);
			persuasionTask4.AddOptionToTask(option18);
		}
		else if (forLord.GetTraitLevel(DefaultTraits.Valor) > 0)
		{
			PersuasionOptionArgs option19 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Valor, TraitEffect.Positive, PersuasionArgumentStrength.Hard, givesCriticalSuccess: true, new TextObject("{=7QdKwOhY}Fortune favors the bold. With you with us, we will win this war quickly."), null, canBlockOtherOption: false, canMoveToTheNextReservation: true);
			persuasionTask4.AddOptionToTask(option19);
		}
		PersuasionOptionArgs option20 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Calculating, TraitEffect.Positive, PersuasionArgumentStrength.Normal, givesCriticalSuccess: false, new TextObject("{=dGkJi1yb}I have a strategy to win. And my strategies always work, eventually."), null, canBlockOtherOption: false, canMoveToTheNextReservation: true);
		persuasionTask4.AddOptionToTask(option20);
		if (forLord.GetTraitLevel(DefaultTraits.Honor) + forLord.GetTraitLevel(DefaultTraits.Valor) < 0)
		{
			PersuasionOptionArgs option21 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Honor, TraitEffect.Positive, PersuasionArgumentStrength.Normal, givesCriticalSuccess: false, new TextObject("{=IpnQP7A1}Better to die fighting for a just ruler than to live under an unjust one."), null, canBlockOtherOption: false, canMoveToTheNextReservation: true);
			persuasionTask4.AddOptionToTask(option21);
		}
		if (Hero.MainHero == newLiege)
		{
			PersuasionOptionArgs option22 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Generosity, TraitEffect.Positive, argumentStrength, givesCriticalSuccess: false, new TextObject("{=a37zTVVe}Believe me, I'll be generous to those who came to me early. Perhaps not as generous to those who came late."), null, canBlockOtherOption: false, canMoveToTheNextReservation: true);
			persuasionTask4.AddOptionToTask(option22);
		}
		else
		{
			PersuasionOptionArgs option23 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Honor, TraitEffect.Positive, PersuasionArgumentStrength.Normal, givesCriticalSuccess: false, new TextObject("{=aMICdOjq}{NEW_LIEGE.NAME} will be grateful to those who backed {?NEW_LIEGE.GENDER}her{?}him{\\?} before {?NEW_LIEGE.GENDER}her{?}his{\\?} victory was assured. Not so much after it's assured."), null, canBlockOtherOption: false, canMoveToTheNextReservation: true);
			persuasionTask4.AddOptionToTask(option23);
		}
		list.Add(persuasionTask4);
		return list;
	}

	public bool conversation_player_is_asking_to_recruit_enemy_on_condition()
	{
		Kingdom kingdom = Clan.PlayerClan.Kingdom;
		if (kingdom != null && Campaign.Current.Models.DefectionModel.CanHeroDefectToFaction(Hero.OneToOneConversationHero, kingdom) && FactionManager.IsAtWarAgainstFaction(Hero.OneToOneConversationHero.MapFaction, Hero.MainHero.MapFaction))
		{
			Hero.OneToOneConversationHero.MapFaction.Leader.SetTextVariables();
			MBTextManager.SetTextVariable("FACTION_NAME", Hero.MainHero.MapFaction.Name);
			return true;
		}
		return false;
	}

	public bool conversation_player_is_asking_to_recruit_neutral_on_condition()
	{
		Kingdom kingdom = Clan.PlayerClan.Kingdom;
		if (kingdom != null && Campaign.Current.Models.DefectionModel.CanHeroDefectToFaction(Hero.OneToOneConversationHero, kingdom) && !FactionManager.IsAtWarAgainstFaction(Hero.OneToOneConversationHero.MapFaction, Hero.MainHero.MapFaction))
		{
			Hero.OneToOneConversationHero.MapFaction.Leader.SetTextVariables();
			MBTextManager.SetTextVariable("FACTION_NAME", Hero.MainHero.MapFaction.Name);
			return true;
		}
		return false;
	}

	private bool conversation_suggest_treason_on_condition()
	{
		return false;
	}

	public bool conversation_lord_from_ruling_clan_on_condition()
	{
		float num = 0f;
		_allReservations = GetPersuasionTasksForDefection(Hero.OneToOneConversationHero, Hero.MainHero.MapFaction.Leader);
		foreach (PersuasionTask allReservation in _allReservations)
		{
			foreach (PersuasionAttempt previousDefectionPersuasionAttempt in _previousDefectionPersuasionAttempts)
			{
				if (previousDefectionPersuasionAttempt.Matches(Hero.OneToOneConversationHero, allReservation.ReservationType))
				{
					switch (previousDefectionPersuasionAttempt.Result)
					{
					case PersuasionOptionResult.Success:
						num += _successValue;
						break;
					case PersuasionOptionResult.CriticalSuccess:
						num += _criticalSuccessValue;
						break;
					case PersuasionOptionResult.CriticalFailure:
						num -= _criticalFailValue;
						break;
					case PersuasionOptionResult.Failure:
						num -= 0f;
						break;
					}
				}
			}
		}
		if (_maximumScoreCap > num && _previousDefectionPersuasionAttempts.Any((PersuasionAttempt x) => x.PersuadedHero == Hero.OneToOneConversationHero))
		{
			MBTextManager.SetTextVariable("LIEGE_IS_RELATIVE", new TextObject("{=03lc5R2t}You have tried to persuade me before. I will not stand your words again."));
			return true;
		}
		if (Hero.OneToOneConversationHero.Clan.IsMapFaction)
		{
			return false;
		}
		if (Hero.OneToOneConversationHero.Clan == Hero.OneToOneConversationHero.MapFaction.Leader.Clan)
		{
			TextObject textObject = new TextObject("{=jF4Nl8Au}{NPC_LIEGE.NAME}, {LIEGE_RELATIONSHIP}? Long may {?NPC_LIEGE.GENDER}she{?}he{\\?} live.");
			StringHelpers.SetCharacterProperties("NPC_LIEGE", Hero.OneToOneConversationHero.Clan.Leader.CharacterObject, textObject);
			textObject.SetTextVariable("LIEGE_RELATIONSHIP", ConversationHelper.HeroRefersToHero(Hero.OneToOneConversationHero, Hero.OneToOneConversationHero.Clan.Leader, uppercaseFirst: true));
			MBTextManager.SetTextVariable("LIEGE_IS_RELATIVE", textObject);
			return true;
		}
		if (Hero.OneToOneConversationHero.PartyBelongedTo != null && Hero.OneToOneConversationHero.PartyBelongedTo.Army != null && (Hero.OneToOneConversationHero.PartyBelongedTo.Army.LeaderParty == Hero.OneToOneConversationHero.PartyBelongedTo || Hero.OneToOneConversationHero.PartyBelongedTo.AttachedTo != null))
		{
			MBTextManager.SetTextVariable("LIEGE_IS_RELATIVE", new TextObject("{=MalIalPA}I will not listen to such matters while I'm in an army."));
			return true;
		}
		return false;
	}

	public bool conversation_lord_redirects_to_clan_leader_on_condition()
	{
		if (Hero.OneToOneConversationHero.Clan.IsMapFaction)
		{
			return false;
		}
		MBTextManager.SetTextVariable("REDIRECT_HERO_RELATIONSHIP", ConversationHelper.HeroRefersToHero(Hero.OneToOneConversationHero, Hero.OneToOneConversationHero.Clan.Leader, uppercaseFirst: true));
		if (!Hero.OneToOneConversationHero.Clan.IsMapFaction)
		{
			return Hero.OneToOneConversationHero.Clan.Leader != Hero.OneToOneConversationHero;
		}
		return false;
	}

	private void persuasion_redierect_player_finish_on_consequece()
	{
	}

	private bool conversation_lord_can_recruit_on_condition()
	{
		if (Hero.MainHero.MapFaction.Leader == Hero.MainHero)
		{
			MBTextManager.SetTextVariable("RECRUIT_START", new TextObject("{=Fr7wzk97}I am the rightful ruler of this land. I would like your support."));
		}
		else if (Hero.MainHero.MapFaction == Hero.OneToOneConversationHero.MapFaction)
		{
			StringHelpers.SetCharacterProperties("CURRENT_LIEGE", Hero.OneToOneConversationHero.MapFaction.Leader.CharacterObject);
			MBTextManager.SetTextVariable("RECRUIT_START", "{=V7qF7uas}I should lead our people, not {CURRENT_LIEGE.NAME}.");
		}
		else
		{
			StringHelpers.SetCharacterProperties("NEW_LIEGE", Hero.MainHero.MapFaction.Leader.CharacterObject);
			MBTextManager.SetTextVariable("RECRUIT_START", new TextObject("{=UwPs3wmj}My liege {NEW_LIEGE.NAME} would welcome your support. Join us!"));
		}
		return true;
	}

	private void start_lord_defection_persuasion_on_consequence()
	{
		Hero newLiege = Hero.MainHero.MapFaction.Leader;
		if (Hero.MainHero.MapFaction == Hero.OneToOneConversationHero.MapFaction)
		{
			newLiege = Hero.MainHero;
		}
		_allReservations = GetPersuasionTasksForDefection(Hero.OneToOneConversationHero, newLiege);
		_maximumScoreCap = (float)_allReservations.Count * 1f;
		float num = 0f;
		foreach (PersuasionTask allReservation in _allReservations)
		{
			foreach (PersuasionAttempt previousDefectionPersuasionAttempt in _previousDefectionPersuasionAttempts)
			{
				if (previousDefectionPersuasionAttempt.Matches(Hero.OneToOneConversationHero, allReservation.ReservationType))
				{
					switch (previousDefectionPersuasionAttempt.Result)
					{
					case PersuasionOptionResult.Success:
						num += _successValue;
						break;
					case PersuasionOptionResult.CriticalSuccess:
						num += _criticalSuccessValue;
						break;
					case PersuasionOptionResult.CriticalFailure:
						num -= _criticalFailValue;
						break;
					case PersuasionOptionResult.Failure:
						num -= 0f;
						break;
					}
				}
			}
		}
		ConversationManager.StartPersuasion(_maximumScoreCap, _successValue, _failValue, _criticalSuccessValue, _criticalFailValue, num);
	}

	private void OnDailyTick()
	{
		RemoveOldAttempts();
	}

	private void conversation_clear_persuasion_on_consequence()
	{
		ClearPersuasion();
	}

	private void conversation_leave_faction_barter_consequence()
	{
		BarterManager.Instance.StartBarterOffer(Hero.MainHero, Hero.OneToOneConversationHero, PartyBase.MainParty, Hero.OneToOneConversationHero.PartyBelongedTo?.Party, null, BarterManager.Instance.InitializeJoinFactionBarterContext, 0, isAIBarter: false, new Barterable[1]
		{
			new JoinKingdomAsClanBarterable(Hero.OneToOneConversationHero, Clan.PlayerClan.Kingdom, isDefecting: true)
		});
		_allReservations = null;
		ConversationManager.EndPersuasion();
		if (Hero.OneToOneConversationHero.PartyBelongedTo != null && !Hero.OneToOneConversationHero.PartyBelongedTo.MapFaction.IsAtWarWith(MobileParty.MainParty.MapFaction))
		{
			PlayerEncounter.LeaveEncounter = true;
		}
	}

	private bool CanAttemptToPersuade(Hero targetHero, int reservationType)
	{
		foreach (PersuasionAttempt previousDefectionPersuasionAttempt in _previousDefectionPersuasionAttempts)
		{
			if (previousDefectionPersuasionAttempt.Matches(targetHero, reservationType) && !previousDefectionPersuasionAttempt.IsSuccesful() && previousDefectionPersuasionAttempt.GameTime.ElapsedWeeksUntilNow < 1f)
			{
				return false;
			}
		}
		return true;
	}

	private void RemoveOldAttempts()
	{
		for (int num = _previousDefectionPersuasionAttempts.Count - 1; num >= 0; num--)
		{
			if (_previousDefectionPersuasionAttempts[num].GameTime.ElapsedYearsUntilNow > 1f)
			{
				_previousDefectionPersuasionAttempts.RemoveAt(num);
			}
		}
	}
}
