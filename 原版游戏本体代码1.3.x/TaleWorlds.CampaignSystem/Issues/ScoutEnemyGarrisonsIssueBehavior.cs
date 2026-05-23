using System;
using System.Collections.Generic;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace TaleWorlds.CampaignSystem.Issues
{
	// Token: 0x02000376 RID: 886
	public class ScoutEnemyGarrisonsIssueBehavior : CampaignBehaviorBase
	{
		// Token: 0x060033AE RID: 13230 RVA: 0x000D4695 File Offset: 0x000D2895
		public override void RegisterEvents()
		{
			CampaignEvents.OnCheckForIssueEvent.AddNonSerializedListener(this, new Action<Hero>(this.OnCheckForIssue));
		}

		// Token: 0x060033AF RID: 13231 RVA: 0x000D46B0 File Offset: 0x000D28B0
		public void OnCheckForIssue(Hero hero)
		{
			List<Settlement> relatedObject;
			if (this.ConditionsHold(hero, out relatedObject))
			{
				Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(new PotentialIssueData.StartIssueDelegate(this.OnStartIssue), typeof(ScoutEnemyGarrisonsIssueBehavior.ScoutEnemyGarrisonsIssue), IssueBase.IssueFrequency.VeryCommon, relatedObject));
				return;
			}
			Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(typeof(ScoutEnemyGarrisonsIssueBehavior.ScoutEnemyGarrisonsIssue), IssueBase.IssueFrequency.VeryCommon));
		}

		// Token: 0x060033B0 RID: 13232 RVA: 0x000D4718 File Offset: 0x000D2918
		private bool ConditionsHold(Hero issueGiver, out List<Settlement> settlements)
		{
			settlements = new List<Settlement>();
			if (issueGiver.MapFaction.IsKingdomFaction && issueGiver.IsFactionLeader && !issueGiver.IsMinorFactionHero && !issueGiver.IsPrisoner && !issueGiver.IsFugitive)
			{
				Kingdom randomElementWithPredicate = Kingdom.All.GetRandomElementWithPredicate((Kingdom x) => x.IsAtWarWith(issueGiver.MapFaction));
				MapDistanceModel mapDistanceModel = Campaign.Current.Models.MapDistanceModel;
				IMapPoint mapPoint = issueGiver.GetMapPoint();
				float num = Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(MobileParty.NavigationType.All) * 5f;
				if (randomElementWithPredicate != null && mapPoint != null)
				{
					ValueTuple<Settlement, float>[] array = new ValueTuple<Settlement, float>[3];
					foreach (Settlement settlement in randomElementWithPredicate.Settlements)
					{
						if (ScoutEnemyGarrisonsIssueBehavior.SuitableSettlementCondition(settlement, issueGiver))
						{
							float num2 = float.MaxValue;
							if (issueGiver.CurrentSettlement != null)
							{
								num2 = mapDistanceModel.GetDistance(issueGiver.CurrentSettlement, settlement, false, false, MobileParty.NavigationType.All);
							}
							else if (issueGiver.PartyBelongedTo != null)
							{
								float num3;
								num2 = mapDistanceModel.GetDistance(issueGiver.PartyBelongedTo, settlement, false, MobileParty.NavigationType.All, out num3);
							}
							else if (issueGiver.PartyBelongedToAsPrisoner != null)
							{
								float num3;
								num2 = mapDistanceModel.GetDistance(issueGiver.PartyBelongedToAsPrisoner.MobileParty, settlement, false, MobileParty.NavigationType.All, out num3);
							}
							if (num2 <= num)
							{
								if (array[2].Item1 == null || array[2].Item2 > num2)
								{
									array[2] = new ValueTuple<Settlement, float>(settlement, num2);
								}
								int num4 = array.Length - 1;
								while (num4 > 0 && (array[num4 - 1].Item1 == null || array[num4].Item2 < array[num4 - 1].Item2))
								{
									ValueTuple<Settlement, float> valueTuple = array[num4 - 1];
									array[num4 - 1] = array[num4];
									array[num4] = valueTuple;
									num4--;
								}
							}
						}
					}
					if (array[2].Item1 != null)
					{
						settlements.Add(array[2].Item1);
						settlements.Add(array[1].Item1);
						settlements.Add(array[0].Item1);
						return true;
					}
				}
			}
			return false;
		}

		// Token: 0x060033B1 RID: 13233 RVA: 0x000D49CC File Offset: 0x000D2BCC
		private IssueBase OnStartIssue(in PotentialIssueData pid, Hero issueOwner)
		{
			PotentialIssueData potentialIssueData = pid;
			return new ScoutEnemyGarrisonsIssueBehavior.ScoutEnemyGarrisonsIssue(issueOwner, potentialIssueData.RelatedObject as List<Settlement>);
		}

		// Token: 0x060033B2 RID: 13234 RVA: 0x000D49F2 File Offset: 0x000D2BF2
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x060033B3 RID: 13235 RVA: 0x000D49F4 File Offset: 0x000D2BF4
		private static bool SuitableSettlementCondition(Settlement settlement, Hero issueGiver)
		{
			return settlement.IsFortification && settlement.MapFaction.IsAtWarWith(issueGiver.MapFaction) && (!settlement.IsUnderSiege || settlement.SiegeEvent.BesiegerCamp.MapFaction != Hero.MainHero.MapFaction);
		}

		// Token: 0x04000EB3 RID: 3763
		private const IssueBase.IssueFrequency ScoutEnemyGarrisonsIssueFrequency = IssueBase.IssueFrequency.VeryCommon;

		// Token: 0x04000EB4 RID: 3764
		private const int QuestDurationInDays = 30;

		// Token: 0x0200074A RID: 1866
		public class ScoutEnemyGarrisonsIssue : IssueBase
		{
			// Token: 0x06005E61 RID: 24161 RVA: 0x001B3063 File Offset: 0x001B1263
			internal static void AutoGeneratedStaticCollectObjectsScoutEnemyGarrisonsIssue(object o, List<object> collectedObjects)
			{
				((ScoutEnemyGarrisonsIssueBehavior.ScoutEnemyGarrisonsIssue)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
			}

			// Token: 0x06005E62 RID: 24162 RVA: 0x001B3071 File Offset: 0x001B1271
			protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
			{
				base.AutoGeneratedInstanceCollectObjects(collectedObjects);
				collectedObjects.Add(this._settlement1);
				collectedObjects.Add(this._settlement2);
				collectedObjects.Add(this._settlement3);
			}

			// Token: 0x06005E63 RID: 24163 RVA: 0x001B309E File Offset: 0x001B129E
			internal static object AutoGeneratedGetMemberValue_settlement1(object o)
			{
				return ((ScoutEnemyGarrisonsIssueBehavior.ScoutEnemyGarrisonsIssue)o)._settlement1;
			}

			// Token: 0x06005E64 RID: 24164 RVA: 0x001B30AB File Offset: 0x001B12AB
			internal static object AutoGeneratedGetMemberValue_settlement2(object o)
			{
				return ((ScoutEnemyGarrisonsIssueBehavior.ScoutEnemyGarrisonsIssue)o)._settlement2;
			}

			// Token: 0x06005E65 RID: 24165 RVA: 0x001B30B8 File Offset: 0x001B12B8
			internal static object AutoGeneratedGetMemberValue_settlement3(object o)
			{
				return ((ScoutEnemyGarrisonsIssueBehavior.ScoutEnemyGarrisonsIssue)o)._settlement3;
			}

			// Token: 0x17001306 RID: 4870
			// (get) Token: 0x06005E66 RID: 24166 RVA: 0x001B30C5 File Offset: 0x001B12C5
			public override bool IsThereAlternativeSolution
			{
				get
				{
					return false;
				}
			}

			// Token: 0x17001307 RID: 4871
			// (get) Token: 0x06005E67 RID: 24167 RVA: 0x001B30C8 File Offset: 0x001B12C8
			public override bool IsThereLordSolution
			{
				get
				{
					return false;
				}
			}

			// Token: 0x17001308 RID: 4872
			// (get) Token: 0x06005E68 RID: 24168 RVA: 0x001B30CB File Offset: 0x001B12CB
			protected override int RewardGold
			{
				get
				{
					return 0;
				}
			}

			// Token: 0x17001309 RID: 4873
			// (get) Token: 0x06005E69 RID: 24169 RVA: 0x001B30CE File Offset: 0x001B12CE
			public override TextObject IssueBriefByIssueGiver
			{
				get
				{
					return new TextObject("{=rrCkJgtd}We don't know enough about the enemy, [ib:closed][if:convo_thinking]where they are strong and where they are weak. I don't want to lead a huge army through their territory on a wild goose hunt. We need someone to ride through there swiftly, scouting out their garrisons. Can you do this?", null);
				}
			}

			// Token: 0x1700130A RID: 4874
			// (get) Token: 0x06005E6A RID: 24170 RVA: 0x001B30DC File Offset: 0x001B12DC
			public override TextObject IssueAcceptByPlayer
			{
				get
				{
					TextObject textObject = new TextObject("{=dGakGflE}Yes, your {?QUEST_GIVER.GENDER}ladyship{?}lordship{\\?}, I'll gladly do it.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.IssueOwner.CharacterObject, textObject, false);
					return textObject;
				}
			}

			// Token: 0x1700130B RID: 4875
			// (get) Token: 0x06005E6B RID: 24171 RVA: 0x001B3110 File Offset: 0x001B1310
			public override TextObject IssueQuestSolutionExplanationByIssueGiver
			{
				get
				{
					TextObject textObject = new TextObject("{=seEyGLMz}Go deep into {ENEMY} territory, to {SETTLEMENT_1}, {SETTLEMENT_2} and {SETTLEMENT_3}. [ib:hip][if:convo_normal]I want to know every detail about them, what sort of fortifications they have, whether the walls are well-manned or undergarrisoned, and any other enemy forces in the vicinity.", null);
					textObject.SetTextVariable("ENEMY", this._settlement1.MapFaction.Name);
					textObject.SetTextVariable("SETTLEMENT_1", this._settlement1.Name);
					textObject.SetTextVariable("SETTLEMENT_2", this._settlement2.Name);
					textObject.SetTextVariable("SETTLEMENT_3", this._settlement3.Name);
					return textObject;
				}
			}

			// Token: 0x1700130C RID: 4876
			// (get) Token: 0x06005E6C RID: 24172 RVA: 0x001B3189 File Offset: 0x001B1389
			public override TextObject IssueQuestSolutionAcceptByPlayer
			{
				get
				{
					return new TextObject("{=g6P6nKIf}Consider it done, commander.", null);
				}
			}

			// Token: 0x1700130D RID: 4877
			// (get) Token: 0x06005E6D RID: 24173 RVA: 0x001B3196 File Offset: 0x001B1396
			public override TextObject Title
			{
				get
				{
					return new TextObject("{=G79IzJsZ}Scout Enemy Garrisons", null);
				}
			}

			// Token: 0x1700130E RID: 4878
			// (get) Token: 0x06005E6E RID: 24174 RVA: 0x001B31A4 File Offset: 0x001B13A4
			public override TextObject Description
			{
				get
				{
					TextObject textObject = new TextObject("{=AdoaDR26}{QUEST_GIVER.LINK} asks you to scout {SETTLEMENT_1}, {SETTLEMENT_2} and {SETTLEMENT_3}.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.IssueOwner.CharacterObject, textObject, false);
					textObject.SetTextVariable("SETTLEMENT_1", this._settlement1.Name);
					textObject.SetTextVariable("SETTLEMENT_2", this._settlement2.Name);
					textObject.SetTextVariable("SETTLEMENT_3", this._settlement3.Name);
					return textObject;
				}
			}

			// Token: 0x06005E6F RID: 24175 RVA: 0x001B321B File Offset: 0x001B141B
			public ScoutEnemyGarrisonsIssue(Hero issueOwner, List<Settlement> settlements)
				: base(issueOwner, CampaignTime.DaysFromNow(30f))
			{
				this._settlement1 = settlements[0];
				this._settlement2 = settlements[1];
				this._settlement3 = settlements[2];
			}

			// Token: 0x06005E70 RID: 24176 RVA: 0x001B3255 File Offset: 0x001B1455
			protected override void OnGameLoad()
			{
			}

			// Token: 0x06005E71 RID: 24177 RVA: 0x001B3257 File Offset: 0x001B1457
			protected override void HourlyTick()
			{
			}

			// Token: 0x06005E72 RID: 24178 RVA: 0x001B3259 File Offset: 0x001B1459
			protected override QuestBase GenerateIssueQuest(string questId)
			{
				return new ScoutEnemyGarrisonsIssueBehavior.ScoutEnemyGarrisonsQuest(questId, base.IssueOwner, this._settlement1, this._settlement2, this._settlement3);
			}

			// Token: 0x06005E73 RID: 24179 RVA: 0x001B3279 File Offset: 0x001B1479
			public override IssueBase.IssueFrequency GetFrequency()
			{
				return IssueBase.IssueFrequency.VeryCommon;
			}

			// Token: 0x06005E74 RID: 24180 RVA: 0x001B327C File Offset: 0x001B147C
			protected override bool CanPlayerTakeQuestConditions(Hero issueGiver, out IssueBase.PreconditionFlags flag, out Hero relationHero, out SkillObject skill)
			{
				relationHero = null;
				skill = null;
				flag = IssueBase.PreconditionFlags.None;
				if (issueGiver.GetRelationWithPlayer() < -10f)
				{
					flag |= IssueBase.PreconditionFlags.Relation;
					relationHero = issueGiver;
				}
				if (Hero.MainHero.IsKingdomLeader)
				{
					flag |= IssueBase.PreconditionFlags.MainHeroIsKingdomLeader;
				}
				if (issueGiver.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction))
				{
					flag |= IssueBase.PreconditionFlags.AtWar;
				}
				if (Clan.PlayerClan.Tier < 2)
				{
					flag |= IssueBase.PreconditionFlags.ClanTier;
				}
				if (Hero.MainHero.GetSkillValue(DefaultSkills.Scouting) < 30)
				{
					flag |= IssueBase.PreconditionFlags.Skill;
					skill = DefaultSkills.Scouting;
				}
				if (Hero.MainHero.MapFaction != base.IssueOwner.MapFaction)
				{
					flag |= IssueBase.PreconditionFlags.NotInSameFaction;
				}
				return flag == IssueBase.PreconditionFlags.None;
			}

			// Token: 0x06005E75 RID: 24181 RVA: 0x001B333C File Offset: 0x001B153C
			public override bool IssueStayAliveConditions()
			{
				bool flag = this._settlement1.MapFaction.IsAtWarWith(base.IssueOwner.MapFaction) && this._settlement2.MapFaction.IsAtWarWith(base.IssueOwner.MapFaction) && this._settlement3.MapFaction.IsAtWarWith(base.IssueOwner.MapFaction);
				if (!flag)
				{
					flag = this.TryToUpdateSettlements();
				}
				return flag && base.IssueOwner.MapFaction.IsKingdomFaction;
			}

			// Token: 0x06005E76 RID: 24182 RVA: 0x001B33C1 File Offset: 0x001B15C1
			protected override float GetIssueEffectAmountInternal(IssueEffect issueEffect)
			{
				if (issueEffect == DefaultIssueEffects.ClanInfluence)
				{
					return -0.1f;
				}
				return 0f;
			}

			// Token: 0x06005E77 RID: 24183 RVA: 0x001B33D8 File Offset: 0x001B15D8
			private bool TryToUpdateSettlements()
			{
				Kingdom randomElementWithPredicate = Kingdom.All.GetRandomElementWithPredicate((Kingdom x) => x.IsAtWarWith(base.IssueOwner.MapFaction));
				MapDistanceModel mapDistanceModel = Campaign.Current.Models.MapDistanceModel;
				IMapPoint mapPoint = base.IssueOwner.GetMapPoint();
				float num = Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(MobileParty.NavigationType.All) * 5f;
				if (randomElementWithPredicate != null && mapPoint != null)
				{
					ValueTuple<Settlement, float>[] array = new ValueTuple<Settlement, float>[3];
					foreach (Settlement settlement in randomElementWithPredicate.Settlements)
					{
						if (ScoutEnemyGarrisonsIssueBehavior.SuitableSettlementCondition(settlement, base.IssueOwner))
						{
							float num2 = float.MaxValue;
							if (base.IssueOwner.CurrentSettlement != null)
							{
								num2 = mapDistanceModel.GetDistance(base.IssueOwner.CurrentSettlement, settlement, false, false, MobileParty.NavigationType.All);
							}
							else if (base.IssueOwner.PartyBelongedTo != null)
							{
								float num3;
								num2 = mapDistanceModel.GetDistance(base.IssueOwner.PartyBelongedTo, settlement, false, MobileParty.NavigationType.All, out num3);
							}
							else if (base.IssueOwner.PartyBelongedToAsPrisoner != null)
							{
								float num3;
								num2 = mapDistanceModel.GetDistance(base.IssueOwner.PartyBelongedToAsPrisoner.MobileParty, settlement, false, MobileParty.NavigationType.All, out num3);
							}
							if (num2 <= num)
							{
								if (array[2].Item1 == null || array[2].Item2 > num2)
								{
									array[2] = new ValueTuple<Settlement, float>(settlement, num2);
								}
								int num4 = array.Length - 1;
								while (num4 > 0 && (array[num4 - 1].Item1 == null || array[num4].Item2 < array[num4 - 1].Item2))
								{
									ValueTuple<Settlement, float> valueTuple = array[num4 - 1];
									array[num4 - 1] = array[num4];
									array[num4] = valueTuple;
									num4--;
								}
							}
						}
					}
					if (array[2].Item1 != null)
					{
						this._settlement1 = array[2].Item1;
						this._settlement2 = array[1].Item1;
						this._settlement3 = array[0].Item1;
						return true;
					}
				}
				return false;
			}

			// Token: 0x06005E78 RID: 24184 RVA: 0x001B361C File Offset: 0x001B181C
			protected override void CompleteIssueWithTimedOutConsequences()
			{
			}

			// Token: 0x04001DBC RID: 7612
			private const int MinimumRelationToTakeQuest = -10;

			// Token: 0x04001DBD RID: 7613
			[SaveableField(10)]
			private Settlement _settlement1;

			// Token: 0x04001DBE RID: 7614
			[SaveableField(20)]
			private Settlement _settlement2;

			// Token: 0x04001DBF RID: 7615
			[SaveableField(30)]
			private Settlement _settlement3;
		}

		// Token: 0x0200074B RID: 1867
		public class ScoutEnemyGarrisonsQuest : QuestBase
		{
			// Token: 0x06005E7A RID: 24186 RVA: 0x001B3631 File Offset: 0x001B1831
			internal static void AutoGeneratedStaticCollectObjectsScoutEnemyGarrisonsQuest(object o, List<object> collectedObjects)
			{
				((ScoutEnemyGarrisonsIssueBehavior.ScoutEnemyGarrisonsQuest)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
			}

			// Token: 0x06005E7B RID: 24187 RVA: 0x001B363F File Offset: 0x001B183F
			protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
			{
				base.AutoGeneratedInstanceCollectObjects(collectedObjects);
				collectedObjects.Add(this._questSettlement1);
				collectedObjects.Add(this._questSettlement2);
				collectedObjects.Add(this._questSettlement3);
				collectedObjects.Add(this._startQuestLog);
			}

			// Token: 0x06005E7C RID: 24188 RVA: 0x001B3678 File Offset: 0x001B1878
			internal static object AutoGeneratedGetMemberValue_questSettlement1(object o)
			{
				return ((ScoutEnemyGarrisonsIssueBehavior.ScoutEnemyGarrisonsQuest)o)._questSettlement1;
			}

			// Token: 0x06005E7D RID: 24189 RVA: 0x001B3685 File Offset: 0x001B1885
			internal static object AutoGeneratedGetMemberValue_questSettlement2(object o)
			{
				return ((ScoutEnemyGarrisonsIssueBehavior.ScoutEnemyGarrisonsQuest)o)._questSettlement2;
			}

			// Token: 0x06005E7E RID: 24190 RVA: 0x001B3692 File Offset: 0x001B1892
			internal static object AutoGeneratedGetMemberValue_questSettlement3(object o)
			{
				return ((ScoutEnemyGarrisonsIssueBehavior.ScoutEnemyGarrisonsQuest)o)._questSettlement3;
			}

			// Token: 0x06005E7F RID: 24191 RVA: 0x001B369F File Offset: 0x001B189F
			internal static object AutoGeneratedGetMemberValue_scoutedSettlementCount(object o)
			{
				return ((ScoutEnemyGarrisonsIssueBehavior.ScoutEnemyGarrisonsQuest)o)._scoutedSettlementCount;
			}

			// Token: 0x06005E80 RID: 24192 RVA: 0x001B36B1 File Offset: 0x001B18B1
			internal static object AutoGeneratedGetMemberValue_startQuestLog(object o)
			{
				return ((ScoutEnemyGarrisonsIssueBehavior.ScoutEnemyGarrisonsQuest)o)._startQuestLog;
			}

			// Token: 0x1700130F RID: 4879
			// (get) Token: 0x06005E81 RID: 24193 RVA: 0x001B36BE File Offset: 0x001B18BE
			public override bool IsRemainingTimeHidden
			{
				get
				{
					return false;
				}
			}

			// Token: 0x17001310 RID: 4880
			// (get) Token: 0x06005E82 RID: 24194 RVA: 0x001B36C1 File Offset: 0x001B18C1
			public override TextObject Title
			{
				get
				{
					return new TextObject("{=G79IzJsZ}Scout Enemy Garrisons", null);
				}
			}

			// Token: 0x17001311 RID: 4881
			// (get) Token: 0x06005E83 RID: 24195 RVA: 0x001B36D0 File Offset: 0x001B18D0
			private TextObject PlayerStartsQuestLogText
			{
				get
				{
					TextObject textObject = new TextObject("{=8avwit9N}{QUEST_GIVER.LINK}, the army commander of {FACTION} has told you that they need detailed information about enemy fortifications and troop numbers of the enemy. {?QUEST_GIVER.GENDER}She{?}He{\\?} wanted you to scout {SETTLEMENT_1}, {SETTLEMENT_2} and {SETTLEMENT_3}.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					textObject.SetTextVariable("FACTION", base.QuestGiver.MapFaction.EncyclopediaLinkWithName);
					textObject.SetTextVariable("SETTLEMENT_1", this._questSettlement1.Settlement.EncyclopediaLinkWithName);
					textObject.SetTextVariable("SETTLEMENT_2", this._questSettlement2.Settlement.EncyclopediaLinkWithName);
					textObject.SetTextVariable("SETTLEMENT_3", this._questSettlement3.Settlement.EncyclopediaLinkWithName);
					return textObject;
				}
			}

			// Token: 0x17001312 RID: 4882
			// (get) Token: 0x06005E84 RID: 24196 RVA: 0x001B3772 File Offset: 0x001B1972
			private TextObject SettlementBecomeNeutralLogText
			{
				get
				{
					return new TextObject("{=wgX2nL5Z}{SETTLEMENT} is no longer in control of enemy. There is no need to scout that settlement.", null);
				}
			}

			// Token: 0x17001313 RID: 4883
			// (get) Token: 0x06005E85 RID: 24197 RVA: 0x001B377F File Offset: 0x001B197F
			private TextObject ArmyDisbandedQuestCancelLogText
			{
				get
				{
					return new TextObject("{=JiHaL6IV}Army has disbanded and your mission has been canceled.", null);
				}
			}

			// Token: 0x17001314 RID: 4884
			// (get) Token: 0x06005E86 RID: 24198 RVA: 0x001B378C File Offset: 0x001B198C
			private TextObject NoLongerAllyQuestCancelLogText
			{
				get
				{
					TextObject textObject = new TextObject("{=vTnSa9rr}You are no longer allied with {QUEST_GIVER.LINK}'s faction. Your agreement with {QUEST_GIVER.LINK} was terminated.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					return textObject;
				}
			}

			// Token: 0x17001315 RID: 4885
			// (get) Token: 0x06005E87 RID: 24199 RVA: 0x001B37BE File Offset: 0x001B19BE
			private TextObject AllTargetsAreNeutral
			{
				get
				{
					return new TextObject("{=LC2F84GR}None of the target settlements are in control of the enemy. Army Commander has canceled the mission.", null);
				}
			}

			// Token: 0x17001316 RID: 4886
			// (get) Token: 0x06005E88 RID: 24200 RVA: 0x001B37CB File Offset: 0x001B19CB
			private TextObject ScoutFinishedForSettlementWallLevel1LogText
			{
				get
				{
					return new TextObject("{=5kxDhBWk}Your scouts have returned from {SETTLEMENT}. According to their report {SETTLEMENT}'s garrison has {GARRISON_SIZE} men and walls are not high enough but can be useful with sufficient garrison support.", null);
				}
			}

			// Token: 0x17001317 RID: 4887
			// (get) Token: 0x06005E89 RID: 24201 RVA: 0x001B37D8 File Offset: 0x001B19D8
			private TextObject ScoutFinishedForSettlementWallLevel2LogText
			{
				get
				{
					return new TextObject("{=GUqjL6xk}Your scouts have returned from {SETTLEMENT}. According to their report {SETTLEMENT}'s garrison has {GARRISON_SIZE} men and walls are high enough to defend against invaders.", null);
				}
			}

			// Token: 0x17001318 RID: 4888
			// (get) Token: 0x06005E8A RID: 24202 RVA: 0x001B37E5 File Offset: 0x001B19E5
			private TextObject ScoutFinishedForSettlementWallLevel3LogText
			{
				get
				{
					return new TextObject("{=YErURO5l}Your scouts have returned from {SETTLEMENT}. According to their report {SETTLEMENT}'s garrison has {GARRISON_SIZE} men and walls are too high and hard to breach.", null);
				}
			}

			// Token: 0x17001319 RID: 4889
			// (get) Token: 0x06005E8B RID: 24203 RVA: 0x001B37F4 File Offset: 0x001B19F4
			private TextObject QuestSuccess
			{
				get
				{
					TextObject textObject = new TextObject("{=Qy7Zmmvk}You have successfully scouted the target settlements and sent the report to {QUEST_GIVER.LINK}.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					return textObject;
				}
			}

			// Token: 0x1700131A RID: 4890
			// (get) Token: 0x06005E8C RID: 24204 RVA: 0x001B3826 File Offset: 0x001B1A26
			private TextObject QuestTimedOut
			{
				get
				{
					return new TextObject("{=GzodT3vS}You have failed to scout the enemy settlements in time.", null);
				}
			}

			// Token: 0x06005E8D RID: 24205 RVA: 0x001B3834 File Offset: 0x001B1A34
			public ScoutEnemyGarrisonsQuest(string questId, Hero questGiver, Settlement settlement1, Settlement settlement2, Settlement settlement3)
				: base(questId, questGiver, CampaignTime.DaysFromNow(30f), 0)
			{
				this._questSettlement1 = new ScoutEnemyGarrisonsIssueBehavior.QuestSettlement(settlement1, 0);
				this._questSettlement2 = new ScoutEnemyGarrisonsIssueBehavior.QuestSettlement(settlement2, 0);
				this._questSettlement3 = new ScoutEnemyGarrisonsIssueBehavior.QuestSettlement(settlement3, 0);
				this.SetDialogs();
				base.InitializeQuestOnCreation();
			}

			// Token: 0x06005E8E RID: 24206 RVA: 0x001B3889 File Offset: 0x001B1A89
			protected override void InitializeQuestOnGameLoad()
			{
				this.SetDialogs();
			}

			// Token: 0x06005E8F RID: 24207 RVA: 0x001B3894 File Offset: 0x001B1A94
			protected override void SetDialogs()
			{
				this.OfferDialogFlow = DialogFlow.CreateDialogFlow("issue_classic_quest_start", 100).NpcLine(new TextObject("{=lyGvyZK4}Very well. When you reach one of their fortresses, spend some time observing. Don't move on to the next one at once. You don't need to find me to report back the details, just send your messengers.", null), null, null, null, null).Condition(() => Hero.OneToOneConversationHero == base.QuestGiver)
					.Consequence(new ConversationSentence.OnConsequenceDelegate(this.QuestAcceptedConsequences))
					.CloseDialog();
				this.DiscussDialogFlow = DialogFlow.CreateDialogFlow("quest_discuss", 100).NpcLine(new TextObject("{=x3TO0gkN}Is there any progress on the task I gave you?[ib:closed][if:convo_normal]", null), null, null, null, null).Condition(() => Hero.OneToOneConversationHero == base.QuestGiver)
					.Consequence(delegate
					{
						Campaign.Current.ConversationManager.ConversationEndOneShot += MapEventHelper.OnConversationEnd;
					})
					.BeginPlayerOptions(null, false)
					.PlayerOption(new TextObject("{=W5ab31gQ}Soon, commander. We are still working on it.", null), null, null, null)
					.NpcLine(new TextObject("{=U3LR7dyK}Good. I'll be waiting for your messengers.[if:convo_thinking]", null), null, null, null, null)
					.CloseDialog()
					.PlayerOption(new TextObject("{=v75k1FoT}Not yet. We need to make more preparations.", null), null, null, null)
					.NpcLine(new TextObject("{=zYKeYZAo}All right. Don't rush this but also don't wait too long.", null), null, null, null, null)
					.CloseDialog()
					.EndPlayerOptions()
					.CloseDialog();
			}

			// Token: 0x06005E90 RID: 24208 RVA: 0x001B39B4 File Offset: 0x001B1BB4
			private void QuestAcceptedConsequences()
			{
				base.StartQuest();
				base.AddTrackedObject(this._questSettlement1.Settlement);
				base.AddTrackedObject(this._questSettlement2.Settlement);
				base.AddTrackedObject(this._questSettlement3.Settlement);
				this._scoutedSettlementCount = 0;
				this._startQuestLog = base.AddDiscreteLog(this.PlayerStartsQuestLogText, new TextObject("{=jpBpwgAs}Settlements", null), this._scoutedSettlementCount, 3, null, false);
			}

			// Token: 0x06005E91 RID: 24209 RVA: 0x001B3A28 File Offset: 0x001B1C28
			protected override void RegisterEvents()
			{
				CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, new Action<Clan, Kingdom, Kingdom, ChangeKingdomAction.ChangeKingdomActionDetail, bool>(this.OnClanChangedKingdom));
				CampaignEvents.ArmyDispersed.AddNonSerializedListener(this, new Action<Army, Army.ArmyDispersionReason, bool>(this.OnArmyDispersed));
				CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, new Action<Settlement, bool, Hero, Hero, Hero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail>(this.OnSettlementOwnerChanged));
			}

			// Token: 0x06005E92 RID: 24210 RVA: 0x001B3A7C File Offset: 0x001B1C7C
			protected override void HourlyTick()
			{
				if (base.IsOngoing)
				{
					List<ScoutEnemyGarrisonsIssueBehavior.QuestSettlement> list = new List<ScoutEnemyGarrisonsIssueBehavior.QuestSettlement> { this._questSettlement1, this._questSettlement2, this._questSettlement3 };
					if (list.TrueForAll((ScoutEnemyGarrisonsIssueBehavior.QuestSettlement x) => !x.Settlement.MapFaction.IsAtWarWith(base.QuestGiver.MapFaction)))
					{
						base.AddLog(this.AllTargetsAreNeutral, false);
						base.CompleteQuestWithCancel(null);
						return;
					}
					foreach (ScoutEnemyGarrisonsIssueBehavior.QuestSettlement questSettlement in list)
					{
						if (!questSettlement.IsScoutingCompleted())
						{
							if (DistanceHelper.FindClosestDistanceFromMobilePartyToSettlement(MobileParty.MainParty, questSettlement.Settlement, MobileParty.NavigationType.Default) <= MobileParty.MainParty.SeeingRange)
							{
								questSettlement.CurrentScoutProgress++;
								if (questSettlement.CurrentScoutProgress == 1)
								{
									TextObject textObject = new TextObject("{=qfjRGjM4}Your scouts started to gather information about {SETTLEMENT}.", null);
									textObject.SetTextVariable("SETTLEMENT", questSettlement.Settlement.Name);
									MBInformationManager.AddQuickInformation(textObject, 0, null, null, "");
								}
								else if (questSettlement.IsScoutingCompleted())
								{
									JournalLog startQuestLog = this._startQuestLog;
									int num = this._scoutedSettlementCount + 1;
									this._scoutedSettlementCount = num;
									startQuestLog.UpdateCurrentProgress(num);
									base.RemoveTrackedObject(questSettlement.Settlement);
									TextObject textObject2;
									if (questSettlement.Settlement.Town.GetWallLevel() == 1)
									{
										textObject2 = this.ScoutFinishedForSettlementWallLevel1LogText;
									}
									else if (questSettlement.Settlement.Town.GetWallLevel() == 2)
									{
										textObject2 = this.ScoutFinishedForSettlementWallLevel2LogText;
									}
									else
									{
										textObject2 = this.ScoutFinishedForSettlementWallLevel3LogText;
									}
									textObject2.SetTextVariable("SETTLEMENT", questSettlement.Settlement.EncyclopediaLinkWithName);
									MobileParty garrisonParty = questSettlement.Settlement.Town.GarrisonParty;
									int num2 = ((garrisonParty != null) ? garrisonParty.MemberRoster.TotalHealthyCount : 0);
									int num3 = (int)questSettlement.Settlement.Militia;
									textObject2.SetTextVariable("GARRISON_SIZE", num2 + num3);
									base.AddLog(textObject2, false);
								}
							}
							else
							{
								questSettlement.ResetCurrentProgress();
							}
						}
					}
					if (list.TrueForAll((ScoutEnemyGarrisonsIssueBehavior.QuestSettlement x) => x.IsScoutingCompleted()))
					{
						this.AllScoutingDone();
					}
				}
			}

			// Token: 0x06005E93 RID: 24211 RVA: 0x001B3CB8 File Offset: 0x001B1EB8
			private void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
			{
				List<ScoutEnemyGarrisonsIssueBehavior.QuestSettlement> list = new List<ScoutEnemyGarrisonsIssueBehavior.QuestSettlement> { this._questSettlement1, this._questSettlement2, this._questSettlement3 };
				foreach (ScoutEnemyGarrisonsIssueBehavior.QuestSettlement questSettlement in list)
				{
					if (settlement == questSettlement.Settlement && !questSettlement.IsScoutingCompleted() && (newOwner.MapFaction == base.QuestGiver.MapFaction || !newOwner.MapFaction.IsAtWarWith(base.QuestGiver.MapFaction)))
					{
						questSettlement.IsCompletedThroughBeingNeutral = true;
						questSettlement.SetScoutingCompleted();
						JournalLog startQuestLog = this._startQuestLog;
						int num = this._scoutedSettlementCount + 1;
						this._scoutedSettlementCount = num;
						startQuestLog.UpdateCurrentProgress(num);
						if (base.IsTracked(questSettlement.Settlement))
						{
							base.RemoveTrackedObject(questSettlement.Settlement);
						}
						TextObject settlementBecomeNeutralLogText = this.SettlementBecomeNeutralLogText;
						settlementBecomeNeutralLogText.SetTextVariable("SETTLEMENT", questSettlement.Settlement.EncyclopediaLinkWithName);
						base.AddLog(settlementBecomeNeutralLogText, false);
						if (list.TrueForAll((ScoutEnemyGarrisonsIssueBehavior.QuestSettlement x) => x.IsCompletedThroughBeingNeutral))
						{
							base.AddLog(this.AllTargetsAreNeutral, false);
							base.CompleteQuestWithCancel(null);
							break;
						}
						break;
					}
				}
			}

			// Token: 0x06005E94 RID: 24212 RVA: 0x001B3E2C File Offset: 0x001B202C
			private void OnArmyDispersed(Army army, Army.ArmyDispersionReason reason, bool isPlayersArmy)
			{
				if (army.ArmyOwner == base.QuestGiver)
				{
					base.AddLog(this.ArmyDisbandedQuestCancelLogText, false);
					base.CompleteQuestWithCancel(null);
				}
			}

			// Token: 0x06005E95 RID: 24213 RVA: 0x001B3E51 File Offset: 0x001B2051
			private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
			{
				if (clan == Clan.PlayerClan && oldKingdom == base.QuestGiver.MapFaction)
				{
					base.AddLog(this.NoLongerAllyQuestCancelLogText, false);
					base.CompleteQuestWithCancel(null);
				}
			}

			// Token: 0x06005E96 RID: 24214 RVA: 0x001B3E7E File Offset: 0x001B207E
			private void AllScoutingDone()
			{
				base.AddLog(this.QuestSuccess, false);
				GainRenownAction.Apply(Hero.MainHero, 3f, false);
				GainKingdomInfluenceAction.ApplyForDefault(Hero.MainHero, 10f);
				this.RelationshipChangeWithQuestGiver = 3;
				base.CompleteQuestWithSuccess();
			}

			// Token: 0x06005E97 RID: 24215 RVA: 0x001B3EBA File Offset: 0x001B20BA
			protected override void OnTimedOut()
			{
				base.AddLog(this.QuestTimedOut, false);
				this.RelationshipChangeWithQuestGiver = -2;
			}

			// Token: 0x04001DC0 RID: 7616
			[SaveableField(10)]
			private ScoutEnemyGarrisonsIssueBehavior.QuestSettlement _questSettlement1;

			// Token: 0x04001DC1 RID: 7617
			[SaveableField(20)]
			private ScoutEnemyGarrisonsIssueBehavior.QuestSettlement _questSettlement2;

			// Token: 0x04001DC2 RID: 7618
			[SaveableField(30)]
			private ScoutEnemyGarrisonsIssueBehavior.QuestSettlement _questSettlement3;

			// Token: 0x04001DC3 RID: 7619
			[SaveableField(40)]
			private int _scoutedSettlementCount;

			// Token: 0x04001DC4 RID: 7620
			[SaveableField(50)]
			private JournalLog _startQuestLog;
		}

		// Token: 0x0200074C RID: 1868
		public class QuestSettlement
		{
			// Token: 0x06005E9B RID: 24219 RVA: 0x001B3F10 File Offset: 0x001B2110
			internal static void AutoGeneratedStaticCollectObjectsQuestSettlement(object o, List<object> collectedObjects)
			{
				((ScoutEnemyGarrisonsIssueBehavior.QuestSettlement)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
			}

			// Token: 0x06005E9C RID: 24220 RVA: 0x001B3F1E File Offset: 0x001B211E
			protected virtual void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
			{
				collectedObjects.Add(this.Settlement);
			}

			// Token: 0x06005E9D RID: 24221 RVA: 0x001B3F2C File Offset: 0x001B212C
			internal static object AutoGeneratedGetMemberValueSettlement(object o)
			{
				return ((ScoutEnemyGarrisonsIssueBehavior.QuestSettlement)o).Settlement;
			}

			// Token: 0x06005E9E RID: 24222 RVA: 0x001B3F39 File Offset: 0x001B2139
			internal static object AutoGeneratedGetMemberValueCurrentScoutProgress(object o)
			{
				return ((ScoutEnemyGarrisonsIssueBehavior.QuestSettlement)o).CurrentScoutProgress;
			}

			// Token: 0x06005E9F RID: 24223 RVA: 0x001B3F4B File Offset: 0x001B214B
			public QuestSettlement(Settlement settlement, int currentScoutProgress)
			{
				this.Settlement = settlement;
				this.CurrentScoutProgress = currentScoutProgress;
				this.IsCompletedThroughBeingNeutral = false;
			}

			// Token: 0x06005EA0 RID: 24224 RVA: 0x001B3F68 File Offset: 0x001B2168
			public bool IsScoutingCompleted()
			{
				return this.CurrentScoutProgress >= 8;
			}

			// Token: 0x06005EA1 RID: 24225 RVA: 0x001B3F76 File Offset: 0x001B2176
			public void SetScoutingCompleted()
			{
				this.CurrentScoutProgress = 8;
			}

			// Token: 0x06005EA2 RID: 24226 RVA: 0x001B3F7F File Offset: 0x001B217F
			public void ResetCurrentProgress()
			{
				this.CurrentScoutProgress = 0;
			}

			// Token: 0x04001DC5 RID: 7621
			private const int CompleteScoutAfterHours = 8;

			// Token: 0x04001DC6 RID: 7622
			[SaveableField(10)]
			public Settlement Settlement;

			// Token: 0x04001DC7 RID: 7623
			[SaveableField(20)]
			public int CurrentScoutProgress;

			// Token: 0x04001DC8 RID: 7624
			public bool IsCompletedThroughBeingNeutral;
		}

		// Token: 0x0200074D RID: 1869
		public class ScoutEnemyGarrisonsIssueTypeDefiner : SaveableTypeDefiner
		{
			// Token: 0x06005EA3 RID: 24227 RVA: 0x001B3F88 File Offset: 0x001B2188
			public ScoutEnemyGarrisonsIssueTypeDefiner()
				: base(97600)
			{
			}

			// Token: 0x06005EA4 RID: 24228 RVA: 0x001B3F95 File Offset: 0x001B2195
			protected override void DefineClassTypes()
			{
				base.AddClassDefinition(typeof(ScoutEnemyGarrisonsIssueBehavior.ScoutEnemyGarrisonsIssue), 1, null);
				base.AddClassDefinition(typeof(ScoutEnemyGarrisonsIssueBehavior.ScoutEnemyGarrisonsQuest), 2, null);
				base.AddClassDefinition(typeof(ScoutEnemyGarrisonsIssueBehavior.QuestSettlement), 3, null);
			}
		}
	}
}
