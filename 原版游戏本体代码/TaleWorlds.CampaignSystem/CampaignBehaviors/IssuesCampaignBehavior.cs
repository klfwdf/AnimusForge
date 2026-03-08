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

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x02000409 RID: 1033
	public class IssuesCampaignBehavior : CampaignBehaviorBase
	{
		// Token: 0x06004041 RID: 16449 RVA: 0x0012B790 File Offset: 0x00129990
		public override void RegisterEvents()
		{
			CampaignEvents.DailyTickClanEvent.AddNonSerializedListener(this, new Action<Clan>(this.DailyTickClan));
			CampaignEvents.SettlementEntered.AddNonSerializedListener(this, new Action<MobileParty, Settlement, Hero>(this.OnSettlementEntered));
			CampaignEvents.OnNewGameCreatedPartialFollowUpEndEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnNewGameCreatedPartialFollowUpEnd));
			CampaignEvents.OnIssueUpdatedEvent.AddNonSerializedListener(this, new Action<IssueBase, IssueBase.IssueUpdateDetails, Hero>(this.OnIssueUpdated));
			CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnGameLoaded));
			CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnSessionLaunched));
			CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, new Action<Settlement>(this.OnSettlementDailyTick));
		}

		// Token: 0x06004042 RID: 16450 RVA: 0x0012B840 File Offset: 0x00129A40
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
			if (num < (float)num3 && (num < (float)num2 || MBRandom.RandomFloat < this.GetIssueGenerationChance(num, num3)))
			{
				int num4 = 0;
				foreach (KeyValuePair<Hero, IssueBase> keyValuePair in Campaign.Current.IssueManager.Issues)
				{
					if (!keyValuePair.Value.IsTriedToSolveBefore)
					{
						num4++;
					}
				}
				this.CreateAnIssueForSettlementNotables(settlement, num4 + 1);
			}
		}

		// Token: 0x06004043 RID: 16451 RVA: 0x0012B924 File Offset: 0x00129B24
		private void OnNewGameCreatedPartialFollowUpEnd(CampaignGameStarter starter)
		{
			Settlement[] array = (from x in Village.All
				select x.Settlement).ToArray<Settlement>();
			int num = MathF.Ceiling(0.7f * (float)array.Length);
			Settlement[] array2 = (from x in Town.AllTowns
				select x.Settlement).ToArray<Settlement>();
			int num2 = MathF.Ceiling(0.8f * (float)array2.Length);
			int num3 = Hero.AllAliveHeroes.Count((Hero x) => x.IsLord && x.Clan != null && !x.Clan.IsBanditFaction && !x.IsChild);
			int num4 = MathF.Ceiling(0.120000005f * (float)num3);
			int totalDesiredIssueCount = num + num2 + num4;
			Campaign.Current.ConversationManager.DisableSentenceSort();
			this._additionalFrequencyScore = -0.4f;
			array.Shuffle<Settlement>();
			this.CreateRandomSettlementIssues(array, 2, num, totalDesiredIssueCount);
			array2.Shuffle<Settlement>();
			this.CreateRandomSettlementIssues(array2, 3, num2, totalDesiredIssueCount);
			Clan[] array3 = (from x in Clan.NonBanditFactions
				where x.Heroes.Count != 0
				select x).ToArray<Clan>();
			array3.Shuffle<Clan>();
			this.CreateRandomClanIssues(array3, num4, totalDesiredIssueCount);
			this._additionalFrequencyScore = 0.2f;
			Campaign.Current.ConversationManager.EnableSentenceSort();
		}

		// Token: 0x06004044 RID: 16452 RVA: 0x0012BA8C File Offset: 0x00129C8C
		private void DailyTickClan(Clan clan)
		{
			if (this.IsClanSuitableForIssueCreation(clan))
			{
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
				int num3 = MathF.Ceiling((float)num2 * 0.1f);
				int num4 = MathF.Floor((float)num2 * 0.2f);
				if (num4 > 0 && num < num4 && (num < num3 || MBRandom.RandomFloat < this.GetIssueGenerationChance((float)num, num4)))
				{
					int num5 = 0;
					foreach (KeyValuePair<Hero, IssueBase> keyValuePair in Campaign.Current.IssueManager.Issues)
					{
						if (!keyValuePair.Value.IsTriedToSolveBefore)
						{
							num5++;
						}
					}
					this.CreateAnIssueForClanNobles(clan, num5 + 1);
				}
			}
		}

		// Token: 0x06004045 RID: 16453 RVA: 0x0012BBA4 File Offset: 0x00129DA4
		private bool IsClanSuitableForIssueCreation(Clan clan)
		{
			return clan.Heroes.Count > 0 && !clan.IsBanditFaction;
		}

		// Token: 0x06004046 RID: 16454 RVA: 0x0012BBC0 File Offset: 0x00129DC0
		private void OnGameLoaded(CampaignGameStarter obj)
		{
			this._additionalFrequencyScore = 0.2f;
			List<IssueBase> list = new List<IssueBase>();
			foreach (KeyValuePair<Hero, IssueBase> keyValuePair in Campaign.Current.IssueManager.Issues)
			{
				if (keyValuePair.Key.IsNotable && keyValuePair.Key.CurrentSettlement == null)
				{
					list.Add(keyValuePair.Value);
				}
			}
			foreach (IssueBase issueBase in list)
			{
				issueBase.CompleteIssueWithCancel(null);
			}
		}

		// Token: 0x06004047 RID: 16455 RVA: 0x0012BC8C File Offset: 0x00129E8C
		private float GetIssueGenerationChance(float currentIssueCount, int maxIssueCount)
		{
			float num = 1f - currentIssueCount / (float)maxIssueCount;
			return 0.3f * num * num;
		}

		// Token: 0x06004048 RID: 16456 RVA: 0x0012BCB0 File Offset: 0x00129EB0
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
				else if (array[num6] < maxIssueCountPerSettlement && this.CreateAnIssueForSettlementNotables(shuffledSettlementArray[num6], totalDesiredIssueCount))
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

		// Token: 0x06004049 RID: 16457 RVA: 0x0012BD24 File Offset: 0x00129F24
		private void CreateRandomClanIssues(Clan[] shuffledClanArray, int desiredIssueCount, int totalDesiredIssueCount)
		{
			int num = shuffledClanArray.Length;
			int num2 = 0;
			int num3 = 0;
			while (num2 < num && num3 < desiredIssueCount)
			{
				if (this.CreateAnIssueForClanNobles(shuffledClanArray[(num3 + num2) % num], totalDesiredIssueCount))
				{
					num3++;
				}
				else
				{
					num2++;
				}
			}
		}

		// Token: 0x0600404A RID: 16458 RVA: 0x0012BD60 File Offset: 0x00129F60
		private bool CreateAnIssueForSettlementNotables(Settlement settlement, int totalDesiredIssueCount)
		{
			IssueManager issueManager = Campaign.Current.IssueManager;
			foreach (Hero hero in settlement.Notables)
			{
				if (hero.Issue == null && hero.CanHaveCampaignIssues())
				{
					List<PotentialIssueData> list = Campaign.Current.IssueManager.CheckForIssues(hero);
					int totalFrequencyScore = list.SumQ((PotentialIssueData x) => this.GetFrequencyScore(x.Frequency));
					foreach (PotentialIssueData issueData in list)
					{
						if (issueData.IsValid)
						{
							float num = this.CalculateIssueScoreForNotable(issueData, settlement, totalDesiredIssueCount, totalFrequencyScore);
							if (num > 0f && !issueManager.HasIssueCoolDown(issueData.IssueType, hero))
							{
								this._cachedIssueDataList.Add(new IssuesCampaignBehavior.IssueData(issueData, hero, num));
							}
						}
					}
				}
			}
			if (this._cachedIssueDataList.Count > 0)
			{
				List<ValueTuple<IssuesCampaignBehavior.IssueData, float>> list2 = new List<ValueTuple<IssuesCampaignBehavior.IssueData, float>>();
				foreach (IssuesCampaignBehavior.IssueData issueData2 in this._cachedIssueDataList)
				{
					list2.Add(new ValueTuple<IssuesCampaignBehavior.IssueData, float>(issueData2, issueData2.Score));
				}
				IssuesCampaignBehavior.IssueData issueData3 = MBRandom.ChooseWeighted<IssuesCampaignBehavior.IssueData>(list2);
				Campaign.Current.IssueManager.CreateNewIssue(issueData3.PotentialIssueData, issueData3.Hero);
				this._cachedIssueDataList.Clear();
				return true;
			}
			this._cachedIssueDataList.Clear();
			return false;
		}

		// Token: 0x0600404B RID: 16459 RVA: 0x0012BF1C File Offset: 0x0012A11C
		private bool CreateAnIssueForClanNobles(Clan clan, int totalDesiredIssueCount)
		{
			IssuesCampaignBehavior.IssueData? issueData = null;
			float num = 0f;
			IssueManager issueManager = Campaign.Current.IssueManager;
			foreach (Hero hero in clan.AliveLords)
			{
				if (hero.Clan != Clan.PlayerClan && hero.CanHaveCampaignIssues() && hero.Age >= (float)Campaign.Current.Models.AgeModel.HeroComesOfAge && (hero.IsActive || hero.IsPrisoner) && hero.Issue == null)
				{
					List<PotentialIssueData> list = Campaign.Current.IssueManager.CheckForIssues(hero);
					int totalFrequencyScore = list.SumQ((PotentialIssueData x) => this.GetFrequencyScore(x.Frequency));
					foreach (PotentialIssueData issueData2 in list)
					{
						if (issueData2.IsValid)
						{
							float num2 = this.CalculateIssueScoreForClan(issueData2, clan, totalDesiredIssueCount, totalFrequencyScore);
							if (num2 > num && !issueManager.HasIssueCoolDown(issueData2.IssueType, hero))
							{
								issueData = new IssuesCampaignBehavior.IssueData?(new IssuesCampaignBehavior.IssueData(issueData2, hero, num2));
								num = num2;
							}
						}
					}
				}
			}
			if (issueData != null)
			{
				Campaign.Current.IssueManager.CreateNewIssue(issueData.Value.PotentialIssueData, issueData.Value.Hero);
				return true;
			}
			return false;
		}

		// Token: 0x0600404C RID: 16460 RVA: 0x0012C0D4 File Offset: 0x0012A2D4
		private float CalculateIssueScoreForClan(in PotentialIssueData pid, Clan clan, int totalDesiredIssueCount, int totalFrequencyScore)
		{
			foreach (Hero hero in clan.Heroes)
			{
				if (hero.Issue != null)
				{
					Type type = hero.Issue.GetType();
					PotentialIssueData potentialIssueData = pid;
					if (type == potentialIssueData.IssueType)
					{
						return 0f;
					}
				}
			}
			return this.CalculateIssueScoreInternal(pid, totalDesiredIssueCount, totalFrequencyScore);
		}

		// Token: 0x0600404D RID: 16461 RVA: 0x0012C15C File Offset: 0x0012A35C
		private float CalculateIssueScoreForNotable(in PotentialIssueData pid, Settlement settlement, int totalDesiredIssueCount, int totalFrequencyScore)
		{
			foreach (Hero hero in settlement.Notables)
			{
				if (hero.Issue != null)
				{
					Type type = hero.Issue.GetType();
					PotentialIssueData potentialIssueData = pid;
					if (type == potentialIssueData.IssueType)
					{
						return 0f;
					}
				}
			}
			return this.CalculateIssueScoreInternal(pid, totalDesiredIssueCount, totalFrequencyScore);
		}

		// Token: 0x0600404E RID: 16462 RVA: 0x0012C1E4 File Offset: 0x0012A3E4
		private float CalculateIssueScoreInternal(in PotentialIssueData pid, int totalDesiredIssueCount, int totalFrequencyScore)
		{
			PotentialIssueData potentialIssueData = pid;
			float num = (float)this.GetFrequencyScore(potentialIssueData.Frequency) / (float)totalFrequencyScore;
			float num2;
			if (totalDesiredIssueCount == 0)
			{
				num2 = 1f;
			}
			else
			{
				int num3 = 0;
				foreach (KeyValuePair<Hero, IssueBase> keyValuePair in Campaign.Current.IssueManager.Issues)
				{
					Type type = keyValuePair.Value.GetType();
					potentialIssueData = pid;
					if (type == potentialIssueData.IssueType)
					{
						num3++;
					}
				}
				num2 = (float)num3 / (float)totalDesiredIssueCount;
			}
			float num4 = 1f + this._additionalFrequencyScore - num2 / num;
			if (num4 < 0f)
			{
				num4 = 0f;
			}
			else if (num4 < this._additionalFrequencyScore)
			{
				num4 *= 0.01f;
			}
			else if (num4 < this._additionalFrequencyScore + 0.4f)
			{
				num4 *= 0.1f;
			}
			return num * num4;
		}

		// Token: 0x0600404F RID: 16463 RVA: 0x0012C2E0 File Offset: 0x0012A4E0
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

		// Token: 0x06004050 RID: 16464 RVA: 0x0012C310 File Offset: 0x0012A510
		private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
		{
			CharacterObject characterObject;
			if (party == null)
			{
				characterObject = hero.CharacterObject;
			}
			else
			{
				Hero leaderHero = party.LeaderHero;
				characterObject = ((leaderHero != null) ? leaderHero.CharacterObject : null);
			}
			CharacterObject characterObject2 = characterObject;
			if (characterObject2 != null && !characterObject2.IsPlayerCharacter && ((party != null) ? party.Army : null) == null && Campaign.Current.GameStarted)
			{
				MBList<IssueBase> mblist = IssueManager.GetIssuesInSettlement(settlement, true).ToMBList<IssueBase>();
				float num = ((settlement.OwnerClan == characterObject2.HeroObject.Clan) ? 0.05f : 0.01f);
				if (mblist.Count > 0 && MBRandom.RandomFloat < num)
				{
					IssueBase randomElement = mblist.GetRandomElement<IssueBase>();
					if (randomElement.CanBeCompletedByAI() && randomElement.IsOngoingWithoutQuest)
					{
						randomElement.CompleteIssueWithAiLord(characterObject2.HeroObject);
					}
				}
			}
		}

		// Token: 0x06004051 RID: 16465 RVA: 0x0012C3C4 File Offset: 0x0012A5C4
		private void OnIssueUpdated(IssueBase issue, IssueBase.IssueUpdateDetails details, Hero issueSolver = null)
		{
			if (details == IssueBase.IssueUpdateDetails.IssueFinishedWithSuccess && issueSolver != null && issueSolver.GetPerkValue(DefaultPerks.Charm.Oratory))
			{
				GainRenownAction.Apply(issueSolver, (float)MathF.Round(DefaultPerks.Charm.Oratory.PrimaryBonus), false);
				GainKingdomInfluenceAction.ApplyForDefault(issueSolver, (float)MathF.Round(DefaultPerks.Charm.Oratory.PrimaryBonus));
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
					ChangeRelationAction.ApplyPlayerRelation(issue.IsSolvingWithQuest ? issue.IssueQuest.QuestGiver : issue.IssueOwner, num, true, true);
				}
				else if (num < 0)
				{
					ChangeRelationAction.ApplyPlayerRelation(issue.IsSolvingWithQuest ? issue.IssueQuest.QuestGiver : issue.IssueOwner, num, true, true);
				}
			}
			if (details == IssueBase.IssueUpdateDetails.IssueCancel || details == IssueBase.IssueUpdateDetails.IssueFail || details == IssueBase.IssueUpdateDetails.IssueFinishedWithSuccess || details == IssueBase.IssueUpdateDetails.IssueFinishedWithBetrayal || details == IssueBase.IssueUpdateDetails.IssueTimedOut || details == IssueBase.IssueUpdateDetails.SentTroopsFinishedQuest || details == IssueBase.IssueUpdateDetails.SentTroopsFailedQuest || details == IssueBase.IssueUpdateDetails.IssueFinishedByAILord)
			{
				Campaign.Current.IssueManager.AddIssueCoolDownData(issue.GetType(), new HeroRelatedIssueCoolDownData(issue.IssueOwner, CampaignTime.DaysFromNow((float)Campaign.Current.Models.IssueModel.IssueOwnerCoolDownInDays)));
			}
		}

		// Token: 0x06004052 RID: 16466 RVA: 0x0012C557 File Offset: 0x0012A757
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x06004053 RID: 16467 RVA: 0x0012C55C File Offset: 0x0012A75C
		private void OnSessionLaunched(CampaignGameStarter starter)
		{
			List<Settlement> settlements = (from x in Settlement.All
				where x.IsTown || x.IsVillage
				select x).ToList<Settlement>();
			this.DeterministicShuffle(settlements);
			this.AddDialogues(starter);
		}

		// Token: 0x06004054 RID: 16468 RVA: 0x0012C5A8 File Offset: 0x0012A7A8
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

		// Token: 0x06004055 RID: 16469 RVA: 0x0012C5FC File Offset: 0x0012A7FC
		private void AddDialogues(CampaignGameStarter starter)
		{
			starter.AddDialogLine("issue_not_offered", "issue_offer", "hero_main_options", "{=!}{ISSUE_NOT_OFFERED_EXPLANATION}", new ConversationSentence.OnConditionDelegate(IssuesCampaignBehavior.issue_not_offered_condition), new ConversationSentence.OnConsequenceDelegate(this.leave_on_conversation_end_consequence), 100, null);
			starter.AddDialogLine("issue_explanation", "issue_offer", "issue_explanation_player_response", "{=!}{IssueBriefByIssueGiverText}", new ConversationSentence.OnConditionDelegate(IssuesCampaignBehavior.issue_offered_begin_condition), new ConversationSentence.OnConsequenceDelegate(this.leave_on_conversation_end_consequence), 100, null);
			starter.AddPlayerLine("issue_explanation_player_response_pre_lord_solution", "issue_explanation_player_response", "issue_lord_solution_brief", "{=!}{IssueAcceptByPlayerText}", new ConversationSentence.OnConditionDelegate(this.issue_explanation_player_response_pre_lord_solution_condition), null, 100, null, null);
			starter.AddPlayerLine("issue_explanation_player_response_pre_quest_solution", "issue_explanation_player_response", "issue_quest_solution_brief", "{=!}{IssueAcceptByPlayerText}", new ConversationSentence.OnConditionDelegate(this.issue_explanation_player_response_pre_quest_solution_condition), null, 100, null, null);
			starter.AddDialogLine("issue_lord_solution_brief", "issue_lord_solution_brief", "issue_lord_solution_player_response", "{=!}{IssueLordSolutionExplanationByIssueGiverText}", new ConversationSentence.OnConditionDelegate(IssuesCampaignBehavior.issue_lord_solution_brief_condition), null, 100, null);
			starter.AddPlayerLine("issue_lord_solution_player_response", "issue_lord_solution_player_response", "issue_quest_solution_brief", "{=!}{IssuePlayerResponseAfterLordExplanationText}", new ConversationSentence.OnConditionDelegate(IssuesCampaignBehavior.issue_lord_solution_player_response_condition), null, 100, null, null);
			starter.AddDialogLine("issue_quest_solution_brief_pre_alternative_solution", "issue_quest_solution_brief", "issue_alternative_solution_player_response", "{=!}{IssueQuestSolutionExplanationByIssueGiverText}", new ConversationSentence.OnConditionDelegate(IssuesCampaignBehavior.issue_quest_solution_brief_pre_alternative_solution_condition), null, 100, null);
			starter.AddDialogLine("issue_quest_solution_brief_pre_player_response", "issue_quest_solution_brief", "issue_offer_player_response", "{=!}{IssueQuestSolutionExplanationByIssueGiverText}", new ConversationSentence.OnConditionDelegate(IssuesCampaignBehavior.issue_quest_solution_brief_pre_player_response_condition), null, 100, null);
			starter.AddPlayerLine("issue_alternative_solution_player_response", "issue_alternative_solution_player_response", "issue_alternative_solution_brief", "{=!}{IssuePlayerResponseAfterAlternativeExplanationText}", new ConversationSentence.OnConditionDelegate(IssuesCampaignBehavior.issue_alternative_solution_player_response_condition), null, 100, null, null);
			starter.AddDialogLine("issue_alternative_solution_brief", "issue_alternative_solution_brief", "issue_offer_player_response", "{=!}{IssueAlternativeSolutionExplanationByIssueGiverText}", new ConversationSentence.OnConditionDelegate(IssuesCampaignBehavior.issue_alternative_solution_brief_condition), new ConversationSentence.OnConsequenceDelegate(IssuesCampaignBehavior.issue_offer_player_accept_alternative_2_consequence), 100, null);
			starter.AddPlayerLine("issue_offer_player_accept_quest", "issue_offer_player_response", "issue_classic_quest_start", "{=!}{IssueQuestSolutionAcceptByPlayerText}", new ConversationSentence.OnConditionDelegate(IssuesCampaignBehavior.issue_offer_player_accept_quest_condition), delegate
			{
				Campaign.Current.IssueManager.StartIssueQuest(Hero.OneToOneConversationHero);
			}, 100, null, null);
			starter.AddPlayerLine("issue_offer_player_accept_alternative", "issue_offer_player_response", "issue_offer_player_accept_alternative_2", "{=!}{IssueAlternativeSolutionAcceptByPlayerText}", new ConversationSentence.OnConditionDelegate(IssuesCampaignBehavior.issue_offer_player_accept_alternative_condition), null, 100, new ConversationSentence.OnClickableConditionDelegate(IssuesCampaignBehavior.issue_offer_player_accept_alternative_clickable_condition), null);
			starter.AddPlayerLine("issue_offer_player_accept_lord", "issue_offer_player_response", "issue_offer_player_accept_lord_2", "{=!}{IssueLordSolutionAcceptByPlayerText}", new ConversationSentence.OnConditionDelegate(this.issue_offer_player_accept_lord_condition), new ConversationSentence.OnConsequenceDelegate(IssuesCampaignBehavior.issue_offer_player_accept_lord_consequence), 100, new ConversationSentence.OnClickableConditionDelegate(IssuesCampaignBehavior.issue_offer_player_accept_lord_clickable_condition), null);
			starter.AddPlayerLine("issue_offer_player_response_reject", "issue_offer_player_response", "issue_offer_hero_response_reject", "{=l549ODcw}Sorry. I can't do that right now.", null, null, 100, null, null);
			starter.AddDialogLine("issue_offer_player_accept_alternative_2", "issue_offer_player_accept_alternative_2", "issue_offer_player_accept_alternative_3", "{=X4ITSQOl}Which of your people can help us?", null, null, 100, null);
			starter.AddRepeatablePlayerLine("issue_offer_player_accept_alternative_3", "issue_offer_player_accept_alternative_3", "issue_offer_player_accept_alternative_4", "{=C2ZGNwwh}{COMPANION.NAME} {COMPANION_SCALED_PARAMETERS}", "{=nomZx5Nw}I am thinking of a different companion.", "issue_offer_player_accept_alternative_2", new ConversationSentence.OnConditionDelegate(IssuesCampaignBehavior.issue_offer_player_accept_alternative_3_condition), new ConversationSentence.OnConsequenceDelegate(IssuesCampaignBehavior.issue_offer_player_accept_alternative_3_consequence), 100, new ConversationSentence.OnClickableConditionDelegate(IssuesCampaignBehavior.issue_offer_player_accept_alternative_3_clickable_condition));
			starter.AddPlayerLine("issue_offer_player_accept_go_back", "issue_offer_player_accept_alternative_3", "issue_offer_hero_response_reject", "{=OymJQD7M}Actually, I don't have any available men right now...", null, null, 100, null, null);
			starter.AddDialogLine("issue_offer_player_accept_alternative_4", "issue_offer_player_accept_alternative_4", "issue_offer_player_accept_alternative_5", "{=!}Party screen goes here", null, new ConversationSentence.OnConsequenceDelegate(this.issue_offer_player_accept_alternative_4_consequence), 100, null);
			starter.AddDialogLine("issue_offer_player_accept_alternative_5_a", "issue_offer_player_accept_alternative_5", "close_window", "{=!}{IssueAlternativeSolutionResponseByIssueGiverText}", new ConversationSentence.OnConditionDelegate(this.issue_offer_player_accept_alternative_5_a_condition), new ConversationSentence.OnConsequenceDelegate(IssuesCampaignBehavior.issue_offer_player_accept_alternative_5_a_consequence), 100, null);
			starter.AddDialogLine("issue_offer_player_accept_alternative_5_b", "issue_offer_player_accept_alternative_5", "issue_offer_player_response", "{=!}{IssueGiverResponseToRejection}", new ConversationSentence.OnConditionDelegate(IssuesCampaignBehavior.issue_offer_hero_response_reject_condition), new ConversationSentence.OnConsequenceDelegate(IssuesCampaignBehavior.issue_offer_player_accept_alternative_5_b_consequence), 100, null);
			starter.AddPlayerLine("issue_offer_player_back", "issue_offer_player_accept_alternative_5", "issue_offer_player_response", GameTexts.FindText("str_back", null).ToString(), null, null, 100, null, null);
			starter.AddDialogLine("issue_offer_player_accept_lord_2", "issue_offer_player_accept_lord_2", "hero_main_options", "{=!}{IssueLordSolutionResponseByIssueGiverText}", new ConversationSentence.OnConditionDelegate(IssuesCampaignBehavior.issue_offer_player_accept_lord_2_condition), null, 100, null);
			starter.AddDialogLine("issue_offer_hero_response_reject", "issue_offer_hero_response_reject", "hero_main_options", "{=!}{IssueGiverResponseToRejection}", new ConversationSentence.OnConditionDelegate(IssuesCampaignBehavior.issue_offer_hero_response_reject_condition), null, 100, null);
			starter.AddDialogLine("issue_counter_offer_1", "start", "issue_counter_offer_2", "{=!}{IssueLordSolutionCounterOfferBriefByOtherNpcText}", new ConversationSentence.OnConditionDelegate(IssuesCampaignBehavior.issue_counter_offer_start_condition), null, int.MaxValue, null);
			starter.AddDialogLine("issue_counter_offer_2", "issue_counter_offer_2", "issue_counter_offer_player_response", "{=!}{IssueLordSolutionCounterOfferExplanationByOtherNpcText}", new ConversationSentence.OnConditionDelegate(IssuesCampaignBehavior.issue_counter_offer_2_condition), null, 100, null);
			starter.AddPlayerLine("issue_counter_offer_player_accept", "issue_counter_offer_player_response", "issue_counter_offer_accepted", "{=!}{IssueLordSolutionCounterOfferAcceptByPlayerText}", new ConversationSentence.OnConditionDelegate(IssuesCampaignBehavior.issue_counter_offer_player_accept_condition), null, 100, null, null);
			starter.AddDialogLine("issue_counter_offer_accepted", "issue_counter_offer_accepted", "close_window", "{=!}{IssueLordSolutionCounterOfferAcceptResponseByOtherNpcText}", new ConversationSentence.OnConditionDelegate(IssuesCampaignBehavior.issue_counter_offer_accepted_condition), new ConversationSentence.OnConsequenceDelegate(IssuesCampaignBehavior.issue_counter_offer_accepted_consequence), 100, null);
			starter.AddPlayerLine("issue_counter_offer_player_reject", "issue_counter_offer_player_response", "issue_counter_offer_reject", "{=!}{IssueLordSolutionCounterOfferDeclineByPlayerText}", new ConversationSentence.OnConditionDelegate(IssuesCampaignBehavior.issue_counter_offer_player_reject_condition), null, 100, null, null);
			starter.AddDialogLine("issue_counter_offer_reject", "issue_counter_offer_reject", "close_window", "{=!}{IssueLordSolutionCounterOfferDeclineResponseByOtherNpcText}", new ConversationSentence.OnConditionDelegate(IssuesCampaignBehavior.issue_counter_offer_reject_condition), new ConversationSentence.OnConsequenceDelegate(IssuesCampaignBehavior.issue_counter_offer_reject_consequence), 100, null);
			starter.AddDialogLine("issue_alternative_solution_discuss", "issue_discuss_alternative_solution", "close_window", "{=!}{IssueDiscussAlternativeSolution}", new ConversationSentence.OnConditionDelegate(IssuesCampaignBehavior.issue_alternative_solution_discussion_condition), new ConversationSentence.OnConsequenceDelegate(this.issue_alternative_solution_discussion_consequence), int.MaxValue, null);
		}

		// Token: 0x06004056 RID: 16470 RVA: 0x0012CBBC File Offset: 0x0012ADBC
		private static bool issue_alternative_solution_discussion_condition()
		{
			IssueBase issueOwnersIssue = IssuesCampaignBehavior.GetIssueOwnersIssue();
			if (issueOwnersIssue != null && issueOwnersIssue.IsThereAlternativeSolution && issueOwnersIssue.IsSolvingWithAlternative)
			{
				MBTextManager.SetTextVariable("IssueDiscussAlternativeSolution", issueOwnersIssue.IssueDiscussAlternativeSolution, false);
				return true;
			}
			return false;
		}

		// Token: 0x06004057 RID: 16471 RVA: 0x0012CBF6 File Offset: 0x0012ADF6
		private void issue_alternative_solution_discussion_consequence()
		{
			if (PlayerEncounter.Current != null && Campaign.Current.ConversationManager.ConversationParty == PlayerEncounter.EncounteredMobileParty)
			{
				PlayerEncounter.LeaveEncounter = true;
			}
		}

		// Token: 0x06004058 RID: 16472 RVA: 0x0012CC1C File Offset: 0x0012AE1C
		private static void issue_counter_offer_reject_consequence()
		{
			IssueBase counterOfferersIssue = IssuesCampaignBehavior.GetCounterOfferersIssue();
			Campaign.Current.ConversationManager.ConversationEndOneShot += counterOfferersIssue.CompleteIssueWithLordSolutionWithRefuseCounterOffer;
		}

		// Token: 0x06004059 RID: 16473 RVA: 0x0012CC4C File Offset: 0x0012AE4C
		private static bool issue_counter_offer_reject_condition()
		{
			IssueBase counterOfferersIssue = IssuesCampaignBehavior.GetCounterOfferersIssue();
			MBTextManager.SetTextVariable("IssueLordSolutionCounterOfferDeclineResponseByOtherNpcText", counterOfferersIssue.IssueLordSolutionCounterOfferDeclineResponseByOtherNpc, false);
			return true;
		}

		// Token: 0x0600405A RID: 16474 RVA: 0x0012CC74 File Offset: 0x0012AE74
		private static bool issue_counter_offer_player_reject_condition()
		{
			IssueBase counterOfferersIssue = IssuesCampaignBehavior.GetCounterOfferersIssue();
			MBTextManager.SetTextVariable("IssueLordSolutionCounterOfferDeclineByPlayerText", counterOfferersIssue.IssueLordSolutionCounterOfferDeclineByPlayer, false);
			return true;
		}

		// Token: 0x0600405B RID: 16475 RVA: 0x0012CC9C File Offset: 0x0012AE9C
		private static void issue_counter_offer_accepted_consequence()
		{
			IssueBase counterOfferersIssue = IssuesCampaignBehavior.GetCounterOfferersIssue();
			Campaign.Current.ConversationManager.ConversationEndOneShot += counterOfferersIssue.CompleteIssueWithLordSolutionWithAcceptCounterOffer;
		}

		// Token: 0x0600405C RID: 16476 RVA: 0x0012CCCC File Offset: 0x0012AECC
		private static bool issue_counter_offer_accepted_condition()
		{
			IssueBase counterOfferersIssue = IssuesCampaignBehavior.GetCounterOfferersIssue();
			MBTextManager.SetTextVariable("IssueLordSolutionCounterOfferAcceptResponseByOtherNpcText", counterOfferersIssue.IssueLordSolutionCounterOfferAcceptResponseByOtherNpc, false);
			return true;
		}

		// Token: 0x0600405D RID: 16477 RVA: 0x0012CCF4 File Offset: 0x0012AEF4
		private static bool issue_counter_offer_player_accept_condition()
		{
			IssueBase counterOfferersIssue = IssuesCampaignBehavior.GetCounterOfferersIssue();
			MBTextManager.SetTextVariable("IssueLordSolutionCounterOfferAcceptByPlayerText", counterOfferersIssue.IssueLordSolutionCounterOfferAcceptByPlayer, false);
			return true;
		}

		// Token: 0x0600405E RID: 16478 RVA: 0x0012CD1C File Offset: 0x0012AF1C
		private static bool issue_counter_offer_2_condition()
		{
			IssueBase counterOfferersIssue = IssuesCampaignBehavior.GetCounterOfferersIssue();
			MBTextManager.SetTextVariable("IssueLordSolutionCounterOfferExplanationByOtherNpcText", counterOfferersIssue.IssueLordSolutionCounterOfferExplanationByOtherNpc, false);
			return true;
		}

		// Token: 0x0600405F RID: 16479 RVA: 0x0012CD44 File Offset: 0x0012AF44
		private static bool issue_counter_offer_start_condition()
		{
			IssueBase counterOfferersIssue = IssuesCampaignBehavior.GetCounterOfferersIssue();
			if (counterOfferersIssue != null)
			{
				MBTextManager.SetTextVariable("IssueLordSolutionCounterOfferBriefByOtherNpcText", counterOfferersIssue.IssueLordSolutionCounterOfferBriefByOtherNpc, false);
				return true;
			}
			return false;
		}

		// Token: 0x06004060 RID: 16480 RVA: 0x0012CD70 File Offset: 0x0012AF70
		private static bool issue_offer_player_accept_lord_2_condition()
		{
			IssueBase issueOwnersIssue = IssuesCampaignBehavior.GetIssueOwnersIssue();
			MBTextManager.SetTextVariable("IssueLordSolutionResponseByIssueGiverText", issueOwnersIssue.IssueLordSolutionResponseByIssueGiver, false);
			return true;
		}

		// Token: 0x06004061 RID: 16481 RVA: 0x0012CD98 File Offset: 0x0012AF98
		private void issue_offer_player_accept_alternative_4_consequence()
		{
			IssueBase issueOwnersIssue = IssuesCampaignBehavior.GetIssueOwnersIssue();
			int totalAlternativeSolutionNeededMenCount = issueOwnersIssue.GetTotalAlternativeSolutionNeededMenCount();
			if (totalAlternativeSolutionNeededMenCount > 1)
			{
				PartyScreenHelper.OpenScreenAsQuest(issueOwnersIssue.AlternativeSolutionSentTroops, new TextObject("{=FbLOFO88}Select troops for mission", null), totalAlternativeSolutionNeededMenCount + 1, issueOwnersIssue.GetTotalAlternativeSolutionDurationInDays(), new PartyPresentationDoneButtonConditionDelegate(this.PartyScreenDoneCondition), new PartyScreenClosedDelegate(IssuesCampaignBehavior.PartyScreenDoneClicked), new IsTroopTransferableDelegate(IssuesCampaignBehavior.TroopTransferableDelegate), null);
				return;
			}
			Campaign.Current.ConversationManager.ContinueConversation();
		}

		// Token: 0x06004062 RID: 16482 RVA: 0x0012CE0C File Offset: 0x0012B00C
		private static void issue_offer_player_accept_alternative_5_b_consequence()
		{
			IssueBase issueOwnersIssue = IssuesCampaignBehavior.GetIssueOwnersIssue();
			MobileParty.MainParty.MemberRoster.Add(issueOwnersIssue.AlternativeSolutionSentTroops);
			issueOwnersIssue.AlternativeSolutionSentTroops.Clear();
		}

		// Token: 0x06004063 RID: 16483 RVA: 0x0012CE3F File Offset: 0x0012B03F
		private static void issue_offer_player_accept_alternative_5_a_consequence()
		{
			IssueBase issueOwnersIssue = IssuesCampaignBehavior.GetIssueOwnersIssue();
			issueOwnersIssue.AlternativeSolutionStartConsequence();
			issueOwnersIssue.StartIssueWithAlternativeSolution();
		}

		// Token: 0x06004064 RID: 16484 RVA: 0x0012CE54 File Offset: 0x0012B054
		private bool issue_offer_player_accept_alternative_5_a_condition()
		{
			IssueBase issueOwnersIssue = IssuesCampaignBehavior.GetIssueOwnersIssue();
			MBTextManager.SetTextVariable("IssueAlternativeSolutionResponseByIssueGiverText", issueOwnersIssue.IssueAlternativeSolutionResponseByIssueGiver, false);
			TextObject textObject;
			return IssuesCampaignBehavior.DoTroopsSatisfyAlternativeSolutionInternal(issueOwnersIssue.AlternativeSolutionSentTroops, out textObject);
		}

		// Token: 0x06004065 RID: 16485 RVA: 0x0012CE88 File Offset: 0x0012B088
		private static bool issue_offer_player_accept_alternative_3_clickable_condition(out TextObject explanation)
		{
			bool result = true;
			Hero hero = ConversationSentence.CurrentProcessedRepeatObject as Hero;
			if (hero == null || hero.PartyBelongedTo != MobileParty.MainParty)
			{
				explanation = null;
				result = false;
			}
			else if (!hero.CanHaveCampaignIssues())
			{
				explanation = new TextObject("{=DBabgrcC}This hero is not available right now.", null);
				result = false;
			}
			else if (hero.IsWounded)
			{
				explanation = new TextObject("{=CyrOuz4h}This hero is wounded.", null);
				result = false;
			}
			else if (hero.IsPregnant)
			{
				explanation = new TextObject("{=BaKOWJb6}This hero is pregnant.", null);
				result = false;
			}
			else
			{
				explanation = null;
			}
			return result;
		}

		// Token: 0x06004066 RID: 16486 RVA: 0x0012CF08 File Offset: 0x0012B108
		private static void issue_offer_player_accept_alternative_3_consequence()
		{
			IssueBase issueOwnersIssue = IssuesCampaignBehavior.GetIssueOwnersIssue();
			Hero hero = ConversationSentence.SelectedRepeatObject as Hero;
			if (hero != null)
			{
				MobileParty.MainParty.MemberRoster.AddToCounts(hero.CharacterObject, -1, false, 0, 0, true, -1);
				issueOwnersIssue.AlternativeSolutionSentTroops.AddToCounts(hero.CharacterObject, 1, false, 0, 0, true, -1);
				CampaignEventDispatcher.Instance.OnHeroGetsBusy(hero, HeroGetsBusyReasons.SolvesIssue);
			}
		}

		// Token: 0x06004067 RID: 16487 RVA: 0x0012CF6C File Offset: 0x0012B16C
		private static bool TroopTransferableDelegate(CharacterObject character, PartyScreenLogic.TroopType type, PartyScreenLogic.PartyRosterSide side, PartyBase leftOwnerParty)
		{
			IssueBase issueOwnersIssue = IssuesCampaignBehavior.GetIssueOwnersIssue();
			return !character.IsHero && !character.IsNotTransferableInPartyScreen && type != PartyScreenLogic.TroopType.Prisoner && issueOwnersIssue.IsTroopTypeNeededByAlternativeSolution(character);
		}

		// Token: 0x06004068 RID: 16488 RVA: 0x0012CF9C File Offset: 0x0012B19C
		private static void PartyScreenDoneClicked(PartyBase leftOwnerParty, TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, PartyBase rightOwnerParty, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, bool fromCancel)
		{
			Campaign.Current.ConversationManager.ContinueConversation();
		}

		// Token: 0x06004069 RID: 16489 RVA: 0x0012CFB0 File Offset: 0x0012B1B0
		private Tuple<bool, TextObject> PartyScreenDoneCondition(TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, int leftLimitNum, int rightLimitNum)
		{
			TextObject item;
			return new Tuple<bool, TextObject>(IssuesCampaignBehavior.DoTroopsSatisfyAlternativeSolutionInternal(leftMemberRoster, out item), item);
		}

		// Token: 0x0600406A RID: 16490 RVA: 0x0012CFCC File Offset: 0x0012B1CC
		private static bool DoTroopsSatisfyAlternativeSolutionInternal(TroopRoster troopRoster, out TextObject explanation)
		{
			IssueBase issueOwnersIssue = IssuesCampaignBehavior.GetIssueOwnersIssue();
			int totalAlternativeSolutionNeededMenCount = issueOwnersIssue.GetTotalAlternativeSolutionNeededMenCount();
			if (troopRoster.TotalRegulars >= totalAlternativeSolutionNeededMenCount && troopRoster.TotalRegulars - troopRoster.TotalWoundedRegulars < totalAlternativeSolutionNeededMenCount)
			{
				explanation = new TextObject("{=fjmGXcLW}You have to send healthy troops to this quest.", null);
				return false;
			}
			return issueOwnersIssue.DoTroopsSatisfyAlternativeSolution(troopRoster, out explanation);
		}

		// Token: 0x0600406B RID: 16491 RVA: 0x0012D018 File Offset: 0x0012B218
		private static bool issue_offer_player_accept_alternative_3_condition()
		{
			Hero hero = ConversationSentence.CurrentProcessedRepeatObject as Hero;
			if (hero != null)
			{
				StringHelpers.SetRepeatableCharacterProperties("COMPANION", hero.CharacterObject, false);
			}
			List<TextObject> list = new List<TextObject>();
			IssueModel issueModel = Campaign.Current.Models.IssueModel;
			IssueBase issueOwnersIssue = IssuesCampaignBehavior.GetIssueOwnersIssue();
			bool flag = false;
			if (issueOwnersIssue.AlternativeSolutionHasCasualties)
			{
				ValueTuple<int, int> causalityForHero = issueModel.GetCausalityForHero(hero, issueOwnersIssue);
				if (causalityForHero.Item2 > 0)
				{
					TextObject textObject;
					if (causalityForHero.Item1 == causalityForHero.Item2)
					{
						textObject = new TextObject("{=zPlFvCRm}{NUMBER_OF_TROOPS} troop loss", null);
						textObject.SetTextVariable("NUMBER_OF_TROOPS", causalityForHero.Item1);
					}
					else
					{
						textObject = new TextObject("{=bdlomGZ1}{MIN_NUMBER_OF_TROOPS} - {MAX_NUMBER_OF_TROOPS_LOST} troop loss", null);
						textObject.SetTextVariable("MIN_NUMBER_OF_TROOPS", causalityForHero.Item1);
						textObject.SetTextVariable("MAX_NUMBER_OF_TROOPS_LOST", causalityForHero.Item2);
					}
					flag = true;
					list.Add(textObject);
				}
			}
			if (issueOwnersIssue.AlternativeSolutionHasFailureRisk)
			{
				float num = issueModel.GetFailureRiskForHero(hero, issueOwnersIssue);
				if (num > 0f)
				{
					num = (float)((int)(num * 100f));
					TextObject textObject2 = new TextObject("{=9tLYXGGc}{FAILURE_RISK}% risk of failure", null);
					textObject2.SetTextVariable("FAILURE_RISK", num, 2);
					list.Add(textObject2);
					flag = true;
				}
				else
				{
					TextObject item = new TextObject("{=way8jWK8}no risk of failure", null);
					list.Add(item);
				}
			}
			if (issueOwnersIssue.AlternativeSolutionHasScaledRequiredTroops)
			{
				int troopsRequiredForHero = issueModel.GetTroopsRequiredForHero(hero, issueOwnersIssue);
				if (troopsRequiredForHero > 0)
				{
					TextObject textObject3 = new TextObject("{=b3bJXMt2}{NUMBER_OF_TROOPS} required troops", null);
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
					TextObject textObject4 = new TextObject("{=ImatoO4Y}{DURATION_IN_DAYS} required days to complete", null);
					textObject4.SetTextVariable("DURATION_IN_DAYS", (float)durationOfResolutionForHero.ToDays, 2);
					list.Add(textObject4);
					flag = true;
				}
			}
			if (flag)
			{
				ValueTuple<SkillObject, int> issueAlternativeSolutionSkill = issueModel.GetIssueAlternativeSolutionSkill(hero, issueOwnersIssue);
				if (issueAlternativeSolutionSkill.Item1 != null)
				{
					TextObject textObject5 = new TextObject("{=!}{SKILL}: {NUMBER}", null);
					textObject5.SetTextVariable("SKILL", issueAlternativeSolutionSkill.Item1.Name);
					textObject5.SetTextVariable("NUMBER", hero.GetSkillValue(issueAlternativeSolutionSkill.Item1));
					list.Add(textObject5);
				}
			}
			if (list.IsEmpty<TextObject>())
			{
				ConversationSentence.SelectedRepeatLine.SetTextVariable("COMPANION_SCALED_PARAMETERS", TextObject.GetEmpty());
			}
			else
			{
				TextObject variable = GameTexts.GameTextHelper.MergeTextObjectsWithComma(list, false);
				TextObject textObject6 = GameTexts.FindText("str_STR_in_parentheses", null);
				textObject6.SetTextVariable("STR", variable);
				ConversationSentence.SelectedRepeatLine.SetTextVariable("COMPANION_SCALED_PARAMETERS", textObject6);
			}
			return true;
		}

		// Token: 0x0600406C RID: 16492 RVA: 0x0012D2A4 File Offset: 0x0012B4A4
		private static void issue_offer_player_accept_alternative_2_consequence()
		{
			List<Hero> list = new List<Hero>();
			foreach (TroopRosterElement troopRosterElement in MobileParty.MainParty.MemberRoster.GetTroopRoster())
			{
				if (troopRosterElement.Character.IsHero && !troopRosterElement.Character.IsPlayerCharacter && troopRosterElement.Character.HeroObject.CanHaveCampaignIssues())
				{
					list.Add(troopRosterElement.Character.HeroObject);
				}
			}
			ConversationSentence.SetObjectsToRepeatOver(list, 5);
		}

		// Token: 0x0600406D RID: 16493 RVA: 0x0012D344 File Offset: 0x0012B544
		private static bool issue_offer_hero_response_reject_condition()
		{
			if (CharacterObject.OneToOneConversationCharacter.GetPersona() == DefaultTraits.PersonaCurt)
			{
				MBTextManager.SetTextVariable("IssueGiverResponseToRejection", new TextObject("{=h2Wle7ZI}Well. That's a pity.", null), false);
			}
			else if (CharacterObject.OneToOneConversationCharacter.GetPersona() == DefaultTraits.PersonaIronic)
			{
				MBTextManager.SetTextVariable("IssueGiverResponseToRejection", new TextObject("{=wbLnJrJA}Ah, well. I can look elsewhere for help, I suppose.", null), false);
			}
			else
			{
				MBTextManager.SetTextVariable("IssueGiverResponseToRejection", new TextObject("{=Uoy2tTZJ}Very well. But perhaps you will reconsider later.", null), false);
			}
			return true;
		}

		// Token: 0x0600406E RID: 16494 RVA: 0x0012D3BC File Offset: 0x0012B5BC
		private static bool issue_offer_player_accept_lord_clickable_condition(out TextObject explanation)
		{
			IssueBase issueOwnersIssue = IssuesCampaignBehavior.GetIssueOwnersIssue();
			if (!issueOwnersIssue.LordSolutionCondition(out explanation))
			{
				return false;
			}
			if (Clan.PlayerClan.Influence < (float)issueOwnersIssue.NeededInfluenceForLordSolution)
			{
				explanation = new TextObject("{=hRdhfSs0}You don't have enough influence for this solution. ({NEEDED_INFLUENCE}{INFLUENCE_ICON})", null);
				explanation.SetTextVariable("NEEDED_INFLUENCE", issueOwnersIssue.NeededInfluenceForLordSolution);
				explanation.SetTextVariable("INFLUENCE_ICON", "{=!}<img src=\"General\\Icons\\Influence@2x\" extend=\"7\">");
				return false;
			}
			explanation = new TextObject("{=xbvgc8Sp}This solution will cost {INFLUENCE} influence.", null);
			explanation.SetTextVariable("INFLUENCE", issueOwnersIssue.NeededInfluenceForLordSolution);
			return true;
		}

		// Token: 0x0600406F RID: 16495 RVA: 0x0012D442 File Offset: 0x0012B642
		private static void issue_offer_player_accept_lord_consequence()
		{
			Hero.OneToOneConversationHero.Issue.StartIssueWithLordSolution();
		}

		// Token: 0x06004070 RID: 16496 RVA: 0x0012D454 File Offset: 0x0012B654
		private bool issue_offer_player_accept_lord_condition()
		{
			IssueBase issueOwnersIssue = IssuesCampaignBehavior.GetIssueOwnersIssue();
			if (issueOwnersIssue.IsThereLordSolution)
			{
				MBTextManager.SetTextVariable("IssueLordSolutionAcceptByPlayerText", issueOwnersIssue.IssueLordSolutionAcceptByPlayer, false);
				return IssuesCampaignBehavior.IssueLordSolutionCondition();
			}
			return false;
		}

		// Token: 0x06004071 RID: 16497 RVA: 0x0012D488 File Offset: 0x0012B688
		private static bool issue_offer_player_accept_alternative_clickable_condition(out TextObject explanation)
		{
			IssueBase issueOwnersIssue = IssuesCampaignBehavior.GetIssueOwnersIssue();
			if ((from m in MobileParty.MainParty.MemberRoster.GetTroopRoster()
				where m.Character.IsHero && !m.Character.IsPlayerCharacter && m.Character.HeroObject.CanHaveCampaignIssues()
				select m).IsEmpty<TroopRosterElement>())
			{
				if (MobileParty.MainParty.IsCurrentlyAtSea)
				{
					explanation = new TextObject("{=3V2BTAfB}You cannot do this action when you are at sea.", null);
				}
				else
				{
					explanation = new TextObject("{=qjpNREwg}You don't have any companions or family members.", null);
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

		// Token: 0x06004072 RID: 16498 RVA: 0x0012D510 File Offset: 0x0012B710
		private static bool issue_offer_player_accept_alternative_condition()
		{
			IssueBase issueOwnersIssue = IssuesCampaignBehavior.GetIssueOwnersIssue();
			if (issueOwnersIssue.IsThereAlternativeSolution)
			{
				MBTextManager.SetTextVariable("IssueAlternativeSolutionAcceptByPlayerText", issueOwnersIssue.IssueAlternativeSolutionAcceptByPlayer, false);
				return true;
			}
			return false;
		}

		// Token: 0x06004073 RID: 16499 RVA: 0x0012D540 File Offset: 0x0012B740
		private static bool issue_offer_player_accept_quest_condition()
		{
			IssueBase issueOwnersIssue = IssuesCampaignBehavior.GetIssueOwnersIssue();
			MBTextManager.SetTextVariable("IssueQuestSolutionAcceptByPlayerText", issueOwnersIssue.IssueQuestSolutionAcceptByPlayer, false);
			return true;
		}

		// Token: 0x06004074 RID: 16500 RVA: 0x0012D568 File Offset: 0x0012B768
		private static bool issue_alternative_solution_brief_condition()
		{
			IssueBase issueOwnersIssue = IssuesCampaignBehavior.GetIssueOwnersIssue();
			MBTextManager.SetTextVariable("IssueAlternativeSolutionExplanationByIssueGiverText", issueOwnersIssue.IssueAlternativeSolutionExplanationByIssueGiver, false);
			return true;
		}

		// Token: 0x06004075 RID: 16501 RVA: 0x0012D590 File Offset: 0x0012B790
		private static bool issue_alternative_solution_player_response_condition()
		{
			IssueBase issueOwnersIssue = IssuesCampaignBehavior.GetIssueOwnersIssue();
			MBTextManager.SetTextVariable("IssuePlayerResponseAfterAlternativeExplanationText", issueOwnersIssue.IssuePlayerResponseAfterAlternativeExplanation, false);
			return issueOwnersIssue.IsThereAlternativeSolution;
		}

		// Token: 0x06004076 RID: 16502 RVA: 0x0012D5BC File Offset: 0x0012B7BC
		private static bool issue_quest_solution_brief_pre_player_response_condition()
		{
			IssueBase issueOwnersIssue = IssuesCampaignBehavior.GetIssueOwnersIssue();
			MBTextManager.SetTextVariable("IssueQuestSolutionExplanationByIssueGiverText", issueOwnersIssue.IssueQuestSolutionExplanationByIssueGiver, false);
			return !issueOwnersIssue.IsThereAlternativeSolution;
		}

		// Token: 0x06004077 RID: 16503 RVA: 0x0012D5EC File Offset: 0x0012B7EC
		private static bool issue_quest_solution_brief_pre_alternative_solution_condition()
		{
			IssueBase issueOwnersIssue = IssuesCampaignBehavior.GetIssueOwnersIssue();
			MBTextManager.SetTextVariable("IssueQuestSolutionExplanationByIssueGiverText", issueOwnersIssue.IssueQuestSolutionExplanationByIssueGiver, false);
			return issueOwnersIssue.IsThereAlternativeSolution;
		}

		// Token: 0x06004078 RID: 16504 RVA: 0x0012D618 File Offset: 0x0012B818
		private static bool issue_lord_solution_player_response_condition()
		{
			IssueBase issueOwnersIssue = IssuesCampaignBehavior.GetIssueOwnersIssue();
			MBTextManager.SetTextVariable("IssuePlayerResponseAfterLordExplanationText", issueOwnersIssue.IssuePlayerResponseAfterLordExplanation, false);
			return true;
		}

		// Token: 0x06004079 RID: 16505 RVA: 0x0012D640 File Offset: 0x0012B840
		private static bool issue_lord_solution_brief_condition()
		{
			IssueBase issueOwnersIssue = IssuesCampaignBehavior.GetIssueOwnersIssue();
			MBTextManager.SetTextVariable("IssueLordSolutionExplanationByIssueGiverText", issueOwnersIssue.IssueLordSolutionExplanationByIssueGiver, false);
			return true;
		}

		// Token: 0x0600407A RID: 16506 RVA: 0x0012D668 File Offset: 0x0012B868
		private bool issue_explanation_player_response_pre_quest_solution_condition()
		{
			IssueBase issueOwnersIssue = IssuesCampaignBehavior.GetIssueOwnersIssue();
			MBTextManager.SetTextVariable("IssueAcceptByPlayerText", issueOwnersIssue.IssueAcceptByPlayer, false);
			return !issueOwnersIssue.IsThereLordSolution || !IssuesCampaignBehavior.IssueLordSolutionCondition();
		}

		// Token: 0x0600407B RID: 16507 RVA: 0x0012D6A0 File Offset: 0x0012B8A0
		private bool issue_explanation_player_response_pre_lord_solution_condition()
		{
			IssueBase issueOwnersIssue = IssuesCampaignBehavior.GetIssueOwnersIssue();
			MBTextManager.SetTextVariable("IssueAcceptByPlayerText", issueOwnersIssue.IssueAcceptByPlayer, false);
			return issueOwnersIssue.IsThereLordSolution && IssuesCampaignBehavior.IssueLordSolutionCondition();
		}

		// Token: 0x0600407C RID: 16508 RVA: 0x0012D6D4 File Offset: 0x0012B8D4
		private static bool IssueLordSolutionCondition()
		{
			IssueBase issueOwnersIssue = IssuesCampaignBehavior.GetIssueOwnersIssue();
			return issueOwnersIssue.IssueOwner.CurrentSettlement != null && issueOwnersIssue.IssueOwner.CurrentSettlement.OwnerClan == Clan.PlayerClan;
		}

		// Token: 0x0600407D RID: 16509 RVA: 0x0012D710 File Offset: 0x0012B910
		private static bool issue_offered_begin_condition()
		{
			IssueBase issueOwnersIssue = IssuesCampaignBehavior.GetIssueOwnersIssue();
			TextObject textObject;
			if (issueOwnersIssue != null && issueOwnersIssue.CheckPreconditions(Hero.OneToOneConversationHero, out textObject))
			{
				MBTextManager.SetTextVariable("IssueBriefByIssueGiverText", issueOwnersIssue.IssueBriefByIssueGiver, false);
				return true;
			}
			return false;
		}

		// Token: 0x0600407E RID: 16510 RVA: 0x0012D74C File Offset: 0x0012B94C
		private static bool issue_not_offered_condition()
		{
			IssueBase issueOwnersIssue = IssuesCampaignBehavior.GetIssueOwnersIssue();
			TextObject text;
			if (issueOwnersIssue != null && !issueOwnersIssue.CheckPreconditions(Hero.OneToOneConversationHero, out text))
			{
				MBTextManager.SetTextVariable("ISSUE_NOT_OFFERED_EXPLANATION", text, false);
				return true;
			}
			return false;
		}

		// Token: 0x0600407F RID: 16511 RVA: 0x0012D780 File Offset: 0x0012B980
		private void leave_on_conversation_end_consequence()
		{
			Campaign.Current.ConversationManager.ConversationEndOneShot += MapEventHelper.OnConversationEnd;
		}

		// Token: 0x06004080 RID: 16512 RVA: 0x0012D79D File Offset: 0x0012B99D
		private static IssueBase GetIssueOwnersIssue()
		{
			Hero oneToOneConversationHero = Hero.OneToOneConversationHero;
			if (oneToOneConversationHero == null)
			{
				return null;
			}
			return oneToOneConversationHero.Issue;
		}

		// Token: 0x06004081 RID: 16513 RVA: 0x0012D7B0 File Offset: 0x0012B9B0
		private static IssueBase GetCounterOfferersIssue()
		{
			if (Hero.OneToOneConversationHero != null)
			{
				foreach (IssueBase issueBase in Campaign.Current.IssueManager.Issues.Values)
				{
					if (issueBase.CounterOfferHero == Hero.OneToOneConversationHero && issueBase.IsSolvingWithLordSolution)
					{
						return issueBase;
					}
				}
			}
			return null;
		}

		// Token: 0x040012D2 RID: 4818
		private const int MinNotableIssueCountForTowns = 1;

		// Token: 0x040012D3 RID: 4819
		private const int MaxNotableIssueCountForTowns = 3;

		// Token: 0x040012D4 RID: 4820
		private const int MinNotableIssueCountForVillages = 1;

		// Token: 0x040012D5 RID: 4821
		private const int MaxNotableIssueCountForVillages = 2;

		// Token: 0x040012D6 RID: 4822
		private const float MinIssuePercentageForClanHeroes = 0.1f;

		// Token: 0x040012D7 RID: 4823
		private const float MaxIssuePercentageForClanHeroes = 0.2f;

		// Token: 0x040012D8 RID: 4824
		private float _additionalFrequencyScore;

		// Token: 0x040012D9 RID: 4825
		private List<IssuesCampaignBehavior.IssueData> _cachedIssueDataList = new List<IssuesCampaignBehavior.IssueData>();

		// Token: 0x0200080A RID: 2058
		private struct IssueData
		{
			// Token: 0x06006560 RID: 25952 RVA: 0x001C144C File Offset: 0x001BF64C
			public IssueData(PotentialIssueData issueData, Hero hero, float score)
			{
				this.PotentialIssueData = issueData;
				this.Hero = hero;
				this.Score = score;
			}

			// Token: 0x040021CF RID: 8655
			public readonly PotentialIssueData PotentialIssueData;

			// Token: 0x040021D0 RID: 8656
			public readonly Hero Hero;

			// Token: 0x040021D1 RID: 8657
			public readonly float Score;
		}
	}
}
