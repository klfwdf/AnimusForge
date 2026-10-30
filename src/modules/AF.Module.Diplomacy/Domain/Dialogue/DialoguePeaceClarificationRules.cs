using System.Globalization;

namespace AnimusForge.DiplomacyDialogue;

/// <summary>Shared clarification and failure wording; never supplies unagreed treaty terms.</summary>
public static class DialoguePeaceClarificationRules
{
    public static string MainReplyInstruction => global::AnimusForge.DiplomacyPromptTexts.Get("AF_DIP_PeaceMainIntegrity", "[Completeness of orally negotiated peace terms] If peace between kingdoms involves daily tribute, explicitly agree on the payer kingdom, recipient kingdom, daily amount, and number of payment days (1 to 252 days). \"Daily payment\" specifies only frequency, and \"permanent peace\" does not establish the tribute period. If any term is missing, ask about it directly in this turn, for example, \"The daily amount is clear; for how many consecutive days will it be paid?\" Do not keep promising that a declaration has already been published or a truce is already in effect, or invent explanations involving returning home, seals, captivity, or release. Do not fill in 100 days or any other duration yourself. Acceptance of an existing formal proposal inherits its complete original terms; do not ask again for conditions already explicit in that proposal. When terms are complete and you genuinely agree, explicitly commit to submitting a formal peace proposal on those exact terms. Only an execution receipt supplied by the game proves it has taken effect. A player's demand for immediate effect does not replace the other party's formal acceptance. Do not describe drafting, signing, or oral agreement as confirmed execution. This requirement applies only to treaties between kingdoms and does not change existing unconditional peace rules for independent clans.");

    public static string PostprocessInstruction => global::AnimusForge.DiplomacyPromptTexts.Get("AF_DIP_PeacePostprocessIntegrity", "[Oral peace completeness check] For a new Peace proposal with tribute greater than 0, payer, receiver, tribute, and days are all required. days may only use the 1 to 252 day period explicitly agreed in the current conversation. If the period is missing, do not invent 100 days or 0. In the lobbyist COMMIT scope only, an explicitly committed but incomplete matter may emit the known fields with days omitted for pending clarification; this does not submit a peace proposal. Outside that scope do not output an incomplete COMMIT. Preserve terms established earlier and not changed in this turn; do not drop them because latest_reply did not repeat them. Peace without tribute has days=0. AcceptProposal inherits the original terms from its source and does not rewrite duration. If latest_reply explicitly agrees to submit a formal proposal on complete terms, output the corresponding COMMIT; lack of an execution receipt does not prevent submission. An NPC's claim that it is already effective does not replace condition checks.");

    public static string DescribeValidationFailure(string reason, DialogueDiplomaticTerms terms)
    {
        if (reason == "peace_duration_requires_explicit_tribute_period")
            return terms != null && terms.DailyTribute > 0
                ? global::AnimusForge.DiplomacyPromptTexts.Get("AF_DIP_PeaceTributeRecognized", "Recognized daily tribute of ") + terms.DailyTribute.ToString(CultureInfo.InvariantCulture)
                    + global::AnimusForge.DiplomacyPromptTexts.Get("AF_DIP_PeaceTributeMissingPeriod", " denars, but the submitted terms lack a payment period. Explicitly agree with the other party on the number of consecutive payment days (1 to 252). If already agreed earlier, have the other party restate the amount and period completely before resubmitting. No peace proposal has been submitted yet, and the war state has not changed as a result")
                : global::AnimusForge.DiplomacyPromptTexts.Get("AF_DIP_PeaceNoTributePeriod", "Peace without tribute cannot specify a fixed tribute period. Confirm whether tribute is required and restate the terms; no peace proposal has been submitted yet");
        if (reason == "duration_out_of_range")
            return global::AnimusForge.DiplomacyPromptTexts.Get("AF_DIP_PeacePeriodOutOfRange", "The term is outside the supported range. Specify a period of 1 to 252 days; peace without tribute has no tribute period");
        return reason ?? global::AnimusForge.DiplomacyPromptTexts.Get("AF_DIP_PeaceValidationFailed", "Terms validation failed");
    }
}
