namespace AnimusForge.Refactor.Contracts;

// Transient receipt: only observed terms may be described as fulfilled.
internal readonly struct DiplomacyPeaceEffectReceipt
{
    internal readonly bool PeaceKnown, PeaceApplied, TermsKnown, TermsMatch;
    internal readonly int ActualDailyTribute, ActualDurationDays;
    internal readonly string Diagnostic;
    internal DiplomacyPeaceEffectReceipt(bool peaceKnown, bool peaceApplied, bool termsKnown,
        int actualTribute, int actualDays, bool termsMatch, string diagnostic)
    {
        PeaceKnown = peaceKnown; PeaceApplied = peaceKnown && peaceApplied;
        TermsKnown = termsKnown; ActualDailyTribute = actualTribute; ActualDurationDays = actualDays;
        TermsMatch = termsKnown && termsMatch; Diagnostic = diagnostic ?? "";
    }
    internal bool Complete => PeaceApplied && TermsMatch;
}
