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
		if (status.StartsWith(SocialSourceTexts.Get("S220"), StringComparison.Ordinal))
		{
			prefixId = "AF_SOCIAL_UI_RecruitmentFailed";
			prefixFallback = "[Recruitment failed] {DETAIL}";
			detail = status.Substring(SocialSourceTexts.Get("S220").Length);
		}
		else if (status.StartsWith(SocialSourceTexts.Get("S221"), StringComparison.Ordinal))
		{
			prefixId = "AF_SOCIAL_UI_RecruitmentFailed";
			prefixFallback = "[Recruitment failed] {DETAIL}";
			detail = status.Substring(SocialSourceTexts.Get("S222").Length);
		}
		else if (status.StartsWith(SocialSourceTexts.Get("S223"), StringComparison.Ordinal))
		{
			prefixId = "AF_SOCIAL_UI_RecruitmentSkipped";
			prefixFallback = "[Recruitment skipped] {DETAIL}";
			detail = status.Substring(SocialSourceTexts.Get("S223").Length);
		}
		else if (status.StartsWith(SocialSourceTexts.Get("S224"), StringComparison.Ordinal))
		{
			prefixId = "AF_SOCIAL_UI_RecruitmentIntercepted";
			prefixFallback = "[Recruitment intercepted] {DETAIL}";
			detail = status.Substring(SocialSourceTexts.Get("S224").Length);
		}
		else if (status.StartsWith(SocialSourceTexts.Get("S225"), StringComparison.Ordinal))
		{
			prefixId = "AF_SOCIAL_UI_RecruitmentSucceeded";
			prefixFallback = "[Recruitment succeeded] {DETAIL}";
			detail = status.Substring(SocialSourceTexts.Get("S225").Length);
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
		if (status.StartsWith(SocialSourceTexts.Get("S177"), StringComparison.Ordinal))
			return Format("AF_SOCIAL_UI_MarriageFormalFailed", "Formal marriage failed: {REASON}", ("REASON", TranslateMarriageReason(status.Substring(SocialSourceTexts.Get("S177").Length))));
		if (status.StartsWith(SocialSourceTexts.Get("S188"), StringComparison.Ordinal))
			return Format("AF_SOCIAL_UI_MarriageElopeFailed", "Elope failed: {REASON}", ("REASON", TranslateMarriageReason(status.Substring(SocialSourceTexts.Get("S188").Length))));
		if (status.StartsWith(SocialSourceTexts.Get("S226"), StringComparison.Ordinal))
			return Format("AF_SOCIAL_UI_MarriageDivorceFailed", "Divorce failed: {REASON}", ("REASON", TranslateMarriageReason(status.Substring(SocialSourceTexts.Get("S226").Length))));

		Match match = Regex.Match(status, SocialSourceTexts.Get("S227"));
		if (match.Success)
		{
			string extra = TranslateMarriageExtra(match.Groups["extra"].Value);
			return Format("AF_SOCIAL_UI_MarriageFormalSucceeded", "Formal marriage succeeded: {LEFT} and {RIGHT} are now married. Tier gap D={TIER}, clan relation={RELATION}, leader trust={TRUST}.{EXTRA}",
				("LEFT", match.Groups["left"].Value), ("RIGHT", match.Groups["right"].Value), ("TIER", match.Groups["tier"].Value), ("RELATION", match.Groups["relation"].Value), ("TRUST", match.Groups["trust"].Value), ("EXTRA", extra));
		}
		match = Regex.Match(status, SocialSourceTexts.Get("S228"));
		if (match.Success)
			return Format("AF_SOCIAL_UI_MarriageElopeSucceeded", "Elope succeeded: you and {TARGET} are now married. Current L={LOVE}, personal trust={TRUST}, family harmony F={HARMONY}.", ("TARGET", match.Groups["target"].Value), ("LOVE", match.Groups["love"].Value), ("TRUST", match.Groups["trust"].Value), ("HARMONY", match.Groups["harmony"].Value));
		match = Regex.Match(status, SocialSourceTexts.Get("S229"));
		if (match.Success)
			return Format("AF_SOCIAL_UI_MarriageDivorceSucceeded", "Divorce succeeded: {LEFT} and {RIGHT} are no longer married.{EXTRA}", ("LEFT", match.Groups["left"].Value), ("RIGHT", match.Groups["right"].Value), ("EXTRA", TranslateMarriageExtra(match.Groups["extra"].Value)));
		return status;
	}

	private static string TranslateMarriageReason(string reason)
	{
		if (string.IsNullOrWhiteSpace(reason)) return string.Empty;
		if (reason == SocialSourceTexts.Get("S116")) return Text("AF_SOCIAL_UI_MarriageReasonMissingPair", "The marriage pair could not be found.");
		if (reason == SocialSourceTexts.Get("S230")) return Text("AF_SOCIAL_UI_MarriageReasonMissingDivorcePair", "The divorce pair could not be found.");
		if (reason == SocialSourceTexts.Get("S117") || reason == SocialSourceTexts.Get("S172")) return Text("AF_SOCIAL_UI_MarriageReasonSelf", "A character cannot marry themselves.");
		if (reason == SocialSourceTexts.Get("S118")) return Text("AF_SOCIAL_UI_MarriageReasonAlreadyMarried", "The pair already has an active marriage record.");
		if (reason == SocialSourceTexts.Get("S156")) return Text("AF_SOCIAL_UI_MarriageReasonSameClan", "A formal marriage requires two different clans.");
		if (reason == SocialSourceTexts.Get("S231") || reason == SocialSourceTexts.Get("S232")) return Text("AF_SOCIAL_UI_MarriageReasonMissingTarget", "The target marriage partner could not be found.");
		if (reason == SocialSourceTexts.Get("S233")) return Text("AF_SOCIAL_UI_MarriageReasonMissingElopeTarget", "The target partner could not be found.");
		if (reason == SocialSourceTexts.Get("S234")) return Text("AF_SOCIAL_UI_MarriageReasonMissingTargetClanLeader", "The target clan leader could not be found.");
		if (reason == SocialSourceTexts.Get("S171")) return Text("AF_SOCIAL_UI_MarriageReasonPlayerCandidateNotClan", "The player-side marriage candidate must belong to the player's clan.");
		if (reason == SocialSourceTexts.Get("S173") || reason == SocialSourceTexts.Get("S151")) return Text("AF_SOCIAL_UI_MarriageReasonPlayerNotEligible", "The player-side marriage candidate does not meet basic marriage requirements.");
		if (reason == SocialSourceTexts.Get("S174") || reason == SocialSourceTexts.Get("S152")) return Text("AF_SOCIAL_UI_MarriageReasonTargetNotEligible", "The target marriage candidate does not meet basic marriage requirements.");
		if (reason == SocialSourceTexts.Get("S153")) return Text("AF_SOCIAL_UI_MarriageReasonAgeGapExceeded", "The age gap between both parties exceeds the allowed limit.");
		if (reason == SocialSourceTexts.Get("S154")) return Text("AF_SOCIAL_UI_MarriageReasonAgeGapUnverified", "Could not verify whether the age gap meets marriage requirements.");
		if (reason == SocialSourceTexts.Get("S235")) return Text("AF_SOCIAL_UI_MarriageReasonMissingPlayerCandidate", "The player-clan marriage candidate could not be found.");
		if (reason == SocialSourceTexts.Get("S236")) return Text("AF_SOCIAL_UI_MarriageReasonMissingDivorcePlayerCandidate", "The player-clan divorce candidate could not be found.");
		if (reason == SocialSourceTexts.Get("S237")) return Text("AF_SOCIAL_UI_MarriageReasonMissingDivorceTarget", "The target divorce partner could not be found.");
		if (reason == SocialSourceTexts.Get("S176")) return Text("AF_SOCIAL_UI_MarriageReasonTargetNoClan", "The target has no clan and cannot use the clan marriage flow.");
		if (reason == SocialSourceTexts.Get("S238")) return Text("AF_SOCIAL_UI_MarriageReasonNoRecord", "No active marriage record was found.");
		if (reason == SocialSourceTexts.Get("S239")) return Text("AF_SOCIAL_UI_MarriageReasonNotSpouses", "The pair are not currently spouses.");
		if (reason == SocialSourceTexts.Get("S175")) return Text("AF_SOCIAL_UI_MarriageReasonMissingPlayer", "The player hero could not be found.");
		if (reason == SocialSourceTexts.Get("S155")) return Text("AF_SOCIAL_UI_MarriageReasonMissingClans", "Clan information for both parties could not be found.");
		if (reason == SocialSourceTexts.Get("S157")) return Text("AF_SOCIAL_UI_MarriageReasonNoPlayerEligibleMembers", "The player clan currently has no eligible members for marriage.");
		if (reason.StartsWith(SocialSourceTexts.Get("S240"), StringComparison.Ordinal))
			return Format("AF_SOCIAL_UI_MarriageReasonMultiFailed", "Polygamy execution failed: {DETAIL}", ("DETAIL", reason.Substring(SocialSourceTexts.Get("S240").Length)));
		if (reason.StartsWith(SocialSourceTexts.Get("S241"), StringComparison.Ordinal))
			return Format("AF_SOCIAL_UI_MarriageReasonActionFailed", "MarriageAction execution failed: {DETAIL}", ("DETAIL", reason.Substring(SocialSourceTexts.Get("S241").Length)));
		if (reason.StartsWith(SocialSourceTexts.Get("S242"), StringComparison.Ordinal))
			return Format("AF_SOCIAL_UI_MarriageReasonDissolveException", "Error dissolving marriage: {DETAIL}", ("DETAIL", reason.Substring(SocialSourceTexts.Get("S242").Length)));
		return reason;
	}

	private static string TranslateMarriageExtra(string extra)
	{
		if (string.IsNullOrWhiteSpace(extra)) return string.Empty;
		Match match = Regex.Match(extra, SocialSourceTexts.Get("S243"));
		if (match.Success) return Format("AF_SOCIAL_UI_MarriageBridePriceConsumed", " The previously prepaid bride price of {AMOUNT} denars was consumed.", ("AMOUNT", match.Groups["amount"].Value));
		match = Regex.Match(extra, SocialSourceTexts.Get("S244"));
		if (match.Success) return Format("AF_SOCIAL_UI_MarriageBridePricePaid", " The other clan leader paid the player a bride price of {AMOUNT} denars.", ("AMOUNT", match.Groups["amount"].Value));
		match = Regex.Match(extra, SocialSourceTexts.Get("S245"));
		if (match.Success) return Format("AF_SOCIAL_UI_MarriageBridePriceRefunded", " A bride price of {AMOUNT} denars was refunded.", ("AMOUNT", match.Groups["amount"].Value));
		return extra;
	}

	private static string TranslateRecruitmentDetail(string detail)
	{
		if (string.IsNullOrWhiteSpace(detail))
			return string.Empty;

		if (detail.StartsWith(SocialSourceTexts.Get("S246"), StringComparison.Ordinal))
			return Format("AF_SOCIAL_UI_RecruitmentException", "Exception: {ERROR}", ("ERROR", detail.Substring(SocialSourceTexts.Get("S246").Length)));
		if (detail.StartsWith(SocialSourceTexts.Get("S247"), StringComparison.Ordinal))
			return Format("AF_SOCIAL_UI_RecruitmentJoinClanException", "Join player clan exception: {ERROR}", ("ERROR", detail.Substring(SocialSourceTexts.Get("S247").Length)));
		if (detail.StartsWith(SocialSourceTexts.Get("S248"), StringComparison.Ordinal))
			return Format("AF_SOCIAL_UI_RecruitmentBecomeCompanionException", "Become player companion exception: {ERROR}", ("ERROR", detail.Substring(SocialSourceTexts.Get("S248").Length)));

		if (detail == SocialSourceTexts.Get("S249"))
			return Text("AF_SOCIAL_UI_RecruitmentMissingClanHero", "No target hero is available to join the player's clan.");
		if (detail == SocialSourceTexts.Get("S250"))
			return Text("AF_SOCIAL_UI_RecruitmentMissingCompanionHero", "No target hero is available to become a player companion.");
		if (detail == SocialSourceTexts.Get("S251"))
			return Text("AF_SOCIAL_UI_RecruitmentAlreadyPlayer", "The target is already the player.");
		if (detail == SocialSourceTexts.Get("S252") || detail == SocialSourceTexts.Get("S253"))
			return Text("AF_SOCIAL_UI_RecruitmentPlayerPartyUnavailable", "The player's clan or party is unavailable.");
		if (detail == SocialSourceTexts.Get("S254") || detail == SocialSourceTexts.Get("S255"))
			return Text("AF_SOCIAL_UI_RecruitmentMissingNonHero", "No non-hero NPC is available to join the party.");
		if (detail == SocialSourceTexts.Get("S256"))
			return Text("AF_SOCIAL_UI_RecruitmentTargetIsHero", "The target is a hero and must use the hero recruitment path.");
		if (detail == SocialSourceTexts.Get("S257"))
			return Text("AF_SOCIAL_UI_RecruitmentMissingLiveAgent", "The speaking NPC's live agent could not be found; promotion was cancelled.");
		if (detail == SocialSourceTexts.Get("S258"))
			return Text("AF_SOCIAL_UI_RecruitmentInvalidAgentTemplate", "The current agent is not a promotable non-hero troop template.");
		if (detail == SocialSourceTexts.Get("S259"))
			return Text("AF_SOCIAL_UI_RecruitmentMissingPromotedName", "The promoted hero's personal name could not be determined; promotion was cancelled.");
		if (detail == SocialSourceTexts.Get("S260"))
			return Text("AF_SOCIAL_UI_RecruitmentTavernAlreadyHandled", "The tavern mercenary pool was already handled by the native option or another flow; recruitment was not repeated.");
		if (detail == SocialSourceTexts.Get("S261"))
			return Text("AF_SOCIAL_UI_RecruitmentCaptivityCleanupFailed", "The target hero's captivity state or prisoner roster residue could not be cleared.");
		if (detail == SocialSourceTexts.Get("S262"))
			return Text("AF_SOCIAL_UI_RecruitmentHeroActivationFailed", "The target hero could not be restored to an active state for party entry.");
		if (detail == SocialSourceTexts.Get("S263"))
			return Text("AF_SOCIAL_UI_RecruitmentLordJoinFailed", "The target hero could not be made a player-clan member or assigned to the main party.");
		if (detail == SocialSourceTexts.Get("S264"))
			return Text("AF_SOCIAL_UI_RecruitmentCompanionJoinFailed", "The target hero could not be made a companion or assigned to the main party.");

		Match match = Regex.Match(detail, SocialSourceTexts.Get("S265"));
		if (match.Success)
			return Format("AF_SOCIAL_UI_RecruitmentClanSuccessionIncomplete", "Clan leader succession for {CLAN} is incomplete; recruiting the active ruler was prevented.", ("CLAN", match.Groups["clan"].Value));
		match = Regex.Match(detail, SocialSourceTexts.Get("S266"));
		if (match.Success)
			return Format("AF_SOCIAL_UI_RecruitmentCopyAppearanceFailed", "Failed to copy current NPC appearance ({ERROR}); promotion cancelled.", ("ERROR", match.Groups["error"].Value));
		match = Regex.Match(detail, SocialSourceTexts.Get("S267"));
		if (match.Success)
			return Format("AF_SOCIAL_UI_RecruitmentCopyEquipmentFailed", "Failed to copy current NPC equipment ({ERROR}); promotion cancelled.", ("ERROR", match.Groups["error"].Value));

		match = Regex.Match(detail, SocialSourceTexts.Get("S268"));
		if (match.Success)
			return Format("AF_SOCIAL_UI_RecruitmentAlreadyCompanion", "{NAME} is already a player companion in the main party.", ("NAME", match.Groups["name"].Value));
		match = Regex.Match(detail, SocialSourceTexts.Get("S269"));
		if (match.Success)
			return Format("AF_SOCIAL_UI_RecruitmentAlreadyClanMember", "{NAME} is already a player-clan member in the main party.", ("NAME", match.Groups["name"].Value));
		match = Regex.Match(detail, SocialSourceTexts.Get("S270"));
		if (match.Success)
			return Format("AF_SOCIAL_UI_RecruitmentAlreadyPromoted", "{NAME} was already promoted from the current scene NPC; a second hero cannot be recruited.", ("NAME", match.Groups["name"].Value));
		match = Regex.Match(detail, SocialSourceTexts.Get("S271"));
		if (match.Success)
			return Format("AF_SOCIAL_UI_RecruitmentTavernSoldiers", "{COUNT} {NAME} mercenaries joined the player's party as regular troops.", ("COUNT", match.Groups["count"].Value), ("NAME", match.Groups["name"].Value));
		match = Regex.Match(detail, SocialSourceTexts.Get("S272"));
		if (match.Success)
			return Format("AF_SOCIAL_UI_RecruitmentPromotedCompanion", "{NAME} was promoted to player companion \"{HERO}\" and joined the player's party{FOLLOW}.", ("NAME", match.Groups["name"].Value), ("HERO", match.Groups["hero"].Value), ("FOLLOW", match.Groups["follow"].Success ? Text("AF_SOCIAL_UI_RecruitmentFollowSuffix", " and is now following you in the scene") : ""));
		match = Regex.Match(detail, SocialSourceTexts.Get("S273"));
		if (match.Success)
			return Format("AF_SOCIAL_UI_RecruitmentPromotedLord", "{NAME} was promoted to player-clan hero \"{HERO}\" and joined the player's party{FOLLOW}.", ("NAME", match.Groups["name"].Value), ("HERO", match.Groups["hero"].Value), ("FOLLOW", match.Groups["follow"].Success ? Text("AF_SOCIAL_UI_RecruitmentFollowSuffix", " and is now following you in the scene") : ""));

		match = Regex.Match(detail, SocialSourceTexts.Get("S274"));
		if (match.Success)
			return Format("AF_SOCIAL_UI_RecruitmentBecameCompanion", "{NAME} has become a player companion and joined the party{SUMMARY}.", ("NAME", match.Groups["name"].Value), ("SUMMARY", TranslateRecruitmentTransitionSummary(match.Groups["summary"].Value)));
		match = Regex.Match(detail, SocialSourceTexts.Get("S275"));
		if (match.Success)
			return Format("AF_SOCIAL_UI_RecruitmentBecameClanMember", "{NAME} has become a player-clan member and joined the party{SUMMARY}.", ("NAME", match.Groups["name"].Value), ("SUMMARY", TranslateRecruitmentTransitionSummary(match.Groups["summary"].Value)));
		match = Regex.Match(detail, SocialSourceTexts.Get("S276"));
		if (match.Success)
		{
			string role = match.Groups["role"].Value == SocialSourceTexts.Get("S277") ? Text("AF_SOCIAL_UI_RoleSpouse", "spouse") : Text("AF_SOCIAL_UI_RoleClanMember", "player-clan member");
			return Format("AF_SOCIAL_UI_RecruitmentRetainedIdentity", "{NAME} retained {ROLE} status and joined the party{SUMMARY}.", ("NAME", match.Groups["name"].Value), ("ROLE", role), ("SUMMARY", TranslateRecruitmentTransitionSummary(match.Groups["summary"].Value)));
		}

		return detail;
	}

	private static string TranslateRecruitmentTransitionSummary(string rawSummary)
	{
		if (string.IsNullOrWhiteSpace(rawSummary))
			return string.Empty;
		string inner = rawSummary.Trim();
		if (inner.StartsWith("（") && inner.EndsWith("）"))
			inner = inner.Substring(1, inner.Length - 2);
		else if (inner.StartsWith("(") && inner.EndsWith(")"))
			inner = inner.Substring(1, inner.Length - 2);

		string[] parts = inner.Split(new char[] { '；', ';' }, StringSplitOptions.RemoveEmptyEntries);
		List<string> translatedParts = new List<string>();
		foreach (string part in parts)
		{
			string p = part.Trim();
			Match m = Regex.Match(p, SocialSourceTexts.Get("S278"));
			if (m.Success)
			{
				translatedParts.Add(Format("AF_SOCIAL_UI_NoteGovernorRemoved", "removed as governor of {TOWN}", ("TOWN", m.Groups["town"].Value)));
				continue;
			}
			m = Regex.Match(p, SocialSourceTexts.Get("S279"));
			if (m.Success)
			{
				translatedParts.Add(Format("AF_SOCIAL_UI_NoteClanLeaderSuccession", "{HERO} succeeded as leader of {CLAN}", ("HERO", m.Groups["hero"].Value), ("CLAN", m.Groups["clan"].Value)));
				continue;
			}
			if (p.EndsWith(SocialSourceTexts.Get("S280")))
			{
				string clan = p.Substring(0, p.Length - SocialSourceTexts.Get("S280").Length).Trim();
				translatedParts.Add(Format("AF_SOCIAL_UI_NoteClanDestroyedNoHeir", "{CLAN} had no heirs and was disbanded", ("CLAN", clan)));
				continue;
			}
			m = Regex.Match(p, SocialSourceTexts.Get("S281"));
			if (m.Success)
			{
				translatedParts.Add(Format("AF_SOCIAL_UI_NotePartyMergedNoExtras", "{PARTY} merged into player party with no extra troops or prisoners", ("PARTY", m.Groups["party"].Value)));
				continue;
			}
			m = Regex.Match(p, SocialSourceTexts.Get("S282"));
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
}
