using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace AnimusForge.Refactor.Domain;

public static class WorldDiplomacyReputationRules
{
	internal const int DefaultNationalPrestige = 100;
	internal const int DefaultInternationalReputation = 50;
	internal const int MaximumInternationalReputationChangePerDocument = 10;
	internal const int InternationalReputationNaturalAnchor = 20;
	internal const int InternationalReputationFastDecayMinimum = 71;
	internal const int InternationalReputationNormalDecayMinimum = 51;
	internal const int InternationalReputationSlowDecayMinimum = 21;
	internal const int InternationalReputationDailyIntervalDays = 1;
	internal const int InternationalReputationFastDecayStep = 3;
	internal const int InternationalReputationNormalDecayStep = 2;

	public static int CalculateInternationalReputationNaturalChange(
		int currentReputation,
		int elapsedDays,
		out int consumedDays)
	{
		int reputation = Math.Max(0, Math.Min(100, currentReputation));
		int remainingDays = Math.Max(0, elapsedDays);
		consumedDays = 0;
		while (remainingDays > 0 && reputation != InternationalReputationNaturalAnchor)
		{
			int intervalDays;
			int step;
			int maximumTicksInBand;
			if (reputation >= InternationalReputationFastDecayMinimum)
			{
				intervalDays = InternationalReputationDailyIntervalDays;
				step = -InternationalReputationFastDecayStep;
				maximumTicksInBand = (reputation - (InternationalReputationFastDecayMinimum - 1)
					+ InternationalReputationFastDecayStep - 1) / InternationalReputationFastDecayStep;
			}
			else if (reputation >= InternationalReputationNormalDecayMinimum)
			{
				intervalDays = InternationalReputationDailyIntervalDays;
				step = -InternationalReputationNormalDecayStep;
				maximumTicksInBand = (reputation - (InternationalReputationNormalDecayMinimum - 1)
					+ InternationalReputationNormalDecayStep - 1) / InternationalReputationNormalDecayStep;
			}
			else if (reputation >= InternationalReputationSlowDecayMinimum)
			{
				intervalDays = InternationalReputationDailyIntervalDays;
				step = -1;
				maximumTicksInBand = reputation - InternationalReputationNaturalAnchor;
			}
			else
			{
				intervalDays = InternationalReputationDailyIntervalDays;
				step = 1;
				maximumTicksInBand = InternationalReputationNaturalAnchor - reputation;
			}

			int availableTicks = remainingDays / intervalDays;
			if (availableTicks <= 0) break;
			int appliedTicks = Math.Min(availableTicks, maximumTicksInBand);
			reputation = Math.Max(0, Math.Min(100, reputation + (step * appliedTicks)));
			int segmentDays = appliedTicks * intervalDays;
			remainingDays -= segmentDays;
			consumedDays += segmentDays;
		}
		return reputation;
	}

	public static string DescribeInternationalReputationNaturalTrend(int value)
	{
		if (value >= InternationalReputationFastDecayMinimum) return "每天自然下降3点，需要持续以实际成果维护";
		if (value >= InternationalReputationNormalDecayMinimum) return "每天自然下降2点，需要积极以实际行为维护";
		if (value >= InternationalReputationSlowDecayMinimum) return "每天自然下降1点，正逐步回归常态";
		if (value == InternationalReputationNaturalAnchor) return "保持稳定";
		return "每天自然恢复1点直至稳定点，但仍需实际行为才能取得更高评价";
	}

	public static string DescribeInternationalReputation(int value)
	{
		if (value >= 80) return "广受信赖";
		if (value >= 60) return "评价良好";
		if (value >= 40) return "褒贬不一";
		if (value >= 20) return "信誉不佳";
		return "普遍不受信任";
	}

	public static string DescribeNationalPrestige(int value)
	{
		if (value >= 80) return "威望卓著";
		if (value >= 60) return "威望良好";
		if (value >= 40) return "威望受损";
		if (value >= 20) return "威望低下";
		if (value >= 1) return "威信濒临崩溃";
		return "威信尽失";
	}

	public static int GetNationalPrestigeRelationTarget(int prestige)
	{
		if (prestige >= 80) return 0;
		if (prestige >= 60) return -2;
		if (prestige >= 40) return -5;
		if (prestige >= 20) return -10;
		if (prestige >= 1) return -15;
		return -20;
	}

	public static string FormatSignedDelta(int value)
	{
		return value > 0
			? "+" + value.ToString(CultureInfo.InvariantCulture)
			: value.ToString(CultureInfo.InvariantCulture);
	}

	public static string FormatSignedStandingDelta(int value)
	{
		if (value == 0) return "无变化";
		return value > 0
			? "+" + value.ToString(CultureInfo.InvariantCulture)
			: value.ToString(CultureInfo.InvariantCulture);
	}

	public static string BuildInternationalReputationImpactDeltaText(
		WorldDiplomacyDocument document,
		WorldDiplomacyStandingChange change)
	{
		int actualDelta = change?.Delta ?? 0;
		int evaluatedDelta = document?.InternationalReputationEvaluationDelta ?? 0;
		if (actualDelta != 0 || evaluatedDelta == 0) return FormatSignedStandingDelta(actualDelta);
		string boundary = change?.After >= 100 && evaluatedDelta > 0
			? "已达上限100"
			: change?.After <= 0 && evaluatedDelta < 0
				? "已达下限0"
				: "数值边界未产生实际位移";
		return "无实际变化（评价" + FormatSignedStandingDelta(evaluatedDelta) + "，" + boundary + "）";
	}

	public static void ApplyInternationalReputationEvaluation(WorldDiplomacyDocument document, JObject json)
	{
		if (document == null) return;
		string modelReason = WorldDiplomacyTextRules.Limit(
			WorldDiplomacyTextRules.SanitizePublicDiplomacyText(WorldDiplomacyEnvelopeJsonRules.ReadString(json, "international_reputation_reason")), 240);
		if (WorldDiplomacyEnvelopeJsonRules.TryReadInteger(json, "international_reputation_delta", out int modelDelta)
			&& modelDelta != 0
			&& !string.IsNullOrWhiteSpace(modelReason))
		{
			document.InternationalReputationEvaluationDelta = Math.Max(-MaximumInternationalReputationChangePerDocument,
				Math.Min(MaximumInternationalReputationChangePerDocument, modelDelta));
			document.InternationalReputationEvaluationReason = modelReason;
			document.InternationalReputationEvaluationSource = "llm";
			return;
		}

		int fallbackDelta = CalculateStructuredInternationalReputationFallback(document, out string fallbackReason);
		document.InternationalReputationEvaluationDelta = fallbackDelta;
		document.InternationalReputationEvaluationReason = WorldDiplomacyTextRules.Limit(fallbackReason, 240);
		document.InternationalReputationEvaluationSource = "local_structured_fallback";
	}

	public static int CalculateStructuredInternationalReputationFallback(
		WorldDiplomacyDocument document,
		out string reason)
	{
		List<string> intents = document.Actions?.Where(x => x != null)
			.Select(x => WorldDiplomacyIntentVocabulary.NormalizeIntent(x.Intent))
			.Where(x => !string.IsNullOrWhiteSpace(x))
			.ToList() ?? new List<string>();
		if (intents.Count == 0) intents.Add(WorldDiplomacyIntentVocabulary.NormalizeIntent(document.Intent));
		int score = 0;
		bool hasPositiveAction = false;
		bool hasNegativeAction = false;
		foreach (string intent in intents)
		{
			int actionScore = intent switch
			{
				"accept_peace" or "accept_alliance" or "accept_trade" or "apology" or "concession" => 2,
				"propose_peace" or "propose_alliance" or "propose_trade" or "comply_ultimatum" => 1,
				"break_alliance" or "cancel_trade" or "declare_war" => -2,
				"condemn" or "warning" or "ultimatum" => -1,
				_ => 0
			};
			score += actionScore;
			hasPositiveAction |= actionScore > 0;
			hasNegativeAction |= actionScore < 0;
		}
		string basis = score > 0
			? "建设性承诺、合作或承担责任"
			: score < 0
				? "公开施压、毁约或冲突升级"
				: "";
		string move = WorldDiplomacyIntentVocabulary.NormalizeNegotiationMove(document.NegotiationMove);
		if (score == 0 && document.MadeDiplomaticProgress)
		{
			score = 1;
			basis = "本篇提供了新的可执行条件或谈判进展";
		}
		else if (score == 0 && move is "question" or "clarification" or "acknowledge_concern"
			or "counterproposal" or "conditional_acceptance" or "partial_concession"
			or "request_concession" or "revise_terms")
		{
			score = 1;
			basis = "本篇提出了能够继续谈判的新问题、回应或条件";
		}
		else if (score == 0 && move is "set_deadline" or "final_offer" or "withdraw_offer"
			or "end_negotiation" or "declare_deadlock")
		{
			score = -1;
			basis = "本篇收紧、撤回或终止了谈判空间";
		}
		else if (score == 0 && string.Equals(document.Tone, "hostile", StringComparison.OrdinalIgnoreCase))
		{
			score = -1;
			basis = "本篇以敌对措辞加剧了国际疑虑";
		}
		else if (score == 0 && intents.Any(x => x.StartsWith("reject_", StringComparison.Ordinal)))
		{
			score = 1;
			basis = "本篇及时、明确地答复了正式提案";
		}
		else if (score == 0 && string.Equals(document.Tone, "conciliatory", StringComparison.OrdinalIgnoreCase))
		{
			score = 1;
			basis = "本篇以克制且可沟通的方式公开立场";
		}
		else if (score == 0 && hasPositiveAction && hasNegativeAction)
		{
			score = -1;
			basis = "同篇正负行为相互抵消，系统按冲突与毁约风险作保守判定";
		}
		else if (score == 0)
		{
			score = -1;
			basis = "本篇没有提供新的条件、解释、行动或谈判进展";
		}
		score = Math.Max(-4, Math.Min(4, score));
		if (score == 0) score = -1;
		reason = "系统依据本篇已解析的外交动作作出非零保守判定：" + basis + "。";
		return score;
	}
	public static int GetNationalPrestige(IReadOnlyDictionary<string, int> prestigeByKingdom, string kingdomId)
{
		string normalizedId = (kingdomId ?? "").Trim();
		if (normalizedId.Length == 0) return DefaultNationalPrestige;
		return prestigeByKingdom != null
			&& prestigeByKingdom.TryGetValue(normalizedId, out int value)
			? Math.Max(0, Math.Min(DefaultNationalPrestige, value))
			: DefaultNationalPrestige;
	}
	public static int GetInternationalReputation(IReadOnlyDictionary<string, int> reputationByKingdom, string kingdomId)
{
		string normalizedId = (kingdomId ?? "").Trim();
		if (normalizedId.Length == 0) return DefaultInternationalReputation;
		return reputationByKingdom != null
			&& reputationByKingdom.TryGetValue(normalizedId, out int value)
			? Math.Max(0, Math.Min(100, value))
			: DefaultInternationalReputation;
	}
	public static int ApplyInternationalReputationDelta(
		Dictionary<string, int> reputationByKingdom,
		string kingdomId,
		int delta,
		WorldDiplomacyDocument sourceDocument,
		string reason,
		Func<string, string> resolveKingdomName)
{
		string normalizedId = (kingdomId ?? "").Trim();
		if (normalizedId.Length == 0) return DefaultInternationalReputation;
		int before = GetInternationalReputation(reputationByKingdom, normalizedId);
		int boundedDelta = Math.Max(-WorldDiplomacyReputationRules.MaximumInternationalReputationChangePerDocument,
			Math.Min(WorldDiplomacyReputationRules.MaximumInternationalReputationChangePerDocument, delta));
		int updated = (int)Math.Max(0L, Math.Min(100L, (long)before + boundedDelta));
		reputationByKingdom[normalizedId] = updated;
		WorldDiplomacyDocumentFactRules.RecordDiplomaticStandingChange(sourceDocument, "international_reputation", normalizedId, before, updated, reason,
				resolveKingdomName);
		return updated;
	}
	public static int ApplyNationalPrestigeDelta(
		Dictionary<string, int> prestigeByKingdom,
		string kingdomId,
		int delta,
		WorldDiplomacyDocument sourceDocument,
		string reason,
		Func<string, string> resolveKingdomName,
		Action<string> reconcileVassalRelations)
{
		string normalizedId = (kingdomId ?? "").Trim();
		if (normalizedId.Length == 0) return DefaultNationalPrestige;
		int before = GetNationalPrestige(prestigeByKingdom, normalizedId);
		int updated = (int)Math.Max(0L, Math.Min(DefaultNationalPrestige, (long)before + delta));
		prestigeByKingdom[normalizedId] = updated;
		WorldDiplomacyDocumentFactRules.RecordDiplomaticStandingChange(sourceDocument, "national_prestige", normalizedId, before, updated, reason,
				resolveKingdomName);
		reconcileVassalRelations?.Invoke(normalizedId);
		return updated;
	}
	public static void RecoverUnsettledAiInternationalReputation(List<WorldDiplomacyDocument> documents, Action<WorldDiplomacyDocument> settleReputation, Action<string> log)
{
		if (documents == null || settleReputation == null) return;
		int recovered = 0;
		foreach (WorldDiplomacyDocument document in WorldDiplomacyRoundLifecycleRules.OrderDocumentsChronologically(documents
				.Where(x => x != null && !x.IsPlayerAuthored && x.IsReadyForPublication
					&& !x.InternationalReputationSettled
					&& (x.InternationalReputationEvaluationDelta != 0
						|| !string.IsNullOrWhiteSpace(x.InternationalReputationEvaluationReason)))))
		{
			settleReputation(document);
			recovered++;
		}
		if (recovered > 0)
		{
			log?.Invoke("international-reputation.recovered documents="
				+ recovered.ToString(CultureInfo.InvariantCulture));
		}
	}

	public static void SettleInternationalReputationForDocument(
		Dictionary<string, int> reputationByKingdom,
		WorldDiplomacyDocument document,
		Func<string, string> resolveKingdomName,
		Action<string> log)
	{
		if (document == null || reputationByKingdom == null || document.InternationalReputationSettled
			|| string.IsNullOrWhiteSpace(document.AuthorKingdomId)) return;
		int delta = Math.Max(-MaximumInternationalReputationChangePerDocument,
			Math.Min(MaximumInternationalReputationChangePerDocument, document.InternationalReputationEvaluationDelta));
		if (delta == 0)
		{
			delta = CalculateStructuredInternationalReputationFallback(document, out string fallbackReason);
			document.InternationalReputationEvaluationReason = WorldDiplomacyTextRules.Limit(fallbackReason, 240);
			document.InternationalReputationEvaluationSource = "local_nonzero_settlement_guard";
		}
		string reason = WorldDiplomacyTextRules.Limit(WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(
			document.InternationalReputationEvaluationReason,
			"本篇宣言的公开表现改变了国际观感。"), 240);
		document.InternationalReputationEvaluationDelta = delta;
		document.InternationalReputationEvaluationReason = reason;
		if (string.IsNullOrWhiteSpace(document.InternationalReputationEvaluationSource))
		{
			document.InternationalReputationEvaluationSource = "legacy_or_unspecified";
		}
		int before = GetInternationalReputation(reputationByKingdom, document.AuthorKingdomId);
		int after = ApplyInternationalReputationDelta(reputationByKingdom, document.AuthorKingdomId, delta, document, reason, resolveKingdomName);
		document.InternationalReputationSettled = true;
		log?.Invoke("international-reputation.settled document=" + document.DocumentId
			+ " author=" + document.AuthorKingdomId
			+ " delta=" + delta.ToString(CultureInfo.InvariantCulture)
			+ " before=" + before.ToString(CultureInfo.InvariantCulture)
			+ " after=" + after.ToString(CultureInfo.InvariantCulture)
			+ " source=" + document.InternationalReputationEvaluationSource);
	}

	public static void AnchorInternationalReputationNaturalChangeDays(
		Dictionary<string, int> lastDayByKingdom,
		IEnumerable<string> kingdomIds,
		int today)
	{
		if (lastDayByKingdom == null || kingdomIds == null) return;
		foreach (string kingdomId in kingdomIds)
		{
			lastDayByKingdom[kingdomId] = today;
		}
	}

    public static void ProcessInternationalReputationNaturalChange(
        Dictionary<string, int> reputationByKingdom,
        Dictionary<string, int> lastDayByKingdom,
        IEnumerable<string> kingdomIds,
        int today,
        Action<string> log)
{
    int changedKingdoms = 0;
    int totalAbsoluteChange = 0;
    // This runs once per campaign day over the small live-kingdom set. Catch-up is batched by
    // reputation band, so long time skips never become a per-day or per-point hot loop.
    foreach (string kingdomId in kingdomIds)
    {
        if (!lastDayByKingdom.TryGetValue(kingdomId, out int lastDay))
        {
            // Old saves and newly created kingdoms start tracking now; never apply retroactive decay.
            lastDayByKingdom[kingdomId] = today;
            continue;
        }
        if (lastDay > today)
        {
            lastDayByKingdom[kingdomId] = today;
            continue;
        }

        int before = GetInternationalReputation(reputationByKingdom, kingdomId);
        if (before == InternationalReputationNaturalAnchor)
        {
            // Time spent at the anchor must not accumulate and fire immediately after a declaration.
            lastDayByKingdom[kingdomId] = today;
            continue;
        }

        int elapsedDays = today - lastDay;
        int consumedDays;
        int updated = CalculateInternationalReputationNaturalChange(before, elapsedDays, out consumedDays);
        if (updated == InternationalReputationNaturalAnchor)
        {
            lastDayByKingdom[kingdomId] = today;
        }
        else if (consumedDays > 0)
        {
            lastDayByKingdom[kingdomId] = lastDay + consumedDays;
        }
        if (updated == before) continue;
        reputationByKingdom[kingdomId] = updated;
        changedKingdoms++;
        totalAbsoluteChange += Math.Abs(updated - before);
    }
    if (changedKingdoms > 0)
    {
        log?.Invoke("international reputation natural change kingdoms="
            + changedKingdoms.ToString(CultureInfo.InvariantCulture)
            + " absolute_delta=" + totalAbsoluteChange.ToString(CultureInfo.InvariantCulture));
    }
}
}
