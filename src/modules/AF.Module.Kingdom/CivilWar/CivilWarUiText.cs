using System;
using AnimusForge.Refactor.Modules;
using TaleWorlds.Localization;

namespace AnimusForge;

/// <summary>Player-facing Civil War panel text. IDs are stable; state and rule values remain owned by the Civil War owner.</summary>
internal static class CivilWarUiText
{
    internal static string T(string id, string fallback)
        => new TextObject("{" + "=" + id + "}" + fallback).ToString();

    internal static string T(string id, string fallback, params (string Name, string Value)[] variables)
    {
        TextObject text = new TextObject("{" + "=" + id + "}" + fallback);
        foreach ((string name, string value) in variables) text.SetTextVariable(name, value ?? "");
        return text.ToString();
    }

    internal static string DemandTag(CivilWarDemandDef demand)
    {
        string id = demand?.Id ?? "unknown";
        string fallback = id switch
        {
            "make_peace" => "Anti-war faction",
            "continue_war" => "War faction",
            "revoke_policy" => "Reform faction",
            "redress" => "Retribution faction",
            "autonomy" => "Separation faction",
            CivilWarCatalog.UsurpDemandId => "Claimant faction",
            _ => "Opposition faction"
        };
        return T("AF_KINGDOM_CW_FactionTag_" + id, fallback);
    }

    internal static string DemandText(CivilWarDemandDef demand, string target)
    {
        string id = demand?.Id ?? "unknown";
        string fallback = id switch
        {
            "make_peace" => "End the war with {TARGET} and negotiate peace",
            "continue_war" => "Do not make peace with {TARGET}",
            "revoke_policy" => "Repeal {TARGET}",
            "redress" => "Pay compensation and hold those responsible to account",
            "autonomy" => "Recognize autonomy for this faction's fiefs",
            CivilWarCatalog.UsurpDemandId => "The king abdicates and the faction leader succeeds",
            _ => "Pursue the faction's demand"
        };
        return T("AF_KINGDOM_CW_Demand_" + id, fallback, ("TARGET", string.IsNullOrWhiteSpace(target) ? T("AF_KINGDOM_CW_Target", "the target") : target));
    }

    internal static string SourceName(string sourceId, string fallback = "")
    {
        string fb = !string.IsNullOrEmpty(fallback) ? fallback : (sourceId switch
        {
            "royal_execution" => "Royal execution",
            "fief_denied" => "Fief denied",
            "policy_imposed" => "Policy imposed",
            "war_imposed" => "War imposed",
            "peace_imposed" => "Peace imposed",
            "lands_raided" => "Lands raided",
            "fief_lost" => "Fief lost",
            "broken_pledge" => "Royal pledge broken",
            "demand_refused" => "Demand refused",
            "royal_suppression" => "Royal suppression",
            _ => "Grievance"
        });
        fb = fb switch
        {
            "王室处决" => "Royal execution",
            "封地落选" => "Fief denied",
            "政策强推" => "Policy imposed",
            "强行开战" => "War imposed",
            "强行议和" => "Peace imposed",
            "领地遭劫" => "Lands raided",
            "失守封地" => "Fief lost",
            "国王背诺" => "Royal pledge broken",
            "诉求被拒" => "Demand refused",
            "王室打压" => "Royal suppression",
            _ => fb
        };
        return T("AF_KINGDOM_CW_Source_" + (sourceId ?? "unknown"), fb);
    }

    internal static string Role(CivilWarPanelRole role, string factionTag)
        => role switch
        {
            CivilWarPanelRole.King => T("AF_KINGDOM_CW_Role_King", "King"),
            CivilWarPanelRole.Crown => T("AF_KINGDOM_CW_Role_Crown", "Crown faction"),
            CivilWarPanelRole.Member => T("AF_KINGDOM_CW_Role_Member", "{TAG} member", ("TAG", factionTag ?? "Faction")),
            CivilWarPanelRole.Leader => T("AF_KINGDOM_CW_Role_Leader", "{TAG} leader", ("TAG", factionTag ?? "Faction")),
            _ => T("AF_KINGDOM_CW_Role_Middle", "Uncommitted")
        };

    internal static string Stance(string id, string fallback)
        => T("AF_KINGDOM_CW_Stance_" + (id ?? "unknown"), fallback);

    internal static string StabilityTier(int value)
        => value switch
        {
            0 => T("AF_KINGDOM_CW_Stability_ExtremelyHigh", "Extremely high"),
            1 => T("AF_KINGDOM_CW_Stability_High", "High"),
            2 => T("AF_KINGDOM_CW_Stability_FairlyHigh", "Fairly high"),
            3 => T("AF_KINGDOM_CW_Stability_Average", "Average"),
            4 => T("AF_KINGDOM_CW_Stability_Poor", "Poor"),
            5 => T("AF_KINGDOM_CW_Stability_VeryPoor", "Very poor"),
            _ => T("AF_KINGDOM_CW_Stability_ExtremelyPoor", "Extremely poor")
        };
}
