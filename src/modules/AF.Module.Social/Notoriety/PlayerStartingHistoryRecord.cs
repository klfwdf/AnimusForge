using System;
using System.Text.RegularExpressions;

namespace AnimusForge;

// A bounded canonical opening fact. Natural-language projection happens only
// at UI/request boundaries; player-authored history remains untouched.
internal static class PlayerStartingHistoryRecord
{
    private const string Prefix = "[[AF_PLAYER_START_V1:";
    private static readonly Regex Token = new Regex(@"^\[\[AF_PLAYER_START_V1:(?<id>[a-z]{1,32})\]\](?=\r?$)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline);

    internal static bool TrySeed(string typeId, bool alreadyRecorded, string existingHistory, out string history)
    {
        history = existingHistory ?? "";
        if (alreadyRecorded) return false;
        string id = (typeId ?? "").Trim().ToLowerInvariant();
        if (EnglishUiTemplate(id) == null) return false;
        foreach (Match existing in Token.Matches(history))
            if (EnglishUiTemplate(existing.Groups["id"].Value) != null) return true;
        string token = Prefix + id + "]]";
        history = token + (history.Length == 0 ? "" : "\n" + history);
        return true;
    }

    internal static string Render(string raw, string playerName, Func<string, string, string> render)
    {
        string text = raw ?? "";
        if (text.IndexOf(Prefix, StringComparison.Ordinal) < 0) return text;
        return Token.Replace(text, match => EnglishUiTemplate(match.Groups["id"].Value) == null
            ? match.Value : render(match.Groups["id"].Value, playerName));
    }

    internal static string EnglishUiTemplate(string id)
    {
        switch (id)
        {
            case "default": return "{PLAYER} left home to make a way in Calradia under the name of an independent clan. A secure foothold, wealth, and a reputation for the family were still to be built through the journey ahead.";
            case "king": return "{PLAYER} once ruled a kingdom, commanding regular soldiers and elite warriors with companions at hand and money and provisions to sustain the company. Their responsibilities encompassed their clan and followers as well as affairs of state and wars between kingdoms.";
            case "vassal": return "{PLAYER} once served as a vassal of a kingdom, bringing companions, regular soldiers, and elite warriors into its service, with funds and supplies to maintain the company. This placed them within the feudal ties linking lords, clans, and the crown.";
            case "mercenary": return "{PLAYER} once led a companion and a company of soldiers drawn from different troop traditions into paid service for a kingdom. They had funds and provisions for the march; their service rested on a mercenary contract, tying their livelihood to warfare and military pay.";
            case "trader": return "{PLAYER} was a merchant combining workshop ownership with long-distance trade. They ran a workshop in town and entrusted a companion with a trading caravan, while keeping companions, guards, pack animals, goods for sale, and working capital. Their livelihood linked urban production with commerce along the trade roads.";
            case "outlaw": return "{PLAYER} once operated near a town with one companion and a small band of bandits, carrying money, limited provisions, and goods for sale while already bearing criminal charges across the land. Life outside the law exposed them to the authorities' hostility and the risk of pursuit.";
            case "beggar": return "{PLAYER} once lived on the streets of a town, penniless and dressed in worn clothing, struggling to meet daily needs. They had very few material resources to rely on and no wealth or enterprise sufficient to sustain a larger undertaking.";
            case "fleetadmiral": return "{PLAYER} once commanded a fleet in the service of a kingdom, leading both land soldiers and mariners with two companions to assist them, several ships, and provisions for the company. This background linked seafaring with the military affairs of the kingdom.";
            case "merchantventurer": return "{PLAYER} was a merchant combining port enterprise with overseas trade. They ran a workshop in a port town and entrusted a companion with a maritime caravan, while also sailing aboard merchant vessels with guards and trade goods. Ships, cargo, and working capital sustained their seaborne commerce.";
            default: return null;
        }
    }
}
