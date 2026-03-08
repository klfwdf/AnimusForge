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
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace TaleWorlds.CampaignSystem.Issues
{
	// Token: 0x02000371 RID: 881
	public class MerchantArmyOfPoachersIssueBehavior : CampaignBehaviorBase
	{
		// Token: 0x06003357 RID: 13143 RVA: 0x000D267E File Offset: 0x000D087E
		private void engage_poachers_consequence(MenuCallbackArgs args)
		{
			MerchantArmyOfPoachersIssueBehavior.Instance.StartQuestBattle();
		}

		// Token: 0x17000C3C RID: 3132
		// (get) Token: 0x06003358 RID: 13144 RVA: 0x000D268C File Offset: 0x000D088C
		private static MerchantArmyOfPoachersIssueBehavior.MerchantArmyOfPoachersIssueQuest Instance
		{
			get
			{
				MerchantArmyOfPoachersIssueBehavior campaignBehavior = Campaign.Current.GetCampaignBehavior<MerchantArmyOfPoachersIssueBehavior>();
				if (campaignBehavior._cachedQuest != null && campaignBehavior._cachedQuest.IsOngoing)
				{
					return campaignBehavior._cachedQuest;
				}
				using (List<QuestBase>.Enumerator enumerator = Campaign.Current.QuestManager.Quests.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						MerchantArmyOfPoachersIssueBehavior.MerchantArmyOfPoachersIssueQuest cachedQuest;
						if ((cachedQuest = enumerator.Current as MerchantArmyOfPoachersIssueBehavior.MerchantArmyOfPoachersIssueQuest) != null)
						{
							campaignBehavior._cachedQuest = cachedQuest;
							return campaignBehavior._cachedQuest;
						}
					}
				}
				return null;
			}
		}

		// Token: 0x06003359 RID: 13145 RVA: 0x000D2724 File Offset: 0x000D0924
		public override void RegisterEvents()
		{
			CampaignEvents.OnCheckForIssueEvent.AddNonSerializedListener(this, new Action<Hero>(this.OnCheckForIssue));
			CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnSessionLaunched));
		}

		// Token: 0x0600335A RID: 13146 RVA: 0x000D2754 File Offset: 0x000D0954
		private bool poachers_menu_back_condition(MenuCallbackArgs args)
		{
			return Hero.MainHero.IsWounded;
		}

		// Token: 0x0600335B RID: 13147 RVA: 0x000D2760 File Offset: 0x000D0960
		private void OnSessionLaunched(CampaignGameStarter gameStarter)
		{
			gameStarter.AddGameMenu("army_of_poachers_village", "{=eaQxeRh6}A boy runs out of the village and asks you to talk to the leader of the poachers. The villagers want to avoid a fight outside their homes.", new OnInitDelegate(this.army_of_poachers_village_on_init), GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
			gameStarter.AddGameMenuOption("army_of_poachers_village", "engage_the_poachers", "{=xF7he8fZ}Fight the poachers", new GameMenuOption.OnConditionDelegate(this.engage_poachers_condition), new GameMenuOption.OnConsequenceDelegate(this.engage_poachers_consequence), false, -1, false, null);
			gameStarter.AddGameMenuOption("army_of_poachers_village", "talk_to_the_poachers", "{=wwJGE28v}Negotiate with the poachers", new GameMenuOption.OnConditionDelegate(this.talk_to_leader_of_poachers_condition), new GameMenuOption.OnConsequenceDelegate(this.talk_to_leader_of_poachers_consequence), false, -1, false, null);
			gameStarter.AddGameMenuOption("army_of_poachers_village", "back_poachers", "{=E1OwmQFb}Back", new GameMenuOption.OnConditionDelegate(this.poachers_menu_back_condition), new GameMenuOption.OnConsequenceDelegate(this.poachers_menu_back_consequence), false, -1, false, null);
		}

		// Token: 0x0600335C RID: 13148 RVA: 0x000D2820 File Offset: 0x000D0A20
		private void army_of_poachers_village_on_init(MenuCallbackArgs args)
		{
			if (MerchantArmyOfPoachersIssueBehavior.Instance != null && MerchantArmyOfPoachersIssueBehavior.Instance.IsOngoing)
			{
				args.MenuContext.SetBackgroundMeshName(MerchantArmyOfPoachersIssueBehavior.Instance._questVillage.Settlement.SettlementComponent.WaitMeshName);
				if (MerchantArmyOfPoachersIssueBehavior.Instance._poachersParty == null && !Hero.MainHero.IsWounded)
				{
					MerchantArmyOfPoachersIssueBehavior.Instance.CreatePoachersParty();
				}
				if (MerchantArmyOfPoachersIssueBehavior.Instance._isReadyToBeFinalized && PlayerEncounter.Current != null)
				{
					bool flag = PlayerEncounter.Battle.WinningSide == PlayerEncounter.Battle.PlayerSide;
					PlayerEncounter.Update();
					if (PlayerEncounter.Current == null)
					{
						MerchantArmyOfPoachersIssueBehavior.Instance._isReadyToBeFinalized = false;
						if (flag)
						{
							MerchantArmyOfPoachersIssueBehavior.Instance.QuestSuccessWithPlayerDefeatedPoachers();
						}
						else
						{
							MerchantArmyOfPoachersIssueBehavior.Instance.QuestFailWithPlayerDefeatedAgainstPoachers();
						}
					}
					else if (PlayerEncounter.Battle.WinningSide == BattleSideEnum.None)
					{
						PlayerEncounter.LeaveEncounter = true;
						PlayerEncounter.Update();
						MerchantArmyOfPoachersIssueBehavior.Instance.QuestFailWithPlayerDefeatedAgainstPoachers();
					}
					else if (flag && PlayerEncounter.Current != null && Game.Current.GameStateManager.ActiveState is MapState)
					{
						PlayerEncounter.Finish(true);
						MerchantArmyOfPoachersIssueBehavior.Instance.QuestSuccessWithPlayerDefeatedPoachers();
					}
				}
				if (MerchantArmyOfPoachersIssueBehavior.Instance != null && MerchantArmyOfPoachersIssueBehavior.Instance._talkedToPoachersBattleWillStart)
				{
					MerchantArmyOfPoachersIssueBehavior.Instance.StartQuestBattle();
				}
			}
		}

		// Token: 0x0600335D RID: 13149 RVA: 0x000D295F File Offset: 0x000D0B5F
		private bool engage_poachers_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Mission;
			if (Hero.MainHero.IsWounded)
			{
				args.Tooltip = new TextObject("{=gEHEQazX}You're heavily wounded and not fit for the fight. Come back when you're ready.", null);
				args.IsEnabled = false;
			}
			return true;
		}

		// Token: 0x0600335E RID: 13150 RVA: 0x000D298D File Offset: 0x000D0B8D
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x0600335F RID: 13151 RVA: 0x000D298F File Offset: 0x000D0B8F
		private bool talk_to_leader_of_poachers_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Conversation;
			if (Hero.MainHero.IsWounded)
			{
				args.Tooltip = new TextObject("{=gEHEQazX}You're heavily wounded and not fit for the fight. Come back when you're ready.", null);
				args.IsEnabled = false;
			}
			return true;
		}

		// Token: 0x06003360 RID: 13152 RVA: 0x000D29BE File Offset: 0x000D0BBE
		private void poachers_menu_back_consequence(MenuCallbackArgs args)
		{
			PlayerEncounter.LeaveSettlement();
			PlayerEncounter.Finish(true);
		}

		// Token: 0x06003361 RID: 13153 RVA: 0x000D29CC File Offset: 0x000D0BCC
		private bool ConditionsHold(Hero issueGiver, out Village questVillage)
		{
			questVillage = null;
			if (issueGiver.CurrentSettlement != null)
			{
				questVillage = issueGiver.CurrentSettlement.BoundVillages.GetRandomElementWithPredicate((Village x) => !x.Settlement.IsUnderRaid && !x.Settlement.IsRaided);
				if (questVillage != null && issueGiver.IsMerchant && issueGiver.GetTraitLevel(DefaultTraits.Mercy) + issueGiver.GetTraitLevel(DefaultTraits.Honor) < 0)
				{
					Town town = issueGiver.CurrentSettlement.Town;
					if (town != null && town.Security <= (float)60)
					{
						return SettlementHelper.FindNearestHideoutToSettlement(questVillage.Settlement, MobileParty.NavigationType.Default, (Settlement x) => x.IsActive) != null;
					}
				}
				return false;
			}
			return false;
		}

		// Token: 0x06003362 RID: 13154 RVA: 0x000D2A94 File Offset: 0x000D0C94
		private void talk_to_leader_of_poachers_consequence(MenuCallbackArgs args)
		{
			CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty, false, false, false, false, false, false), new ConversationCharacterData(ConversationHelper.GetConversationCharacterPartyLeader(MerchantArmyOfPoachersIssueBehavior.Instance._poachersParty.Party), MerchantArmyOfPoachersIssueBehavior.Instance._poachersParty.Party, false, false, false, false, false, false));
		}

		// Token: 0x06003363 RID: 13155 RVA: 0x000D2AEC File Offset: 0x000D0CEC
		public void OnCheckForIssue(Hero hero)
		{
			Village relatedObject;
			if (this.ConditionsHold(hero, out relatedObject))
			{
				Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(new PotentialIssueData.StartIssueDelegate(this.OnSelected), typeof(MerchantArmyOfPoachersIssueBehavior.MerchantArmyOfPoachersIssue), IssueBase.IssueFrequency.Common, relatedObject));
				return;
			}
			Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(typeof(MerchantArmyOfPoachersIssueBehavior.MerchantArmyOfPoachersIssue), IssueBase.IssueFrequency.Common));
		}

		// Token: 0x06003364 RID: 13156 RVA: 0x000D2B54 File Offset: 0x000D0D54
		private IssueBase OnSelected(in PotentialIssueData pid, Hero issueOwner)
		{
			PotentialIssueData potentialIssueData = pid;
			return new MerchantArmyOfPoachersIssueBehavior.MerchantArmyOfPoachersIssue(issueOwner, potentialIssueData.RelatedObject as Village);
		}

		// Token: 0x04000EA6 RID: 3750
		private const IssueBase.IssueFrequency ArmyOfPoachersIssueFrequency = IssueBase.IssueFrequency.Common;

		// Token: 0x04000EA7 RID: 3751
		private MerchantArmyOfPoachersIssueBehavior.MerchantArmyOfPoachersIssueQuest _cachedQuest;

		// Token: 0x02000735 RID: 1845
		public class MerchantArmyOfPoachersIssue : IssueBase
		{
			// Token: 0x06005C8E RID: 23694 RVA: 0x001ACE3F File Offset: 0x001AB03F
			internal static void AutoGeneratedStaticCollectObjectsMerchantArmyOfPoachersIssue(object o, List<object> collectedObjects)
			{
				((MerchantArmyOfPoachersIssueBehavior.MerchantArmyOfPoachersIssue)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
			}

			// Token: 0x06005C8F RID: 23695 RVA: 0x001ACE4D File Offset: 0x001AB04D
			protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
			{
				base.AutoGeneratedInstanceCollectObjects(collectedObjects);
				collectedObjects.Add(this._questVillage);
			}

			// Token: 0x06005C90 RID: 23696 RVA: 0x001ACE62 File Offset: 0x001AB062
			internal static object AutoGeneratedGetMemberValue_questVillage(object o)
			{
				return ((MerchantArmyOfPoachersIssueBehavior.MerchantArmyOfPoachersIssue)o)._questVillage;
			}

			// Token: 0x17001283 RID: 4739
			// (get) Token: 0x06005C91 RID: 23697 RVA: 0x001ACE6F File Offset: 0x001AB06F
			public override int AlternativeSolutionBaseNeededMenCount
			{
				get
				{
					return 12 + MathF.Ceiling(28f * base.IssueDifficultyMultiplier);
				}
			}

			// Token: 0x17001284 RID: 4740
			// (get) Token: 0x06005C92 RID: 23698 RVA: 0x001ACE85 File Offset: 0x001AB085
			protected override int AlternativeSolutionBaseDurationInDaysInternal
			{
				get
				{
					return 3 + MathF.Ceiling(5f * base.IssueDifficultyMultiplier);
				}
			}

			// Token: 0x17001285 RID: 4741
			// (get) Token: 0x06005C93 RID: 23699 RVA: 0x001ACE9A File Offset: 0x001AB09A
			protected override int RewardGold
			{
				get
				{
					return (int)(500f + 3000f * base.IssueDifficultyMultiplier);
				}
			}

			// Token: 0x17001286 RID: 4742
			// (get) Token: 0x06005C94 RID: 23700 RVA: 0x001ACEAF File Offset: 0x001AB0AF
			public override IssueBase.AlternativeSolutionScaleFlag AlternativeSolutionScaleFlags
			{
				get
				{
					return IssueBase.AlternativeSolutionScaleFlag.Casualties | IssueBase.AlternativeSolutionScaleFlag.FailureRisk;
				}
			}

			// Token: 0x17001287 RID: 4743
			// (get) Token: 0x06005C95 RID: 23701 RVA: 0x001ACEB3 File Offset: 0x001AB0B3
			public override TextObject IssueBriefByIssueGiver
			{
				get
				{
					return new TextObject("{=Jk3mDlU6}Yeah... I've got some problems. A few years ago, I needed hides for my tannery and I hired some hunters. I didn't ask too many questions about where they came by the skins they sold me. Well, that was a bit of mistake. Now they've banded together as a gang and are trying to muscle me out of the leather business.[ib:closed2][if:convo_thinking]", null);
				}
			}

			// Token: 0x17001288 RID: 4744
			// (get) Token: 0x06005C96 RID: 23702 RVA: 0x001ACEC0 File Offset: 0x001AB0C0
			public override TextObject IssueAcceptByPlayer
			{
				get
				{
					return new TextObject("{=apuNQC2W}What can I do for you?", null);
				}
			}

			// Token: 0x17001289 RID: 4745
			// (get) Token: 0x06005C97 RID: 23703 RVA: 0x001ACECD File Offset: 0x001AB0CD
			public override TextObject IssueQuestSolutionExplanationByIssueGiver
			{
				get
				{
					TextObject textObject = new TextObject("{=LbTETjZu}I want you to crush them. Go to {VILLAGE} and give them a lesson they won't forget.[ib:closed2][if:convo_grave]", null);
					textObject.SetTextVariable("VILLAGE", this._questVillage.Settlement.EncyclopediaLinkWithName);
					return textObject;
				}
			}

			// Token: 0x1700128A RID: 4746
			// (get) Token: 0x06005C98 RID: 23704 RVA: 0x001ACEF6 File Offset: 0x001AB0F6
			public override TextObject IssueAlternativeSolutionExplanationByIssueGiver
			{
				get
				{
					TextObject textObject = new TextObject("{=2ELhox6C}If you don't want to get involved in this yourself, leave one of your capable companions and {NUMBER_OF_TROOPS} men for some days.[ib:closed][if:convo_grave]", null);
					textObject.SetTextVariable("NUMBER_OF_TROOPS", base.GetTotalAlternativeSolutionNeededMenCount());
					return textObject;
				}
			}

			// Token: 0x1700128B RID: 4747
			// (get) Token: 0x06005C99 RID: 23705 RVA: 0x001ACF15 File Offset: 0x001AB115
			public override TextObject IssueQuestSolutionAcceptByPlayer
			{
				get
				{
					return new TextObject("{=b6naGx6H}I'll rid you of those poachers myself.", null);
				}
			}

			// Token: 0x1700128C RID: 4748
			// (get) Token: 0x06005C9A RID: 23706 RVA: 0x001ACF22 File Offset: 0x001AB122
			public override TextObject IssueAlternativeSolutionAcceptByPlayer
			{
				get
				{
					return new TextObject("{=lA14Ubal}I can send a companion to hunt these poachers.", null);
				}
			}

			// Token: 0x1700128D RID: 4749
			// (get) Token: 0x06005C9B RID: 23707 RVA: 0x001ACF2F File Offset: 0x001AB12F
			public override TextObject IssueAlternativeSolutionResponseByIssueGiver
			{
				get
				{
					return new TextObject("{=Xmtlrrmf}Thank you.[ib:normal][if:convo_normal]  Don't forget to warn your men. These poachers are not ordinary bandits. Good luck.", null);
				}
			}

			// Token: 0x1700128E RID: 4750
			// (get) Token: 0x06005C9C RID: 23708 RVA: 0x001ACF3C File Offset: 0x001AB13C
			public override TextObject IssueDiscussAlternativeSolution
			{
				get
				{
					return new TextObject("{=51ahPi69}I understand that your men are still chasing those poachers. I realize that this mess might take a little time to clean up.[ib:normal2][if:convo_grave]", null);
				}
			}

			// Token: 0x1700128F RID: 4751
			// (get) Token: 0x06005C9D RID: 23709 RVA: 0x001ACF49 File Offset: 0x001AB149
			public override bool IsThereAlternativeSolution
			{
				get
				{
					return true;
				}
			}

			// Token: 0x17001290 RID: 4752
			// (get) Token: 0x06005C9E RID: 23710 RVA: 0x001ACF4C File Offset: 0x001AB14C
			public override bool IsThereLordSolution
			{
				get
				{
					return false;
				}
			}

			// Token: 0x17001291 RID: 4753
			// (get) Token: 0x06005C9F RID: 23711 RVA: 0x001ACF50 File Offset: 0x001AB150
			protected override TextObject AlternativeSolutionStartLog
			{
				get
				{
					TextObject textObject = new TextObject("{=428B377z}{ISSUE_GIVER.LINK}, a merchant of {QUEST_GIVER_SETTLEMENT}, told you that the poachers {?ISSUE_GIVER.GENDER}she{?}he{\\?} hired are now out of control. You asked {COMPANION.LINK} to take {NEEDED_MEN_COUNT} of your men to go to {QUEST_VILLAGE} and kill the poachers. They should rejoin your party in {RETURN_DAYS} days.", null);
					StringHelpers.SetCharacterProperties("ISSUE_GIVER", base.IssueOwner.CharacterObject, textObject, false);
					StringHelpers.SetCharacterProperties("COMPANION", base.AlternativeSolutionHero.CharacterObject, textObject, false);
					textObject.SetTextVariable("QUEST_GIVER_SETTLEMENT", base.IssueOwner.CurrentSettlement.EncyclopediaLinkWithName);
					textObject.SetTextVariable("NEEDED_MEN_COUNT", this.AlternativeSolutionSentTroops.TotalManCount - 1);
					textObject.SetTextVariable("QUEST_VILLAGE", this._questVillage.Settlement.EncyclopediaLinkWithName);
					textObject.SetTextVariable("RETURN_DAYS", base.GetTotalAlternativeSolutionDurationInDays());
					return textObject;
				}
			}

			// Token: 0x17001292 RID: 4754
			// (get) Token: 0x06005CA0 RID: 23712 RVA: 0x001ACFFD File Offset: 0x001AB1FD
			public override TextObject Title
			{
				get
				{
					return new TextObject("{=iHFo2kjz}Army of Poachers", null);
				}
			}

			// Token: 0x17001293 RID: 4755
			// (get) Token: 0x06005CA1 RID: 23713 RVA: 0x001AD00A File Offset: 0x001AB20A
			public override TextObject Description
			{
				get
				{
					TextObject result = new TextObject("{=NCC4VUOc}{ISSUE_GIVER.LINK} wants you to get rid of the poachers who once worked for {?ISSUE_GIVER.GENDER}her{?}him{\\?} but are now out of control.", null);
					StringHelpers.SetCharacterProperties("ISSUE_GIVER", base.IssueOwner.CharacterObject, null, false);
					return result;
				}
			}

			// Token: 0x06005CA2 RID: 23714 RVA: 0x001AD02F File Offset: 0x001AB22F
			public MerchantArmyOfPoachersIssue(Hero issueOwner, Village questVillage)
				: base(issueOwner, CampaignTime.DaysFromNow(15f))
			{
				this._questVillage = questVillage;
			}

			// Token: 0x06005CA3 RID: 23715 RVA: 0x001AD049 File Offset: 0x001AB249
			protected override float GetIssueEffectAmountInternal(IssueEffect issueEffect)
			{
				if (issueEffect == DefaultIssueEffects.SettlementProsperity)
				{
					return 0.2f;
				}
				if (issueEffect == DefaultIssueEffects.SettlementSecurity)
				{
					return -1f;
				}
				if (issueEffect == DefaultIssueEffects.SettlementLoyalty)
				{
					return -0.2f;
				}
				if (issueEffect == DefaultIssueEffects.IssueOwnerPower)
				{
					return -0.2f;
				}
				return 0f;
			}

			// Token: 0x06005CA4 RID: 23716 RVA: 0x001AD088 File Offset: 0x001AB288
			public override bool DoTroopsSatisfyAlternativeSolution(TroopRoster troopRoster, out TextObject explanation)
			{
				return QuestHelper.CheckRosterForAlternativeSolution(troopRoster, base.GetTotalAlternativeSolutionNeededMenCount(), out explanation, 2, false);
			}

			// Token: 0x06005CA5 RID: 23717 RVA: 0x001AD099 File Offset: 0x001AB299
			public override bool IsTroopTypeNeededByAlternativeSolution(CharacterObject character)
			{
				return character.Tier >= 2;
			}

			// Token: 0x06005CA6 RID: 23718 RVA: 0x001AD0A8 File Offset: 0x001AB2A8
			public override ValueTuple<SkillObject, int> GetAlternativeSolutionSkill(Hero hero)
			{
				int skillValue = hero.GetSkillValue(DefaultSkills.Bow);
				int skillValue2 = hero.GetSkillValue(DefaultSkills.Crossbow);
				int skillValue3 = hero.GetSkillValue(DefaultSkills.Throwing);
				if (skillValue >= skillValue2 && skillValue >= skillValue3)
				{
					return new ValueTuple<SkillObject, int>(DefaultSkills.Bow, 150);
				}
				return new ValueTuple<SkillObject, int>((skillValue2 >= skillValue3) ? DefaultSkills.Crossbow : DefaultSkills.Throwing, 150);
			}

			// Token: 0x06005CA7 RID: 23719 RVA: 0x001AD10B File Offset: 0x001AB30B
			public override bool AlternativeSolutionCondition(out TextObject explanation)
			{
				return QuestHelper.CheckRosterForAlternativeSolution(MobileParty.MainParty.MemberRoster, base.GetTotalAlternativeSolutionNeededMenCount(), out explanation, 2, false);
			}

			// Token: 0x06005CA8 RID: 23720 RVA: 0x001AD125 File Offset: 0x001AB325
			public override IssueBase.IssueFrequency GetFrequency()
			{
				return IssueBase.IssueFrequency.Common;
			}

			// Token: 0x06005CA9 RID: 23721 RVA: 0x001AD128 File Offset: 0x001AB328
			public override bool IssueStayAliveConditions()
			{
				return !this._questVillage.Settlement.IsUnderRaid && !this._questVillage.Settlement.IsRaided && base.IssueOwner.CurrentSettlement.Town.Security <= 90f;
			}

			// Token: 0x06005CAA RID: 23722 RVA: 0x001AD17C File Offset: 0x001AB37C
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
				if (MobileParty.MainParty.MemberRoster.TotalHealthyCount < 15)
				{
					flag |= IssueBase.PreconditionFlags.NotEnoughTroops;
				}
				if (issueGiver.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction))
				{
					flag |= IssueBase.PreconditionFlags.AtWar;
				}
				return flag == IssueBase.PreconditionFlags.None;
			}

			// Token: 0x06005CAB RID: 23723 RVA: 0x001AD1E9 File Offset: 0x001AB3E9
			protected override void OnGameLoad()
			{
			}

			// Token: 0x06005CAC RID: 23724 RVA: 0x001AD1EB File Offset: 0x001AB3EB
			protected override void HourlyTick()
			{
			}

			// Token: 0x06005CAD RID: 23725 RVA: 0x001AD1ED File Offset: 0x001AB3ED
			protected override QuestBase GenerateIssueQuest(string questId)
			{
				return new MerchantArmyOfPoachersIssueBehavior.MerchantArmyOfPoachersIssueQuest(questId, base.IssueOwner, CampaignTime.DaysFromNow(20f), this._questVillage, base.IssueDifficultyMultiplier, this.RewardGold);
			}

			// Token: 0x06005CAE RID: 23726 RVA: 0x001AD217 File Offset: 0x001AB417
			protected override void CompleteIssueWithTimedOutConsequences()
			{
			}

			// Token: 0x17001294 RID: 4756
			// (get) Token: 0x06005CAF RID: 23727 RVA: 0x001AD219 File Offset: 0x001AB419
			protected override int CompanionSkillRewardXP
			{
				get
				{
					return (int)(800f + 1000f * base.IssueDifficultyMultiplier);
				}
			}

			// Token: 0x06005CB0 RID: 23728 RVA: 0x001AD22E File Offset: 0x001AB42E
			protected override void AlternativeSolutionEndWithSuccessConsequence()
			{
				this.RelationshipChangeWithIssueOwner = 5;
				base.IssueOwner.AddPower(30f);
				base.IssueOwner.CurrentSettlement.Town.Prosperity += 50f;
			}

			// Token: 0x06005CB1 RID: 23729 RVA: 0x001AD268 File Offset: 0x001AB468
			protected override void AlternativeSolutionEndWithFailureConsequence()
			{
				this.RelationshipChangeWithIssueOwner = -5;
				base.IssueOwner.AddPower(-50f);
				base.IssueOwner.CurrentSettlement.Town.Prosperity -= 30f;
				base.IssueOwner.CurrentSettlement.Town.Security -= 5f;
				TraitLevelingHelper.OnIssueFailed(base.IssueOwner, new Tuple<TraitObject, int>[]
				{
					new Tuple<TraitObject, int>(DefaultTraits.Honor, -30)
				});
			}

			// Token: 0x04001D44 RID: 7492
			private const int AlternativeSolutionTroopTierRequirement = 2;

			// Token: 0x04001D45 RID: 7493
			private const int CompanionRequiredSkillLevel = 150;

			// Token: 0x04001D46 RID: 7494
			private const int MinimumRequiredMenCount = 15;

			// Token: 0x04001D47 RID: 7495
			private const int IssueDuration = 15;

			// Token: 0x04001D48 RID: 7496
			private const int QuestTimeLimit = 20;

			// Token: 0x04001D49 RID: 7497
			[SaveableField(10)]
			private Village _questVillage;
		}

		// Token: 0x02000736 RID: 1846
		public class MerchantArmyOfPoachersIssueQuest : QuestBase
		{
			// Token: 0x06005CB2 RID: 23730 RVA: 0x001AD2EF File Offset: 0x001AB4EF
			internal static void AutoGeneratedStaticCollectObjectsMerchantArmyOfPoachersIssueQuest(object o, List<object> collectedObjects)
			{
				((MerchantArmyOfPoachersIssueBehavior.MerchantArmyOfPoachersIssueQuest)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
			}

			// Token: 0x06005CB3 RID: 23731 RVA: 0x001AD2FD File Offset: 0x001AB4FD
			protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
			{
				base.AutoGeneratedInstanceCollectObjects(collectedObjects);
				collectedObjects.Add(this._poachersParty);
				collectedObjects.Add(this._questVillage);
			}

			// Token: 0x06005CB4 RID: 23732 RVA: 0x001AD31E File Offset: 0x001AB51E
			internal static object AutoGeneratedGetMemberValue_poachersParty(object o)
			{
				return ((MerchantArmyOfPoachersIssueBehavior.MerchantArmyOfPoachersIssueQuest)o)._poachersParty;
			}

			// Token: 0x06005CB5 RID: 23733 RVA: 0x001AD32B File Offset: 0x001AB52B
			internal static object AutoGeneratedGetMemberValue_questVillage(object o)
			{
				return ((MerchantArmyOfPoachersIssueBehavior.MerchantArmyOfPoachersIssueQuest)o)._questVillage;
			}

			// Token: 0x06005CB6 RID: 23734 RVA: 0x001AD338 File Offset: 0x001AB538
			internal static object AutoGeneratedGetMemberValue_talkedToPoachersBattleWillStart(object o)
			{
				return ((MerchantArmyOfPoachersIssueBehavior.MerchantArmyOfPoachersIssueQuest)o)._talkedToPoachersBattleWillStart;
			}

			// Token: 0x06005CB7 RID: 23735 RVA: 0x001AD34A File Offset: 0x001AB54A
			internal static object AutoGeneratedGetMemberValue_isReadyToBeFinalized(object o)
			{
				return ((MerchantArmyOfPoachersIssueBehavior.MerchantArmyOfPoachersIssueQuest)o)._isReadyToBeFinalized;
			}

			// Token: 0x06005CB8 RID: 23736 RVA: 0x001AD35C File Offset: 0x001AB55C
			internal static object AutoGeneratedGetMemberValue_persuasionTriedOnce(object o)
			{
				return ((MerchantArmyOfPoachersIssueBehavior.MerchantArmyOfPoachersIssueQuest)o)._persuasionTriedOnce;
			}

			// Token: 0x06005CB9 RID: 23737 RVA: 0x001AD36E File Offset: 0x001AB56E
			internal static object AutoGeneratedGetMemberValue_difficultyMultiplier(object o)
			{
				return ((MerchantArmyOfPoachersIssueBehavior.MerchantArmyOfPoachersIssueQuest)o)._difficultyMultiplier;
			}

			// Token: 0x06005CBA RID: 23738 RVA: 0x001AD380 File Offset: 0x001AB580
			internal static object AutoGeneratedGetMemberValue_rewardGold(object o)
			{
				return ((MerchantArmyOfPoachersIssueBehavior.MerchantArmyOfPoachersIssueQuest)o)._rewardGold;
			}

			// Token: 0x17001295 RID: 4757
			// (get) Token: 0x06005CBB RID: 23739 RVA: 0x001AD392 File Offset: 0x001AB592
			public override TextObject Title
			{
				get
				{
					return new TextObject("{=iHFo2kjz}Army of Poachers", null);
				}
			}

			// Token: 0x17001296 RID: 4758
			// (get) Token: 0x06005CBC RID: 23740 RVA: 0x001AD39F File Offset: 0x001AB59F
			public override bool IsRemainingTimeHidden
			{
				get
				{
					return false;
				}
			}

			// Token: 0x17001297 RID: 4759
			// (get) Token: 0x06005CBD RID: 23741 RVA: 0x001AD3A4 File Offset: 0x001AB5A4
			private TextObject QuestStartedLogText
			{
				get
				{
					TextObject textObject = new TextObject("{=fk4ewfQh}{QUEST_GIVER.LINK}, a merchant of {SETTLEMENT}, told you that the poachers {?QUEST_GIVER.GENDER}she{?}he{\\?} hired before are now out of control. {?QUEST_GIVER.GENDER}She{?}He{\\?} asked you to go to {VILLAGE} around midnight and kill the poachers.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					textObject.SetTextVariable("SETTLEMENT", base.QuestGiver.CurrentSettlement.EncyclopediaLinkWithName);
					textObject.SetTextVariable("VILLAGE", this._questVillage.Settlement.EncyclopediaLinkWithName);
					return textObject;
				}
			}

			// Token: 0x17001298 RID: 4760
			// (get) Token: 0x06005CBE RID: 23742 RVA: 0x001AD40E File Offset: 0x001AB60E
			private TextObject QuestCanceledTargetVillageRaidedQuestLogText
			{
				get
				{
					TextObject textObject = new TextObject("{=etYq1Tky}{VILLAGE} was raided and the poachers scattered.", null);
					textObject.SetTextVariable("VILLAGE", this._questVillage.Settlement.EncyclopediaLinkWithName);
					return textObject;
				}
			}

			// Token: 0x17001299 RID: 4761
			// (get) Token: 0x06005CBF RID: 23743 RVA: 0x001AD438 File Offset: 0x001AB638
			private TextObject QuestCanceledWarDeclared
			{
				get
				{
					TextObject textObject = new TextObject("{=vW6kBki9}Your clan is now at war with {QUEST_GIVER.LINK}'s realm. Your agreement with {QUEST_GIVER.LINK} is canceled.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					return textObject;
				}
			}

			// Token: 0x1700129A RID: 4762
			// (get) Token: 0x06005CC0 RID: 23744 RVA: 0x001AD46C File Offset: 0x001AB66C
			private TextObject PlayerDeclaredWarQuestLogText
			{
				get
				{
					TextObject textObject = new TextObject("{=bqeWVVEE}Your actions have started a war with {QUEST_GIVER.LINK}'s faction. {?QUEST_GIVER.GENDER}She{?}He{\\?} cancels your agreement and the quest is a failure.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					return textObject;
				}
			}

			// Token: 0x1700129B RID: 4763
			// (get) Token: 0x06005CC1 RID: 23745 RVA: 0x001AD4A0 File Offset: 0x001AB6A0
			private TextObject QuestFailedAfterTalkingWithProachers
			{
				get
				{
					TextObject textObject = new TextObject("{=PIukmFYA}You decided not to get involved and left the village. You have failed to help {QUEST_GIVER.LINK} as promised.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					return textObject;
				}
			}

			// Token: 0x1700129C RID: 4764
			// (get) Token: 0x06005CC2 RID: 23746 RVA: 0x001AD4D2 File Offset: 0x001AB6D2
			private TextObject QuestSuccessPlayerComesToAnAgreementWithPoachersQuestLogText
			{
				get
				{
					return new TextObject("{=qPfJpwGa}You have persuaded the poachers to leave the district.", null);
				}
			}

			// Token: 0x1700129D RID: 4765
			// (get) Token: 0x06005CC3 RID: 23747 RVA: 0x001AD4E0 File Offset: 0x001AB6E0
			private TextObject QuestFailWithPlayerDefeatedAgainstPoachersQuestLogText
			{
				get
				{
					TextObject textObject = new TextObject("{=p8Kfl5u6}You lost the battle against the poachers and failed to help {QUEST_GIVER.LINK} as promised.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					return textObject;
				}
			}

			// Token: 0x1700129E RID: 4766
			// (get) Token: 0x06005CC4 RID: 23748 RVA: 0x001AD514 File Offset: 0x001AB714
			private TextObject QuestSuccessWithPlayerDefeatedPoachersQuestLogText
			{
				get
				{
					TextObject textObject = new TextObject("{=8gNqLqFl}You have defeated the poachers and helped {QUEST_GIVER.LINK} as promised.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					return textObject;
				}
			}

			// Token: 0x1700129F RID: 4767
			// (get) Token: 0x06005CC5 RID: 23749 RVA: 0x001AD546 File Offset: 0x001AB746
			private TextObject QuestFailedWithTimeOutLogText
			{
				get
				{
					return new TextObject("{=HX7E09XJ}You failed to complete the quest in time.", null);
				}
			}

			// Token: 0x06005CC6 RID: 23750 RVA: 0x001AD553 File Offset: 0x001AB753
			public MerchantArmyOfPoachersIssueQuest(string questId, Hero giverHero, CampaignTime duration, Village questVillage, float difficultyMultiplier, int rewardGold)
				: base(questId, giverHero, duration, rewardGold)
			{
				this._questVillage = questVillage;
				this._talkedToPoachersBattleWillStart = false;
				this._isReadyToBeFinalized = false;
				this._difficultyMultiplier = difficultyMultiplier;
				this._rewardGold = rewardGold;
				this.SetDialogs();
				base.InitializeQuestOnCreation();
			}

			// Token: 0x06005CC7 RID: 23751 RVA: 0x001AD594 File Offset: 0x001AB794
			private bool SetStartDialogOnCondition()
			{
				if (this._poachersParty != null && CharacterObject.OneToOneConversationCharacter == ConversationHelper.GetConversationCharacterPartyLeader(this._poachersParty.Party))
				{
					MBTextManager.SetTextVariable("POACHER_PARTY_START_LINE", "{=j9MBwnWI}Well...Are you working for that merchant in the town ? So it's all fine when the rich folk trade in poached skins, but if we do it, armed men come to hunt us down.", false);
					if (this._persuasionTriedOnce)
					{
						MBTextManager.SetTextVariable("POACHER_PARTY_START_LINE", "{=Nn06TSq9}Anything else to say?", false);
					}
					return true;
				}
				return false;
			}

			// Token: 0x06005CC8 RID: 23752 RVA: 0x001AD5EC File Offset: 0x001AB7EC
			private DialogFlow GetPoacherPartyDialogFlow()
			{
				DialogFlow dialogFlow = DialogFlow.CreateDialogFlow("start", 125).NpcLine("{=!}{POACHER_PARTY_START_LINE}", null, null, null, null).Condition(() => this.SetStartDialogOnCondition())
					.Consequence(delegate
					{
						this._task = this.GetPersuasionTask();
					})
					.BeginPlayerOptions(null, false)
					.PlayerOption("{=afbLOXbb}Maybe we can come to an agreement.", null, null, null)
					.Condition(() => !this._persuasionTriedOnce)
					.Consequence(delegate
					{
						this._persuasionTriedOnce = true;
					})
					.GotoDialogState("start_poachers_persuasion")
					.PlayerOption("{=mvw1ayGt}I'm here to do the job I agreed to do, outlaw. Give up or die.", null, null, null)
					.NpcLine("{=hOVr77fd}You will never see the sunrise again![ib:warrior][if:convo_furious]", null, null, null, null)
					.Consequence(delegate
					{
						this._talkedToPoachersBattleWillStart = true;
					})
					.CloseDialog()
					.PlayerOption("{=VJYEoOAc}Well... You have a point. Go on. We won't bother you any more.", null, null, null)
					.NpcLine("{=wglTyBbx}Thank you, friend. Go in peace.[ib:normal][if:convo_approving]", null, null, null, null)
					.Consequence(delegate
					{
						Campaign.Current.GameMenuManager.SetNextMenu("village");
						Campaign.Current.ConversationManager.ConversationEndOneShot += this.QuestFailedAfterTalkingWithPoachers;
					})
					.CloseDialog()
					.EndPlayerOptions()
					.CloseDialog();
				this.AddPersuasionDialogs(dialogFlow);
				return dialogFlow;
			}

			// Token: 0x06005CC9 RID: 23753 RVA: 0x001AD6EC File Offset: 0x001AB8EC
			private void AddPersuasionDialogs(DialogFlow dialog)
			{
				dialog.AddDialogLine("poachers_persuasion_check_accepted", "start_poachers_persuasion", "poachers_persuasion_start_reservation", "{=6P1ruzsC}Maybe...", new ConversationSentence.OnConditionDelegate(this.persuasion_start_with_poachers_on_condition), new ConversationSentence.OnConsequenceDelegate(this.persuasion_start_with_poachers_on_consequence), this, 100, null, null, null);
				dialog.AddDialogLine("poachers_persuasion_rejected", "poachers_persuasion_start_reservation", "start", "{=!}{FAILED_PERSUASION_LINE}", new ConversationSentence.OnConditionDelegate(this.persuasion_failed_with_poachers_on_condition), new ConversationSentence.OnConsequenceDelegate(this.persuasion_rejected_with_poachers_on_consequence), this, 100, null, null, null);
				dialog.AddDialogLine("poachers_persuasion_attempt", "poachers_persuasion_start_reservation", "poachers_persuasion_select_option", "{=wM77S68a}What's there to discuss?", () => !this.persuasion_failed_with_poachers_on_condition(), null, this, 100, null, null, null);
				dialog.AddDialogLine("poachers_persuasion_success", "poachers_persuasion_start_reservation", "close_window", "{=JQKCPllJ}You've made your point.", new ConversationSentence.OnConditionDelegate(ConversationManager.GetPersuasionProgressSatisfied), new ConversationSentence.OnConsequenceDelegate(this.persuasion_complete_with_poachers_on_consequence), this, 200, null, null, null);
				string id = "poachers_persuasion_select_option_1";
				string inputToken = "poachers_persuasion_select_option";
				string outputToken = "poachers_persuasion_selected_option_response";
				string text = "{=!}{POACHERS_PERSUADE_ATTEMPT_1}";
				ConversationSentence.OnConditionDelegate conditionDelegate = new ConversationSentence.OnConditionDelegate(this.poachers_persuasion_select_option_1_on_condition);
				ConversationSentence.OnConsequenceDelegate consequenceDelegate = new ConversationSentence.OnConsequenceDelegate(this.poachers_persuasion_select_option_1_on_consequence);
				ConversationSentence.OnPersuasionOptionDelegate persuasionOptionDelegate = new ConversationSentence.OnPersuasionOptionDelegate(this.poachers_persuasion_setup_option_1);
				ConversationSentence.OnClickableConditionDelegate clickableConditionDelegate = new ConversationSentence.OnClickableConditionDelegate(this.poachers_persuasion_clickable_option_1_on_condition);
				dialog.AddPlayerLine(id, inputToken, outputToken, text, conditionDelegate, consequenceDelegate, this, 100, clickableConditionDelegate, persuasionOptionDelegate, null, null);
				string id2 = "poachers_persuasion_select_option_2";
				string inputToken2 = "poachers_persuasion_select_option";
				string outputToken2 = "poachers_persuasion_selected_option_response";
				string text2 = "{=!}{POACHERS_PERSUADE_ATTEMPT_2}";
				ConversationSentence.OnConditionDelegate conditionDelegate2 = new ConversationSentence.OnConditionDelegate(this.poachers_persuasion_select_option_2_on_condition);
				ConversationSentence.OnConsequenceDelegate consequenceDelegate2 = new ConversationSentence.OnConsequenceDelegate(this.poachers_persuasion_select_option_2_on_consequence);
				persuasionOptionDelegate = new ConversationSentence.OnPersuasionOptionDelegate(this.poachers_persuasion_setup_option_2);
				clickableConditionDelegate = new ConversationSentence.OnClickableConditionDelegate(this.poachers_persuasion_clickable_option_2_on_condition);
				dialog.AddPlayerLine(id2, inputToken2, outputToken2, text2, conditionDelegate2, consequenceDelegate2, this, 100, clickableConditionDelegate, persuasionOptionDelegate, null, null);
				string id3 = "poachers_persuasion_select_option_3";
				string inputToken3 = "poachers_persuasion_select_option";
				string outputToken3 = "poachers_persuasion_selected_option_response";
				string text3 = "{=!}{POACHERS_PERSUADE_ATTEMPT_3}";
				ConversationSentence.OnConditionDelegate conditionDelegate3 = new ConversationSentence.OnConditionDelegate(this.poachers_persuasion_select_option_3_on_condition);
				ConversationSentence.OnConsequenceDelegate consequenceDelegate3 = new ConversationSentence.OnConsequenceDelegate(this.poachers_persuasion_select_option_3_on_consequence);
				persuasionOptionDelegate = new ConversationSentence.OnPersuasionOptionDelegate(this.poachers_persuasion_setup_option_3);
				clickableConditionDelegate = new ConversationSentence.OnClickableConditionDelegate(this.poachers_persuasion_clickable_option_3_on_condition);
				dialog.AddPlayerLine(id3, inputToken3, outputToken3, text3, conditionDelegate3, consequenceDelegate3, this, 100, clickableConditionDelegate, persuasionOptionDelegate, null, null);
				string id4 = "poachers_persuasion_select_option_4";
				string inputToken4 = "poachers_persuasion_select_option";
				string outputToken4 = "poachers_persuasion_selected_option_response";
				string text4 = "{=!}{POACHERS_PERSUADE_ATTEMPT_4}";
				ConversationSentence.OnConditionDelegate conditionDelegate4 = new ConversationSentence.OnConditionDelegate(this.poachers_persuasion_select_option_4_on_condition);
				ConversationSentence.OnConsequenceDelegate consequenceDelegate4 = new ConversationSentence.OnConsequenceDelegate(this.poachers_persuasion_select_option_4_on_consequence);
				persuasionOptionDelegate = new ConversationSentence.OnPersuasionOptionDelegate(this.poachers_persuasion_setup_option_4);
				clickableConditionDelegate = new ConversationSentence.OnClickableConditionDelegate(this.poachers_persuasion_clickable_option_4_on_condition);
				dialog.AddPlayerLine(id4, inputToken4, outputToken4, text4, conditionDelegate4, consequenceDelegate4, this, 100, clickableConditionDelegate, persuasionOptionDelegate, null, null);
				string id5 = "poachers_persuasion_select_option_5";
				string inputToken5 = "poachers_persuasion_select_option";
				string outputToken5 = "poachers_persuasion_selected_option_response";
				string text5 = "{=!}{POACHERS_PERSUADE_ATTEMPT_5}";
				ConversationSentence.OnConditionDelegate conditionDelegate5 = new ConversationSentence.OnConditionDelegate(this.poachers_persuasion_select_option_5_on_condition);
				ConversationSentence.OnConsequenceDelegate consequenceDelegate5 = new ConversationSentence.OnConsequenceDelegate(this.poachers_persuasion_select_option_5_on_consequence);
				persuasionOptionDelegate = new ConversationSentence.OnPersuasionOptionDelegate(this.poachers_persuasion_setup_option_5);
				clickableConditionDelegate = new ConversationSentence.OnClickableConditionDelegate(this.poachers_persuasion_clickable_option_5_on_condition);
				dialog.AddPlayerLine(id5, inputToken5, outputToken5, text5, conditionDelegate5, consequenceDelegate5, this, 100, clickableConditionDelegate, persuasionOptionDelegate, null, null);
				dialog.AddDialogLine("poachers_persuasion_select_option_reaction", "poachers_persuasion_selected_option_response", "poachers_persuasion_start_reservation", "{=!}{PERSUASION_REACTION}", new ConversationSentence.OnConditionDelegate(this.poachers_persuasion_selected_option_response_on_condition), new ConversationSentence.OnConsequenceDelegate(this.poachers_persuasion_selected_option_response_on_consequence), this, 100, null, null, null);
			}

			// Token: 0x06005CCA RID: 23754 RVA: 0x001AD9B2 File Offset: 0x001ABBB2
			private void persuasion_start_with_poachers_on_consequence()
			{
				ConversationManager.StartPersuasion(2f, 1f, 0f, 2f, 2f, 0f, PersuasionDifficulty.MediumHard);
			}

			// Token: 0x06005CCB RID: 23755 RVA: 0x001AD9D8 File Offset: 0x001ABBD8
			private bool persuasion_start_with_poachers_on_condition()
			{
				return this._poachersParty != null && CharacterObject.OneToOneConversationCharacter == ConversationHelper.GetConversationCharacterPartyLeader(this._poachersParty.Party);
			}

			// Token: 0x06005CCC RID: 23756 RVA: 0x001AD9FC File Offset: 0x001ABBFC
			private PersuasionTask GetPersuasionTask()
			{
				PersuasionTask persuasionTask = new PersuasionTask(0);
				persuasionTask.FinalFailLine = new TextObject("{=l7Jt5tvt}This is how I earn my living, and all your clever talk doesn't make it any different. Leave now!", null);
				persuasionTask.TryLaterLine = new TextObject("{=!}TODO", null);
				persuasionTask.SpokenLine = new TextObject("{=wM77S68a}What's there to discuss?", null);
				PersuasionOptionArgs option = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Calculating, TraitEffect.Positive, PersuasionArgumentStrength.Easy, false, new TextObject("{=cQCs72U7}You're not bad people. You can easily ply your trade somewhere else, somewhere safe.", null), null, false, false, false);
				persuasionTask.AddOptionToTask(option);
				PersuasionOptionArgs option2 = new PersuasionOptionArgs(DefaultSkills.Roguery, DefaultTraits.Valor, TraitEffect.Positive, PersuasionArgumentStrength.ExtremelyHard, true, new TextObject("{=bioyMrUD}You are just a bunch of hunters. You don't stand a chance against us!", null), null, true, false, false);
				persuasionTask.AddOptionToTask(option2);
				PersuasionOptionArgs option3 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Mercy, TraitEffect.Positive, PersuasionArgumentStrength.Normal, false, new TextObject("{=FO1oruNy}You talk about poor folk, but you think the people here like their village turned into a nest of outlaws?", null), null, false, false, false);
				persuasionTask.AddOptionToTask(option3);
				TextObject textObject = new TextObject("{=S0NeQdLp}You had an agreement with {QUEST_GIVER.NAME}. Your word is your bond, no matter which side of the law you're on.", null);
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
				PersuasionOptionArgs option4 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Honor, TraitEffect.Positive, PersuasionArgumentStrength.Normal, false, textObject, null, false, false, false);
				persuasionTask.AddOptionToTask(option4);
				TextObject line = new TextObject("{=brW4pjPQ}Flee while you can. An army is already on its way here to hang you all.", null);
				PersuasionOptionArgs option5 = new PersuasionOptionArgs(DefaultSkills.Roguery, DefaultTraits.Calculating, TraitEffect.Positive, PersuasionArgumentStrength.Hard, true, line, null, false, false, false);
				persuasionTask.AddOptionToTask(option5);
				return persuasionTask;
			}

			// Token: 0x06005CCD RID: 23757 RVA: 0x001ADB34 File Offset: 0x001ABD34
			private bool poachers_persuasion_selected_option_response_on_condition()
			{
				PersuasionOptionResult item = ConversationManager.GetPersuasionChosenOptions().Last<Tuple<PersuasionOptionArgs, PersuasionOptionResult>>().Item2;
				MBTextManager.SetTextVariable("PERSUASION_REACTION", PersuasionHelper.GetDefaultPersuasionOptionReaction(item), false);
				if (item == PersuasionOptionResult.CriticalFailure)
				{
					this._task.BlockAllOptions();
				}
				return true;
			}

			// Token: 0x06005CCE RID: 23758 RVA: 0x001ADB74 File Offset: 0x001ABD74
			private void poachers_persuasion_selected_option_response_on_consequence()
			{
				Tuple<PersuasionOptionArgs, PersuasionOptionResult> tuple = ConversationManager.GetPersuasionChosenOptions().Last<Tuple<PersuasionOptionArgs, PersuasionOptionResult>>();
				float difficulty = Campaign.Current.Models.PersuasionModel.GetDifficulty(PersuasionDifficulty.MediumHard);
				float moveToNextStageChance;
				float blockRandomOptionChance;
				Campaign.Current.Models.PersuasionModel.GetEffectChances(tuple.Item1, out moveToNextStageChance, out blockRandomOptionChance, difficulty);
				this._task.ApplyEffects(moveToNextStageChance, blockRandomOptionChance);
			}

			// Token: 0x06005CCF RID: 23759 RVA: 0x001ADBD0 File Offset: 0x001ABDD0
			private bool poachers_persuasion_select_option_1_on_condition()
			{
				if (this._task.Options.Count > 0)
				{
					TextObject textObject = new TextObject("{=bSo9hKwr}{PERSUASION_OPTION_LINE} {SUCCESS_CHANCE}", null);
					textObject.SetTextVariable("SUCCESS_CHANCE", PersuasionHelper.ShowSuccess(this._task.Options.ElementAt(0), false));
					textObject.SetTextVariable("PERSUASION_OPTION_LINE", this._task.Options.ElementAt(0).Line);
					MBTextManager.SetTextVariable("POACHERS_PERSUADE_ATTEMPT_1", textObject, false);
					return true;
				}
				return false;
			}

			// Token: 0x06005CD0 RID: 23760 RVA: 0x001ADC50 File Offset: 0x001ABE50
			private bool poachers_persuasion_select_option_2_on_condition()
			{
				if (this._task.Options.Count > 1)
				{
					TextObject textObject = new TextObject("{=bSo9hKwr}{PERSUASION_OPTION_LINE} {SUCCESS_CHANCE}", null);
					textObject.SetTextVariable("SUCCESS_CHANCE", PersuasionHelper.ShowSuccess(this._task.Options.ElementAt(1), false));
					textObject.SetTextVariable("PERSUASION_OPTION_LINE", this._task.Options.ElementAt(1).Line);
					MBTextManager.SetTextVariable("POACHERS_PERSUADE_ATTEMPT_2", textObject, false);
					return true;
				}
				return false;
			}

			// Token: 0x06005CD1 RID: 23761 RVA: 0x001ADCD0 File Offset: 0x001ABED0
			private bool poachers_persuasion_select_option_3_on_condition()
			{
				if (this._task.Options.Count > 2)
				{
					TextObject textObject = new TextObject("{=bSo9hKwr}{PERSUASION_OPTION_LINE} {SUCCESS_CHANCE}", null);
					textObject.SetTextVariable("SUCCESS_CHANCE", PersuasionHelper.ShowSuccess(this._task.Options.ElementAt(2), false));
					textObject.SetTextVariable("PERSUASION_OPTION_LINE", this._task.Options.ElementAt(2).Line);
					MBTextManager.SetTextVariable("POACHERS_PERSUADE_ATTEMPT_3", textObject, false);
					return true;
				}
				return false;
			}

			// Token: 0x06005CD2 RID: 23762 RVA: 0x001ADD50 File Offset: 0x001ABF50
			private bool poachers_persuasion_select_option_4_on_condition()
			{
				if (this._task.Options.Count > 3)
				{
					TextObject textObject = new TextObject("{=bSo9hKwr}{PERSUASION_OPTION_LINE} {SUCCESS_CHANCE}", null);
					textObject.SetTextVariable("SUCCESS_CHANCE", PersuasionHelper.ShowSuccess(this._task.Options.ElementAt(3), false));
					textObject.SetTextVariable("PERSUASION_OPTION_LINE", this._task.Options.ElementAt(3).Line);
					MBTextManager.SetTextVariable("POACHERS_PERSUADE_ATTEMPT_4", textObject, false);
					return true;
				}
				return false;
			}

			// Token: 0x06005CD3 RID: 23763 RVA: 0x001ADDD0 File Offset: 0x001ABFD0
			private bool poachers_persuasion_select_option_5_on_condition()
			{
				if (this._task.Options.Count > 4)
				{
					TextObject textObject = new TextObject("{=bSo9hKwr}{PERSUASION_OPTION_LINE} {SUCCESS_CHANCE}", null);
					textObject.SetTextVariable("SUCCESS_CHANCE", PersuasionHelper.ShowSuccess(this._task.Options.ElementAt(4), false));
					textObject.SetTextVariable("PERSUASION_OPTION_LINE", this._task.Options.ElementAt(4).Line);
					MBTextManager.SetTextVariable("POACHERS_PERSUADE_ATTEMPT_5", textObject, false);
					return true;
				}
				return false;
			}

			// Token: 0x06005CD4 RID: 23764 RVA: 0x001ADE50 File Offset: 0x001AC050
			private void poachers_persuasion_select_option_1_on_consequence()
			{
				if (this._task.Options.Count > 0)
				{
					this._task.Options[0].BlockTheOption(true);
				}
			}

			// Token: 0x06005CD5 RID: 23765 RVA: 0x001ADE7C File Offset: 0x001AC07C
			private void poachers_persuasion_select_option_2_on_consequence()
			{
				if (this._task.Options.Count > 1)
				{
					this._task.Options[1].BlockTheOption(true);
				}
			}

			// Token: 0x06005CD6 RID: 23766 RVA: 0x001ADEA8 File Offset: 0x001AC0A8
			private void poachers_persuasion_select_option_3_on_consequence()
			{
				if (this._task.Options.Count > 2)
				{
					this._task.Options[2].BlockTheOption(true);
				}
			}

			// Token: 0x06005CD7 RID: 23767 RVA: 0x001ADED4 File Offset: 0x001AC0D4
			private void poachers_persuasion_select_option_4_on_consequence()
			{
				if (this._task.Options.Count > 3)
				{
					this._task.Options[3].BlockTheOption(true);
				}
			}

			// Token: 0x06005CD8 RID: 23768 RVA: 0x001ADF00 File Offset: 0x001AC100
			private void poachers_persuasion_select_option_5_on_consequence()
			{
				if (this._task.Options.Count > 4)
				{
					this._task.Options[4].BlockTheOption(true);
				}
			}

			// Token: 0x06005CD9 RID: 23769 RVA: 0x001ADF2C File Offset: 0x001AC12C
			private bool persuasion_failed_with_poachers_on_condition()
			{
				if (this._task.Options.All((PersuasionOptionArgs x) => x.IsBlocked) && !ConversationManager.GetPersuasionProgressSatisfied())
				{
					MBTextManager.SetTextVariable("FAILED_PERSUASION_LINE", this._task.FinalFailLine, false);
					return true;
				}
				return false;
			}

			// Token: 0x06005CDA RID: 23770 RVA: 0x001ADF8A File Offset: 0x001AC18A
			private PersuasionOptionArgs poachers_persuasion_setup_option_1()
			{
				return this._task.Options.ElementAt(0);
			}

			// Token: 0x06005CDB RID: 23771 RVA: 0x001ADF9D File Offset: 0x001AC19D
			private PersuasionOptionArgs poachers_persuasion_setup_option_2()
			{
				return this._task.Options.ElementAt(1);
			}

			// Token: 0x06005CDC RID: 23772 RVA: 0x001ADFB0 File Offset: 0x001AC1B0
			private PersuasionOptionArgs poachers_persuasion_setup_option_3()
			{
				return this._task.Options.ElementAt(2);
			}

			// Token: 0x06005CDD RID: 23773 RVA: 0x001ADFC3 File Offset: 0x001AC1C3
			private PersuasionOptionArgs poachers_persuasion_setup_option_4()
			{
				return this._task.Options.ElementAt(3);
			}

			// Token: 0x06005CDE RID: 23774 RVA: 0x001ADFD6 File Offset: 0x001AC1D6
			private PersuasionOptionArgs poachers_persuasion_setup_option_5()
			{
				return this._task.Options.ElementAt(4);
			}

			// Token: 0x06005CDF RID: 23775 RVA: 0x001ADFEC File Offset: 0x001AC1EC
			private bool poachers_persuasion_clickable_option_1_on_condition(out TextObject hintText)
			{
				hintText = new TextObject("{=9ACJsI6S}Blocked", null);
				if (this._task.Options.Count > 0)
				{
					hintText = (this._task.Options.ElementAt(0).IsBlocked ? hintText : null);
					return !this._task.Options.ElementAt(0).IsBlocked;
				}
				return false;
			}

			// Token: 0x06005CE0 RID: 23776 RVA: 0x001AE054 File Offset: 0x001AC254
			private bool poachers_persuasion_clickable_option_2_on_condition(out TextObject hintText)
			{
				hintText = new TextObject("{=9ACJsI6S}Blocked", null);
				if (this._task.Options.Count > 1)
				{
					hintText = (this._task.Options.ElementAt(1).IsBlocked ? hintText : null);
					return !this._task.Options.ElementAt(1).IsBlocked;
				}
				return false;
			}

			// Token: 0x06005CE1 RID: 23777 RVA: 0x001AE0BC File Offset: 0x001AC2BC
			private bool poachers_persuasion_clickable_option_3_on_condition(out TextObject hintText)
			{
				hintText = new TextObject("{=9ACJsI6S}Blocked", null);
				if (this._task.Options.Count > 2)
				{
					hintText = (this._task.Options.ElementAt(2).IsBlocked ? hintText : null);
					return !this._task.Options.ElementAt(2).IsBlocked;
				}
				return false;
			}

			// Token: 0x06005CE2 RID: 23778 RVA: 0x001AE124 File Offset: 0x001AC324
			private bool poachers_persuasion_clickable_option_4_on_condition(out TextObject hintText)
			{
				hintText = new TextObject("{=9ACJsI6S}Blocked", null);
				if (this._task.Options.Count > 3)
				{
					hintText = (this._task.Options.ElementAt(3).IsBlocked ? hintText : null);
					return !this._task.Options.ElementAt(3).IsBlocked;
				}
				return false;
			}

			// Token: 0x06005CE3 RID: 23779 RVA: 0x001AE18C File Offset: 0x001AC38C
			private bool poachers_persuasion_clickable_option_5_on_condition(out TextObject hintText)
			{
				hintText = new TextObject("{=9ACJsI6S}Blocked", null);
				if (this._task.Options.Count > 4)
				{
					hintText = (this._task.Options.ElementAt(4).IsBlocked ? hintText : null);
					return !this._task.Options.ElementAt(4).IsBlocked;
				}
				return false;
			}

			// Token: 0x06005CE4 RID: 23780 RVA: 0x001AE1F3 File Offset: 0x001AC3F3
			private void persuasion_rejected_with_poachers_on_consequence()
			{
				PlayerEncounter.LeaveEncounter = false;
				ConversationManager.EndPersuasion();
			}

			// Token: 0x06005CE5 RID: 23781 RVA: 0x001AE200 File Offset: 0x001AC400
			private void persuasion_complete_with_poachers_on_consequence()
			{
				PlayerEncounter.LeaveEncounter = true;
				ConversationManager.EndPersuasion();
				Campaign.Current.GameMenuManager.SetNextMenu("village");
				Campaign.Current.ConversationManager.ConversationEndOneShot += this.QuestSuccessPlayerComesToAnAgreementWithPoachers;
			}

			// Token: 0x06005CE6 RID: 23782 RVA: 0x001AE23C File Offset: 0x001AC43C
			internal void StartQuestBattle()
			{
				PlayerEncounter.RestartPlayerEncounter(PartyBase.MainParty, this._poachersParty.Party, false);
				PlayerEncounter.StartBattle();
				PlayerEncounter.Update();
				this._talkedToPoachersBattleWillStart = false;
				MapEvent.PlayerMapEvent.AttackerSide.RemoveNearbyPartiesFromPlayerMapEvent();
				MapEvent.PlayerMapEvent.DefenderSide.RemoveNearbyPartiesFromPlayerMapEvent();
				GameMenu.ActivateGameMenu("army_of_poachers_village");
				CampaignMission.OpenBattleMission(this._questVillage.Settlement.LocationComplex.GetScene("village_center", 1), false);
				this._isReadyToBeFinalized = true;
			}

			// Token: 0x06005CE7 RID: 23783 RVA: 0x001AE2C2 File Offset: 0x001AC4C2
			private bool DialogCondition()
			{
				return Hero.OneToOneConversationHero == base.QuestGiver;
			}

			// Token: 0x06005CE8 RID: 23784 RVA: 0x001AE2D4 File Offset: 0x001AC4D4
			protected override void SetDialogs()
			{
				this.OfferDialogFlow = DialogFlow.CreateDialogFlow("issue_classic_quest_start", 100).NpcLine(new TextObject("{=IefM6uAy}Thank you. You'll be paid well. Also you can keep their illegally obtained leather.[ib:normal2][if:convo_bemused]", null), null, null, null, null).Condition(new ConversationSentence.OnConditionDelegate(this.DialogCondition))
					.NpcLine(new TextObject("{=NC2VGafO}They skin their beasts in the woods, then go into the village after midnight to stash the hides. The villagers are terrified of them, I believe. If you go into the village late at night, you should be able to track them down.[ib:normal][if:convo_thinking]", null), null, null, null, null)
					.NpcLine(new TextObject("{=3pkVKMnA}Most poachers would probably run if they were surprised by armed men. But these ones are bold and desperate. Be ready for a fight.[ib:normal2][if:convo_undecided_closed]", null), null, null, null, null)
					.Consequence(new ConversationSentence.OnConsequenceDelegate(this.QuestAcceptedConsequences))
					.CloseDialog();
				this.DiscussDialogFlow = DialogFlow.CreateDialogFlow("quest_discuss", 100).NpcLine(new TextObject("{=QNV1b5s5}Are those poachers still in business?[ib:normal2][if:convo_undecided_open]", null), null, null, null, null).Condition(new ConversationSentence.OnConditionDelegate(this.DialogCondition))
					.BeginPlayerOptions(null, false)
					.PlayerOption(new TextObject("{=JhJBBWab}They will be gone soon.", null), null, null, null)
					.NpcLine(new TextObject("{=gjGb044I}I hope they will be...[ib:normal2][if:convo_dismayed]", null), null, null, null, null)
					.CloseDialog()
					.PlayerOption(new TextObject("{=Gu3jF88V}Any night battle can easily go wrong. I need more time to prepare.", null), null, null, null)
					.NpcLine(new TextObject("{=2EiC1YyZ}Well, if they get wind of what you're up to, things could go very wrong for me. Do be quick.[ib:nervous2][if:convo_dismayed]", null), null, null, null, null)
					.CloseDialog()
					.EndPlayerOptions();
				this.QuestCharacterDialogFlow = this.GetPoacherPartyDialogFlow();
			}

			// Token: 0x06005CE9 RID: 23785 RVA: 0x001AE400 File Offset: 0x001AC600
			internal void CreatePoachersParty()
			{
				Hideout closestHideout = SettlementHelper.FindNearestHideoutToMobileParty(MobileParty.MainParty, MobileParty.NavigationType.Default, (Settlement x) => x.IsActive);
				Clan clan = Clan.BanditFactions.FirstOrDefaultQ((Clan t) => t.Culture == closestHideout.Settlement.Culture);
				this._poachersParty = CustomPartyComponent.CreateCustomPartyWithTroopRoster(this._questVillage.Settlement.GatePosition, 1f, null, new TextObject("{=WQa1R55u}Poachers Party", null), clan, TroopRoster.CreateDummyTroopRoster(), TroopRoster.CreateDummyTroopRoster(), null, "", "", 0f, false);
				ItemObject @object = MBObjectManager.Instance.GetObject<ItemObject>("leather");
				int num = MathF.Ceiling(this._difficultyMultiplier * 5f) + MBRandom.RandomInt(0, 2);
				this._poachersParty.ItemRoster.AddToCounts(@object, num * 2);
				CharacterObject character = CharacterObject.All.FirstOrDefault((CharacterObject t) => t.StringId == "poacher");
				int count = 10 + MathF.Ceiling(40f * this._difficultyMultiplier);
				this._poachersParty.MemberRoster.AddToCounts(character, count, false, 0, 0, true, -1);
				this._poachersParty.SetPartyUsedByQuest(true);
				this._poachersParty.Ai.DisableAi();
				EnterSettlementAction.ApplyForParty(this._poachersParty, Settlement.CurrentSettlement);
			}

			// Token: 0x06005CEA RID: 23786 RVA: 0x001AE568 File Offset: 0x001AC768
			private void QuestAcceptedConsequences()
			{
				base.StartQuest();
				base.AddLog(this.QuestStartedLogText, false);
				base.AddTrackedObject(this._questVillage.Settlement);
			}

			// Token: 0x06005CEB RID: 23787 RVA: 0x001AE590 File Offset: 0x001AC790
			internal void QuestFailedAfterTalkingWithPoachers()
			{
				base.AddLog(this.QuestFailedAfterTalkingWithProachers, false);
				TraitLevelingHelper.OnIssueFailed(base.QuestGiver, new Tuple<TraitObject, int>[]
				{
					new Tuple<TraitObject, int>(DefaultTraits.Honor, -50),
					new Tuple<TraitObject, int>(DefaultTraits.Mercy, 20)
				});
				this.RelationshipChangeWithQuestGiver = -5;
				base.QuestGiver.AddPower(-50f);
				base.QuestGiver.CurrentSettlement.Town.Security -= 5f;
				base.QuestGiver.CurrentSettlement.Town.Prosperity -= 30f;
				base.CompleteQuestWithFail(null);
			}

			// Token: 0x06005CEC RID: 23788 RVA: 0x001AE63C File Offset: 0x001AC83C
			internal void QuestSuccessPlayerComesToAnAgreementWithPoachers()
			{
				base.AddLog(this.QuestSuccessPlayerComesToAnAgreementWithPoachersQuestLogText, false);
				TraitLevelingHelper.OnIssueSolvedThroughQuest(base.QuestGiver, new Tuple<TraitObject, int>[]
				{
					new Tuple<TraitObject, int>(DefaultTraits.Honor, 10),
					new Tuple<TraitObject, int>(DefaultTraits.Mercy, 50)
				});
				GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, this._rewardGold, false);
				this.RelationshipChangeWithQuestGiver = 5;
				GainRenownAction.Apply(Hero.MainHero, 1f, false);
				base.QuestGiver.AddPower(30f);
				base.QuestGiver.CurrentSettlement.Town.Security -= 5f;
				base.QuestGiver.CurrentSettlement.Town.Prosperity += 50f;
				base.CompleteQuestWithSuccess();
			}

			// Token: 0x06005CED RID: 23789 RVA: 0x001AE708 File Offset: 0x001AC908
			internal void QuestFailWithPlayerDefeatedAgainstPoachers()
			{
				base.AddLog(this.QuestFailWithPlayerDefeatedAgainstPoachersQuestLogText, false);
				TraitLevelingHelper.OnIssueSolvedThroughQuest(base.QuestGiver, new Tuple<TraitObject, int>[]
				{
					new Tuple<TraitObject, int>(DefaultTraits.Honor, -30)
				});
				this.RelationshipChangeWithQuestGiver = -5;
				base.QuestGiver.AddPower(-50f);
				base.QuestGiver.CurrentSettlement.Town.Security -= 5f;
				base.QuestGiver.CurrentSettlement.Town.Prosperity -= 30f;
				base.CompleteQuestWithFail(null);
			}

			// Token: 0x06005CEE RID: 23790 RVA: 0x001AE7A4 File Offset: 0x001AC9A4
			internal void QuestSuccessWithPlayerDefeatedPoachers()
			{
				base.AddLog(this.QuestSuccessWithPlayerDefeatedPoachersQuestLogText, false);
				TraitLevelingHelper.OnIssueSolvedThroughQuest(base.QuestGiver, new Tuple<TraitObject, int>[]
				{
					new Tuple<TraitObject, int>(DefaultTraits.Honor, 50)
				});
				GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, this._rewardGold, false);
				this.RelationshipChangeWithQuestGiver = 5;
				base.QuestGiver.AddPower(30f);
				base.QuestGiver.CurrentSettlement.Town.Prosperity += 50f;
				base.CompleteQuestWithSuccess();
			}

			// Token: 0x06005CEF RID: 23791 RVA: 0x001AE830 File Offset: 0x001ACA30
			protected override void OnTimedOut()
			{
				base.AddLog(this.QuestFailedWithTimeOutLogText, false);
				TraitLevelingHelper.OnIssueSolvedThroughQuest(base.QuestGiver, new Tuple<TraitObject, int>[]
				{
					new Tuple<TraitObject, int>(DefaultTraits.Honor, -30)
				});
				this.RelationshipChangeWithQuestGiver = -5;
				base.QuestGiver.AddPower(-50f);
				base.QuestGiver.CurrentSettlement.Town.Prosperity -= 30f;
				base.QuestGiver.CurrentSettlement.Town.Security -= 5f;
			}

			// Token: 0x06005CF0 RID: 23792 RVA: 0x001AE8C5 File Offset: 0x001ACAC5
			private void QuestCanceledTargetVillageRaided()
			{
				base.AddLog(this.QuestCanceledTargetVillageRaidedQuestLogText, false);
				base.CompleteQuestWithFail(null);
			}

			// Token: 0x06005CF1 RID: 23793 RVA: 0x001AE8DC File Offset: 0x001ACADC
			protected override void RegisterEvents()
			{
				CampaignEvents.MapEventEnded.AddNonSerializedListener(this, new Action<MapEvent>(this.MapEventCheck));
				CampaignEvents.MapEventStarted.AddNonSerializedListener(this, new Action<MapEvent, PartyBase, PartyBase>(this.MapEventStarted));
				CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, new Action<MenuCallbackArgs>(this.GameMenuOpened));
				CampaignEvents.WarDeclared.AddNonSerializedListener(this, new Action<IFaction, IFaction, DeclareWarAction.DeclareWarDetail>(this.OnWarDeclared));
				CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, new Action<Clan, Kingdom, Kingdom, ChangeKingdomAction.ChangeKingdomActionDetail, bool>(this.OnClanChangedKingdom));
				CampaignEvents.CanHeroBecomePrisonerEvent.AddNonSerializedListener(this, new ReferenceAction<Hero, bool>(this.OnCanHeroBecomePrisonerInfoIsRequested));
			}

			// Token: 0x06005CF2 RID: 23794 RVA: 0x001AE973 File Offset: 0x001ACB73
			private void OnCanHeroBecomePrisonerInfoIsRequested(Hero hero, ref bool result)
			{
				if (hero == Hero.MainHero && this._isReadyToBeFinalized)
				{
					result = false;
				}
			}

			// Token: 0x06005CF3 RID: 23795 RVA: 0x001AE988 File Offset: 0x001ACB88
			protected override void HourlyTick()
			{
				if (PlayerEncounter.Current != null && PlayerEncounter.Current.IsPlayerWaiting && PlayerEncounter.EncounterSettlement == this._questVillage.Settlement && CampaignTime.Now.IsNightTime && !this._isReadyToBeFinalized && base.IsOngoing)
				{
					EnterSettlementAction.ApplyForParty(MobileParty.MainParty, this._questVillage.Settlement);
					GameMenu.SwitchToMenu("army_of_poachers_village");
				}
			}

			// Token: 0x06005CF4 RID: 23796 RVA: 0x001AE9F8 File Offset: 0x001ACBF8
			private void GameMenuOpened(MenuCallbackArgs obj)
			{
				if (obj.MenuContext.GameMenu.StringId == "village" && CampaignTime.Now.IsNightTime && Settlement.CurrentSettlement == this._questVillage.Settlement && !this._isReadyToBeFinalized)
				{
					GameMenu.SwitchToMenu("army_of_poachers_village");
				}
				if (obj.MenuContext.GameMenu.StringId == "army_of_poachers_village" && this._isReadyToBeFinalized && MapEvent.PlayerMapEvent != null && MapEvent.PlayerMapEvent.HasWinner && this._poachersParty != null)
				{
					this._poachersParty.IsVisible = false;
				}
			}

			// Token: 0x06005CF5 RID: 23797 RVA: 0x001AEA9F File Offset: 0x001ACC9F
			private void MapEventStarted(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
			{
				this.MapEventCheck(mapEvent);
				if (QuestHelper.CheckMinorMajorCoercion(this, mapEvent, attackerParty))
				{
					QuestHelper.ApplyGenericMinorMajorCoercionConsequences(this, mapEvent);
				}
			}

			// Token: 0x06005CF6 RID: 23798 RVA: 0x001AEAB9 File Offset: 0x001ACCB9
			private void MapEventCheck(MapEvent mapEvent)
			{
				if (mapEvent.IsRaid && mapEvent.MapEventSettlement == this._questVillage.Settlement)
				{
					this.QuestCanceledTargetVillageRaided();
				}
			}

			// Token: 0x06005CF7 RID: 23799 RVA: 0x001AEADC File Offset: 0x001ACCDC
			private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
			{
				if (base.QuestGiver.CurrentSettlement.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction))
				{
					base.CompleteQuestWithCancel(this.QuestCanceledWarDeclared);
				}
			}

			// Token: 0x06005CF8 RID: 23800 RVA: 0x001AEB0B File Offset: 0x001ACD0B
			private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
			{
				QuestHelper.CheckWarDeclarationAndFailOrCancelTheQuest(this, faction1, faction2, detail, this.PlayerDeclaredWarQuestLogText, this.QuestCanceledWarDeclared, false);
			}

			// Token: 0x06005CF9 RID: 23801 RVA: 0x001AEB24 File Offset: 0x001ACD24
			protected override void OnFinalize()
			{
				if (this._poachersParty != null && this._poachersParty.IsActive)
				{
					DestroyPartyAction.Apply(null, this._poachersParty);
				}
				if (Hero.MainHero.IsPrisoner)
				{
					EndCaptivityAction.ApplyByPeace(Hero.MainHero, null);
				}
				if (Campaign.Current.CurrentMenuContext != null && Campaign.Current.CurrentMenuContext.GameMenu.StringId == "army_of_poachers_village")
				{
					PlayerEncounter.Finish(true);
				}
			}

			// Token: 0x06005CFA RID: 23802 RVA: 0x001AEB9B File Offset: 0x001ACD9B
			protected override void InitializeQuestOnGameLoad()
			{
				this.SetDialogs();
			}

			// Token: 0x04001D4A RID: 7498
			[SaveableField(10)]
			internal MobileParty _poachersParty;

			// Token: 0x04001D4B RID: 7499
			[SaveableField(20)]
			internal Village _questVillage;

			// Token: 0x04001D4C RID: 7500
			[SaveableField(30)]
			internal bool _talkedToPoachersBattleWillStart;

			// Token: 0x04001D4D RID: 7501
			[SaveableField(40)]
			internal bool _isReadyToBeFinalized;

			// Token: 0x04001D4E RID: 7502
			[SaveableField(50)]
			internal bool _persuasionTriedOnce;

			// Token: 0x04001D4F RID: 7503
			[SaveableField(60)]
			internal float _difficultyMultiplier;

			// Token: 0x04001D50 RID: 7504
			[SaveableField(70)]
			internal int _rewardGold;

			// Token: 0x04001D51 RID: 7505
			private PersuasionTask _task;

			// Token: 0x04001D52 RID: 7506
			private const PersuasionDifficulty Difficulty = PersuasionDifficulty.MediumHard;
		}

		// Token: 0x02000737 RID: 1847
		public class MerchantArmyOfPoachersIssueBehaviorTypeDefiner : SaveableTypeDefiner
		{
			// Token: 0x06005D02 RID: 23810 RVA: 0x001AEC12 File Offset: 0x001ACE12
			public MerchantArmyOfPoachersIssueBehaviorTypeDefiner()
				: base(800000)
			{
			}

			// Token: 0x06005D03 RID: 23811 RVA: 0x001AEC1F File Offset: 0x001ACE1F
			protected override void DefineClassTypes()
			{
				base.AddClassDefinition(typeof(MerchantArmyOfPoachersIssueBehavior.MerchantArmyOfPoachersIssue), 1, null);
				base.AddClassDefinition(typeof(MerchantArmyOfPoachersIssueBehavior.MerchantArmyOfPoachersIssueQuest), 2, null);
			}
		}
	}
}
