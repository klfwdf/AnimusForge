using System;
using System.Collections.Generic;

namespace AnimusForge.SiegeAftermathIntervention;

public enum SiegeNpcResponseEventOrigin
{
    DirectPlayerReply = 0,
    PlayerUtterance = 1,
    SemanticAction = 2,
    PlayerAttack = 3,
    NpcGeneratedResponse = 4
}

public enum SiegeNpcResponseDecisionReason
{
    Allowed = 0,
    InactiveScene = 1,
    InvalidEvent = 2,
    InvalidSpeaker = 3,
    NoAvailableSpeakers = 4,
    GeneratedResponseBlocked = 5,
    DuplicateSpeaker = 6,
    EventLimitReached = 7,
    QueueCapacityReached = 8
}

public sealed class SiegeNpcResponseDecision
{
    public SiegeNpcResponseDecision(
        bool allowed,
        SiegeNpcResponseDecisionReason reason,
        int configuredLimit,
        int allowedCount,
        int claimedCount)
    {
        Allowed = allowed;
        Reason = reason;
        ConfiguredLimit = configuredLimit;
        AllowedCount = allowedCount;
        ClaimedCount = claimedCount;
    }

    public bool Allowed { get; }

    public SiegeNpcResponseDecisionReason Reason { get; }

    public int ConfiguredLimit { get; }

    public int AllowedCount { get; }

    public int ClaimedCount { get; }

    public int RemainingCount => Math.Max(0, AllowedCount - ClaimedCount);
}

public sealed class SiegeNpcResponseEventBudget
{
    private sealed class EventState
    {
        public EventState(int allowedCount)
        {
            AllowedCount = allowedCount;
        }

        public int AllowedCount { get; set; }

        public int ClaimedCount { get; set; }

        public HashSet<string> SpeakerIds { get; } = new HashSet<string>(StringComparer.Ordinal);
    }

    public const int MaxTrackedEvents = 64;

    public const int MaxPendingRequests = 32;

    private readonly Dictionary<string, EventState> _events = new Dictionary<string, EventState>(StringComparer.Ordinal);
    private readonly Queue<string> _eventOrder = new Queue<string>();
    private bool _sceneActive;

    public bool IsSceneActive => _sceneActive;

    public int TrackedEventCount => _events.Count;

    public void BeginScene()
    {
        Reset();
        _sceneActive = true;
    }

    public void EndScene()
    {
        Reset();
        _sceneActive = false;
    }

    public SiegeNpcResponseDecision TryClaim(
        string eventId,
        string speakerId,
        SiegeNpcResponseEventOrigin origin,
        bool unlimited,
        int configuredLimit,
        int availableSpeakerCount,
        int pendingRequestCount)
    {
        int safeConfiguredLimit = SiegeNpcResponseLimitProfile.ClampResponseLimit(configuredLimit);
        if (!_sceneActive)
        {
            return Reject(SiegeNpcResponseDecisionReason.InactiveScene, safeConfiguredLimit);
        }
        if (string.IsNullOrWhiteSpace(eventId))
        {
            return Reject(SiegeNpcResponseDecisionReason.InvalidEvent, safeConfiguredLimit);
        }
        if (string.IsNullOrWhiteSpace(speakerId))
        {
            return Reject(SiegeNpcResponseDecisionReason.InvalidSpeaker, safeConfiguredLimit);
        }
        if (origin == SiegeNpcResponseEventOrigin.NpcGeneratedResponse)
        {
            return Reject(SiegeNpcResponseDecisionReason.GeneratedResponseBlocked, safeConfiguredLimit);
        }

        if (Math.Max(0, pendingRequestCount) >= MaxPendingRequests)
        {
            return Reject(SiegeNpcResponseDecisionReason.QueueCapacityReached, safeConfiguredLimit);
        }

        int safeAvailableCount = Math.Max(0, availableSpeakerCount);
        string normalizedEventId = eventId.Trim();
        string normalizedSpeakerId = speakerId.Trim();
        int allowedCount = Math.Min(
            MaxPendingRequests,
            SiegeNpcResponseLimitProfile.ResolveAllowedResponseCount(unlimited, safeConfiguredLimit, safeAvailableCount));
        EventState state = GetOrCreateEvent(normalizedEventId, allowedCount);
        state.AllowedCount = Math.Max(state.AllowedCount, allowedCount);
        if (state.SpeakerIds.Contains(normalizedSpeakerId))
        {
            return new SiegeNpcResponseDecision(false, SiegeNpcResponseDecisionReason.DuplicateSpeaker, safeConfiguredLimit, state.AllowedCount, state.ClaimedCount);
        }
        if (origin == SiegeNpcResponseEventOrigin.DirectPlayerReply)
        {
            state.SpeakerIds.Add(normalizedSpeakerId);
            return new SiegeNpcResponseDecision(true, SiegeNpcResponseDecisionReason.Allowed, safeConfiguredLimit, state.AllowedCount, state.ClaimedCount);
        }
        if (safeAvailableCount == 0)
        {
            return new SiegeNpcResponseDecision(false, SiegeNpcResponseDecisionReason.NoAvailableSpeakers, safeConfiguredLimit, state.AllowedCount, state.ClaimedCount);
        }
        if (state.ClaimedCount >= state.AllowedCount)
        {
            return new SiegeNpcResponseDecision(false, SiegeNpcResponseDecisionReason.EventLimitReached, safeConfiguredLimit, state.AllowedCount, state.ClaimedCount);
        }

        state.SpeakerIds.Add(normalizedSpeakerId);
        state.ClaimedCount++;
        return new SiegeNpcResponseDecision(true, SiegeNpcResponseDecisionReason.Allowed, safeConfiguredLimit, state.AllowedCount, state.ClaimedCount);
    }

    private EventState GetOrCreateEvent(string eventId, int allowedCount)
    {
        if (_events.TryGetValue(eventId, out EventState existing))
        {
            return existing;
        }

        while (_events.Count >= MaxTrackedEvents && _eventOrder.Count > 0)
        {
            string oldestEventId = _eventOrder.Dequeue();
            _events.Remove(oldestEventId);
        }

        EventState created = new EventState(allowedCount);
        _events[eventId] = created;
        _eventOrder.Enqueue(eventId);
        return created;
    }

    private static SiegeNpcResponseDecision Reject(SiegeNpcResponseDecisionReason reason, int configuredLimit)
    {
        return new SiegeNpcResponseDecision(false, reason, configuredLimit, 0, 0);
    }

    private void Reset()
    {
        _events.Clear();
        _eventOrder.Clear();
    }
}
