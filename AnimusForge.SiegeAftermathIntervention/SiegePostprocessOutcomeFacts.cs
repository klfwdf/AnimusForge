namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free snapshot for postprocess outcome-status wording.
/// </summary>
public sealed class SiegePostprocessOutcomeFacts
{
    public SiegePostprocessOutcomeFacts(
        bool massacreStarted,
        bool plunderStarted,
        bool hasPendingAftermath,
        string pendingAftermathName)
    {
        MassacreStarted = massacreStarted;
        PlunderStarted = plunderStarted;
        HasPendingAftermath = hasPendingAftermath;
        PendingAftermathName = pendingAftermathName ?? string.Empty;
    }

    public bool MassacreStarted { get; }

    public bool PlunderStarted { get; }

    public bool HasPendingAftermath { get; }

    public string PendingAftermathName { get; }
}
