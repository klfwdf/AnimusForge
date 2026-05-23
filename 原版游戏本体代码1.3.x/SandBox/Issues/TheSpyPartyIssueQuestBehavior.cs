using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using SandBox.Conversation.MissionLogics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace SandBox.Issues
{
	// Token: 0x020000B6 RID: 182
	public class TheSpyPartyIssueQuestBehavior : CampaignBehaviorBase
	{
		// Token: 0x06000790 RID: 1936 RVA: 0x00033837 File Offset: 0x00031A37
		public override void RegisterEvents()
		{
			CampaignEvents.OnCheckForIssueEvent.AddNonSerializedListener(this, new Action<Hero>(this.OnCheckForIssue));
		}

		// Token: 0x06000791 RID: 1937 RVA: 0x00033850 File Offset: 0x00031A50
		public void OnCheckForIssue(Hero hero)
		{
			Settlement relatedObject;
			if (this.ConditionsHold(hero, out relatedObject))
			{
				Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(new PotentialIssueData.StartIssueDelegate(this.OnStartIssue), typeof(TheSpyPartyIssueQuestBehavior.TheSpyPartyIssue), IssueBase.IssueFrequency.Rare, relatedObject));
				return;
			}
			Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(typeof(TheSpyPartyIssueQuestBehavior.TheSpyPartyIssue), IssueBase.IssueFrequency.Rare));
		}

		// Token: 0x06000792 RID: 1938 RVA: 0x000338B8 File Offset: 0x00031AB8
		private bool ConditionsHold(Hero issueGiver, out Settlement selectedSettlement)
		{
			selectedSettlement = null;
			if (issueGiver.IsLord && issueGiver.Clan != Clan.PlayerClan)
			{
				if (issueGiver.Clan.Settlements.Any((Settlement x) => x.IsTown))
				{
					selectedSettlement = issueGiver.Clan.Settlements.GetRandomElementWithPredicate((Settlement x) => x.IsTown);
					string difficultySuffix = TheSpyPartyIssueQuestBehavior.GetDifficultySuffix(Campaign.Current.Models.IssueModel.GetIssueDifficultyMultiplier());
					bool flag = MBObjectManager.Instance.GetObject<CharacterObject>("bold_contender_" + difficultySuffix) != null && MBObjectManager.Instance.GetObject<CharacterObject>("confident_contender_" + difficultySuffix) != null && MBObjectManager.Instance.GetObject<CharacterObject>("dignified_contender_" + difficultySuffix) != null && MBObjectManager.Instance.GetObject<CharacterObject>("hardy_contender_" + difficultySuffix) != null;
					if (!flag)
					{
						CampaignEventDispatcher.Instance.RemoveListeners(this);
					}
					return flag;
				}
			}
			return false;
		}

		// Token: 0x06000793 RID: 1939 RVA: 0x000339D2 File Offset: 0x00031BD2
		private static string GetDifficultySuffix(float difficulty)
		{
			if (difficulty <= 0.25f)
			{
				return "easy";
			}
			if (difficulty <= 0.5f)
			{
				return "normal";
			}
			if (difficulty <= 0.75f)
			{
				return "hard";
			}
			return "very_hard";
		}

		// Token: 0x06000794 RID: 1940 RVA: 0x00033A04 File Offset: 0x00031C04
		private IssueBase OnStartIssue(in PotentialIssueData pid, Hero issueOwner)
		{
			PotentialIssueData potentialIssueData = pid;
			return new TheSpyPartyIssueQuestBehavior.TheSpyPartyIssue(issueOwner, potentialIssueData.RelatedObject as Settlement);
		}

		// Token: 0x06000795 RID: 1941 RVA: 0x00033A2A File Offset: 0x00031C2A
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x04000405 RID: 1029
		private const IssueBase.IssueFrequency TheSpyPartyIssueFrequency = IssueBase.IssueFrequency.Rare;

		// Token: 0x04000406 RID: 1030
		private const int IssueDuration = 5;

		// Token: 0x020001D0 RID: 464
		public class TheSpyPartyIssueQuestTypeDefiner : SaveableTypeDefiner
		{
			// Token: 0x06001288 RID: 4744 RVA: 0x00075087 File Offset: 0x00073287
			public TheSpyPartyIssueQuestTypeDefiner()
				: base(121250)
			{
			}

			// Token: 0x06001289 RID: 4745 RVA: 0x00075094 File Offset: 0x00073294
			protected override void DefineClassTypes()
			{
				base.AddClassDefinition(typeof(TheSpyPartyIssueQuestBehavior.TheSpyPartyIssue), 1, null);
				base.AddClassDefinition(typeof(TheSpyPartyIssueQuestBehavior.TheSpyPartyIssueQuest), 2, null);
			}

			// Token: 0x0600128A RID: 4746 RVA: 0x000750BA File Offset: 0x000732BA
			protected override void DefineStructTypes()
			{
				base.AddStructDefinition(typeof(TheSpyPartyIssueQuestBehavior.SuspectNpc), 3, null);
			}
		}

		// Token: 0x020001D1 RID: 465
		public struct SuspectNpc
		{
			// Token: 0x0600128B RID: 4747 RVA: 0x000750CE File Offset: 0x000732CE
			public SuspectNpc(CharacterObject characterObject, bool hasHair, bool hasBigSword, bool withoutMarkings, bool hasBeard)
			{
				this.CharacterObject = characterObject;
				this.HasHair = hasHair;
				this.HasBigSword = hasBigSword;
				this.WithoutMarkings = withoutMarkings;
				this.HasBeard = hasBeard;
			}

			// Token: 0x0600128C RID: 4748 RVA: 0x000750F8 File Offset: 0x000732F8
			public static void AutoGeneratedStaticCollectObjectsSuspectNpc(object o, List<object> collectedObjects)
			{
				((TheSpyPartyIssueQuestBehavior.SuspectNpc)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
			}

			// Token: 0x0600128D RID: 4749 RVA: 0x00075114 File Offset: 0x00073314
			private void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
			{
				collectedObjects.Add(this.CharacterObject);
			}

			// Token: 0x0600128E RID: 4750 RVA: 0x00075122 File Offset: 0x00073322
			internal static object AutoGeneratedGetMemberValueCharacterObject(object o)
			{
				return ((TheSpyPartyIssueQuestBehavior.SuspectNpc)o).CharacterObject;
			}

			// Token: 0x0600128F RID: 4751 RVA: 0x0007512F File Offset: 0x0007332F
			internal static object AutoGeneratedGetMemberValueHasHair(object o)
			{
				return ((TheSpyPartyIssueQuestBehavior.SuspectNpc)o).HasHair;
			}

			// Token: 0x06001290 RID: 4752 RVA: 0x00075141 File Offset: 0x00073341
			internal static object AutoGeneratedGetMemberValueHasBigSword(object o)
			{
				return ((TheSpyPartyIssueQuestBehavior.SuspectNpc)o).HasBigSword;
			}

			// Token: 0x06001291 RID: 4753 RVA: 0x00075153 File Offset: 0x00073353
			internal static object AutoGeneratedGetMemberValueWithoutMarkings(object o)
			{
				return ((TheSpyPartyIssueQuestBehavior.SuspectNpc)o).WithoutMarkings;
			}

			// Token: 0x06001292 RID: 4754 RVA: 0x00075165 File Offset: 0x00073365
			internal static object AutoGeneratedGetMemberValueHasBeard(object o)
			{
				return ((TheSpyPartyIssueQuestBehavior.SuspectNpc)o).HasBeard;
			}

			// Token: 0x040008A3 RID: 2211
			[SaveableField(10)]
			public readonly CharacterObject CharacterObject;

			// Token: 0x040008A4 RID: 2212
			[SaveableField(20)]
			public readonly bool HasHair;

			// Token: 0x040008A5 RID: 2213
			[SaveableField(30)]
			public readonly bool HasBigSword;

			// Token: 0x040008A6 RID: 2214
			[SaveableField(40)]
			public readonly bool WithoutMarkings;

			// Token: 0x040008A7 RID: 2215
			[SaveableField(50)]
			public readonly bool HasBeard;
		}

		// Token: 0x020001D2 RID: 466
		public class TheSpyPartyIssue : IssueBase
		{
			// Token: 0x1700020C RID: 524
			// (get) Token: 0x06001293 RID: 4755 RVA: 0x00075177 File Offset: 0x00073377
			public override IssueBase.AlternativeSolutionScaleFlag AlternativeSolutionScaleFlags
			{
				get
				{
					return IssueBase.AlternativeSolutionScaleFlag.FailureRisk;
				}
			}

			// Token: 0x1700020D RID: 525
			// (get) Token: 0x06001294 RID: 4756 RVA: 0x0007517A File Offset: 0x0007337A
			protected override int CompanionSkillRewardXP
			{
				get
				{
					return (int)(600f + 800f * base.IssueDifficultyMultiplier);
				}
			}

			// Token: 0x1700020E RID: 526
			// (get) Token: 0x06001295 RID: 4757 RVA: 0x0007518F File Offset: 0x0007338F
			public override bool IsThereAlternativeSolution
			{
				get
				{
					return true;
				}
			}

			// Token: 0x1700020F RID: 527
			// (get) Token: 0x06001296 RID: 4758 RVA: 0x00075192 File Offset: 0x00073392
			public override bool IsThereLordSolution
			{
				get
				{
					return false;
				}
			}

			// Token: 0x17000210 RID: 528
			// (get) Token: 0x06001297 RID: 4759 RVA: 0x00075195 File Offset: 0x00073395
			public override int AlternativeSolutionBaseNeededMenCount
			{
				get
				{
					return 1 + MathF.Ceiling(4f * base.IssueDifficultyMultiplier);
				}
			}

			// Token: 0x17000211 RID: 529
			// (get) Token: 0x06001298 RID: 4760 RVA: 0x000751AA File Offset: 0x000733AA
			protected override int AlternativeSolutionBaseDurationInDaysInternal
			{
				get
				{
					return 3 + MathF.Ceiling(3f * base.IssueDifficultyMultiplier);
				}
			}

			// Token: 0x17000212 RID: 530
			// (get) Token: 0x06001299 RID: 4761 RVA: 0x000751BF File Offset: 0x000733BF
			protected override int RewardGold
			{
				get
				{
					return (int)(500f + 3000f * base.IssueDifficultyMultiplier);
				}
			}

			// Token: 0x17000213 RID: 531
			// (get) Token: 0x0600129A RID: 4762 RVA: 0x000751D4 File Offset: 0x000733D4
			public override TextObject IssueBriefByIssueGiver
			{
				get
				{
					TextObject textObject = new TextObject("{=tFPJySG7}I am hosting a tournament at {SELECTED_SETTLEMENT}. [ib:convo_undecided_open][if:convo_pondering]I am expecting contenders to partake from all over the realm. I have my reasons to believe one of the attending warriors is actually a spy, sent to gather information about its defenses.", null);
					textObject.SetTextVariable("SELECTED_SETTLEMENT", this._selectedSettlement.EncyclopediaLinkWithName);
					return textObject;
				}
			}

			// Token: 0x17000214 RID: 532
			// (get) Token: 0x0600129B RID: 4763 RVA: 0x000751F8 File Offset: 0x000733F8
			public override TextObject IssueAcceptByPlayer
			{
				get
				{
					return new TextObject("{=EYT7b2J5}Any traveler can be asked by an enemy to spy on the places he travels. How can I track this one down?", null);
				}
			}

			// Token: 0x17000215 RID: 533
			// (get) Token: 0x0600129C RID: 4764 RVA: 0x00075205 File Offset: 0x00073405
			public override TextObject IssueQuestSolutionExplanationByIssueGiver
			{
				get
				{
					TextObject textObject = new TextObject("{=2lgkL9db}Of course. I have employed spies myself. But if a tournament [if:convo_pondering][ib:confident3]participant is asking questions about the state of the garrison and the walls, things which would concern no honest traveler - well, between that and the private information I've received, I think we'd have our man. The spy must be hiding inside {SELECTED_SETTLEMENT}. Once you are there start questioning the townsfolk.", null);
					textObject.SetTextVariable("SELECTED_SETTLEMENT", this._selectedSettlement.EncyclopediaLinkWithName);
					return textObject;
				}
			}

			// Token: 0x17000216 RID: 534
			// (get) Token: 0x0600129D RID: 4765 RVA: 0x00075229 File Offset: 0x00073429
			public override TextObject IssuePlayerResponseAfterAlternativeExplanation
			{
				get
				{
					return new TextObject("{=2nFBTmao}Is there any other way to solve this other than asking around?", null);
				}
			}

			// Token: 0x17000217 RID: 535
			// (get) Token: 0x0600129E RID: 4766 RVA: 0x00075236 File Offset: 0x00073436
			public override TextObject IssueAlternativeSolutionExplanationByIssueGiver
			{
				get
				{
					TextObject textObject = new TextObject("{=avVno3H8}Well, you can assign a companion of yours with a knack for this kind of game[if:convo_thinking] and enough muscles to back him up. Judging from what I have heard, a group of {NEEDED_MEN_COUNT} should be enough.", null);
					textObject.SetTextVariable("NEEDED_MEN_COUNT", base.GetTotalAlternativeSolutionNeededMenCount());
					return textObject;
				}
			}

			// Token: 0x17000218 RID: 536
			// (get) Token: 0x0600129F RID: 4767 RVA: 0x00075255 File Offset: 0x00073455
			public override TextObject IssueQuestSolutionAcceptByPlayer
			{
				get
				{
					return new TextObject("{=99OsuHGa}I will find the one you are looking for.", null);
				}
			}

			// Token: 0x17000219 RID: 537
			// (get) Token: 0x060012A0 RID: 4768 RVA: 0x00075262 File Offset: 0x00073462
			public override TextObject Title
			{
				get
				{
					return new TextObject("{=SJHtVaNa}The Spy Among Us", null);
				}
			}

			// Token: 0x1700021A RID: 538
			// (get) Token: 0x060012A1 RID: 4769 RVA: 0x00075270 File Offset: 0x00073470
			public override TextObject Description
			{
				get
				{
					TextObject textObject = new TextObject("{=C6rbcpbi}{QUEST_GIVER.LINK} wants you to find a spy before he causes any harm...", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.IssueOwner.CharacterObject, textObject, false);
					return textObject;
				}
			}

			// Token: 0x1700021B RID: 539
			// (get) Token: 0x060012A2 RID: 4770 RVA: 0x000752A2 File Offset: 0x000734A2
			public override TextObject IssueAlternativeSolutionAcceptByPlayer
			{
				get
				{
					TextObject textObject = new TextObject("{=FEcAwSfk}I will assign a companion with {NEEDED_MEN_COUNT} good men for {ALTERNATIVE_SOLUTION_DURATION} days.", null);
					textObject.SetTextVariable("NEEDED_MEN_COUNT", base.GetTotalAlternativeSolutionNeededMenCount());
					textObject.SetTextVariable("ALTERNATIVE_SOLUTION_DURATION", base.GetTotalAlternativeSolutionDurationInDays());
					return textObject;
				}
			}

			// Token: 0x1700021C RID: 540
			// (get) Token: 0x060012A3 RID: 4771 RVA: 0x000752D3 File Offset: 0x000734D3
			public override TextObject IssueDiscussAlternativeSolution
			{
				get
				{
					return new TextObject("{=O0Cjam62}I hope your people are careful about how they proceed.[if:convo_focused_happy] It would be a pity if that spy got away.", null);
				}
			}

			// Token: 0x1700021D RID: 541
			// (get) Token: 0x060012A4 RID: 4772 RVA: 0x000752E0 File Offset: 0x000734E0
			public override TextObject IssueAlternativeSolutionResponseByIssueGiver
			{
				get
				{
					TextObject textObject = new TextObject("{=ciXBiMMa}Thank you {PLAYER.NAME}, I hope your people will be successful.[if:convo_focused_happy]", null);
					StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject, false);
					return textObject;
				}
			}

			// Token: 0x1700021E RID: 542
			// (get) Token: 0x060012A5 RID: 4773 RVA: 0x0007530C File Offset: 0x0007350C
			public override TextObject IssueAlternativeSolutionSuccessLog
			{
				get
				{
					TextObject textObject = new TextObject("{=YIxpNP4k}You received a message from {ISSUE_GIVER.LINK}. \"Thank you for killing the spy. Please accept these {REWARD_GOLD}{GOLD_ICON} denars with our gratitude.\"", null);
					StringHelpers.SetCharacterProperties("ISSUE_GIVER", base.IssueOwner.CharacterObject, textObject, false);
					textObject.SetTextVariable("REWARD_GOLD", this.RewardGold);
					textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
					return textObject;
				}
			}

			// Token: 0x1700021F RID: 543
			// (get) Token: 0x060012A6 RID: 4774 RVA: 0x00075364 File Offset: 0x00073564
			protected override TextObject AlternativeSolutionStartLog
			{
				get
				{
					TextObject textObject = new TextObject("{=s5qs0bPs}{ISSUE_GIVER.LINK}, the {?ISSUE_GIVER.GENDER}lady{?}lord{\\?} of {QUEST_SETTLEMENT}, has told you about a spy that hides as a tournament attendee. You are asked to expose the spy and take care of him. You asked {COMPANION.LINK} to take {NEEDED_MEN_COUNT} of your best men to go and take care of it. They should report back to you in {ALTERNATIVE_SOLUTION_DURATION} days.", null);
					StringHelpers.SetCharacterProperties("ISSUE_GIVER", base.IssueOwner.CharacterObject, textObject, false);
					StringHelpers.SetCharacterProperties("COMPANION", base.AlternativeSolutionHero.CharacterObject, textObject, false);
					textObject.SetTextVariable("QUEST_SETTLEMENT", this._selectedSettlement.EncyclopediaLinkWithName);
					textObject.SetTextVariable("ALTERNATIVE_SOLUTION_DURATION", base.GetTotalAlternativeSolutionDurationInDays());
					textObject.SetTextVariable("NEEDED_MEN_COUNT", this.AlternativeSolutionSentTroops.TotalManCount - 1);
					return textObject;
				}
			}

			// Token: 0x060012A7 RID: 4775 RVA: 0x000753F0 File Offset: 0x000735F0
			public TheSpyPartyIssue(Hero issueOwner, Settlement selectedSettlement)
				: base(issueOwner, CampaignTime.DaysFromNow(5f))
			{
				this._selectedSettlement = selectedSettlement;
			}

			// Token: 0x060012A8 RID: 4776 RVA: 0x0007540A File Offset: 0x0007360A
			protected override float GetIssueEffectAmountInternal(IssueEffect issueEffect)
			{
				if (issueEffect == DefaultIssueEffects.SettlementSecurity)
				{
					return -2f;
				}
				if (issueEffect == DefaultIssueEffects.SettlementLoyalty)
				{
					return -0.5f;
				}
				if (issueEffect == DefaultIssueEffects.ClanInfluence)
				{
					return -0.2f;
				}
				return 0f;
			}

			// Token: 0x060012A9 RID: 4777 RVA: 0x0007543B File Offset: 0x0007363B
			public override ValueTuple<SkillObject, int> GetAlternativeSolutionSkill(Hero hero)
			{
				return new ValueTuple<SkillObject, int>((hero.GetSkillValue(DefaultSkills.Charm) >= hero.GetSkillValue(DefaultSkills.Roguery)) ? DefaultSkills.Charm : DefaultSkills.Roguery, 150);
			}

			// Token: 0x060012AA RID: 4778 RVA: 0x0007546B File Offset: 0x0007366B
			public override bool AlternativeSolutionCondition(out TextObject explanation)
			{
				return QuestHelper.CheckRosterForAlternativeSolution(MobileParty.MainParty.MemberRoster, base.GetTotalAlternativeSolutionNeededMenCount(), out explanation, 2, false);
			}

			// Token: 0x060012AB RID: 4779 RVA: 0x00075485 File Offset: 0x00073685
			public override bool IsTroopTypeNeededByAlternativeSolution(CharacterObject character)
			{
				return character.Tier >= 2;
			}

			// Token: 0x060012AC RID: 4780 RVA: 0x00075493 File Offset: 0x00073693
			public override bool DoTroopsSatisfyAlternativeSolution(TroopRoster troopRoster, out TextObject explanation)
			{
				return QuestHelper.CheckRosterForAlternativeSolution(troopRoster, base.GetTotalAlternativeSolutionNeededMenCount(), out explanation, 2, false);
			}

			// Token: 0x060012AD RID: 4781 RVA: 0x000754A4 File Offset: 0x000736A4
			protected override void AlternativeSolutionEndWithSuccessConsequence()
			{
				GainRenownAction.Apply(Hero.MainHero, 1f, false);
				this.RelationshipChangeWithIssueOwner = 5;
				this._selectedSettlement.Town.Prosperity += 5f;
			}

			// Token: 0x060012AE RID: 4782 RVA: 0x000754DC File Offset: 0x000736DC
			protected override void AlternativeSolutionEndWithFailureConsequence()
			{
				this.RelationshipChangeWithIssueOwner = -5;
				base.IssueOwner.AddPower(-5f);
				this._selectedSettlement.Town.Security -= 10f;
				this._selectedSettlement.Town.Loyalty -= 10f;
			}

			// Token: 0x060012AF RID: 4783 RVA: 0x00075539 File Offset: 0x00073739
			protected override void OnGameLoad()
			{
			}

			// Token: 0x060012B0 RID: 4784 RVA: 0x0007553B File Offset: 0x0007373B
			protected override void HourlyTick()
			{
			}

			// Token: 0x060012B1 RID: 4785 RVA: 0x0007553D File Offset: 0x0007373D
			protected override QuestBase GenerateIssueQuest(string questId)
			{
				return new TheSpyPartyIssueQuestBehavior.TheSpyPartyIssueQuest(questId, base.IssueOwner, CampaignTime.DaysFromNow(16f), this.RewardGold, this._selectedSettlement, base.IssueDifficultyMultiplier);
			}

			// Token: 0x060012B2 RID: 4786 RVA: 0x00075567 File Offset: 0x00073767
			public override IssueBase.IssueFrequency GetFrequency()
			{
				return IssueBase.IssueFrequency.Rare;
			}

			// Token: 0x060012B3 RID: 4787 RVA: 0x0007556C File Offset: 0x0007376C
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
				if (issueGiver.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction))
				{
					flag |= IssueBase.PreconditionFlags.AtWar;
				}
				return flag == IssueBase.PreconditionFlags.None;
			}

			// Token: 0x060012B4 RID: 4788 RVA: 0x000755BC File Offset: 0x000737BC
			public override bool IssueStayAliveConditions()
			{
				return base.IssueOwner.IsAlive && this._selectedSettlement.OwnerClan == base.IssueOwner.Clan && base.IssueOwner.Clan != Clan.PlayerClan;
			}

			// Token: 0x060012B5 RID: 4789 RVA: 0x000755FA File Offset: 0x000737FA
			protected override void CompleteIssueWithTimedOutConsequences()
			{
			}

			// Token: 0x060012B6 RID: 4790 RVA: 0x000755FC File Offset: 0x000737FC
			internal static void AutoGeneratedStaticCollectObjectsTheSpyPartyIssue(object o, List<object> collectedObjects)
			{
				((TheSpyPartyIssueQuestBehavior.TheSpyPartyIssue)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
			}

			// Token: 0x060012B7 RID: 4791 RVA: 0x0007560A File Offset: 0x0007380A
			protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
			{
				base.AutoGeneratedInstanceCollectObjects(collectedObjects);
				collectedObjects.Add(this._selectedSettlement);
			}

			// Token: 0x060012B8 RID: 4792 RVA: 0x0007561F File Offset: 0x0007381F
			internal static object AutoGeneratedGetMemberValue_selectedSettlement(object o)
			{
				return ((TheSpyPartyIssueQuestBehavior.TheSpyPartyIssue)o)._selectedSettlement;
			}

			// Token: 0x040008A8 RID: 2216
			private const int QuestDurationInDays = 16;

			// Token: 0x040008A9 RID: 2217
			private const int RequiredSkillValue = 150;

			// Token: 0x040008AA RID: 2218
			private const int AlternativeSolutionTroopTierRequirement = 2;

			// Token: 0x040008AB RID: 2219
			[SaveableField(10)]
			private readonly Settlement _selectedSettlement;
		}

		// Token: 0x020001D3 RID: 467
		public class TheSpyPartyIssueQuest : QuestBase
		{
			// Token: 0x17000220 RID: 544
			// (get) Token: 0x060012B9 RID: 4793 RVA: 0x0007562C File Offset: 0x0007382C
			public override bool IsRemainingTimeHidden
			{
				get
				{
					return false;
				}
			}

			// Token: 0x17000221 RID: 545
			// (get) Token: 0x060012BA RID: 4794 RVA: 0x0007562F File Offset: 0x0007382F
			public override TextObject Title
			{
				get
				{
					return new TextObject("{=SJHtVaNa}The Spy Among Us", null);
				}
			}

			// Token: 0x17000222 RID: 546
			// (get) Token: 0x060012BB RID: 4795 RVA: 0x0007563C File Offset: 0x0007383C
			private TextObject QuestStartedLog
			{
				get
				{
					TextObject textObject = new TextObject("{=94WRYoQp}{?QUEST_GIVER.GENDER}Lady{?}Lord{\\?} {QUEST_GIVER.LINK} from {QUEST_SETTLEMENT}, has told you about rumors of a spy disguised amongst the tournament attendees. You agreed to take care of the situation by yourself. {QUEST_GIVER.LINK} believes that the spy is posing as a tournament attendee in the city of {QUEST_SETTLEMENT}.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					textObject.SetTextVariable("QUEST_SETTLEMENT", this._selectedSettlement.EncyclopediaLinkWithName);
					return textObject;
				}
			}

			// Token: 0x17000223 RID: 547
			// (get) Token: 0x060012BC RID: 4796 RVA: 0x00075688 File Offset: 0x00073888
			private TextObject QuestSuccessQuestLog
			{
				get
				{
					TextObject textObject = new TextObject("{=YIxpNP4k}You received a message from {QUEST_GIVER.LINK}. \"Thank you for killing the spy. Please accept these {REWARD_GOLD}{GOLD_ICON} denars with our gratitude.\"", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					textObject.SetTextVariable("REWARD_GOLD", this.RewardGold);
					textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
					return textObject;
				}
			}

			// Token: 0x17000224 RID: 548
			// (get) Token: 0x060012BD RID: 4797 RVA: 0x000756E0 File Offset: 0x000738E0
			private TextObject QuestFailedKilledAnotherQuestLog
			{
				get
				{
					TextObject textObject = new TextObject("{=tTKpOFRK}You won the duel but your opponent was innocent. {QUEST_GIVER.LINK} is disappointed.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					return textObject;
				}
			}

			// Token: 0x17000225 RID: 549
			// (get) Token: 0x060012BE RID: 4798 RVA: 0x00075714 File Offset: 0x00073914
			private TextObject PlayerFoundTheSpyButLostTheFightQuestLog
			{
				get
				{
					TextObject textObject = new TextObject("{=hJ1SFkmq}You managed to find the spy but lost the duel. {QUEST_GIVER.LINK} is disappointed.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					return textObject;
				}
			}

			// Token: 0x17000226 RID: 550
			// (get) Token: 0x060012BF RID: 4799 RVA: 0x00075748 File Offset: 0x00073948
			private TextObject PlayerCouldNotFoundTheSpyAndLostTheFightQuestLog
			{
				get
				{
					TextObject textObject = new TextObject("{=dOdp1aKA}You couldn't find the spy and dueled the wrong warrior. {QUEST_GIVER.LINK} is disappointed.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					return textObject;
				}
			}

			// Token: 0x17000227 RID: 551
			// (get) Token: 0x060012C0 RID: 4800 RVA: 0x0007577C File Offset: 0x0007397C
			private TextObject TimedOutQuestLog
			{
				get
				{
					TextObject textObject = new TextObject("{=0dlDkkJV}You have failed to find the spy. {QUEST_GIVER.LINK} is disappointed.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					return textObject;
				}
			}

			// Token: 0x17000228 RID: 552
			// (get) Token: 0x060012C1 RID: 4801 RVA: 0x000757B0 File Offset: 0x000739B0
			private TextObject QuestGiverLostOwnershipQuestLog
			{
				get
				{
					TextObject textObject = new TextObject("{=2OmrHVjp}{QUEST_GIVER.LINK} has lost the ownership of {QUEST_SETTLEMENT}. Your contract with {?QUEST_GIVER.GENDER}her{?}him{\\?} has canceled.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					textObject.SetTextVariable("QUEST_SETTLEMENT", this._selectedSettlement.EncyclopediaLinkWithName);
					return textObject;
				}
			}

			// Token: 0x17000229 RID: 553
			// (get) Token: 0x060012C2 RID: 4802 RVA: 0x000757F9 File Offset: 0x000739F9
			private TextObject WarDeclaredQuestLog
			{
				get
				{
					TextObject textObject = new TextObject("{=cKz1cyuM}Your clan is now at war with {QUEST_GIVER_SETTLEMENT_FACTION}. Quest is canceled.", null);
					textObject.SetTextVariable("QUEST_GIVER_SETTLEMENT_FACTION", base.QuestGiver.MapFaction.Name);
					return textObject;
				}
			}

			// Token: 0x1700022A RID: 554
			// (get) Token: 0x060012C3 RID: 4803 RVA: 0x00075824 File Offset: 0x00073A24
			private TextObject PlayerDeclaredWarQuestLogText
			{
				get
				{
					TextObject textObject = new TextObject("{=bqeWVVEE}Your actions have started a war with {QUEST_GIVER.LINK}'s faction. {?QUEST_GIVER.GENDER}She{?}He{\\?} cancels your agreement and the quest is a failure.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					return textObject;
				}
			}

			// Token: 0x060012C4 RID: 4804 RVA: 0x00075858 File Offset: 0x00073A58
			public TheSpyPartyIssueQuest(string questId, Hero questGiver, CampaignTime duration, int rewardGold, Settlement selectedSettlement, float issueDifficultyMultiplier)
				: base(questId, questGiver, duration, rewardGold)
			{
				this._selectedSettlement = selectedSettlement;
				this._alreadySpokenAgents = new List<Agent>();
				this._issueDifficultyMultiplier = issueDifficultyMultiplier;
				this._giveClueChange = 0.1f;
				this.SetDialogs();
				base.InitializeQuestOnCreation();
				this.InitializeSuspectNpcs();
				this._selectedSpy = this._suspectList.GetRandomElement<TheSpyPartyIssueQuestBehavior.SuspectNpc>();
				if (!base.IsTracked(this._selectedSettlement))
				{
					base.AddTrackedObject(this._selectedSettlement);
				}
			}

			// Token: 0x060012C5 RID: 4805 RVA: 0x000758D4 File Offset: 0x00073AD4
			private void InitializeSuspectNpcs()
			{
				this._suspectList = new MBList<TheSpyPartyIssueQuestBehavior.SuspectNpc>();
				this._currentDifficultySuffix = TheSpyPartyIssueQuestBehavior.GetDifficultySuffix(this._issueDifficultyMultiplier);
				this._suspectList.Add(new TheSpyPartyIssueQuestBehavior.SuspectNpc(MBObjectManager.Instance.GetObject<CharacterObject>("bold_contender_" + this._currentDifficultySuffix), false, true, true, true));
				this._suspectList.Add(new TheSpyPartyIssueQuestBehavior.SuspectNpc(MBObjectManager.Instance.GetObject<CharacterObject>("confident_contender_" + this._currentDifficultySuffix), true, false, true, true));
				this._suspectList.Add(new TheSpyPartyIssueQuestBehavior.SuspectNpc(MBObjectManager.Instance.GetObject<CharacterObject>("dignified_contender_" + this._currentDifficultySuffix), true, true, false, true));
				this._suspectList.Add(new TheSpyPartyIssueQuestBehavior.SuspectNpc(MBObjectManager.Instance.GetObject<CharacterObject>("hardy_contender_" + this._currentDifficultySuffix), true, true, true, false));
			}

			// Token: 0x060012C6 RID: 4806 RVA: 0x000759B5 File Offset: 0x00073BB5
			protected override void InitializeQuestOnGameLoad()
			{
				this._alreadySpokenAgents = new List<Agent>();
				this._giveClueChange = 0.1f;
				this.InitializeSuspectNpcs();
				this.SetDialogs();
				if (Settlement.CurrentSettlement != null && Settlement.CurrentSettlement == this._selectedSettlement)
				{
					this._addSpyNpcsToSettlement = true;
				}
			}

			// Token: 0x060012C7 RID: 4807 RVA: 0x000759F4 File Offset: 0x00073BF4
			protected override void HourlyTick()
			{
			}

			// Token: 0x060012C8 RID: 4808 RVA: 0x000759F8 File Offset: 0x00073BF8
			protected override void SetDialogs()
			{
				TextObject textObject = new TextObject("{=wql79Eta}Good! We understand the spy is going to {TARGET_SETTLEMENT}. If they're trying to gather information,[ib:aggressive2][if:convo_undecided_open] they'll be wandering around the market asking questions in the guise of making small talk. Just go around talking to the townsfolk, and you should be able to figure out who it is.", null);
				textObject.SetTextVariable("TARGET_SETTLEMENT", this._selectedSettlement.EncyclopediaLinkWithName);
				TextObject textObject2 = new TextObject("{=aC0Fq6IE}Do not waste time, {PLAYER.NAME}. The spy probably won't linger any longer than he has to.[if:convo_thinking] Or she has to.", null);
				StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject2, false);
				this.OfferDialogFlow = DialogFlow.CreateDialogFlow("issue_classic_quest_start", 100).NpcLine(textObject, null, null, null, null).Condition(() => Hero.OneToOneConversationHero == base.QuestGiver)
					.Consequence(new ConversationSentence.OnConsequenceDelegate(this.QuestAcceptedConsequences))
					.CloseDialog();
				this.DiscussDialogFlow = DialogFlow.CreateDialogFlow("quest_discuss", 100).NpcLine(new TextObject("{=yLRfb5zb}Any news? Have you managed to find him yet?[if:convo_astonished]", null), null, null, null, null).Condition(() => Hero.OneToOneConversationHero == base.QuestGiver)
					.BeginPlayerOptions(null, false)
					.PlayerOption(new TextObject("{=wErSpkjy}I'm still working on it.", null), null, null, null)
					.NpcLine(textObject2, null, null, null, null)
					.Consequence(new ConversationSentence.OnConsequenceDelegate(this.LeaveEncounter))
					.CloseDialog()
					.PlayerOption(new TextObject("{=I8raOMRH}Sorry. No progress yet.", null), null, null, null)
					.NpcLine(new TextObject("{=ajSm2FEU}I know spies are hard to catch but I tasked this to you for a reason. [if:convo_stern]Do not let me down {PLAYER.NAME}.", null), null, null, null, null)
					.NpcLine(new TextObject("{=pW69nUp8}Finish this task before it is too late.[if:convo_stern]", null), null, null, null, null)
					.Consequence(new ConversationSentence.OnConsequenceDelegate(this.LeaveEncounter))
					.CloseDialog()
					.EndPlayerOptions()
					.CloseDialog();
				Campaign.Current.ConversationManager.AddDialogFlow(this.GetTownsPeopleDialogFlow(), this);
				Campaign.Current.ConversationManager.AddDialogFlow(this.GetNotablesDialogFlow(), this);
				Campaign.Current.ConversationManager.AddDialogFlow(this.GetTradersDialogFlow(), this);
				Campaign.Current.ConversationManager.AddDialogFlow(this.GetSuspectsDialogFlow(), this);
			}

			// Token: 0x060012C9 RID: 4809 RVA: 0x00075BAD File Offset: 0x00073DAD
			private void LeaveEncounter()
			{
				if (PlayerEncounter.Current != null)
				{
					PlayerEncounter.LeaveEncounter = true;
				}
			}

			// Token: 0x060012CA RID: 4810 RVA: 0x00075BBC File Offset: 0x00073DBC
			private void QuestAcceptedConsequences()
			{
				base.StartQuest();
				base.AddLog(this.QuestStartedLog, false);
				if (Settlement.CurrentSettlement != null && Settlement.CurrentSettlement == this._selectedSettlement)
				{
					this._addSpyNpcsToSettlement = true;
				}
			}

			// Token: 0x060012CB RID: 4811 RVA: 0x00075BF0 File Offset: 0x00073DF0
			private DialogFlow GetSuspectsDialogFlow()
			{
				return DialogFlow.CreateDialogFlow("start", 125).NpcLine(new TextObject("{=IqhGJ8Dy}Hello there friend. Are you here for the tournament.[ib:confident3][if:convo_relaxed_happy]", null), null, null, null, null).Condition(() => this._suspectList.Any((TheSpyPartyIssueQuestBehavior.SuspectNpc x) => x.CharacterObject == CharacterObject.OneToOneConversationCharacter))
					.BeginPlayerOptions(null, false)
					.PlayerOption(new TextObject("{=SRa9NyP1}No, my friend. I am on a hunt.", null), null, null, null)
					.NpcLine(new TextObject("{=gYCSwLB2}Eh, what do you mean by that?[ib:closed][if:convo_confused_normal]", null), null, null, null, null)
					.BeginPlayerOptions(null, false)
					.PlayerOption(new TextObject("{=oddzOnad}I'm hunting a spy. And now I have found him.", null), null, null, null)
					.NpcLine(new TextObject("{=MU8nbzwJ}You have nothing on me. If you try to take me anywhere I'll kill you, and it will be in self-defense.[if:convo_grave]", null), null, null, null, null)
					.PlayerLine(new TextObject("{=WDdlPUHw}Not if it's a duel. I challenge you. No true tournament fighter would refuse.", null), null, null, null)
					.NpcLine(new TextObject("{=Ll8q45h5}Hmf... Very well. I shall wipe out this insult with your blood.[ib:warrior][if:convo_furious]", null), null, null, null, null)
					.Consequence(delegate
					{
						Campaign.Current.ConversationManager.ConversationEndOneShot += this.StartFightWithSpy;
					})
					.CloseDialog()
					.PlayerOption(new TextObject("{=O5PDH9Bc}Nothing, nothing. Go on your way.", null), null, null, null)
					.CloseDialog()
					.EndPlayerOptions()
					.PlayerOption(new TextObject("{=O7j0uzcH}I should be on my way.", null), null, null, null)
					.CloseDialog()
					.EndPlayerOptions();
			}

			// Token: 0x060012CC RID: 4812 RVA: 0x00075D04 File Offset: 0x00073F04
			private void StartFightWithSpy()
			{
				this._playerManagedToFindSpy = this._selectedSpy.CharacterObject == CharacterObject.OneToOneConversationCharacter;
				this._duelCharacter = CharacterObject.OneToOneConversationCharacter;
				this._startFightWithSpy = true;
				Campaign.Current.GameMenuManager.NextLocation = LocationComplex.Current.GetLocationWithId("arena");
				Mission.Current.EndMission();
			}

			// Token: 0x060012CD RID: 4813 RVA: 0x00075D64 File Offset: 0x00073F64
			private DialogFlow GetNotablesDialogFlow()
			{
				TextObject textObject = new TextObject("{=0RTwaPBJ}I speak to many people. Of course, as I am loyal to {QUEST_GIVER.NAME}, [if:convo_pondering]I am always on the lookout for spies. But I've seen no one like this.", null);
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
				return DialogFlow.CreateDialogFlow("hero_main_options", 125).BeginPlayerOptions(null, false).PlayerOption(new TextObject("{=xPTxkzVM}I am looking for a spy. Have you seen any warriors in the tournament wandering about, asking too many suspicious questions?", null), null, null, null)
					.Condition(() => Settlement.CurrentSettlement == this._selectedSettlement && Hero.OneToOneConversationHero.IsNotable)
					.NpcLine(textObject, null, null, null, null)
					.CloseDialog()
					.EndPlayerOptions();
			}

			// Token: 0x060012CE RID: 4814 RVA: 0x00075DE0 File Offset: 0x00073FE0
			private DialogFlow GetTradersDialogFlow()
			{
				return DialogFlow.CreateDialogFlow("weaponsmith_talk_player", 125).BeginPlayerOptions(null, false).PlayerOption(new TextObject("{=SHwlcdp3}I ask you because you are a trader here. Have you seen one of the warriors in the tournament walking around here, asking people a lot of suspicious questions?", null), null, null, null)
					.Condition(() => Settlement.CurrentSettlement == this._selectedSettlement && (CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.HorseTrader || CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.GoodsTrader || CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.Blacksmith || CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.Weaponsmith))
					.NpcLine(new TextObject("{=ocoHNhNk}Hmm... I keep pretty busy with my own trade. I haven't heard anything like that.[if:convo_pondering]", null), null, null, null, null)
					.CloseDialog()
					.EndPlayerOptions();
			}

			// Token: 0x060012CF RID: 4815 RVA: 0x00075E44 File Offset: 0x00074044
			private DialogFlow GetTownsPeopleDialogFlow()
			{
				TextObject playerOption1 = new TextObject("{=A2oos2Uo}Listen to me. I'm on assignment from {QUEST_GIVER.NAME}. Have any strangers been around here, asking odd questions about the garrison?", null);
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, playerOption1, false);
				TextObject playerOption2 = new TextObject("{=RXhBl8e1}Act normal. Have any of the participants in the tournament come round, asking very odd questions?", null);
				TextObject playerOption3 = new TextObject("{=HF2GIpbI}Listen to me. Have any of the tournament participants spent long hours in the market and tavern? More than usual?", null);
				float dontGiveClueResponse = 0f;
				bool giveClue = false;
				return DialogFlow.CreateDialogFlow("town_or_village_player", 125).BeginPlayerOptions(null, false).PlayerOption(new TextObject("{=GtgGnMe1}{PLAYER_OPTION}", null), null, null, null)
					.Condition(delegate
					{
						if (Settlement.CurrentSettlement == this._selectedSettlement)
						{
							float randomFloat = MBRandom.RandomFloat;
							dontGiveClueResponse = MBRandom.RandomFloat;
							if (randomFloat < 0.33f)
							{
								MBTextManager.SetTextVariable("PLAYER_OPTION", playerOption1, false);
							}
							else if (randomFloat >= 0.33f && randomFloat <= 0.66f)
							{
								MBTextManager.SetTextVariable("PLAYER_OPTION", playerOption2, false);
							}
							else
							{
								MBTextManager.SetTextVariable("PLAYER_OPTION", playerOption3, false);
							}
							return true;
						}
						return false;
					})
					.Consequence(delegate
					{
						giveClue = this._giveClueChange >= MBRandom.RandomFloat;
						Campaign.Current.ConversationManager.ConversationEndOneShot += this.AddAgentToAlreadySpokenList;
					})
					.BeginNpcOptions(null, false)
					.NpcOption(new TextObject("{=8gmne3b9}Not to me {?PLAYER.GENDER}madam{?}sir{\\?}, no. I did overhear someone talking to another merchant about such things. I remember him because he had this nasty looking sword by his side.[if:convo_disbelief]", null), () => giveClue && this._selectedSpy.HasBigSword && !this._playerLearnedHasBigSword && this.CommonCondition(), null, null, null, null)
					.PlayerLine(new TextObject("{=VP6s1YFW}Many contenders have swords on their backs. Still this information might prove useful.", null), null, null, null)
					.Consequence(new ConversationSentence.OnConsequenceDelegate(this.PlayerLearnedSpyHasSword))
					.CloseDialog()
					.NpcOption(new TextObject("{=gHnMYU9n}Why yes... At the tavern last night... Cornered a drunk and kept pressing him for information about the gatehouse. Had a beard, that one did.[if:convo_pondering]", null), () => giveClue && this._selectedSpy.HasBeard && !this._playerLearnedHasBeard && this.CommonCondition(), null, null, null, null)
					.PlayerLine(new TextObject("{=QaAzicqA}Many men have beards. Still, that is something.", null), null, null, null)
					.Consequence(new ConversationSentence.OnConsequenceDelegate(this.PlayerLearnedSpyHasBeard))
					.CloseDialog()
					.NpcOption(new TextObject("{=DUVqJifX}Yeah. I've seen one like that around the arena, asking all matter of outlandish questions. Middle-aged, normal head of hair, that's really all I can remember though.[if:convo_thinking]", null), () => giveClue && this._selectedSpy.HasHair && !this._playerLearnedHasHair && this.CommonCondition(), null, null, null, null)
					.PlayerLine(new TextObject("{=JjtmptiD}More men have hair than not, but this is another tile in the mosaic.", null), null, null, null)
					.Consequence(new ConversationSentence.OnConsequenceDelegate(this.PlayerLearnedSpyHasHair))
					.CloseDialog()
					.NpcOption(new TextObject("{=tXpmCzoZ}Well, there was one warrior. A handsome young lad. Didn't have any of those scars that some fighters pick up in battle, nor any of those marks or tattoos or whatever that [if:convo_pondering]some of the hard cases like to show off.", null), () => giveClue && this._selectedSpy.WithoutMarkings && !this._playerLearnedHasNoMarkings && this.CommonCondition(), null, null, null, null)
					.PlayerLine(new TextObject("{=ZCbQvqqv}A face without scars and markings is usual for farmers and merchants but less so for warriors. This might be useful.", null), null, null, null)
					.Consequence(new ConversationSentence.OnConsequenceDelegate(this.PlayerLearnedSpyHasNoMarkings))
					.CloseDialog()
					.NpcOption(new TextObject("{=sfxfiWxl}{?PLAYER.GENDER}Madam{?}Sir{\\?}, people gossip. Everyone around here knows you've been asking those questions. Your quarry is going to slip away if you don't move quickly.", null), () => dontGiveClueResponse <= 0.2f, null, null, null, null)
					.PlayerLine(new TextObject("{=04gFKwY1}Well, if you see anyone like that, let me know.", null), null, null, null)
					.CloseDialog()
					.NpcOption(new TextObject("{=VWaNqkqJ}Can't say I've seen anyone around here like that, {?PLAYER.GENDER}madam{?}sir{\\?}.", null), () => dontGiveClueResponse > 0.2f && dontGiveClueResponse <= 0.4f, null, null, null, null)
					.PlayerLine(new TextObject("{=QbzsgawM}Okay, just keep your eyes open.", null), null, null, null)
					.CloseDialog()
					.NpcOption(new TextObject("{=ff5XEKPB}Afraid I can't recall anyone like that, {?PLAYER.GENDER}madam{?}sir{\\?}.", null), () => dontGiveClueResponse > 0.4f && dontGiveClueResponse <= 0.6f, null, null, null, null)
					.PlayerLine(new TextObject("{=ArseaKsm}Very well. Thanks for your time.", null), null, null, null)
					.CloseDialog()
					.NpcOption(new TextObject("{=C6EOT3yY}No, sorry. Haven't seen anything like that.", null), () => dontGiveClueResponse > 0.6f && dontGiveClueResponse <= 0.8f, null, null, null, null)
					.PlayerLine(new TextObject("{=3UX334MB}Hmm.. Very well. Sorry for interrupting.", null), null, null, null)
					.CloseDialog()
					.NpcOption(new TextObject("{=9DDWjL9Y}Hmm... Maybe, but I can't remember who. I didn't think it suspicious.", null), () => dontGiveClueResponse > 0.8f, null, null, null, null)
					.PlayerLine(new TextObject("{=QbzsgawM}Okay, just keep your eyes open.", null), null, null, null)
					.CloseDialog()
					.EndNpcOptions()
					.EndPlayerOptions();
			}

			// Token: 0x060012D0 RID: 4816 RVA: 0x00076155 File Offset: 0x00074355
			private void AddAgentToAlreadySpokenList()
			{
				this._giveClueChange += 0.15f;
				this._alreadySpokenAgents.Add((Agent)MissionConversationLogic.Current.ConversationManager.ConversationAgents[0]);
			}

			// Token: 0x060012D1 RID: 4817 RVA: 0x00076190 File Offset: 0x00074390
			private bool CommonCondition()
			{
				return Settlement.CurrentSettlement == this._selectedSettlement && !this._alreadySpokenAgents.Contains((Agent)MissionConversationLogic.Current.ConversationManager.ConversationAgents[0]) && CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.Townsfolk;
			}

			// Token: 0x060012D2 RID: 4818 RVA: 0x000761E0 File Offset: 0x000743E0
			private void CheckIfPlayerLearnedEverything()
			{
				int num = 0;
				num = (this._playerLearnedHasBeard ? (num + 1) : num);
				num = (this._playerLearnedHasBigSword ? (num + 1) : num);
				num = (this._playerLearnedHasHair ? (num + 1) : num);
				num = (this._playerLearnedHasNoMarkings ? (num + 1) : num);
				if (num >= 3)
				{
					TextObject text = new TextObject("{=2LW2jWuG}You should now have enough evidence to identify the spy. You might be able to find the tournament participants hanging out in the alleys with local thugs. Find and speak with them.", null);
					base.AddLog(text, false);
				}
			}

			// Token: 0x060012D3 RID: 4819 RVA: 0x0007624C File Offset: 0x0007444C
			private void PlayerLearnedSpyHasSword()
			{
				this._giveClueChange = 0f;
				this._playerLearnedHasBigSword = true;
				base.AddLog(new TextObject("{=awYMellZ}The spy is known to carry a sword.", null), false);
				this._alreadySpokenAgents.Add(MissionConversationLogic.Current.ConversationAgent);
				this.CheckIfPlayerLearnedEverything();
			}

			// Token: 0x060012D4 RID: 4820 RVA: 0x0007629C File Offset: 0x0007449C
			private void PlayerLearnedSpyHasBeard()
			{
				this._giveClueChange = 0f;
				this._playerLearnedHasBeard = true;
				base.AddLog(new TextObject("{=5om6Wv1n}After questioning some folk in town, you learned that the spy has a beard.", null), false);
				this._alreadySpokenAgents.Add(MissionConversationLogic.Current.ConversationAgent);
				this.CheckIfPlayerLearnedEverything();
			}

			// Token: 0x060012D5 RID: 4821 RVA: 0x000762EC File Offset: 0x000744EC
			private void PlayerLearnedSpyHasHair()
			{
				this._giveClueChange = 0f;
				this._playerLearnedHasHair = true;
				base.AddLog(new TextObject("{=PLgOm8tV}The townsfolk told you that the spy is not bald.", null), false);
				this._alreadySpokenAgents.Add(MissionConversationLogic.Current.ConversationAgent);
				this.CheckIfPlayerLearnedEverything();
			}

			// Token: 0x060012D6 RID: 4822 RVA: 0x0007633C File Offset: 0x0007453C
			private void PlayerLearnedSpyHasNoMarkings()
			{
				this._giveClueChange = 0f;
				this._playerLearnedHasNoMarkings = true;
				base.AddLog(new TextObject("{=1ieLd5qq}The townsfolk told you that the spy has no distinctive scars or other facial markings.", null), false);
				this._alreadySpokenAgents.Add(MissionConversationLogic.Current.ConversationAgent);
				this.CheckIfPlayerLearnedEverything();
			}

			// Token: 0x060012D7 RID: 4823 RVA: 0x0007638C File Offset: 0x0007458C
			protected override void RegisterEvents()
			{
				CampaignEvents.SettlementEntered.AddNonSerializedListener(this, new Action<MobileParty, Settlement, Hero>(this.OnSettlementEntered));
				CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, new Action<MobileParty, Settlement>(this.OnSettlementLeft));
				CampaignEvents.AfterMissionStarted.AddNonSerializedListener(this, new Action<IMission>(this.OnMissionStarted));
				CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, new Action<Settlement, bool, Hero, Hero, Hero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail>(this.OnSettlementOwnerChanged));
				CampaignEvents.WarDeclared.AddNonSerializedListener(this, new Action<IFaction, IFaction, DeclareWarAction.DeclareWarDetail>(this.OnWarDeclared));
				CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, new Action<Clan, Kingdom, Kingdom, ChangeKingdomAction.ChangeKingdomActionDetail, bool>(this.OnClanChangedKingdom));
				CampaignEvents.BeforeGameMenuOpenedEvent.AddNonSerializedListener(this, new Action<MenuCallbackArgs>(this.BeforeGameMenuOpenedEvent));
				CampaignEvents.MapEventStarted.AddNonSerializedListener(this, new Action<MapEvent, PartyBase, PartyBase>(this.OnMapEventStarted));
			}

			// Token: 0x060012D8 RID: 4824 RVA: 0x00076451 File Offset: 0x00074651
			private void OnMapEventStarted(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
			{
				if (QuestHelper.CheckMinorMajorCoercion(this, mapEvent, attackerParty))
				{
					QuestHelper.ApplyGenericMinorMajorCoercionConsequences(this, mapEvent);
				}
			}

			// Token: 0x060012D9 RID: 4825 RVA: 0x00076464 File Offset: 0x00074664
			private void BeforeGameMenuOpenedEvent(MenuCallbackArgs args)
			{
				if (Settlement.CurrentSettlement == this._selectedSettlement && args.MenuContext.GameMenu.StringId == "town")
				{
					if (this._startFightWithSpy && Campaign.Current.GameMenuManager.NextLocation == LocationComplex.Current.GetLocationWithId("arena") && GameStateManager.Current.ActiveState is MapState)
					{
						this._startFightWithSpy = false;
						CampaignMission.OpenArenaDuelMission(LocationComplex.Current.GetLocationWithId("arena").GetSceneName(this._selectedSettlement.Town.GetWallLevel()), LocationComplex.Current.GetLocationWithId("arena"), this._duelCharacter, false, false, new Action<CharacterObject>(this.OnFightEnd), 225f);
						Campaign.Current.GameMenuManager.NextLocation = null;
					}
					if (this._checkForBattleResult)
					{
						if (this._playerWonTheFight)
						{
							if (this._playerManagedToFindSpy)
							{
								this.PlayerFoundTheSpyAndKilledHim();
								return;
							}
							this.PlayerCouldNotFoundTheSpyAndKilledAnotherSuspect();
							return;
						}
						else
						{
							if (this._playerManagedToFindSpy)
							{
								this.PlayerFoundTheSpyButLostTheFight();
								return;
							}
							this.PlayerCouldNotFoundTheSpyAndLostTheFight();
						}
					}
				}
			}

			// Token: 0x060012DA RID: 4826 RVA: 0x00076580 File Offset: 0x00074780
			private void PlayerFoundTheSpyAndKilledHim()
			{
				base.AddLog(this.QuestSuccessQuestLog, false);
				GainRenownAction.Apply(Hero.MainHero, 1f, false);
				GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, this.RewardGold, false);
				this.RelationshipChangeWithQuestGiver = 5;
				this._selectedSettlement.Town.Prosperity += 5f;
				base.CompleteQuestWithSuccess();
			}

			// Token: 0x060012DB RID: 4827 RVA: 0x000765E8 File Offset: 0x000747E8
			private void PlayerCouldNotFoundTheSpyAndKilledAnotherSuspect()
			{
				base.AddLog(this.QuestFailedKilledAnotherQuestLog, false);
				this.RelationshipChangeWithQuestGiver = -5;
				this._selectedSettlement.Town.Security -= 10f;
				this._selectedSettlement.Town.Loyalty -= 10f;
				base.CompleteQuestWithFail(null);
				ChangeCrimeRatingAction.Apply(base.QuestGiver.MapFaction, 10f, true);
			}

			// Token: 0x060012DC RID: 4828 RVA: 0x00076660 File Offset: 0x00074860
			private void PlayerFoundTheSpyButLostTheFight()
			{
				base.AddLog(this.PlayerFoundTheSpyButLostTheFightQuestLog, false);
				this.RelationshipChangeWithQuestGiver = -5;
				this._selectedSettlement.Town.Security -= 10f;
				this._selectedSettlement.Town.Loyalty -= 10f;
				base.CompleteQuestWithFail(null);
			}

			// Token: 0x060012DD RID: 4829 RVA: 0x000766C4 File Offset: 0x000748C4
			private void PlayerCouldNotFoundTheSpyAndLostTheFight()
			{
				base.AddLog(this.PlayerCouldNotFoundTheSpyAndLostTheFightQuestLog, false);
				this.RelationshipChangeWithQuestGiver = -5;
				this._selectedSettlement.Town.Security -= 10f;
				this._selectedSettlement.Town.Loyalty -= 10f;
				base.CompleteQuestWithFail(null);
			}

			// Token: 0x060012DE RID: 4830 RVA: 0x00076726 File Offset: 0x00074926
			private void OnFightEnd(CharacterObject winnerCharacterObject)
			{
				this._checkForBattleResult = true;
				this._playerWonTheFight = winnerCharacterObject == CharacterObject.PlayerCharacter;
			}

			// Token: 0x060012DF RID: 4831 RVA: 0x0007673D File Offset: 0x0007493D
			private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
			{
				if (base.QuestGiver.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction))
				{
					base.CompleteQuestWithCancel(this.WarDeclaredQuestLog);
				}
			}

			// Token: 0x060012E0 RID: 4832 RVA: 0x00076767 File Offset: 0x00074967
			private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
			{
				QuestHelper.CheckWarDeclarationAndFailOrCancelTheQuest(this, faction1, faction2, detail, this.PlayerDeclaredWarQuestLogText, this.WarDeclaredQuestLog, false);
			}

			// Token: 0x060012E1 RID: 4833 RVA: 0x0007677F File Offset: 0x0007497F
			private void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
			{
				if (settlement == this._selectedSettlement && oldOwner.Clan == base.QuestGiver.Clan)
				{
					base.AddLog(this.QuestGiverLostOwnershipQuestLog, false);
					base.CompleteQuestWithCancel(null);
				}
			}

			// Token: 0x060012E2 RID: 4834 RVA: 0x000767B3 File Offset: 0x000749B3
			private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
			{
				if (party != null && party.IsMainParty && settlement == this._selectedSettlement && hero == Hero.MainHero)
				{
					this._addSpyNpcsToSettlement = true;
				}
			}

			// Token: 0x060012E3 RID: 4835 RVA: 0x000767D8 File Offset: 0x000749D8
			public override GameMenuOption.IssueQuestFlags IsLocationTrackedByQuest(Location location)
			{
				if (PlayerEncounter.LocationEncounter.Settlement == this._selectedSettlement && location.StringId == "center")
				{
					return GameMenuOption.IssueQuestFlags.ActiveIssue;
				}
				return GameMenuOption.IssueQuestFlags.None;
			}

			// Token: 0x060012E4 RID: 4836 RVA: 0x00076801 File Offset: 0x00074A01
			private void OnSettlementLeft(MobileParty party, Settlement settlement)
			{
				if (party.IsMainParty && settlement == this._selectedSettlement)
				{
					this._addSpyNpcsToSettlement = false;
				}
			}

			// Token: 0x060012E5 RID: 4837 RVA: 0x0007681C File Offset: 0x00074A1C
			private void OnMissionStarted(IMission mission)
			{
				if (this._addSpyNpcsToSettlement)
				{
					Location locationWithId = Settlement.CurrentSettlement.LocationComplex.GetLocationWithId("center");
					if (locationWithId != null)
					{
						locationWithId.AddLocationCharacters(new CreateLocationCharacterDelegate(this.CreateBoldSpyLocationCharacter), Settlement.CurrentSettlement.Culture, LocationCharacter.CharacterRelations.Neutral, 1);
						locationWithId.AddLocationCharacters(new CreateLocationCharacterDelegate(this.CreateConfidentSpyLocationCharacter), Settlement.CurrentSettlement.Culture, LocationCharacter.CharacterRelations.Neutral, 1);
						locationWithId.AddLocationCharacters(new CreateLocationCharacterDelegate(this.CreateDignifiedSpyLocationCharacter), Settlement.CurrentSettlement.Culture, LocationCharacter.CharacterRelations.Neutral, 1);
						locationWithId.AddLocationCharacters(new CreateLocationCharacterDelegate(this.CreateHardySpyLocationCharacters), Settlement.CurrentSettlement.Culture, LocationCharacter.CharacterRelations.Neutral, 1);
					}
				}
			}

			// Token: 0x060012E6 RID: 4838 RVA: 0x000768C4 File Offset: 0x00074AC4
			private LocationCharacter CreateBoldSpyLocationCharacter(CultureObject culture, LocationCharacter.CharacterRelations relation)
			{
				CharacterObject @object = MBObjectManager.Instance.GetObject<CharacterObject>("bold_contender_" + this._currentDifficultySuffix);
				Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(@object.Race, "_settlement");
				Tuple<string, Monster> tuple = new Tuple<string, Monster>(ActionSetCode.GenerateActionSetNameWithSuffix(monsterWithSuffix, @object.IsFemale, "_villain"), monsterWithSuffix);
				return new LocationCharacter(new AgentData(new SimpleAgentOrigin(@object, -1, null, default(UniqueTroopDescriptor))).Monster(tuple.Item2), new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddFixedCharacterBehaviors), "alley_1", true, relation, tuple.Item1, true, false, null, false, true, false, null, false);
			}

			// Token: 0x060012E7 RID: 4839 RVA: 0x00076968 File Offset: 0x00074B68
			private LocationCharacter CreateConfidentSpyLocationCharacter(CultureObject culture, LocationCharacter.CharacterRelations relation)
			{
				CharacterObject @object = MBObjectManager.Instance.GetObject<CharacterObject>("confident_contender_" + this._currentDifficultySuffix);
				Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(@object.Race, "_settlement");
				Tuple<string, Monster> tuple = new Tuple<string, Monster>(ActionSetCode.GenerateActionSetNameWithSuffix(monsterWithSuffix, @object.IsFemale, "_villain"), monsterWithSuffix);
				return new LocationCharacter(new AgentData(new SimpleAgentOrigin(@object, -1, null, default(UniqueTroopDescriptor))).Monster(tuple.Item2), new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddFixedCharacterBehaviors), "alley_3", true, relation, tuple.Item1, true, false, null, false, true, false, null, false);
			}

			// Token: 0x060012E8 RID: 4840 RVA: 0x00076A0C File Offset: 0x00074C0C
			private LocationCharacter CreateDignifiedSpyLocationCharacter(CultureObject culture, LocationCharacter.CharacterRelations relation)
			{
				CharacterObject @object = MBObjectManager.Instance.GetObject<CharacterObject>("dignified_contender_" + this._currentDifficultySuffix);
				Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(@object.Race, "_settlement");
				Tuple<string, Monster> tuple = new Tuple<string, Monster>(ActionSetCode.GenerateActionSetNameWithSuffix(monsterWithSuffix, @object.IsFemale, "_villain"), monsterWithSuffix);
				return new LocationCharacter(new AgentData(new SimpleAgentOrigin(@object, -1, null, default(UniqueTroopDescriptor))).Monster(tuple.Item2), new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddFixedCharacterBehaviors), "alley_3", true, relation, tuple.Item1, true, false, null, false, true, false, null, false);
			}

			// Token: 0x060012E9 RID: 4841 RVA: 0x00076AB0 File Offset: 0x00074CB0
			private LocationCharacter CreateHardySpyLocationCharacters(CultureObject culture, LocationCharacter.CharacterRelations relation)
			{
				CharacterObject @object = MBObjectManager.Instance.GetObject<CharacterObject>("hardy_contender_" + this._currentDifficultySuffix);
				Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(@object.Race, "_settlement");
				Tuple<string, Monster> tuple = new Tuple<string, Monster>(ActionSetCode.GenerateActionSetNameWithSuffix(monsterWithSuffix, @object.IsFemale, "_villain"), monsterWithSuffix);
				return new LocationCharacter(new AgentData(new SimpleAgentOrigin(@object, -1, null, default(UniqueTroopDescriptor))).Monster(tuple.Item2), new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddFixedCharacterBehaviors), "alley_2", true, relation, tuple.Item1, true, false, null, false, true, false, null, false);
			}

			// Token: 0x060012EA RID: 4842 RVA: 0x00076B54 File Offset: 0x00074D54
			protected override void OnTimedOut()
			{
				base.AddLog(this.TimedOutQuestLog, false);
				base.QuestGiver.AddPower(-5f);
				this.RelationshipChangeWithQuestGiver = -5;
				this._selectedSettlement.Town.Security -= 10f;
				this._selectedSettlement.Town.Loyalty -= 10f;
			}

			// Token: 0x060012EB RID: 4843 RVA: 0x00076BBF File Offset: 0x00074DBF
			internal static void AutoGeneratedStaticCollectObjectsTheSpyPartyIssueQuest(object o, List<object> collectedObjects)
			{
				((TheSpyPartyIssueQuestBehavior.TheSpyPartyIssueQuest)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
			}

			// Token: 0x060012EC RID: 4844 RVA: 0x00076BCD File Offset: 0x00074DCD
			protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
			{
				base.AutoGeneratedInstanceCollectObjects(collectedObjects);
				collectedObjects.Add(this._selectedSettlement);
				TheSpyPartyIssueQuestBehavior.SuspectNpc.AutoGeneratedStaticCollectObjectsSuspectNpc(this._selectedSpy, collectedObjects);
			}

			// Token: 0x060012ED RID: 4845 RVA: 0x00076BF3 File Offset: 0x00074DF3
			internal static object AutoGeneratedGetMemberValue_selectedSettlement(object o)
			{
				return ((TheSpyPartyIssueQuestBehavior.TheSpyPartyIssueQuest)o)._selectedSettlement;
			}

			// Token: 0x060012EE RID: 4846 RVA: 0x00076C00 File Offset: 0x00074E00
			internal static object AutoGeneratedGetMemberValue_selectedSpy(object o)
			{
				return ((TheSpyPartyIssueQuestBehavior.TheSpyPartyIssueQuest)o)._selectedSpy;
			}

			// Token: 0x060012EF RID: 4847 RVA: 0x00076C12 File Offset: 0x00074E12
			internal static object AutoGeneratedGetMemberValue_playerLearnedHasHair(object o)
			{
				return ((TheSpyPartyIssueQuestBehavior.TheSpyPartyIssueQuest)o)._playerLearnedHasHair;
			}

			// Token: 0x060012F0 RID: 4848 RVA: 0x00076C24 File Offset: 0x00074E24
			internal static object AutoGeneratedGetMemberValue_playerLearnedHasNoMarkings(object o)
			{
				return ((TheSpyPartyIssueQuestBehavior.TheSpyPartyIssueQuest)o)._playerLearnedHasNoMarkings;
			}

			// Token: 0x060012F1 RID: 4849 RVA: 0x00076C36 File Offset: 0x00074E36
			internal static object AutoGeneratedGetMemberValue_playerLearnedHasBigSword(object o)
			{
				return ((TheSpyPartyIssueQuestBehavior.TheSpyPartyIssueQuest)o)._playerLearnedHasBigSword;
			}

			// Token: 0x060012F2 RID: 4850 RVA: 0x00076C48 File Offset: 0x00074E48
			internal static object AutoGeneratedGetMemberValue_playerLearnedHasBeard(object o)
			{
				return ((TheSpyPartyIssueQuestBehavior.TheSpyPartyIssueQuest)o)._playerLearnedHasBeard;
			}

			// Token: 0x060012F3 RID: 4851 RVA: 0x00076C5A File Offset: 0x00074E5A
			internal static object AutoGeneratedGetMemberValue_issueDifficultyMultiplier(object o)
			{
				return ((TheSpyPartyIssueQuestBehavior.TheSpyPartyIssueQuest)o)._issueDifficultyMultiplier;
			}

			// Token: 0x060012F4 RID: 4852 RVA: 0x00076C6C File Offset: 0x00074E6C
			internal static object AutoGeneratedGetMemberValue_currentDifficultySuffix(object o)
			{
				return ((TheSpyPartyIssueQuestBehavior.TheSpyPartyIssueQuest)o)._currentDifficultySuffix;
			}

			// Token: 0x040008AC RID: 2220
			public const float CustomAgentHealth = 225f;

			// Token: 0x040008AD RID: 2221
			[SaveableField(10)]
			private Settlement _selectedSettlement;

			// Token: 0x040008AE RID: 2222
			[SaveableField(20)]
			private TheSpyPartyIssueQuestBehavior.SuspectNpc _selectedSpy;

			// Token: 0x040008AF RID: 2223
			private MBList<TheSpyPartyIssueQuestBehavior.SuspectNpc> _suspectList;

			// Token: 0x040008B0 RID: 2224
			private List<Agent> _alreadySpokenAgents;

			// Token: 0x040008B1 RID: 2225
			[SaveableField(30)]
			private bool _playerLearnedHasHair;

			// Token: 0x040008B2 RID: 2226
			[SaveableField(40)]
			private bool _playerLearnedHasNoMarkings;

			// Token: 0x040008B3 RID: 2227
			[SaveableField(50)]
			private bool _playerLearnedHasBigSword;

			// Token: 0x040008B4 RID: 2228
			[SaveableField(60)]
			private bool _playerLearnedHasBeard;

			// Token: 0x040008B5 RID: 2229
			private bool _playerWonTheFight;

			// Token: 0x040008B6 RID: 2230
			private bool _addSpyNpcsToSettlement;

			// Token: 0x040008B7 RID: 2231
			private bool _startFightWithSpy;

			// Token: 0x040008B8 RID: 2232
			private bool _checkForBattleResult;

			// Token: 0x040008B9 RID: 2233
			private bool _playerManagedToFindSpy;

			// Token: 0x040008BA RID: 2234
			private float _giveClueChange;

			// Token: 0x040008BB RID: 2235
			private CharacterObject _duelCharacter;

			// Token: 0x040008BC RID: 2236
			[SaveableField(70)]
			private float _issueDifficultyMultiplier;

			// Token: 0x040008BD RID: 2237
			[SaveableField(80)]
			private string _currentDifficultySuffix;
		}
	}
}
