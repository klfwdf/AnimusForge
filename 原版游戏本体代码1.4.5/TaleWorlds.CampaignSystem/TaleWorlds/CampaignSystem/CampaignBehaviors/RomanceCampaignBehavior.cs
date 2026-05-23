using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.BarterSystem;
using TaleWorlds.CampaignSystem.BarterSystem.Barterables;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Conversation.Persuasion;
using TaleWorlds.CampaignSystem.Conversation.Tags;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class RomanceCampaignBehavior : CampaignBehaviorBase
{
	public enum RomanticPreference
	{
		Conventional,
		Moralist,
		AttractedToBravery,
		Macchiavellian,
		Romantic,
		Companionship,
		MadAndBad,
		Security,
		PreferencesEnd
	}

	private enum RomanceReservationType
	{
		TravelChat,
		TravelLesson,
		Aspirations,
		Compatibility,
		Attraction,
		Family,
		MaterialWealth,
		NoObjection
	}

	private enum RomanceReservationDescription
	{
		CompatibilityINeedSomeoneUpright,
		CompatibilityNeedSomethingInCommon,
		CompatibiliyINeedSomeoneDangerous,
		CompatibilityStrongPoliticalBeliefs,
		AttractionYoureNotMyType,
		AttractionYoureGoodEnough,
		AttractionIAmDrawnToYou,
		PropertyYouSeemRichEnough,
		PropertyWeNeedToBeComfortable,
		PropertyIWantRealWealth,
		PropertyHowCanIMarryAnAdventuress,
		FamilyApprovalIAmGladYouAreFriendsWithOurFamily,
		FamilyApprovalYouNeedToBeFriendsWithOurFamily,
		FamilyApprovalHowCanYouBeEnemiesWithOurFamily,
		FamilyApprovalItWouldBeBestToBefriendOurFamily
	}

	private const PersuasionDifficulty _difficulty = PersuasionDifficulty.Medium;

	private List<PersuasionTask> _allReservations;

	[SaveableField(1)]
	private List<PersuasionAttempt> _previousRomancePersuasionAttempts;

	private Hero _playerProposalHero;

	private Hero _proposedSpouseForPlayerRelative;

	private float _maximumScoreCap;

	private const float _successValue = 1f;

	private const float _criticalSuccessValue = 2f;

	private const float _criticalFailValue = 2f;

	private const float _failValue = 0f;

	private CampaignTime RomanceCourtshipAttemptCooldown => CampaignTime.DaysFromNow(-1f);

	public RomanceCampaignBehavior()
	{
		_previousRomancePersuasionAttempts = new List<PersuasionAttempt>();
	}

	public override void RegisterEvents()
	{
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
		CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, DailyTick);
		CampaignEvents.DailyTickClanEvent.AddNonSerializedListener(this, DailyTickClan);
	}

	private void DailyTickClan(Clan clan)
	{
		CheckNpcMarriages(clan);
	}

	private void CheckNpcMarriages(Clan consideringClan)
	{
		if (!IsClanSuitableForNpcMarriage(consideringClan))
		{
			return;
		}
		MarriageModel marriageModel = Campaign.Current.Models.MarriageModel;
		foreach (Hero item in consideringClan.AliveLords.ToList())
		{
			if (!item.CanMarry())
			{
				continue;
			}
			Clan clan = Clan.All[MBRandom.RandomInt(Clan.All.Count)];
			if (!IsClanSuitableForNpcMarriage(clan) || !marriageModel.ShouldNpcMarriageBetweenClansBeAllowed(consideringClan, clan))
			{
				continue;
			}
			foreach (Hero item2 in clan.AliveLords.ToList())
			{
				float num = marriageModel.NpcCoupleMarriageChance(item, item2);
				if (!(num > 0f) || !(MBRandom.RandomFloat < num))
				{
					continue;
				}
				bool flag = false;
				foreach (Romance.RomanticState romanticState in Romance.RomanticStateList)
				{
					if (romanticState.Level >= Romance.RomanceLevelEnum.MatchMadeByFamily && (romanticState.Person1 == item || romanticState.Person2 == item || romanticState.Person1 == item2 || romanticState.Person2 == item2))
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					MarriageAction.Apply(item, item2);
					return;
				}
			}
		}
	}

	private bool IsClanSuitableForNpcMarriage(Clan clan)
	{
		if (clan != Clan.PlayerClan)
		{
			return Campaign.Current.Models.MarriageModel.IsClanSuitableForMarriage(clan);
		}
		return false;
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("previousRomancePersuasionAttempts", ref _previousRomancePersuasionAttempts);
	}

	public void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
	{
		AddDialogs(campaignGameStarter);
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

	private void RemoveUnneededPersuasionAttempts()
	{
		foreach (PersuasionAttempt item in _previousRomancePersuasionAttempts.ToList())
		{
			if (item.PersuadedHero.Spouse != null || !item.PersuadedHero.IsAlive)
			{
				_previousRomancePersuasionAttempts.Remove(item);
			}
		}
	}

	protected void AddDialogs(CampaignGameStarter starter)
	{
		starter.AddPlayerLine("lord_special_request_flirt", "lord_talk_speak_diplomacy_2", "lord_start_courtship_response", "{=!}{FLIRTATION_LINE}", conversation_player_can_open_courtship_on_condition, conversation_player_opens_courtship_on_consequence);
		starter.AddPlayerLine("hero_romance_task_pt1", "hero_main_options", "hero_courtship_task_1_begin_reservations", "{=bHZyublA}So... I'm glad to have the chance to spend some time together.", conversation_romance_at_stage_1_discussions_on_condition, conversation_start_courtship_persuasion_pt1_on_consequence);
		starter.AddPlayerLine("hero_romance_task_pt2", "hero_main_options", "hero_courtship_task_2_begin_reservations", "{=nGsQeTll}Perhaps we should discuss a future together...", conversation_romance_at_stage_2_discussions_on_condition, conversation_continue_courtship_stage_2_on_consequence);
		starter.AddPlayerLine("hero_romance_task_pt3a", "hero_main_options", "hero_courtship_final_barter", "{=2aW6NC3Q}Let us discuss the final terms of our marriage.", conversation_finalize_courtship_for_hero_on_condition, null);
		starter.AddPlayerLine("hero_romance_task_pt3b", "hero_main_options", "hero_courtship_final_barter", "{=jd4qUGEA}I wish to discuss the final terms of my marriage with {COURTSHIP_PARTNER}.", conversation_finalize_courtship_for_other_on_condition, null);
		starter.AddPlayerLine("hero_romance_task_blocked", "hero_main_options", "hero_courtship_task_blocked", "{=OaRB1oVI}So... Earlier, we had discussed the possibility of marriage.", conversation_romance_blocked_on_condition, null);
		starter.AddDialogLine("hero_courtship_persuasion_fail", "hero_courtship_task_blocked", "lord_pretalk", "{=!}{ROMANCE_BLOCKED_REASON}", null, null);
		starter.AddDialogLine("hero_courtship_persuasion_fail_2", "hero_courtship_task_1_begin_reservations", "lord_pretalk", "{=!}{FAILED_PERSUASION_LINE}", conversation_lord_player_has_failed_in_courtship_on_condition, conversation_fail_courtship_on_consequence);
		starter.AddDialogLine("hero_courtship_persuasion_start", "hero_courtship_task_1_begin_reservations", "hero_courtship_task_1_next_reservation", "{=bW3ygxro}Yes, it's good to have a chance to get to know each other.", null, null);
		starter.AddDialogLine("hero_courtship_persuasion_fail_3", "hero_courtship_task_1_next_reservation", "lord_pretalk", "{=!}{FAILED_PERSUASION_LINE}", conversation_lord_player_has_failed_in_courtship_on_condition, conversation_fail_courtship_on_consequence);
		starter.AddDialogLine("hero_courtship_persuasion_attempt", "hero_courtship_task_1_next_reservation", "hero_courtship_argument", "{=!}{PERSUASION_TASK_LINE}", conversation_check_if_unmet_reservation_on_condition, null);
		starter.AddDialogLine("hero_courtship_persuasion_success", "hero_courtship_task_1_next_reservation", "lord_conclude_courtship_stage_1", "{=YcdQ1MWq}Well.. It seems we have a fair amount in common.", null, conversation_courtship_stage_1_success_on_consequence);
		starter.AddDialogLine("persuasion_leave_faction_npc_result_success_2_1", "lord_conclude_courtship_stage_1", "close_window", "{=SP7I61x2}Perhaps we can talk more when we meet again.", null, courtship_conversation_leave_on_consequence);
		starter.AddDialogLine("hero_courtship_persuasion_2_start", "hero_courtship_task_2_begin_reservations", "hero_courtship_task_2_next_reservation", "{=VNFKqpyV}Yes, well, I've been thinking about that.", null, null);
		starter.AddDialogLine("hero_courtship_persuasion_2_fail", "hero_courtship_task_2_next_reservation", "lord_pretalk", "{=!}{FAILED_PERSUASION_LINE}", conversation_lord_player_has_failed_in_courtship_on_condition, conversation_fail_courtship_on_consequence);
		starter.AddDialogLine("hero_courtship_persuasion_2_attempt", "hero_courtship_task_2_next_reservation", "hero_courtship_argument", "{=!}{PERSUASION_TASK_LINE}", conversation_check_if_unmet_reservation_on_condition, null);
		starter.AddDialogLine("hero_courtship_persuasion_2_success", "hero_courtship_task_2_next_reservation", "lord_conclude_courtship_stage_2", "{=xwS10c1b}Yes... I think I would be honored to accept your proposal.", null, conversation_courtship_stage_2_success_on_consequence);
		starter.AddDialogLine("persuasion_leave_faction_npc_result_success_2_2", "lord_conclude_courtship_stage_2", "close_window", "{=pvnY5Jwv}{CLAN_LEADER.LINK}, as head of our family, needs to give {?CLAN_LEADER.GENDER}her{?}his{\\?} blessing. There are usually financial arrangements to be made.", courtship_hero_not_clan_leader_on_condition, courtship_conversation_leave_on_consequence);
		starter.AddDialogLine("persuasion_leave_faction_npc_result_success_2_3", "lord_conclude_courtship_stage_2", "close_window", "{=nnutwjOZ}We'll need to work out the details of how we divide our property.", null, courtship_conversation_leave_on_consequence);
		starter.AddPlayerLine("hero_courtship_argument_1", "hero_courtship_argument", "hero_courtship_reaction", "{=!}{ROMANCE_PERSUADE_ATTEMPT_1}", conversation_courtship_persuasion_option_1_on_condition, conversation_romance_1_persuade_option_on_consequence, 100, persuasionOptionDelegate: SetupCourtshipPersuasionOption1, clickableConditionDelegate: RomancePersuasionOption1ClickableOnCondition1);
		starter.AddPlayerLine("hero_courtship_argument_2", "hero_courtship_argument", "hero_courtship_reaction", "{=!}{ROMANCE_PERSUADE_ATTEMPT_2}", conversation_courtship_persuasion_option_2_on_condition, conversation_romance_2_persuade_option_on_consequence, 100, persuasionOptionDelegate: SetupCourtshipPersuasionOption2, clickableConditionDelegate: RomancePersuasionOption2ClickableOnCondition2);
		starter.AddPlayerLine("hero_courtship_argument_3", "hero_courtship_argument", "hero_courtship_reaction", "{=!}{ROMANCE_PERSUADE_ATTEMPT_3}", conversation_courtship_persuasion_option_3_on_condition, conversation_romance_3_persuade_option_on_consequence, 100, persuasionOptionDelegate: SetupCourtshipPersuasionOption3, clickableConditionDelegate: RomancePersuasionOption3ClickableOnCondition3);
		starter.AddPlayerLine("hero_courtship_argument_4", "hero_courtship_argument", "hero_courtship_reaction", "{=!}{ROMANCE_PERSUADE_ATTEMPT_4}", conversation_courtship_persuasion_option_4_on_condition, conversation_romance_4_persuade_option_on_consequence, 100, persuasionOptionDelegate: SetupCourtshipPersuasionOption4, clickableConditionDelegate: RomancePersuasionOption4ClickableOnCondition4);
		starter.AddPlayerLine("lord_ask_recruit_argument_no_answer_2", "hero_courtship_argument", "lord_pretalk", "{=!}{TRY_HARDER_LINE}", conversation_courtship_try_later_on_condition, conversation_fail_courtship_on_consequence);
		starter.AddDialogLine("lord_ask_recruit_argument_reaction_1", "hero_courtship_reaction", "hero_courtship_task_1_next_reservation", "{=!}{PERSUASION_REACTION}", conversation_courtship_reaction_stage_1_on_condition, conversation_lord_persuade_option_reaction_on_consequence);
		starter.AddDialogLine("lord_ask_recruit_argument_reaction_2", "hero_courtship_reaction", "hero_courtship_task_2_next_reservation", "{=!}{PERSUASION_REACTION}", conversation_courtship_reaction_stage_2_on_condition, conversation_lord_persuade_option_reaction_on_consequence);
		starter.AddDialogLine("hero_courtship_end_conversation", "hero_courtship_end_conversation", "close_window", "{=Mk9k8Sec}As always, it is a delight to speak to you.", null, courtship_conversation_leave_on_consequence);
		starter.AddDialogLine("hero_courtship_final_barter", "hero_courtship_final_barter", "hero_courtship_final_barter_setup", "{=0UPds9x3}Very well, then...", null, null);
		starter.AddDialogLine("hero_courtship_final_barter_setup", "hero_courtship_final_barter_setup", "hero_courtship_final_barter_conclusion", "{=qqzJTfo0}Barter line goes here.", null, conversation_finalize_marriage_barter_consequence);
		starter.AddDialogLine("hero_courtship_final_barter_setup_2", "hero_courtship_final_barter_conclusion", "close_window", "{=FGVzQUao}Congratulations, and may the Heavens bless you.", conversation_marriage_barter_successful_on_condition, conversation_marriage_barter_successful_on_consequence);
		starter.AddDialogLine("hero_courtship_final_barter_setup_3", "hero_courtship_final_barter_conclusion", "close_window", "{=iunPaMFv}I guess we should put this aside, for now. But perhaps we can speak again at a later date.", () => !conversation_marriage_barter_successful_on_condition(), null);
		starter.AddPlayerLine("lord_propose_marriage_conv_general_proposal", "lord_talk_speak_diplomacy_2", "lord_propose_marriage_to_clan_leader", "{=v9tQv4eN}I would like to propose an alliance between our families through marriage.", conversation_discuss_marriage_alliance_on_condition, conversation_find_player_relatives_eligible_for_marriage_on_consequence, 120);
		starter.AddDialogLine("lord_propose_marriage_conv_general_proposal_response", "lord_propose_marriage_to_clan_leader", "lord_propose_marriage_to_clan_leader_options", "{=MhPAHpND}And whose hand are you offering?", null, null);
		starter.AddPlayerLine("lord_propose_marriage_conv_general_proposal_2_1", "lord_propose_marriage_to_clan_leader_options", "lord_propose_marriage_to_clan_leader_response", "{=N1Ue4Blt}My own hand.", conversation_player_eligible_for_marriage_with_hero_rltv_on_condition, conversation_player_nominates_self_for_marriage_on_consequence, 120);
		starter.AddRepeatablePlayerLine("lord_propose_marriage_conv_general_proposal_2", "lord_propose_marriage_to_clan_leader_options", "lord_propose_marriage_to_clan_leader_response", "{=QGj8zQIc}The hand of {MARRIAGE_CANDIDATE.NAME}.", "I am thinking of a different person.", "lord_propose_marriage_to_clan_leader", conversation_player_relative_eligible_for_marriage_on_condition, conversation_player_nominates_marriage_relative_on_consequence);
		starter.AddPlayerLine("lord_propose_marriage_conv_general_proposal_3", "lord_propose_marriage_to_clan_leader_options", "lord_pretalk", "{=D33fIGQe}Never mind.", null, null, 120);
		starter.AddDialogLine("lord_propose_marriage_to_clan_leader_response", "lord_propose_marriage_to_clan_leader_response", "lord_propose_marriage_to_clan_leader_response_self", "{=DdtrRYEM}Well yes. I was looking for a suitable match.", conversation_propose_clan_leader_for_player_nomination_on_condition, null);
		starter.AddPlayerLine("lord_propose_marriage_to_clan_leader_response_yes", "lord_propose_marriage_to_clan_leader_response_self", "lord_start_courtship_response", "{=bx4MiPqN}Yes. I would be honored to be considered.", conversation_player_opens_courtship_on_condition, conversation_player_opens_courtship_on_consequence);
		starter.AddPlayerLine("lord_propose_marriage_to_clan_leader_response_plyr_rltv_yes", "lord_propose_marriage_to_clan_leader_response_self", "lord_propose_marriage_to_clan_leader_confirm", "{=ziA4catk}Very good.", conversation_player_rltv_agrees_on_courtship_on_condition, conversation_player_agrees_on_courtship_on_consequence);
		starter.AddPlayerLine("lord_propose_marriage_to_clan_leader_response_no", "lord_propose_marriage_to_clan_leader_response_self", "lord_pretalk", "{=Zw95lDI3}Hmm.. That might not work out.", null, null);
		starter.AddDialogLine("lord_propose_marriage_to_clan_leader_response_2", "lord_propose_marriage_to_clan_leader_response", "lord_propose_marriage_to_clan_leader_response_other", "{=!}{ARRANGE_MARRIAGE_LINE}", conversation_propose_spouse_for_player_nomination_on_condition, null);
		starter.AddPlayerLine("lord_propose_marriage_to_clan_leader_response_plyr_yes", "lord_propose_marriage_to_clan_leader_response_other", "lord_propose_marriage_to_clan_leader_confirm", "{=ziA4catk}Very good.", conversation_player_rltv_agrees_on_courtship_on_condition, conversation_player_agrees_on_courtship_on_consequence);
		starter.AddPlayerLine("lord_propose_marriage_to_clan_leader_response_plyr_no", "lord_propose_marriage_to_clan_leader_response_other", "lord_pretalk", "{=Zw95lDI3}Hmm.. That might not work out.", null, null);
		starter.AddDialogLine("lord_propose_marriage_to_clan_leader_response_negative_plyr_response", "lord_propose_marriage_to_clan_leader_response", "lord_pretalk", "{=Zw95lDI3}Hmm.. That might not work out.", null, null);
		starter.AddDialogLine("lord_propose_marriage_to_clan_leader_confirm", "lord_propose_marriage_to_clan_leader_confirm", "lord_start", "{=VJEM0IcV}Let's discuss the details then.", null, conversation_lord_propose_marriage_to_clan_leader_confirm_consequences);
		starter.AddDialogLine("lord_start_courtship_response", "lord_start_courtship_response", "lord_start_courtship_response_player_offer", "{=!}{INITIAL_COURTSHIP_REACTION}", conversation_courtship_initial_reaction_on_condition, null);
		starter.AddDialogLine("lord_start_courtship_response_decline", "lord_start_courtship_response", "lord_pretalk", "{=!}{COURTSHIP_DECLINE_REACTION}", conversation_courtship_decline_reaction_to_player_on_condition, null);
		starter.AddPlayerLine("lord_start_courtship_response_player_offer", "lord_start_courtship_response_player_offer", "lord_start_courtship_response_2", "{=cKtJBdPD}I wish to offer my hand in marriage.", conversation_player_eligible_for_marriage_with_conversation_hero_on_condition, null, 120);
		starter.AddPlayerLine("lord_start_courtship_response_player_offer_2", "lord_start_courtship_response_player_offer", "lord_start_courtship_response_2", "{=gnXoIChw}Perhaps you and I...", conversation_player_eligible_for_marriage_with_conversation_hero_on_condition, null, 120);
		starter.AddPlayerLine("lord_start_courtship_response_player_offer_nevermind", "lord_start_courtship_response_player_offer", "lord_pretalk", "{=D33fIGQe}Never mind.", null, null, 120);
		starter.AddDialogLine("lord_start_courtship_response_2", "lord_start_courtship_response_2", "lord_start_courtship_response_3", "{=!}{INITIAL_COURTSHIP_REACTION_TO_PLAYER}", conversation_courtship_reaction_to_player_on_condition, null);
		starter.AddDialogLine("lord_start_courtship_response_3", "lord_start_courtship_response_3", "close_window", "{=YHZsHohq}We meet from time to time, as is the custom, to see if we are right for each other. I hope to see you again soon.", null, courtship_conversation_leave_on_consequence);
		starter.AddDialogLine("lord_propose_marriage_conv_general_proposal_response_2", "lord_propose_general_proposal_response", "lord_propose_marriage_options", "{=k1hyviBO}Tell me, what is on your mind?", null, null);
		starter.AddPlayerLine("lord_propose_marriage_conv_nevermind", "lord_propose_marriage_options", "lord_pretalk", "{=D33fIGQe}Never mind.", null, null);
		starter.AddPlayerLine("lord_propose_marriage_conv_nevermind_2", "lord_propose_marry_our_children_options", "lord_pretalk", "{=D33fIGQe}Never mind.", null, null);
		starter.AddPlayerLine("lord_propose_marriage_conv_nevermind_3", "lord_propose_marry_one_of_your_kind_options", "lord_pretalk", "{=D33fIGQe}Never mind.", null, null);
	}

	private bool courtship_hero_not_clan_leader_on_condition()
	{
		Hero leader = Hero.OneToOneConversationHero.Clan.Leader;
		if (leader == Hero.OneToOneConversationHero)
		{
			return false;
		}
		StringHelpers.SetCharacterProperties("CLAN_LEADER", leader.CharacterObject);
		return true;
	}

	private void courtship_conversation_leave_on_consequence()
	{
		if (PlayerEncounter.Current != null)
		{
			PlayerEncounter.LeaveEncounter = true;
		}
	}

	private void conversation_finalize_marriage_barter_consequence()
	{
		Hero heroBeingProposedTo = Hero.OneToOneConversationHero;
		foreach (Hero aliveLord in Hero.OneToOneConversationHero.Clan.AliveLords)
		{
			if (Romance.GetRomanticLevel(Hero.MainHero, aliveLord) == Romance.RomanceLevelEnum.CoupleAgreedOnMarriage)
			{
				heroBeingProposedTo = aliveLord;
				break;
			}
		}
		MarriageBarterable marriageBarterable = new MarriageBarterable(Hero.MainHero, PartyBase.MainParty, heroBeingProposedTo, Hero.MainHero);
		BarterManager.Instance.StartBarterOffer(Hero.MainHero, Hero.OneToOneConversationHero, PartyBase.MainParty, Hero.OneToOneConversationHero.PartyBelongedTo?.Party, null, (Barterable barterable, BarterData _args, object obj) => BarterManager.Instance.InitializeMarriageBarterContext(barterable, _args, new Tuple<Hero, Hero>(heroBeingProposedTo, Hero.MainHero)), (int)Romance.GetRomanticState(Hero.MainHero, heroBeingProposedTo).ScoreFromPersuasion, isAIBarter: false, new Barterable[1] { marriageBarterable });
		if (PlayerEncounter.Current != null)
		{
			PlayerEncounter.LeaveEncounter = true;
		}
	}

	private void DailyTick()
	{
		foreach (Romance.RomanticState item in Romance.RomanticStateList.ToList())
		{
			if (item.Person1.IsDead || item.Person2.IsDead)
			{
				Romance.RomanticStateList.Remove(item);
			}
		}
	}

	private IEnumerable<RomanceReservationDescription> GetRomanceReservations(Hero wooed, Hero wooer)
	{
		List<RomanceReservationDescription> list = new List<RomanceReservationDescription>();
		bool num = wooed.GetTraitLevel(DefaultTraits.Honor) + wooed.GetTraitLevel(DefaultTraits.Mercy) > 0;
		bool flag = wooed.GetTraitLevel(DefaultTraits.Honor) < 1 && wooed.GetTraitLevel(DefaultTraits.Valor) < 1 && wooed.GetTraitLevel(DefaultTraits.Calculating) < 1;
		bool flag2 = wooed.GetTraitLevel(DefaultTraits.Calculating) - wooed.GetTraitLevel(DefaultTraits.Mercy) >= 0;
		bool flag3 = wooed.GetTraitLevel(DefaultTraits.Valor) - wooed.GetTraitLevel(DefaultTraits.Calculating) > 0 && wooed.GetTraitLevel(DefaultTraits.Mercy) <= 0;
		if (num)
		{
			list.Add(RomanceReservationDescription.CompatibilityINeedSomeoneUpright);
		}
		else if (flag3 && wooed.IsFemale)
		{
			list.Add(RomanceReservationDescription.CompatibiliyINeedSomeoneDangerous);
		}
		else
		{
			list.Add(RomanceReservationDescription.CompatibilityNeedSomethingInCommon);
		}
		int attractionValuePercentage = Campaign.Current.Models.RomanceModel.GetAttractionValuePercentage(Hero.OneToOneConversationHero, Hero.MainHero);
		if (attractionValuePercentage > 70)
		{
			list.Add(RomanceReservationDescription.AttractionIAmDrawnToYou);
		}
		else if (attractionValuePercentage > 40)
		{
			list.Add(RomanceReservationDescription.AttractionYoureGoodEnough);
		}
		else
		{
			list.Add(RomanceReservationDescription.AttractionYoureNotMyType);
		}
		List<Settlement> list2 = Settlement.All.Where((Settlement x) => x.OwnerClan == wooer.Clan).ToList();
		if (flag2 && wooer.IsFemale && list2.Count < 1)
		{
			list.Add(RomanceReservationDescription.PropertyHowCanIMarryAnAdventuress);
		}
		else if (flag2 && list2.Count < 3)
		{
			list.Add(RomanceReservationDescription.PropertyIWantRealWealth);
		}
		else if (list2.Count < 1)
		{
			list.Add(RomanceReservationDescription.PropertyWeNeedToBeComfortable);
		}
		else
		{
			list.Add(RomanceReservationDescription.PropertyYouSeemRichEnough);
		}
		float unmodifiedClanLeaderRelationshipWithPlayer = Hero.OneToOneConversationHero.GetUnmodifiedClanLeaderRelationshipWithPlayer();
		if (unmodifiedClanLeaderRelationshipWithPlayer < -10f)
		{
			list.Add(RomanceReservationDescription.FamilyApprovalHowCanYouBeEnemiesWithOurFamily);
		}
		else if (!flag && unmodifiedClanLeaderRelationshipWithPlayer < 10f)
		{
			list.Add(RomanceReservationDescription.FamilyApprovalItWouldBeBestToBefriendOurFamily);
		}
		else if (flag && unmodifiedClanLeaderRelationshipWithPlayer < 10f)
		{
			list.Add(RomanceReservationDescription.FamilyApprovalYouNeedToBeFriendsWithOurFamily);
		}
		else
		{
			list.Add(RomanceReservationDescription.FamilyApprovalIAmGladYouAreFriendsWithOurFamily);
		}
		return list;
	}

	private List<PersuasionTask> GetPersuasionTasksForCourtshipStage1(Hero wooed, Hero wooer)
	{
		StringHelpers.SetCharacterProperties("PLAYER", Hero.MainHero.CharacterObject);
		List<PersuasionTask> list = new List<PersuasionTask>();
		PersuasionTask persuasionTask = new PersuasionTask(0);
		list.Add(persuasionTask);
		persuasionTask.FinalFailLine = new TextObject("{=dY2PzpIV}I'm not sure how much we have in common..");
		persuasionTask.TryLaterLine = new TextObject("{=PoDVgQaz}Well, it would take a bit long to discuss this.");
		persuasionTask.SpokenLine = Campaign.Current.ConversationManager.FindMatchingTextOrNull("str_courtship_travel_task", CharacterObject.OneToOneConversationCharacter);
		Tuple<TraitObject, int>[] traitCorrelations = GetTraitCorrelations(1, -1, 0, 0, 1);
		PersuasionOptionArgs option = new PersuasionOptionArgs(argumentStrength: Campaign.Current.Models.PersuasionModel.GetArgumentStrengthBasedOnTargetTraits(CharacterObject.OneToOneConversationCharacter, traitCorrelations), skill: DefaultSkills.Leadership, trait: DefaultTraits.Valor, traitEffect: TraitEffect.Positive, givesCriticalSuccess: false, line: new TextObject("{=YNBm3LkC}I feel lucky to live in a time where a valiant warrior can make a name for {?PLAYER.GENDER}herself{?}himself{\\?}."), traitCorrelation: traitCorrelations, canBlockOtherOption: false, canMoveToTheNextReservation: true);
		persuasionTask.AddOptionToTask(option);
		Tuple<TraitObject, int>[] traitCorrelations2 = GetTraitCorrelations(1, -1, 0, 0, 1);
		PersuasionOptionArgs option2 = new PersuasionOptionArgs(argumentStrength: Campaign.Current.Models.PersuasionModel.GetArgumentStrengthBasedOnTargetTraits(CharacterObject.OneToOneConversationCharacter, traitCorrelations2), skill: DefaultSkills.Roguery, trait: DefaultTraits.Valor, traitEffect: TraitEffect.Positive, givesCriticalSuccess: false, line: new TextObject("{=rtqD9cnu}Yeah, it's a rough world, but there are lots of opportunities to be seized right now if you're not afraid to get your hands a bit dirty."), traitCorrelation: traitCorrelations2, canBlockOtherOption: false, canMoveToTheNextReservation: true);
		persuasionTask.AddOptionToTask(option2);
		Tuple<TraitObject, int>[] traitCorrelations3 = GetTraitCorrelations(0, 1, 1, 0, -1);
		PersuasionOptionArgs option3 = new PersuasionOptionArgs(argumentStrength: Campaign.Current.Models.PersuasionModel.GetArgumentStrengthBasedOnTargetTraits(CharacterObject.OneToOneConversationCharacter, traitCorrelations3), skill: DefaultSkills.Charm, trait: DefaultTraits.Mercy, traitEffect: TraitEffect.Positive, givesCriticalSuccess: false, line: new TextObject("{=rfyalLyY}What can I say? It's a beautiful world, but filled with so much suffering."), traitCorrelation: traitCorrelations3, canBlockOtherOption: false, canMoveToTheNextReservation: true);
		persuasionTask.AddOptionToTask(option3);
		Tuple<TraitObject, int>[] traitCorrelations4 = GetTraitCorrelations(-1, 0, -1, -1);
		PersuasionOptionArgs option4 = new PersuasionOptionArgs(argumentStrength: Campaign.Current.Models.PersuasionModel.GetArgumentStrengthBasedOnTargetTraits(CharacterObject.OneToOneConversationCharacter, traitCorrelations4), skill: DefaultSkills.Charm, trait: DefaultTraits.Generosity, traitEffect: TraitEffect.Negative, givesCriticalSuccess: false, line: new TextObject("{=ja5bAOMr}The world's a dungheap, basically. The sooner I earn enough to retire, the better."), traitCorrelation: traitCorrelations4, canBlockOtherOption: false, canMoveToTheNextReservation: true);
		persuasionTask.AddOptionToTask(option4);
		PersuasionTask persuasionTask2 = new PersuasionTask(1);
		list.Add(persuasionTask2);
		persuasionTask2.SpokenLine = new TextObject("{=5Vk6I1sf}Between your followers, your rivals and your enemies, you must have met a lot of interesting people...");
		persuasionTask2.FinalFailLine = new TextObject("{=lDJUL4lZ}I think we maybe see the world a bit differently.");
		persuasionTask2.TryLaterLine = new TextObject("{=ZmxbIXsp}I am sorry you feel that way. We can speak later.");
		Tuple<TraitObject, int>[] traitCorrelations5 = GetTraitCorrelations(1, 0, 1, 2);
		PersuasionOptionArgs option5 = new PersuasionOptionArgs(argumentStrength: Campaign.Current.Models.PersuasionModel.GetArgumentStrengthBasedOnTargetTraits(CharacterObject.OneToOneConversationCharacter, traitCorrelations5), skill: DefaultSkills.Charm, trait: DefaultTraits.Generosity, traitEffect: TraitEffect.Positive, givesCriticalSuccess: false, line: new TextObject("{=8BnWa83o}I'm just honored to have fought alongside comrades who thought nothing of shedding their blood to keep me alive."), traitCorrelation: traitCorrelations5, canBlockOtherOption: false, canMoveToTheNextReservation: true);
		persuasionTask2.AddOptionToTask(option5);
		Tuple<TraitObject, int>[] traitCorrelations6 = GetTraitCorrelations(0, 0, -1, 0, 1);
		PersuasionOptionArgs option6 = new PersuasionOptionArgs(argumentStrength: Campaign.Current.Models.PersuasionModel.GetArgumentStrengthBasedOnTargetTraits(CharacterObject.OneToOneConversationCharacter, traitCorrelations6), skill: DefaultSkills.Roguery, trait: DefaultTraits.Calculating, traitEffect: TraitEffect.Positive, givesCriticalSuccess: false, line: new TextObject("{=QHG6LU1g}Ah yes, I've seen cruelty, degradation and degeneracy like you wouldn't believe. Fascinating stuff, all of it."), traitCorrelation: traitCorrelations6, canBlockOtherOption: false, canMoveToTheNextReservation: true);
		persuasionTask2.AddOptionToTask(option6);
		Tuple<TraitObject, int>[] traitCorrelations7 = GetTraitCorrelations(0, 2);
		PersuasionOptionArgs option7 = new PersuasionOptionArgs(argumentStrength: Campaign.Current.Models.PersuasionModel.GetArgumentStrengthBasedOnTargetTraits(CharacterObject.OneToOneConversationCharacter, traitCorrelations7), skill: DefaultSkills.Leadership, trait: DefaultTraits.Mercy, traitEffect: TraitEffect.Positive, givesCriticalSuccess: false, line: new TextObject("{=bwWdGLDv}I have seen great good and great evil, but I can only hope the good outweights the evil in most people's hearts."), traitCorrelation: traitCorrelations7, canBlockOtherOption: false, canMoveToTheNextReservation: true);
		persuasionTask2.AddOptionToTask(option7);
		Tuple<TraitObject, int>[] traitCorrelations8 = GetTraitCorrelations(-1, 0, -1, -1);
		PersuasionOptionArgs option8 = new PersuasionOptionArgs(argumentStrength: Campaign.Current.Models.PersuasionModel.GetArgumentStrengthBasedOnTargetTraits(CharacterObject.OneToOneConversationCharacter, traitCorrelations8), skill: DefaultSkills.Charm, trait: DefaultTraits.Generosity, traitEffect: TraitEffect.Negative, givesCriticalSuccess: false, line: new TextObject("{=3skTM1DC}Most people would put a knife in your back for a few coppers. Have a few friends and keep them close, I guess."), traitCorrelation: traitCorrelations8, canBlockOtherOption: false, canMoveToTheNextReservation: true);
		persuasionTask2.AddOptionToTask(option8);
		PersuasionTask persuasionTask3 = new PersuasionTask(2);
		list.Add(persuasionTask3);
		persuasionTask3.SpokenLine = Campaign.Current.ConversationManager.FindMatchingTextOrNull("str_courtship_aspirations_task", CharacterObject.OneToOneConversationCharacter);
		persuasionTask3.ImmediateFailLine = new TextObject("{=8hEVO9hw}Hmm. Perhaps you and I have different priorities in life.");
		persuasionTask3.FinalFailLine = new TextObject("{=HAtHptbV}In the end, I don't think we have that much in common.");
		persuasionTask3.TryLaterLine = new TextObject("{=PoDVgQaz}Well, it would take a bit long to discuss this.");
		Tuple<TraitObject, int>[] traitCorrelations9 = GetTraitCorrelations(0, 2, 1);
		PersuasionOptionArgs option9 = new PersuasionOptionArgs(argumentStrength: Campaign.Current.Models.PersuasionModel.GetArgumentStrengthBasedOnTargetTraits(CharacterObject.OneToOneConversationCharacter, traitCorrelations9), skill: DefaultSkills.Leadership, trait: DefaultTraits.Mercy, traitEffect: TraitEffect.Positive, givesCriticalSuccess: false, line: new TextObject("{=6kjacaiB}I hope I can bring peace to the land, and justice, and alleviate people's suffering."), traitCorrelation: traitCorrelations9, canBlockOtherOption: false, canMoveToTheNextReservation: true);
		persuasionTask3.AddOptionToTask(option9);
		Tuple<TraitObject, int>[] traitCorrelations10 = GetTraitCorrelations(1, 1, 0, 2);
		PersuasionOptionArgs option10 = new PersuasionOptionArgs(argumentStrength: Campaign.Current.Models.PersuasionModel.GetArgumentStrengthBasedOnTargetTraits(CharacterObject.OneToOneConversationCharacter, traitCorrelations10), skill: DefaultSkills.Charm, trait: DefaultTraits.Generosity, traitEffect: TraitEffect.Positive, givesCriticalSuccess: false, line: new TextObject("{=rrqCZa0H}I'll make sure those who stuck their necks out for me, who sweated and bled for me, get their due."), traitCorrelation: traitCorrelations10, canBlockOtherOption: false, canMoveToTheNextReservation: true);
		persuasionTask3.AddOptionToTask(option10);
		Tuple<TraitObject, int>[] traitCorrelations11 = GetTraitCorrelations(0, 0, 0, 0, 2);
		PersuasionOptionArgs option11 = new PersuasionOptionArgs(argumentStrength: Campaign.Current.Models.PersuasionModel.GetArgumentStrengthBasedOnTargetTraits(CharacterObject.OneToOneConversationCharacter, traitCorrelations11), skill: DefaultSkills.Roguery, trait: DefaultTraits.Calculating, traitEffect: TraitEffect.Positive, givesCriticalSuccess: false, line: new TextObject("{=ggKa4Bd8}Hmm... First thing to do after taking power is to work on your plan to remain in power."), traitCorrelation: traitCorrelations11, canBlockOtherOption: false, canMoveToTheNextReservation: true);
		persuasionTask3.AddOptionToTask(option11);
		Tuple<TraitObject, int>[] traitCorrelations12 = GetTraitCorrelations(0, -2, 0, -1, 1);
		PersuasionOptionArgs option12 = new PersuasionOptionArgs(argumentStrength: Campaign.Current.Models.PersuasionModel.GetArgumentStrengthBasedOnTargetTraits(CharacterObject.OneToOneConversationCharacter, traitCorrelations12), skill: DefaultSkills.Charm, trait: DefaultTraits.Calculating, traitEffect: TraitEffect.Positive, givesCriticalSuccess: false, line: new TextObject("{=6L1b1nJa}Oh I have a long list of scores to settle. You can be sure of that."), traitCorrelation: traitCorrelations12, canBlockOtherOption: false, canMoveToTheNextReservation: true);
		persuasionTask3.AddOptionToTask(option12);
		persuasionTask2.FinalFailLine = new TextObject("{=Ns315pxY}Perhaps we are not meant for each other.");
		persuasionTask2.TryLaterLine = new TextObject("{=PoDVgQaz}Well, it would take a bit long to discuss this.");
		return list;
	}

	private Tuple<TraitObject, int>[] GetTraitCorrelations(int valor = 0, int mercy = 0, int honor = 0, int generosity = 0, int calculating = 0)
	{
		return new Tuple<TraitObject, int>[5]
		{
			new Tuple<TraitObject, int>(DefaultTraits.Valor, valor),
			new Tuple<TraitObject, int>(DefaultTraits.Mercy, mercy),
			new Tuple<TraitObject, int>(DefaultTraits.Honor, honor),
			new Tuple<TraitObject, int>(DefaultTraits.Generosity, generosity),
			new Tuple<TraitObject, int>(DefaultTraits.Calculating, calculating)
		};
	}

	private List<PersuasionTask> GetPersuasionTasksForCourtshipStage2(Hero wooed, Hero wooer)
	{
		StringHelpers.SetCharacterProperties("PLAYER", Hero.MainHero.CharacterObject);
		List<PersuasionTask> list = new List<PersuasionTask>();
		IEnumerable<RomanceReservationDescription> romanceReservations = GetRomanceReservations(wooed, wooer);
		bool flag = romanceReservations.Any((RomanceReservationDescription x) => x == RomanceReservationDescription.AttractionIAmDrawnToYou);
		List<RomanceReservationDescription> list2 = romanceReservations.Where((RomanceReservationDescription x) => x == RomanceReservationDescription.CompatibiliyINeedSomeoneDangerous || x == RomanceReservationDescription.CompatibilityNeedSomethingInCommon || x == RomanceReservationDescription.CompatibilityINeedSomeoneUpright || x == RomanceReservationDescription.CompatibilityStrongPoliticalBeliefs).ToList();
		if (list2.Count > 0)
		{
			RomanceReservationDescription num = list2[0];
			PersuasionTask persuasionTask = new PersuasionTask(3);
			list.Add(persuasionTask);
			persuasionTask.SpokenLine = new TextObject("{=rtP6vnmj}I'm not sure we're compatible.");
			persuasionTask.FinalFailLine = new TextObject("{=bBTHy6f9}I just don't think that we would be happy together.");
			persuasionTask.TryLaterLine = new TextObject("{=o9ouu97M}I will endeavor to be worthy of your affections.");
			PersuasionArgumentStrength persuasionArgumentStrength = PersuasionArgumentStrength.Normal;
			if (num == RomanceReservationDescription.CompatibiliyINeedSomeoneDangerous)
			{
				if (Hero.OneToOneConversationHero.IsFemale)
				{
					persuasionTask.SpokenLine = new TextObject("{=EkkNQb5N}I like a warrior who strikes fear in the hearts of his enemies. Are you that kind of man?");
				}
				else
				{
					persuasionTask.SpokenLine = new TextObject("{=3cw5pRFM}I had not thought that I might marry a shieldmaiden. But it is intriguing. Tell me, have you killed men in battle?");
				}
				PersuasionOptionArgs option = new PersuasionOptionArgs(DefaultSkills.Roguery, DefaultTraits.Valor, TraitEffect.Positive, persuasionArgumentStrength, givesCriticalSuccess: false, new TextObject("{=FEmiPPbO}Perhaps you've heard the stories about me, then. They're all true."), null, canBlockOtherOption: false, canMoveToTheNextReservation: true);
				persuasionTask.AddOptionToTask(option);
				PersuasionOptionArgs option2 = new PersuasionOptionArgs(DefaultSkills.Roguery, DefaultTraits.Calculating, TraitEffect.Positive, persuasionArgumentStrength + 1, givesCriticalSuccess: false, new TextObject("{=Oe5Tf7OZ}My foes may not fear my sword, but they should fear my cunning."), null, canBlockOtherOption: false, canMoveToTheNextReservation: true);
				persuasionTask.AddOptionToTask(option2);
				if (flag)
				{
					PersuasionOptionArgs option3 = new PersuasionOptionArgs(DefaultSkills.Leadership, DefaultTraits.Calculating, TraitEffect.Negative, persuasionArgumentStrength - 1, givesCriticalSuccess: true, new TextObject("{=zWTNOfHm}I want you and if you want me, that should be enough!"), null, canBlockOtherOption: false, canMoveToTheNextReservation: true);
					persuasionTask.AddOptionToTask(option3);
				}
				PersuasionOptionArgs option4 = new PersuasionOptionArgs(DefaultSkills.Roguery, DefaultTraits.Generosity, TraitEffect.Positive, persuasionArgumentStrength + 1, givesCriticalSuccess: false, new TextObject("{=8a13MGzr}All I can say is that I try to repay good with good, and evil with evil."), null, canBlockOtherOption: false, canMoveToTheNextReservation: true);
				persuasionTask.AddOptionToTask(option4);
			}
			if (num == RomanceReservationDescription.CompatibilityINeedSomeoneUpright)
			{
				persuasionTask.SpokenLine = new TextObject("{=lay7hKUK}I insist that my {?PLAYER.GENDER}wife{?}husband{\\?} conduct {?PLAYER.GENDER}herself{?}himself{\\?} according to the highest standards.");
				PersuasionOptionArgs option5 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Honor, TraitEffect.Positive, PersuasionArgumentStrength.Normal, givesCriticalSuccess: false, new TextObject("{=bOQEc7jA}I am a {?PLAYER.GENDER}woman{?}man{\\?} of my word. I hope that it is sufficient."), null, canBlockOtherOption: false, canMoveToTheNextReservation: true);
				persuasionTask.AddOptionToTask(option5);
				PersuasionOptionArgs option6 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Mercy, TraitEffect.Positive, PersuasionArgumentStrength.Normal, givesCriticalSuccess: false, new TextObject("{=faa9sFfE}I do what I can to alleviate suffering in this world. I hope that is enough."), null, canBlockOtherOption: false, canMoveToTheNextReservation: true);
				persuasionTask.AddOptionToTask(option6);
				if (flag)
				{
					PersuasionOptionArgs option7 = new PersuasionOptionArgs(DefaultSkills.Leadership, DefaultTraits.Calculating, TraitEffect.Negative, persuasionArgumentStrength - 1, givesCriticalSuccess: true, new TextObject("{=zWTNOfHm}I want you and if you want me, that should be enough!"), null, canBlockOtherOption: false, canMoveToTheNextReservation: true);
					persuasionTask.AddOptionToTask(option7);
				}
				PersuasionOptionArgs option8 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Generosity, TraitEffect.Positive, PersuasionArgumentStrength.Hard, givesCriticalSuccess: false, new TextObject("{=b2ePtImV}Those who are loyal to me, I am loyal to them."), null, canBlockOtherOption: false, canMoveToTheNextReservation: true);
				persuasionTask.AddOptionToTask(option8);
			}
			if (num == RomanceReservationDescription.CompatibilityNeedSomethingInCommon)
			{
				persuasionTask.SpokenLine = new TextObject("{=ZsGqHBlR}I need a partner whom I can trust...");
				PersuasionOptionArgs option9 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Generosity, TraitEffect.Positive, persuasionArgumentStrength - 1, givesCriticalSuccess: false, new TextObject("{=LTUEFTaF}I hope that I am known as someone who understands the value of loyalty."), null, canBlockOtherOption: false, canMoveToTheNextReservation: true);
				persuasionTask.AddOptionToTask(option9);
				PersuasionOptionArgs option10 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Honor, TraitEffect.Positive, persuasionArgumentStrength, givesCriticalSuccess: false, new TextObject("{=9qoLQva5}Whatever oath I give to you, you may be sure that I will keep it."), null, canBlockOtherOption: false, canMoveToTheNextReservation: true);
				persuasionTask.AddOptionToTask(option10);
				if (flag)
				{
					PersuasionOptionArgs option11 = new PersuasionOptionArgs(DefaultSkills.Leadership, DefaultTraits.Calculating, TraitEffect.Negative, persuasionArgumentStrength - 1, givesCriticalSuccess: true, new TextObject("{=zWTNOfHm}I want you and if you want me, that should be enough!"), null, canBlockOtherOption: false, canMoveToTheNextReservation: true);
					persuasionTask.AddOptionToTask(option11);
				}
				PersuasionOptionArgs option12 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Generosity, TraitEffect.Positive, persuasionArgumentStrength, givesCriticalSuccess: false, new TextObject("{=b2ePtImV}Those who are loyal to me, I am loyal to them."), null, canBlockOtherOption: false, canMoveToTheNextReservation: true);
				persuasionTask.AddOptionToTask(option12);
			}
			if (num == RomanceReservationDescription.CompatibilityStrongPoliticalBeliefs)
			{
				if (wooed.GetTraitLevel(DefaultTraits.Egalitarian) > 0)
				{
					persuasionTask.SpokenLine = new TextObject("{=s3Fna6wY}I've always seen myself as someone who sides with the weak of this realm. I don't want to find myself at odds with you.");
				}
				if (wooed.GetTraitLevel(DefaultTraits.Oligarchic) > 0)
				{
					persuasionTask.SpokenLine = new TextObject("{=DR2aK4aQ}I respect our ancient laws and traditions. I don't want to find myself at odds with you.");
				}
				if (wooed.GetTraitLevel(DefaultTraits.Authoritarian) > 0)
				{
					persuasionTask.SpokenLine = new TextObject("{=c2Yrci3B}I believe that we need a strong ruler in this realm. I don't want to find myself at odds with you.");
				}
				PersuasionOptionArgs option13 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Mercy, TraitEffect.Positive, persuasionArgumentStrength, givesCriticalSuccess: false, new TextObject("{=pVPkpP20}We may differ on politics, but I hope you'll think me a man with a good heart."), null, canBlockOtherOption: false, canMoveToTheNextReservation: true);
				persuasionTask.AddOptionToTask(option13);
				if (flag)
				{
					PersuasionOptionArgs option14 = new PersuasionOptionArgs(DefaultSkills.Leadership, DefaultTraits.Calculating, TraitEffect.Negative, persuasionArgumentStrength - 1, givesCriticalSuccess: true, new TextObject("{=yghMrFdT}Put petty politics aside and trust your heart!"), null, canBlockOtherOption: false, canMoveToTheNextReservation: true);
					persuasionTask.AddOptionToTask(option14);
				}
				PersuasionOptionArgs option15 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Generosity, TraitEffect.Positive, persuasionArgumentStrength, givesCriticalSuccess: false, new TextObject("{=Tj8bGW4b}If a man and a woman respect each other, politics should not divide them."), null, canBlockOtherOption: false, canMoveToTheNextReservation: true);
				persuasionTask.AddOptionToTask(option15);
			}
		}
		if (romanceReservations.Where((RomanceReservationDescription x) => x == RomanceReservationDescription.AttractionYoureNotMyType).ToList().Count > 0)
		{
			PersuasionTask persuasionTask2 = new PersuasionTask(4);
			list.Add(persuasionTask2);
			persuasionTask2.SpokenLine = new TextObject("{=cOyolp4F}I am just not... How can I say this? I am not attracted to you.");
			persuasionTask2.FinalFailLine = new TextObject("{=LjiYq9cH}I am sorry. I am not sure that I could ever love you.");
			persuasionTask2.TryLaterLine = new TextObject("{=E9s2bjqw}I can only hope that some day you could change your mind.");
			PersuasionOptionArgs option16 = new PersuasionOptionArgs(argumentStrength: (PersuasionArgumentStrength)(-Hero.OneToOneConversationHero.GetTraitLevel(DefaultTraits.Calculating)), skill: DefaultSkills.Charm, trait: DefaultTraits.Calculating, traitEffect: TraitEffect.Positive, givesCriticalSuccess: false, line: new TextObject("{=hwjzKcUw}So what? This is supposed to be an alliance of our houses, not of our hearts."), traitCorrelation: null, canBlockOtherOption: false, canMoveToTheNextReservation: true);
			persuasionTask2.AddOptionToTask(option16);
			PersuasionOptionArgs option17 = new PersuasionOptionArgs(argumentStrength: (PersuasionArgumentStrength)(-Hero.OneToOneConversationHero.GetTraitLevel(DefaultTraits.Generosity) - 1), skill: DefaultSkills.Charm, trait: DefaultTraits.Generosity, traitEffect: TraitEffect.Positive, givesCriticalSuccess: true, line: new TextObject("{=m3EkYCA6}Perhaps if you see how much I love you, you could come to love me over time."), traitCorrelation: null, canBlockOtherOption: false, canMoveToTheNextReservation: true);
			persuasionTask2.AddOptionToTask(option17);
			PersuasionOptionArgs option18 = new PersuasionOptionArgs(argumentStrength: (PersuasionArgumentStrength)(-Hero.OneToOneConversationHero.GetTraitLevel(DefaultTraits.Honor)), skill: DefaultSkills.Charm, trait: DefaultTraits.Honor, traitEffect: TraitEffect.Positive, givesCriticalSuccess: false, line: new TextObject("{=LN7SGvnS}Love is but an infatuation. Judge me by my character."), traitCorrelation: null, canBlockOtherOption: false, canMoveToTheNextReservation: true);
			persuasionTask2.AddOptionToTask(option18);
		}
		List<RomanceReservationDescription> list3 = romanceReservations.Where((RomanceReservationDescription x) => x == RomanceReservationDescription.PropertyHowCanIMarryAnAdventuress || x == RomanceReservationDescription.PropertyIWantRealWealth || x == RomanceReservationDescription.PropertyWeNeedToBeComfortable).ToList();
		if (list3.Count > 0)
		{
			RomanceReservationDescription romanceReservationDescription = list3[0];
			PersuasionTask persuasionTask3 = new PersuasionTask(6);
			list.Add(persuasionTask3);
			persuasionTask3.SpokenLine = new TextObject("{=beK0AZ2y}I am concerned that you do not have the means to support a family.");
			persuasionTask3.FinalFailLine = new TextObject("{=z6vJlozm}I am sorry. I don't believe you have the means to support a family.)");
			persuasionTask3.TryLaterLine = new TextObject("{=vaISh0sx}I will go off to make something of myself, then, and shall return to you.");
			PersuasionArgumentStrength persuasionArgumentStrength2 = PersuasionArgumentStrength.Normal;
			switch (romanceReservationDescription)
			{
			case RomanceReservationDescription.PropertyIWantRealWealth:
				persuasionTask3.SpokenLine = new TextObject("{=pbqjBGk0}I will be honest. I have plans, and I expect the person I marry to have the income to support them.");
				persuasionArgumentStrength2 = PersuasionArgumentStrength.Hard;
				break;
			case RomanceReservationDescription.PropertyHowCanIMarryAnAdventuress:
				persuasionTask3.SpokenLine = new TextObject("{=ZNfWXliN}I will be honest, my lady. You are but a common adventurer, and by marrying you I give up a chance to forge an alliance with a family of real influence and power.");
				persuasionArgumentStrength2 = PersuasionArgumentStrength.Normal;
				break;
			}
			PersuasionOptionArgs option19 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Calculating, TraitEffect.Positive, persuasionArgumentStrength2, givesCriticalSuccess: false, new TextObject("{=erKuPRWA}I have a plan to rise in this world. I'm still only a little way up the ladder."), null, canBlockOtherOption: false, canMoveToTheNextReservation: true);
			persuasionTask3.AddOptionToTask(option19);
			PersuasionOptionArgs option20 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Valor, TraitEffect.Positive, persuasionArgumentStrength2, givesCriticalSuccess: false, (romanceReservationDescription == RomanceReservationDescription.PropertyHowCanIMarryAnAdventuress) ? new TextObject("{=a2dJDUoL}My sword is my dowry. The gold and land will follow.") : new TextObject("{=DLc6NfiV}I shall win you the riches you deserve, or die in the attempt."), null, canBlockOtherOption: false, canMoveToTheNextReservation: true);
			persuasionTask3.AddOptionToTask(option20);
			if (flag)
			{
				PersuasionOptionArgs option21 = new PersuasionOptionArgs(argumentStrength: persuasionArgumentStrength2 - Hero.OneToOneConversationHero.GetTraitLevel(DefaultTraits.Calculating), skill: DefaultSkills.Charm, trait: DefaultTraits.Generosity, traitEffect: TraitEffect.Positive, givesCriticalSuccess: true, line: new TextObject("{=6LfkfJiJ}Can't your passion for me overcome such base feelings?"), traitCorrelation: null, canBlockOtherOption: false, canMoveToTheNextReservation: true);
				persuasionTask3.AddOptionToTask(option21);
			}
		}
		List<RomanceReservationDescription> list4 = romanceReservations.Where((RomanceReservationDescription x) => x == RomanceReservationDescription.FamilyApprovalHowCanYouBeEnemiesWithOurFamily || x == RomanceReservationDescription.FamilyApprovalItWouldBeBestToBefriendOurFamily || x == RomanceReservationDescription.FamilyApprovalYouNeedToBeFriendsWithOurFamily).ToList();
		if (list4.Count > 0 && list.Count < 3)
		{
			_ = list4[0];
			PersuasionTask persuasionTask4 = new PersuasionTask(5);
			list.Add(persuasionTask4);
			persuasionTask4.SpokenLine = new TextObject("{=fAdwIqbg}I think you should try to win my family's approval.");
			persuasionTask4.FinalFailLine = new TextObject("{=Xa7PsIao}I am sorry. I will not marry without my family's blessing.");
			persuasionTask4.TryLaterLine = new TextObject("{=44tA6fNa}I will try to earn your family's trust, then.");
			PersuasionArgumentStrength argumentStrength = PersuasionArgumentStrength.Normal;
			PersuasionOptionArgs option22 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Generosity, TraitEffect.Positive, argumentStrength, givesCriticalSuccess: false, new TextObject("{=563qB3ar}I can only hope that if they come to know my loyalty, they will accept me."), null, canBlockOtherOption: false, canMoveToTheNextReservation: true);
			persuasionTask4.AddOptionToTask(option22);
			if (flag)
			{
				PersuasionOptionArgs option23 = new PersuasionOptionArgs(DefaultSkills.Leadership, DefaultTraits.Valor, TraitEffect.Positive, argumentStrength, givesCriticalSuccess: true, new TextObject("{=LEsuGM8a}Let no one - not even your family - come between us!"), null, canBlockOtherOption: false, canMoveToTheNextReservation: true);
				persuasionTask4.AddOptionToTask(option23);
			}
			PersuasionOptionArgs option24 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Honor, TraitEffect.Positive, argumentStrength, givesCriticalSuccess: false, new TextObject("{=ZbvbsA4i}I can only hope that if they come to know my virtues, they will accept me."), null, canBlockOtherOption: false, canMoveToTheNextReservation: true);
			persuasionTask4.AddOptionToTask(option24);
		}
		else if (list4.Count == 0 && list.Count < 3)
		{
			PersuasionTask persuasionTask5 = new PersuasionTask(7);
			list.Add(persuasionTask5);
			persuasionTask5.SpokenLine = new TextObject("{=HFkXIyCV}My family likes you...");
			persuasionTask5.FinalFailLine = new TextObject("{=3IBVEOwh}I still think we may not be ready yet.");
			persuasionTask5.TryLaterLine = new TextObject("{=44tA6fNa}I will try to earn your family's trust, then.");
			PersuasionArgumentStrength argumentStrength2 = PersuasionArgumentStrength.ExtremelyEasy;
			PersuasionOptionArgs option25 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Generosity, TraitEffect.Positive, argumentStrength2, givesCriticalSuccess: false, new TextObject("{=2LrFafpB}And I will respect and cherish your family."), null, canBlockOtherOption: false, canMoveToTheNextReservation: true);
			persuasionTask5.AddOptionToTask(option25);
			PersuasionOptionArgs option26 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Calculating, TraitEffect.Positive, argumentStrength2, givesCriticalSuccess: false, new TextObject("{=BaifRgT5}That's useful to know for when it comes time to discuss the exchange of dowries."), null, canBlockOtherOption: false, canMoveToTheNextReservation: true);
			persuasionTask5.AddOptionToTask(option26);
		}
		return list;
	}

	private bool conversation_courtship_initial_reaction_on_condition()
	{
		IEnumerable<RomanceReservationDescription> romanceReservations = GetRomanceReservations(Hero.OneToOneConversationHero, Hero.MainHero);
		if (Romance.GetRomanticLevel(Hero.MainHero, Hero.OneToOneConversationHero) == Romance.RomanceLevelEnum.FailedInPracticalities || Romance.GetRomanticLevel(Hero.MainHero, Hero.OneToOneConversationHero) == Romance.RomanceLevelEnum.FailedInCompatibility)
		{
			return false;
		}
		MBTextManager.SetTextVariable("INITIAL_COURTSHIP_REACTION", romanceReservations.Any((RomanceReservationDescription x) => x == RomanceReservationDescription.AttractionIAmDrawnToYou) ? "{=WEkjz9tg}Ah! Yes... We are considering offers... Did you have someone in mind?" : "{=KdhnBhZ1}Yes, we are considering offers. These things are not rushed into.");
		return true;
	}

	private bool conversation_courtship_decline_reaction_to_player_on_condition()
	{
		if (Romance.GetRomanticLevel(Hero.MainHero, Hero.OneToOneConversationHero) == Romance.RomanceLevelEnum.FailedInPracticalities)
		{
			MBTextManager.SetTextVariable("COURTSHIP_DECLINE_REACTION", "{=emLBsWj6}I am terribly sorry. It is practically not possible for us to be married.");
			return true;
		}
		if (Romance.GetRomanticLevel(Hero.MainHero, Hero.OneToOneConversationHero) == Romance.RomanceLevelEnum.FailedInCompatibility)
		{
			MBTextManager.SetTextVariable("COURTSHIP_DECLINE_REACTION", "{=s7idfhBO}I am terribly sorry. We are not really compatible with each other.");
			return true;
		}
		return false;
	}

	private bool conversation_courtship_reaction_to_player_on_condition()
	{
		IEnumerable<RomanceReservationDescription> romanceReservations = GetRomanceReservations(Hero.OneToOneConversationHero, Hero.MainHero);
		bool flag = Hero.OneToOneConversationHero.GetTraitLevel(DefaultTraits.Generosity) + Hero.OneToOneConversationHero.GetTraitLevel(DefaultTraits.Mercy) > 0;
		TraitObject persona = Hero.OneToOneConversationHero.CharacterObject.GetPersona();
		bool flag2 = ConversationTagHelper.UsesHighRegister(Hero.OneToOneConversationHero.CharacterObject);
		if (romanceReservations.Any((RomanceReservationDescription x) => x == RomanceReservationDescription.AttractionIAmDrawnToYou))
		{
			if (persona == DefaultTraits.PersonaIronic && flag2)
			{
				MBTextManager.SetTextVariable("INITIAL_COURTSHIP_REACTION_TO_PLAYER", "{=5ao0RdRT}Well, I do not deny that there is something about you to which I am drawn.");
			}
			if (persona == DefaultTraits.PersonaIronic && !flag2)
			{
				MBTextManager.SetTextVariable("INITIAL_COURTSHIP_REACTION_TO_PLAYER", "{=r77ZrSUJ}You're straightforward. I like that.");
			}
			else if (persona == DefaultTraits.PersonaCurt)
			{
				MBTextManager.SetTextVariable("INITIAL_COURTSHIP_REACTION_TO_PLAYER", Hero.MainHero.IsFemale ? "{=YXCGUSYd}Mm. Well, you'd make a very unusual match. But, well, I won't rule it out." : "{=iKYSgoZx}You're a handsome devil, I'll give you that.");
			}
			else if (persona == DefaultTraits.PersonaEarnest)
			{
				MBTextManager.SetTextVariable("INITIAL_COURTSHIP_REACTION_TO_PLAYER", "{=UCjFAPnk}I am flattered, {?PLAYER.GENDER}my lady{?}sir{\\?}.");
			}
			else
			{
				MBTextManager.SetTextVariable("INITIAL_COURTSHIP_REACTION_TO_PLAYER", "{=8PwNj5tR}Yes... Yes. We should, em, discuss this.");
			}
		}
		else if (romanceReservations.Any((RomanceReservationDescription x) => x == RomanceReservationDescription.PropertyHowCanIMarryAnAdventuress))
		{
			MBTextManager.SetTextVariable("INITIAL_COURTSHIP_REACTION_TO_PLAYER", "{=YRN4RBeI}Very well, madame, but I would have you know.... I intend to marry someone of my own rank.");
		}
		else if (!flag && romanceReservations.Any((RomanceReservationDescription x) => x == RomanceReservationDescription.PropertyIWantRealWealth || x == RomanceReservationDescription.PropertyWeNeedToBeComfortable))
		{
			MBTextManager.SetTextVariable("INITIAL_COURTSHIP_REACTION_TO_PLAYER", "{=P407baEa}I think you would need to rise considerably in the world before I could consider such a thing...");
		}
		else if (flag && romanceReservations.Any((RomanceReservationDescription x) => x == RomanceReservationDescription.PropertyIWantRealWealth))
		{
			MBTextManager.SetTextVariable("INITIAL_COURTSHIP_REACTION_TO_PLAYER", "{=gS1noLvf}I do not know whether to find that charming or impertinent...");
		}
		else if (romanceReservations.Any((RomanceReservationDescription x) => x == RomanceReservationDescription.AttractionYoureNotMyType))
		{
			MBTextManager.SetTextVariable("INITIAL_COURTSHIP_REACTION_TO_PLAYER", "{=ltXu3DbR}Em... Yes, well, I suppose I can consider your offer.");
		}
		else if (romanceReservations.Any((RomanceReservationDescription x) => x == RomanceReservationDescription.FamilyApprovalIAmGladYouAreFriendsWithOurFamily))
		{
			MBTextManager.SetTextVariable("INITIAL_COURTSHIP_REACTION_TO_PLAYER", "{=UQtXV3kf}Certainly, you have always been close to our family.");
		}
		else
		{
			MBTextManager.SetTextVariable("INITIAL_COURTSHIP_REACTION_TO_PLAYER", "{=VYmQmqIv}We are considering many offers. You may certainly add your name to the list.");
		}
		return true;
	}

	private void conversation_fail_courtship_on_consequence()
	{
		if (Romance.GetRomanticLevel(Hero.MainHero, Hero.OneToOneConversationHero) == Romance.RomanceLevelEnum.CourtshipStarted)
		{
			ChangeRomanticStateAction.Apply(Hero.MainHero, Hero.OneToOneConversationHero, Romance.RomanceLevelEnum.FailedInCompatibility);
		}
		else if (Romance.GetRomanticLevel(Hero.MainHero, Hero.OneToOneConversationHero) == Romance.RomanceLevelEnum.CoupleDecidedThatTheyAreCompatible)
		{
			ChangeRomanticStateAction.Apply(Hero.MainHero, Hero.OneToOneConversationHero, Romance.RomanceLevelEnum.FailedInPracticalities);
		}
		if (PlayerEncounter.Current != null)
		{
			PlayerEncounter.LeaveEncounter = true;
		}
		_allReservations = null;
		ConversationManager.EndPersuasion();
	}

	private void conversation_start_courtship_persuasion_pt1_on_consequence()
	{
		if (Romance.GetRomanticLevel(Hero.MainHero, Hero.OneToOneConversationHero) == Romance.RomanceLevelEnum.MatchMadeByFamily)
		{
			ChangeRomanticStateAction.Apply(Hero.MainHero, Hero.OneToOneConversationHero, Romance.RomanceLevelEnum.CourtshipStarted);
		}
		Hero wooer = Hero.MainHero.MapFaction.Leader;
		if (Hero.MainHero.MapFaction == Hero.OneToOneConversationHero.MapFaction)
		{
			wooer = Hero.MainHero;
		}
		_allReservations = GetPersuasionTasksForCourtshipStage1(Hero.OneToOneConversationHero, wooer);
		_maximumScoreCap = (float)_allReservations.Count * 1f;
		float num = 0f;
		foreach (PersuasionTask allReservation in _allReservations)
		{
			foreach (PersuasionAttempt previousRomancePersuasionAttempt in _previousRomancePersuasionAttempts)
			{
				if (previousRomancePersuasionAttempt.Matches(Hero.OneToOneConversationHero, allReservation.ReservationType))
				{
					switch (previousRomancePersuasionAttempt.Result)
					{
					case PersuasionOptionResult.Success:
						num += 1f;
						break;
					case PersuasionOptionResult.CriticalSuccess:
						num += 2f;
						break;
					case PersuasionOptionResult.CriticalFailure:
						num -= 2f;
						break;
					case PersuasionOptionResult.Failure:
						num -= 0f;
						break;
					}
				}
			}
		}
		RemoveUnneededPersuasionAttempts();
		ConversationManager.StartPersuasion(_maximumScoreCap, 1f, 0f, 2f, 2f, num);
	}

	private void conversation_courtship_stage_1_success_on_consequence()
	{
		Romance.RomanticState romanticState = Romance.GetRomanticState(Hero.MainHero, Hero.OneToOneConversationHero);
		float scoreFromPersuasion = ConversationManager.GetPersuasionProgress() - ConversationManager.GetPersuasionGoalValue();
		romanticState.ScoreFromPersuasion = scoreFromPersuasion;
		_allReservations = null;
		ConversationManager.EndPersuasion();
		ChangeRomanticStateAction.Apply(Hero.MainHero, Hero.OneToOneConversationHero, Romance.RomanceLevelEnum.CoupleDecidedThatTheyAreCompatible);
	}

	private void conversation_courtship_stage_2_success_on_consequence()
	{
		Romance.RomanticState romanticState = Romance.GetRomanticState(Hero.MainHero, Hero.OneToOneConversationHero);
		float num = ConversationManager.GetPersuasionProgress() - ConversationManager.GetPersuasionGoalValue();
		romanticState.ScoreFromPersuasion += num;
		_allReservations = null;
		ConversationManager.EndPersuasion();
		ChangeRomanticStateAction.Apply(Hero.MainHero, Hero.OneToOneConversationHero, Romance.RomanceLevelEnum.CoupleAgreedOnMarriage);
	}

	private void conversation_continue_courtship_stage_2_on_consequence()
	{
		Hero wooer = Hero.MainHero.MapFaction.Leader;
		if (Hero.MainHero.MapFaction == Hero.OneToOneConversationHero.MapFaction)
		{
			wooer = Hero.MainHero;
		}
		_allReservations = GetPersuasionTasksForCourtshipStage2(Hero.OneToOneConversationHero, wooer);
		_maximumScoreCap = (float)_allReservations.Count * 1f;
		float num = 0f;
		foreach (PersuasionTask allReservation in _allReservations)
		{
			foreach (PersuasionAttempt previousRomancePersuasionAttempt in _previousRomancePersuasionAttempts)
			{
				if (previousRomancePersuasionAttempt.Matches(Hero.OneToOneConversationHero, allReservation.ReservationType))
				{
					switch (previousRomancePersuasionAttempt.Result)
					{
					case PersuasionOptionResult.Success:
						num += 1f;
						break;
					case PersuasionOptionResult.CriticalSuccess:
						num += 2f;
						break;
					case PersuasionOptionResult.CriticalFailure:
						num -= 2f;
						break;
					case PersuasionOptionResult.Failure:
						num -= 0f;
						break;
					}
				}
			}
		}
		RemoveUnneededPersuasionAttempts();
		ConversationManager.StartPersuasion(_maximumScoreCap, 1f, 0f, 2f, 2f, num);
	}

	private bool conversation_check_if_unmet_reservation_on_condition()
	{
		PersuasionTask currentPersuasionTask = GetCurrentPersuasionTask();
		if ((currentPersuasionTask == _allReservations[_allReservations.Count - 1] && currentPersuasionTask.Options.All((PersuasionOptionArgs x) => x.IsBlocked)) || ConversationManager.GetPersuasionProgressSatisfied())
		{
			return false;
		}
		MBTextManager.SetTextVariable("PERSUASION_TASK_LINE", currentPersuasionTask.SpokenLine);
		return true;
	}

	private bool conversation_lord_player_has_failed_in_courtship_on_condition()
	{
		PersuasionTask currentPersuasionTask = GetCurrentPersuasionTask();
		if (currentPersuasionTask.Options.All((PersuasionOptionArgs x) => x.IsBlocked) && !ConversationManager.GetPersuasionProgressSatisfied())
		{
			MBTextManager.SetTextVariable("FAILED_PERSUASION_LINE", currentPersuasionTask.FinalFailLine);
			return true;
		}
		return false;
	}

	private bool conversation_courtship_persuasion_option_1_on_condition()
	{
		PersuasionTask currentPersuasionTask = GetCurrentPersuasionTask();
		if (currentPersuasionTask.Options.Count > 0)
		{
			TextObject textObject = new TextObject("{=bSo9hKwr}{PERSUASION_OPTION_LINE} {SUCCESS_CHANCE}");
			textObject.SetTextVariable("SUCCESS_CHANCE", PersuasionHelper.ShowSuccess(currentPersuasionTask.Options.ElementAt(0)));
			textObject.SetTextVariable("PERSUASION_OPTION_LINE", currentPersuasionTask.Options.ElementAt(0).Line);
			MBTextManager.SetTextVariable("ROMANCE_PERSUADE_ATTEMPT_1", textObject);
			return true;
		}
		return false;
	}

	private bool conversation_courtship_persuasion_option_2_on_condition()
	{
		PersuasionTask currentPersuasionTask = GetCurrentPersuasionTask();
		if (currentPersuasionTask.Options.Count > 1)
		{
			TextObject textObject = new TextObject("{=bSo9hKwr}{PERSUASION_OPTION_LINE} {SUCCESS_CHANCE}");
			textObject.SetTextVariable("SUCCESS_CHANCE", PersuasionHelper.ShowSuccess(currentPersuasionTask.Options.ElementAt(1)));
			textObject.SetTextVariable("PERSUASION_OPTION_LINE", currentPersuasionTask.Options.ElementAt(1).Line);
			MBTextManager.SetTextVariable("ROMANCE_PERSUADE_ATTEMPT_2", textObject);
			return true;
		}
		return false;
	}

	private bool conversation_courtship_persuasion_option_3_on_condition()
	{
		PersuasionTask currentPersuasionTask = GetCurrentPersuasionTask();
		if (currentPersuasionTask.Options.Count > 2)
		{
			TextObject textObject = new TextObject("{=bSo9hKwr}{PERSUASION_OPTION_LINE} {SUCCESS_CHANCE}");
			textObject.SetTextVariable("SUCCESS_CHANCE", PersuasionHelper.ShowSuccess(currentPersuasionTask.Options.ElementAt(2)));
			textObject.SetTextVariable("PERSUASION_OPTION_LINE", currentPersuasionTask.Options.ElementAt(2).Line);
			MBTextManager.SetTextVariable("ROMANCE_PERSUADE_ATTEMPT_3", textObject);
			return true;
		}
		return false;
	}

	private bool conversation_courtship_persuasion_option_4_on_condition()
	{
		PersuasionTask currentPersuasionTask = GetCurrentPersuasionTask();
		if (currentPersuasionTask.Options.Count > 3)
		{
			TextObject textObject = new TextObject("{=bSo9hKwr}{PERSUASION_OPTION_LINE} {SUCCESS_CHANCE}");
			textObject.SetTextVariable("SUCCESS_CHANCE", PersuasionHelper.ShowSuccess(currentPersuasionTask.Options.ElementAt(3)));
			textObject.SetTextVariable("PERSUASION_OPTION_LINE", currentPersuasionTask.Options.ElementAt(3).Line);
			MBTextManager.SetTextVariable("ROMANCE_PERSUADE_ATTEMPT_4", textObject);
			return true;
		}
		return false;
	}

	private void conversation_romance_1_persuade_option_on_consequence()
	{
		PersuasionTask currentPersuasionTask = GetCurrentPersuasionTask();
		if (currentPersuasionTask.Options.Count > 0)
		{
			currentPersuasionTask.Options[0].BlockTheOption(isBlocked: true);
		}
	}

	private void conversation_romance_2_persuade_option_on_consequence()
	{
		PersuasionTask currentPersuasionTask = GetCurrentPersuasionTask();
		if (currentPersuasionTask.Options.Count > 1)
		{
			currentPersuasionTask.Options[1].BlockTheOption(isBlocked: true);
		}
	}

	private void conversation_romance_3_persuade_option_on_consequence()
	{
		PersuasionTask currentPersuasionTask = GetCurrentPersuasionTask();
		if (currentPersuasionTask.Options.Count > 2)
		{
			currentPersuasionTask.Options[2].BlockTheOption(isBlocked: true);
		}
	}

	private void conversation_romance_4_persuade_option_on_consequence()
	{
		PersuasionTask currentPersuasionTask = GetCurrentPersuasionTask();
		if (currentPersuasionTask.Options.Count > 3)
		{
			currentPersuasionTask.Options[3].BlockTheOption(isBlocked: true);
		}
	}

	private bool RomancePersuasionOption1ClickableOnCondition1(out TextObject hintText)
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

	private bool RomancePersuasionOption2ClickableOnCondition2(out TextObject hintText)
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

	private bool RomancePersuasionOption3ClickableOnCondition3(out TextObject hintText)
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

	private bool RomancePersuasionOption4ClickableOnCondition4(out TextObject hintText)
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

	private PersuasionOptionArgs SetupCourtshipPersuasionOption1()
	{
		return GetCurrentPersuasionTask().Options.ElementAt(0);
	}

	private PersuasionOptionArgs SetupCourtshipPersuasionOption2()
	{
		return GetCurrentPersuasionTask().Options.ElementAt(1);
	}

	private PersuasionOptionArgs SetupCourtshipPersuasionOption3()
	{
		return GetCurrentPersuasionTask().Options.ElementAt(2);
	}

	private PersuasionOptionArgs SetupCourtshipPersuasionOption4()
	{
		return GetCurrentPersuasionTask().Options.ElementAt(3);
	}

	private bool conversation_player_eligible_for_marriage_with_conversation_hero_on_condition()
	{
		if (Hero.MainHero.Spouse == null && Hero.OneToOneConversationHero != null)
		{
			return MarriageCourtshipPossibility(Hero.MainHero, Hero.OneToOneConversationHero);
		}
		return false;
	}

	private bool conversation_player_eligible_for_marriage_with_hero_rltv_on_condition()
	{
		if (Hero.MainHero.Spouse == null)
		{
			return Hero.OneToOneConversationHero != null;
		}
		return false;
	}

	private void conversation_find_player_relatives_eligible_for_marriage_on_consequence()
	{
		ConversationSentence.SetObjectsToRepeatOver(FindPlayerRelativesEligibleForMarriage(Hero.OneToOneConversationHero.Clan).ToList());
	}

	private void conversation_player_nominates_self_for_marriage_on_consequence()
	{
		_playerProposalHero = Hero.MainHero;
	}

	private void conversation_player_nominates_marriage_relative_on_consequence()
	{
		CharacterObject characterObject = ConversationSentence.SelectedRepeatObject as CharacterObject;
		_playerProposalHero = characterObject.HeroObject;
	}

	private bool conversation_player_relative_eligible_for_marriage_on_condition()
	{
		if (ConversationSentence.CurrentProcessedRepeatObject is CharacterObject character)
		{
			StringHelpers.SetRepeatableCharacterProperties("MARRIAGE_CANDIDATE", character);
			return true;
		}
		return false;
	}

	private bool conversation_propose_clan_leader_for_player_nomination_on_condition()
	{
		foreach (Hero item in Hero.OneToOneConversationHero.Clan.AliveLords.OrderByDescending((Hero x) => x.Age))
		{
			if (MarriageCourtshipPossibility(_playerProposalHero, item) && item.CharacterObject == Hero.OneToOneConversationHero.CharacterObject)
			{
				_proposedSpouseForPlayerRelative = item;
				return true;
			}
		}
		return false;
	}

	private bool conversation_propose_spouse_for_player_nomination_on_condition()
	{
		foreach (Hero item in Hero.OneToOneConversationHero.Clan.AliveLords.OrderByDescending((Hero x) => x.Age))
		{
			if (MarriageCourtshipPossibility(_playerProposalHero, item) && item != Hero.OneToOneConversationHero)
			{
				_proposedSpouseForPlayerRelative = item;
				TextObject textObject = new TextObject("{=TjAQbTab}Well, yes, we are looking for a suitable marriage for { OTHER_CLAN_NOMINEE.LINK}.");
				item.SetPropertiesToTextObject(textObject, "OTHER_CLAN_NOMINEE");
				MBTextManager.SetTextVariable("ARRANGE_MARRIAGE_LINE", textObject);
				item.IsKnownToPlayer = true;
				return true;
			}
		}
		return false;
	}

	private bool conversation_player_rltv_agrees_on_courtship_on_condition()
	{
		Hero courtedHeroInOtherClan = Romance.GetCourtedHeroInOtherClan(_playerProposalHero, _proposedSpouseForPlayerRelative);
		if (courtedHeroInOtherClan != null)
		{
			return courtedHeroInOtherClan == _proposedSpouseForPlayerRelative;
		}
		return true;
	}

	private void conversation_player_agrees_on_courtship_on_consequence()
	{
		ChangeRomanticStateAction.Apply(_playerProposalHero, _proposedSpouseForPlayerRelative, Romance.RomanceLevelEnum.MatchMadeByFamily);
	}

	private void conversation_lord_propose_marriage_to_clan_leader_confirm_consequences()
	{
		MarriageBarterable marriageBarterable = new MarriageBarterable(Hero.MainHero, PartyBase.MainParty, _playerProposalHero, _proposedSpouseForPlayerRelative);
		BarterManager.Instance.StartBarterOffer(Hero.MainHero, Hero.OneToOneConversationHero, PartyBase.MainParty, Hero.OneToOneConversationHero.PartyBelongedTo?.Party, null, (Barterable barterableObj, BarterData _args, object obj) => BarterManager.Instance.InitializeMarriageBarterContext(barterableObj, _args, new Tuple<Hero, Hero>(_playerProposalHero, _proposedSpouseForPlayerRelative)), 0, isAIBarter: false, new Barterable[1] { marriageBarterable });
	}

	private bool conversation_romance_blocked_on_condition()
	{
		if (Hero.OneToOneConversationHero == null)
		{
			return false;
		}
		Romance.RomanceLevelEnum romanticLevel = Romance.GetRomanticLevel(Hero.MainHero, Hero.OneToOneConversationHero);
		if (!MarriageCourtshipPossibility(Hero.MainHero, Hero.OneToOneConversationHero) && romanticLevel >= Romance.RomanceLevelEnum.MatchMadeByFamily && romanticLevel < Romance.RomanceLevelEnum.Marriage)
		{
			if (FactionManager.IsAtWarAgainstFaction(Hero.MainHero.MapFaction, Hero.OneToOneConversationHero.MapFaction))
			{
				MBTextManager.SetTextVariable("ROMANCE_BLOCKED_REASON", "{=wNxhmNOc}I am afraid I cannot entertain such a proposal so long as we are at war.");
				ChangeRomanticStateAction.Apply(Hero.MainHero, Hero.OneToOneConversationHero, Romance.RomanceLevelEnum.FailedInCompatibility);
			}
			else if (Hero.OneToOneConversationHero.Clan.Leader == Hero.OneToOneConversationHero)
			{
				MBTextManager.SetTextVariable("ROMANCE_BLOCKED_REASON", "{=1FcxAGWU}Ah, yes. I am afraid I can no longer entertain such a proposal. I am now the head of my family, and the factors that we must consider have changed. You would need to place your property under my control, and I do not think that you would accept that.");
				ChangeRomanticStateAction.Apply(Hero.MainHero, Hero.OneToOneConversationHero, Romance.RomanceLevelEnum.FailedInCompatibility);
			}
			else if (Hero.OneToOneConversationHero.PartyBelongedTo != null && Hero.OneToOneConversationHero.PartyBelongedTo.Army != null)
			{
				MBTextManager.SetTextVariable("ROMANCE_BLOCKED_REASON", "{=9LwYa3Tv}Ah, yes. My efforts are currently focused on this campaign, so it's best we discuss your proposal at a later time.");
			}
			else if (Hero.OneToOneConversationHero.PartyBelongedToAsPrisoner != null)
			{
				MBTextManager.SetTextVariable("ROMANCE_BLOCKED_REASON", "{=TuqmbbqB}Ah, yes. Unfortunately, this is no discussion to be had while I am captive. We shall discuss our future after I am freed from these chains.");
			}
			else if (MobileParty.MainParty.Army != null)
			{
				MBTextManager.SetTextVariable("ROMANCE_BLOCKED_REASON", "{=bLjYzudi}Let's discuss this matter at a later date, after your campaign has ended.");
			}
			else
			{
				MBTextManager.SetTextVariable("ROMANCE_BLOCKED_REASON", "{=BQn8yTs5}Ah, yes. I am afraid I can no longer entertain your proposal, at least not for now.");
				ChangeRomanticStateAction.Apply(Hero.MainHero, Hero.OneToOneConversationHero, Romance.RomanceLevelEnum.FailedInCompatibility);
			}
			return true;
		}
		return false;
	}

	private bool conversation_romance_at_stage_1_discussions_on_condition()
	{
		if (Hero.OneToOneConversationHero == null)
		{
			return false;
		}
		Romance.RomanceLevelEnum romanticLevel = Romance.GetRomanticLevel(Hero.MainHero, Hero.OneToOneConversationHero);
		if (MarriageCourtshipPossibility(Hero.MainHero, Hero.OneToOneConversationHero) && (romanticLevel == Romance.RomanceLevelEnum.CourtshipStarted || romanticLevel == Romance.RomanceLevelEnum.MatchMadeByFamily))
		{
			List<PersuasionAttempt> list = (from x in _previousRomancePersuasionAttempts
				where x.PersuadedHero == Hero.OneToOneConversationHero
				orderby x.GameTime descending
				select x).ToList();
			if (list.Count == 0 || list[0].GameTime < RomanceCourtshipAttemptCooldown)
			{
				return true;
			}
		}
		return false;
	}

	private bool conversation_romance_at_stage_2_discussions_on_condition()
	{
		if (Hero.OneToOneConversationHero == null)
		{
			return false;
		}
		Romance.RomanceLevelEnum romanticLevel = Romance.GetRomanticLevel(Hero.MainHero, Hero.OneToOneConversationHero);
		if (MarriageCourtshipPossibility(Hero.MainHero, Hero.OneToOneConversationHero) && romanticLevel == Romance.RomanceLevelEnum.CoupleDecidedThatTheyAreCompatible)
		{
			List<PersuasionAttempt> list = (from x in _previousRomancePersuasionAttempts
				where x.PersuadedHero == Hero.OneToOneConversationHero
				orderby x.GameTime descending
				select x).ToList();
			if (list.Count == 0 || list[0].GameTime < RomanceCourtshipAttemptCooldown)
			{
				return true;
			}
		}
		return false;
	}

	private bool conversation_finalize_courtship_for_hero_on_condition()
	{
		if (MarriageCourtshipPossibility(Hero.MainHero, Hero.OneToOneConversationHero) && Hero.OneToOneConversationHero.Clan.Leader == Hero.OneToOneConversationHero && Romance.GetRomanticLevel(Hero.MainHero, Hero.OneToOneConversationHero) == Romance.RomanceLevelEnum.CoupleAgreedOnMarriage)
		{
			return true;
		}
		return false;
	}

	private bool conversation_finalize_courtship_for_other_on_condition()
	{
		if (Hero.OneToOneConversationHero == null || Hero.OneToOneConversationHero.Clan?.Leader != Hero.OneToOneConversationHero || Hero.OneToOneConversationHero.IsPrisoner)
		{
			return false;
		}
		foreach (Hero aliveLord in Hero.OneToOneConversationHero.Clan.AliveLords)
		{
			if (aliveLord != Hero.OneToOneConversationHero && MarriageCourtshipPossibility(Hero.MainHero, aliveLord) && Romance.GetRomanticLevel(Hero.MainHero, aliveLord) == Romance.RomanceLevelEnum.CoupleAgreedOnMarriage)
			{
				MBTextManager.SetTextVariable("COURTSHIP_PARTNER", aliveLord.Name);
				return true;
			}
		}
		return false;
	}

	private bool conversation_discuss_marriage_alliance_on_condition()
	{
		if (Hero.OneToOneConversationHero != null)
		{
			IFaction mapFaction = Hero.OneToOneConversationHero.MapFaction;
			if ((mapFaction == null || !mapFaction.IsMinorFaction) && !Hero.OneToOneConversationHero.IsPrisoner)
			{
				if (FactionManager.IsAtWarAgainstFaction(Hero.MainHero.MapFaction, Hero.OneToOneConversationHero.MapFaction))
				{
					return false;
				}
				if (Hero.OneToOneConversationHero.Clan == null || Hero.OneToOneConversationHero.Clan.Leader != Hero.OneToOneConversationHero)
				{
					return false;
				}
				bool result = false;
				{
					foreach (Hero hero in Hero.MainHero.Clan.Heroes)
					{
						foreach (Hero aliveLord in Hero.OneToOneConversationHero.Clan.AliveLords)
						{
							if (MarriageCourtshipPossibility(hero, aliveLord))
							{
								result = true;
							}
						}
					}
					return result;
				}
			}
		}
		return false;
	}

	private bool conversation_player_can_open_courtship_on_condition()
	{
		if (Hero.OneToOneConversationHero != null)
		{
			IFaction mapFaction = Hero.OneToOneConversationHero.MapFaction;
			if ((mapFaction == null || !mapFaction.IsMinorFaction) && !Hero.OneToOneConversationHero.IsPrisoner)
			{
				if (MarriageCourtshipPossibility(Hero.MainHero, Hero.OneToOneConversationHero) && Romance.GetRomanticLevel(Hero.MainHero, Hero.OneToOneConversationHero) == Romance.RomanceLevelEnum.Untested)
				{
					if (Hero.MainHero.IsFemale)
					{
						MBTextManager.SetTextVariable("FLIRTATION_LINE", "{=bjJs0eeB}My lord, I note that you have not yet taken a wife.");
					}
					else
					{
						MBTextManager.SetTextVariable("FLIRTATION_LINE", "{=v1hC6Aem}My lady, I wish to profess myself your most ardent admirer.");
					}
					return true;
				}
				if (Romance.GetRomanticLevel(Hero.MainHero, Hero.OneToOneConversationHero) == Romance.RomanceLevelEnum.FailedInCompatibility || Romance.GetRomanticLevel(Hero.MainHero, Hero.OneToOneConversationHero) == Romance.RomanceLevelEnum.FailedInPracticalities)
				{
					if (Hero.MainHero.IsFemale)
					{
						MBTextManager.SetTextVariable("FLIRTATION_LINE", "{=2WnhUBMM}My lord, may you give me another chance to prove myself?");
					}
					else
					{
						MBTextManager.SetTextVariable("FLIRTATION_LINE", "{=4iTaEZKg}My lady, may you give me another chance to prove myself?");
					}
					return true;
				}
				return false;
			}
		}
		return false;
	}

	private bool conversation_player_opens_courtship_on_condition()
	{
		return _playerProposalHero == Hero.MainHero;
	}

	private void conversation_player_opens_courtship_on_consequence()
	{
		if (Romance.GetRomanticLevel(Hero.MainHero, Hero.OneToOneConversationHero) != Romance.RomanceLevelEnum.FailedInCompatibility && Romance.GetRomanticLevel(Hero.MainHero, Hero.OneToOneConversationHero) != Romance.RomanceLevelEnum.FailedInPracticalities)
		{
			ChangeRomanticStateAction.Apply(Hero.MainHero, Hero.OneToOneConversationHero, Romance.RomanceLevelEnum.CourtshipStarted);
		}
	}

	private bool conversation_courtship_try_later_on_condition()
	{
		PersuasionTask currentPersuasionTask = GetCurrentPersuasionTask();
		MBTextManager.SetTextVariable("TRY_HARDER_LINE", currentPersuasionTask.TryLaterLine);
		return true;
	}

	private bool conversation_courtship_reaction_stage_1_on_condition()
	{
		PersuasionOptionResult item = ConversationManager.GetPersuasionChosenOptions().Last().Item2;
		if (Romance.GetRomanticLevel(Hero.MainHero, Hero.OneToOneConversationHero) == Romance.RomanceLevelEnum.CourtshipStarted)
		{
			if ((item == PersuasionOptionResult.Failure || item == PersuasionOptionResult.CriticalFailure) && GetCurrentPersuasionTask().ImmediateFailLine != null)
			{
				MBTextManager.SetTextVariable("PERSUASION_REACTION", GetCurrentPersuasionTask().ImmediateFailLine);
				if (item == PersuasionOptionResult.CriticalFailure)
				{
					foreach (PersuasionTask allReservation in _allReservations)
					{
						allReservation.BlockAllOptions();
					}
				}
			}
			else
			{
				MBTextManager.SetTextVariable("PERSUASION_REACTION", PersuasionHelper.GetDefaultPersuasionOptionReaction(item));
			}
			return true;
		}
		return false;
	}

	private bool conversation_marriage_barter_successful_on_condition()
	{
		return Campaign.Current.BarterManager.LastBarterIsAccepted;
	}

	private void conversation_marriage_barter_successful_on_consequence()
	{
		foreach (PersuasionAttempt previousRomancePersuasionAttempt in _previousRomancePersuasionAttempts)
		{
			if (previousRomancePersuasionAttempt.PersuadedHero == Hero.OneToOneConversationHero || Hero.OneToOneConversationHero.Clan.AliveLords.Contains(previousRomancePersuasionAttempt.PersuadedHero))
			{
				switch (previousRomancePersuasionAttempt.Result)
				{
				case PersuasionOptionResult.Success:
				{
					int num = ((previousRomancePersuasionAttempt.Args.ArgumentStrength < PersuasionArgumentStrength.Normal) ? (TaleWorlds.Library.MathF.Abs((int)previousRomancePersuasionAttempt.Args.ArgumentStrength) * 50) : 50);
					SkillLevelingManager.OnPersuasionSucceeded(Hero.MainHero, previousRomancePersuasionAttempt.Args.SkillUsed, PersuasionDifficulty.Medium, num);
					break;
				}
				case PersuasionOptionResult.CriticalSuccess:
				{
					int num = ((previousRomancePersuasionAttempt.Args.ArgumentStrength < PersuasionArgumentStrength.Normal) ? (TaleWorlds.Library.MathF.Abs((int)previousRomancePersuasionAttempt.Args.ArgumentStrength) * 50) : 50);
					SkillLevelingManager.OnPersuasionSucceeded(Hero.MainHero, previousRomancePersuasionAttempt.Args.SkillUsed, PersuasionDifficulty.Medium, 2 * num);
					break;
				}
				}
			}
		}
	}

	private bool conversation_courtship_reaction_stage_2_on_condition()
	{
		PersuasionOptionResult item = ConversationManager.GetPersuasionChosenOptions().Last().Item2;
		if (item == PersuasionOptionResult.Success)
		{
			MBTextManager.SetTextVariable("PERSUASION_REACTION", "{=KWBzmJQl}I am happy to hear that.");
		}
		else if (item == PersuasionOptionResult.CriticalSuccess)
		{
			MBTextManager.SetTextVariable("PERSUASION_REACTION", "{=RGZWdKDx}Ah. It makes me so glad to hear you say that!");
		}
		else if ((item == PersuasionOptionResult.Failure || item == PersuasionOptionResult.CriticalFailure) && GetCurrentPersuasionTask().ImmediateFailLine != null)
		{
			MBTextManager.SetTextVariable("PERSUASION_REACTION", GetCurrentPersuasionTask().ImmediateFailLine);
		}
		else
		{
			switch (item)
			{
			case PersuasionOptionResult.Failure:
				MBTextManager.SetTextVariable("PERSUASION_REACTION", "{=OqqUatT9}I... I think this will be difficult. Perhaps we are not meant for each other.");
				break;
			case PersuasionOptionResult.CriticalFailure:
				MBTextManager.SetTextVariable("PERSUASION_REACTION", "{=APSE3Q6r}What? No... I cannot, I cannot agree.");
				foreach (PersuasionTask allReservation in _allReservations)
				{
					allReservation.BlockAllOptions();
				}
				break;
			}
		}
		return true;
	}

	private void conversation_lord_persuade_option_reaction_on_consequence()
	{
		PersuasionTask currentPersuasionTask = GetCurrentPersuasionTask();
		Tuple<PersuasionOptionArgs, PersuasionOptionResult> tuple = ConversationManager.GetPersuasionChosenOptions().Last();
		float difficulty = Campaign.Current.Models.PersuasionModel.GetDifficulty(PersuasionDifficulty.Medium);
		Campaign.Current.Models.PersuasionModel.GetEffectChances(tuple.Item1, out var moveToNextStageChance, out var blockRandomOptionChance, difficulty);
		FindTaskOfOption(tuple.Item1).ApplyEffects(moveToNextStageChance, blockRandomOptionChance);
		PersuasionAttempt item = new PersuasionAttempt(Hero.OneToOneConversationHero, CampaignTime.Now, tuple.Item1, tuple.Item2, currentPersuasionTask.ReservationType);
		_previousRomancePersuasionAttempts.Add(item);
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

	private List<CharacterObject> FindPlayerRelativesEligibleForMarriage(Clan withClan)
	{
		List<CharacterObject> list = new List<CharacterObject>();
		MarriageModel marriageModel = Campaign.Current.Models.MarriageModel;
		foreach (Hero characterRelative in Hero.MainHero.Clan.AliveLords)
		{
			IEnumerable<Hero> source = withClan.AliveLords.Where((Hero x) => marriageModel.IsCoupleSuitableForMarriage(x, characterRelative));
			if (characterRelative != Hero.MainHero && source.Any())
			{
				list.Add(characterRelative.CharacterObject);
			}
		}
		return list;
	}

	private TextObject ShowSuccess(PersuasionOptionArgs optionArgs)
	{
		return TextObject.GetEmpty();
	}

	private bool MarriageCourtshipPossibility(Hero person1, Hero person2)
	{
		if (Campaign.Current.Models.MarriageModel.IsCoupleSuitableForMarriage(person1, person2))
		{
			return !FactionManager.IsAtWarAgainstFaction(person1.MapFaction, person2.MapFaction);
		}
		return false;
	}
}
