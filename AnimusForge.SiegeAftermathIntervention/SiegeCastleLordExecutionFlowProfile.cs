namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>Pure state policy for the castle captured-lord execution confirmation flow.</summary>
public static class SiegeCastleLordExecutionFlowProfile
{
    public const float NotificationOpenGraceSeconds = 3f;
    public const string RuntimeSource = "castle_lord_execution";

    public static SiegeCastleLordExecutionFlowDecision Evaluate(
        bool notificationActive,
        bool notificationSeenActive,
        bool affirmativeActionReceived,
        float elapsedSinceRequest)
    {
        if (notificationActive)
        {
            return SiegeCastleLordExecutionFlowDecision.Wait;
        }

        if (affirmativeActionReceived)
        {
            return SiegeCastleLordExecutionFlowDecision.Commit;
        }

        if (notificationSeenActive)
        {
            return SiegeCastleLordExecutionFlowDecision.Cancel;
        }

        return elapsedSinceRequest >= NotificationOpenGraceSeconds
            ? SiegeCastleLordExecutionFlowDecision.OpenFailed
            : SiegeCastleLordExecutionFlowDecision.Wait;
    }
}

public enum SiegeCastleLordExecutionFlowDecision
{
    Wait = 0,
    Commit = 1,
    Cancel = 2,
    OpenFailed = 3
}
