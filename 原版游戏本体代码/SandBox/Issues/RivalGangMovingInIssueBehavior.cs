using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using SandBox.Missions.MissionLogics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace SandBox.Issues
{
	// Token: 0x020000B3 RID: 179
	public class RivalGangMovingInIssueBehavior : CampaignBehaviorBase
	{
		// Token: 0x1700009A RID: 154
		// (get) Token: 0x06000772 RID: 1906 RVA: 0x000330F0 File Offset: 0x000312F0
		private static RivalGangMovingInIssueBehavior.RivalGangMovingInIssueQuest Instance
		{
			get
			{
				RivalGangMovingInIssueBehavior campaignBehavior = Campaign.Current.GetCampaignBehavior<RivalGangMovingInIssueBehavior>();
				if (campaignBehavior._cachedQuest != null && campaignBehavior._cachedQuest.IsOngoing)
				{
					return campaignBehavior._cachedQuest;
				}
				using (List<QuestBase>.Enumerator enumerator = Campaign.Current.QuestManager.Quests.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						RivalGangMovingInIssueBehavior.RivalGangMovingInIssueQuest cachedQuest;
						if ((cachedQuest = enumerator.Current as RivalGangMovingInIssueBehavior.RivalGangMovingInIssueQuest) != null)
						{
							campaignBehavior._cachedQuest = cachedQuest;
							return campaignBehavior._cachedQuest;
						}
					}
				}
				return null;
			}
		}

		// Token: 0x06000773 RID: 1907 RVA: 0x00033188 File Offset: 0x00031388
		private void OnCheckForIssue(Hero hero)
		{
			if (this.ConditionsHold(hero))
			{
				Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(new PotentialIssueData.StartIssueDelegate(this.OnStartIssue), typeof(RivalGangMovingInIssueBehavior.RivalGangMovingInIssue), IssueBase.IssueFrequency.Common, null));
				return;
			}
			Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(typeof(RivalGangMovingInIssueBehavior.RivalGangMovingInIssue), IssueBase.IssueFrequency.Common));
		}

		// Token: 0x06000774 RID: 1908 RVA: 0x000331EC File Offset: 0x000313EC
		private IssueBase OnStartIssue(in PotentialIssueData pid, Hero issueOwner)
		{
			Hero rivalGangLeader = this.GetRivalGangLeader(issueOwner);
			return new RivalGangMovingInIssueBehavior.RivalGangMovingInIssue(issueOwner, rivalGangLeader);
		}

		// Token: 0x06000775 RID: 1909 RVA: 0x00033208 File Offset: 0x00031408
		private static void rival_gang_wait_duration_is_over_menu_on_init(MenuCallbackArgs args)
		{
			Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
			TextObject text = new TextObject("{=9Kr9pjGs}{QUEST_GIVER.LINK} has prepared {?QUEST_GIVER.GENDER}her{?}his{\\?} men and is waiting for you.", null);
			StringHelpers.SetCharacterProperties("QUEST_GIVER", RivalGangMovingInIssueBehavior.Instance.QuestGiver.CharacterObject, null, false);
			MBTextManager.SetTextVariable("MENU_TEXT", text, false);
		}

		// Token: 0x06000776 RID: 1910 RVA: 0x00033254 File Offset: 0x00031454
		private bool ConditionsHold(Hero issueGiver)
		{
			return issueGiver.IsGangLeader && issueGiver.CurrentSettlement != null && issueGiver.CurrentSettlement.IsTown && issueGiver.CurrentSettlement.Town.Security <= 60f && this.GetRivalGangLeader(issueGiver) != null;
		}

		// Token: 0x06000777 RID: 1911 RVA: 0x000332A4 File Offset: 0x000314A4
		private void rival_gang_quest_wait_duration_is_over_yes_consequence(MenuCallbackArgs args)
		{
			CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter, null, true, true, false, false, false, false), new ConversationCharacterData(RivalGangMovingInIssueBehavior.Instance.QuestGiver.CharacterObject, null, true, true, false, false, false, false));
		}

		// Token: 0x06000778 RID: 1912 RVA: 0x000332E4 File Offset: 0x000314E4
		private Hero GetRivalGangLeader(Hero issueOwner)
		{
			Hero result = null;
			foreach (Hero hero in issueOwner.CurrentSettlement.Notables)
			{
				if (hero != issueOwner && hero.IsGangLeader && hero.CanHaveCampaignIssues())
				{
					result = hero;
					break;
				}
			}
			return result;
		}

		// Token: 0x06000779 RID: 1913 RVA: 0x00033350 File Offset: 0x00031550
		private bool rival_gang_quest_wait_duration_is_over_yes_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Continue;
			return true;
		}

		// Token: 0x0600077A RID: 1914 RVA: 0x0003335B File Offset: 0x0003155B
		private bool rival_gang_quest_wait_duration_is_over_no_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Leave;
			return true;
		}

		// Token: 0x0600077B RID: 1915 RVA: 0x00033366 File Offset: 0x00031566
		public override void RegisterEvents()
		{
			CampaignEvents.OnCheckForIssueEvent.AddNonSerializedListener(this, new Action<Hero>(this.OnCheckForIssue));
			CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnSessionLaunched));
		}

		// Token: 0x0600077C RID: 1916 RVA: 0x00033398 File Offset: 0x00031598
		private void OnSessionLaunched(CampaignGameStarter gameStarter)
		{
			gameStarter.AddGameMenu("rival_gang_quest_before_fight", "", new OnInitDelegate(RivalGangMovingInIssueBehavior.rival_gang_quest_before_fight_init), GameMenu.MenuOverlayType.SettlementWithBoth, GameMenu.MenuFlags.None, null);
			gameStarter.AddGameMenu("rival_gang_quest_after_fight", "", new OnInitDelegate(RivalGangMovingInIssueBehavior.rival_gang_quest_after_fight_init), GameMenu.MenuOverlayType.SettlementWithBoth, GameMenu.MenuFlags.None, null);
			gameStarter.AddGameMenu("rival_gang_quest_wait_duration_is_over", "{MENU_TEXT}", new OnInitDelegate(RivalGangMovingInIssueBehavior.rival_gang_wait_duration_is_over_menu_on_init), GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
			gameStarter.AddGameMenuOption("rival_gang_quest_wait_duration_is_over", "rival_gang_quest_wait_duration_is_over_yes", "{=aka03VdU}Meet {?QUEST_GIVER.GENDER}her{?}him{\\?} now", new GameMenuOption.OnConditionDelegate(this.rival_gang_quest_wait_duration_is_over_yes_condition), new GameMenuOption.OnConsequenceDelegate(this.rival_gang_quest_wait_duration_is_over_yes_consequence), false, -1, false, null);
			gameStarter.AddGameMenuOption("rival_gang_quest_wait_duration_is_over", "rival_gang_quest_wait_duration_is_over_no", "{=NIzQb6nT}Leave and meet {?QUEST_GIVER.GENDER}her{?}him{\\?} later", new GameMenuOption.OnConditionDelegate(this.rival_gang_quest_wait_duration_is_over_no_condition), new GameMenuOption.OnConsequenceDelegate(this.rival_gang_quest_wait_duration_is_over_no_consequence), true, -1, false, null);
		}

		// Token: 0x0600077D RID: 1917 RVA: 0x00033464 File Offset: 0x00031664
		private void rival_gang_quest_wait_duration_is_over_no_consequence(MenuCallbackArgs args)
		{
			Campaign.Current.CurrentMenuContext.SwitchToMenu("town_wait_menus");
		}

		// Token: 0x0600077E RID: 1918 RVA: 0x0003347A File Offset: 0x0003167A
		private static void rival_gang_quest_before_fight_init(MenuCallbackArgs args)
		{
			if (RivalGangMovingInIssueBehavior.Instance != null && RivalGangMovingInIssueBehavior.Instance._isFinalStage)
			{
				RivalGangMovingInIssueBehavior.Instance.StartAlleyBattle();
			}
		}

		// Token: 0x0600077F RID: 1919 RVA: 0x0003349C File Offset: 0x0003169C
		private static void rival_gang_quest_after_fight_init(MenuCallbackArgs args)
		{
			if (RivalGangMovingInIssueBehavior.Instance != null && RivalGangMovingInIssueBehavior.Instance._isReadyToBeFinalized)
			{
				bool hasPlayerWon = PlayerEncounter.Battle.WinningSide == PlayerEncounter.Battle.PlayerSide;
				PlayerEncounter.Current.FinalizeBattle();
				RivalGangMovingInIssueBehavior.Instance.HandlePlayerEncounterResult(hasPlayerWon);
			}
		}

		// Token: 0x06000780 RID: 1920 RVA: 0x000334E8 File Offset: 0x000316E8
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x06000781 RID: 1921 RVA: 0x000334EC File Offset: 0x000316EC
		[GameMenuInitializationHandler("rival_gang_quest_after_fight")]
		[GameMenuInitializationHandler("rival_gang_quest_wait_duration_is_over")]
		private static void game_menu_rival_gang_quest_end_on_init(MenuCallbackArgs args)
		{
			Settlement currentSettlement = Settlement.CurrentSettlement;
			if (currentSettlement != null)
			{
				args.MenuContext.SetBackgroundMeshName(currentSettlement.SettlementComponent.WaitMeshName);
			}
		}

		// Token: 0x040003FF RID: 1023
		private const IssueBase.IssueFrequency RivalGangLeaderIssueFrequency = IssueBase.IssueFrequency.Common;

		// Token: 0x04000400 RID: 1024
		private RivalGangMovingInIssueBehavior.RivalGangMovingInIssueQuest _cachedQuest;

		// Token: 0x020001C7 RID: 455
		public class RivalGangMovingInIssueTypeDefiner : SaveableTypeDefiner
		{
			// Token: 0x06001134 RID: 4404 RVA: 0x0006FAF6 File Offset: 0x0006DCF6
			public RivalGangMovingInIssueTypeDefiner()
				: base(310000)
			{
			}

			// Token: 0x06001135 RID: 4405 RVA: 0x0006FB03 File Offset: 0x0006DD03
			protected override void DefineClassTypes()
			{
				base.AddClassDefinition(typeof(RivalGangMovingInIssueBehavior.RivalGangMovingInIssue), 1, null);
				base.AddClassDefinition(typeof(RivalGangMovingInIssueBehavior.RivalGangMovingInIssueQuest), 2, null);
			}
		}

		// Token: 0x020001C8 RID: 456
		public class RivalGangMovingInIssue : IssueBase
		{
			// Token: 0x170001A0 RID: 416
			// (get) Token: 0x06001136 RID: 4406 RVA: 0x0006FB29 File Offset: 0x0006DD29
			public override IssueBase.AlternativeSolutionScaleFlag AlternativeSolutionScaleFlags
			{
				get
				{
					return IssueBase.AlternativeSolutionScaleFlag.Casualties | IssueBase.AlternativeSolutionScaleFlag.FailureRisk;
				}
			}

			// Token: 0x170001A1 RID: 417
			// (get) Token: 0x06001137 RID: 4407 RVA: 0x0006FB2D File Offset: 0x0006DD2D
			// (set) Token: 0x06001138 RID: 4408 RVA: 0x0006FB35 File Offset: 0x0006DD35
			[SaveableProperty(207)]
			public Hero RivalGangLeader { get; private set; }

			// Token: 0x170001A2 RID: 418
			// (get) Token: 0x06001139 RID: 4409 RVA: 0x0006FB3E File Offset: 0x0006DD3E
			public override int AlternativeSolutionBaseNeededMenCount
			{
				get
				{
					return 4 + MathF.Ceiling(6f * base.IssueDifficultyMultiplier);
				}
			}

			// Token: 0x170001A3 RID: 419
			// (get) Token: 0x0600113A RID: 4410 RVA: 0x0006FB53 File Offset: 0x0006DD53
			protected override int AlternativeSolutionBaseDurationInDaysInternal
			{
				get
				{
					return 3 + MathF.Ceiling(5f * base.IssueDifficultyMultiplier);
				}
			}

			// Token: 0x170001A4 RID: 420
			// (get) Token: 0x0600113B RID: 4411 RVA: 0x0006FB68 File Offset: 0x0006DD68
			protected override int RewardGold
			{
				get
				{
					return (int)(600f + 1700f * base.IssueDifficultyMultiplier);
				}
			}

			// Token: 0x170001A5 RID: 421
			// (get) Token: 0x0600113C RID: 4412 RVA: 0x0006FB7D File Offset: 0x0006DD7D
			protected override int CompanionSkillRewardXP
			{
				get
				{
					return (int)(750f + 1000f * base.IssueDifficultyMultiplier);
				}
			}

			// Token: 0x170001A6 RID: 422
			// (get) Token: 0x0600113D RID: 4413 RVA: 0x0006FB94 File Offset: 0x0006DD94
			public override TextObject IssueBriefByIssueGiver
			{
				get
				{
					TextObject textObject = new TextObject("{=GXk6f9ah}I've got a problem... [ib:confident][if:convo_undecided_closed]And {?TARGET_NOTABLE.GENDER}her{?}his{\\?} name is {TARGET_NOTABLE.LINK}. {?TARGET_NOTABLE.GENDER}Her{?}His{\\?} people have been coming around outside the walls, robbing the dice-players and the drinkers enjoying themselves under our protection. Me and my boys are eager to teach them a lesson but I figure some extra muscle wouldn't hurt.", null);
					if (base.IssueOwner.RandomInt(2) == 0)
					{
						textObject = new TextObject("{=rgTGzfzI}Yeah. I have a problem all right. [ib:confident][if:convo_undecided_closed]{?TARGET_NOTABLE.GENDER}Her{?}His{\\?} name is {TARGET_NOTABLE.LINK}. {?TARGET_NOTABLE.GENDER}Her{?}His{\\?} people have been bothering shop owners under our protection, demanding money and making threats. Let me tell you something - those shop owners are my cows, and no one else gets to milk them. We're ready to teach these interlopers a lesson, but I could use some help.", null);
					}
					if (this.RivalGangLeader != null)
					{
						StringHelpers.SetCharacterProperties("TARGET_NOTABLE", this.RivalGangLeader.CharacterObject, textObject, false);
					}
					return textObject;
				}
			}

			// Token: 0x170001A7 RID: 423
			// (get) Token: 0x0600113E RID: 4414 RVA: 0x0006FBE8 File Offset: 0x0006DDE8
			public override TextObject IssueAcceptByPlayer
			{
				get
				{
					return new TextObject("{=kc6vCycY}What exactly do you want me to do?", null);
				}
			}

			// Token: 0x170001A8 RID: 424
			// (get) Token: 0x0600113F RID: 4415 RVA: 0x0006FBF5 File Offset: 0x0006DDF5
			public override TextObject IssueQuestSolutionExplanationByIssueGiver
			{
				get
				{
					TextObject textObject = new TextObject("{=tyyAfWRR}We already had a small scuffle with them recently. [if:convo_mocking_revenge]They'll be waiting for us to come down hard. Instead, we'll hold off for {NUMBER} days. Let them think that we're backing off… Then, after {NUMBER} days, your men and mine will hit them in the middle of the night when they least expect it. I'll send you a messenger when the time comes and we'll strike them down together.", null);
					textObject.SetTextVariable("NUMBER", 2);
					return textObject;
				}
			}

			// Token: 0x170001A9 RID: 425
			// (get) Token: 0x06001140 RID: 4416 RVA: 0x0006FC0F File Offset: 0x0006DE0F
			public override TextObject IssueAlternativeSolutionExplanationByIssueGiver
			{
				get
				{
					TextObject textObject = new TextObject("{=sSIjPCPO}If you'd rather not go into the fray yourself, [if:convo_mocking_aristocratic]you can leave me one of your companions together with {TROOP_COUNT} or so good men. If they stuck around for {RETURN_DAYS} days or so, I'd count it a very big favor.", null);
					textObject.SetTextVariable("TROOP_COUNT", base.GetTotalAlternativeSolutionNeededMenCount());
					textObject.SetTextVariable("RETURN_DAYS", base.GetTotalAlternativeSolutionDurationInDays());
					return textObject;
				}
			}

			// Token: 0x170001AA RID: 426
			// (get) Token: 0x06001141 RID: 4417 RVA: 0x0006FC40 File Offset: 0x0006DE40
			protected override TextObject AlternativeSolutionStartLog
			{
				get
				{
					TextObject textObject = new TextObject("{=ymbVPod1}{ISSUE_GIVER.LINK}, a gang leader from {SETTLEMENT}, has told you about a new gang that is trying to get a hold on the town. You asked {COMPANION.LINK} to take {TROOP_COUNT} of your best men to stay with {ISSUE_GIVER.LINK} and help {?ISSUE_GIVER.GENDER}her{?}him{\\?} in the coming gang war. They should return to you in {RETURN_DAYS} days.", null);
					StringHelpers.SetCharacterProperties("ISSUE_GIVER", base.IssueOwner.CharacterObject, textObject, false);
					StringHelpers.SetCharacterProperties("COMPANION", base.AlternativeSolutionHero.CharacterObject, textObject, false);
					textObject.SetTextVariable("SETTLEMENT", base.IssueOwner.CurrentSettlement.EncyclopediaLinkWithName);
					textObject.SetTextVariable("TROOP_COUNT", this.AlternativeSolutionSentTroops.TotalManCount - 1);
					textObject.SetTextVariable("RETURN_DAYS", base.GetTotalAlternativeSolutionDurationInDays());
					return textObject;
				}
			}

			// Token: 0x170001AB RID: 427
			// (get) Token: 0x06001142 RID: 4418 RVA: 0x0006FCD1 File Offset: 0x0006DED1
			public override TextObject IssueQuestSolutionAcceptByPlayer
			{
				get
				{
					return new TextObject("{=LdCte9H0}I'll fight the other gang with you myself.", null);
				}
			}

			// Token: 0x170001AC RID: 428
			// (get) Token: 0x06001143 RID: 4419 RVA: 0x0006FCDE File Offset: 0x0006DEDE
			public override TextObject IssueAlternativeSolutionAcceptByPlayer
			{
				get
				{
					return new TextObject("{=AdbiUqtT}I'm busy, but I will leave a companion and some men.", null);
				}
			}

			// Token: 0x170001AD RID: 429
			// (get) Token: 0x06001144 RID: 4420 RVA: 0x0006FCEB File Offset: 0x0006DEEB
			public override TextObject IssueAlternativeSolutionResponseByIssueGiver
			{
				get
				{
					return new TextObject("{=0enbhess}Thank you. [ib:normal][if:convo_approving]I'm sure your guys are worth their salt..", null);
				}
			}

			// Token: 0x170001AE RID: 430
			// (get) Token: 0x06001145 RID: 4421 RVA: 0x0006FCF8 File Offset: 0x0006DEF8
			public override TextObject IssueDiscussAlternativeSolution
			{
				get
				{
					return new TextObject("{=QR0V8Ae5}Our lads are well hidden nearby,[ib:normal][if:convo_excited] waiting for the signal to go get those bastards. I won't forget this little favor you're doing me.", null);
				}
			}

			// Token: 0x170001AF RID: 431
			// (get) Token: 0x06001146 RID: 4422 RVA: 0x0006FD05 File Offset: 0x0006DF05
			public override bool IsThereAlternativeSolution
			{
				get
				{
					return true;
				}
			}

			// Token: 0x170001B0 RID: 432
			// (get) Token: 0x06001147 RID: 4423 RVA: 0x0006FD08 File Offset: 0x0006DF08
			public override bool IsThereLordSolution
			{
				get
				{
					return false;
				}
			}

			// Token: 0x170001B1 RID: 433
			// (get) Token: 0x06001148 RID: 4424 RVA: 0x0006FD0B File Offset: 0x0006DF0B
			public override TextObject Title
			{
				get
				{
					TextObject textObject = new TextObject("{=vAjgn7yx}Rival Gang Moving in at {SETTLEMENT}", null);
					string tag = "SETTLEMENT";
					Settlement issueSettlement = base.IssueSettlement;
					textObject.SetTextVariable(tag, ((issueSettlement != null) ? issueSettlement.Name : null) ?? base.IssueOwner.HomeSettlement.Name);
					return textObject;
				}
			}

			// Token: 0x170001B2 RID: 434
			// (get) Token: 0x06001149 RID: 4425 RVA: 0x0006FD4A File Offset: 0x0006DF4A
			public override TextObject Description
			{
				get
				{
					return new TextObject("{=H4EVfKAh}Gang leader needs help to beat the rival gang.", null);
				}
			}

			// Token: 0x170001B3 RID: 435
			// (get) Token: 0x0600114A RID: 4426 RVA: 0x0006FD58 File Offset: 0x0006DF58
			public override TextObject IssueAsRumorInSettlement
			{
				get
				{
					TextObject textObject = new TextObject("{=C9feTaca}I hear {QUEST_GIVER.LINK} is going to sort it out with {RIVAL_GANG_LEADER.LINK} once and for all.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.IssueOwner.CharacterObject, textObject, false);
					StringHelpers.SetCharacterProperties("RIVAL_GANG_LEADER", this.RivalGangLeader.CharacterObject, textObject, false);
					return textObject;
				}
			}

			// Token: 0x170001B4 RID: 436
			// (get) Token: 0x0600114B RID: 4427 RVA: 0x0006FDA2 File Offset: 0x0006DFA2
			protected override bool IssueQuestCanBeDuplicated
			{
				get
				{
					return false;
				}
			}

			// Token: 0x0600114C RID: 4428 RVA: 0x0006FDA5 File Offset: 0x0006DFA5
			public RivalGangMovingInIssue(Hero issueOwner, Hero rivalGangLeader)
				: base(issueOwner, CampaignTime.DaysFromNow(15f))
			{
				this.RivalGangLeader = rivalGangLeader;
			}

			// Token: 0x0600114D RID: 4429 RVA: 0x0006FDBF File Offset: 0x0006DFBF
			public override void OnHeroCanHaveCampaignIssuesInfoIsRequested(Hero hero, ref bool result)
			{
				if (hero == this.RivalGangLeader)
				{
					result = false;
				}
			}

			// Token: 0x0600114E RID: 4430 RVA: 0x0006FDCD File Offset: 0x0006DFCD
			protected override float GetIssueEffectAmountInternal(IssueEffect issueEffect)
			{
				if (issueEffect == DefaultIssueEffects.IssueOwnerPower)
				{
					return -0.2f;
				}
				if (issueEffect == DefaultIssueEffects.SettlementSecurity)
				{
					return -0.5f;
				}
				return 0f;
			}

			// Token: 0x0600114F RID: 4431 RVA: 0x0006FDF0 File Offset: 0x0006DFF0
			protected override void AlternativeSolutionEndWithSuccessConsequence()
			{
				this.RelationshipChangeWithIssueOwner = 5;
				ChangeRelationAction.ApplyPlayerRelation(this.RivalGangLeader, -5, true, true);
				base.IssueOwner.AddPower(10f);
				this.RivalGangLeader.AddPower(-10f);
			}

			// Token: 0x06001150 RID: 4432 RVA: 0x0006FE28 File Offset: 0x0006E028
			protected override void AlternativeSolutionEndWithFailureConsequence()
			{
				this.RelationshipChangeWithIssueOwner = -5;
				base.IssueSettlement.Town.Security += -10f;
				base.IssueOwner.AddPower(-10f);
			}

			// Token: 0x06001151 RID: 4433 RVA: 0x0006FE60 File Offset: 0x0006E060
			public override ValueTuple<SkillObject, int> GetAlternativeSolutionSkill(Hero hero)
			{
				int skillValue = hero.GetSkillValue(DefaultSkills.OneHanded);
				int skillValue2 = hero.GetSkillValue(DefaultSkills.TwoHanded);
				int skillValue3 = hero.GetSkillValue(DefaultSkills.Polearm);
				int skillValue4 = hero.GetSkillValue(DefaultSkills.Roguery);
				if (skillValue >= skillValue2 && skillValue >= skillValue3 && skillValue >= skillValue4)
				{
					return new ValueTuple<SkillObject, int>(DefaultSkills.OneHanded, 150);
				}
				if (skillValue2 >= skillValue3 && skillValue2 >= skillValue4)
				{
					return new ValueTuple<SkillObject, int>(DefaultSkills.TwoHanded, 150);
				}
				if (skillValue3 < skillValue4)
				{
					return new ValueTuple<SkillObject, int>(DefaultSkills.Roguery, 120);
				}
				return new ValueTuple<SkillObject, int>(DefaultSkills.Polearm, 150);
			}

			// Token: 0x06001152 RID: 4434 RVA: 0x0006FEF1 File Offset: 0x0006E0F1
			public override bool AlternativeSolutionCondition(out TextObject explanation)
			{
				return QuestHelper.CheckRosterForAlternativeSolution(MobileParty.MainParty.MemberRoster, base.GetTotalAlternativeSolutionNeededMenCount(), out explanation, 2, false);
			}

			// Token: 0x06001153 RID: 4435 RVA: 0x0006FF0B File Offset: 0x0006E10B
			public override bool DoTroopsSatisfyAlternativeSolution(TroopRoster troopRoster, out TextObject explanation)
			{
				return QuestHelper.CheckRosterForAlternativeSolution(troopRoster, base.GetTotalAlternativeSolutionNeededMenCount(), out explanation, 2, false);
			}

			// Token: 0x06001154 RID: 4436 RVA: 0x0006FF1C File Offset: 0x0006E11C
			public override bool IsTroopTypeNeededByAlternativeSolution(CharacterObject character)
			{
				return character.Tier >= 2;
			}

			// Token: 0x06001155 RID: 4437 RVA: 0x0006FF2A File Offset: 0x0006E12A
			protected override void OnGameLoad()
			{
			}

			// Token: 0x06001156 RID: 4438 RVA: 0x0006FF2C File Offset: 0x0006E12C
			protected override void HourlyTick()
			{
			}

			// Token: 0x06001157 RID: 4439 RVA: 0x0006FF2E File Offset: 0x0006E12E
			protected override QuestBase GenerateIssueQuest(string questId)
			{
				return new RivalGangMovingInIssueBehavior.RivalGangMovingInIssueQuest(questId, base.IssueOwner, this.RivalGangLeader, 8, this.RewardGold, base.IssueDifficultyMultiplier);
			}

			// Token: 0x06001158 RID: 4440 RVA: 0x0006FF4F File Offset: 0x0006E14F
			public override IssueBase.IssueFrequency GetFrequency()
			{
				return IssueBase.IssueFrequency.Common;
			}

			// Token: 0x06001159 RID: 4441 RVA: 0x0006FF54 File Offset: 0x0006E154
			protected override bool CanPlayerTakeQuestConditions(Hero issueGiver, out IssueBase.PreconditionFlags flag, out Hero relationHero, out SkillObject skill)
			{
				flag = IssueBase.PreconditionFlags.None;
				relationHero = null;
				skill = null;
				if (Hero.MainHero.IsWounded)
				{
					flag |= IssueBase.PreconditionFlags.Wounded;
				}
				if (issueGiver.GetRelationWithPlayer() < -10f)
				{
					flag |= IssueBase.PreconditionFlags.Relation;
					relationHero = issueGiver;
				}
				if (MobileParty.MainParty.MemberRoster.TotalHealthyCount < 5)
				{
					flag |= IssueBase.PreconditionFlags.NotEnoughTroops;
				}
				if (base.IssueOwner.CurrentSettlement.OwnerClan == Clan.PlayerClan)
				{
					flag |= IssueBase.PreconditionFlags.PlayerIsOwnerOfSettlement;
				}
				return flag == IssueBase.PreconditionFlags.None;
			}

			// Token: 0x0600115A RID: 4442 RVA: 0x0006FFD8 File Offset: 0x0006E1D8
			public override bool IssueStayAliveConditions()
			{
				return this.RivalGangLeader.IsAlive && base.IssueOwner.CurrentSettlement.OwnerClan != Clan.PlayerClan && base.IssueOwner.CurrentSettlement.Town.Security <= 80f;
			}

			// Token: 0x0600115B RID: 4443 RVA: 0x0007002A File Offset: 0x0006E22A
			protected override void CompleteIssueWithTimedOutConsequences()
			{
			}

			// Token: 0x0600115C RID: 4444 RVA: 0x0007002C File Offset: 0x0006E22C
			internal static void AutoGeneratedStaticCollectObjectsRivalGangMovingInIssue(object o, List<object> collectedObjects)
			{
				((RivalGangMovingInIssueBehavior.RivalGangMovingInIssue)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
			}

			// Token: 0x0600115D RID: 4445 RVA: 0x0007003A File Offset: 0x0006E23A
			protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
			{
				base.AutoGeneratedInstanceCollectObjects(collectedObjects);
				collectedObjects.Add(this.RivalGangLeader);
			}

			// Token: 0x0600115E RID: 4446 RVA: 0x0007004F File Offset: 0x0006E24F
			internal static object AutoGeneratedGetMemberValueRivalGangLeader(object o)
			{
				return ((RivalGangMovingInIssueBehavior.RivalGangMovingInIssue)o).RivalGangLeader;
			}

			// Token: 0x0400084A RID: 2122
			private const int AlternativeSolutionRelationChange = 5;

			// Token: 0x0400084B RID: 2123
			private const int AlternativeSolutionFailRelationChange = -5;

			// Token: 0x0400084C RID: 2124
			private const int AlternativeSolutionQuestGiverPowerChange = 10;

			// Token: 0x0400084D RID: 2125
			private const int AlternativeSolutionRivalGangLeaderPowerChange = -10;

			// Token: 0x0400084E RID: 2126
			private const int AlternativeSolutionFailQuestGiverPowerChange = -10;

			// Token: 0x0400084F RID: 2127
			private const int AlternativeSolutionFailSecurityChange = -10;

			// Token: 0x04000850 RID: 2128
			private const int AlternativeSolutionRivalGangLeaderRelationChange = -5;

			// Token: 0x04000851 RID: 2129
			private const int AlternativeSolutionMinimumTroopTier = 2;

			// Token: 0x04000852 RID: 2130
			private const int IssueDuration = 15;

			// Token: 0x04000853 RID: 2131
			private const int MinimumRequiredMenCount = 5;

			// Token: 0x04000854 RID: 2132
			private const int IssueQuestDuration = 8;

			// Token: 0x04000855 RID: 2133
			private const int MeleeSkillValueThreshold = 150;

			// Token: 0x04000856 RID: 2134
			private const int RoguerySkillValueThreshold = 120;

			// Token: 0x04000857 RID: 2135
			private const int PreparationDurationInDays = 2;
		}

		// Token: 0x020001C9 RID: 457
		public class RivalGangMovingInIssueQuest : QuestBase
		{
			// Token: 0x170001B5 RID: 437
			// (get) Token: 0x0600115F RID: 4447 RVA: 0x0007005C File Offset: 0x0006E25C
			private TextObject OnQuestStartedLogText
			{
				get
				{
					TextObject textObject = new TextObject("{=dav5rmDd}{QUEST_GIVER.LINK}, a gang leader from {SETTLEMENT} has told you about a rival that is trying to get a foothold in {?QUEST_GIVER.GENDER}her{?}his{\\?} town. {?QUEST_GIVER.GENDER}She{?}He{\\?} asked you to wait {DAY_COUNT} days so that the other gang lets its guard down.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					textObject.SetTextVariable("SETTLEMENT", this._questSettlement.EncyclopediaLinkWithName);
					textObject.SetTextVariable("DAY_COUNT", 2);
					return textObject;
				}
			}

			// Token: 0x170001B6 RID: 438
			// (get) Token: 0x06001160 RID: 4448 RVA: 0x000700B4 File Offset: 0x0006E2B4
			private TextObject OnQuestFailedWithRejectionLogText
			{
				get
				{
					TextObject textObject = new TextObject("{=aXMg9M7t}You decided to stay out of the fight. {?QUEST_GIVER.GENDER}She{?}He{\\?} will certainly lose to the rival gang without your help.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					return textObject;
				}
			}

			// Token: 0x170001B7 RID: 439
			// (get) Token: 0x06001161 RID: 4449 RVA: 0x000700E8 File Offset: 0x0006E2E8
			private TextObject OnQuestFailedWithBetrayalLogText
			{
				get
				{
					TextObject textObject = new TextObject("{=Rf0QqRIX}You have chosen to side with the rival gang leader, {RIVAL_GANG_LEADER.LINK}. {QUEST_GIVER.LINK} must be furious.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					StringHelpers.SetCharacterProperties("RIVAL_GANG_LEADER", this._rivalGangLeader.CharacterObject, textObject, false);
					return textObject;
				}
			}

			// Token: 0x170001B8 RID: 440
			// (get) Token: 0x06001162 RID: 4450 RVA: 0x00070134 File Offset: 0x0006E334
			private TextObject OnQuestFailedWithDefeatLogText
			{
				get
				{
					TextObject textObject = new TextObject("{=du3dpMaV}You were unable to defeat {RIVAL_GANG_LEADER.LINK}'s gang, and thus failed to fulfill your commitment to {QUEST_GIVER.LINK}.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					StringHelpers.SetCharacterProperties("RIVAL_GANG_LEADER", this._rivalGangLeader.CharacterObject, textObject, false);
					return textObject;
				}
			}

			// Token: 0x170001B9 RID: 441
			// (get) Token: 0x06001163 RID: 4451 RVA: 0x00070180 File Offset: 0x0006E380
			private TextObject OnQuestSucceededLogText
			{
				get
				{
					TextObject textObject = new TextObject("{=vpUl7xcy}You have defeated the rival gang and protected the interests of {QUEST_GIVER.LINK} in {SETTLEMENT}.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					textObject.SetTextVariable("SETTLEMENT", this._questSettlement.EncyclopediaLinkWithName);
					return textObject;
				}
			}

			// Token: 0x170001BA RID: 442
			// (get) Token: 0x06001164 RID: 4452 RVA: 0x000701CC File Offset: 0x0006E3CC
			private TextObject OnQuestPreperationsCompletedLogText
			{
				get
				{
					TextObject textObject = new TextObject("{=OIBiRTRP}{QUEST_GIVER.LINK} is waiting for you at {SETTLEMENT}.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					textObject.SetTextVariable("SETTLEMENT", this._questSettlement.EncyclopediaLinkWithName);
					return textObject;
				}
			}

			// Token: 0x170001BB RID: 443
			// (get) Token: 0x06001165 RID: 4453 RVA: 0x00070218 File Offset: 0x0006E418
			private TextObject OnQuestCancelledDueToWarLogText
			{
				get
				{
					TextObject textObject = new TextObject("{=vaUlAZba}Your clan is now at war with {QUEST_GIVER.LINK}. Your agreement with {QUEST_GIVER.LINK} was canceled.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					return textObject;
				}
			}

			// Token: 0x170001BC RID: 444
			// (get) Token: 0x06001166 RID: 4454 RVA: 0x0007024C File Offset: 0x0006E44C
			private TextObject PlayerDeclaredWarQuestLogText
			{
				get
				{
					TextObject textObject = new TextObject("{=bqeWVVEE}Your actions have started a war with {QUEST_GIVER.LINK}'s faction. {?QUEST_GIVER.GENDER}She{?}He{\\?} cancels your agreement and the quest is a failure.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					return textObject;
				}
			}

			// Token: 0x170001BD RID: 445
			// (get) Token: 0x06001167 RID: 4455 RVA: 0x00070280 File Offset: 0x0006E480
			private TextObject OnQuestCancelledDueToSiegeLogText
			{
				get
				{
					TextObject textObject = new TextObject("{=s1GWSE9Y}{QUEST_GIVER.LINK} cancels your plans due to the siege of {SETTLEMENT}. {?QUEST_GIVER.GENDER}She{?}He{\\?} has worse troubles than {?QUEST_GIVER.GENDER}her{?}his{\\?} quarrel with the rival gang.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					textObject.SetTextVariable("SETTLEMENT", this._questSettlement.EncyclopediaLinkWithName);
					return textObject;
				}
			}

			// Token: 0x170001BE RID: 446
			// (get) Token: 0x06001168 RID: 4456 RVA: 0x000702CC File Offset: 0x0006E4CC
			private TextObject PlayerStartedAlleyFightWithRivalGangLeader
			{
				get
				{
					TextObject textObject = new TextObject("{=OeKgpuAv}After your attack on the rival gang's alley, {QUEST_GIVER.LINK} decided to change {?QUEST_GIVER.GENDER}her{?}his{\\?} plans, and doesn't need your assistance anymore. Quest is canceled.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					return textObject;
				}
			}

			// Token: 0x170001BF RID: 447
			// (get) Token: 0x06001169 RID: 4457 RVA: 0x00070300 File Offset: 0x0006E500
			private TextObject PlayerStartedAlleyFightWithQuestgiver
			{
				get
				{
					TextObject textObject = new TextObject("{=VPGkIqlh}Your attack on {QUEST_GIVER.LINK}'s gang has angered {?QUEST_GIVER.GENDER}her{?}him{\\?} and {?QUEST_GIVER.GENDER}she{?}he{\\?} broke off the agreement that you had.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					return textObject;
				}
			}

			// Token: 0x170001C0 RID: 448
			// (get) Token: 0x0600116A RID: 4458 RVA: 0x00070334 File Offset: 0x0006E534
			private TextObject OwnerOfQuestSettlementIsPlayerClanLogText
			{
				get
				{
					TextObject textObject = new TextObject("{=KxEnNEoD}Your clan is now owner of the settlement. As the {?PLAYER.GENDER}lady{?}lord{\\?} of the settlement you cannot get involved in gang wars anymore. Your agreement with the {QUEST_GIVER.LINK} has canceled.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject, false);
					return textObject;
				}
			}

			// Token: 0x0600116B RID: 4459 RVA: 0x00070378 File Offset: 0x0006E578
			public RivalGangMovingInIssueQuest(string questId, Hero questGiver, Hero rivalGangLeader, int duration, int rewardGold, float issueDifficulty)
				: base(questId, questGiver, CampaignTime.DaysFromNow((float)duration), rewardGold)
			{
				this._rivalGangLeader = rivalGangLeader;
				this._rewardGold = rewardGold;
				this._issueDifficulty = issueDifficulty;
				this._timeoutDurationInDays = (float)duration;
				this._preparationCompletionTime = CampaignTime.DaysFromNow(2f);
				this._questTimeoutTime = CampaignTime.DaysFromNow(this._timeoutDurationInDays);
				this._sentTroops = new List<CharacterObject>();
				this._allPlayerTroops = new List<TroopRosterElement>();
				this.InitializeQuestSettlement();
				this.SetDialogs();
				base.InitializeQuestOnCreation();
			}

			// Token: 0x170001C1 RID: 449
			// (get) Token: 0x0600116C RID: 4460 RVA: 0x00070400 File Offset: 0x0006E600
			public override TextObject Title
			{
				get
				{
					TextObject textObject = new TextObject("{=vAjgn7yx}Rival Gang Moving in at {SETTLEMENT}", null);
					textObject.SetTextVariable("SETTLEMENT", this._questSettlement.Name);
					return textObject;
				}
			}

			// Token: 0x170001C2 RID: 450
			// (get) Token: 0x0600116D RID: 4461 RVA: 0x00070424 File Offset: 0x0006E624
			public override bool IsRemainingTimeHidden
			{
				get
				{
					return false;
				}
			}

			// Token: 0x0600116E RID: 4462 RVA: 0x00070428 File Offset: 0x0006E628
			protected override void InitializeQuestOnGameLoad()
			{
				this.InitializeQuestSettlement();
				this.SetDialogs();
				Campaign.Current.ConversationManager.AddDialogFlow(this.GetRivalGangLeaderDialogFlow(), this);
				Campaign.Current.ConversationManager.AddDialogFlow(this.GetQuestGiverPreparationCompletedDialogFlow(), this);
				MobileParty rivalGangLeaderParty = this._rivalGangLeaderParty;
				if (rivalGangLeaderParty != null)
				{
					rivalGangLeaderParty.SetPartyUsedByQuest(true);
				}
				this._sentTroops = new List<CharacterObject>();
				this._allPlayerTroops = new List<TroopRosterElement>();
			}

			// Token: 0x0600116F RID: 4463 RVA: 0x00070495 File Offset: 0x0006E695
			private void InitializeQuestSettlement()
			{
				this._questSettlement = base.QuestGiver.CurrentSettlement;
			}

			// Token: 0x06001170 RID: 4464 RVA: 0x000704A8 File Offset: 0x0006E6A8
			protected override void SetDialogs()
			{
				this.OfferDialogFlow = DialogFlow.CreateDialogFlow("issue_classic_quest_start", 100).NpcLine("{=Fwm0PwVb}Great. As I said we need minimum of {NUMBER} days,[ib:normal][if:convo_mocking_revenge] so they'll let their guard down. I will let you know when it's time. Remember, we wait for the dark of the night to strike.", null, null, null, null).Condition(delegate
				{
					MBTextManager.SetTextVariable("SETTLEMENT", this._questSettlement.EncyclopediaLinkWithName, false);
					MBTextManager.SetTextVariable("NUMBER", 2);
					return Hero.OneToOneConversationHero == base.QuestGiver;
				})
					.Consequence(new ConversationSentence.OnConsequenceDelegate(this.OnQuestAccepted))
					.CloseDialog();
				this.DiscussDialogFlow = DialogFlow.CreateDialogFlow("quest_discuss", 100).NpcLine("{=z43j3Tzq}I'm still gathering my men for the fight. I'll send a runner for you when the time comes.", null, null, null, null).Condition(delegate
				{
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, null, false);
					return Hero.OneToOneConversationHero == base.QuestGiver && !this._isFinalStage && !this._preparationsComplete;
				})
					.BeginPlayerOptions(null, false)
					.PlayerOption("{=4IHRAmnA}All right. I am waiting for your runner.", null, null, null)
					.NpcLine("{=xEs830bT}You'll know right away once the preparations are complete.[ib:closed][if:convo_mocking_teasing] Just don't leave town.", null, null, null, null)
					.CloseDialog()
					.PlayerOption("{=6g8qvD2M}I can't just hang on here forever. Be quick about it.", null, null, null)
					.NpcLine("{=lM7AscLo}I'm getting this together as quickly as I can.[ib:closed][if:convo_nervous]", null, null, null, null)
					.CloseDialog()
					.EndPlayerOptions()
					.CloseDialog();
			}

			// Token: 0x06001171 RID: 4465 RVA: 0x00070580 File Offset: 0x0006E780
			private DialogFlow GetRivalGangLeaderDialogFlow()
			{
				return DialogFlow.CreateDialogFlow("start", 125).NpcLine("{=IfeN8lYd}Coming to fight us, eh? Did {QUEST_GIVER.LINK} put you up to this?[ib:aggressive2][if:convo_confused_annoyed] Look, there's no need for bloodshed. This town is big enough for all of us. But... if bloodshed is what you want, we will be happy to provide.", null, null, null, null).Condition(delegate
				{
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, null, false);
					return Hero.OneToOneConversationHero == this._rivalGangLeaderHenchmanHero && this._isReadyToBeFinalized;
				})
					.NpcLine("{=WSJxl2Hu}What I want to say is... [if:convo_mocking_teasing]You don't need to be a part of this. My boss will double whatever {?QUEST_GIVER.GENDER}she{?}he{\\?} is paying you if you join us.", null, null, null, null)
					.BeginPlayerOptions(null, false)
					.PlayerOption("{=GPBja02V}I gave my word to {QUEST_GIVER.LINK}, and I won't be bought.", null, null, null)
					.Consequence(delegate
					{
						Campaign.Current.ConversationManager.ConversationEndOneShot += delegate()
						{
							CombatMissionWithDialogueController missionBehavior = Mission.Current.GetMissionBehavior<CombatMissionWithDialogueController>();
							if (missionBehavior == null)
							{
								return;
							}
							missionBehavior.StartFight(false);
						};
					})
					.NpcLine("{=OSgBicif}You will regret this![ib:warrior][if:convo_furious]", null, null, null, null)
					.CloseDialog()
					.PlayerOption("{=RB4uQpPV}You're going to pay me a lot then, {REWARD}{GOLD_ICON} to be exact. But at that price, I agree.", null, null, null)
					.Condition(delegate
					{
						MBTextManager.SetTextVariable("REWARD", this._rewardGold * 2);
						return true;
					})
					.Consequence(delegate
					{
						Campaign.Current.ConversationManager.ConversationEndOneShot += delegate()
						{
							this._hasBetrayedQuestGiver = true;
							CombatMissionWithDialogueController missionBehavior = Mission.Current.GetMissionBehavior<CombatMissionWithDialogueController>();
							if (missionBehavior == null)
							{
								return;
							}
							missionBehavior.StartFight(true);
						};
					})
					.NpcLine("{=5jW4FVDc}Welcome to our ranks then. [ib:warrior][if:convo_evil_smile]Let's kill those bastards!", null, null, null, null)
					.CloseDialog()
					.EndPlayerOptions()
					.CloseDialog();
			}

			// Token: 0x06001172 RID: 4466 RVA: 0x00070660 File Offset: 0x0006E860
			private DialogFlow GetQuestGiverPreparationCompletedDialogFlow()
			{
				return DialogFlow.CreateDialogFlow("start", 125).BeginNpcOptions(null, false).NpcOption(new TextObject("{=hM7LSuB1}Good to see you. But we still need to wait until after dusk. {HERO.LINK}'s men may be watching, so let's keep our distance from each other until night falls.", null), delegate()
				{
					StringHelpers.SetCharacterProperties("HERO", this._rivalGangLeader.CharacterObject, null, false);
					return Hero.OneToOneConversationHero == base.QuestGiver && !this._isFinalStage && this._preparationCompletionTime.IsPast && (!this._preparationsComplete || !CampaignTime.Now.IsNightTime);
				}, null, null, null, null)
					.CloseDialog()
					.NpcOption("{=JxNlB547}Are you ready for the fight?[ib:normal][if:convo_undecided_open]", () => Hero.OneToOneConversationHero == base.QuestGiver && this._preparationsComplete && !this._isFinalStage && CampaignTime.Now.IsNightTime, null, null, null, null)
					.EndNpcOptions()
					.BeginPlayerOptions(null, false)
					.PlayerOption("{=NzMX0s21}I am ready.", null, null, null)
					.Condition(() => !Hero.MainHero.IsWounded)
					.NpcLine("{=dNjepcKu}Let's finish this![ib:hip][if:convo_mocking_revenge]", null, null, null, null)
					.Consequence(delegate
					{
						Campaign.Current.ConversationManager.ConversationEndOneShot += this.rival_gang_start_fight_on_consequence;
					})
					.CloseDialog()
					.PlayerOption("{=B2Donbwz}I need more time.", null, null, null)
					.Condition(() => !Hero.MainHero.IsWounded)
					.NpcLine("{=advPT3WY}You'd better hurry up![ib:closed][if:convo_astonished]", null, null, null, null)
					.Consequence(delegate
					{
						Campaign.Current.ConversationManager.ConversationEndOneShot += this.rival_gang_need_more_time_on_consequence;
					})
					.CloseDialog()
					.PlayerOption("{=QaN26CZ5}My wounds are still fresh. I need some time to recover.", null, null, null)
					.Condition(() => Hero.MainHero.IsWounded)
					.NpcLine("{=s0jKaYo0}We must attack before the rival gang hears about our plan. You'd better hurry up![if:convo_astonished]", null, null, null, null)
					.CloseDialog()
					.EndPlayerOptions()
					.CloseDialog();
			}

			// Token: 0x06001173 RID: 4467 RVA: 0x000707C3 File Offset: 0x0006E9C3
			public override void OnHeroCanDieInfoIsRequested(Hero hero, KillCharacterAction.KillCharacterActionDetail causeOfDeath, ref bool result)
			{
				if (hero == base.QuestGiver || hero == this._rivalGangLeader)
				{
					result = false;
				}
			}

			// Token: 0x06001174 RID: 4468 RVA: 0x000707DA File Offset: 0x0006E9DA
			private void rival_gang_start_fight_on_consequence()
			{
				this._isFinalStage = true;
				if (Mission.Current != null)
				{
					Mission.Current.EndMission();
				}
				Campaign.Current.GameMenuManager.SetNextMenu("rival_gang_quest_before_fight");
			}

			// Token: 0x06001175 RID: 4469 RVA: 0x00070808 File Offset: 0x0006EA08
			private void rival_gang_need_more_time_on_consequence()
			{
				if (Campaign.Current.CurrentMenuContext.GameMenu.StringId == "rival_gang_quest_wait_duration_is_over")
				{
					Campaign.Current.GameMenuManager.SetNextMenu("town_wait_menus");
				}
			}

			// Token: 0x06001176 RID: 4470 RVA: 0x00070840 File Offset: 0x0006EA40
			private void AddQuestGiverGangLeaderOnSuccessDialogFlow()
			{
				Campaign.Current.ConversationManager.AddDialogFlow(DialogFlow.CreateDialogFlow("start", 125).NpcLine("{=zNPzh5jO}Ah! Now that was as good a fight as any I've had. Here, take this purse, It is all yours as {QUEST_GIVER.LINK} has promised.[ib:hip2][if:convo_huge_smile]", null, null, null, null).Condition(delegate
				{
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, null, false);
					return base.IsOngoing && Hero.OneToOneConversationHero == this._allyGangLeaderHenchmanHero;
				})
					.Consequence(delegate
					{
						Campaign.Current.ConversationManager.ConversationEndOneShot += this.OnQuestSucceeded;
					})
					.CloseDialog(), null);
			}

			// Token: 0x06001177 RID: 4471 RVA: 0x000708A0 File Offset: 0x0006EAA0
			private CharacterObject GetTroopTypeTemplateForDifficulty()
			{
				int difficultyRange = MBMath.ClampInt(MathF.Ceiling(this._issueDifficulty / 0.1f), 1, 10);
				CharacterObject characterObject;
				if (difficultyRange == 1)
				{
					characterObject = CharacterObject.All.FirstOrDefault((CharacterObject t) => t.StringId == "looter");
				}
				else if (difficultyRange == 10)
				{
					characterObject = CharacterObject.All.FirstOrDefault((CharacterObject t) => t.StringId == "mercenary_8");
				}
				else
				{
					characterObject = CharacterObject.All.FirstOrDefault((CharacterObject t) => t.StringId == "mercenary_" + (difficultyRange - 1));
				}
				if (characterObject == null)
				{
					Debug.FailedAssert("Can't find troop in rival gang leader quest", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\Issues\\RivalGangMovingInIssueBehavior.cs", "GetTroopTypeTemplateForDifficulty", 791);
					characterObject = CharacterObject.All.First((CharacterObject t) => t.IsBasicTroop && t.IsSoldier);
				}
				return characterObject;
			}

			// Token: 0x06001178 RID: 4472 RVA: 0x0007099C File Offset: 0x0006EB9C
			internal void StartAlleyBattle()
			{
				this.CreateRivalGangLeaderParty();
				this.CreateAllyGangLeaderParty();
				this.PreparePlayerParty();
				PlayerEncounter.RestartPlayerEncounter(this._rivalGangLeaderParty.Party, PartyBase.MainParty, false);
				PlayerEncounter.StartBattle();
				this._allyGangLeaderParty.MapEventSide = PlayerEncounter.Battle.GetMapEventSide(PlayerEncounter.Battle.PlayerSide);
				GameMenu.ActivateGameMenu("rival_gang_quest_after_fight");
				this._isReadyToBeFinalized = true;
				PlayerEncounter.StartCombatMissionWithDialogueInTownCenter(this._rivalGangLeaderHenchmanHero.CharacterObject);
			}

			// Token: 0x06001179 RID: 4473 RVA: 0x00070A18 File Offset: 0x0006EC18
			private void CreateRivalGangLeaderParty()
			{
				TextObject textObject = new TextObject("{=u4jhIFwG}{GANG_LEADER}'s Party", null);
				textObject.SetTextVariable("RIVAL_GANG_LEADER", this._rivalGangLeader.Name);
				textObject.SetTextVariable("GANG_LEADER", this._rivalGangLeader.Name);
				Hideout closestHideout = SettlementHelper.FindNearestHideoutToMobileParty(MobileParty.MainParty, MobileParty.NavigationType.All, (Settlement x) => x.IsActive);
				Clan clan = Clan.BanditFactions.FirstOrDefaultQ((Clan t) => t.Culture == closestHideout.Settlement.Culture);
				this._rivalGangLeaderParty = CustomPartyComponent.CreateCustomPartyWithTroopRoster(this._questSettlement.GatePosition, 1f, this._questSettlement, textObject, clan, TroopRoster.CreateDummyTroopRoster(), TroopRoster.CreateDummyTroopRoster(), null, "", "", 0f, false);
				this._rivalGangLeaderParty.SetPartyUsedByQuest(true);
				CharacterObject troopTypeTemplateForDifficulty = this.GetTroopTypeTemplateForDifficulty();
				this._rivalGangLeaderParty.MemberRoster.AddToCounts(troopTypeTemplateForDifficulty, 15, false, 0, 0, true, -1);
				CharacterObject @object = MBObjectManager.Instance.GetObject<CharacterObject>("gangster_3");
				this._rivalGangLeaderHenchmanHero = HeroCreator.CreateSpecialHero(@object, null, null, null, -1);
				TextObject textObject2 = new TextObject("{=zJqEdDiq}Henchman of {GANG_LEADER}", null);
				textObject2.SetTextVariable("GANG_LEADER", this._rivalGangLeader.Name);
				this._rivalGangLeaderHenchmanHero.SetName(textObject2, textObject2);
				this._rivalGangLeaderHenchmanHero.HiddenInEncyclopedia = true;
				this._rivalGangLeaderHenchmanHero.Culture = this._rivalGangLeader.Culture;
				this._rivalGangLeaderHenchmanHero.SetNewOccupation(Occupation.Special);
				this._rivalGangLeaderHenchmanHero.ChangeState(Hero.CharacterStates.Active);
				this._rivalGangLeaderParty.MemberRoster.AddToCounts(this._rivalGangLeaderHenchmanHero.CharacterObject, 1, false, 0, 0, true, -1);
				this._rivalGangLeaderParty.IgnoreByOtherPartiesTill(CampaignTime.Never);
				EnterSettlementAction.ApplyForParty(this._rivalGangLeaderParty, this._questSettlement);
			}

			// Token: 0x0600117A RID: 4474 RVA: 0x00070BEC File Offset: 0x0006EDEC
			private void CreateAllyGangLeaderParty()
			{
				TextObject textObject = new TextObject("{=u4jhIFwG}{GANG_LEADER}'s Party", null);
				textObject.SetTextVariable("GANG_LEADER", base.QuestGiver.Name);
				Hideout closestHideout = SettlementHelper.FindNearestHideoutToMobileParty(MobileParty.MainParty, MobileParty.NavigationType.All, (Settlement x) => x.IsActive);
				Clan clan = Clan.BanditFactions.FirstOrDefaultQ((Clan t) => t.Culture == closestHideout.Settlement.Culture);
				this._allyGangLeaderParty = CustomPartyComponent.CreateCustomPartyWithTroopRoster(this._questSettlement.GatePosition, 1f, this._questSettlement, textObject, clan, TroopRoster.CreateDummyTroopRoster(), TroopRoster.CreateDummyTroopRoster(), null, "", "", 0f, false);
				this._allyGangLeaderParty.SetPartyUsedByQuest(true);
				CharacterObject troopTypeTemplateForDifficulty = this.GetTroopTypeTemplateForDifficulty();
				this._allyGangLeaderParty.MemberRoster.AddToCounts(troopTypeTemplateForDifficulty, 20, false, 0, 0, true, -1);
				CharacterObject @object = MBObjectManager.Instance.GetObject<CharacterObject>("gangster_2");
				this._allyGangLeaderHenchmanHero = HeroCreator.CreateSpecialHero(@object, null, null, null, -1);
				TextObject textObject2 = new TextObject("{=zJqEdDiq}Henchman of {GANG_LEADER}", null);
				textObject2.SetTextVariable("GANG_LEADER", base.QuestGiver.Name);
				this._allyGangLeaderHenchmanHero.SetName(textObject2, textObject2);
				this._allyGangLeaderHenchmanHero.HiddenInEncyclopedia = true;
				this._allyGangLeaderHenchmanHero.Culture = base.QuestGiver.Culture;
				this._allyGangLeaderHenchmanHero.ChangeState(Hero.CharacterStates.Active);
				this._allyGangLeaderParty.MemberRoster.AddToCounts(this._allyGangLeaderHenchmanHero.CharacterObject, 1, false, 0, 0, true, -1);
				this._allyGangLeaderParty.IgnoreByOtherPartiesTill(CampaignTime.Never);
				EnterSettlementAction.ApplyForParty(this._allyGangLeaderParty, this._questSettlement);
			}

			// Token: 0x0600117B RID: 4475 RVA: 0x00070D9C File Offset: 0x0006EF9C
			private void PreparePlayerParty()
			{
				this._allPlayerTroops.Clear();
				foreach (TroopRosterElement troopRosterElement in PartyBase.MainParty.MemberRoster.GetTroopRoster())
				{
					if (!troopRosterElement.Character.IsPlayerCharacter)
					{
						this._allPlayerTroops.Add(troopRosterElement);
					}
				}
				this._partyEngineer = MobileParty.MainParty.GetRoleHolder(PartyRole.Engineer);
				this._partyScout = MobileParty.MainParty.GetRoleHolder(PartyRole.Scout);
				this._partyQuartermaster = MobileParty.MainParty.GetRoleHolder(PartyRole.Quartermaster);
				this._partySurgeon = MobileParty.MainParty.GetRoleHolder(PartyRole.Surgeon);
				PartyBase.MainParty.MemberRoster.RemoveIf((TroopRosterElement t) => !t.Character.IsPlayerCharacter);
				if (!this._allPlayerTroops.IsEmpty<TroopRosterElement>())
				{
					this._sentTroops.Clear();
					int num = 5;
					foreach (TroopRosterElement troopRosterElement2 in from t in this._allPlayerTroops
						orderby t.Character.Level descending
						select t)
					{
						if (num <= 0)
						{
							break;
						}
						int num2 = 0;
						while (num2 < troopRosterElement2.Number - troopRosterElement2.WoundedNumber && num > 0)
						{
							this._sentTroops.Add(troopRosterElement2.Character);
							num--;
							num2++;
						}
					}
					foreach (CharacterObject character in this._sentTroops)
					{
						PartyBase.MainParty.MemberRoster.AddToCounts(character, 1, false, 0, 0, true, -1);
					}
				}
			}

			// Token: 0x0600117C RID: 4476 RVA: 0x00070F94 File Offset: 0x0006F194
			internal void HandlePlayerEncounterResult(bool hasPlayerWon)
			{
				PlayerEncounter.Finish(false);
				EncounterManager.StartSettlementEncounter(MobileParty.MainParty, this._questSettlement);
				TroopRoster troopRoster = PartyBase.MainParty.MemberRoster.CloneRosterData();
				PartyBase.MainParty.MemberRoster.RemoveIf((TroopRosterElement t) => !t.Character.IsPlayerCharacter);
				using (List<TroopRosterElement>.Enumerator enumerator = this._allPlayerTroops.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						TroopRosterElement playerTroop = enumerator.Current;
						int num = troopRoster.FindIndexOfTroop(playerTroop.Character);
						int num2 = playerTroop.Number;
						int num3 = playerTroop.WoundedNumber;
						int num4 = playerTroop.Xp;
						if (num >= 0)
						{
							TroopRosterElement elementCopyAtIndex = troopRoster.GetElementCopyAtIndex(num);
							num2 -= this._sentTroops.Count((CharacterObject t) => t == playerTroop.Character) - elementCopyAtIndex.Number;
							num3 += elementCopyAtIndex.WoundedNumber;
							num4 += elementCopyAtIndex.Xp;
						}
						PartyBase.MainParty.MemberRoster.AddToCounts(playerTroop.Character, num2, false, num3, num4, true, -1);
					}
				}
				MobileParty.MainParty.SetPartyEngineer(this._partyEngineer);
				MobileParty.MainParty.SetPartyScout(this._partyScout);
				MobileParty.MainParty.SetPartyQuartermaster(this._partyQuartermaster);
				MobileParty.MainParty.SetPartySurgeon(this._partySurgeon);
				if (this._rivalGangLeader.PartyBelongedTo == this._rivalGangLeaderParty)
				{
					this._rivalGangLeaderParty.MemberRoster.AddToCounts(this._rivalGangLeader.CharacterObject, -1, false, 0, 0, true, -1);
				}
				if (hasPlayerWon)
				{
					if (!this._hasBetrayedQuestGiver)
					{
						this.AddQuestGiverGangLeaderOnSuccessDialogFlow();
						this.SpawnAllyHenchmanAfterMissionSuccess();
						PlayerEncounter.LocationEncounter.CreateAndOpenMissionController(LocationComplex.Current.GetLocationOfCharacter(this._allyGangLeaderHenchmanHero), null, this._allyGangLeaderHenchmanHero.CharacterObject, null);
						return;
					}
					this.OnBattleWonWithBetrayal();
					return;
				}
				else
				{
					if (!this._hasBetrayedQuestGiver)
					{
						this.OnQuestFailedWithDefeat();
						return;
					}
					this.OnBattleLostWithBetrayal();
					return;
				}
			}

			// Token: 0x0600117D RID: 4477 RVA: 0x000711C0 File Offset: 0x0006F3C0
			protected override void RegisterEvents()
			{
				CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, new Action<Hero, Hero, KillCharacterAction.KillCharacterActionDetail, bool>(this.OnHeroKilled));
				CampaignEvents.AlleyClearedByPlayer.AddNonSerializedListener(this, new Action<Alley>(this.OnAlleyClearedByPlayer));
				CampaignEvents.AlleyOccupiedByPlayer.AddNonSerializedListener(this, new Action<Alley, TroopRoster>(this.OnAlleyOccupiedByPlayer));
				CampaignEvents.WarDeclared.AddNonSerializedListener(this, new Action<IFaction, IFaction, DeclareWarAction.DeclareWarDetail>(this.OnWarDeclared));
				CampaignEvents.OnSiegeEventStartedEvent.AddNonSerializedListener(this, new Action<SiegeEvent>(this.OnSiegeEventStarted));
				CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, new Action<Clan, Kingdom, Kingdom, ChangeKingdomAction.ChangeKingdomActionDetail, bool>(this.OnClanChangedKingdom));
				CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, new Action<Settlement, bool, Hero, Hero, Hero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail>(this.OnSettlementOwnerChanged));
			}

			// Token: 0x0600117E RID: 4478 RVA: 0x00071270 File Offset: 0x0006F470
			private void SpawnAllyHenchmanAfterMissionSuccess()
			{
				Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(this._allyGangLeaderHenchmanHero.CharacterObject.Race, "_settlement");
				LocationCharacter locationCharacter = new LocationCharacter(new AgentData(new SimpleAgentOrigin(this._allyGangLeaderHenchmanHero.CharacterObject, -1, null, default(UniqueTroopDescriptor))).Monster(monsterWithSuffix), new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors), "npc_common", true, LocationCharacter.CharacterRelations.Neutral, null, true, false, null, false, false, true, null, false);
				LocationComplex.Current.GetLocationWithId("center").AddCharacter(locationCharacter);
			}

			// Token: 0x0600117F RID: 4479 RVA: 0x00071300 File Offset: 0x0006F500
			private void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
			{
				if (settlement == base.QuestGiver.CurrentSettlement && newOwner == Hero.MainHero)
				{
					base.AddLog(this.OwnerOfQuestSettlementIsPlayerClanLogText, false);
					base.QuestGiver.AddPower(-10f);
					ChangeRelationAction.ApplyPlayerRelation(base.QuestGiver, -5, true, true);
					base.CompleteQuestWithCancel(null);
				}
			}

			// Token: 0x06001180 RID: 4480 RVA: 0x00071357 File Offset: 0x0006F557
			public override void OnHeroCanHaveCampaignIssuesInfoIsRequested(Hero hero, ref bool result)
			{
				if (hero == this._rivalGangLeader)
				{
					result = false;
				}
			}

			// Token: 0x06001181 RID: 4481 RVA: 0x00071365 File Offset: 0x0006F565
			private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
			{
				if (base.QuestGiver.CurrentSettlement.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction))
				{
					base.CompleteQuestWithCancel(this.OnQuestCancelledDueToWarLogText);
				}
			}

			// Token: 0x06001182 RID: 4482 RVA: 0x00071394 File Offset: 0x0006F594
			private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
			{
				QuestHelper.CheckWarDeclarationAndFailOrCancelTheQuest(this, faction1, faction2, detail, this.PlayerDeclaredWarQuestLogText, this.OnQuestCancelledDueToWarLogText, false);
			}

			// Token: 0x06001183 RID: 4483 RVA: 0x000713AC File Offset: 0x0006F5AC
			private void OnSiegeEventStarted(SiegeEvent siegeEvent)
			{
				if (siegeEvent.BesiegedSettlement == this._questSettlement)
				{
					base.AddLog(this.OnQuestCancelledDueToSiegeLogText, false);
					base.CompleteQuestWithCancel(null);
				}
			}

			// Token: 0x06001184 RID: 4484 RVA: 0x000713D4 File Offset: 0x0006F5D4
			protected override void HourlyTick()
			{
				if (RivalGangMovingInIssueBehavior.Instance != null && RivalGangMovingInIssueBehavior.Instance.IsOngoing && (2f - RivalGangMovingInIssueBehavior.Instance._preparationCompletionTime.RemainingDaysFromNow) / 2f >= 1f && !this._preparationsComplete && CampaignTime.Now.IsNightTime)
				{
					this.OnGuestGiverPreparationsCompleted();
				}
			}

			// Token: 0x06001185 RID: 4485 RVA: 0x00071438 File Offset: 0x0006F638
			private void OnHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification = true)
			{
				if (victim == this._rivalGangLeader)
				{
					TextObject textObject = ((detail == KillCharacterAction.KillCharacterActionDetail.Lost) ? this.TargetHeroDisappearedLogText : this.TargetHeroDiedLogText);
					StringHelpers.SetCharacterProperties("QUEST_TARGET", this._rivalGangLeader.CharacterObject, textObject, false);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					base.AddLog(textObject, false);
					base.CompleteQuestWithCancel(null);
				}
			}

			// Token: 0x06001186 RID: 4486 RVA: 0x000714A1 File Offset: 0x0006F6A1
			private void OnPlayerAlleyFightEnd(Alley alley)
			{
				if (!this._isReadyToBeFinalized)
				{
					if (alley.Owner == this._rivalGangLeader)
					{
						this.OnPlayerAttackedRivalGangAlley();
						return;
					}
					if (alley.Owner == base.QuestGiver)
					{
						this.OnPlayerAttackedQuestGiverAlley();
					}
				}
			}

			// Token: 0x06001187 RID: 4487 RVA: 0x000714D4 File Offset: 0x0006F6D4
			private void OnAlleyClearedByPlayer(Alley alley)
			{
				this.OnPlayerAlleyFightEnd(alley);
			}

			// Token: 0x06001188 RID: 4488 RVA: 0x000714DD File Offset: 0x0006F6DD
			private void OnAlleyOccupiedByPlayer(Alley alley, TroopRoster troops)
			{
				this.OnPlayerAlleyFightEnd(alley);
			}

			// Token: 0x06001189 RID: 4489 RVA: 0x000714E6 File Offset: 0x0006F6E6
			private void OnPlayerAttackedRivalGangAlley()
			{
				base.AddLog(this.PlayerStartedAlleyFightWithRivalGangLeader, false);
				base.CompleteQuestWithCancel(null);
			}

			// Token: 0x0600118A RID: 4490 RVA: 0x00071500 File Offset: 0x0006F700
			private void OnPlayerAttackedQuestGiverAlley()
			{
				TraitLevelingHelper.OnIssueSolvedThroughQuest(base.QuestGiver, new Tuple<TraitObject, int>[]
				{
					new Tuple<TraitObject, int>(DefaultTraits.Honor, -150)
				});
				base.QuestGiver.AddPower(-10f);
				ChangeRelationAction.ApplyPlayerRelation(base.QuestGiver, -8, true, true);
				this._questSettlement.Town.Security += -10f;
				base.AddLog(this.PlayerStartedAlleyFightWithQuestgiver, false);
				base.CompleteQuestWithFail(null);
			}

			// Token: 0x0600118B RID: 4491 RVA: 0x00071580 File Offset: 0x0006F780
			protected override void OnTimedOut()
			{
				this.OnQuestFailedWithRejectionOrTimeout();
			}

			// Token: 0x0600118C RID: 4492 RVA: 0x00071588 File Offset: 0x0006F788
			private void OnGuestGiverPreparationsCompleted()
			{
				this._preparationsComplete = true;
				if (Settlement.CurrentSettlement != null && Settlement.CurrentSettlement == this._questSettlement && Campaign.Current.CurrentMenuContext != null && Campaign.Current.CurrentMenuContext.GameMenu.StringId == "town_wait_menus")
				{
					Campaign.Current.CurrentMenuContext.SwitchToMenu("rival_gang_quest_wait_duration_is_over");
				}
				TextObject textObject = new TextObject("{=DUKbtlNb}{QUEST_GIVER.LINK} has finally sent a messenger telling you it's time to meet {?QUEST_GIVER.GENDER}her{?}him{\\?} and join the fight.", null);
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
				base.AddLog(this.OnQuestPreperationsCompletedLogText, false);
				MBInformationManager.AddQuickInformation(textObject, 0, null, null, "");
			}

			// Token: 0x0600118D RID: 4493 RVA: 0x00071630 File Offset: 0x0006F830
			private void OnQuestAccepted()
			{
				base.StartQuest();
				this._onQuestStartedLog = base.AddLog(this.OnQuestStartedLogText, false);
				Campaign.Current.ConversationManager.AddDialogFlow(this.GetRivalGangLeaderDialogFlow(), this);
				Campaign.Current.ConversationManager.AddDialogFlow(this.GetQuestGiverPreparationCompletedDialogFlow(), this);
			}

			// Token: 0x0600118E RID: 4494 RVA: 0x00071684 File Offset: 0x0006F884
			private void OnQuestSucceeded()
			{
				this._onQuestSucceededLog = base.AddLog(this.OnQuestSucceededLogText, false);
				GainRenownAction.Apply(Hero.MainHero, 1f, false);
				GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, this._rewardGold, false);
				TraitLevelingHelper.OnIssueSolvedThroughQuest(base.QuestGiver, new Tuple<TraitObject, int>[]
				{
					new Tuple<TraitObject, int>(DefaultTraits.Honor, 50)
				});
				base.QuestGiver.AddPower(10f);
				this._rivalGangLeader.AddPower(-10f);
				this.RelationshipChangeWithQuestGiver = 5;
				ChangeRelationAction.ApplyPlayerRelation(this._rivalGangLeader, -5, true, true);
				GameMenu.ExitToLast();
				GameMenu.ActivateGameMenu("town");
				base.CompleteQuestWithSuccess();
			}

			// Token: 0x0600118F RID: 4495 RVA: 0x00071731 File Offset: 0x0006F931
			private void OnQuestFailedWithRejectionOrTimeout()
			{
				base.AddLog(this.OnQuestFailedWithRejectionLogText, false);
				TraitLevelingHelper.OnIssueFailed(base.QuestGiver, new Tuple<TraitObject, int>[]
				{
					new Tuple<TraitObject, int>(DefaultTraits.Honor, -20)
				});
				this.RelationshipChangeWithQuestGiver = -5;
				this.ApplyQuestFailConsequences();
			}

			// Token: 0x06001190 RID: 4496 RVA: 0x00071770 File Offset: 0x0006F970
			private void OnBattleWonWithBetrayal()
			{
				base.AddLog(this.OnQuestFailedWithBetrayalLogText, false);
				this.RelationshipChangeWithQuestGiver = -15;
				if (!this._rivalGangLeader.IsDead)
				{
					ChangeRelationAction.ApplyPlayerRelation(this._rivalGangLeader, 5, true, true);
				}
				GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, this._rewardGold * 2, false);
				TraitLevelingHelper.OnIssueSolvedThroughBetrayal(base.QuestGiver, new Tuple<TraitObject, int>[]
				{
					new Tuple<TraitObject, int>(DefaultTraits.Honor, -100)
				});
				this._rivalGangLeader.AddPower(10f);
				GameMenu.SwitchToMenu("town");
				this.ApplyQuestFailConsequences();
				base.CompleteQuestWithBetrayal(null);
			}

			// Token: 0x06001191 RID: 4497 RVA: 0x0007180C File Offset: 0x0006FA0C
			private void OnBattleLostWithBetrayal()
			{
				base.AddLog(this.OnQuestFailedWithBetrayalLogText, false);
				this.RelationshipChangeWithQuestGiver = -10;
				if (!this._rivalGangLeader.IsDead)
				{
					ChangeRelationAction.ApplyPlayerRelation(this._rivalGangLeader, -5, true, true);
				}
				this._rivalGangLeader.AddPower(-10f);
				TraitLevelingHelper.OnIssueSolvedThroughBetrayal(base.QuestGiver, new Tuple<TraitObject, int>[]
				{
					new Tuple<TraitObject, int>(DefaultTraits.Honor, -100)
				});
				GameMenu.SwitchToMenu("town");
				this.ApplyQuestFailConsequences();
				base.CompleteQuestWithBetrayal(null);
			}

			// Token: 0x06001192 RID: 4498 RVA: 0x00071892 File Offset: 0x0006FA92
			private void OnQuestFailedWithDefeat()
			{
				this.RelationshipChangeWithQuestGiver = -5;
				GameMenu.SwitchToMenu("town");
				base.AddLog(this.OnQuestFailedWithDefeatLogText, false);
				this.ApplyQuestFailConsequences();
				base.CompleteQuestWithFail(null);
			}

			// Token: 0x06001193 RID: 4499 RVA: 0x000718C4 File Offset: 0x0006FAC4
			private void ApplyQuestFailConsequences()
			{
				base.QuestGiver.AddPower(-10f);
				this._questSettlement.Town.Security += -10f;
				if (this._rivalGangLeaderParty != null && this._rivalGangLeaderParty.IsActive)
				{
					DestroyPartyAction.Apply(null, this._rivalGangLeaderParty);
				}
			}

			// Token: 0x06001194 RID: 4500 RVA: 0x00071920 File Offset: 0x0006FB20
			protected override void OnFinalize()
			{
				if (this._rivalGangLeaderParty != null && this._rivalGangLeaderParty.IsActive)
				{
					DestroyPartyAction.Apply(null, this._rivalGangLeaderParty);
				}
				if (this._allyGangLeaderParty != null && this._allyGangLeaderParty.IsActive)
				{
					DestroyPartyAction.Apply(null, this._allyGangLeaderParty);
				}
				if (this._allyGangLeaderHenchmanHero != null && this._allyGangLeaderHenchmanHero.IsAlive)
				{
					this._allyGangLeaderHenchmanHero.SetNewOccupation(Occupation.NotAssigned);
					KillCharacterAction.ApplyByRemove(this._allyGangLeaderHenchmanHero, false, true);
				}
				if (this._rivalGangLeaderHenchmanHero != null && this._rivalGangLeaderHenchmanHero.IsAlive)
				{
					this._rivalGangLeaderHenchmanHero.SetNewOccupation(Occupation.NotAssigned);
					KillCharacterAction.ApplyByRemove(this._rivalGangLeaderHenchmanHero, false, true);
				}
			}

			// Token: 0x06001195 RID: 4501 RVA: 0x000719CB File Offset: 0x0006FBCB
			internal static void AutoGeneratedStaticCollectObjectsRivalGangMovingInIssueQuest(object o, List<object> collectedObjects)
			{
				((RivalGangMovingInIssueBehavior.RivalGangMovingInIssueQuest)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
			}

			// Token: 0x06001196 RID: 4502 RVA: 0x000719DC File Offset: 0x0006FBDC
			protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
			{
				base.AutoGeneratedInstanceCollectObjects(collectedObjects);
				collectedObjects.Add(this._rivalGangLeader);
				collectedObjects.Add(this._rivalGangLeaderParty);
				CampaignTime.AutoGeneratedStaticCollectObjectsCampaignTime(this._preparationCompletionTime, collectedObjects);
				CampaignTime.AutoGeneratedStaticCollectObjectsCampaignTime(this._questTimeoutTime, collectedObjects);
			}

			// Token: 0x06001197 RID: 4503 RVA: 0x00071A2A File Offset: 0x0006FC2A
			internal static object AutoGeneratedGetMemberValue_rivalGangLeader(object o)
			{
				return ((RivalGangMovingInIssueBehavior.RivalGangMovingInIssueQuest)o)._rivalGangLeader;
			}

			// Token: 0x06001198 RID: 4504 RVA: 0x00071A37 File Offset: 0x0006FC37
			internal static object AutoGeneratedGetMemberValue_timeoutDurationInDays(object o)
			{
				return ((RivalGangMovingInIssueBehavior.RivalGangMovingInIssueQuest)o)._timeoutDurationInDays;
			}

			// Token: 0x06001199 RID: 4505 RVA: 0x00071A49 File Offset: 0x0006FC49
			internal static object AutoGeneratedGetMemberValue_isFinalStage(object o)
			{
				return ((RivalGangMovingInIssueBehavior.RivalGangMovingInIssueQuest)o)._isFinalStage;
			}

			// Token: 0x0600119A RID: 4506 RVA: 0x00071A5B File Offset: 0x0006FC5B
			internal static object AutoGeneratedGetMemberValue_isReadyToBeFinalized(object o)
			{
				return ((RivalGangMovingInIssueBehavior.RivalGangMovingInIssueQuest)o)._isReadyToBeFinalized;
			}

			// Token: 0x0600119B RID: 4507 RVA: 0x00071A6D File Offset: 0x0006FC6D
			internal static object AutoGeneratedGetMemberValue_hasBetrayedQuestGiver(object o)
			{
				return ((RivalGangMovingInIssueBehavior.RivalGangMovingInIssueQuest)o)._hasBetrayedQuestGiver;
			}

			// Token: 0x0600119C RID: 4508 RVA: 0x00071A7F File Offset: 0x0006FC7F
			internal static object AutoGeneratedGetMemberValue_rivalGangLeaderParty(object o)
			{
				return ((RivalGangMovingInIssueBehavior.RivalGangMovingInIssueQuest)o)._rivalGangLeaderParty;
			}

			// Token: 0x0600119D RID: 4509 RVA: 0x00071A8C File Offset: 0x0006FC8C
			internal static object AutoGeneratedGetMemberValue_preparationCompletionTime(object o)
			{
				return ((RivalGangMovingInIssueBehavior.RivalGangMovingInIssueQuest)o)._preparationCompletionTime;
			}

			// Token: 0x0600119E RID: 4510 RVA: 0x00071A9E File Offset: 0x0006FC9E
			internal static object AutoGeneratedGetMemberValue_questTimeoutTime(object o)
			{
				return ((RivalGangMovingInIssueBehavior.RivalGangMovingInIssueQuest)o)._questTimeoutTime;
			}

			// Token: 0x0600119F RID: 4511 RVA: 0x00071AB0 File Offset: 0x0006FCB0
			internal static object AutoGeneratedGetMemberValue_preparationsComplete(object o)
			{
				return ((RivalGangMovingInIssueBehavior.RivalGangMovingInIssueQuest)o)._preparationsComplete;
			}

			// Token: 0x060011A0 RID: 4512 RVA: 0x00071AC2 File Offset: 0x0006FCC2
			internal static object AutoGeneratedGetMemberValue_rewardGold(object o)
			{
				return ((RivalGangMovingInIssueBehavior.RivalGangMovingInIssueQuest)o)._rewardGold;
			}

			// Token: 0x060011A1 RID: 4513 RVA: 0x00071AD4 File Offset: 0x0006FCD4
			internal static object AutoGeneratedGetMemberValue_issueDifficulty(object o)
			{
				return ((RivalGangMovingInIssueBehavior.RivalGangMovingInIssueQuest)o)._issueDifficulty;
			}

			// Token: 0x04000859 RID: 2137
			private const int QuestGiverRelationChangeOnSuccess = 5;

			// Token: 0x0400085A RID: 2138
			private const int RivalGangLeaderRelationChangeOnSuccess = -5;

			// Token: 0x0400085B RID: 2139
			private const int QuestGiverNotablePowerChangeOnSuccess = 10;

			// Token: 0x0400085C RID: 2140
			private const int RivalGangLeaderPowerChangeOnSuccess = -10;

			// Token: 0x0400085D RID: 2141
			private const int RenownChangeOnSuccess = 1;

			// Token: 0x0400085E RID: 2142
			private const int QuestGiverRelationChangeOnFail = -5;

			// Token: 0x0400085F RID: 2143
			private const int QuestGiverRelationChangeOnTimedOut = -5;

			// Token: 0x04000860 RID: 2144
			private const int NotablePowerChangeOnFail = -10;

			// Token: 0x04000861 RID: 2145
			private const int TownSecurityChangeOnFail = -10;

			// Token: 0x04000862 RID: 2146
			private const int RivalGangLeaderRelationChangeOnSuccessfulBetrayal = 5;

			// Token: 0x04000863 RID: 2147
			private const int QuestGiverRelationChangeOnSuccessfulBetrayal = -15;

			// Token: 0x04000864 RID: 2148
			private const int RivalGangLeaderPowerChangeOnSuccessfulBetrayal = 10;

			// Token: 0x04000865 RID: 2149
			private const int QuestGiverRelationChangeOnFailedBetrayal = -10;

			// Token: 0x04000866 RID: 2150
			private const int PlayerAttackedQuestGiverHonorChange = -150;

			// Token: 0x04000867 RID: 2151
			private const int PlayerAttackedQuestGiverPowerChange = -10;

			// Token: 0x04000868 RID: 2152
			private const int NumberOfRegularEnemyTroops = 15;

			// Token: 0x04000869 RID: 2153
			private const int PlayerAttackedQuestGiverRelationChange = -8;

			// Token: 0x0400086A RID: 2154
			private const int PlayerAttackedQuestGiverSecurityChange = -10;

			// Token: 0x0400086B RID: 2155
			private const int NumberOfRegularAllyTroops = 20;

			// Token: 0x0400086C RID: 2156
			private const int MaxNumberOfPlayerOwnedTroops = 5;

			// Token: 0x0400086D RID: 2157
			private const string AllyGangLeaderHenchmanStringId = "gangster_2";

			// Token: 0x0400086E RID: 2158
			private const string RivalGangLeaderHenchmanStringId = "gangster_3";

			// Token: 0x0400086F RID: 2159
			private const int PreparationDurationInDays = 2;

			// Token: 0x04000870 RID: 2160
			[SaveableField(10)]
			internal readonly Hero _rivalGangLeader;

			// Token: 0x04000871 RID: 2161
			[SaveableField(20)]
			private MobileParty _rivalGangLeaderParty;

			// Token: 0x04000872 RID: 2162
			private Hero _rivalGangLeaderHenchmanHero;

			// Token: 0x04000873 RID: 2163
			[SaveableField(30)]
			private readonly CampaignTime _preparationCompletionTime;

			// Token: 0x04000874 RID: 2164
			private Hero _allyGangLeaderHenchmanHero;

			// Token: 0x04000875 RID: 2165
			private MobileParty _allyGangLeaderParty;

			// Token: 0x04000876 RID: 2166
			[SaveableField(40)]
			private readonly CampaignTime _questTimeoutTime;

			// Token: 0x04000877 RID: 2167
			[SaveableField(60)]
			internal readonly float _timeoutDurationInDays;

			// Token: 0x04000878 RID: 2168
			[SaveableField(70)]
			internal bool _isFinalStage;

			// Token: 0x04000879 RID: 2169
			[SaveableField(80)]
			internal bool _isReadyToBeFinalized;

			// Token: 0x0400087A RID: 2170
			[SaveableField(90)]
			internal bool _hasBetrayedQuestGiver;

			// Token: 0x0400087B RID: 2171
			private List<TroopRosterElement> _allPlayerTroops;

			// Token: 0x0400087C RID: 2172
			private List<CharacterObject> _sentTroops;

			// Token: 0x0400087D RID: 2173
			private Hero _partyEngineer;

			// Token: 0x0400087E RID: 2174
			private Hero _partyScout;

			// Token: 0x0400087F RID: 2175
			private Hero _partyQuartermaster;

			// Token: 0x04000880 RID: 2176
			private Hero _partySurgeon;

			// Token: 0x04000881 RID: 2177
			[SaveableField(110)]
			private bool _preparationsComplete;

			// Token: 0x04000882 RID: 2178
			[SaveableField(120)]
			private int _rewardGold;

			// Token: 0x04000883 RID: 2179
			[SaveableField(130)]
			private float _issueDifficulty;

			// Token: 0x04000884 RID: 2180
			private Settlement _questSettlement;

			// Token: 0x04000885 RID: 2181
			private JournalLog _onQuestStartedLog;

			// Token: 0x04000886 RID: 2182
			private JournalLog _onQuestSucceededLog;
		}
	}
}
