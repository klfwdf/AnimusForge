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
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace SandBox.Issues
{
	// Token: 0x020000B2 RID: 178
	public class ProdigalSonIssueBehavior : CampaignBehaviorBase
	{
		// Token: 0x17000099 RID: 153
		// (get) Token: 0x0600076A RID: 1898 RVA: 0x00032E51 File Offset: 0x00031051
		private float MaxDistanceForSettlementSelection
		{
			get
			{
				return Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(MobileParty.NavigationType.Default) * 2.18f;
			}
		}

		// Token: 0x0600076B RID: 1899 RVA: 0x00032E64 File Offset: 0x00031064
		public override void RegisterEvents()
		{
			CampaignEvents.OnCheckForIssueEvent.AddNonSerializedListener(this, new Action<Hero>(this.CheckForIssue));
		}

		// Token: 0x0600076C RID: 1900 RVA: 0x00032E7D File Offset: 0x0003107D
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x0600076D RID: 1901 RVA: 0x00032E80 File Offset: 0x00031080
		public void CheckForIssue(Hero hero)
		{
			Hero item;
			Hero item2;
			if (this.ConditionsHold(hero, out item, out item2))
			{
				Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(new PotentialIssueData.StartIssueDelegate(this.OnStartIssue), typeof(ProdigalSonIssueBehavior.ProdigalSonIssue), IssueBase.IssueFrequency.Rare, new Tuple<Hero, Hero>(item, item2)));
				return;
			}
			Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(typeof(ProdigalSonIssueBehavior.ProdigalSonIssue), IssueBase.IssueFrequency.Rare));
		}

		// Token: 0x0600076E RID: 1902 RVA: 0x00032EF0 File Offset: 0x000310F0
		private bool ConditionsHoldForSettlement(Settlement settlement, Hero issueGiver)
		{
			if (settlement.IsTown && settlement.MapFaction == issueGiver.MapFaction && settlement != issueGiver.CurrentSettlement && settlement.OwnerClan != issueGiver.Clan && settlement.OwnerClan != Clan.PlayerClan)
			{
				if (settlement.HeroesWithoutParty.FirstOrDefault((Hero x) => x.CanHaveCampaignIssues() && x.IsGangLeader) != null)
				{
					return settlement.LocationComplex.GetListOfLocations().AnyQ((Location x) => x.CanBeReserved && !x.IsReserved);
				}
			}
			return false;
		}

		// Token: 0x0600076F RID: 1903 RVA: 0x00032F9C File Offset: 0x0003119C
		private bool ConditionsHold(Hero issueGiver, out Hero selectedHero, out Hero targetHero)
		{
			selectedHero = null;
			targetHero = null;
			if (issueGiver.IsLord && !issueGiver.IsPrisoner && issueGiver.Clan != Clan.PlayerClan && issueGiver.Age > 30f && issueGiver.GetTraitLevel(DefaultTraits.Mercy) <= 0 && (issueGiver.CurrentSettlement != null || issueGiver.PartyBelongedTo != null))
			{
				selectedHero = issueGiver.Clan.AliveLords.GetRandomElementWithPredicate((Hero x) => x.IsActive && !x.IsFemale && x.Age < 35f && (int)x.Age + 10 <= (int)issueGiver.Age && !x.IsPrisoner && x.CanHaveCampaignIssues() && x.PartyBelongedTo == null && x.CurrentSettlement != null && x.GovernorOf == null && x.GetTraitLevel(DefaultTraits.Honor) + x.GetTraitLevel(DefaultTraits.Calculating) < 0);
				if (selectedHero != null)
				{
					Settlement settlement = SettlementHelper.FindRandomSettlement((Settlement x) => this.ConditionsHoldForSettlement(x, issueGiver) && x.HeroesWithoutParty.FirstOrDefault((Hero y) => y.CanHaveCampaignIssues() && y.IsGangLeader && Campaign.Current.Models.MapDistanceModel.GetDistance(issueGiver.CurrentSettlement, x, false, false, MobileParty.NavigationType.Default) < this.MaxDistanceForSettlementSelection) != null);
					Hero hero;
					if (settlement == null)
					{
						hero = null;
					}
					else
					{
						hero = settlement.HeroesWithoutParty.FirstOrDefault((Hero y) => y.CanHaveCampaignIssues() && y.IsGangLeader);
					}
					targetHero = hero;
				}
			}
			return selectedHero != null && targetHero != null;
		}

		// Token: 0x06000770 RID: 1904 RVA: 0x000330B4 File Offset: 0x000312B4
		private IssueBase OnStartIssue(in PotentialIssueData pid, Hero issueOwner)
		{
			PotentialIssueData potentialIssueData = pid;
			Tuple<Hero, Hero> tuple = potentialIssueData.RelatedObject as Tuple<Hero, Hero>;
			return new ProdigalSonIssueBehavior.ProdigalSonIssue(issueOwner, tuple.Item1, tuple.Item2);
		}

		// Token: 0x040003FB RID: 1019
		private const IssueBase.IssueFrequency ProdigalSonIssueFrequency = IssueBase.IssueFrequency.Rare;

		// Token: 0x040003FC RID: 1020
		private const int AgeLimitForSon = 35;

		// Token: 0x040003FD RID: 1021
		private const int AgeLimitForIssueOwner = 30;

		// Token: 0x040003FE RID: 1022
		private const int MinimumAgeDifference = 10;

		// Token: 0x020001C1 RID: 449
		public class ProdigalSonIssueTypeDefiner : SaveableTypeDefiner
		{
			// Token: 0x060010A8 RID: 4264 RVA: 0x0006D817 File Offset: 0x0006BA17
			public ProdigalSonIssueTypeDefiner()
				: base(345000)
			{
			}

			// Token: 0x060010A9 RID: 4265 RVA: 0x0006D824 File Offset: 0x0006BA24
			protected override void DefineClassTypes()
			{
				base.AddClassDefinition(typeof(ProdigalSonIssueBehavior.ProdigalSonIssue), 1, null);
				base.AddClassDefinition(typeof(ProdigalSonIssueBehavior.ProdigalSonIssueQuest), 2, null);
			}
		}

		// Token: 0x020001C2 RID: 450
		public class ProdigalSonIssue : IssueBase
		{
			// Token: 0x1700017E RID: 382
			// (get) Token: 0x060010AA RID: 4266 RVA: 0x0006D84A File Offset: 0x0006BA4A
			public override IssueBase.AlternativeSolutionScaleFlag AlternativeSolutionScaleFlags
			{
				get
				{
					return IssueBase.AlternativeSolutionScaleFlag.FailureRisk;
				}
			}

			// Token: 0x1700017F RID: 383
			// (get) Token: 0x060010AB RID: 4267 RVA: 0x0006D84D File Offset: 0x0006BA4D
			private Clan Clan
			{
				get
				{
					return base.IssueOwner.Clan;
				}
			}

			// Token: 0x17000180 RID: 384
			// (get) Token: 0x060010AC RID: 4268 RVA: 0x0006D85A File Offset: 0x0006BA5A
			protected override int RewardGold
			{
				get
				{
					return 1200 + (int)(3000f * base.IssueDifficultyMultiplier);
				}
			}

			// Token: 0x17000181 RID: 385
			// (get) Token: 0x060010AD RID: 4269 RVA: 0x0006D870 File Offset: 0x0006BA70
			public override TextObject IssueBriefByIssueGiver
			{
				get
				{
					TextObject textObject = new TextObject("{=5a6KlSXt}I have a problem. [ib:normal2][if:convo_pondering]My young kinsman {PRODIGAL_SON.LINK} has gone to town to have fun, drinking, wenching and gambling. Many young men do that, but it seems he was a bit reckless. Now he sends news that he owes a large sum of money to {TARGET_HERO.LINK}, one of the local gang bosses in the city of {SETTLEMENT_LINK}. These ruffians are holding him as a “guest” in their house until someone pays his debt.", null);
					StringHelpers.SetCharacterProperties("PRODIGAL_SON", this._prodigalSon.CharacterObject, textObject, false);
					StringHelpers.SetCharacterProperties("TARGET_HERO", this._targetHero.CharacterObject, textObject, false);
					textObject.SetTextVariable("SETTLEMENT_LINK", this._targetSettlement.EncyclopediaLinkWithName);
					return textObject;
				}
			}

			// Token: 0x17000182 RID: 386
			// (get) Token: 0x060010AE RID: 4270 RVA: 0x0006D8D1 File Offset: 0x0006BAD1
			public override TextObject IssueAcceptByPlayer
			{
				get
				{
					return new TextObject("{=YtS3cgto}What are you planning to do?", null);
				}
			}

			// Token: 0x17000183 RID: 387
			// (get) Token: 0x060010AF RID: 4271 RVA: 0x0006D8DE File Offset: 0x0006BADE
			public override TextObject IssueQuestSolutionExplanationByIssueGiver
			{
				get
				{
					TextObject textObject = new TextObject("{=ZC1slXw1}I'm not inclined to pay the debt. [ib:closed][if:convo_worried]I'm not going to reward this kind of lawlessness, when even the best families aren't safe. I've sent word to the lord of {SETTLEMENT_NAME} but I can't say I expect to hear back, what with the wars and all. I want someone to go there and free the lad. You could pay, I suppose, but I'd prefer it if you taught those bastards a lesson. I'll pay you either way but obviously you get to keep more if you use force.", null);
					textObject.SetTextVariable("SETTLEMENT_NAME", this._targetSettlement.EncyclopediaLinkWithName);
					return textObject;
				}
			}

			// Token: 0x17000184 RID: 388
			// (get) Token: 0x060010B0 RID: 4272 RVA: 0x0006D902 File Offset: 0x0006BB02
			public override TextObject IssuePlayerResponseAfterAlternativeExplanation
			{
				get
				{
					return new TextObject("{=4zf1lg6L}I could go myself, or send a companion.", null);
				}
			}

			// Token: 0x17000185 RID: 389
			// (get) Token: 0x060010B1 RID: 4273 RVA: 0x0006D90F File Offset: 0x0006BB0F
			public override TextObject IssueAlternativeSolutionExplanationByIssueGiver
			{
				get
				{
					TextObject textObject = new TextObject("{=CWbAoGRu}Yes, I don't care how you solve it. [if:convo_normal]Just solve it any way you like. I reckon {NEEDED_MEN_COUNT} led by someone who knows how to handle thugs could solve this in about {ALTERNATIVE_SOLUTION_DURATION} days. I'd send my own men but it could cause complications for us to go marching in wearing our clan colors in another lord's territory.", null);
					textObject.SetTextVariable("NEEDED_MEN_COUNT", base.GetTotalAlternativeSolutionNeededMenCount());
					textObject.SetTextVariable("ALTERNATIVE_SOLUTION_DURATION", base.GetTotalAlternativeSolutionDurationInDays());
					return textObject;
				}
			}

			// Token: 0x17000186 RID: 390
			// (get) Token: 0x060010B2 RID: 4274 RVA: 0x0006D940 File Offset: 0x0006BB40
			public override TextObject IssueQuestSolutionAcceptByPlayer
			{
				get
				{
					return new TextObject("{=aKbyJsho}I will free your kinsman myself.", null);
				}
			}

			// Token: 0x17000187 RID: 391
			// (get) Token: 0x060010B3 RID: 4275 RVA: 0x0006D94D File Offset: 0x0006BB4D
			public override TextObject IssueAlternativeSolutionAcceptByPlayer
			{
				get
				{
					TextObject textObject = new TextObject("{=PuuVGOyM}I will send {NEEDED_MEN_COUNT} of my men with one of my lieutenants for {ALTERNATIVE_SOLUTION_DURATION} days to help you.", null);
					textObject.SetTextVariable("NEEDED_MEN_COUNT", base.GetTotalAlternativeSolutionNeededMenCount());
					textObject.SetTextVariable("ALTERNATIVE_SOLUTION_DURATION", base.GetTotalAlternativeSolutionDurationInDays());
					return textObject;
				}
			}

			// Token: 0x17000188 RID: 392
			// (get) Token: 0x060010B4 RID: 4276 RVA: 0x0006D97E File Offset: 0x0006BB7E
			public override TextObject IssueDiscussAlternativeSolution
			{
				get
				{
					return new TextObject("{=qxhMagyZ}I'm glad someone's on it.[if:convo_relaxed_happy] Just see that they do it quickly.", null);
				}
			}

			// Token: 0x17000189 RID: 393
			// (get) Token: 0x060010B5 RID: 4277 RVA: 0x0006D98B File Offset: 0x0006BB8B
			public override TextObject IssueAlternativeSolutionResponseByIssueGiver
			{
				get
				{
					return new TextObject("{=mDXzDXKY}Very good. [if:convo_relaxed_happy]I'm sure you'll chose competent men to bring our boy back.", null);
				}
			}

			// Token: 0x1700018A RID: 394
			// (get) Token: 0x060010B6 RID: 4278 RVA: 0x0006D998 File Offset: 0x0006BB98
			protected override TextObject AlternativeSolutionStartLog
			{
				get
				{
					TextObject textObject = new TextObject("{=Z9sp21rl}{QUEST_GIVER.LINK}, a lord from the {QUEST_GIVER_CLAN} clan, asked you to free {?QUEST_GIVER.GENDER}her{?}his{\\?} relative. The young man is currently held by {TARGET_HERO.LINK} a local gang leader because of his debts. {?QUEST_GIVER.GENDER}Lady{?}Lord{\\?} {QUEST_GIVER.LINK} has given you enough gold to settle {?QUEST_GIVER.GENDER}her{?}his{\\?} debts but {?QUEST_GIVER.GENDER}she{?}he{\\?} encourages you to keep the money to yourself and make an example of these criminals so no one would dare to hold a nobleman again. You have sent {COMPANION.LINK} and {NEEDED_MEN_COUNT} men to take care of the situation for you. They should be back in {ALTERNATIVE_SOLUTION_DURATION} days.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.IssueOwner.CharacterObject, textObject, false);
					StringHelpers.SetCharacterProperties("TARGET_HERO", this._targetHero.CharacterObject, textObject, false);
					StringHelpers.SetCharacterProperties("COMPANION", base.AlternativeSolutionHero.CharacterObject, textObject, false);
					textObject.SetTextVariable("QUEST_GIVER_CLAN", base.IssueOwner.Clan.EncyclopediaLinkWithName);
					textObject.SetTextVariable("SETTLEMENT", this._targetSettlement.EncyclopediaLinkWithName);
					textObject.SetTextVariable("NEEDED_MEN_COUNT", base.GetTotalAlternativeSolutionNeededMenCount());
					textObject.SetTextVariable("ALTERNATIVE_SOLUTION_DURATION", base.GetTotalAlternativeSolutionDurationInDays());
					return textObject;
				}
			}

			// Token: 0x1700018B RID: 395
			// (get) Token: 0x060010B7 RID: 4279 RVA: 0x0006DA54 File Offset: 0x0006BC54
			public override TextObject IssueAlternativeSolutionSuccessLog
			{
				get
				{
					TextObject textObject = new TextObject("{=IXnvQ8kG}{COMPANION.LINK} and the men you sent with {?COMPANION.GENDER}her{?}him{\\?} safely return with the news of success. {QUEST_GIVER.LINK} is happy and sends you {?QUEST_GIVER.GENDER}her{?}his{\\?} regards with {REWARD}{GOLD_ICON} the money he promised.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.IssueOwner.CharacterObject, textObject, false);
					StringHelpers.SetCharacterProperties("COMPANION", base.AlternativeSolutionHero.CharacterObject, textObject, false);
					textObject.SetTextVariable("REWARD", this.RewardGold);
					return textObject;
				}
			}

			// Token: 0x1700018C RID: 396
			// (get) Token: 0x060010B8 RID: 4280 RVA: 0x0006DAB0 File Offset: 0x0006BCB0
			public override bool IsThereAlternativeSolution
			{
				get
				{
					return true;
				}
			}

			// Token: 0x1700018D RID: 397
			// (get) Token: 0x060010B9 RID: 4281 RVA: 0x0006DAB3 File Offset: 0x0006BCB3
			public override int AlternativeSolutionBaseNeededMenCount
			{
				get
				{
					return 1 + MathF.Ceiling(3f * base.IssueDifficultyMultiplier);
				}
			}

			// Token: 0x1700018E RID: 398
			// (get) Token: 0x060010BA RID: 4282 RVA: 0x0006DAC8 File Offset: 0x0006BCC8
			protected override int AlternativeSolutionBaseDurationInDaysInternal
			{
				get
				{
					return 7 + MathF.Ceiling(7f * base.IssueDifficultyMultiplier);
				}
			}

			// Token: 0x1700018F RID: 399
			// (get) Token: 0x060010BB RID: 4283 RVA: 0x0006DADD File Offset: 0x0006BCDD
			protected override int CompanionSkillRewardXP
			{
				get
				{
					return (int)(700f + 900f * base.IssueDifficultyMultiplier);
				}
			}

			// Token: 0x17000190 RID: 400
			// (get) Token: 0x060010BC RID: 4284 RVA: 0x0006DAF2 File Offset: 0x0006BCF2
			public override bool IsThereLordSolution
			{
				get
				{
					return false;
				}
			}

			// Token: 0x17000191 RID: 401
			// (get) Token: 0x060010BD RID: 4285 RVA: 0x0006DAF5 File Offset: 0x0006BCF5
			public override TextObject Title
			{
				get
				{
					TextObject textObject = new TextObject("{=Mr2rt8g8}Prodigal Son of {CLAN_NAME}", null);
					textObject.SetTextVariable("CLAN_NAME", this.Clan.Name);
					return textObject;
				}
			}

			// Token: 0x17000192 RID: 402
			// (get) Token: 0x060010BE RID: 4286 RVA: 0x0006DB1C File Offset: 0x0006BD1C
			public override TextObject Description
			{
				get
				{
					TextObject textObject = new TextObject("{=5puy0Jle}{ISSUE_OWNER.NAME} asks the player to aid a young clan member. He is supposed to have huge gambling debts so the gang leaders holds him as a hostage. You are asked to retrieve him any way possible.", null);
					StringHelpers.SetCharacterProperties("ISSUE_OWNER", base.IssueOwner.CharacterObject, textObject, false);
					return textObject;
				}
			}

			// Token: 0x060010BF RID: 4287 RVA: 0x0006DB50 File Offset: 0x0006BD50
			public ProdigalSonIssue(Hero issueOwner, Hero prodigalSon, Hero targetGangHero)
				: base(issueOwner, CampaignTime.DaysFromNow(50f))
			{
				this._prodigalSon = prodigalSon;
				this._targetHero = targetGangHero;
				this._targetSettlement = this._targetHero.CurrentSettlement;
				this._targetHouse = this._targetSettlement.LocationComplex.GetListOfLocations().FirstOrDefault((Location x) => x.CanBeReserved && !x.IsReserved);
				TextObject textObject = new TextObject("{=EZ19JOGj}{MENTOR.NAME}'s House", null);
				StringHelpers.SetCharacterProperties("MENTOR", this._targetHero.CharacterObject, textObject, false);
				this._targetHouse.ReserveLocation(textObject, textObject);
				DisableHeroAction.Apply(this._prodigalSon);
			}

			// Token: 0x060010C0 RID: 4288 RVA: 0x0006DC03 File Offset: 0x0006BE03
			public override void OnHeroCanHaveCampaignIssuesInfoIsRequested(Hero hero, ref bool result)
			{
				if (hero == this._targetHero || hero == this._prodigalSon)
				{
					result = false;
				}
			}

			// Token: 0x060010C1 RID: 4289 RVA: 0x0006DC1A File Offset: 0x0006BE1A
			protected override float GetIssueEffectAmountInternal(IssueEffect issueEffect)
			{
				if (issueEffect == DefaultIssueEffects.IssueOwnerPower)
				{
					return -0.2f;
				}
				return 0f;
			}

			// Token: 0x060010C2 RID: 4290 RVA: 0x0006DC2F File Offset: 0x0006BE2F
			public override ValueTuple<SkillObject, int> GetAlternativeSolutionSkill(Hero hero)
			{
				return new ValueTuple<SkillObject, int>((hero.GetSkillValue(DefaultSkills.Charm) >= hero.GetSkillValue(DefaultSkills.Roguery)) ? DefaultSkills.Charm : DefaultSkills.Roguery, 120);
			}

			// Token: 0x060010C3 RID: 4291 RVA: 0x0006DC5C File Offset: 0x0006BE5C
			protected override void OnGameLoad()
			{
				Town town = Town.AllTowns.FirstOrDefault((Town x) => x.Settlement.LocationComplex.GetListOfLocations().Contains(this._targetHouse));
				if (town != null)
				{
					this._targetSettlement = town.Settlement;
				}
			}

			// Token: 0x060010C4 RID: 4292 RVA: 0x0006DC8F File Offset: 0x0006BE8F
			protected override void HourlyTick()
			{
			}

			// Token: 0x060010C5 RID: 4293 RVA: 0x0006DC91 File Offset: 0x0006BE91
			protected override QuestBase GenerateIssueQuest(string questId)
			{
				return new ProdigalSonIssueBehavior.ProdigalSonIssueQuest(questId, base.IssueOwner, this._targetHero, this._prodigalSon, this._targetHouse, base.IssueDifficultyMultiplier, CampaignTime.DaysFromNow(24f), this.RewardGold);
			}

			// Token: 0x060010C6 RID: 4294 RVA: 0x0006DCC7 File Offset: 0x0006BEC7
			public override IssueBase.IssueFrequency GetFrequency()
			{
				return IssueBase.IssueFrequency.Rare;
			}

			// Token: 0x060010C7 RID: 4295 RVA: 0x0006DCCC File Offset: 0x0006BECC
			protected override bool CanPlayerTakeQuestConditions(Hero issueGiver, out IssueBase.PreconditionFlags flag, out Hero relationHero, out SkillObject skill)
			{
				bool flag2 = issueGiver.GetRelationWithPlayer() >= -10f && !issueGiver.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction) && Clan.PlayerClan.Tier >= 1;
				flag = (flag2 ? IssueBase.PreconditionFlags.None : ((!issueGiver.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction)) ? ((Clan.PlayerClan.Tier >= 1) ? IssueBase.PreconditionFlags.Relation : IssueBase.PreconditionFlags.ClanTier) : IssueBase.PreconditionFlags.AtWar));
				relationHero = issueGiver;
				skill = null;
				return flag2;
			}

			// Token: 0x060010C8 RID: 4296 RVA: 0x0006DD51 File Offset: 0x0006BF51
			public override bool IssueStayAliveConditions()
			{
				return this._targetHero.IsActive;
			}

			// Token: 0x060010C9 RID: 4297 RVA: 0x0006DD5E File Offset: 0x0006BF5E
			protected override void CompleteIssueWithTimedOutConsequences()
			{
			}

			// Token: 0x060010CA RID: 4298 RVA: 0x0006DD60 File Offset: 0x0006BF60
			public override bool DoTroopsSatisfyAlternativeSolution(TroopRoster troopRoster, out TextObject explanation)
			{
				return QuestHelper.CheckRosterForAlternativeSolution(troopRoster, base.GetTotalAlternativeSolutionNeededMenCount(), out explanation, 2, false);
			}

			// Token: 0x060010CB RID: 4299 RVA: 0x0006DD71 File Offset: 0x0006BF71
			public override bool IsTroopTypeNeededByAlternativeSolution(CharacterObject character)
			{
				return character.Tier >= 2;
			}

			// Token: 0x060010CC RID: 4300 RVA: 0x0006DD7F File Offset: 0x0006BF7F
			public override bool AlternativeSolutionCondition(out TextObject explanation)
			{
				return QuestHelper.CheckRosterForAlternativeSolution(MobileParty.MainParty.MemberRoster, base.GetTotalAlternativeSolutionNeededMenCount(), out explanation, 2, false);
			}

			// Token: 0x060010CD RID: 4301 RVA: 0x0006DD99 File Offset: 0x0006BF99
			protected override void AlternativeSolutionEndWithSuccessConsequence()
			{
				base.AlternativeSolutionHero.AddSkillXp(DefaultSkills.Charm, (float)((int)(700f + 900f * base.IssueDifficultyMultiplier)));
				this.RelationshipChangeWithIssueOwner = 5;
				GainRenownAction.Apply(Hero.MainHero, 3f, false);
			}

			// Token: 0x060010CE RID: 4302 RVA: 0x0006DDD6 File Offset: 0x0006BFD6
			protected override void AlternativeSolutionEndWithFailureConsequence()
			{
				this.RelationshipChangeWithIssueOwner = -5;
			}

			// Token: 0x060010CF RID: 4303 RVA: 0x0006DDE0 File Offset: 0x0006BFE0
			protected override void OnIssueFinalized()
			{
				if (this._prodigalSon.HeroState == Hero.CharacterStates.Disabled)
				{
					this._prodigalSon.ChangeState(Hero.CharacterStates.Released);
				}
			}

			// Token: 0x060010D0 RID: 4304 RVA: 0x0006DDFC File Offset: 0x0006BFFC
			internal static void AutoGeneratedStaticCollectObjectsProdigalSonIssue(object o, List<object> collectedObjects)
			{
				((ProdigalSonIssueBehavior.ProdigalSonIssue)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
			}

			// Token: 0x060010D1 RID: 4305 RVA: 0x0006DE0A File Offset: 0x0006C00A
			protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
			{
				base.AutoGeneratedInstanceCollectObjects(collectedObjects);
				collectedObjects.Add(this._prodigalSon);
				collectedObjects.Add(this._targetHero);
				collectedObjects.Add(this._targetHouse);
			}

			// Token: 0x060010D2 RID: 4306 RVA: 0x0006DE37 File Offset: 0x0006C037
			internal static object AutoGeneratedGetMemberValue_prodigalSon(object o)
			{
				return ((ProdigalSonIssueBehavior.ProdigalSonIssue)o)._prodigalSon;
			}

			// Token: 0x060010D3 RID: 4307 RVA: 0x0006DE44 File Offset: 0x0006C044
			internal static object AutoGeneratedGetMemberValue_targetHero(object o)
			{
				return ((ProdigalSonIssueBehavior.ProdigalSonIssue)o)._targetHero;
			}

			// Token: 0x060010D4 RID: 4308 RVA: 0x0006DE51 File Offset: 0x0006C051
			internal static object AutoGeneratedGetMemberValue_targetHouse(object o)
			{
				return ((ProdigalSonIssueBehavior.ProdigalSonIssue)o)._targetHouse;
			}

			// Token: 0x0400082C RID: 2092
			private const int IssueDurationInDays = 50;

			// Token: 0x0400082D RID: 2093
			private const int QuestDurationInDays = 24;

			// Token: 0x0400082E RID: 2094
			private const int TroopTierForAlternativeSolution = 2;

			// Token: 0x0400082F RID: 2095
			private const int RequiredSkillValueForAlternativeSolution = 120;

			// Token: 0x04000830 RID: 2096
			[SaveableField(10)]
			private readonly Hero _prodigalSon;

			// Token: 0x04000831 RID: 2097
			[SaveableField(20)]
			private readonly Hero _targetHero;

			// Token: 0x04000832 RID: 2098
			[SaveableField(30)]
			private readonly Location _targetHouse;

			// Token: 0x04000833 RID: 2099
			private Settlement _targetSettlement;
		}

		// Token: 0x020001C3 RID: 451
		public class ProdigalSonIssueQuest : QuestBase
		{
			// Token: 0x17000193 RID: 403
			// (get) Token: 0x060010D6 RID: 4310 RVA: 0x0006DE7B File Offset: 0x0006C07B
			public override TextObject Title
			{
				get
				{
					TextObject textObject = new TextObject("{=Mr2rt8g8}Prodigal Son of {CLAN_NAME}", null);
					textObject.SetTextVariable("CLAN_NAME", base.QuestGiver.Clan.Name);
					return textObject;
				}
			}

			// Token: 0x17000194 RID: 404
			// (get) Token: 0x060010D7 RID: 4311 RVA: 0x0006DEA4 File Offset: 0x0006C0A4
			public override bool IsRemainingTimeHidden
			{
				get
				{
					return false;
				}
			}

			// Token: 0x17000195 RID: 405
			// (get) Token: 0x060010D8 RID: 4312 RVA: 0x0006DEA7 File Offset: 0x0006C0A7
			private Settlement Settlement
			{
				get
				{
					return this._targetHero.CurrentSettlement;
				}
			}

			// Token: 0x17000196 RID: 406
			// (get) Token: 0x060010D9 RID: 4313 RVA: 0x0006DEB4 File Offset: 0x0006C0B4
			private int DebtWithInterest
			{
				get
				{
					return (int)((float)this.RewardGold * 1.1f);
				}
			}

			// Token: 0x17000197 RID: 407
			// (get) Token: 0x060010DA RID: 4314 RVA: 0x0006DEC4 File Offset: 0x0006C0C4
			private TextObject QuestStartedLog
			{
				get
				{
					TextObject textObject = new TextObject("{=CXw9a1i5}{QUEST_GIVER.LINK}, a {?QUEST_GIVER.GENDER}lady{?}lord{\\?} from the {QUEST_GIVER_CLAN} clan, asked you to go to {SETTLEMENT} to free {?QUEST_GIVER.GENDER}her{?}his{\\?} relative. The young man is currently held by {TARGET_HERO.LINK}, a local gang leader, because of his debts. {QUEST_GIVER.LINK} has suggested that you make an example of the gang so no one would dare to hold a nobleman again. {?QUEST_GIVER.GENDER}She{?}He{\\?} said you can easily find the house in which the young nobleman is held in the town square.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					StringHelpers.SetCharacterProperties("TARGET_HERO", this._targetHero.CharacterObject, textObject, false);
					textObject.SetTextVariable("QUEST_GIVER_CLAN", base.QuestGiver.Clan.EncyclopediaLinkWithName);
					textObject.SetTextVariable("SETTLEMENT", this.Settlement.EncyclopediaLinkWithName);
					return textObject;
				}
			}

			// Token: 0x17000198 RID: 408
			// (get) Token: 0x060010DB RID: 4315 RVA: 0x0006DF44 File Offset: 0x0006C144
			private TextObject PlayerDefeatsThugsQuestSuccessLog
			{
				get
				{
					TextObject textObject = new TextObject("{=axLR9bQo}You have defeated the thugs that held {PRODIGAL_SON.LINK} as {QUEST_GIVER.LINK} has asked you to. {?QUEST_GIVER.GENDER}Lady{?}Lord{\\?} {QUEST_GIVER.LINK} soon sends {?QUEST_GIVER.GENDER}her{?}his{\\?} best regards and a sum of {REWARD}{GOLD_ICON} as a reward.", null);
					StringHelpers.SetCharacterProperties("PRODIGAL_SON", this._prodigalSon.CharacterObject, textObject, false);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					textObject.SetTextVariable("REWARD", this.RewardGold);
					return textObject;
				}
			}

			// Token: 0x17000199 RID: 409
			// (get) Token: 0x060010DC RID: 4316 RVA: 0x0006DFA0 File Offset: 0x0006C1A0
			private TextObject PlayerPaysTheDebtQuestSuccessLog
			{
				get
				{
					TextObject textObject = new TextObject("{=skMoB7c6}You have paid the debt that {PRODIGAL_SON.LINK} owes. True to {?TARGET_HERO.GENDER}her{?}his{\\?} word {TARGET_HERO.LINK} releases the boy immediately. Soon after, {?QUEST_GIVER.GENDER}Lady{?}Lord{\\?} {QUEST_GIVER.LINK} sends {?QUEST_GIVER.GENDER}her{?}his{\\?} best regards and a sum of {REWARD}{GOLD_ICON} as a reward.", null);
					StringHelpers.SetCharacterProperties("PRODIGAL_SON", this._prodigalSon.CharacterObject, textObject, false);
					StringHelpers.SetCharacterProperties("TARGET_HERO", this._targetHero.CharacterObject, textObject, false);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					textObject.SetTextVariable("REWARD", this.RewardGold);
					return textObject;
				}
			}

			// Token: 0x1700019A RID: 410
			// (get) Token: 0x060010DD RID: 4317 RVA: 0x0006E014 File Offset: 0x0006C214
			private TextObject QuestTimeOutFailLog
			{
				get
				{
					TextObject textObject = new TextObject("{=dmijPqWn}You have failed to extract {QUEST_GIVER.LINK}'s relative captive in time. They have moved the boy to a more secure place. Its impossible to find him now. {QUEST_GIVER.LINK} will have to deal with {TARGET_HERO.LINK} himself now. {?QUEST_GIVER.GENDER}She{?}He{\\?} won't be happy to hear this.", null);
					StringHelpers.SetCharacterProperties("TARGET_HERO", this._targetHero.CharacterObject, textObject, false);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					return textObject;
				}
			}

			// Token: 0x1700019B RID: 411
			// (get) Token: 0x060010DE RID: 4318 RVA: 0x0006E060 File Offset: 0x0006C260
			private TextObject PlayerHasDefeatedQuestFailLog
			{
				get
				{
					TextObject textObject = new TextObject("{=d5a8xQos}You have failed to defeat the thugs that keep {QUEST_GIVER.LINK}'s relative captive. After your assault you learn that they move the boy to a more secure place. Now its impossible to find him. {QUEST_GIVER.LINK} will have to deal with {TARGET_HERO.LINK} himself now. {?QUEST_GIVER.GENDER}She{?}He{\\?} won't be happy to hear this.", null);
					StringHelpers.SetCharacterProperties("TARGET_HERO", this._targetHero.CharacterObject, textObject, false);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					return textObject;
				}
			}

			// Token: 0x1700019C RID: 412
			// (get) Token: 0x060010DF RID: 4319 RVA: 0x0006E0AC File Offset: 0x0006C2AC
			private TextObject PlayerConvincesGangLeaderQuestSuccessLog
			{
				get
				{
					TextObject textObject = new TextObject("{=Rb7g1U2s}You have convinced {TARGET_HERO.LINK} to release {PRODIGAL_SON.LINK}. Soon after, {?QUEST_GIVER.GENDER}Lady{?}Lord{\\?} {QUEST_GIVER.LINK} sends {?QUEST_GIVER.GENDER}her{?}his{\\?} best regards and a sum of {REWARD}{GOLD_ICON} as a reward.", null);
					StringHelpers.SetCharacterProperties("PRODIGAL_SON", this._prodigalSon.CharacterObject, textObject, false);
					StringHelpers.SetCharacterProperties("TARGET_HERO", this._targetHero.CharacterObject, textObject, false);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					textObject.SetTextVariable("REWARD", this.RewardGold);
					return textObject;
				}
			}

			// Token: 0x1700019D RID: 413
			// (get) Token: 0x060010E0 RID: 4320 RVA: 0x0006E120 File Offset: 0x0006C320
			private TextObject WarDeclaredQuestCancelLog
			{
				get
				{
					TextObject textObject = new TextObject("{=VuqZuSe2}Your clan is now at war with the {QUEST_GIVER.LINK}'s faction. Your agreement has been canceled.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					return textObject;
				}
			}

			// Token: 0x1700019E RID: 414
			// (get) Token: 0x060010E1 RID: 4321 RVA: 0x0006E154 File Offset: 0x0006C354
			private TextObject PlayerDeclaredWarQuestLogText
			{
				get
				{
					TextObject textObject = new TextObject("{=bqeWVVEE}Your actions have started a war with {QUEST_GIVER.LINK}'s faction. {?QUEST_GIVER.GENDER}She{?}He{\\?} cancels your agreement and the quest is a failure.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					return textObject;
				}
			}

			// Token: 0x1700019F RID: 415
			// (get) Token: 0x060010E2 RID: 4322 RVA: 0x0006E186 File Offset: 0x0006C386
			private TextObject CrimeRatingCancelLog
			{
				get
				{
					TextObject textObject = new TextObject("{=oulvvl52}You are accused in {SETTLEMENT} of a crime, and {QUEST_GIVER.LINK} no longer trusts you in this matter.", null);
					textObject.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, false);
					textObject.SetTextVariable("SETTLEMENT", this.Settlement.EncyclopediaLinkWithName);
					return textObject;
				}
			}

			// Token: 0x060010E3 RID: 4323 RVA: 0x0006E1C1 File Offset: 0x0006C3C1
			public ProdigalSonIssueQuest(string questId, Hero questGiver, Hero targetHero, Hero prodigalSon, Location targetHouse, float questDifficulty, CampaignTime duration, int rewardGold)
				: base(questId, questGiver, duration, rewardGold)
			{
				this._targetHero = targetHero;
				this._prodigalSon = prodigalSon;
				this._targetHouse = targetHouse;
				this._questDifficulty = questDifficulty;
				this.SetDialogs();
				base.InitializeQuestOnCreation();
			}

			// Token: 0x060010E4 RID: 4324 RVA: 0x0006E1FC File Offset: 0x0006C3FC
			protected override void SetDialogs()
			{
				TextObject textObject = new TextObject("{=bQnVtegC}Good, even better. [ib:confident][if:convo_astonished]You can find the house easily when you go to {SETTLEMENT} and walk around the town square. Or you could just speak to this gang leader, {TARGET_HERO.LINK}, and make {?TARGET_HERO.GENDER}her{?}him{\\?} understand and get my boy released. Good luck. I await good news.", null);
				StringHelpers.SetCharacterProperties("TARGET_HERO", this._targetHero.CharacterObject, textObject, false);
				Settlement settlement = ((this._targetHero.CurrentSettlement != null) ? this._targetHero.CurrentSettlement : this._targetHero.PartyBelongedTo.HomeSettlement);
				textObject.SetTextVariable("SETTLEMENT", settlement.EncyclopediaLinkWithName);
				this.OfferDialogFlow = DialogFlow.CreateDialogFlow("issue_classic_quest_start", 100).NpcLine(textObject, null, null, null, null).Condition(new ConversationSentence.OnConditionDelegate(this.is_talking_to_quest_giver))
					.Consequence(new ConversationSentence.OnConsequenceDelegate(this.QuestAcceptedConsequences))
					.CloseDialog();
				this.DiscussDialogFlow = DialogFlow.CreateDialogFlow("quest_discuss", 100).NpcLine(new TextObject("{=TkYk5yxn}Yes? Go already. Get our boy back.[if:convo_excited]", null), null, null, null, null).Condition(new ConversationSentence.OnConditionDelegate(this.is_talking_to_quest_giver))
					.BeginPlayerOptions(null, false)
					.PlayerOption(new TextObject("{=kqXxvtwQ}Don't worry I'll free him.", null), null, null, null)
					.NpcLine(new TextObject("{=ddEu5IFQ}I hope so.", null), null, null, null, null)
					.Consequence(new ConversationSentence.OnConsequenceDelegate(MapEventHelper.OnConversationEnd))
					.CloseDialog()
					.PlayerOption(new TextObject("{=Jss9UqZC}I'll go right away", null), null, null, null)
					.NpcLine(new TextObject("{=IdKG3IaS}Good to hear that.", null), null, null, null, null)
					.Consequence(new ConversationSentence.OnConsequenceDelegate(MapEventHelper.OnConversationEnd))
					.CloseDialog()
					.EndPlayerOptions()
					.CloseDialog();
				Campaign.Current.ConversationManager.AddDialogFlow(this.GetTargetHeroDialogFlow(), this);
				Campaign.Current.ConversationManager.AddDialogFlow(this.GetProdigalSonDialogFlow(), this);
			}

			// Token: 0x060010E5 RID: 4325 RVA: 0x0006E39B File Offset: 0x0006C59B
			protected override void InitializeQuestOnGameLoad()
			{
				this.SetDialogs();
			}

			// Token: 0x060010E6 RID: 4326 RVA: 0x0006E3A3 File Offset: 0x0006C5A3
			protected override void HourlyTick()
			{
			}

			// Token: 0x060010E7 RID: 4327 RVA: 0x0006E3A8 File Offset: 0x0006C5A8
			protected override void RegisterEvents()
			{
				CampaignEvents.BeforeMissionOpenedEvent.AddNonSerializedListener(this, new Action(this.BeforeMissionOpened));
				CampaignEvents.MissionTickEvent.AddNonSerializedListener(this, new Action<float>(this.OnMissionTick));
				CampaignEvents.WarDeclared.AddNonSerializedListener(this, new Action<IFaction, IFaction, DeclareWarAction.DeclareWarDetail>(this.OnWarDeclared));
				CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, new Action<Clan, Kingdom, Kingdom, ChangeKingdomAction.ChangeKingdomActionDetail, bool>(this.OnClanChangedKingdom));
				CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, new Action<Hero, Hero, KillCharacterAction.KillCharacterActionDetail, bool>(this.OnHeroKilled));
				CampaignEvents.MapEventStarted.AddNonSerializedListener(this, new Action<MapEvent, PartyBase, PartyBase>(this.OnMapEventStarted));
				CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this, new Action<IMission>(this.OnMissionStarted));
			}

			// Token: 0x060010E8 RID: 4328 RVA: 0x0006E456 File Offset: 0x0006C656
			private void OnMissionStarted(IMission mission)
			{
				ICampaignMission campaignMission = CampaignMission.Current;
				if (((campaignMission != null) ? campaignMission.Location : null) == this._targetHouse)
				{
					this._isFirstMissionTick = true;
				}
			}

			// Token: 0x060010E9 RID: 4329 RVA: 0x0006E478 File Offset: 0x0006C678
			public override void OnHeroCanHaveCampaignIssuesInfoIsRequested(Hero hero, ref bool result)
			{
				if (hero == this._prodigalSon || hero == this._targetHero)
				{
					result = false;
				}
			}

			// Token: 0x060010EA RID: 4330 RVA: 0x0006E48F File Offset: 0x0006C68F
			public override void OnHeroCanMoveToSettlementInfoIsRequested(Hero hero, ref bool result)
			{
				if (hero == this._prodigalSon)
				{
					result = false;
				}
			}

			// Token: 0x060010EB RID: 4331 RVA: 0x0006E49D File Offset: 0x0006C69D
			private void OnMapEventStarted(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
			{
				if (QuestHelper.CheckMinorMajorCoercion(this, mapEvent, attackerParty))
				{
					QuestHelper.ApplyGenericMinorMajorCoercionConsequences(this, mapEvent);
				}
			}

			// Token: 0x060010EC RID: 4332 RVA: 0x0006E4B0 File Offset: 0x0006C6B0
			private void OnHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification = true)
			{
				if (victim == this._targetHero || victim == this._prodigalSon)
				{
					TextObject textObject = ((detail == KillCharacterAction.KillCharacterActionDetail.Lost) ? this.TargetHeroDisappearedLogText : this.TargetHeroDiedLogText);
					StringHelpers.SetCharacterProperties("QUEST_TARGET", victim.CharacterObject, textObject, false);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					base.AddLog(textObject, false);
					base.CompleteQuestWithCancel(null);
				}
			}

			// Token: 0x060010ED RID: 4333 RVA: 0x0006E51D File Offset: 0x0006C71D
			protected override void OnTimedOut()
			{
				this.FinishQuestFail1();
			}

			// Token: 0x060010EE RID: 4334 RVA: 0x0006E525 File Offset: 0x0006C725
			protected override void OnFinalize()
			{
				this._targetHouse.RemoveReservation();
			}

			// Token: 0x060010EF RID: 4335 RVA: 0x0006E534 File Offset: 0x0006C734
			private void BeforeMissionOpened()
			{
				if (Settlement.CurrentSettlement == this.Settlement && LocationComplex.Current != null)
				{
					if (LocationComplex.Current.GetLocationOfCharacter(this._prodigalSon) == null)
					{
						this.SpawnProdigalSonInHouse();
						if (!this._isHouseFightFinished)
						{
							this.SpawnThugsInHouse();
							this._isMissionFightInitialized = false;
						}
					}
					using (List<AccompanyingCharacter>.Enumerator enumerator = PlayerEncounter.LocationEncounter.CharactersAccompanyingPlayer.GetEnumerator())
					{
						while (enumerator.MoveNext())
						{
							AccompanyingCharacter character = enumerator.Current;
							if (!character.CanEnterLocation(this._targetHouse))
							{
								character.AllowEntranceToLocations((Location x) => character.CanEnterLocation(x) || x == this._targetHouse);
							}
						}
					}
				}
			}

			// Token: 0x060010F0 RID: 4336 RVA: 0x0006E608 File Offset: 0x0006C808
			private void OnMissionTick(float dt)
			{
				if (CampaignMission.Current.Location == this._targetHouse)
				{
					Mission mission = Mission.Current;
					if (this._isFirstMissionTick)
					{
						Mission.Current.Agents.First((Agent x) => x.Character == this._prodigalSon.CharacterObject).GetComponent<CampaignAgentComponent>().AgentNavigator.RemoveBehaviorGroup<AlarmedBehaviorGroup>();
						this._isFirstMissionTick = false;
					}
					if (!this._isMissionFightInitialized && !this._isHouseFightFinished && mission.Agents.Count > 0)
					{
						this._isMissionFightInitialized = true;
						MissionFightHandler missionBehavior = mission.GetMissionBehavior<MissionFightHandler>();
						List<Agent> list = new List<Agent>();
						List<Agent> list2 = new List<Agent>();
						foreach (Agent agent in mission.Agents)
						{
							if (agent.IsEnemyOf(Agent.Main))
							{
								list.Add(agent);
							}
							else if (agent.Team == Agent.Main.Team)
							{
								list2.Add(agent);
							}
						}
						missionBehavior.StartCustomFight(list2, list, false, false, new MissionFightHandler.OnFightEndDelegate(this.HouseFightFinished), float.Epsilon);
						foreach (Agent agent2 in list)
						{
							agent2.Defensiveness = 2f;
						}
					}
				}
			}

			// Token: 0x060010F1 RID: 4337 RVA: 0x0006E77C File Offset: 0x0006C97C
			private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
			{
				if (base.QuestGiver.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction))
				{
					if (detail == DeclareWarAction.DeclareWarDetail.CausedByCrimeRatingChange)
					{
						this.RelationshipChangeWithQuestGiver = -5;
						Tuple<TraitObject, int>[] effectedTraits = new Tuple<TraitObject, int>[]
						{
							new Tuple<TraitObject, int>(DefaultTraits.Honor, -50)
						};
						TraitLevelingHelper.OnIssueSolvedThroughQuest(Hero.MainHero, effectedTraits);
					}
					if (DiplomacyHelper.IsWarCausedByPlayer(faction1, faction2, detail))
					{
						base.CompleteQuestWithFail(this.PlayerDeclaredWarQuestLogText);
						return;
					}
					base.CompleteQuestWithCancel((detail == DeclareWarAction.DeclareWarDetail.CausedByCrimeRatingChange) ? this.CrimeRatingCancelLog : this.WarDeclaredQuestCancelLog);
				}
			}

			// Token: 0x060010F2 RID: 4338 RVA: 0x0006E804 File Offset: 0x0006CA04
			private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
			{
				if (clan == Clan.PlayerClan && ((newKingdom != null && newKingdom.IsAtWarWith(base.QuestGiver.MapFaction)) || (newKingdom == null && clan.IsAtWarWith(base.QuestGiver.MapFaction))))
				{
					base.CompleteQuestWithCancel(this.WarDeclaredQuestCancelLog);
				}
			}

			// Token: 0x060010F3 RID: 4339 RVA: 0x0006E854 File Offset: 0x0006CA54
			private void HouseFightFinished(bool isPlayerSideWon)
			{
				if (isPlayerSideWon)
				{
					Agent agent = Mission.Current.Agents.FirstOrDefaultQ((Agent x) => x.Character == this._prodigalSon.CharacterObject);
					if (agent == null)
					{
						Debug.Print("Prodigal son id: " + this._prodigalSon.CharacterObject.StringId, 0, Debug.DebugColor.White, 17592186044416UL);
						Debug.Print("Mission agent count: " + Mission.Current.Agents.Count, 0, Debug.DebugColor.White, 17592186044416UL);
						foreach (Agent agent2 in Mission.Current.Agents)
						{
							Debug.Print(string.Concat(new object[]
							{
								"Agent: ",
								agent2.Character.Name,
								", id: ",
								agent2.Character.StringId,
								", team: ",
								agent2.Team
							}), 0, Debug.DebugColor.White, 17592186044416UL);
						}
					}
					if (agent.Position.Distance(Agent.Main.Position) > agent.GetInteractionDistanceToUsable(Agent.Main))
					{
						ScriptBehavior.AddTargetWithDelegate(agent, new ScriptBehavior.SelectTargetDelegate(this.SelectPlayerAsTarget), null, new ScriptBehavior.OnTargetReachedDelegate(this.OnTargetReached), 0f);
					}
					else
					{
						Agent agent3 = null;
						UsableMachine usableMachine = null;
						WorldFrame invalid = WorldFrame.Invalid;
						this.OnTargetReached(agent, ref agent3, ref usableMachine, ref invalid);
					}
				}
				else
				{
					this.FinishQuestFail2();
				}
				this._isHouseFightFinished = true;
			}

			// Token: 0x060010F4 RID: 4340 RVA: 0x0006E9F4 File Offset: 0x0006CBF4
			private bool OnTargetReached(Agent agent, ref Agent targetAgent, ref UsableMachine targetUsableMachine, ref WorldFrame targetFrame)
			{
				Mission.Current.GetMissionBehavior<MissionConversationLogic>().StartConversation(agent, false, false);
				targetAgent = null;
				return false;
			}

			// Token: 0x060010F5 RID: 4341 RVA: 0x0006EA0C File Offset: 0x0006CC0C
			private bool SelectPlayerAsTarget(Agent agent, ref Agent targetAgent, ref UsableMachine targetUsableMachine, ref WorldFrame targetFrame, ref float customTargetReachedRangeThreshold, ref float customTargetReachedRotationThreshold)
			{
				targetAgent = null;
				if (agent.Position.Distance(Agent.Main.Position) > agent.GetInteractionDistanceToUsable(Agent.Main))
				{
					targetAgent = Agent.Main;
				}
				return targetAgent != null;
			}

			// Token: 0x060010F6 RID: 4342 RVA: 0x0006EA50 File Offset: 0x0006CC50
			private void SpawnProdigalSonInHouse()
			{
				Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(this._prodigalSon.CharacterObject.Race, "_settlement");
				LocationCharacter locationCharacter = new LocationCharacter(new AgentData(new SimpleAgentOrigin(this._prodigalSon.CharacterObject, -1, null, default(UniqueTroopDescriptor))).Monster(monsterWithSuffix), new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors), "npc_common", true, LocationCharacter.CharacterRelations.Neutral, null, true, false, null, false, false, true, null, false);
				this._targetHouse.AddCharacter(locationCharacter);
			}

			// Token: 0x060010F7 RID: 4343 RVA: 0x0006EAD8 File Offset: 0x0006CCD8
			private void SpawnThugsInHouse()
			{
				CharacterObject @object = MBObjectManager.Instance.GetObject<CharacterObject>("gangster_1");
				CharacterObject object2 = MBObjectManager.Instance.GetObject<CharacterObject>("gangster_2");
				CharacterObject object3 = MBObjectManager.Instance.GetObject<CharacterObject>("gangster_3");
				List<CharacterObject> list = new List<CharacterObject>();
				if (this._questDifficulty < 0.4f)
				{
					list.Add(@object);
					list.Add(@object);
					if (this._questDifficulty >= 0.2f)
					{
						list.Add(object2);
					}
				}
				else if (this._questDifficulty < 0.6f)
				{
					list.Add(@object);
					list.Add(object2);
					list.Add(object2);
				}
				else
				{
					list.Add(object2);
					list.Add(object3);
					list.Add(object3);
				}
				foreach (CharacterObject characterObject in list)
				{
					Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(characterObject.Race, "_settlement");
					LocationCharacter locationCharacter = new LocationCharacter(new AgentData(new SimpleAgentOrigin(characterObject, -1, null, default(UniqueTroopDescriptor))).Monster(monsterWithSuffix), new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors), "npc_common", true, LocationCharacter.CharacterRelations.Enemy, null, true, false, null, false, false, true, null, false);
					this._targetHouse.AddCharacter(locationCharacter);
				}
			}

			// Token: 0x060010F8 RID: 4344 RVA: 0x0006EC28 File Offset: 0x0006CE28
			private void QuestAcceptedConsequences()
			{
				base.StartQuest();
				base.AddTrackedObject(this.Settlement);
				base.AddTrackedObject(this._targetHero);
				base.AddLog(this.QuestStartedLog, false);
			}

			// Token: 0x060010F9 RID: 4345 RVA: 0x0006EC58 File Offset: 0x0006CE58
			private DialogFlow GetProdigalSonDialogFlow()
			{
				return DialogFlow.CreateDialogFlow("start", 125).NpcLine("{=DYq30shK}Thank you, {?PLAYER.GENDER}milady{?}sir{\\?}.", null, null, null, null).Condition(() => Hero.OneToOneConversationHero == this._prodigalSon)
					.NpcLine("{=K8TSoRSD}Did {?QUEST_GIVER.GENDER}Lady{?}Lord{\\?} {QUEST_GIVER.LINK} send you to rescue me?", null, null, null, null)
					.Condition(delegate
					{
						StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, null, false);
						return true;
					})
					.PlayerLine("{=ln3bGyIO}Yes, I'm here to take you back.", null, null, null)
					.NpcLine("{=evIohG6b}Thank you, but there's no need. Once we are out of here I can manage to return on my own.[if:convo_happy] I appreciate your efforts. I'll tell everyone in my clan of your heroism.", null, null, null, null)
					.NpcLine("{=qsJxhNGZ}Safe travels {?PLAYER.GENDER}milady{?}sir{\\?}.", null, null, null, null)
					.Consequence(delegate
					{
						Mission.Current.Agents.First((Agent x) => x.Character == this._prodigalSon.CharacterObject).GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<DailyBehaviorGroup>().DisableScriptedBehavior();
						Campaign.Current.ConversationManager.ConversationEndOneShot += this.OnEndHouseMissionDialog;
					})
					.CloseDialog();
			}

			// Token: 0x060010FA RID: 4346 RVA: 0x0006ECF0 File Offset: 0x0006CEF0
			private DialogFlow GetTargetHeroDialogFlow()
			{
				DialogFlow dialogFlow = DialogFlow.CreateDialogFlow("start", 125).BeginNpcOptions(null, false).NpcOption(new TextObject("{=M0vxXQGB}Yes? Do you have something to say?[ib:closed][if:convo_nonchalant]", null), () => Hero.OneToOneConversationHero == this._targetHero && !this._playerTalkedToTargetHero, null, null, null, null)
					.Consequence(delegate
					{
						StringHelpers.SetCharacterProperties("PRODIGAL_SON", this._prodigalSon.CharacterObject, null, false);
						this._playerTalkedToTargetHero = true;
					})
					.PlayerLine("{=K5DgDU2a}I am here for the boy. {PRODIGAL_SON.LINK}. You know who I mean.", null, null, null)
					.GotoDialogState("start")
					.NpcOption(new TextObject("{=I979VDEn}Yes, did you bring {GOLD_AMOUNT}{GOLD_ICON}? [ib:hip][if:convo_stern]That's what he owes... With an interest of course.", null), delegate()
					{
						bool flag = Hero.OneToOneConversationHero == this._targetHero && this._playerTalkedToTargetHero;
						if (flag)
						{
							MBTextManager.SetTextVariable("GOLD_AMOUNT", this.DebtWithInterest);
						}
						return flag;
					}, null, null, null, null)
					.BeginPlayerOptions(null, false)
					.PlayerOption("{=IboStvbL}Here is the money, now release him!", null, null, null)
					.ClickableCondition(delegate(out TextObject explanation)
					{
						bool result = false;
						if (Hero.MainHero.Gold >= this.DebtWithInterest)
						{
							explanation = null;
							result = true;
						}
						else
						{
							explanation = new TextObject("{=YuLLsAUb}You don't have {GOLD_AMOUNT}{GOLD_ICON}.", null);
							explanation.SetTextVariable("GOLD_AMOUNT", this.DebtWithInterest);
						}
						return result;
					})
					.NpcLine("{=7k03GxZ1}It's great doing business with you. I'll order my men to release him immediately.[if:convo_mocking_teasing]", null, null, null, null)
					.Consequence(new ConversationSentence.OnConsequenceDelegate(this.FinishQuestSuccess4))
					.CloseDialog()
					.PlayerOption("{=9pTkQ5o2}It would be in your interest to let this young nobleman go...", null, null, null)
					.Condition(() => !this._playerTriedToPersuade)
					.Consequence(delegate
					{
						this._playerTriedToPersuade = true;
						this._task = this.GetPersuasionTask();
						this.persuasion_start_on_consequence();
					})
					.GotoDialogState("persuade_gang_start_reservation")
					.PlayerOption("{=AwZhx2tT}I will be back.", null, null, null)
					.NpcLine("{=0fp67gxl}Have a good day.", null, null, null, null)
					.CloseDialog()
					.EndPlayerOptions()
					.EndNpcOptions();
				this.AddPersuasionDialogs(dialogFlow);
				return dialogFlow;
			}

			// Token: 0x060010FB RID: 4347 RVA: 0x0006EE30 File Offset: 0x0006D030
			private void AddPersuasionDialogs(DialogFlow dialog)
			{
				dialog.AddDialogLine("persuade_gang_introduction", "persuade_gang_start_reservation", "persuade_gang_player_option", "{=EIsQnfLP}Tell me how it's in my interest...[ib:closed][if:convo_nonchalant]", new ConversationSentence.OnConditionDelegate(this.persuasion_start_on_condition), null, this, 100, null, null, null);
				dialog.AddDialogLine("persuade_gang_success", "persuade_gang_start_reservation", "close_window", "{=alruamIW}Hmm... You may be right. It's not worth it. I'll release the boy immediately.[ib:hip][if:convo_pondering]", new ConversationSentence.OnConditionDelegate(ConversationManager.GetPersuasionProgressSatisfied), new ConversationSentence.OnConsequenceDelegate(this.persuasion_success_on_consequence), this, int.MaxValue, null, null, null);
				dialog.AddDialogLine("persuade_gang_failed", "persuade_gang_start_reservation", "start", "{=1YGgXOB7}Meh... Do you think ruling the streets of a city is easy? You underestimate us. Now, about the money.[ib:closed2][if:convo_nonchalant]", null, new ConversationSentence.OnConsequenceDelegate(ConversationManager.EndPersuasion), this, 100, null, null, null);
				string id = "persuade_gang_player_option_1";
				string inputToken = "persuade_gang_player_option";
				string outputToken = "persuade_gang_player_option_response";
				string text = "{=!}{PERSUADE_GANG_ATTEMPT_1}";
				ConversationSentence.OnConditionDelegate conditionDelegate = new ConversationSentence.OnConditionDelegate(this.persuasion_select_option_1_on_condition);
				ConversationSentence.OnConsequenceDelegate consequenceDelegate = new ConversationSentence.OnConsequenceDelegate(this.persuasion_select_option_1_on_consequence);
				ConversationSentence.OnPersuasionOptionDelegate persuasionOptionDelegate = new ConversationSentence.OnPersuasionOptionDelegate(this.persuasion_setup_option_1);
				ConversationSentence.OnClickableConditionDelegate clickableConditionDelegate = new ConversationSentence.OnClickableConditionDelegate(this.persuasion_clickable_option_1_on_condition);
				dialog.AddPlayerLine(id, inputToken, outputToken, text, conditionDelegate, consequenceDelegate, this, 100, clickableConditionDelegate, persuasionOptionDelegate, null, null);
				string id2 = "persuade_gang_player_option_2";
				string inputToken2 = "persuade_gang_player_option";
				string outputToken2 = "persuade_gang_player_option_response";
				string text2 = "{=!}{PERSUADE_GANG_ATTEMPT_2}";
				ConversationSentence.OnConditionDelegate conditionDelegate2 = new ConversationSentence.OnConditionDelegate(this.persuasion_select_option_2_on_condition);
				ConversationSentence.OnConsequenceDelegate consequenceDelegate2 = new ConversationSentence.OnConsequenceDelegate(this.persuasion_select_option_2_on_consequence);
				persuasionOptionDelegate = new ConversationSentence.OnPersuasionOptionDelegate(this.persuasion_setup_option_2);
				clickableConditionDelegate = new ConversationSentence.OnClickableConditionDelegate(this.persuasion_clickable_option_2_on_condition);
				dialog.AddPlayerLine(id2, inputToken2, outputToken2, text2, conditionDelegate2, consequenceDelegate2, this, 100, clickableConditionDelegate, persuasionOptionDelegate, null, null);
				string id3 = "persuade_gang_player_option_3";
				string inputToken3 = "persuade_gang_player_option";
				string outputToken3 = "persuade_gang_player_option_response";
				string text3 = "{=!}{PERSUADE_GANG_ATTEMPT_3}";
				ConversationSentence.OnConditionDelegate conditionDelegate3 = new ConversationSentence.OnConditionDelegate(this.persuasion_select_option_3_on_condition);
				ConversationSentence.OnConsequenceDelegate consequenceDelegate3 = new ConversationSentence.OnConsequenceDelegate(this.persuasion_select_option_3_on_consequence);
				persuasionOptionDelegate = new ConversationSentence.OnPersuasionOptionDelegate(this.persuasion_setup_option_3);
				clickableConditionDelegate = new ConversationSentence.OnClickableConditionDelegate(this.persuasion_clickable_option_3_on_condition);
				dialog.AddPlayerLine(id3, inputToken3, outputToken3, text3, conditionDelegate3, consequenceDelegate3, this, 100, clickableConditionDelegate, persuasionOptionDelegate, null, null);
				dialog.AddDialogLine("persuade_gang_option_reaction", "persuade_gang_player_option_response", "persuade_gang_start_reservation", "{=!}{PERSUASION_REACTION}", new ConversationSentence.OnConditionDelegate(this.persuasion_selected_option_response_on_condition), new ConversationSentence.OnConsequenceDelegate(this.persuasion_selected_option_response_on_consequence), this, 100, null, null, null);
			}

			// Token: 0x060010FC RID: 4348 RVA: 0x0006F00A File Offset: 0x0006D20A
			private bool is_talking_to_quest_giver()
			{
				return Hero.OneToOneConversationHero == base.QuestGiver;
			}

			// Token: 0x060010FD RID: 4349 RVA: 0x0006F01C File Offset: 0x0006D21C
			private bool persuasion_start_on_condition()
			{
				if (Hero.OneToOneConversationHero == this._targetHero && !ConversationManager.GetPersuasionIsFailure())
				{
					return this._task.Options.Any((PersuasionOptionArgs x) => !x.IsBlocked);
				}
				return false;
			}

			// Token: 0x060010FE RID: 4350 RVA: 0x0006F070 File Offset: 0x0006D270
			private void persuasion_selected_option_response_on_consequence()
			{
				Tuple<PersuasionOptionArgs, PersuasionOptionResult> tuple = ConversationManager.GetPersuasionChosenOptions().Last<Tuple<PersuasionOptionArgs, PersuasionOptionResult>>();
				float difficulty = Campaign.Current.Models.PersuasionModel.GetDifficulty(PersuasionDifficulty.Hard);
				float moveToNextStageChance;
				float blockRandomOptionChance;
				Campaign.Current.Models.PersuasionModel.GetEffectChances(tuple.Item1, out moveToNextStageChance, out blockRandomOptionChance, difficulty);
				this._task.ApplyEffects(moveToNextStageChance, blockRandomOptionChance);
			}

			// Token: 0x060010FF RID: 4351 RVA: 0x0006F0CC File Offset: 0x0006D2CC
			private bool persuasion_selected_option_response_on_condition()
			{
				PersuasionOptionResult item = ConversationManager.GetPersuasionChosenOptions().Last<Tuple<PersuasionOptionArgs, PersuasionOptionResult>>().Item2;
				MBTextManager.SetTextVariable("PERSUASION_REACTION", PersuasionHelper.GetDefaultPersuasionOptionReaction(item), false);
				if (item == PersuasionOptionResult.CriticalFailure)
				{
					this._task.BlockAllOptions();
				}
				return true;
			}

			// Token: 0x06001100 RID: 4352 RVA: 0x0006F10C File Offset: 0x0006D30C
			private bool persuasion_select_option_1_on_condition()
			{
				if (this._task.Options.Count > 0)
				{
					TextObject textObject = new TextObject("{=bSo9hKwr}{PERSUASION_OPTION_LINE} {SUCCESS_CHANCE}", null);
					textObject.SetTextVariable("SUCCESS_CHANCE", PersuasionHelper.ShowSuccess(this._task.Options.ElementAt(0), false));
					textObject.SetTextVariable("PERSUASION_OPTION_LINE", this._task.Options.ElementAt(0).Line);
					MBTextManager.SetTextVariable("PERSUADE_GANG_ATTEMPT_1", textObject, false);
					return true;
				}
				return false;
			}

			// Token: 0x06001101 RID: 4353 RVA: 0x0006F18C File Offset: 0x0006D38C
			private bool persuasion_select_option_2_on_condition()
			{
				if (this._task.Options.Count > 1)
				{
					TextObject textObject = new TextObject("{=bSo9hKwr}{PERSUASION_OPTION_LINE} {SUCCESS_CHANCE}", null);
					textObject.SetTextVariable("SUCCESS_CHANCE", PersuasionHelper.ShowSuccess(this._task.Options.ElementAt(1), false));
					textObject.SetTextVariable("PERSUASION_OPTION_LINE", this._task.Options.ElementAt(1).Line);
					MBTextManager.SetTextVariable("PERSUADE_GANG_ATTEMPT_2", textObject, false);
					return true;
				}
				return false;
			}

			// Token: 0x06001102 RID: 4354 RVA: 0x0006F20C File Offset: 0x0006D40C
			private bool persuasion_select_option_3_on_condition()
			{
				if (this._task.Options.Count > 2)
				{
					TextObject textObject = new TextObject("{=bSo9hKwr}{PERSUASION_OPTION_LINE} {SUCCESS_CHANCE}", null);
					textObject.SetTextVariable("SUCCESS_CHANCE", PersuasionHelper.ShowSuccess(this._task.Options.ElementAt(2), false));
					textObject.SetTextVariable("PERSUASION_OPTION_LINE", this._task.Options.ElementAt(2).Line);
					MBTextManager.SetTextVariable("PERSUADE_GANG_ATTEMPT_3", textObject, false);
					return true;
				}
				return false;
			}

			// Token: 0x06001103 RID: 4355 RVA: 0x0006F28C File Offset: 0x0006D48C
			private void persuasion_select_option_1_on_consequence()
			{
				if (this._task.Options.Count > 0)
				{
					this._task.Options[0].BlockTheOption(true);
				}
			}

			// Token: 0x06001104 RID: 4356 RVA: 0x0006F2B8 File Offset: 0x0006D4B8
			private void persuasion_select_option_2_on_consequence()
			{
				if (this._task.Options.Count > 1)
				{
					this._task.Options[1].BlockTheOption(true);
				}
			}

			// Token: 0x06001105 RID: 4357 RVA: 0x0006F2E4 File Offset: 0x0006D4E4
			private void persuasion_select_option_3_on_consequence()
			{
				if (this._task.Options.Count > 2)
				{
					this._task.Options[2].BlockTheOption(true);
				}
			}

			// Token: 0x06001106 RID: 4358 RVA: 0x0006F310 File Offset: 0x0006D510
			private PersuasionOptionArgs persuasion_setup_option_1()
			{
				return this._task.Options.ElementAt(0);
			}

			// Token: 0x06001107 RID: 4359 RVA: 0x0006F323 File Offset: 0x0006D523
			private PersuasionOptionArgs persuasion_setup_option_2()
			{
				return this._task.Options.ElementAt(1);
			}

			// Token: 0x06001108 RID: 4360 RVA: 0x0006F336 File Offset: 0x0006D536
			private PersuasionOptionArgs persuasion_setup_option_3()
			{
				return this._task.Options.ElementAt(2);
			}

			// Token: 0x06001109 RID: 4361 RVA: 0x0006F34C File Offset: 0x0006D54C
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

			// Token: 0x0600110A RID: 4362 RVA: 0x0006F3B4 File Offset: 0x0006D5B4
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

			// Token: 0x0600110B RID: 4363 RVA: 0x0006F41C File Offset: 0x0006D61C
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

			// Token: 0x0600110C RID: 4364 RVA: 0x0006F483 File Offset: 0x0006D683
			private void persuasion_success_on_consequence()
			{
				ConversationManager.EndPersuasion();
				this.FinishQuestSuccess3();
			}

			// Token: 0x0600110D RID: 4365 RVA: 0x0006F490 File Offset: 0x0006D690
			private void OnEndHouseMissionDialog()
			{
				Campaign.Current.GameMenuManager.NextLocation = LocationComplex.Current.GetLocationWithId("center");
				Campaign.Current.GameMenuManager.PreviousLocation = CampaignMission.Current.Location;
				Mission.Current.EndMission();
				this.FinishQuestSuccess1();
			}

			// Token: 0x0600110E RID: 4366 RVA: 0x0006F4E4 File Offset: 0x0006D6E4
			private PersuasionTask GetPersuasionTask()
			{
				PersuasionTask persuasionTask = new PersuasionTask(0);
				persuasionTask.FinalFailLine = TextObject.GetEmpty();
				persuasionTask.TryLaterLine = TextObject.GetEmpty();
				persuasionTask.SpokenLine = new TextObject("{=6P1ruzsC}Maybe...", null);
				PersuasionOptionArgs option = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Calculating, TraitEffect.Positive, PersuasionArgumentStrength.ExtremelyHard, true, new TextObject("{=Lol4clzR}Look, it was a good try, but they're not going to pay. Releasing the kid is the only move that makes sense.", null), null, false, false, false);
				persuasionTask.AddOptionToTask(option);
				PersuasionOptionArgs option2 = new PersuasionOptionArgs(DefaultSkills.Roguery, DefaultTraits.Mercy, TraitEffect.Negative, PersuasionArgumentStrength.Hard, false, new TextObject("{=wJCVlVF7}These nobles aren't like you and me. They've kept their wealth by crushing people like you for generations. Don't mess with them.", null), null, false, false, false);
				persuasionTask.AddOptionToTask(option2);
				PersuasionOptionArgs option3 = new PersuasionOptionArgs(DefaultSkills.Roguery, DefaultTraits.Generosity, TraitEffect.Positive, PersuasionArgumentStrength.Normal, false, new TextObject("{=o1KOn4WZ}If you let this boy go, his family will remember you did them a favor. That's a better deal for you than a fight you can't hope to win.", null), null, false, false, false);
				persuasionTask.AddOptionToTask(option3);
				return persuasionTask;
			}

			// Token: 0x0600110F RID: 4367 RVA: 0x0006F59A File Offset: 0x0006D79A
			private void persuasion_start_on_consequence()
			{
				ConversationManager.StartPersuasion(2f, 1f, 1f, 2f, 2f, 0f, PersuasionDifficulty.Hard);
			}

			// Token: 0x06001110 RID: 4368 RVA: 0x0006F5C0 File Offset: 0x0006D7C0
			private void FinishQuestSuccess1()
			{
				base.CompleteQuestWithSuccess();
				base.AddLog(this.PlayerDefeatsThugsQuestSuccessLog, false);
				ChangeRelationAction.ApplyPlayerRelation(base.QuestGiver, 5, true, true);
				GainRenownAction.Apply(Hero.MainHero, 3f, false);
				GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, this.RewardGold, false);
			}

			// Token: 0x06001111 RID: 4369 RVA: 0x0006F614 File Offset: 0x0006D814
			private void FinishQuestSuccess3()
			{
				base.CompleteQuestWithSuccess();
				base.AddLog(this.PlayerConvincesGangLeaderQuestSuccessLog, false);
				ChangeRelationAction.ApplyPlayerRelation(base.QuestGiver, 5, true, true);
				GainRenownAction.Apply(Hero.MainHero, 1f, false);
				GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, this.RewardGold, false);
			}

			// Token: 0x06001112 RID: 4370 RVA: 0x0006F668 File Offset: 0x0006D868
			private void FinishQuestSuccess4()
			{
				GainRenownAction.Apply(Hero.MainHero, 1f, false);
				GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, this._targetHero, this.DebtWithInterest, false);
				base.CompleteQuestWithSuccess();
				base.AddLog(this.PlayerPaysTheDebtQuestSuccessLog, false);
				ChangeRelationAction.ApplyPlayerRelation(base.QuestGiver, 5, true, true);
				GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, this.RewardGold, false);
			}

			// Token: 0x06001113 RID: 4371 RVA: 0x0006F6D0 File Offset: 0x0006D8D0
			private void FinishQuestFail1()
			{
				base.AddLog(this.QuestTimeOutFailLog, false);
				ChangeRelationAction.ApplyPlayerRelation(base.QuestGiver, -5, true, true);
			}

			// Token: 0x06001114 RID: 4372 RVA: 0x0006F6EF File Offset: 0x0006D8EF
			private void FinishQuestFail2()
			{
				base.CompleteQuestWithFail(null);
				base.AddLog(this.PlayerHasDefeatedQuestFailLog, false);
				ChangeRelationAction.ApplyPlayerRelation(base.QuestGiver, -5, true, true);
			}

			// Token: 0x06001115 RID: 4373 RVA: 0x0006F715 File Offset: 0x0006D915
			internal static void AutoGeneratedStaticCollectObjectsProdigalSonIssueQuest(object o, List<object> collectedObjects)
			{
				((ProdigalSonIssueBehavior.ProdigalSonIssueQuest)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
			}

			// Token: 0x06001116 RID: 4374 RVA: 0x0006F723 File Offset: 0x0006D923
			protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
			{
				base.AutoGeneratedInstanceCollectObjects(collectedObjects);
				collectedObjects.Add(this._targetHero);
				collectedObjects.Add(this._prodigalSon);
				collectedObjects.Add(this._targetHouse);
			}

			// Token: 0x06001117 RID: 4375 RVA: 0x0006F750 File Offset: 0x0006D950
			internal static object AutoGeneratedGetMemberValue_targetHero(object o)
			{
				return ((ProdigalSonIssueBehavior.ProdigalSonIssueQuest)o)._targetHero;
			}

			// Token: 0x06001118 RID: 4376 RVA: 0x0006F75D File Offset: 0x0006D95D
			internal static object AutoGeneratedGetMemberValue_prodigalSon(object o)
			{
				return ((ProdigalSonIssueBehavior.ProdigalSonIssueQuest)o)._prodigalSon;
			}

			// Token: 0x06001119 RID: 4377 RVA: 0x0006F76A File Offset: 0x0006D96A
			internal static object AutoGeneratedGetMemberValue_playerTalkedToTargetHero(object o)
			{
				return ((ProdigalSonIssueBehavior.ProdigalSonIssueQuest)o)._playerTalkedToTargetHero;
			}

			// Token: 0x0600111A RID: 4378 RVA: 0x0006F77C File Offset: 0x0006D97C
			internal static object AutoGeneratedGetMemberValue_targetHouse(object o)
			{
				return ((ProdigalSonIssueBehavior.ProdigalSonIssueQuest)o)._targetHouse;
			}

			// Token: 0x0600111B RID: 4379 RVA: 0x0006F789 File Offset: 0x0006D989
			internal static object AutoGeneratedGetMemberValue_questDifficulty(object o)
			{
				return ((ProdigalSonIssueBehavior.ProdigalSonIssueQuest)o)._questDifficulty;
			}

			// Token: 0x0600111C RID: 4380 RVA: 0x0006F79B File Offset: 0x0006D99B
			internal static object AutoGeneratedGetMemberValue_isHouseFightFinished(object o)
			{
				return ((ProdigalSonIssueBehavior.ProdigalSonIssueQuest)o)._isHouseFightFinished;
			}

			// Token: 0x0600111D RID: 4381 RVA: 0x0006F7AD File Offset: 0x0006D9AD
			internal static object AutoGeneratedGetMemberValue_playerTriedToPersuade(object o)
			{
				return ((ProdigalSonIssueBehavior.ProdigalSonIssueQuest)o)._playerTriedToPersuade;
			}

			// Token: 0x04000834 RID: 2100
			private const PersuasionDifficulty Difficulty = PersuasionDifficulty.Hard;

			// Token: 0x04000835 RID: 2101
			private const int DistanceSquaredToStartConversation = 4;

			// Token: 0x04000836 RID: 2102
			private const int CrimeRatingCancelRelationshipPenalty = -5;

			// Token: 0x04000837 RID: 2103
			private const int CrimeRatingCancelHonorXpPenalty = -50;

			// Token: 0x04000838 RID: 2104
			[SaveableField(10)]
			private readonly Hero _targetHero;

			// Token: 0x04000839 RID: 2105
			[SaveableField(20)]
			private readonly Hero _prodigalSon;

			// Token: 0x0400083A RID: 2106
			[SaveableField(30)]
			private bool _playerTalkedToTargetHero;

			// Token: 0x0400083B RID: 2107
			[SaveableField(40)]
			private readonly Location _targetHouse;

			// Token: 0x0400083C RID: 2108
			[SaveableField(50)]
			private readonly float _questDifficulty;

			// Token: 0x0400083D RID: 2109
			[SaveableField(60)]
			private bool _isHouseFightFinished;

			// Token: 0x0400083E RID: 2110
			[SaveableField(70)]
			private bool _playerTriedToPersuade;

			// Token: 0x0400083F RID: 2111
			private PersuasionTask _task;

			// Token: 0x04000840 RID: 2112
			private bool _isMissionFightInitialized;

			// Token: 0x04000841 RID: 2113
			private bool _isFirstMissionTick;
		}
	}
}
