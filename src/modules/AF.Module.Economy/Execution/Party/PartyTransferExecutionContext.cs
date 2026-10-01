using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Entry = AnimusForge.MyBehavior.PartyTransferPromptEntry;

namespace AnimusForge;

// The four lists are the exact authorization snapshots used by prompt indexing.
// Handles inside entries are opaque to the executor and accessed only by main-thread ports.
internal sealed class PartyTransferExecutionContext
{
    internal bool Eligible;
    internal bool HasTarget;
    internal string DisplayName;
    internal bool HasTroops;
    internal bool HasPrisoners;
    internal bool HasAllTroops;
    internal bool HasAllPrisoners;
    internal List<Entry> Troops;
    internal List<Entry> Prisoners;
    internal List<Entry> AllTroops;
    internal List<Entry> AllPrisoners;
    internal Func<Entry, int, PartyTransferEffectResult> TransferTroop;
    internal Func<Entry, int, PartyTransferEffectResult> TransferPrisoner;
    internal Func<Entry, bool> IsVolunteer;
}

internal readonly struct PartyTransferEffectResult
{
    internal readonly int Delivered;
    internal readonly int Debited;
    internal readonly bool Failed;
    internal readonly bool Indeterminate;
    internal bool HasEffects => Delivered > 0 || Debited > 0 || Indeterminate;
    internal bool IsPartial => Failed || Debited != Delivered;

    internal PartyTransferEffectResult(int delivered, int debited, bool failed, bool indeterminate = false)
    {
        Delivered = Math.Max(0, delivered);
        Debited = Math.Max(0, debited);
        Failed = failed;
        Indeterminate = indeterminate;
    }
}

internal static class PartyTransferTagCodec
{
    internal static readonly Regex TroopRegex = new Regex("\\[ATT:(ALL|\\d+):(ALL|\\d+)\\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    internal static readonly Regex PrisonerRegex = new Regex("\\[ATP:(ALL|\\d+):(ALL|\\d+)\\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    internal static string Strip(string text)
    {
        string result = Regex.Replace(text ?? "", "\\[ATT:[^\\]\\r\\n]*\\]", "", RegexOptions.IgnoreCase);
        return Regex.Replace(result, "\\[ATP:[^\\]\\r\\n]*\\]", "", RegexOptions.IgnoreCase).Trim();
    }
}
