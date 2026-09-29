using RichExecutions.Core;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace RichExecutions.Scene;

/// <summary>
/// Runtime-only native-style F-key interaction attached to the generated
/// block, gallows control or pyre. The component carries no campaign state;
/// the mission controller owns the idempotent transaction.
/// </summary>
public sealed class ExecutionUsePoint : StandingPoint
{
    private TownExecutionMissionBehavior? _controller;
    private ExecutionActor _actor;

    public ExecutionUsePoint()
    {
        IsInstantUse = true;
        SetUsableByPlayerOnly();
        LockUserFrames = false;
        LockUserPositions = false;
        AutoSheathWeapons = false;
        DescriptionMessage = TextObject.GetEmpty();
        ActionMessage = TextObject.GetEmpty();
    }

    internal void Configure(
        TownExecutionMissionBehavior controller,
        ExecutionActor actor,
        TextObject description,
        TextObject action)
    {
        _controller = controller;
        _actor = actor;
        DescriptionMessage = description;

        var keyAction = GameTexts.FindText("str_key_action");
        keyAction.SetTextVariable("KEY", GameTexts.FindText("str_ui_agent_interaction_use"));
        keyAction.SetTextVariable("ACTION", action);
        ActionMessage = keyAction;
        IsDeactivated = false;
        IsDisabledForPlayers = false;
    }

    public override TextObject GetDescriptionText(WeakGameEntity gameEntity) =>
        DescriptionMessage;

    public override bool IsDisabledForAgent(Agent agent) =>
        base.IsDisabledForAgent(agent) ||
        _controller?.IsBoundPlayer(agent) != true ||
        _controller?.CanPlayerUseExecutionPoint != true;

    public override bool IsUsableByAgent(Agent userAgent) =>
        _controller?.IsBoundPlayer(userAgent) == true &&
        _controller.CanPlayerUseExecutionPoint;

    public override void OnUse(Agent userAgent, sbyte agentBoneIndex)
    {
        if (!IsUsableByAgent(userAgent) || _controller is null)
        {
            return;
        }

        // Register the instant-use lifecycle first. Single-stage methods consume
        // the proxy permanently; wheel/impalement temporarily deactivate it and
        // their strategy repositions/reactivates it for the next stage.
        base.OnUse(userAgent, agentBoneIndex);
        if (_controller.TryUseExecutionPoint(_actor))
        {
            SetIsDeactivatedSynched(true);
            return;
        }

        userAgent.StopUsingGameObject(isSuccessful: false);
    }

    internal void UpdateStage(
        TextObject description,
        TextObject action,
        bool enabled)
    {
        DescriptionMessage = description;
        var keyAction = GameTexts.FindText("str_key_action");
        keyAction.SetTextVariable("KEY", GameTexts.FindText("str_ui_agent_interaction_use"));
        keyAction.SetTextVariable("ACTION", action);
        ActionMessage = keyAction;
        SetIsDisabledForPlayersSynched(!enabled);
        SetIsDeactivatedSynched(!enabled);
    }
}
