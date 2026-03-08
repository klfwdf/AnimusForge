using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x0200015C RID: 348
	public class DefaultTradeAgreementModel : TradeAgreementModel
	{
		// Token: 0x170006D4 RID: 1748
		// (get) Token: 0x06001AA1 RID: 6817 RVA: 0x0008809B File Offset: 0x0008629B
		private ITradeAgreementsCampaignBehavior TradeAgreementsCampaignBehavior
		{
			get
			{
				if (this._tradeAgreementsBehavior == null)
				{
					this._tradeAgreementsBehavior = Campaign.Current.GetCampaignBehavior<ITradeAgreementsCampaignBehavior>();
				}
				return this._tradeAgreementsBehavior;
			}
		}

		// Token: 0x06001AA2 RID: 6818 RVA: 0x000880BB File Offset: 0x000862BB
		public override int GetInfluenceCostOfProposingTradeAgreement(Clan proposerClan)
		{
			return 200;
		}

		// Token: 0x06001AA3 RID: 6819 RVA: 0x000880C2 File Offset: 0x000862C2
		public override int GetMaximumTradeAgreementCount(Kingdom kingdom)
		{
			return 2;
		}

		// Token: 0x06001AA4 RID: 6820 RVA: 0x000880C8 File Offset: 0x000862C8
		public override bool CanMakeTradeAgreement(Kingdom kingdom, Kingdom other, bool checkOtherSideTradeSupport, out TextObject reason, bool includeReason = false)
		{
			reason = (includeReason ? TextObject.GetEmpty() : null);
			if (kingdom.IsAtWarWith(other))
			{
				reason = DefaultTradeAgreementModel._kingdomsAtWarText;
				return false;
			}
			if (other.IsEliminated)
			{
				reason = DefaultTradeAgreementModel._eliminatedKingdomText;
				return false;
			}
			if (this.TradeAgreementsCampaignBehavior.HasTradeAgreement(kingdom, other))
			{
				reason = DefaultTradeAgreementModel._existingTradeAgreementText;
				return false;
			}
			if (Kingdom.All.Count((Kingdom x) => x != kingdom && !x.IsEliminated && this.TradeAgreementsCampaignBehavior.HasTradeAgreement(kingdom, x)) >= Campaign.Current.Models.TradeAgreementModel.GetMaximumTradeAgreementCount(kingdom))
			{
				reason = DefaultTradeAgreementModel._maximumNumberOfTradeAgreementsText;
				return false;
			}
			if (Kingdom.All.Count((Kingdom x) => x != other && !x.IsEliminated && this.TradeAgreementsCampaignBehavior.HasTradeAgreement(other, x)) >= Campaign.Current.Models.TradeAgreementModel.GetMaximumTradeAgreementCount(kingdom))
			{
				if (includeReason)
				{
					reason = new TextObject("{=O6zpuLGa}{OTHER_KINGDOM} already has maximum number of trade agreements.", null);
					reason.SetTextVariable("OTHER_KINGDOM", other.Name);
				}
				return false;
			}
			return !checkOtherSideTradeSupport || Campaign.Current.Models.TradeAgreementModel.GetScoreOfStartingTradeAgreement(kingdom, other, kingdom.RulingClan, out reason, includeReason) >= 50f;
		}

		// Token: 0x06001AA5 RID: 6821 RVA: 0x00088230 File Offset: 0x00086430
		public override float GetScoreOfStartingTradeAgreement(Kingdom kingdom, Kingdom targetKingdom, Clan clan, out TextObject explanation, bool includeExplanation = false)
		{
			ExplainedNumber tooltip = new ExplainedNumber(0f, includeExplanation, null);
			explanation = null;
			CampaignTime peaceDeclarationDate = Campaign.Current.FactionManager.GetStanceLinkInternal(kingdom, targetKingdom).PeaceDeclarationDate;
			float num = ((float)clan.Leader.GetRelation(targetKingdom.Leader) + (float)kingdom.Leader.GetRelation(targetKingdom.Leader) * 3f) * 0.25f;
			this.AddRelationshipEffectToTradeAgreementExplanationTooltip(num, ref tooltip);
			float num2 = MathF.Min((peaceDeclarationDate == CampaignTime.Zero) ? 0f : peaceDeclarationDate.ElapsedDaysUntilNow, 20f);
			this.AddRecentWarEffectToTradeAgreementExplanationTooltip(num2, peaceDeclarationDate, ref tooltip);
			float exposureScoreToOtherKingdom = this.GetExposureScoreToOtherKingdom(kingdom, targetKingdom);
			this.AddExposureEffectToTradeAgreementExplanationTooltip(exposureScoreToOtherKingdom, ref tooltip);
			float value = 15f + num + num2 + exposureScoreToOtherKingdom + kingdom.Leader.RandomFloatWithSeed((uint)CampaignTime.Now.ToDays, 0f, 5f);
			if (includeExplanation)
			{
				explanation = this.BuildExplanationForTradeAgreement(targetKingdom, tooltip);
			}
			return MBMath.ClampFloat(value, 0f, 100f);
		}

		// Token: 0x06001AA6 RID: 6822 RVA: 0x00088335 File Offset: 0x00086535
		private void AddExposureEffectToTradeAgreementExplanationTooltip(float exposure, ref ExplainedNumber explanation)
		{
			if (exposure < 40f)
			{
				explanation.Add(40f - exposure, DefaultTradeAgreementModel._limitedSharerBordersText, null);
			}
		}

		// Token: 0x06001AA7 RID: 6823 RVA: 0x00088354 File Offset: 0x00086554
		private void AddRelationshipEffectToTradeAgreementExplanationTooltip(float relationshipScore, ref ExplainedNumber explanation)
		{
			if (relationshipScore < 0.25f * (float)Campaign.Current.Models.DiplomacyModel.MaxRelationLimit)
			{
				explanation.Add((float)Campaign.Current.Models.DiplomacyModel.MaxRelationLimit - relationshipScore, DefaultTradeAgreementModel._relationsText, null);
			}
		}

		// Token: 0x06001AA8 RID: 6824 RVA: 0x000883A2 File Offset: 0x000865A2
		private void AddRecentWarEffectToTradeAgreementExplanationTooltip(float warScore, CampaignTime peaceDeclarationDate, ref ExplainedNumber explanation)
		{
			if (warScore < 20f && peaceDeclarationDate != CampaignTime.Zero)
			{
				explanation.Add(20f - warScore, DefaultTradeAgreementModel._recentWarText, null);
			}
		}

		// Token: 0x06001AA9 RID: 6825 RVA: 0x000883CC File Offset: 0x000865CC
		private TextObject BuildExplanationForTradeAgreement(Kingdom other, ExplainedNumber tooltip)
		{
			TextObject textObject = new TextObject("{=fFuiV5EZ}{KINGDOM} will not agree to a trade agreement.{newline}{newline}Strongest Factors:{newline}{REASONS_BY_LINE}", null);
			textObject.SetTextVariable("KINGDOM", other.Name);
			textObject.SetTextVariable("REASONS_BY_LINE", this.GetTradeAgreementExplanation(tooltip));
			textObject.SetTextVariable("KINGDOM", other.Name);
			MBTextManager.SetTextVariable("newline", "\n", false);
			return textObject;
		}

		// Token: 0x06001AAA RID: 6826 RVA: 0x0008842C File Offset: 0x0008662C
		private TextObject GetTradeAgreementExplanation(ExplainedNumber explainedNumber)
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

		// Token: 0x06001AAB RID: 6827 RVA: 0x000884E4 File Offset: 0x000866E4
		public override CampaignTime GetTradeAgreementDurationInYears(Kingdom iniatatingKingdom, Kingdom otherKingdom)
		{
			return CampaignTime.Years(1f);
		}

		// Token: 0x06001AAC RID: 6828 RVA: 0x000884F0 File Offset: 0x000866F0
		private float GetExposureScoreToOtherKingdom(Kingdom kingdom1, Kingdom kingdom2)
		{
			HashSet<Settlement> hashSet = new HashSet<Settlement>();
			float num = 0f;
			float num2 = 0f;
			foreach (Town town in kingdom1.Fiefs)
			{
				foreach (Settlement settlement in town.GetNeighborFortifications(MobileParty.NavigationType.All))
				{
					if (settlement.MapFaction != kingdom1 && !hashSet.Contains(settlement))
					{
						if (settlement.MapFaction == kingdom2)
						{
							num2 += 1.6f;
						}
						num += 1.6f;
						hashSet.Add(settlement);
					}
				}
			}
			HashSet<Settlement> hashSet2 = new HashSet<Settlement>();
			foreach (Settlement settlement2 in hashSet)
			{
				foreach (Settlement settlement3 in settlement2.Town.GetNeighborFortifications(MobileParty.NavigationType.All))
				{
					if (settlement3.MapFaction != kingdom1 && !hashSet.Contains(settlement3) && !hashSet2.Contains(settlement3))
					{
						if (settlement3.MapFaction == kingdom2)
						{
							num2 += 0.3f;
						}
						num += 0.3f;
						hashSet2.Add(settlement3);
					}
				}
			}
			if (num2 < 0.3f)
			{
				return 0f;
			}
			return num;
		}

		// Token: 0x040008DC RID: 2268
		private const float FirstDegreeNeighborScore = 1.6f;

		// Token: 0x040008DD RID: 2269
		private const float SecondDegreeNeighborScore = 0.3f;

		// Token: 0x040008DE RID: 2270
		private const float MaxPeaceDurationBonus = 20f;

		// Token: 0x040008DF RID: 2271
		private const float RelationshipMultiplier = 0.25f;

		// Token: 0x040008E0 RID: 2272
		private const float MaxAssumedExposureBonus = 40f;

		// Token: 0x040008E1 RID: 2273
		private static readonly TextObject _kingdomsAtWarText = new TextObject("{=vo7kAlkR}The kingdoms are at war.", null);

		// Token: 0x040008E2 RID: 2274
		private static readonly TextObject _eliminatedKingdomText = new TextObject("{=ZeNt57yM}The kingdom is eliminated.", null);

		// Token: 0x040008E3 RID: 2275
		private static readonly TextObject _existingTradeAgreementText = new TextObject("{=8HXcla1b}These kingdoms already have a trade agreement.", null);

		// Token: 0x040008E4 RID: 2276
		private static readonly TextObject _maximumNumberOfTradeAgreementsText = new TextObject("{=DJ51OJWj}You already have maximum number of trade agreements.", null);

		// Token: 0x040008E5 RID: 2277
		private static readonly TextObject _limitedSharerBordersText = new TextObject("{=EapZFDGF}Limited shared borders", null);

		// Token: 0x040008E6 RID: 2278
		private static readonly TextObject _relationsText = new TextObject("{=3YVDMg5X}Low relations between rulers", null);

		// Token: 0x040008E7 RID: 2279
		private static readonly TextObject _recentWarText = new TextObject("{=lDIz0nEY}Recent war", null);

		// Token: 0x040008E8 RID: 2280
		private const int MaxReasonsInExplanation = 3;

		// Token: 0x040008E9 RID: 2281
		private ITradeAgreementsCampaignBehavior _tradeAgreementsBehavior;
	}
}
