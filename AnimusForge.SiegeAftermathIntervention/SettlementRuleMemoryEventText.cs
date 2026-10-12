using System;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>Only describes committed lifecycle events/counters; never turns a command into its outcome.</summary>
public static class SettlementRuleMemoryEventText
{
    public static string Aftermath(string actor, string outcome)
    {
        return Aftermath(actor, outcome, isEnglish: false);
    }

    public static string Aftermath(string actor, string outcome, bool isEnglish)
    {
        string name = (actor != null ? actor : (isEnglish ? "unknown commander" : SiegeAftermathCompatibilityTexts.Raw("aftermath_unknown_commander")));
        if (isEnglish)
        {
            string action = outcome == "Pillage" ? "pillage" : outcome == "Devastate" ? "devastation" : "mercy";
            return "Commander " + name + "\u0027s post-siege disposition at this town was settled as " + action + ".";
        }
        string actionCn = outcome == "Pillage" ? SiegeAftermathCompatibilityTexts.Raw("action_pillage") : outcome == "Devastate" ? SiegeAftermathCompatibilityTexts.Raw("action_devastate") : SiegeAftermathCompatibilityTexts.Raw("action_mercy");
        return SiegeAftermathCompatibilityTexts.Raw("aftermath_template").Replace("{actor}", name).Replace("{outcome}", actionCn);
    }

    public static string CompletedIntervention(string actor, string outcome, long lootValue, long gold,
        int civilianDeaths, int notableDeaths, int reliefGold, int reliefFood)
    {
        return CompletedIntervention(actor, outcome, lootValue, gold, civilianDeaths, notableDeaths, reliefGold, reliefFood, isEnglish: false);
    }

    public static string CompletedIntervention(string actor, string outcome, long lootValue, long gold,
        int civilianDeaths, int notableDeaths, int reliefGold, int reliefFood, bool isEnglish)
    {
        if (isEnglish)
        {
            return Aftermath(actor, outcome, true) + " Settled loot valuation: " + Math.Max(0, lootValue)
                + "; denars taken: " + Math.Max(0, gold) + "; civilian deaths: " + Math.Max(0, civilianDeaths)
                + "; notable deaths: " + Math.Max(0, notableDeaths) + "; relief delivered: " + Math.Max(0, reliefGold)
                + " denars and " + Math.Max(0, reliefFood) + " food. This records the actual settlement and does not claim that later promises were fulfilled.";
        }
        return SiegeAftermathCompatibilityTexts.Raw("completed_template").Replace("{aftermath}", Aftermath(actor, outcome, false)).Replace("{loot}", Math.Max(0, lootValue).ToString()).Replace("{gold}", Math.Max(0, gold).ToString()).Replace("{civilians}", Math.Max(0, civilianDeaths).ToString()).Replace("{notables}", Math.Max(0, notableDeaths).ToString()).Replace("{relief_gold}", Math.Max(0, reliefGold).ToString()).Replace("{relief_food}", Math.Max(0, reliefFood).ToString());
    }

    public static string Policy(string eventKind, string title)
    {
        return Policy(eventKind, title, isEnglish: false);
    }

    public static string Policy(string eventKind, string title, bool isEnglish)
    {
        if (isEnglish)
        {
            string status;
            switch (eventKind)
            {
                case "published": status = "published"; break;
                case "renewed": status = "renewed"; break;
                case "abolished": status = "abolished"; break;
                case "expired": status = "expired"; break;
                default: return string.Empty;
            }
            return "Local policy \u0022" + title + "\u0022 was " + status + " in this town. This only confirms the policy state; expected economic or public-order effects are not implied.";
        }
        string statusCn;
        switch (eventKind)
        {
            case "published": statusCn = SiegeAftermathCompatibilityTexts.Raw("policy_published"); break;
            case "renewed": statusCn = SiegeAftermathCompatibilityTexts.Raw("policy_renewed"); break;
            case "abolished": statusCn = SiegeAftermathCompatibilityTexts.Raw("policy_abolished"); break;
            case "expired": statusCn = SiegeAftermathCompatibilityTexts.Raw("policy_expired"); break;
            default: return string.Empty;
        }
        return SiegeAftermathCompatibilityTexts.Raw("policy_template").Replace("{title}", title).Replace("{status}", statusCn);
    }
}
