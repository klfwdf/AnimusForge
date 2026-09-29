using System;
using System.Collections.Generic;
using RichExecutions.Diagnostics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace RichExecutions.Core;

/// <summary>
/// Delegates ordinary executions to the active game model, but narrows this
/// module's synchronous campaign commit to the victim's kingdom and halves
/// every negative vanilla relation value.
/// </summary>
internal sealed class ScopedExecutionRelationModel : ExecutionRelationModel
{
    public override int HeroKillingHeroClanRelationPenalty =>
        BaseModel.HeroKillingHeroClanRelationPenalty;

    public override int HeroKillingHeroFriendRelationPenalty =>
        BaseModel.HeroKillingHeroFriendRelationPenalty;

    public override int PlayerExecutingHeroFactionRelationPenaltyDishonorable =>
        BaseModel.PlayerExecutingHeroFactionRelationPenaltyDishonorable;

    public override int PlayerExecutingHeroClanRelationPenaltyDishonorable =>
        BaseModel.PlayerExecutingHeroClanRelationPenaltyDishonorable;

    public override int PlayerExecutingHeroFriendRelationPenaltyDishonorable =>
        BaseModel.PlayerExecutingHeroFriendRelationPenaltyDishonorable;

    public override int PlayerExecutingHeroHonorPenalty =>
        BaseModel.PlayerExecutingHeroHonorPenalty;

    public override int PlayerExecutingHeroFactionRelationPenalty =>
        BaseModel.PlayerExecutingHeroFactionRelationPenalty;

    public override int PlayerExecutingHeroHonorableNobleRelationPenalty =>
        BaseModel.PlayerExecutingHeroHonorableNobleRelationPenalty;

    public override int PlayerExecutingHeroClanRelationPenalty =>
        BaseModel.PlayerExecutingHeroClanRelationPenalty;

    public override int PlayerExecutingHeroFriendRelationPenalty =>
        BaseModel.PlayerExecutingHeroFriendRelationPenalty;

    public override int GetRelationChangeForExecutingHero(
        Hero victim,
        Hero hero,
        out bool showQuickNotification)
    {
        var vanillaChange = BaseModel.GetRelationChangeForExecutingHero(
            victim,
            hero,
            out showQuickNotification);
        if (!ExecutionRelationScope.IsActiveFor(victim))
        {
            return vanillaChange;
        }

        // Replace the base game's stream of one-line relationship notices with
        // one complete kingdom/lord/clan summary when the scoped execution ends.
        showQuickNotification = false;
        if (!BelongsToVictimKingdom(victim, hero))
        {
            ExecutionRelationScope.RecordDecision(
                hero,
                vanillaChange,
                adjustedChange: 0,
                outsideKingdom: true);
            return 0;
        }

        var adjustedChange = vanillaChange < 0
            ? (int)Math.Round(vanillaChange * 0.5d, MidpointRounding.AwayFromZero)
            : vanillaChange;
        ExecutionRelationScope.RecordDecision(
            hero,
            vanillaChange,
            adjustedChange,
            outsideKingdom: false);
        return adjustedChange;
    }

    private static bool BelongsToVictimKingdom(Hero victim, Hero target)
    {
        var victimClan = victim.Clan;
        var targetClan = target.Clan;
        if (victimClan is null || targetClan is null)
        {
            return false;
        }

        var victimKingdom = victimClan.Kingdom;
        return victimKingdom is not null
            ? targetClan.Kingdom == victimKingdom
            : targetClan == victimClan;
    }
}

internal static class ExecutionRelationScope
{
    // [ThreadStatic] is deliberate: the vanilla KillCharacterAction resolves
    // relation changes synchronously on the campaign thread that called it, so
    // the scope must follow that exact thread. If a future caller ever commits
    // an execution off the main campaign thread, the scope simply stays
    // inactive there - the safe direction is "no scoping", never "wrong scope".
    [ThreadStatic]
    private static ScopeState? _active;

    public static IDisposable Begin(Hero victim)
    {
        if (victim is null)
        {
            throw new ArgumentNullException(nameof(victim));
        }

        if (_active is not null)
        {
            throw new InvalidOperationException(
                "A RichExecutions relation scope is already active on this campaign thread.");
        }

        var state = new ScopeState(victim);
        _active = state;
        return new ScopeToken(state);
    }

    public static bool IsActiveFor(Hero victim) =>
        _active is not null && ReferenceEquals(_active.Victim, victim);

    public static void RecordDecision(
        Hero target,
        int vanillaChange,
        int adjustedChange,
        bool outsideKingdom)
    {
        var state = _active;
        if (state is null || vanillaChange >= 0)
        {
            return;
        }

        if (outsideKingdom)
        {
            state.BlockedOutsideKingdom++;
            return;
        }

        state.HalvedInsideKingdom++;
        state.VanillaMagnitude += Math.Abs(vanillaChange);
        state.AppliedMagnitude += Math.Abs(adjustedChange);
        if (adjustedChange < 0)
        {
            state.AffectedHeroes.Add(target);
            if (target.Clan is not null)
            {
                state.AffectedClans.Add(target.Clan);
            }
        }
    }

    private sealed class ScopeState
    {
        public ScopeState(Hero victim)
        {
            Victim = victim;
            ScopeName = victim.Clan?.Kingdom?.Name ??
                        victim.Clan?.Name ??
                        victim.Name;
            IsKingdomScope = victim.Clan?.Kingdom is not null;
        }

        public Hero Victim { get; }
        public TextObject ScopeName { get; }
        public bool IsKingdomScope { get; }
        public HashSet<Hero> AffectedHeroes { get; } = new();
        public HashSet<Clan> AffectedClans { get; } = new();
        public int BlockedOutsideKingdom { get; set; }
        public int HalvedInsideKingdom { get; set; }
        public int VanillaMagnitude { get; set; }
        public int AppliedMagnitude { get; set; }
    }

    private sealed class ScopeToken : IDisposable
    {
        private readonly ScopeState _state;
        private bool _disposed;

        public ScopeToken(ScopeState state)
        {
            _state = state;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (!ReferenceEquals(_active, _state))
            {
                RexLog.Error("The RichExecutions relation scope ended out of order.");
                return;
            }

            _active = null;
            ShowRelationSummary(_state);
            RexLog.Info(
                $"Scoped execution relations for victim {_state.Victim.StringId}: " +
                $"halvedInsideKingdom={_state.HalvedInsideKingdom}, " +
                $"blockedOutsideKingdom={_state.BlockedOutsideKingdom}, " +
                $"uniqueLords={_state.AffectedHeroes.Count}, " +
                $"uniqueClans={_state.AffectedClans.Count}, " +
                $"magnitude={_state.VanillaMagnitude}->{_state.AppliedMagnitude}.");
        }

        private static void ShowRelationSummary(ScopeState state)
        {
            if (state.AffectedHeroes.Count == 0)
            {
                return;
            }

            try
            {
                var scopeLabel = new TextObject(
                    state.IsKingdomScope
                        ? "{=REX_Execution_Relation_Kingdom_Label}Kingdom of {NAME}"
                        : "{=REX_Execution_Relation_Clan_Label}the {NAME} clan");
                scopeLabel.SetTextVariable("NAME", state.ScopeName);
                var summary = new TextObject(
                    "{=REX_Execution_Relation_Summary}{SCOPE}: relations decreased with {LORDS} lords across {CLANS} clans.");
                summary.SetTextVariable("SCOPE", scopeLabel);
                summary.SetTextVariable("LORDS", state.AffectedHeroes.Count);
                summary.SetTextVariable("CLANS", state.AffectedClans.Count);
                InformationManager.DisplayMessage(new InformationMessage(summary.ToString()));
            }
            catch (Exception exception)
            {
                RexLog.Warning(
                    "Could not display the complete execution relation summary; " +
                    "the already-applied scoped relationship changes are unchanged. " +
                    exception.Message);
            }
        }
    }
}
