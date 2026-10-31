using TaleWorlds.Localization;
using System;
using System.Globalization;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AnimusForge;

internal static class SocialUiLocalization
{
    private static readonly Regex LiteralValues = new Regex(@"AFSOCIALUILITERAL([0-9]+)END", RegexOptions.CultureInvariant);
	internal static string Text(string id, string englishFallback)
	{
		return new TextObject("{=" + id + "}" + englishFallback).ToString();
	}

	internal static string Format(string id, string englishFallback, params (string Key, object Value)[] variables)
	{
		TextObject text = new TextObject("{=" + id + "}" + englishFallback);
		if (variables != null)
        {
            for (int i = 0; i < variables.Length; i++)
                text.SetTextVariable(variables[i].Key, "AFSOCIALUILITERAL" + i.ToString(CultureInfo.InvariantCulture) + "END");
        }
        return LiteralValues.Replace(text.ToString(), match =>
            variables[int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture)].Value?.ToString() ?? string.Empty);
	}

	/// <summary>
	/// Converts the recruitment result only at the player-facing notification boundary.
	/// The source status remains unchanged because it is also used by AFEF/history and
	/// by the legacy action-prefix guard.
	/// </summary>
	internal static string RecruitmentStatusForDisplay(string status)
	{
		if (string.IsNullOrWhiteSpace(status))
		{
			return status ?? string.Empty;
		}

		string prefixId = null;
		string prefixFallback = null;
		string detail = status;
		if (StartsWithAny(status, "S220", out int cut1))
		{
			prefixId = "AF_SOCIAL_UI_RecruitmentFailed";
			prefixFallback = "[Recruitment failed] {DETAIL}";
			detail = status.Substring(cut1);
		}
		else if (StartsWithAny(status, "S221", out _) && StartsWithAny(status, "S222", out int failedCut))
		{
			prefixId = "AF_SOCIAL_UI_RecruitmentFailed";
			prefixFallback = "[Recruitment failed] {DETAIL}";
			detail = status.Substring(failedCut).TrimStart();
		}
		else if (StartsWithAny(status, "S223", out int cut2))
		{
			prefixId = "AF_SOCIAL_UI_RecruitmentSkipped";
			prefixFallback = "[Recruitment skipped] {DETAIL}";
			detail = status.Substring(cut2);
		}
		else if (StartsWithAny(status, "S224", out int cut3))
		{
			prefixId = "AF_SOCIAL_UI_RecruitmentIntercepted";
			prefixFallback = "[Recruitment intercepted] {DETAIL}";
			detail = status.Substring(cut3);
		}
		else if (StartsWithAny(status, "S225", out int cut4))
		{
			prefixId = "AF_SOCIAL_UI_RecruitmentSucceeded";
			prefixFallback = "[Recruitment succeeded] {DETAIL}";
			detail = status.Substring(cut4);
		}

		if (prefixId == null)
		{
			string direct = TranslateRecruitmentDetail(status);
			return direct != status ? direct : status;
		}

		string translatedDetail = TranslateRecruitmentDetail(detail);
		return Format(prefixId, prefixFallback, ("DETAIL", translatedDetail));
	}

	internal static string MarriageStatusForDisplay(string status)
	{
		if (string.IsNullOrWhiteSpace(status))
			return status ?? string.Empty;
		if (StartsWithAny(status, "S177", out int cut5))
			return Format("AF_SOCIAL_UI_MarriageFormalFailed", "Formal marriage failed: {REASON}", ("REASON", TranslateMarriageReason(status.Substring(cut5))));
		if (StartsWithAny(status, "S188", out int cut6))
			return Format("AF_SOCIAL_UI_MarriageElopeFailed", "Elope failed: {REASON}", ("REASON", TranslateMarriageReason(status.Substring(cut6))));
		if (StartsWithAny(status, "S226", out int cut7))
			return Format("AF_SOCIAL_UI_MarriageDivorceFailed", "Divorce failed: {REASON}", ("REASON", TranslateMarriageReason(status.Substring(cut7))));

		Match match = MatchAny(status, "S227");
		if (match.Success)
		{
			string extra = TranslateMarriageExtra(match.Groups["extra"].Value);
			return Format("AF_SOCIAL_UI_MarriageFormalSucceeded", "Formal marriage succeeded: {LEFT} and {RIGHT} are now married. Tier gap D={TIER}, clan relation={RELATION}, leader trust={TRUST}.{EXTRA}",
				("LEFT", match.Groups["left"].Value), ("RIGHT", match.Groups["right"].Value), ("TIER", match.Groups["tier"].Value), ("RELATION", match.Groups["relation"].Value), ("TRUST", match.Groups["trust"].Value), ("EXTRA", extra));
		}
		match = MatchAny(status, "S228");
		if (match.Success)
			return Format("AF_SOCIAL_UI_MarriageElopeSucceeded", "Elope succeeded: you and {TARGET} are now married. Current L={LOVE}, personal trust={TRUST}, family harmony F={HARMONY}.", ("TARGET", match.Groups["target"].Value), ("LOVE", match.Groups["love"].Value), ("TRUST", match.Groups["trust"].Value), ("HARMONY", match.Groups["harmony"].Value));
		match = MatchAny(status, "S229");
		if (match.Success)
			return Format("AF_SOCIAL_UI_MarriageDivorceSucceeded", "Divorce succeeded: {LEFT} and {RIGHT} are no longer married.{EXTRA}", ("LEFT", match.Groups["left"].Value), ("RIGHT", match.Groups["right"].Value), ("EXTRA", TranslateMarriageExtra(match.Groups["extra"].Value)));
		return status;
	}

	private static string TranslateMarriageReason(string reason)
	{
		if (reason == SocialPromptTexts.Get("MarriageInvalidClanSubject")) return Text("AF_SOCIAL_UI_MarriageInvalidClanSubject", "A clan-arranged marriage must name a player-clan member other than the player.");
		if (reason == SocialPromptTexts.Get("MarriageInvalidApprover")) return Text("AF_SOCIAL_UI_MarriageInvalidApprover", "The speaking NPC must lead the actual marriage partner's clan.");
		if (reason == SocialPromptTexts.Get("MarriageInvalidElopeSubject")) return Text("AF_SOCIAL_UI_MarriageInvalidElopeSubject", "Only the speaking NPC may personally elope with the player.");
		if (string.IsNullOrWhiteSpace(reason)) return string.Empty;
		if (SocialSourceTexts.Is(reason, "S116")) return Text("AF_SOCIAL_UI_MarriageReasonMissingPair", "The marriage pair could not be found.");
		if (SocialSourceTexts.Is(reason, "S230")) return Text("AF_SOCIAL_UI_MarriageReasonMissingDivorcePair", "The divorce pair could not be found.");
		if (SocialSourceTexts.Is(reason, "S117") || SocialSourceTexts.Is(reason, "S172")) return Text("AF_SOCIAL_UI_MarriageReasonSelf", "A character cannot marry themselves.");
		if (SocialSourceTexts.Is(reason, "S118")) return Text("AF_SOCIAL_UI_MarriageReasonAlreadyMarried", "The pair already has an active marriage record.");
		if (SocialSourceTexts.Is(reason, "S156")) return Text("AF_SOCIAL_UI_MarriageReasonSameClan", "A formal marriage requires two different clans.");
		if (SocialSourceTexts.Is(reason, "S231") || SocialSourceTexts.Is(reason, "S232")) return Text("AF_SOCIAL_UI_MarriageReasonMissingTarget", "The target marriage partner could not be found.");
		if (SocialSourceTexts.Is(reason, "S233")) return Text("AF_SOCIAL_UI_MarriageReasonMissingElopeTarget", "The target partner could not be found.");
		if (SocialSourceTexts.Is(reason, "S234")) return Text("AF_SOCIAL_UI_MarriageReasonMissingTargetClanLeader", "The target clan leader could not be found.");
		if (SocialSourceTexts.Is(reason, "S171")) return Text("AF_SOCIAL_UI_MarriageReasonPlayerCandidateNotClan", "The player-side marriage candidate must belong to the player's clan.");
		if (SocialSourceTexts.Is(reason, "S173") || SocialSourceTexts.Is(reason, "S151")) return Text("AF_SOCIAL_UI_MarriageReasonPlayerNotEligible", "The player-side marriage candidate does not meet basic marriage requirements.");
		if (SocialSourceTexts.Is(reason, "S174") || SocialSourceTexts.Is(reason, "S152")) return Text("AF_SOCIAL_UI_MarriageReasonTargetNotEligible", "The target marriage candidate does not meet basic marriage requirements.");
		if (SocialSourceTexts.Is(reason, "S153")) return Text("AF_SOCIAL_UI_MarriageReasonAgeGapExceeded", "The age gap between both parties exceeds the allowed limit.");
		if (SocialSourceTexts.Is(reason, "S154")) return Text("AF_SOCIAL_UI_MarriageReasonAgeGapUnverified", "Could not verify whether the age gap meets marriage requirements.");
		if (SocialSourceTexts.Is(reason, "S235")) return Text("AF_SOCIAL_UI_MarriageReasonMissingPlayerCandidate", "The player-clan marriage candidate could not be found.");
		if (SocialSourceTexts.Is(reason, "S236")) return Text("AF_SOCIAL_UI_MarriageReasonMissingDivorcePlayerCandidate", "The player-clan divorce candidate could not be found.");
		if (SocialSourceTexts.Is(reason, "S237")) return Text("AF_SOCIAL_UI_MarriageReasonMissingDivorceTarget", "The target divorce partner could not be found.");
		if (SocialSourceTexts.Is(reason, "S176")) return Text("AF_SOCIAL_UI_MarriageReasonTargetNoClan", "The target has no clan and cannot use the clan marriage flow.");
		if (SocialSourceTexts.Is(reason, "S238")) return Text("AF_SOCIAL_UI_MarriageReasonNoRecord", "No active marriage record was found.");
		if (SocialSourceTexts.Is(reason, "S239")) return Text("AF_SOCIAL_UI_MarriageReasonNotSpouses", "The pair are not currently spouses.");
		if (SocialSourceTexts.Is(reason, "S175")) return Text("AF_SOCIAL_UI_MarriageReasonMissingPlayer", "The player hero could not be found.");
		if (SocialSourceTexts.Is(reason, "S155")) return Text("AF_SOCIAL_UI_MarriageReasonMissingClans", "Clan information for both parties could not be found.");
		if (SocialSourceTexts.Is(reason, "S157")) return Text("AF_SOCIAL_UI_MarriageReasonNoPlayerEligibleMembers", "The player clan currently has no eligible members for marriage.");
		if (StartsWithAny(reason, "S240", out int cut8))
			return Format("AF_SOCIAL_UI_MarriageReasonMultiFailed", "Polygamy execution failed: {DETAIL}", ("DETAIL", reason.Substring(cut8)));
		if (StartsWithAny(reason, "S241", out int cut9))
			return Format("AF_SOCIAL_UI_MarriageReasonActionFailed", "MarriageAction execution failed: {DETAIL}", ("DETAIL", reason.Substring(cut9)));
		if (StartsWithAny(reason, "S242", out int cut10))
			return Format("AF_SOCIAL_UI_MarriageReasonDissolveException", "Error dissolving marriage: {DETAIL}", ("DETAIL", reason.Substring(cut10)));
		return reason;
	}

	private static string TranslateMarriageExtra(string extra)
	{
		if (string.IsNullOrWhiteSpace(extra)) return string.Empty;
		Match match = MatchAny(extra, "S243");
		if (match.Success) return Format("AF_SOCIAL_UI_MarriageBridePriceConsumed", " The previously prepaid bride price of {AMOUNT} denars was consumed.", ("AMOUNT", match.Groups["amount"].Value));
		match = MatchAny(extra, "S244");
		if (match.Success) return Format("AF_SOCIAL_UI_MarriageBridePricePaid", " The other clan leader paid the player a bride price of {AMOUNT} denars.", ("AMOUNT", match.Groups["amount"].Value));
		match = MatchAny(extra, "S245");
		if (match.Success) return Format("AF_SOCIAL_UI_MarriageBridePriceRefunded", " A bride price of {AMOUNT} denars was refunded.", ("AMOUNT", match.Groups["amount"].Value));
		return extra;
	}

	private static string TranslateRecruitmentDetail(string detail)
	{
		if (string.IsNullOrWhiteSpace(detail))
			return string.Empty;

		if (StartsWithAny(detail, "S246", out int cut11))
			return Format("AF_SOCIAL_UI_RecruitmentException", "Exception: {ERROR}", ("ERROR", detail.Substring(cut11)));
		if (StartsWithAny(detail, "S247", out int cut12))
			return Format("AF_SOCIAL_UI_RecruitmentJoinClanException", "Join player clan exception: {ERROR}", ("ERROR", detail.Substring(cut12)));
		if (StartsWithAny(detail, "S248", out int cut13))
			return Format("AF_SOCIAL_UI_RecruitmentBecomeCompanionException", "Become player companion exception: {ERROR}", ("ERROR", detail.Substring(cut13)));

		if (SocialSourceTexts.Is(detail, "S249"))
			return Text("AF_SOCIAL_UI_RecruitmentMissingClanHero", "No target hero is available to join the player's clan.");
		if (SocialSourceTexts.Is(detail, "S250"))
			return Text("AF_SOCIAL_UI_RecruitmentMissingCompanionHero", "No target hero is available to become a player companion.");
		if (SocialSourceTexts.Is(detail, "S251"))
			return Text("AF_SOCIAL_UI_RecruitmentAlreadyPlayer", "The target is already the player.");
		if (SocialSourceTexts.Is(detail, "S252") || SocialSourceTexts.Is(detail, "S253"))
			return Text("AF_SOCIAL_UI_RecruitmentPlayerPartyUnavailable", "The player's clan or party is unavailable.");
		if (SocialSourceTexts.Is(detail, "S254") || SocialSourceTexts.Is(detail, "S255"))
			return Text("AF_SOCIAL_UI_RecruitmentMissingNonHero", "No non-hero NPC is available to join the party.");
		if (SocialSourceTexts.Is(detail, "S256"))
			return Text("AF_SOCIAL_UI_RecruitmentTargetIsHero", "The target is a hero and must use the hero recruitment path.");
		if (SocialSourceTexts.Is(detail, "S257"))
			return Text("AF_SOCIAL_UI_RecruitmentMissingLiveAgent", "The speaking NPC's live agent could not be found; promotion was cancelled.");
		if (SocialSourceTexts.Is(detail, "S258"))
			return Text("AF_SOCIAL_UI_RecruitmentInvalidAgentTemplate", "The current agent is not a promotable non-hero troop template.");
		if (SocialSourceTexts.Is(detail, "S259"))
			return Text("AF_SOCIAL_UI_RecruitmentMissingPromotedName", "The promoted hero's personal name could not be determined; promotion was cancelled.");
		if (SocialSourceTexts.Is(detail, "S260"))
			return Text("AF_SOCIAL_UI_RecruitmentTavernAlreadyHandled", "The tavern mercenary pool was already handled by the native option or another flow; recruitment was not repeated.");
		if (SocialSourceTexts.Is(detail, "S261"))
			return Text("AF_SOCIAL_UI_RecruitmentCaptivityCleanupFailed", "The target hero's captivity state or prisoner roster residue could not be cleared.");
		if (SocialSourceTexts.Is(detail, "S262"))
			return Text("AF_SOCIAL_UI_RecruitmentHeroActivationFailed", "The target hero could not be restored to an active state for party entry.");
		if (SocialSourceTexts.Is(detail, "S263"))
			return Text("AF_SOCIAL_UI_RecruitmentLordJoinFailed", "The target hero could not be made a player-clan member or assigned to the main party.");
		if (SocialSourceTexts.Is(detail, "S264"))
			return Text("AF_SOCIAL_UI_RecruitmentCompanionJoinFailed", "The target hero could not be made a companion or assigned to the main party.");

		Match match = MatchAny(detail, "S265");
		if (match.Success)
			return Format("AF_SOCIAL_UI_RecruitmentClanSuccessionIncomplete", "Clan leader succession for {CLAN} is incomplete; recruiting the active ruler was prevented.", ("CLAN", match.Groups["clan"].Value));
		match = MatchAny(detail, "S266");
		if (match.Success)
			return Format("AF_SOCIAL_UI_RecruitmentCopyAppearanceFailed", "Failed to copy current NPC appearance ({ERROR}); promotion cancelled.", ("ERROR", match.Groups["error"].Value));
		match = MatchAny(detail, "S267");
		if (match.Success)
			return Format("AF_SOCIAL_UI_RecruitmentCopyEquipmentFailed", "Failed to copy current NPC equipment ({ERROR}); promotion cancelled.", ("ERROR", match.Groups["error"].Value));

		match = MatchAny(detail, "S268");
		if (match.Success)
			return Format("AF_SOCIAL_UI_RecruitmentAlreadyCompanion", "{NAME} is already a player companion in the main party.", ("NAME", match.Groups["name"].Value));
		match = MatchAny(detail, "S269");
		if (match.Success)
			return Format("AF_SOCIAL_UI_RecruitmentAlreadyClanMember", "{NAME} is already a player-clan member in the main party.", ("NAME", match.Groups["name"].Value));
		match = MatchAny(detail, "S270");
		if (match.Success)
			return Format("AF_SOCIAL_UI_RecruitmentAlreadyPromoted", "{NAME} was already promoted from the current scene NPC; a second hero cannot be recruited.", ("NAME", match.Groups["name"].Value));
		match = MatchAny(detail, "S271");
		if (match.Success)
			return Format("AF_SOCIAL_UI_RecruitmentTavernSoldiers", "{COUNT} {NAME} mercenaries joined the player's party as regular troops.", ("COUNT", match.Groups["count"].Value), ("NAME", match.Groups["name"].Value));
		match = MatchAny(detail, "S272");
		if (match.Success)
			return Format("AF_SOCIAL_UI_RecruitmentPromotedCompanion", "{NAME} was promoted to player companion \"{HERO}\" and joined the player's party{FOLLOW}.", ("NAME", match.Groups["name"].Value), ("HERO", match.Groups["hero"].Value), ("FOLLOW", match.Groups["follow"].Success ? Text("AF_SOCIAL_UI_RecruitmentFollowSuffix", " and is now following you in the scene") : ""));
		match = MatchAny(detail, "S273");
		if (match.Success)
			return Format("AF_SOCIAL_UI_RecruitmentPromotedLord", "{NAME} was promoted to player-clan hero \"{HERO}\" and joined the player's party{FOLLOW}.", ("NAME", match.Groups["name"].Value), ("HERO", match.Groups["hero"].Value), ("FOLLOW", match.Groups["follow"].Success ? Text("AF_SOCIAL_UI_RecruitmentFollowSuffix", " and is now following you in the scene") : ""));

		match = MatchAny(detail, "S274");
		if (match.Success)
			return Format("AF_SOCIAL_UI_RecruitmentBecameCompanion", "{NAME} has become a player companion and joined the party{SUMMARY}.", ("NAME", match.Groups["name"].Value), ("SUMMARY", TranslateRecruitmentTransitionSummary(match.Groups["summary"].Value)));
		match = MatchAny(detail, "S275");
		if (match.Success)
			return Format("AF_SOCIAL_UI_RecruitmentBecameClanMember", "{NAME} has become a player-clan member and joined the party{SUMMARY}.", ("NAME", match.Groups["name"].Value), ("SUMMARY", TranslateRecruitmentTransitionSummary(match.Groups["summary"].Value)));
		match = MatchAny(detail, "S276");
		if (match.Success)
		{
			string role = SocialSourceTexts.Is(match.Groups["role"].Value, "S277") ? Text("AF_SOCIAL_UI_RoleSpouse", "spouse") : Text("AF_SOCIAL_UI_RoleClanMember", "player-clan member");
			return Format("AF_SOCIAL_UI_RecruitmentRetainedIdentity", "{NAME} retained {ROLE} status and joined the party{SUMMARY}.", ("NAME", match.Groups["name"].Value), ("ROLE", role), ("SUMMARY", TranslateRecruitmentTransitionSummary(match.Groups["summary"].Value)));
		}

		return detail;
	}

	private static string TranslateRecruitmentTransitionSummary(string rawSummary)
	{
		if (string.IsNullOrWhiteSpace(rawSummary))
			return string.Empty;
		string inner = rawSummary.Trim();
		if (inner.StartsWith("\uFF08") && inner.EndsWith("\uFF09"))
			inner = inner.Substring(1, inner.Length - 2);
		else if (inner.StartsWith("(") && inner.EndsWith(")"))
			inner = inner.Substring(1, inner.Length - 2);

		string[] parts = inner.Split(new char[] { '\uFF1B', ';' }, StringSplitOptions.RemoveEmptyEntries);
		List<string> translatedParts = new List<string>();
		foreach (string part in parts)
		{
			string p = part.Trim();
			Match m = MatchAny(p, "S278");
			if (m.Success)
			{
				translatedParts.Add(Format("AF_SOCIAL_UI_NoteGovernorRemoved", "removed as governor of {TOWN}", ("TOWN", m.Groups["town"].Value)));
				continue;
			}
			m = MatchAny(p, "S279");
			if (m.Success)
			{
				translatedParts.Add(Format("AF_SOCIAL_UI_NoteClanLeaderSuccession", "{HERO} succeeded as leader of {CLAN}", ("HERO", m.Groups["hero"].Value), ("CLAN", m.Groups["clan"].Value)));
				continue;
			}
			if (EndsWithAny(p, "S280", out int suffixLength))
			{
				string clan = p.Substring(0, p.Length - suffixLength).Trim();
				translatedParts.Add(Format("AF_SOCIAL_UI_NoteClanDestroyedNoHeir", "{CLAN} had no heirs and was disbanded", ("CLAN", clan)));
				continue;
			}
			m = MatchAny(p, "S281");
			if (m.Success)
			{
				translatedParts.Add(Format("AF_SOCIAL_UI_NotePartyMergedNoExtras", "{PARTY} merged into player party with no extra troops or prisoners", ("PARTY", m.Groups["party"].Value)));
				continue;
			}
			m = MatchAny(p, "S282");
			if (m.Success)
			{
				translatedParts.Add(Format("AF_SOCIAL_UI_NotePartyTransferred", "{COUNT} from {PARTY} transferred to player party", ("COUNT", m.Groups["count"].Value), ("PARTY", m.Groups["party"].Value)));
				continue;
			}
			translatedParts.Add(p);
		}
		if (translatedParts.Count == 0)
			return string.Empty;

		string separator = Text("AF_SOCIAL_UI_SummarySeparator", "; ");
		string open = Text("AF_SOCIAL_UI_SummaryOpen", " (");
		string close = Text("AF_SOCIAL_UI_SummaryClose", ")");
		return open + string.Join(separator, translatedParts) + close;
	}

	// Status text may have been written in either language (it is also stored in history), so every
	// recognizer accepts the Chinese and English forms. Runs once per notification, not per frame;
	// Regex.Match reuses the framework's pattern cache.
	private static bool StartsWithAny(string value, string key, out int length)
	{
		foreach (string variant in SocialSourceTexts.Variants(key))
		{
			if (variant.Length > 0 && value.StartsWith(variant, StringComparison.Ordinal))
			{
				length = variant.Length;
				return true;
			}
		}
		length = 0;
		return false;
	}

	private static bool EndsWithAny(string value, string key, out int length)
	{
		foreach (string variant in SocialSourceTexts.Variants(key))
		{
			if (variant.Length > 0 && value.EndsWith(variant, StringComparison.Ordinal))
			{
				length = variant.Length;
				return true;
			}
		}
		length = 0;
		return false;
	}

	private static Match MatchAny(string value, string key)
	{
		Match match = Match.Empty;
		foreach (Regex pattern in SocialSourceTexts.Regexes(key))
		{
			match = pattern.Match(value);
			if (match.Success)
				return match;
		}
		return match;
	}
}
