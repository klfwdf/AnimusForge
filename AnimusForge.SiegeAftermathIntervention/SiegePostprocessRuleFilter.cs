using System;
using System.Linq;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free postprocess-rule filtering for the active GCCZ intervention scene.
/// AF adapters pass runtime state in; this core owns action-tag classification.
/// </summary>
public static class SiegePostprocessRuleFilter
{
    public static bool ShouldAllowTag(
        string tag,
        SiegePostprocessRuleEligibilityFacts facts)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return false;
        }

        var kinds = SiegeActionTagCatalog.ExtractKinds(tag.Trim());
        if (kinds.Count == 0)
        {
            return true;
        }

        if (!facts.ReplyIsDirectPlayerResponse)
        {
            return false;
        }

        bool mercyTrackTag = kinds.Any(SiegeInterventionActionRules.IsMercyTrack);
        if (facts.DestructiveLocked && mercyTrackTag)
        {
            return false;
        }

        bool soldierAppeasementTag = kinds.Contains(SiegeInterventionActionKind.AppeaseSoldiers);
        if (soldierAppeasementTag
            && (!facts.IsAlliedSoldier
                || !facts.SoldierAppeasementRequired
                || facts.SoldierAppeasementApplied))
        {
            return false;
        }

        bool soldierMediatedDestructiveTag = kinds.Any(SiegeInterventionActionRules.IsSoldierMediatedDestructive);
        if (soldierMediatedDestructiveTag && !facts.IsAlliedSoldier)
        {
            return false;
        }

        bool civilianRobberyTag = kinds.Contains(SiegeInterventionActionKind.CivilianRobbery);
        if (civilianRobberyTag && !facts.IsCivilian)
        {
            return false;
        }

        return true;
    }
}
