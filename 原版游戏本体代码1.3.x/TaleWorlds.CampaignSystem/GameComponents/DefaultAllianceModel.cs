using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x020000EF RID: 239
	public class DefaultAllianceModel : AllianceModel
	{
		// Token: 0x17000607 RID: 1543
		// (get) Token: 0x060015FF RID: 5631 RVA: 0x00063C02 File Offset: 0x00061E02
		public override CampaignTime MaxDurationOfAlliance
		{
			get
			{
				return CampaignTime.Days(84f);
			}
		}

		// Token: 0x17000608 RID: 1544
		// (get) Token: 0x06001600 RID: 5632 RVA: 0x00063C0E File Offset: 0x00061E0E
		public override CampaignTime MaxDurationOfWarParticipation
		{
			get
			{
				return CampaignTime.Days(42f);
			}
		}

		// Token: 0x17000609 RID: 1545
		// (get) Token: 0x06001601 RID: 5633 RVA: 0x00063C1A File Offset: 0x00061E1A
		public override int MaxNumberOfAlliances
		{
			get
			{
				return 2;
			}
		}

		// Token: 0x1700060A RID: 1546
		// (get) Token: 0x06001602 RID: 5634 RVA: 0x00063C1D File Offset: 0x00061E1D
		public override CampaignTime DurationForOffers
		{
			get
			{
				return CampaignTime.Hours(24f);
			}
		}

		// Token: 0x06001603 RID: 5635 RVA: 0x00063C2C File Offset: 0x00061E2C
		public override int GetCallToWarCost(Kingdom callingKingdom, Kingdom calledKingdom, Kingdom kingdomToCallToWarAgainst)
		{
			int callToWarCostForCalledKingdom = this.GetCallToWarCostForCalledKingdom(calledKingdom, kingdomToCallToWarAgainst);
			int callToWarBudgetOfCallingKingdom = this.GetCallToWarBudgetOfCallingKingdom(callingKingdom, calledKingdom, kingdomToCallToWarAgainst);
			if (callingKingdom == Clan.PlayerClan.Kingdom && callToWarBudgetOfCallingKingdom < 0)
			{
				return callToWarCostForCalledKingdom;
			}
			return (callToWarCostForCalledKingdom + callToWarBudgetOfCallingKingdom) / 2;
		}

		// Token: 0x06001604 RID: 5636 RVA: 0x00063C64 File Offset: 0x00061E64
		public override ExplainedNumber GetScoreOfStartingAlliance(Kingdom kingdomDeclaresAlliance, Kingdom kingdomDeclaredAlliance, IFaction evaluatingFaction, out TextObject explanationText, bool includeDescription = false)
		{
			ExplainedNumber result = new ExplainedNumber(0f, includeDescription, null);
			ExplainedNumber tooltip = new ExplainedNumber(0f, includeDescription, null);
			int num = kingdomDeclaresAlliance.FactionsAtWarWith.Count((IFaction x) => x.IsKingdomFaction);
			int num2 = kingdomDeclaredAlliance.FactionsAtWarWith.Count((IFaction x) => x.IsKingdomFaction);
			if (num > 0 && num2 > 0)
			{
				int num3 = kingdomDeclaredAlliance.FactionsAtWarWith.Count((IFaction x) => x.IsKingdomFaction && kingdomDeclaresAlliance.FactionsAtWarWith.Contains(x));
				float sharedWarsEffect = (float)num3 / (float)num * 25f * 2f;
				result.Add((float)num3, DefaultAllianceModel._sharedWarsText, null);
				float num4 = (float)(num2 - num3) / (float)num2 * -25f;
				result.Add(num4, DefaultAllianceModel._unsharedWarsText, null);
				this.AddSharedWarsEffectToExplanationTooltip(num3, sharedWarsEffect, num4, num2, num, ref tooltip);
			}
			else
			{
				this.AddNoWarsEffectToExplanationTooltip(ref tooltip);
			}
			int num5 = MBMath.ClampInt(kingdomDeclaredAlliance.Leader.GetRelation(kingdomDeclaresAlliance.Leader), -20, 20);
			result.Add((float)num5, DefaultAllianceModel._relationText, null);
			this.AddLowRelationEffectToExplanationTooltip(num5, ref tooltip);
			int traitLevel = kingdomDeclaredAlliance.Leader.GetTraitLevel(DefaultTraits.Honor);
			result.Add((float)(traitLevel * 10), DefaultAllianceModel._traitLevelText, null);
			this.AddHonorEffectToExplanationTooltip(traitLevel, kingdomDeclaredAlliance.Leader, ref tooltip);
			int dailyTributeToPay = kingdomDeclaresAlliance.GetStanceWith(kingdomDeclaredAlliance).GetDailyTributeToPay(kingdomDeclaredAlliance);
			if (dailyTributeToPay > 0)
			{
				int num6 = 10000;
				float num7 = MBMath.Map((float)dailyTributeToPay, 0f, (float)num6, 0f, 20f);
				this.AddTributeEffectToExplanationTooltip(num7, ref tooltip);
				result.Add(-num7, DefaultAllianceModel._receivedTributeText, null);
			}
			int dailyTributeToPay2 = kingdomDeclaredAlliance.GetStanceWith(kingdomDeclaresAlliance).GetDailyTributeToPay(kingdomDeclaresAlliance);
			if (dailyTributeToPay2 > 0)
			{
				int num8 = 10000;
				result.Add(MBMath.Map((float)dailyTributeToPay2, 0f, (float)num8, 0f, 20f), DefaultAllianceModel._paidTributeText, null);
			}
			if ((float)kingdomDeclaredAlliance.Fiefs.Count / (float)(Campaign.Current.AllTowns.Count + Campaign.Current.AllCastles.Count) > 0.3f)
			{
				this.AddTooPowerfulEffectToExplanationTooltip(ref tooltip);
				result.Add(-20f, DefaultAllianceModel._threatenedText, null);
			}
			if (kingdomDeclaresAlliance.Fiefs.Count < 3)
			{
				result.Add(10f, DefaultAllianceModel._townsText, null);
			}
			int num9 = 0;
			using (List<Kingdom>.Enumerator enumerator = kingdomDeclaredAlliance.AlliedKingdoms.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (enumerator.Current.IsAtWarWith(kingdomDeclaresAlliance))
					{
						num9 -= 5;
					}
				}
			}
			if (num9 < 0)
			{
				result.Add((float)num9, DefaultAllianceModel._warWithTheirAllyText, null);
			}
			int num10 = 0;
			using (List<Kingdom>.Enumerator enumerator = kingdomDeclaresAlliance.AlliedKingdoms.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (enumerator.Current.IsAtWarWith(kingdomDeclaredAlliance))
					{
						num10 -= 5;
					}
				}
			}
			if (num10 < 0)
			{
				result.Add((float)num10, DefaultAllianceModel._allyWithTheirEnemyText, null);
			}
			this.AddConflictingAlliancesEffectToExplanationTooltip(num10, num9, ref tooltip);
			explanationText = this.BuildExplanationForAlliance(kingdomDeclaresAlliance, tooltip);
			return result;
		}

		// Token: 0x06001605 RID: 5637 RVA: 0x00063FEC File Offset: 0x000621EC
		private void AddHonorEffectToExplanationTooltip(int honor, Hero ruler, ref ExplainedNumber explanation)
		{
			if (honor <= 0)
			{
				TextObject textObject = new TextObject("{=Gnkeh9HW}{RULER.NAME}{.o} honor", null);
				textObject.SetCharacterProperties("RULER", ruler.CharacterObject, false);
				explanation.Add((float)(-(float)honor * 10), textObject, null);
			}
		}

		// Token: 0x06001606 RID: 5638 RVA: 0x00064029 File Offset: 0x00062229
		private void AddConflictingAlliancesEffectToExplanationTooltip(int enemyAllyEffectOnOurSide, int enemyAllyEffectOnTheirSide, ref ExplainedNumber explanation)
		{
			if (enemyAllyEffectOnOurSide + enemyAllyEffectOnTheirSide < 0)
			{
				explanation.Add((float)(-(float)enemyAllyEffectOnOurSide - enemyAllyEffectOnTheirSide), DefaultAllianceModel._conflictingAllianceText, null);
			}
		}

		// Token: 0x06001607 RID: 5639 RVA: 0x00064042 File Offset: 0x00062242
		private void AddSharedWarsEffectToExplanationTooltip(int numberOfSharedWars, float sharedWarsEffect, float unsharedWarsEffect, int numberOfWarsOfDeclaredKingdom, int numberOfWarsOfDeclaringKingdom, ref ExplainedNumber explanation)
		{
			if (numberOfSharedWars < numberOfWarsOfDeclaredKingdom || numberOfSharedWars < numberOfWarsOfDeclaringKingdom)
			{
				if (numberOfSharedWars < numberOfWarsOfDeclaringKingdom)
				{
					unsharedWarsEffect -= 50f - sharedWarsEffect;
				}
				explanation.Add(-unsharedWarsEffect, DefaultAllianceModel._unsharedWarsText, null);
			}
		}

		// Token: 0x06001608 RID: 5640 RVA: 0x0006406D File Offset: 0x0006226D
		private void AddNoWarsEffectToExplanationTooltip(ref ExplainedNumber explanation)
		{
			explanation.Add(50f, DefaultAllianceModel._lackOfCommonEnemiesText, null);
		}

		// Token: 0x06001609 RID: 5641 RVA: 0x00064080 File Offset: 0x00062280
		private void AddTributeEffectToExplanationTooltip(float tributeEffect, ref ExplainedNumber explanation)
		{
			if (tributeEffect > 0f)
			{
				explanation.Add(tributeEffect, DefaultAllianceModel._receivedTributeText, null);
			}
		}

		// Token: 0x0600160A RID: 5642 RVA: 0x00064097 File Offset: 0x00062297
		private void AddTooPowerfulEffectToExplanationTooltip(ref ExplainedNumber explanation)
		{
			explanation.Add(20f, DefaultAllianceModel._threatenedText, null);
		}

		// Token: 0x0600160B RID: 5643 RVA: 0x000640AA File Offset: 0x000622AA
		private void AddLowRelationEffectToExplanationTooltip(int relationshipEffect, ref ExplainedNumber explanation)
		{
			if (relationshipEffect < 20)
			{
				explanation.Add((float)(20 - relationshipEffect), DefaultAllianceModel._relationText, null);
			}
		}

		// Token: 0x0600160C RID: 5644 RVA: 0x000640C4 File Offset: 0x000622C4
		private TextObject BuildExplanationForAlliance(Kingdom other, ExplainedNumber tooltip)
		{
			TextObject textObject = TextObject.GetEmpty();
			if (tooltip.IncludeDescriptions)
			{
				textObject = new TextObject("{=eLJ4O0Yl}{KINGDOM} is not ready for an alliance.{newline}{newline}Strongest Factors:{newline}{REASONS_BY_LINE}", null);
				textObject.SetTextVariable("REASONS_BY_LINE", this.GetAllianceExplanation(tooltip));
				textObject.SetTextVariable("KINGDOM", other.Name);
				MBTextManager.SetTextVariable("newline", "\n", false);
			}
			return textObject;
		}

		// Token: 0x0600160D RID: 5645 RVA: 0x00064124 File Offset: 0x00062324
		private TextObject GetAllianceExplanation(ExplainedNumber explainedNumber)
		{
			List<TextObject> list = new List<TextObject>();
			foreach (ValueTuple<string, float> valueTuple in from x in explainedNumber.GetLines()
				orderby x.Item2 descending
				select x)
			{
				string item = valueTuple.Item1;
				TextObject textObject = new TextObject("{=!}{REASON}", null);
				textObject.SetTextVariable("REASON", item);
				list.Add(textObject);
				if (list.Count >= 3)
				{
					break;
				}
			}
			return GameTexts.GameTextHelper.MergeTextObjectsWithSymbol(list, new TextObject("{=!}{newline}", null), null);
		}

		// Token: 0x0600160E RID: 5646 RVA: 0x000641DC File Offset: 0x000623DC
		public override int GetInfluenceCostOfProposingStartingAlliance(Clan proposingClan)
		{
			return 200;
		}

		// Token: 0x0600160F RID: 5647 RVA: 0x000641E4 File Offset: 0x000623E4
		public override float GetScoreOfCallingToWar(Kingdom callingKingdom, Kingdom calledKingdom, Kingdom kingdomToCallToWarAgainst, IFaction evaluatingFaction, out TextObject reason)
		{
			float num = 60f;
			reason = TextObject.GetEmpty();
			int callToWarBudgetOfCallingKingdom = this.GetCallToWarBudgetOfCallingKingdom(callingKingdom, calledKingdom, kingdomToCallToWarAgainst);
			int callToWarCost = Campaign.Current.Models.AllianceModel.GetCallToWarCost(callingKingdom, calledKingdom, kingdomToCallToWarAgainst);
			if (callToWarBudgetOfCallingKingdom < 0 || callingKingdom.CallToWarWallet < -100000 || (float)callToWarBudgetOfCallingKingdom * 1.5f < (float)callToWarCost)
			{
				return -100f;
			}
			if (callToWarCost == 0)
			{
				return 100f;
			}
			float num2 = (float)callToWarBudgetOfCallingKingdom / (float)callToWarCost;
			return num * num2;
		}

		// Token: 0x06001610 RID: 5648 RVA: 0x00064258 File Offset: 0x00062458
		public override float GetScoreOfJoiningWar(Kingdom callingKingdom, Kingdom calledKingdom, Kingdom kingdomToCallToWarAgainst, IFaction evaluatingFaction, out TextObject reason)
		{
			float num = 70f;
			reason = TextObject.GetEmpty();
			int callToWarCostForCalledKingdom = this.GetCallToWarCostForCalledKingdom(calledKingdom, kingdomToCallToWarAgainst);
			int callToWarCost = Campaign.Current.Models.AllianceModel.GetCallToWarCost(callingKingdom, calledKingdom, kingdomToCallToWarAgainst);
			if (callToWarCostForCalledKingdom == 0)
			{
				return 100f;
			}
			float num2 = (float)callToWarCost / (float)callToWarCostForCalledKingdom;
			num2 = MathF.Clamp(num2, 1E-05f, 2f);
			return num * num2;
		}

		// Token: 0x06001611 RID: 5649 RVA: 0x000642B9 File Offset: 0x000624B9
		public override int GetInfluenceCostOfCallingToWar(Clan proposingClan)
		{
			return 200;
		}

		// Token: 0x06001612 RID: 5650 RVA: 0x000642C0 File Offset: 0x000624C0
		private int GetCallToWarCostForCalledKingdom(Kingdom calledKingdom, Kingdom kingdomToCallToWarAgainst)
		{
			TextObject textObject;
			float scoreOfDeclaringWar = Campaign.Current.Models.DiplomacyModel.GetScoreOfDeclaringWar(calledKingdom, kingdomToCallToWarAgainst, calledKingdom.RulingClan, out textObject, false);
			float num = Campaign.Current.Models.DiplomacyModel.GetDecisionMakingThreshold(kingdomToCallToWarAgainst) - scoreOfDeclaringWar;
			if (num <= 0f)
			{
				return 0;
			}
			float valueOfSettlementsForFaction = Campaign.Current.Models.DiplomacyModel.GetValueOfSettlementsForFaction(calledKingdom);
			double num2 = (double)(num / (valueOfSettlementsForFaction + 1f));
			double num3 = (double)calledKingdom.Fiefs.SumQ((Town x) => x.Prosperity) * 0.35;
			return (int)(num2 * num3 * Campaign.Current.Models.AllianceModel.MaxDurationOfWarParticipation.ToDays);
		}

		// Token: 0x06001613 RID: 5651 RVA: 0x00064388 File Offset: 0x00062588
		private int GetCallToWarBudgetOfCallingKingdom(Kingdom callingKingdom, Kingdom calledKingdom, Kingdom kingdomToCallToWarAgainst)
		{
			float currentTotalStrength = callingKingdom.CurrentTotalStrength;
			float currentTotalStrength2 = calledKingdom.CurrentTotalStrength;
			float currentTotalStrength3 = kingdomToCallToWarAgainst.CurrentTotalStrength;
			double num = (double)callingKingdom.Fiefs.SumQ((Town x) => x.Prosperity) * 0.35;
			float num2 = currentTotalStrength - currentTotalStrength3;
			if (num2 == 0f)
			{
				return int.MinValue;
			}
			return (int)((double)MathF.Clamp(-(currentTotalStrength2 / num2), float.MinValue, 1f) * num * Campaign.Current.Models.AllianceModel.MaxDurationOfWarParticipation.ToDays);
		}

		// Token: 0x04000746 RID: 1862
		private const int _thresholdForCallToWarWallet = 100000;

		// Token: 0x04000747 RID: 1863
		private const float SharedWarsEffect = 25f;

		// Token: 0x04000748 RID: 1864
		private const int MaxRelationshipEffect = 20;

		// Token: 0x04000749 RID: 1865
		private const int PotentialAllyBonus = 5;

		// Token: 0x0400074A RID: 1866
		private const int TooPowerfulEffect = 20;

		// Token: 0x0400074B RID: 1867
		private static readonly TextObject _sharedWarsText = new TextObject("{=Pg7bxzcY}Effect of shared wars", null);

		// Token: 0x0400074C RID: 1868
		private static readonly TextObject _unsharedWarsText = new TextObject("{=9YFVXAZ3}Unshared wars", null);

		// Token: 0x0400074D RID: 1869
		private static readonly TextObject _lackOfCommonEnemiesText = new TextObject("{=ugMAk9nb}Lack of common enemies", null);

		// Token: 0x0400074E RID: 1870
		private static readonly TextObject _relationText = new TextObject("{=3YVDMg5X}Low relations between rulers", null);

		// Token: 0x0400074F RID: 1871
		private static readonly TextObject _traitLevelText = new TextObject("{=iUURpauf}Effect of trait level", null);

		// Token: 0x04000750 RID: 1872
		private static readonly TextObject _receivedTributeText = new TextObject("{=pV1LM0aE}Receiving tribute", null);

		// Token: 0x04000751 RID: 1873
		private static readonly TextObject _paidTributeText = new TextObject("{=lyxa5jbH}Effect of tribute paying to the declared", null);

		// Token: 0x04000752 RID: 1874
		private static readonly TextObject _threatenedText = new TextObject("{=92m8jTWP}Feels threatened", null);

		// Token: 0x04000753 RID: 1875
		private static readonly TextObject _townsText = new TextObject("{=WaYxP7bX}Effect of having less than 3 towns", null);

		// Token: 0x04000754 RID: 1876
		private static readonly TextObject _warWithTheirAllyText = new TextObject("{=EOkS8gn8}Effect of having an ally that we are at war with", null);

		// Token: 0x04000755 RID: 1877
		private static readonly TextObject _allyWithTheirEnemyText = new TextObject("{=LhrU9cu3}Effect of having a ally that they are at war with", null);

		// Token: 0x04000756 RID: 1878
		private static readonly TextObject _conflictingAllianceText = new TextObject("{=IeGgrMlx}Conflicting alliances", null);

		// Token: 0x04000757 RID: 1879
		private const int MaxReasonsInExplanation = 3;
	}
}
