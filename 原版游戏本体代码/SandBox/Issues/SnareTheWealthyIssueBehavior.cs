using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.SaveSystem;

namespace SandBox.Issues
{
	// Token: 0x020000B5 RID: 181
	public class SnareTheWealthyIssueBehavior : CampaignBehaviorBase
	{
		// Token: 0x06000789 RID: 1929 RVA: 0x0003363C File Offset: 0x0003183C
		public override void RegisterEvents()
		{
			CampaignEvents.OnCheckForIssueEvent.AddNonSerializedListener(this, new Action<Hero>(this.OnCheckForIssue));
		}

		// Token: 0x0600078A RID: 1930 RVA: 0x00033658 File Offset: 0x00031858
		private void OnCheckForIssue(Hero hero)
		{
			if (this.ConditionsHold(hero))
			{
				Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(new PotentialIssueData.StartIssueDelegate(this.OnStartIssue), typeof(SnareTheWealthyIssueBehavior.SnareTheWealthyIssue), IssueBase.IssueFrequency.Rare, null));
				return;
			}
			Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(typeof(SnareTheWealthyIssueBehavior.SnareTheWealthyIssue), IssueBase.IssueFrequency.Rare));
		}

		// Token: 0x0600078B RID: 1931 RVA: 0x000336BC File Offset: 0x000318BC
		private bool ConditionsHold(Hero issueGiver)
		{
			return issueGiver.IsGangLeader && issueGiver.CurrentSettlement != null && issueGiver.CurrentSettlement.IsTown && !issueGiver.CurrentSettlement.HasPort && issueGiver.CurrentSettlement.Town.Security <= 50f && this.GetTargetMerchant(issueGiver) != null;
		}

		// Token: 0x0600078C RID: 1932 RVA: 0x00033718 File Offset: 0x00031918
		private Hero GetTargetMerchant(Hero issueOwner)
		{
			Hero result = null;
			foreach (Hero hero in issueOwner.CurrentSettlement.Notables)
			{
				if (hero != issueOwner && hero.IsMerchant && hero.Power >= 150f && hero.GetTraitLevel(DefaultTraits.Mercy) + hero.GetTraitLevel(DefaultTraits.Honor) < 0 && hero.CanHaveCampaignIssues() && !Campaign.Current.IssueManager.HasIssueCoolDown(typeof(SnareTheWealthyIssueBehavior.SnareTheWealthyIssue), hero) && !Campaign.Current.IssueManager.HasIssueCoolDown(typeof(EscortMerchantCaravanIssueBehavior), hero) && !Campaign.Current.IssueManager.HasIssueCoolDown(typeof(CaravanAmbushIssueBehavior), hero))
				{
					result = hero;
					break;
				}
			}
			return result;
		}

		// Token: 0x0600078D RID: 1933 RVA: 0x0003380C File Offset: 0x00031A0C
		private IssueBase OnStartIssue(in PotentialIssueData pid, Hero issueOwner)
		{
			Hero targetMerchant = this.GetTargetMerchant(issueOwner);
			return new SnareTheWealthyIssueBehavior.SnareTheWealthyIssue(issueOwner, targetMerchant.CharacterObject);
		}

		// Token: 0x0600078E RID: 1934 RVA: 0x0003382D File Offset: 0x00031A2D
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x04000404 RID: 1028
		private const IssueBase.IssueFrequency SnareTheWealthyIssueFrequency = IssueBase.IssueFrequency.Rare;

		// Token: 0x020001CD RID: 461
		public class SnareTheWealthyIssueTypeDefiner : SaveableTypeDefiner
		{
			// Token: 0x06001203 RID: 4611 RVA: 0x00072E91 File Offset: 0x00071091
			public SnareTheWealthyIssueTypeDefiner()
				: base(340000)
			{
			}

			// Token: 0x06001204 RID: 4612 RVA: 0x00072E9E File Offset: 0x0007109E
			protected override void DefineClassTypes()
			{
				base.AddClassDefinition(typeof(SnareTheWealthyIssueBehavior.SnareTheWealthyIssue), 1, null);
				base.AddClassDefinition(typeof(SnareTheWealthyIssueBehavior.SnareTheWealthyIssueQuest), 2, null);
			}

			// Token: 0x06001205 RID: 4613 RVA: 0x00072EC4 File Offset: 0x000710C4
			protected override void DefineEnumTypes()
			{
				base.AddEnumDefinition(typeof(SnareTheWealthyIssueBehavior.SnareTheWealthyIssueQuest.SnareTheWealthyQuestChoice), 3, null);
			}
		}

		// Token: 0x020001CE RID: 462
		public class SnareTheWealthyIssue : IssueBase
		{
			// Token: 0x170001E1 RID: 481
			// (get) Token: 0x06001206 RID: 4614 RVA: 0x00072ED8 File Offset: 0x000710D8
			private int AlternativeSolutionReward
			{
				get
				{
					return MathF.Floor(1000f + 3000f * base.IssueDifficultyMultiplier);
				}
			}

			// Token: 0x06001207 RID: 4615 RVA: 0x00072EF1 File Offset: 0x000710F1
			public SnareTheWealthyIssue(Hero issueOwner, CharacterObject targetMerchant)
				: base(issueOwner, CampaignTime.DaysFromNow(30f))
			{
				this._targetMerchantCharacter = targetMerchant;
			}

			// Token: 0x170001E2 RID: 482
			// (get) Token: 0x06001208 RID: 4616 RVA: 0x00072F0C File Offset: 0x0007110C
			public override TextObject IssueBriefByIssueGiver
			{
				get
				{
					TextObject textObject = new TextObject("{=bLigh8Sd}Well, let's just say there's an idea I've been mulling over.[ib:confident2][if:convo_bemused] You may be able to help. Have you met {TARGET_MERCHANT.NAME}? {?TARGET_MERCHANT.GENDER}She{?}He{\\?} is a very rich merchant. Very rich indeed. But not very honest… It's not right that someone without morals should have so much wealth, is it? I have a plan to redistribute it a bit.", null);
					StringHelpers.SetCharacterProperties("TARGET_MERCHANT", this._targetMerchantCharacter, textObject, false);
					return textObject;
				}
			}

			// Token: 0x170001E3 RID: 483
			// (get) Token: 0x06001209 RID: 4617 RVA: 0x00072F39 File Offset: 0x00071139
			public override TextObject IssueAcceptByPlayer
			{
				get
				{
					return new TextObject("{=keKEFagm}So what's the plan?", null);
				}
			}

			// Token: 0x170001E4 RID: 484
			// (get) Token: 0x0600120A RID: 4618 RVA: 0x00072F48 File Offset: 0x00071148
			public override TextObject IssueQuestSolutionExplanationByIssueGiver
			{
				get
				{
					TextObject textObject = new TextObject("{=SliFGAX4}{TARGET_MERCHANT.NAME} is always looking for extra swords to protect[if:convo_evil_smile] {?TARGET_MERCHANT.GENDER}her{?}his{\\?} caravans. The wicked are the ones who fear wickedness the most, you might say. What if those guards turned out to be robbers? {TARGET_MERCHANT.NAME} wouldn't trust just anyone but I think {?TARGET_MERCHANT.GENDER}she{?}he{\\?} might hire a renowned warrior like yourself. And if that warrior were to lead the caravan into an ambush… Oh I suppose it's all a bit dishonorable, but I wouldn't worry too much about your reputation. {TARGET_MERCHANT.NAME} is known to defraud {?TARGET_MERCHANT.GENDER}her{?}his{\\?} partners. If something happened to one of {?TARGET_MERCHANT.GENDER}her{?}his{\\?} caravans - well, most people won't know who to believe, and won't really care either.", null);
					StringHelpers.SetCharacterProperties("TARGET_MERCHANT", this._targetMerchantCharacter, textObject, false);
					return textObject;
				}
			}

			// Token: 0x170001E5 RID: 485
			// (get) Token: 0x0600120B RID: 4619 RVA: 0x00072F75 File Offset: 0x00071175
			public override TextObject IssueQuestSolutionAcceptByPlayer
			{
				get
				{
					return new TextObject("{=4upBpsnb}All right. I am in.", null);
				}
			}

			// Token: 0x170001E6 RID: 486
			// (get) Token: 0x0600120C RID: 4620 RVA: 0x00072F82 File Offset: 0x00071182
			public override TextObject IssueAlternativeSolutionExplanationByIssueGiver
			{
				get
				{
					TextObject textObject = new TextObject("{=ivNVRP69}I prefer if you do this yourself, but one of your trusted companions with a strong[if:convo_evil_smile] sword-arm and enough brains to set an ambush can do the job with {TROOP_COUNT} fighters. We'll split the loot, and I'll throw in a little bonus on top of that for you..", null);
					textObject.SetTextVariable("TROOP_COUNT", base.GetTotalAlternativeSolutionNeededMenCount());
					return textObject;
				}
			}

			// Token: 0x170001E7 RID: 487
			// (get) Token: 0x0600120D RID: 4621 RVA: 0x00072FA1 File Offset: 0x000711A1
			public override TextObject IssueAlternativeSolutionAcceptByPlayer
			{
				get
				{
					return new TextObject("{=biqYiCnr}My companion can handle it. Do not worry.", null);
				}
			}

			// Token: 0x170001E8 RID: 488
			// (get) Token: 0x0600120E RID: 4622 RVA: 0x00072FAE File Offset: 0x000711AE
			public override TextObject IssueAlternativeSolutionResponseByIssueGiver
			{
				get
				{
					return new TextObject("{=UURamhdC}Thank you. This should make both of us a pretty penny.[if:convo_delighted]", null);
				}
			}

			// Token: 0x170001E9 RID: 489
			// (get) Token: 0x0600120F RID: 4623 RVA: 0x00072FBB File Offset: 0x000711BB
			public override TextObject IssueDiscussAlternativeSolution
			{
				get
				{
					return new TextObject("{=pmuEeFV8}We are still arranging with your men how we'll spring this ambush. Do not worry. Everything will go smoothly.", null);
				}
			}

			// Token: 0x170001EA RID: 490
			// (get) Token: 0x06001210 RID: 4624 RVA: 0x00072FC8 File Offset: 0x000711C8
			protected override TextObject AlternativeSolutionStartLog
			{
				get
				{
					TextObject textObject = new TextObject("{=28lLrXOe}{ISSUE_GIVER.LINK} shared their plan for robbing {TARGET_MERCHANT.LINK} with you. You agreed to send your companion along with {TROOP_COUNT} men to lead the ambush for them. They will return after {RETURN_DAYS} days.", null);
					StringHelpers.SetCharacterProperties("ISSUE_GIVER", base.IssueOwner.CharacterObject, textObject, false);
					StringHelpers.SetCharacterProperties("TARGET_MERCHANT", this._targetMerchantCharacter, textObject, false);
					textObject.SetTextVariable("TROOP_COUNT", this.AlternativeSolutionSentTroops.TotalManCount - 1);
					textObject.SetTextVariable("RETURN_DAYS", base.GetTotalAlternativeSolutionDurationInDays());
					return textObject;
				}
			}

			// Token: 0x170001EB RID: 491
			// (get) Token: 0x06001211 RID: 4625 RVA: 0x00073038 File Offset: 0x00071238
			public override bool IsThereAlternativeSolution
			{
				get
				{
					return true;
				}
			}

			// Token: 0x170001EC RID: 492
			// (get) Token: 0x06001212 RID: 4626 RVA: 0x0007303B File Offset: 0x0007123B
			public override bool IsThereLordSolution
			{
				get
				{
					return false;
				}
			}

			// Token: 0x170001ED RID: 493
			// (get) Token: 0x06001213 RID: 4627 RVA: 0x0007303E File Offset: 0x0007123E
			public override TextObject Title
			{
				get
				{
					return new TextObject("{=IeihUvCD}Snare the Wealthy", null);
				}
			}

			// Token: 0x170001EE RID: 494
			// (get) Token: 0x06001214 RID: 4628 RVA: 0x0007304C File Offset: 0x0007124C
			public override TextObject Description
			{
				get
				{
					TextObject textObject = new TextObject("{=8LghFfQO}Help {ISSUE_GIVER.NAME} to rob {TARGET_MERCHANT.NAME} by acting as their guard.", null);
					StringHelpers.SetCharacterProperties("TARGET_MERCHANT", this._targetMerchantCharacter, textObject, false);
					StringHelpers.SetCharacterProperties("ISSUE_GIVER", base.IssueOwner.CharacterObject, textObject, false);
					return textObject;
				}
			}

			// Token: 0x170001EF RID: 495
			// (get) Token: 0x06001215 RID: 4629 RVA: 0x00073091 File Offset: 0x00071291
			protected override bool IssueQuestCanBeDuplicated
			{
				get
				{
					return false;
				}
			}

			// Token: 0x06001216 RID: 4630 RVA: 0x00073094 File Offset: 0x00071294
			protected override float GetIssueEffectAmountInternal(IssueEffect issueEffect)
			{
				if (issueEffect == DefaultIssueEffects.SettlementLoyalty)
				{
					return -0.1f;
				}
				if (issueEffect == DefaultIssueEffects.SettlementSecurity)
				{
					return -0.5f;
				}
				return 0f;
			}

			// Token: 0x170001F0 RID: 496
			// (get) Token: 0x06001217 RID: 4631 RVA: 0x000730B7 File Offset: 0x000712B7
			public override IssueBase.AlternativeSolutionScaleFlag AlternativeSolutionScaleFlags
			{
				get
				{
					return IssueBase.AlternativeSolutionScaleFlag.Casualties | IssueBase.AlternativeSolutionScaleFlag.FailureRisk;
				}
			}

			// Token: 0x170001F1 RID: 497
			// (get) Token: 0x06001218 RID: 4632 RVA: 0x000730BC File Offset: 0x000712BC
			public override int AlternativeSolutionBaseNeededMenCount
			{
				get
				{
					return 10 + MathF.Ceiling(16f * base.IssueDifficultyMultiplier);
				}
			}

			// Token: 0x170001F2 RID: 498
			// (get) Token: 0x06001219 RID: 4633 RVA: 0x000730D2 File Offset: 0x000712D2
			protected override int AlternativeSolutionBaseDurationInDaysInternal
			{
				get
				{
					return 2 + MathF.Ceiling(4f * base.IssueDifficultyMultiplier);
				}
			}

			// Token: 0x170001F3 RID: 499
			// (get) Token: 0x0600121A RID: 4634 RVA: 0x000730E7 File Offset: 0x000712E7
			protected override int CompanionSkillRewardXP
			{
				get
				{
					return (int)(800f + 1000f * base.IssueDifficultyMultiplier);
				}
			}

			// Token: 0x0600121B RID: 4635 RVA: 0x000730FC File Offset: 0x000712FC
			public override ValueTuple<SkillObject, int> GetAlternativeSolutionSkill(Hero hero)
			{
				return new ValueTuple<SkillObject, int>((hero.GetSkillValue(DefaultSkills.Roguery) >= hero.GetSkillValue(DefaultSkills.Tactics)) ? DefaultSkills.Roguery : DefaultSkills.Tactics, 120);
			}

			// Token: 0x0600121C RID: 4636 RVA: 0x00073129 File Offset: 0x00071329
			public override bool AlternativeSolutionCondition(out TextObject explanation)
			{
				return QuestHelper.CheckRosterForAlternativeSolution(MobileParty.MainParty.MemberRoster, base.GetTotalAlternativeSolutionNeededMenCount(), out explanation, 2, false);
			}

			// Token: 0x0600121D RID: 4637 RVA: 0x00073143 File Offset: 0x00071343
			public override bool IsTroopTypeNeededByAlternativeSolution(CharacterObject character)
			{
				return character.Tier >= 2;
			}

			// Token: 0x0600121E RID: 4638 RVA: 0x00073151 File Offset: 0x00071351
			public override bool DoTroopsSatisfyAlternativeSolution(TroopRoster troopRoster, out TextObject explanation)
			{
				explanation = null;
				return QuestHelper.CheckRosterForAlternativeSolution(troopRoster, base.GetTotalAlternativeSolutionNeededMenCount(), out explanation, 2, false);
			}

			// Token: 0x0600121F RID: 4639 RVA: 0x00073168 File Offset: 0x00071368
			protected override void AlternativeSolutionEndWithSuccessConsequence()
			{
				TraitLevelingHelper.OnIssueSolvedThroughAlternativeSolution(Hero.MainHero, new Tuple<TraitObject, int>[]
				{
					new Tuple<TraitObject, int>(DefaultTraits.Honor, -50)
				});
				TraitLevelingHelper.OnIssueSolvedThroughAlternativeSolution(Hero.MainHero, new Tuple<TraitObject, int>[]
				{
					new Tuple<TraitObject, int>(DefaultTraits.Calculating, 50)
				});
				ChangeRelationAction.ApplyPlayerRelation(base.IssueOwner, 5, true, true);
				ChangeRelationAction.ApplyPlayerRelation(this._targetMerchantCharacter.HeroObject, -10, true, true);
				GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, this.AlternativeSolutionReward, false);
			}

			// Token: 0x06001220 RID: 4640 RVA: 0x000731E8 File Offset: 0x000713E8
			protected override void AlternativeSolutionEndWithFailureConsequence()
			{
				TraitLevelingHelper.OnIssueSolvedThroughAlternativeSolution(Hero.MainHero, new Tuple<TraitObject, int>[]
				{
					new Tuple<TraitObject, int>(DefaultTraits.Honor, -100)
				});
				TraitLevelingHelper.OnIssueSolvedThroughAlternativeSolution(Hero.MainHero, new Tuple<TraitObject, int>[]
				{
					new Tuple<TraitObject, int>(DefaultTraits.Calculating, 100)
				});
				ChangeRelationAction.ApplyPlayerRelation(base.IssueOwner, -10, true, true);
				ChangeRelationAction.ApplyPlayerRelation(this._targetMerchantCharacter.HeroObject, -10, true, true);
			}

			// Token: 0x06001221 RID: 4641 RVA: 0x00073256 File Offset: 0x00071456
			protected override void OnGameLoad()
			{
			}

			// Token: 0x06001222 RID: 4642 RVA: 0x00073258 File Offset: 0x00071458
			protected override void HourlyTick()
			{
			}

			// Token: 0x06001223 RID: 4643 RVA: 0x0007325A File Offset: 0x0007145A
			protected override QuestBase GenerateIssueQuest(string questId)
			{
				return new SnareTheWealthyIssueBehavior.SnareTheWealthyIssueQuest(questId, base.IssueOwner, this._targetMerchantCharacter, base.IssueDifficultyMultiplier, CampaignTime.DaysFromNow(10f));
			}

			// Token: 0x06001224 RID: 4644 RVA: 0x00073280 File Offset: 0x00071480
			protected override void OnIssueFinalized()
			{
				if (base.IsSolvingWithQuest)
				{
					Campaign.Current.IssueManager.AddIssueCoolDownData(base.GetType(), new HeroRelatedIssueCoolDownData(this._targetMerchantCharacter.HeroObject, CampaignTime.DaysFromNow((float)Campaign.Current.Models.IssueModel.IssueOwnerCoolDownInDays)));
					Campaign.Current.IssueManager.AddIssueCoolDownData(typeof(EscortMerchantCaravanIssueBehavior.EscortMerchantCaravanIssueQuest), new HeroRelatedIssueCoolDownData(this._targetMerchantCharacter.HeroObject, CampaignTime.DaysFromNow((float)Campaign.Current.Models.IssueModel.IssueOwnerCoolDownInDays)));
					Campaign.Current.IssueManager.AddIssueCoolDownData(typeof(CaravanAmbushIssueBehavior.CaravanAmbushIssueQuest), new HeroRelatedIssueCoolDownData(this._targetMerchantCharacter.HeroObject, CampaignTime.DaysFromNow((float)Campaign.Current.Models.IssueModel.IssueOwnerCoolDownInDays)));
				}
			}

			// Token: 0x06001225 RID: 4645 RVA: 0x0007335D File Offset: 0x0007155D
			public override IssueBase.IssueFrequency GetFrequency()
			{
				return IssueBase.IssueFrequency.Rare;
			}

			// Token: 0x06001226 RID: 4646 RVA: 0x00073360 File Offset: 0x00071560
			protected override bool CanPlayerTakeQuestConditions(Hero issueGiver, out IssueBase.PreconditionFlags flag, out Hero relationHero, out SkillObject skill)
			{
				flag = IssueBase.PreconditionFlags.None;
				relationHero = null;
				skill = null;
				if (MobileParty.MainParty.MemberRoster.TotalHealthyCount < 20)
				{
					flag |= IssueBase.PreconditionFlags.NotEnoughTroops;
				}
				if (issueGiver.GetRelationWithPlayer() < -10f)
				{
					flag |= IssueBase.PreconditionFlags.Relation;
					relationHero = issueGiver;
				}
				if (issueGiver.CurrentSettlement.OwnerClan == Clan.PlayerClan)
				{
					flag |= IssueBase.PreconditionFlags.PlayerIsOwnerOfSettlement;
				}
				return flag == IssueBase.PreconditionFlags.None;
			}

			// Token: 0x06001227 RID: 4647 RVA: 0x000733CB File Offset: 0x000715CB
			public override bool IssueStayAliveConditions()
			{
				return base.IssueOwner.IsAlive && base.IssueOwner.CurrentSettlement.Town.Security <= 80f && this._targetMerchantCharacter.HeroObject.IsAlive;
			}

			// Token: 0x06001228 RID: 4648 RVA: 0x00073408 File Offset: 0x00071608
			protected override void CompleteIssueWithTimedOutConsequences()
			{
			}

			// Token: 0x06001229 RID: 4649 RVA: 0x0007340A File Offset: 0x0007160A
			public override void OnHeroCanHaveCampaignIssuesInfoIsRequested(Hero hero, ref bool result)
			{
				if (hero == this._targetMerchantCharacter.HeroObject)
				{
					result = false;
				}
			}

			// Token: 0x0600122A RID: 4650 RVA: 0x0007341D File Offset: 0x0007161D
			internal static void AutoGeneratedStaticCollectObjectsSnareTheWealthyIssue(object o, List<object> collectedObjects)
			{
				((SnareTheWealthyIssueBehavior.SnareTheWealthyIssue)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
			}

			// Token: 0x0600122B RID: 4651 RVA: 0x0007342B File Offset: 0x0007162B
			protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
			{
				base.AutoGeneratedInstanceCollectObjects(collectedObjects);
				collectedObjects.Add(this._targetMerchantCharacter);
			}

			// Token: 0x0600122C RID: 4652 RVA: 0x00073440 File Offset: 0x00071640
			internal static object AutoGeneratedGetMemberValue_targetMerchantCharacter(object o)
			{
				return ((SnareTheWealthyIssueBehavior.SnareTheWealthyIssue)o)._targetMerchantCharacter;
			}

			// Token: 0x04000893 RID: 2195
			private const int IssueDuration = 30;

			// Token: 0x04000894 RID: 2196
			private const int IssueQuestDuration = 10;

			// Token: 0x04000895 RID: 2197
			private const int MinimumRequiredMenCount = 20;

			// Token: 0x04000896 RID: 2198
			private const int MinimumRequiredRelationWithIssueGiver = -10;

			// Token: 0x04000897 RID: 2199
			private const int AlternativeSolutionMinimumTroopTier = 2;

			// Token: 0x04000898 RID: 2200
			private const int CompanionRoguerySkillValueThreshold = 120;

			// Token: 0x04000899 RID: 2201
			[SaveableField(1)]
			private readonly CharacterObject _targetMerchantCharacter;
		}

		// Token: 0x020001CF RID: 463
		public class SnareTheWealthyIssueQuest : QuestBase
		{
			// Token: 0x170001F4 RID: 500
			// (get) Token: 0x0600122D RID: 4653 RVA: 0x0007344D File Offset: 0x0007164D
			private float CaravanEncounterStartDistance
			{
				get
				{
					return Campaign.Current.Models.EncounterModel.GetEncounterJoiningRadius * 7f;
				}
			}

			// Token: 0x170001F5 RID: 501
			// (get) Token: 0x0600122E RID: 4654 RVA: 0x00073469 File Offset: 0x00071669
			private int CaravanPartyTroopCount
			{
				get
				{
					return 20 + MathF.Ceiling(40f * this._questDifficulty);
				}
			}

			// Token: 0x170001F6 RID: 502
			// (get) Token: 0x0600122F RID: 4655 RVA: 0x0007347F File Offset: 0x0007167F
			private int GangPartyTroopCount
			{
				get
				{
					return 10 + MathF.Ceiling(25f * this._questDifficulty);
				}
			}

			// Token: 0x170001F7 RID: 503
			// (get) Token: 0x06001230 RID: 4656 RVA: 0x00073495 File Offset: 0x00071695
			private int Reward1
			{
				get
				{
					return MathF.Floor(1000f + 3000f * this._questDifficulty);
				}
			}

			// Token: 0x170001F8 RID: 504
			// (get) Token: 0x06001231 RID: 4657 RVA: 0x000734AE File Offset: 0x000716AE
			private int Reward2
			{
				get
				{
					return MathF.Floor((float)this.Reward1 * 0.4f);
				}
			}

			// Token: 0x06001232 RID: 4658 RVA: 0x000734C2 File Offset: 0x000716C2
			public SnareTheWealthyIssueQuest(string questId, Hero questGiver, CharacterObject targetMerchantCharacter, float questDifficulty, CampaignTime duration)
				: base(questId, questGiver, duration, 0)
			{
				this._targetMerchantCharacter = targetMerchantCharacter;
				this._targetSettlement = this.GetTargetSettlement();
				this._questDifficulty = questDifficulty;
				this.SetDialogs();
				base.InitializeQuestOnCreation();
			}

			// Token: 0x170001F9 RID: 505
			// (get) Token: 0x06001233 RID: 4659 RVA: 0x000734FD File Offset: 0x000716FD
			public override TextObject Title
			{
				get
				{
					return new TextObject("{=IeihUvCD}Snare the Wealthy", null);
				}
			}

			// Token: 0x170001FA RID: 506
			// (get) Token: 0x06001234 RID: 4660 RVA: 0x0007350A File Offset: 0x0007170A
			public override bool IsRemainingTimeHidden
			{
				get
				{
					return false;
				}
			}

			// Token: 0x170001FB RID: 507
			// (get) Token: 0x06001235 RID: 4661 RVA: 0x00073510 File Offset: 0x00071710
			private TextObject QuestStartedLogText
			{
				get
				{
					TextObject textObject = new TextObject("{=Ba9nsfHc}{QUEST_GIVER.LINK} shared their plan for robbing {TARGET_MERCHANT.LINK} with you. You agreed to talk with {TARGET_MERCHANT.LINK} to convince {?TARGET_MERCHANT.GENDER}her{?}him{\\?} to guard {?TARGET_MERCHANT.GENDER}her{?}his{\\?} caravan and lead the caravan to ambush around {TARGET_SETTLEMENT}.", null);
					StringHelpers.SetCharacterProperties("TARGET_MERCHANT", this._targetMerchantCharacter, textObject, false);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					textObject.SetTextVariable("TARGET_SETTLEMENT", this._targetSettlement.EncyclopediaLinkWithName);
					return textObject;
				}
			}

			// Token: 0x170001FC RID: 508
			// (get) Token: 0x06001236 RID: 4662 RVA: 0x0007356C File Offset: 0x0007176C
			private TextObject Success1LogText
			{
				get
				{
					TextObject textObject = new TextObject("{=bblwaDi1}You have successfully robbed {TARGET_MERCHANT.LINK}'s caravan with {QUEST_GIVER.LINK}.", null);
					StringHelpers.SetCharacterProperties("TARGET_MERCHANT", this._targetMerchantCharacter, textObject, false);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					return textObject;
				}
			}

			// Token: 0x170001FD RID: 509
			// (get) Token: 0x06001237 RID: 4663 RVA: 0x000735B4 File Offset: 0x000717B4
			private TextObject SidedWithGangLogText
			{
				get
				{
					TextObject textObject = new TextObject("{=lZjj3MZg}When {QUEST_GIVER.LINK} arrived, you kept your side of the bargain and attacked the caravan", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					return textObject;
				}
			}

			// Token: 0x170001FE RID: 510
			// (get) Token: 0x06001238 RID: 4664 RVA: 0x000735E8 File Offset: 0x000717E8
			private TextObject TimedOutWithoutTalkingToMerchantText
			{
				get
				{
					TextObject textObject = new TextObject("{=OMKgidoP}You have failed to convince the merchant to guard {?TARGET_MERCHANT.GENDER}her{?}his{\\?} caravan in time. {QUEST_GIVER.LINK} must be furious.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					StringHelpers.SetCharacterProperties("TARGET_MERCHANT", this._targetMerchantCharacter, textObject, false);
					return textObject;
				}
			}

			// Token: 0x170001FF RID: 511
			// (get) Token: 0x06001239 RID: 4665 RVA: 0x0007362D File Offset: 0x0007182D
			private TextObject Fail1LogText
			{
				get
				{
					return new TextObject("{=DRpcqEMI}The caravan leader said your decisions were wasting their time and decided to go on his way. You have failed to uphold your part in the plan.", null);
				}
			}

			// Token: 0x17000200 RID: 512
			// (get) Token: 0x0600123A RID: 4666 RVA: 0x0007363A File Offset: 0x0007183A
			private TextObject Fail2LogText
			{
				get
				{
					return new TextObject("{=EFjas6hI}At the last moment, you decided to side with the caravan guard and defend them.", null);
				}
			}

			// Token: 0x17000201 RID: 513
			// (get) Token: 0x0600123B RID: 4667 RVA: 0x00073648 File Offset: 0x00071848
			private TextObject Fail2OutcomeLogText
			{
				get
				{
					TextObject textObject = new TextObject("{=JgrG0uoO}Having the {TARGET_MERCHANT.LINK} by your side, you were successful in protecting the caravan.", null);
					StringHelpers.SetCharacterProperties("TARGET_MERCHANT", this._targetMerchantCharacter, textObject, false);
					return textObject;
				}
			}

			// Token: 0x17000202 RID: 514
			// (get) Token: 0x0600123C RID: 4668 RVA: 0x00073675 File Offset: 0x00071875
			private TextObject Fail3LogText
			{
				get
				{
					return new TextObject("{=0NxiTi8b}You didn't feel like splitting the loot, so you betrayed both the merchant and the gang leader.", null);
				}
			}

			// Token: 0x17000203 RID: 515
			// (get) Token: 0x0600123D RID: 4669 RVA: 0x00073682 File Offset: 0x00071882
			private TextObject Fail3OutcomeLogText
			{
				get
				{
					return new TextObject("{=KbMew14D}Although the gang leader and the caravaneer joined their forces, you have successfully defeated them and kept the loot for yourself.", null);
				}
			}

			// Token: 0x17000204 RID: 516
			// (get) Token: 0x0600123E RID: 4670 RVA: 0x00073690 File Offset: 0x00071890
			private TextObject Fail4LogText
			{
				get
				{
					TextObject textObject = new TextObject("{=22nahm29}You have lost the battle against the merchant's caravan and failed to help {QUEST_GIVER.LINK}.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					return textObject;
				}
			}

			// Token: 0x17000205 RID: 517
			// (get) Token: 0x0600123F RID: 4671 RVA: 0x000736C4 File Offset: 0x000718C4
			private TextObject Fail5LogText
			{
				get
				{
					TextObject textObject = new TextObject("{=QEgzLRnC}You have lost the battle against {QUEST_GIVER.LINK} and failed to help the merchant as you promised.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					return textObject;
				}
			}

			// Token: 0x17000206 RID: 518
			// (get) Token: 0x06001240 RID: 4672 RVA: 0x000736F8 File Offset: 0x000718F8
			private TextObject Fail6LogText
			{
				get
				{
					TextObject textObject = new TextObject("{=pGu2mcar}You have lost the battle against the combined forces of the {QUEST_GIVER.LINK} and the caravan.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					return textObject;
				}
			}

			// Token: 0x17000207 RID: 519
			// (get) Token: 0x06001241 RID: 4673 RVA: 0x0007372A File Offset: 0x0007192A
			private TextObject PlayerCapturedQuestSettlementLogText
			{
				get
				{
					return new TextObject("{=gPFfHluf}Your clan is now owner of the settlement. As the lord of the settlement you cannot be part of the criminal activities anymore. Your agreement with the questgiver has canceled.", null);
				}
			}

			// Token: 0x17000208 RID: 520
			// (get) Token: 0x06001242 RID: 4674 RVA: 0x00073738 File Offset: 0x00071938
			private TextObject QuestSettlementWasCapturedLogText
			{
				get
				{
					TextObject textObject = new TextObject("{=uVigJ3LP}{QUEST_GIVER.LINK} has lost the control of {SETTLEMENT} and the deal is now invalid.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					textObject.SetTextVariable("SETTLEMENT", base.QuestGiver.CurrentSettlement.EncyclopediaLinkWithName);
					return textObject;
				}
			}

			// Token: 0x17000209 RID: 521
			// (get) Token: 0x06001243 RID: 4675 RVA: 0x00073788 File Offset: 0x00071988
			private TextObject WarDeclaredBetweenPlayerAndQuestGiverLogText
			{
				get
				{
					TextObject textObject = new TextObject("{=ojpW4WRD}Your clan is now at war with the {QUEST_GIVER.LINK}'s lord. Your agreement with {QUEST_GIVER.LINK} was canceled.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					return textObject;
				}
			}

			// Token: 0x1700020A RID: 522
			// (get) Token: 0x06001244 RID: 4676 RVA: 0x000737BC File Offset: 0x000719BC
			private TextObject TargetSettlementRaidedLogText
			{
				get
				{
					TextObject textObject = new TextObject("{=QkbkesNJ}{QUEST_GIVER.LINK} called off the ambush after {TARGET_SETTLEMENT} was raided.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					textObject.SetTextVariable("TARGET_SETTLEMENT", this._targetSettlement.EncyclopediaLinkWithName);
					return textObject;
				}
			}

			// Token: 0x1700020B RID: 523
			// (get) Token: 0x06001245 RID: 4677 RVA: 0x00073808 File Offset: 0x00071A08
			private TextObject TalkedToMerchantLogText
			{
				get
				{
					TextObject textObject = new TextObject("{=N1ZiaLRL}You talked to {TARGET_MERCHANT.LINK} as {QUEST_GIVER.LINK} asked. The caravan is waiting for you outside the gates to be escorted to {TARGET_SETTLEMENT}.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					StringHelpers.SetCharacterProperties("TARGET_MERCHANT", this._targetMerchantCharacter, textObject, false);
					textObject.SetTextVariable("TARGET_SETTLEMENT", this._targetSettlement.EncyclopediaLinkWithName);
					return textObject;
				}
			}

			// Token: 0x06001246 RID: 4678 RVA: 0x00073864 File Offset: 0x00071A64
			protected override void InitializeQuestOnGameLoad()
			{
				this.SetDialogs();
				Campaign.Current.ConversationManager.AddDialogFlow(this.GetEncounterDialogue(), this);
				Campaign.Current.ConversationManager.AddDialogFlow(this.GetDialogueWithMerchant(), this);
				Campaign.Current.ConversationManager.AddDialogFlow(this.GetDialogueWithCaravan(), this);
				Campaign.Current.ConversationManager.AddDialogFlow(this.GetDialogueWithGangWithoutCaravan(), this);
			}

			// Token: 0x06001247 RID: 4679 RVA: 0x000738D0 File Offset: 0x00071AD0
			private Settlement GetTargetSettlement()
			{
				MapDistanceModel model = Campaign.Current.Models.MapDistanceModel;
				return (from t in Settlement.All
					where t != this.QuestGiver.CurrentSettlement && t.IsTown
					select t).MinBy((Settlement t) => model.GetDistance(t, this.QuestGiver.CurrentSettlement, false, false, MobileParty.NavigationType.Default)).BoundVillages.GetRandomElement<Village>().Settlement;
			}

			// Token: 0x06001248 RID: 4680 RVA: 0x00073938 File Offset: 0x00071B38
			protected override void SetDialogs()
			{
				TextObject discussIntroDialogue = new TextObject("{=lOFR5sq6}Have you talked with {TARGET_MERCHANT.NAME}? It would be a damned waste if we waited too long and word of our plans leaked out.", null);
				TextObject textObject = new TextObject("{=cc4EEDMg}Splendid. Go have a word with {TARGET_MERCHANT.LINK}. [if:convo_focused_happy]If you can convince {?TARGET_MERCHANT.GENDER}her{?}him{\\?} to guide the caravan, we will wait in ambush along their route.", null);
				StringHelpers.SetCharacterProperties("TARGET_MERCHANT", this._targetMerchantCharacter, textObject, false);
				this.OfferDialogFlow = DialogFlow.CreateDialogFlow("issue_classic_quest_start", 100).NpcLine(textObject, null, null, null, null).Condition(() => Hero.OneToOneConversationHero == this.QuestGiver)
					.Consequence(new ConversationSentence.OnConsequenceDelegate(this.OnQuestAccepted))
					.CloseDialog();
				this.DiscussDialogFlow = DialogFlow.CreateDialogFlow("quest_discuss", 100).NpcLine(discussIntroDialogue, null, null, null, null).Condition(delegate
				{
					StringHelpers.SetCharacterProperties("TARGET_MERCHANT", this._targetMerchantCharacter, discussIntroDialogue, false);
					return Hero.OneToOneConversationHero == this.QuestGiver;
				})
					.BeginPlayerOptions(null, false)
					.PlayerOption("{=YuabHAbV}I'll take care of it shortly..", null, null, null)
					.NpcLine("{=CDXUehf0}Good, good.", null, null, null, null)
					.CloseDialog()
					.PlayerOption("{=2haJj9mp}I have but I need to deal with some other problems before leading the caravan.", null, null, null)
					.NpcLine("{=bSDIHQzO}Please do so. Hate to have word leak out.[if:convo_nervous]", null, null, null, null)
					.CloseDialog()
					.EndPlayerOptions()
					.CloseDialog();
			}

			// Token: 0x06001249 RID: 4681 RVA: 0x00073A48 File Offset: 0x00071C48
			private DialogFlow GetDialogueWithMerchant()
			{
				TextObject npcText = new TextObject("{=OJtUNAbN}Very well. You'll find the caravan [if:convo_calm_friendly]getting ready outside the gates. You will get your payment after the job. Good luck, friend.", null);
				return DialogFlow.CreateDialogFlow("hero_main_options", 125).BeginPlayerOptions(null, false).PlayerOption(new TextObject("{=K1ICRis9}I have heard you are looking for extra swords to protect your caravan. I am here to offer my services.", null), null, null, null)
					.Condition(() => Hero.OneToOneConversationHero == this._targetMerchantCharacter.HeroObject && this._caravanParty == null)
					.NpcLine("{=ltbu3S63}Yes, you have heard correctly. I am looking for a capable [if:convo_astonished]leader with a good number of followers. You only need to escort the caravan until they reach {TARGET_SETTLEMENT}. A simple job, but the cargo is very important. I'm willing to pay {MERCHANT_REWARD} denars. And of course, if you betrayed me...", null, null, null, null)
					.Condition(delegate
					{
						MBTextManager.SetTextVariable("TARGET_SETTLEMENT", this._targetSettlement.EncyclopediaLinkWithName, false);
						MBTextManager.SetTextVariable("MERCHANT_REWARD", this.Reward2);
						return true;
					})
					.Consequence(new ConversationSentence.OnConsequenceDelegate(this.SpawnQuestParties))
					.BeginPlayerOptions(null, false)
					.PlayerOption("{=AGnd7nDb}Worry not. The outlaws in these parts know my name well, and fear it.", null, null, null)
					.NpcLine(npcText, null, null, null, null)
					.CloseDialog()
					.PlayerOption("{=RCsbpizl}If you have the denars we'll do the job.", null, null, null)
					.NpcLine(npcText, null, null, null, null)
					.CloseDialog()
					.PlayerOption("{=TfDomerj}I think my men and I are more than enough to protect the caravan, good {?TARGET_MERCHANT.GENDER}madam{?}sir{\\?}.", null, null, null)
					.Condition(delegate
					{
						StringHelpers.SetCharacterProperties("TARGET_MERCHANT", this._targetMerchantCharacter, null, false);
						return true;
					})
					.NpcLine(npcText, null, null, null, null)
					.CloseDialog()
					.EndPlayerOptions()
					.EndPlayerOptions()
					.CloseDialog();
			}

			// Token: 0x0600124A RID: 4682 RVA: 0x00073B44 File Offset: 0x00071D44
			private DialogFlow GetDialogueWithCaravan()
			{
				return DialogFlow.CreateDialogFlow("start", 125).NpcLine("{=Xs7Qweuw}Lead the way, {PLAYER.NAME}.", null, null, null, null).Condition(() => MobileParty.ConversationParty == this._caravanParty && this._caravanParty != null && !this._canEncounterConversationStart)
					.Consequence(delegate
					{
						PlayerEncounter.LeaveEncounter = true;
					})
					.CloseDialog();
			}

			// Token: 0x0600124B RID: 4683 RVA: 0x00073BA8 File Offset: 0x00071DA8
			private DialogFlow GetDialogueWithGangWithoutCaravan()
			{
				return DialogFlow.CreateDialogFlow("start", 125).NpcLine("{=F44s8kPB}Where is the caravan? My men can't wait here for too long.[if:convo_undecided_open]", null, null, null, null).Condition(() => MobileParty.ConversationParty == this._gangParty && this._gangParty != null && !this._canEncounterConversationStart)
					.BeginPlayerOptions(null, false)
					.PlayerOption("{=Yqv1jk7D}Don't worry, they are coming towards our trap.", null, null, null)
					.NpcLine("{=fHc6fwrb}Good, let's finish this.", null, null, null, null)
					.CloseDialog()
					.EndPlayerOptions()
					.CloseDialog();
			}

			// Token: 0x0600124C RID: 4684 RVA: 0x00073C14 File Offset: 0x00071E14
			private DialogFlow GetEncounterDialogue()
			{
				return DialogFlow.CreateDialogFlow("start", 125).NpcLine("{=vVH7wT07}Who are these men? Be on your guard {PLAYER.NAME}, I smell trouble![if:convo_confused_annoyed]", null, null, null, null).Condition(() => MobileParty.ConversationParty == this._caravanParty && this._caravanParty != null && this._canEncounterConversationStart)
					.Consequence(delegate
					{
						StringHelpers.SetCharacterProperties("TARGET_MERCHANT", this._targetMerchantCharacter, null, false);
						AgentBuildData agentBuildData = new AgentBuildData(ConversationHelper.GetConversationCharacterPartyLeader(this._gangParty.Party));
						agentBuildData.TroopOrigin(new SimpleAgentOrigin(agentBuildData.AgentCharacter, -1, null, default(UniqueTroopDescriptor)));
						Vec3 v = Agent.Main.LookDirection * 10f;
						v.RotateAboutZ(1.3962634f);
						AgentBuildData agentBuildData2 = agentBuildData;
						Vec3 vec = Agent.Main.Position + v;
						agentBuildData2.InitialPosition(vec);
						AgentBuildData agentBuildData3 = agentBuildData;
						vec = Agent.Main.LookDirection;
						Vec2 vec2 = vec.AsVec2;
						vec2 = -vec2.Normalized();
						agentBuildData3.InitialDirection(vec2);
						Agent item = Mission.Current.SpawnAgent(agentBuildData, false);
						Campaign.Current.ConversationManager.AddConversationAgents(new List<IAgent> { item }, true);
					})
					.NpcLine("{=LJ2AoQyS}Well, well. What do we have here? Must be one of our lucky days, [if:convo_huge_smile]huh? Release all the valuables you carry and nobody gets hurt.", new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsGangPartyLeader), new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsCaravanMaster), null, null)
					.NpcLine("{=SdgDF4OZ}Hah! You're making a big mistake. See that group of men over there, [if:convo_excited]led by the warrior {PLAYER.NAME}? They're with us, and they'll cut you open.", new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsCaravanMaster), new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsGangPartyLeader), null, null)
					.NpcLine("{=LaHWB3r0}Oh… I'm afraid there's been a misunderstanding. {PLAYER.NAME} is with us, you see.[if:convo_evil_smile] Did {TARGET_MERCHANT.LINK} stuff you with lies and then send you out to your doom? Oh, shameful, shameful. {?TARGET_MERCHANT.GENDER}She{?}He{\\?} does that fairly often, unfortunately.", new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsGangPartyLeader), new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsCaravanMaster), null, null)
					.NpcLine("{=EGC4BA4h}{PLAYER.NAME}! Is this true? Look, you're a smart {?PLAYER.GENDER}woman{?}man{\\?}. [if:convo_shocked]You know that {TARGET_MERCHANT.LINK} can pay more than these scum. Take the money and keep your reputation.", new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsCaravanMaster), new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsMainHero), null, null)
					.NpcLine("{=zUKqWeUa}Come on, {PLAYER.NAME}. All this back-and-forth  is making me anxious. Let's finish this.[if:convo_nervous]", new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsGangPartyLeader), new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsMainHero), null, null)
					.BeginPlayerOptions(null, false)
					.PlayerOption("{=UEY5aQ2l}I'm here to rob {TARGET_MERCHANT.NAME}, not be {?TARGET_MERCHANT.GENDER}her{?}his{\\?} lackey. Now, cough up the goods or fight.", new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsGangPartyLeader), null, null)
					.NpcLine("{=tHUHfe6C}You're with them? This is the basest treachery I have ever witnessed![if:convo_furious]", new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsCaravanMaster), new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsMainHero), null, null)
					.Consequence(delegate
					{
						base.AddLog(this.SidedWithGangLogText, false);
					})
					.NpcLine("{=IKeZLbIK}No offense, captain, but if that's the case you need to get out more. [if:convo_mocking_teasing]Anyway, shall we go to it?", new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsGangPartyLeader), new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsMainHero), null, null)
					.Consequence(delegate
					{
						this.StartBattle(SnareTheWealthyIssueBehavior.SnareTheWealthyIssueQuest.SnareTheWealthyQuestChoice.SidedWithGang);
					})
					.CloseDialog()
					.PlayerOption("{=W7TD4yTc}You know, {TARGET_MERCHANT.NAME}'s man makes a good point. I'm guarding this caravan.", new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsGangPartyLeader), null, null)
					.NpcLine("{=VXp0R7da}Heaven protect you! I knew you'd never be tempted by such a perfidious offer.[if:convo_huge_smile]", new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsCaravanMaster), new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsMainHero), null, null)
					.Consequence(delegate
					{
						base.AddLog(this.Fail2LogText, false);
					})
					.NpcLine("{=XJOqws2b}Hmf. A funny sense of honor you have… Anyway, I'm not going home empty handed, so let's do this.[if:convo_furious]", new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsGangPartyLeader), new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsMainHero), null, null)
					.Consequence(delegate
					{
						this.StartBattle(SnareTheWealthyIssueBehavior.SnareTheWealthyIssueQuest.SnareTheWealthyQuestChoice.SidedWithCaravan);
					})
					.CloseDialog()
					.PlayerOption("{=ILrYPvTV}You know, I think I'd prefer to take all the loot for myself.", new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsGangPartyLeader), null, null)
					.NpcLine("{=cpTMttNb}Is that so? Hey, caravan captain, whatever your name is… [if:convo_contemptuous]As long as we're all switching sides here, how about I join with you to defeat this miscreant who just betrayed both of us? Whichever of us comes out of this with the most men standing keeps your goods.", new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsGangPartyLeader), new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsMainHero), null, null)
					.Consequence(delegate
					{
						base.AddLog(this.Fail3LogText, false);
					})
					.NpcLine("{=15UCTrNA}I have no choice, do I? Well, better an honest robber than a traitor![if:convo_aggressive] Let's take {?PLAYER.GENDER}her{?}him{\\?} down.", new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsCaravanMaster), new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsMainHero), null, null)
					.Consequence(delegate
					{
						this.StartBattle(SnareTheWealthyIssueBehavior.SnareTheWealthyIssueQuest.SnareTheWealthyQuestChoice.BetrayedBoth);
					})
					.CloseDialog()
					.EndPlayerOptions()
					.CloseDialog();
			}

			// Token: 0x0600124D RID: 4685 RVA: 0x00073EB8 File Offset: 0x000720B8
			private void OnQuestAccepted()
			{
				base.StartQuest();
				base.AddLog(this.QuestStartedLogText, false);
				base.AddTrackedObject(this._targetMerchantCharacter.HeroObject);
				Campaign.Current.ConversationManager.AddDialogFlow(this.GetEncounterDialogue(), this);
				Campaign.Current.ConversationManager.AddDialogFlow(this.GetDialogueWithMerchant(), this);
				Campaign.Current.ConversationManager.AddDialogFlow(this.GetDialogueWithCaravan(), this);
				Campaign.Current.ConversationManager.AddDialogFlow(this.GetDialogueWithGangWithoutCaravan(), this);
			}

			// Token: 0x0600124E RID: 4686 RVA: 0x00073F44 File Offset: 0x00072144
			public void GetMountAndHarnessVisualIdsForQuestCaravan(CultureObject culture, out string mountStringId, out string harnessStringId)
			{
				if (culture.StringId == "khuzait" || culture.StringId == "aserai")
				{
					mountStringId = "camel";
					harnessStringId = "camel_saddle_b";
					return;
				}
				mountStringId = "mule";
				harnessStringId = "mule_load_c";
			}

			// Token: 0x0600124F RID: 4687 RVA: 0x00073F94 File Offset: 0x00072194
			private void SpawnQuestParties()
			{
				TextObject textObject = GameTexts.FindText("str_caravan_party_name", null);
				textObject.SetCharacterProperties("OWNER", this._targetMerchantCharacter, false);
				string partyMountStringId;
				string partyHarnessStringId;
				this.GetMountAndHarnessVisualIdsForQuestCaravan(this._targetMerchantCharacter.Culture, out partyMountStringId, out partyHarnessStringId);
				PartyTemplateObject randomCaravanTemplate = CaravanHelper.GetRandomCaravanTemplate(this._targetMerchantCharacter.Culture, false, true);
				this._caravanParty = CustomPartyComponent.CreateCustomPartyWithTroopRoster(this._targetMerchantCharacter.HeroObject.CurrentSettlement.GatePosition, 0.1f, this._targetMerchantCharacter.HeroObject.CurrentSettlement, textObject, this._targetMerchantCharacter.HeroObject.Clan, TroopRoster.CreateDummyTroopRoster(), TroopRoster.CreateDummyTroopRoster(), this._targetMerchantCharacter.HeroObject, partyMountStringId, partyHarnessStringId, MobileParty.MainParty.Speed, false);
				MobilePartyHelper.FillPartyManuallyAfterCreation(this._caravanParty, randomCaravanTemplate, this.CaravanPartyTroopCount);
				this._caravanParty.MemberRoster.AddToCounts(this._targetMerchantCharacter.Culture.CaravanMaster, 1, false, 0, 0, true, -1);
				this._caravanParty.ItemRoster.AddToCounts(Game.Current.ObjectManager.GetObject<ItemObject>("grain"), 40);
				this._caravanParty.IgnoreByOtherPartiesTill(base.QuestDueTime);
				SetPartyAiAction.GetActionForEscortingParty(this._caravanParty, MobileParty.MainParty, MobileParty.NavigationType.Default, false, false);
				this._caravanParty.Ai.SetDoNotMakeNewDecisions(true);
				this._caravanParty.SetPartyUsedByQuest(true);
				base.AddTrackedObject(this._caravanParty);
				MobilePartyHelper.TryMatchPartySpeedWithItemWeight(this._caravanParty, MobileParty.MainParty.Speed * 1.5f, null);
				Hideout closestHideout = SettlementHelper.FindNearestHideoutToMobileParty(MobileParty.MainParty, MobileParty.NavigationType.Default, (Settlement x) => x.IsActive);
				Clan clan = Clan.BanditFactions.FirstOrDefault((Clan t) => t.Culture == closestHideout.Settlement.Culture);
				CampaignVec2 gatePosition = this._targetSettlement.GatePosition;
				PartyTemplateObject partyTemplate = Campaign.Current.ObjectManager.GetObject<PartyTemplateObject>("kingdom_hero_party_caravan_ambushers") ?? base.QuestGiver.Culture.BanditBossPartyTemplate;
				this._gangParty = CustomPartyComponent.CreateCustomPartyWithTroopRoster(gatePosition, 0.1f, this._targetSettlement, new TextObject("{=gJNdkwHV}Gang Party", null), null, TroopRoster.CreateDummyTroopRoster(), TroopRoster.CreateDummyTroopRoster(), base.QuestGiver, "", "", 0f, false);
				MobilePartyHelper.FillPartyManuallyAfterCreation(this._gangParty, partyTemplate, this.GangPartyTroopCount);
				this._gangParty.MemberRoster.AddToCounts(clan.Culture.BanditBoss, 1, true, 0, 0, true, -1);
				this._gangParty.ItemRoster.AddToCounts(Game.Current.ObjectManager.GetObject<ItemObject>("grain"), 40);
				this._gangParty.SetPartyUsedByQuest(true);
				this._gangParty.IgnoreByOtherPartiesTill(base.QuestDueTime);
				this._gangParty.Ai.SetDoNotMakeNewDecisions(true);
				this._gangParty.Ai.DisableAi();
				MobilePartyHelper.TryMatchPartySpeedWithItemWeight(this._gangParty, 0.2f, null);
				this._gangParty.SetMoveGoToSettlement(this._targetSettlement, MobileParty.NavigationType.Default, false);
				EnterSettlementAction.ApplyForParty(this._gangParty, this._targetSettlement);
				base.AddTrackedObject(this._targetSettlement);
				base.AddLog(this.TalkedToMerchantLogText, false);
			}

			// Token: 0x06001250 RID: 4688 RVA: 0x000742D0 File Offset: 0x000724D0
			private void StartBattle(SnareTheWealthyIssueBehavior.SnareTheWealthyIssueQuest.SnareTheWealthyQuestChoice playerChoice)
			{
				this._playerChoice = playerChoice;
				if (this._caravanParty.MapEvent != null)
				{
					this._caravanParty.MapEvent.FinalizeEvent();
				}
				Hideout closestHideout = SettlementHelper.FindNearestHideoutToMobileParty(MobileParty.MainParty, MobileParty.NavigationType.Default, (Settlement x) => x.IsActive);
				Clan clan = Clan.BanditFactions.FirstOrDefault((Clan t) => t.Culture == closestHideout.Settlement.Culture);
				Clan actualClan = ((playerChoice != SnareTheWealthyIssueBehavior.SnareTheWealthyIssueQuest.SnareTheWealthyQuestChoice.SidedWithCaravan) ? clan : this._caravanParty.Owner.SupporterOf);
				this._caravanParty.ActualClan = actualClan;
				Clan actualClan2 = ((playerChoice == SnareTheWealthyIssueBehavior.SnareTheWealthyIssueQuest.SnareTheWealthyQuestChoice.SidedWithGang) ? base.QuestGiver.SupporterOf : clan);
				this._gangParty.ActualClan = actualClan2;
				PartyBase attackerParty = ((playerChoice != SnareTheWealthyIssueBehavior.SnareTheWealthyIssueQuest.SnareTheWealthyQuestChoice.SidedWithGang) ? this._gangParty.Party : this._caravanParty.Party);
				PlayerEncounter.Start();
				PlayerEncounter.Current.SetupFields(attackerParty, PartyBase.MainParty);
				PlayerEncounter.StartBattle();
				if (playerChoice == SnareTheWealthyIssueBehavior.SnareTheWealthyIssueQuest.SnareTheWealthyQuestChoice.BetrayedBoth)
				{
					this._caravanParty.MapEventSide = this._gangParty.MapEventSide;
					return;
				}
				if (playerChoice == SnareTheWealthyIssueBehavior.SnareTheWealthyIssueQuest.SnareTheWealthyQuestChoice.SidedWithCaravan)
				{
					this._caravanParty.MapEventSide = PartyBase.MainParty.MapEventSide;
					return;
				}
				this._gangParty.MapEventSide = PartyBase.MainParty.MapEventSide;
			}

			// Token: 0x06001251 RID: 4689 RVA: 0x00074418 File Offset: 0x00072618
			private void StartEncounterDialogue()
			{
				if (this._gangParty.CurrentSettlement != null)
				{
					LeaveSettlementAction.ApplyForParty(this._gangParty);
				}
				PlayerEncounter.Finish(true);
				this._canEncounterConversationStart = true;
				ConversationCharacterData playerCharacterData = new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty, true, false, false, false, false, false);
				ConversationCharacterData conversationPartnerData = new ConversationCharacterData(ConversationHelper.GetConversationCharacterPartyLeader(this._caravanParty.Party), this._caravanParty.Party, true, false, false, false, false, true);
				CampaignMission.OpenConversationMission(playerCharacterData, conversationPartnerData, "", "", false);
			}

			// Token: 0x06001252 RID: 4690 RVA: 0x0007449C File Offset: 0x0007269C
			private void StartDialogueWithoutCaravan()
			{
				PlayerEncounter.Finish(true);
				ConversationCharacterData playerCharacterData = new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty, true, false, false, false, false, false);
				ConversationCharacterData conversationPartnerData = new ConversationCharacterData(ConversationHelper.GetConversationCharacterPartyLeader(this._gangParty.Party), this._gangParty.Party, true, false, false, false, false, false);
				CampaignMission.OpenConversationMission(playerCharacterData, conversationPartnerData, "", "", false);
			}

			// Token: 0x06001253 RID: 4691 RVA: 0x00074500 File Offset: 0x00072700
			protected override void HourlyTick()
			{
				if (this._caravanParty != null)
				{
					if (this._caravanParty.DefaultBehavior != AiBehavior.EscortParty || this._caravanParty.ShortTermBehavior != AiBehavior.EscortParty)
					{
						SetPartyAiAction.GetActionForEscortingParty(this._caravanParty, MobileParty.MainParty, MobileParty.NavigationType.Default, false, false);
					}
					(this._caravanParty.PartyComponent as CustomPartyComponent).CustomPartyBaseSpeed = MobileParty.MainParty.Speed;
					if (MobileParty.MainParty.TargetParty == this._caravanParty)
					{
						this._caravanParty.SetMoveModeHold();
						this._isCaravanFollowing = false;
						return;
					}
					if (!this._isCaravanFollowing)
					{
						SetPartyAiAction.GetActionForEscortingParty(this._caravanParty, MobileParty.MainParty, MobileParty.NavigationType.Default, false, false);
						this._isCaravanFollowing = true;
					}
				}
			}

			// Token: 0x06001254 RID: 4692 RVA: 0x000745AF File Offset: 0x000727AF
			private void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
			{
				if (settlement == base.QuestGiver.CurrentSettlement)
				{
					if (newOwner.Clan == Clan.PlayerClan)
					{
						this.OnCancel4();
						return;
					}
					this.OnCancel2();
				}
			}

			// Token: 0x06001255 RID: 4693 RVA: 0x000745D9 File Offset: 0x000727D9
			public void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail reason)
			{
				if ((faction1 == base.QuestGiver.MapFaction && faction2 == Hero.MainHero.MapFaction) || (faction2 == base.QuestGiver.MapFaction && faction1 == Hero.MainHero.MapFaction))
				{
					this.OnCancel1();
				}
			}

			// Token: 0x06001256 RID: 4694 RVA: 0x00074617 File Offset: 0x00072817
			public void OnVillageStateChanged(Village village, Village.VillageStates oldState, Village.VillageStates newState, MobileParty raiderParty)
			{
				if (village == this._targetSettlement.Village && newState != Village.VillageStates.Normal)
				{
					this.OnCancel3();
				}
			}

			// Token: 0x06001257 RID: 4695 RVA: 0x00074630 File Offset: 0x00072830
			public void OnMapEventEnded(MapEvent mapEvent)
			{
				if (mapEvent.IsPlayerMapEvent && this._caravanParty != null)
				{
					if (mapEvent.InvolvedParties.Contains(this._caravanParty.Party))
					{
						if (!mapEvent.InvolvedParties.Contains(this._gangParty.Party))
						{
							this.OnFail1();
							return;
						}
						if (mapEvent.WinningSide == mapEvent.PlayerSide)
						{
							if (this._playerChoice == SnareTheWealthyIssueBehavior.SnareTheWealthyIssueQuest.SnareTheWealthyQuestChoice.SidedWithGang)
							{
								this.OnSuccess1();
								return;
							}
							if (this._playerChoice == SnareTheWealthyIssueBehavior.SnareTheWealthyIssueQuest.SnareTheWealthyQuestChoice.SidedWithCaravan)
							{
								this.OnFail2();
								return;
							}
							this.OnFail3();
							return;
						}
						else
						{
							if (this._playerChoice == SnareTheWealthyIssueBehavior.SnareTheWealthyIssueQuest.SnareTheWealthyQuestChoice.SidedWithGang)
							{
								this.OnFail4();
								return;
							}
							if (this._playerChoice == SnareTheWealthyIssueBehavior.SnareTheWealthyIssueQuest.SnareTheWealthyQuestChoice.SidedWithCaravan)
							{
								this.OnFail5();
								return;
							}
							this.OnFail6();
							return;
						}
					}
					else
					{
						this.OnFail1();
					}
				}
			}

			// Token: 0x06001258 RID: 4696 RVA: 0x000746EC File Offset: 0x000728EC
			private void OnPartyJoinedArmy(MobileParty mobileParty)
			{
				if (mobileParty == MobileParty.MainParty && this._caravanParty != null)
				{
					this.OnFail1();
				}
			}

			// Token: 0x06001259 RID: 4697 RVA: 0x00074704 File Offset: 0x00072904
			private void OnGameMenuOpened(MenuCallbackArgs args)
			{
				if (this._startConversationDelegate != null && MobileParty.MainParty.CurrentSettlement == this._targetSettlement && this._caravanParty != null)
				{
					this._startConversationDelegate();
					this._startConversationDelegate = null;
				}
			}

			// Token: 0x0600125A RID: 4698 RVA: 0x0007473C File Offset: 0x0007293C
			public void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
			{
				if (party == MobileParty.MainParty && settlement == this._targetSettlement && this._caravanParty != null)
				{
					if (this._caravanParty.Position.DistanceSquared(this._targetSettlement.Position) <= this.CaravanEncounterStartDistance)
					{
						this._startConversationDelegate = new SnareTheWealthyIssueBehavior.SnareTheWealthyIssueQuest.QuestEndDelegate(this.StartEncounterDialogue);
						return;
					}
					this._startConversationDelegate = new SnareTheWealthyIssueBehavior.SnareTheWealthyIssueQuest.QuestEndDelegate(this.StartDialogueWithoutCaravan);
				}
			}

			// Token: 0x0600125B RID: 4699 RVA: 0x000747AD File Offset: 0x000729AD
			public void OnSettlementLeft(MobileParty party, Settlement settlement)
			{
				if (party == MobileParty.MainParty && this._caravanParty != null)
				{
					SetPartyAiAction.GetActionForEscortingParty(this._caravanParty, MobileParty.MainParty, MobileParty.NavigationType.Default, false, false);
				}
			}

			// Token: 0x0600125C RID: 4700 RVA: 0x000747D2 File Offset: 0x000729D2
			private void CanHeroBecomePrisoner(Hero hero, ref bool result)
			{
				if (hero == Hero.MainHero && this._playerChoice != SnareTheWealthyIssueBehavior.SnareTheWealthyIssueQuest.SnareTheWealthyQuestChoice.None)
				{
					result = false;
				}
			}

			// Token: 0x0600125D RID: 4701 RVA: 0x000747E8 File Offset: 0x000729E8
			protected override void OnFinalize()
			{
				if (this._caravanParty != null && this._caravanParty.IsActive)
				{
					DestroyPartyAction.Apply(null, this._caravanParty);
				}
				if (this._gangParty != null && this._gangParty.IsActive)
				{
					DestroyPartyAction.Apply(null, this._gangParty);
				}
			}

			// Token: 0x0600125E RID: 4702 RVA: 0x00074838 File Offset: 0x00072A38
			private void OnSuccess1()
			{
				base.AddLog(this.Success1LogText, false);
				TraitLevelingHelper.OnIssueSolvedThroughQuest(Hero.MainHero, new Tuple<TraitObject, int>[]
				{
					new Tuple<TraitObject, int>(DefaultTraits.Honor, -100)
				});
				TraitLevelingHelper.OnIssueSolvedThroughQuest(Hero.MainHero, new Tuple<TraitObject, int>[]
				{
					new Tuple<TraitObject, int>(DefaultTraits.Calculating, 50)
				});
				ChangeRelationAction.ApplyPlayerRelation(base.QuestGiver, 5, true, true);
				ChangeRelationAction.ApplyPlayerRelation(this._targetMerchantCharacter.HeroObject, -10, true, true);
				base.QuestGiver.AddPower(30f);
				GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, this.Reward1, false);
				base.CompleteQuestWithSuccess();
			}

			// Token: 0x0600125F RID: 4703 RVA: 0x000748DB File Offset: 0x00072ADB
			private void OnTimedOutWithoutTalkingToMerchant()
			{
				base.AddLog(this.TimedOutWithoutTalkingToMerchantText, false);
				TraitLevelingHelper.OnIssueFailed(Hero.MainHero, new Tuple<TraitObject, int>[]
				{
					new Tuple<TraitObject, int>(DefaultTraits.Honor, -50)
				});
				ChangeRelationAction.ApplyPlayerRelation(base.QuestGiver, -5, true, true);
			}

			// Token: 0x06001260 RID: 4704 RVA: 0x00074919 File Offset: 0x00072B19
			private void OnFail1()
			{
				this.ApplyFail1Consequences();
				base.CompleteQuestWithFail(null);
			}

			// Token: 0x06001261 RID: 4705 RVA: 0x00074928 File Offset: 0x00072B28
			private void ApplyFail1Consequences()
			{
				base.AddLog(this.Fail1LogText, false);
				TraitLevelingHelper.OnIssueFailed(Hero.MainHero, new Tuple<TraitObject, int>[]
				{
					new Tuple<TraitObject, int>(DefaultTraits.Honor, -50)
				});
				ChangeRelationAction.ApplyPlayerRelation(base.QuestGiver, -5, true, true);
				ChangeRelationAction.ApplyPlayerRelation(this._targetMerchantCharacter.HeroObject, -5, true, true);
			}

			// Token: 0x06001262 RID: 4706 RVA: 0x00074988 File Offset: 0x00072B88
			private void OnFail2()
			{
				base.AddLog(this.Fail2OutcomeLogText, false);
				TraitLevelingHelper.OnIssueFailed(Hero.MainHero, new Tuple<TraitObject, int>[]
				{
					new Tuple<TraitObject, int>(DefaultTraits.Honor, 100)
				});
				ChangeRelationAction.ApplyPlayerRelation(base.QuestGiver, -10, true, true);
				ChangeRelationAction.ApplyPlayerRelation(this._targetMerchantCharacter.HeroObject, 5, true, true);
				GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, this.Reward2, false);
				base.CompleteQuestWithBetrayal(null);
			}

			// Token: 0x06001263 RID: 4707 RVA: 0x00074A00 File Offset: 0x00072C00
			private void OnFail3()
			{
				base.AddLog(this.Fail3OutcomeLogText, false);
				TraitLevelingHelper.OnIssueFailed(Hero.MainHero, new Tuple<TraitObject, int>[]
				{
					new Tuple<TraitObject, int>(DefaultTraits.Honor, -200)
				});
				TraitLevelingHelper.OnIssueFailed(Hero.MainHero, new Tuple<TraitObject, int>[]
				{
					new Tuple<TraitObject, int>(DefaultTraits.Calculating, 100)
				});
				ChangeRelationAction.ApplyPlayerRelation(base.QuestGiver, -15, true, true);
				ChangeRelationAction.ApplyPlayerRelation(this._targetMerchantCharacter.HeroObject, -20, true, true);
				base.CompleteQuestWithBetrayal(null);
			}

			// Token: 0x06001264 RID: 4708 RVA: 0x00074A88 File Offset: 0x00072C88
			private void OnFail4()
			{
				base.AddLog(this.Fail4LogText, false);
				TraitLevelingHelper.OnIssueFailed(Hero.MainHero, new Tuple<TraitObject, int>[]
				{
					new Tuple<TraitObject, int>(DefaultTraits.Honor, -100)
				});
				TraitLevelingHelper.OnIssueFailed(Hero.MainHero, new Tuple<TraitObject, int>[]
				{
					new Tuple<TraitObject, int>(DefaultTraits.Calculating, 100)
				});
				ChangeRelationAction.ApplyPlayerRelation(base.QuestGiver, -10, true, true);
				ChangeRelationAction.ApplyPlayerRelation(this._targetMerchantCharacter.HeroObject, -10, true, true);
				base.CompleteQuestWithFail(null);
			}

			// Token: 0x06001265 RID: 4709 RVA: 0x00074B0C File Offset: 0x00072D0C
			private void OnFail5()
			{
				base.AddLog(this.Fail5LogText, false);
				TraitLevelingHelper.OnIssueFailed(Hero.MainHero, new Tuple<TraitObject, int>[]
				{
					new Tuple<TraitObject, int>(DefaultTraits.Honor, -100)
				});
				TraitLevelingHelper.OnIssueFailed(Hero.MainHero, new Tuple<TraitObject, int>[]
				{
					new Tuple<TraitObject, int>(DefaultTraits.Calculating, 100)
				});
				ChangeRelationAction.ApplyPlayerRelation(base.QuestGiver, -10, true, true);
				ChangeRelationAction.ApplyPlayerRelation(this._targetMerchantCharacter.HeroObject, -10, true, true);
				base.CompleteQuestWithBetrayal(null);
			}

			// Token: 0x06001266 RID: 4710 RVA: 0x00074B90 File Offset: 0x00072D90
			private void OnFail6()
			{
				base.AddLog(this.Fail6LogText, false);
				TraitLevelingHelper.OnIssueFailed(Hero.MainHero, new Tuple<TraitObject, int>[]
				{
					new Tuple<TraitObject, int>(DefaultTraits.Honor, -200)
				});
				TraitLevelingHelper.OnIssueFailed(Hero.MainHero, new Tuple<TraitObject, int>[]
				{
					new Tuple<TraitObject, int>(DefaultTraits.Calculating, 100)
				});
				ChangeRelationAction.ApplyPlayerRelation(base.QuestGiver, -15, true, true);
				ChangeRelationAction.ApplyPlayerRelation(this._targetMerchantCharacter.HeroObject, -20, true, true);
				base.CompleteQuestWithBetrayal(null);
			}

			// Token: 0x06001267 RID: 4711 RVA: 0x00074C16 File Offset: 0x00072E16
			protected override void OnTimedOut()
			{
				if (this._caravanParty == null)
				{
					this.OnTimedOutWithoutTalkingToMerchant();
					return;
				}
				this.ApplyFail1Consequences();
			}

			// Token: 0x06001268 RID: 4712 RVA: 0x00074C2D File Offset: 0x00072E2D
			private void OnCancel1()
			{
				base.AddLog(this.WarDeclaredBetweenPlayerAndQuestGiverLogText, false);
				base.CompleteQuestWithCancel(null);
			}

			// Token: 0x06001269 RID: 4713 RVA: 0x00074C44 File Offset: 0x00072E44
			private void OnCancel2()
			{
				base.AddLog(this.QuestSettlementWasCapturedLogText, false);
				base.CompleteQuestWithCancel(null);
			}

			// Token: 0x0600126A RID: 4714 RVA: 0x00074C5B File Offset: 0x00072E5B
			private void OnCancel3()
			{
				base.AddLog(this.TargetSettlementRaidedLogText, false);
				base.CompleteQuestWithCancel(null);
			}

			// Token: 0x0600126B RID: 4715 RVA: 0x00074C72 File Offset: 0x00072E72
			private void OnCancel4()
			{
				base.AddLog(this.PlayerCapturedQuestSettlementLogText, false);
				base.CompleteQuestWithCancel(null);
			}

			// Token: 0x0600126C RID: 4716 RVA: 0x00074C89 File Offset: 0x00072E89
			private bool IsGangPartyLeader(IAgent agent)
			{
				return agent.Character == ConversationHelper.GetConversationCharacterPartyLeader(this._gangParty.Party);
			}

			// Token: 0x0600126D RID: 4717 RVA: 0x00074CA3 File Offset: 0x00072EA3
			private bool IsCaravanMaster(IAgent agent)
			{
				return agent.Character == ConversationHelper.GetConversationCharacterPartyLeader(this._caravanParty.Party);
			}

			// Token: 0x0600126E RID: 4718 RVA: 0x00074CBD File Offset: 0x00072EBD
			private bool IsMainHero(IAgent agent)
			{
				return agent.Character == CharacterObject.PlayerCharacter;
			}

			// Token: 0x0600126F RID: 4719 RVA: 0x00074CCC File Offset: 0x00072ECC
			public override void OnHeroCanHaveCampaignIssuesInfoIsRequested(Hero hero, ref bool result)
			{
				if (hero == this._targetMerchantCharacter.HeroObject)
				{
					result = false;
				}
			}

			// Token: 0x06001270 RID: 4720 RVA: 0x00074CE0 File Offset: 0x00072EE0
			protected override void RegisterEvents()
			{
				CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, new Action<Settlement, bool, Hero, Hero, Hero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail>(this.OnSettlementOwnerChanged));
				CampaignEvents.WarDeclared.AddNonSerializedListener(this, new Action<IFaction, IFaction, DeclareWarAction.DeclareWarDetail>(this.OnWarDeclared));
				CampaignEvents.VillageStateChanged.AddNonSerializedListener(this, new Action<Village, Village.VillageStates, Village.VillageStates, MobileParty>(this.OnVillageStateChanged));
				CampaignEvents.MapEventEnded.AddNonSerializedListener(this, new Action<MapEvent>(this.OnMapEventEnded));
				CampaignEvents.OnPartyJoinedArmyEvent.AddNonSerializedListener(this, new Action<MobileParty>(this.OnPartyJoinedArmy));
				CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, new Action<MenuCallbackArgs>(this.OnGameMenuOpened));
				CampaignEvents.SettlementEntered.AddNonSerializedListener(this, new Action<MobileParty, Settlement, Hero>(this.OnSettlementEntered));
				CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, new Action<MobileParty, Settlement>(this.OnSettlementLeft));
				CampaignEvents.CanHeroBecomePrisonerEvent.AddNonSerializedListener(this, new ReferenceAction<Hero, bool>(this.CanHeroBecomePrisoner));
				CampaignEvents.CanHaveCampaignIssuesEvent.AddNonSerializedListener(this, new ReferenceAction<Hero, bool>(this.OnHeroCanHaveCampaignIssuesInfoIsRequested));
			}

			// Token: 0x06001271 RID: 4721 RVA: 0x00074DD4 File Offset: 0x00072FD4
			internal static void AutoGeneratedStaticCollectObjectsSnareTheWealthyIssueQuest(object o, List<object> collectedObjects)
			{
				((SnareTheWealthyIssueBehavior.SnareTheWealthyIssueQuest)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
			}

			// Token: 0x06001272 RID: 4722 RVA: 0x00074DE2 File Offset: 0x00072FE2
			protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
			{
				base.AutoGeneratedInstanceCollectObjects(collectedObjects);
				collectedObjects.Add(this._targetMerchantCharacter);
				collectedObjects.Add(this._targetSettlement);
				collectedObjects.Add(this._caravanParty);
				collectedObjects.Add(this._gangParty);
			}

			// Token: 0x06001273 RID: 4723 RVA: 0x00074E1B File Offset: 0x0007301B
			internal static object AutoGeneratedGetMemberValue_targetMerchantCharacter(object o)
			{
				return ((SnareTheWealthyIssueBehavior.SnareTheWealthyIssueQuest)o)._targetMerchantCharacter;
			}

			// Token: 0x06001274 RID: 4724 RVA: 0x00074E28 File Offset: 0x00073028
			internal static object AutoGeneratedGetMemberValue_targetSettlement(object o)
			{
				return ((SnareTheWealthyIssueBehavior.SnareTheWealthyIssueQuest)o)._targetSettlement;
			}

			// Token: 0x06001275 RID: 4725 RVA: 0x00074E35 File Offset: 0x00073035
			internal static object AutoGeneratedGetMemberValue_caravanParty(object o)
			{
				return ((SnareTheWealthyIssueBehavior.SnareTheWealthyIssueQuest)o)._caravanParty;
			}

			// Token: 0x06001276 RID: 4726 RVA: 0x00074E42 File Offset: 0x00073042
			internal static object AutoGeneratedGetMemberValue_gangParty(object o)
			{
				return ((SnareTheWealthyIssueBehavior.SnareTheWealthyIssueQuest)o)._gangParty;
			}

			// Token: 0x06001277 RID: 4727 RVA: 0x00074E4F File Offset: 0x0007304F
			internal static object AutoGeneratedGetMemberValue_questDifficulty(object o)
			{
				return ((SnareTheWealthyIssueBehavior.SnareTheWealthyIssueQuest)o)._questDifficulty;
			}

			// Token: 0x06001278 RID: 4728 RVA: 0x00074E61 File Offset: 0x00073061
			internal static object AutoGeneratedGetMemberValue_playerChoice(object o)
			{
				return ((SnareTheWealthyIssueBehavior.SnareTheWealthyIssueQuest)o)._playerChoice;
			}

			// Token: 0x06001279 RID: 4729 RVA: 0x00074E73 File Offset: 0x00073073
			internal static object AutoGeneratedGetMemberValue_canEncounterConversationStart(object o)
			{
				return ((SnareTheWealthyIssueBehavior.SnareTheWealthyIssueQuest)o)._canEncounterConversationStart;
			}

			// Token: 0x0600127A RID: 4730 RVA: 0x00074E85 File Offset: 0x00073085
			internal static object AutoGeneratedGetMemberValue_isCaravanFollowing(object o)
			{
				return ((SnareTheWealthyIssueBehavior.SnareTheWealthyIssueQuest)o)._isCaravanFollowing;
			}

			// Token: 0x0400089A RID: 2202
			private SnareTheWealthyIssueBehavior.SnareTheWealthyIssueQuest.QuestEndDelegate _startConversationDelegate;

			// Token: 0x0400089B RID: 2203
			[SaveableField(1)]
			private CharacterObject _targetMerchantCharacter;

			// Token: 0x0400089C RID: 2204
			[SaveableField(2)]
			private Settlement _targetSettlement;

			// Token: 0x0400089D RID: 2205
			[SaveableField(3)]
			private MobileParty _caravanParty;

			// Token: 0x0400089E RID: 2206
			[SaveableField(4)]
			private MobileParty _gangParty;

			// Token: 0x0400089F RID: 2207
			[SaveableField(5)]
			private readonly float _questDifficulty;

			// Token: 0x040008A0 RID: 2208
			[SaveableField(6)]
			private SnareTheWealthyIssueBehavior.SnareTheWealthyIssueQuest.SnareTheWealthyQuestChoice _playerChoice;

			// Token: 0x040008A1 RID: 2209
			[SaveableField(7)]
			private bool _canEncounterConversationStart;

			// Token: 0x040008A2 RID: 2210
			[SaveableField(8)]
			private bool _isCaravanFollowing = true;

			// Token: 0x02000242 RID: 578
			internal enum SnareTheWealthyQuestChoice
			{
				// Token: 0x040009FE RID: 2558
				None,
				// Token: 0x040009FF RID: 2559
				SidedWithCaravan,
				// Token: 0x04000A00 RID: 2560
				SidedWithGang,
				// Token: 0x04000A01 RID: 2561
				BetrayedBoth
			}

			// Token: 0x02000243 RID: 579
			// (Invoke) Token: 0x0600144C RID: 5196
			private delegate void QuestEndDelegate();
		}
	}
}
