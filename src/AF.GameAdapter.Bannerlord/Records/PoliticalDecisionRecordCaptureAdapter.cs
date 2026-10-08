using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using static AnimusForge.MemoryEntityIdentityBannerlordAdapter;
using static AnimusForge.CampaignCharacterRecordCaptureAdapter;
using static AnimusForge.WeeklyPoliticalMaterialPolicy;
using NpcActionFacts = AnimusForge.MyBehavior.NpcActionFacts;

namespace AnimusForge;

// Political decision material capture. Reads live game facts only on the original event thread;
// retains no game object, owns no ledger, and reuses the unique political material formatting rules.
internal sealed class PoliticalDecisionRecordCaptureAdapter
{
 internal delegate void MajorRecord(Hero hero,string text,string stableKey,NpcActionFacts facts=null,bool allowNonLordHero=false);
 internal delegate void RecentRecord(Hero hero,string text,string stableKey,bool dedupeAcrossWindow=false,NpcActionFacts facts=null,bool allowNonLordHero=false);
 internal delegate void MaterialRecord(string materialKind,string label,string snapshotText,string stableKey,string kingdomId,string settlementId,bool includeInWorld,bool includeInKingdom,string actorHeroId="",string actorKingdomId="",int dayOverride=-1,string gameDateOverride="");
 internal delegate void PlayerRecord(string text,string stableKey,string actionKind,bool isMajor,Hero targetHero,Settlement settlement,string locationText,bool? won);
 private readonly MajorRecord RecordNpcMajorAction;
 private readonly RecentRecord RecordNpcRecentAction;
 private readonly MaterialRecord RecordEventSourceMaterial;
 private readonly PlayerRecord RecordExternalPlayerAction;
 internal PoliticalDecisionRecordCaptureAdapter(MajorRecord major,RecentRecord recent,MaterialRecord material,PlayerRecord player)
 {RecordNpcMajorAction=major;RecordNpcRecentAction=recent;RecordEventSourceMaterial=material;RecordExternalPlayerAction=player;}

internal static string BuildKingdomDecisionStableKey(KingdomDecision decision)
	{
		string text = (decision?.GetType()?.Name ?? "decision").Trim();
		string text2 = GetKingdomId(decision?.Kingdom);
		string text3 = GetClanId(decision?.ProposerClan);
		string text4 = "";
		try
		{
			text4 = decision?.TriggerTime.ToString() ?? "";
		}
		catch
		{
			text4 = "";
		}
		return "kingdom_decision:" + text2 + ":" + text3 + ":" + text + ":" + text4;
	}

internal static string BuildKingdomDecisionNarrative(KingdomDecision decision, DecisionOutcome chosenOutcome, bool isPlayerInvolved, bool forProposer)
	{
		string text = "";
		try
		{
			text = new KingdomDecisionConcludedLogEntry(decision, chosenOutcome, isPlayerInvolved).GetNotificationText()?.ToString() ?? "";
		}
		catch
		{
			text = "";
		}
		text = (text ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			try
			{
				text = decision?.GetGeneralTitle()?.ToString() ?? "";
			}
			catch
			{
				text = "";
			}
		}
		string kingdomDisplayName = GetKingdomDisplayName(decision?.Kingdom, "该王国");
		if (string.IsNullOrWhiteSpace(text))
		{
			return (forProposer ? "你推动的" : "你所参与的") + kingdomDisplayName + "王国决议已经得出结果。";
		}
		return (forProposer ? "你推动的" : "你所参与的") + kingdomDisplayName + "王国决议已有结果：" + text;
	}

internal static string AppendEventMaterialSentence(string text, string addition)
	{
		string text2 = (text ?? "").Trim();
		string text3 = (addition ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text3))
		{
			return text2;
		}
		return string.IsNullOrWhiteSpace(text2) ? text3 : (text2 + " " + text3);
	}

internal static string BuildKingdomDecisionPoliticalTriggerReason(KingdomDecision decision, DecisionOutcome chosenOutcome)
	{
		try
		{
			if (decision is DeclareWarDecision declareWarDecision)
			{
				return BuildDeclareWarPoliticalTriggerReason(decision.Kingdom, declareWarDecision.FactionToDeclareWarOn, decision.ProposerClan, declareWarDecision, "KingdomDecision");
			}
			if (decision is MakePeaceKingdomDecision makePeaceKingdomDecision)
			{
				return BuildMakePeacePoliticalTriggerReason(decision.Kingdom, makePeaceKingdomDecision.FactionToMakePeaceWith, decision.ProposerClan, makePeaceKingdomDecision, "KingdomDecision");
			}
			if (decision is KingdomPolicyDecision kingdomPolicyDecision)
			{
				return BuildKingdomPolicyPoliticalTriggerReason(kingdomPolicyDecision);
			}
			if (decision is KingSelectionKingdomDecision kingSelectionKingdomDecision)
			{
				return BuildKingSelectionPoliticalTriggerReason(kingSelectionKingdomDecision, chosenOutcome);
			}
			if (decision is StartAllianceDecision startAllianceDecision)
			{
				return BuildStartAlliancePoliticalTriggerReason(startAllianceDecision, chosenOutcome);
			}
			if (decision is TradeAgreementDecision tradeAgreementDecision)
			{
				return BuildTradeAgreementPoliticalTriggerReason(tradeAgreementDecision, chosenOutcome);
			}
			if (decision is ProposeCallToWarAgreementDecision proposeCallToWarAgreementDecision)
			{
				return BuildProposeCallToWarPoliticalTriggerReason(proposeCallToWarAgreementDecision, chosenOutcome);
			}
			if (decision is AcceptCallToWarAgreementDecision acceptCallToWarAgreementDecision)
			{
				return BuildAcceptCallToWarPoliticalTriggerReason(acceptCallToWarAgreementDecision, chosenOutcome);
			}
			if (decision is ExpelClanFromKingdomDecision expelClanFromKingdomDecision)
			{
				return BuildExpelClanPoliticalTriggerReason(expelClanFromKingdomDecision, chosenOutcome);
			}
			if (decision is SettlementClaimantPreliminaryDecision settlementClaimantPreliminaryDecision)
			{
				return BuildSettlementClaimantPreliminaryPoliticalTriggerReason(settlementClaimantPreliminaryDecision, chosenOutcome);
			}
			if (decision is SettlementClaimantDecision settlementClaimantDecision)
			{
				return BuildSettlementClaimantPoliticalTriggerReason(settlementClaimantDecision, chosenOutcome);
			}
		}
		catch
		{
		}
		return "";
	}

internal static string BuildWarOrPeacePoliticalTriggerReason(string materialKind, IFaction faction1, IFaction faction2, string detailText)
	{
		try
		{
			if (string.Equals((materialKind ?? "").Trim(), "war_declared", StringComparison.OrdinalIgnoreCase))
			{
				return BuildDeclareWarPoliticalTriggerReason(faction1, faction2, ResolvePoliticalReasonEvaluatorClan(faction1), null, detailText);
			}
			if (string.Equals((materialKind ?? "").Trim(), "peace_made", StringComparison.OrdinalIgnoreCase))
			{
				return BuildMakePeacePoliticalTriggerReason(faction1, faction2, ResolvePoliticalReasonEvaluatorClan(faction1), null, detailText);
			}
		}
		catch
		{
		}
		return "";
	}

internal static string BuildDeclareWarPoliticalTriggerReason(IFaction declaringFaction, IFaction targetFaction, Clan evaluatingClan, DeclareWarDecision decision, string detailText)
	{
		try
		{
			if (declaringFaction == null || targetFaction == null)
			{
				return "";
			}
			Clan clan = evaluatingClan ?? ResolvePoliticalReasonEvaluatorClan(declaringFaction);
			List<string> parts = new List<string>();
			bool hasModelReason = false;
			if (clan != null)
			{
				try
				{
					var diplomacyModel = Campaign.Current?.Models?.DiplomacyModel;
					if (diplomacyModel != null)
					{
						TextObject reason;
						float score = diplomacyModel.GetScoreOfDeclaringWar(declaringFaction, targetFaction, clan, out reason, true);
						string text = NormalizePoliticalReasonText(reason?.ToString());
						if (!string.IsNullOrWhiteSpace(text))
						{
							parts.Add("原版理由：" + text);
							hasModelReason = true;
						}
						AddPoliticalScoreAgainstThresholdPart(parts, "宣战倾向", score, SafeGetPoliticalDecisionThreshold(targetFaction), "目标阈值");
					}
				}
				catch
				{
				}
				if (decision != null)
				{
					AddPoliticalReasonNumberPart(parts, "提案家族支持度", SafeCalculateDeclareWarSupport(decision, clan));
				}
			}
			AddPoliticalFallbackPartIfNeeded(parts, hasModelReason, "宣战", detailText);
			AddPoliticalFactionParameterParts(parts, declaringFaction, targetFaction, clan);
			return BuildPoliticalReasonSentence("宣战触发原因", parts);
		}
		catch
		{
			return "";
		}
	}

internal static string BuildMakePeacePoliticalTriggerReason(IFaction peaceFaction, IFaction targetFaction, Clan evaluatingClan, MakePeaceKingdomDecision decision, string detailText)
	{
		try
		{
			if (peaceFaction == null || targetFaction == null)
			{
				return "";
			}
			Clan clan = evaluatingClan ?? ResolvePoliticalReasonEvaluatorClan(peaceFaction);
			List<string> parts = new List<string>();
			bool hasModelReason = false;
			if (clan != null)
			{
				try
				{
					var diplomacyModel = Campaign.Current?.Models?.DiplomacyModel;
					if (diplomacyModel != null)
					{
						TextObject reason;
						float score = diplomacyModel.GetScoreOfDeclaringPeaceForClan(peaceFaction, targetFaction, clan, out reason, true);
						string text = NormalizePoliticalReasonText(reason?.ToString());
						if (!string.IsNullOrWhiteSpace(text))
						{
							parts.Add("原版理由：" + text);
							hasModelReason = true;
						}
						AddPoliticalScoreAgainstThresholdPart(parts, "议和倾向", score, SafeGetPoliticalDecisionThreshold(peaceFaction), "己方阈值");
					}
				}
				catch
				{
				}
				if (decision != null)
				{
					AddPoliticalReasonNumberPart(parts, "提案家族支持度", SafeCalculateMakePeaceSupport(decision, clan));
					string tributeText = BuildPeaceTributeParameter(decision, peaceFaction);
					if (!string.IsNullOrWhiteSpace(tributeText))
					{
						parts.Add(tributeText);
					}
				}
			}
			AddPoliticalFallbackPartIfNeeded(parts, hasModelReason, "议和", detailText);
			AddPoliticalFactionParameterParts(parts, peaceFaction, targetFaction, clan);
			return BuildPoliticalReasonSentence("议和触发原因", parts);
		}
		catch
		{
			return "";
		}
	}

internal static string BuildKingdomPolicyPoliticalTriggerReason(KingdomPolicyDecision decision)
	{
		try
		{
			if (decision == null || decision.Policy == null)
			{
				return "";
			}
			Clan proposerClan = decision.ProposerClan;
			PolicyObject policy = decision.Policy;
			bool isInverted = SafeGetKingdomPolicyDecisionIsInverted(decision);
			List<string> parts = new List<string>();
			string policyName = GetPolicyDisplayName(policy);
			parts.Add((isInverted ? "议题类型：废止政策“" : "议题类型：推行政策“") + policyName + "”");
			string dominantWeight = BuildPolicyDominantWeightPart(policy, isInverted);
			if (!string.IsNullOrWhiteSpace(dominantWeight))
			{
				parts.Add(dominantWeight);
			}
			string weightSummary = BuildPolicyWeightSummaryPart(policy);
			if (!string.IsNullOrWhiteSpace(weightSummary))
			{
				parts.Add(weightSummary);
			}
			string proposerPart = BuildPolicyProposerProfilePart(proposerClan);
			if (!string.IsNullOrWhiteSpace(proposerPart))
			{
				parts.Add(proposerPart);
			}
			string traitPart = BuildPolicyLeaderTraitPart(proposerClan?.Leader);
			if (!string.IsNullOrWhiteSpace(traitPart))
			{
				parts.Add(traitPart);
			}
			AddPolicySupportTendencyPart(parts, SafeCalculateKingdomPolicySupport(decision, proposerClan));
			return BuildPoliticalReasonSentence("政策触发原因", parts);
		}
		catch
		{
			return "";
		}
	}

internal static bool SafeGetKingdomPolicyDecisionIsInverted(KingdomPolicyDecision decision)
	{
		try
		{
			if (decision == null)
			{
				return false;
			}
			FieldInfo field = typeof(KingdomPolicyDecision).GetField("_isInvertedDecision", BindingFlags.Instance | BindingFlags.NonPublic);
			if (field?.GetValue(decision) is bool result)
			{
				return result;
			}
		}
		catch
		{
		}
		return false;
	}

internal static float? SafeCalculateKingdomPolicySupport(KingdomPolicyDecision decision, Clan clan)
	{
		try
		{
			return decision != null && clan != null ? decision.CalculateSupport(clan) : (float?)null;
		}
		catch
		{
			return null;
		}
	}

internal static string GetPolicyDisplayName(PolicyObject policy)
	{
		try
		{
			string text = NormalizePoliticalReasonText(policy?.Name?.ToString());
			return string.IsNullOrWhiteSpace(text) ? "未知政策" : text;
		}
		catch
		{
			return "未知政策";
		}
	}

internal static string BuildPolicyDominantWeightPart(PolicyObject policy, bool isInverted)
	{
		if (policy == null)
		{
			return "";
		}
		float egalitarianAbs = Math.Abs(policy.EgalitarianWeight);
		float oligarchicAbs = Math.Abs(policy.OligarchicWeight);
		float authoritarianAbs = Math.Abs(policy.AuthoritarianWeight);
		float max = Math.Max(egalitarianAbs, Math.Max(oligarchicAbs, authoritarianAbs));
		if (max < 0.001f)
		{
			return "权重方向：政策取向较中性";
		}
		string axis;
		float weight;
		if (egalitarianAbs >= oligarchicAbs && egalitarianAbs >= authoritarianAbs)
		{
			axis = "平民与地方共同体利益";
			weight = policy.EgalitarianWeight;
		}
		else if (oligarchicAbs >= authoritarianAbs)
		{
			axis = "贵族寡头与高阶家族利益";
			weight = policy.OligarchicWeight;
		}
		else
		{
			axis = "王权集中与统治家族利益";
			weight = policy.AuthoritarianWeight;
		}
		if (isInverted)
		{
			return "权重方向：废止议题会" + (weight >= 0f ? "削弱" : "强化") + axis;
		}
		return "权重方向：推行议题会" + (weight >= 0f ? "强化" : "削弱") + axis;
	}

internal static string BuildPolicyWeightSummaryPart(PolicyObject policy)
	{
		if (policy == null)
		{
			return "";
		}
		return "政策权重：平民 " + FormatPoliticalPolicyWeight(policy.EgalitarianWeight) + "、贵族 " + FormatPoliticalPolicyWeight(policy.OligarchicWeight) + "、王权 " + FormatPoliticalPolicyWeight(policy.AuthoritarianWeight);
	}

internal static string FormatPoliticalPolicyWeight(float value)
	{
		if (float.IsNaN(value) || float.IsInfinity(value))
		{
			return "0";
		}
		string text = value.ToString("0.##");
		return value > 0f ? ("+" + text) : text;
	}

internal static string BuildPolicyProposerProfilePart(Clan clan)
	{
		if (clan == null)
		{
			return "";
		}
		string clanName = GetClanDisplayName(clan);
		try
		{
			if (clan.Kingdom != null && clan.Kingdom.RulingClan == clan)
			{
				return "提案家族：" + clanName + "是统治家族，公式中更偏王权集中";
			}
			if (clan.IsMinorFaction)
			{
				return "提案家族：" + clanName + "是小派系，公式中更偏平民取向";
			}
			if (clan.Tier >= 3)
			{
				return "提案家族：" + clanName + "是高阶贵族家族（等级 " + clan.Tier + "），公式中更偏贵族寡头";
			}
			if (clan.Tier == 2)
			{
				return "提案家族：" + clanName + "是二阶家族，公式中略偏贵族寡头";
			}
			return "提案家族：" + clanName + "是低阶家族（等级 " + clan.Tier + "），身份修正较弱";
		}
		catch
		{
			return "";
		}
	}

internal static string BuildPolicyLeaderTraitPart(Hero leader)
	{
		try
		{
			if (leader == null)
			{
				return "";
			}
			List<string> parts = new List<string>();
			AddPolicyLeaderTraitLabel(parts, "平民", leader.GetTraitLevel(DefaultTraits.Egalitarian));
			AddPolicyLeaderTraitLabel(parts, "贵族寡头", leader.GetTraitLevel(DefaultTraits.Oligarchic));
			AddPolicyLeaderTraitLabel(parts, "王权", leader.GetTraitLevel(DefaultTraits.Authoritarian));
			if (parts.Count == 0)
			{
				return "";
			}
			return "领袖政治性格：" + string.Join("、", parts);
		}
		catch
		{
			return "";
		}
	}

internal static void AddPolicyLeaderTraitLabel(List<string> parts, string label, int level)
	{
		if (parts == null || string.IsNullOrWhiteSpace(label) || level == 0)
		{
			return;
		}
		parts.Add(label.Trim() + " " + (level > 0 ? "+" : "") + level);
	}

internal static void AddPolicySupportTendencyPart(List<string> parts, float? support)
	{
		if (parts == null || !support.HasValue || float.IsNaN(support.Value) || float.IsInfinity(support.Value))
		{
			return;
		}
		string supportText = FormatPoliticalReasonNumber(support);
		if (string.IsNullOrWhiteSpace(supportText))
		{
			return;
		}
		parts.Add("提案家族" + GetPolicySupportTendencyLabel(support.Value) + "（支持度 " + supportText + "）");
	}

internal static string GetPolicySupportTendencyLabel(float support)
	{
		if (support >= 100f)
		{
			return "强烈支持该议题";
		}
		if (support >= 35f)
		{
			return "支持该议题";
		}
		if (support > 0f)
		{
			return "轻度支持该议题";
		}
		if (support <= -100f)
		{
			return "强烈反对该议题";
		}
		if (support <= -35f)
		{
			return "反对该议题";
		}
		return "倾向反对该议题";
	}

internal static string BuildKingSelectionPoliticalTriggerReason(KingSelectionKingdomDecision decision, DecisionOutcome chosenOutcome)
	{
		try
		{
			if (decision == null || chosenOutcome == null)
			{
				return "";
			}
			Hero chosenKing = ResolveKingSelectionOutcomeKing(chosenOutcome);
			if (chosenKing == null)
			{
				return "";
			}
			Kingdom kingdom = decision.Kingdom;
			if (kingdom == null)
			{
				kingdom = chosenKing.MapFaction as Kingdom;
			}
			List<string> parts = new List<string>();
			parts.Add("议题类型：选择" + GetKingdomDisplayName(kingdom, "该王国") + "新统治者，最终由" + GetHeroDisplayName(chosenKing) + "继位");
			AddKingSelectionFormulaRulePart(parts);
			AddKingSelectionCandidateProfileParts(parts, chosenKing, kingdom);
			AddKingSelectionCandidateRankingPart(parts, decision, chosenKing);
			AddKingSelectionOutcomeScorePart(parts, chosenOutcome);
			AddKingSelectionVoteSupportPart(parts, chosenOutcome);
			AddKingSelectionVoterTraitPart(parts, kingdom);
			return BuildPoliticalReasonSentence("统治者选择触发原因", parts);
		}
		catch
		{
			return "";
		}
	}

internal static Hero ResolveKingSelectionOutcomeKing(DecisionOutcome chosenOutcome)
	{
		try
		{
			return (chosenOutcome as KingSelectionKingdomDecision.KingSelectionDecisionOutcome)?.King;
		}
		catch
		{
			return null;
		}
	}

internal static void AddKingSelectionFormulaRulePart(List<string> parts)
	{
		if (parts == null)
		{
			return;
		}
		parts.Add("原版候选池：符合资格的家族按家族强度取前三");
		parts.Add("原版评分因素：候选家族强度、王族延续偏好、各家族投票支持");
	}

internal static void AddKingSelectionCandidateProfileParts(List<string> parts, Hero chosenKing, Kingdom kingdom)
	{
		if (parts == null || chosenKing?.Clan == null)
		{
			return;
		}
		Clan clan = chosenKing.Clan;
		List<string> profileParts = new List<string>();
		profileParts.Add("候选家族：" + GetClanDisplayName(clan));
		profileParts.Add("等级 " + clan.Tier);
		string strengthText = FormatPoliticalReasonNumber(SafeGetDiplomacyClanStrength(clan));
		if (!string.IsNullOrWhiteSpace(strengthText))
		{
			profileParts.Add("家族强度 " + strengthText);
		}
		string influenceText = FormatPoliticalReasonNumber(SafeGetClanInfluence(clan));
		if (!string.IsNullOrWhiteSpace(influenceText))
		{
			profileParts.Add("影响力 " + influenceText);
		}
		string renownText = FormatPoliticalReasonNumber(SafeGetClanRenown(clan));
		if (!string.IsNullOrWhiteSpace(renownText))
		{
			profileParts.Add("声望 " + renownText);
		}
		parts.Add(string.Join("、", profileParts));
		AddKingSelectionCandidateFiefPart(parts, clan, kingdom);
		AddKingSelectionCandidateWarPartyPart(parts, clan);
	}

internal static float? SafeGetDiplomacyClanStrength(Clan clan)
	{
		try
		{
			return clan != null && Campaign.Current?.Models?.DiplomacyModel != null ? Campaign.Current.Models.DiplomacyModel.GetClanStrength(clan) : (float?)null;
		}
		catch
		{
			return null;
		}
	}

internal static void AddKingSelectionCandidateFiefPart(List<string> parts, Clan clan, Kingdom kingdom)
	{
		if (parts == null || clan == null)
		{
			return;
		}
		try
		{
			int count = 0;
			float totalValue = 0f;
			IEnumerable<Settlement> settlements = Enumerable.Empty<Settlement>();
			if (clan.Settlements != null)
			{
				settlements = clan.Settlements;
			}
			foreach (Settlement settlement in settlements)
			{
				if (settlement == null || !settlement.IsFortification)
				{
					continue;
				}
				count++;
				totalValue += SafeGetSettlementValueForKingdom(settlement, kingdom) ?? 0f;
			}
			if (count <= 0)
			{
				parts.Add("候选家族没有可统计城镇或城堡");
				return;
			}
			parts.Add("候选家族掌握要塞 " + count + " 处，封地价值 " + FormatPoliticalReasonNumber(totalValue));
		}
		catch
		{
		}
	}

internal static void AddKingSelectionCandidateWarPartyPart(List<string> parts, Clan clan)
	{
		if (parts == null || clan == null)
		{
			return;
		}
		try
		{
			int partyCount = 0;
			int troopCount = 0;
			IEnumerable<WarPartyComponent> warPartyComponents = Enumerable.Empty<WarPartyComponent>();
			if (clan.WarPartyComponents != null)
			{
				warPartyComponents = clan.WarPartyComponents;
			}
			foreach (WarPartyComponent warPartyComponent in warPartyComponents)
			{
				if (warPartyComponent?.MobileParty == null)
				{
					continue;
				}
				partyCount++;
				troopCount += warPartyComponent.MobileParty.MemberRoster.TotalManCount;
			}
			if (partyCount <= 0)
			{
				parts.Add("候选家族无可统计战争部队");
				return;
			}
			parts.Add("候选家族战争部队 " + partyCount + " 支、兵员 " + troopCount);
		}
		catch
		{
		}
	}

internal static void AddKingSelectionCandidateRankingPart(List<string> parts, KingSelectionKingdomDecision decision, Hero chosenKing)
	{
		if (parts == null || decision == null || chosenKing?.Clan == null)
		{
			return;
		}
		try
		{
			Kingdom kingdom = decision.Kingdom;
			var diplomacyModel = Campaign.Current?.Models?.DiplomacyModel;
			if (kingdom?.Clans == null || diplomacyModel == null)
			{
				return;
			}
			Clan excludedClan = SafeGetKingSelectionClanToExclude(decision);
			List<Tuple<Clan, float>> eligible = new List<Tuple<Clan, float>>();
			foreach (Clan clan in kingdom.Clans)
			{
				if (clan == null || clan == excludedClan)
				{
					continue;
				}
				if (!diplomacyModel.IsClanEligibleToBecomeRuler(clan))
				{
					continue;
				}
				eligible.Add(Tuple.Create(clan, diplomacyModel.GetClanStrength(clan)));
			}
			eligible = eligible.OrderByDescending((Tuple<Clan, float> x) => x.Item2).ToList();
			if (eligible.Count == 0)
			{
				return;
			}
			string chosenClanId = GetClanId(chosenKing.Clan);
			int fullIndex = eligible.FindIndex((Tuple<Clan, float> x) => string.Equals(GetClanId(x.Item1), chosenClanId, StringComparison.OrdinalIgnoreCase));
			if (fullIndex < 0)
			{
				return;
			}
			List<Tuple<Clan, float>> candidatePool = eligible.Take(3).ToList();
			int poolIndex = candidatePool.FindIndex((Tuple<Clan, float> x) => string.Equals(GetClanId(x.Item1), chosenClanId, StringComparison.OrdinalIgnoreCase));
			parts.Add("家族强度排名：" + (fullIndex + 1) + "/" + eligible.Count + "（强度 " + FormatPoliticalReasonNumber(eligible[fullIndex].Item2) + "）");
			if (poolIndex >= 0)
			{
				parts.Add("候选池排名：" + (poolIndex + 1) + "/" + candidatePool.Count);
			}
			if (fullIndex > 0)
			{
				parts.Add("家族强度最高者为" + GetClanDisplayName(eligible[0].Item1) + "，最终结果还受到议会支持和结算评分影响");
			}
		}
		catch
		{
		}
	}

internal static Clan SafeGetKingSelectionClanToExclude(KingSelectionKingdomDecision decision)
	{
		try
		{
			if (decision == null)
			{
				return null;
			}
			FieldInfo field = typeof(KingSelectionKingdomDecision).GetField("_clanToExclude", BindingFlags.Instance | BindingFlags.NonPublic);
			return field?.GetValue(decision) as Clan;
		}
		catch
		{
			return null;
		}
	}

internal static void AddKingSelectionOutcomeScorePart(List<string> parts, DecisionOutcome chosenOutcome)
	{
		if (parts == null || chosenOutcome == null)
		{
			return;
		}
		string initialText = FormatPoliticalReasonNumber(chosenOutcome.InitialMerit);
		string meritText = FormatPoliticalReasonNumber(chosenOutcome.Merit);
		if (string.IsNullOrWhiteSpace(initialText) && string.IsNullOrWhiteSpace(meritText))
		{
			return;
		}
		if (!string.IsNullOrWhiteSpace(initialText) && !string.IsNullOrWhiteSpace(meritText))
		{
			parts.Add("结算保存评分：基础 " + initialText + "，投票后 " + meritText);
			return;
		}
		parts.Add("结算保存评分：" + (string.IsNullOrWhiteSpace(meritText) ? initialText : meritText));
	}

internal static void AddKingSelectionVoteSupportPart(List<string> parts, DecisionOutcome chosenOutcome)
	{
		if (parts == null || chosenOutcome == null)
		{
			return;
		}
		try
		{
			int slight = 0;
			int strong = 0;
			int full = 0;
			int other = 0;
			foreach (Supporter supporter in chosenOutcome.SupporterList ?? new List<Supporter>())
			{
				if (supporter?.Clan == null)
				{
					continue;
				}
				switch (supporter.SupportWeight)
				{
				case Supporter.SupportWeights.SlightlyFavor:
					slight++;
					break;
				case Supporter.SupportWeights.StronglyFavor:
					strong++;
					break;
				case Supporter.SupportWeights.FullyPush:
					full++;
					break;
				default:
					other++;
					break;
				}
			}
			int total = slight + strong + full + other;
			if (total <= 0)
			{
				return;
			}
			List<string> labels = new List<string>();
			if (slight > 0)
			{
				labels.Add("轻度 " + slight);
			}
			if (strong > 0)
			{
				labels.Add("强力 " + strong);
			}
			if (full > 0)
			{
				labels.Add("全力 " + full);
			}
			if (other > 0)
			{
				labels.Add("其他 " + other);
			}
			parts.Add("投票支持：" + total + "个家族支持获选者" + (labels.Count > 0 ? "（" + string.Join("、", labels) + "）" : ""));
		}
		catch
		{
		}
	}

internal static void AddKingSelectionVoterTraitPart(List<string> parts, Kingdom kingdom)
	{
		if (parts == null || kingdom?.Clans == null)
		{
			return;
		}
		try
		{
			int authoritarian = 0;
			int oligarchic = 0;
			int other = 0;
			foreach (Clan clan in kingdom.Clans)
			{
				Hero leader = clan?.Leader;
				if (leader == null || leader == Hero.MainHero)
				{
					continue;
				}
				if (leader.GetTraitLevel(DefaultTraits.Authoritarian) > 0)
				{
					authoritarian++;
				}
				else if (leader.GetTraitLevel(DefaultTraits.Oligarchic) > 0)
				{
					oligarchic++;
				}
				else
				{
					other++;
				}
			}
			if (authoritarian + oligarchic + other <= 0)
			{
				return;
			}
			parts.Add("王国内投票性格：王权倾向 " + authoritarian + "、贵族寡头倾向 " + oligarchic + "、其他 " + other + "；这些性格会影响王族延续加权");
		}
		catch
		{
		}
	}

internal static string BuildStartAlliancePoliticalTriggerReason(StartAllianceDecision decision, DecisionOutcome chosenOutcome)
	{
		try
		{
			if (decision == null || decision.KingdomToStartAllianceWith == null)
			{
				return "";
			}
			Kingdom sourceKingdom = decision.Kingdom;
			Kingdom targetKingdom = decision.KingdomToStartAllianceWith;
			bool? shouldStart = SafeGetStartAllianceOutcomeShouldStart(chosenOutcome);
			List<string> parts = new List<string>();
			parts.Add("议题类型：与" + GetKingdomDisplayName(targetKingdom, "目标王国") + "结盟，最终" + GetAgreementOutcomeLabel(shouldStart, "结盟", "拒绝结盟"));
			string reasonText;
			float? score = SafeGetStartAllianceSupportScore(decision, decision.ProposerClan, out reasonText);
			AddAgreementModelScorePart(parts, "提案家族结盟倾向", score, reasonText);
			AddAgreementFinalSupportPart(parts, decision, decision.ProposerClan, chosenOutcome);
			AddAgreementVoteSupportPart(parts, chosenOutcome, shouldStart, "结盟", "拒绝结盟");
			AddKingdomDiplomaticStatusPart(parts, sourceKingdom, targetKingdom);
			AddPoliticalFactionParameterParts(parts, sourceKingdom, targetKingdom, decision.ProposerClan);
			return BuildPoliticalReasonSentence("联盟协议触发原因", parts);
		}
		catch
		{
			return "";
		}
	}

internal static string BuildTradeAgreementPoliticalTriggerReason(TradeAgreementDecision decision, DecisionOutcome chosenOutcome)
	{
		try
		{
			if (decision == null || decision.TargetKingdom == null)
			{
				return "";
			}
			Kingdom sourceKingdom = decision.Kingdom;
			Kingdom targetKingdom = decision.TargetKingdom;
			bool? shouldStart = SafeGetTradeAgreementOutcomeShouldStart(chosenOutcome);
			List<string> parts = new List<string>();
			parts.Add("议题类型：与" + GetKingdomDisplayName(targetKingdom, "目标王国") + "签署贸易协议，最终" + GetAgreementOutcomeLabel(shouldStart, "签署", "拒绝签署"));
			string reasonText;
			float? score = SafeGetTradeAgreementSupportScore(sourceKingdom, targetKingdom, decision.ProposerClan, out reasonText);
			AddTradeAgreementScorePart(parts, score, reasonText);
			AddAgreementFinalSupportPart(parts, decision, decision.ProposerClan, chosenOutcome);
			AddAgreementVoteSupportPart(parts, chosenOutcome, shouldStart, "签署", "拒绝签署");
			AddKingdomDiplomaticStatusPart(parts, sourceKingdom, targetKingdom);
			AddPoliticalFactionParameterParts(parts, sourceKingdom, targetKingdom, decision.ProposerClan);
			return BuildPoliticalReasonSentence("贸易协议触发原因", parts);
		}
		catch
		{
			return "";
		}
	}

internal static string BuildProposeCallToWarPoliticalTriggerReason(ProposeCallToWarAgreementDecision decision, DecisionOutcome chosenOutcome)
	{
		try
		{
			if (decision == null || decision.CalledKingdom == null || decision.KingdomToCallToWarAgainst == null)
			{
				return "";
			}
			Kingdom sourceKingdom = decision.Kingdom;
			Kingdom calledKingdom = decision.CalledKingdom;
			Kingdom targetKingdom = decision.KingdomToCallToWarAgainst;
			bool? shouldCall = SafeGetProposeCallToWarOutcomeShouldCall(chosenOutcome);
			List<string> parts = new List<string>();
			parts.Add("议题类型：召唤盟友" + GetKingdomDisplayName(calledKingdom, "盟友王国") + "对" + GetKingdomDisplayName(targetKingdom, "敌对王国") + "参战，最终" + GetAgreementOutcomeLabel(shouldCall, "召战", "不召战"));
			parts.Add("召战费用 " + decision.CallToWarCost);
			string reasonText;
			float? score = SafeGetCallingToWarScore(sourceKingdom, calledKingdom, targetKingdom, decision.ProposerClan, out reasonText);
			AddAgreementModelScorePart(parts, "提案家族召战倾向", score, reasonText);
			AddAgreementFinalSupportPart(parts, decision, decision.ProposerClan, chosenOutcome);
			AddAgreementVoteSupportPart(parts, chosenOutcome, shouldCall, "召战", "不召战");
			AddCallToWarDiplomaticStatusParts(parts, sourceKingdom, calledKingdom, targetKingdom, isAcceptanceDecision: false);
			AddCallToWarStrengthParts(parts, sourceKingdom, calledKingdom, targetKingdom);
			return BuildPoliticalReasonSentence("召盟友参战触发原因", parts);
		}
		catch
		{
			return "";
		}
	}

internal static string BuildAcceptCallToWarPoliticalTriggerReason(AcceptCallToWarAgreementDecision decision, DecisionOutcome chosenOutcome)
	{
		try
		{
			if (decision == null || decision.CallingKingdom == null || decision.KingdomToCallToWarAgainst == null)
			{
				return "";
			}
			Kingdom calledKingdom = decision.Kingdom;
			Kingdom callingKingdom = decision.CallingKingdom;
			Kingdom targetKingdom = decision.KingdomToCallToWarAgainst;
			bool? shouldAccept = SafeGetAcceptCallToWarOutcomeShouldAccept(chosenOutcome);
			List<string> parts = new List<string>();
			parts.Add("议题类型：响应盟友" + GetKingdomDisplayName(callingKingdom, "召战王国") + "，对" + GetKingdomDisplayName(targetKingdom, "敌对王国") + "参战，最终" + GetAgreementOutcomeLabel(shouldAccept, "应召参战", "拒绝参战"));
			parts.Add("应召付款 " + decision.CallToWarCost);
			string reasonText;
			float? score = SafeGetJoiningWarScore(callingKingdom, calledKingdom, targetKingdom, decision.ProposerClan, out reasonText);
			AddAgreementModelScorePart(parts, "提案家族应召倾向", score, reasonText);
			AddAgreementFinalSupportPart(parts, decision, decision.ProposerClan, chosenOutcome);
			AddAgreementVoteSupportPart(parts, chosenOutcome, shouldAccept, "应召参战", "拒绝参战");
			AddCallToWarDiplomaticStatusParts(parts, callingKingdom, calledKingdom, targetKingdom, isAcceptanceDecision: true);
			AddCallToWarStrengthParts(parts, calledKingdom, callingKingdom, targetKingdom);
			return BuildPoliticalReasonSentence("应召参战触发原因", parts);
		}
		catch
		{
			return "";
		}
	}

internal static bool? SafeGetStartAllianceOutcomeShouldStart(DecisionOutcome chosenOutcome)
	{
		try
		{
			if (chosenOutcome is StartAllianceDecision.StartAllianceDecisionOutcome outcome)
			{
				return outcome.ShouldAllianceBeStarted;
			}
		}
		catch
		{
		}
		return null;
	}

internal static bool? SafeGetTradeAgreementOutcomeShouldStart(DecisionOutcome chosenOutcome)
	{
		try
		{
			if (chosenOutcome is TradeAgreementDecision.TradeAgreementDecisionOutcome outcome)
			{
				return outcome.ShouldTradeAgreementStart;
			}
		}
		catch
		{
		}
		return null;
	}

internal static bool? SafeGetProposeCallToWarOutcomeShouldCall(DecisionOutcome chosenOutcome)
	{
		try
		{
			if (chosenOutcome is ProposeCallToWarAgreementDecision.ProposeCallToWarAgreementDecisionOutcome outcome)
			{
				return outcome.ShouldCallToWar;
			}
		}
		catch
		{
		}
		return null;
	}

internal static bool? SafeGetAcceptCallToWarOutcomeShouldAccept(DecisionOutcome chosenOutcome)
	{
		try
		{
			if (chosenOutcome is AcceptCallToWarAgreementDecision.AcceptCallToWarAgreementDecisionOutcome outcome)
			{
				return outcome.ShouldAcceptCallToWar;
			}
		}
		catch
		{
		}
		return null;
	}

internal static string GetAgreementOutcomeLabel(bool? accepted, string positiveLabel, string negativeLabel)
	{
		if (!accepted.HasValue)
		{
			return "已结算";
		}
		return accepted.Value ? positiveLabel : negativeLabel;
	}

internal static float? SafeGetStartAllianceSupportScore(StartAllianceDecision decision, Clan clan, out string reasonText)
	{
		reasonText = "";
		try
		{
			if (decision == null || clan == null)
			{
				return null;
			}
			TextObject hint;
			float score = decision.CalculateSupport(clan, out hint, true);
			reasonText = NormalizePoliticalReasonText(hint?.ToString());
			return score;
		}
		catch
		{
			reasonText = "";
			return null;
		}
	}

internal static float? SafeGetTradeAgreementSupportScore(Kingdom sourceKingdom, Kingdom targetKingdom, Clan clan, out string reasonText)
	{
		reasonText = "";
		try
		{
			var tradeModel = Campaign.Current?.Models?.TradeAgreementModel;
			if (sourceKingdom == null || targetKingdom == null || clan == null || tradeModel == null)
			{
				return null;
			}
			TextObject hint;
			float score = tradeModel.GetScoreOfStartingTradeAgreement(sourceKingdom, targetKingdom, clan, out hint, true);
			reasonText = NormalizePoliticalReasonText(hint?.ToString());
			return score;
		}
		catch
		{
			reasonText = "";
			return null;
		}
	}

internal static float? SafeGetCallingToWarScore(Kingdom callingKingdom, Kingdom calledKingdom, Kingdom targetKingdom, Clan clan, out string reasonText)
	{
		reasonText = "";
		try
		{
			var allianceModel = Campaign.Current?.Models?.AllianceModel;
			if (callingKingdom == null || calledKingdom == null || targetKingdom == null || clan == null || allianceModel == null)
			{
				return null;
			}
			TextObject hint;
			float score = allianceModel.GetScoreOfCallingToWar(callingKingdom, calledKingdom, targetKingdom, clan, out hint);
			reasonText = NormalizePoliticalReasonText(hint?.ToString());
			return score;
		}
		catch
		{
			reasonText = "";
			return null;
		}
	}

internal static float? SafeGetJoiningWarScore(Kingdom callingKingdom, Kingdom calledKingdom, Kingdom targetKingdom, Clan clan, out string reasonText)
	{
		reasonText = "";
		try
		{
			var allianceModel = Campaign.Current?.Models?.AllianceModel;
			if (callingKingdom == null || calledKingdom == null || targetKingdom == null || clan == null || allianceModel == null)
			{
				return null;
			}
			TextObject hint;
			float score = allianceModel.GetScoreOfJoiningWar(callingKingdom, calledKingdom, targetKingdom, clan, out hint);
			reasonText = NormalizePoliticalReasonText(hint?.ToString());
			return score;
		}
		catch
		{
			reasonText = "";
			return null;
		}
	}

internal static void AddAgreementModelScorePart(List<string> parts, string label, float? score, string reasonText)
	{
		if (parts == null)
		{
			return;
		}
		string scoreText = FormatPoliticalReasonNumber(score);
		if (!string.IsNullOrWhiteSpace(scoreText))
		{
			string tendency = score.HasValue && score.Value >= 0f ? "支持" : "反对";
			parts.Add((string.IsNullOrWhiteSpace(label) ? "模型倾向" : label.Trim()) + "：" + tendency + "（评分 " + scoreText + "）");
		}
		AddAgreementReasonTextPart(parts, reasonText);
	}

internal static void AddTradeAgreementScorePart(List<string> parts, float? score, string reasonText)
	{
		if (parts == null)
		{
			return;
		}
		string scoreText = FormatPoliticalReasonNumber(score);
		if (!string.IsNullOrWhiteSpace(scoreText))
		{
			string tendency = score.HasValue && score.Value >= 50f ? "倾向签署" : "倾向拒绝";
			parts.Add("提案家族贸易协议倾向：" + tendency + "（评分 " + scoreText + "/阈值 50）");
		}
		AddAgreementReasonTextPart(parts, reasonText);
	}

internal static void AddAgreementReasonTextPart(List<string> parts, string reasonText)
	{
		if (parts == null)
		{
			return;
		}
		string text = NormalizePoliticalReasonText(reasonText);
		if (!string.IsNullOrWhiteSpace(text))
		{
			parts.Add("原版理由：" + text);
		}
	}

internal static void AddAgreementFinalSupportPart(List<string> parts, KingdomDecision decision, Clan clan, DecisionOutcome chosenOutcome)
	{
		float? support = SafeDetermineKingdomDecisionSupport(decision, clan, chosenOutcome);
		AddSettlementSupportTendencyPart(parts, "提案家族对最终选项", support);
	}

internal static void AddAgreementVoteSupportPart(List<string> parts, DecisionOutcome chosenOutcome, bool? accepted, string positiveLabel, string negativeLabel)
	{
		if (parts == null || chosenOutcome == null)
		{
			return;
		}
		try
		{
			int count = chosenOutcome.SupporterList?.Count((Supporter x) => x?.Clan != null) ?? 0;
			if (count <= 0)
			{
				return;
			}
			string action = accepted.HasValue ? (accepted.Value ? positiveLabel : negativeLabel) : "最终选项";
			parts.Add("投票支持：" + count + "个家族支持" + action);
		}
		catch
		{
		}
	}

internal static void AddKingdomDiplomaticStatusPart(List<string> parts, Kingdom sourceKingdom, Kingdom targetKingdom)
	{
		if (parts == null || sourceKingdom == null || targetKingdom == null)
		{
			return;
		}
		try
		{
			string sourceName = GetKingdomDisplayName(sourceKingdom, "己方王国");
			string targetName = GetKingdomDisplayName(targetKingdom, "目标王国");
			if (sourceKingdom.IsAtWarWith(targetKingdom))
			{
				parts.Add(sourceName + "与" + targetName + "当前处于战争状态");
			}
			else if (sourceKingdom.IsAllyWith(targetKingdom))
			{
				parts.Add(sourceName + "与" + targetName + "当前已是盟友");
			}
			else
			{
				parts.Add(sourceName + "与" + targetName + "当前未交战");
			}
		}
		catch
		{
		}
	}

internal static void AddCallToWarDiplomaticStatusParts(List<string> parts, Kingdom callingKingdom, Kingdom calledKingdom, Kingdom targetKingdom, bool isAcceptanceDecision)
	{
		if (parts == null || callingKingdom == null || calledKingdom == null || targetKingdom == null)
		{
			return;
		}
		try
		{
			string callingName = GetKingdomDisplayName(callingKingdom, "召战方");
			string calledName = GetKingdomDisplayName(calledKingdom, "应召方");
			string targetName = GetKingdomDisplayName(targetKingdom, "敌对王国");
			parts.Add(callingName + "与" + calledName + (callingKingdom.IsAllyWith(calledKingdom) ? "是盟友" : "不是盟友"));
			parts.Add(callingName + "与" + targetName + (callingKingdom.IsAtWarWith(targetKingdom) ? "已经交战" : "未交战"));
			parts.Add(calledName + "与" + targetName + (calledKingdom.IsAtWarWith(targetKingdom) ? "已经交战" : "尚未交战"));
			if (isAcceptanceDecision)
			{
				parts.Add("该议题由盟友召战请求触发");
			}
			else
			{
				parts.Add("该议题由主动召唤盟友参战触发");
			}
		}
		catch
		{
		}
	}

internal static void AddCallToWarStrengthParts(List<string> parts, Kingdom firstKingdom, Kingdom secondKingdom, Kingdom targetKingdom)
	{
		if (parts == null)
		{
			return;
		}
		string firstPart = BuildPoliticalStrengthComparisonPart(firstKingdom, targetKingdom, SafeGetFactionStrength(firstKingdom), SafeGetFactionStrength(targetKingdom));
		if (!string.IsNullOrWhiteSpace(firstPart))
		{
			parts.Add(firstPart);
		}
		string secondPart = BuildPoliticalStrengthComparisonPart(secondKingdom, targetKingdom, SafeGetFactionStrength(secondKingdom), SafeGetFactionStrength(targetKingdom));
		if (!string.IsNullOrWhiteSpace(secondPart))
		{
			parts.Add(secondPart);
		}
	}

internal static string BuildExpelClanPoliticalTriggerReason(ExpelClanFromKingdomDecision decision, DecisionOutcome chosenOutcome)
	{
		try
		{
			if (decision == null || decision.ClanToExpel == null)
			{
				return "";
			}
			Clan targetClan = decision.ClanToExpel;
			Kingdom kingdom = decision.OldKingdom ?? decision.Kingdom ?? targetClan.Kingdom;
			bool? shouldExpel = SafeGetExpelClanOutcomeShouldBeExpelled(chosenOutcome);
			List<string> parts = new List<string>();
			string kingdomName = GetKingdomDisplayName(kingdom, "该王国");
			string clanName = GetClanDisplayName(targetClan);
			parts.Add(shouldExpel.HasValue ? ("议题类型：是否将" + clanName + "逐出" + kingdomName + "，最终" + (shouldExpel.Value ? "驱逐" : "保留")) : ("议题类型：是否将" + clanName + "逐出" + kingdomName));
			AddExpelClanTargetProfileParts(parts, targetClan, kingdom);
			AddExpelClanRelationNetworkParts(parts, targetClan, kingdom);
			AddExpelClanRelationToKeyClanPart(parts, "与提案家族关系", targetClan, decision.ProposerClan);
			AddExpelClanRelationToKeyClanPart(parts, "与统治家族关系", targetClan, kingdom?.RulingClan);
			AddExpelClanDefaultFormulaPart(parts, shouldExpel);
			AddExpelClanVoteSupportPart(parts, chosenOutcome, shouldExpel);
			AddExpelClanRelationCostPart(parts, shouldExpel);
			AddExpelClanFormulaValuePart(parts, decision, chosenOutcome, shouldExpel);
			return BuildPoliticalReasonSentence("驱逐家族触发原因", parts);
		}
		catch
		{
			return "";
		}
	}

internal static bool? SafeGetExpelClanOutcomeShouldBeExpelled(DecisionOutcome chosenOutcome)
	{
		try
		{
			if (chosenOutcome is ExpelClanFromKingdomDecision.ExpelClanDecisionOutcome expelOutcome)
			{
				return expelOutcome.ShouldBeExpelled;
			}
		}
		catch
		{
		}
		return null;
	}

internal static void AddExpelClanTargetProfileParts(List<string> parts, Clan targetClan, Kingdom kingdom)
	{
		if (parts == null || targetClan == null)
		{
			return;
		}
		parts.Add("目标家族等级 " + targetClan.Tier + "、影响力 " + FormatPoliticalReasonNumber(SafeGetClanInfluence(targetClan)) + "、声望 " + FormatPoliticalReasonNumber(SafeGetClanRenown(targetClan)));
		string strengthText = FormatPoliticalReasonNumber(SafeGetClanStrength(targetClan));
		if (!string.IsNullOrWhiteSpace(strengthText))
		{
			parts.Add("目标家族野战力量 " + strengthText);
		}
		AddExpelClanFiefValueParts(parts, targetClan, kingdom);
		AddExpelClanWarPartyParts(parts, targetClan);
	}

internal static float? SafeGetClanInfluence(Clan clan)
	{
		try
		{
			return clan?.Influence;
		}
		catch
		{
			return null;
		}
	}

internal static float? SafeGetClanRenown(Clan clan)
	{
		try
		{
			return clan?.Renown;
		}
		catch
		{
			return null;
		}
	}

internal static void AddExpelClanFiefValueParts(List<string> parts, Clan targetClan, Kingdom kingdom)
	{
		if (parts == null || targetClan == null)
		{
			return;
		}
		try
		{
			int count = 0;
			float totalValue = 0f;
			IEnumerable<Settlement> settlements = Enumerable.Empty<Settlement>();
			if (targetClan.Settlements != null)
			{
				settlements = targetClan.Settlements;
			}
			foreach (Settlement settlement in settlements)
			{
				if (settlement == null)
				{
					continue;
				}
				count++;
				totalValue += SafeGetSettlementValueForKingdom(settlement, kingdom) ?? 0f;
			}
			if (count <= 0)
			{
				parts.Add("目标家族没有可统计封地，封地价值不会提高保留权重");
				return;
			}
			parts.Add("目标家族封地 " + count + " 处，封地价值 " + FormatPoliticalReasonNumber(totalValue) + " 会提高保留该家族的公式权重");
		}
		catch
		{
		}
	}

internal static float? SafeGetSettlementValueForKingdom(Settlement settlement, Kingdom kingdom)
	{
		try
		{
			return settlement != null && kingdom != null ? settlement.GetSettlementValueForFaction(kingdom) : (float?)null;
		}
		catch
		{
			return null;
		}
	}

internal static void AddExpelClanWarPartyParts(List<string> parts, Clan targetClan)
	{
		if (parts == null || targetClan == null)
		{
			return;
		}
		try
		{
			int partyCount = 0;
			int troopCount = 0;
			IEnumerable<WarPartyComponent> warPartyComponents = Enumerable.Empty<WarPartyComponent>();
			if (targetClan.WarPartyComponents != null)
			{
				warPartyComponents = targetClan.WarPartyComponents;
			}
			foreach (WarPartyComponent warPartyComponent in warPartyComponents)
			{
				if (warPartyComponent?.MobileParty == null)
				{
					continue;
				}
				partyCount++;
				troopCount += warPartyComponent.MobileParty.MemberRoster.TotalManCount;
			}
			if (partyCount <= 0)
			{
				parts.Add("目标家族无可统计战争部队");
				return;
			}
			parts.Add("目标家族战争部队 " + partyCount + " 支、兵员 " + troopCount + "，统帅型投票者会把兵力计入保留权重");
		}
		catch
		{
		}
	}

internal static void AddExpelClanRelationNetworkParts(List<string> parts, Clan targetClan, Kingdom kingdom)
	{
		if (parts == null || targetClan == null || kingdom?.Clans == null)
		{
			return;
		}
		int positiveCount = 0;
		int nonPositiveCount = 0;
		int positiveMagnitude = 0;
		int nonPositiveMagnitude = 0;
		int? worstRelation = null;
		int? bestRelation = null;
		foreach (Clan clan in kingdom.Clans)
		{
			if (clan == null || clan == targetClan)
			{
				continue;
			}
			int? relation = SafeGetClanRelation(targetClan, clan);
			if (!relation.HasValue)
			{
				continue;
			}
			if (!worstRelation.HasValue || relation.Value < worstRelation.Value)
			{
				worstRelation = relation.Value;
			}
			if (!bestRelation.HasValue || relation.Value > bestRelation.Value)
			{
				bestRelation = relation.Value;
			}
			if (relation.Value > 0)
			{
				positiveCount++;
				positiveMagnitude += relation.Value;
			}
			else
			{
				nonPositiveCount++;
				nonPositiveMagnitude += Math.Abs(relation.Value);
			}
		}
		if (positiveCount + nonPositiveCount == 0)
		{
			return;
		}
		string range = worstRelation.HasValue && bestRelation.HasValue ? ("，关系范围 " + worstRelation.Value + "/" + bestRelation.Value) : "";
		parts.Add("王国内关系网：" + positiveCount + "个家族关系为正、" + nonPositiveCount + "个不高于0" + range);
		if (nonPositiveMagnitude > positiveMagnitude)
		{
			parts.Add("负面关系权重更重，政治上更容易形成清算目标");
		}
		else if (positiveMagnitude > nonPositiveMagnitude)
		{
			parts.Add("正面关系权重更重，原版公式更倾向保留该家族");
		}
	}

internal static int? SafeGetClanRelation(Clan sourceClan, Clan targetClan)
	{
		try
		{
			return sourceClan != null && targetClan != null ? FactionManager.GetRelationBetweenClans(sourceClan, targetClan) : (int?)null;
		}
		catch
		{
			return null;
		}
	}

internal static void AddExpelClanRelationToKeyClanPart(List<string> parts, string label, Clan targetClan, Clan keyClan)
	{
		if (parts == null || string.IsNullOrWhiteSpace(label) || targetClan == null || keyClan == null)
		{
			return;
		}
		int? relation = SafeGetClanRelation(targetClan, keyClan);
		if (!relation.HasValue)
		{
			return;
		}
		parts.Add(label.Trim() + " " + relation.Value);
	}

internal static void AddExpelClanDefaultFormulaPart(List<string> parts, bool? shouldExpel)
	{
		if (parts == null)
		{
			return;
		}
		if (shouldExpel == true)
		{
			parts.Add("原版公式有 10000 的保留基准，最终驱逐通常意味着政治表态或统治者裁决压过保留倾向");
		}
		else if (shouldExpel == false)
		{
			parts.Add("原版公式有 10000 的保留基准，关系、封地、军力和声望通常都会强化保留倾向");
		}
		else
		{
			parts.Add("原版公式有 10000 的保留基准，天然更偏向保留家族");
		}
	}

internal static void AddExpelClanVoteSupportPart(List<string> parts, DecisionOutcome chosenOutcome, bool? shouldExpel)
	{
		if (parts == null || chosenOutcome == null)
		{
			return;
		}
		try
		{
			int count = chosenOutcome.SupporterList?.Count((Supporter x) => x?.Clan != null) ?? 0;
			if (count <= 0)
			{
				return;
			}
			string action = shouldExpel == true ? "驱逐" : shouldExpel == false ? "保留" : "最终选项";
			parts.Add("投票支持：" + count + "个家族支持" + action);
		}
		catch
		{
		}
	}

internal static void AddExpelClanRelationCostPart(List<string> parts, bool? shouldExpel)
	{
		if (parts == null || shouldExpel != true)
		{
			return;
		}
		try
		{
			var diplomacyModel = Campaign.Current?.Models?.DiplomacyModel;
			if (diplomacyModel == null)
			{
				return;
			}
			int relationCost = diplomacyModel.GetRelationCostOfExpellingClanFromKingdom();
			parts.Add("驱逐副作用：支持者会与目标家族产生关系变化 " + relationCost);
		}
		catch
		{
		}
	}

internal static void AddExpelClanFormulaValuePart(List<string> parts, ExpelClanFromKingdomDecision decision, DecisionOutcome chosenOutcome, bool? shouldExpel)
	{
		if (parts == null || decision == null || chosenOutcome == null)
		{
			return;
		}
		float? value = SafeDetermineKingdomDecisionSupport(decision, decision.ProposerClan, chosenOutcome);
		string valueText = FormatPoliticalReasonNumber(value);
		if (string.IsNullOrWhiteSpace(valueText))
		{
			return;
		}
		if (shouldExpel == true && value.HasValue && value.Value < 0f)
		{
			parts.Add("提案家族公式阻力：驱逐选项被保留基准压制（公式值 " + valueText + "）");
		}
		else if (shouldExpel == false && value.HasValue && value.Value > 0f)
		{
			parts.Add("提案家族公式倾向：保留目标家族（公式值 " + valueText + "）");
		}
		else
		{
			parts.Add("提案家族公式值 " + valueText);
		}
	}

internal static string BuildSettlementClaimantPreliminaryPoliticalTriggerReason(SettlementClaimantPreliminaryDecision decision, DecisionOutcome chosenOutcome)
	{
		try
		{
			if (decision == null || decision.Settlement == null)
			{
				return "";
			}
			Settlement settlement = decision.Settlement;
			Clan ownerClan = SafeResolveSettlementClaimantPreliminaryOwnerClan(decision) ?? settlement.OwnerClan;
			bool? shouldChange = SafeGetSettlementClaimantPreliminaryOutcomeShouldChange(chosenOutcome);
			List<string> parts = new List<string>();
			string settlementName = GetSettlementDisplayName(settlement);
			if (shouldChange.HasValue)
			{
				parts.Add("议题类型：是否重分配" + settlementName + "，最终" + (shouldChange.Value ? "进入新封地主竞争" : "维持现任封地主"));
			}
			else
			{
				parts.Add("议题类型：是否重分配" + settlementName);
			}
			if (ownerClan != null)
			{
				parts.Add("现任封地主：" + GetClanDisplayName(ownerClan));
				AddSettlementOwnerRelationNetworkParts(parts, ownerClan);
				AddSettlementOwnerStrengthPart(parts, ownerClan);
			}
			AddSettlementSupportTendencyPart(parts, "提案家族对重分配", SafeCalculateSettlementClaimantPreliminarySupport(decision, decision.ProposerClan));
			AddSettlementSupportTendencyPart(parts, "提案家族对最终选项", SafeDetermineKingdomDecisionSupport(decision, decision.ProposerClan, chosenOutcome));
			return BuildPoliticalReasonSentence("封地重分配触发原因", parts);
		}
		catch
		{
			return "";
		}
	}

internal static string BuildSettlementClaimantPoliticalTriggerReason(SettlementClaimantDecision decision, DecisionOutcome chosenOutcome)
	{
		try
		{
			if (decision == null || decision.Settlement == null || chosenOutcome == null)
			{
				return "";
			}
			Clan chosenClan = ResolveSettlementClaimantOutcomeClan(chosenOutcome);
			if (chosenClan == null)
			{
				return "";
			}
			Settlement settlement = decision.Settlement;
			List<string> parts = new List<string>();
			parts.Add("议题类型：决定" + GetSettlementDisplayName(settlement) + "归属，获选家族为" + GetClanDisplayName(chosenClan));
			AddSettlementClaimantCandidateFormulaParts(parts, settlement, chosenClan);
			AddSettlementClaimantCandidateRankingPart(parts, decision, chosenClan);
			AddSettlementClaimantVoteSupportPart(parts, chosenOutcome);
			AddSettlementSupportTendencyPart(parts, "提案家族对获选方", SafeDetermineKingdomDecisionSupport(decision, decision.ProposerClan, chosenOutcome));
			return BuildPoliticalReasonSentence("封地分配触发原因", parts);
		}
		catch
		{
			return "";
		}
	}

internal static Clan SafeResolveSettlementClaimantPreliminaryOwnerClan(SettlementClaimantPreliminaryDecision decision)
	{
		try
		{
			if (decision == null)
			{
				return null;
			}
			FieldInfo field = typeof(SettlementClaimantPreliminaryDecision).GetField("_ownerClan", BindingFlags.Instance | BindingFlags.NonPublic);
			return field?.GetValue(decision) as Clan;
		}
		catch
		{
			return null;
		}
	}

internal static bool? SafeGetSettlementClaimantPreliminaryOutcomeShouldChange(DecisionOutcome chosenOutcome)
	{
		try
		{
			if (chosenOutcome is SettlementClaimantPreliminaryDecision.SettlementClaimantPreliminaryOutcome preliminaryOutcome)
			{
				return preliminaryOutcome.ShouldSettlementOwnerChange;
			}
		}
		catch
		{
		}
		return null;
	}

internal static Clan ResolveSettlementClaimantOutcomeClan(DecisionOutcome chosenOutcome)
	{
		try
		{
			return (chosenOutcome as SettlementClaimantDecision.ClanAsDecisionOutcome)?.Clan ?? chosenOutcome?.SponsorClan;
		}
		catch
		{
			return null;
		}
	}

internal static float? SafeCalculateSettlementClaimantPreliminarySupport(SettlementClaimantPreliminaryDecision decision, Clan clan)
	{
		try
		{
			return decision != null && clan != null ? decision.CalculateSupport(clan) : (float?)null;
		}
		catch
		{
			return null;
		}
	}

internal static float? SafeDetermineKingdomDecisionSupport(KingdomDecision decision, Clan clan, DecisionOutcome outcome)
	{
		try
		{
			return decision != null && clan != null && outcome != null ? decision.DetermineSupport(clan, outcome) : (float?)null;
		}
		catch
		{
			return null;
		}
	}

internal static void AddSettlementOwnerRelationNetworkParts(List<string> parts, Clan ownerClan)
	{
		if (parts == null || ownerClan?.Leader == null)
		{
			return;
		}
		List<Clan> clans = new List<Clan>();
		try
		{
			if (ownerClan.Kingdom?.Clans != null)
			{
				clans.AddRange(ownerClan.Kingdom.Clans.Where((Clan x) => x?.Leader != null));
			}
			else
			{
				clans.Add(ownerClan);
			}
		}
		catch
		{
			clans.Clear();
			clans.Add(ownerClan);
		}
		int positiveCount = 0;
		int nonPositiveCount = 0;
		int positiveMagnitude = 0;
		int nonPositiveMagnitude = 0;
		foreach (Clan clan in clans)
		{
			int? relation = SafeGetLeaderRelation(clan?.Leader, ownerClan.Leader);
			if (!relation.HasValue)
			{
				continue;
			}
			if (relation.Value > 0)
			{
				positiveCount++;
				positiveMagnitude += relation.Value;
			}
			else
			{
				nonPositiveCount++;
				nonPositiveMagnitude += Math.Abs(relation.Value);
			}
		}
		if (positiveCount + nonPositiveCount == 0)
		{
			return;
		}
		parts.Add("现任家族关系网：" + positiveCount + "个家族关系为正、" + nonPositiveCount + "个不高于0");
		if (nonPositiveMagnitude > positiveMagnitude)
		{
			parts.Add("关系权重显示对现任家族不满更重，原版公式更容易推动重分配");
		}
		else if (positiveMagnitude > nonPositiveMagnitude)
		{
			parts.Add("关系权重显示现任家族盟友更强，原版公式更容易维持原主");
		}
	}

internal static int? SafeGetLeaderRelation(Hero source, Hero target)
	{
		try
		{
			return source != null && target != null ? source.GetRelation(target) : (int?)null;
		}
		catch
		{
			return null;
		}
	}

internal static void AddSettlementOwnerStrengthPart(List<string> parts, Clan ownerClan)
	{
		if (parts == null || ownerClan == null)
		{
			return;
		}
		string strengthText = FormatPoliticalReasonNumber(SafeGetClanStrength(ownerClan));
		if (string.IsNullOrWhiteSpace(strengthText))
		{
			return;
		}
		parts.Add("现任家族实力 " + strengthText + "，实力越高越会提高维持原主的公式基准");
	}

internal static void AddSettlementClaimantCandidateFormulaParts(List<string> parts, Settlement settlement, Clan clan)
	{
		if (parts == null || settlement == null || clan == null)
		{
			return;
		}
		parts.Add("候选家族等级 " + clan.Tier + "，等级越高基础分越高");
		string strengthText = FormatPoliticalReasonNumber(SafeGetSettlementClaimantAdjustedStrength(settlement, clan));
		if (!string.IsNullOrWhiteSpace(strengthText))
		{
			parts.Add("候选家族野战力量 " + strengthText + "，会折入候选基础分");
		}
		AddSettlementClaimantExistingFiefParts(parts, settlement, clan);
		AddSettlementClaimantSpecialBonusParts(parts, settlement, clan);
		string settlementValue = FormatPoliticalReasonNumber(SafeGetSettlementValueForClan(settlement, clan));
		if (!string.IsNullOrWhiteSpace(settlementValue))
		{
			parts.Add("目标封地价值 " + settlementValue + "，价值越高越会稀释候选分母");
		}
	}

internal static void AddSettlementClaimantExistingFiefParts(List<string> parts, Settlement targetSettlement, Clan clan)
	{
		if (parts == null || targetSettlement == null || clan == null)
		{
			return;
		}
		int count = 0;
		float totalValue = 0f;
		float nearest = Campaign.MapDiagonal + 1f;
		float secondNearest = Campaign.MapDiagonal + 1f;
		try
		{
			foreach (Settlement settlement in Settlement.All)
			{
				if (settlement == null || settlement == targetSettlement || settlement.OwnerClan != clan || !settlement.IsFortification)
				{
					continue;
				}
				count++;
				totalValue += SafeGetSettlementValueForClan(settlement, clan) ?? 0f;
				float distance = Campaign.Current.Models.MapDistanceModel.GetDistance(settlement, targetSettlement, false, false, MobileParty.NavigationType.All);
				if (distance < secondNearest)
				{
					if (distance < nearest)
					{
						secondNearest = nearest;
						nearest = distance;
					}
					else
					{
						secondNearest = distance;
					}
				}
			}
		}
		catch
		{
		}
		if (count == 0)
		{
			parts.Add("该家族没有其他城镇或城堡，原版公式给予无封地补正");
			return;
		}
		string valueText = FormatPoliticalReasonNumber(totalValue);
		parts.Add("该家族已有要塞 " + count + " 处" + (string.IsNullOrWhiteSpace(valueText) ? "" : ("，既有封地价值 " + valueText + " 会降低新增封地需求")));
		string distancePart = BuildSettlementClaimantDistancePart(nearest, secondNearest);
		if (!string.IsNullOrWhiteSpace(distancePart))
		{
			parts.Add(distancePart);
		}
	}

internal static string BuildSettlementClaimantDistancePart(float nearest, float secondNearest)
	{
		try
		{
			if (nearest >= Campaign.MapDiagonal)
			{
				return "";
			}
			float distance = secondNearest < Campaign.MapDiagonal ? ((nearest + secondNearest) / 2f) : nearest;
			float days = Campaign.Current.EstimatedAverageLordPartySpeed > 0f ? distance / (Campaign.Current.EstimatedAverageLordPartySpeed * (float)CampaignTime.HoursInDay) : 0f;
			string daysText = days.ToString("0.#");
			if (days <= 1f)
			{
				return "现有封地与目标封地很近（约 " + daysText + " 天行军），连片治理加成明显";
			}
			if (days <= 2.5f)
			{
				return "现有封地与目标封地距离适中（约 " + daysText + " 天行军），仍有连片治理加成";
			}
			return "现有封地离目标较远（约 " + daysText + " 天行军），距离因子不占优";
		}
		catch
		{
			return "";
		}
	}

internal static void AddSettlementClaimantSpecialBonusParts(List<string> parts, Settlement settlement, Clan clan)
	{
		if (parts == null || settlement == null || clan?.Leader == null)
		{
			return;
		}
		try
		{
			if (clan.Leader == clan.Kingdom?.Leader)
			{
				parts.Add("候选人是王国统治者，原版公式给予统治者加成");
			}
			if (settlement.Town != null && settlement.Town.LastCapturedBy == clan)
			{
				parts.Add("该家族最后攻下此封地，原版公式给予攻城功劳加成");
			}
			if (clan.Leader == Hero.MainHero)
			{
				parts.Add("候选人是玩家，原版公式给予玩家候选加成");
			}
			int gold = clan.Leader.Gold;
			if (gold < 30000)
			{
				parts.Add("家族领袖资金不足（" + gold + "），原版公式给予财政困难补正");
			}
		}
		catch
		{
		}
	}

internal static float? SafeGetSettlementClaimantAdjustedStrength(Settlement settlement, Clan clan)
	{
		try
		{
			if (settlement == null || clan == null)
			{
				return null;
			}
			float strength = clan.CurrentTotalStrength;
			if (settlement.OwnerClan == clan && settlement.Town?.GarrisonParty?.Party != null)
			{
				strength -= settlement.Town.GarrisonParty.Party.CalculateCurrentStrength();
				if (strength < 0f)
				{
					strength = 0f;
				}
			}
			return strength;
		}
		catch
		{
			return null;
		}
	}

internal static float? SafeGetClanStrength(Clan clan)
	{
		try
		{
			return clan?.CurrentTotalStrength;
		}
		catch
		{
			return null;
		}
	}

internal static float? SafeGetSettlementValueForClan(Settlement settlement, Clan clan)
	{
		try
		{
			return settlement != null && clan?.Kingdom != null ? settlement.GetSettlementValueForFaction(clan.Kingdom) : (float?)null;
		}
		catch
		{
			return null;
		}
	}

internal static void AddSettlementClaimantCandidateRankingPart(List<string> parts, SettlementClaimantDecision decision, Clan chosenClan)
	{
		if (parts == null || decision == null || chosenClan == null)
		{
			return;
		}
		try
		{
			List<Tuple<Clan, float>> candidates = new List<Tuple<Clan, float>>();
			foreach (DecisionOutcome outcome in decision.DetermineInitialCandidates() ?? Enumerable.Empty<DecisionOutcome>())
			{
				Clan clan = ResolveSettlementClaimantOutcomeClan(outcome);
				if (clan == null)
				{
					continue;
				}
				float merit = decision.CalculateMeritOfOutcome(outcome);
				candidates.Add(Tuple.Create(clan, merit));
			}
			candidates = candidates.OrderByDescending((Tuple<Clan, float> x) => x.Item2).ToList();
			if (candidates.Count == 0)
			{
				return;
			}
			string chosenId = GetClanId(chosenClan);
			int index = candidates.FindIndex((Tuple<Clan, float> x) => string.Equals(GetClanId(x.Item1), chosenId, StringComparison.OrdinalIgnoreCase));
			if (index < 0)
			{
				return;
			}
			parts.Add("候选基础评分排名：" + (index + 1) + "/" + candidates.Count + "（评分 " + FormatPoliticalReasonNumber(candidates[index].Item2) + "）");
			if (index > 0)
			{
				parts.Add("基础评分最高者为" + GetClanDisplayName(candidates[0].Item1) + "，最终结果还受到投票支持和统治者裁决影响");
			}
		}
		catch
		{
		}
	}

internal static void AddSettlementClaimantVoteSupportPart(List<string> parts, DecisionOutcome chosenOutcome)
	{
		if (parts == null || chosenOutcome == null)
		{
			return;
		}
		try
		{
			int count = chosenOutcome.SupporterList?.Count((Supporter x) => x?.Clan != null) ?? 0;
			if (count <= 0)
			{
				return;
			}
			string meritText = FormatPoliticalReasonNumber(chosenOutcome.Merit);
			parts.Add("投票支持：" + count + "个家族支持获选方" + (string.IsNullOrWhiteSpace(meritText) ? "" : ("，支持后总评分 " + meritText)));
		}
		catch
		{
		}
	}

internal static void AddSettlementSupportTendencyPart(List<string> parts, string label, float? support)
	{
		if (parts == null || string.IsNullOrWhiteSpace(label) || !support.HasValue || float.IsNaN(support.Value) || float.IsInfinity(support.Value))
		{
			return;
		}
		string supportText = FormatPoliticalReasonNumber(support);
		if (string.IsNullOrWhiteSpace(supportText))
		{
			return;
		}
		parts.Add(label.Trim() + GetSettlementSupportTendencyLabel(support.Value) + "（支持度 " + supportText + "）");
	}

internal static string GetSettlementSupportTendencyLabel(float support)
	{
		if (support >= 100f)
		{
			return "强烈支持";
		}
		if (support >= 35f)
		{
			return "支持";
		}
		if (support > 0f)
		{
			return "轻度支持";
		}
		if (support <= -100f)
		{
			return "强烈反对";
		}
		if (support <= -35f)
		{
			return "反对";
		}
		return "倾向反对";
	}

internal static Clan ResolvePoliticalReasonEvaluatorClan(IFaction faction)
	{
		try
		{
			return (faction as Clan) ?? faction?.Leader?.Clan ?? (faction as Kingdom)?.RulingClan;
		}
		catch
		{
			return null;
		}
	}

internal static float? SafeGetPoliticalDecisionThreshold(IFaction faction)
	{
		try
		{
			if (faction == null || Campaign.Current?.Models?.DiplomacyModel == null)
			{
				return null;
			}
			return Campaign.Current.Models.DiplomacyModel.GetDecisionMakingThreshold(faction);
		}
		catch
		{
			return null;
		}
	}

internal static float? SafeCalculateDeclareWarSupport(DeclareWarDecision decision, Clan clan)
	{
		try
		{
			return decision != null && clan != null ? decision.CalculateSupport(clan) : (float?)null;
		}
		catch
		{
			return null;
		}
	}

internal static float? SafeCalculateMakePeaceSupport(MakePeaceKingdomDecision decision, Clan clan)
	{
		try
		{
			return decision != null && clan != null ? decision.CalculateSupport(clan) : (float?)null;
		}
		catch
		{
			return null;
		}
	}

internal static string BuildPeaceTributeParameter(MakePeaceKingdomDecision decision, IFaction peaceFaction)
	{
		if (decision == null)
		{
			return "";
		}
		int tribute = decision.DailyTributeToBePaid;
		if (tribute == 0)
		{
			return "停战贡金 0";
		}
		string factionName = GetFactionDisplayName(peaceFaction, "己方");
		string verb = tribute > 0 ? "支付" : "收取";
		return factionName + verb + "每日贡金 " + Math.Abs(tribute) + "，持续 " + Math.Abs(decision.DailyTributeDurationInDays) + " 天";
	}

internal static void AddPoliticalFactionParameterParts(List<string> parts, IFaction sourceFaction, IFaction targetFaction, Clan evaluatingClan)
	{
		if (parts == null)
		{
			return;
		}
		float? sourceStrengthValue = SafeGetFactionStrength(sourceFaction);
		float? targetStrengthValue = SafeGetFactionStrength(targetFaction);
		string strengthPart = BuildPoliticalStrengthComparisonPart(sourceFaction, targetFaction, sourceStrengthValue, targetStrengthValue);
		if (!string.IsNullOrWhiteSpace(strengthPart))
		{
			parts.Add(strengthPart);
		}
		int? relation = SafeGetPoliticalRelation(evaluatingClan, sourceFaction, targetFaction);
		if (relation.HasValue)
		{
			parts.Add("相关家族关系 " + relation.Value);
		}
		int? sourceWarParties = SafeGetFactionWarPartyCount(sourceFaction);
		int? targetWarParties = SafeGetFactionWarPartyCount(targetFaction);
		if (sourceWarParties.HasValue && targetWarParties.HasValue)
		{
			parts.Add("战争部队数 " + sourceWarParties.Value + "/" + targetWarParties.Value);
		}
	}

internal static void AddPoliticalFallbackPartIfNeeded(List<string> parts, bool hasModelReason, string actionLabel, string detailText)
	{
		if (parts == null || hasModelReason)
		{
			return;
		}
		string text = GetPoliticalEventDetailLabel(detailText);
		string action = string.IsNullOrWhiteSpace(actionLabel) ? "外交事件" : actionLabel.Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			parts.Add("触发方式：" + action + "事件，原版未提供更细原因");
		}
		else
		{
			parts.Add("触发方式：" + text + "，原版未提供更细原因");
		}
	}

internal static string GetPoliticalEventDetailLabel(string detailText)
	{
		string text = (detailText ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		if (text.IndexOf("KingdomDecision", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return "王国决议";
		}
		if (text.IndexOf("Default", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return "默认外交结算";
		}
		if (text.IndexOf("Barter", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return "交易或谈判";
		}
		if (text.IndexOf("Conversation", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return "对话谈判";
		}
		return "原版细节 " + text;
	}

internal static string BuildPoliticalStrengthComparisonPart(IFaction sourceFaction, IFaction targetFaction, float? sourceStrength, float? targetStrength)
	{
		string sourceText = FormatPoliticalReasonNumber(sourceStrength);
		string targetText = FormatPoliticalReasonNumber(targetStrength);
		if (string.IsNullOrWhiteSpace(sourceText) || string.IsNullOrWhiteSpace(targetText))
		{
			return "";
		}
		string sourceName = GetFactionDisplayName(sourceFaction, "己方");
		string targetName = GetFactionDisplayName(targetFaction, "对方");
		if (sourceStrength.Value <= 0f && targetStrength.Value <= 0f)
		{
			return "实力对比：双方均无可统计野战力量";
		}
		if (sourceStrength.Value <= 0f)
		{
			return "实力对比：" + sourceName + "无可统计野战力量";
		}
		if (targetStrength.Value <= 0f)
		{
			return "实力对比：" + targetName + "无可统计野战力量";
		}
		string relation = GetPoliticalStrengthComparisonLabel(sourceStrength.Value, targetStrength.Value, targetName);
		return "实力对比：" + sourceName + relation + "（" + sourceText + "/" + targetText + "）";
	}

internal static string GetPoliticalStrengthComparisonLabel(float sourceStrength, float targetStrength, string targetName)
	{
		string targetText = string.IsNullOrWhiteSpace(targetName) ? "对方" : targetName.Trim();
		float ratio = sourceStrength / targetStrength;
		if (ratio >= 1.75f)
		{
			return "明显强于" + targetText;
		}
		if (ratio >= 1.2f)
		{
			return "略强于" + targetText;
		}
		if (ratio > 0.83f)
		{
			return "与" + targetText + "接近";
		}
		if (ratio > 0.57f)
		{
			return "略弱于" + targetText;
		}
		return "明显弱于" + targetText;
	}

internal static float? SafeGetFactionStrength(IFaction faction)
	{
		try
		{
			return faction?.CurrentTotalStrength;
		}
		catch
		{
			return null;
		}
	}

internal static int? SafeGetFactionWarPartyCount(IFaction faction)
	{
		try
		{
			return faction?.WarPartyComponents?.Count;
		}
		catch
		{
			return null;
		}
	}

internal static int? SafeGetPoliticalRelation(Clan evaluatingClan, IFaction sourceFaction, IFaction targetFaction)
	{
		try
		{
			Clan sourceClan = evaluatingClan ?? ResolvePoliticalReasonEvaluatorClan(sourceFaction);
			Clan targetClan = ResolvePoliticalReasonEvaluatorClan(targetFaction);
			if (sourceClan == null || targetClan == null)
			{
				return null;
			}
			return FactionManager.GetRelationBetweenClans(sourceClan, targetClan);
		}
		catch
		{
			return null;
		}
	}

internal static void AddPoliticalReasonNumberPart(List<string> parts, string label, float value)
	{
		AddPoliticalReasonNumberPart(parts, label, (float?)value);
	}

internal static void AddPoliticalReasonNumberPart(List<string> parts, string label, float? value)
	{
		if (parts == null || string.IsNullOrWhiteSpace(label))
		{
			return;
		}
		string text = FormatPoliticalReasonNumber(value);
		if (!string.IsNullOrWhiteSpace(text))
		{
			parts.Add(label.Trim() + " " + text);
		}
	}

internal static void AddPoliticalScoreAgainstThresholdPart(List<string> parts, string label, float score, float? threshold, string thresholdLabel)
	{
		if (parts == null || string.IsNullOrWhiteSpace(label))
		{
			return;
		}
		string scoreLabel = label.Trim();
		string thresholdText = FormatPoliticalReasonNumber(threshold);
		if (IsExtremePoliticalReasonScore(score))
		{
			string tendency = score > 0f ? "极高" : "极低";
			parts.Add(string.IsNullOrWhiteSpace(thresholdText) ? (scoreLabel + tendency) : (scoreLabel + tendency + "（" + thresholdLabel + " " + thresholdText + "）"));
			return;
		}
		string scoreText = FormatPoliticalReasonNumber(score);
		if (string.IsNullOrWhiteSpace(scoreText))
		{
			return;
		}
		if (!threshold.HasValue || string.IsNullOrWhiteSpace(thresholdText))
		{
			parts.Add(scoreLabel + " " + scoreText);
			return;
		}
		parts.Add(scoreLabel + GetPoliticalScoreThresholdRelation(score, threshold.Value) + thresholdLabel + "（" + scoreText + "/" + thresholdText + "）");
	}

internal static bool IsExtremePoliticalReasonScore(float value)
	{
		return !float.IsNaN(value) && !float.IsInfinity(value) && Math.Abs(value) >= 1000000f;
	}

internal static string GetPoliticalScoreThresholdRelation(float score, float threshold)
	{
		if (threshold <= 0f)
		{
			return score >= threshold ? "高于" : "低于";
		}
		float ratio = score / threshold;
		if (ratio >= 1.5f)
		{
			return "明显高于";
		}
		if (ratio >= 1.05f)
		{
			return "略高于";
		}
		if (ratio >= 0.95f)
		{
			return "接近";
		}
		return "低于";
	}

internal static string FormatPoliticalReasonNumber(float? value)
	{
		if (!value.HasValue || float.IsNaN(value.Value) || float.IsInfinity(value.Value))
		{
			return "";
		}
		return value.Value.ToString("0");
	}

internal static string BuildPlayerKingdomDecisionActionText(KingdomDecision decision, DecisionOutcome chosenOutcome)
	{
		string kingdomDisplayName = GetKingdomDisplayName(decision?.Kingdom, "该王国");
		string decisionTitle = "";
		try
		{
			decisionTitle = decision?.GetGeneralTitle()?.ToString() ?? "";
		}
		catch
		{
			decisionTitle = "";
		}
		string outcomeTitle = "";
		try
		{
			outcomeTitle = chosenOutcome?.GetDecisionTitle()?.ToString() ?? "";
		}
		catch
		{
			outcomeTitle = "";
		}
		decisionTitle = (decisionTitle ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
		outcomeTitle = (outcomeTitle ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
		if (string.IsNullOrWhiteSpace(decisionTitle))
		{
			decisionTitle = "一项王国决议";
		}
		return string.IsNullOrWhiteSpace(outcomeTitle)
			? ("你参与了" + kingdomDisplayName + "的“" + decisionTitle + "”决议。")
			: ("你参与了" + kingdomDisplayName + "的“" + decisionTitle + "”决议，最终结果为“" + outcomeTitle + "”。");
	}

internal static Hero ResolveKingdomDecisionActionTargetHero(KingdomDecision decision, DecisionOutcome chosenOutcome)
	{
		try
		{
			return chosenOutcome?.SponsorClan?.Leader ?? decision?.ProposerClan?.Leader ?? decision?.DetermineChooser()?.Leader;
		}
		catch
		{
			return decision?.ProposerClan?.Leader;
		}
	}

internal static void ApplyKingdomDecisionSpecificFacts(NpcActionFacts facts, KingdomDecision decision, DecisionOutcome chosenOutcome)
	{
		if (facts == null || decision == null)
		{
			return;
		}
		AddUniqueId(facts.RelatedKingdomIds, GetKingdomId(decision.Kingdom));
		if (decision is DeclareWarDecision declareWarDecision)
		{
			AddRelatedFactionFacts(facts, declareWarDecision.FactionToDeclareWarOn);
		}
		else if (decision is MakePeaceKingdomDecision makePeaceKingdomDecision)
		{
			AddRelatedFactionFacts(facts, makePeaceKingdomDecision.FactionToMakePeaceWith);
		}
		else if (decision is StartAllianceDecision startAllianceDecision)
		{
			AddUniqueId(facts.RelatedKingdomIds, GetKingdomId(startAllianceDecision.KingdomToStartAllianceWith));
		}
		else if (decision is TradeAgreementDecision tradeAgreementDecision)
		{
			AddUniqueId(facts.RelatedKingdomIds, GetKingdomId(tradeAgreementDecision.TargetKingdom));
		}
		else if (decision is ProposeCallToWarAgreementDecision proposeCallToWarAgreementDecision)
		{
			AddUniqueId(facts.RelatedKingdomIds, GetKingdomId(proposeCallToWarAgreementDecision.CalledKingdom));
			AddUniqueId(facts.RelatedKingdomIds, GetKingdomId(proposeCallToWarAgreementDecision.KingdomToCallToWarAgainst));
		}
		else if (decision is AcceptCallToWarAgreementDecision acceptCallToWarAgreementDecision)
		{
			AddUniqueId(facts.RelatedKingdomIds, GetKingdomId(acceptCallToWarAgreementDecision.CallingKingdom));
			AddUniqueId(facts.RelatedKingdomIds, GetKingdomId(acceptCallToWarAgreementDecision.KingdomToCallToWarAgainst));
		}
		else if (decision is ExpelClanFromKingdomDecision expelClanFromKingdomDecision)
		{
			AddUniqueId(facts.RelatedClanIds, GetClanId(expelClanFromKingdomDecision.ClanToExpel));
			AddUniqueId(facts.RelatedKingdomIds, GetKingdomId(expelClanFromKingdomDecision.OldKingdom));
		}
		else if (decision is SettlementClaimantPreliminaryDecision settlementClaimantPreliminaryDecision)
		{
			ApplySettlementFacts(facts, settlementClaimantPreliminaryDecision.Settlement);
			AddUniqueId(facts.RelatedClanIds, GetClanId(settlementClaimantPreliminaryDecision.Settlement?.OwnerClan));
			AddUniqueId(facts.RelatedKingdomIds, GetKingdomId(settlementClaimantPreliminaryDecision.Settlement?.MapFaction));
		}
		if (chosenOutcome is KingSelectionKingdomDecision.KingSelectionDecisionOutcome kingSelectionDecisionOutcome)
		{
			ApplyTargetFacts(facts, kingSelectionDecisionOutcome.King);
			AddUniqueId(facts.RelatedClanIds, GetClanId(kingSelectionDecisionOutcome.King?.Clan));
		}
	}

internal static string BuildKingdomDecisionSupporterSummary(KingdomDecision decision, DecisionOutcome chosenOutcome)
	{
		if (decision == null || chosenOutcome == null)
		{
			return "";
		}
		string text = GetKingdomDecisionTitle(decision);
		string text2 = GetKingdomDecisionOutcomeTitle(chosenOutcome);
		if (IsTrivialKingdomDecisionOutcomeTitle(text2))
		{
			text2 = "";
		}
		List<string> list = new List<string>();
		foreach (Supporter supporter in chosenOutcome.SupporterList ?? new List<Supporter>())
		{
			if (supporter?.Clan?.Leader == null)
			{
				continue;
			}
			list.Add(GetHeroDisplayName(supporter.Clan.Leader) + "（" + GetSupportWeightLabel(supporter.SupportWeight) + "）");
		}
		if (list.Count == 0)
		{
			return "";
		}
		string text3 = string.IsNullOrWhiteSpace(text) ? "本次决议" : text.Trim();
		string text4 = string.IsNullOrWhiteSpace(text2) ? "" : "，最终结果为“" + text2.Trim() + "”";
		return GetKingdomDisplayName(decision.Kingdom, "该王国") + "的“" + text3 + "”决议" + text4 + "，得到这些支持者表态：" + string.Join("；", list) + "。";
	}

internal static string GetKingdomDecisionTitle(KingdomDecision decision)
	{
		string text = "";
		try
		{
			text = decision?.GetGeneralTitle()?.ToString() ?? "";
		}
		catch
		{
			text = "";
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			try
			{
				text = decision?.GetSupportTitle()?.ToString() ?? "";
			}
			catch
			{
				text = "";
			}
		}
		return (text ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
	}

internal static string GetKingdomDecisionOutcomeTitle(DecisionOutcome chosenOutcome)
	{
		string text = "";
		try
		{
			text = chosenOutcome?.GetDecisionTitle()?.ToString() ?? "";
		}
		catch
		{
			text = "";
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			try
			{
				text = chosenOutcome?.GetDecisionDescription()?.ToString() ?? "";
			}
			catch
			{
				text = "";
			}
		}
		return (text ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
	}

internal static bool IsTrivialKingdomDecisionOutcomeTitle(string text)
	{
		string text2 = (text ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text2))
		{
			return true;
		}
		return string.Equals(text2, "是", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(text2, "否", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(text2, "yes", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(text2, "no", StringComparison.OrdinalIgnoreCase);
	}

internal static string GetSupportWeightLabel(Supporter.SupportWeights supportWeight)
	{
		switch (supportWeight)
		{
		case Supporter.SupportWeights.SlightlyFavor:
			return "轻度支持";
		case Supporter.SupportWeights.StronglyFavor:
			return "强力支持";
		case Supporter.SupportWeights.FullyPush:
			return "全力推动";
		case Supporter.SupportWeights.StayNeutral:
			return "中立";
		case Supporter.SupportWeights.Choose:
			return "选择";
		default:
			return supportWeight.ToString();
		}
	}

internal void OnKingdomDecisionConcluded(KingdomDecision decision, DecisionOutcome chosenOutcome, bool isPlayerInvolved)
	{
		try
		{
			if (decision == null)
			{
				return;
			}
			if (VoteDealBehavior.IsBilateralDiplomacyMemoryHandledDecision(decision))
			{
				Logger.Log("NpcAction", "Skipped generic kingdom decision memory because bilateral diplomacy state was recorded explicitly: " + (decision.GetGeneralTitle()?.ToString() ?? decision.GetType().Name));
				return;
			}
			string text = BuildKingdomDecisionStableKey(decision);
			string text2 = BuildKingdomDecisionNarrative(decision, chosenOutcome, isPlayerInvolved, forProposer: true);
			Hero leader = decision.ProposerClan?.Leader;
			if (ShouldTrackNpcActionHero(leader))
			{
				NpcActionFacts npcActionFacts = CreateNpcActionFacts("kingdom_decision_concluded", leader);
				AddUniqueId(npcActionFacts.RelatedClanIds, GetClanId(decision.ProposerClan));
				AddUniqueId(npcActionFacts.RelatedKingdomIds, GetKingdomId(decision.Kingdom));
				ApplyKingdomDecisionSpecificFacts(npcActionFacts, decision, chosenOutcome);
				RecordNpcMajorAction(leader, text2, text + ":proposer", npcActionFacts);
				RecordNpcRecentAction(leader, text2, text + ":proposer", facts: npcActionFacts);
			}
			Hero leader2 = decision.DetermineChooser()?.Leader;
			if (ShouldTrackNpcActionHero(leader2) && !string.Equals(GetHeroId(leader2), GetHeroId(leader), StringComparison.OrdinalIgnoreCase))
			{
				NpcActionFacts npcActionFacts2 = CreateNpcActionFacts("kingdom_decision_concluded", leader2);
				AddUniqueId(npcActionFacts2.RelatedClanIds, GetClanId(decision.ProposerClan));
				AddUniqueId(npcActionFacts2.RelatedKingdomIds, GetKingdomId(decision.Kingdom));
				ApplyKingdomDecisionSpecificFacts(npcActionFacts2, decision, chosenOutcome);
				string text3 = BuildKingdomDecisionNarrative(decision, chosenOutcome, isPlayerInvolved, forProposer: false);
				RecordNpcMajorAction(leader2, text3, text + ":chooser", npcActionFacts2);
				RecordNpcRecentAction(leader2, text3, text + ":chooser", facts: npcActionFacts2);
			}
			string text4 = BuildKingdomDecisionSupporterSummary(decision, chosenOutcome);
			string text5 = BuildKingdomDecisionPoliticalTriggerReason(decision, chosenOutcome);
			text4 = AppendEventMaterialSentence(text4, text5);
			if (!string.IsNullOrWhiteSpace(text4))
			{
				string kingdomId = GetKingdomId(decision.Kingdom);
				string labelPrefix = string.IsNullOrWhiteSpace(text5) ? "决议支持明细" : "决议支持与触发原因";
				RecordEventSourceMaterial("kingdom_decision_support", labelPrefix + " - " + GetKingdomDisplayName(decision.Kingdom, "该王国"), text4, text + ":supporters", kingdomId, "", includeInWorld: false, includeInKingdom: true);
			}
			if (isPlayerInvolved)
			{
				Hero targetHero = ResolveKingdomDecisionActionTargetHero(decision, chosenOutcome);
				string playerText = BuildPlayerKingdomDecisionActionText(decision, chosenOutcome);
				RecordExternalPlayerAction(playerText, text + ":player", "kingdom_decision_player_involved", isMajor: true, targetHero: targetHero, settlement: null, locationText: GetKingdomDisplayName(decision.Kingdom, ""), won: null);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("NpcAction", "[ERROR] OnKingdomDecisionConcluded: " + ex.Message);
		}
	}

internal static string LimitCustomPolicyWeeklyMaterialText(string text, int maxChars)
	{
		text = (text ?? "").Replace("\r\n", " ").Replace('\r', ' ').Replace('\n', ' ').Trim();
		while (text.Contains("  "))
		{
			text = text.Replace("  ", " ");
		}
		if (string.IsNullOrWhiteSpace(text) || maxChars <= 0 || text.Length <= maxChars)
		{
			return text;
		}
		return text.Substring(0, Math.Max(1, maxChars - 1)).TrimEnd() + "…";
	}

internal void RecordWarOrPeaceMaterial(string materialKind, string actionLabel, IFaction faction1, IFaction faction2, string detailText)
	{
		string text = GetFactionDisplayName(faction1, "一方势力");
		string text2 = GetFactionDisplayName(faction2, "另一方势力");
		bool flag = string.Equals((materialKind ?? "").Trim(), "war_declared", StringComparison.OrdinalIgnoreCase);
		string text3 = flag ? (text + "向" + text2 + "宣战，双方进入战争状态。") : (text + "与" + text2 + "停战议和，双方结束战争状态。");
		if (!string.IsNullOrWhiteSpace(detailText))
		{
			text3 += " 原版事件细节：" + detailText.Trim() + "。";
		}
		text3 = AppendEventMaterialSentence(text3, BuildWarOrPeacePoliticalTriggerReason(materialKind, faction1, faction2, detailText));
		string kingdomId = GetKingdomId(faction1);
		string kingdomId2 = GetKingdomId(faction2);
		string text4 = (materialKind ?? "diplomacy").Trim() + ":" + kingdomId + ":" + kingdomId2 + ":" + (detailText ?? "").Trim();
		RecordEventSourceMaterial(materialKind, actionLabel + " - " + text + " / " + text2, text3, text4 + ":world", kingdomId, "", includeInWorld: true, includeInKingdom: false, "", kingdomId2);
		if (!string.IsNullOrWhiteSpace(kingdomId))
		{
			RecordEventSourceMaterial(materialKind, actionLabel + " - " + text + " / " + text2, text3, text4 + ":side1", kingdomId, "", includeInWorld: false, includeInKingdom: true, "", kingdomId2);
		}
		if (!string.IsNullOrWhiteSpace(kingdomId2) && !string.Equals(kingdomId, kingdomId2, StringComparison.OrdinalIgnoreCase))
		{
			RecordEventSourceMaterial(materialKind, actionLabel + " - " + text2 + " / " + text, text3, text4 + ":side2", kingdomId2, "", includeInWorld: false, includeInKingdom: true, "", kingdomId);
		}
	}
}
