using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace TaleWorlds.CampaignSystem.Issues
{
	// Token: 0x0200035A RID: 858
	public class BettingFraudIssueBehavior : CampaignBehaviorBase
	{
		// Token: 0x17000C34 RID: 3124
		// (get) Token: 0x060032A7 RID: 12967 RVA: 0x000CFD30 File Offset: 0x000CDF30
		private static BettingFraudIssueBehavior.BettingFraudQuest Instance
		{
			get
			{
				BettingFraudIssueBehavior campaignBehavior = Campaign.Current.GetCampaignBehavior<BettingFraudIssueBehavior>();
				if (campaignBehavior._cachedQuest != null && campaignBehavior._cachedQuest.IsOngoing)
				{
					return campaignBehavior._cachedQuest;
				}
				using (List<QuestBase>.Enumerator enumerator = Campaign.Current.QuestManager.Quests.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						BettingFraudIssueBehavior.BettingFraudQuest cachedQuest;
						if ((cachedQuest = enumerator.Current as BettingFraudIssueBehavior.BettingFraudQuest) != null)
						{
							campaignBehavior._cachedQuest = cachedQuest;
							return campaignBehavior._cachedQuest;
						}
					}
				}
				return null;
			}
		}

		// Token: 0x060032A8 RID: 12968 RVA: 0x000CFDC8 File Offset: 0x000CDFC8
		public override void RegisterEvents()
		{
			CampaignEvents.OnCheckForIssueEvent.AddNonSerializedListener(this, new Action<Hero>(this.CheckForIssue));
			CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnSessionLaunched));
		}

		// Token: 0x060032A9 RID: 12969 RVA: 0x000CFDF8 File Offset: 0x000CDFF8
		private void OnSessionLaunched(CampaignGameStarter gameStarter)
		{
			gameStarter.AddGameMenu("menu_town_tournament_join_betting_fraud", "{=5Adr6toM}{MENU_TEXT}", new OnInitDelegate(this.game_menu_tournament_join_on_init), GameMenu.MenuOverlayType.SettlementWithBoth, GameMenu.MenuFlags.None, null);
			gameStarter.AddGameMenuOption("menu_town_tournament_join_betting_fraud", "mno_tournament_event_1", "{=es0Y3Bxc}Join", delegate(MenuCallbackArgs args)
			{
				args.optionLeaveType = GameMenuOption.LeaveType.Mission;
				args.OptionQuestData = GameMenuOption.IssueQuestFlags.ActiveIssue;
				return true;
			}, new GameMenuOption.OnConsequenceDelegate(this.game_menu_tournament_join_current_game_on_consequence), false, -1, false, null);
			gameStarter.AddGameMenuOption("menu_town_tournament_join_betting_fraud", "mno_tournament_leave", "{=3sRdGQou}Leave", delegate(MenuCallbackArgs args)
			{
				args.optionLeaveType = GameMenuOption.LeaveType.Leave;
				return true;
			}, delegate(MenuCallbackArgs args)
			{
				GameMenu.SwitchToMenu("town_arena");
			}, true, -1, false, null);
		}

		// Token: 0x060032AA RID: 12970 RVA: 0x000CFEC0 File Offset: 0x000CE0C0
		private void game_menu_tournament_join_on_init(MenuCallbackArgs args)
		{
			TournamentGame tournamentGame = Campaign.Current.TournamentManager.GetTournamentGame(Settlement.CurrentSettlement.Town);
			tournamentGame.UpdateTournamentPrize(true, false);
			GameTexts.SetVariable("MENU_TEXT", tournamentGame.GetMenuText());
		}

		// Token: 0x060032AB RID: 12971 RVA: 0x000CFF00 File Offset: 0x000CE100
		private void game_menu_tournament_join_current_game_on_consequence(MenuCallbackArgs args)
		{
			CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter, null, false, false, false, false, false, false), new ConversationCharacterData(BettingFraudIssueBehavior.Instance._thug, null, false, false, false, false, false, false));
		}

		// Token: 0x060032AC RID: 12972 RVA: 0x000CFF3C File Offset: 0x000CE13C
		[GameMenuInitializationHandler("menu_town_tournament_join_betting_fraud")]
		private static void game_menu_ui_town_ui_on_init(MenuCallbackArgs args)
		{
			Settlement currentSettlement = Settlement.CurrentSettlement;
			args.MenuContext.SetBackgroundMeshName(currentSettlement.Town.WaitMeshName);
		}

		// Token: 0x060032AD RID: 12973 RVA: 0x000CFF65 File Offset: 0x000CE165
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x060032AE RID: 12974 RVA: 0x000CFF68 File Offset: 0x000CE168
		private void CheckForIssue(Hero hero)
		{
			if (this.ConditionsHold(hero))
			{
				Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(new PotentialIssueData.StartIssueDelegate(this.OnStartIssue), typeof(BettingFraudIssueBehavior.BettingFraudIssue), IssueBase.IssueFrequency.Rare, null));
				return;
			}
			Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(typeof(BettingFraudIssueBehavior.BettingFraudIssue), IssueBase.IssueFrequency.Rare));
		}

		// Token: 0x060032AF RID: 12975 RVA: 0x000CFFCC File Offset: 0x000CE1CC
		private bool ConditionsHold(Hero issueGiver)
		{
			return issueGiver.IsGangLeader && issueGiver.CurrentSettlement != null && issueGiver.CurrentSettlement.Town != null && issueGiver.CurrentSettlement.Town.Security < 50f;
		}

		// Token: 0x060032B0 RID: 12976 RVA: 0x000D0004 File Offset: 0x000CE204
		private IssueBase OnStartIssue(in PotentialIssueData pid, Hero issueOwner)
		{
			return new BettingFraudIssueBehavior.BettingFraudIssue(issueOwner);
		}

		// Token: 0x04000E7B RID: 3707
		private const IssueBase.IssueFrequency BettingFraudIssueFrequency = IssueBase.IssueFrequency.Rare;

		// Token: 0x04000E7C RID: 3708
		private const string JoinTournamentMenuId = "menu_town_tournament_join";

		// Token: 0x04000E7D RID: 3709
		private const string JoinTournamentForBettingFraudQuestMenuId = "menu_town_tournament_join_betting_fraud";

		// Token: 0x04000E7E RID: 3710
		private const int SettlementSecurityLimit = 50;

		// Token: 0x04000E7F RID: 3711
		private BettingFraudIssueBehavior.BettingFraudQuest _cachedQuest;

		// Token: 0x020006E7 RID: 1767
		public class BettingFraudIssue : IssueBase
		{
			// Token: 0x0600544C RID: 21580 RVA: 0x0018C788 File Offset: 0x0018A988
			internal static void AutoGeneratedStaticCollectObjectsBettingFraudIssue(object o, List<object> collectedObjects)
			{
				((BettingFraudIssueBehavior.BettingFraudIssue)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
			}

			// Token: 0x0600544D RID: 21581 RVA: 0x0018C796 File Offset: 0x0018A996
			protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
			{
				base.AutoGeneratedInstanceCollectObjects(collectedObjects);
			}

			// Token: 0x17000FD9 RID: 4057
			// (get) Token: 0x0600544E RID: 21582 RVA: 0x0018C79F File Offset: 0x0018A99F
			public override TextObject IssueBriefByIssueGiver
			{
				get
				{
					return new TextObject("{=kru5Vpog}Yes. I'm glad to have the chance to talk to you. I keep an eye on the careers of champions like yourself for professional reasons, and I have a proposal that might interest a good fighter like you. Interested?[ib:confident3][if:convo_bemused]", null);
				}
			}

			// Token: 0x17000FDA RID: 4058
			// (get) Token: 0x0600544F RID: 21583 RVA: 0x0018C7AC File Offset: 0x0018A9AC
			public override TextObject IssueAcceptByPlayer
			{
				get
				{
					return new TextObject("{=YWXkgDSd}What kind of a partnership are we talking about?", null);
				}
			}

			// Token: 0x17000FDB RID: 4059
			// (get) Token: 0x06005450 RID: 21584 RVA: 0x0018C7B9 File Offset: 0x0018A9B9
			public override TextObject IssueQuestSolutionExplanationByIssueGiver
			{
				get
				{
					return new TextObject("{=vLaoZhkF}I follow tournaments, you see, and like to both place and take bets. But of course I need someone who can not only win those tournaments but lose if necessary... if you understand what I mean. Not all the time. That would be too obvious. Here's what I propose. We enter into a partnership for five tournaments. Don't bother memorizing which ones you win and which ones you lose. Before each fight, an associate of my mine will let you know how you should place. Follow my instructions and I promise you will be rewarded handsomely. What do you say?[if:convo_bemused][ib:demure2]", null);
				}
			}

			// Token: 0x17000FDC RID: 4060
			// (get) Token: 0x06005451 RID: 21585 RVA: 0x0018C7C6 File Offset: 0x0018A9C6
			public override TextObject IssueQuestSolutionAcceptByPlayer
			{
				get
				{
					return new TextObject("{=cL9BX7ph}As long as the payment is good, I agree.", null);
				}
			}

			// Token: 0x17000FDD RID: 4061
			// (get) Token: 0x06005452 RID: 21586 RVA: 0x0018C7D3 File Offset: 0x0018A9D3
			public override bool IsThereAlternativeSolution
			{
				get
				{
					return false;
				}
			}

			// Token: 0x17000FDE RID: 4062
			// (get) Token: 0x06005453 RID: 21587 RVA: 0x0018C7D6 File Offset: 0x0018A9D6
			public override bool IsThereLordSolution
			{
				get
				{
					return false;
				}
			}

			// Token: 0x17000FDF RID: 4063
			// (get) Token: 0x06005454 RID: 21588 RVA: 0x0018C7D9 File Offset: 0x0018A9D9
			public override TextObject Title
			{
				get
				{
					return new TextObject("{=xhVrxgC4}Betting Fraud", null);
				}
			}

			// Token: 0x17000FE0 RID: 4064
			// (get) Token: 0x06005455 RID: 21589 RVA: 0x0018C7E6 File Offset: 0x0018A9E6
			public override TextObject Description
			{
				get
				{
					TextObject textObject = new TextObject("{=3j8pV58L}{ISSUE_GIVER.NAME} offers you a deal to fix {TOURNAMENT_COUNT} tournaments and share the profit from the bet winnings.", null);
					textObject.SetCharacterProperties("ISSUE_GIVER", base.IssueOwner.CharacterObject, false);
					textObject.SetTextVariable("TOURNAMENT_COUNT", 5);
					return textObject;
				}
			}

			// Token: 0x06005456 RID: 21590 RVA: 0x0018C817 File Offset: 0x0018AA17
			public BettingFraudIssue(Hero issueOwner)
				: base(issueOwner, CampaignTime.DaysFromNow(45f))
			{
			}

			// Token: 0x06005457 RID: 21591 RVA: 0x0018C82A File Offset: 0x0018AA2A
			protected override float GetIssueEffectAmountInternal(IssueEffect issueEffect)
			{
				if (issueEffect == DefaultIssueEffects.IssueOwnerPower)
				{
					return -0.2f;
				}
				return 0f;
			}

			// Token: 0x06005458 RID: 21592 RVA: 0x0018C83F File Offset: 0x0018AA3F
			protected override void OnGameLoad()
			{
			}

			// Token: 0x06005459 RID: 21593 RVA: 0x0018C841 File Offset: 0x0018AA41
			protected override void HourlyTick()
			{
			}

			// Token: 0x0600545A RID: 21594 RVA: 0x0018C843 File Offset: 0x0018AA43
			protected override QuestBase GenerateIssueQuest(string questId)
			{
				return new BettingFraudIssueBehavior.BettingFraudQuest(questId, base.IssueOwner, CampaignTime.DaysFromNow(45f), 0);
			}

			// Token: 0x0600545B RID: 21595 RVA: 0x0018C85C File Offset: 0x0018AA5C
			public override IssueBase.IssueFrequency GetFrequency()
			{
				return IssueBase.IssueFrequency.Rare;
			}

			// Token: 0x0600545C RID: 21596 RVA: 0x0018C860 File Offset: 0x0018AA60
			protected override bool CanPlayerTakeQuestConditions(Hero issueOwner, out IssueBase.PreconditionFlags flag, out Hero relationHero, out SkillObject skill)
			{
				relationHero = null;
				skill = null;
				flag = IssueBase.PreconditionFlags.None;
				if (Clan.PlayerClan.Renown < 50f)
				{
					flag |= IssueBase.PreconditionFlags.Renown;
				}
				if (issueOwner.GetRelationWithPlayer() < -10f)
				{
					flag |= IssueBase.PreconditionFlags.Relation;
					relationHero = issueOwner;
				}
				if (Hero.MainHero.GetSkillValue(DefaultSkills.OneHanded) < 50 && Hero.MainHero.GetSkillValue(DefaultSkills.TwoHanded) < 50 && Hero.MainHero.GetSkillValue(DefaultSkills.Polearm) < 50 && Hero.MainHero.GetSkillValue(DefaultSkills.Bow) < 50 && Hero.MainHero.GetSkillValue(DefaultSkills.Crossbow) < 50 && Hero.MainHero.GetSkillValue(DefaultSkills.Throwing) < 50)
				{
					if (Hero.MainHero.GetSkillValue(DefaultSkills.OneHanded) < 50)
					{
						flag |= IssueBase.PreconditionFlags.Skill;
						skill = DefaultSkills.OneHanded;
					}
					else if (Hero.MainHero.GetSkillValue(DefaultSkills.TwoHanded) < 50)
					{
						flag |= IssueBase.PreconditionFlags.Skill;
						skill = DefaultSkills.TwoHanded;
					}
					else if (Hero.MainHero.GetSkillValue(DefaultSkills.Polearm) < 50)
					{
						flag |= IssueBase.PreconditionFlags.Skill;
						skill = DefaultSkills.Polearm;
					}
					else if (Hero.MainHero.GetSkillValue(DefaultSkills.Bow) < 50)
					{
						flag |= IssueBase.PreconditionFlags.Skill;
						skill = DefaultSkills.Bow;
					}
					else if (Hero.MainHero.GetSkillValue(DefaultSkills.Crossbow) < 50)
					{
						flag |= IssueBase.PreconditionFlags.Skill;
						skill = DefaultSkills.Crossbow;
					}
					else if (Hero.MainHero.GetSkillValue(DefaultSkills.Throwing) < 50)
					{
						flag |= IssueBase.PreconditionFlags.Skill;
						skill = DefaultSkills.Throwing;
					}
				}
				return flag == IssueBase.PreconditionFlags.None;
			}

			// Token: 0x0600545D RID: 21597 RVA: 0x0018C9FC File Offset: 0x0018ABFC
			public override bool IssueStayAliveConditions()
			{
				return true;
			}

			// Token: 0x0600545E RID: 21598 RVA: 0x0018C9FF File Offset: 0x0018ABFF
			protected override void CompleteIssueWithTimedOutConsequences()
			{
			}

			// Token: 0x04001B99 RID: 7065
			private const int NeededTournamentCount = 5;

			// Token: 0x04001B9A RID: 7066
			private const int IssueDuration = 45;

			// Token: 0x04001B9B RID: 7067
			private const int MainHeroSkillLimit = 50;

			// Token: 0x04001B9C RID: 7068
			private const int MainClanRenownLimit = 50;

			// Token: 0x04001B9D RID: 7069
			private const int RelationLimitWithIssueOwner = -10;

			// Token: 0x04001B9E RID: 7070
			private const float IssueOwnerPowerPenaltyForIssueEffect = -0.2f;
		}

		// Token: 0x020006E8 RID: 1768
		public class BettingFraudQuest : QuestBase
		{
			// Token: 0x0600545F RID: 21599 RVA: 0x0018CA01 File Offset: 0x0018AC01
			internal static void AutoGeneratedStaticCollectObjectsBettingFraudQuest(object o, List<object> collectedObjects)
			{
				((BettingFraudIssueBehavior.BettingFraudQuest)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
			}

			// Token: 0x06005460 RID: 21600 RVA: 0x0018CA0F File Offset: 0x0018AC0F
			protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
			{
				base.AutoGeneratedInstanceCollectObjects(collectedObjects);
				collectedObjects.Add(this._thug);
				collectedObjects.Add(this._startLog);
				collectedObjects.Add(this._counterOfferNotable);
			}

			// Token: 0x06005461 RID: 21601 RVA: 0x0018CA3C File Offset: 0x0018AC3C
			internal static object AutoGeneratedGetMemberValue_thug(object o)
			{
				return ((BettingFraudIssueBehavior.BettingFraudQuest)o)._thug;
			}

			// Token: 0x06005462 RID: 21602 RVA: 0x0018CA49 File Offset: 0x0018AC49
			internal static object AutoGeneratedGetMemberValue_startLog(object o)
			{
				return ((BettingFraudIssueBehavior.BettingFraudQuest)o)._startLog;
			}

			// Token: 0x06005463 RID: 21603 RVA: 0x0018CA56 File Offset: 0x0018AC56
			internal static object AutoGeneratedGetMemberValue_counterOfferNotable(object o)
			{
				return ((BettingFraudIssueBehavior.BettingFraudQuest)o)._counterOfferNotable;
			}

			// Token: 0x06005464 RID: 21604 RVA: 0x0018CA63 File Offset: 0x0018AC63
			internal static object AutoGeneratedGetMemberValue_fixedTournamentCount(object o)
			{
				return ((BettingFraudIssueBehavior.BettingFraudQuest)o)._fixedTournamentCount;
			}

			// Token: 0x06005465 RID: 21605 RVA: 0x0018CA75 File Offset: 0x0018AC75
			internal static object AutoGeneratedGetMemberValue_minorOffensiveCount(object o)
			{
				return ((BettingFraudIssueBehavior.BettingFraudQuest)o)._minorOffensiveCount;
			}

			// Token: 0x06005466 RID: 21606 RVA: 0x0018CA87 File Offset: 0x0018AC87
			internal static object AutoGeneratedGetMemberValue_counterOfferConversationDone(object o)
			{
				return ((BettingFraudIssueBehavior.BettingFraudQuest)o)._counterOfferConversationDone;
			}

			// Token: 0x17000FE1 RID: 4065
			// (get) Token: 0x06005467 RID: 21607 RVA: 0x0018CA99 File Offset: 0x0018AC99
			public override TextObject Title
			{
				get
				{
					return new TextObject("{=xhVrxgC4}Betting Fraud", null);
				}
			}

			// Token: 0x17000FE2 RID: 4066
			// (get) Token: 0x06005468 RID: 21608 RVA: 0x0018CAA6 File Offset: 0x0018ACA6
			public override bool IsRemainingTimeHidden
			{
				get
				{
					return false;
				}
			}

			// Token: 0x17000FE3 RID: 4067
			// (get) Token: 0x06005469 RID: 21609 RVA: 0x0018CAA9 File Offset: 0x0018ACA9
			private TextObject StartLog
			{
				get
				{
					TextObject textObject = new TextObject("{=6rweIvZS}{QUEST_GIVER.LINK}, a gang leader from {SETTLEMENT} offers you to fix 5 tournaments together and share the profit.{newline}{?QUEST_GIVER.GENDER}She{?}He{\\?} asked you to enter 5 tournaments and follow the instructions given by {?QUEST_GIVER.GENDER}her{?}his{\\?} associate.", null);
					textObject.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, false);
					textObject.SetTextVariable("SETTLEMENT", base.QuestGiver.CurrentSettlement.EncyclopediaLinkWithName);
					return textObject;
				}
			}

			// Token: 0x17000FE4 RID: 4068
			// (get) Token: 0x0600546A RID: 21610 RVA: 0x0018CAE9 File Offset: 0x0018ACE9
			private TextObject CurrentDirectiveLog
			{
				get
				{
					TextObject textObject = new TextObject("{=dnZekyZI}Directive from {QUEST_GIVER.LINK}: {DIRECTIVE}", null);
					textObject.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, false);
					textObject.SetTextVariable("DIRECTIVE", this.GetDirectiveText());
					return textObject;
				}
			}

			// Token: 0x17000FE5 RID: 4069
			// (get) Token: 0x0600546B RID: 21611 RVA: 0x0018CB20 File Offset: 0x0018AD20
			private TextObject QuestFailedWithTimeOutLog
			{
				get
				{
					TextObject textObject = new TextObject("{=2brAaeFh}You failed to complete tournaments in time. {QUEST_GIVER.LINK} will certainly be disappointed.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					return textObject;
				}
			}

			// Token: 0x0600546C RID: 21612 RVA: 0x0018CB54 File Offset: 0x0018AD54
			public BettingFraudQuest(string questId, Hero questGiver, CampaignTime duration, int rewardGold)
				: base(questId, questGiver, duration, rewardGold)
			{
				this._counterOfferNotable = null;
				this._fixedTournamentCount = 0;
				this._minorOffensiveCount = 0;
				this._counterOfferAccepted = false;
				this._readyToStartTournament = false;
				this._startTournamentEndConversation = false;
				this._counterOfferConversationDone = false;
				this._currentDirective = BettingFraudIssueBehavior.BettingFraudQuest.Directives.None;
				this._afterTournamentConversationState = BettingFraudIssueBehavior.BettingFraudQuest.AfterTournamentConversationState.None;
				this._thug = MBObjectManager.Instance.GetObject<CharacterObject>((MBRandom.RandomFloat > 0.5f) ? "betting_fraud_thug_male" : "betting_fraud_thug_female");
				this._startLog = null;
				this.SetDialogs();
				base.InitializeQuestOnCreation();
			}

			// Token: 0x0600546D RID: 21613 RVA: 0x0018CBE6 File Offset: 0x0018ADE6
			protected override void InitializeQuestOnGameLoad()
			{
				this.SetDialogs();
			}

			// Token: 0x0600546E RID: 21614 RVA: 0x0018CBEE File Offset: 0x0018ADEE
			protected override void HourlyTick()
			{
			}

			// Token: 0x0600546F RID: 21615 RVA: 0x0018CBF0 File Offset: 0x0018ADF0
			private void SelectCounterOfferNotable(Settlement settlement)
			{
				this._counterOfferNotable = settlement.Notables.GetRandomElement<Hero>();
			}

			// Token: 0x06005470 RID: 21616 RVA: 0x0018CC03 File Offset: 0x0018AE03
			private void IncreaseMinorOffensive()
			{
				this._minorOffensiveCount++;
				this._currentDirective = BettingFraudIssueBehavior.BettingFraudQuest.Directives.None;
				if (this._minorOffensiveCount >= 2)
				{
					this._afterTournamentConversationState = BettingFraudIssueBehavior.BettingFraudQuest.AfterTournamentConversationState.SecondMinorOffense;
					return;
				}
				this._afterTournamentConversationState = BettingFraudIssueBehavior.BettingFraudQuest.AfterTournamentConversationState.MinorOffense;
			}

			// Token: 0x06005471 RID: 21617 RVA: 0x0018CC32 File Offset: 0x0018AE32
			private void IncreaseFixedTournamentCount()
			{
				this._fixedTournamentCount++;
				this._startLog.UpdateCurrentProgress(this._fixedTournamentCount);
				this._currentDirective = BettingFraudIssueBehavior.BettingFraudQuest.Directives.None;
				if (this._fixedTournamentCount >= 5)
				{
					this._afterTournamentConversationState = BettingFraudIssueBehavior.BettingFraudQuest.AfterTournamentConversationState.BigReward;
					return;
				}
				this._afterTournamentConversationState = BettingFraudIssueBehavior.BettingFraudQuest.AfterTournamentConversationState.SmallReward;
			}

			// Token: 0x06005472 RID: 21618 RVA: 0x0018CC72 File Offset: 0x0018AE72
			private void SetCurrentDirective()
			{
				this._currentDirective = ((MBRandom.RandomFloat <= 0.33f) ? BettingFraudIssueBehavior.BettingFraudQuest.Directives.LoseAt3RdRound : ((MBRandom.RandomFloat < 0.5f) ? BettingFraudIssueBehavior.BettingFraudQuest.Directives.LoseAt4ThRound : BettingFraudIssueBehavior.BettingFraudQuest.Directives.WinTheTournament));
				base.AddLog(this.CurrentDirectiveLog, false);
			}

			// Token: 0x06005473 RID: 21619 RVA: 0x0018CCA8 File Offset: 0x0018AEA8
			private void StartTournamentMission()
			{
				TournamentGame tournamentGame = Campaign.Current.TournamentManager.GetTournamentGame(Settlement.CurrentSettlement.Town);
				GameMenu.SwitchToMenu("town");
				tournamentGame.PrepareForTournamentGame(true);
				Campaign.Current.TournamentManager.OnPlayerJoinTournament(tournamentGame.GetType(), Settlement.CurrentSettlement);
			}

			// Token: 0x06005474 RID: 21620 RVA: 0x0018CCFC File Offset: 0x0018AEFC
			protected override void RegisterEvents()
			{
				CampaignEvents.PlayerEliminatedFromTournament.AddNonSerializedListener(this, new Action<int, Town>(this.OnPlayerEliminatedFromTournament));
				CampaignEvents.TournamentFinished.AddNonSerializedListener(this, new Action<CharacterObject, MBReadOnlyList<CharacterObject>, Town, ItemObject>(this.OnTournamentFinished));
				CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, new Action<MenuCallbackArgs>(this.OnGameMenuOpened));
			}

			// Token: 0x06005475 RID: 21621 RVA: 0x0018CD4E File Offset: 0x0018AF4E
			private void OnPlayerEliminatedFromTournament(int round, Town town)
			{
				this._startTournamentEndConversation = true;
				if (round == (int)this._currentDirective)
				{
					this.IncreaseFixedTournamentCount();
					return;
				}
				if (round < (int)this._currentDirective)
				{
					this.IncreaseMinorOffensive();
					return;
				}
				if (round > (int)this._currentDirective)
				{
					this._afterTournamentConversationState = BettingFraudIssueBehavior.BettingFraudQuest.AfterTournamentConversationState.MajorOffense;
				}
			}

			// Token: 0x06005476 RID: 21622 RVA: 0x0018CD88 File Offset: 0x0018AF88
			private void OnTournamentFinished(CharacterObject winner, MBReadOnlyList<CharacterObject> participants, Town town, ItemObject prize)
			{
				if (participants.Contains(CharacterObject.PlayerCharacter) && this._currentDirective != BettingFraudIssueBehavior.BettingFraudQuest.Directives.None)
				{
					this._startTournamentEndConversation = true;
					if (this._currentDirective == BettingFraudIssueBehavior.BettingFraudQuest.Directives.WinTheTournament)
					{
						if (winner == CharacterObject.PlayerCharacter)
						{
							this.IncreaseFixedTournamentCount();
							return;
						}
						this.IncreaseMinorOffensive();
						return;
					}
					else if (winner == CharacterObject.PlayerCharacter)
					{
						this._afterTournamentConversationState = BettingFraudIssueBehavior.BettingFraudQuest.AfterTournamentConversationState.MajorOffense;
					}
				}
			}

			// Token: 0x06005477 RID: 21623 RVA: 0x0018CDE0 File Offset: 0x0018AFE0
			private void OnGameMenuOpened(MenuCallbackArgs args)
			{
				if (args.MenuContext.GameMenu.StringId == "menu_town_tournament_join")
				{
					GameMenu.SwitchToMenu("menu_town_tournament_join_betting_fraud");
				}
				if (args.MenuContext.GameMenu.StringId == "menu_town_tournament_join_betting_fraud")
				{
					if (this._readyToStartTournament)
					{
						if (this._fixedTournamentCount == 4 && !this._counterOfferConversationDone && this._counterOfferNotable != null && this._currentDirective != BettingFraudIssueBehavior.BettingFraudQuest.Directives.WinTheTournament)
						{
							CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter, null, false, false, false, false, false, false), new ConversationCharacterData(this._counterOfferNotable.CharacterObject, null, false, false, false, false, false, false));
						}
						else
						{
							this.StartTournamentMission();
							this._readyToStartTournament = false;
						}
					}
					if (this._fixedTournamentCount == 4 && (this._counterOfferNotable == null || this._counterOfferNotable.CurrentSettlement != Settlement.CurrentSettlement))
					{
						this.SelectCounterOfferNotable(Settlement.CurrentSettlement);
					}
				}
				if (this._startTournamentEndConversation)
				{
					CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter, null, false, false, false, false, false, false), new ConversationCharacterData(this._thug, null, false, false, false, false, false, false));
				}
			}

			// Token: 0x06005478 RID: 21624 RVA: 0x0018CEF6 File Offset: 0x0018B0F6
			protected override void OnTimedOut()
			{
				base.OnTimedOut();
				this.PlayerDidNotCompleteTournaments();
			}

			// Token: 0x06005479 RID: 21625 RVA: 0x0018CF04 File Offset: 0x0018B104
			protected override void SetDialogs()
			{
				this.OfferDialogFlow = this.GetOfferDialogFlow();
				this.DiscussDialogFlow = this.GetDiscussDialogFlow();
				Campaign.Current.ConversationManager.AddDialogFlow(this.GetDialogWithThugStart(), this);
				Campaign.Current.ConversationManager.AddDialogFlow(this.GetDialogWithThugEnd(), this);
				Campaign.Current.ConversationManager.AddDialogFlow(this.GetCounterOfferDialog(), this);
			}

			// Token: 0x0600547A RID: 21626 RVA: 0x0018CF6C File Offset: 0x0018B16C
			private DialogFlow GetOfferDialogFlow()
			{
				return DialogFlow.CreateDialogFlow("issue_classic_quest_start", 100).NpcLine(new TextObject("{=sp52g5AQ}Very good, very good. Try to enter five tournaments over the next 45 days or so. Right before the fight you'll hear from my associate how far I want you to go in the rankings before you lose.[if:convo_delighted][ib:hip]", null), null, null, null, null).Condition(() => Hero.OneToOneConversationHero == base.QuestGiver)
					.NpcLine(new TextObject("{=ADIYnC4u}Now, I know you can't win every fight, so if you underperform once or twice, I'd understand. But if you lose every time, or worse, if you overperform, well, then I'll be a bit angry.[if:convo_nonchalant][ib:normal2]", null), null, null, null, null)
					.NpcLine(new TextObject("{=1hOPCf8I}But I'm sure you won't disappoint me. Enjoy your riches![if:convo_focused_happy][ib:confident]", null), null, null, null, null)
					.Consequence(new ConversationSentence.OnConsequenceDelegate(this.OfferDialogFlowConsequence))
					.CloseDialog();
			}

			// Token: 0x0600547B RID: 21627 RVA: 0x0018CFE8 File Offset: 0x0018B1E8
			private void OfferDialogFlowConsequence()
			{
				base.StartQuest();
				this._startLog = base.AddDiscreteLog(this.StartLog, new TextObject("{=dLfWFa61}Fix 5 Tournaments", null), 0, 5, null, false);
			}

			// Token: 0x0600547C RID: 21628 RVA: 0x0018D014 File Offset: 0x0018B214
			private DialogFlow GetDiscussDialogFlow()
			{
				return DialogFlow.CreateDialogFlow("quest_discuss", 100).NpcLine(new TextObject("{=!}{RESPONSE_TEXT}", null), null, null, null, null).Condition(new ConversationSentence.OnConditionDelegate(this.DiscussDialogCondition))
					.BeginPlayerOptions(null, false)
					.PlayerOption(new TextObject("{=abLgPWzf}I will continue to honor our deal. Do not forget to do your end, that's all.", null), null, null, null)
					.BeginNpcOptions(null, false)
					.NpcOption(new TextObject("{=ZLPEsMUx}Well, there are tournament happening in {NEARBY_TOURNAMENTS_LIST} right now. You can go there and do the job. Your denars will be waiting for you.", null), new ConversationSentence.OnConditionDelegate(this.NpcTournamentLocationCondition), null, null, null, null)
					.CloseDialog()
					.NpcDefaultOption("{=sUfSCLQx}Sadly, I've heard no news of an upcoming tournament. I am sure one will be held before too long.")
					.CloseDialog()
					.EndNpcOptions()
					.CloseDialog()
					.PlayerOption(new TextObject("{=XUS5wNsD}I feel like I do all the job and you get your denars.", null), null, null, null)
					.BeginNpcOptions(null, false)
					.NpcOption(new TextObject("{=ZLPEsMUx}Well, there are tournament happening in {NEARBY_TOURNAMENTS_LIST} right now. You can go there and do the job. Your denars will be waiting for you.", null), new ConversationSentence.OnConditionDelegate(this.NpcTournamentLocationCondition), null, null, null, null)
					.CloseDialog()
					.NpcDefaultOption("{=sUfSCLQx}Sadly, I've heard no news of an upcoming tournament. I am sure one will be held before too long.")
					.CloseDialog()
					.EndNpcOptions()
					.CloseDialog()
					.EndPlayerOptions()
					.CloseDialog();
			}

			// Token: 0x0600547D RID: 21629 RVA: 0x0018D114 File Offset: 0x0018B314
			private bool DiscussDialogCondition()
			{
				bool flag = Hero.OneToOneConversationHero == base.QuestGiver;
				if (flag)
				{
					if (this._minorOffensiveCount > 0)
					{
						MBTextManager.SetTextVariable("RESPONSE_TEXT", new TextObject("{=7SPwGYvf}I had expected better of you. But even the best can fail sometimes. Just make sure it does not happen again.[if:convo_bored][ib:closed2] ", null), false);
						return flag;
					}
					MBTextManager.SetTextVariable("RESPONSE_TEXT", new TextObject("{=vo0uhUsZ}I have high hopes for you, friend. Just follow my directives and we will be rich.[if:convo_relaxed_happy][ib:demure2]", null), false);
				}
				return flag;
			}

			// Token: 0x0600547E RID: 21630 RVA: 0x0018D168 File Offset: 0x0018B368
			private bool NpcTournamentLocationCondition()
			{
				List<Town> list = (from x in Town.AllTowns
					where Campaign.Current.TournamentManager.GetTournamentGame(x) != null && x != Settlement.CurrentSettlement.Town
					select x).ToList<Town>();
				list = (from x in list
					orderby DistanceHelper.FindClosestDistanceFromSettlementToSettlement(x.Settlement, Settlement.CurrentSettlement, MobileParty.NavigationType.Default)
					select x).ToList<Town>();
				if (list.Count > 0)
				{
					MBTextManager.SetTextVariable("NEARBY_TOURNAMENTS_LIST", list[0].Name, false);
					return true;
				}
				return false;
			}

			// Token: 0x0600547F RID: 21631 RVA: 0x0018D1F4 File Offset: 0x0018B3F4
			private DialogFlow GetDialogWithThugStart()
			{
				return DialogFlow.CreateDialogFlow("start", 125).NpcLine(new TextObject("{=!}{GREETING_LINE}", null), null, null, null, null).Condition(new ConversationSentence.OnConditionDelegate(this.DialogWithThugStartCondition))
					.BeginPlayerOptions(null, false)
					.PlayerOption(new TextObject("{=!}{POSITIVE_OPTION}", null), null, null, null)
					.Condition(new ConversationSentence.OnConditionDelegate(this.PositiveOptionCondition))
					.Consequence(new ConversationSentence.OnConsequenceDelegate(this.PositiveOptionConsequences))
					.CloseDialog()
					.PlayerOption(new TextObject("{=!}{NEGATIVE_OPTION}", null), null, null, null)
					.Condition(new ConversationSentence.OnConditionDelegate(this.NegativeOptionCondition))
					.Consequence(new ConversationSentence.OnConsequenceDelegate(this.NegativeOptionConsequence))
					.CloseDialog()
					.EndPlayerOptions()
					.CloseDialog();
			}

			// Token: 0x06005480 RID: 21632 RVA: 0x0018D2B8 File Offset: 0x0018B4B8
			private bool DialogWithThugStartCondition()
			{
				bool flag = CharacterObject.OneToOneConversationCharacter == this._thug && !this._startTournamentEndConversation;
				if (flag)
				{
					this.SetCurrentDirective();
					if (this._fixedTournamentCount < 2)
					{
						TextObject textObject = new TextObject("{=xYu4yVRU}Hey there friend. So... You don't need to know my name, but suffice to say that we're both friends of {QUEST_GIVER.LINK}. Here's {?QUEST_GIVER.GENDER}her{?}his{\\?} message for you: {DIRECTIVE}.[ib:confident][if:convo_nonchalant]", null);
						textObject.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, false);
						textObject.SetTextVariable("DIRECTIVE", this.GetDirectiveText());
						MBTextManager.SetTextVariable("GREETING_LINE", textObject, false);
						return flag;
					}
					if (this._fixedTournamentCount < 4)
					{
						TextObject textObject2 = new TextObject("{=cQE9tQOy}My friend! Good to see you. You did very well in that last fight. People definitely won't be expecting you to \"{DIRECTIVE}\". What a surprise that would be. Well, I should not keep you from your tournament. You know what to do.[if:convo_happy][ib:closed2]", null);
						textObject2.SetTextVariable("DIRECTIVE", this.GetDirectiveText());
						MBTextManager.SetTextVariable("GREETING_LINE", textObject2, false);
						return flag;
					}
					TextObject textObject3 = new TextObject("{=RVLPQ4rm}My friend. I am almost sad that these meetings are going to come to an end. Well, a deal is a deal. I won't beat around the bush. Here's your final message: {DIRECTIVE}. I wish you luck, right up until the moment that you have to go down.[if:convo_mocking_teasing][ib:closed]", null);
					textObject3.SetTextVariable("DIRECTIVE", this.GetDirectiveText());
					MBTextManager.SetTextVariable("GREETING_LINE", textObject3, false);
				}
				return flag;
			}

			// Token: 0x06005481 RID: 21633 RVA: 0x0018D394 File Offset: 0x0018B594
			private bool PositiveOptionCondition()
			{
				if (this._fixedTournamentCount < 2)
				{
					MBTextManager.SetTextVariable("POSITIVE_OPTION", new TextObject("{=PrUauabl}As long as the payment is as we talked, you got nothing to worry about.", null), false);
				}
				else if (this._fixedTournamentCount < 4)
				{
					MBTextManager.SetTextVariable("POSITIVE_OPTION", new TextObject("{=TKRsPVMU}Yes, I did. Be around when the tournament is over.", null), false);
				}
				else
				{
					MBTextManager.SetTextVariable("POSITIVE_OPTION", new TextObject("{=26XPQw2v}I will miss this little deal we had. See you at the end", null), false);
				}
				return true;
			}

			// Token: 0x06005482 RID: 21634 RVA: 0x0018D3FA File Offset: 0x0018B5FA
			private void PositiveOptionConsequences()
			{
				this._readyToStartTournament = true;
			}

			// Token: 0x06005483 RID: 21635 RVA: 0x0018D403 File Offset: 0x0018B603
			private bool NegativeOptionCondition()
			{
				bool flag = this._fixedTournamentCount >= 4;
				if (flag)
				{
					MBTextManager.SetTextVariable("NEGATIVE_OPTION", new TextObject("{=vapdvRQO}This deal was a mistake. We will not talk again after this last tournament.", null), false);
				}
				return flag;
			}

			// Token: 0x06005484 RID: 21636 RVA: 0x0018D42A File Offset: 0x0018B62A
			private void NegativeOptionConsequence()
			{
				this._readyToStartTournament = true;
			}

			// Token: 0x06005485 RID: 21637 RVA: 0x0018D434 File Offset: 0x0018B634
			private TextObject GetDirectiveText()
			{
				if (this._currentDirective == BettingFraudIssueBehavior.BettingFraudQuest.Directives.LoseAt3RdRound)
				{
					return new TextObject("{=aHlcBLYB}Lose this tournament at 3rd round", null);
				}
				if (this._currentDirective == BettingFraudIssueBehavior.BettingFraudQuest.Directives.LoseAt4ThRound)
				{
					return new TextObject("{=hc1mnqOx}Lose this tournament at 4th round", null);
				}
				if (this._currentDirective == BettingFraudIssueBehavior.BettingFraudQuest.Directives.WinTheTournament)
				{
					return new TextObject("{=hl4pTsaO}Win this tournament", null);
				}
				return TextObject.GetEmpty();
			}

			// Token: 0x06005486 RID: 21638 RVA: 0x0018D488 File Offset: 0x0018B688
			private DialogFlow GetDialogWithThugEnd()
			{
				return DialogFlow.CreateDialogFlow("start", 125).NpcLine(new TextObject("{=!}{GREETING_LINE}", null), null, null, null, null).Condition(new ConversationSentence.OnConditionDelegate(this.DialogWithThugEndCondition))
					.Consequence(new ConversationSentence.OnConsequenceDelegate(this.DialogWithThugEndConsequence))
					.CloseDialog();
			}

			// Token: 0x06005487 RID: 21639 RVA: 0x0018D4DC File Offset: 0x0018B6DC
			private bool DialogWithThugEndCondition()
			{
				bool flag = CharacterObject.OneToOneConversationCharacter == this._thug && this._startTournamentEndConversation;
				if (flag)
				{
					TextObject textObject = TextObject.GetEmpty();
					switch (this._afterTournamentConversationState)
					{
					case BettingFraudIssueBehavior.BettingFraudQuest.AfterTournamentConversationState.SmallReward:
						textObject = new TextObject("{=ZM8t4ZW2}We are very impressed, my friend. Here is the payment as promised. I hope we can continue this profitable partnership. See you at the next tournament.[if:convo_happy][ib:demure]", null);
						GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, 250, false);
						break;
					case BettingFraudIssueBehavior.BettingFraudQuest.AfterTournamentConversationState.BigReward:
						textObject = new TextObject("{=9vOZWY25}What an exciting result! I will definitely miss these tournaments. Well, maybe after some time goes by and memories get a little hazy we can continue. Here is the last payment. Very well deserved.[if:convo_happy][ib:demure]", null);
						break;
					case BettingFraudIssueBehavior.BettingFraudQuest.AfterTournamentConversationState.MinorOffense:
						textObject = new TextObject("{=d8bGHJnZ}This was not we were expecting. We lost some money. Well, Lady Fortune always casts her ballot too in these contests. But try to reassure us that this was her plan, and not yours, eh?[if:convo_grave][ib:closed2]", null);
						break;
					case BettingFraudIssueBehavior.BettingFraudQuest.AfterTournamentConversationState.SecondMinorOffense:
						textObject = new TextObject("{=bNAG2t8S}Well, my friend, either you're playing us false or you're just not very good at this. Either way, {QUEST_GIVER.LINK} wishes to tell you that {?QUEST_GIVER.GENDER}her{?}his{\\?} association with you is over.[if:convo_predatory][ib:closed2]", null);
						textObject.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, false);
						break;
					case BettingFraudIssueBehavior.BettingFraudQuest.AfterTournamentConversationState.MajorOffense:
						textObject = new TextObject("{=Lyqx3NYE}Well... What happened back there... That wasn't bad luck or incompetence. {QUEST_GIVER.LINK} trusted in you and {?QUEST_GIVER.GENDER}She{?}He{\\?} doesn't take well to betrayal.[if:convo_angry][ib:warrior]", null);
						textObject.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, false);
						break;
					default:
						Debug.FailedAssert("After tournament conversation state is not set!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Issues\\BettingFraudIssueBehavior.cs", "DialogWithThugEndCondition", 722);
						break;
					}
					MBTextManager.SetTextVariable("GREETING_LINE", textObject, false);
				}
				return flag;
			}

			// Token: 0x06005488 RID: 21640 RVA: 0x0018D5E0 File Offset: 0x0018B7E0
			private void DialogWithThugEndConsequence()
			{
				this._startTournamentEndConversation = false;
				switch (this._afterTournamentConversationState)
				{
				case BettingFraudIssueBehavior.BettingFraudQuest.AfterTournamentConversationState.SmallReward:
				case BettingFraudIssueBehavior.BettingFraudQuest.AfterTournamentConversationState.MinorOffense:
					break;
				case BettingFraudIssueBehavior.BettingFraudQuest.AfterTournamentConversationState.BigReward:
					this.MainHeroSuccessfullyFixedTournaments();
					return;
				case BettingFraudIssueBehavior.BettingFraudQuest.AfterTournamentConversationState.SecondMinorOffense:
					this.MainHeroFailToFixTournaments();
					return;
				case BettingFraudIssueBehavior.BettingFraudQuest.AfterTournamentConversationState.MajorOffense:
					if (this._counterOfferAccepted)
					{
						this.MainHeroAcceptsCounterOffer();
						return;
					}
					this.MainHeroChooseNotToFixTournaments();
					break;
				default:
					return;
				}
			}

			// Token: 0x06005489 RID: 21641 RVA: 0x0018D63C File Offset: 0x0018B83C
			private DialogFlow GetCounterOfferDialog()
			{
				return DialogFlow.CreateDialogFlow("start", 125).NpcLine(new TextObject("{=bUfBHSsz}Hold on a moment, friend. I need to talk to you.[ib:aggressive]", null), null, null, null, null).Condition(new ConversationSentence.OnConditionDelegate(this.CounterOfferConversationStartCondition))
					.Consequence(new ConversationSentence.OnConsequenceDelegate(this.CounterOfferConversationStartConsequence))
					.PlayerLine(new TextObject("{=PZfR7hEK}What do you want? I have a tournament to prepare for.", null), null, null, null)
					.NpcLine(new TextObject("{=GN9F316V}Oh of course you do. {QUEST_GIVER.LINK}'s people have been running around placing bets - we know all about your arrangement, you see. And let me tell you something: as these arrangements go, {QUEST_GIVER.LINK} is getting you cheap. Do you want to see real money? Win this tournament and I will pay you what you're worth. And isn't it better to win than to lose?[if:convo_mocking_aristocratic][ib:confident2]", null), null, null, null, null)
					.Condition(new ConversationSentence.OnConditionDelegate(this.AccusationCondition))
					.BeginPlayerOptions(null, false)
					.PlayerOption(new TextObject("{=MacG8ikN}I will think about it.", null), null, null, null)
					.Consequence(new ConversationSentence.OnConsequenceDelegate(this.CounterOfferAcceptedConsequence))
					.CloseDialog()
					.PlayerOption(new TextObject("{=bT279pk9}I have no idea what you talking about. Be on your way, friend.", null), null, null, null)
					.CloseDialog()
					.EndPlayerOptions()
					.CloseDialog();
			}

			// Token: 0x0600548A RID: 21642 RVA: 0x0018D715 File Offset: 0x0018B915
			private bool CounterOfferConversationStartCondition()
			{
				return this._counterOfferNotable != null && CharacterObject.OneToOneConversationCharacter == this._counterOfferNotable.CharacterObject;
			}

			// Token: 0x0600548B RID: 21643 RVA: 0x0018D733 File Offset: 0x0018B933
			private void CounterOfferConversationStartConsequence()
			{
				this._counterOfferConversationDone = true;
			}

			// Token: 0x0600548C RID: 21644 RVA: 0x0018D73C File Offset: 0x0018B93C
			private bool AccusationCondition()
			{
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, null, false);
				return true;
			}

			// Token: 0x0600548D RID: 21645 RVA: 0x0018D757 File Offset: 0x0018B957
			private void CounterOfferAcceptedConsequence()
			{
				this._counterOfferAccepted = true;
			}

			// Token: 0x0600548E RID: 21646 RVA: 0x0018D760 File Offset: 0x0018B960
			private void MainHeroSuccessfullyFixedTournaments()
			{
				TextObject textObject = new TextObject("{=aCA83avL}You have placed in the tournaments as {QUEST_GIVER.LINK} wished.", null);
				textObject.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, false);
				base.AddLog(textObject, false);
				GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, 2500, false);
				Clan.PlayerClan.AddRenown(2f, true);
				base.QuestGiver.AddPower(10f);
				base.QuestGiver.CurrentSettlement.Town.Security += -20f;
				this.RelationshipChangeWithQuestGiver = 5;
				base.CompleteQuestWithSuccess();
			}

			// Token: 0x0600548F RID: 21647 RVA: 0x0018D7F8 File Offset: 0x0018B9F8
			private void MainHeroFailToFixTournaments()
			{
				TextObject textObject = new TextObject("{=ETbToaZC}You have failed to place in the tournaments as {QUEST_GIVER.LINK} wished.", null);
				textObject.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, false);
				base.AddLog(textObject, false);
				base.QuestGiver.AddPower(-10f);
				base.QuestGiver.CurrentSettlement.Town.Security += 10f;
				this.RelationshipChangeWithQuestGiver = -5;
				base.CompleteQuestWithFail(null);
			}

			// Token: 0x06005490 RID: 21648 RVA: 0x0018D874 File Offset: 0x0018BA74
			private void MainHeroChooseNotToFixTournaments()
			{
				TextObject textObject = new TextObject("{=52smwnzz}You have chosen not to place in the tournaments as {QUEST_GIVER.LINK} wished.", null);
				textObject.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, false);
				base.AddLog(textObject, false);
				base.QuestGiver.AddPower(-15f);
				base.QuestGiver.CurrentSettlement.Town.Security += 15f;
				this.RelationshipChangeWithQuestGiver = -10;
				base.CompleteQuestWithFail(null);
			}

			// Token: 0x06005491 RID: 21649 RVA: 0x0018D8F0 File Offset: 0x0018BAF0
			private void MainHeroAcceptsCounterOffer()
			{
				TextObject textObject = new TextObject("{=nb0wqaGA}You have made a deal with {NOTABLE.LINK} to betray {QUEST_GIVER.LINK}.", null);
				textObject.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, false);
				textObject.SetCharacterProperties("NOTABLE", this._counterOfferNotable.CharacterObject, false);
				base.AddLog(textObject, false);
				GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, 4500, false);
				base.QuestGiver.AddPower(-15f);
				base.QuestGiver.CurrentSettlement.Town.Security += 15f;
				ChangeRelationAction.ApplyPlayerRelation(this._counterOfferNotable, 2, true, true);
				this.RelationshipChangeWithQuestGiver = -10;
				base.CompleteQuestWithFail(null);
			}

			// Token: 0x06005492 RID: 21650 RVA: 0x0018D99F File Offset: 0x0018BB9F
			private void PlayerDidNotCompleteTournaments()
			{
				base.AddLog(this.QuestFailedWithTimeOutLog, false);
				ChangeRelationAction.ApplyPlayerRelation(base.QuestGiver, -5, true, true);
			}

			// Token: 0x04001B9F RID: 7071
			private const int TournamentFixCount = 5;

			// Token: 0x04001BA0 RID: 7072
			private const int MinorOffensiveLimit = 2;

			// Token: 0x04001BA1 RID: 7073
			private const int SmallReward = 250;

			// Token: 0x04001BA2 RID: 7074
			private const int BigReward = 2500;

			// Token: 0x04001BA3 RID: 7075
			private const int CounterOfferReward = 4500;

			// Token: 0x04001BA4 RID: 7076
			private const string MaleThug = "betting_fraud_thug_male";

			// Token: 0x04001BA5 RID: 7077
			private const string FemaleThug = "betting_fraud_thug_female";

			// Token: 0x04001BA6 RID: 7078
			[SaveableField(100)]
			private JournalLog _startLog;

			// Token: 0x04001BA7 RID: 7079
			[SaveableField(1)]
			private Hero _counterOfferNotable;

			// Token: 0x04001BA8 RID: 7080
			[SaveableField(10)]
			internal readonly CharacterObject _thug;

			// Token: 0x04001BA9 RID: 7081
			[SaveableField(20)]
			private int _fixedTournamentCount;

			// Token: 0x04001BAA RID: 7082
			[SaveableField(30)]
			private int _minorOffensiveCount;

			// Token: 0x04001BAB RID: 7083
			private BettingFraudIssueBehavior.BettingFraudQuest.Directives _currentDirective;

			// Token: 0x04001BAC RID: 7084
			private BettingFraudIssueBehavior.BettingFraudQuest.AfterTournamentConversationState _afterTournamentConversationState;

			// Token: 0x04001BAD RID: 7085
			private bool _counterOfferAccepted;

			// Token: 0x04001BAE RID: 7086
			private bool _readyToStartTournament;

			// Token: 0x04001BAF RID: 7087
			private bool _startTournamentEndConversation;

			// Token: 0x04001BB0 RID: 7088
			[SaveableField(40)]
			private bool _counterOfferConversationDone;

			// Token: 0x020008BF RID: 2239
			private enum Directives
			{
				// Token: 0x040024AA RID: 9386
				None,
				// Token: 0x040024AB RID: 9387
				LoseAt3RdRound = 2,
				// Token: 0x040024AC RID: 9388
				LoseAt4ThRound,
				// Token: 0x040024AD RID: 9389
				WinTheTournament
			}

			// Token: 0x020008C0 RID: 2240
			private enum AfterTournamentConversationState
			{
				// Token: 0x040024AF RID: 9391
				None,
				// Token: 0x040024B0 RID: 9392
				SmallReward,
				// Token: 0x040024B1 RID: 9393
				BigReward,
				// Token: 0x040024B2 RID: 9394
				MinorOffense,
				// Token: 0x040024B3 RID: 9395
				SecondMinorOffense,
				// Token: 0x040024B4 RID: 9396
				MajorOffense
			}
		}

		// Token: 0x020006E9 RID: 1769
		public class BettingFraudIssueTypeDefiner : SaveableTypeDefiner
		{
			// Token: 0x06005494 RID: 21652 RVA: 0x0018D9CD File Offset: 0x0018BBCD
			public BettingFraudIssueTypeDefiner()
				: base(600327)
			{
			}

			// Token: 0x06005495 RID: 21653 RVA: 0x0018D9DA File Offset: 0x0018BBDA
			protected override void DefineClassTypes()
			{
				base.AddClassDefinition(typeof(BettingFraudIssueBehavior.BettingFraudIssue), 1, null);
				base.AddClassDefinition(typeof(BettingFraudIssueBehavior.BettingFraudQuest), 2, null);
			}
		}
	}
}
