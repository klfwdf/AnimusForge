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
		string kingdomDisplayName = GetKingdomDisplayName(decision?.Kingdom, AdapterRecordTexts.Raw("AF_REC_Political_0001"));
		if (string.IsNullOrWhiteSpace(text))
		{
			return (forProposer ? AdapterRecordTexts.Raw("AF_REC_Political_0002") : AdapterRecordTexts.Raw("AF_REC_Political_0003")) + kingdomDisplayName + AdapterRecordTexts.Raw("AF_REC_Political_0004");
		}
		return (forProposer ? AdapterRecordTexts.Raw("AF_REC_Political_0002") : AdapterRecordTexts.Raw("AF_REC_Political_0003")) + kingdomDisplayName + AdapterRecordTexts.Raw("AF_REC_Political_0005") + text;
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
							parts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0006") + text);
							hasModelReason = true;
						}
						AddPoliticalScoreAgainstThresholdPart(parts, AdapterRecordTexts.Raw("AF_REC_Political_0007"), score, SafeGetPoliticalDecisionThreshold(targetFaction), AdapterRecordTexts.Raw("AF_REC_Political_0008"));
					}
				}
				catch
				{
				}
				if (decision != null)
				{
					AddPoliticalReasonNumberPart(parts, AdapterRecordTexts.Raw("AF_REC_Political_0009"), SafeCalculateDeclareWarSupport(decision, clan));
				}
			}
			AddPoliticalFallbackPartIfNeeded(parts, hasModelReason, AdapterRecordTexts.Raw("AF_REC_Political_0010"), detailText);
			AddPoliticalFactionParameterParts(parts, declaringFaction, targetFaction, clan);
			return BuildPoliticalReasonSentence(AdapterRecordTexts.Raw("AF_REC_Political_0011"), parts);
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
							parts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0006") + text);
							hasModelReason = true;
						}
						AddPoliticalScoreAgainstThresholdPart(parts, AdapterRecordTexts.Raw("AF_REC_Political_0012"), score, SafeGetPoliticalDecisionThreshold(peaceFaction), AdapterRecordTexts.Raw("AF_REC_Political_0013"));
					}
				}
				catch
				{
				}
				if (decision != null)
				{
					AddPoliticalReasonNumberPart(parts, AdapterRecordTexts.Raw("AF_REC_Political_0009"), SafeCalculateMakePeaceSupport(decision, clan));
					string tributeText = BuildPeaceTributeParameter(decision, peaceFaction);
					if (!string.IsNullOrWhiteSpace(tributeText))
					{
						parts.Add(tributeText);
					}
				}
			}
			AddPoliticalFallbackPartIfNeeded(parts, hasModelReason, AdapterRecordTexts.Raw("AF_REC_Political_0014"), detailText);
			AddPoliticalFactionParameterParts(parts, peaceFaction, targetFaction, clan);
			return BuildPoliticalReasonSentence(AdapterRecordTexts.Raw("AF_REC_Political_0015"), parts);
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
			parts.Add((isInverted ? AdapterRecordTexts.Raw("AF_REC_Political_0016") : AdapterRecordTexts.Raw("AF_REC_Political_0017")) + policyName + "”");
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
			return BuildPoliticalReasonSentence(AdapterRecordTexts.Raw("AF_REC_Political_0018"), parts);
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
			return string.IsNullOrWhiteSpace(text) ? AdapterRecordTexts.Raw("AF_REC_Political_0019") : text;
		}
		catch
		{
			return AdapterRecordTexts.Raw("AF_REC_Political_0019");
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
			return AdapterRecordTexts.Raw("AF_REC_Political_0020");
		}
		string axis;
		float weight;
		if (egalitarianAbs >= oligarchicAbs && egalitarianAbs >= authoritarianAbs)
		{
			axis = AdapterRecordTexts.Raw("AF_REC_Political_0021");
			weight = policy.EgalitarianWeight;
		}
		else if (oligarchicAbs >= authoritarianAbs)
		{
			axis = AdapterRecordTexts.Raw("AF_REC_Political_0022");
			weight = policy.OligarchicWeight;
		}
		else
		{
			axis = AdapterRecordTexts.Raw("AF_REC_Political_0023");
			weight = policy.AuthoritarianWeight;
		}
		if (isInverted)
		{
			return AdapterRecordTexts.Raw("AF_REC_Political_0024") + (weight >= 0f ? AdapterRecordTexts.Raw("AF_REC_Political_0025") : AdapterRecordTexts.Raw("AF_REC_Political_0026")) + axis;
		}
		return AdapterRecordTexts.Raw("AF_REC_Political_0027") + (weight >= 0f ? AdapterRecordTexts.Raw("AF_REC_Political_0026") : AdapterRecordTexts.Raw("AF_REC_Political_0025")) + axis;
	}

internal static string BuildPolicyWeightSummaryPart(PolicyObject policy)
	{
		if (policy == null)
		{
			return "";
		}
		return AdapterRecordTexts.Raw("AF_REC_Political_0028") + FormatPoliticalPolicyWeight(policy.EgalitarianWeight) + AdapterRecordTexts.Raw("AF_REC_Political_0029") + FormatPoliticalPolicyWeight(policy.OligarchicWeight) + AdapterRecordTexts.Raw("AF_REC_Political_0030") + FormatPoliticalPolicyWeight(policy.AuthoritarianWeight);
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
				return AdapterRecordTexts.Raw("AF_REC_Political_0031") + clanName + AdapterRecordTexts.Raw("AF_REC_Political_0032");
			}
			if (clan.IsMinorFaction)
			{
				return AdapterRecordTexts.Raw("AF_REC_Political_0031") + clanName + AdapterRecordTexts.Raw("AF_REC_Political_0033");
			}
			if (clan.Tier >= 3)
			{
				return AdapterRecordTexts.Raw("AF_REC_Political_0031") + clanName + AdapterRecordTexts.Raw("AF_REC_Political_0034") + clan.Tier + AdapterRecordTexts.Raw("AF_REC_Political_0035");
			}
			if (clan.Tier == 2)
			{
				return AdapterRecordTexts.Raw("AF_REC_Political_0031") + clanName + AdapterRecordTexts.Raw("AF_REC_Political_0036");
			}
			return AdapterRecordTexts.Raw("AF_REC_Political_0031") + clanName + AdapterRecordTexts.Raw("AF_REC_Political_0037") + clan.Tier + AdapterRecordTexts.Raw("AF_REC_Political_0038");
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
			AddPolicyLeaderTraitLabel(parts, AdapterRecordTexts.Raw("AF_REC_Political_0039"), leader.GetTraitLevel(DefaultTraits.Egalitarian));
			AddPolicyLeaderTraitLabel(parts, AdapterRecordTexts.Raw("AF_REC_Political_0040"), leader.GetTraitLevel(DefaultTraits.Oligarchic));
			AddPolicyLeaderTraitLabel(parts, AdapterRecordTexts.Raw("AF_REC_Political_0041"), leader.GetTraitLevel(DefaultTraits.Authoritarian));
			if (parts.Count == 0)
			{
				return "";
			}
			return AdapterRecordTexts.Raw("AF_REC_Political_0042") + string.Join("、", parts);
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
		parts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0043") + GetPolicySupportTendencyLabel(support.Value) + AdapterRecordTexts.Raw("AF_REC_Political_0044") + supportText + "）");
	}

internal static string GetPolicySupportTendencyLabel(float support)
	{
		if (support >= 100f)
		{
			return AdapterRecordTexts.Raw("AF_REC_Political_0045");
		}
		if (support >= 35f)
		{
			return AdapterRecordTexts.Raw("AF_REC_Political_0046");
		}
		if (support > 0f)
		{
			return AdapterRecordTexts.Raw("AF_REC_Political_0047");
		}
		if (support <= -100f)
		{
			return AdapterRecordTexts.Raw("AF_REC_Political_0048");
		}
		if (support <= -35f)
		{
			return AdapterRecordTexts.Raw("AF_REC_Political_0049");
		}
		return AdapterRecordTexts.Raw("AF_REC_Political_0050");
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
			parts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0051") + GetKingdomDisplayName(kingdom, AdapterRecordTexts.Raw("AF_REC_Political_0001")) + AdapterRecordTexts.Raw("AF_REC_Political_0052") + GetHeroDisplayName(chosenKing) + AdapterRecordTexts.Raw("AF_REC_Political_0053"));
			AddKingSelectionFormulaRulePart(parts);
			AddKingSelectionCandidateProfileParts(parts, chosenKing, kingdom);
			AddKingSelectionCandidateRankingPart(parts, decision, chosenKing);
			AddKingSelectionOutcomeScorePart(parts, chosenOutcome);
			AddKingSelectionVoteSupportPart(parts, chosenOutcome);
			AddKingSelectionVoterTraitPart(parts, kingdom);
			return BuildPoliticalReasonSentence(AdapterRecordTexts.Raw("AF_REC_Political_0054"), parts);
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
		parts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0055"));
		parts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0056"));
	}

internal static void AddKingSelectionCandidateProfileParts(List<string> parts, Hero chosenKing, Kingdom kingdom)
	{
		if (parts == null || chosenKing?.Clan == null)
		{
			return;
		}
		Clan clan = chosenKing.Clan;
		List<string> profileParts = new List<string>();
		profileParts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0057") + GetClanDisplayName(clan));
		profileParts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0058") + clan.Tier);
		string strengthText = FormatPoliticalReasonNumber(SafeGetDiplomacyClanStrength(clan));
		if (!string.IsNullOrWhiteSpace(strengthText))
		{
			profileParts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0059") + strengthText);
		}
		string influenceText = FormatPoliticalReasonNumber(SafeGetClanInfluence(clan));
		if (!string.IsNullOrWhiteSpace(influenceText))
		{
			profileParts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0060") + influenceText);
		}
		string renownText = FormatPoliticalReasonNumber(SafeGetClanRenown(clan));
		if (!string.IsNullOrWhiteSpace(renownText))
		{
			profileParts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0061") + renownText);
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
				parts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0062"));
				return;
			}
			parts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0063") + count + AdapterRecordTexts.Raw("AF_REC_Political_0064") + FormatPoliticalReasonNumber(totalValue));
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
				parts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0065"));
				return;
			}
			parts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0066") + partyCount + AdapterRecordTexts.Raw("AF_REC_Political_0067") + troopCount);
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
			parts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0068") + (fullIndex + 1) + "/" + eligible.Count + AdapterRecordTexts.Raw("AF_REC_Political_0069") + FormatPoliticalReasonNumber(eligible[fullIndex].Item2) + "）");
			if (poolIndex >= 0)
			{
				parts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0070") + (poolIndex + 1) + "/" + candidatePool.Count);
			}
			if (fullIndex > 0)
			{
				parts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0071") + GetClanDisplayName(eligible[0].Item1) + AdapterRecordTexts.Raw("AF_REC_Political_0072"));
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
			parts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0073") + initialText + AdapterRecordTexts.Raw("AF_REC_Political_0074") + meritText);
			return;
		}
		parts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0075") + (string.IsNullOrWhiteSpace(meritText) ? initialText : meritText));
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
				labels.Add(AdapterRecordTexts.Raw("AF_REC_Political_0076") + slight);
			}
			if (strong > 0)
			{
				labels.Add(AdapterRecordTexts.Raw("AF_REC_Political_0077") + strong);
			}
			if (full > 0)
			{
				labels.Add(AdapterRecordTexts.Raw("AF_REC_Political_0078") + full);
			}
			if (other > 0)
			{
				labels.Add(AdapterRecordTexts.Raw("AF_REC_Political_0079") + other);
			}
			parts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0080") + total + AdapterRecordTexts.Raw("AF_REC_Political_0081") + (labels.Count > 0 ? "（" + string.Join("、", labels) + "）" : ""));
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
			parts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0082") + authoritarian + AdapterRecordTexts.Raw("AF_REC_Political_0083") + oligarchic + AdapterRecordTexts.Raw("AF_REC_Political_0084") + other + AdapterRecordTexts.Raw("AF_REC_Political_0085"));
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
			parts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0086") + GetKingdomDisplayName(targetKingdom, AdapterRecordTexts.Raw("AF_REC_Character_0143")) + AdapterRecordTexts.Raw("AF_REC_Political_0087") + GetAgreementOutcomeLabel(shouldStart, AdapterRecordTexts.Raw("AF_REC_Political_0088"), AdapterRecordTexts.Raw("AF_REC_Political_0089")));
			string reasonText;
			float? score = SafeGetStartAllianceSupportScore(decision, decision.ProposerClan, out reasonText);
			AddAgreementModelScorePart(parts, AdapterRecordTexts.Raw("AF_REC_Political_0090"), score, reasonText);
			AddAgreementFinalSupportPart(parts, decision, decision.ProposerClan, chosenOutcome);
			AddAgreementVoteSupportPart(parts, chosenOutcome, shouldStart, AdapterRecordTexts.Raw("AF_REC_Political_0088"), AdapterRecordTexts.Raw("AF_REC_Political_0089"));
			AddKingdomDiplomaticStatusPart(parts, sourceKingdom, targetKingdom);
			AddPoliticalFactionParameterParts(parts, sourceKingdom, targetKingdom, decision.ProposerClan);
			return BuildPoliticalReasonSentence(AdapterRecordTexts.Raw("AF_REC_Political_0091"), parts);
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
			parts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0086") + GetKingdomDisplayName(targetKingdom, AdapterRecordTexts.Raw("AF_REC_Character_0143")) + AdapterRecordTexts.Raw("AF_REC_Political_0092") + GetAgreementOutcomeLabel(shouldStart, AdapterRecordTexts.Raw("AF_REC_Political_0093"), AdapterRecordTexts.Raw("AF_REC_Political_0094")));
			string reasonText;
			float? score = SafeGetTradeAgreementSupportScore(sourceKingdom, targetKingdom, decision.ProposerClan, out reasonText);
			AddTradeAgreementScorePart(parts, score, reasonText);
			AddAgreementFinalSupportPart(parts, decision, decision.ProposerClan, chosenOutcome);
			AddAgreementVoteSupportPart(parts, chosenOutcome, shouldStart, AdapterRecordTexts.Raw("AF_REC_Political_0093"), AdapterRecordTexts.Raw("AF_REC_Political_0094"));
			AddKingdomDiplomaticStatusPart(parts, sourceKingdom, targetKingdom);
			AddPoliticalFactionParameterParts(parts, sourceKingdom, targetKingdom, decision.ProposerClan);
			return BuildPoliticalReasonSentence(AdapterRecordTexts.Raw("AF_REC_Political_0095"), parts);
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
			parts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0096") + GetKingdomDisplayName(calledKingdom, AdapterRecordTexts.Raw("AF_REC_Political_0097")) + AdapterRecordTexts.Raw("AF_REC_Battle_0096") + GetKingdomDisplayName(targetKingdom, AdapterRecordTexts.Raw("AF_REC_Political_0098")) + AdapterRecordTexts.Raw("AF_REC_Political_0099") + GetAgreementOutcomeLabel(shouldCall, AdapterRecordTexts.Raw("AF_REC_Political_0100"), AdapterRecordTexts.Raw("AF_REC_Political_0101")));
			parts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0102") + decision.CallToWarCost);
			string reasonText;
			float? score = SafeGetCallingToWarScore(sourceKingdom, calledKingdom, targetKingdom, decision.ProposerClan, out reasonText);
			AddAgreementModelScorePart(parts, AdapterRecordTexts.Raw("AF_REC_Political_0103"), score, reasonText);
			AddAgreementFinalSupportPart(parts, decision, decision.ProposerClan, chosenOutcome);
			AddAgreementVoteSupportPart(parts, chosenOutcome, shouldCall, AdapterRecordTexts.Raw("AF_REC_Political_0100"), AdapterRecordTexts.Raw("AF_REC_Political_0101"));
			AddCallToWarDiplomaticStatusParts(parts, sourceKingdom, calledKingdom, targetKingdom, isAcceptanceDecision: false);
			AddCallToWarStrengthParts(parts, sourceKingdom, calledKingdom, targetKingdom);
			return BuildPoliticalReasonSentence(AdapterRecordTexts.Raw("AF_REC_Political_0104"), parts);
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
			parts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0105") + GetKingdomDisplayName(callingKingdom, AdapterRecordTexts.Raw("AF_REC_Political_0106")) + AdapterRecordTexts.Raw("AF_REC_Political_0107") + GetKingdomDisplayName(targetKingdom, AdapterRecordTexts.Raw("AF_REC_Political_0098")) + AdapterRecordTexts.Raw("AF_REC_Political_0099") + GetAgreementOutcomeLabel(shouldAccept, AdapterRecordTexts.Raw("AF_REC_Political_0108"), AdapterRecordTexts.Raw("AF_REC_Political_0109")));
			parts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0110") + decision.CallToWarCost);
			string reasonText;
			float? score = SafeGetJoiningWarScore(callingKingdom, calledKingdom, targetKingdom, decision.ProposerClan, out reasonText);
			AddAgreementModelScorePart(parts, AdapterRecordTexts.Raw("AF_REC_Political_0111"), score, reasonText);
			AddAgreementFinalSupportPart(parts, decision, decision.ProposerClan, chosenOutcome);
			AddAgreementVoteSupportPart(parts, chosenOutcome, shouldAccept, AdapterRecordTexts.Raw("AF_REC_Political_0108"), AdapterRecordTexts.Raw("AF_REC_Political_0109"));
			AddCallToWarDiplomaticStatusParts(parts, callingKingdom, calledKingdom, targetKingdom, isAcceptanceDecision: true);
			AddCallToWarStrengthParts(parts, calledKingdom, callingKingdom, targetKingdom);
			return BuildPoliticalReasonSentence(AdapterRecordTexts.Raw("AF_REC_Political_0112"), parts);
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
			return AdapterRecordTexts.Raw("AF_REC_Political_0113");
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
			string tendency = score.HasValue && score.Value >= 0f ? AdapterRecordTexts.Raw("AF_REC_Political_0114") : AdapterRecordTexts.Raw("AF_REC_Political_0115");
			parts.Add((string.IsNullOrWhiteSpace(label) ? AdapterRecordTexts.Raw("AF_REC_Political_0116") : label.Trim()) + "：" + tendency + AdapterRecordTexts.Raw("AF_REC_Political_0117") + scoreText + "）");
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
			string tendency = score.HasValue && score.Value >= 50f ? AdapterRecordTexts.Raw("AF_REC_Political_0118") : AdapterRecordTexts.Raw("AF_REC_Political_0119");
			parts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0120") + tendency + AdapterRecordTexts.Raw("AF_REC_Political_0117") + scoreText + AdapterRecordTexts.Raw("AF_REC_Political_0121"));
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
			parts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0006") + text);
		}
	}

internal static void AddAgreementFinalSupportPart(List<string> parts, KingdomDecision decision, Clan clan, DecisionOutcome chosenOutcome)
	{
		float? support = SafeDetermineKingdomDecisionSupport(decision, clan, chosenOutcome);
		AddSettlementSupportTendencyPart(parts, AdapterRecordTexts.Raw("AF_REC_Political_0122"), support);
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
			string action = accepted.HasValue ? (accepted.Value ? positiveLabel : negativeLabel) : AdapterRecordTexts.Raw("AF_REC_Political_0123");
			parts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0080") + count + AdapterRecordTexts.Raw("AF_REC_Political_0124") + action);
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
			string sourceName = GetKingdomDisplayName(sourceKingdom, AdapterRecordTexts.Raw("AF_REC_Political_0125"));
			string targetName = GetKingdomDisplayName(targetKingdom, AdapterRecordTexts.Raw("AF_REC_Character_0143"));
			if (sourceKingdom.IsAtWarWith(targetKingdom))
			{
				parts.Add(sourceName + AdapterRecordTexts.Raw("AF_REC_Political_0126") + targetName + AdapterRecordTexts.Raw("AF_REC_Political_0127"));
			}
			else if (sourceKingdom.IsAllyWith(targetKingdom))
			{
				parts.Add(sourceName + AdapterRecordTexts.Raw("AF_REC_Political_0126") + targetName + AdapterRecordTexts.Raw("AF_REC_Political_0128"));
			}
			else
			{
				parts.Add(sourceName + AdapterRecordTexts.Raw("AF_REC_Political_0126") + targetName + AdapterRecordTexts.Raw("AF_REC_Political_0129"));
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
			string callingName = GetKingdomDisplayName(callingKingdom, AdapterRecordTexts.Raw("AF_REC_Political_0130"));
			string calledName = GetKingdomDisplayName(calledKingdom, AdapterRecordTexts.Raw("AF_REC_Political_0131"));
			string targetName = GetKingdomDisplayName(targetKingdom, AdapterRecordTexts.Raw("AF_REC_Political_0098"));
			parts.Add(callingName + AdapterRecordTexts.Raw("AF_REC_Political_0126") + calledName + (callingKingdom.IsAllyWith(calledKingdom) ? AdapterRecordTexts.Raw("AF_REC_Political_0132") : AdapterRecordTexts.Raw("AF_REC_Political_0133")));
			parts.Add(callingName + AdapterRecordTexts.Raw("AF_REC_Political_0126") + targetName + (callingKingdom.IsAtWarWith(targetKingdom) ? AdapterRecordTexts.Raw("AF_REC_Political_0134") : AdapterRecordTexts.Raw("AF_REC_Political_0135")));
			parts.Add(calledName + AdapterRecordTexts.Raw("AF_REC_Political_0126") + targetName + (calledKingdom.IsAtWarWith(targetKingdom) ? AdapterRecordTexts.Raw("AF_REC_Political_0134") : AdapterRecordTexts.Raw("AF_REC_Political_0136")));
			if (isAcceptanceDecision)
			{
				parts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0137"));
			}
			else
			{
				parts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0138"));
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
			string kingdomName = GetKingdomDisplayName(kingdom, AdapterRecordTexts.Raw("AF_REC_Political_0001"));
			string clanName = GetClanDisplayName(targetClan);
			parts.Add(shouldExpel.HasValue ? (AdapterRecordTexts.Raw("AF_REC_Political_0139") + clanName + AdapterRecordTexts.Raw("AF_REC_Political_0140") + kingdomName + AdapterRecordTexts.Raw("AF_REC_Political_0141") + (shouldExpel.Value ? AdapterRecordTexts.Raw("AF_REC_Political_0142") : AdapterRecordTexts.Raw("AF_REC_Political_0143"))) : (AdapterRecordTexts.Raw("AF_REC_Political_0139") + clanName + AdapterRecordTexts.Raw("AF_REC_Political_0140") + kingdomName));
			AddExpelClanTargetProfileParts(parts, targetClan, kingdom);
			AddExpelClanRelationNetworkParts(parts, targetClan, kingdom);
			AddExpelClanRelationToKeyClanPart(parts, AdapterRecordTexts.Raw("AF_REC_Political_0144"), targetClan, decision.ProposerClan);
			AddExpelClanRelationToKeyClanPart(parts, AdapterRecordTexts.Raw("AF_REC_Political_0145"), targetClan, kingdom?.RulingClan);
			AddExpelClanDefaultFormulaPart(parts, shouldExpel);
			AddExpelClanVoteSupportPart(parts, chosenOutcome, shouldExpel);
			AddExpelClanRelationCostPart(parts, shouldExpel);
			AddExpelClanFormulaValuePart(parts, decision, chosenOutcome, shouldExpel);
			return BuildPoliticalReasonSentence(AdapterRecordTexts.Raw("AF_REC_Political_0146"), parts);
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
		parts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0147") + targetClan.Tier + AdapterRecordTexts.Raw("AF_REC_Political_0148") + FormatPoliticalReasonNumber(SafeGetClanInfluence(targetClan)) + AdapterRecordTexts.Raw("AF_REC_Political_0149") + FormatPoliticalReasonNumber(SafeGetClanRenown(targetClan)));
		string strengthText = FormatPoliticalReasonNumber(SafeGetClanStrength(targetClan));
		if (!string.IsNullOrWhiteSpace(strengthText))
		{
			parts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0150") + strengthText);
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
				parts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0151"));
				return;
			}
			parts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0152") + count + AdapterRecordTexts.Raw("AF_REC_Political_0064") + FormatPoliticalReasonNumber(totalValue) + AdapterRecordTexts.Raw("AF_REC_Political_0153"));
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
				parts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0154"));
				return;
			}
			parts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0155") + partyCount + AdapterRecordTexts.Raw("AF_REC_Political_0067") + troopCount + AdapterRecordTexts.Raw("AF_REC_Political_0156"));
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
		string range = worstRelation.HasValue && bestRelation.HasValue ? (AdapterRecordTexts.Raw("AF_REC_Political_0157") + worstRelation.Value + "/" + bestRelation.Value) : "";
		parts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0158") + positiveCount + AdapterRecordTexts.Raw("AF_REC_Political_0159") + nonPositiveCount + AdapterRecordTexts.Raw("AF_REC_Political_0160") + range);
		if (nonPositiveMagnitude > positiveMagnitude)
		{
			parts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0161"));
		}
		else if (positiveMagnitude > nonPositiveMagnitude)
		{
			parts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0162"));
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
			parts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0163"));
		}
		else if (shouldExpel == false)
		{
			parts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0164"));
		}
		else
		{
			parts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0165"));
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
			string action = shouldExpel == true ? AdapterRecordTexts.Raw("AF_REC_Political_0142") : shouldExpel == false ? AdapterRecordTexts.Raw("AF_REC_Political_0143") : AdapterRecordTexts.Raw("AF_REC_Political_0123");
			parts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0080") + count + AdapterRecordTexts.Raw("AF_REC_Political_0124") + action);
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
			parts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0166") + relationCost);
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
			parts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0167") + valueText + "）");
		}
		else if (shouldExpel == false && value.HasValue && value.Value > 0f)
		{
			parts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0168") + valueText + "）");
		}
		else
		{
			parts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0169") + valueText);
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
				parts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0170") + settlementName + AdapterRecordTexts.Raw("AF_REC_Political_0141") + (shouldChange.Value ? AdapterRecordTexts.Raw("AF_REC_Political_0171") : AdapterRecordTexts.Raw("AF_REC_Political_0172")));
			}
			else
			{
				parts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0170") + settlementName);
			}
			if (ownerClan != null)
			{
				parts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0173") + GetClanDisplayName(ownerClan));
				AddSettlementOwnerRelationNetworkParts(parts, ownerClan);
				AddSettlementOwnerStrengthPart(parts, ownerClan);
			}
			AddSettlementSupportTendencyPart(parts, AdapterRecordTexts.Raw("AF_REC_Political_0174"), SafeCalculateSettlementClaimantPreliminarySupport(decision, decision.ProposerClan));
			AddSettlementSupportTendencyPart(parts, AdapterRecordTexts.Raw("AF_REC_Political_0122"), SafeDetermineKingdomDecisionSupport(decision, decision.ProposerClan, chosenOutcome));
			return BuildPoliticalReasonSentence(AdapterRecordTexts.Raw("AF_REC_Political_0175"), parts);
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
			parts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0176") + GetSettlementDisplayName(settlement) + AdapterRecordTexts.Raw("AF_REC_Political_0177") + GetClanDisplayName(chosenClan));
			AddSettlementClaimantCandidateFormulaParts(parts, settlement, chosenClan);
			AddSettlementClaimantCandidateRankingPart(parts, decision, chosenClan);
			AddSettlementClaimantVoteSupportPart(parts, chosenOutcome);
			AddSettlementSupportTendencyPart(parts, AdapterRecordTexts.Raw("AF_REC_Political_0178"), SafeDetermineKingdomDecisionSupport(decision, decision.ProposerClan, chosenOutcome));
			return BuildPoliticalReasonSentence(AdapterRecordTexts.Raw("AF_REC_Political_0179"), parts);
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
		parts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0180") + positiveCount + AdapterRecordTexts.Raw("AF_REC_Political_0159") + nonPositiveCount + AdapterRecordTexts.Raw("AF_REC_Political_0160"));
		if (nonPositiveMagnitude > positiveMagnitude)
		{
			parts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0181"));
		}
		else if (positiveMagnitude > nonPositiveMagnitude)
		{
			parts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0182"));
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
		parts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0183") + strengthText + AdapterRecordTexts.Raw("AF_REC_Political_0184"));
	}

internal static void AddSettlementClaimantCandidateFormulaParts(List<string> parts, Settlement settlement, Clan clan)
	{
		if (parts == null || settlement == null || clan == null)
		{
			return;
		}
		parts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0185") + clan.Tier + AdapterRecordTexts.Raw("AF_REC_Political_0186"));
		string strengthText = FormatPoliticalReasonNumber(SafeGetSettlementClaimantAdjustedStrength(settlement, clan));
		if (!string.IsNullOrWhiteSpace(strengthText))
		{
			parts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0187") + strengthText + AdapterRecordTexts.Raw("AF_REC_Political_0188"));
		}
		AddSettlementClaimantExistingFiefParts(parts, settlement, clan);
		AddSettlementClaimantSpecialBonusParts(parts, settlement, clan);
		string settlementValue = FormatPoliticalReasonNumber(SafeGetSettlementValueForClan(settlement, clan));
		if (!string.IsNullOrWhiteSpace(settlementValue))
		{
			parts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0189") + settlementValue + AdapterRecordTexts.Raw("AF_REC_Political_0190"));
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
			parts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0191"));
			return;
		}
		string valueText = FormatPoliticalReasonNumber(totalValue);
		parts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0192") + count + AdapterRecordTexts.Raw("AF_REC_Political_0193") + (string.IsNullOrWhiteSpace(valueText) ? "" : (AdapterRecordTexts.Raw("AF_REC_Political_0194") + valueText + AdapterRecordTexts.Raw("AF_REC_Political_0195"))));
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
				return AdapterRecordTexts.Raw("AF_REC_Political_0196") + daysText + AdapterRecordTexts.Raw("AF_REC_Political_0197");
			}
			if (days <= 2.5f)
			{
				return AdapterRecordTexts.Raw("AF_REC_Political_0198") + daysText + AdapterRecordTexts.Raw("AF_REC_Political_0199");
			}
			return AdapterRecordTexts.Raw("AF_REC_Political_0200") + daysText + AdapterRecordTexts.Raw("AF_REC_Political_0201");
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
				parts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0202"));
			}
			if (settlement.Town != null && settlement.Town.LastCapturedBy == clan)
			{
				parts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0203"));
			}
			if (clan.Leader == Hero.MainHero)
			{
				parts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0204"));
			}
			int gold = clan.Leader.Gold;
			if (gold < 30000)
			{
				parts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0205") + gold + AdapterRecordTexts.Raw("AF_REC_Political_0206"));
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
			parts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0207") + (index + 1) + "/" + candidates.Count + AdapterRecordTexts.Raw("AF_REC_Political_0117") + FormatPoliticalReasonNumber(candidates[index].Item2) + "）");
			if (index > 0)
			{
				parts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0208") + GetClanDisplayName(candidates[0].Item1) + AdapterRecordTexts.Raw("AF_REC_Political_0209"));
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
			parts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0080") + count + AdapterRecordTexts.Raw("AF_REC_Political_0210") + (string.IsNullOrWhiteSpace(meritText) ? "" : (AdapterRecordTexts.Raw("AF_REC_Political_0211") + meritText)));
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
		parts.Add(label.Trim() + GetSettlementSupportTendencyLabel(support.Value) + AdapterRecordTexts.Raw("AF_REC_Political_0044") + supportText + "）");
	}

internal static string GetSettlementSupportTendencyLabel(float support)
	{
		if (support >= 100f)
		{
			return AdapterRecordTexts.Raw("AF_REC_Political_0212");
		}
		if (support >= 35f)
		{
			return AdapterRecordTexts.Raw("AF_REC_Political_0114");
		}
		if (support > 0f)
		{
			return AdapterRecordTexts.Raw("AF_REC_Political_0213");
		}
		if (support <= -100f)
		{
			return AdapterRecordTexts.Raw("AF_REC_Political_0214");
		}
		if (support <= -35f)
		{
			return AdapterRecordTexts.Raw("AF_REC_Political_0115");
		}
		return AdapterRecordTexts.Raw("AF_REC_Political_0215");
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
			return AdapterRecordTexts.Raw("AF_REC_Political_0216");
		}
		string factionName = GetFactionDisplayName(peaceFaction, AdapterRecordTexts.Raw("AF_REC_Political_0217"));
		string verb = tribute > 0 ? AdapterRecordTexts.Raw("AF_REC_Political_0218") : AdapterRecordTexts.Raw("AF_REC_Political_0219");
		return factionName + verb + AdapterRecordTexts.Raw("AF_REC_Political_0220") + Math.Abs(tribute) + AdapterRecordTexts.Raw("AF_REC_Political_0221") + Math.Abs(decision.DailyTributeDurationInDays) + AdapterRecordTexts.Raw("AF_REC_Political_0222");
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
			parts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0223") + relation.Value);
		}
		int? sourceWarParties = SafeGetFactionWarPartyCount(sourceFaction);
		int? targetWarParties = SafeGetFactionWarPartyCount(targetFaction);
		if (sourceWarParties.HasValue && targetWarParties.HasValue)
		{
			parts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0224") + sourceWarParties.Value + "/" + targetWarParties.Value);
		}
	}

internal static void AddPoliticalFallbackPartIfNeeded(List<string> parts, bool hasModelReason, string actionLabel, string detailText)
	{
		if (parts == null || hasModelReason)
		{
			return;
		}
		string text = GetPoliticalEventDetailLabel(detailText);
		string action = string.IsNullOrWhiteSpace(actionLabel) ? AdapterRecordTexts.Raw("AF_REC_Political_0225") : actionLabel.Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			parts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0226") + action + AdapterRecordTexts.Raw("AF_REC_Political_0227"));
		}
		else
		{
			parts.Add(AdapterRecordTexts.Raw("AF_REC_Political_0226") + text + AdapterRecordTexts.Raw("AF_REC_Political_0228"));
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
			return AdapterRecordTexts.Raw("AF_REC_Character_0049");
		}
		if (text.IndexOf("Default", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return AdapterRecordTexts.Raw("AF_REC_Political_0229");
		}
		if (text.IndexOf("Barter", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return AdapterRecordTexts.Raw("AF_REC_Political_0230");
		}
		if (text.IndexOf("Conversation", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return AdapterRecordTexts.Raw("AF_REC_Political_0231");
		}
		return AdapterRecordTexts.Raw("AF_REC_Political_0232") + text;
	}

internal static string BuildPoliticalStrengthComparisonPart(IFaction sourceFaction, IFaction targetFaction, float? sourceStrength, float? targetStrength)
	{
		string sourceText = FormatPoliticalReasonNumber(sourceStrength);
		string targetText = FormatPoliticalReasonNumber(targetStrength);
		if (string.IsNullOrWhiteSpace(sourceText) || string.IsNullOrWhiteSpace(targetText))
		{
			return "";
		}
		string sourceName = GetFactionDisplayName(sourceFaction, AdapterRecordTexts.Raw("AF_REC_Political_0217"));
		string targetName = GetFactionDisplayName(targetFaction, AdapterRecordTexts.Raw("AF_REC_Battle_0067"));
		if (sourceStrength.Value <= 0f && targetStrength.Value <= 0f)
		{
			return AdapterRecordTexts.Raw("AF_REC_Political_0233");
		}
		if (sourceStrength.Value <= 0f)
		{
			return AdapterRecordTexts.Raw("AF_REC_Political_0234") + sourceName + AdapterRecordTexts.Raw("AF_REC_Political_0235");
		}
		if (targetStrength.Value <= 0f)
		{
			return AdapterRecordTexts.Raw("AF_REC_Political_0234") + targetName + AdapterRecordTexts.Raw("AF_REC_Political_0235");
		}
		string relation = GetPoliticalStrengthComparisonLabel(sourceStrength.Value, targetStrength.Value, targetName);
		return AdapterRecordTexts.Raw("AF_REC_Political_0234") + sourceName + relation + "（" + sourceText + "/" + targetText + "）";
	}

internal static string GetPoliticalStrengthComparisonLabel(float sourceStrength, float targetStrength, string targetName)
	{
		string targetText = string.IsNullOrWhiteSpace(targetName) ? AdapterRecordTexts.Raw("AF_REC_Battle_0067") : targetName.Trim();
		float ratio = sourceStrength / targetStrength;
		if (ratio >= 1.75f)
		{
			return AdapterRecordTexts.Raw("AF_REC_Political_0236") + targetText;
		}
		if (ratio >= 1.2f)
		{
			return AdapterRecordTexts.Raw("AF_REC_Political_0237") + targetText;
		}
		if (ratio > 0.83f)
		{
			return AdapterRecordTexts.Raw("AF_REC_Political_0126") + targetText + AdapterRecordTexts.Raw("AF_REC_Political_0238");
		}
		if (ratio > 0.57f)
		{
			return AdapterRecordTexts.Raw("AF_REC_Political_0239") + targetText;
		}
		return AdapterRecordTexts.Raw("AF_REC_Political_0240") + targetText;
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
			string tendency = score > 0f ? AdapterRecordTexts.Raw("AF_REC_Political_0241") : AdapterRecordTexts.Raw("AF_REC_Political_0242");
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
			return score >= threshold ? AdapterRecordTexts.Raw("AF_REC_Political_0243") : AdapterRecordTexts.Raw("AF_REC_Political_0244");
		}
		float ratio = score / threshold;
		if (ratio >= 1.5f)
		{
			return AdapterRecordTexts.Raw("AF_REC_Political_0245");
		}
		if (ratio >= 1.05f)
		{
			return AdapterRecordTexts.Raw("AF_REC_Political_0246");
		}
		if (ratio >= 0.95f)
		{
			return AdapterRecordTexts.Raw("AF_REC_Political_0238");
		}
		return AdapterRecordTexts.Raw("AF_REC_Political_0244");
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
		string kingdomDisplayName = GetKingdomDisplayName(decision?.Kingdom, AdapterRecordTexts.Raw("AF_REC_Political_0001"));
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
			decisionTitle = AdapterRecordTexts.Raw("AF_REC_Political_0247");
		}
		return string.IsNullOrWhiteSpace(outcomeTitle)
			? (AdapterRecordTexts.Raw("AF_REC_Political_0248") + kingdomDisplayName + AdapterRecordTexts.Raw("AF_REC_Political_0249") + decisionTitle + AdapterRecordTexts.Raw("AF_REC_Political_0250"))
			: (AdapterRecordTexts.Raw("AF_REC_Political_0248") + kingdomDisplayName + AdapterRecordTexts.Raw("AF_REC_Political_0249") + decisionTitle + AdapterRecordTexts.Raw("AF_REC_Political_0251") + outcomeTitle + "”。");
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
		string text3 = string.IsNullOrWhiteSpace(text) ? AdapterRecordTexts.Raw("AF_REC_Political_0252") : text.Trim();
		string text4 = string.IsNullOrWhiteSpace(text2) ? "" : AdapterRecordTexts.Raw("AF_REC_Political_0253") + text2.Trim() + "”";
		return GetKingdomDisplayName(decision.Kingdom, AdapterRecordTexts.Raw("AF_REC_Political_0001")) + AdapterRecordTexts.Raw("AF_REC_Political_0249") + text3 + AdapterRecordTexts.Raw("AF_REC_Political_0254") + text4 + AdapterRecordTexts.Raw("AF_REC_Political_0255") + string.Join("；", list) + "。";
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
		return string.Equals(text2, AdapterRecordTexts.Raw("AF_REC_Political_0256"), StringComparison.OrdinalIgnoreCase)
			|| string.Equals(text2, AdapterRecordTexts.Raw("AF_REC_Political_0257"), StringComparison.OrdinalIgnoreCase)
			|| string.Equals(text2, "yes", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(text2, "no", StringComparison.OrdinalIgnoreCase);
	}

internal static string GetSupportWeightLabel(Supporter.SupportWeights supportWeight)
	{
		switch (supportWeight)
		{
		case Supporter.SupportWeights.SlightlyFavor:
			return AdapterRecordTexts.Raw("AF_REC_Political_0213");
		case Supporter.SupportWeights.StronglyFavor:
			return AdapterRecordTexts.Raw("AF_REC_Political_0258");
		case Supporter.SupportWeights.FullyPush:
			return AdapterRecordTexts.Raw("AF_REC_Political_0259");
		case Supporter.SupportWeights.StayNeutral:
			return AdapterRecordTexts.Raw("AF_REC_Political_0260");
		case Supporter.SupportWeights.Choose:
			return AdapterRecordTexts.Raw("AF_REC_Political_0261");
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
				string labelPrefix = string.IsNullOrWhiteSpace(text5) ? AdapterRecordTexts.Raw("AF_REC_Political_0262") : AdapterRecordTexts.Raw("AF_REC_Political_0263");
				RecordEventSourceMaterial("kingdom_decision_support", labelPrefix + " - " + GetKingdomDisplayName(decision.Kingdom, AdapterRecordTexts.Raw("AF_REC_Political_0001")), text4, text + ":supporters", kingdomId, "", includeInWorld: false, includeInKingdom: true);
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
		string text = GetFactionDisplayName(faction1, AdapterRecordTexts.Raw("AF_REC_Political_0264"));
		string text2 = GetFactionDisplayName(faction2, AdapterRecordTexts.Raw("AF_REC_Political_0265"));
		bool flag = string.Equals((materialKind ?? "").Trim(), "war_declared", StringComparison.OrdinalIgnoreCase);
		string text3 = flag ? (text + AdapterRecordTexts.Raw("AF_REC_Political_0266") + text2 + AdapterRecordTexts.Raw("AF_REC_Political_0267")) : (text + AdapterRecordTexts.Raw("AF_REC_Political_0126") + text2 + AdapterRecordTexts.Raw("AF_REC_Political_0268"));
		if (!string.IsNullOrWhiteSpace(detailText))
		{
			text3 += AdapterRecordTexts.Raw("AF_REC_Political_0269") + detailText.Trim() + "。";
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
