using AnimusForge.PolicyEffects;

namespace AnimusForge;

/// <summary>
/// Player UI projection for policy effect names and catalog summaries.
/// Runtime descriptors remain the source of prompts, diagnostics, persistence, and raw facts.
/// </summary>
internal static class PolicyEffectUiText
{
    internal static string Name(IPolicyEffectModule module)
    {
        return Name(module?.Id, module?.Descriptor?.PlayerDisplayName ?? module?.Id);
    }

    internal static string Name(string moduleId, string fallback)
    {
        return Resolve(moduleId, fallback, "Name", "Module effect");
    }

    internal static string Summary(IPolicyEffectModule module)
    {
        return Summary(module?.Id, (module?.Descriptor?.CatalogSummary ?? module?.RetrievalText ?? string.Empty).Trim());
    }

    internal static string Summary(string moduleId, string fallback)
    {
        return Resolve(moduleId, fallback, "Summary", "Policy effect module");
    }

    private static string Resolve(string moduleId, string fallback, string suffix, string defaultFallback)
    {
        string key = GetKey(moduleId);
        if (string.IsNullOrWhiteSpace(key))
        {
            return string.IsNullOrWhiteSpace(fallback) ? defaultFallback : fallback;
        }
        return PolicyUiTexts.Get("AFPOL_UI_Effect_" + key + suffix, EnglishFallback(key, suffix == "Summary"));
    }

    private static string EnglishFallback(string key, bool summary)
    {
        switch (key)
        {
            case "ClanInfluence": return summary ? "Clan influence: one-time and daily changes" : "Clan influence";
            case "HeroGold": return summary ? "Character and native-role personal denar changes" : "Character denars";
            case "KingdomStability": return summary ? "One-time kingdom stability change on the next game day" : "Kingdom stability";
            case "PartySizeLimit": return summary ? "Adjusts party size limits for rulers, clans, or all lords" : "Lords' party size limit";
            case "SoldierTroopXp": return summary ? "One-time or daily experience for clan lord parties and garrison soldiers" : "Soldier elite training";
            case "ClanLeaderRelation": return summary ? "One-time relation change between the publisher and other clan leaders" : "Clan leader relation";
            case "ProsperityPerDay": return summary ? "Daily prosperity change for towns" : "Town prosperity change";
            case "VolunteerProductionGrowth": return summary ? "Relative change to settlement volunteer checks and native growth attempts" : "Volunteer replenishment and growth";
            case "ArmyFormationTendency": return summary ? "Relative change to the army formation candidate score of eligible lords in target clans" : "Army formation tendency";
            case "VillageProduction": return summary ? "Relative change to native goods production in administrative attached villages" : "Village goods production";
            case "MilitiaPerDay": return summary ? "Daily militia change for settlements" : "Militia";
            case "FoodPerDay": return summary ? "Daily town food stock change" : "Food reserves";
            case "LoyaltyPerDay": return summary ? "Daily town loyalty change" : "Town loyalty";
            case "TaxIncome": return summary ? "Tax percentage change for the clans owning towns or castles" : "Town and castle taxes";
            case "KingdomVillageRaidBan": return summary ? "Prevents lords of the publishing kingdom from raiding villages voluntarily" : "Kingdom village raid ban";
            case "SecurityPerDay": return summary ? "Daily town security change" : "Town security";
            case "HearthPerDay": return summary ? "Daily village hearth change" : "Village hearths";
            case "ConstructionPerDay": return summary ? "Daily construction points for towns or castles" : "Construction power";
            default: return "Module effect";
        }
    }

    private static string GetKey(string moduleId)
    {
        switch ((moduleId ?? string.Empty).Trim())
        {
            case "clanInfluence":
            case "clanInfluenceNextDayOnce":
            case "clanInfluencePerDay": return "ClanInfluence";
            case "heroGold":
            case "heroGoldNextDayOnce":
            case "heroGoldPerDay": return "HeroGold";
            case "kingdomStability":
            case "kingdomStabilityOnce":
            case "kingdomStabilityNextDayOnce": return "KingdomStability";
            case "partySizeLimit":
            case "partySizeLimitClanLeader":
            case "partySizeLimitClanLords": return "PartySizeLimit";
            case "soldierTroopXp":
            case "soldierTroopXpOnce":
            case "soldierTroopXpPerDay": return "SoldierTroopXp";
            case "clanLeaderRelationOnce": return "ClanLeaderRelation";
            case "prosperityPerDay": return "ProsperityPerDay";
            case "volunteerProductionGrowthPct": return "VolunteerProductionGrowth";
            case "armyFormationTendencyPct": return "ArmyFormationTendency";
            case "villageProductionPct": return "VillageProduction";
            case "militiaPerDay": return "MilitiaPerDay";
            case "foodPerDay": return "FoodPerDay";
            case "loyaltyPerDay": return "LoyaltyPerDay";
            case "taxIncomePct": return "TaxIncome";
            case "kingdomVillageRaidBan": return "KingdomVillageRaidBan";
            case "securityPerDay": return "SecurityPerDay";
            case "hearthPerDay": return "HearthPerDay";
            case "constructionPerDay": return "ConstructionPerDay";
            default: return string.Empty;
        }
    }
}
