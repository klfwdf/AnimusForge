using System;
using System.Linq;

namespace AnimusForge.Refactor.Domain;

public static class WorldDiplomacyIntentVocabulary
{
    public static string NormalizeToken(string value)
    {
        return (value ?? "").Trim().ToLowerInvariant().Replace('-', '_').Replace(' ', '_');
    }

    public static string NormalizeIntent(string value)
    {
        string token = NormalizeToken(value);
        return token switch
        {
            "make_peace" or "peace" or "peace_proposal" => "propose_peace",
            "form_alliance" or "alliance" or "alliance_proposal" => "propose_alliance",
            "make_trade" or "trade" or "trade_proposal" => "propose_trade",
            "terminate_alliance" => "break_alliance",
            "end_trade" or "terminate_trade" => "cancel_trade",
            "war" or "declarewar" => "declare_war",
            "complyultimatum" or "comply_with_ultimatum" => "comply_ultimatum",
            "threat" => "warning",
            "denounce" => "condemn",
            "accept" => "accept",
            "reject" => "reject",
            _ => token
        };
    }

    public static string NormalizeCommitment(string value)
    {
        string token = NormalizeToken(value);
        return token switch
        {
            "formal" or "explicit" or "committed" => "binding",
            "offer" => "proposal",
            "accepted" => "acceptance",
            "rejected" => "rejection",
            "none" or "nonbinding" => "non_binding",
            _ => token
        };
    }

    public static string DefaultCommitmentForIntent(string intent)
    {
		string normalized = NormalizeIntent(intent);
		if (IsProposalIntent(normalized)) return "proposal";
		if (normalized.StartsWith("accept_", StringComparison.Ordinal)) return "acceptance";
		if (normalized.StartsWith("reject_", StringComparison.Ordinal)) return "rejection";
		return normalized is "withdraw_offer" or "ultimatum" or "comply_ultimatum" or "apology" or "concession"
			or "declare_war" or "break_alliance" or "cancel_trade" or "release_subject"
			? "binding"
			: "non_binding";
	}

    public static string NormalizeTone(string value)
    {
        string token = NormalizeToken(value);
        return token is "conciliatory" or "neutral" or "firm" or "hostile" ? token : "neutral";
    }

    public static string NormalizeNegotiationMove(string value)
    {
        return NormalizeToken(value);
    }

    public static bool ContainsAny(string text, params string[] needles)
    {
        return (needles ?? Array.Empty<string>()).Any(x => !string.IsNullOrWhiteSpace(x) && (text ?? "").IndexOf(x, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    public static bool IsImmediateIntent(string intent)
    {
		string normalized = NormalizeIntent(intent);
		return normalized == "withdraw_offer" || normalized == "declare_war" || normalized == "break_alliance" || normalized == "cancel_trade" || normalized == "release_subject"
			|| normalized == "comply_ultimatum"
			|| (IsFormalTreatyIntent(normalized) && normalized.StartsWith("accept_"));
	}

    public static bool IsProposalIntent(string intent)
    {
		string normalized = NormalizeIntent(intent);
		return normalized == "propose_peace" || normalized == "propose_alliance" || normalized == "propose_trade"
			|| (IsFormalTreatyIntent(normalized) && normalized.StartsWith("propose_"));
	}

    public static string ResponseIntentToProposalIntent(string intent)
    {
		return NormalizeIntent(intent) switch
		{
			"accept_peace" or "reject_peace" => "propose_peace",
			"accept_alliance" or "reject_alliance" => "propose_alliance",
			"accept_trade" or "reject_trade" => "propose_trade",
			"accept_vassal" or "reject_vassal" => "propose_vassal",
			"accept_garrison" or "reject_garrison" => "propose_garrison",
			"accept_tributary" or "reject_tributary" => "propose_tributary",
			"accept_annexation" or "reject_annexation" => "propose_annexation",
			_ => ""
		};
	}

    public static string ProposalIntentToResponseIntent(string proposalIntent, bool accepted)
    {
		return NormalizeIntent(proposalIntent) switch
		{
			"propose_peace" => accepted ? "accept_peace" : "reject_peace",
			"propose_alliance" => accepted ? "accept_alliance" : "reject_alliance",
			"propose_trade" => accepted ? "accept_trade" : "reject_trade",
			"propose_vassal" => accepted ? "accept_vassal" : "reject_vassal",
			"propose_garrison" => accepted ? "accept_garrison" : "reject_garrison",
			"propose_tributary" => accepted ? "accept_tributary" : "reject_tributary",
			"propose_annexation" => accepted ? "accept_annexation" : "reject_annexation",
			_ => ""
		};
	}

    public static bool IsPeaceIntent(string intent)
    {
        string normalized = NormalizeIntent(intent);
        return normalized == "propose_peace" || normalized == "accept_peace" || normalized == "reject_peace";
    }

    public static bool IsSupportedDiplomacyIntent(string intent)
    {
		return NormalizeIntent(intent) is "statement" or "condemn" or "warning" or "ultimatum" or "comply_ultimatum" or "apology" or "concession"
			or "propose_peace" or "accept_peace" or "reject_peace"
			or "propose_alliance" or "accept_alliance" or "reject_alliance" or "break_alliance"
			or "propose_trade" or "accept_trade" or "reject_trade" or "cancel_trade" or "release_subject" or "withdraw_offer" or "declare_war"
			|| IsFormalTreatyIntent(intent);
	}

    public static bool IsSupportedNegotiationMove(string value)
    {
        return NormalizeNegotiationMove(value) is "question" or "clarification" or "state_position" or "justify_demand"
            or "acknowledge_concern" or "dispute_claim" or "counterproposal" or "conditional_acceptance"
            or "partial_concession" or "request_concession" or "revise_terms" or "request_delay"
            or "consult_court" or "set_deadline" or "final_offer" or "withdraw_offer"
            or "end_negotiation" or "declare_deadlock";
    }

    public static bool IsTerminalNegotiationMove(string value)
    {
        return NormalizeNegotiationMove(value) is "end_negotiation" or "declare_deadlock";
    }

    public static bool IsActionableDiplomacyIntent(string intent)
    {
		return NormalizeIntent(intent) is "warning" or "ultimatum" or "comply_ultimatum"
			or "propose_peace" or "accept_peace" or "reject_peace"
			or "propose_alliance" or "accept_alliance" or "reject_alliance" or "break_alliance"
			or "propose_trade" or "accept_trade" or "reject_trade" or "cancel_trade" or "release_subject" or "withdraw_offer" or "declare_war"
			|| IsFormalTreatyIntent(intent);
	}

    public static bool IsExternallyResolvedDiplomaticIntent(string intent)
    {
		return NormalizeIntent(intent) is "declare_war" or "accept_peace"
			or "accept_alliance" or "break_alliance"
			or "accept_trade" or "cancel_trade" or "release_subject";
	}

    public static bool IsSupportedCommitment(string commitment)
    {
        return NormalizeCommitment(commitment) is "non_binding" or "proposal" or "acceptance" or "rejection" or "binding";
    }

    public static bool IsRoundDiplomaticBehaviorIntent(string intent)
    {
        string normalized = NormalizeIntent(intent);
        return IsActionableDiplomacyIntent(normalized) || normalized is "apology" or "concession";
    }

    public static bool IsTerminalResponseIntent(string intent)
    {
        return NormalizeIntent(intent) is "accept_peace" or "reject_peace"
            or "accept_alliance" or "reject_alliance"
            or "accept_trade" or "reject_trade"
            or "comply_ultimatum" or "apology" or "concession" or "break_alliance" or "cancel_trade" or "release_subject" or "declare_war";
    }

    public static bool IsAcceptanceIntent(string intent)
    {
        string normalized = NormalizeIntent(intent);
        return normalized == "accept"
            || normalized == "accept_peace"
            || normalized == "accept_alliance"
            || normalized == "accept_trade"
            || normalized == "comply_ultimatum";
    }

    public static string IntentLabel(string intent)
        => global::AnimusForge.DiplomacyCompatibilityTexts.Get(IntentLabelKey(intent));

    /// <summary>True when <paramref name="text"/> contains the intent label in either language (stored or player text).</summary>
    public static bool ContainsIntentLabel(string text, string intent)
    {
        if (string.IsNullOrEmpty(text)) return false;
        foreach (string label in global::AnimusForge.DiplomacyCompatibilityTexts.Variants(IntentLabelKey(intent)))
        {
            if (label.Length != 0 && text.IndexOf(label, StringComparison.OrdinalIgnoreCase) >= 0) return true;
        }
        return false;
    }

    private static string IntentLabelKey(string intent)
    {
		return NormalizeIntent(intent) switch
		{
			"withdraw_offer" => "IntentLabel_withdraw_offer",
			"propose_annexation" => "IntentLabel_propose_annexation",
			"accept_annexation" => "IntentLabel_accept_annexation",
			"reject_annexation" => "IntentLabel_reject_annexation",
			"propose_tributary" => "IntentLabel_propose_tributary",
			"accept_tributary" => "IntentLabel_accept_tributary",
			"reject_tributary" => "IntentLabel_reject_tributary",
			"propose_garrison" => "IntentLabel_propose_garrison",
			"accept_garrison" => "IntentLabel_accept_garrison",
			"reject_garrison" => "IntentLabel_reject_garrison",
			"propose_vassal" => "IntentLabel_propose_vassal",
			"accept_vassal" => "IntentLabel_accept_vassal",
			"reject_vassal" => "IntentLabel_reject_vassal",
			"declare_war" => "IntentLabel_declare_war",
			"propose_peace" => "IntentLabel_propose_peace",
			"accept_peace" => "IntentLabel_accept_peace",
			"reject_peace" => "IntentLabel_reject_peace",
			"propose_alliance" => "IntentLabel_propose_alliance",
			"accept_alliance" => "IntentLabel_accept_alliance",
			"reject_alliance" => "IntentLabel_reject_alliance",
			"break_alliance" => "IntentLabel_break_alliance",
			"propose_trade" => "IntentLabel_propose_trade",
			"accept_trade" => "IntentLabel_accept_trade",
			"reject_trade" => "IntentLabel_reject_trade",
			"cancel_trade" => "IntentLabel_cancel_trade",
            "release_subject" => "IntentLabel_release_subject",
			"comply_ultimatum" => "IntentLabel_comply_ultimatum",
			"ultimatum" => "IntentLabel_ultimatum",
			"warning" => "IntentLabel_warning",
			"condemn" => "IntentLabel_condemn",
			"apology" => "IntentLabel_apology",
			"concession" => "IntentLabel_concession",
			_ => "IntentVocabulary_OriginalTerm_032"
		};
	}

    public static string IntentLabel(string intent, Func<string, string, string> localize)
    {
        if (localize == null) return IntentLabel(intent);
        return NormalizeIntent(intent) switch
        {
            "withdraw_offer" => localize("IntentLabel_withdraw_offer", "Withdraw the original proposal"),
            "propose_annexation" => localize("IntentLabel_propose_annexation", "Propose kingdom annexation"),
            "accept_annexation" => localize("IntentLabel_accept_annexation", "Accept kingdom annexation"),
            "reject_annexation" => localize("IntentLabel_reject_annexation", "Reject kingdom annexation"),
            "propose_tributary" => localize("IntentLabel_propose_tributary", "Propose a tributary treaty"),
            "accept_tributary" => localize("IntentLabel_accept_tributary", "Accept a tributary treaty"),
            "reject_tributary" => localize("IntentLabel_reject_tributary", "Reject a tributary treaty"),
            "propose_garrison" => localize("IntentLabel_propose_garrison", "Propose a garrison treaty"),
            "accept_garrison" => localize("IntentLabel_accept_garrison", "Accept a garrison treaty"),
            "reject_garrison" => localize("IntentLabel_reject_garrison", "Reject a garrison treaty"),
            "propose_vassal" => localize("IntentLabel_propose_vassal", "Propose a full vassal treaty"),
            "accept_vassal" => localize("IntentLabel_accept_vassal", "Accept a full vassal treaty"),
            "reject_vassal" => localize("IntentLabel_reject_vassal", "Reject a full vassal treaty"),
            "declare_war" => localize("IntentLabel_declare_war", "Formal declaration of war"),
            "propose_peace" => localize("IntentLabel_propose_peace", "Peace proposal"),
            "accept_peace" => localize("IntentLabel_accept_peace", "Accept peace"),
            "reject_peace" => localize("IntentLabel_reject_peace", "Reject peace"),
            "propose_alliance" => localize("IntentLabel_propose_alliance", "Alliance proposal"),
            "accept_alliance" => localize("IntentLabel_accept_alliance", "Accept an alliance"),
            "reject_alliance" => localize("IntentLabel_reject_alliance", "Reject an alliance"),
            "break_alliance" => localize("IntentLabel_break_alliance", "Dissolve the alliance"),
            "propose_trade" => localize("IntentLabel_propose_trade", "Trade proposal"),
            "accept_trade" => localize("IntentLabel_accept_trade", "Accept trade"),
            "reject_trade" => localize("IntentLabel_reject_trade", "Reject trade"),
            "cancel_trade" => localize("IntentLabel_cancel_trade", "Terminate trade"),
            "release_subject" => localize("IntentLabel_release_subject", "Suzerain releases a subject kingdom"),
            "comply_ultimatum" => localize("IntentLabel_comply_ultimatum", "Comply with the ultimatum"),
            "ultimatum" => localize("IntentLabel_ultimatum", "Ultimatum"),
            "warning" => localize("IntentLabel_warning", "Condemnation"),
            "condemn" => localize("IntentLabel_condemn", "Public condemnation"),
            "apology" => localize("IntentLabel_apology", "Public apology"),
            "concession" => localize("IntentLabel_concession", "Diplomatic concession"),
            _ => localize("IntentVocabulary_OriginalTerm_032", "Diplomatic statement")
        };
    }

    public static bool ResolveValidatedResponseObligation(
        WorldDiplomacyDocument document, string intent, bool modelRequestedResponse, int maxAutomaticReplyDepth)
    {
        if (document == null || string.IsNullOrWhiteSpace(document.TargetKingdomId))
        {
            return false;
        }
        if ((!document.IsPlayerAuthored && document.AutomaticReplyDepth >= maxAutomaticReplyDepth) || IsTerminalResponseIntent(intent))
        {
            return false;
        }
        return IsProposalIntent(intent)
            || string.Equals(NormalizeIntent(intent), "warning", StringComparison.OrdinalIgnoreCase)
            || string.Equals(NormalizeIntent(intent), "ultimatum", StringComparison.OrdinalIgnoreCase)
            || modelRequestedResponse;
    }
	// Round topics are stored text written in either game language; match both language variants.
	// Built once (language-independent), so the hot path allocates nothing per call.
	private static readonly Lazy<string[][]> TopicCategoryNeedles = new Lazy<string[][]>(() => new[]
	{
		TopicNeedles("IntentVocabulary_OriginalTerm_033", "IntentVocabulary_OriginalTerm_034"),
		TopicNeedles("IntentVocabulary_OriginalTerm_035", "IntentVocabulary_OriginalTerm_036", "IntentVocabulary_OriginalTerm_037", "IntentLabel_ultimatum", "IntentLabel_warning", "IntentVocabulary_OriginalTerm_038", "IntentVocabulary_OriginalTerm_039", "IntentVocabulary_OriginalTerm_040"),
		TopicNeedles("IntentVocabulary_OriginalTerm_041", "IntentVocabulary_OriginalTerm_042", "IntentVocabulary_OriginalTerm_043", "IntentVocabulary_OriginalTerm_044", "IntentVocabulary_OriginalTerm_045", "IntentVocabulary_OriginalTerm_046", "IntentVocabulary_OriginalTerm_047"),
		TopicNeedles("IntentVocabulary_OriginalTerm_048", "IntentVocabulary_OriginalTerm_049")
	}, true);
	private static readonly string[] TopicCategories = { "peace_terms", "war_escalation", "trade_order", "alliance_duties" };

	private static string[] TopicNeedles(params string[] ids)
		=> ids.SelectMany(id => global::AnimusForge.DiplomacyCompatibilityTexts.Variants(id)).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

	// Latin needles must start at a word boundary so that e.g. "war" does not match "toward" and
	// "pact" does not match "impact"; Chinese needles keep plain substring matching.
	private static bool ContainsTopicTerm(string text, string needle)
	{
		int at = text.IndexOf(needle, StringComparison.OrdinalIgnoreCase);
		if (at < 0 || !IsLatinLetter(needle[0])) return at >= 0;
		while (at >= 0)
		{
			if (at == 0 || !IsLatinLetter(text[at - 1])) return true;
			at = text.IndexOf(needle, at + 1, StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

	private static bool IsLatinLetter(char c) => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');

	public static string InferTopicCategory(string topic, bool initiatorAndTargetAtWar)
	{
		string value = topic ?? "";
		string[][] needles = TopicCategoryNeedles.Value;
		for (int category = 0; category < needles.Length; category++)
		{
			foreach (string needle in needles[category])
			{
				if (ContainsTopicTerm(value, needle)) return TopicCategories[category];
			}
		}
		if (initiatorAndTargetAtWar) return "war_conduct";
		return "regional_security";
	}


    public static bool IsFormalTreatyIntent(string intent)
    {
        string value = NormalizeIntent(intent);
        return value is "propose_annexation" or "accept_annexation" or "reject_annexation"
            or "propose_tributary" or "accept_tributary" or "reject_tributary"
            or "propose_garrison" or "accept_garrison" or "reject_garrison"
            or "propose_vassal" or "accept_vassal" or "reject_vassal";
    }
}
