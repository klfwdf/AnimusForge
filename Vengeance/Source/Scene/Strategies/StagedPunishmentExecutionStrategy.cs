using System;
using RichExecutions.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RichExecutions.Scene;

/// <summary>
/// Shared choreography for prop-driven sentences. The default outcome uses
/// the proven single-Blow battlefield death chain, while a concrete method may
/// override the outcome and defer campaign death to mission exit when its final
/// pose must remain fixed. Each method still owns its ID, timing and visuals.
/// </summary>
internal abstract class StagedPunishmentExecutionStrategy : IExecutionMethodStrategy
{
    private IExecutionSceneHost _host = null!;
    private bool _releaseFrameHandled;
    private bool _sequenceStarted;
    private bool _sequenceCompleted;
    private float _sequenceElapsed;

    public abstract string MethodId { get; }

    protected abstract float ReactionSeconds { get; }

    protected abstract string SequenceDescription { get; }

    /// <summary>
    /// Most staged punishments begin by replacing the victim idle with a visible
    /// reaction clip. Stoning deliberately keeps the original prisoner-kneeling
    /// idle for the entire barrage, including after real stone impacts.
    /// </summary>
    protected virtual bool PlayVictimReactionAtSequenceStart => true;

    protected IExecutionSceneHost Host => _host;

    public bool UsesStagedExecution => true;

    public bool RequiresExecutionAxe => false;

    public virtual bool DefersVictimDeathToMissionExit => false;

    public void AttachHost(IExecutionSceneHost host)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
    }

    public virtual void SetupSceneVisuals()
    {
        // Dedicated native props are spawned by ExecutionSceneVisualProfile.
    }

    public virtual void TickSceneVisuals(float dt)
    {
        // These native props are static and require no per-tick maintenance.
    }

    public virtual void PrefetchActions()
    {
        var deathAction = _host.SceneAct?.DeathAction;
        if (!string.IsNullOrWhiteSpace(deathAction))
        {
            _host.TryPlayRequiredAction(
                _host.VictimAgent,
                deathAction!,
                MethodId + " victim reaction prefetch",
                forceFullBody: true);
        }
    }

    public virtual void StartExecutionAction()
    {
        _host.StartStagedMethodExecutionAction();
    }

    public virtual void TickExecution(float dt)
    {
        if (_sequenceStarted || _sequenceCompleted)
        {
            return;
        }

        var safeDt = MathF.Max(0f, dt);
        _host.ExecutionActionAttemptElapsed += safeDt;
        var actionAgent = _host.GetActiveExecutionActor();
        if (actionAgent is null || !actionAgent.IsActive())
        {
            return;
        }

        _host.ReassertExecutionActionRoot(actionAgent, MethodId + " action tick");
        var progress = _host.ObserveExecutionAction(actionAgent);
        if (progress >= 0.01f)
        {
            _host.MaximumExecutionActionProgress = MathF.Max(
                _host.MaximumExecutionActionProgress,
                progress);
        }

        var releaseProgress = _host.SceneAct?.LethalProgress ?? 0.52f;
        if (progress >= releaseProgress ||
            _host.MaximumExecutionActionProgress >= releaseProgress)
        {
            if (!_releaseFrameHandled)
            {
                _releaseFrameHandled = true;
                BeginMethodSequence("action release frame");
            }

            return;
        }

        var timeout = _host.SceneAct?.MaximumActionSeconds ?? 4.5f;
        if (_host.ExecutionActionAttemptElapsed >= timeout && !_releaseFrameHandled)
        {
            _releaseFrameHandled = true;
            _host.LogWarning(
                $"{MethodId} action stalled at progress " +
                $"{MathF.Max(progress, _host.MaximumExecutionActionProgress):0.000}; " +
                "the dedicated victim reaction will continue without ending the mission.");
            BeginMethodSequence("action timeout");
        }
    }

    public void BeginMethodSequence(string trigger)
    {
        if (_sequenceStarted || _sequenceCompleted)
        {
            return;
        }

        _sequenceStarted = true;
        _sequenceElapsed = 0f;
        var victim = _host.VictimAgent;
        var reactionAction = _host.SceneAct?.DeathAction;
        if (victim is null || !victim.IsActive())
        {
            _host.LogError(
                $"The original prisoner Agent is unavailable when the {MethodId} reaction begins; " +
                "the timed lethal flow remains active for diagnosis.");
        }
        else if (PlayVictimReactionAtSequenceStart &&
                 (string.IsNullOrWhiteSpace(reactionAction) ||
                  !_host.TryPlayRequiredAction(
                      victim,
                      reactionAction!,
                      MethodId + " victim reaction",
                      forceFullBody: true)))
        {
            _host.LogWarning(
                $"The {MethodId} victim reaction '{reactionAction}' could not start; " +
                "the lethal timer continues and no mission exit is forced.");
        }

        _host.LogInfo(
            $"Began {MethodId} after {trigger}: {SequenceDescription}, " +
            $"reaction={ReactionSeconds:0.00}s before the single-Blow death chain.");
    }

    public void TickMethodSequence(float dt)
    {
        if (!_sequenceStarted || _sequenceCompleted)
        {
            return;
        }

        _sequenceElapsed += MathF.Max(0f, dt);
        if (_sequenceElapsed < ReactionSeconds)
        {
            return;
        }

        _sequenceCompleted = true;
        _host.LogInfo(
            $"Completed the visible {MethodId} reaction; requesting the shared lethal frame.");
        _host.RequestLethalFrame();
    }

    public virtual void ApplyLethalFrameEffects()
    {
        // Method visuals and the victim reaction are already active.
    }

    public virtual bool ApplyDeath()
    {
        var result = _host.ApplySharedBattlefieldDeath(_host.GetActiveExecutionActor());
        var applied = result is ExecutionVictimDeathResult.Applied or
            ExecutionVictimDeathResult.AlreadyApplied;
        if (!applied)
        {
            _host.LogError(
                $"{MethodId} death chain returned {result}; no replacement kill is attempted.");
        }

        return applied;
    }

    public virtual void TickAfterDeath(float dt)
    {
        // Native corpse handling owns the final body after the single Blow.
    }

    public virtual void Cleanup()
    {
        // Shared runtime cleanup removes profile props and restores player gear.
    }
}
