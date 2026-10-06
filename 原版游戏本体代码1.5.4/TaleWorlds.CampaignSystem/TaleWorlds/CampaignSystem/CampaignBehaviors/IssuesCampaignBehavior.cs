using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class IssuesCampaignBehavior : CampaignBehaviorBase
{
	private struct IssueData
	{
		public readonly PotentialIssueData PotentialIssueData;

		public readonly Hero Hero;

		public readonly float Score;

		public IssueData(PotentialIssueData issueData, Hero hero, float score)
		{
			PotentialIssueData = issueData;
			Hero = hero;
			Score = score;
		}
	}

	private const int MinNotableIssueCountForTowns = 1;

	private const int MaxNotableIssueCountForTowns = 3;

	private const int MinNotableIssueCountForVillages = 1;

	private const int MaxNotableIssueCountForVillages = 2;

	private const float MinIssuePercentageForClanHeroes = 0.1f;

	private const float MaxIssuePercentageForClanHeroes = 0.2f;

	private float _additionalFrequencyScore;

	private List<IssueData> _cachedIssueDataList = new List<IssueData>();

	public override void RegisterEvents()
	{
		CampaignEvents.DailyTickClanEvent.AddNonSerializedListener(this, DailyTickClan);
		CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
		CampaignEvents.OnNewGameCreatedPartialFollowUpEndEvent.AddNonSerializedListener(this, OnNewGameCreatedPartialFollowUpEnd);
		CampaignEvents.OnIssueUpdatedEvent.AddNonSerializedListener(this, OnIssueUpdated);
		CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
		CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, OnSettlementDailyTick);
	}

	private void OnSettlementDailyTick(Settlement settlement)
	{
		float num = 0f;
		for (int i = 0; i < settlement.HeroesWithoutParty.Count; i++)
		{
			if (settlement.HeroesWithoutParty[i].Issue != null)
			{
				num += 1f;
			}
		}
		int num2 = (settlement.IsTown ? 1 : 1);
		int num3 = (settlement.IsTown ? 3 : 2);
		if (!(num < (float)num3) || (!(num < (float)num2) && !(MBRandom.RandomFloat < GetIssueGenerationChance(num, num3))))
		{
			return;
		}
		int num4 = 0;
		foreach (KeyValuePair<Hero, IssueBase> issue in Campaign.Current.IssueManager.Issues)
		{
			if (!issue.Value.IsTriedToSolveBefore)
			{
				num4++;
			}
		}
		CreateAnIssueForSettlementNotables(settlement, num4 + 1);
	}

	private void OnNewGameCreatedPartialFollowUpEnd(CampaignGameStarter starter)
	{
		Settlement[] array = Village.All.Select((Village x) => x.Settlement).ToArray();
		int num = TaleWorlds.Library.MathF.Ceiling(0.7f * (float)array.Length);
		Settlement[] array2 = Town.AllTowns.Select((Town x) => x.Settlement).ToArray();
		int num2 = TaleWorlds.Library.MathF.Ceiling(0.8f * (float)array2.Length);
		int num3 = Hero.AllAliveHeroes.Count((Hero x) => x.IsLord && x.Clan != null && !x.Clan.IsBanditFaction && !x.IsChild);
		int num4 = TaleWorlds.Library.MathF.Ceiling(0.120000005f * (float)num3);
		int totalDesiredIssueCount = num + num2 + num4;
		Campaign.Current.ConversationManager.DisableSentenceSort();
		_additionalFrequencyScore = -0.4f;
		array.Shuffle();
		CreateRandomSettlementIssues(array, 2, num, totalDesiredIssueCount);
		array2.Shuffle();
		CreateRandomSettlementIssues(array2, 3, num2, totalDesiredIssueCount);
		Clan[] array3 = Clan.NonBanditFactions.Where((Clan x) => x.Heroes.Count != 0).ToArray();
		array3.Shuffle();
		CreateRandomClanIssues(array3, num4, totalDesiredIssueCount);
		_additionalFrequencyScore = 0.2f;
		Campaign.Current.ConversationManager.EnableSentenceSort();
	}

	private void DailyTickClan(Clan clan)
	{
		if (!IsClanSuitableForIssueCreation(clan))
		{
			return;
		}
		int num = 0;
		int num2 = 0;
		for (int i = 0; i < clan.Heroes.Count; i++)
		{
			Hero hero = clan.Heroes[i];
			if (hero.Issue != null)
			{
				num++;
			}
			if (hero.IsAlive && !hero.IsChild && hero.IsLord)
			{
				num2++;
			}
		}
		int num3 = TaleWorlds.Library.MathF.Ceiling((float)num2 * 0.1f);
		int num4 = TaleWorlds.Library.MathF.Floor((float)num2 * 0.2f);
		if (num4 <= 0 || num >= num4 || (num >= num3 && !(MBRandom.RandomFloat < GetIssueGenerationChance(num, num4))))
		{
			return;
		}
		int num5 = 0;
		foreach (KeyValuePair<Hero, IssueBase> issue in Campaign.Current.IssueManager.Issues)
		{
			if (!issue.Value.IsTriedToSolveBefore)
			{
				num5++;
			}
		}
		CreateAnIssueForClanNobles(clan, num5 + 1);
	}

	private bool IsClanSuitableForIssueCreation(Clan clan)
	{
		if (clan.Heroes.Count > 0)
		{
			return !clan.IsBanditFaction;
		}
		return false;
	}

	private void OnGameLoaded(CampaignGameStarter obj)
	{
		_additionalFrequencyScore = 0.2f;
		List<IssueBase> list = new List<IssueBase>();
		foreach (KeyValuePair<Hero, IssueBase> issue in Campaign.Current.IssueManager.Issues)
		{
			if (issue.Key.IsNotable && issue.Key.CurrentSettlement == null)
			{
				list.Add(issue.Value);
			}
		}
		foreach (IssueBase item in list)
		{
			item.CompleteIssueWithCancel();
		}
	}

	private float GetIssueGenerationChance(float currentIssueCount, int maxIssueCount)
	{
		float num = 1f - currentIssueCount / (float)maxIssueCount;
		return 0.3f * num * num;
	}

	private void CreateRandomSettlementIssues(Settlement[] shuffledSettlementArray, int maxIssueCountPerSettlement, int desiredIssueCount, int totalDesiredIssueCount)
	{
		int num = shuffledSettlementArray.Length;
		int[] array = new int[num];
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		int num5 = 0;
		while (num2 < num && num4 < desiredIssueCount)
		{
			int num6 = (num4 + num2 + num3) % num;
			if (array[num6] < num5)
			{
				num3++;
			}
			else if (array[num6] < maxIssueCountPerSettlement && CreateAnIssueForSettlementNotables(shuffledSettlementArray[num6], totalDesiredIssueCount))
			{
				num4++;
				array[num6]++;
			}
			else
			{
				num2++;
			}
		}
	}

	private void CreateRandomClanIssues(Clan[] shuffledClanArray, int desiredIssueCount, int totalDesiredIssueCount)
	{
		int num = shuffledClanArray.Length;
		int num2 = 0;
		int num3 = 0;
		while (num2 < num && num3 < desiredIssueCount)
		{
			if (CreateAnIssueForClanNobles(shuffledClanArray[(num3 + num2) % num], totalDesiredIssueCount))
			{
				num3++;
			}
			else
			{
				num2++;
			}
		}
	}

	private bool CreateAnIssueForSettlementNotables(Settlement settlement, int totalDesiredIssueCount)
	{
		IssueManager issueManager = Campaign.Current.IssueManager;
		foreach (Hero notable in settlement.Notables)
		{
			if (notable.Issue != null || !notable.CanHaveCampaignIssues())
			{
				continue;
			}
			List<PotentialIssueData> list = Campaign.Current.IssueManager.CheckForIssues(notable);
			int totalFrequencyScore = list.SumQ((PotentialIssueData x) => GetFrequencyScore(x.Frequency));
			foreach (PotentialIssueData item in list)
			{
				PotentialIssueData pid = item;
				if (pid.IsValid)
				{
					float num = CalculateIssueScoreForNotable(in pid, settlement, totalDesiredIssueCount, totalFrequencyScore);
					if (num > 0f && !issueManager.HasIssueCoolDown(pid.IssueType, notable))
					{
						_cachedIssueDataList.Add(new IssueData(pid, notable, num));
					}
				}
			}
		}
		if (_cachedIssueDataList.Count > 0)
		{
			List<(IssueData, float)> list2 = new List<(IssueData, float)>();
			foreach (IssueData cachedIssueData in _cachedIssueDataList)
			{
				list2.Add((cachedIssueData, cachedIssueData.Score));
			}
			IssueData issueData = MBRandom.ChooseWeighted(list2);
			Campaign.Current.IssueManager.CreateNewIssue(in issueData.PotentialIssueData, issueData.Hero);
			_cachedIssueDataList.Clear();
			return true;
		}
		_cachedIssueDataList.Clear();
		return false;
	}

	private bool CreateAnIssueForClanNobles(Clan clan, int totalDesiredIssueCount)
	{
		IssueData? issueData = null;
		float num = 0f;
		IssueManager issueManager = Campaign.Current.IssueManager;
		foreach (Hero aliveLord in clan.AliveLords)
		{
			if (aliveLord.Clan == Clan.PlayerClan || !aliveLord.CanHaveCampaignIssues() || !(aliveLord.Age >= (float)Campaign.Current.Models.AgeModel.HeroComesOfAge) || (!aliveLord.IsActive && !aliveLord.IsPrisoner) || aliveLord.Issue != null)
			{
				continue;
			}
			List<PotentialIssueData> list = Campaign.Current.IssueManager.CheckForIssues(aliveLord);
			int totalFrequencyScore = list.SumQ((PotentialIssueData x) => GetFrequencyScore(x.Frequency));
			foreach (PotentialIssueData item in list)
			{
				PotentialIssueData pid = item;
				if (pid.IsValid)
				{
					float num2 = CalculateIssueScoreForClan(in pid, clan, totalDesiredIssueCount, totalFrequencyScore);
					if (num2 > num && !issueManager.HasIssueCoolDown(pid.IssueType, aliveLord))
					{
						issueData = new IssueData(pid, aliveLord, num2);
						num = num2;
					}
				}
			}
		}
		if (issueData.HasValue)
		{
			IssueManager issueManager2 = Campaign.Current.IssueManager;
			IssueData value = issueData.Value;
			issueManager2.CreateNewIssue(in value.PotentialIssueData, issueData.Value.Hero);
			return true;
		}
		return false;
	}

	private float CalculateIssueScoreForClan(in PotentialIssueData pid, Clan clan, int totalDesiredIssueCount, int totalFrequencyScore)
	{
		foreach (Hero hero in clan.Heroes)
		{
			if (hero.Issue != null && hero.Issue.GetType() == pid.IssueType)
			{
				return 0f;
			}
		}
		return CalculateIssueScoreInternal(in pid, totalDesiredIssueCount, totalFrequencyScore);
	}

	private float CalculateIssueScoreForNotable(in PotentialIssueData pid, Settlement settlement, int totalDesiredIssueCount, int totalFrequencyScore)
	{
		foreach (Hero notable in settlement.Notables)
		{
			if (notable.Issue != null && notable.Issue.GetType() == pid.IssueType)
			{
				return 0f;
			}
		}
		return CalculateIssueScoreInternal(in pid, totalDesiredIssueCount, totalFrequencyScore);
	}

	private float CalculateIssueScoreInternal(in PotentialIssueData pid, int totalDesiredIssueCount, int totalFrequencyScore)
	{
		float num = (float)GetFrequencyScore(pid.Frequency) / (float)totalFrequencyScore;
		float num2;
		if (totalDesiredIssueCount == 0)
		{
			num2 = 1f;
		}
		else
		{
			int num3 = 0;
			foreach (KeyValuePair<Hero, IssueBase> issue in Campaign.Current.IssueManager.Issues)
			{
				if (issue.Value.GetType() == pid.IssueType)
				{
					num3++;
				}
			}
			num2 = (float)num3 / (float)totalDesiredIssueCount;
		}
		float num4 = 1f + _additionalFrequencyScore - num2 / num;
		if (num4 < 0f)
		{
			num4 = 0f;
		}
		else if (num4 < _additionalFrequencyScore)
		{
			num4 *= 0.01f;
		}
		else if (num4 < _additionalFrequencyScore + 0.4f)
		{
			num4 *= 0.1f;
		}
		return num * num4;
	}

	private int GetFrequencyScore(IssueBase.IssueFrequency frequency)
	{
		int result = 0;
		switch (frequency)
		{
		case IssueBase.IssueFrequency.VeryCommon:
			result = 6;
			break;
		case IssueBase.IssueFrequency.Common:
			result = 3;
			break;
		case IssueBase.IssueFrequency.Rare:
			result = 1;
			break;
		}
		return result;
	}

	private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
	{
		CharacterObject characterObject = ((party == null) ? hero.CharacterObject : party.LeaderHero?.CharacterObject);
		if (characterObject == null || characterObject.IsPlayerCharacter || party?.Army != null || !Campaign.Current.GameStarted)
		{
			return;
		}
		MBList<IssueBase> mBList = IssueManager.GetIssuesInSettlement(settlement).ToMBList();
		float num = ((settlement.OwnerClan == characterObject.HeroObject.Clan) ? 0.05f : 0.01f);
		if (mBList.Count > 0 && MBRandom.RandomFloat < num)
		{
			IssueBase randomElement = mBList.GetRandomElement();
			if (randomElement.CanBeCompletedByAI() && randomElement.IsOngoingWithoutQuest)
			{
				randomElement.CompleteIssueWithAiLord(characterObject.HeroObject);
			}
		}
	}

	private void OnIssueUpdated(IssueBase issue, IssueBase.IssueUpdateDetails details, Hero issueSolver = null)
	{
		if (details == IssueBase.IssueUpdateDetails.IssueFinishedWithSuccess && issueSolver != null && issueSolver.GetPerkValue(DefaultPerks.Charm.Oratory))
		{
			GainRenownAction.Apply(issueSolver, TaleWorlds.Library.MathF.Round(DefaultPerks.Charm.Oratory.PrimaryBonus));
			GainKingdomInfluenceAction.ApplyForDefault(issueSolver, TaleWorlds.Library.MathF.Round(DefaultPerks.Charm.Oratory.PrimaryBonus));
		}
		if ((details == IssueBase.IssueUpdateDetails.IssueFail || details == IssueBase.IssueUpdateDetails.IssueFinishedWithSuccess || details == IssueBase.IssueUpdateDetails.IssueFinishedWithBetrayal || details == IssueBase.IssueUpdateDetails.IssueTimedOut || details == IssueBase.IssueUpdateDetails.SentTroopsFinishedQuest || details == IssueBase.IssueUpdateDetails.SentTroopsFailedQuest) && issueSolver != null && issue.IssueOwner != null)
		{
			int num = (issue.IsSolvingWithQuest ? issue.IssueQuest.RelationshipChangeWithQuestGiver : issue.RelationshipChangeWithIssueOwner);
			if (num > 0)
			{
				if (issueSolver.GetPerkValue(DefaultPerks.Trade.DistributedGoods) && issue.IssueOwner.IsArtisan)
				{
					num *= (int)DefaultPerks.Trade.DistributedGoods.PrimaryBonus;
				}
				if (issueSolver.GetPerkValue(DefaultPerks.Trade.LocalConnection) && issue.IssueOwner.IsMerchant)
				{
					num *= (int)DefaultPerks.Trade.LocalConnection.PrimaryBonus;
				}
				ChangeRelationAction.ApplyPlayerRelation(issue.IsSolvingWithQuest ? issue.IssueQuest.QuestGiver : issue.IssueOwner, num);
			}
			else if (num < 0)
			{
				ChangeRelationAction.ApplyPlayerRelation(issue.IsSolvingWithQuest ? issue.IssueQuest.QuestGiver : issue.IssueOwner, num);
			}
		}
		if (details == IssueBase.IssueUpdateDetails.IssueCancel || details == IssueBase.IssueUpdateDetails.IssueFail || details == IssueBase.IssueUpdateDetails.IssueFinishedWithSuccess || details == IssueBase.IssueUpdateDetails.IssueFinishedWithBetrayal || details == IssueBase.IssueUpdateDetails.IssueTimedOut || details == IssueBase.IssueUpdateDetails.SentTroopsFinishedQuest || details == IssueBase.IssueUpdateDetails.SentTroopsFailedQuest || details == IssueBase.IssueUpdateDetails.IssueFinishedByAILord)
		{
			Campaign.Current.IssueManager.AddIssueCoolDownData(issue.GetType(), new HeroRelatedIssueCoolDownData(issue.IssueOwner, CampaignTime.DaysFromNow(Campaign.Current.Models.IssueModel.IssueOwnerCoolDownInDays)));
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void OnSessionLaunched(CampaignGameStarter starter)
	{
		List<Settlement> settlements = Settlement.All.Where((Settlement x) => x.IsTown || x.IsVillage).ToList();
		DeterministicShuffle(settlements);
		AddDialogues(starter);
	}

	private void DeterministicShuffle(List<Settlement> settlements)
	{
		Random random = new Random(53);
		for (int i = 0; i < settlements.Count; i++)
		{
			int index = random.Next() % settlements.Count;
			Settlement value = settlements[i];
			settlements[i] = settlements[index];
			settlements[index] = value;
		}
	}

	private void AddDialogues(CampaignGameStarter starter)
	{
		starter.AddDialogLine("issue_not_offered", "issue_offer", "hero_main_options", "{=!}{ISSUE_NOT_OFFERED_EXPLANATION}", issue_not_offered_condition, leave_on_conversation_end_consequence);
		starter.AddDialogLine("issue_explanation", "issue_offer", "issue_explanation_player_response", "{=!}{IssueBriefByIssueGiverText}", issue_offered_begin_condition, leave_on_conversation_end_consequence);
		starter.AddPlayerLine("issue_explanation_player_response_pre_lord_solution", "issue_explanation_player_response", "issue_lord_solution_brief", "{=!}{IssueAcceptByPlayerText}", issue_explanation_player_response_pre_lord_solution_condition, null);
		starter.AddPlayerLine("issue_explanation_player_response_pre_quest_solution", "issue_explanation_player_response", "issue_quest_solution_brief", "{=!}{IssueAcceptByPlayerText}", issue_explanation_player_response_pre_quest_solution_condition, null);
		starter.AddDialogLine("issue_lord_solution_brief", "issue_lord_solution_brief", "issue_lord_solution_player_response", "{=!}{IssueLordSolutionExplanationByIssueGiverText}", issue_lord_solution_brief_condition, null);
		starter.AddPlayerLine("issue_lord_solution_player_response", "issue_lord_solution_player_response", "issue_quest_solution_brief", "{=!}{IssuePlayerResponseAfterLordExplanationText}", issue_lord_solution_player_response_condition, null);
		starter.AddDialogLine("issue_quest_solution_brief_pre_alternative_solution", "issue_quest_solution_brief", "issue_alternative_solution_player_response", "{=!}{IssueQuestSolutionExplanationByIssueGiverText}", issue_quest_solution_brief_pre_alternative_solution_condition, null);
		starter.AddDialogLine("issue_quest_solution_brief_pre_player_response", "issue_quest_solution_brief", "issue_offer_player_response", "{=!}{IssueQuestSolutionExplanationByIssueGiverText}", issue_quest_solution_brief_pre_player_response_condition, null);
		starter.AddPlayerLine("issue_alternative_solution_player_response", "issue_alternative_solution_player_response", "issue_alternative_solution_brief", "{=!}{IssuePlayerResponseAfterAlternativeExplanationText}", issue_alternative_solution_player_response_condition, null);
		starter.AddDialogLine("issue_alternative_solution_brief", "issue_alternative_solution_brief", "issue_offer_player_response", "{=!}{IssueAlternativeSolutionExplanationByIssueGiverText}", issue_alternative_solution_brief_condition, issue_offer_player_accept_alternative_2_consequence);
		starter.AddPlayerLine("issue_offer_player_accept_quest", "issue_offer_player_response", "issue_classic_quest_start", "{=!}{IssueQuestSolutionAcceptByPlayerText}", issue_offer_player_accept_quest_condition, delegate
		{
			Campaign.Current.IssueManager.StartIssueQuest(Hero.OneToOneConversationHero);
		});
		starter.AddPlayerLine("issue_offer_player_accept_alternative", "issue_offer_player_response", "issue_offer_player_accept_alternative_2", "{=!}{IssueAlternativeSolutionAcceptByPlayerText}", issue_offer_player_accept_alternative_condition, null, 100, issue_offer_player_accept_alternative_clickable_condition);
		starter.AddPlayerLine("issue_offer_player_accept_lord", "issue_offer_player_response", "issue_offer_player_accept_lord_2", "{=!}{IssueLordSolutionAcceptByPlayerText}", issue_offer_player_accept_lord_condition, issue_offer_player_accept_lord_consequence, 100, issue_offer_player_accept_lord_clickable_condition);
		starter.AddPlayerLine("issue_offer_player_response_reject", "issue_offer_player_response", "issue_offer_hero_response_reject", "{=l549ODcw}Sorry. I can't do that right now.", null, null);
		starter.AddDialogLine("issue_offer_player_accept_alternative_2", "issue_offer_player_accept_alternative_2", "issue_offer_player_accept_alternative_3", "{=X4ITSQOl}Which of your people can help us?", null, null);
		starter.AddRepeatablePlayerLine("issue_offer_player_accept_alternative_3", "issue_offer_player_accept_alternative_3", "issue_offer_player_accept_alternative_4", "{=C2ZGNwwh}{COMPANION.NAME} {COMPANION_SCALED_PARAMETERS}", "{=nomZx5Nw}I am thinking of a different companion.", "issue_offer_player_accept_alternative_2", issue_offer_player_accept_alternative_3_condition, issue_offer_player_accept_alternative_3_consequence, 100, issue_offer_player_accept_alternative_3_clickable_condition);
		starter.AddPlayerLine("issue_offer_player_accept_go_back", "issue_offer_player_accept_alternative_3", "issue_offer_hero_response_reject", "{=OymJQD7M}Actually, I don't have any available men right now...", null, null);
		starter.AddDialogLine("issue_offer_player_accept_alternative_4", "issue_offer_player_accept_alternative_4", "issue_offer_player_accept_alternative_5", "{=!}Party screen goes here", null, issue_offer_player_accept_alternative_4_consequence);
		starter.AddDialogLine("issue_offer_player_accept_alternative_5_a", "issue_offer_player_accept_alternative_5", "close_window", "{=!}{IssueAlternativeSolutionResponseByIssueGiverText}", issue_offer_player_accept_alternative_5_a_condition, issue_offer_player_accept_alternative_5_a_consequence);
		starter.AddDialogLine("issue_offer_player_accept_alternative_5_b", "issue_offer_player_accept_alternative_5", "issue_offer_player_response", "{=!}{IssueGiverResponseToRejection}", issue_offer_hero_response_reject_condition, issue_offer_player_accept_alternative_5_b_consequence);
		starter.AddPlayerLine("issue_offer_player_back", "issue_offer_player_accept_alternative_5", "issue_offer_player_response", GameTexts.FindText("str_back").ToString(), null, null);
		starter.AddDialogLine("issue_offer_player_accept_lord_2", "issue_offer_player_accept_lord_2", "hero_main_options", "{=!}{IssueLordSolutionResponseByIssueGiverText}", issue_offer_player_accept_lord_2_condition, null);
		starter.AddDialogLine("issue_offer_hero_response_reject", "issue_offer_hero_response_reject", "hero_main_options", "{=!}{IssueGiverResponseToRejection}", issue_offer_hero_response_reject_condition, null);
		starter.AddDialogLine("issue_counter_offer_1", "start", "issue_counter_offer_2", "{=!}{IssueLordSolutionCounterOfferBriefByOtherNpcText}", issue_counter_offer_start_condition, null, int.MaxValue);
		starter.AddDialogLine("issue_counter_offer_2", "issue_counter_offer_2", "issue_counter_offer_player_response", "{=!}{IssueLordSolutionCounterOfferExplanationByOtherNpcText}", issue_counter_offer_2_condition, null);
		starter.AddPlayerLine("issue_counter_offer_player_accept", "issue_counter_offer_player_response", "issue_counter_offer_accepted", "{=!}{IssueLordSolutionCounterOfferAcceptByPlayerText}", issue_counter_offer_player_accept_condition, null);
		starter.AddDialogLine("issue_counter_offer_accepted", "issue_counter_offer_accepted", "close_window", "{=!}{IssueLordSolutionCounterOfferAcceptResponseByOtherNpcText}", issue_counter_offer_accepted_condition, issue_counter_offer_accepted_consequence);
		starter.AddPlayerLine("issue_counter_offer_player_reject", "issue_counter_offer_player_response", "issue_counter_offer_reject", "{=!}{IssueLordSolutionCounterOfferDeclineByPlayerText}", issue_counter_offer_player_reject_condition, null);
		starter.AddDialogLine("issue_counter_offer_reject", "issue_counter_offer_reject", "close_window", "{=!}{IssueLordSolutionCounterOfferDeclineResponseByOtherNpcText}", issue_counter_offer_reject_condition, issue_counter_offer_reject_consequence);
		starter.AddDialogLine("issue_alternative_solution_discuss", "issue_discuss_alternative_solution", "close_window", "{=!}{IssueDiscussAlternativeSolution}", issue_alternative_solution_discussion_condition, issue_alternative_solution_discussion_consequence, int.MaxValue);
	}

	private static bool issue_alternative_solution_discussion_condition()
	{
		IssueBase issueOwnersIssue = GetIssueOwnersIssue();
		if (issueOwnersIssue != null && issueOwnersIssue.IsThereAlternativeSolution && issueOwnersIssue.IsSolvingWithAlternative)
		{
			MBTextManager.SetTextVariable("IssueDiscussAlternativeSolution", issueOwnersIssue.IssueDiscussAlternativeSolution);
			return true;
		}
		return false;
	}

	private void issue_alternative_solution_discussion_consequence()
	{
		if (PlayerEncounter.Current != null && Campaign.Current.ConversationManager.ConversationParty == PlayerEncounter.EncounteredMobileParty)
		{
			PlayerEncounter.LeaveEncounter = true;
		}
	}

	private static void issue_counter_offer_reject_consequence()
	{
		IssueBase counterOfferersIssue = GetCounterOfferersIssue();
		Campaign.Current.ConversationManager.ConversationEndOneShot += counterOfferersIssue.CompleteIssueWithLordSolutionWithRefuseCounterOffer;
	}

	private static bool issue_counter_offer_reject_condition()
	{
		IssueBase counterOfferersIssue = GetCounterOfferersIssue();
		MBTextManager.SetTextVariable("IssueLordSolutionCounterOfferDeclineResponseByOtherNpcText", counterOfferersIssue.IssueLordSolutionCounterOfferDeclineResponseByOtherNpc);
		return true;
	}

	private static bool issue_counter_offer_player_reject_condition()
	{
		IssueBase counterOfferersIssue = GetCounterOfferersIssue();
		MBTextManager.SetTextVariable("IssueLordSolutionCounterOfferDeclineByPlayerText", counterOfferersIssue.IssueLordSolutionCounterOfferDeclineByPlayer);
		return true;
	}

	private static void issue_counter_offer_accepted_consequence()
	{
		IssueBase counterOfferersIssue = GetCounterOfferersIssue();
		Campaign.Current.ConversationManager.ConversationEndOneShot += counterOfferersIssue.CompleteIssueWithLordSolutionWithAcceptCounterOffer;
	}

	private static bool issue_counter_offer_accepted_condition()
	{
		IssueBase counterOfferersIssue = GetCounterOfferersIssue();
		MBTextManager.SetTextVariable("IssueLordSolutionCounterOfferAcceptResponseByOtherNpcText", counterOfferersIssue.IssueLordSolutionCounterOfferAcceptResponseByOtherNpc);
		return true;
	}

	private static bool issue_counter_offer_player_accept_condition()
	{
		IssueBase counterOfferersIssue = GetCounterOfferersIssue();
		MBTextManager.SetTextVariable("IssueLordSolutionCounterOfferAcceptByPlayerText", counterOfferersIssue.IssueLordSolutionCounterOfferAcceptByPlayer);
		return true;
	}

	private static bool issue_counter_offer_2_condition()
	{
		IssueBase counterOfferersIssue = GetCounterOfferersIssue();
		MBTextManager.SetTextVariable("IssueLordSolutionCounterOfferExplanationByOtherNpcText", counterOfferersIssue.IssueLordSolutionCounterOfferExplanationByOtherNpc);
		return true;
	}

	private static bool issue_counter_offer_start_condition()
	{
		IssueBase counterOfferersIssue = GetCounterOfferersIssue();
		if (counterOfferersIssue != null)
		{
			MBTextManager.SetTextVariable("IssueLordSolutionCounterOfferBriefByOtherNpcText", counterOfferersIssue.IssueLordSolutionCounterOfferBriefByOtherNpc);
			return true;
		}
		return false;
	}

	private static bool issue_offer_player_accept_lord_2_condition()
	{
		IssueBase issueOwnersIssue = GetIssueOwnersIssue();
		MBTextManager.SetTextVariable("IssueLordSolutionResponseByIssueGiverText", issueOwnersIssue.IssueLordSolutionResponseByIssueGiver);
		return true;
	}

	private void issue_offer_player_accept_alternative_4_consequence()
	{
		IssueBase issueOwnersIssue = GetIssueOwnersIssue();
		int totalAlternativeSolutionNeededMenCount = issueOwnersIssue.GetTotalAlternativeSolutionNeededMenCount();
		if (totalAlternativeSolutionNeededMenCount > 1)
		{
			PartyScreenHelper.OpenScreenAsQuest(issueOwnersIssue.AlternativeSolutionSentTroops, new TextObject("{=FbLOFO88}Select troops for mission"), totalAlternativeSolutionNeededMenCount + 1, issueOwnersIssue.GetTotalAlternativeSolutionDurationInDays(), PartyScreenDoneCondition, PartyScreenDoneClicked, TroopTransferableDelegate);
		}
		else
		{
			Campaign.Current.ConversationManager.ContinueConversation();
		}
	}

	private static void issue_offer_player_accept_alternative_5_b_consequence()
	{
		IssueBase issueOwnersIssue = GetIssueOwnersIssue();
		MobileParty.MainParty.MemberRoster.Add(issueOwnersIssue.AlternativeSolutionSentTroops);
		issueOwnersIssue.AlternativeSolutionSentTroops.Clear();
	}

	private static void issue_offer_player_accept_alternative_5_a_consequence()
	{
		IssueBase issueOwnersIssue = GetIssueOwnersIssue();
		issueOwnersIssue.AlternativeSolutionStartConsequence();
		issueOwnersIssue.StartIssueWithAlternativeSolution();
	}

	private bool issue_offer_player_accept_alternative_5_a_condition()
	{
		IssueBase issueOwnersIssue = GetIssueOwnersIssue();
		MBTextManager.SetTextVariable("IssueAlternativeSolutionResponseByIssueGiverText", issueOwnersIssue.IssueAlternativeSolutionResponseByIssueGiver);
		TextObject explanation;
		return DoTroopsSatisfyAlternativeSolutionInternal(issueOwnersIssue.AlternativeSolutionSentTroops, out explanation);
	}

	private static bool issue_offer_player_accept_alternative_3_clickable_condition(out TextObject explanation)
	{
		bool result = true;
		if (!(ConversationSentence.CurrentProcessedRepeatObject is Hero hero) || hero.PartyBelongedTo != MobileParty.MainParty)
		{
			explanation = null;
			result = false;
		}
		else if (!hero.CanHaveCampaignIssues())
		{
			explanation = new TextObject("{=DBabgrcC}This hero is not available right now.");
			result = false;
		}
		else if (hero.IsWounded)
		{
			explanation = new TextObject("{=CyrOuz4h}This hero is wounded.");
			result = false;
		}
		else if (hero.IsPregnant)
		{
			explanation = new TextObject("{=BaKOWJb6}This hero is pregnant.");
			result = false;
		}
		else
		{
			explanation = null;
		}
		return result;
	}

	private static void issue_offer_player_accept_alternative_3_consequence()
	{
		IssueBase issueOwnersIssue = GetIssueOwnersIssue();
		if (ConversationSentence.SelectedRepeatObject is Hero hero)
		{
			MobileParty.MainParty.MemberRoster.AddToCounts(hero.CharacterObject, -1);
			issueOwnersIssue.AlternativeSolutionSentTroops.AddToCounts(hero.CharacterObject, 1);
			CampaignEventDispatcher.Instance.OnHeroGetsBusy(hero, HeroGetsBusyReasons.SolvesIssue);
		}
	}

	private static bool TroopTransferableDelegate(CharacterObject character, PartyScreenLogic.TroopType type, PartyScreenLogic.PartyRosterSide side, PartyBase leftOwnerParty)
	{
		IssueBase issueOwnersIssue = GetIssueOwnersIssue();
		if (!character.IsHero && !character.IsNotTransferableInPartyScreen && type != PartyScreenLogic.TroopType.Prisoner)
		{
			return issueOwnersIssue.IsTroopTypeNeededByAlternativeSolution(character);
		}
		return false;
	}

	private static void PartyScreenDoneClicked(PartyBase leftOwnerParty, TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, PartyBase rightOwnerParty, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, bool fromCancel)
	{
		Campaign.Current.ConversationManager.ContinueConversation();
	}

	private Tuple<bool, TextObject> PartyScreenDoneCondition(TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, int leftLimitNum, int rightLimitNum)
	{
		TextObject explanation;
		return new Tuple<bool, TextObject>(DoTroopsSatisfyAlternativeSolutionInternal(leftMemberRoster, out explanation), explanation);
	}

	private static bool DoTroopsSatisfyAlternativeSolutionInternal(TroopRoster troopRoster, out TextObject explanation)
	{
		IssueBase issueOwnersIssue = GetIssueOwnersIssue();
		int totalAlternativeSolutionNeededMenCount = issueOwnersIssue.GetTotalAlternativeSolutionNeededMenCount();
		if (troopRoster.TotalRegulars >= totalAlternativeSolutionNeededMenCount && troopRoster.TotalRegulars - troopRoster.TotalWoundedRegulars < totalAlternativeSolutionNeededMenCount)
		{
			explanation = new TextObject("{=fjmGXcLW}You have to send healthy troops to this quest.");
			return false;
		}
		return issueOwnersIssue.DoTroopsSatisfyAlternativeSolution(troopRoster, out explanation);
	}

	private static bool issue_offer_player_accept_alternative_3_condition()
	{
		Hero hero = ConversationSentence.CurrentProcessedRepeatObject as Hero;
		if (hero != null)
		{
			StringHelpers.SetRepeatableCharacterProperties("COMPANION", hero.CharacterObject);
		}
		List<TextObject> list = new List<TextObject>();
		IssueModel issueModel = Campaign.Current.Models.IssueModel;
		IssueBase issueOwnersIssue = GetIssueOwnersIssue();
		bool flag = false;
		if (issueOwnersIssue.AlternativeSolutionHasCasualties)
		{
			(int, int) causalityForHero = issueModel.GetCausalityForHero(hero, issueOwnersIssue);
			if (causalityForHero.Item2 > 0)
			{
				TextObject textObject;
				if (causalityForHero.Item1 == causalityForHero.Item2)
				{
					textObject = new TextObject("{=zPlFvCRm}{NUMBER_OF_TROOPS} troop loss");
					textObject.SetTextVariable("NUMBER_OF_TROOPS", causalityForHero.Item1);
				}
				else
				{
					textObject = new TextObject("{=bdlomGZ1}{MIN_NUMBER_OF_TROOPS} - {MAX_NUMBER_OF_TROOPS_LOST} troop loss");
					textObject.SetTextVariable("MIN_NUMBER_OF_TROOPS", causalityForHero.Item1);
					textObject.SetTextVariable("MAX_NUMBER_OF_TROOPS_LOST", causalityForHero.Item2);
				}
				flag = true;
				list.Add(textObject);
			}
		}
		if (issueOwnersIssue.AlternativeSolutionHasFailureRisk)
		{
			float failureRiskForHero = issueModel.GetFailureRiskForHero(hero, issueOwnersIssue);
			if (failureRiskForHero > 0f)
			{
				failureRiskForHero = (int)(failureRiskForHero * 100f);
				TextObject textObject2 = new TextObject("{=9tLYXGGc}{FAILURE_RISK}% risk of failure");
				textObject2.SetTextVariable("FAILURE_RISK", failureRiskForHero);
				list.Add(textObject2);
				flag = true;
			}
			else
			{
				TextObject item = new TextObject("{=way8jWK8}no risk of failure");
				list.Add(item);
			}
		}
		if (issueOwnersIssue.AlternativeSolutionHasScaledRequiredTroops)
		{
			int troopsRequiredForHero = issueModel.GetTroopsRequiredForHero(hero, issueOwnersIssue);
			if (troopsRequiredForHero > 0)
			{
				TextObject textObject3 = new TextObject("{=b3bJXMt2}{NUMBER_OF_TROOPS} required troops");
				textObject3.SetTextVariable("NUMBER_OF_TROOPS", troopsRequiredForHero);
				list.Add(textObject3);
				flag = true;
			}
		}
		if (issueOwnersIssue.AlternativeSolutionHasScaledDuration)
		{
			CampaignTime durationOfResolutionForHero = issueModel.GetDurationOfResolutionForHero(hero, issueOwnersIssue);
			if (durationOfResolutionForHero > CampaignTime.Days(0f))
			{
				TextObject textObject4 = new TextObject("{=ImatoO4Y}{DURATION_IN_DAYS} required days to complete");
				textObject4.SetTextVariable("DURATION_IN_DAYS", (float)durationOfResolutionForHero.ToDays);
				list.Add(textObject4);
				flag = true;
			}
		}
		if (flag)
		{
			(SkillObject, int) issueAlternativeSolutionSkill = issueModel.GetIssueAlternativeSolutionSkill(hero, issueOwnersIssue);
			if (issueAlternativeSolutionSkill.Item1 != null)
			{
				TextObject textObject5 = new TextObject("{=!}{SKILL}: {NUMBER}");
				textObject5.SetTextVariable("SKILL", issueAlternativeSolutionSkill.Item1.Name);
				textObject5.SetTextVariable("NUMBER", hero.GetSkillValue(issueAlternativeSolutionSkill.Item1));
				list.Add(textObject5);
			}
		}
		if (list.IsEmpty())
		{
			ConversationSentence.SelectedRepeatLine.SetTextVariable("COMPANION_SCALED_PARAMETERS", TextObject.GetEmpty());
		}
		else
		{
			TextObject variable = GameTexts.GameTextHelper.MergeTextObjectsWithComma(list, includeAnd: false);
			TextObject textObject6 = GameTexts.FindText("str_STR_in_parentheses");
			textObject6.SetTextVariable("STR", variable);
			ConversationSentence.SelectedRepeatLine.SetTextVariable("COMPANION_SCALED_PARAMETERS", textObject6);
		}
		return true;
	}

	private static void issue_offer_player_accept_alternative_2_consequence()
	{
		List<Hero> list = new List<Hero>();
		foreach (TroopRosterElement item in MobileParty.MainParty.MemberRoster.GetTroopRoster())
		{
			if (item.Character.IsHero && !item.Character.IsPlayerCharacter && item.Character.HeroObject.CanHaveCampaignIssues())
			{
				list.Add(item.Character.HeroObject);
			}
		}
		ConversationSentence.SetObjectsToRepeatOver(list);
	}

	private static bool issue_offer_hero_response_reject_condition()
	{
		if (CharacterObject.OneToOneConversationCharacter.GetPersona() == DefaultTraits.PersonaCurt)
		{
			MBTextManager.SetTextVariable("IssueGiverResponseToRejection", new TextObject("{=h2Wle7ZI}Well. That's a pity."));
		}
		else if (CharacterObject.OneToOneConversationCharacter.GetPersona() == DefaultTraits.PersonaIronic)
		{
			MBTextManager.SetTextVariable("IssueGiverResponseToRejection", new TextObject("{=wbLnJrJA}Ah, well. I can look elsewhere for help, I suppose."));
		}
		else
		{
			MBTextManager.SetTextVariable("IssueGiverResponseToRejection", new TextObject("{=Uoy2tTZJ}Very well. But perhaps you will reconsider later."));
		}
		return true;
	}

	private static bool issue_offer_player_accept_lord_clickable_condition(out TextObject explanation)
	{
		IssueBase issueOwnersIssue = GetIssueOwnersIssue();
		if (!issueOwnersIssue.LordSolutionCondition(out explanation))
		{
			return false;
		}
		if (Clan.PlayerClan.Influence < (float)issueOwnersIssue.NeededInfluenceForLordSolution)
		{
			explanation = new TextObject("{=hRdhfSs0}You don't have enough influence for this solution. ({NEEDED_INFLUENCE}{INFLUENCE_ICON})");
			explanation.SetTextVariable("NEEDED_INFLUENCE", issueOwnersIssue.NeededInfluenceForLordSolution);
			explanation.SetTextVariable("INFLUENCE_ICON", "{=!}<img src=\"General\\Icons\\Influence@2x\" extend=\"5\">");
			return false;
		}
		explanation = new TextObject("{=xbvgc8Sp}This solution will cost {INFLUENCE} influence.");
		explanation.SetTextVariable("INFLUENCE", issueOwnersIssue.NeededInfluenceForLordSolution);
		return true;
	}

	private static void issue_offer_player_accept_lord_consequence()
	{
		Hero.OneToOneConversationHero.Issue.StartIssueWithLordSolution();
	}

	private bool issue_offer_player_accept_lord_condition()
	{
		IssueBase issueOwnersIssue = GetIssueOwnersIssue();
		if (issueOwnersIssue.IsThereLordSolution)
		{
			MBTextManager.SetTextVariable("IssueLordSolutionAcceptByPlayerText", issueOwnersIssue.IssueLordSolutionAcceptByPlayer);
			return IssueLordSolutionCondition();
		}
		return false;
	}

	private static bool issue_offer_player_accept_alternative_clickable_condition(out TextObject explanation)
	{
		IssueBase issueOwnersIssue = GetIssueOwnersIssue();
		if ((from m in MobileParty.MainParty.MemberRoster.GetTroopRoster()
			where m.Character.IsHero && !m.Character.IsPlayerCharacter && m.Character.HeroObject.CanHaveCampaignIssues()
			select m).IsEmpty())
		{
			if (MobileParty.MainParty.IsCurrentlyAtSea)
			{
				explanation = new TextObject("{=3V2BTAfB}You cannot do this action when you are at sea.");
			}
			else
			{
				explanation = new TextObject("{=qjpNREwg}You don't have any companions or family members.");
			}
			return false;
		}
		if (!issueOwnersIssue.AlternativeSolutionCondition(out explanation))
		{
			return false;
		}
		explanation = null;
		return true;
	}

	private static bool issue_offer_player_accept_alternative_condition()
	{
		IssueBase issueOwnersIssue = GetIssueOwnersIssue();
		if (issueOwnersIssue.IsThereAlternativeSolution)
		{
			MBTextManager.SetTextVariable("IssueAlternativeSolutionAcceptByPlayerText", issueOwnersIssue.IssueAlternativeSolutionAcceptByPlayer);
			return true;
		}
		return false;
	}

	private static bool issue_offer_player_accept_quest_condition()
	{
		IssueBase issueOwnersIssue = GetIssueOwnersIssue();
		MBTextManager.SetTextVariable("IssueQuestSolutionAcceptByPlayerText", issueOwnersIssue.IssueQuestSolutionAcceptByPlayer);
		return true;
	}

	private static bool issue_alternative_solution_brief_condition()
	{
		IssueBase issueOwnersIssue = GetIssueOwnersIssue();
		MBTextManager.SetTextVariable("IssueAlternativeSolutionExplanationByIssueGiverText", issueOwnersIssue.IssueAlternativeSolutionExplanationByIssueGiver);
		return true;
	}

	private static bool issue_alternative_solution_player_response_condition()
	{
		IssueBase issueOwnersIssue = GetIssueOwnersIssue();
		MBTextManager.SetTextVariable("IssuePlayerResponseAfterAlternativeExplanationText", issueOwnersIssue.IssuePlayerResponseAfterAlternativeExplanation);
		return issueOwnersIssue.IsThereAlternativeSolution;
	}

	private static bool issue_quest_solution_brief_pre_player_response_condition()
	{
		IssueBase issueOwnersIssue = GetIssueOwnersIssue();
		MBTextManager.SetTextVariable("IssueQuestSolutionExplanationByIssueGiverText", issueOwnersIssue.IssueQuestSolutionExplanationByIssueGiver);
		return !issueOwnersIssue.IsThereAlternativeSolution;
	}

	private static bool issue_quest_solution_brief_pre_alternative_solution_condition()
	{
		IssueBase issueOwnersIssue = GetIssueOwnersIssue();
		MBTextManager.SetTextVariable("IssueQuestSolutionExplanationByIssueGiverText", issueOwnersIssue.IssueQuestSolutionExplanationByIssueGiver);
		return issueOwnersIssue.IsThereAlternativeSolution;
	}

	private static bool issue_lord_solution_player_response_condition()
	{
		IssueBase issueOwnersIssue = GetIssueOwnersIssue();
		MBTextManager.SetTextVariable("IssuePlayerResponseAfterLordExplanationText", issueOwnersIssue.IssuePlayerResponseAfterLordExplanation);
		return true;
	}

	private static bool issue_lord_solution_brief_condition()
	{
		IssueBase issueOwnersIssue = GetIssueOwnersIssue();
		MBTextManager.SetTextVariable("IssueLordSolutionExplanationByIssueGiverText", issueOwnersIssue.IssueLordSolutionExplanationByIssueGiver);
		return true;
	}

	private bool issue_explanation_player_response_pre_quest_solution_condition()
	{
		IssueBase issueOwnersIssue = GetIssueOwnersIssue();
		MBTextManager.SetTextVariable("IssueAcceptByPlayerText", issueOwnersIssue.IssueAcceptByPlayer);
		if (issueOwnersIssue.IsThereLordSolution)
		{
			return !IssueLordSolutionCondition();
		}
		return true;
	}

	private bool issue_explanation_player_response_pre_lord_solution_condition()
	{
		IssueBase issueOwnersIssue = GetIssueOwnersIssue();
		MBTextManager.SetTextVariable("IssueAcceptByPlayerText", issueOwnersIssue.IssueAcceptByPlayer);
		if (issueOwnersIssue.IsThereLordSolution)
		{
			return IssueLordSolutionCondition();
		}
		return false;
	}

	private static bool IssueLordSolutionCondition()
	{
		IssueBase issueOwnersIssue = GetIssueOwnersIssue();
		if (issueOwnersIssue.IssueOwner.CurrentSettlement != null)
		{
			return issueOwnersIssue.IssueOwner.CurrentSettlement.OwnerClan == Clan.PlayerClan;
		}
		return false;
	}

	private static bool issue_offered_begin_condition()
	{
		IssueBase issueOwnersIssue = GetIssueOwnersIssue();
		if (issueOwnersIssue != null && issueOwnersIssue.CheckPreconditions(Hero.OneToOneConversationHero, out var _))
		{
			MBTextManager.SetTextVariable("IssueBriefByIssueGiverText", issueOwnersIssue.IssueBriefByIssueGiver);
			return true;
		}
		return false;
	}

	private static bool issue_not_offered_condition()
	{
		IssueBase issueOwnersIssue = GetIssueOwnersIssue();
		if (issueOwnersIssue != null && !issueOwnersIssue.CheckPreconditions(Hero.OneToOneConversationHero, out var explanation))
		{
			MBTextManager.SetTextVariable("ISSUE_NOT_OFFERED_EXPLANATION", explanation);
			return true;
		}
		return false;
	}

	private void leave_on_conversation_end_consequence()
	{
		Campaign.Current.ConversationManager.ConversationEndOneShot += MapEventHelper.OnConversationEnd;
	}

	private static IssueBase GetIssueOwnersIssue()
	{
		return Hero.OneToOneConversationHero?.Issue;
	}

	private static IssueBase GetCounterOfferersIssue()
	{
		if (Hero.OneToOneConversationHero != null)
		{
			foreach (IssueBase value in Campaign.Current.IssueManager.Issues.Values)
			{
				if (value.CounterOfferHero == Hero.OneToOneConversationHero && value.IsSolvingWithLordSolution)
				{
					return value;
				}
			}
		}
		return null;
	}
}
