using System;

namespace AnimusForge.Refactor.Adapters;

// An action is attempted once. Observer failures never substitute for a read of
// the resulting game state, and a failed read must not mean "false".
internal readonly struct DiplomacyEffectReadback
{
    internal readonly bool IsKnown;
    internal readonly bool Applied;
    internal readonly string Diagnostic;
    internal DiplomacyEffectReadback(bool known, bool applied, string diagnostic)
    { IsKnown = known; Applied = known && applied; Diagnostic = diagnostic ?? ""; }

    internal static DiplomacyEffectReadback Execute(Action action, Func<bool> confirm)
    {
        string diagnostic = "";
        try { action(); }
        catch (Exception ex) { diagnostic = ex.Message; }
        try { return new DiplomacyEffectReadback(true, confirm(), diagnostic); }
        catch (Exception ex) { return new DiplomacyEffectReadback(false, false, diagnostic + " | readback: " + ex.Message); }
    }
}
