using System;
using AnimusForge.Refactor.Contracts;

namespace AnimusForge.Refactor.Adapters;

internal static class DiplomacyRelationEffect
{
    internal static WorldDiplomacyRelationEffectReceipt Apply(Func<int> read, Action action)
    {
        int before;
        try { before = read(); }
        catch (Exception ex) { return new(true, 0, "before read: " + ex.Message); }
        string diagnostic = "";
        try { action(); }
        catch (Exception ex) { diagnostic = ex.Message; }
        try { return new(true, read() - before, diagnostic); }
        catch (Exception ex) { return new(false, 0, diagnostic + " | after read: " + ex.Message); }
    }
}
