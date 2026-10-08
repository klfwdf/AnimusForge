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
    {
		return NormalizeIntent(intent) switch
		{
			"withdraw_offer" => "撤回原提案",
			"propose_annexation" => "提议王国并入",
			"accept_annexation" => "接受王国并入",
			"reject_annexation" => "拒绝王国并入",
			"propose_tributary" => "提议朝贡条约",
			"accept_tributary" => "接受朝贡条约",
			"reject_tributary" => "拒绝朝贡条约",
			"propose_garrison" => "提议驻军条约",
			"accept_garrison" => "接受驻军条约",
			"reject_garrison" => "拒绝驻军条约",
			"propose_vassal" => "提议完全臣属条约",
			"accept_vassal" => "接受完全臣属条约",
			"reject_vassal" => "拒绝完全臣属条约",
			"declare_war" => "正式宣战",
			"propose_peace" => "和平提议",
			"accept_peace" => "接受和平",
			"reject_peace" => "拒绝和平",
			"propose_alliance" => "结盟提议",
			"accept_alliance" => "接受结盟",
			"reject_alliance" => "拒绝结盟",
			"break_alliance" => "解除同盟",
			"propose_trade" => "贸易提议",
			"accept_trade" => "接受贸易",
			"reject_trade" => "拒绝贸易",
			"cancel_trade" => "终止贸易",
            "release_subject" => "宗主释放臣属国",
			"comply_ultimatum" => "服从最后通牒",
			"ultimatum" => "最后通牒",
			"warning" => "谴责",
			"condemn" => "公开谴责",
			"apology" => "公开致歉",
			"concession" => "外交让步",
			_ => "外交声明"
		};
	}

    public static bool ResolveValidatedResponseObligation(
        WorldDiplomacyDocument document, string intent, bool modelRequestedResponse, int maxAutomaticReplyDepth)
    {
        if (document == null || string.IsNullOrWhiteSpace(document.TargetKingdomId))
        {
            return false;
        }
        if (document.AutomaticReplyDepth >= maxAutomaticReplyDepth || IsTerminalResponseIntent(intent))
        {
            return false;
        }
        return IsProposalIntent(intent)
            || string.Equals(NormalizeIntent(intent), "warning", StringComparison.OrdinalIgnoreCase)
            || string.Equals(NormalizeIntent(intent), "ultimatum", StringComparison.OrdinalIgnoreCase)
            || modelRequestedResponse;
    }
	public static string InferTopicCategory(string topic, bool initiatorAndTargetAtWar)
	{
		string value = topic ?? "";
		if (value.IndexOf("议和", StringComparison.OrdinalIgnoreCase) >= 0 || value.IndexOf("停战", StringComparison.OrdinalIgnoreCase) >= 0) return "peace_terms";
		if (ContainsAny(value, "战争", "宣战", "开战", "最后通牒", "谴责", "军事警告", "战争警告", "动武")) return "war_escalation";
		if (ContainsAny(value, "贸易", "通商", "商路", "商贸", "互市", "关税", "商队")) return "trade_order";
		if (value.IndexOf("同盟", StringComparison.OrdinalIgnoreCase) >= 0 || value.IndexOf("盟约", StringComparison.OrdinalIgnoreCase) >= 0) return "alliance_duties";
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
