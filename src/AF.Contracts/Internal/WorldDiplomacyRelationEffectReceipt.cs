namespace AnimusForge.Refactor.Contracts;

internal readonly struct WorldDiplomacyRelationEffectReceipt
{
    internal readonly bool IsKnown;
    internal readonly int AppliedDelta;
    internal readonly string Diagnostic;
    internal WorldDiplomacyRelationEffectReceipt(bool known, int delta, string diagnostic = "")
    { IsKnown = known; AppliedDelta = known ? delta : 0; Diagnostic = diagnostic ?? ""; }
}
