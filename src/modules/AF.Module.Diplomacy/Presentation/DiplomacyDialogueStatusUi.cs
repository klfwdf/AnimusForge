using System;

namespace AnimusForge;

internal static class DiplomacyDialogueStatusUi
{
    private static readonly string[] Keys =
    {
        "OralDiplomacy_NotReady",
        "OralDiplomacy_DuplicatePrefix",

        "OralDiplomacy_SubmissionRejected",
        "OralDiplomacy_Uncommitted",
        "OralDiplomacy_InvalidFormat",
        "OralDiplomacy_Unauthorized",
        "OralDiplomacy_SubmitFailedPrefix",
        "OralDiplomacy_MultipleProposals",
        "OralDiplomacy_UnavailableProposal",
        "OralDiplomacy_Published",
        "OralDiplomacy_Executed",
        "OralDiplomacy_DeferredPrefix",
        "OralDiplomacy_CancelledPrefix",
        "DialogueControl_InvalidFormat",
        "DialogueControl_AmbiguousSourceReason",
        "DialogueControl_AlreadyUpdatedPrefix",
        "DialogueControl_CannotWithdraw",
        "DialogueControl_WithdrawIncomplete",
        "DialogueControl_InvalidStatus",
        "DialogueControl_HasFormalResult",
        "DialogueControl_HistoricalCommitmentPrefix",
        "DialogueControl_ReasonPrefix",
        "DialogueControl_UpdatedPrefix",
        "DialogueControl_WithdrawStandalone",
        "DialogueControl_WithdrawUnauthorized",
        "DialogueControl_WithdrawSuccess",
        "OralDiplomacy_ReleaseSubjectUnavailable",
        "OralDiplomacy_ReleaseSubjectInvalidFormat",
        "OralDiplomacy_ReleaseSubjectStateChanged",
        "OralDiplomacy_CommitmentDisabled",
    };

    internal static string Project(string message)
    {
        if (string.IsNullOrEmpty(message)) return message;
        foreach (string key in Keys)
        {
            string source = DiplomacyMiscPromptTexts.Get(key, "");
            if (string.IsNullOrEmpty(source)) continue;
            if (message == source) return Text(key);
            if (key.EndsWith("Prefix", StringComparison.Ordinal) && message.StartsWith(source, StringComparison.Ordinal))
            {
                string suffix = message.Substring(source.Length);
                if (key == "DialogueControl_UpdatedPrefix" || key == "DialogueControl_AlreadyUpdatedPrefix")
                    suffix = ProjectState(suffix);
                return Text(key) + suffix;
            }
            int variable = source.IndexOf("{0}", StringComparison.Ordinal);
            if (variable >= 0)
            {
                string prefix = source.Substring(0, variable), ending = source.Substring(variable + 3);
                if (message.StartsWith(prefix, StringComparison.Ordinal) && message.EndsWith(ending, StringComparison.Ordinal)
                    && message.Length >= prefix.Length + ending.Length)
                    return DiplomacyUiLocalization.Format("AF_DIP_STATUS_OralDiplomacy_SubmissionRejected",
                        "Diplomatic commitment not submitted: {REASON}.",
                        ("REASON", message.Substring(prefix.Length, message.Length - prefix.Length - ending.Length)));
            }
        }
        return message;
    }

    private static string ProjectState(string status)
    {
        // Only exact generated state tokens are projected. User reasons stay untouched.
        string token = status.EndsWith("。", StringComparison.Ordinal) ? status.Substring(0, status.Length - 1) : status;
        return token switch
        {
            "accepted" => DiplomacyUiLocalization.Text("AF_DIP_STATUS_State_accepted", "accepted; not yet published"),
            "deferred" => DiplomacyUiLocalization.Text("AF_DIP_STATUS_State_deferred", "publication deferred"),
            "cancelled" => DiplomacyUiLocalization.Text("AF_DIP_STATUS_State_cancelled", "cancelled; not executed"),
            "published" => DiplomacyUiLocalization.Text("AF_DIP_STATUS_State_published", "published; awaiting formal outcome"),
            "executed" => DiplomacyUiLocalization.Text("AF_DIP_STATUS_State_executed", "executed"),
            "withdrawn" => DiplomacyUiLocalization.Text("AF_DIP_STATUS_State_withdrawn", "proposal withdrawn"),
            "expired" => DiplomacyUiLocalization.Text("AF_DIP_STATUS_State_expired", "expired"),
            "rejected" => DiplomacyUiLocalization.Text("AF_DIP_STATUS_State_rejected", "rejected"),
            "superseded" => DiplomacyUiLocalization.Text("AF_DIP_STATUS_State_superseded", "superseded"),
            _ => status
        };
    }

    private static string Text(string key) => key switch
    {
        "OralDiplomacy_SubmissionRejected" => DiplomacyUiLocalization.Format("AF_DIP_STATUS_OralDiplomacy_SubmissionRejected", "Diplomatic commitment not submitted: {REASON}.", ("REASON", "{0}")),
        "OralDiplomacy_Uncommitted" => DiplomacyUiLocalization.Text("AF_DIP_STATUS_OralDiplomacy_Uncommitted", "Diplomatic commitment not submitted: world diplomacy system is not enabled."),
        "OralDiplomacy_InvalidFormat" => DiplomacyUiLocalization.Text("AF_DIP_STATUS_OralDiplomacy_InvalidFormat", "Diplomatic commitment not submitted: clause format is invalid."),
        "OralDiplomacy_Unauthorized" => DiplomacyUiLocalization.Text("AF_DIP_STATUS_OralDiplomacy_Unauthorized", "Diplomatic commitment not established: the other party does not have authority to represent the kingdom."),
        "OralDiplomacy_SubmitFailedPrefix" => DiplomacyUiLocalization.Text("AF_DIP_STATUS_OralDiplomacy_SubmitFailedPrefix", "Diplomatic commitment not submitted: "),
        "OralDiplomacy_MultipleProposals" => DiplomacyUiLocalization.Text("AF_DIP_STATUS_OralDiplomacy_MultipleProposals", "There are multiple related proposals; please specify which one to respond to first."),
        "OralDiplomacy_UnavailableProposal" => DiplomacyUiLocalization.Text("AF_DIP_STATUS_OralDiplomacy_UnavailableProposal", "The original proposal is currently not answerable; please verify the source first."),
        "OralDiplomacy_Published" => DiplomacyUiLocalization.Text("AF_DIP_STATUS_OralDiplomacy_Published", "Official declaration published; bilateral treaty requires formal response from counterparty."),
        "OralDiplomacy_Executed" => DiplomacyUiLocalization.Text("AF_DIP_STATUS_OralDiplomacy_Executed", "Official declaration published and diplomatic action executed."),
        "OralDiplomacy_DeferredPrefix" => DiplomacyUiLocalization.Text("AF_DIP_STATUS_OralDiplomacy_DeferredPrefix", "Commitment recorded; official documentation deferred due to: "),
        "OralDiplomacy_CancelledPrefix" => DiplomacyUiLocalization.Text("AF_DIP_STATUS_OralDiplomacy_CancelledPrefix", "Commitment recorded but official documentation cancelled due to situation change: "),
        "DialogueControl_InvalidFormat" => DiplomacyUiLocalization.Text("AF_DIP_STATUS_DialogueControl_InvalidFormat", "Commitment disposition not submitted: invalid format."),
        "DialogueControl_AmbiguousSourceReason" => DiplomacyUiLocalization.Text("AF_DIP_STATUS_DialogueControl_AmbiguousSourceReason", "Commitment disposition not submitted: source or reason unclear."),
        "DialogueControl_AlreadyUpdatedPrefix" => DiplomacyUiLocalization.Text("AF_DIP_STATUS_DialogueControl_AlreadyUpdatedPrefix", "Diplomatic commitment was previously updated: "),
        "DialogueControl_CannotWithdraw" => DiplomacyUiLocalization.Text("AF_DIP_STATUS_DialogueControl_CannotWithdraw", "The original proposal has taken effect, is closed, or cannot be withdrawn; completed diplomatic actions cannot be undone."),
        "DialogueControl_WithdrawIncomplete" => DiplomacyUiLocalization.Text("AF_DIP_STATUS_DialogueControl_WithdrawIncomplete", "Withdrawal declaration not completed; original proposal remains at its current state."),
        "DialogueControl_InvalidStatus" => DiplomacyUiLocalization.Text("AF_DIP_STATUS_DialogueControl_InvalidStatus", "Commitment disposition not submitted: invalid status."),
        "DialogueControl_HasFormalResult" => DiplomacyUiLocalization.Text("AF_DIP_STATUS_DialogueControl_HasFormalResult", "This commitment already has a formal outcome and cannot be rolled back through private discussion."),
        "DialogueControl_HistoricalCommitmentPrefix" => DiplomacyUiLocalization.Text("AF_DIP_STATUS_DialogueControl_HistoricalCommitmentPrefix", "Previous diplomatic commitment is currently "),
        "DialogueControl_ReasonPrefix" => DiplomacyUiLocalization.Text("AF_DIP_STATUS_DialogueControl_ReasonPrefix", ", reason: "),
        "DialogueControl_UpdatedPrefix" => DiplomacyUiLocalization.Text("AF_DIP_STATUS_DialogueControl_UpdatedPrefix", "Diplomatic commitment updated: "),
        "DialogueControl_WithdrawStandalone" => DiplomacyUiLocalization.Text("AF_DIP_STATUS_DialogueControl_WithdrawStandalone", "Withdrawal not executed: proposal withdrawal must be declared separately and cannot be mixed with other diplomatic actions."),
        "DialogueControl_WithdrawUnauthorized" => DiplomacyUiLocalization.Text("AF_DIP_STATUS_DialogueControl_WithdrawUnauthorized", "Withdrawal not executed: original proposal is closed or publisher lacks authority."),
        "DialogueControl_WithdrawSuccess" => DiplomacyUiLocalization.Text("AF_DIP_STATUS_DialogueControl_WithdrawSuccess", "Original diplomatic proposal withdrawn via formal declaration; effective diplomatic actions retain actual results."),
        "OralDiplomacy_NotReady" => DiplomacyUiLocalization.Text("AF_DIP_STATUS_OralDiplomacy_NotReady", "Diplomatic commitment not submitted: world diplomacy system is not ready."),
        "OralDiplomacy_DuplicatePrefix" => DiplomacyUiLocalization.Text("AF_DIP_STATUS_OralDiplomacy_DuplicatePrefix", "This commitment has already been submitted to formal diplomacy, document ID: "),
        "OralDiplomacy_ReleaseSubjectUnavailable" => DiplomacyUiLocalization.Text("AF_DIP_STATUS_OralDiplomacy_ReleaseSubjectUnavailable", "Subject release not executed: subject system is unavailable."),
        "OralDiplomacy_ReleaseSubjectInvalidFormat" => DiplomacyUiLocalization.Text("AF_DIP_STATUS_OralDiplomacy_ReleaseSubjectInvalidFormat", "Subject release not executed: invalid target or agreement token format."),
        "OralDiplomacy_ReleaseSubjectStateChanged" => DiplomacyUiLocalization.Text("AF_DIP_STATUS_OralDiplomacy_ReleaseSubjectStateChanged", "Subject release not executed: missing player directive, or ruler authority, target, or direct treaty has changed."),
        "OralDiplomacy_CommitmentDisabled" => DiplomacyUiLocalization.Text("AF_DIP_STATUS_OralDiplomacy_CommitmentDisabled", "AI diplomacy is disabled; existing commitment arrangements cannot be changed in the current mode."),
        _ => key
    };
}
