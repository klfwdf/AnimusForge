using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TaleWorlds.Localization;

namespace AnimusForge;

/// <summary>
/// Projects the optional dynamic detail blocks that recruitment, abdication and
/// kingdom-service results append after their leading sentence.
///
/// The producers intentionally keep appending machine-facing Chinese facts
/// (<c>，其封地 A、B 已随家族归入玩家王国</c> and <c>（note1；note2）</c>) because
/// logs, facts and saves depend on their exact shape.  This helper recognises
/// those two block shapes and re-renders them for display without touching the
/// producer, and returns any unrecognised remainder verbatim so no information
/// is ever dropped.
///
/// Called only from <see cref="EconomyUiTexts"/> projection boundaries, i.e. on
/// a player-visible notification, never on a Tick.
/// </summary>
internal static class EconomyRecruitmentDetailTexts
{
	private const string CarriedFiefPrefix = "，其封地 ";
	private const string CarriedFiefSuffix = " 已随家族归入玩家王国";

	// Greedy up to the final "）" so that a note which itself contains
	// parentheses (for example an embedded exception message) is kept whole
	// rather than being split or dropped.
	private static readonly Regex NoteListPattern =
		new Regex(@"^（(?<notes>.*)）", RegexOptions.CultureInvariant);

	/// <summary>
	/// Splits a trailing detail region into its leading fief block and its
	/// trailing transition-note block, preserving anything unrecognised.
	/// </summary>
	public static string Describe(string detail)
	{
		if (string.IsNullOrEmpty(detail))
		{
			return string.Empty;
		}

		string remaining = detail;
		string result = string.Empty;

		string fief = TakeCarriedFiefBlock(ref remaining);
		if (!string.IsNullOrEmpty(fief))
		{
			result += fief;
		}

		string notes = TakeTransitionNoteBlock(ref remaining);
		if (!string.IsNullOrEmpty(notes))
		{
			result += notes;
		}

		// Unknown producer wording stays verbatim: dropping it would lose
		// information the player can otherwise still read.
		if (!string.IsNullOrEmpty(remaining))
		{
			result += remaining;
		}

		return result;
	}

	private static string TakeCarriedFiefBlock(ref string remaining)
	{
		if (!remaining.StartsWith(CarriedFiefPrefix, StringComparison.Ordinal))
		{
			return string.Empty;
		}

		int end = remaining.IndexOf(CarriedFiefSuffix, CarriedFiefPrefix.Length, StringComparison.Ordinal);
		if (end < 0)
		{
			return string.Empty;
		}

		string settlements = remaining.Substring(
			CarriedFiefPrefix.Length,
			end - CarriedFiefPrefix.Length);
		remaining = remaining.Substring(end + CarriedFiefSuffix.Length);

		if (string.IsNullOrWhiteSpace(settlements))
		{
			return string.Empty;
		}

		return EconomyUiTexts.Localized(
			"AF_ECO_UI_RecruitmentCarriedFiefs",
			"; its fiefs {SETTLEMENTS} joined the player's kingdom with the clan",
			("SETTLEMENTS", settlements));
	}

	private static string TakeTransitionNoteBlock(ref string remaining)
	{
		Match match = NoteListPattern.Match(remaining);
		if (!match.Success)
		{
			return string.Empty;
		}

		string raw = match.Groups["notes"].Value;
		remaining = remaining.Substring(match.Length);

		if (string.IsNullOrWhiteSpace(raw))
		{
			return string.Empty;
		}

		List<string> rendered = new List<string>();
		foreach (string part in raw.Split('；'))
		{
			string note = part.Trim();
			if (note.Length == 0)
			{
				continue;
			}
			rendered.Add(DescribeNote(note));
		}

		if (rendered.Count == 0)
		{
			return string.Empty;
		}

		return EconomyUiTexts.Localized(
			"AF_ECO_UI_RecruitmentTransitionNotes",
			" ({NOTES})",
			("NOTES", string.Join("; ", rendered)));
	}

	/// <summary>
	/// Re-renders one transition note.  Unrecognised notes are returned
	/// unchanged so no producer detail is swallowed.
	/// </summary>
	private static string DescribeNote(string note)
	{
		Match match = Regex.Match(note, @"^(?<kingdom>.+) 已无可接任执政家族，将在目标家族离开后清理旧王国$", RegexOptions.CultureInvariant);
		if (match.Success)
		{
			return EconomyUiTexts.Localized(
				"AF_ECO_UI_RecruitmentOldKingdomWillBeCleaned",
				"{KINGDOM} has no successor ruling clan left and will be cleared after the target clan departs",
				("KINGDOM", match.Groups["kingdom"].Value));
		}

		match = Regex.Match(note, @"^(?<kingdom>.+) 已由 (?<clan>.+) 接任执政家族$", RegexOptions.CultureInvariant);
		if (match.Success)
		{
			return EconomyUiTexts.Localized(
				"AF_ECO_UI_RecruitmentRulingClanSucceeded",
				"{CLAN} has taken over as the ruling clan of {KINGDOM}",
				("KINGDOM", match.Groups["kingdom"].Value), ("CLAN", match.Groups["clan"].Value));
		}

		match = Regex.Match(note, @"^(?<kingdom>.+) 已无可接任执政家族，已按原版逻辑解散$", RegexOptions.CultureInvariant);
		if (match.Success)
		{
			return EconomyUiTexts.Localized(
				"AF_ECO_UI_RecruitmentOldKingdomDissolvedNoSuccessor",
				"{KINGDOM} has no successor ruling clan left and was dissolved following the native flow",
				("KINGDOM", match.Groups["kingdom"].Value));
		}

		match = Regex.Match(note, @"^(?<kingdom>.+) 已按原版逻辑解散$", RegexOptions.CultureInvariant);
		if (match.Success)
		{
			return EconomyUiTexts.Localized(
				"AF_ECO_UI_RecruitmentOldKingdomDissolved",
				"{KINGDOM} was dissolved following the native flow",
				("KINGDOM", match.Groups["kingdom"].Value));
		}

		match = Regex.Match(note, @"^(?<kingdom>.+) 已因原国王离开触发新统治者选举$", RegexOptions.CultureInvariant);
		if (match.Success)
		{
			return EconomyUiTexts.Localized(
				"AF_ECO_UI_RecruitmentRulerElectionTriggered",
				"{KINGDOM} triggered a new ruler election because its king departed",
				("KINGDOM", match.Groups["kingdom"].Value));
		}

		match = Regex.Match(note, @"^(?<kingdom>.+) 已由 (?<clan>.+) 直接接任执政$", RegexOptions.CultureInvariant);
		if (match.Success)
		{
			return EconomyUiTexts.Localized(
				"AF_ECO_UI_RecruitmentRulingClanTookOverDirectly",
				"{CLAN} took over as the ruling clan of {KINGDOM} directly",
				("KINGDOM", match.Groups["kingdom"].Value), ("CLAN", match.Groups["clan"].Value));
		}

		const string repairFailedPrefix = "旧王国执政家族修复失败：";
		if (note.StartsWith(repairFailedPrefix, StringComparison.Ordinal))
		{
			return EconomyUiTexts.Localized(
				"AF_ECO_UI_RecruitmentRulingClanRepairFailed",
				"Repairing the old kingdom's ruling clan failed: {ERROR}",
				("ERROR", note.Substring(repairFailedPrefix.Length)));
		}

		const string dissolveFailedPrefix = "旧王国解散失败：";
		if (note.StartsWith(dissolveFailedPrefix, StringComparison.Ordinal))
		{
			return EconomyUiTexts.Localized(
				"AF_ECO_UI_RecruitmentOldKingdomDissolveFailed",
				"Dissolving the old kingdom failed: {ERROR}",
				("ERROR", note.Substring(dissolveFailedPrefix.Length)));
		}

		if (string.Equals(note, "已解除其俘虏身份", StringComparison.Ordinal))
		{
			return EconomyUiTexts.Localized(
				"AF_ECO_UI_RecruitmentCaptivityReleased",
				"their prisoner status was lifted");
		}

		return note;
	}
}
