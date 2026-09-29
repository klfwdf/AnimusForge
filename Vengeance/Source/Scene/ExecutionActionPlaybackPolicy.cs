namespace RichExecutions.Scene;

internal static class ExecutionActionPlaybackPolicy
{
    internal const float MinimumVisibleChannelWeight = 0.01f;

    internal static bool HasConfirmedExecutionActionClear(
        bool executionActionResolved,
        bool currentMatchesExecutionAction)
    {
        return !executionActionResolved || !currentMatchesExecutionAction;
    }

    internal static bool IsPreparedActionVisiblyBound(
        bool currentMatchesPreparedAction,
        float channelWeight)
    {
        return currentMatchesPreparedAction &&
               channelWeight > MinimumVisibleChannelWeight;
    }
}
