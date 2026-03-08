using System;
using System.Collections.Generic;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace TaleWorlds.CampaignSystem.Issues
{
	// Token: 0x02000372 RID: 882
	public class MerchantNeedsHelpWithOutlawsIssueQuestBehavior : CampaignBehaviorBase
	{
		// Token: 0x17000C3D RID: 3133
		// (get) Token: 0x06003366 RID: 13158 RVA: 0x000D2B82 File Offset: 0x000D0D82
		private static float ValidBanditPartyDistance
		{
			get
			{
				return MerchantNeedsHelpWithOutlawsIssueQuestBehavior.NeededHideoutDistanceToSpawnTheQuest * 0.75f;
			}
		}

		// Token: 0x17000C3E RID: 3134
		// (get) Token: 0x06003367 RID: 13159 RVA: 0x000D2B8F File Offset: 0x000D0D8F
		private static float NeededHideoutDistanceToSpawnTheQuest
		{
			get
			{
				return Campaign.Current.EstimatedAverageBanditPartySpeed * (float)CampaignTime.HoursInDay;
			}
		}

		// Token: 0x06003368 RID: 13160 RVA: 0x000D2BA4 File Offset: 0x000D0DA4
		public override void RegisterEvents()
		{
			CampaignEvents.OnCheckForIssueEvent.AddNonSerializedListener(this, new Action<Hero>(this.OnCheckForIssue));
			CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnNewGameCreated));
			CampaignEvents.OnGameEarlyLoadedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnGameEarlyLoaded));
		}

		// Token: 0x06003369 RID: 13161 RVA: 0x000D2BF6 File Offset: 0x000D0DF6
		private void OnGameEarlyLoaded(CampaignGameStarter obj)
		{
			this.InitializeCache();
		}

		// Token: 0x0600336A RID: 13162 RVA: 0x000D2BFE File Offset: 0x000D0DFE
		private void OnNewGameCreated(CampaignGameStarter obj)
		{
			this.InitializeCache();
		}

		// Token: 0x0600336B RID: 13163 RVA: 0x000D2C08 File Offset: 0x000D0E08
		private void InitializeCache()
		{
			foreach (Settlement settlement in Settlement.All)
			{
				if (settlement.IsTown || settlement.IsVillage)
				{
					foreach (Hideout hideout in Hideout.All)
					{
						if (Campaign.Current.Models.MapDistanceModel.GetDistance(hideout.Settlement, settlement, false, false, MobileParty.NavigationType.Default) < Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(MobileParty.NavigationType.Default) * 1.25f)
						{
							if (!this._closestHideoutsToSettlements.ContainsKey(settlement))
							{
								this._closestHideoutsToSettlements.Add(settlement, new List<Hideout> { hideout });
							}
							else
							{
								this._closestHideoutsToSettlements[settlement].Add(hideout);
							}
						}
					}
				}
			}
		}

		// Token: 0x0600336C RID: 13164 RVA: 0x000D2D14 File Offset: 0x000D0F14
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x0600336D RID: 13165 RVA: 0x000D2D18 File Offset: 0x000D0F18
		private bool ConditionsHold(Hero issueGiver, out Hideout hideout)
		{
			hideout = null;
			List<Hideout> list;
			if ((issueGiver.IsMerchant || issueGiver.IsRuralNotable) && this._closestHideoutsToSettlements.TryGetValue(issueGiver.CurrentSettlement, out list))
			{
				foreach (Hideout hideout2 in list)
				{
					if (hideout2.IsInfested && !hideout2.Settlement.IsSettlementBusy(this))
					{
						hideout = hideout2;
						return true;
					}
				}
				return false;
			}
			return false;
		}

		// Token: 0x0600336E RID: 13166 RVA: 0x000D2DA8 File Offset: 0x000D0FA8
		public void OnCheckForIssue(Hero hero)
		{
			Hideout relatedObject;
			if (this.ConditionsHold(hero, out relatedObject))
			{
				Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(new PotentialIssueData.StartIssueDelegate(this.OnSelected), typeof(MerchantNeedsHelpWithOutlawsIssueQuestBehavior.MerchantNeedsHelpWithOutlawsIssue), IssueBase.IssueFrequency.VeryCommon, relatedObject));
				return;
			}
			Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(typeof(MerchantNeedsHelpWithOutlawsIssueQuestBehavior.MerchantNeedsHelpWithOutlawsIssue), IssueBase.IssueFrequency.VeryCommon));
		}

		// Token: 0x0600336F RID: 13167 RVA: 0x000D2E10 File Offset: 0x000D1010
		private IssueBase OnSelected(in PotentialIssueData pid, Hero issueOwner)
		{
			PotentialIssueData potentialIssueData = pid;
			Hideout relatedHideout = potentialIssueData.RelatedObject as Hideout;
			return new MerchantNeedsHelpWithOutlawsIssueQuestBehavior.MerchantNeedsHelpWithOutlawsIssue(issueOwner, relatedHideout);
		}

		// Token: 0x04000EA8 RID: 3752
		private const IssueBase.IssueFrequency MerchantNeedsHelpWithOutlawsIssueFrequency = IssueBase.IssueFrequency.VeryCommon;

		// Token: 0x04000EA9 RID: 3753
		private readonly Dictionary<Settlement, List<Hideout>> _closestHideoutsToSettlements = new Dictionary<Settlement, List<Hideout>>();

		// Token: 0x02000739 RID: 1849
		public class MerchantNeedsHelpWithOutlawsIssue : IssueBase
		{
			// Token: 0x06005D08 RID: 23816 RVA: 0x001AEC80 File Offset: 0x001ACE80
			internal static void AutoGeneratedStaticCollectObjectsMerchantNeedsHelpWithOutlawsIssue(object o, List<object> collectedObjects)
			{
				((MerchantNeedsHelpWithOutlawsIssueQuestBehavior.MerchantNeedsHelpWithOutlawsIssue)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
			}

			// Token: 0x06005D09 RID: 23817 RVA: 0x001AEC8E File Offset: 0x001ACE8E
			protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
			{
				base.AutoGeneratedInstanceCollectObjects(collectedObjects);
				collectedObjects.Add(this.RelatedHideout);
			}

			// Token: 0x06005D0A RID: 23818 RVA: 0x001AECA3 File Offset: 0x001ACEA3
			internal static object AutoGeneratedGetMemberValueRelatedHideout(object o)
			{
				return ((MerchantNeedsHelpWithOutlawsIssueQuestBehavior.MerchantNeedsHelpWithOutlawsIssue)o).RelatedHideout;
			}

			// Token: 0x170012A0 RID: 4768
			// (get) Token: 0x06005D0B RID: 23819 RVA: 0x001AECB0 File Offset: 0x001ACEB0
			public override IssueBase.AlternativeSolutionScaleFlag AlternativeSolutionScaleFlags
			{
				get
				{
					return IssueBase.AlternativeSolutionScaleFlag.Casualties | IssueBase.AlternativeSolutionScaleFlag.FailureRisk;
				}
			}

			// Token: 0x170012A1 RID: 4769
			// (get) Token: 0x06005D0C RID: 23820 RVA: 0x001AECB4 File Offset: 0x001ACEB4
			private int TotalPartyCount
			{
				get
				{
					return (int)(2f + 6f * base.IssueDifficultyMultiplier);
				}
			}

			// Token: 0x170012A2 RID: 4770
			// (get) Token: 0x06005D0D RID: 23821 RVA: 0x001AECC9 File Offset: 0x001ACEC9
			public override int AlternativeSolutionBaseNeededMenCount
			{
				get
				{
					return 8 + MathF.Ceiling(11f * base.IssueDifficultyMultiplier);
				}
			}

			// Token: 0x170012A3 RID: 4771
			// (get) Token: 0x06005D0E RID: 23822 RVA: 0x001AECDE File Offset: 0x001ACEDE
			protected override int AlternativeSolutionBaseDurationInDaysInternal
			{
				get
				{
					return 5 + MathF.Ceiling(7f * base.IssueDifficultyMultiplier);
				}
			}

			// Token: 0x170012A4 RID: 4772
			// (get) Token: 0x06005D0F RID: 23823 RVA: 0x001AECF3 File Offset: 0x001ACEF3
			protected override int RewardGold
			{
				get
				{
					return (int)(400f + 1500f * base.IssueDifficultyMultiplier);
				}
			}

			// Token: 0x170012A5 RID: 4773
			// (get) Token: 0x06005D10 RID: 23824 RVA: 0x001AED08 File Offset: 0x001ACF08
			public override TextObject IssueBriefByIssueGiver
			{
				get
				{
					return new TextObject("{=ib6ltlM0}Yes... We've always had trouble with bandits, but recently we've had a lot more than our share. The hills outside of town are infested. A lot of us are afraid to take their goods to market. Some have been murdered. People tell me, 'I'm getting so desperate, maybe I'll turn bandit myself.' It's bad...[ib:demure2][if:convo_dismayed]", null);
				}
			}

			// Token: 0x170012A6 RID: 4774
			// (get) Token: 0x06005D11 RID: 23825 RVA: 0x001AED15 File Offset: 0x001ACF15
			public override TextObject IssueAcceptByPlayer
			{
				get
				{
					return new TextObject("{=qNxdWLFY}So you want me to hunt them down?", null);
				}
			}

			// Token: 0x170012A7 RID: 4775
			// (get) Token: 0x06005D12 RID: 23826 RVA: 0x001AED24 File Offset: 0x001ACF24
			public override TextObject IssueQuestSolutionExplanationByIssueGiver
			{
				get
				{
					TextObject textObject = new TextObject("{=DlRMT7XD}Well, {?PLAYER.GENDER}my lady{?}sir{\\?}, you'll never get all those outlaws,[if:convo_thinking] but if word gets around that you took down some of the most vicious ones - let's say {TOTAL_COUNT} bands of brigands - robbing us wouldn't seem so lucrative. Maybe the rest would go bother someone else... Do you think you can help us?", null);
					StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject, false);
					textObject.SetTextVariable("TOTAL_COUNT", this.TotalPartyCount);
					return textObject;
				}
			}

			// Token: 0x170012A8 RID: 4776
			// (get) Token: 0x06005D13 RID: 23827 RVA: 0x001AED62 File Offset: 0x001ACF62
			public override TextObject IssueAlternativeSolutionExplanationByIssueGiver
			{
				get
				{
					TextObject textObject = new TextObject("{=5RjvnQ3d}I bet even a party of {ALTERNATIVE_COUNT} properly trained men accompanied by one of your lieutenants can handle any band they find. Give them {TOTAL_DAYS} days, say... That will make a difference.[if:convo_undecided_open]", null);
					textObject.SetTextVariable("ALTERNATIVE_COUNT", base.GetTotalAlternativeSolutionNeededMenCount());
					textObject.SetTextVariable("TOTAL_DAYS", base.GetTotalAlternativeSolutionDurationInDays());
					return textObject;
				}
			}

			// Token: 0x170012A9 RID: 4777
			// (get) Token: 0x06005D14 RID: 23828 RVA: 0x001AED93 File Offset: 0x001ACF93
			public override TextObject IssuePlayerResponseAfterAlternativeExplanation
			{
				get
				{
					return new TextObject("{=BPfuSkCl}That depends. How many men do you think are required to get the job done?", null);
				}
			}

			// Token: 0x170012AA RID: 4778
			// (get) Token: 0x06005D15 RID: 23829 RVA: 0x001AEDA0 File Offset: 0x001ACFA0
			public override TextObject IssueQuestSolutionAcceptByPlayer
			{
				get
				{
					TextObject textObject = new TextObject("{=2ApU6iCB}I'll hunt down {TOTAL_COUNT} bands of brigands for you.", null);
					textObject.SetTextVariable("TOTAL_COUNT", this.TotalPartyCount);
					return textObject;
				}
			}

			// Token: 0x170012AB RID: 4779
			// (get) Token: 0x06005D16 RID: 23830 RVA: 0x001AEDBF File Offset: 0x001ACFBF
			public override TextObject IssueAlternativeSolutionAcceptByPlayer
			{
				get
				{
					TextObject textObject = new TextObject("{=DLbFbYkR}I will have one of my companions and {ALTERNATIVE_COUNT} of my men patrol the area for {TOTAL_DAYS} days.", null);
					textObject.SetTextVariable("ALTERNATIVE_COUNT", base.GetTotalAlternativeSolutionNeededMenCount());
					textObject.SetTextVariable("TOTAL_DAYS", base.GetTotalAlternativeSolutionDurationInDays());
					return textObject;
				}
			}

			// Token: 0x170012AC RID: 4780
			// (get) Token: 0x06005D17 RID: 23831 RVA: 0x001AEDF0 File Offset: 0x001ACFF0
			public override TextObject IssueDiscussAlternativeSolution
			{
				get
				{
					return new TextObject("{=PexmGuOd}{?PLAYER.GENDER}Madam{?}Sir{\\?}, I am happy to tell that the men you left are patrolling, and already we feel safer. Thank you again.[ib:demure][if:convo_grateful]", null);
				}
			}

			// Token: 0x170012AD RID: 4781
			// (get) Token: 0x06005D18 RID: 23832 RVA: 0x001AEE00 File Offset: 0x001AD000
			public override TextObject IssueAlternativeSolutionResponseByIssueGiver
			{
				get
				{
					TextObject textObject = new TextObject("{=FYfZFve3}Thank you, {?PLAYER.GENDER}my lady{?}my lord{\\?}. Hopefully, we can travel safely again.", null);
					StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject, false);
					return textObject;
				}
			}

			// Token: 0x170012AE RID: 4782
			// (get) Token: 0x06005D19 RID: 23833 RVA: 0x001AEE2C File Offset: 0x001AD02C
			public override bool IsThereAlternativeSolution
			{
				get
				{
					return true;
				}
			}

			// Token: 0x170012AF RID: 4783
			// (get) Token: 0x06005D1A RID: 23834 RVA: 0x001AEE2F File Offset: 0x001AD02F
			public override bool IsThereLordSolution
			{
				get
				{
					return false;
				}
			}

			// Token: 0x170012B0 RID: 4784
			// (get) Token: 0x06005D1B RID: 23835 RVA: 0x001AEE32 File Offset: 0x001AD032
			protected override int CompanionSkillRewardXP
			{
				get
				{
					return (int)(600f + 800f * base.IssueDifficultyMultiplier);
				}
			}

			// Token: 0x170012B1 RID: 4785
			// (get) Token: 0x06005D1C RID: 23836 RVA: 0x001AEE48 File Offset: 0x001AD048
			protected override TextObject AlternativeSolutionStartLog
			{
				get
				{
					TextObject textObject = new TextObject("{=Bdt41knf}You have accepted {QUEST_GIVER.LINK}'s request to find at least {TOTAL_COUNT} different parties of brigands around {QUEST_SETTLEMENT} and sent {COMPANION.LINK} and with {?COMPANION.GENDER}her{?}his{\\?} {ALTERNATIVE_COUNT} of your men to deal with them. They should return with the reward of {GOLD_AMOUNT}{GOLD_ICON} denars as promised by {QUEST_GIVER.LINK} after dealing with them in {RETURN_DAYS} days.", null);
					textObject.SetTextVariable("TOTAL_COUNT", this.TotalPartyCount);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.IssueOwner.CharacterObject, textObject, false);
					StringHelpers.SetCharacterProperties("COMPANION", base.AlternativeSolutionHero.CharacterObject, textObject, false);
					textObject.SetTextVariable("QUEST_SETTLEMENT", base.IssueOwner.CurrentSettlement.EncyclopediaLinkWithName);
					textObject.SetTextVariable("ALTERNATIVE_COUNT", this.AlternativeSolutionSentTroops.TotalManCount - 1);
					textObject.SetTextVariable("GOLD_AMOUNT", this.RewardGold);
					textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
					textObject.SetTextVariable("RETURN_DAYS", base.GetTotalAlternativeSolutionDurationInDays());
					return textObject;
				}
			}

			// Token: 0x170012B2 RID: 4786
			// (get) Token: 0x06005D1D RID: 23837 RVA: 0x001AEF10 File Offset: 0x001AD110
			public override TextObject Title
			{
				get
				{
					TextObject textObject = new TextObject("{=ABmCO23x}{QUEST_GIVER.NAME} Needs Help With Brigands", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.IssueOwner.CharacterObject, textObject, false);
					return textObject;
				}
			}

			// Token: 0x170012B3 RID: 4787
			// (get) Token: 0x06005D1E RID: 23838 RVA: 0x001AEF42 File Offset: 0x001AD142
			public override TextObject Description
			{
				get
				{
					return new TextObject("{=sAobCa9U}Brigands are disturbing travelers outside the town. Someone needs to hunt them down.", null);
				}
			}

			// Token: 0x06005D1F RID: 23839 RVA: 0x001AEF4F File Offset: 0x001AD14F
			public MerchantNeedsHelpWithOutlawsIssue(Hero issueOwner, Hideout relatedHideout)
				: base(issueOwner, CampaignTime.DaysFromNow(15f))
			{
				this.RelatedHideout = relatedHideout;
			}

			// Token: 0x06005D20 RID: 23840 RVA: 0x001AEF69 File Offset: 0x001AD169
			protected override float GetIssueEffectAmountInternal(IssueEffect issueEffect)
			{
				if (issueEffect == DefaultIssueEffects.SettlementProsperity)
				{
					return -0.2f;
				}
				if (issueEffect == DefaultIssueEffects.IssueOwnerPower)
				{
					return -0.1f;
				}
				if (issueEffect == DefaultIssueEffects.SettlementSecurity)
				{
					return -1f;
				}
				return 0f;
			}

			// Token: 0x06005D21 RID: 23841 RVA: 0x001AEF9A File Offset: 0x001AD19A
			public override ValueTuple<SkillObject, int> GetAlternativeSolutionSkill(Hero hero)
			{
				return new ValueTuple<SkillObject, int>((hero.GetSkillValue(DefaultSkills.Tactics) >= hero.GetSkillValue(DefaultSkills.Scouting)) ? DefaultSkills.Tactics : DefaultSkills.Scouting, 120);
			}

			// Token: 0x06005D22 RID: 23842 RVA: 0x001AEFC7 File Offset: 0x001AD1C7
			public override bool DoTroopsSatisfyAlternativeSolution(TroopRoster troopRoster, out TextObject explanation)
			{
				return QuestHelper.CheckRosterForAlternativeSolution(troopRoster, base.GetTotalAlternativeSolutionNeededMenCount(), out explanation, 2, false);
			}

			// Token: 0x06005D23 RID: 23843 RVA: 0x001AEFD8 File Offset: 0x001AD1D8
			public override bool AlternativeSolutionCondition(out TextObject explanation)
			{
				return QuestHelper.CheckRosterForAlternativeSolution(MobileParty.MainParty.MemberRoster, base.GetTotalAlternativeSolutionNeededMenCount(), out explanation, 2, false);
			}

			// Token: 0x06005D24 RID: 23844 RVA: 0x001AEFF2 File Offset: 0x001AD1F2
			public override bool IsTroopTypeNeededByAlternativeSolution(CharacterObject character)
			{
				return character.Tier >= 2;
			}

			// Token: 0x06005D25 RID: 23845 RVA: 0x001AF000 File Offset: 0x001AD200
			protected override void AlternativeSolutionEndWithSuccessConsequence()
			{
				this.RelationshipChangeWithIssueOwner = 3;
				if (base.IssueOwner.CurrentSettlement.IsVillage && base.IssueOwner.CurrentSettlement.Village.TradeBound != null)
				{
					base.IssueOwner.CurrentSettlement.Village.Bound.Town.Security += 5f;
					base.IssueOwner.CurrentSettlement.Village.Bound.Town.Prosperity += 5f;
				}
				else if (base.IssueOwner.CurrentSettlement.IsTown)
				{
					base.IssueOwner.CurrentSettlement.Town.Security += 5f;
					base.IssueOwner.CurrentSettlement.Town.Prosperity += 5f;
				}
				Hero.MainHero.Clan.AddRenown(1f, true);
			}

			// Token: 0x06005D26 RID: 23846 RVA: 0x001AF100 File Offset: 0x001AD300
			protected override void AlternativeSolutionEndWithFailureConsequence()
			{
				if (base.IssueOwner.CurrentSettlement.IsVillage)
				{
					base.IssueOwner.CurrentSettlement.Village.Bound.Town.Prosperity -= 10f;
				}
				else if (base.IssueOwner.CurrentSettlement.IsTown)
				{
					base.IssueOwner.CurrentSettlement.Town.Prosperity -= 10f;
				}
				this.RelationshipChangeWithIssueOwner = -5;
			}

			// Token: 0x06005D27 RID: 23847 RVA: 0x001AF187 File Offset: 0x001AD387
			public override IssueBase.IssueFrequency GetFrequency()
			{
				return IssueBase.IssueFrequency.VeryCommon;
			}

			// Token: 0x06005D28 RID: 23848 RVA: 0x001AF18C File Offset: 0x001AD38C
			protected override bool CanPlayerTakeQuestConditions(Hero issueGiver, out IssueBase.PreconditionFlags flag, out Hero relationHero, out SkillObject skill)
			{
				flag = IssueBase.PreconditionFlags.None;
				relationHero = null;
				skill = null;
				if (issueGiver.GetRelationWithPlayer() < -10f)
				{
					flag |= IssueBase.PreconditionFlags.Relation;
					relationHero = issueGiver;
				}
				if (MobileParty.MainParty.MemberRoster.TotalHealthyCount < 5)
				{
					flag |= IssueBase.PreconditionFlags.NotEnoughTroops;
				}
				if (issueGiver.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction))
				{
					flag |= IssueBase.PreconditionFlags.AtWar;
				}
				return flag == IssueBase.PreconditionFlags.None;
			}

			// Token: 0x06005D29 RID: 23849 RVA: 0x001AF1F8 File Offset: 0x001AD3F8
			public override bool IssueStayAliveConditions()
			{
				return !base.IssueOwner.CurrentSettlement.IsRaided && !base.IssueOwner.CurrentSettlement.IsUnderRaid && this.RelatedHideout != null && this.RelatedHideout.IsInfested;
			}

			// Token: 0x06005D2A RID: 23850 RVA: 0x001AF233 File Offset: 0x001AD433
			protected override void OnGameLoad()
			{
				if (MBSaveLoad.IsUpdatingGameVersion && MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("v1.2.9", 0) && this.RelatedHideout == null)
				{
					base.CompleteIssueWithCancel(null);
				}
			}

			// Token: 0x06005D2B RID: 23851 RVA: 0x001AF262 File Offset: 0x001AD462
			public override void IsSettlementBusy(Settlement settlement, object asker, ref int priority)
			{
				if (asker != this && settlement == this.RelatedHideout.Settlement)
				{
					priority = Math.Max(priority, 100);
				}
			}

			// Token: 0x06005D2C RID: 23852 RVA: 0x001AF281 File Offset: 0x001AD481
			protected override void HourlyTick()
			{
			}

			// Token: 0x06005D2D RID: 23853 RVA: 0x001AF283 File Offset: 0x001AD483
			protected override QuestBase GenerateIssueQuest(string questId)
			{
				return new MerchantNeedsHelpWithOutlawsIssueQuestBehavior.MerchantNeedsHelpWithOutlawsIssueQuest(questId, base.IssueOwner, CampaignTime.DaysFromNow(20f), this.RewardGold, this.TotalPartyCount, this.RelatedHideout);
			}

			// Token: 0x06005D2E RID: 23854 RVA: 0x001AF2AD File Offset: 0x001AD4AD
			protected override void CompleteIssueWithTimedOutConsequences()
			{
			}

			// Token: 0x06005D2F RID: 23855 RVA: 0x001AF2AF File Offset: 0x001AD4AF
			protected override void OnIssueFinalized()
			{
			}

			// Token: 0x04001D56 RID: 7510
			private const int IssueDuration = 15;

			// Token: 0x04001D57 RID: 7511
			private const int QuestTimeLimit = 20;

			// Token: 0x04001D58 RID: 7512
			private const int MinimumRequiredMenCount = 5;

			// Token: 0x04001D59 RID: 7513
			private const int AlternativeSolutionMinimumSkillValue = 120;

			// Token: 0x04001D5A RID: 7514
			private const int AlternativeSolutionTroopTierRequirement = 2;

			// Token: 0x04001D5B RID: 7515
			[SaveableField(10)]
			private Hideout RelatedHideout;
		}

		// Token: 0x0200073A RID: 1850
		public class MerchantNeedsHelpWithOutlawsIssueQuest : QuestBase
		{
			// Token: 0x06005D30 RID: 23856 RVA: 0x001AF2B1 File Offset: 0x001AD4B1
			internal static void AutoGeneratedStaticCollectObjectsMerchantNeedsHelpWithOutlawsIssueQuest(object o, List<object> collectedObjects)
			{
				((MerchantNeedsHelpWithOutlawsIssueQuestBehavior.MerchantNeedsHelpWithOutlawsIssueQuest)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
			}

			// Token: 0x06005D31 RID: 23857 RVA: 0x001AF2BF File Offset: 0x001AD4BF
			protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
			{
				base.AutoGeneratedInstanceCollectObjects(collectedObjects);
				collectedObjects.Add(this._validPartiesList);
				collectedObjects.Add(this._relatedHideout);
				collectedObjects.Add(this._questProgressLogTest);
			}

			// Token: 0x06005D32 RID: 23858 RVA: 0x001AF2EC File Offset: 0x001AD4EC
			internal static object AutoGeneratedGetMemberValue_totalPartyCount(object o)
			{
				return ((MerchantNeedsHelpWithOutlawsIssueQuestBehavior.MerchantNeedsHelpWithOutlawsIssueQuest)o)._totalPartyCount;
			}

			// Token: 0x06005D33 RID: 23859 RVA: 0x001AF2FE File Offset: 0x001AD4FE
			internal static object AutoGeneratedGetMemberValue_destroyedPartyCount(object o)
			{
				return ((MerchantNeedsHelpWithOutlawsIssueQuestBehavior.MerchantNeedsHelpWithOutlawsIssueQuest)o)._destroyedPartyCount;
			}

			// Token: 0x06005D34 RID: 23860 RVA: 0x001AF310 File Offset: 0x001AD510
			internal static object AutoGeneratedGetMemberValue_recruitedPartyCount(object o)
			{
				return ((MerchantNeedsHelpWithOutlawsIssueQuestBehavior.MerchantNeedsHelpWithOutlawsIssueQuest)o)._recruitedPartyCount;
			}

			// Token: 0x06005D35 RID: 23861 RVA: 0x001AF322 File Offset: 0x001AD522
			internal static object AutoGeneratedGetMemberValue_validPartiesList(object o)
			{
				return ((MerchantNeedsHelpWithOutlawsIssueQuestBehavior.MerchantNeedsHelpWithOutlawsIssueQuest)o)._validPartiesList;
			}

			// Token: 0x06005D36 RID: 23862 RVA: 0x001AF32F File Offset: 0x001AD52F
			internal static object AutoGeneratedGetMemberValue_relatedHideout(object o)
			{
				return ((MerchantNeedsHelpWithOutlawsIssueQuestBehavior.MerchantNeedsHelpWithOutlawsIssueQuest)o)._relatedHideout;
			}

			// Token: 0x06005D37 RID: 23863 RVA: 0x001AF33C File Offset: 0x001AD53C
			internal static object AutoGeneratedGetMemberValue_questProgressLogTest(object o)
			{
				return ((MerchantNeedsHelpWithOutlawsIssueQuestBehavior.MerchantNeedsHelpWithOutlawsIssueQuest)o)._questProgressLogTest;
			}

			// Token: 0x170012B4 RID: 4788
			// (get) Token: 0x06005D38 RID: 23864 RVA: 0x001AF34C File Offset: 0x001AD54C
			public override TextObject Title
			{
				get
				{
					TextObject textObject = new TextObject("{=PBGiIbEM}{ISSUE_GIVER.NAME} Needs Help With Brigands", null);
					StringHelpers.SetCharacterProperties("ISSUE_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					return textObject;
				}
			}

			// Token: 0x170012B5 RID: 4789
			// (get) Token: 0x06005D39 RID: 23865 RVA: 0x001AF37E File Offset: 0x001AD57E
			public override bool IsRemainingTimeHidden
			{
				get
				{
					return false;
				}
			}

			// Token: 0x170012B6 RID: 4790
			// (get) Token: 0x06005D3A RID: 23866 RVA: 0x001AF381 File Offset: 0x001AD581
			private int _questPartyProgress
			{
				get
				{
					return this._destroyedPartyCount + this._recruitedPartyCount;
				}
			}

			// Token: 0x170012B7 RID: 4791
			// (get) Token: 0x06005D3B RID: 23867 RVA: 0x001AF390 File Offset: 0x001AD590
			private TextObject PlayerStartsQuestLogText
			{
				get
				{
					TextObject textObject = new TextObject("{=6iLxrDBa}You have accepted {QUEST_GIVER.LINK}'s request to find at least {TOTAL_COUNT} different parties of brigands around {QUEST_SETTLEMENT} and decided to hunt them down personally. {?QUEST_GIVER.GENDER}She{?}He{\\?} will reward you {AMOUNT}{GOLD_ICON} gold once you have dealt with them.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					textObject.SetTextVariable("TOTAL_COUNT", this._totalPartyCount);
					textObject.SetTextVariable("QUEST_SETTLEMENT", base.QuestGiver.CurrentSettlement.EncyclopediaLinkWithName);
					textObject.SetTextVariable("AMOUNT", this.RewardGold);
					textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
					return textObject;
				}
			}

			// Token: 0x170012B8 RID: 4792
			// (get) Token: 0x06005D3C RID: 23868 RVA: 0x001AF414 File Offset: 0x001AD614
			private TextObject SuccessQuestLogText1
			{
				get
				{
					TextObject textObject = new TextObject("{=cQ6CzXKM}You have defeated all the brigands as {QUEST_GIVER.LINK} has asked. {?QUEST_GIVER.GENDER}She{?}He{\\?} is grateful. And sends you the reward, {GOLD_AMOUNT}{GOLD_ICON} gold as promised.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					textObject.SetTextVariable("SETTLEMENT", base.QuestGiver.CurrentSettlement.Name);
					textObject.SetTextVariable("GOLD_AMOUNT", this.RewardGold);
					textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
					return textObject;
				}
			}

			// Token: 0x170012B9 RID: 4793
			// (get) Token: 0x06005D3D RID: 23869 RVA: 0x001AF488 File Offset: 0x001AD688
			private TextObject SuccessQuestLogText2
			{
				get
				{
					TextObject textObject = new TextObject("{=dSHgU9gD}You have defeated some of the brigands and recruited the rest into your party. {QUEST_GIVER.LINK} is grateful and sends you the {GOLD_AMOUNT}{GOLD_ICON} as promised. ", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					textObject.SetTextVariable("GOLD_AMOUNT", this.RewardGold);
					textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
					return textObject;
				}
			}

			// Token: 0x170012BA RID: 4794
			// (get) Token: 0x06005D3E RID: 23870 RVA: 0x001AF4E0 File Offset: 0x001AD6E0
			private TextObject SuccessQuestLogText3
			{
				get
				{
					TextObject textObject = new TextObject("{=3V5udYJO}You have recruited the brigands into your party. {QUEST_GIVER.LINK} finds your solution acceptable and sends you the {GOLD_AMOUNT}{GOLD_ICON} as promised.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					textObject.SetTextVariable("GOLD_AMOUNT", this.RewardGold);
					textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
					return textObject;
				}
			}

			// Token: 0x170012BB RID: 4795
			// (get) Token: 0x06005D3F RID: 23871 RVA: 0x001AF538 File Offset: 0x001AD738
			private TextObject TimeoutLog
			{
				get
				{
					TextObject textObject = new TextObject("{=Tcux6Sru}You have failed to defeat all {TOTAL_COUNT} outlaw parties in time as {QUEST_GIVER.LINK} asked. {?QUEST_GIVER.GENDER}She{?}He{\\?} is very disappointed.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					textObject.SetTextVariable("TOTAL_COUNT", this._totalPartyCount);
					return textObject;
				}
			}

			// Token: 0x170012BC RID: 4796
			// (get) Token: 0x06005D40 RID: 23872 RVA: 0x001AF57C File Offset: 0x001AD77C
			private TextObject QuestGiverVillageRaided
			{
				get
				{
					TextObject textObject = new TextObject("{=4rCIZ6e5}{QUEST_SETTLEMENT} was raided, Your agreement with {QUEST_GIVER.LINK} is canceled.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					textObject.SetTextVariable("QUEST_SETTLEMENT", base.QuestGiver.CurrentSettlement.EncyclopediaLinkWithName);
					return textObject;
				}
			}

			// Token: 0x170012BD RID: 4797
			// (get) Token: 0x06005D41 RID: 23873 RVA: 0x001AF5CC File Offset: 0x001AD7CC
			private TextObject QuestCanceledWarDeclaredLog
			{
				get
				{
					TextObject textObject = new TextObject("{=vW6kBki9}Your clan is now at war with {QUEST_GIVER.LINK}'s realm. Your agreement with {QUEST_GIVER.LINK} is canceled.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					return textObject;
				}
			}

			// Token: 0x170012BE RID: 4798
			// (get) Token: 0x06005D42 RID: 23874 RVA: 0x001AF600 File Offset: 0x001AD800
			private TextObject PlayerDeclaredWarQuestLogText
			{
				get
				{
					TextObject textObject = new TextObject("{=DDur6mHb}Your actions have started a war with {ISSUE_GIVER.LINK}'s faction. Your agreement with {ISSUE_GIVER.LINK} is failed.", null);
					StringHelpers.SetCharacterProperties("ISSUE_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					return textObject;
				}
			}

			// Token: 0x06005D43 RID: 23875 RVA: 0x001AF634 File Offset: 0x001AD834
			public MerchantNeedsHelpWithOutlawsIssueQuest(string questId, Hero giverHero, CampaignTime duration, int rewardGold, int totalPartyCount, Hideout relatedHideout)
				: base(questId, giverHero, duration, rewardGold)
			{
				this._totalPartyCount = totalPartyCount;
				this._destroyedPartyCount = 0;
				this._recruitedPartyCount = 0;
				this._validPartiesList = new List<MobileParty>();
				this._relatedHideout = relatedHideout;
				this.AddHideoutPartiesToValidPartiesList();
				this.SetDialogs();
				base.InitializeQuestOnCreation();
			}

			// Token: 0x06005D44 RID: 23876 RVA: 0x001AF688 File Offset: 0x001AD888
			protected override void SetDialogs()
			{
				TextObject textObject = new TextObject("{=PQIYPCDn}Very good. I will be waiting for the good news then. Once you return, I'm ready to offer a reward of {REWARD_GOLD}{GOLD_ICON} denars. Just make sure that you defeat at least {TROOP_COUNT} bands no more than a day's ride away from here.[ib:normal][if:convo_bemused]", null);
				textObject.SetTextVariable("REWARD_GOLD", this.RewardGold);
				textObject.SetTextVariable("TROOP_COUNT", this._totalPartyCount);
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
				this.OfferDialogFlow = DialogFlow.CreateDialogFlow("issue_classic_quest_start", 100).NpcLine(textObject, null, null, null, null).Condition(() => Hero.OneToOneConversationHero == base.QuestGiver)
					.Consequence(new ConversationSentence.OnConsequenceDelegate(this.QuestAcceptedConsequences))
					.CloseDialog();
				this.DiscussDialogFlow = DialogFlow.CreateDialogFlow("quest_discuss", 100).NpcLine(new TextObject("{=jjTcNhKE}Have you been able to find any bandits yet?[if:convo_undecided_open]", null), null, null, null, null).Condition(() => Hero.OneToOneConversationHero == base.QuestGiver)
					.BeginPlayerOptions(null, false)
					.PlayerOption(new TextObject("{=mU45Th70}We're off to hunt them now.", null), null, null, null)
					.NpcLine(new TextObject("{=u9vtceCV}You are a savior.[if:convo_astonished]", null), null, null, null, null)
					.CloseDialog()
					.Condition(() => Hero.OneToOneConversationHero == base.QuestGiver)
					.PlayerOption(new TextObject("{=QPv1b7f8}I haven't had the time yet.", null), null, null, null)
					.NpcLine(new TextObject("{=6ba4n9n6}We are waiting for your good news {?PLAYER.GENDER}my lady{?}sir{\\?}.[if:convo_focused_happy]", null), null, null, null, null)
					.CloseDialog()
					.Condition(() => Hero.OneToOneConversationHero == base.QuestGiver)
					.EndPlayerOptions()
					.CloseDialog();
			}

			// Token: 0x06005D45 RID: 23877 RVA: 0x001AF7DB File Offset: 0x001AD9DB
			private void QuestAcceptedConsequences()
			{
				base.StartQuest();
				this._questProgressLogTest = base.AddDiscreteLog(this.PlayerStartsQuestLogText, new TextObject("{=HzcLsnYn}Destroyed parties", null), this._destroyedPartyCount, this._totalPartyCount, null, true);
			}

			// Token: 0x06005D46 RID: 23878 RVA: 0x001AF810 File Offset: 0x001ADA10
			private void AddQuestStepLog()
			{
				this._questProgressLogTest.UpdateCurrentProgress(this._questPartyProgress);
				if (this._questPartyProgress >= this._totalPartyCount)
				{
					this.SuccessConsequences();
					return;
				}
				TextObject textObject = new TextObject("{=xbVCRbUu}You hunted {CURRENT_COUNT}/{TOTAL_COUNT} gang of brigands.", null);
				textObject.SetTextVariable("CURRENT_COUNT", this._questPartyProgress);
				textObject.SetTextVariable("TOTAL_COUNT", this._totalPartyCount);
				MBInformationManager.AddQuickInformation(textObject, 0, null, null, "");
			}

			// Token: 0x06005D47 RID: 23879 RVA: 0x001AF87F File Offset: 0x001ADA7F
			protected override void HourlyTick()
			{
			}

			// Token: 0x06005D48 RID: 23880 RVA: 0x001AF884 File Offset: 0x001ADA84
			protected override void RegisterEvents()
			{
				CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, new Action<MobileParty, PartyBase>(this.MobilePartyDestroyed));
				CampaignEvents.WarDeclared.AddNonSerializedListener(this, new Action<IFaction, IFaction, DeclareWarAction.DeclareWarDetail>(this.OnWarDeclared));
				CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, new Action<Clan, Kingdom, Kingdom, ChangeKingdomAction.ChangeKingdomActionDetail, bool>(this.OnClanChangedKingdom));
				CampaignEvents.VillageBeingRaided.AddNonSerializedListener(this, new Action<Village>(this.OnVillageRaided));
				CampaignEvents.MapEventStarted.AddNonSerializedListener(this, new Action<MapEvent, PartyBase, PartyBase>(this.OnMapEventStarted));
				CampaignEvents.BanditPartyRecruited.AddNonSerializedListener(this, new Action<MobileParty>(this.OnBanditPartyRecruited));
				CampaignEvents.SettlementEntered.AddNonSerializedListener(this, new Action<MobileParty, Settlement, Hero>(this.OnSettlementEntered));
				CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, new Action<MobileParty, Settlement>(this.OnSettlementLeft));
				CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, new Action(this.OnGameLoadFinished));
				CampaignEvents.IsSettlementBusyEvent.AddNonSerializedListener(this, new ReferenceAction<Settlement, object, int>(this.IsSettlementBusy));
			}

			// Token: 0x06005D49 RID: 23881 RVA: 0x001AF977 File Offset: 0x001ADB77
			private void IsSettlementBusy(Settlement settlement, object asker, ref int priority)
			{
				if (asker != this && settlement == this._relatedHideout.Settlement)
				{
					priority = Math.Max(priority, 200);
				}
			}

			// Token: 0x06005D4A RID: 23882 RVA: 0x001AF99C File Offset: 0x001ADB9C
			private void OnGameLoadFinished()
			{
				if (this._relatedHideout == null && MBSaveLoad.IsUpdatingGameVersion && MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("v1.2.9", 0))
				{
					Hideout hideout = SettlementHelper.FindNearestHideoutToMobileParty(MobileParty.MainParty, MobileParty.NavigationType.Default, (Settlement x) => x.Hideout.IsInfested);
					if (hideout != null && Campaign.Current.Models.MapDistanceModel.GetDistance(base.QuestGiver.CurrentSettlement, hideout.Settlement, false, false, MobileParty.NavigationType.Default) < Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(MobileParty.NavigationType.Default) * 1.25f)
					{
						this._relatedHideout = hideout;
					}
					if (this._relatedHideout != null)
					{
						this.AddHideoutPartiesToValidPartiesList();
					}
					else
					{
						base.CompleteQuestWithCancel(null);
					}
				}
				if (this._relatedHideout != null && base.IsOngoing && MBSaveLoad.IsUpdatingGameVersion && MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("v1.2.9", 0) && this._relatedHideout.Settlement.IsSettlementBusy(this))
				{
					base.CompleteQuestWithCancel(null);
				}
			}

			// Token: 0x06005D4B RID: 23883 RVA: 0x001AFAA8 File Offset: 0x001ADCA8
			private void AddHideoutPartiesToValidPartiesList()
			{
				foreach (MobileParty mobileParty in this._relatedHideout.Settlement.Parties)
				{
					if (mobileParty.IsBandit)
					{
						this._validPartiesList.Add(mobileParty);
					}
				}
			}

			// Token: 0x06005D4C RID: 23884 RVA: 0x001AFB14 File Offset: 0x001ADD14
			private void OnSettlementLeft(MobileParty party, Settlement settlement)
			{
				if (this._validPartiesList.Contains(party) && settlement.IsHideout && settlement.Hideout == this._relatedHideout)
				{
					this._validPartiesList.Remove(party);
				}
			}

			// Token: 0x06005D4D RID: 23885 RVA: 0x001AFB47 File Offset: 0x001ADD47
			private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
			{
				if (party != null && party.IsBandit && settlement.IsHideout && settlement.Hideout == this._relatedHideout)
				{
					this._validPartiesList.Add(party);
				}
			}

			// Token: 0x06005D4E RID: 23886 RVA: 0x001AFB76 File Offset: 0x001ADD76
			private void OnBanditPartyRecruited(MobileParty banditParty)
			{
				if (this._validPartiesList.Contains(banditParty))
				{
					this._recruitedPartyCount++;
					this._validPartiesList.Remove(banditParty);
					this.AddQuestStepLog();
				}
			}

			// Token: 0x06005D4F RID: 23887 RVA: 0x001AFBA7 File Offset: 0x001ADDA7
			private void OnMapEventStarted(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
			{
				if (QuestHelper.CheckMinorMajorCoercion(this, mapEvent, attackerParty))
				{
					QuestHelper.ApplyGenericMinorMajorCoercionConsequences(this, mapEvent);
				}
			}

			// Token: 0x06005D50 RID: 23888 RVA: 0x001AFBBA File Offset: 0x001ADDBA
			private void OnVillageRaided(Village village)
			{
				if (village == base.QuestGiver.CurrentSettlement.Village)
				{
					base.AddLog(this.QuestGiverVillageRaided, false);
					base.CompleteQuestWithCancel(null);
				}
			}

			// Token: 0x06005D51 RID: 23889 RVA: 0x001AFBE4 File Offset: 0x001ADDE4
			private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
			{
				if (base.QuestGiver.CurrentSettlement.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction))
				{
					base.CompleteQuestWithCancel(this.QuestCanceledWarDeclaredLog);
				}
			}

			// Token: 0x06005D52 RID: 23890 RVA: 0x001AFC13 File Offset: 0x001ADE13
			private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
			{
				QuestHelper.CheckWarDeclarationAndFailOrCancelTheQuest(this, faction1, faction2, detail, this.PlayerDeclaredWarQuestLogText, this.QuestCanceledWarDeclaredLog, false);
			}

			// Token: 0x06005D53 RID: 23891 RVA: 0x001AFC2B File Offset: 0x001ADE2B
			private void MobilePartyDestroyed(MobileParty mobileParty, PartyBase destroyerParty)
			{
				if (destroyerParty == PartyBase.MainParty && this._validPartiesList.Contains(mobileParty))
				{
					this._destroyedPartyCount++;
					this.AddQuestStepLog();
				}
			}

			// Token: 0x06005D54 RID: 23892 RVA: 0x001AFC58 File Offset: 0x001ADE58
			protected override void HourlyTickParty(MobileParty mobileParty)
			{
				if (base.IsOngoing && mobileParty.IsBandit && mobileParty.MapEvent == null && mobileParty.MapFaction.IsBanditFaction && !mobileParty.IsCurrentlyUsedByAQuest && !mobileParty.IsCurrentlyAtSea)
				{
					float num;
					if (((mobileParty.CurrentSettlement != null) ? Campaign.Current.Models.MapDistanceModel.GetDistance(mobileParty.CurrentSettlement, base.QuestGiver.CurrentSettlement, false, false, MobileParty.NavigationType.Default, out num) : Campaign.Current.Models.MapDistanceModel.GetDistance(mobileParty, base.QuestGiver.CurrentSettlement, false, MobileParty.NavigationType.Default, out num)) <= MerchantNeedsHelpWithOutlawsIssueQuestBehavior.ValidBanditPartyDistance)
					{
						if (!this._validPartiesList.Contains(mobileParty))
						{
							if (!base.IsTracked(mobileParty))
							{
								base.AddTrackedObject(mobileParty);
							}
							this._validPartiesList.Add(mobileParty);
							if (mobileParty.CurrentSettlement == null && MBRandom.RandomFloat < 1f / (float)this._validPartiesList.Count)
							{
								SetPartyAiAction.GetActionForPatrollingAroundSettlement(mobileParty, base.QuestGiver.CurrentSettlement, MobileParty.NavigationType.Default, false, false);
								mobileParty.Ai.SetDoNotMakeNewDecisions(true);
								mobileParty.IgnoreForHours(500f);
								return;
							}
						}
						else if (MBRandom.RandomFloat < 0.05f)
						{
							mobileParty.Ai.SetDoNotMakeNewDecisions(false);
							return;
						}
					}
					else if (base.IsTracked(mobileParty))
					{
						base.RemoveTrackedObject(mobileParty);
						this._validPartiesList.Remove(mobileParty);
						mobileParty.Ai.SetDoNotMakeNewDecisions(false);
					}
				}
			}

			// Token: 0x06005D55 RID: 23893 RVA: 0x001AFDCC File Offset: 0x001ADFCC
			private void SuccessConsequences()
			{
				if (this._destroyedPartyCount == this._totalPartyCount)
				{
					base.AddLog(this.SuccessQuestLogText1, false);
				}
				else if (this._recruitedPartyCount != 0 && this._recruitedPartyCount < this._totalPartyCount)
				{
					base.AddLog(this.SuccessQuestLogText2, false);
				}
				else
				{
					base.AddLog(this.SuccessQuestLogText3, false);
				}
				this.RelationshipChangeWithQuestGiver = 3;
				GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, this.RewardGold, false);
				if (base.QuestGiver.CurrentSettlement.IsVillage && base.QuestGiver.CurrentSettlement.Village.TradeBound != null)
				{
					base.QuestGiver.CurrentSettlement.Village.Bound.Town.Security += 5f;
					base.QuestGiver.CurrentSettlement.Village.Bound.Town.Prosperity += 5f;
				}
				else if (base.QuestGiver.CurrentSettlement.IsTown)
				{
					base.QuestGiver.CurrentSettlement.Town.Security += 5f;
					base.QuestGiver.CurrentSettlement.Town.Prosperity += 5f;
				}
				Hero.MainHero.Clan.AddRenown(1f, true);
				base.CompleteQuestWithSuccess();
			}

			// Token: 0x06005D56 RID: 23894 RVA: 0x001AFF34 File Offset: 0x001AE134
			protected override void OnTimedOut()
			{
				this.RelationshipChangeWithQuestGiver = -5;
				if (base.QuestGiver.CurrentSettlement.IsVillage)
				{
					base.QuestGiver.CurrentSettlement.Village.Bound.Town.Prosperity -= 10f;
				}
				else if (base.QuestGiver.CurrentSettlement.IsTown)
				{
					base.QuestGiver.CurrentSettlement.Town.Prosperity -= 10f;
				}
				base.AddLog(this.TimeoutLog, false);
			}

			// Token: 0x06005D57 RID: 23895 RVA: 0x001AFFC9 File Offset: 0x001AE1C9
			protected override void InitializeQuestOnGameLoad()
			{
				this.SetDialogs();
			}

			// Token: 0x06005D58 RID: 23896 RVA: 0x001AFFD4 File Offset: 0x001AE1D4
			protected override void OnFinalize()
			{
				foreach (MobileParty mobileParty in this._validPartiesList)
				{
					mobileParty.Ai.SetDoNotMakeNewDecisions(false);
					mobileParty.IgnoreForHours(0f);
					if (base.IsTracked(mobileParty))
					{
						base.RemoveTrackedObject(mobileParty);
					}
				}
				this._validPartiesList.Clear();
			}

			// Token: 0x04001D5C RID: 7516
			[SaveableField(10)]
			private readonly int _totalPartyCount;

			// Token: 0x04001D5D RID: 7517
			[SaveableField(30)]
			private int _destroyedPartyCount;

			// Token: 0x04001D5E RID: 7518
			[SaveableField(50)]
			private int _recruitedPartyCount;

			// Token: 0x04001D5F RID: 7519
			[SaveableField(40)]
			private List<MobileParty> _validPartiesList;

			// Token: 0x04001D60 RID: 7520
			[SaveableField(70)]
			private Hideout _relatedHideout;

			// Token: 0x04001D61 RID: 7521
			private const float ValidBanditPartyEnableAiChance = 0.05f;

			// Token: 0x04001D62 RID: 7522
			[SaveableField(60)]
			private JournalLog _questProgressLogTest;
		}

		// Token: 0x0200073B RID: 1851
		public class MerchantNeedsHelpWithOutlawsIssueTypeDefiner : SaveableTypeDefiner
		{
			// Token: 0x06005D5D RID: 23901 RVA: 0x001B0090 File Offset: 0x001AE290
			public MerchantNeedsHelpWithOutlawsIssueTypeDefiner()
				: base(590000)
			{
			}

			// Token: 0x06005D5E RID: 23902 RVA: 0x001B009D File Offset: 0x001AE29D
			protected override void DefineClassTypes()
			{
				base.AddClassDefinition(typeof(MerchantNeedsHelpWithOutlawsIssueQuestBehavior.MerchantNeedsHelpWithOutlawsIssue), 1, null);
				base.AddClassDefinition(typeof(MerchantNeedsHelpWithOutlawsIssueQuestBehavior.MerchantNeedsHelpWithOutlawsIssueQuest), 2, null);
			}
		}
	}
}
