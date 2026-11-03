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
        string cn = string.Format(DiplomacyReplyTextRules.SponsorReceiptChinesePattern, p, n);
        string en = string.Format(DiplomacyReplyTextRules.SponsorReceiptEnglishPattern, p, n);
        var match = Regex.Match(text.Trim(), cn, RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(30));
        if (!match.Success) match = Regex.Match(text.Trim(), en, RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(30));
        if (!match.Success || !int.TryParse(match.Groups["gold"].Value, out int gold)) return null;
        return new DiplomacySponsorReceipt { Id = "afef_gold_" + WorldDiplomacyPromptContractRules.StablePromptHash(rulerId + "|" + day + "|" + text.Trim()),
            Ruler = rulerId, Gold = gold, Day = day };
    }
}
