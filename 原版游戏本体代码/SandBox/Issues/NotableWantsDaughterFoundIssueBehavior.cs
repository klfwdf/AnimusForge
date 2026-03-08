using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using SandBox.Conversation.MissionLogics;
using SandBox.Missions.AgentBehaviors;
using SandBox.Missions.MissionLogics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Conversation.Persuasion;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Extensions;
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
	// Token: 0x020000B1 RID: 177
	public class NotableWantsDaughterFoundIssueBehavior : CampaignBehaviorBase
	{
		// Token: 0x06000763 RID: 1891 RVA: 0x00032BC7 File Offset: 0x00030DC7
		public override void RegisterEvents()
		{
			CampaignEvents.OnCheckForIssueEvent.AddNonSerializedListener(this, new Action<Hero>(this.OnCheckForIssue));
			CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, new Action(this.OnGameLoadFinished));
		}

		// Token: 0x06000764 RID: 1892 RVA: 0x00032BF8 File Offset: 0x00030DF8
		private void OnGameLoadFinished()
		{
			if (MBSaveLoad.IsUpdatingGameVersion && MBSaveLoad.LastLoadedGameVersion.IsOlderThan(ApplicationVersion.FromString("v1.2.8.31599", 0)))
			{
				foreach (Hero hero in Hero.DeadOrDisabledHeroes)
				{
					if (hero.IsDead && hero.CompanionOf == Clan.PlayerClan && hero.Father != null && hero.Father.IsNotable && hero.Father.CurrentSettlement.IsVillage)
					{
						RemoveCompanionAction.ApplyByDeath(Clan.PlayerClan, hero);
					}
				}
			}
		}

		// Token: 0x06000765 RID: 1893 RVA: 0x00032CB0 File Offset: 0x00030EB0
		public void OnCheckForIssue(Hero hero)
		{
			if (this.ConditionsHold(hero))
			{
				Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(new PotentialIssueData.StartIssueDelegate(this.OnStartIssue), typeof(NotableWantsDaughterFoundIssueBehavior.NotableWantsDaughterFoundIssue), IssueBase.IssueFrequency.Rare, null));
				return;
			}
			Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(typeof(NotableWantsDaughterFoundIssueBehavior.NotableWantsDaughterFoundIssue), IssueBase.IssueFrequency.Rare));
		}

		// Token: 0x06000766 RID: 1894 RVA: 0x00032D14 File Offset: 0x00030F14
		private bool ConditionsHold(Hero issueGiver)
		{
			if (issueGiver.IsRuralNotable && issueGiver.CurrentSettlement.IsVillage && issueGiver.CurrentSettlement.Village.Bound != null && issueGiver.CurrentSettlement.Village.Bound.BoundVillages.Count > 2 && issueGiver.CanHaveCampaignIssues() && issueGiver.Age > (float)(Campaign.Current.Models.AgeModel.HeroComesOfAge * 2) && CharacterHelper.GetRandomCompanionTemplateWithPredicate((CharacterObject x) => x.IsFemale && x.Culture == issueGiver.CurrentSettlement.Culture) != null)
			{
				if (issueGiver.CurrentSettlement.Culture.NotableTemplates.Any((CharacterObject x) => x.Occupation == Occupation.GangLeader && !x.IsFemale) && issueGiver.GetTraitLevel(DefaultTraits.Mercy) <= 0)
				{
					return issueGiver.GetTraitLevel(DefaultTraits.Generosity) <= 0;
				}
			}
			return false;
		}

		// Token: 0x06000767 RID: 1895 RVA: 0x00032E3F File Offset: 0x0003103F
		private IssueBase OnStartIssue(in PotentialIssueData pid, Hero issueOwner)
		{
			return new NotableWantsDaughterFoundIssueBehavior.NotableWantsDaughterFoundIssue(issueOwner);
		}

		// Token: 0x06000768 RID: 1896 RVA: 0x00032E47 File Offset: 0x00031047
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x040003F7 RID: 1015
		private const IssueBase.IssueFrequency NotableWantsDaughterFoundIssueFrequency = IssueBase.IssueFrequency.Rare;

		// Token: 0x040003F8 RID: 1016
		private const int IssueDuration = 30;

		// Token: 0x040003F9 RID: 1017
		private const int QuestTimeLimit = 19;

		// Token: 0x040003FA RID: 1018
		private const int BaseRewardGold = 500;

		// Token: 0x020001BC RID: 444
		public class NotableWantsDaughterFoundIssueTypeDefiner : SaveableTypeDefiner
		{
			// Token: 0x0600100C RID: 4108 RVA: 0x0006A2A6 File Offset: 0x000684A6
			public NotableWantsDaughterFoundIssueTypeDefiner()
				: base(1088000)
			{
			}

			// Token: 0x0600100D RID: 4109 RVA: 0x0006A2B3 File Offset: 0x000684B3
			protected override void DefineClassTypes()
			{
				base.AddClassDefinition(typeof(NotableWantsDaughterFoundIssueBehavior.NotableWantsDaughterFoundIssue), 1, null);
				base.AddClassDefinition(typeof(NotableWantsDaughterFoundIssueBehavior.NotableWantsDaughterFoundIssueQuest), 2, null);
			}
		}

		// Token: 0x020001BD RID: 445
		public class NotableWantsDaughterFoundIssue : IssueBase
		{
			// Token: 0x1700015F RID: 351
			// (get) Token: 0x0600100E RID: 4110 RVA: 0x0006A2D9 File Offset: 0x000684D9
			public override IssueBase.AlternativeSolutionScaleFlag AlternativeSolutionScaleFlags
			{
				get
				{
					return IssueBase.AlternativeSolutionScaleFlag.FailureRisk;
				}
			}

			// Token: 0x17000160 RID: 352
			// (get) Token: 0x0600100F RID: 4111 RVA: 0x0006A2DC File Offset: 0x000684DC
			public override bool IsThereAlternativeSolution
			{
				get
				{
					return true;
				}
			}

			// Token: 0x17000161 RID: 353
			// (get) Token: 0x06001010 RID: 4112 RVA: 0x0006A2DF File Offset: 0x000684DF
			public override bool IsThereLordSolution
			{
				get
				{
					return false;
				}
			}

			// Token: 0x17000162 RID: 354
			// (get) Token: 0x06001011 RID: 4113 RVA: 0x0006A2E2 File Offset: 0x000684E2
			protected override int RewardGold
			{
				get
				{
					return 500 + MathF.Round(1200f * base.IssueDifficultyMultiplier);
				}
			}

			// Token: 0x17000163 RID: 355
			// (get) Token: 0x06001012 RID: 4114 RVA: 0x0006A2FB File Offset: 0x000684FB
			public override int AlternativeSolutionBaseNeededMenCount
			{
				get
				{
					return 2 + MathF.Ceiling(4f * base.IssueDifficultyMultiplier);
				}
			}

			// Token: 0x17000164 RID: 356
			// (get) Token: 0x06001013 RID: 4115 RVA: 0x0006A310 File Offset: 0x00068510
			protected override int AlternativeSolutionBaseDurationInDaysInternal
			{
				get
				{
					return 4 + MathF.Ceiling(5f * base.IssueDifficultyMultiplier);
				}
			}

			// Token: 0x17000165 RID: 357
			// (get) Token: 0x06001014 RID: 4116 RVA: 0x0006A325 File Offset: 0x00068525
			protected override int CompanionSkillRewardXP
			{
				get
				{
					return (int)(500f + 1000f * base.IssueDifficultyMultiplier);
				}
			}

			// Token: 0x06001015 RID: 4117 RVA: 0x0006A33A File Offset: 0x0006853A
			public NotableWantsDaughterFoundIssue(Hero issueOwner)
				: base(issueOwner, CampaignTime.DaysFromNow(30f))
			{
			}

			// Token: 0x06001016 RID: 4118 RVA: 0x0006A34D File Offset: 0x0006854D
			protected override float GetIssueEffectAmountInternal(IssueEffect issueEffect)
			{
				if (issueEffect == DefaultIssueEffects.IssueOwnerPower)
				{
					return -0.1f;
				}
				return 0f;
			}

			// Token: 0x17000166 RID: 358
			// (get) Token: 0x06001017 RID: 4119 RVA: 0x0006A364 File Offset: 0x00068564
			public override TextObject IssueBriefByIssueGiver
			{
				get
				{
					TextObject textObject = new TextObject("{=x9VgLEzi}Yes... I've suffered a great misfortune. [ib:demure][if:convo_shocked]My daughter, a headstrong girl, has been bewitched by this never-do-well. I told her to stop seeing him but she wouldn't listen! Now she's missing - I'm sure she's been abducted by him! I'm offering a bounty of {BASE_REWARD_GOLD}{GOLD_ICON} to anyone who brings her back. Please {?PLAYER.GENDER}ma'am{?}sir{\\?}! Don't let a father's heart be broken.", null);
					StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject, false);
					textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
					textObject.SetTextVariable("BASE_REWARD_GOLD", this.RewardGold);
					return textObject;
				}
			}

			// Token: 0x17000167 RID: 359
			// (get) Token: 0x06001018 RID: 4120 RVA: 0x0006A3B3 File Offset: 0x000685B3
			public override TextObject IssueAcceptByPlayer
			{
				get
				{
					return new TextObject("{=35w6g8gM}Tell me more. What's wrong with the man? ", null);
				}
			}

			// Token: 0x17000168 RID: 360
			// (get) Token: 0x06001019 RID: 4121 RVA: 0x0006A3C0 File Offset: 0x000685C0
			public override TextObject IssueQuestSolutionExplanationByIssueGiver
			{
				get
				{
					return new TextObject("{=IY5b9vZV}Everything is wrong. [if:convo_annoyed]He is from a low family, the kind who is always involved in some land fraud scheme, or seen dealing with known bandits. Every village has a black sheep like that but I never imagined he would get his hooks into my daughter!", null);
				}
			}

			// Token: 0x17000169 RID: 361
			// (get) Token: 0x0600101A RID: 4122 RVA: 0x0006A3CD File Offset: 0x000685CD
			public override TextObject IssueAlternativeSolutionExplanationByIssueGiver
			{
				get
				{
					TextObject textObject = new TextObject("{=v0XsM7Zz}If you send your best tracker with a few men, I am sure they will find my girl [if:convo_pondering]and be back to you in no more than {ALTERNATIVE_SOLUTION_WAIT_DAYS} days.", null);
					textObject.SetTextVariable("ALTERNATIVE_SOLUTION_WAIT_DAYS", base.GetTotalAlternativeSolutionDurationInDays());
					return textObject;
				}
			}

			// Token: 0x1700016A RID: 362
			// (get) Token: 0x0600101B RID: 4123 RVA: 0x0006A3EC File Offset: 0x000685EC
			public override TextObject IssuePlayerResponseAfterAlternativeExplanation
			{
				get
				{
					return new TextObject("{=Ldp6ckgj}Don't worry, either I or one of my companions should be able to find her and see what's going on.", null);
				}
			}

			// Token: 0x1700016B RID: 363
			// (get) Token: 0x0600101C RID: 4124 RVA: 0x0006A3F9 File Offset: 0x000685F9
			public override TextObject IssueQuestSolutionAcceptByPlayer
			{
				get
				{
					return new TextObject("{=uYrxCtDa}I should be able to find her and see what's going on.", null);
				}
			}

			// Token: 0x1700016C RID: 364
			// (get) Token: 0x0600101D RID: 4125 RVA: 0x0006A406 File Offset: 0x00068606
			public override TextObject IssueAlternativeSolutionAcceptByPlayer
			{
				get
				{
					TextObject textObject = new TextObject("{=WSrGHkal}I will have one of my trackers and {REQUIRED_TROOP_AMOUNT} of my men to find your daughter.", null);
					textObject.SetTextVariable("REQUIRED_TROOP_AMOUNT", base.GetTotalAlternativeSolutionNeededMenCount());
					return textObject;
				}
			}

			// Token: 0x1700016D RID: 365
			// (get) Token: 0x0600101E RID: 4126 RVA: 0x0006A425 File Offset: 0x00068625
			public override TextObject IssueDiscussAlternativeSolution
			{
				get
				{
					return new TextObject("{=mBPcZddA}{?PLAYER.GENDER}Madam{?}Sir{\\?}, we are still waiting [ib:demure][if:convo_undecided_open]for your men to bring my daughter back. I pray for their success.", null);
				}
			}

			// Token: 0x1700016E RID: 366
			// (get) Token: 0x0600101F RID: 4127 RVA: 0x0006A434 File Offset: 0x00068634
			public override TextObject IssueAlternativeSolutionResponseByIssueGiver
			{
				get
				{
					TextObject textObject = new TextObject("{=Hhd3KaKu}Thank you, my {?PLAYER.GENDER}lady{?}lord{\\?}. If your men can find my girl and bring her back to me, I will be so grateful.[if:convo_happy] I will pay you {BASE_REWARD_GOLD}{GOLD_ICON} for your trouble.", null);
					textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
					textObject.SetTextVariable("BASE_REWARD_GOLD", this.RewardGold);
					StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject, false);
					return textObject;
				}
			}

			// Token: 0x1700016F RID: 367
			// (get) Token: 0x06001020 RID: 4128 RVA: 0x0006A484 File Offset: 0x00068684
			protected override TextObject AlternativeSolutionStartLog
			{
				get
				{
					TextObject textObject = new TextObject("{=6OmbzoBs}{ISSUE_GIVER.LINK}, a merchant from {ISSUE_GIVER_SETTLEMENT}, has told you that {?ISSUE_GIVER.GENDER}her{?}his{\\?} daughter has gone missing. You choose {COMPANION.LINK} and {REQUIRED_TROOP_AMOUNT} men to search for her and bring her back. You expect them to complete this task and return in {ALTERNATIVE_SOLUTION_DAYS} days.", null);
					textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
					textObject.SetTextVariable("BASE_REWARD_GOLD", this.RewardGold);
					textObject.SetTextVariable("ISSUE_GIVER_SETTLEMENT", base.IssueOwner.CurrentSettlement.Name);
					textObject.SetTextVariable("REQUIRED_TROOP_AMOUNT", this.AlternativeSolutionSentTroops.TotalManCount - 1);
					textObject.SetTextVariable("ALTERNATIVE_SOLUTION_DAYS", base.GetTotalAlternativeSolutionDurationInDays());
					StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject, false);
					StringHelpers.SetCharacterProperties("ISSUE_GIVER", base.IssueOwner.CharacterObject, textObject, false);
					StringHelpers.SetCharacterProperties("COMPANION", base.AlternativeSolutionHero.CharacterObject, textObject, false);
					return textObject;
				}
			}

			// Token: 0x06001021 RID: 4129 RVA: 0x0006A54C File Offset: 0x0006874C
			protected override void AlternativeSolutionEndWithSuccessConsequence()
			{
				this.ApplySuccessRewards();
				float randomFloat = MBRandom.RandomFloat;
				SkillObject skill;
				if (randomFloat <= 0.33f)
				{
					skill = DefaultSkills.OneHanded;
				}
				else if (randomFloat <= 0.66f)
				{
					skill = DefaultSkills.TwoHanded;
				}
				else
				{
					skill = DefaultSkills.Polearm;
				}
				base.AlternativeSolutionHero.AddSkillXp(skill, (float)((int)(500f + 1000f * base.IssueDifficultyMultiplier)));
			}

			// Token: 0x06001022 RID: 4130 RVA: 0x0006A5B0 File Offset: 0x000687B0
			protected override void AlternativeSolutionEndWithFailureConsequence()
			{
				this.RelationshipChangeWithIssueOwner = -10;
				if (base.IssueOwner.CurrentSettlement.Village.Bound != null)
				{
					base.IssueOwner.CurrentSettlement.Village.Bound.Town.Prosperity -= 5f;
					base.IssueOwner.CurrentSettlement.Village.Bound.Town.Security -= 5f;
				}
			}

			// Token: 0x17000170 RID: 368
			// (get) Token: 0x06001023 RID: 4131 RVA: 0x0006A634 File Offset: 0x00068834
			public override TextObject IssueAlternativeSolutionSuccessLog
			{
				get
				{
					TextObject textObject = new TextObject("{=MaXA5HJi}Your companions report that the {ISSUE_GIVER.LINK}'s daughter returns to {?ISSUE_GIVER.GENDER}her{?}him{\\?} safe and sound. {?ISSUE_GIVER.GENDER}She{?}He{\\?} is happy and sends {?ISSUE_GIVER.GENDER}her{?}his{\\?} regards with a large pouch of {BASE_REWARD_GOLD}{GOLD_ICON}.", null);
					StringHelpers.SetCharacterProperties("ISSUE_GIVER", base.IssueOwner.CharacterObject, textObject, false);
					textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
					textObject.SetTextVariable("BASE_REWARD_GOLD", this.RewardGold);
					return textObject;
				}
			}

			// Token: 0x06001024 RID: 4132 RVA: 0x0006A68C File Offset: 0x0006888C
			private void ApplySuccessRewards()
			{
				GainRenownAction.Apply(Hero.MainHero, 2f, false);
				base.IssueOwner.AddPower(10f);
				this.RelationshipChangeWithIssueOwner = 10;
				if (base.IssueOwner.CurrentSettlement.Village.Bound != null)
				{
					base.IssueOwner.CurrentSettlement.Village.Bound.Town.Security += 10f;
				}
			}

			// Token: 0x17000171 RID: 369
			// (get) Token: 0x06001025 RID: 4133 RVA: 0x0006A704 File Offset: 0x00068904
			public override TextObject Title
			{
				get
				{
					TextObject textObject = new TextObject("{=kr68V5pm}{ISSUE_GIVER.NAME} Wants {?ISSUE_GIVER.GENDER}Her{?}His{\\?} Daughter Found", null);
					StringHelpers.SetCharacterProperties("ISSUE_GIVER", base.IssueOwner.CharacterObject, textObject, false);
					return textObject;
				}
			}

			// Token: 0x17000172 RID: 370
			// (get) Token: 0x06001026 RID: 4134 RVA: 0x0006A738 File Offset: 0x00068938
			public override TextObject Description
			{
				get
				{
					TextObject textObject = new TextObject("{=SkzM5eSv}{ISSUE_GIVER.LINK}'s daughter is missing. {?ISSUE_GIVER.GENDER}She{?}He{\\?} is offering a substantial reward to find the young woman and bring her back safely.", null);
					StringHelpers.SetCharacterProperties("ISSUE_GIVER", base.IssueOwner.CharacterObject, textObject, false);
					return textObject;
				}
			}

			// Token: 0x17000173 RID: 371
			// (get) Token: 0x06001027 RID: 4135 RVA: 0x0006A76C File Offset: 0x0006896C
			public override TextObject IssueAsRumorInSettlement
			{
				get
				{
					TextObject textObject = new TextObject("{=7RyXSkEE}Wouldn't want to be the poor lovesick sap who ran off with {QUEST_GIVER.NAME}'s daughter.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.IssueOwner.CharacterObject, textObject, false);
					return textObject;
				}
			}

			// Token: 0x06001028 RID: 4136 RVA: 0x0006A79E File Offset: 0x0006899E
			protected override void OnGameLoad()
			{
			}

			// Token: 0x06001029 RID: 4137 RVA: 0x0006A7A0 File Offset: 0x000689A0
			protected override void HourlyTick()
			{
			}

			// Token: 0x0600102A RID: 4138 RVA: 0x0006A7A2 File Offset: 0x000689A2
			protected override QuestBase GenerateIssueQuest(string questId)
			{
				return new NotableWantsDaughterFoundIssueBehavior.NotableWantsDaughterFoundIssueQuest(questId, base.IssueOwner, CampaignTime.DaysFromNow(19f), this.RewardGold, base.IssueDifficultyMultiplier);
			}

			// Token: 0x0600102B RID: 4139 RVA: 0x0006A7C6 File Offset: 0x000689C6
			public override IssueBase.IssueFrequency GetFrequency()
			{
				return IssueBase.IssueFrequency.Rare;
			}

			// Token: 0x0600102C RID: 4140 RVA: 0x0006A7C9 File Offset: 0x000689C9
			public override ValueTuple<SkillObject, int> GetAlternativeSolutionSkill(Hero hero)
			{
				return new ValueTuple<SkillObject, int>((hero.GetSkillValue(DefaultSkills.Charm) >= hero.GetSkillValue(DefaultSkills.Scouting)) ? DefaultSkills.Charm : DefaultSkills.Scouting, 120);
			}

			// Token: 0x0600102D RID: 4141 RVA: 0x0006A7F6 File Offset: 0x000689F6
			public override bool AlternativeSolutionCondition(out TextObject explanation)
			{
				return QuestHelper.CheckRosterForAlternativeSolution(MobileParty.MainParty.MemberRoster, base.GetTotalAlternativeSolutionNeededMenCount(), out explanation, 2, false);
			}

			// Token: 0x0600102E RID: 4142 RVA: 0x0006A810 File Offset: 0x00068A10
			public override bool DoTroopsSatisfyAlternativeSolution(TroopRoster troopRoster, out TextObject explanation)
			{
				return QuestHelper.CheckRosterForAlternativeSolution(troopRoster, base.GetTotalAlternativeSolutionNeededMenCount(), out explanation, 2, false);
			}

			// Token: 0x0600102F RID: 4143 RVA: 0x0006A821 File Offset: 0x00068A21
			public override bool IsTroopTypeNeededByAlternativeSolution(CharacterObject character)
			{
				return character.Tier >= 2;
			}

			// Token: 0x06001030 RID: 4144 RVA: 0x0006A830 File Offset: 0x00068A30
			protected override bool CanPlayerTakeQuestConditions(Hero issueGiver, out IssueBase.PreconditionFlags flag, out Hero relationHero, out SkillObject skill)
			{
				bool flag2 = issueGiver.GetRelationWithPlayer() >= -10f && !issueGiver.CurrentSettlement.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction);
				flag = (flag2 ? IssueBase.PreconditionFlags.None : ((!issueGiver.CurrentSettlement.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction)) ? IssueBase.PreconditionFlags.Relation : IssueBase.PreconditionFlags.AtWar));
				relationHero = issueGiver;
				skill = null;
				return flag2;
			}

			// Token: 0x06001031 RID: 4145 RVA: 0x0006A89C File Offset: 0x00068A9C
			public override bool IssueStayAliveConditions()
			{
				return !base.IssueOwner.CurrentSettlement.IsRaided && !base.IssueOwner.CurrentSettlement.IsUnderRaid;
			}

			// Token: 0x06001032 RID: 4146 RVA: 0x0006A8C5 File Offset: 0x00068AC5
			protected override void CompleteIssueWithTimedOutConsequences()
			{
			}

			// Token: 0x06001033 RID: 4147 RVA: 0x0006A8C7 File Offset: 0x00068AC7
			internal static void AutoGeneratedStaticCollectObjectsNotableWantsDaughterFoundIssue(object o, List<object> collectedObjects)
			{
				((NotableWantsDaughterFoundIssueBehavior.NotableWantsDaughterFoundIssue)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
			}

			// Token: 0x06001034 RID: 4148 RVA: 0x0006A8D5 File Offset: 0x00068AD5
			protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
			{
				base.AutoGeneratedInstanceCollectObjects(collectedObjects);
			}

			// Token: 0x04000813 RID: 2067
			private const int TroopTierForAlternativeSolution = 2;

			// Token: 0x04000814 RID: 2068
			private const int RequiredSkillLevelForAlternativeSolution = 120;
		}

		// Token: 0x020001BE RID: 446
		public class NotableWantsDaughterFoundIssueQuest : QuestBase
		{
			// Token: 0x17000174 RID: 372
			// (get) Token: 0x06001035 RID: 4149 RVA: 0x0006A8E0 File Offset: 0x00068AE0
			public override TextObject Title
			{
				get
				{
					TextObject textObject = new TextObject("{=kr68V5pm}{ISSUE_GIVER.NAME} Wants {?ISSUE_GIVER.GENDER}Her{?}His{\\?} Daughter Found", null);
					StringHelpers.SetCharacterProperties("ISSUE_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					return textObject;
				}
			}

			// Token: 0x17000175 RID: 373
			// (get) Token: 0x06001036 RID: 4150 RVA: 0x0006A912 File Offset: 0x00068B12
			public override bool IsRemainingTimeHidden
			{
				get
				{
					return false;
				}
			}

			// Token: 0x17000176 RID: 374
			// (get) Token: 0x06001037 RID: 4151 RVA: 0x0006A915 File Offset: 0x00068B15
			private bool DoesMainPartyHasEnoughScoutingSkill
			{
				get
				{
					return (float)MobilePartyHelper.GetMainPartySkillCounsellor(DefaultSkills.Scouting).GetSkillValue(DefaultSkills.Scouting) >= 150f * this._questDifficultyMultiplier;
				}
			}

			// Token: 0x17000177 RID: 375
			// (get) Token: 0x06001038 RID: 4152 RVA: 0x0006A940 File Offset: 0x00068B40
			private TextObject PlayerStartsQuestLogText
			{
				get
				{
					TextObject textObject = new TextObject("{=1jExD58d}{QUEST_GIVER.LINK}, a merchant from {SETTLEMENT_NAME}, told you that {?QUEST_GIVER.GENDER}her{?}his{\\?} daughter {TARGET_HERO.NAME} has either been abducted or run off with a local rogue. You have agreed to search for her and bring her back to {SETTLEMENT_NAME}. If you cannot find their tracks when you exit settlement, you should visit the nearby villages of {SETTLEMENT_NAME} to look for clues and tracks of the kidnapper.", null);
					textObject.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, false);
					textObject.SetCharacterProperties("TARGET_HERO", this._daughterHero.CharacterObject, false);
					textObject.SetTextVariable("SETTLEMENT_NAME", base.QuestGiver.CurrentSettlement.EncyclopediaLinkWithName);
					textObject.SetTextVariable("BASE_REWARD_GOLD", this.RewardGold);
					textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
					return textObject;
				}
			}

			// Token: 0x17000178 RID: 376
			// (get) Token: 0x06001039 RID: 4153 RVA: 0x0006A9C8 File Offset: 0x00068BC8
			private TextObject SuccessQuestLogText
			{
				get
				{
					TextObject textObject = new TextObject("{=asVE53ac}Daughter returns to {QUEST_GIVER.LINK}. {?QUEST_GIVER.GENDER}She{?}He{\\?} is happy. Sends {?QUEST_GIVER.GENDER}her{?}his{\\?} regards with a large pouch of {BASE_REWARD}{GOLD_ICON}.", null);
					textObject.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, false);
					textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
					textObject.SetTextVariable("BASE_REWARD", this.RewardGold);
					return textObject;
				}
			}

			// Token: 0x17000179 RID: 377
			// (get) Token: 0x0600103A RID: 4154 RVA: 0x0006AA1C File Offset: 0x00068C1C
			private TextObject PlayerDefeatedByRogueLogText
			{
				get
				{
					TextObject textObject = new TextObject("{=i1sth9Ls}You were defeated by the rogue. He and {TARGET_HERO.NAME} ran off while you were unconscious. You failed to bring the daughter back to her {?QUEST_GIVER.GENDER}mother{?}father{\\?} as promised to {QUEST_GIVER.LINK}. {?QUEST_GIVER.GENDER}She{?}He{\\?} is furious.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					StringHelpers.SetCharacterProperties("TARGET_HERO", this._daughterHero.CharacterObject, textObject, false);
					return textObject;
				}
			}

			// Token: 0x1700017A RID: 378
			// (get) Token: 0x0600103B RID: 4155 RVA: 0x0006AA66 File Offset: 0x00068C66
			private TextObject FailQuestLogText
			{
				get
				{
					TextObject textObject = new TextObject("{=ak2EMWWR}You failed to bring the daughter back to her {?QUEST_GIVER.GENDER}mother{?}father{\\?} as promised to {QUEST_GIVER.LINK}. {QUEST_GIVER.LINK} is furious", null);
					textObject.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, false);
					return textObject;
				}
			}

			// Token: 0x1700017B RID: 379
			// (get) Token: 0x0600103C RID: 4156 RVA: 0x0006AA8A File Offset: 0x00068C8A
			private TextObject QuestCanceledWarDeclaredLog
			{
				get
				{
					TextObject textObject = new TextObject("{=vW6kBki9}Your clan is now at war with {QUEST_GIVER.LINK}'s realm. Your agreement with {QUEST_GIVER.LINK} is canceled.", null);
					textObject.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, false);
					return textObject;
				}
			}

			// Token: 0x1700017C RID: 380
			// (get) Token: 0x0600103D RID: 4157 RVA: 0x0006AAAE File Offset: 0x00068CAE
			private TextObject PlayerDeclaredWarQuestLogText
			{
				get
				{
					TextObject textObject = new TextObject("{=bqeWVVEE}Your actions have started a war with {QUEST_GIVER.LINK}'s faction. {?QUEST_GIVER.GENDER}She{?}He{\\?} cancels your agreement and the quest is a failure.", null);
					textObject.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, false);
					return textObject;
				}
			}

			// Token: 0x1700017D RID: 381
			// (get) Token: 0x0600103E RID: 4158 RVA: 0x0006AAD2 File Offset: 0x00068CD2
			private TextObject VillageRaidedCancelQuestLogText
			{
				get
				{
					TextObject textObject = new TextObject("{=aN85Kfnq}{SETTLEMENT} was raided. Your agreement with {QUEST_GIVER.LINK} is canceled.", null);
					textObject.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, false);
					textObject.SetTextVariable("SETTLEMENT", base.QuestGiver.CurrentSettlement.EncyclopediaLinkWithName);
					return textObject;
				}
			}

			// Token: 0x0600103F RID: 4159 RVA: 0x0006AB14 File Offset: 0x00068D14
			public NotableWantsDaughterFoundIssueQuest(string questId, Hero questGiver, CampaignTime duration, int baseReward, float issueDifficultyMultiplier)
				: base(questId, questGiver, duration, baseReward)
			{
				this._questDifficultyMultiplier = issueDifficultyMultiplier;
				this._targetVillage = questGiver.CurrentSettlement.Village.Bound.BoundVillages.GetRandomElementWithPredicate((Village x) => x != questGiver.CurrentSettlement.Village);
				Dictionary<string, CharacterObject> rogueCharacterBasedOnCulture = this._rogueCharacterBasedOnCulture;
				string key = "khuzait";
				Clan clan = Clan.BanditFactions.FirstOrDefault((Clan x) => x.StringId == "steppe_bandits");
				rogueCharacterBasedOnCulture.Add(key, (clan != null) ? clan.Culture.BanditBoss : null);
				Dictionary<string, CharacterObject> rogueCharacterBasedOnCulture2 = this._rogueCharacterBasedOnCulture;
				string key2 = "vlandia";
				Clan clan2 = Clan.BanditFactions.FirstOrDefault((Clan x) => x.StringId == "mountain_bandits");
				rogueCharacterBasedOnCulture2.Add(key2, (clan2 != null) ? clan2.Culture.BanditBoss : null);
				Dictionary<string, CharacterObject> rogueCharacterBasedOnCulture3 = this._rogueCharacterBasedOnCulture;
				string key3 = "aserai";
				Clan clan3 = Clan.BanditFactions.FirstOrDefault((Clan x) => x.StringId == "desert_bandits");
				rogueCharacterBasedOnCulture3.Add(key3, (clan3 != null) ? clan3.Culture.BanditBoss : null);
				Dictionary<string, CharacterObject> rogueCharacterBasedOnCulture4 = this._rogueCharacterBasedOnCulture;
				string key4 = "battania";
				Clan clan4 = Clan.BanditFactions.FirstOrDefault((Clan x) => x.StringId == "forest_bandits");
				rogueCharacterBasedOnCulture4.Add(key4, (clan4 != null) ? clan4.Culture.BanditBoss : null);
				Dictionary<string, CharacterObject> rogueCharacterBasedOnCulture5 = this._rogueCharacterBasedOnCulture;
				string key5 = "sturgia";
				Clan clan5 = Clan.BanditFactions.FirstOrDefault((Clan x) => x.StringId == "sea_raiders");
				rogueCharacterBasedOnCulture5.Add(key5, (clan5 != null) ? clan5.Culture.BanditBoss : null);
				Dictionary<string, CharacterObject> rogueCharacterBasedOnCulture6 = this._rogueCharacterBasedOnCulture;
				string key6 = "empire_w";
				Clan clan6 = Clan.BanditFactions.FirstOrDefault((Clan x) => x.StringId == "mountain_bandits");
				rogueCharacterBasedOnCulture6.Add(key6, (clan6 != null) ? clan6.Culture.BanditBoss : null);
				Dictionary<string, CharacterObject> rogueCharacterBasedOnCulture7 = this._rogueCharacterBasedOnCulture;
				string key7 = "empire_s";
				Clan clan7 = Clan.BanditFactions.FirstOrDefault((Clan x) => x.StringId == "mountain_bandits");
				rogueCharacterBasedOnCulture7.Add(key7, (clan7 != null) ? clan7.Culture.BanditBoss : null);
				Dictionary<string, CharacterObject> rogueCharacterBasedOnCulture8 = this._rogueCharacterBasedOnCulture;
				string key8 = "empire";
				Clan clan8 = Clan.BanditFactions.FirstOrDefault((Clan x) => x.StringId == "mountain_bandits");
				rogueCharacterBasedOnCulture8.Add(key8, (clan8 != null) ? clan8.Culture.BanditBoss : null);
				Dictionary<string, CharacterObject> rogueCharacterBasedOnCulture9 = this._rogueCharacterBasedOnCulture;
				string key9 = "nord";
				Clan clan9 = Clan.BanditFactions.FirstOrDefault((Clan x) => x.StringId == "sea_raiders");
				rogueCharacterBasedOnCulture9.Add(key9, (clan9 != null) ? clan9.Culture.BanditBoss : null);
				int heroComesOfAge = Campaign.Current.Models.AgeModel.HeroComesOfAge;
				int age = MBRandom.RandomInt(heroComesOfAge, 25);
				int age2 = MBRandom.RandomInt(heroComesOfAge, 25);
				CharacterObject randomCompanionTemplateWithPredicate = CharacterHelper.GetRandomCompanionTemplateWithPredicate((CharacterObject x) => x.IsFemale && x.Culture == questGiver.CurrentSettlement.Culture);
				this._daughterHero = HeroCreator.CreateSpecialHero(randomCompanionTemplateWithPredicate, questGiver.HomeSettlement, questGiver.Clan, null, age);
				this._daughterHero.HiddenInEncyclopedia = true;
				this._daughterHero.Father = questGiver;
				this._rogueHero = HeroCreator.CreateSpecialHero(this.GetRogueCharacterBasedOnCulture(questGiver.Culture.StringId), questGiver.HomeSettlement, questGiver.Clan, null, age2);
				this._rogueHero.Culture = questGiver.Culture;
				this._rogueHero.HiddenInEncyclopedia = true;
				this.SetDialogs();
				base.InitializeQuestOnCreation();
			}

			// Token: 0x06001040 RID: 4160 RVA: 0x0006AF10 File Offset: 0x00069110
			private CharacterObject GetRogueCharacterBasedOnCulture(string cultureStrId)
			{
				CharacterObject result;
				if (this._rogueCharacterBasedOnCulture.ContainsKey(cultureStrId))
				{
					result = this._rogueCharacterBasedOnCulture[cultureStrId];
				}
				else
				{
					result = base.QuestGiver.CurrentSettlement.Culture.NotableTemplates.GetRandomElementWithPredicate((CharacterObject x) => x.Occupation == Occupation.GangLeader && !x.IsFemale);
				}
				return result;
			}

			// Token: 0x06001041 RID: 4161 RVA: 0x0006AF78 File Offset: 0x00069178
			protected override void SetDialogs()
			{
				TextObject textObject = new TextObject("{=PZq1EMcx}Thank you for your help. I am still very worried about my girl {TARGET_HERO.FIRSTNAME}. Please find her and bring her back to me as soon as you can.[if:convo_worried]", null);
				StringHelpers.SetCharacterProperties("TARGET_HERO", this._daughterHero.CharacterObject, textObject, false);
				TextObject npcText = new TextObject("{=sglD6abb}Please! Bring my daughter back.", null);
				TextObject npcText2 = new TextObject("{=ddEu5IFQ}I hope so.", null);
				TextObject npcText3 = new TextObject("{=IdKG3IaS}Good to hear that.", null);
				TextObject text = new TextObject("{=0hXofVLx}Don't worry I'll bring her.", null);
				TextObject text2 = new TextObject("{=zpqP5LsC}I'll go right away.", null);
				this.OfferDialogFlow = DialogFlow.CreateDialogFlow("issue_classic_quest_start", 100).NpcLine(textObject, null, null, null, null).Condition(() => Hero.OneToOneConversationHero == base.QuestGiver && !this._didPlayerBeatRouge)
					.Consequence(new ConversationSentence.OnConsequenceDelegate(this.QuestAcceptedConsequences))
					.CloseDialog();
				this.DiscussDialogFlow = DialogFlow.CreateDialogFlow("quest_discuss", 100).NpcLine(npcText, null, null, null, null).Condition(() => Hero.OneToOneConversationHero == base.QuestGiver && !this._didPlayerBeatRouge)
					.BeginPlayerOptions(null, false)
					.PlayerOption(text, null, null, null)
					.NpcLine(npcText2, null, null, null, null)
					.CloseDialog()
					.PlayerOption(text2, null, null, null)
					.NpcLine(npcText3, null, null, null, null)
					.CloseDialog();
				Campaign.Current.ConversationManager.AddDialogFlow(this.GetRougeDialogFlow(), this);
				Campaign.Current.ConversationManager.AddDialogFlow(this.GetDaughterAfterFightDialog(), this);
				Campaign.Current.ConversationManager.AddDialogFlow(this.GetDaughterAfterAcceptDialog(), this);
				Campaign.Current.ConversationManager.AddDialogFlow(this.GetDaughterAfterPersuadedDialog(), this);
				Campaign.Current.ConversationManager.AddDialogFlow(this.GetDaughterDialogWhenVillageRaid(), this);
				Campaign.Current.ConversationManager.AddDialogFlow(this.GetRougeAfterAcceptDialog(), this);
				Campaign.Current.ConversationManager.AddDialogFlow(this.GetRogueAfterPersuadedDialog(), this);
			}

			// Token: 0x06001042 RID: 4162 RVA: 0x0006B12A File Offset: 0x0006932A
			protected override void InitializeQuestOnGameLoad()
			{
				this.SetDialogs();
				if (this._daughterHero != null)
				{
					this._daughterHero.HiddenInEncyclopedia = true;
				}
				if (this._rogueHero != null)
				{
					this._rogueHero.HiddenInEncyclopedia = true;
				}
			}

			// Token: 0x06001043 RID: 4163 RVA: 0x0006B15A File Offset: 0x0006935A
			protected override void HourlyTick()
			{
			}

			// Token: 0x06001044 RID: 4164 RVA: 0x0006B15C File Offset: 0x0006935C
			private bool IsRougeHero(IAgent agent)
			{
				return agent.Character == this._rogueHero.CharacterObject;
			}

			// Token: 0x06001045 RID: 4165 RVA: 0x0006B171 File Offset: 0x00069371
			private bool IsDaughterHero(IAgent agent)
			{
				return agent.Character == this._daughterHero.CharacterObject;
			}

			// Token: 0x06001046 RID: 4166 RVA: 0x0006B186 File Offset: 0x00069386
			private bool IsMainHero(IAgent agent)
			{
				return agent.Character == CharacterObject.PlayerCharacter;
			}

			// Token: 0x06001047 RID: 4167 RVA: 0x0006B198 File Offset: 0x00069398
			private bool multi_character_conversation_on_condition()
			{
				if (!this._villageIsRaidedTalkWithDaughter && !this._isDaughterPersuaded && !this._didPlayerBeatRouge && !this._acceptedDaughtersEscape && this._isQuestTargetMission && (CharacterObject.OneToOneConversationCharacter == this._daughterHero.CharacterObject || CharacterObject.OneToOneConversationCharacter == this._rogueHero.CharacterObject))
				{
					MBList<Agent> agents = new MBList<Agent>();
					foreach (Agent agent in Mission.Current.GetNearbyAgents(Agent.Main.Position.AsVec2, 100f, agents))
					{
						if (agent.Character == this._daughterHero.CharacterObject)
						{
							this._daughterAgent = agent;
							if (Mission.Current.GetMissionBehavior<MissionConversationLogic>() != null && Hero.OneToOneConversationHero != this._daughterHero)
							{
								Campaign.Current.ConversationManager.AddConversationAgents(new List<Agent> { this._daughterAgent }, true);
							}
						}
						else if (agent.Character == this._rogueHero.CharacterObject)
						{
							this._rogueAgent = agent;
							if (Mission.Current.GetMissionBehavior<MissionConversationLogic>() != null && Hero.OneToOneConversationHero != this._rogueHero)
							{
								Campaign.Current.ConversationManager.AddConversationAgents(new List<Agent> { this._rogueAgent }, true);
							}
						}
					}
					return this._daughterAgent != null && this._rogueAgent != null && this._daughterAgent.Health > 10f && this._rogueAgent.Health > 10f;
				}
				return false;
			}

			// Token: 0x06001048 RID: 4168 RVA: 0x0006B350 File Offset: 0x00069550
			private bool daughter_conversation_after_fight_on_condition()
			{
				return CharacterObject.OneToOneConversationCharacter == this._daughterHero.CharacterObject && this._didPlayerBeatRouge;
			}

			// Token: 0x06001049 RID: 4169 RVA: 0x0006B36C File Offset: 0x0006956C
			private void multi_agent_conversation_on_consequence()
			{
				this._task = this.GetPersuasionTask();
			}

			// Token: 0x0600104A RID: 4170 RVA: 0x0006B37C File Offset: 0x0006957C
			private DialogFlow GetRougeDialogFlow()
			{
				TextObject textObject = new TextObject("{=ovFbMMTJ}Who are you? Are you one of the bounty hunters sent by {QUEST_GIVER.LINK} to track us? Like we're animals or something? Look friend, we have done nothing wrong. As you may have figured out already, this woman and I, we love each other. I didn't force her to do anything.[ib:closed][if:convo_innocent_smile]", null);
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
				TextObject textObject2 = new TextObject("{=D25oY3j1}Thank you {?PLAYER.GENDER}lady{?}sir{\\?}. For your kindness and understanding. We won't forget this.[ib:demure][if:convo_happy]", null);
				StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject2, false);
				TextObject textObject3 = new TextObject("{=oL3amiu1}Come {DAUGHTER_NAME.NAME}, let's go before other hounds sniff our trail... I mean... No offense {?PLAYER.GENDER}madam{?}sir{\\?}.", null);
				StringHelpers.SetCharacterProperties("DAUGHTER_NAME", this._daughterHero.CharacterObject, textObject3, false);
				StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject3, false);
				TextObject textObject4 = new TextObject("{=92sbq1YY}I'm no child, {?PLAYER.GENDER}lady{?}sir{\\?}! Draw your weapon! I challenge you to a duel![ib:warrior2][if:convo_excited]", null);
				StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject4, false);
				TextObject textObject5 = new TextObject("{=jfzErupx}He is right! I ran away with him willingly. I love my {?QUEST_GIVER.GENDER}mother{?}father{\\?},[ib:closed][if:convo_grave] but {?QUEST_GIVER.GENDER}she{?}he{\\?} can be such a tyrant. Please {?PLAYER.GENDER}lady{?}sir{\\?}, if you believe in freedom and love, please leave us be.", null);
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject5, false);
				StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject5, false);
				TextObject textObject6 = new TextObject("{=5NljlbLA}Thank you kind {?PLAYER.GENDER}lady{?}sir{\\?}, thank you.", null);
				StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject6, false);
				TextObject textObject7 = new TextObject("{=i5fNZrhh}Please, {?PLAYER.GENDER}lady{?}sir{\\?}. I love him truly and I wish to spend the rest of my life with him.[ib:demure][if:convo_worried] I beg of you, please don't stand in our way.", null);
				StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject7, false);
				TextObject textObject8 = new TextObject("{=0RCdPKj2}Yes {?QUEST_GIVER.GENDER}she{?}he{\\?} would probably be sad. But not because of what you think. See, {QUEST_GIVER.LINK} promised me to one of {?QUEST_GIVER.GENDER}her{?}his{\\?} allies' sons and this will devastate {?QUEST_GIVER.GENDER}her{?}his{\\?} plans. That is true.", null);
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject8, false);
				TextObject text = new TextObject("{=5W7Kxfq9}I understand. If that is the case, I will let you go.", null);
				TextObject text2 = new TextObject("{=3XimdHOn}How do I know he's not forcing you to say that?", null);
				TextObject textObject9 = new TextObject("{=zNqDEuAw}But I've promised to find you and return you to your {?QUEST_GIVER.GENDER}mother{?}father{\\?}. {?QUEST_GIVER.GENDER}She{?}He{\\?} would be devastated.", null);
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject9, false);
				TextObject textObject10 = new TextObject("{=tuaQ5uU3}I guess the only way to free you from this pretty boy's spell is to kill him.", null);
				TextObject textObject11 = new TextObject("{=HDCmeGhG}I'm sorry but I gave a promise. I don't break my promises.", null);
				TextObject text3 = new TextObject("{=VGrHWxzf}This will be a massacre, not a duel, but I'm fine with that.", null);
				TextObject text4 = new TextObject("{=sytYViXb}I accept your duel.", null);
				DialogFlow dialogFlow = DialogFlow.CreateDialogFlow("start", 125).NpcLine(textObject, new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsRougeHero), new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsMainHero), null, null).Condition(new ConversationSentence.OnConditionDelegate(this.multi_character_conversation_on_condition))
					.Consequence(new ConversationSentence.OnConsequenceDelegate(this.multi_agent_conversation_on_consequence))
					.NpcLine(textObject5, new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsDaughterHero), new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsMainHero), null, null)
					.BeginPlayerOptions(null, false)
					.PlayerOption(text, new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsDaughterHero), null, null)
					.NpcLine(textObject2, new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsRougeHero), new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsMainHero), null, null)
					.NpcLine(textObject3, new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsRougeHero), new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsDaughterHero), null, null)
					.NpcLine(textObject6, new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsDaughterHero), new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsMainHero), null, null)
					.Consequence(delegate
					{
						Campaign.Current.ConversationManager.ConversationEndOneShot += this.PlayerAcceptedDaughtersEscape;
					})
					.CloseDialog()
					.PlayerOption(text2, new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsDaughterHero), null, null)
					.NpcLine(textObject7, new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsDaughterHero), new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsMainHero), null, null)
					.PlayerLine(textObject9, new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsDaughterHero), null, null)
					.NpcLine(textObject8, new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsDaughterHero), new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsMainHero), null, null)
					.GotoDialogState("start_daughter_persuade_to_come_persuasion")
					.GoBackToDialogState("daughter_persuade_to_come_persuasion_finished")
					.PlayerLine((Hero.MainHero.GetTraitLevel(DefaultTraits.Mercy) < 0) ? textObject10 : textObject11, new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsDaughterHero), null, null)
					.NpcLine(textObject4, new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsRougeHero), new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsMainHero), null, null)
					.BeginPlayerOptions(null, false)
					.PlayerOption(text3, new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsRougeHero), null, null)
					.NpcLine(new TextObject("{=XWVW0oTB}You bastard![ib:aggressive][if:convo_furious]", null), new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsRougeHero), null, null, null)
					.Consequence(delegate
					{
						Campaign.Current.ConversationManager.ConversationEndOneShot += this.PlayerRejectsDuelFight;
					})
					.CloseDialog()
					.PlayerOption(text4, new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsRougeHero), null, null)
					.NpcLine(new TextObject("{=jqahxjWD}Heaven protect me![ib:aggressive][if:convo_astonished]", null), new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsRougeHero), null, null, null)
					.Consequence(delegate
					{
						Campaign.Current.ConversationManager.ConversationEndOneShot += this.PlayerAcceptsDuelFight;
					})
					.CloseDialog()
					.EndPlayerOptions()
					.EndPlayerOptions()
					.CloseDialog();
				this.AddPersuasionDialogs(dialogFlow);
				return dialogFlow;
			}

			// Token: 0x0600104B RID: 4171 RVA: 0x0006B7B8 File Offset: 0x000699B8
			private DialogFlow GetDaughterAfterFightDialog()
			{
				TextObject npcText = new TextObject("{=MN2v1AZQ}I hate you! You killed him! I can't believe it! I will hate you with all my heart till my dying days.[if:convo_angry]", null);
				TextObject npcText2 = new TextObject("{=TTkVcObg}What choice do I have, you heartless bastard?![if:convo_furious]", null);
				TextObject textObject = new TextObject("{=XqsrsjiL}I did what I had to do. Pack up, you need to go.", null);
				TextObject textObject2 = new TextObject("{=KQ3aYvp3}Some day you'll see I did you a favor. Pack up, you need to go.", null);
				return DialogFlow.CreateDialogFlow("start", 125).NpcLine(npcText, null, null, null, null).Condition(new ConversationSentence.OnConditionDelegate(this.daughter_conversation_after_fight_on_condition))
					.PlayerLine((Hero.MainHero.GetTraitLevel(DefaultTraits.Mercy) < 0) ? textObject : textObject2, null, null, null)
					.NpcLine(npcText2, null, null, null, null)
					.Consequence(delegate
					{
						Campaign.Current.ConversationManager.ConversationEndOneShot += this.PlayerWonTheFight;
					})
					.CloseDialog();
			}

			// Token: 0x0600104C RID: 4172 RVA: 0x0006B85C File Offset: 0x00069A5C
			private DialogFlow GetDaughterAfterAcceptDialog()
			{
				TextObject textObject = new TextObject("{=0Wg00sfN}Thank you, {?PLAYER.GENDER}madam{?}sir{\\?}. We will be moving immediately.", null);
				StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject, false);
				TextObject playerText = new TextObject("{=kUReBc04}Good.", null);
				return DialogFlow.CreateDialogFlow("start", 125).NpcLine(textObject, null, null, null, null).Condition(new ConversationSentence.OnConditionDelegate(this.daughter_conversation_after_accept_on_condition))
					.PlayerLine(playerText, null, null, null)
					.CloseDialog();
			}

			// Token: 0x0600104D RID: 4173 RVA: 0x0006B8C8 File Offset: 0x00069AC8
			private bool daughter_conversation_after_accept_on_condition()
			{
				return CharacterObject.OneToOneConversationCharacter == this._daughterHero.CharacterObject && this._acceptedDaughtersEscape;
			}

			// Token: 0x0600104E RID: 4174 RVA: 0x0006B8E4 File Offset: 0x00069AE4
			private DialogFlow GetDaughterAfterPersuadedDialog()
			{
				TextObject textObject = new TextObject("{=B8bHpJRP}You are right, {?PLAYER.GENDER}my lady{?}sir{\\?}. I should be moving immediately.", null);
				StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject, false);
				TextObject playerText = new TextObject("{=kUReBc04}Good.", null);
				return DialogFlow.CreateDialogFlow("start", 125).NpcLine(textObject, null, null, null, null).Condition(new ConversationSentence.OnConditionDelegate(this.daughter_conversation_after_persuaded_on_condition))
					.PlayerLine(playerText, null, null, null)
					.CloseDialog();
			}

			// Token: 0x0600104F RID: 4175 RVA: 0x0006B950 File Offset: 0x00069B50
			private DialogFlow GetDaughterDialogWhenVillageRaid()
			{
				return DialogFlow.CreateDialogFlow("start", 125).NpcLine(new TextObject("{=w0HPC53e}Who are you? What do you want from me?[ib:nervous][if:convo_bared_teeth]", null), null, null, null, null).Condition(() => this._villageIsRaidedTalkWithDaughter)
					.PlayerLine(new TextObject("{=iRupMGI0}Calm down! Your father has sent me to find you.", null), null, null, null)
					.NpcLine(new TextObject("{=dwNquUNr}My father? Oh, thank god! I saw terrible things. [ib:nervous2][if:convo_shocked]They took my beloved one and slew many innocents without hesitation.", null), null, null, null, null)
					.PlayerLine("{=HtAr22re}Try to forget all about these and return to your father's house.", null, null, null)
					.NpcLine("{=FgSIsasF}Yes, you are right. I shall be on my way...", null, null, null, null)
					.Consequence(delegate
					{
						Campaign.Current.ConversationManager.ConversationEndOneShot += delegate()
						{
							this.ApplyDeliverySuccessConsequences();
							base.CompleteQuestWithSuccess();
							base.AddLog(this.SuccessQuestLogText, false);
							this._villageIsRaidedTalkWithDaughter = false;
						};
					})
					.CloseDialog();
			}

			// Token: 0x06001050 RID: 4176 RVA: 0x0006B9E6 File Offset: 0x00069BE6
			private bool daughter_conversation_after_persuaded_on_condition()
			{
				return CharacterObject.OneToOneConversationCharacter == this._daughterHero.CharacterObject && this._isDaughterPersuaded;
			}

			// Token: 0x06001051 RID: 4177 RVA: 0x0006BA04 File Offset: 0x00069C04
			private DialogFlow GetRougeAfterAcceptDialog()
			{
				TextObject textObject = new TextObject("{=wlKtDR2z}Thank you, {?PLAYER.GENDER}my lady{?}sir{\\?}.", null);
				StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject, false);
				return DialogFlow.CreateDialogFlow("start", 125).NpcLine(textObject, null, null, null, null).Condition(new ConversationSentence.OnConditionDelegate(this.rogue_conversation_after_accept_on_condition))
					.PlayerLine(new TextObject("{=0YJGvJ7o}You should leave now.", null), null, null, null)
					.NpcLine(new TextObject("{=6Q4cPOSG}Yes, we will.", null), null, null, null, null)
					.CloseDialog();
			}

			// Token: 0x06001052 RID: 4178 RVA: 0x0006BA82 File Offset: 0x00069C82
			private bool rogue_conversation_after_accept_on_condition()
			{
				return CharacterObject.OneToOneConversationCharacter == this._rogueHero.CharacterObject && this._acceptedDaughtersEscape;
			}

			// Token: 0x06001053 RID: 4179 RVA: 0x0006BAA0 File Offset: 0x00069CA0
			private DialogFlow GetRogueAfterPersuadedDialog()
			{
				TextObject textObject = new TextObject("{=GFt9KiHP}You are right. Maybe we need to persuade {QUEST_GIVER.NAME}.", null);
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
				TextObject playerText = new TextObject("{=btJkBTSF}I am sure you can solve it.", null);
				return DialogFlow.CreateDialogFlow("start", 125).NpcLine(textObject, null, null, null, null).Condition(new ConversationSentence.OnConditionDelegate(this.rogue_conversation_after_persuaded_on_condition))
					.PlayerLine(playerText, null, null, null)
					.CloseDialog();
			}

			// Token: 0x06001054 RID: 4180 RVA: 0x0006BB12 File Offset: 0x00069D12
			private bool rogue_conversation_after_persuaded_on_condition()
			{
				return CharacterObject.OneToOneConversationCharacter == this._rogueHero.CharacterObject && this._isDaughterPersuaded;
			}

			// Token: 0x06001055 RID: 4181 RVA: 0x0006BB30 File Offset: 0x00069D30
			protected override void OnTimedOut()
			{
				this.ApplyDeliveryRejectedFailConsequences();
				TextObject textObject = new TextObject("{=KAvwytDK}You didn't bring {DAUGHTER.NAME} to {QUEST_GIVER.LINK}. {?QUEST_GIVER.GENDER}she{?}he{\\?} must be furious.", null);
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
				StringHelpers.SetCharacterProperties("DAUGHTER", this._daughterHero.CharacterObject, textObject, false);
				base.AddLog(textObject, false);
			}

			// Token: 0x06001056 RID: 4182 RVA: 0x0006BB88 File Offset: 0x00069D88
			private void PlayerAcceptedDaughtersEscape()
			{
				this._acceptedDaughtersEscape = true;
			}

			// Token: 0x06001057 RID: 4183 RVA: 0x0006BB91 File Offset: 0x00069D91
			private void PlayerWonTheFight()
			{
				this._isDaughterCaptured = true;
				Mission.Current.SetMissionMode(MissionMode.StartUp, false);
			}

			// Token: 0x06001058 RID: 4184 RVA: 0x0006BBA8 File Offset: 0x00069DA8
			private void ApplyDaughtersEscapeAcceptedFailConsequences()
			{
				this.RelationshipChangeWithQuestGiver = -10;
				if (base.QuestGiver.CurrentSettlement.Village.Bound != null)
				{
					base.QuestGiver.CurrentSettlement.Village.Bound.Town.Security -= 5f;
					base.QuestGiver.CurrentSettlement.Village.Bound.Town.Prosperity -= 5f;
				}
			}

			// Token: 0x06001059 RID: 4185 RVA: 0x0006BC2C File Offset: 0x00069E2C
			private void ApplyDeliveryRejectedFailConsequences()
			{
				this.RelationshipChangeWithQuestGiver = -10;
				if (base.QuestGiver.CurrentSettlement.Village.Bound != null)
				{
					base.QuestGiver.CurrentSettlement.Village.Bound.Town.Security -= 5f;
					base.QuestGiver.CurrentSettlement.Village.Bound.Town.Prosperity -= 5f;
				}
			}

			// Token: 0x0600105A RID: 4186 RVA: 0x0006BCB0 File Offset: 0x00069EB0
			private void ApplyDeliverySuccessConsequences()
			{
				GainRenownAction.Apply(Hero.MainHero, 2f, false);
				base.QuestGiver.AddPower(10f);
				this.RelationshipChangeWithQuestGiver = 10;
				if (base.QuestGiver.CurrentSettlement.Village.Bound != null)
				{
					base.QuestGiver.CurrentSettlement.Village.Bound.Town.Security += 10f;
				}
				GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, this.RewardGold, false);
			}

			// Token: 0x0600105B RID: 4187 RVA: 0x0006BD3C File Offset: 0x00069F3C
			private void PlayerRejectsDuelFight()
			{
				this._rogueAgent = (Agent)MissionConversationLogic.Current.ConversationManager.ConversationAgents.First((IAgent x) => !x.Character.IsFemale);
				List<Agent> list = new List<Agent> { Agent.Main };
				List<Agent> opponentSideAgents = new List<Agent> { this._rogueAgent };
				MBList<Agent> agents = new MBList<Agent>();
				foreach (Agent agent in Mission.Current.GetNearbyAgents(Agent.Main.Position.AsVec2, 30f, agents))
				{
					foreach (Hero hero in Hero.MainHero.CompanionsInParty)
					{
						if (agent.Character == hero.CharacterObject)
						{
							list.Add(agent);
							break;
						}
					}
				}
				this._rogueAgent.Health = (float)(150 + list.Count * 20);
				this._rogueAgent.Defensiveness = 1f;
				Mission.Current.GetMissionBehavior<MissionFightHandler>().StartCustomFight(list, opponentSideAgents, false, false, new MissionFightHandler.OnFightEndDelegate(this.StartConversationAfterFight), float.Epsilon);
			}

			// Token: 0x0600105C RID: 4188 RVA: 0x0006BEB8 File Offset: 0x0006A0B8
			private void PlayerAcceptsDuelFight()
			{
				this._rogueAgent = (Agent)MissionConversationLogic.Current.ConversationManager.ConversationAgents.First((IAgent x) => !x.Character.IsFemale);
				List<Agent> playerSideAgents = new List<Agent> { Agent.Main };
				List<Agent> opponentSideAgents = new List<Agent> { this._rogueAgent };
				MBList<Agent> agents = new MBList<Agent>();
				foreach (Agent agent in Mission.Current.GetNearbyAgents(Agent.Main.Position.AsVec2, 30f, agents))
				{
					foreach (Hero hero in Hero.MainHero.CompanionsInParty)
					{
						if (agent.Character == hero.CharacterObject)
						{
							agent.SetTeam(Mission.Current.SpectatorTeam, false);
							DailyBehaviorGroup behaviorGroup = agent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<DailyBehaviorGroup>();
							if (behaviorGroup.GetActiveBehavior() is FollowAgentBehavior)
							{
								behaviorGroup.GetBehavior<FollowAgentBehavior>().SetTargetAgent(null);
								break;
							}
							break;
						}
					}
				}
				this._rogueAgent.Health = 200f;
				Mission.Current.GetMissionBehavior<MissionFightHandler>().StartCustomFight(playerSideAgents, opponentSideAgents, false, false, new MissionFightHandler.OnFightEndDelegate(this.StartConversationAfterFight), float.Epsilon);
			}

			// Token: 0x0600105D RID: 4189 RVA: 0x0006C054 File Offset: 0x0006A254
			private void StartConversationAfterFight(bool isPlayerSideWon)
			{
				if (isPlayerSideWon)
				{
					this._didPlayerBeatRouge = true;
					Campaign.Current.ConversationManager.SetupAndStartMissionConversation(this._daughterAgent, Mission.Current.MainAgent, false);
					TraitLevelingHelper.OnHostileAction(-50);
					return;
				}
				this._playerDefeatedByRogue = true;
			}

			// Token: 0x0600105E RID: 4190 RVA: 0x0006C090 File Offset: 0x0006A290
			private void AddPersuasionDialogs(DialogFlow dialog)
			{
				TextObject textObject = new TextObject("{=ob5SejgJ}I will not abandon my love, {?PLAYER.GENDER}lady{?}sir{\\?}!", null);
				StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject, false);
				TextObject textObject2 = new TextObject("{=cqe8FU8M}{?QUEST_GIVER.GENDER}She{?}He{\\?} cares nothing about me! Only about {?QUEST_GIVER.GENDER}her{?}his{\\?} reputation in our district.", null);
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject2, false);
				dialog.AddDialogLine("daughter_persuade_to_come_introduction", "start_daughter_persuade_to_come_persuasion", "daughter_persuade_to_come_start_reservation", textObject2.ToString(), null, new ConversationSentence.OnConsequenceDelegate(this.persuasion_start_with_daughter_on_consequence), this, 100, null, new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsDaughterHero), new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsMainHero));
				dialog.AddDialogLine("daughter_persuade_to_come_rejected", "daughter_persuade_to_come_start_reservation", "daughter_persuade_to_come_persuasion_failed", "{=!}{FAILED_PERSUASION_LINE}", new ConversationSentence.OnConditionDelegate(this.daughter_persuade_to_come_persuasion_failed_on_condition), new ConversationSentence.OnConsequenceDelegate(this.daughter_persuade_to_come_persuasion_failed_on_consequence), this, 100, null, new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsDaughterHero), new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsMainHero));
				dialog.AddDialogLine("daughter_persuade_to_come_failed", "daughter_persuade_to_come_persuasion_failed", "daughter_persuade_to_come_persuasion_finished", textObject.ToString(), null, null, this, 100, null, null, null);
				dialog.AddDialogLine("daughter_persuade_to_come_start", "daughter_persuade_to_come_start_reservation", "daughter_persuade_to_come_persuasion_select_option", "{=9b2BETct}I have already decided. Don't expect me to change my mind.", () => !this.daughter_persuade_to_come_persuasion_failed_on_condition(), null, this, 100, null, new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsDaughterHero), new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsMainHero));
				dialog.AddDialogLine("daughter_persuade_to_come_success", "daughter_persuade_to_come_start_reservation", "close_window", "{=3tmXBpRH}You're right. I cannot do this. I will return to my family. ", new ConversationSentence.OnConditionDelegate(ConversationManager.GetPersuasionProgressSatisfied), new ConversationSentence.OnConsequenceDelegate(this.daughter_persuade_to_come_persuasion_success_on_consequence), this, int.MaxValue, null, new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsDaughterHero), new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsMainHero));
				string id = "daughter_persuade_to_come_select_option_1";
				string inputToken = "daughter_persuade_to_come_persuasion_select_option";
				string outputToken = "daughter_persuade_to_come_persuasion_selected_option_response";
				string text = "{=!}{DAUGHTER_PERSUADE_TO_COME_PERSUADE_ATTEMPT_1}";
				ConversationSentence.OnConditionDelegate conditionDelegate = new ConversationSentence.OnConditionDelegate(this.persuasion_select_option_1_on_condition);
				ConversationSentence.OnConsequenceDelegate consequenceDelegate = new ConversationSentence.OnConsequenceDelegate(this.persuasion_select_option_1_on_consequence);
				ConversationSentence.OnPersuasionOptionDelegate persuasionOptionDelegate = new ConversationSentence.OnPersuasionOptionDelegate(this.persuasion_setup_option_1);
				ConversationSentence.OnClickableConditionDelegate clickableConditionDelegate = new ConversationSentence.OnClickableConditionDelegate(this.persuasion_clickable_option_1_on_condition);
				dialog.AddPlayerLine(id, inputToken, outputToken, text, conditionDelegate, consequenceDelegate, this, 100, clickableConditionDelegate, persuasionOptionDelegate, new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsMainHero), new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsDaughterHero));
				string id2 = "daughter_persuade_to_come_select_option_2";
				string inputToken2 = "daughter_persuade_to_come_persuasion_select_option";
				string outputToken2 = "daughter_persuade_to_come_persuasion_selected_option_response";
				string text2 = "{=!}{DAUGHTER_PERSUADE_TO_COME_PERSUADE_ATTEMPT_2}";
				ConversationSentence.OnConditionDelegate conditionDelegate2 = new ConversationSentence.OnConditionDelegate(this.persuasion_select_option_2_on_condition);
				ConversationSentence.OnConsequenceDelegate consequenceDelegate2 = new ConversationSentence.OnConsequenceDelegate(this.persuasion_select_option_2_on_consequence);
				persuasionOptionDelegate = new ConversationSentence.OnPersuasionOptionDelegate(this.persuasion_setup_option_2);
				clickableConditionDelegate = new ConversationSentence.OnClickableConditionDelegate(this.persuasion_clickable_option_2_on_condition);
				dialog.AddPlayerLine(id2, inputToken2, outputToken2, text2, conditionDelegate2, consequenceDelegate2, this, 100, clickableConditionDelegate, persuasionOptionDelegate, new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsMainHero), new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsDaughterHero));
				string id3 = "daughter_persuade_to_come_select_option_3";
				string inputToken3 = "daughter_persuade_to_come_persuasion_select_option";
				string outputToken3 = "daughter_persuade_to_come_persuasion_selected_option_response";
				string text3 = "{=!}{DAUGHTER_PERSUADE_TO_COME_PERSUADE_ATTEMPT_3}";
				ConversationSentence.OnConditionDelegate conditionDelegate3 = new ConversationSentence.OnConditionDelegate(this.persuasion_select_option_3_on_condition);
				ConversationSentence.OnConsequenceDelegate consequenceDelegate3 = new ConversationSentence.OnConsequenceDelegate(this.persuasion_select_option_3_on_consequence);
				persuasionOptionDelegate = new ConversationSentence.OnPersuasionOptionDelegate(this.persuasion_setup_option_3);
				clickableConditionDelegate = new ConversationSentence.OnClickableConditionDelegate(this.persuasion_clickable_option_3_on_condition);
				dialog.AddPlayerLine(id3, inputToken3, outputToken3, text3, conditionDelegate3, consequenceDelegate3, this, 100, clickableConditionDelegate, persuasionOptionDelegate, new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsMainHero), new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsDaughterHero));
				string id4 = "daughter_persuade_to_come_select_option_4";
				string inputToken4 = "daughter_persuade_to_come_persuasion_select_option";
				string outputToken4 = "daughter_persuade_to_come_persuasion_selected_option_response";
				string text4 = "{=!}{DAUGHTER_PERSUADE_TO_COME_PERSUADE_ATTEMPT_4}";
				ConversationSentence.OnConditionDelegate conditionDelegate4 = new ConversationSentence.OnConditionDelegate(this.persuasion_select_option_4_on_condition);
				ConversationSentence.OnConsequenceDelegate consequenceDelegate4 = new ConversationSentence.OnConsequenceDelegate(this.persuasion_select_option_4_on_consequence);
				persuasionOptionDelegate = new ConversationSentence.OnPersuasionOptionDelegate(this.persuasion_setup_option_4);
				clickableConditionDelegate = new ConversationSentence.OnClickableConditionDelegate(this.persuasion_clickable_option_4_on_condition);
				dialog.AddPlayerLine(id4, inputToken4, outputToken4, text4, conditionDelegate4, consequenceDelegate4, this, 100, clickableConditionDelegate, persuasionOptionDelegate, new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsMainHero), new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsDaughterHero));
				dialog.AddDialogLine("daughter_persuade_to_come_select_option_reaction", "daughter_persuade_to_come_persuasion_selected_option_response", "daughter_persuade_to_come_start_reservation", "{=D0xDRqvm}{PERSUASION_REACTION}", new ConversationSentence.OnConditionDelegate(this.persuasion_selected_option_response_on_condition), new ConversationSentence.OnConsequenceDelegate(this.persuasion_selected_option_response_on_consequence), this, 100, null, null, null);
			}

			// Token: 0x0600105F RID: 4191 RVA: 0x0006C410 File Offset: 0x0006A610
			private void persuasion_selected_option_response_on_consequence()
			{
				Tuple<PersuasionOptionArgs, PersuasionOptionResult> tuple = ConversationManager.GetPersuasionChosenOptions().Last<Tuple<PersuasionOptionArgs, PersuasionOptionResult>>();
				float difficulty = Campaign.Current.Models.PersuasionModel.GetDifficulty(PersuasionDifficulty.Hard);
				float moveToNextStageChance;
				float blockRandomOptionChance;
				Campaign.Current.Models.PersuasionModel.GetEffectChances(tuple.Item1, out moveToNextStageChance, out blockRandomOptionChance, difficulty);
				this._task.ApplyEffects(moveToNextStageChance, blockRandomOptionChance);
			}

			// Token: 0x06001060 RID: 4192 RVA: 0x0006C46C File Offset: 0x0006A66C
			private bool persuasion_selected_option_response_on_condition()
			{
				PersuasionOptionResult item = ConversationManager.GetPersuasionChosenOptions().Last<Tuple<PersuasionOptionArgs, PersuasionOptionResult>>().Item2;
				MBTextManager.SetTextVariable("PERSUASION_REACTION", PersuasionHelper.GetDefaultPersuasionOptionReaction(item), false);
				return true;
			}

			// Token: 0x06001061 RID: 4193 RVA: 0x0006C49C File Offset: 0x0006A69C
			private bool persuasion_select_option_1_on_condition()
			{
				if (this._task.Options.Count > 0)
				{
					TextObject textObject = new TextObject("{=bSo9hKwr}{PERSUASION_OPTION_LINE} {SUCCESS_CHANCE}", null);
					textObject.SetTextVariable("SUCCESS_CHANCE", PersuasionHelper.ShowSuccess(this._task.Options.ElementAt(0), false));
					textObject.SetTextVariable("PERSUASION_OPTION_LINE", this._task.Options.ElementAt(0).Line);
					MBTextManager.SetTextVariable("DAUGHTER_PERSUADE_TO_COME_PERSUADE_ATTEMPT_1", textObject, false);
					return true;
				}
				return false;
			}

			// Token: 0x06001062 RID: 4194 RVA: 0x0006C51C File Offset: 0x0006A71C
			private bool persuasion_select_option_2_on_condition()
			{
				if (this._task.Options.Count > 1)
				{
					TextObject textObject = new TextObject("{=bSo9hKwr}{PERSUASION_OPTION_LINE} {SUCCESS_CHANCE}", null);
					textObject.SetTextVariable("SUCCESS_CHANCE", PersuasionHelper.ShowSuccess(this._task.Options.ElementAt(1), false));
					textObject.SetTextVariable("PERSUASION_OPTION_LINE", this._task.Options.ElementAt(1).Line);
					MBTextManager.SetTextVariable("DAUGHTER_PERSUADE_TO_COME_PERSUADE_ATTEMPT_2", textObject, false);
					return true;
				}
				return false;
			}

			// Token: 0x06001063 RID: 4195 RVA: 0x0006C59C File Offset: 0x0006A79C
			private bool persuasion_select_option_3_on_condition()
			{
				if (this._task.Options.Count > 2)
				{
					TextObject textObject = new TextObject("{=bSo9hKwr}{PERSUASION_OPTION_LINE} {SUCCESS_CHANCE}", null);
					textObject.SetTextVariable("SUCCESS_CHANCE", PersuasionHelper.ShowSuccess(this._task.Options.ElementAt(2), false));
					textObject.SetTextVariable("PERSUASION_OPTION_LINE", this._task.Options.ElementAt(2).Line);
					MBTextManager.SetTextVariable("DAUGHTER_PERSUADE_TO_COME_PERSUADE_ATTEMPT_3", textObject, false);
					return true;
				}
				return false;
			}

			// Token: 0x06001064 RID: 4196 RVA: 0x0006C61C File Offset: 0x0006A81C
			private bool persuasion_select_option_4_on_condition()
			{
				if (this._task.Options.Count > 3)
				{
					TextObject textObject = new TextObject("{=bSo9hKwr}{PERSUASION_OPTION_LINE} {SUCCESS_CHANCE}", null);
					textObject.SetTextVariable("SUCCESS_CHANCE", PersuasionHelper.ShowSuccess(this._task.Options.ElementAt(3), false));
					textObject.SetTextVariable("PERSUASION_OPTION_LINE", this._task.Options.ElementAt(3).Line);
					MBTextManager.SetTextVariable("DAUGHTER_PERSUADE_TO_COME_PERSUADE_ATTEMPT_4", textObject, false);
					return true;
				}
				return false;
			}

			// Token: 0x06001065 RID: 4197 RVA: 0x0006C69C File Offset: 0x0006A89C
			private void persuasion_select_option_1_on_consequence()
			{
				if (this._task.Options.Count > 0)
				{
					this._task.Options[0].BlockTheOption(true);
				}
			}

			// Token: 0x06001066 RID: 4198 RVA: 0x0006C6C8 File Offset: 0x0006A8C8
			private void persuasion_select_option_2_on_consequence()
			{
				if (this._task.Options.Count > 1)
				{
					this._task.Options[1].BlockTheOption(true);
				}
			}

			// Token: 0x06001067 RID: 4199 RVA: 0x0006C6F4 File Offset: 0x0006A8F4
			private void persuasion_select_option_3_on_consequence()
			{
				if (this._task.Options.Count > 2)
				{
					this._task.Options[2].BlockTheOption(true);
				}
			}

			// Token: 0x06001068 RID: 4200 RVA: 0x0006C720 File Offset: 0x0006A920
			private void persuasion_select_option_4_on_consequence()
			{
				if (this._task.Options.Count > 3)
				{
					this._task.Options[3].BlockTheOption(true);
				}
			}

			// Token: 0x06001069 RID: 4201 RVA: 0x0006C74C File Offset: 0x0006A94C
			private PersuasionOptionArgs persuasion_setup_option_1()
			{
				return this._task.Options.ElementAt(0);
			}

			// Token: 0x0600106A RID: 4202 RVA: 0x0006C75F File Offset: 0x0006A95F
			private PersuasionOptionArgs persuasion_setup_option_2()
			{
				return this._task.Options.ElementAt(1);
			}

			// Token: 0x0600106B RID: 4203 RVA: 0x0006C772 File Offset: 0x0006A972
			private PersuasionOptionArgs persuasion_setup_option_3()
			{
				return this._task.Options.ElementAt(2);
			}

			// Token: 0x0600106C RID: 4204 RVA: 0x0006C785 File Offset: 0x0006A985
			private PersuasionOptionArgs persuasion_setup_option_4()
			{
				return this._task.Options.ElementAt(3);
			}

			// Token: 0x0600106D RID: 4205 RVA: 0x0006C798 File Offset: 0x0006A998
			private bool persuasion_clickable_option_1_on_condition(out TextObject hintText)
			{
				hintText = new TextObject("{=9ACJsI6S}Blocked", null);
				if (this._task.Options.Count > 0)
				{
					hintText = (this._task.Options.ElementAt(0).IsBlocked ? hintText : null);
					return !this._task.Options.ElementAt(0).IsBlocked;
				}
				return false;
			}

			// Token: 0x0600106E RID: 4206 RVA: 0x0006C800 File Offset: 0x0006AA00
			private bool persuasion_clickable_option_2_on_condition(out TextObject hintText)
			{
				hintText = new TextObject("{=9ACJsI6S}Blocked", null);
				if (this._task.Options.Count > 1)
				{
					hintText = (this._task.Options.ElementAt(1).IsBlocked ? hintText : null);
					return !this._task.Options.ElementAt(1).IsBlocked;
				}
				return false;
			}

			// Token: 0x0600106F RID: 4207 RVA: 0x0006C868 File Offset: 0x0006AA68
			private bool persuasion_clickable_option_3_on_condition(out TextObject hintText)
			{
				hintText = new TextObject("{=9ACJsI6S}Blocked", null);
				if (this._task.Options.Count > 2)
				{
					hintText = (this._task.Options.ElementAt(2).IsBlocked ? hintText : null);
					return !this._task.Options.ElementAt(2).IsBlocked;
				}
				return false;
			}

			// Token: 0x06001070 RID: 4208 RVA: 0x0006C8D0 File Offset: 0x0006AAD0
			private bool persuasion_clickable_option_4_on_condition(out TextObject hintText)
			{
				hintText = new TextObject("{=9ACJsI6S}Blocked", null);
				if (this._task.Options.Count > 3)
				{
					hintText = (this._task.Options.ElementAt(3).IsBlocked ? hintText : null);
					return !this._task.Options.ElementAt(3).IsBlocked;
				}
				return false;
			}

			// Token: 0x06001071 RID: 4209 RVA: 0x0006C938 File Offset: 0x0006AB38
			private PersuasionTask GetPersuasionTask()
			{
				PersuasionTask persuasionTask = new PersuasionTask(0);
				persuasionTask.FinalFailLine = new TextObject("{=5aDlmdmb}No... No. It does not make sense.", null);
				persuasionTask.TryLaterLine = TextObject.GetEmpty();
				persuasionTask.SpokenLine = new TextObject("{=6P1ruzsC}Maybe...", null);
				PersuasionOptionArgs option = new PersuasionOptionArgs(DefaultSkills.Leadership, DefaultTraits.Honor, TraitEffect.Positive, PersuasionArgumentStrength.Hard, true, new TextObject("{=Nhfl6tcM}Maybe, but that is your duty to your family.", null), null, false, false, false);
				persuasionTask.AddOptionToTask(option);
				TextObject textObject = new TextObject("{=lustkZ7s}Perhaps {?QUEST_GIVER.GENDER}she{?}he{\\?} made those plans because {?QUEST_GIVER.GENDER}she{?}he{\\?} loves you.", null);
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
				PersuasionOptionArgs option2 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Mercy, TraitEffect.Positive, PersuasionArgumentStrength.VeryEasy, false, textObject, null, false, false, false);
				persuasionTask.AddOptionToTask(option2);
				PersuasionOptionArgs option3 = new PersuasionOptionArgs(DefaultSkills.Roguery, DefaultTraits.Calculating, TraitEffect.Positive, PersuasionArgumentStrength.VeryHard, false, new TextObject("{=Ns6Svjsn}Do you think this one will be faithful to you over many years? I know a rogue when I see one.", null), null, false, false, false);
				persuasionTask.AddOptionToTask(option3);
				PersuasionOptionArgs option4 = new PersuasionOptionArgs(DefaultSkills.Roguery, DefaultTraits.Mercy, TraitEffect.Negative, PersuasionArgumentStrength.ExtremelyHard, true, new TextObject("{=2dL6j8Hp}You want to marry a corpse? Because I'm going to kill your lover if you don't listen.", null), null, true, false, false);
				persuasionTask.AddOptionToTask(option4);
				return persuasionTask;
			}

			// Token: 0x06001072 RID: 4210 RVA: 0x0006CA3A File Offset: 0x0006AC3A
			private void persuasion_start_with_daughter_on_consequence()
			{
				ConversationManager.StartPersuasion(2f, 1f, 0f, 2f, 2f, 0f, PersuasionDifficulty.Hard);
			}

			// Token: 0x06001073 RID: 4211 RVA: 0x0006CA60 File Offset: 0x0006AC60
			private void daughter_persuade_to_come_persuasion_success_on_consequence()
			{
				ConversationManager.EndPersuasion();
				this._isDaughterPersuaded = true;
			}

			// Token: 0x06001074 RID: 4212 RVA: 0x0006CA70 File Offset: 0x0006AC70
			private bool daughter_persuade_to_come_persuasion_failed_on_condition()
			{
				if (this._task.Options.All((PersuasionOptionArgs x) => x.IsBlocked) && !ConversationManager.GetPersuasionProgressSatisfied())
				{
					MBTextManager.SetTextVariable("FAILED_PERSUASION_LINE", this._task.FinalFailLine, false);
					return true;
				}
				return false;
			}

			// Token: 0x06001075 RID: 4213 RVA: 0x0006CACE File Offset: 0x0006ACCE
			private void daughter_persuade_to_come_persuasion_failed_on_consequence()
			{
				ConversationManager.EndPersuasion();
			}

			// Token: 0x06001076 RID: 4214 RVA: 0x0006CAD8 File Offset: 0x0006ACD8
			private void OnSettlementLeft(MobileParty party, Settlement settlement)
			{
				if (party.IsMainParty && settlement == base.QuestGiver.CurrentSettlement && this._exitedQuestSettlementForTheFirstTime)
				{
					if (this.DoesMainPartyHasEnoughScoutingSkill)
					{
						QuestHelper.AddMapArrowFromPointToTarget(new TextObject("{=YdwLnWa1}Direction of daughter and rogue", null), settlement.Position, this._targetVillage.Settlement.Position, 5f, 0.1f);
						MBInformationManager.AddQuickInformation(new TextObject("{=O15PyNUK}With the help of your scouting skill, you were able to trace their tracks.", null), 0, null, null, "");
						MBInformationManager.AddQuickInformation(new TextObject("{=gOWebWiK}Their direction is marked with an arrow in the campaign map.", null), 0, null, null, "");
						base.AddTrackedObject(this._targetVillage.Settlement);
					}
					else
					{
						foreach (Village village in base.QuestGiver.CurrentSettlement.Village.Bound.BoundVillages)
						{
							if (village != base.QuestGiver.CurrentSettlement.Village)
							{
								this._villagesAndAlreadyVisitedBooleans.Add(village, false);
								base.AddTrackedObject(village.Settlement);
							}
						}
					}
					TextObject textObject = new TextObject("{=FvtAJE2Q}In order to find {QUEST_GIVER.LINK}'s daughter, you have decided to visit nearby villages.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					base.AddLog(textObject, this.DoesMainPartyHasEnoughScoutingSkill);
					this._exitedQuestSettlementForTheFirstTime = false;
				}
				if (party.IsMainParty && settlement == this._targetVillage.Settlement)
				{
					this._isQuestTargetMission = false;
				}
			}

			// Token: 0x06001077 RID: 4215 RVA: 0x0006CC5C File Offset: 0x0006AE5C
			public void OnBeforeMissionOpened()
			{
				if (this._isQuestTargetMission)
				{
					Location locationWithId = Settlement.CurrentSettlement.LocationComplex.GetLocationWithId("village_center");
					if (locationWithId != null)
					{
						this.HandleRogueEquipment();
						locationWithId.AddCharacter(this.CreateQuestLocationCharacter(this._daughterHero.CharacterObject, LocationCharacter.CharacterRelations.Neutral));
						locationWithId.AddCharacter(this.CreateQuestLocationCharacter(this._rogueHero.CharacterObject, LocationCharacter.CharacterRelations.Neutral));
					}
				}
			}

			// Token: 0x06001078 RID: 4216 RVA: 0x0006CCC0 File Offset: 0x0006AEC0
			private void HandleRogueEquipment()
			{
				ItemObject @object = MBObjectManager.Instance.GetObject<ItemObject>("short_sword_t3");
				this._rogueHero.CivilianEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.WeaponItemBeginSlot, new EquipmentElement(@object, null, null, false));
				for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumAllWeaponSlots; equipmentIndex++)
				{
					ItemObject item = this._rogueHero.BattleEquipment[equipmentIndex].Item;
					if (item != null && item.WeaponComponent.PrimaryWeapon.IsShield)
					{
						this._rogueHero.BattleEquipment.AddEquipmentToSlotWithoutAgent(equipmentIndex, default(EquipmentElement));
					}
				}
			}

			// Token: 0x06001079 RID: 4217 RVA: 0x0006CD4C File Offset: 0x0006AF4C
			private void OnMissionEnded(IMission mission)
			{
				if (this._isQuestTargetMission)
				{
					this._daughterAgent = null;
					this._rogueAgent = null;
					if (this._isDaughterPersuaded)
					{
						this.ApplyDeliverySuccessConsequences();
						base.CompleteQuestWithSuccess();
						base.AddLog(this.SuccessQuestLogText, false);
						this.RemoveQuestCharacters();
						return;
					}
					if (this._acceptedDaughtersEscape)
					{
						this.ApplyDaughtersEscapeAcceptedFailConsequences();
						base.CompleteQuestWithFail(this.FailQuestLogText);
						this.RemoveQuestCharacters();
						return;
					}
					if (this._isDaughterCaptured)
					{
						this.ApplyDeliverySuccessConsequences();
						base.CompleteQuestWithSuccess();
						base.AddLog(this.SuccessQuestLogText, false);
						this.RemoveQuestCharacters();
						return;
					}
					if (this._playerDefeatedByRogue)
					{
						this.ApplyDeliveryFailedDueToDuelLostConsequences();
						base.CompleteQuestWithFail(null);
						base.AddLog(this.PlayerDefeatedByRogueLogText, false);
						this.RemoveQuestCharacters();
					}
				}
			}

			// Token: 0x0600107A RID: 4218 RVA: 0x0006CE10 File Offset: 0x0006B010
			private void ApplyDeliveryFailedDueToDuelLostConsequences()
			{
				ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, this._daughterHero, -5, true);
				this.RelationshipChangeWithQuestGiver = -10;
				if (base.QuestGiver.CurrentSettlement.Village.Bound != null)
				{
					base.QuestGiver.CurrentSettlement.Village.Bound.Town.Security -= 5f;
					base.QuestGiver.CurrentSettlement.Village.Bound.Town.Prosperity -= 5f;
				}
			}

			// Token: 0x0600107B RID: 4219 RVA: 0x0006CEA8 File Offset: 0x0006B0A8
			private LocationCharacter CreateQuestLocationCharacter(CharacterObject character, LocationCharacter.CharacterRelations relation)
			{
				Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(character.Race, "_settlement");
				Tuple<string, Monster> tuple = new Tuple<string, Monster>(ActionSetCode.GenerateActionSetNameWithSuffix(monsterWithSuffix, character.IsFemale, "_villager"), monsterWithSuffix);
				return new LocationCharacter(new AgentData(new SimpleAgentOrigin(character, -1, null, default(UniqueTroopDescriptor))).Monster(tuple.Item2), new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddCompanionBehaviors), "alley_2", true, relation, tuple.Item1, false, false, null, false, true, true, null, false);
			}

			// Token: 0x0600107C RID: 4220 RVA: 0x0006CF2F File Offset: 0x0006B12F
			private void RemoveQuestCharacters()
			{
				Settlement.CurrentSettlement.LocationComplex.RemoveCharacterIfExists(this._daughterHero);
				Settlement.CurrentSettlement.LocationComplex.RemoveCharacterIfExists(this._rogueHero);
			}

			// Token: 0x0600107D RID: 4221 RVA: 0x0006CF5C File Offset: 0x0006B15C
			private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
			{
				if (party != null && party.IsMainParty && settlement.IsVillage)
				{
					if (this._villagesAndAlreadyVisitedBooleans.ContainsKey(settlement.Village) && !this._villagesAndAlreadyVisitedBooleans[settlement.Village])
					{
						if (settlement.Village != this._targetVillage)
						{
							if (settlement.IsSettlementBusy(this))
							{
								TextObject textObject = (settlement.IsRaided ? new TextObject("{=YTaM6G1E}It seems the village has been raided a short while ago. You found nothing but smoke, fire and crying people.", null) : new TextObject("{=2P3UJ8be}You ask around the village if anyone saw {TARGET_HERO.NAME} or some suspicious characters with a young woman.{newline}{newline}Villagers say that they saw a young man and woman ride in early in the morning. They bought some supplies and trotted off towards {TARGET_VILLAGE}.", null));
								textObject.SetTextVariable("TARGET_VILLAGE", this._targetVillage.Name);
								StringHelpers.SetCharacterProperties("TARGET_HERO", this._daughterHero.CharacterObject, textObject, false);
								InformationManager.ShowInquiry(new InquiryData(this.Title.ToString(), textObject.ToString(), true, false, new TextObject("{=yS7PvrTD}OK", null).ToString(), "", null, null, "", 0f, null, null, null), false, false);
							}
							if (!this._isTrackerLogAdded)
							{
								TextObject textObject2 = new TextObject("{=WGi3Zuv7}You asked the villagers around {CURRENT_SETTLEMENT} if they saw a young woman matching the description of {QUEST_GIVER.LINK}'s daughter, {TARGET_HERO.NAME}.{newline}{newline}They said a young woman and a young man dropped by early in the morning to buy some supplies and then rode off towards {TARGET_VILLAGE}.", null);
								textObject2.SetTextVariable("CURRENT_SETTLEMENT", Hero.MainHero.CurrentSettlement.Name);
								textObject2.SetTextVariable("TARGET_VILLAGE", this._targetVillage.Settlement.EncyclopediaLinkWithName);
								StringHelpers.SetCharacterProperties("TARGET_HERO", this._daughterHero.CharacterObject, textObject2, false);
								StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject2, false);
								base.AddLog(textObject2, false);
								this._isTrackerLogAdded = true;
							}
						}
						else
						{
							InquiryData inquiryData = null;
							if (settlement.IsRaided)
							{
								TextObject textObject3 = new TextObject("{=edoXFdmg}You have found {QUEST_GIVER.NAME}'s daughter.", null);
								StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject3, false);
								TextObject textObject4 = new TextObject("{=aYMW8bWi}Talk to her", null);
								inquiryData = new InquiryData(this.Title.ToString(), textObject3.ToString(), true, false, textObject4.ToString(), null, new Action(this.TalkWithDaughterAfterRaid), null, "", 0f, null, null, null);
							}
							else if (settlement.IsSettlementBusy(this))
							{
								TextObject textObject5 = new TextObject("{=*}You ask around the village if anyone saw {TARGET_HERO.NAME} or some suspicious characters with a young woman. Villagers say that there was a young man and woman who arrived here exhausted. The villagers allowed them to stay for a while. You should search the village to find her.", null);
								StringHelpers.SetCharacterProperties("TARGET_HERO", this._daughterHero.CharacterObject, textObject5, false);
								base.AddLog(textObject5, false);
							}
							else
							{
								TextObject textObject6 = new TextObject("{=bbwNIIKI}You ask around the village if anyone saw {TARGET_HERO.NAME} or some suspicious characters with a young woman.{newline}{newline}Villagers say that there was a young man and woman who arrived here exhausted. The villagers allowed them to stay for a while.{newline}You can check the area, and see if they are still hiding here.", null);
								StringHelpers.SetCharacterProperties("TARGET_HERO", this._daughterHero.CharacterObject, textObject6, false);
								inquiryData = new InquiryData(this.Title.ToString(), textObject6.ToString(), true, true, new TextObject("{=bb6e8DoM}Search the village", null).ToString(), new TextObject("{=3CpNUnVl}Cancel", null).ToString(), new Action(this.SearchTheVillage), null, "", 0f, null, null, null);
							}
							if (inquiryData != null)
							{
								InformationManager.ShowInquiry(inquiryData, false, false);
							}
						}
						this._villagesAndAlreadyVisitedBooleans[settlement.Village] = true;
					}
					if (settlement == this._targetVillage.Settlement)
					{
						if (!base.IsTracked(this._daughterHero))
						{
							base.AddTrackedObject(this._daughterHero);
						}
						if (!base.IsTracked(this._rogueHero))
						{
							base.AddTrackedObject(this._rogueHero);
						}
						this._isQuestTargetMission = true;
					}
				}
			}

			// Token: 0x0600107E RID: 4222 RVA: 0x0006D28A File Offset: 0x0006B48A
			private void SearchTheVillage()
			{
				VillageEncounter villageEncounter = PlayerEncounter.LocationEncounter as VillageEncounter;
				if (villageEncounter == null)
				{
					return;
				}
				villageEncounter.CreateAndOpenMissionController(LocationComplex.Current.GetLocationWithId("village_center"), null, null, null);
			}

			// Token: 0x0600107F RID: 4223 RVA: 0x0006D2B4 File Offset: 0x0006B4B4
			private void TalkWithDaughterAfterRaid()
			{
				this._villageIsRaidedTalkWithDaughter = true;
				CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter, null, false, false, false, false, false, false), new ConversationCharacterData(this._daughterHero.CharacterObject, null, false, false, false, false, false, false));
			}

			// Token: 0x06001080 RID: 4224 RVA: 0x0006D2F5 File Offset: 0x0006B4F5
			private void QuestAcceptedConsequences()
			{
				base.StartQuest();
				base.AddLog(this.PlayerStartsQuestLogText, false);
			}

			// Token: 0x06001081 RID: 4225 RVA: 0x0006D30B File Offset: 0x0006B50B
			private void CanHeroDie(Hero victim, KillCharacterAction.KillCharacterActionDetail detail, ref bool result)
			{
				if (victim == Hero.MainHero && Settlement.CurrentSettlement == this._targetVillage.Settlement && Mission.Current != null)
				{
					result = false;
				}
			}

			// Token: 0x06001082 RID: 4226 RVA: 0x0006D334 File Offset: 0x0006B534
			protected override void RegisterEvents()
			{
				CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, new Action<MobileParty, Settlement>(this.OnSettlementLeft));
				CampaignEvents.SettlementEntered.AddNonSerializedListener(this, new Action<MobileParty, Settlement, Hero>(this.OnSettlementEntered));
				CampaignEvents.BeforeMissionOpenedEvent.AddNonSerializedListener(this, new Action(this.OnBeforeMissionOpened));
				CampaignEvents.OnMissionEndedEvent.AddNonSerializedListener(this, new Action<IMission>(this.OnMissionEnded));
				CampaignEvents.WarDeclared.AddNonSerializedListener(this, new Action<IFaction, IFaction, DeclareWarAction.DeclareWarDetail>(this.OnWarDeclared));
				CampaignEvents.CanHeroDieEvent.AddNonSerializedListener(this, new ReferenceAction<Hero, KillCharacterAction.KillCharacterActionDetail, bool>(this.CanHeroDie));
				CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, new Action<Clan, Kingdom, Kingdom, ChangeKingdomAction.ChangeKingdomActionDetail, bool>(this.OnClanChangedKingdom));
				CampaignEvents.MapEventStarted.AddNonSerializedListener(this, new Action<MapEvent, PartyBase, PartyBase>(this.OnMapEventStarted));
				CampaignEvents.RaidCompletedEvent.AddNonSerializedListener(this, new Action<BattleSideEnum, RaidEventComponent>(this.OnRaidCompleted));
			}

			// Token: 0x06001083 RID: 4227 RVA: 0x0006D410 File Offset: 0x0006B610
			private void OnRaidCompleted(BattleSideEnum side, RaidEventComponent raidEventComponent)
			{
				if (raidEventComponent.MapEventSettlement == base.QuestGiver.CurrentSettlement)
				{
					base.CompleteQuestWithCancel(this.VillageRaidedCancelQuestLogText);
				}
			}

			// Token: 0x06001084 RID: 4228 RVA: 0x0006D431 File Offset: 0x0006B631
			public override void OnHeroCanHaveCampaignIssuesInfoIsRequested(Hero hero, ref bool result)
			{
				if (hero == this._rogueHero || hero == this._daughterHero)
				{
					result = false;
				}
			}

			// Token: 0x06001085 RID: 4229 RVA: 0x0006D448 File Offset: 0x0006B648
			public override void OnHeroCanMoveToSettlementInfoIsRequested(Hero hero, ref bool result)
			{
				if (hero == this._rogueHero || hero == this._daughterHero)
				{
					result = false;
				}
			}

			// Token: 0x06001086 RID: 4230 RVA: 0x0006D45F File Offset: 0x0006B65F
			private void OnMapEventStarted(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
			{
				if (QuestHelper.CheckMinorMajorCoercion(this, mapEvent, attackerParty))
				{
					QuestHelper.ApplyGenericMinorMajorCoercionConsequences(this, mapEvent);
				}
			}

			// Token: 0x06001087 RID: 4231 RVA: 0x0006D472 File Offset: 0x0006B672
			private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
			{
				if (base.QuestGiver.CurrentSettlement.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction))
				{
					base.CompleteQuestWithCancel(this.QuestCanceledWarDeclaredLog);
				}
			}

			// Token: 0x06001088 RID: 4232 RVA: 0x0006D4A1 File Offset: 0x0006B6A1
			private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
			{
				QuestHelper.CheckWarDeclarationAndFailOrCancelTheQuest(this, faction1, faction2, detail, this.PlayerDeclaredWarQuestLogText, this.QuestCanceledWarDeclaredLog, false);
			}

			// Token: 0x06001089 RID: 4233 RVA: 0x0006D4BC File Offset: 0x0006B6BC
			protected override void OnFinalize()
			{
				if (base.IsTracked(this._targetVillage.Settlement))
				{
					base.RemoveTrackedObject(this._targetVillage.Settlement);
				}
				if (!Hero.MainHero.IsPrisoner && !this.DoesMainPartyHasEnoughScoutingSkill)
				{
					foreach (Village village in base.QuestGiver.CurrentSettlement.BoundVillages)
					{
						if (base.IsTracked(village.Settlement))
						{
							base.RemoveTrackedObject(village.Settlement);
						}
					}
				}
				if (this._rogueHero != null && this._rogueHero.IsAlive)
				{
					KillCharacterAction.ApplyByRemove(this._rogueHero, false, true);
				}
				if (this._daughterHero != null && this._daughterHero.IsAlive)
				{
					KillCharacterAction.ApplyByRemove(this._daughterHero, false, true);
				}
			}

			// Token: 0x0600108A RID: 4234 RVA: 0x0006D5A8 File Offset: 0x0006B7A8
			internal static void AutoGeneratedStaticCollectObjectsNotableWantsDaughterFoundIssueQuest(object o, List<object> collectedObjects)
			{
				((NotableWantsDaughterFoundIssueBehavior.NotableWantsDaughterFoundIssueQuest)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
			}

			// Token: 0x0600108B RID: 4235 RVA: 0x0006D5B6 File Offset: 0x0006B7B6
			protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
			{
				base.AutoGeneratedInstanceCollectObjects(collectedObjects);
				collectedObjects.Add(this._daughterHero);
				collectedObjects.Add(this._rogueHero);
				collectedObjects.Add(this._targetVillage);
				collectedObjects.Add(this._villagesAndAlreadyVisitedBooleans);
			}

			// Token: 0x0600108C RID: 4236 RVA: 0x0006D5EF File Offset: 0x0006B7EF
			internal static object AutoGeneratedGetMemberValue_daughterHero(object o)
			{
				return ((NotableWantsDaughterFoundIssueBehavior.NotableWantsDaughterFoundIssueQuest)o)._daughterHero;
			}

			// Token: 0x0600108D RID: 4237 RVA: 0x0006D5FC File Offset: 0x0006B7FC
			internal static object AutoGeneratedGetMemberValue_rogueHero(object o)
			{
				return ((NotableWantsDaughterFoundIssueBehavior.NotableWantsDaughterFoundIssueQuest)o)._rogueHero;
			}

			// Token: 0x0600108E RID: 4238 RVA: 0x0006D609 File Offset: 0x0006B809
			internal static object AutoGeneratedGetMemberValue_isQuestTargetMission(object o)
			{
				return ((NotableWantsDaughterFoundIssueBehavior.NotableWantsDaughterFoundIssueQuest)o)._isQuestTargetMission;
			}

			// Token: 0x0600108F RID: 4239 RVA: 0x0006D61B File Offset: 0x0006B81B
			internal static object AutoGeneratedGetMemberValue_didPlayerBeatRouge(object o)
			{
				return ((NotableWantsDaughterFoundIssueBehavior.NotableWantsDaughterFoundIssueQuest)o)._didPlayerBeatRouge;
			}

			// Token: 0x06001090 RID: 4240 RVA: 0x0006D62D File Offset: 0x0006B82D
			internal static object AutoGeneratedGetMemberValue_exitedQuestSettlementForTheFirstTime(object o)
			{
				return ((NotableWantsDaughterFoundIssueBehavior.NotableWantsDaughterFoundIssueQuest)o)._exitedQuestSettlementForTheFirstTime;
			}

			// Token: 0x06001091 RID: 4241 RVA: 0x0006D63F File Offset: 0x0006B83F
			internal static object AutoGeneratedGetMemberValue_isTrackerLogAdded(object o)
			{
				return ((NotableWantsDaughterFoundIssueBehavior.NotableWantsDaughterFoundIssueQuest)o)._isTrackerLogAdded;
			}

			// Token: 0x06001092 RID: 4242 RVA: 0x0006D651 File Offset: 0x0006B851
			internal static object AutoGeneratedGetMemberValue_isDaughterPersuaded(object o)
			{
				return ((NotableWantsDaughterFoundIssueBehavior.NotableWantsDaughterFoundIssueQuest)o)._isDaughterPersuaded;
			}

			// Token: 0x06001093 RID: 4243 RVA: 0x0006D663 File Offset: 0x0006B863
			internal static object AutoGeneratedGetMemberValue_isDaughterCaptured(object o)
			{
				return ((NotableWantsDaughterFoundIssueBehavior.NotableWantsDaughterFoundIssueQuest)o)._isDaughterCaptured;
			}

			// Token: 0x06001094 RID: 4244 RVA: 0x0006D675 File Offset: 0x0006B875
			internal static object AutoGeneratedGetMemberValue_acceptedDaughtersEscape(object o)
			{
				return ((NotableWantsDaughterFoundIssueBehavior.NotableWantsDaughterFoundIssueQuest)o)._acceptedDaughtersEscape;
			}

			// Token: 0x06001095 RID: 4245 RVA: 0x0006D687 File Offset: 0x0006B887
			internal static object AutoGeneratedGetMemberValue_targetVillage(object o)
			{
				return ((NotableWantsDaughterFoundIssueBehavior.NotableWantsDaughterFoundIssueQuest)o)._targetVillage;
			}

			// Token: 0x06001096 RID: 4246 RVA: 0x0006D694 File Offset: 0x0006B894
			internal static object AutoGeneratedGetMemberValue_villageIsRaidedTalkWithDaughter(object o)
			{
				return ((NotableWantsDaughterFoundIssueBehavior.NotableWantsDaughterFoundIssueQuest)o)._villageIsRaidedTalkWithDaughter;
			}

			// Token: 0x06001097 RID: 4247 RVA: 0x0006D6A6 File Offset: 0x0006B8A6
			internal static object AutoGeneratedGetMemberValue_villagesAndAlreadyVisitedBooleans(object o)
			{
				return ((NotableWantsDaughterFoundIssueBehavior.NotableWantsDaughterFoundIssueQuest)o)._villagesAndAlreadyVisitedBooleans;
			}

			// Token: 0x06001098 RID: 4248 RVA: 0x0006D6B3 File Offset: 0x0006B8B3
			internal static object AutoGeneratedGetMemberValue_questDifficultyMultiplier(object o)
			{
				return ((NotableWantsDaughterFoundIssueBehavior.NotableWantsDaughterFoundIssueQuest)o)._questDifficultyMultiplier;
			}

			// Token: 0x04000815 RID: 2069
			[SaveableField(10)]
			private readonly Hero _daughterHero;

			// Token: 0x04000816 RID: 2070
			[SaveableField(20)]
			private readonly Hero _rogueHero;

			// Token: 0x04000817 RID: 2071
			private Agent _daughterAgent;

			// Token: 0x04000818 RID: 2072
			private Agent _rogueAgent;

			// Token: 0x04000819 RID: 2073
			[SaveableField(50)]
			private bool _isQuestTargetMission;

			// Token: 0x0400081A RID: 2074
			[SaveableField(60)]
			private bool _didPlayerBeatRouge;

			// Token: 0x0400081B RID: 2075
			[SaveableField(70)]
			private bool _exitedQuestSettlementForTheFirstTime = true;

			// Token: 0x0400081C RID: 2076
			[SaveableField(80)]
			private bool _isTrackerLogAdded;

			// Token: 0x0400081D RID: 2077
			[SaveableField(90)]
			private bool _isDaughterPersuaded;

			// Token: 0x0400081E RID: 2078
			[SaveableField(91)]
			private bool _isDaughterCaptured;

			// Token: 0x0400081F RID: 2079
			[SaveableField(100)]
			private bool _acceptedDaughtersEscape;

			// Token: 0x04000820 RID: 2080
			[SaveableField(110)]
			private readonly Village _targetVillage;

			// Token: 0x04000821 RID: 2081
			[SaveableField(120)]
			private bool _villageIsRaidedTalkWithDaughter;

			// Token: 0x04000822 RID: 2082
			[SaveableField(140)]
			private Dictionary<Village, bool> _villagesAndAlreadyVisitedBooleans = new Dictionary<Village, bool>();

			// Token: 0x04000823 RID: 2083
			private Dictionary<string, CharacterObject> _rogueCharacterBasedOnCulture = new Dictionary<string, CharacterObject>();

			// Token: 0x04000824 RID: 2084
			private bool _playerDefeatedByRogue;

			// Token: 0x04000825 RID: 2085
			private PersuasionTask _task;

			// Token: 0x04000826 RID: 2086
			private const PersuasionDifficulty Difficulty = PersuasionDifficulty.Hard;

			// Token: 0x04000827 RID: 2087
			private const int MaxAgeForDaughterAndRogue = 25;

			// Token: 0x04000828 RID: 2088
			[SaveableField(130)]
			private readonly float _questDifficultyMultiplier;
		}
	}
}
