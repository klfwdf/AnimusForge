using System;
using System.Text.RegularExpressions;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

internal static class DiplomacySponsorReceiptRules
{
    // Exact compatibility projections of game-owned AFEF receipts, not free
    // text money extraction. Unknown/mixed/partial facts remain unproved.
    internal static DiplomacySponsorReceipt Read(string kind, string text, string playerName,
        string rulerName, string rulerId, int day)
    {
        if (kind != "afef_player" || string.IsNullOrEmpty(text) || text.Length > 3000
            || string.IsNullOrEmpty(playerName) || string.IsNullOrEmpty(rulerName)) return null;
        string p = Regex.Escape(playerName), n = Regex.Escape(rulerName);
        string cn = "^" + p + @"已经将 (?<gold>[1-9][0-9]*) 第纳尔（合计总值约 \k<gold> 第纳尔） 交给 " + n
            + "。如果你正在和" + p + "交易，并核对好了数量和价值，那么你现在可以将商量好的物品，第纳尔或人交给" + p
            + @"（本次交易动作记录#[0-9]+）$";
        string en = "^" + p + @" has given (?<gold>[1-9][0-9]*) denars \(total value approx\. \k<gold> denars\) to " + n
            + @"\. If you are trading with " + p + " and have verified the quantity and value, you may now deliver the agreed items, denars, or people to " + p
            + @" \(trade action record #[0-9]+\)$";
        var match = Regex.Match(text.Trim(), cn, RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(30));
        if (!match.Success) match = Regex.Match(text.Trim(), en, RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(30));
        if (!match.Success || !int.TryParse(match.Groups["gold"].Value, out int gold)) return null;
        return new DiplomacySponsorReceipt { Id = "afef_gold_" + WorldDiplomacyPromptContractRules.StablePromptHash(rulerId + "|" + day + "|" + text.Trim()),
            Ruler = rulerId, Gold = gold, Day = day };
    }
}
