using System;

namespace AnimusForge;

/// <summary>
/// Display port for the vanilla issue bridge (原版任务接取 / 同伴代办 / 任务交付).
///
/// <para>
/// <c>VanillaIssueOfferBridge</c> and <c>IssueActionOwner</c> deliberately keep
/// writing machine-facing Chinese into their <c>out error</c> and
/// <c>out failureReason</c> channels: those exact bytes are consumed by
/// <c>Logger</c> output, AFEF fact writes and save data, and - critically - by
/// <c>IssueRuntimePromptOwner</c>, which re-exposes the same
/// <c>TryCheckQuestPreconditions</c> reason to the model as Chinese prompt text
/// (<c>"当前不能交付的原版原因：" + text5</c>).  Nothing here touches the producer.
/// </para>
///
/// <para>
/// This helper recognises the known raw rejection shapes and re-renders them as
/// keyed bilingual text at the display boundary.  Anything it does not recognise
/// - an already-resolved resource string, a new producer message, a vanilla
/// <c>{=...}</c> TextObject result, an unrelated error - is returned verbatim, so
/// no information is ever dropped and no unknown text is ever silently replaced
/// by a generic sentence.  That passthrough property also makes the helper
/// idempotent: applying it twice to the same value is the same as applying it
/// once, so it is safe both at a call site and again at the display boundary.
/// </para>
///
/// <para>
/// Performance: pure string work with no engine access, no reflection and no
/// allocation on a Tick.  It runs once per already-failing player action (a
/// handful of times per conversation branch), so the literal table is a static
/// readonly array of (literal, key, english) triples scanned in declaration
/// order - O(n) over 13 entries with an ordinal equality compare each, plus one
/// ordinal <see cref="string.StartsWith(string, StringComparison)"/> for the
/// single prefix shape.
/// </para>
/// </summary>
internal static class IssueDisplayTexts
{
	/// <summary>
	/// Static literal fallbacks.  Each entry is (exact producer literal,
	/// localisation key, English fallback).  Order is irrelevant because every
	/// entry is compared with ordinal equality against the whole trimmed
	/// message; the array is therefore safe to extend in any order.
	/// </summary>
	private static readonly (string Literal, string Key, string English)[] ExactMessages =
		new (string, string, string)[]
	{
		// --- TryCheckQuestPreconditions ---
		("未找到任务或发放者。", "AF_CONV_UI_Issue_NoQuestOrGiver",
			"No quest or quest giver was found."),

		// --- FinalizeClassicQuestAcceptance ---
		("任务对象未创建。", "AF_CONV_UI_Issue_QuestNotCreated",
			"The quest object was not created."),
		("原版接取分支已立即结束或移除了任务，未能进入普通进行中状态。", "AF_CONV_UI_Issue_AcceptBranchEnded",
			"The vanilla acceptance branch ended or removed the quest immediately, so it never reached the normal in-progress state."),
		("任务没有进入进行中状态。", "AF_CONV_UI_Issue_QuestNotOngoing",
			"The quest did not enter the in-progress state."),
		("原版任务接受收尾异常。", "AF_CONV_UI_Issue_AcceptFinalizeException",
			"An error occurred while finalizing the vanilla quest acceptance."),

		// --- TryProbeQuestTurnIn ---
		("当前没有进行中的原版任务。", "AF_CONV_UI_Issue_NoOngoingVanillaQuest",
			"There is no ongoing vanilla quest right now."),
		("当前任务没有可用的原版 discuss 流。", "AF_CONV_UI_Issue_NoDiscussFlow",
			"The current quest has no usable vanilla discuss flow."),
		("当前场景中找不到原版 discuss 探测所需的 Agent。", "AF_CONV_UI_Issue_DiscussAgentMissing",
			"The Agent required for the vanilla discuss probe could not be found in the current scene."),
		("当前原版 ConversationManager 不可用于静默任务交付。", "AF_CONV_UI_Issue_ConversationManagerUnavailable",
			"The vanilla ConversationManager is not available for silent quest turn-in."),
		("当前原版 discuss 流里没有足够明确的可交付成功选项。", "AF_CONV_UI_Issue_NoClearTurnInOption",
			"The vanilla discuss flow has no sufficiently clear successful turn-in option."),

		// --- ExecuteTurnInOption ---
		("缺少原版任务交付执行参数。", "AF_CONV_UI_Issue_TurnInMissingArgs",
			"Missing the vanilla quest turn-in execution parameters."),
		("原版交付链在中途出现多条分支，且无法高置信度自动选择。", "AF_CONV_UI_Issue_TurnInAmbiguousBranch",
			"The vanilla turn-in chain branched into multiple paths and could not be auto-selected with high confidence."),
		("原版交付链已运行，但任务仍未完成。", "AF_CONV_UI_Issue_TurnInNotCompleted",
			"The vanilla turn-in chain ran, but the quest is still not completed."),
	};

	/// <summary>
	/// <c>静默原版任务交付异常: {REASON}</c> - the producer concatenates the raw
	/// <c>ex.Message</c>, which is preserved verbatim as the <c>{REASON}</c>
	/// variable so no diagnostic detail is lost.  Only the surrounding sentence
	/// is re-rendered, which also corrects the misleading "静默" (silent) wording:
	/// the failure is surfaced to the player, so nothing about it is silent.
	/// </summary>
	private const string TurnInProbeExceptionPrefix = "静默原版任务交付异常: ";

	/// <summary>
	/// The placeholder <c>VanillaIssueOfferBridge.GetHeroName</c> falls back to
	/// when a companion hero has neither a name nor a stable StringId.
	/// </summary>
	private const string UnknownCompanionFallback = "未知同伴";

	/// <summary>
	/// Projects the companion-name variable of a display sentence.
	///
	/// <para>
	/// <c>GetHeroName</c> is shared between display call sites and non-display
	/// ones: <c>IssueActionOwner</c> line 254 emits it into the model prompt body
	/// and <c>IssueRuntimePromptOwner</c> line 190 into
	/// <c>"当前带队同伴："</c>.  Those prompt/fact channels must keep the raw
	/// Chinese bytes, so this projection is applied to the *variable value* at the
	/// display call site rather than inside the producer.
	/// </para>
	///
	/// <para>
	/// Only the exact placeholder is replaced; a real hero name - including a
	/// name that merely contains CJK characters - is returned unchanged.  Like
	/// <see cref="ForDisplay(string)"/> this is idempotent and never drops
	/// information.
	/// </para>
	/// </summary>
	/// <param name="heroName">Raw companion name or placeholder.</param>
	/// <returns>Localised placeholder text, or the input unchanged.</returns>
	internal static string CompanionNameForDisplay(string heroName)
		=> string.Equals(heroName, UnknownCompanionFallback, StringComparison.Ordinal)
			? ConversationUiLocalization.Text("AF_CONV_UI_Issue_UnknownCompanion", "an unknown companion")
			: heroName;

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
	{
		if (string.IsNullOrWhiteSpace(reason))
		{
			return reason;
		}
		if (reason.IndexOf('\n') < 0)
		{
			return ProjectLine(reason);
		}
		string[] lines = reason.Split('\n');
		for (int i = 0; i < lines.Length; i++)
		{
			lines[i] = ProjectLine(lines[i]);
		}
		return string.Join("\n", lines);
	}

	/// <summary>
	/// Projects a single line, preserving the caller's surrounding whitespace so
	/// the shape of a multi-line producer message is not silently rewritten.
	/// </summary>
	private static string ProjectLine(string line)
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
		string body = ProjectBody(trimmed);
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

	private static string ProjectBody(string value)
	{
		if (value.StartsWith(TurnInProbeExceptionPrefix, StringComparison.Ordinal))
		{
			return ConversationUiLocalization.Text(
				"AF_CONV_UI_Issue_TurnInProbeException",
				"An error occurred while turning in the vanilla quest: {REASON}",
				("REASON", value.Substring(TurnInProbeExceptionPrefix.Length)));
		}

		foreach ((string literal, string key, string english) in ExactMessages)
		{
			if (string.Equals(value, literal, StringComparison.Ordinal))
			{
				return ConversationUiLocalization.Text(key, english);
			}
		}

		return value;
	}
}
