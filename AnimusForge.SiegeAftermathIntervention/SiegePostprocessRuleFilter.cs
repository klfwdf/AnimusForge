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
        bool destructiveAllowed,
        bool destructiveLocked,
        bool soldierAppeasementRequired,
        bool soldierAppeasementApplied)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return false;
        }

        var kinds = SiegeActionTagCatalog.ExtractKinds(tag.Trim());
        bool mercyTrackTag = kinds.Any(SiegeInterventionActionRules.IsMercyTrack);
        if (destructiveLocked && mercyTrackTag)
        {
            return false;
        }

        bool soldierAppeasementTag = kinds.Contains(SiegeInterventionActionKind.AppeaseSoldiers);
        if (soldierAppeasementTag && (!soldierAppeasementRequired || soldierAppeasementApplied))
        {
            return false;
        }

        return true;
    }
}
