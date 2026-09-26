using AnimusForge.Refactor.Runtime;

namespace AnimusForge.Refactor.Modules;

// Projects observed host effects into one typed terminal receipt. This owner
// never executes Mission, Economy, memory or save side effects.
internal static class DuelSettlementEffectOwner
{
    internal static bool TryCreate(
        DuelSessionKind kind,
        bool hasHero,
        bool hasNonHeroMemory,
        bool playerDefeated,
        DuelOutcomeEffectState renown,
        DuelOutcomeEffectState stake,
        out DuelOutcomeEffects effects,
        out string errorCode)
    {
        effects = null;
        if (kind != DuelSessionKind.Meeting
            && kind != DuelSessionKind.Arena
            && kind != DuelSessionKind.Wilderness)
        {
            errorCode = "duel_session_kind_invalid";
            return false;
        }

        DuelOutcomeEffectState memory = hasHero
            || (kind == DuelSessionKind.Wilderness && hasNonHeroMemory)
                ? DuelOutcomeEffectState.AttemptedUnconfirmed
                : DuelOutcomeEffectState.NotApplicable;
        DuelOutcomeEffectState afef = hasHero
            ? DuelOutcomeEffectState.AttemptedUnconfirmed
            : DuelOutcomeEffectState.NotApplicable;
        DuelOutcomeEffectState death = playerDefeated
            ? DuelOutcomeEffectState.AttemptedUnconfirmed
            : DuelOutcomeEffectState.NotApplicable;
        return DuelOutcomeEffects.TryCreate(
            memory, afef, death, renown, stake, out effects, out errorCode);
    }
}
