using System;
using RichExecutions.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RichExecutions.Scene;

/// <summary>
/// Beheading pipeline: axe ceremony, custom cut-scene action, detached-head
/// visual and its own single-Blow death chain. Fully isolated from hanging
/// and burning; it only uses the shared host for stage/agents.
/// </summary>
internal sealed class BeheadingExecutionStrategy : IExecutionMethodStrategy
{
    private const string TwoHandedBeheadingAction = "act_ver_cutscene_executioner_action";
    private const string TwoHandedBeheadingFallback = "act_quick_release_overswing_2h";
    private const string OneHandedBeheadingFallback = "act_quick_release_overswing_1h";
    private const string GenericFallbackAction = "act_quick_release_overswing_2h";
    private const float LethalProgressFallback = 0.49f;

    private IExecutionSceneHost _host = null!;
    private float _executionElapsed;
    private bool _actionStarted;

    public string MethodId => ExecutionMethodRules.Beheading;

    public bool UsesStagedExecution => false;

    public bool RequiresExecutionAxe => true;

    public bool DefersVictimDeathToMissionExit => false;

    public void AttachHost(IExecutionSceneHost host)
    {
        _host = host ?? throw new System.ArgumentNullException(nameof(host));
    }

    public void SetupSceneVisuals()
    {
        // The execution block is already placed by the shared visual profile.
    }

    public void TickSceneVisuals(float dt)
    {
        // Beheading has no continuously tracked scene visual.
    }

    public void PrefetchActions()
    {
        // Axe idle/action are warmed up by the shared ceremony preparation.
    }

    public void StartExecutionAction()
    {
        if (_actionStarted)
        {
            return;
        }

        _actionStarted = true;
        var actionAgent = _host.GetActiveExecutionActor();
        if (actionAgent is null || !actionAgent.IsActive())
        {
            return;
        }

        var primary = _host.GetCeremonyExecutionActionName();
        if (_host.TryStartExecutionActionClip(actionAgent, primary, isGenericFallback: false))
        {
            return;
        }

        if (_host.TryStartExecutionActionClip(actionAgent, TwoHandedBeheadingFallback, isGenericFallback: false) ||
            _host.TryStartExecutionActionClip(actionAgent, OneHandedBeheadingFallback, isGenericFallback: false) ||
            _host.TryStartExecutionActionClip(actionAgent, GenericFallbackAction, isGenericFallback: true))
        {
            return;
        }

        _host.LogWarning(
            $"Beheading action '{primary}' and all fallbacks could not start; continuing to the lethal flow.");
        _host.RequestLethalFrame();
    }

    public void TickExecution(float dt)
    {
        if (_host.LethalAttempted)
        {
            return;
        }

        var safeDt = MathF.Max(0f, dt);
        _executionElapsed += safeDt;
        var actionAgent = _host.GetActiveExecutionActor();
        var progress = 0f;
        if (actionAgent is not null && actionAgent.IsActive())
        {
            _host.ReassertExecutionActionRoot(actionAgent, "beheading tick");
            progress = _host.ObserveExecutionAction(actionAgent);
        }

        if (progress > 0.01f)
        {
            _host.MaximumExecutionActionProgress = MathF.Max(
                _host.MaximumExecutionActionProgress,
                progress);
        }

        var lethal = _host.SceneAct?.LethalProgress ?? LethalProgressFallback;
        if (progress >= lethal || _host.MaximumExecutionActionProgress >= lethal)
        {
            _host.LogInfo(
                $"Beheading reached the lethal frame (progress={MathF.Max(progress, _host.MaximumExecutionActionProgress):0.000}).");
            _host.RequestLethalFrame();
            return;
        }

        // A missing/replaced action reports zero progress, and its actor may
        // disappear. Neither condition may suppress the bounded lethal flow.
        if (_executionElapsed >= (_host.SceneAct?.MaximumActionSeconds ?? 4.5f))
        {
            _host.LogWarning(
                "Beheading stalled before its lethal frame; continuing to the lethal flow.");
            _host.RequestLethalFrame();
        }
    }

    public void BeginMethodSequence(string trigger)
    {
        _host.LogWarning($"Beheading does not use a staged method sequence ({trigger}).");
    }

    public void TickMethodSequence(float dt)
    {
        // Beheading has no staged method sequence.
    }

    public void ApplyLethalFrameEffects()
    {
        _host.PlayBeheadingLethalEffects();
    }

    public bool ApplyDeath()
    {
        var visualActor = _host.GetActiveExecutionActor();
        var result = _host.ApplySharedBattlefieldDeath(visualActor);
        var applied = result is ExecutionVictimDeathResult.Applied or ExecutionVictimDeathResult.AlreadyApplied;
        if (!applied)
        {
            _host.LogError(
                $"Beheading death chain returned {result}; no replacement kill is attempted.");
        }

        return applied;
    }

    public void TickAfterDeath(float dt)
    {
        // Native corpse pool handles the beheaded body; nothing to pin.
    }

    public void Cleanup()
    {
        _host.RestoreDetachedHeadSourceIfUncommitted();
    }
}
