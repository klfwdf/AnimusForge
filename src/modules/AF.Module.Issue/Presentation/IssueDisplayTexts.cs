using System;

namespace AnimusForge;

/// <summary>
/// Projects exact known vanilla issue failure sources at display and prompt
/// boundaries. Raw error values remain unchanged in producers, logs, AFEF facts,
/// and saved data. Unknown values pass through without a generic replacement.
/// The source catalog is loaded once; projections use ordinal comparisons and
/// bounded string work, never a Tick scan or reflection.
/// </summary>
internal static class IssueDisplayTexts
{
	/// <summary>
	/// Static source IDs paired with existing display keys and English fallbacks.
	/// Exact source bytes live in the cached Issue match catalog.
	/// </summary>
	private static readonly (string SourceId, string Key, string English)[] ExactMessages =
		new (string, string, string)[]
	{
		// --- TryCheckQuestPreconditions ---
		("NoQuestOrGiver", "AF_CONV_UI_Issue_NoQuestOrGiver",
			"No quest or quest giver was found."),

		// --- FinalizeClassicQuestAcceptance ---
		("QuestNotCreated", "AF_CONV_UI_Issue_QuestNotCreated",
			"The quest object was not created."),
		("AcceptBranchEnded", "AF_CONV_UI_Issue_AcceptBranchEnded",
			"The vanilla acceptance branch ended or removed the quest immediately, so it never reached the normal in-progress state."),
		("QuestNotOngoing", "AF_CONV_UI_Issue_QuestNotOngoing",
			"The quest did not enter the in-progress state."),
		("AcceptFinalizeException", "AF_CONV_UI_Issue_AcceptFinalizeException",
			"An error occurred while finalizing the vanilla quest acceptance."),

		// --- TryProbeQuestTurnIn ---
		("NoOngoingVanillaQuest", "AF_CONV_UI_Issue_NoOngoingVanillaQuest",
			"There is no ongoing vanilla quest right now."),
		("NoDiscussFlow", "AF_CONV_UI_Issue_NoDiscussFlow",
			"The current quest has no usable vanilla discuss flow."),
		("DiscussAgentMissing", "AF_CONV_UI_Issue_DiscussAgentMissing",
			"The Agent required for the vanilla discuss probe could not be found in the current scene."),
		("ConversationManagerUnavailable", "AF_CONV_UI_Issue_ConversationManagerUnavailable",
			"The vanilla ConversationManager is not available for silent quest turn-in."),
		("NoClearTurnInOption", "AF_CONV_UI_Issue_NoClearTurnInOption",
			"The vanilla discuss flow has no sufficiently clear successful turn-in option."),

		// --- ExecuteTurnInOption ---
		("TurnInMissingArgs", "AF_CONV_UI_Issue_TurnInMissingArgs",
			"Missing the vanilla quest turn-in execution parameters."),
		("TurnInAmbiguousBranch", "AF_CONV_UI_Issue_TurnInAmbiguousBranch",
			"The vanilla turn-in chain branched into multiple paths and could not be auto-selected with high confidence."),
		("TurnInNotCompleted", "AF_CONV_UI_Issue_TurnInNotCompleted",
			"The vanilla turn-in chain ran, but the quest is still not completed."),
	};

	/// <summary>
	/// The producer concatenates the raw <c>ex.Message</c>, which is preserved
	/// verbatim as the <c>{REASON}</c> variable so no diagnostic detail is lost.
	/// Only the surrounding sentence is re-rendered at the presentation boundary.
	/// </summary>
	private const string TurnInProbeExceptionPrefixSourceId = "TurnInProbeExceptionPrefix";

	/// <summary>
	/// The placeholder <c>VanillaIssueOfferBridge.GetHeroName</c> falls back to
	/// when a companion hero has neither a name nor a stable StringId.
	/// </summary>
	private const string UnknownCompanionFallbackSourceId = "UnknownCompanion";

	/// <summary>
	/// Projects the exact unknown-companion placeholder at a presentation or
	/// prompt boundary. Raw AFEF facts keep the producer value, and real hero names
	/// - including names containing CJK characters - pass through unchanged.
	/// </summary>
	/// <param name="heroName">Raw companion name or placeholder.</param>
	/// <returns>Localised placeholder text, or the input unchanged.</returns>
	internal static string CompanionNameForDisplay(string heroName)
	{
		return IssueDisplayMatchCatalog.TryGet(UnknownCompanionFallbackSourceId, out string fallback)
			&& string.Equals(heroName, fallback, StringComparison.Ordinal)
			? ConversationUiLocalization.Text("AF_CONV_UI_Issue_UnknownCompanion", "an unknown companion")
			: heroName;
	}

	internal static string CompanionNameForPrompt(string heroName)
	{
		return IssueDisplayMatchCatalog.TryGet(UnknownCompanionFallbackSourceId, out string fallback)
			&& string.Equals(heroName, fallback, StringComparison.Ordinal)
			? IssuePromptTexts.Text("AF_CONV_UI_Issue_UnknownCompanion", "an unknown companion")
			: heroName;
	}

	/// <summary>
	/// Projects one producer reason into player-facing bilingual text.  Returns
	/// the input unchanged when nothing is recognised.
	/// </summary>
	/// <param name="reason">Raw producer reason; may be null, empty or unknown.</param>
	/// <returns>
	/// Localised display text, or the original input unchanged when nothing is
	/// recognised.  Never returns null for a non-null input.
	/// </returns>
	internal static string ForDisplay(string reason)
		=> Project(reason, false);

	private static string Project(string reason, bool forPrompt)
	{
		if (string.IsNullOrWhiteSpace(reason))
		{
			return reason;
		}
		if (reason.IndexOf('\n') < 0)
		{
			return ProjectLine(reason, forPrompt);
		}
		string[] lines = reason.Split('\n');
		for (int i = 0; i < lines.Length; i++)
		{
			lines[i] = ProjectLine(lines[i], forPrompt);
		}
		return string.Join("\n", lines);
	}

	internal static string FailureReasonForPrompt(string reason) => Project(reason, true);

	internal static string IssueTitleForPrompt(string title)
	{
		if (string.IsNullOrWhiteSpace(title)
			|| !IssueDisplayMatchCatalog.TryGet("UnnamedQuestFallback", out string fallback)
			|| !string.Equals(title.Trim(), fallback, StringComparison.Ordinal))
			return title;
		string trimmed = title.Trim();
		int leading = title.Length - title.TrimStart().Length;
		int trailing = title.Length - title.TrimEnd().Length;
		string projected = IssuePromptTexts.Text("AF_ISSUE_PROMPT_UnnamedQuest", "an unnamed quest");
		return title.Substring(0, leading) + projected + title.Substring(title.Length - trailing);
	}

	internal static string RecentJournalSectionForPrompt(string rawBlock)
	{
		if (string.IsNullOrEmpty(rawBlock)
			|| !IssueDisplayMatchCatalog.TryGet("RecentJournalHeading", out string heading)
			|| !rawBlock.StartsWith(heading, StringComparison.Ordinal))
			return rawBlock;
		return IssuePromptTexts.Text("AF_ISSUE_PROMPT_JournalSummary", "Recent vanilla quest journal summary:")
			+ rawBlock.Substring(heading.Length);
	}

	internal static string AlternativeCandidatePromptLineForPrompt(string rawLine)
	{
		if (string.IsNullOrWhiteSpace(rawLine)) return rawLine;
		const string separator = " | ";
		string[] fields = rawLine.Split(new[] { separator }, StringSplitOptions.None);
		if (fields.Length < 2 || !fields[1].StartsWith("HeroId=", StringComparison.Ordinal)) return rawLine;
		bool changed = false;
		if (fields[0].StartsWith("- ", StringComparison.Ordinal))
		{
			string name = CompanionNameForPrompt(fields[0].Substring(2));
			if (!string.Equals(name, fields[0].Substring(2), StringComparison.Ordinal))
			{
				fields[0] = "- " + name;
				changed = true;
			}
		}
		for (int i = 2; i < fields.Length; i++)
		{
			string projected = ProjectCandidateField(fields[i]);
			if (!string.Equals(projected, fields[i], StringComparison.Ordinal))
			{
				fields[i] = projected;
				changed = true;
			}
		}
		return changed ? string.Join(separator, fields) : rawLine;
	}

	private static string ProjectCandidateField(string field)
	{
		if (IssueDisplayMatchCatalog.TryGet("CandidateSkillFallback", out string skill)
			&& TryReadCandidateField(field, skill + "=", out string skillValue)
			&& HasNumericPayload(skillValue, allowDecimal: false, allowRange: false, allowPercent: false))
			return IssuePromptTexts.Format("AF_ISSUE_PROMPT_CandidateSkill", "Skill={VALUE}", ("VALUE", skillValue));
		if (IssueDisplayMatchCatalog.TryGet("CandidateFailureRiskLabel", out string risk)
			&& TryReadCandidateField(field, risk, out string riskValue)
			&& HasNumericPayload(riskValue, allowDecimal: true, allowRange: false, allowPercent: true))
			return IssuePromptTexts.Format("AF_ISSUE_PROMPT_CandidateRisk", "Failure risk={VALUE}", ("VALUE", riskValue));
		if (IssueDisplayMatchCatalog.TryGet("CandidateDurationLabel", out string duration)
			&& TryReadCandidateField(field, duration, out string durationValue)
			&& IssueDisplayMatchCatalog.TryGet("CandidateDaysSuffix", out string daysSuffix)
			&& durationValue.EndsWith(daysSuffix, StringComparison.Ordinal))
		{
			string days = durationValue.Substring(0, durationValue.Length - daysSuffix.Length);
			if (HasNumericPayload(days, allowDecimal: false, allowRange: false, allowPercent: false))
				return IssuePromptTexts.Format("AF_ISSUE_PROMPT_CandidateDuration", "Estimated duration={DAYS} days", ("DAYS", days));
		}
		if (IssueDisplayMatchCatalog.TryGet("CandidateTroopsLabel", out string troops)
			&& TryReadCandidateField(field, troops, out string troopValue)
			&& HasNumericPayload(troopValue, allowDecimal: false, allowRange: false, allowPercent: false))
			return IssuePromptTexts.Format("AF_ISSUE_PROMPT_CandidateTroops", "Required soldiers={VALUE}", ("VALUE", troopValue));
		if (IssueDisplayMatchCatalog.TryGet("CandidateCasualtiesLabel", out string casualties)
			&& TryReadCandidateField(field, casualties, out string casualtyValue)
			&& HasNumericPayload(casualtyValue, allowDecimal: false, allowRange: true, allowPercent: false))
			return IssuePromptTexts.Format("AF_ISSUE_PROMPT_CandidateCasualties", "Estimated casualties={VALUE}", ("VALUE", casualtyValue));
		return field;
	}

	private static bool TryReadCandidateField(string field, string prefix, out string value)
	{
		value = "";
		if (!field.StartsWith(prefix, StringComparison.Ordinal)) return false;
		value = field.Substring(prefix.Length);
		return value.Length > 0;
	}

	private static bool HasNumericPayload(string value, bool allowDecimal, bool allowRange, bool allowPercent)
	{
		bool hasDigit = false;
		for (int i = 0; i < value.Length; i++)
		{
			char current = value[i];
			if (char.IsDigit(current)) { hasDigit = true; continue; }
			if (allowDecimal && (current == '.' || current == ',')) continue;
			if (allowRange && current == '-') continue;
			if (allowPercent && current == '%' && i == value.Length - 1) continue;
			return false;
		}
		return hasDigit;
	}

	/// <summary>
	/// Projects a single line, preserving the caller's surrounding whitespace so
	/// the shape of a multi-line producer message is not silently rewritten.
	/// </summary>
	private static string ProjectLine(string line, bool forPrompt)
	{
		if (line == null)
		{
			return "";
		}
		string trimmed = line.Trim();
		if (trimmed.Length == 0)
		{
			return line;
		}
		string body = ProjectBody(trimmed, forPrompt);
		if (string.Equals(body, trimmed, StringComparison.Ordinal))
		{
			return line;
		}
		int leading = line.Length - line.TrimStart().Length;
		int trailing = line.Length - line.TrimEnd().Length;
		return line.Substring(0, leading)
			+ body
			+ line.Substring(line.Length - trailing);
	}

	private static string ProjectBody(string value, bool forPrompt)
	{
		if (IssueDisplayMatchCatalog.TryGet(TurnInProbeExceptionPrefixSourceId, out string prefix)
			&& value.StartsWith(prefix, StringComparison.Ordinal))
		{
			if (forPrompt) return IssuePromptTexts.Format(
				"AF_CONV_UI_Issue_TurnInProbeException",
				"An error occurred while turning in the vanilla quest: {REASON}",
				("REASON", value.Substring(prefix.Length)));
			return ConversationUiLocalization.Text(
				"AF_CONV_UI_Issue_TurnInProbeException",
				"An error occurred while turning in the vanilla quest: {REASON}",
				("REASON", value.Substring(prefix.Length)));
		}

		foreach ((string sourceId, string key, string english) in ExactMessages)
		{
			if (IssueDisplayMatchCatalog.TryGet(sourceId, out string literal)
				&& string.Equals(value, literal, StringComparison.Ordinal))
			{
				return forPrompt ? IssuePromptTexts.Text(key, english) : ConversationUiLocalization.Text(key, english);
			}
		}

		return value;
	}
}
