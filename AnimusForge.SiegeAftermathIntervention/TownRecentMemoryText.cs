using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AnimusForge.SiegeAftermathIntervention;

public static class TownRecentMemoryText
{
    public static readonly HashSet<string> ActionKeys = new HashSet<string>(new[] {
        "native.raid", "native.capture", "native.ShowMercy", "native.Pillage", "native.Devastate", "native.force_supplies", "native.force_volunteers",
        "village.ForcedRansom", "village.ForcedFood", "village.ForcedGoods", "village.ForcedLivestock",
        "village.Plunder", "village.Enslave", "village.ForcedRecruitment", "village.MassacreAndSeize", "village.Destroy",
        "town.mercy", "town.relief", "town.civilian_verbal_relief", "town.inspiration", "town.rally_oath",
        "town.plunder", "town.massacre", "town.cultural_repopulation", "town.constructive_culture_change", "town.stop_massacre"
    }, StringComparer.Ordinal);

    public static Dictionary<string, string> EnglishDefaults() => new Dictionary<string, string>(StringComparer.Ordinal) {
        ["header"] = "[Town events in the last seven days]",
        ["boundary"] = "Confirmed local facts only. Missing records do not prove nothing happened. These facts do not establish the speaker's personal involvement or feelings.",
        ["empty"] = "No confirmed event records are available.",
        ["unavailable"] = "Recent town records are unavailable; do not infer events or arrival totals.",
        ["date"] = "〔{year}, {season}, day {day}〕",
        ["town"] = "{date}, {actor} carried out {action} at {town}.",
        ["village"] = "{date}, {actor} carried out {action} at {village}, a village bound to {town}.",
        ["arrivals"] = "In the last seven days, {villagers} villager parties and {caravans} caravans arrived.",
        ["partial"] = "Arrival records cover only part of the last seven days: {villagers} villager-party arrivals and {caravans} caravan arrivals recorded; full-window totals are unknown.",
        ["season.0"] = "spring", ["season.1"] = "summer", ["season.2"] = "autumn", ["season.3"] = "winter",
        ["native.raid"] = "raiding", ["native.capture"] = "capture", ["native.ShowMercy"] = "mercy",
        ["native.Pillage"] = "pillaging", ["native.Devastate"] = "devastation",
        ["native.force_supplies"] = "forced supplies", ["native.force_volunteers"] = "forced recruitment",
        ["village.ForcedRansom"] = "forced ransom", ["village.ForcedFood"] = "forced food requisition",
        ["village.ForcedGoods"] = "forced goods requisition", ["village.ForcedLivestock"] = "forced livestock requisition",
        ["village.Plunder"] = "village plunder", ["village.Enslave"] = "enslavement",
        ["village.ForcedRecruitment"] = "forced recruitment", ["village.MassacreAndSeize"] = "village massacre",
        ["village.Destroy"] = "pure destruction", ["town.mercy"] = "mercy", ["town.relief"] = "relief",
        ["town.civilian_verbal_relief"] = "civilian reassurance", ["town.inspiration"] = "inspiration",
        ["town.rally_oath"] = "an oath rally", ["town.plunder"] = "plunder", ["town.massacre"] = "massacre",
        ["town.cultural_repopulation"] = "colonization", ["town.constructive_culture_change"] = "cultural adoption",
        ["town.stop_massacre"] = "cessation of massacre"
    };

    private static readonly Dictionary<string, string> Fallback = EnglishDefaults();
    private static readonly Regex Slots = new Regex(@"\{([a-z]+)\}", RegexOptions.Compiled);
    private static string Get(TownPromptTextCatalog catalog, string key)
        => catalog?.RecentMemoryTexts != null && catalog.RecentMemoryTexts.TryGetValue(key, out string text)
            && !string.IsNullOrWhiteSpace(text) ? text : Fallback[key];
    private static string Fill(string template, Dictionary<string, string> values)
        => Slots.Replace(template, m => values.TryGetValue(m.Groups[1].Value, out string value) ? value : m.Value);

    public static string FormatFact(TownRecentMemoryFact fact, TownPromptTextCatalog catalog)
    {
        var values = new Dictionary<string, string> {
            ["year"] = fact.Year.ToString(CultureInfo.InvariantCulture),
            ["season"] = Get(catalog, "season." + fact.Season),
            ["day"] = fact.DayOfSeason.ToString(CultureInfo.InvariantCulture)
        };
        string date = Fill(Get(catalog, "date"), values);
        string townName = fact.TownName;
        if (catalog?.Language == "zh-CN" && !townName.EndsWith("城", StringComparison.Ordinal)) townName += "城";
        return Fill(Get(catalog, fact.VillageName.Length == 0 ? "town" : "village"), new Dictionary<string, string> {
            ["date"] = date, ["actor"] = fact.ActorName, ["town"] = townName,
            ["village"] = fact.VillageName, ["action"] = Get(catalog, fact.Action)
        });
    }

    public static string Build(TownRecentMemoryTown town, bool complete, TownPromptTextCatalog catalog)
    {
        var text = new StringBuilder().AppendLine(Get(catalog, "header")).AppendLine(Get(catalog, "boundary"));
        foreach (var fact in town.Facts.OrderBy(f => f.Day)) text.AppendLine(FormatFact(fact, catalog));
        if (town.Facts.Count == 0) text.AppendLine(Get(catalog, "empty"));
        text.Append(Fill(Get(catalog, complete ? "arrivals" : "partial"), new Dictionary<string, string> {
            ["villagers"] = town.Arrivals.Sum(b => (long)b.Villagers).ToString(CultureInfo.InvariantCulture),
            ["caravans"] = town.Arrivals.Sum(b => (long)b.Caravans).ToString(CultureInfo.InvariantCulture)
        }));
        return text.ToString();
    }

    public static string Unavailable(TownPromptTextCatalog catalog) => Get(catalog, "unavailable");
}
