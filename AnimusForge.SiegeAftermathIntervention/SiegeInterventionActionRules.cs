namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Standalone extraction of the current GCCZ post-siege outcome-routing invariants.
/// It deliberately contains no AF, Harmony, or Bannerlord calls.
/// </summary>
public static class SiegeInterventionActionRules
{
    public static bool IsMercyTrack(SiegeInterventionActionKind action)
    {
        return action == SiegeInterventionActionKind.Mercy
            || action == SiegeInterventionActionKind.Relief
            || action == SiegeInterventionActionKind.Inspire
            || action == SiegeInterventionActionKind.RallyOath;
    }

    public static bool IsDestructive(SiegeInterventionActionKind action)
    {
        return action == SiegeInterventionActionKind.CivilianRobbery
            || action == SiegeInterventionActionKind.Plunder
            || action == SiegeInterventionActionKind.Massacre
            || action == SiegeInterventionActionKind.CulturalRepopulation;
    }

    public static bool IsSoldierMediatedDestructive(SiegeInterventionActionKind action)
    {
        return action == SiegeInterventionActionKind.Plunder
            || action == SiegeInterventionActionKind.Massacre
            || action == SiegeInterventionActionKind.CulturalRepopulation
            || action == SiegeInterventionActionKind.StopMassacre;
    }

    public static bool IsIrreversible(SiegeInterventionActionKind action)
    {
        return action == SiegeInterventionActionKind.CulturalRepopulation;
    }

    public static bool HasDestructiveOutcomeLocked(
        SiegeInterventionOutcome currentOutcome,
        bool culturalRepopulationRequested = false,
        bool pendingDevastateAftermath = false)
    {
        return culturalRepopulationRequested
            || pendingDevastateAftermath
            || currentOutcome == SiegeInterventionOutcome.Massacre;
    }

    public static SiegeInterventionActionRuleDecision Evaluate(
        SiegeInterventionActionKind action,
        SiegeInterventionOutcome currentOutcome,
        bool destructiveAllowed,
        bool targetIsAlliedSoldier = true,
        bool culturalRepopulationRequested = false,
        bool pendingDevastateAftermath = false)
    {
        if (action == SiegeInterventionActionKind.Unknown)
        {
            return Block(action, currentOutcome, "unknown_action");
        }

        if (action == SiegeInterventionActionKind.AppeaseSoldiers
            || action == SiegeInterventionActionKind.GatherCivilians)
        {
            return Allow(action, currentOutcome, currentOutcome, stopsReversiblePlunder: false, "scene_control_action");
        }

        bool destructiveLocked = HasDestructiveOutcomeLocked(currentOutcome, culturalRepopulationRequested, pendingDevastateAftermath);

        if (action == SiegeInterventionActionKind.StopMassacre)
        {
            return currentOutcome == SiegeInterventionOutcome.Massacre
                ? Allow(action, currentOutcome, SiegeInterventionOutcome.Plunder, stopsReversiblePlunder: false, "massacre_stop_allowed")
                : Block(action, currentOutcome, "massacre_stop_requires_active_massacre");
        }

        if (IsMercyTrack(action))
        {
            if (destructiveLocked)
            {
                return Block(action, currentOutcome, "mercy_track_blocked_after_irreversible_destructive_outcome");
            }

            return Allow(
                action,
                currentOutcome,
                SiegeInterventionOutcome.MercyRelief,
                stopsReversiblePlunder: currentOutcome == SiegeInterventionOutcome.Plunder,
                currentOutcome == SiegeInterventionOutcome.Plunder
                    ? "mercy_track_overrides_reversible_plunder"
                    : "mercy_track_allowed");
        }

        if (action == SiegeInterventionActionKind.Plunder)
        {
            if (destructiveLocked)
            {
                return Block(action, currentOutcome, "plunder_blocked_after_irreversible_destructive_outcome");
            }

            return Allow(action, currentOutcome, SiegeInterventionOutcome.Plunder, stopsReversiblePlunder: false, "plunder_allowed_reversible");
        }

        if (action == SiegeInterventionActionKind.CivilianRobbery)
        {
            if (destructiveLocked)
            {
                return Block(action, currentOutcome, "robbery_blocked_after_irreversible_destructive_outcome");
            }

            return Allow(action, currentOutcome, currentOutcome, stopsReversiblePlunder: false, "civilian_robbery_allowed_local_penalty");
        }

        if (action == SiegeInterventionActionKind.Massacre)
        {
            return Allow(action, currentOutcome, SiegeInterventionOutcome.Massacre, stopsReversiblePlunder: false, "massacre_allowed_interruptible");
        }

        if (action == SiegeInterventionActionKind.CulturalRepopulation)
        {
            if (!targetIsAlliedSoldier)
            {
                return Block(action, currentOutcome, "cultural_repopulation_requires_allied_soldier_context");
            }

            return Allow(action, currentOutcome, SiegeInterventionOutcome.Massacre, stopsReversiblePlunder: false, "cultural_repopulation_allowed_irreversible");
        }

        return Block(action, currentOutcome, "unhandled_action");
    }

    private static SiegeInterventionActionRuleDecision Allow(
        SiegeInterventionActionKind action,
        SiegeInterventionOutcome currentOutcome,
        SiegeInterventionOutcome resultingOutcome,
        bool stopsReversiblePlunder,
        string reasonCode)
    {
        return new SiegeInterventionActionRuleDecision(
            isAllowed: true,
            action,
            currentOutcome,
            resultingOutcome,
            stopsReversiblePlunder,
            reasonCode);
    }

    private static SiegeInterventionActionRuleDecision Block(
        SiegeInterventionActionKind action,
        SiegeInterventionOutcome currentOutcome,
        string reasonCode)
    {
        return new SiegeInterventionActionRuleDecision(
            isAllowed: false,
            action,
            currentOutcome,
            currentOutcome,
            stopsReversiblePlunder: false,
            reasonCode);
    }
}
