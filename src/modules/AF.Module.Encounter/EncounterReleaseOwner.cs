using System;

namespace AnimusForge.Refactor.Modules;

internal interface IEncounterReleaseRequest
{
    object EncounterIdentity { get; }
    object PartyIdentity { get; }
    object SourceMissionIdentity { get; }
    long RuntimeGeneration { get; }
    float RequestedAt { get; }
    float LastAttemptAt { get; set; }
}

// Owns one release authorization and one delayed native-conversation request.
// All game objects are identity snapshots; native exit/release remains in host.
internal sealed class EncounterReleaseOwner<TRequest> where TRequest : class, IEncounterReleaseRequest
{
    internal TRequest Authorization { get; private set; }
    internal TRequest Pending { get; private set; }

    internal static bool IsCurrent(TRequest request, bool generationCurrent,
        object encounter, object party, object mission)
        => request != null && generationCurrent
            && ReferenceEquals(request.EncounterIdentity, encounter)
            && ReferenceEquals(request.PartyIdentity, party)
            && (mission == null || ReferenceEquals(request.SourceMissionIdentity, mission));

    internal void Authorize(TRequest request) => Authorization = request;

    internal void Schedule(TRequest request)
    {
        Pending = request;
        Authorization = request;
    }

    internal bool ConsumeAuthorization(Func<TRequest, bool> isCurrent, float now, float lifetimeSeconds)
    {
        TRequest request = Authorization;
        bool accepted = request != null && isCurrent(request)
            && now - request.RequestedAt <= lifetimeSeconds;
        Authorization = null;
        return accepted;
    }

    internal void ClearAuthorization() => Authorization = null;

    internal bool HasCurrentPending(Func<TRequest, bool> isCurrent, float now,
        float lifetimeSeconds, out string invalidReason)
    {
        invalidReason = null;
        TRequest request = Pending;
        if (request == null) return false;
        if (!isCurrent(request))
        {
            invalidReason = "context_changed";
            return false;
        }
        if (now - request.RequestedAt > lifetimeSeconds)
        {
            invalidReason = "expired";
            return false;
        }
        return true;
    }

    internal bool ClearPending()
    {
        bool revokedAuthorization = ReferenceEquals(Authorization, Pending) && Pending != null;
        if (revokedAuthorization) Authorization = null;
        Pending = null;
        return revokedAuthorization;
    }

    internal bool TryBeginPendingAttempt(float now, bool missionActive,
        bool missionPresent, float dialogDelaySeconds, float retryIntervalSeconds)
    {
        TRequest request = Pending;
        if (request == null || missionActive && !missionPresent
            || missionActive && now - request.RequestedAt < dialogDelaySeconds
            || request.LastAttemptAt >= 0f && now - request.LastAttemptAt < retryIntervalSeconds)
            return false;
        request.LastAttemptAt = now;
        return true;
    }
}
