using System;
using System.Collections.Generic;
using System.Linq;
using RichExecutions.Diagnostics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;
using BannerlordCampaign = TaleWorlds.CampaignSystem.Campaign;

namespace RichExecutions.Core;

public sealed class ExecutionService
{
    private const int MaximumRememberedOutcomes = 128;

    private enum CommitState
    {
        Starting,
        Ready,
        Committing,
        ApplyingDeath,
        Finished
    }

    private sealed class ActiveExecution
    {
        public ActiveExecution(ExecutionRequest request)
        {
            Request = request;
        }

        public ExecutionRequest Request { get; }
        public CommitState State { get; set; } = CommitState.Starting;
        public ExecutionActor Actor { get; set; } = ExecutionActor.Undecided;
        public ExecutionOutcome? Outcome { get; set; }
        public bool LethalFrameValidated { get; set; }
    }

    private readonly List<IExecutionConsequenceRule> _consequenceRules = new();

    // Guards the session maps AND every mutable ActiveExecution field. Game
    // operations and extension callbacks run outside the lock. ApplyingDeath
    // is the cancellation cutoff, claimed under this same lock immediately
    // before calling the irreversible vanilla action. This is a transaction
    // guard, not permission to invoke game APIs from a background thread.
    private readonly object _sync = new();
    private readonly Dictionary<Guid, ActiveExecution> _active = new();
    private readonly Dictionary<Guid, ExecutionOutcome> _rememberedOutcomes = new();
    private readonly Queue<Guid> _outcomeOrder = new();

    public IReadOnlyList<IExecutionConsequenceRule> ConsequenceRules
    {
        get
        {
            lock (_sync)
            {
                return _consequenceRules.ToArray();
            }
        }
    }

    internal event Action<ExecutionOutcome>? OutcomeCommitted;

    public void AddConsequenceRule(IExecutionConsequenceRule rule)
    {
        if (rule is null)
        {
            throw new ArgumentNullException(nameof(rule));
        }

        lock (_sync)
        {
            _consequenceRules.RemoveAll(existing =>
                string.Equals(existing.StringId, rule.StringId, StringComparison.OrdinalIgnoreCase));
            _consequenceRules.Add(rule);
        }
    }

    public ExecutionValidationResult Validate(
        ExecutionRequest? request,
        ExecutionValidationStage stage = ExecutionValidationStage.Confirmation) =>
        ValidateCore(request, stage, requireCustody: true);

    private ExecutionValidationResult ValidateCore(
        ExecutionRequest? request,
        ExecutionValidationStage stage,
        bool requireCustody)
    {
        if (request is null || request.Victim is null || request.Executor is null ||
            request.Venue is null || request.Method is null || request.Charge is null)
        {
            return Invalid(
                ExecutionFailureReason.InvalidRequest,
                "{=REX_Error_Invalid_Request}The execution request is incomplete.");
        }

        lock (_sync)
        {
            _active.TryGetValue(request.SessionId, out var session);
            if (session is not null && !ReferenceEquals(session.Request, request))
            {
                return RequestMismatch();
            }

            if (_rememberedOutcomes.TryGetValue(request.SessionId, out var remembered))
            {
                return ReferenceEquals(remembered.Request, request)
                    ? SessionNotActive()
                    : RequestMismatch();
            }

            if (stage == ExecutionValidationStage.LethalFrame &&
                (session is null || session.State == CommitState.Starting))
            {
                return SessionNotActive();
            }
        }

        if (Hero.MainHero.IsPrisoner)
        {
            return Invalid(
                ExecutionFailureReason.PlayerUnavailable,
                "{=REX_Tooltip_Player_Prisoner}You cannot organize a sentence while you are a prisoner.");
        }

        if (BannerlordCampaign.Current.IsMainHeroDisguised)
        {
            return Invalid(
                ExecutionFailureReason.PlayerUnavailable,
                "{=REX_Tooltip_Disguised}You cannot organize a public sentence while disguised.");
        }

        if (request.Executor != Hero.MainHero)
        {
            return Invalid(
                ExecutionFailureReason.InvalidRequest,
                "{=REX_Error_Invalid_Executor}Only the player may authorize this public sentence.");
        }

        if (request.Venue != Settlement.CurrentSettlement || !request.Venue.IsTown ||
            request.Venue.IsUnderSiege)
        {
            return Invalid(
                ExecutionFailureReason.VenueUnavailable,
                "{=REX_Error_Venue_Unavailable}This town cannot host an execution right now.");
        }

        var authority = GetVenueAuthority(request.Venue);
        if (authority == VenueAuthority.None)
        {
            return Invalid(
                ExecutionFailureReason.InsufficientAuthority,
                "{=REX_Tooltip_No_Jurisdiction}You may only hold a public execution in your own town or a town of your current kingdom.");
        }

        var playerIsRuler = Clan.PlayerClan.Kingdom?.RulingClan == Clan.PlayerClan;
        var expectedCost = ExecutionRuleMath.GetInfluenceCost(authority, playerIsRuler);
        if (authority != request.VenueAuthority || expectedCost != request.InfluenceCost)
        {
            return Invalid(
                ExecutionFailureReason.VenueUnavailable,
                "{=REX_Error_Authority_Changed}Authority over this town has changed since the sentence was prepared.");
        }

        var townOwner = request.Venue.OwnerClan?.Leader;
        if (authority == VenueAuthority.AlliedTown && !playerIsRuler && townOwner is not null &&
            Hero.MainHero.GetRelation(townOwner) < 0)
        {
            return Invalid(
                ExecutionFailureReason.OwnerRelationTooLow,
                "{=REX_Tooltip_Relation_Low}The town's lord refuses to grant you the square because your relation is below zero.");
        }

        if (expectedCost > 0 && Clan.PlayerClan.Influence + 0.001f < expectedCost)
        {
            return Invalid(
                ExecutionFailureReason.InsufficientInfluence,
                "{=REX_Tooltip_Influence_Low}Your clan lacks the influence required to use an allied town's square.");
        }

        if (requireCustody && !IsVictimInRequiredCustody(request))
        {
            return Invalid(
                ExecutionFailureReason.VictimUnavailable,
                "{=REX_Error_Victim_Unavailable}The selected prisoner is no longer in the required custody.");
        }

        if (!request.Victim.IsAlive || request.Victim.IsChild)
        {
            return Invalid(
                ExecutionFailureReason.VictimUnavailable,
                "{=REX_Error_Victim_Ineligible}The selected prisoner is not a living adult.");
        }

        if (!request.Victim.CanDie(KillCharacterAction.KillCharacterActionDetail.Executed))
        {
            return Invalid(
                ExecutionFailureReason.VictimProtected,
                "{=REX_Error_Protected_Victim}The selected hero is protected by the campaign or an active quest.");
        }

        return ExecutionValidationResult.Valid();
    }

    public ExecutionValidationResult ValidateAndStageLethalFrame(ExecutionRequest request)
    {
        var validation = Validate(request, ExecutionValidationStage.LethalFrame);
        if (!validation.IsValid)
        {
            return validation;
        }

        lock (_sync)
        {
            if (!_active.TryGetValue(request.SessionId, out var active))
            {
                return SessionNotActive();
            }

            if (!ReferenceEquals(active.Request, request))
            {
                return RequestMismatch();
            }

            if (active.State != CommitState.Ready)
            {
                return CommitInProgress();
            }

            // Only this exact request may own or use the custody exception.
            active.LethalFrameValidated = true;
        }

        RexLog.Info(
            $"Session {request.SessionId} recorded its lethal-frame custody validation for mission-exit commit.");
        return ExecutionValidationResult.Valid();
    }

    // The immutable request instance is the session's authorization token.
    // Callers must retain it; reconstructing a request with the same GUID does
    // not authorize a different request to stage, commit, or cancel it.
    public ExecutionValidationResult Begin(ExecutionRequest request)
    {
        var validation = Validate(request, ExecutionValidationStage.Confirmation);
        if (!validation.IsValid)
        {
            return validation;
        }

        ActiveExecution active;
        lock (_sync)
        {
            if (_active.TryGetValue(request.SessionId, out var existing))
            {
                if (!ReferenceEquals(existing.Request, request))
                {
                    return RequestMismatch();
                }

                return existing.State == CommitState.Ready
                    ? ExecutionValidationResult.Valid()
                    : CommitInProgress();
            }

            if (_rememberedOutcomes.ContainsKey(request.SessionId))
            {
                return Invalid(
                    ExecutionFailureReason.InvalidRequest,
                    "{=REX_Error_Duplicate_Session}This execution session has already finished.");
            }

            // Reserve before publishing Starting so reentrant Begin/Commit
            // cannot bypass the original subscriber's authorization decision.
            active = new ActiveExecution(request);
            _active.Add(request.SessionId, active);
        }

        var starting = RichExecutionEvents.RaiseStarting(request);
        if (starting.IsCancelled)
        {
            var message = TextObject.IsNullOrEmpty(starting.CancellationReason)
                ? new TextObject("{=REX_Error_Cancelled}Another module cancelled this execution.")
                : starting.CancellationReason;
            Cancel(request, ExecutionFailureReason.CancelledByExtension, message);
        }

        // Subscribers may have cancelled the reserved session or changed the
        // campaign context. Do not publish it as ready in either case.
        validation = Validate(request, ExecutionValidationStage.Confirmation);
        if (!validation.IsValid)
        {
            var failed = CompleteWithoutDeath(active, ExecutionOutcome.Failed(
                request, ExecutionActor.Undecided, validation.Reason, validation.Message));
            return ExecutionValidationResult.Invalid(failed.FailureReason, failed.Message);
        }

        lock (_sync)
        {
            if (active.Outcome is not null)
            {
                return ExecutionValidationResult.Invalid(
                    active.Outcome.FailureReason, active.Outcome.Message);
            }

            active.State = CommitState.Ready;
        }

        RexLog.Info($"Session {request.SessionId} prepared for {request.Victim.StringId}.");
        return ExecutionValidationResult.Valid();
    }

    public ExecutionOutcome Commit(ExecutionRequest request, ExecutionActor actor)
    {
        ActiveExecution active;
        bool requireCustody;
        lock (_sync)
        {
            if (_rememberedOutcomes.TryGetValue(request.SessionId, out var remembered))
            {
                return ReferenceEquals(remembered.Request, request)
                    ? remembered
                    : FailedValidation(request, actor, RequestMismatch());
            }

            if (!_active.TryGetValue(request.SessionId, out active!))
            {
                return FailedValidation(request, actor, SessionNotActive());
            }

            if (!ReferenceEquals(active.Request, request))
            {
                return FailedValidation(request, actor, RequestMismatch());
            }

            if (active.State != CommitState.Ready)
            {
                return FailedValidation(request, actor, CommitInProgress());
            }

            active.State = CommitState.Committing;
            active.Actor = actor;
            requireCustody = !active.LethalFrameValidated;
        }

        // A mission-staged death can clear PartyBelongedToAsPrisoner before
        // OnEndMission. Custody was already checked immediately before that
        // visual death, so only that one volatile field is skipped here.
        var validation = ValidateCore(
            request,
            ExecutionValidationStage.LethalFrame,
            requireCustody);
        if (!validation.IsValid)
        {
            var failed = ExecutionOutcome.Failed(
                request,
                actor,
                validation.Reason,
                validation.Message);
            return CompleteWithoutDeath(active, failed);
        }

        lock (_sync)
        {
            // Validation can invoke other game/module code; a cancellation
            // accepted there must not publish a later pre-death notification.
            if (active.Outcome is not null)
            {
                return active.Outcome;
            }
        }

        RichExecutionEvents.RaiseDeathCommitting(request, actor);
        lock (_sync)
        {
            if (active.Outcome is not null)
            {
                return active.Outcome;
            }

            // Cancel uses the same lock and may succeed throughout the
            // DeathCommitting callbacks. Once this state is claimed, a late
            // cancellation cannot report a false Cancelled notification.
            active.State = CommitState.ApplyingDeath;
        }

        try
        {
            try
            {
                using (ExecutionRelationScope.Begin(request.Victim))
                {
                    KillCharacterAction.ApplyByExecution(request.Victim, request.Executor, false, false);
                }
            }
            catch (Exception exception)
            {
                if (request.Victim.IsAlive)
                {
                    throw;
                }

                RexLog.Error("Vanilla execution threw after committing the hero death; continuing cleanup.", exception);
            }

            if (request.Victim.IsAlive)
            {
                var rejected = ExecutionOutcome.Failed(
                    request,
                    actor,
                    ExecutionFailureReason.VanillaActionRejected,
                    new TextObject("{=REX_Error_Vanilla_Rejected}The campaign rejected the execution; no public consequences were applied."));
                return CompleteWithoutDeath(active, rejected);
            }

            var effects = new ExecutionEffectContext(request);
            try
            {
                effects.ChangePlayerInfluence(-request.InfluenceCost);
            }
            catch (Exception exception)
            {
                RexLog.Error("The allied-town authority cost could not be applied after the committed death.", exception);
            }

            IExecutionConsequenceRule[] rules;
            lock (_sync)
            {
                rules = _consequenceRules.ToArray();
            }

            foreach (var rule in rules)
            {
                try
                {
                    rule.Apply(request, effects);
                }
                catch (Exception exception)
                {
                    RexLog.Error($"Consequence rule '{rule.StringId}' failed.", exception);
                }
            }

            var outcome = ExecutionOutcome.Succeeded(
                request,
                actor,
                new TextObject("{=REX_Result_Success}The public sentence has been carried out."),
                effects.ToDeltas());
            CompleteCommitted(active, outcome);
            return outcome;
        }
        catch (Exception exception)
        {
            RexLog.Error($"Execution session {request.SessionId} failed before death was committed.", exception);
            var failed = ExecutionOutcome.Failed(
                request,
                actor,
                ExecutionFailureReason.UnexpectedError,
                new TextObject("{=REX_Error_Unexpected}The execution failed because of an unexpected game or module error."));
            return CompleteWithoutDeath(active, failed);
        }
    }

    // Accepted through the pre-death event; terminal cancellations are cached
    // for retry idempotence. After ApplyingDeath starts this legacy void API
    // ignores cancellation and emits no Cancelled notification.
    public void Cancel(
        ExecutionRequest request,
        ExecutionFailureReason reason,
        TextObject message)
    {
        ExecutionOutcome cancelled;
        lock (_sync)
        {
            if (!_active.TryGetValue(request.SessionId, out var active) ||
                !ReferenceEquals(active.Request, request) ||
                active.State == CommitState.ApplyingDeath || active.State == CommitState.Finished)
            {
                return;
            }

            cancelled = ExecutionOutcome.Failed(active.Request, active.Actor, reason, message);
            FinishLocked(active, cancelled);
        }

        RexLog.Info($"Session {request.SessionId} cancelled: {reason}.");
        RichExecutionEvents.RaiseCancelled(cancelled.Request, reason, cancelled.Message);
    }

    public static VenueAuthority GetVenueAuthority(Settlement settlement)
    {
        var ownerClan = settlement.OwnerClan;
        if (ownerClan is null)
        {
            return VenueAuthority.None;
        }

        if (ownerClan == Clan.PlayerClan)
        {
            return VenueAuthority.OwnTown;
        }

        var playerKingdom = Clan.PlayerClan.Kingdom;
        return playerKingdom is not null && ownerClan.Kingdom == playerKingdom
            ? VenueAuthority.AlliedTown
            : VenueAuthority.None;
    }

    public static VictimPoliticalStatus GetVictimPoliticalStatus(Hero victim)
    {
        var clan = victim.Clan;
        if (clan?.IsBanditFaction == true || clan?.IsRebelClan == true)
        {
            return VictimPoliticalStatus.BanditOrRebel;
        }

        var playerFaction = Hero.MainHero.MapFaction;
        if (clan?.Kingdom is not null && clan.Kingdom == Clan.PlayerClan.Kingdom)
        {
            return VictimPoliticalStatus.SameKingdom;
        }

        if (FactionManager.IsAtWarAgainstFaction(victim.MapFaction, playerFaction))
        {
            return VictimPoliticalStatus.WarEnemy;
        }

        return VictimPoliticalStatus.Neutral;
    }

    private static bool IsVictimInRequiredCustody(ExecutionRequest request)
    {
        var prisonerParty = request.Victim.PartyBelongedToAsPrisoner;
        if (request.PrisonerSource == PrisonerSource.PlayerParty)
        {
            return prisonerParty == MobileParty.MainParty?.Party;
        }

        return request.Venue.OwnerClan == Clan.PlayerClan && prisonerParty == request.Venue.Party;
    }

    private static ExecutionValidationResult Invalid(ExecutionFailureReason reason, string text) =>
        ExecutionValidationResult.Invalid(reason, new TextObject(text));

    private static ExecutionValidationResult RequestMismatch() =>
        Invalid(ExecutionFailureReason.InvalidRequest,
            "{=REX_Error_Invalid_Request}The execution request is incomplete.");

    private static ExecutionValidationResult SessionNotActive() =>
        Invalid(ExecutionFailureReason.SessionNotActive,
            "{=REX_Error_Session_Not_Active}This execution session is no longer active.");

    private static ExecutionValidationResult CommitInProgress() =>
        Invalid(ExecutionFailureReason.CommitInProgress,
            "{=REX_Error_Commit_In_Progress}The lethal moment is already being resolved.");

    private static ExecutionOutcome FailedValidation(
        ExecutionRequest request, ExecutionActor actor, ExecutionValidationResult validation) =>
        ExecutionOutcome.Failed(request, actor, validation.Reason, validation.Message);

    private ExecutionOutcome CompleteWithoutDeath(ActiveExecution active, ExecutionOutcome outcome)
    {
        lock (_sync)
        {
            if (active.Outcome is not null)
            {
                return active.Outcome;
            }

            FinishLocked(active, outcome);
        }

        RichExecutionEvents.RaiseCancelled(
            active.Request,
            outcome.FailureReason,
            outcome.Message);
        return outcome;
    }

    private void CompleteCommitted(ActiveExecution active, ExecutionOutcome outcome)
    {
        lock (_sync)
        {
            FinishLocked(active, outcome);
        }

        RexLog.Info($"Session {active.Request.SessionId} committed exactly once.");
        if (OutcomeCommitted is not null)
        {
            foreach (Action<ExecutionOutcome> handler in OutcomeCommitted.GetInvocationList())
            {
                try
                {
                    handler(outcome);
                }
                catch (Exception exception)
                {
                    RexLog.Error("An execution-history observer failed after a committed death.", exception);
                }
            }
        }

        RichExecutionEvents.RaiseCompleted(outcome);
    }

    // Caller must already hold _sync. The terminal outcome is retained on
    // active as well as in the bounded cache, so a suspended callback cannot
    // lose its cancellation if other sessions evict the cached entry.
    private void FinishLocked(ActiveExecution active, ExecutionOutcome outcome)
    {
        active.State = CommitState.Finished;
        active.Outcome = outcome;
        if (_active.TryGetValue(active.Request.SessionId, out var current) &&
            ReferenceEquals(current, active))
        {
            _active.Remove(active.Request.SessionId);
        }

        RememberLocked(outcome);
    }

    // Caller must already hold _sync.
    private void RememberLocked(ExecutionOutcome outcome)
    {
        var id = outcome.Request.SessionId;
        if (_rememberedOutcomes.ContainsKey(id))
        {
            return;
        }

        _rememberedOutcomes.Add(id, outcome);
        _outcomeOrder.Enqueue(id);
        while (_outcomeOrder.Count > MaximumRememberedOutcomes)
        {
            _rememberedOutcomes.Remove(_outcomeOrder.Dequeue());
        }
    }
}
