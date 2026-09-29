using RichExecutions.Core;

namespace RichExecutions.Scene;

internal sealed class StoningExecutionStrategy : StagedPunishmentExecutionStrategy
{
    private bool _firstThrowStarted;

    public override string MethodId => ExecutionMethodRules.Stoning;

    protected override float ReactionSeconds => 6.00f;

    protected override string SequenceDescription =>
        "first-stone throw followed by a six-second randomized crowd barrage";

    protected override bool PlayVictimReactionAtSequenceStart => false;

    public override void StartExecutionAction()
    {
        if (_firstThrowStarted)
        {
            return;
        }

        _firstThrowStarted = true;
        Host.ActionStarted = true;
        var thrower = Host.GetActiveExecutionActor();
        if (thrower is null || !thrower.IsActive())
        {
            Host.LogError(
                "The first-stone thrower was unavailable; the six-second public barrage still begins.");
            BeginMethodSequence("missing first-stone thrower");
            return;
        }

        var preservePlayerControl = ReferenceEquals(thrower, Host.PlayerAgent);
        if (!Host.TryStartStoningFirstThrow(thrower, preservePlayerControl))
        {
            Host.LogWarning(
                "The first stone could not be launched; executioner and crowd throws continue without cancelling.");
        }

        BeginMethodSequence(
            preservePlayerControl
                ? "player first stone from the unchanged free-movement position"
                : "executioner first stone from the authored blue-box standby mark");
    }
}
