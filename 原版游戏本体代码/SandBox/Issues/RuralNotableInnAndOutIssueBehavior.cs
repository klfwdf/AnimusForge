using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using SandBox.BoardGames.MissionLogics;
using SandBox.CampaignBehaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.SaveSystem;

namespace SandBox.Issues
{
	// Token: 0x020000B4 RID: 180
	public class RuralNotableInnAndOutIssueBehavior : CampaignBehaviorBase
	{
		// Token: 0x06000783 RID: 1923 RVA: 0x00033520 File Offset: 0x00031720
		public override void RegisterEvents()
		{
			CampaignEvents.OnCheckForIssueEvent.AddNonSerializedListener(this, new Action<Hero>(this.OnCheckForIssue));
		}

		// Token: 0x06000784 RID: 1924 RVA: 0x00033539 File Offset: 0x00031739
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x06000785 RID: 1925 RVA: 0x0003353C File Offset: 0x0003173C
		private bool ConditionsHold(Hero issueGiver)
		{
			return (issueGiver.IsRuralNotable || issueGiver.IsHeadman) && issueGiver.CurrentSettlement.Village != null && issueGiver.CurrentSettlement.Village.Bound.IsTown && issueGiver.GetTraitLevel(DefaultTraits.Mercy) + issueGiver.GetTraitLevel(DefaultTraits.Honor) < 0 && Campaign.Current.GetCampaignBehavior<BoardGameCampaignBehavior>() != null && issueGiver.CurrentSettlement.Village.Bound.Culture.BoardGame != CultureObject.BoardGameType.None;
		}

		// Token: 0x06000786 RID: 1926 RVA: 0x000335C8 File Offset: 0x000317C8
		public void OnCheckForIssue(Hero hero)
		{
			if (this.ConditionsHold(hero))
			{
				Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(new PotentialIssueData.StartIssueDelegate(this.OnSelected), typeof(RuralNotableInnAndOutIssueBehavior.RuralNotableInnAndOutIssue), IssueBase.IssueFrequency.Common, null));
				return;
			}
			Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(typeof(RuralNotableInnAndOutIssueBehavior.RuralNotableInnAndOutIssue), IssueBase.IssueFrequency.Common));
		}

		// Token: 0x06000787 RID: 1927 RVA: 0x0003362C File Offset: 0x0003182C
		private IssueBase OnSelected(in PotentialIssueData pid, Hero issueOwner)
		{
			return new RuralNotableInnAndOutIssueBehavior.RuralNotableInnAndOutIssue(issueOwner);
		}

		// Token: 0x04000401 RID: 1025
		private const IssueBase.IssueFrequency RuralNotableInnAndOutIssueFrequency = IssueBase.IssueFrequency.Common;

		// Token: 0x04000402 RID: 1026
		private const float IssueDuration = 30f;

		// Token: 0x04000403 RID: 1027
		private const float QuestDuration = 14f;

		// Token: 0x020001CA RID: 458
		public class RuralNotableInnAndOutIssueTypeDefiner : SaveableTypeDefiner
		{
			// Token: 0x060011AE RID: 4526 RVA: 0x00071CF9 File Offset: 0x0006FEF9
			public RuralNotableInnAndOutIssueTypeDefiner()
				: base(585900)
			{
			}

			// Token: 0x060011AF RID: 4527 RVA: 0x00071D06 File Offset: 0x0006FF06
			protected override void DefineClassTypes()
			{
				base.AddClassDefinition(typeof(RuralNotableInnAndOutIssueBehavior.RuralNotableInnAndOutIssue), 1, null);
				base.AddClassDefinition(typeof(RuralNotableInnAndOutIssueBehavior.RuralNotableInnAndOutIssueQuest), 2, null);
			}
		}

		// Token: 0x020001CB RID: 459
		public class RuralNotableInnAndOutIssue : IssueBase
		{
			// Token: 0x170001C3 RID: 451
			// (get) Token: 0x060011B0 RID: 4528 RVA: 0x00071D2C File Offset: 0x0006FF2C
			public override IssueBase.AlternativeSolutionScaleFlag AlternativeSolutionScaleFlags
			{
				get
				{
					return IssueBase.AlternativeSolutionScaleFlag.FailureRisk;
				}
			}

			// Token: 0x170001C4 RID: 452
			// (get) Token: 0x060011B1 RID: 4529 RVA: 0x00071D2F File Offset: 0x0006FF2F
			protected override bool IssueQuestCanBeDuplicated
			{
				get
				{
					return false;
				}
			}

			// Token: 0x170001C5 RID: 453
			// (get) Token: 0x060011B2 RID: 4530 RVA: 0x00071D32 File Offset: 0x0006FF32
			public override int AlternativeSolutionBaseNeededMenCount
			{
				get
				{
					return 1 + MathF.Ceiling(3f * base.IssueDifficultyMultiplier);
				}
			}

			// Token: 0x170001C6 RID: 454
			// (get) Token: 0x060011B3 RID: 4531 RVA: 0x00071D47 File Offset: 0x0006FF47
			protected override int AlternativeSolutionBaseDurationInDaysInternal
			{
				get
				{
					return 1 + MathF.Ceiling(3f * base.IssueDifficultyMultiplier);
				}
			}

			// Token: 0x170001C7 RID: 455
			// (get) Token: 0x060011B4 RID: 4532 RVA: 0x00071D5C File Offset: 0x0006FF5C
			protected override int RewardGold
			{
				get
				{
					return 1000;
				}
			}

			// Token: 0x170001C8 RID: 456
			// (get) Token: 0x060011B5 RID: 4533 RVA: 0x00071D63 File Offset: 0x0006FF63
			public override TextObject Title
			{
				get
				{
					return new TextObject("{=uUhtKnfA}Inn and Out", null);
				}
			}

			// Token: 0x170001C9 RID: 457
			// (get) Token: 0x060011B6 RID: 4534 RVA: 0x00071D70 File Offset: 0x0006FF70
			public override TextObject Description
			{
				get
				{
					TextObject textObject = new TextObject("{=swamqBRq}{ISSUE_OWNER.NAME} wants you to beat the game host", null);
					StringHelpers.SetCharacterProperties("ISSUE_OWNER", base.IssueOwner.CharacterObject, textObject, false);
					return textObject;
				}
			}

			// Token: 0x170001CA RID: 458
			// (get) Token: 0x060011B7 RID: 4535 RVA: 0x00071DA2 File Offset: 0x0006FFA2
			public override TextObject IssueBriefByIssueGiver
			{
				get
				{
					TextObject textObject = new TextObject("{=T0zupcGB}Ah yes... It is a bit embarrassing to mention, [ib:nervous][if:convo_nervous]but... Well, when I am in town, I often have a drink at the inn and perhaps play a round of {GAME_TYPE} or two. Normally I play for low stakes but let's just say that last time the wine went to my head, and I lost something I couldn't afford to lose.", null);
					textObject.SetTextVariable("GAME_TYPE", GameTexts.FindText("str_boardgame_name", this._boardGameType.ToString()));
					return textObject;
				}
			}

			// Token: 0x170001CB RID: 459
			// (get) Token: 0x060011B8 RID: 4536 RVA: 0x00071DD6 File Offset: 0x0006FFD6
			public override TextObject IssueAcceptByPlayer
			{
				get
				{
					return new TextObject("{=h2tMadtI}I've heard that story before. What did you lose?", null);
				}
			}

			// Token: 0x170001CC RID: 460
			// (get) Token: 0x060011B9 RID: 4537 RVA: 0x00071DE4 File Offset: 0x0006FFE4
			public override TextObject IssueQuestSolutionExplanationByIssueGiver
			{
				get
				{
					TextObject textObject = new TextObject("{=LD4tGYCA}It's a deed to a plot of farmland. Not a big or valuable plot,[ib:normal][if:convo_disbelief] mind you, but I'd rather not have to explain to my men why they won't be sowing it this year. You can find the man who took it from me at the tavern in {TARGET_SETTLEMENT}. They call him the \"Game Host\". Just be straight about what you're doing. He's in no position to work the land. I don't imagine that he'll turn down a chance to make more money off of it. Bring it back and {REWARD}{GOLD_ICON} is yours.", null);
					textObject.SetTextVariable("REWARD", this.RewardGold);
					textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
					textObject.SetTextVariable("TARGET_SETTLEMENT", this._targetSettlement.Name);
					return textObject;
				}
			}

			// Token: 0x170001CD RID: 461
			// (get) Token: 0x060011BA RID: 4538 RVA: 0x00071E36 File Offset: 0x00070036
			public override TextObject IssueAlternativeSolutionExplanationByIssueGiver
			{
				get
				{
					TextObject textObject = new TextObject("{=urCXu9Fc}Well, I could try and buy it from him, but I would not really prefer that.[if:convo_innocent_smile] I would be the joke of the tavern for months to come... If you choose to do that, I can only offer {REWARD}{GOLD_ICON} to compensate for your payment. If you have a man with a knack for such games he might do the trick.", null);
					textObject.SetTextVariable("REWARD", this.RewardGold);
					textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
					return textObject;
				}
			}

			// Token: 0x170001CE RID: 462
			// (get) Token: 0x060011BB RID: 4539 RVA: 0x00071E66 File Offset: 0x00070066
			public override TextObject IssueQuestSolutionAcceptByPlayer
			{
				get
				{
					return new TextObject("{=KMThnMbt}I'll go to the tavern and win it back the same way you lost it.", null);
				}
			}

			// Token: 0x170001CF RID: 463
			// (get) Token: 0x060011BC RID: 4540 RVA: 0x00071E74 File Offset: 0x00070074
			public override TextObject IssueAlternativeSolutionAcceptByPlayer
			{
				get
				{
					TextObject textObject = new TextObject("{=QdKWaabR}Worry not {ISSUE_OWNER.NAME}, my men will be back with your deed in no time.", null);
					StringHelpers.SetCharacterProperties("ISSUE_OWNER", base.IssueOwner.CharacterObject, textObject, false);
					return textObject;
				}
			}

			// Token: 0x170001D0 RID: 464
			// (get) Token: 0x060011BD RID: 4541 RVA: 0x00071EA6 File Offset: 0x000700A6
			public override TextObject IssueDiscussAlternativeSolution
			{
				get
				{
					return new TextObject("{=1yEyUHJe}I really hope your men can get my deed back. [if:convo_excited]On my father's name, I will never gamble again.", null);
				}
			}

			// Token: 0x170001D1 RID: 465
			// (get) Token: 0x060011BE RID: 4542 RVA: 0x00071EB4 File Offset: 0x000700B4
			public override TextObject IssueAlternativeSolutionResponseByIssueGiver
			{
				get
				{
					TextObject textObject = new TextObject("{=kiaN39yb}Thank you, {PLAYER.NAME}. I'm sure your companion will be persuasive.[if:convo_relaxed_happy]", null);
					StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject, false);
					return textObject;
				}
			}

			// Token: 0x170001D2 RID: 466
			// (get) Token: 0x060011BF RID: 4543 RVA: 0x00071EE0 File Offset: 0x000700E0
			public override bool IsThereAlternativeSolution
			{
				get
				{
					return true;
				}
			}

			// Token: 0x170001D3 RID: 467
			// (get) Token: 0x060011C0 RID: 4544 RVA: 0x00071EE3 File Offset: 0x000700E3
			public override bool IsThereLordSolution
			{
				get
				{
					return false;
				}
			}

			// Token: 0x170001D4 RID: 468
			// (get) Token: 0x060011C1 RID: 4545 RVA: 0x00071EE8 File Offset: 0x000700E8
			protected override TextObject AlternativeSolutionStartLog
			{
				get
				{
					TextObject textObject = new TextObject("{=MIxzaqzi}{QUEST_GIVER.LINK} told you that he lost a land deed in a wager in {TARGET_CITY}. He needs to buy it back, and he wants your companions to intimidate the seller into offering a reasonable price. You asked {COMPANION.LINK} to take {TROOP_COUNT} of your men to go and take care of it. They should report back to you in {RETURN_DAYS} days.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.IssueOwner.CharacterObject, textObject, false);
					StringHelpers.SetCharacterProperties("COMPANION", base.AlternativeSolutionHero.CharacterObject, textObject, false);
					textObject.SetTextVariable("TARGET_CITY", this._targetSettlement.EncyclopediaLinkWithName);
					textObject.SetTextVariable("RETURN_DAYS", base.GetTotalAlternativeSolutionDurationInDays());
					textObject.SetTextVariable("TROOP_COUNT", this.AlternativeSolutionSentTroops.TotalManCount - 1);
					return textObject;
				}
			}

			// Token: 0x060011C2 RID: 4546 RVA: 0x00071F74 File Offset: 0x00070174
			public RuralNotableInnAndOutIssue(Hero issueOwner)
				: base(issueOwner, CampaignTime.DaysFromNow(30f))
			{
				this.InitializeQuestVariables();
			}

			// Token: 0x060011C3 RID: 4547 RVA: 0x00071F8D File Offset: 0x0007018D
			protected override float GetIssueEffectAmountInternal(IssueEffect issueEffect)
			{
				if (issueEffect == DefaultIssueEffects.SettlementProsperity)
				{
					return -0.1f;
				}
				if (issueEffect == DefaultIssueEffects.IssueOwnerPower)
				{
					return -0.1f;
				}
				return 0f;
			}

			// Token: 0x060011C4 RID: 4548 RVA: 0x00071FB0 File Offset: 0x000701B0
			public override ValueTuple<SkillObject, int> GetAlternativeSolutionSkill(Hero hero)
			{
				return new ValueTuple<SkillObject, int>((hero.GetSkillValue(DefaultSkills.Charm) >= hero.GetSkillValue(DefaultSkills.Tactics)) ? DefaultSkills.Charm : DefaultSkills.Tactics, 120);
			}

			// Token: 0x060011C5 RID: 4549 RVA: 0x00071FDD File Offset: 0x000701DD
			public override bool AlternativeSolutionCondition(out TextObject explanation)
			{
				return QuestHelper.CheckRosterForAlternativeSolution(MobileParty.MainParty.MemberRoster, base.GetTotalAlternativeSolutionNeededMenCount(), out explanation, 0, false) && QuestHelper.CheckGoldForAlternativeSolution(1000, out explanation);
			}

			// Token: 0x170001D5 RID: 469
			// (get) Token: 0x060011C6 RID: 4550 RVA: 0x00072006 File Offset: 0x00070206
			protected override int CompanionSkillRewardXP
			{
				get
				{
					return (int)(500f + 1000f * base.IssueDifficultyMultiplier);
				}
			}

			// Token: 0x060011C7 RID: 4551 RVA: 0x0007201C File Offset: 0x0007021C
			protected override void AlternativeSolutionEndWithSuccessConsequence()
			{
				this.RelationshipChangeWithIssueOwner = 5;
				GainRenownAction.Apply(Hero.MainHero, 5f, false);
				base.IssueOwner.CurrentSettlement.Village.Bound.Town.Loyalty += 5f;
			}

			// Token: 0x060011C8 RID: 4552 RVA: 0x0007206B File Offset: 0x0007026B
			protected override void AlternativeSolutionEndWithFailureConsequence()
			{
				this.RelationshipChangeWithIssueOwner -= 5;
				base.IssueOwner.CurrentSettlement.Village.Bound.Town.Loyalty -= 5f;
			}

			// Token: 0x060011C9 RID: 4553 RVA: 0x000720A6 File Offset: 0x000702A6
			public override bool DoTroopsSatisfyAlternativeSolution(TroopRoster troopRoster, out TextObject explanation)
			{
				return QuestHelper.CheckRosterForAlternativeSolution(troopRoster, base.GetTotalAlternativeSolutionNeededMenCount(), out explanation, 0, false);
			}

			// Token: 0x060011CA RID: 4554 RVA: 0x000720B7 File Offset: 0x000702B7
			public override IssueBase.IssueFrequency GetFrequency()
			{
				return IssueBase.IssueFrequency.Common;
			}

			// Token: 0x060011CB RID: 4555 RVA: 0x000720BC File Offset: 0x000702BC
			public override bool IssueStayAliveConditions()
			{
				BoardGameCampaignBehavior campaignBehavior = Campaign.Current.GetCampaignBehavior<BoardGameCampaignBehavior>();
				return campaignBehavior != null && !campaignBehavior.WonBoardGamesInOneWeekInSettlement.Contains(this._targetSettlement) && !base.IssueOwner.CurrentSettlement.IsRaided && !base.IssueOwner.CurrentSettlement.IsUnderRaid;
			}

			// Token: 0x060011CC RID: 4556 RVA: 0x00072111 File Offset: 0x00070311
			protected override void CompleteIssueWithTimedOutConsequences()
			{
			}

			// Token: 0x060011CD RID: 4557 RVA: 0x00072113 File Offset: 0x00070313
			private void InitializeQuestVariables()
			{
				this._targetSettlement = base.IssueOwner.CurrentSettlement.Village.Bound;
				this._boardGameType = this._targetSettlement.Culture.BoardGame;
			}

			// Token: 0x060011CE RID: 4558 RVA: 0x00072146 File Offset: 0x00070346
			protected override void OnGameLoad()
			{
				this.InitializeQuestVariables();
			}

			// Token: 0x060011CF RID: 4559 RVA: 0x0007214E File Offset: 0x0007034E
			protected override void HourlyTick()
			{
			}

			// Token: 0x060011D0 RID: 4560 RVA: 0x00072150 File Offset: 0x00070350
			protected override QuestBase GenerateIssueQuest(string questId)
			{
				return new RuralNotableInnAndOutIssueBehavior.RuralNotableInnAndOutIssueQuest(questId, base.IssueOwner, CampaignTime.DaysFromNow(14f), this.RewardGold);
			}

			// Token: 0x060011D1 RID: 4561 RVA: 0x00072170 File Offset: 0x00070370
			protected override bool CanPlayerTakeQuestConditions(Hero issueGiver, out IssueBase.PreconditionFlags flag, out Hero relationHero, out SkillObject skill)
			{
				skill = null;
				relationHero = null;
				flag = IssueBase.PreconditionFlags.None;
				if (issueGiver.GetRelationWithPlayer() < -10f)
				{
					flag |= IssueBase.PreconditionFlags.Relation;
					relationHero = issueGiver;
				}
				if (FactionManager.IsAtWarAgainstFaction(issueGiver.CurrentSettlement.MapFaction, Hero.MainHero.MapFaction))
				{
					flag |= IssueBase.PreconditionFlags.AtWar;
				}
				if (Hero.MainHero.Gold < 2000)
				{
					flag |= IssueBase.PreconditionFlags.Money;
				}
				return flag == IssueBase.PreconditionFlags.None;
			}

			// Token: 0x060011D2 RID: 4562 RVA: 0x000721DC File Offset: 0x000703DC
			internal static void AutoGeneratedStaticCollectObjectsRuralNotableInnAndOutIssue(object o, List<object> collectedObjects)
			{
				((RuralNotableInnAndOutIssueBehavior.RuralNotableInnAndOutIssue)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
			}

			// Token: 0x060011D3 RID: 4563 RVA: 0x000721EA File Offset: 0x000703EA
			protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
			{
				base.AutoGeneratedInstanceCollectObjects(collectedObjects);
			}

			// Token: 0x04000887 RID: 2183
			private const int CompanionSkillLimit = 120;

			// Token: 0x04000888 RID: 2184
			private const int QuestMoneyLimit = 2000;

			// Token: 0x04000889 RID: 2185
			private const int AlternativeSolutionGoldCost = 1000;

			// Token: 0x0400088A RID: 2186
			private CultureObject.BoardGameType _boardGameType;

			// Token: 0x0400088B RID: 2187
			private Settlement _targetSettlement;
		}

		// Token: 0x020001CC RID: 460
		public class RuralNotableInnAndOutIssueQuest : QuestBase
		{
			// Token: 0x170001D6 RID: 470
			// (get) Token: 0x060011D4 RID: 4564 RVA: 0x000721F4 File Offset: 0x000703F4
			private TextObject QuestStartLog
			{
				get
				{
					TextObject textObject = new TextObject("{=tirG1BB2}{QUEST_GIVER.LINK} told you that he lost a land deed while playing games in a tavern in {TARGET_SETTLEMENT}. He wants you to go find the game host and win it back for him. You told him that you will take care of the situation yourself.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					textObject.SetTextVariable("TARGET_SETTLEMENT", this._targetSettlement.EncyclopediaLinkWithName);
					return textObject;
				}
			}

			// Token: 0x170001D7 RID: 471
			// (get) Token: 0x060011D5 RID: 4565 RVA: 0x00072240 File Offset: 0x00070440
			private TextObject SuccessLog
			{
				get
				{
					TextObject textObject = new TextObject("{=bvhWLb4C}You defeated the Game Host and got the deed back. {QUEST_GIVER.LINK}.{newline}\"Thank you for resolving this issue so neatly. Please accept these {GOLD}{GOLD_ICON} denars with our gratitude.\"", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					textObject.SetTextVariable("GOLD", this.RewardGold);
					textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
					return textObject;
				}
			}

			// Token: 0x170001D8 RID: 472
			// (get) Token: 0x060011D6 RID: 4566 RVA: 0x00072298 File Offset: 0x00070498
			private TextObject SuccessWithPayingLog
			{
				get
				{
					TextObject textObject = new TextObject("{=TIPxWsYW}You have bought the deed from the game host. {QUEST_GIVER.LINK}.{newline}\"I am happy that I got my land back. I'm not so happy that everyone knows I had to pay for it, but... Anyway, please accept these {GOLD}{GOLD_ICON} denars with my gratitude.\"", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					textObject.SetTextVariable("GOLD", 800);
					textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
					return textObject;
				}
			}

			// Token: 0x170001D9 RID: 473
			// (get) Token: 0x060011D7 RID: 4567 RVA: 0x000722EC File Offset: 0x000704EC
			private TextObject LostLog
			{
				get
				{
					TextObject textObject = new TextObject("{=ye4oqBFB}You lost the board game and failed to help {QUEST_GIVER.LINK}. \"Thank you for trying, {PLAYER.NAME}, but I guess I chose the wrong person for the job.\"", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject, false);
					return textObject;
				}
			}

			// Token: 0x170001DA RID: 474
			// (get) Token: 0x060011D8 RID: 4568 RVA: 0x00072330 File Offset: 0x00070530
			private TextObject QuestCanceledTargetVillageRaided
			{
				get
				{
					TextObject textObject = new TextObject("{=DLesz9jI}{QUEST_GIVER.LINK}’s village is raided. {?QUEST_GIVER.GENDER}She{?}He{\\?} flees to the countryside, and your agreement with {?QUEST_GIVER.GENDER}her{?}him{\\?} is canceled.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					textObject.SetTextVariable("SETTLEMENT", this._targetSettlement.EncyclopediaLinkWithName);
					return textObject;
				}
			}

			// Token: 0x170001DB RID: 475
			// (get) Token: 0x060011D9 RID: 4569 RVA: 0x00072379 File Offset: 0x00070579
			private TextObject QuestCanceledWarDeclared
			{
				get
				{
					TextObject textObject = new TextObject("{=cKz1cyuM}Your clan is now at war with {QUEST_GIVER_SETTLEMENT_FACTION}. Quest is canceled.", null);
					textObject.SetTextVariable("QUEST_GIVER_SETTLEMENT_FACTION", base.QuestGiver.CurrentSettlement.MapFaction.Name);
					return textObject;
				}
			}

			// Token: 0x170001DC RID: 476
			// (get) Token: 0x060011DA RID: 4570 RVA: 0x000723A8 File Offset: 0x000705A8
			private TextObject PlayerDeclaredWarQuestLogText
			{
				get
				{
					TextObject textObject = new TextObject("{=bqeWVVEE}Your actions have started a war with {QUEST_GIVER.LINK}'s faction. {?QUEST_GIVER.GENDER}She{?}He{\\?} cancels your agreement and the quest is a failure.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					return textObject;
				}
			}

			// Token: 0x170001DD RID: 477
			// (get) Token: 0x060011DB RID: 4571 RVA: 0x000723DC File Offset: 0x000705DC
			private TextObject QuestCanceledSettlementIsUnderSiege
			{
				get
				{
					TextObject textObject = new TextObject("{=b5LdBYpF}{SETTLEMENT} is under siege. Your agreement with {QUEST_GIVER.LINK} is canceled.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					textObject.SetTextVariable("SETTLEMENT", this._targetSettlement.EncyclopediaLinkWithName);
					return textObject;
				}
			}

			// Token: 0x170001DE RID: 478
			// (get) Token: 0x060011DC RID: 4572 RVA: 0x00072428 File Offset: 0x00070628
			private TextObject TimeoutLog
			{
				get
				{
					TextObject textObject = new TextObject("{=XLy8anVr}You received a message from {QUEST_GIVER.LINK}. \"This may not have seemed like an important task, but I placed my trust in you. I guess I was wrong to do so.\"", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					return textObject;
				}
			}

			// Token: 0x170001DF RID: 479
			// (get) Token: 0x060011DD RID: 4573 RVA: 0x0007245A File Offset: 0x0007065A
			public override TextObject Title
			{
				get
				{
					return new TextObject("{=uUhtKnfA}Inn and Out", null);
				}
			}

			// Token: 0x170001E0 RID: 480
			// (get) Token: 0x060011DE RID: 4574 RVA: 0x00072467 File Offset: 0x00070667
			public override bool IsRemainingTimeHidden
			{
				get
				{
					return false;
				}
			}

			// Token: 0x060011DF RID: 4575 RVA: 0x0007246A File Offset: 0x0007066A
			public RuralNotableInnAndOutIssueQuest(string questId, Hero giverHero, CampaignTime duration, int rewardGold)
				: base(questId, giverHero, duration, rewardGold)
			{
				this.InitializeQuestVariables();
				this.SetDialogs();
				base.InitializeQuestOnCreation();
			}

			// Token: 0x060011E0 RID: 4576 RVA: 0x00072489 File Offset: 0x00070689
			private void InitializeQuestVariables()
			{
				this._targetSettlement = base.QuestGiver.CurrentSettlement.Village.Bound;
				this._boardGameType = this._targetSettlement.Culture.BoardGame;
			}

			// Token: 0x060011E1 RID: 4577 RVA: 0x000724BC File Offset: 0x000706BC
			private void QuestAcceptedConsequences()
			{
				base.StartQuest();
				base.AddLog(this.QuestStartLog, false);
				base.AddTrackedObject(this._targetSettlement);
			}

			// Token: 0x060011E2 RID: 4578 RVA: 0x000724DE File Offset: 0x000706DE
			protected override void InitializeQuestOnGameLoad()
			{
				this.InitializeQuestVariables();
				this.SetDialogs();
				if (Campaign.Current.GetCampaignBehavior<BoardGameCampaignBehavior>() == null)
				{
					base.CompleteQuestWithCancel(null);
				}
			}

			// Token: 0x060011E3 RID: 4579 RVA: 0x000724FF File Offset: 0x000706FF
			protected override void HourlyTick()
			{
			}

			// Token: 0x060011E4 RID: 4580 RVA: 0x00072504 File Offset: 0x00070704
			protected override void RegisterEvents()
			{
				CampaignEvents.OnPlayerBoardGameOverEvent.AddNonSerializedListener(this, new Action<Hero, BoardGameHelper.BoardGameState>(this.OnBoardGameEnd));
				CampaignEvents.WarDeclared.AddNonSerializedListener(this, new Action<IFaction, IFaction, DeclareWarAction.DeclareWarDetail>(this.OnWarDeclared));
				CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, new Action<Clan, Kingdom, Kingdom, ChangeKingdomAction.ChangeKingdomActionDetail, bool>(this.OnClanChangedKingdom));
				CampaignEvents.OnSiegeEventStartedEvent.AddNonSerializedListener(this, new Action<SiegeEvent>(this.OnSiegeStarted));
				CampaignEvents.MapEventStarted.AddNonSerializedListener(this, new Action<MapEvent, PartyBase, PartyBase>(this.OnMapEventStarted));
				CampaignEvents.VillageBeingRaided.AddNonSerializedListener(this, new Action<Village>(this.OnVillageBeingRaided));
				CampaignEvents.LocationCharactersSimulatedEvent.AddNonSerializedListener(this, new Action(this.OnLocationCharactersSimulated));
			}

			// Token: 0x060011E5 RID: 4581 RVA: 0x000725B4 File Offset: 0x000707B4
			private void OnLocationCharactersSimulated()
			{
				if (Settlement.CurrentSettlement != null && Settlement.CurrentSettlement == this._targetSettlement && Campaign.Current.GameMenuManager.MenuLocations.Count > 0 && Campaign.Current.GameMenuManager.MenuLocations[0].StringId == "tavern")
				{
					foreach (Agent agent in Mission.Current.Agents)
					{
						LocationCharacter locationCharacter = LocationComplex.Current.GetLocationWithId("tavern").GetLocationCharacter(agent.Origin);
						if (locationCharacter != null && locationCharacter.Character.Occupation == Occupation.TavernGameHost)
						{
							locationCharacter.IsVisualTracked = true;
						}
					}
				}
			}

			// Token: 0x060011E6 RID: 4582 RVA: 0x00072694 File Offset: 0x00070894
			private void OnMapEventStarted(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
			{
				if (QuestHelper.CheckMinorMajorCoercion(this, mapEvent, attackerParty))
				{
					QuestHelper.ApplyGenericMinorMajorCoercionConsequences(this, mapEvent);
				}
			}

			// Token: 0x060011E7 RID: 4583 RVA: 0x000726A7 File Offset: 0x000708A7
			private void OnVillageBeingRaided(Village village)
			{
				if (village == base.QuestGiver.CurrentSettlement.Village)
				{
					base.CompleteQuestWithCancel(this.QuestCanceledTargetVillageRaided);
				}
			}

			// Token: 0x060011E8 RID: 4584 RVA: 0x000726C8 File Offset: 0x000708C8
			private void OnBoardGameEnd(Hero opposingHero, BoardGameHelper.BoardGameState state)
			{
				if (this._checkForBoardGameEnd)
				{
					this._playerWonTheGame = state == BoardGameHelper.BoardGameState.Win;
				}
			}

			// Token: 0x060011E9 RID: 4585 RVA: 0x000726DC File Offset: 0x000708DC
			private void OnSiegeStarted(SiegeEvent siegeEvent)
			{
				if (siegeEvent.BesiegedSettlement == this._targetSettlement)
				{
					base.CompleteQuestWithCancel(this.QuestCanceledSettlementIsUnderSiege);
				}
			}

			// Token: 0x060011EA RID: 4586 RVA: 0x000726F8 File Offset: 0x000708F8
			protected override void SetDialogs()
			{
				TextObject textObject = new TextObject("{=I6amLvVE}Good, good. That's the best way to do these things. [if:convo_normal]Go to {TARGET_SETTLEMENT}, find this game host and wipe the smirk off of his face.", null);
				textObject.SetTextVariable("TARGET_SETTLEMENT", this._targetSettlement.Name);
				this.OfferDialogFlow = DialogFlow.CreateDialogFlow("issue_classic_quest_start", 100).NpcLine(textObject, null, null, null, null).Condition(() => Hero.OneToOneConversationHero == base.QuestGiver)
					.Consequence(new ConversationSentence.OnConsequenceDelegate(this.QuestAcceptedConsequences))
					.CloseDialog();
				this.DiscussDialogFlow = DialogFlow.CreateDialogFlow("quest_discuss", 100).NpcLine(new TextObject("{=HGRWs0zE}Have you met the man who took my deed? Did you get it back?[if:convo_astonished]", null), null, null, null, null).Condition(() => Hero.OneToOneConversationHero == base.QuestGiver)
					.BeginPlayerOptions(null, false)
					.PlayerOption(new TextObject("{=uJPAYUU7}I will be on my way soon enough.", null), null, null, null)
					.NpcLine(new TextObject("{=MOmePlJQ}Could you hurry this along? I don't want him to find another buyer.[if:convo_pondering] Thank you.", null), null, null, null, null)
					.CloseDialog()
					.PlayerOption(new TextObject("{=azVhRGik}I am waiting for the right moment.", null), null, null, null)
					.NpcLine(new TextObject("{=bRMLn0jj}Well, if he wanders off to another town, or gets his throat slit,[if:convo_pondering] or loses the deed, that would be the wrong moment, now wouldn't it?", null), null, null, null, null)
					.CloseDialog()
					.EndPlayerOptions();
				Campaign.Current.ConversationManager.AddDialogFlow(this.GetGameHostDialogFlow(), this);
				Campaign.Current.ConversationManager.AddDialogFlow(this.GetGameHostDialogueAfterFirstGame(), this);
			}

			// Token: 0x060011EB RID: 4587 RVA: 0x00072834 File Offset: 0x00070A34
			private DialogFlow GetGameHostDialogFlow()
			{
				return DialogFlow.CreateDialogFlow("start", 125).NpcLine("{=dzWioKRa}Hello there, are you looking for a friendly match? A wager perhaps?[if:convo_mocking_aristocratic]", null, null, null, null).Condition(() => this.TavernHostDialogCondition(true))
					.PlayerLine(new TextObject("{=eOle8pYT}You won a deed of land from my associate. I'm here to win it back.", null), null, null, null)
					.NpcLine("{=bEipgE5E}Ah, yes, these are the most interesting kinds of games, aren't they? [if:convo_excited]I won't deny myself the pleasure but clearly that deed is worth more to him than just the value of the land. I'll wager the deed, but you need to put up 1000 denars.", null, null, null, null)
					.BeginPlayerOptions(null, false)
					.PlayerOption("{=XvkSbY6N}I see your wager. Let's play.", null, null, null)
					.Condition(() => Hero.MainHero.Gold >= 1000)
					.Consequence(new ConversationSentence.OnConsequenceDelegate(this.StartBoardGame))
					.CloseDialog()
					.PlayerOption("{=89b5ao7P}As of now, I do not have 1000 denars to afford on gambling. I may get back to you once I get the required amount.", null, null, null)
					.Condition(() => Hero.MainHero.Gold < 1000)
					.NpcLine(new TextObject("{=ppi6eVos}As you wish.", null), null, null, null, null)
					.CloseDialog()
					.PlayerOption("{=WrnvRayQ}Let's just save ourselves some trouble, and I'll just pay you that amount.", null, null, null)
					.ClickableCondition(new ConversationSentence.OnClickableConditionDelegate(this.CheckPlayerHasEnoughDenarsClickableCondition))
					.NpcLine("{=pa3RY39w}Sure. I'm happy to turn paper into silver... 1000 denars it is.[if:convo_evil_smile]", null, null, null, null)
					.Consequence(new ConversationSentence.OnConsequenceDelegate(this.PlayerPaid1000QuestSuccess))
					.CloseDialog()
					.PlayerOption("{=BSeplVwe}That's too much. I will be back later.", null, null, null)
					.CloseDialog()
					.EndPlayerOptions()
					.CloseDialog();
			}

			// Token: 0x060011EC RID: 4588 RVA: 0x00072984 File Offset: 0x00070B84
			private DialogFlow GetGameHostDialogueAfterFirstGame()
			{
				return DialogFlow.CreateDialogFlow("start", 125).BeginNpcOptions(null, false).NpcOption(new TextObject("{=dyhZUHao}Well, I thought you were here to be sheared, [if:convo_shocked]but it looks like the sheep bites back. Very well, nicely played, here's your man's land back.", null), () => this._playerWonTheGame && this.TavernHostDialogCondition(false), null, null, null, null)
					.Consequence(new ConversationSentence.OnConsequenceDelegate(this.PlayerWonTheBoardGame))
					.CloseDialog()
					.NpcOption("{=TdnD29Ax}Ah! You almost had me! Maybe you just weren't paying attention. [if:convo_mocking_teasing]Care to put another 1000 denars on the table and have another go?", () => !this._playerWonTheGame && this._tryCount < 2 && this.TavernHostDialogCondition(false), null, null, null, null)
					.BeginPlayerOptions(null, false)
					.PlayerOption("{=fiMZ696A}Yes, I'll play again.", null, null, null)
					.ClickableCondition(new ConversationSentence.OnClickableConditionDelegate(this.CheckPlayerHasEnoughDenarsClickableCondition))
					.Consequence(new ConversationSentence.OnConsequenceDelegate(this.StartBoardGame))
					.CloseDialog()
					.PlayerOption("{=zlFSIvD5}No, no. I know a trap when I see one. You win. Good-bye.", null, null, null)
					.NpcLine(new TextObject("{=ppi6eVos}As you wish.", null), null, null, null, null)
					.Consequence(new ConversationSentence.OnConsequenceDelegate(this.PlayerFailAfterBoardGame))
					.CloseDialog()
					.EndPlayerOptions()
					.NpcOption("{=hkNrC5d3}That was fun, but I've learned not to inflict too great a humiliation on those who carry a sword.[if:convo_merry] I'll take my winnings and enjoy them now. Good-bye to you!", () => this._tryCount >= 2 && this.TavernHostDialogCondition(false), null, null, null, null)
					.Consequence(new ConversationSentence.OnConsequenceDelegate(this.PlayerFailAfterBoardGame))
					.CloseDialog()
					.EndNpcOptions();
			}

			// Token: 0x060011ED RID: 4589 RVA: 0x00072AA0 File Offset: 0x00070CA0
			private bool CheckPlayerHasEnoughDenarsClickableCondition(out TextObject explanation)
			{
				if (Hero.MainHero.Gold >= 1000)
				{
					explanation = null;
					return true;
				}
				explanation = new TextObject("{=AMlaYbJv}You don't have 1000 denars.", null);
				return false;
			}

			// Token: 0x060011EE RID: 4590 RVA: 0x00072AC8 File Offset: 0x00070CC8
			private bool TavernHostDialogCondition(bool isInitialDialogue = false)
			{
				if ((!this._checkForBoardGameEnd || !isInitialDialogue) && Settlement.CurrentSettlement == this._targetSettlement && CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.TavernGameHost)
				{
					LocationComplex locationComplex = LocationComplex.Current;
					if (((locationComplex != null) ? locationComplex.GetLocationWithId("tavern") : null) != null)
					{
						Mission.Current.GetMissionBehavior<MissionBoardGameLogic>().DetectOpposingAgent();
						return Mission.Current.GetMissionBehavior<MissionBoardGameLogic>().CheckIfBothSidesAreSitting();
					}
				}
				return false;
			}

			// Token: 0x060011EF RID: 4591 RVA: 0x00072B33 File Offset: 0x00070D33
			private void PlayerPaid1000QuestSuccess()
			{
				base.AddLog(this.SuccessWithPayingLog, false);
				this._applyLesserReward = true;
				GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, 1000, false);
				base.CompleteQuestWithSuccess();
			}

			// Token: 0x060011F0 RID: 4592 RVA: 0x00072B64 File Offset: 0x00070D64
			protected override void OnFinalize()
			{
				if (Mission.Current != null)
				{
					foreach (Agent agent in Mission.Current.Agents)
					{
						Location locationWithId = LocationComplex.Current.GetLocationWithId("tavern");
						if (locationWithId != null)
						{
							LocationCharacter locationCharacter = locationWithId.GetLocationCharacter(agent.Origin);
							if (locationCharacter != null && locationCharacter.Character.Occupation == Occupation.TavernGameHost)
							{
								locationCharacter.IsVisualTracked = false;
							}
						}
					}
				}
			}

			// Token: 0x060011F1 RID: 4593 RVA: 0x00072BF4 File Offset: 0x00070DF4
			private void ApplySuccessRewards()
			{
				GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, this._applyLesserReward ? 800 : this.RewardGold, false);
				ChangeRelationAction.ApplyPlayerRelation(base.QuestGiver, 5, true, true);
				GainRenownAction.Apply(Hero.MainHero, 1f, false);
				base.QuestGiver.CurrentSettlement.Village.Bound.Town.Loyalty += 5f;
			}

			// Token: 0x060011F2 RID: 4594 RVA: 0x00072C6B File Offset: 0x00070E6B
			protected override void OnCompleteWithSuccess()
			{
				this.ApplySuccessRewards();
			}

			// Token: 0x060011F3 RID: 4595 RVA: 0x00072C74 File Offset: 0x00070E74
			private void StartBoardGame()
			{
				MissionBoardGameLogic missionBehavior = Mission.Current.GetMissionBehavior<MissionBoardGameLogic>();
				Campaign.Current.GetCampaignBehavior<BoardGameCampaignBehavior>().SetBetAmount(1000);
				missionBehavior.DetectOpposingAgent();
				missionBehavior.SetCurrentDifficulty(BoardGameHelper.AIDifficulty.Normal);
				missionBehavior.SetBoardGame(this._boardGameType);
				missionBehavior.StartBoardGame();
				this._checkForBoardGameEnd = true;
				this._tryCount++;
			}

			// Token: 0x060011F4 RID: 4596 RVA: 0x00072CD2 File Offset: 0x00070ED2
			private void PlayerWonTheBoardGame()
			{
				base.AddLog(this.SuccessLog, false);
				base.CompleteQuestWithSuccess();
			}

			// Token: 0x060011F5 RID: 4597 RVA: 0x00072CE8 File Offset: 0x00070EE8
			private void PlayerFailAfterBoardGame()
			{
				base.AddLog(this.LostLog, false);
				this.RelationshipChangeWithQuestGiver = -5;
				base.QuestGiver.CurrentSettlement.Village.Bound.Town.Loyalty -= 5f;
				base.CompleteQuestWithFail(null);
			}

			// Token: 0x060011F6 RID: 4598 RVA: 0x00072D3D File Offset: 0x00070F3D
			private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
			{
				if (base.QuestGiver.CurrentSettlement.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction))
				{
					base.CompleteQuestWithCancel(this.QuestCanceledWarDeclared);
				}
			}

			// Token: 0x060011F7 RID: 4599 RVA: 0x00072D6C File Offset: 0x00070F6C
			private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
			{
				QuestHelper.CheckWarDeclarationAndFailOrCancelTheQuest(this, faction1, faction2, detail, this.PlayerDeclaredWarQuestLogText, this.QuestCanceledWarDeclared, false);
			}

			// Token: 0x060011F8 RID: 4600 RVA: 0x00072D84 File Offset: 0x00070F84
			public override GameMenuOption.IssueQuestFlags IsLocationTrackedByQuest(Location location)
			{
				if (PlayerEncounter.LocationEncounter.Settlement == this._targetSettlement && location.StringId == "tavern")
				{
					return GameMenuOption.IssueQuestFlags.ActiveIssue;
				}
				return GameMenuOption.IssueQuestFlags.None;
			}

			// Token: 0x060011F9 RID: 4601 RVA: 0x00072DB0 File Offset: 0x00070FB0
			protected override void OnTimedOut()
			{
				this.RelationshipChangeWithQuestGiver = -5;
				base.QuestGiver.CurrentSettlement.Village.Bound.Town.Loyalty -= 5f;
				base.AddLog(this.TimeoutLog, false);
			}

			// Token: 0x060011FA RID: 4602 RVA: 0x00072DFE File Offset: 0x00070FFE
			internal static void AutoGeneratedStaticCollectObjectsRuralNotableInnAndOutIssueQuest(object o, List<object> collectedObjects)
			{
				((RuralNotableInnAndOutIssueBehavior.RuralNotableInnAndOutIssueQuest)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
			}

			// Token: 0x060011FB RID: 4603 RVA: 0x00072E0C File Offset: 0x0007100C
			protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
			{
				base.AutoGeneratedInstanceCollectObjects(collectedObjects);
			}

			// Token: 0x060011FC RID: 4604 RVA: 0x00072E15 File Offset: 0x00071015
			internal static object AutoGeneratedGetMemberValue_tryCount(object o)
			{
				return ((RuralNotableInnAndOutIssueBehavior.RuralNotableInnAndOutIssueQuest)o)._tryCount;
			}

			// Token: 0x0400088C RID: 2188
			public const int LesserReward = 800;

			// Token: 0x0400088D RID: 2189
			private CultureObject.BoardGameType _boardGameType;

			// Token: 0x0400088E RID: 2190
			private Settlement _targetSettlement;

			// Token: 0x0400088F RID: 2191
			private bool _checkForBoardGameEnd;

			// Token: 0x04000890 RID: 2192
			private bool _playerWonTheGame;

			// Token: 0x04000891 RID: 2193
			private bool _applyLesserReward;

			// Token: 0x04000892 RID: 2194
			[SaveableField(1)]
			private int _tryCount;
		}
	}
}
