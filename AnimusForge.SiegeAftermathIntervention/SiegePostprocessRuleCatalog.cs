using System.Collections.Generic;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Fallback postprocess rules for the active GCCZ intervention scene.
/// These mirror the passive ModuleData rule and keep rule wording out of the AF adapter.
/// </summary>
public static class SiegePostprocessRuleCatalog
{
    public const string RuleId = "siege_intervention_aftermath";

    public const string InjectedRuleBlockMarker = "\u3010\u9644\u52a0\u89c4\u5219:siege_intervention_aftermath\u3011";

    private static readonly SiegePostprocessRuleDefinition[] FallbackRules =
    {
        new SiegePostprocessRuleDefinition(SiegeActionTagCatalog.MercyPromptTag, "Use when an eligible speaker on either side directly acknowledges the player's current mercy decision. Soldier complaints do not cancel a valid player order."),
        new SiegePostprocessRuleDefinition(SiegeActionTagCatalog.ReliefPromptTag, "Use only when a settlement notable/headman or ordinary civilian directly accepts the player's current relief, protection, discipline, food, gold, or supply arrangement. Other roles may only suggest it."),
        new SiegePostprocessRuleDefinition(SiegeActionTagCatalog.InspirePromptTag, "Use only when a settlement notable/headman or ordinary civilian directly accepts the player's current public reassurance, speech, stabilization, or cooperation plan. Other roles may only suggest it."),
        new SiegePostprocessRuleDefinition(SiegeActionTagCatalog.RallyOathPromptTag, "Use only when a settlement notable/headman or ordinary civilian directly accepts the player's current public oath or civilian allegiance arrangement. Other roles may only suggest it."),
        new SiegePostprocessRuleDefinition(SiegeActionTagCatalog.SoldierAppeasementPromptTag, "Use only when appeasement is pending and an allied ordinary soldier directly accepts the player's current compensation, discipline explanation, or loot arrangement."),
        new SiegePostprocessRuleDefinition(SiegeActionTagCatalog.GatherCiviliansPromptTag, "Use when an eligible responder directly accepts, relays, or executes the player's current order to gather hidden residents. Decide from full semantics, never fixed words."),
        new SiegePostprocessRuleDefinition(SiegeActionTagCatalog.CivilianRobberyPromptTag, "Use only when a defeated civilian, merchant, artisan, headman, or notable directly responds to the player's current demand for gold or goods. This is partial robbery, not full-town plunder."),
        new SiegePostprocessRuleDefinition(SiegeActionTagCatalog.PlunderPromptTag, "Use only when an allied ordinary soldier directly receives the player's current full-town plunder order. The soldier may complain but cannot refuse, delay, redirect, or replace a valid order."),
        new SiegePostprocessRuleDefinition(SiegeActionTagCatalog.MassacrePromptTag, "Use only when an allied ordinary soldier directly receives the player's current massacre order. Negative wording does not cancel a valid order; a later eligible order may stop before all captured targets die."),
        new SiegePostprocessRuleDefinition(SiegeActionTagCatalog.CulturalRepopulationPromptTag, "Use only when an allied ordinary soldier directly receives the player's current order to kill the original residents and repopulate with the player's culture. Suggestions and NPC-to-NPC speech never execute it."),
        new SiegePostprocessRuleDefinition(SiegeActionTagCatalog.StopMassacrePromptTag, "Use only when an allied ordinary soldier directly receives the player's current order to stop an active incomplete massacre. Existing deaths, loot, and consequences remain."),
        new SiegePostprocessRuleDefinition(SiegeActionTagCatalog.ConstructiveCultureChangePromptTag, "Use only when an authorized responder directly accepts the player's current non-destructive town culture decree. It changes only town culture and never means killing, expulsion, repopulation, or colonization."),
    };

    public static IReadOnlyList<SiegePostprocessRuleDefinition> GetFallbackRules()
    {
        return FallbackRules;
    }
}
