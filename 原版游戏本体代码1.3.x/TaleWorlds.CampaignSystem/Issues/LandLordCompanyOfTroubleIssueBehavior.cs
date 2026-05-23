using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Conversation.Persuasion;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace TaleWorlds.CampaignSystem.Issues
{
	// Token: 0x02000367 RID: 871
	public class LandLordCompanyOfTroubleIssueBehavior : CampaignBehaviorBase
	{
		// Token: 0x17000C3A RID: 3130
		// (get) Token: 0x0600330E RID: 13070 RVA: 0x000D1240 File Offset: 0x000CF440
		private static LandLordCompanyOfTroubleIssueBehavior.LandLordCompanyOfTroubleIssueQuest Instance
		{
			get
			{
				LandLordCompanyOfTroubleIssueBehavior campaignBehavior = Campaign.Current.GetCampaignBehavior<LandLordCompanyOfTroubleIssueBehavior>();
				if (campaignBehavior._cachedQuest != null && campaignBehavior._cachedQuest.IsOngoing)
				{
					return campaignBehavior._cachedQuest;
				}
				using (List<QuestBase>.Enumerator enumerator = Campaign.Current.QuestManager.Quests.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						LandLordCompanyOfTroubleIssueBehavior.LandLordCompanyOfTroubleIssueQuest cachedQuest;
						if ((cachedQuest = enumerator.Current as LandLordCompanyOfTroubleIssueBehavior.LandLordCompanyOfTroubleIssueQuest) != null)
						{
							campaignBehavior._cachedQuest = cachedQuest;
							return campaignBehavior._cachedQuest;
						}
					}
				}
				return null;
			}
		}

		// Token: 0x0600330F RID: 13071 RVA: 0x000D12D8 File Offset: 0x000CF4D8
		public override void RegisterEvents()
		{
			CampaignEvents.OnCheckForIssueEvent.AddNonSerializedListener(this, new Action<Hero>(this.OnCheckForIssue));
			CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnSessionLaunched));
		}

		// Token: 0x06003310 RID: 13072 RVA: 0x000D1308 File Offset: 0x000CF508
		private void OnSessionLaunched(CampaignGameStarter gameStarter)
		{
			gameStarter.AddGameMenu("company_of_trouble_menu", "", new OnInitDelegate(this.company_of_trouble_menu_on_init), GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
		}

		// Token: 0x06003311 RID: 13073 RVA: 0x000D132C File Offset: 0x000CF52C
		private void company_of_trouble_menu_on_init(MenuCallbackArgs args)
		{
			if (LandLordCompanyOfTroubleIssueBehavior.Instance != null)
			{
				if (LandLordCompanyOfTroubleIssueBehavior.Instance._checkForBattleResults)
				{
					bool flag = PlayerEncounter.Battle.WinningSide == PlayerEncounter.Battle.PlayerSide;
					PlayerEncounter.Finish(true);
					if (LandLordCompanyOfTroubleIssueBehavior.Instance._companyOfTroubleParty != null && LandLordCompanyOfTroubleIssueBehavior.Instance._companyOfTroubleParty.IsActive)
					{
						DestroyPartyAction.Apply(null, LandLordCompanyOfTroubleIssueBehavior.Instance._companyOfTroubleParty);
					}
					LandLordCompanyOfTroubleIssueBehavior.Instance._checkForBattleResults = false;
					if (flag)
					{
						LandLordCompanyOfTroubleIssueBehavior.Instance.QuestSuccessWithPlayerDefeatedCompany();
						return;
					}
					LandLordCompanyOfTroubleIssueBehavior.Instance.QuestFailWithPlayerDefeatedAgainstCompany();
					return;
				}
				else
				{
					if (LandLordCompanyOfTroubleIssueBehavior.Instance._triggerCompanyOfTroubleConversation)
					{
						CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty, false, false, false, false, false, false), new ConversationCharacterData(LandLordCompanyOfTroubleIssueBehavior.Instance._troubleCharacterObject, PartyBase.MainParty, false, false, false, false, false, false));
						LandLordCompanyOfTroubleIssueBehavior.Instance._triggerCompanyOfTroubleConversation = false;
						return;
					}
					if (LandLordCompanyOfTroubleIssueBehavior.Instance._battleWillStart)
					{
						PlayerEncounter.Start();
						PlayerEncounter.Current.SetupFields(PartyBase.MainParty, LandLordCompanyOfTroubleIssueBehavior.Instance._companyOfTroubleParty.Party);
						PlayerEncounter.StartBattle();
						IMapScene mapSceneWrapper = Campaign.Current.MapSceneWrapper;
						CampaignVec2 position = MobileParty.MainParty.Position;
						MapPatchData mapPatchAtPosition = mapSceneWrapper.GetMapPatchAtPosition(position);
						CampaignMission.OpenBattleMission(Campaign.Current.Models.SceneModel.GetBattleSceneForMapPatch(mapPatchAtPosition, PlayerEncounter.IsNavalEncounter()), false);
						LandLordCompanyOfTroubleIssueBehavior.Instance._battleWillStart = false;
						LandLordCompanyOfTroubleIssueBehavior.Instance._checkForBattleResults = true;
						return;
					}
					if (LandLordCompanyOfTroubleIssueBehavior.Instance._companyLeftQuestWillFail)
					{
						LandLordCompanyOfTroubleIssueBehavior.Instance.CompanyLeftQuestFail();
					}
				}
			}
		}

		// Token: 0x06003312 RID: 13074 RVA: 0x000D14A7 File Offset: 0x000CF6A7
		[GameMenuInitializationHandler("company_of_trouble_menu")]
		public static void company_of_trouble_menu_game_menu_on_init_background(MenuCallbackArgs args)
		{
			args.MenuContext.SetBackgroundMeshName("wait_ambush");
		}

		// Token: 0x06003313 RID: 13075 RVA: 0x000D14BC File Offset: 0x000CF6BC
		public void OnCheckForIssue(Hero hero)
		{
			if (hero.IsLord && hero.Clan != Clan.PlayerClan && hero.PartyBelongedTo != null && !hero.IsMinorFactionHero && hero.GetTraitLevel(DefaultTraits.Mercy) <= 0)
			{
				Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(new PotentialIssueData.StartIssueDelegate(this.OnStartIssue), typeof(LandLordCompanyOfTroubleIssueBehavior.LandLordCompanyOfTroubleIssue), IssueBase.IssueFrequency.Rare, null));
				return;
			}
			Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(typeof(LandLordCompanyOfTroubleIssueBehavior.LandLordCompanyOfTroubleIssue), IssueBase.IssueFrequency.Rare));
		}

		// Token: 0x06003314 RID: 13076 RVA: 0x000D154A File Offset: 0x000CF74A
		private IssueBase OnStartIssue(in PotentialIssueData pid, Hero issueOwner)
		{
			return new LandLordCompanyOfTroubleIssueBehavior.LandLordCompanyOfTroubleIssue(issueOwner);
		}

		// Token: 0x06003315 RID: 13077 RVA: 0x000D1552 File Offset: 0x000CF752
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x04000E93 RID: 3731
		private const IssueBase.IssueFrequency LandLordCompanyOfTroubleIssueFrequency = IssueBase.IssueFrequency.Rare;

		// Token: 0x04000E94 RID: 3732
		private LandLordCompanyOfTroubleIssueBehavior.LandLordCompanyOfTroubleIssueQuest _cachedQuest;

		// Token: 0x04000E95 RID: 3733
		private const int IssueDuration = 25;

		// Token: 0x02000714 RID: 1812
		public class LandLordCompanyOfTroubleIssue : IssueBase
		{
			// Token: 0x060058E3 RID: 22755 RVA: 0x0019DBD5 File Offset: 0x0019BDD5
			internal static void AutoGeneratedStaticCollectObjectsLandLordCompanyOfTroubleIssue(object o, List<object> collectedObjects)
			{
				((LandLordCompanyOfTroubleIssueBehavior.LandLordCompanyOfTroubleIssue)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
			}

			// Token: 0x060058E4 RID: 22756 RVA: 0x0019DBE3 File Offset: 0x0019BDE3
			protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
			{
				base.AutoGeneratedInstanceCollectObjects(collectedObjects);
			}

			// Token: 0x1700115C RID: 4444
			// (get) Token: 0x060058E5 RID: 22757 RVA: 0x0019DBEC File Offset: 0x0019BDEC
			private int CompanyTroopCount
			{
				get
				{
					return 5 + (int)(base.IssueDifficultyMultiplier * 30f);
				}
			}

			// Token: 0x1700115D RID: 4445
			// (get) Token: 0x060058E6 RID: 22758 RVA: 0x0019DBFD File Offset: 0x0019BDFD
			public override bool IsThereAlternativeSolution
			{
				get
				{
					return false;
				}
			}

			// Token: 0x1700115E RID: 4446
			// (get) Token: 0x060058E7 RID: 22759 RVA: 0x0019DC00 File Offset: 0x0019BE00
			public override bool IsThereLordSolution
			{
				get
				{
					return false;
				}
			}

			// Token: 0x1700115F RID: 4447
			// (get) Token: 0x060058E8 RID: 22760 RVA: 0x0019DC03 File Offset: 0x0019BE03
			public override TextObject IssueBriefByIssueGiver
			{
				get
				{
					return new TextObject("{=wrpsJM2u}Yes... I hired a band of mercenaries for a campaign some time back. But... normally mercenaries have their own peculiar kind of honor. You pay them, they fight for you, you don't, they go somewhere else. But these ones have made it pretty clear that if I don't keep renewing the contract, they'll turn bandit. I can't afford that right now.[if:convo_thinking][ib:closed]", null);
				}
			}

			// Token: 0x17001160 RID: 4448
			// (get) Token: 0x060058E9 RID: 22761 RVA: 0x0019DC10 File Offset: 0x0019BE10
			public override TextObject IssueAcceptByPlayer
			{
				get
				{
					return new TextObject("{=VlbCFDWu}What do you want from me?", null);
				}
			}

			// Token: 0x17001161 RID: 4449
			// (get) Token: 0x060058EA RID: 22762 RVA: 0x0019DC1D File Offset: 0x0019BE1D
			public override TextObject IssueQuestSolutionExplanationByIssueGiver
			{
				get
				{
					return new TextObject("{=wxDbPiNH}Well, you have the reputation of being able to manage ruffians. Maybe you can take them off my hands, find some other lord who has more need of them and more denars to pay them. I've paid their contract for a few months. I can give you a small reward and if you can find a buyer, you can transfer the rest of the contract to him and pocket the down payment.[if:convo_innocent_smile]", null);
				}
			}

			// Token: 0x17001162 RID: 4450
			// (get) Token: 0x060058EB RID: 22763 RVA: 0x0019DC2A File Offset: 0x0019BE2A
			public override TextObject IssueQuestSolutionAcceptByPlayer
			{
				get
				{
					return new TextObject("{=6bvJSIqh}Yes. I can find a new lord to take them on.", null);
				}
			}

			// Token: 0x17001163 RID: 4451
			// (get) Token: 0x060058EC RID: 22764 RVA: 0x0019DC37 File Offset: 0x0019BE37
			public override TextObject Title
			{
				get
				{
					return new TextObject("{=PV7RHgUl}Company of Trouble", null);
				}
			}

			// Token: 0x17001164 RID: 4452
			// (get) Token: 0x060058ED RID: 22765 RVA: 0x0019DC44 File Offset: 0x0019BE44
			public override TextObject Description
			{
				get
				{
					TextObject textObject = new TextObject("{=zw7a9eIt}{ISSUE_GIVER.NAME} wants you to take {?ISSUE_GIVER.GENDER}her{?}his{\\?} mercenaries and transfer them to another lord before they cause any trouble.", null);
					StringHelpers.SetCharacterProperties("ISSUE_GIVER", base.IssueOwner.CharacterObject, textObject, false);
					return textObject;
				}
			}

			// Token: 0x17001165 RID: 4453
			// (get) Token: 0x060058EE RID: 22766 RVA: 0x0019DC78 File Offset: 0x0019BE78
			public override TextObject IssueAsRumorInSettlement
			{
				get
				{
					TextObject textObject = new TextObject("{=I022Z9Ub}Heh. {QUEST_GIVER.NAME} got in deeper than {?QUEST_GIVER.GENDER}she{?}he{\\?} could handle with those mercenaries.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.IssueOwner.CharacterObject, textObject, false);
					return textObject;
				}
			}

			// Token: 0x060058EF RID: 22767 RVA: 0x0019DCAA File Offset: 0x0019BEAA
			public LandLordCompanyOfTroubleIssue(Hero issueOwner)
				: base(issueOwner, CampaignTime.DaysFromNow(25f))
			{
			}

			// Token: 0x060058F0 RID: 22768 RVA: 0x0019DCBD File Offset: 0x0019BEBD
			protected override float GetIssueEffectAmountInternal(IssueEffect issueEffect)
			{
				if (issueEffect == DefaultIssueEffects.ClanInfluence)
				{
					return -0.1f;
				}
				return 0f;
			}

			// Token: 0x060058F1 RID: 22769 RVA: 0x0019DCD2 File Offset: 0x0019BED2
			protected override void OnGameLoad()
			{
			}

			// Token: 0x060058F2 RID: 22770 RVA: 0x0019DCD4 File Offset: 0x0019BED4
			protected override void HourlyTick()
			{
			}

			// Token: 0x060058F3 RID: 22771 RVA: 0x0019DCD6 File Offset: 0x0019BED6
			protected override QuestBase GenerateIssueQuest(string questId)
			{
				return new LandLordCompanyOfTroubleIssueBehavior.LandLordCompanyOfTroubleIssueQuest(questId, base.IssueOwner, CampaignTime.Never, this.CompanyTroopCount);
			}

			// Token: 0x060058F4 RID: 22772 RVA: 0x0019DCEF File Offset: 0x0019BEEF
			public override IssueBase.IssueFrequency GetFrequency()
			{
				return IssueBase.IssueFrequency.Rare;
			}

			// Token: 0x060058F5 RID: 22773 RVA: 0x0019DCF4 File Offset: 0x0019BEF4
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
				if (Clan.PlayerClan.Tier < 1)
				{
					flag |= IssueBase.PreconditionFlags.ClanTier;
				}
				if (MobileParty.MainParty.MemberRoster.TotalManCount < this.CompanyTroopCount)
				{
					flag |= IssueBase.PreconditionFlags.NotEnoughTroops;
				}
				if (MobileParty.MainParty.MemberRoster.TotalManCount + this.CompanyTroopCount > PartyBase.MainParty.PartySizeLimit)
				{
					flag |= IssueBase.PreconditionFlags.PartySizeLimit;
				}
				if (issueGiver.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction))
				{
					flag |= IssueBase.PreconditionFlags.AtWar;
				}
				return flag == IssueBase.PreconditionFlags.None;
			}

			// Token: 0x060058F6 RID: 22774 RVA: 0x0019DDA8 File Offset: 0x0019BFA8
			public override bool IssueStayAliveConditions()
			{
				return base.IssueOwner.Clan != Clan.PlayerClan;
			}

			// Token: 0x060058F7 RID: 22775 RVA: 0x0019DDBF File Offset: 0x0019BFBF
			protected override void CompleteIssueWithTimedOutConsequences()
			{
			}
		}

		// Token: 0x02000715 RID: 1813
		public class LandLordCompanyOfTroubleIssueQuest : QuestBase
		{
			// Token: 0x060058F8 RID: 22776 RVA: 0x0019DDC1 File Offset: 0x0019BFC1
			internal static void AutoGeneratedStaticCollectObjectsLandLordCompanyOfTroubleIssueQuest(object o, List<object> collectedObjects)
			{
				((LandLordCompanyOfTroubleIssueBehavior.LandLordCompanyOfTroubleIssueQuest)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
			}

			// Token: 0x060058F9 RID: 22777 RVA: 0x0019DDCF File Offset: 0x0019BFCF
			protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
			{
				base.AutoGeneratedInstanceCollectObjects(collectedObjects);
				collectedObjects.Add(this._companyOfTroubleParty);
				collectedObjects.Add(this._persuationTriedHeroesList);
			}

			// Token: 0x060058FA RID: 22778 RVA: 0x0019DDF0 File Offset: 0x0019BFF0
			internal static object AutoGeneratedGetMemberValue_companyOfTroubleParty(object o)
			{
				return ((LandLordCompanyOfTroubleIssueBehavior.LandLordCompanyOfTroubleIssueQuest)o)._companyOfTroubleParty;
			}

			// Token: 0x060058FB RID: 22779 RVA: 0x0019DDFD File Offset: 0x0019BFFD
			internal static object AutoGeneratedGetMemberValue_battleWillStart(object o)
			{
				return ((LandLordCompanyOfTroubleIssueBehavior.LandLordCompanyOfTroubleIssueQuest)o)._battleWillStart;
			}

			// Token: 0x060058FC RID: 22780 RVA: 0x0019DE0F File Offset: 0x0019C00F
			internal static object AutoGeneratedGetMemberValue_triggerCompanyOfTroubleConversation(object o)
			{
				return ((LandLordCompanyOfTroubleIssueBehavior.LandLordCompanyOfTroubleIssueQuest)o)._triggerCompanyOfTroubleConversation;
			}

			// Token: 0x060058FD RID: 22781 RVA: 0x0019DE21 File Offset: 0x0019C021
			internal static object AutoGeneratedGetMemberValue_thieveryCount(object o)
			{
				return ((LandLordCompanyOfTroubleIssueBehavior.LandLordCompanyOfTroubleIssueQuest)o)._thieveryCount;
			}

			// Token: 0x060058FE RID: 22782 RVA: 0x0019DE33 File Offset: 0x0019C033
			internal static object AutoGeneratedGetMemberValue_demandGold(object o)
			{
				return ((LandLordCompanyOfTroubleIssueBehavior.LandLordCompanyOfTroubleIssueQuest)o)._demandGold;
			}

			// Token: 0x060058FF RID: 22783 RVA: 0x0019DE45 File Offset: 0x0019C045
			internal static object AutoGeneratedGetMemberValue_persuationTriedHeroesList(object o)
			{
				return ((LandLordCompanyOfTroubleIssueBehavior.LandLordCompanyOfTroubleIssueQuest)o)._persuationTriedHeroesList;
			}

			// Token: 0x17001166 RID: 4454
			// (get) Token: 0x06005900 RID: 22784 RVA: 0x0019DE52 File Offset: 0x0019C052
			public override bool IsRemainingTimeHidden
			{
				get
				{
					return true;
				}
			}

			// Token: 0x17001167 RID: 4455
			// (get) Token: 0x06005901 RID: 22785 RVA: 0x0019DE55 File Offset: 0x0019C055
			public override TextObject Title
			{
				get
				{
					return new TextObject("{=PV7RHgUl}Company of Trouble", null);
				}
			}

			// Token: 0x17001168 RID: 4456
			// (get) Token: 0x06005902 RID: 22786 RVA: 0x0019DE64 File Offset: 0x0019C064
			private TextObject PlayerStartsQuestLogText
			{
				get
				{
					TextObject textObject = new TextObject("{=8nS3QgD7}{QUEST_GIVER.LINK} is a {?QUEST_GIVER.GENDER}lady{?}lord{\\?} who told you that {?QUEST_GIVER.GENDER}she{?}he{\\?} wants to sell {?QUEST_GIVER.GENDER}her{?}his{\\?} mercenaries to another lord's service. {?QUEST_GIVER.GENDER}She{?}He{\\?} asked you sell them for {?QUEST_GIVER.GENDER}her{?}him{\\?} without causing any trouble.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					return textObject;
				}
			}

			// Token: 0x17001169 RID: 4457
			// (get) Token: 0x06005903 RID: 22787 RVA: 0x0019DE98 File Offset: 0x0019C098
			private TextObject QuestSuccessPlayerSoldCompany
			{
				get
				{
					TextObject textObject = new TextObject("{=34MdCd6u}You have sold the mercenaries to another lord as you promised. {QUEST_GIVER.LINK} is grateful and sends {?QUEST_GIVER.GENDER}her{?}his{\\?} regards.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					return textObject;
				}
			}

			// Token: 0x1700116A RID: 4458
			// (get) Token: 0x06005904 RID: 22788 RVA: 0x0019DECC File Offset: 0x0019C0CC
			private TextObject AllCompanyDiedLogText
			{
				get
				{
					TextObject textObject = new TextObject("{=RrTAX7QE}You got the troublesome mercenaries killed off. You get no extra money for the contract, but you did get rid of them as you promised. {QUEST_GIVER.LINK} is grateful and sends {?QUEST_GIVER.GENDER}her{?}his{\\?} regards.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					return textObject;
				}
			}

			// Token: 0x1700116B RID: 4459
			// (get) Token: 0x06005905 RID: 22789 RVA: 0x0019DEFE File Offset: 0x0019C0FE
			private TextObject PlayerDefeatedAgainstCompany
			{
				get
				{
					return new TextObject("{=7naLQmq1}You have lost the battle against the mercenaries. You have failed to get rid of them as you promised. Now they've turned bandit and are starting to plunder the countryside", null);
				}
			}

			// Token: 0x1700116C RID: 4460
			// (get) Token: 0x06005906 RID: 22790 RVA: 0x0019DF0B File Offset: 0x0019C10B
			private TextObject QuestFailCompanyLeft
			{
				get
				{
					return new TextObject("{=k9SksaXg}The mercenaries left your party, as you failed to get rid of them as you promised. Now the mercenaries have turned bandit and start to plunder countryside.", null);
				}
			}

			// Token: 0x1700116D RID: 4461
			// (get) Token: 0x06005907 RID: 22791 RVA: 0x0019DF18 File Offset: 0x0019C118
			private TextObject QuestCanceledWarDeclared
			{
				get
				{
					TextObject textObject = new TextObject("{=ItueKmqd}Your clan is now at war with the {QUEST_GIVER_SETTLEMENT_FACTION}. You contract with {QUEST_GIVER.LINK} is canceled.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					textObject.SetTextVariable("QUEST_GIVER_SETTLEMENT_FACTION", base.QuestGiver.MapFaction.InformalName);
					return textObject;
				}
			}

			// Token: 0x1700116E RID: 4462
			// (get) Token: 0x06005908 RID: 22792 RVA: 0x0019DF68 File Offset: 0x0019C168
			private TextObject PlayerDeclaredWarQuestLogText
			{
				get
				{
					TextObject textObject = new TextObject("{=bqeWVVEE}Your actions have started a war with {QUEST_GIVER.LINK}'s faction. {?QUEST_GIVER.GENDER}She{?}He{\\?} cancels your agreement and the quest is a failure.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					return textObject;
				}
			}

			// Token: 0x06005909 RID: 22793 RVA: 0x0019DF9C File Offset: 0x0019C19C
			public LandLordCompanyOfTroubleIssueQuest(string questId, Hero questGiver, CampaignTime duration, int companyTroopCount)
				: base(questId, questGiver, duration, 500)
			{
				this._troubleCharacterObject = MBObjectManager.Instance.GetObject<CharacterObject>("company_of_trouble_character");
				this._persuationTriedHeroesList = new List<Hero>();
				this._troubleCharacterObject.SetTransferableInPartyScreen(false);
				this._troubleCharacterObject.SetTransferableInHideouts(false);
				this._companyTroopCount = companyTroopCount;
				this._tasks = new PersuasionTask[3];
				this._battleWillStart = false;
				this._thieveryCount = 0;
				this._demandGold = 0;
				this.SetDialogs();
				base.InitializeQuestOnCreation();
			}

			// Token: 0x0600590A RID: 22794 RVA: 0x0019E024 File Offset: 0x0019C224
			protected override void InitializeQuestOnGameLoad()
			{
				this._troubleCharacterObject = MBObjectManager.Instance.GetObject<CharacterObject>("company_of_trouble_character");
				this._troubleCharacterObject.SetTransferableInPartyScreen(false);
				this._troubleCharacterObject.SetTransferableInHideouts(false);
				this._tasks = new PersuasionTask[3];
				this.UpdateCompanyTroopCount();
				this.SetDialogs();
			}

			// Token: 0x0600590B RID: 22795 RVA: 0x0019E078 File Offset: 0x0019C278
			protected override void SetDialogs()
			{
				this.OfferDialogFlow = DialogFlow.CreateDialogFlow("issue_classic_quest_start", 100).NpcLine(new TextObject("{=T6d7wtJX}Very well. I'll tell them to join your party. Good luck.[if:convo_mocking_aristocratic][ib:hip]", null), null, null, null, null).Condition(() => Hero.OneToOneConversationHero == base.QuestGiver)
					.Consequence(new ConversationSentence.OnConsequenceDelegate(this.QuestAcceptedConsequences))
					.CloseDialog();
				this.DiscussDialogFlow = DialogFlow.CreateDialogFlow("quest_discuss", 100).NpcLine(new TextObject("{=bWpLYiEg}Did you ever find a way to handle those mercenaries?[if:convo_astonished]", null), null, null, null, null).Condition(() => Hero.OneToOneConversationHero == base.QuestGiver)
					.Consequence(delegate
					{
						Campaign.Current.ConversationManager.ConversationEndOneShot += MapEventHelper.OnConversationEnd;
					})
					.BeginPlayerOptions(null, false)
					.PlayerOption(new TextObject("{=XzK4niIb}I'll find an employer soon.", null), null, null, null)
					.NpcLine(new TextObject("{=rOBRabQz}Good. I'm waiting for your good news.[if:convo_mocking_aristocratic]", null), null, null, null, null)
					.CloseDialog()
					.PlayerOption(new TextObject("{=Zb3EdxDT}That kind of lord is hard to find.", null), null, null, null)
					.NpcLine(new TextObject("{=yOfrb9Lu}Don't wait too long. These are dangerous men. Be careful.[if:convo_nonchalant]", null), null, null, null, null)
					.CloseDialog()
					.EndPlayerOptions()
					.CloseDialog();
				Campaign.Current.ConversationManager.AddDialogFlow(this.GetCompanyDialogFlow(), this);
				Campaign.Current.ConversationManager.AddDialogFlow(this.GetOtherLordsDialogFlow(), this);
			}

			// Token: 0x0600590C RID: 22796 RVA: 0x0019E1C4 File Offset: 0x0019C3C4
			private DialogFlow GetOtherLordsDialogFlow()
			{
				DialogFlow dialogFlow = DialogFlow.CreateDialogFlow("hero_main_options", 700).BeginPlayerOptions(null, false).PlayerOption(new TextObject("{=2E7s4L9R}Do you need mercenaries? I have a contract that I can transfer to you for {DEMAND_GOLD} denars.", null), null, null, null)
					.Condition(new ConversationSentence.OnConditionDelegate(this.PersuasionDialogForLordGeneralCondition))
					.BeginNpcOptions(null, false)
					.NpcOption(new TextObject("{=ZR4RJdYS}Hmm, that sounds interesting...[if:convo_thinking]", null), new ConversationSentence.OnConditionDelegate(this.PersuasionDialogSpecialCondition), null, null, null, null)
					.GotoDialogState("company_of_trouble_persuasion")
					.NpcOption(new TextObject("{=pmrjUNEz}As it happens, I already have a mercenary contract that I wish to sell. So, no thank you.[if:convo_calm_friendly]", null), new ConversationSentence.OnConditionDelegate(this.HasSameIssue), null, null, null, null)
					.GotoDialogState("hero_main_options")
					.NpcOption(new TextObject("{=bw0hEPN6}You already bought their contract from our clan. Why would I want to buy them back?[if:convo_confused_normal]", null), new ConversationSentence.OnConditionDelegate(this.IsSameClanMember), null, null, null, null)
					.GotoDialogState("hero_main_options")
					.NpcOption(new TextObject("{=64bH4bUo}No, thank you. But perhaps one of the other lords of our clan would be interested.[if:convo_undecided_closed]", null), () => !this.HasMobileParty(), null, null, null, null)
					.GotoDialogState("hero_main_options")
					.NpcOption(new TextObject("{=Zs6L1aBL}I'm sorry. I don't need mercenaries right now.[if:convo_normal]", null), null, null, null, null, null)
					.GotoDialogState("hero_main_options")
					.EndNpcOptions()
					.EndPlayerOptions();
				this.AddPersuasionDialogs(dialogFlow);
				return dialogFlow;
			}

			// Token: 0x0600590D RID: 22797 RVA: 0x0019E2EC File Offset: 0x0019C4EC
			private bool PersuasionDialogSpecialCondition()
			{
				return !this.IsSameClanMember() && !this.HasSameIssue() && this.HasMobileParty() && !this.InSameSettlement();
			}

			// Token: 0x0600590E RID: 22798 RVA: 0x0019E311 File Offset: 0x0019C511
			private bool HasMobileParty()
			{
				return Hero.OneToOneConversationHero.PartyBelongedTo != null;
			}

			// Token: 0x0600590F RID: 22799 RVA: 0x0019E320 File Offset: 0x0019C520
			private bool IsSameClanMember()
			{
				return Hero.OneToOneConversationHero.Clan == base.QuestGiver.Clan;
			}

			// Token: 0x06005910 RID: 22800 RVA: 0x0019E339 File Offset: 0x0019C539
			private bool InSameSettlement()
			{
				return Hero.OneToOneConversationHero.CurrentSettlement != null && base.QuestGiver.CurrentSettlement != null && Hero.OneToOneConversationHero.CurrentSettlement == base.QuestGiver.CurrentSettlement;
			}

			// Token: 0x06005911 RID: 22801 RVA: 0x0019E36D File Offset: 0x0019C56D
			private bool HasSameIssue()
			{
				IssueBase issue = Hero.OneToOneConversationHero.Issue;
				return ((issue != null) ? issue.GetType() : null) == base.GetType();
			}

			// Token: 0x06005912 RID: 22802 RVA: 0x0019E390 File Offset: 0x0019C590
			private bool PersuasionDialogForLordGeneralCondition()
			{
				if (Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.IsLord && Hero.OneToOneConversationHero.Age >= (float)Campaign.Current.Models.AgeModel.HeroComesOfAge && Hero.OneToOneConversationHero != base.QuestGiver && !Hero.OneToOneConversationHero.MapFaction.IsAtWarWith(base.QuestGiver.MapFaction) && Hero.OneToOneConversationHero.Clan != Clan.PlayerClan && !this._persuationTriedHeroesList.Contains(Hero.OneToOneConversationHero))
				{
					this.UpdateCompanyTroopCount();
					this._demandGold = 1000 + this._companyTroopCount * 150;
					MBTextManager.SetTextVariable("DEMAND_GOLD", this._demandGold);
					this._tasks[0] = this.GetPersuasionTask1();
					this._tasks[1] = this.GetPersuasionTask2();
					this._tasks[2] = this.GetPersuasionTask3();
					this._selectedTask = this._tasks.GetRandomElement<PersuasionTask>();
					return true;
				}
				return false;
			}

			// Token: 0x06005913 RID: 22803 RVA: 0x0019E490 File Offset: 0x0019C690
			private void AddPersuasionDialogs(DialogFlow dialog)
			{
				dialog.AddDialogLine("company_of_trouble_persuasion_check_accepted", "company_of_trouble_persuasion", "company_of_trouble_persuasion_start_reservation", "{=GCH6RgIQ}How tough are they?", new ConversationSentence.OnConditionDelegate(this.persuasion_start_with_company_of_trouble_on_condition), new ConversationSentence.OnConsequenceDelegate(this.persuasion_start_with_company_of_trouble_on_consequence), this, 100, null, null, null);
				dialog.AddDialogLine("company_of_trouble_persuasion_rejected", "company_of_trouble_persuasion_start_reservation", "hero_main_options", "{=!}{FAILED_PERSUASION_LINE}", new ConversationSentence.OnConditionDelegate(this.persuasion_failed_with_company_of_trouble_on_condition), new ConversationSentence.OnConsequenceDelegate(this.persuasion_rejected_with_company_of_trouble_on_consequence), this, 100, null, null, null);
				dialog.AddDialogLine("company_of_trouble_persuasion_attempt", "company_of_trouble_persuasion_start_reservation", "company_of_trouble_persuasion_select_option", "{=K0Qtl5RZ}Tell me about the details...", () => !this.persuasion_failed_with_company_of_trouble_on_condition(), null, this, 100, null, null, null);
				dialog.AddDialogLine("company_of_trouble_persuasion_success", "company_of_trouble_persuasion_start_reservation", "close_window", "{=QlECaaHt}Hmm...They can be useful.", new ConversationSentence.OnConditionDelegate(ConversationManager.GetPersuasionProgressSatisfied), new ConversationSentence.OnConsequenceDelegate(this.persuasion_complete_with_company_of_trouble_on_consequence), this, 200, null, null, null);
				string id = "company_of_trouble_persuasion_select_option_1";
				string inputToken = "company_of_trouble_persuasion_select_option";
				string outputToken = "company_of_trouble_persuasion_selected_option_response";
				string text = "{=0AUZvSAq}{COMPANY_OF_TROUBLE_PERSUADE_ATTEMPT_1}";
				ConversationSentence.OnConditionDelegate conditionDelegate = new ConversationSentence.OnConditionDelegate(this.company_of_trouble_persuasion_select_option_1_on_condition);
				ConversationSentence.OnConsequenceDelegate consequenceDelegate = new ConversationSentence.OnConsequenceDelegate(this.company_of_trouble_persuasion_select_option_1_on_consequence);
				ConversationSentence.OnPersuasionOptionDelegate persuasionOptionDelegate = new ConversationSentence.OnPersuasionOptionDelegate(this.company_of_trouble_persuasion_setup_option_1);
				ConversationSentence.OnClickableConditionDelegate clickableConditionDelegate = new ConversationSentence.OnClickableConditionDelegate(this.company_of_trouble_persuasion_clickable_option_1_on_condition);
				dialog.AddPlayerLine(id, inputToken, outputToken, text, conditionDelegate, consequenceDelegate, this, 100, clickableConditionDelegate, persuasionOptionDelegate, null, null);
				string id2 = "company_of_trouble_persuasion_select_option_2";
				string inputToken2 = "company_of_trouble_persuasion_select_option";
				string outputToken2 = "company_of_trouble_persuasion_selected_option_response";
				string text2 = "{=GG1W8qGd}{COMPANY_OF_TROUBLE_PERSUADE_ATTEMPT_2}";
				ConversationSentence.OnConditionDelegate conditionDelegate2 = new ConversationSentence.OnConditionDelegate(this.company_of_trouble_persuasion_select_option_2_on_condition);
				ConversationSentence.OnConsequenceDelegate consequenceDelegate2 = new ConversationSentence.OnConsequenceDelegate(this.company_of_trouble_persuasion_select_option_2_on_consequence);
				persuasionOptionDelegate = new ConversationSentence.OnPersuasionOptionDelegate(this.company_of_trouble_persuasion_setup_option_2);
				clickableConditionDelegate = new ConversationSentence.OnClickableConditionDelegate(this.company_of_trouble_persuasion_clickable_option_2_on_condition);
				dialog.AddPlayerLine(id2, inputToken2, outputToken2, text2, conditionDelegate2, consequenceDelegate2, this, 100, clickableConditionDelegate, persuasionOptionDelegate, null, null);
				string id3 = "company_of_trouble_persuasion_select_option_3";
				string inputToken3 = "company_of_trouble_persuasion_select_option";
				string outputToken3 = "company_of_trouble_persuasion_selected_option_response";
				string text3 = "{=kFs940kp}{COMPANY_OF_TROUBLE_PERSUADE_ATTEMPT_3}";
				ConversationSentence.OnConditionDelegate conditionDelegate3 = new ConversationSentence.OnConditionDelegate(this.company_of_trouble_persuasion_select_option_3_on_condition);
				ConversationSentence.OnConsequenceDelegate consequenceDelegate3 = new ConversationSentence.OnConsequenceDelegate(this.company_of_trouble_persuasion_select_option_3_on_consequence);
				persuasionOptionDelegate = new ConversationSentence.OnPersuasionOptionDelegate(this.company_of_trouble_persuasion_setup_option_3);
				clickableConditionDelegate = new ConversationSentence.OnClickableConditionDelegate(this.company_of_trouble_persuasion_clickable_option_3_on_condition);
				dialog.AddPlayerLine(id3, inputToken3, outputToken3, text3, conditionDelegate3, consequenceDelegate3, this, 100, clickableConditionDelegate, persuasionOptionDelegate, null, null);
				dialog.AddDialogLine("company_of_trouble_persuasion_select_option_reaction", "company_of_trouble_persuasion_selected_option_response", "company_of_trouble_persuasion_start_reservation", "{=D0xDRqvm}{PERSUASION_REACTION}", new ConversationSentence.OnConditionDelegate(this.company_of_trouble_persuasion_selected_option_response_on_condition), new ConversationSentence.OnConsequenceDelegate(this.company_of_trouble_persuasion_selected_option_response_on_consequence), this, 100, null, null, null);
			}

			// Token: 0x06005914 RID: 22804 RVA: 0x0019E6AE File Offset: 0x0019C8AE
			private void persuasion_start_with_company_of_trouble_on_consequence()
			{
				this._persuationTriedHeroesList.Add(Hero.OneToOneConversationHero);
				ConversationManager.StartPersuasion(2f, 1f, 0f, 2f, 2f, 0f, PersuasionDifficulty.Hard);
			}

			// Token: 0x06005915 RID: 22805 RVA: 0x0019E6E4 File Offset: 0x0019C8E4
			private bool persuasion_start_with_company_of_trouble_on_condition()
			{
				return !this._persuationTriedHeroesList.Contains(Hero.OneToOneConversationHero);
			}

			// Token: 0x06005916 RID: 22806 RVA: 0x0019E6FC File Offset: 0x0019C8FC
			private PersuasionTask GetPersuasionTask1()
			{
				PersuasionTask persuasionTask = new PersuasionTask(0);
				persuasionTask.FinalFailLine = new TextObject("{=1V9GeKr8}Fah...I don't need more men. Thank you.", null);
				persuasionTask.TryLaterLine = new TextObject("{=!}TODO", null);
				persuasionTask.SpokenLine = new TextObject("{=EvAubSxs}What kind of troops do they make?", null);
				PersuasionOptionArgs option = new PersuasionOptionArgs(DefaultSkills.Trade, DefaultTraits.Calculating, TraitEffect.Positive, PersuasionArgumentStrength.Easy, false, new TextObject("{=sqMUtasn}Cheap, disposable and effective. What you say?", null), null, false, false, false);
				persuasionTask.AddOptionToTask(option);
				PersuasionOptionArgs option2 = new PersuasionOptionArgs(DefaultSkills.Tactics, DefaultTraits.Calculating, TraitEffect.Positive, PersuasionArgumentStrength.ExtremelyHard, true, new TextObject("{=Pcgqs9aX}Here's a quick run down of their training...", null), null, true, false, false);
				persuasionTask.AddOptionToTask(option2);
				PersuasionOptionArgs option3 = new PersuasionOptionArgs(DefaultSkills.Roguery, DefaultTraits.Valor, TraitEffect.Positive, PersuasionArgumentStrength.Normal, false, new TextObject("{=WvQDatMJ}I won't kid you, they're mean bastards, but that's good if you can manage them.", null), null, false, false, false);
				persuasionTask.AddOptionToTask(option3);
				return persuasionTask;
			}

			// Token: 0x06005917 RID: 22807 RVA: 0x0019E7C0 File Offset: 0x0019C9C0
			private PersuasionTask GetPersuasionTask2()
			{
				PersuasionTask persuasionTask = new PersuasionTask(0);
				persuasionTask.FinalFailLine = new TextObject("{=UP0pMGDR}There are enough bandits around here already. I don't need more on retainer.", null);
				persuasionTask.TryLaterLine = new TextObject("{=!}TODO", null);
				persuasionTask.SpokenLine = new TextObject("{=zR356YDY}I have to say, they seem more like bandits than soldiers.", null);
				PersuasionOptionArgs option = new PersuasionOptionArgs(DefaultSkills.Roguery, DefaultTraits.Valor, TraitEffect.Positive, PersuasionArgumentStrength.Easy, false, new TextObject("{=JI6Q9pQ7}Bandits can kill as well as any other kind of troops, if used correctly.", null), null, false, false, false);
				persuasionTask.AddOptionToTask(option);
				PersuasionOptionArgs option2 = new PersuasionOptionArgs(DefaultSkills.Trade, DefaultTraits.Calculating, TraitEffect.Positive, PersuasionArgumentStrength.ExtremelyHard, true, new TextObject("{=SqceZdzH}Of course. That's why they're cheap. You get what you pay for. ", null), null, true, false, false);
				persuasionTask.AddOptionToTask(option2);
				PersuasionOptionArgs option3 = new PersuasionOptionArgs(DefaultSkills.Scouting, DefaultTraits.Calculating, TraitEffect.Positive, PersuasionArgumentStrength.Normal, false, new TextObject("{=NWLH02KL}Bandits are good in the wilderness, having been both predator and prey.", null), null, false, false, false);
				persuasionTask.AddOptionToTask(option3);
				return persuasionTask;
			}

			// Token: 0x06005918 RID: 22808 RVA: 0x0019E884 File Offset: 0x0019CA84
			private PersuasionTask GetPersuasionTask3()
			{
				PersuasionTask persuasionTask = new PersuasionTask(0);
				persuasionTask.FinalFailLine = new TextObject("{=97pacK2l}Fah... I don't need more men. Thank you.", null);
				persuasionTask.TryLaterLine = new TextObject("{=!}TODO", null);
				persuasionTask.SpokenLine = new TextObject("{=A2ju7YTZ}I don't know... They look treacherous.", null);
				PersuasionOptionArgs option = new PersuasionOptionArgs(DefaultSkills.Tactics, DefaultTraits.Mercy, TraitEffect.Negative, PersuasionArgumentStrength.Easy, false, new TextObject("{=z1mdQhDB}Of course. Send them in ahead of your other troops. If they die, you don't need to pay them.", null), null, false, false, false);
				persuasionTask.AddOptionToTask(option);
				PersuasionOptionArgs option2 = new PersuasionOptionArgs(DefaultSkills.Roguery, DefaultTraits.Calculating, TraitEffect.Positive, PersuasionArgumentStrength.ExtremelyHard, true, new TextObject("{=jWavM9AD}You've been around in the world. You know that mercenaries aren't saints.", null), null, true, false, false);
				persuasionTask.AddOptionToTask(option2);
				PersuasionOptionArgs option3 = new PersuasionOptionArgs(DefaultSkills.Roguery, DefaultTraits.Generosity, TraitEffect.Positive, PersuasionArgumentStrength.Normal, false, new TextObject("{=sLjGguGy}Sure, they're bastards. But they'll be loyal bastards if you treat them well.", null), null, false, false, false);
				persuasionTask.AddOptionToTask(option3);
				return persuasionTask;
			}

			// Token: 0x06005919 RID: 22809 RVA: 0x0019E948 File Offset: 0x0019CB48
			private bool company_of_trouble_persuasion_selected_option_response_on_condition()
			{
				PersuasionOptionResult item = ConversationManager.GetPersuasionChosenOptions().Last<Tuple<PersuasionOptionArgs, PersuasionOptionResult>>().Item2;
				MBTextManager.SetTextVariable("PERSUASION_REACTION", PersuasionHelper.GetDefaultPersuasionOptionReaction(item), false);
				if (item == PersuasionOptionResult.CriticalFailure)
				{
					this._selectedTask.BlockAllOptions();
				}
				return true;
			}

			// Token: 0x0600591A RID: 22810 RVA: 0x0019E988 File Offset: 0x0019CB88
			private void company_of_trouble_persuasion_selected_option_response_on_consequence()
			{
				Tuple<PersuasionOptionArgs, PersuasionOptionResult> tuple = ConversationManager.GetPersuasionChosenOptions().Last<Tuple<PersuasionOptionArgs, PersuasionOptionResult>>();
				float difficulty = Campaign.Current.Models.PersuasionModel.GetDifficulty(PersuasionDifficulty.Hard);
				float moveToNextStageChance;
				float blockRandomOptionChance;
				Campaign.Current.Models.PersuasionModel.GetEffectChances(tuple.Item1, out moveToNextStageChance, out blockRandomOptionChance, difficulty);
				this._selectedTask.ApplyEffects(moveToNextStageChance, blockRandomOptionChance);
			}

			// Token: 0x0600591B RID: 22811 RVA: 0x0019E9E4 File Offset: 0x0019CBE4
			private bool company_of_trouble_persuasion_select_option_1_on_condition()
			{
				if (this._selectedTask.Options.Count > 0)
				{
					TextObject textObject = new TextObject("{=bSo9hKwr}{PERSUASION_OPTION_LINE} {SUCCESS_CHANCE}", null);
					textObject.SetTextVariable("SUCCESS_CHANCE", PersuasionHelper.ShowSuccess(this._selectedTask.Options.ElementAt(0), false));
					textObject.SetTextVariable("PERSUASION_OPTION_LINE", this._selectedTask.Options.ElementAt(0).Line);
					MBTextManager.SetTextVariable("COMPANY_OF_TROUBLE_PERSUADE_ATTEMPT_1", textObject, false);
					return true;
				}
				return false;
			}

			// Token: 0x0600591C RID: 22812 RVA: 0x0019EA64 File Offset: 0x0019CC64
			private bool company_of_trouble_persuasion_select_option_2_on_condition()
			{
				if (this._selectedTask.Options.Count > 1)
				{
					TextObject textObject = new TextObject("{=bSo9hKwr}{PERSUASION_OPTION_LINE} {SUCCESS_CHANCE}", null);
					textObject.SetTextVariable("SUCCESS_CHANCE", PersuasionHelper.ShowSuccess(this._selectedTask.Options.ElementAt(1), false));
					textObject.SetTextVariable("PERSUASION_OPTION_LINE", this._selectedTask.Options.ElementAt(1).Line);
					MBTextManager.SetTextVariable("COMPANY_OF_TROUBLE_PERSUADE_ATTEMPT_2", textObject, false);
					return true;
				}
				return false;
			}

			// Token: 0x0600591D RID: 22813 RVA: 0x0019EAE4 File Offset: 0x0019CCE4
			private bool company_of_trouble_persuasion_select_option_3_on_condition()
			{
				if (this._selectedTask.Options.Count > 2)
				{
					TextObject textObject = new TextObject("{=bSo9hKwr}{PERSUASION_OPTION_LINE} {SUCCESS_CHANCE}", null);
					textObject.SetTextVariable("SUCCESS_CHANCE", PersuasionHelper.ShowSuccess(this._selectedTask.Options.ElementAt(2), false));
					textObject.SetTextVariable("PERSUASION_OPTION_LINE", this._selectedTask.Options.ElementAt(2).Line);
					MBTextManager.SetTextVariable("COMPANY_OF_TROUBLE_PERSUADE_ATTEMPT_3", textObject, false);
					return true;
				}
				return false;
			}

			// Token: 0x0600591E RID: 22814 RVA: 0x0019EB64 File Offset: 0x0019CD64
			private void company_of_trouble_persuasion_select_option_1_on_consequence()
			{
				if (this._selectedTask.Options.Count > 0)
				{
					this._selectedTask.Options[0].BlockTheOption(true);
				}
			}

			// Token: 0x0600591F RID: 22815 RVA: 0x0019EB90 File Offset: 0x0019CD90
			private void company_of_trouble_persuasion_select_option_2_on_consequence()
			{
				if (this._selectedTask.Options.Count > 1)
				{
					this._selectedTask.Options[1].BlockTheOption(true);
				}
			}

			// Token: 0x06005920 RID: 22816 RVA: 0x0019EBBC File Offset: 0x0019CDBC
			private void company_of_trouble_persuasion_select_option_3_on_consequence()
			{
				if (this._selectedTask.Options.Count > 2)
				{
					this._selectedTask.Options[2].BlockTheOption(true);
				}
			}

			// Token: 0x06005921 RID: 22817 RVA: 0x0019EBE8 File Offset: 0x0019CDE8
			private bool persuasion_failed_with_company_of_trouble_on_condition()
			{
				if (this._selectedTask.Options.All((PersuasionOptionArgs x) => x.IsBlocked) && !ConversationManager.GetPersuasionProgressSatisfied())
				{
					MBTextManager.SetTextVariable("FAILED_PERSUASION_LINE", this._selectedTask.FinalFailLine, false);
					return true;
				}
				return false;
			}

			// Token: 0x06005922 RID: 22818 RVA: 0x0019EC46 File Offset: 0x0019CE46
			private PersuasionOptionArgs company_of_trouble_persuasion_setup_option_1()
			{
				return this._selectedTask.Options.ElementAt(0);
			}

			// Token: 0x06005923 RID: 22819 RVA: 0x0019EC59 File Offset: 0x0019CE59
			private PersuasionOptionArgs company_of_trouble_persuasion_setup_option_2()
			{
				return this._selectedTask.Options.ElementAt(1);
			}

			// Token: 0x06005924 RID: 22820 RVA: 0x0019EC6C File Offset: 0x0019CE6C
			private PersuasionOptionArgs company_of_trouble_persuasion_setup_option_3()
			{
				return this._selectedTask.Options.ElementAt(2);
			}

			// Token: 0x06005925 RID: 22821 RVA: 0x0019EC80 File Offset: 0x0019CE80
			private bool company_of_trouble_persuasion_clickable_option_1_on_condition(out TextObject hintText)
			{
				hintText = new TextObject("{=9ACJsI6S}Blocked", null);
				if (this._selectedTask.Options.Count > 0)
				{
					hintText = (this._selectedTask.Options.ElementAt(0).IsBlocked ? hintText : null);
					return !this._selectedTask.Options.ElementAt(0).IsBlocked;
				}
				return false;
			}

			// Token: 0x06005926 RID: 22822 RVA: 0x0019ECE8 File Offset: 0x0019CEE8
			private bool company_of_trouble_persuasion_clickable_option_2_on_condition(out TextObject hintText)
			{
				hintText = new TextObject("{=9ACJsI6S}Blocked", null);
				if (this._selectedTask.Options.Count > 1)
				{
					hintText = (this._selectedTask.Options.ElementAt(1).IsBlocked ? hintText : null);
					return !this._selectedTask.Options.ElementAt(1).IsBlocked;
				}
				return false;
			}

			// Token: 0x06005927 RID: 22823 RVA: 0x0019ED50 File Offset: 0x0019CF50
			private bool company_of_trouble_persuasion_clickable_option_3_on_condition(out TextObject hintText)
			{
				hintText = new TextObject("{=9ACJsI6S}Blocked", null);
				if (this._selectedTask.Options.Count > 2)
				{
					hintText = (this._selectedTask.Options.ElementAt(2).IsBlocked ? hintText : null);
					return !this._selectedTask.Options.ElementAt(2).IsBlocked;
				}
				return false;
			}

			// Token: 0x06005928 RID: 22824 RVA: 0x0019EDB7 File Offset: 0x0019CFB7
			private void persuasion_rejected_with_company_of_trouble_on_consequence()
			{
				if (PlayerEncounter.Current != null)
				{
					PlayerEncounter.LeaveEncounter = true;
				}
				ConversationManager.EndPersuasion();
			}

			// Token: 0x06005929 RID: 22825 RVA: 0x0019EDCC File Offset: 0x0019CFCC
			private void persuasion_complete_with_company_of_trouble_on_consequence()
			{
				if (PlayerEncounter.Current != null)
				{
					PlayerEncounter.LeaveEncounter = true;
				}
				ConversationManager.EndPersuasion();
				this.UpdateCompanyTroopCount();
				MobileParty.MainParty.MemberRoster.AddToCounts(this._troubleCharacterObject, -this._companyTroopCount, false, 0, 0, true, -1);
				GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, this._demandGold, false);
				this.RelationshipChangeWithQuestGiver = 5;
				base.AddLog(this.QuestSuccessPlayerSoldCompany, false);
				base.CompleteQuestWithSuccess();
			}

			// Token: 0x0600592A RID: 22826 RVA: 0x0019EE40 File Offset: 0x0019D040
			private DialogFlow GetCompanyDialogFlow()
			{
				return DialogFlow.CreateDialogFlow("start", 125).NpcLine(new TextObject("{=8TCev3Qs}So, captain. We expect a bit of looting and plundering as compensation, in addition to the wages. You don't seem like you're going to provide it to us. So, farewell.[if:innocent_smile][ib:hip]", null), null, null, null, null).Condition(new ConversationSentence.OnConditionDelegate(this.CompanyDialogFromCondition))
					.BeginPlayerOptions(null, false)
					.PlayerOption(new TextObject("{=1aaoSpNf}Your contract with the {QUEST_GIVER.NAME} is still in force. I can't let you go without {?QUEST_GIVER.GENDER}her{?}his{\\?} permission.", null), null, null, null)
					.NpcLine(new TextObject("{=oI5H6Xo8}Don't think we won't fight you if you try and stop us.[if:convo_mocking_aristocratic]", null), null, null, null, null)
					.BeginPlayerOptions(null, false)
					.PlayerOption(new TextObject("{=hIFazIcK}So be it!", null), null, null, null)
					.NpcLine(new TextObject("{=KKeRi477}All right, lads. Let's kill the boss.[if:convo_predatory][ib:aggressive]", null), null, null, null, null)
					.Consequence(delegate
					{
						Campaign.Current.ConversationManager.ConversationEndOneShot += this.CreateCompanyEnemyParty;
					})
					.CloseDialog()
					.PlayerOption(new TextObject("{=bm7UcuQj}No! There is no need to fight. I don't want any bloodshed... Just leave.", null), null, null, null)
					.NpcLine(new TextObject("{=1vnaskLR}It was a pleasure to work with you, chief. Farewell...[if:convo_nonchalant][ib:normal2]", null), null, null, null, null)
					.Consequence(delegate
					{
						this._companyLeftQuestWillFail = true;
					})
					.CloseDialog()
					.EndPlayerOptions()
					.CloseDialog()
					.PlayerOption(new TextObject("{=hj4vfgxk}As you wish! Good luck. ", null), null, null, null)
					.NpcLine(new TextObject("{=1vnaskLR}It was a pleasure to work with you, chief. Farewell...[if:convo_nonchalant][ib:normal2]", null), null, null, null, null)
					.Consequence(delegate
					{
						this._companyLeftQuestWillFail = true;
					})
					.CloseDialog()
					.EndPlayerOptions();
			}

			// Token: 0x0600592B RID: 22827 RVA: 0x0019EF79 File Offset: 0x0019D179
			private bool CompanyDialogFromCondition()
			{
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, null, false);
				return this._troubleCharacterObject == CharacterObject.OneToOneConversationCharacter;
			}

			// Token: 0x0600592C RID: 22828 RVA: 0x0019EFA0 File Offset: 0x0019D1A0
			private void CreateCompanyEnemyParty()
			{
				MobileParty.MainParty.MemberRoster.AddToCounts(this._troubleCharacterObject, -this._companyTroopCount, false, 0, 0, true, -1);
				Settlement settlement = SettlementHelper.FindRandomSettlement((Settlement x) => x.IsHideout);
				this._companyOfTroubleParty = BanditPartyComponent.CreateBanditParty("company_of_trouble_" + base.StringId, settlement.OwnerClan, settlement.Hideout, false, null, MobileParty.MainParty.Position);
				this._companyOfTroubleParty.MemberRoster.AddToCounts(this._troubleCharacterObject, this._companyTroopCount, false, 0, 0, true, -1);
				TextObject customName = new TextObject("{=PV7RHgUl}Company of Trouble", null);
				this._companyOfTroubleParty.Party.SetCustomName(customName);
				this._companyOfTroubleParty.SetPartyUsedByQuest(true);
				this._battleWillStart = true;
			}

			// Token: 0x0600592D RID: 22829 RVA: 0x0019F07C File Offset: 0x0019D27C
			internal void CompanyLeftQuestFail()
			{
				this.RelationshipChangeWithQuestGiver = -2;
				this.UpdateCompanyTroopCount();
				MobileParty.MainParty.MemberRoster.AddToCounts(this._troubleCharacterObject, -this._companyTroopCount, false, 0, 0, true, -1);
				base.AddLog(this.QuestFailCompanyLeft, false);
				base.CompleteQuestWithFail(null);
				this._companyLeftQuestWillFail = false;
				GameMenu.ExitToLast();
			}

			// Token: 0x0600592E RID: 22830 RVA: 0x0019F0DC File Offset: 0x0019D2DC
			private void QuestAcceptedConsequences()
			{
				base.StartQuest();
				base.AddLog(this.PlayerStartsQuestLogText, false);
				MobileParty.MainParty.MemberRoster.AddToCounts(this._troubleCharacterObject, this._companyTroopCount, false, 0, 0, true, -1);
				MBInformationManager.AddQuickInformation(new TextObject("{=jGIxKb99}Mercenaries have joined your party.", null), 0, null, null, "");
			}

			// Token: 0x0600592F RID: 22831 RVA: 0x0019F138 File Offset: 0x0019D338
			protected override void RegisterEvents()
			{
				CampaignEvents.MapEventEnded.AddNonSerializedListener(this, new Action<MapEvent>(this.OnMapEventEnded));
				CampaignEvents.WarDeclared.AddNonSerializedListener(this, new Action<IFaction, IFaction, DeclareWarAction.DeclareWarDetail>(this.OnWarDeclared));
				CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, new Action<Clan, Kingdom, Kingdom, ChangeKingdomAction.ChangeKingdomActionDetail, bool>(this.OnClanChangedKingdom));
				CampaignEvents.MapEventStarted.AddNonSerializedListener(this, new Action<MapEvent, PartyBase, PartyBase>(this.OnMapEventStarted));
			}

			// Token: 0x06005930 RID: 22832 RVA: 0x0019F1A1 File Offset: 0x0019D3A1
			private void OnMapEventStarted(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
			{
				if (QuestHelper.CheckMinorMajorCoercion(this, mapEvent, attackerParty))
				{
					QuestHelper.ApplyGenericMinorMajorCoercionConsequences(this, mapEvent);
				}
			}

			// Token: 0x06005931 RID: 22833 RVA: 0x0019F1B4 File Offset: 0x0019D3B4
			private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
			{
				if (base.QuestGiver.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction))
				{
					base.CompleteQuestWithCancel(this.QuestCanceledWarDeclared);
				}
			}

			// Token: 0x06005932 RID: 22834 RVA: 0x0019F1DE File Offset: 0x0019D3DE
			private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
			{
				QuestHelper.CheckWarDeclarationAndFailOrCancelTheQuest(this, faction1, faction2, detail, this.PlayerDeclaredWarQuestLogText, this.QuestCanceledWarDeclared, false);
			}

			// Token: 0x06005933 RID: 22835 RVA: 0x0019F1F8 File Offset: 0x0019D3F8
			private void OnMapEventEnded(MapEvent mapEvent)
			{
				if ((mapEvent.IsPlayerMapEvent || mapEvent.IsPlayerSimulation) && !this._checkForBattleResults)
				{
					this.UpdateCompanyTroopCount();
					if (this._companyTroopCount == 0)
					{
						base.AddLog(this.AllCompanyDiedLogText, false);
						this.RelationshipChangeWithQuestGiver = 5;
						GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, this.RewardGold, false);
						base.CompleteQuestWithSuccess();
					}
				}
			}

			// Token: 0x06005934 RID: 22836 RVA: 0x0019F258 File Offset: 0x0019D458
			protected override void HourlyTick()
			{
				if (base.IsOngoing)
				{
					this.UpdateCompanyTroopCount();
					if (MobileParty.MainParty.MemberRoster.TotalManCount - this._companyTroopCount <= this._companyTroopCount && MapEvent.PlayerMapEvent == null && Settlement.CurrentSettlement == null && PlayerEncounter.Current == null && !Hero.MainHero.IsWounded && !MobileParty.MainParty.IsCurrentlyAtSea)
					{
						this._triggerCompanyOfTroubleConversation = true;
						GameMenu.ActivateGameMenu("company_of_trouble_menu");
					}
				}
			}

			// Token: 0x06005935 RID: 22837 RVA: 0x0019F2D0 File Offset: 0x0019D4D0
			private void TryToStealItemFromPlayer()
			{
				bool flag = false;
				for (int i = 0; i < MobileParty.MainParty.ItemRoster.Count; i++)
				{
					ItemRosterElement itemRosterElement = MobileParty.MainParty.ItemRoster[i];
					ItemObject item = itemRosterElement.EquipmentElement.Item;
					if (!itemRosterElement.IsEmpty && item.IsFood)
					{
						MobileParty.MainParty.ItemRoster.AddToCounts(item, -1);
						flag = true;
						break;
					}
				}
				if (flag)
				{
					if (this._thieveryCount == 0 || this._thieveryCount == 1)
					{
						InformationManager.ShowInquiry(new InquiryData(this.Title.ToString(), (this._thieveryCount == 0) ? new TextObject("{=OKpwA8Az}Your men have noticed some of the goods in the baggage train are missing.", null).ToString() : new TextObject("{=acu1wTeq}Your men are sure of that some of the goods were stolen from the baggage train.", null).ToString(), true, false, GameTexts.FindText("str_ok", null).ToString(), "", null, null, "", 0f, null, null, null), true, false);
					}
					else
					{
						MBInformationManager.AddQuickInformation(new TextObject("{=xlm8oYhM}Your men reported that some of the goods were stolen from the baggage train.", null), 0, null, null, "");
					}
					this._thieveryCount++;
				}
			}

			// Token: 0x06005936 RID: 22838 RVA: 0x0019F3E8 File Offset: 0x0019D5E8
			protected override void DailyTick()
			{
				if (MBRandom.RandomFloat > 0.5f)
				{
					this.TryToStealItemFromPlayer();
				}
			}

			// Token: 0x06005937 RID: 22839 RVA: 0x0019F3FC File Offset: 0x0019D5FC
			private void UpdateCompanyTroopCount()
			{
				bool flag = false;
				foreach (TroopRosterElement troopRosterElement in MobileParty.MainParty.MemberRoster.GetTroopRoster())
				{
					if (troopRosterElement.Character == this._troubleCharacterObject)
					{
						flag = true;
						this._companyTroopCount = troopRosterElement.Number;
						break;
					}
				}
				if (!flag)
				{
					this._companyTroopCount = 0;
				}
			}

			// Token: 0x06005938 RID: 22840 RVA: 0x0019F47C File Offset: 0x0019D67C
			internal void QuestSuccessWithPlayerDefeatedCompany()
			{
				base.AddLog(this.AllCompanyDiedLogText, false);
				this.RelationshipChangeWithQuestGiver = 5;
				GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, this.RewardGold, false);
				base.CompleteQuestWithSuccess();
			}

			// Token: 0x06005939 RID: 22841 RVA: 0x0019F4AB File Offset: 0x0019D6AB
			internal void QuestFailWithPlayerDefeatedAgainstCompany()
			{
				this.RelationshipChangeWithQuestGiver = -2;
				base.AddLog(this.PlayerDefeatedAgainstCompany, false);
				base.CompleteQuestWithFail(null);
			}

			// Token: 0x0600593A RID: 22842 RVA: 0x0019F4CA File Offset: 0x0019D6CA
			protected override void OnFinalize()
			{
				this.UpdateCompanyTroopCount();
				if (this._companyTroopCount > 0)
				{
					MobileParty.MainParty.MemberRoster.AddToCounts(this._troubleCharacterObject, -this._companyTroopCount, false, 0, 0, true, -1);
				}
			}

			// Token: 0x04001CA7 RID: 7335
			private const string TroubleCharacterObjectStringId = "company_of_trouble_character";

			// Token: 0x04001CA8 RID: 7336
			private int _companyTroopCount;

			// Token: 0x04001CA9 RID: 7337
			[SaveableField(20)]
			internal MobileParty _companyOfTroubleParty;

			// Token: 0x04001CAA RID: 7338
			[SaveableField(30)]
			internal bool _battleWillStart;

			// Token: 0x04001CAB RID: 7339
			internal bool _checkForBattleResults;

			// Token: 0x04001CAC RID: 7340
			[SaveableField(40)]
			private int _thieveryCount;

			// Token: 0x04001CAD RID: 7341
			[SaveableField(80)]
			internal bool _triggerCompanyOfTroubleConversation;

			// Token: 0x04001CAE RID: 7342
			[SaveableField(50)]
			private int _demandGold;

			// Token: 0x04001CAF RID: 7343
			internal CharacterObject _troubleCharacterObject;

			// Token: 0x04001CB0 RID: 7344
			private PersuasionTask[] _tasks;

			// Token: 0x04001CB1 RID: 7345
			private PersuasionTask _selectedTask;

			// Token: 0x04001CB2 RID: 7346
			private const PersuasionDifficulty Difficulty = PersuasionDifficulty.Hard;

			// Token: 0x04001CB3 RID: 7347
			[SaveableField(70)]
			private List<Hero> _persuationTriedHeroesList;

			// Token: 0x04001CB4 RID: 7348
			internal bool _companyLeftQuestWillFail;
		}

		// Token: 0x02000716 RID: 1814
		public class LandLordCompanyOfTroubleIssueTypeDefiner : SaveableTypeDefiner
		{
			// Token: 0x06005942 RID: 22850 RVA: 0x0019F560 File Offset: 0x0019D760
			public LandLordCompanyOfTroubleIssueTypeDefiner()
				: base(4800000)
			{
			}

			// Token: 0x06005943 RID: 22851 RVA: 0x0019F56D File Offset: 0x0019D76D
			protected override void DefineClassTypes()
			{
				base.AddClassDefinition(typeof(LandLordCompanyOfTroubleIssueBehavior.LandLordCompanyOfTroubleIssue), 1, null);
				base.AddClassDefinition(typeof(LandLordCompanyOfTroubleIssueBehavior.LandLordCompanyOfTroubleIssueQuest), 2, null);
			}
		}
	}
}
