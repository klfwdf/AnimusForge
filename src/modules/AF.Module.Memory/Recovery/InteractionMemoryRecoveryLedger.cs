using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace AnimusForge.Refactor.Runtime;

/// <summary>
/// Save-friendly, action-free journal for repairing only the visible memory
/// projection of an interaction. The raw commit id is used to derive an opaque
/// key and is never retained or exported.
/// </summary>
internal sealed class InteractionMemoryRecoveryLedger
{
    internal const int CurrentSchemaVersion = 1;
    internal const int MaximumPendingEntries = 64;
    internal const int MaximumCompletedEntries = 512;
    internal const int MaximumQuarantineEntries = 64;
    internal const int MaximumComponentCount = 3;
    internal const int MaximumTextLength = 32768;
    internal const int MaximumAggregateTextBytes = 196608;
    internal const int MaximumSerializedValueLength = 393216;
    internal const int MaximumQuarantinePreservedValueLength = 8192;
    internal const int MaximumQuarantineDiagnosticPrefixLength = 1024;
    internal const int MaximumAttemptsPerStep = 5;

    private const string WirePrefix = "AFMR1:";
    private const string DisabledStorageKey = "!disabled";
    private const string RecoveryNamespace = "AnimusForge.MemoryRecovery.v1";
    private readonly Dictionary<string, InteractionMemoryRecoveryEntry> _entries =
        new Dictionary<string, InteractionMemoryRecoveryEntry>(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _quarantine =
        new Dictionary<string, string>(StringComparer.Ordinal);
    private readonly HashSet<string> _blockedRecoveryIds = new HashSet<string>(StringComparer.Ordinal);
    private readonly Queue<InteractionMemoryRecoveryEviction> _markerEvictions =
        new Queue<InteractionMemoryRecoveryEviction>();
    private bool _disabled;
    private string _disabledReason = string.Empty;

    internal int PendingCount => _entries.Values.Count(entry => entry.Lifecycle == InteractionMemoryRecoveryLifecycle.Pending);
    internal int CompletedCount => _entries.Values.Count(entry => entry.Lifecycle == InteractionMemoryRecoveryLifecycle.Completed);
    internal int QuarantineCount => _quarantine.Count;
    internal bool IsDisabled => _disabled;
    internal bool HasPendingWork => !_disabled && _entries.Values.Any(HasRunnableStep);
    internal bool HasUnresolvedWork => !_disabled && _entries.Values.Any(entry => entry.Lifecycle == InteractionMemoryRecoveryLifecycle.Pending
        && entry.Components.Any(component => component.DailyState == InteractionMemoryRecoveryStepState.Unknown
            || component.RecentState == InteractionMemoryRecoveryStepState.Unknown));

    internal InteractionMemoryRecoveryBeginStatus Begin(
        InteractionMemoryRecoverySeed seed,
        out string recoveryId,
        out string errorCode)
    {
        recoveryId = string.Empty;
        errorCode = string.Empty;
        if (_disabled)
        {
            errorCode = string.IsNullOrWhiteSpace(_disabledReason)
                ? "memory_recovery_disabled"
                : _disabledReason;
            return InteractionMemoryRecoveryBeginStatus.Rejected;
        }
        if (!TryBuildRecoveryIdentityCore(
            seed,
            out InteractionMemoryRecoverySeed normalized,
            out recoveryId,
            out string payloadHash,
            out errorCode))
        {
            return InteractionMemoryRecoveryBeginStatus.Rejected;
        }
        if (_blockedRecoveryIds.Contains(recoveryId))
        {
            errorCode = "memory_recovery_quarantined";
            return InteractionMemoryRecoveryBeginStatus.Rejected;
        }
        if (_entries.TryGetValue(recoveryId, out InteractionMemoryRecoveryEntry existing))
        {
            if (!string.Equals(existing.PayloadHash, payloadHash, StringComparison.Ordinal))
            {
                errorCode = "memory_recovery_payload_conflict";
                return InteractionMemoryRecoveryBeginStatus.Conflict;
            }
            if (existing.Lifecycle == InteractionMemoryRecoveryLifecycle.Completed)
            {
                return InteractionMemoryRecoveryBeginStatus.DuplicateCompleted;
            }
            errorCode = "memory_recovery_already_pending";
            return InteractionMemoryRecoveryBeginStatus.ExistingPending;
        }

        if (PendingCount >= MaximumPendingEntries)
        {
            errorCode = "memory_recovery_capacity_exceeded";
            return InteractionMemoryRecoveryBeginStatus.CapacityExceeded;
        }

        var entry = new InteractionMemoryRecoveryEntry
        {
            SchemaVersion = CurrentSchemaVersion,
            RecoveryId = recoveryId,
            PayloadHash = payloadHash,
            Lifecycle = InteractionMemoryRecoveryLifecycle.Pending,
            CreatedUtcTicks = DateTime.UtcNow.Ticks,
            LastAttemptUtcTicks = 0L,
            LastErrorCode = string.Empty,
            Channel = normalized.Channel,
            SessionId = normalized.SessionId,
            OriginalSubjectId = normalized.SubjectId,
            SubjectId = normalized.SubjectId,
            IsNonHero = normalized.IsNonHero,
            NpcName = normalized.NpcName,
            RuntimeGeneration = normalized.RuntimeGeneration,
            SaveGeneration = normalized.SaveGeneration,
            TraceId = normalized.TraceId,
            OriginGameDay = normalized.OriginGameDay,
            OriginGameDate = normalized.OriginGameDate,
            OriginGameHour = normalized.OriginGameHour,
            OriginScene = normalized.OriginScene,
            DailyStorageDay = normalized.DailyStorageDay,
            DailyStorageDate = normalized.DailyStorageDate,
            SceneSessionId = normalized.SceneSessionId,
            DialogueSessionId = normalized.DialogueSessionId,
            MemorySessionKey = normalized.MemorySessionKey,
            TargetAgentIndex = normalized.TargetAgentIndex,
            TargetName = normalized.TargetName,
            Components = normalized.Components.Select(component => new InteractionMemoryRecoveryComponent
            {
                Part = component.Part,
                DailySpeaker = component.DailySpeaker,
                DailyText = component.DailyText,
                RecentText = component.RecentText,
                IsAfef = component.IsAfef,
                IsLlmDialogue = component.IsLlmDialogue,
                DailyState = string.IsNullOrWhiteSpace(component.DailyText)
                    ? InteractionMemoryRecoveryStepState.NotApplicable
                    : InteractionMemoryRecoveryStepState.Pending,
                RecentState = string.IsNullOrWhiteSpace(component.RecentText)
                    ? InteractionMemoryRecoveryStepState.NotApplicable
                    : InteractionMemoryRecoveryStepState.Pending
            }).ToList()
        };
        entry.ExpectedMarkerMask = ComputeExpectedMarkerMask(entry.Components);
        _entries.Add(recoveryId, entry);
        CompleteIfTerminal(entry);
        return InteractionMemoryRecoveryBeginStatus.Began;
    }

    internal bool TryGetNextWork(out InteractionMemoryRecoveryWorkItem work)
        => TryGetNextWorkFor(string.Empty, out work);

    internal bool TryGetNextWorkFor(string recoveryId, out InteractionMemoryRecoveryWorkItem work)
    {
        work = null;
        if (_disabled)
        {
            return false;
        }
        foreach (InteractionMemoryRecoveryEntry entry in OrderedPendingEntries())
        {
            if (entry.Components.Any(component => component.DailyState == InteractionMemoryRecoveryStepState.Unknown
                || component.RecentState == InteractionMemoryRecoveryStepState.Unknown))
            {
                continue;
            }
            if (!string.IsNullOrEmpty(recoveryId)
                && !string.Equals(entry.RecoveryId, recoveryId, StringComparison.Ordinal))
            {
                continue;
            }
            foreach (InteractionMemoryRecoveryComponent component in entry.Components)
            {
                if (component.DailyState == InteractionMemoryRecoveryStepState.Pending
                    && component.DailyAttempts < MaximumAttemptsPerStep)
                {
                    component.DailyState = InteractionMemoryRecoveryStepState.Started;
                    work = BuildWork(entry, component, InteractionMemoryRecoveryTarget.Daily,
                        InteractionMemoryRecoveryStepState.Started);
                    return true;
                }
            }
            foreach (InteractionMemoryRecoveryComponent component in entry.Components)
            {
                if (component.RecentState == InteractionMemoryRecoveryStepState.Pending
                    && component.RecentAttempts < MaximumAttemptsPerStep)
                {
                    component.RecentState = InteractionMemoryRecoveryStepState.Started;
                    work = BuildWork(entry, component, InteractionMemoryRecoveryTarget.Recent,
                        InteractionMemoryRecoveryStepState.Started);
                    return true;
                }
            }
        }
        return false;
    }

    internal IReadOnlyList<InteractionMemoryRecoveryWorkItem> GetUnresolvedWork()
    {
        var result = new List<InteractionMemoryRecoveryWorkItem>();
        if (_disabled)
        {
            return result;
        }
        foreach (InteractionMemoryRecoveryEntry entry in OrderedPendingEntries())
        {
            foreach (InteractionMemoryRecoveryComponent component in entry.Components)
            {
                if (component.DailyState == InteractionMemoryRecoveryStepState.Unknown)
                {
                    result.Add(BuildWork(entry, component, InteractionMemoryRecoveryTarget.Daily,
                        InteractionMemoryRecoveryStepState.Unknown));
                }
                if (component.RecentState == InteractionMemoryRecoveryStepState.Unknown)
                {
                    result.Add(BuildWork(entry, component, InteractionMemoryRecoveryTarget.Recent,
                        InteractionMemoryRecoveryStepState.Unknown));
                }
            }
        }
        return result;
    }

    internal bool MarkApplied(InteractionMemoryRecoveryWorkItem work)
    {
        if (!TryResolveWork(work, out InteractionMemoryRecoveryEntry entry,
            out InteractionMemoryRecoveryComponent component))
        {
            return false;
        }
        SetState(component, work.Target, InteractionMemoryRecoveryStepState.Applied);
        CompleteIfTerminal(entry);
        return true;
    }

    internal bool MarkPending(InteractionMemoryRecoveryWorkItem work)
    {
        if (!TryResolveWork(work, out _, out InteractionMemoryRecoveryComponent component))
        {
            return false;
        }
        SetState(component, work.Target, InteractionMemoryRecoveryStepState.Pending);
        return true;
    }

    internal bool MarkUnknown(InteractionMemoryRecoveryWorkItem work)
    {
        if (!TryResolveWork(work, out _, out InteractionMemoryRecoveryComponent component))
        {
            return false;
        }
        SetState(component, work.Target, InteractionMemoryRecoveryStepState.Unknown);
        return true;
    }

    internal bool RegisterRetry(
        InteractionMemoryRecoveryWorkItem work,
        string errorCode,
        out bool exhausted)
    {
        exhausted = false;
        if (!TryResolveWork(work, out InteractionMemoryRecoveryEntry entry,
            out InteractionMemoryRecoveryComponent component))
        {
            return false;
        }
        SetState(component, work.Target, InteractionMemoryRecoveryStepState.Pending);
        int attempts;
        if (work.Target == InteractionMemoryRecoveryTarget.Daily)
        {
            component.DailyAttempts = Math.Min(MaximumAttemptsPerStep, component.DailyAttempts + 1);
            attempts = component.DailyAttempts;
        }
        else
        {
            component.RecentAttempts = Math.Min(MaximumAttemptsPerStep, component.RecentAttempts + 1);
            attempts = component.RecentAttempts;
        }
        entry.LastAttemptUtcTicks = DateTime.UtcNow.Ticks;
        string normalizedError = (errorCode ?? string.Empty).Replace("\r", " ").Replace("\n", " ").Trim();
        entry.LastErrorCode = normalizedError.Length <= 128
            ? normalizedError
            : normalizedError.Substring(0, 128);
        exhausted = attempts >= MaximumAttemptsPerStep;
        return true;
    }

    internal bool QuarantineEntry(string recoveryId, string reason)
    {
        if (string.IsNullOrWhiteSpace(recoveryId) || !_entries.TryGetValue(recoveryId, out InteractionMemoryRecoveryEntry entry))
        {
            return false;
        }
        string raw;
        try
        {
            raw = Serialize(entry);
        }
        catch
        {
            raw = WirePrefix + Convert.ToBase64String(Encoding.UTF8.GetBytes("unserializable"));
        }
        _entries.Remove(recoveryId);
        _markerEvictions.Enqueue(new InteractionMemoryRecoveryEviction
        {
            RecoveryId = entry.RecoveryId,
            SubjectId = entry.SubjectId
        });
        AddQuarantine(recoveryId, raw, reason);
        return true;
    }

    internal bool IsCompleted(string recoveryId)
        => !string.IsNullOrWhiteSpace(recoveryId)
            && _entries.TryGetValue(recoveryId, out InteractionMemoryRecoveryEntry entry)
            && entry.Lifecycle == InteractionMemoryRecoveryLifecycle.Completed;

    internal static bool TryBuildRecoveryId(string commitId, out string recoveryId)
    {
        recoveryId = string.Empty;
        string normalized = (commitId ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length > 512)
        {
            return false;
        }
        recoveryId = Hash(writer =>
        {
            writer.Write(RecoveryNamespace);
            writer.Write(normalized);
        });
        return true;
    }

    internal static bool TryBuildRecoveryIdentity(
        InteractionMemoryRecoverySeed seed,
        out string recoveryId,
        out string payloadHash,
        out string errorCode)
        => TryBuildRecoveryIdentityCore(
            seed,
            out _,
            out recoveryId,
            out payloadHash,
            out errorCode);

    private static bool TryBuildRecoveryIdentityCore(
        InteractionMemoryRecoverySeed seed,
        out InteractionMemoryRecoverySeed normalized,
        out string recoveryId,
        out string payloadHash,
        out string errorCode)
    {
        recoveryId = string.Empty;
        payloadHash = string.Empty;
        if (!TryNormalizeSeed(seed, out normalized, out errorCode))
        {
            return false;
        }
        if (!TryBuildRecoveryId(normalized.CommitId, out recoveryId))
        {
            errorCode = "memory_recovery_commit_id_invalid";
            return false;
        }
        payloadHash = ComputePayloadHash(normalized);
        return true;
    }

    internal InteractionMemoryRecoveryLookupStatus GetLookupStatus(
        string recoveryId,
        string expectedSubjectId,
        string expectedPayloadHash)
    {
        string key = (recoveryId ?? string.Empty).Trim();
        string subject = (expectedSubjectId ?? string.Empty).Trim();
        string payloadHash = (expectedPayloadHash ?? string.Empty).Trim();
        if (!IsHexDigest(key) || !IsHexDigest(payloadHash)
            || string.IsNullOrWhiteSpace(subject) || subject.Length > 512)
        {
            return InteractionMemoryRecoveryLookupStatus.Invalid;
        }
        if (_disabled)
        {
            return InteractionMemoryRecoveryLookupStatus.Disabled;
        }
        if (_blockedRecoveryIds.Contains(key))
        {
            return InteractionMemoryRecoveryLookupStatus.Quarantined;
        }
        if (!_entries.TryGetValue(key, out InteractionMemoryRecoveryEntry entry))
        {
            return InteractionMemoryRecoveryLookupStatus.Missing;
        }
        if (!string.Equals(entry.OriginalSubjectId, subject, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(entry.SubjectId, subject, StringComparison.OrdinalIgnoreCase))
        {
            return InteractionMemoryRecoveryLookupStatus.SubjectMismatch;
        }
        if (!string.Equals(entry.PayloadHash, payloadHash, StringComparison.Ordinal))
        {
            return InteractionMemoryRecoveryLookupStatus.PayloadMismatch;
        }
        return entry.Lifecycle == InteractionMemoryRecoveryLifecycle.Completed
            ? InteractionMemoryRecoveryLookupStatus.Completed
            : entry.Lifecycle == InteractionMemoryRecoveryLifecycle.Pending
                ? InteractionMemoryRecoveryLookupStatus.Pending
                : InteractionMemoryRecoveryLookupStatus.Invalid;
    }

    internal void DisableForCurrentCampaign(string reason)
        => Disable(reason);

    internal IReadOnlyList<InteractionMemoryRecoveryEviction> DrainMarkerEvictions()
    {
        var evicted = new List<InteractionMemoryRecoveryEviction>();
        while (_markerEvictions.Count > 0)
        {
            evicted.Add(_markerEvictions.Dequeue());
        }
        return evicted;
    }

    internal IReadOnlyList<InteractionMemoryRecoveryRetention> GetRetainedEntries()
        => _entries.Values.Select(entry => new InteractionMemoryRecoveryRetention
        {
            RecoveryId = entry.RecoveryId,
            PayloadHash = entry.PayloadHash,
            SubjectId = entry.SubjectId,
            IsPending = entry.Lifecycle == InteractionMemoryRecoveryLifecycle.Pending,
            ExpectedMarkerMask = entry.ExpectedMarkerMask,
            AppliedDailyMarkerMask = (entry.Lifecycle == InteractionMemoryRecoveryLifecycle.Completed
                ? entry.ExpectedMarkerMask
                : ComputeAppliedMarkerMask(entry.Components)) & 0x15,
            AppliedRecentMarkerMask = (entry.Lifecycle == InteractionMemoryRecoveryLifecycle.Completed
                ? entry.ExpectedMarkerMask
                : ComputeAppliedMarkerMask(entry.Components)) & 0x2A,
            DailyStorageDay = entry.DailyStorageDay,
            DailyStorageDate = entry.DailyStorageDate
        }).ToList();

    internal bool RecordDailyStorage(
        InteractionMemoryRecoveryWorkItem work,
        int storageDay,
        string storageDate)
    {
        if (!TryResolveWork(work, out InteractionMemoryRecoveryEntry entry, out _)
            || storageDay < 0 || (storageDate ?? string.Empty).Trim().Length > 256)
        {
            return false;
        }
        entry.DailyStorageDay = storageDay;
        entry.DailyStorageDate = (storageDate ?? string.Empty).Trim();
        return true;
    }

    internal int RetargetProjectionSubject(string sourceSubjectId, string targetSubjectId)
    {
        string source = (sourceSubjectId ?? string.Empty).Trim();
        string target = (targetSubjectId ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target)
            || source.Length > 512 || target.Length > 512)
        {
            return 0;
        }
        int changed = 0;
        foreach (InteractionMemoryRecoveryEntry entry in _entries.Values)
        {
            if (string.Equals(entry.SubjectId, source, StringComparison.OrdinalIgnoreCase))
            {
                entry.SubjectId = target;
                changed++;
            }
        }
        return changed;
    }

    internal int QuarantineProjectionSubject(string subjectId, string reason)
    {
        string normalized = (subjectId ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return 0;
        }
        List<string> recoveryIds = _entries.Values
            .Where(entry => string.Equals(entry.SubjectId, normalized, StringComparison.OrdinalIgnoreCase))
            .Select(entry => entry.RecoveryId)
            .ToList();
        foreach (string recoveryId in recoveryIds)
        {
            QuarantineEntry(recoveryId, reason);
        }
        return recoveryIds.Count;
    }

    internal Dictionary<string, string> Export()
    {
        var exported = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (InteractionMemoryRecoveryEntry entry in _entries.Values
            .OrderBy(item => item.CreatedUtcTicks)
            .ThenBy(item => item.RecoveryId, StringComparer.Ordinal))
        {
            exported[entry.RecoveryId] = Serialize(entry);
        }
        foreach (KeyValuePair<string, string> quarantined in _quarantine.OrderBy(item => item.Key, StringComparer.Ordinal))
        {
            if (!exported.ContainsKey(quarantined.Key))
            {
                exported[quarantined.Key] = quarantined.Value;
            }
        }
        if (_disabled)
        {
            exported[DisabledStorageKey] = string.IsNullOrWhiteSpace(_disabledReason)
                ? "memory_recovery_disabled"
                : _disabledReason;
        }
        return exported;
    }

    internal void Import(IDictionary<string, string> storage)
    {
        _entries.Clear();
        _quarantine.Clear();
        _blockedRecoveryIds.Clear();
        _markerEvictions.Clear();
        _disabled = false;
        _disabledReason = string.Empty;
        if (storage == null)
        {
            return;
        }
        int maximumStoredEntries = MaximumPendingEntries + MaximumCompletedEntries + MaximumQuarantineEntries;
        int storedRecordCount = storage.Keys.Count(key => !string.Equals(key, DisabledStorageKey, StringComparison.Ordinal));
        if (storedRecordCount > maximumStoredEntries)
        {
            Disable("memory_recovery_storage_overflow");
        }
        IEnumerable<KeyValuePair<string, string>> orderedStorage = storage
            .Where(pair => string.Equals(pair.Key, DisabledStorageKey, StringComparison.Ordinal))
            .Concat(storage
                .Where(pair => !string.Equals(pair.Key, DisabledStorageKey, StringComparison.Ordinal))
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Take(maximumStoredEntries));
        foreach (KeyValuePair<string, string> item in orderedStorage)
        {
            string key = item.Key ?? string.Empty;
            string value = item.Value ?? string.Empty;
            if (string.Equals(key, DisabledStorageKey, StringComparison.Ordinal))
            {
                Disable(string.IsNullOrWhiteSpace(value) || value.Length > 128
                    ? "memory_recovery_disabled_sentinel_invalid"
                    : value);
                continue;
            }
            if (!TryDeserialize(key, value, out InteractionMemoryRecoveryEntry entry, out string errorCode))
            {
                AddQuarantine(key, value, errorCode);
                continue;
            }
            if (_entries.ContainsKey(entry.RecoveryId))
            {
                AddQuarantine(key, value, "memory_recovery_duplicate_storage_key");
                continue;
            }
            if (entry.Lifecycle == InteractionMemoryRecoveryLifecycle.Pending)
            {
                foreach (InteractionMemoryRecoveryComponent component in entry.Components)
                {
                    if (component.DailyState == InteractionMemoryRecoveryStepState.Started)
                    {
                        component.DailyState = InteractionMemoryRecoveryStepState.Unknown;
                    }
                    if (component.RecentState == InteractionMemoryRecoveryStepState.Started)
                    {
                        component.RecentState = InteractionMemoryRecoveryStepState.Unknown;
                    }
                }
                if (HasExhaustedStep(entry))
                {
                    AddQuarantine(key, value, "memory_recovery_retry_exhausted");
                    continue;
                }
                if (PendingCount >= MaximumPendingEntries)
                {
                    AddQuarantine(key, value, "memory_recovery_pending_overflow");
                    continue;
                }
            }
            _entries.Add(entry.RecoveryId, entry);
        }
        foreach (string blockedRecoveryId in _blockedRecoveryIds)
        {
            _entries.Remove(blockedRecoveryId);
        }
        TrimCompletedTombstones();
    }

    private IEnumerable<InteractionMemoryRecoveryEntry> OrderedPendingEntries()
        => _entries.Values
            .Where(entry => entry.Lifecycle == InteractionMemoryRecoveryLifecycle.Pending)
            .OrderBy(entry => entry.LastAttemptUtcTicks)
            .ThenBy(entry => entry.CreatedUtcTicks)
            .ThenBy(entry => entry.RecoveryId, StringComparer.Ordinal);

    private static bool HasRunnableStep(InteractionMemoryRecoveryEntry entry)
        => entry != null && entry.Lifecycle == InteractionMemoryRecoveryLifecycle.Pending
            && !entry.Components.Any(component => component.DailyState == InteractionMemoryRecoveryStepState.Unknown
                || component.RecentState == InteractionMemoryRecoveryStepState.Unknown)
            && entry.Components.Any(component =>
                (component.DailyState == InteractionMemoryRecoveryStepState.Pending
                    && component.DailyAttempts < MaximumAttemptsPerStep)
                || (component.RecentState == InteractionMemoryRecoveryStepState.Pending
                    && component.RecentAttempts < MaximumAttemptsPerStep));

    private static InteractionMemoryRecoveryWorkItem BuildWork(
        InteractionMemoryRecoveryEntry entry,
        InteractionMemoryRecoveryComponent component,
        InteractionMemoryRecoveryTarget target,
        InteractionMemoryRecoveryStepState state)
        => new InteractionMemoryRecoveryWorkItem
        {
            RecoveryId = entry.RecoveryId,
            PayloadHash = entry.PayloadHash,
            Channel = entry.Channel,
            SessionId = entry.SessionId,
            SubjectId = entry.SubjectId,
            IsNonHero = entry.IsNonHero,
            NpcName = entry.NpcName,
            RuntimeGeneration = entry.RuntimeGeneration,
            SaveGeneration = entry.SaveGeneration,
            TraceId = entry.TraceId,
            OriginGameDay = entry.OriginGameDay,
            OriginGameDate = entry.OriginGameDate,
            OriginGameHour = entry.OriginGameHour,
            OriginScene = entry.OriginScene,
            DailyStorageDay = entry.DailyStorageDay,
            DailyStorageDate = entry.DailyStorageDate,
            SceneSessionId = entry.SceneSessionId,
            DialogueSessionId = entry.DialogueSessionId,
            MemorySessionKey = entry.MemorySessionKey,
            TargetAgentIndex = entry.TargetAgentIndex,
            TargetName = entry.TargetName,
            Part = component.Part,
            DailySpeaker = component.DailySpeaker,
            DailyText = component.DailyText,
            RecentText = component.RecentText,
            IsAfef = component.IsAfef,
            IsLlmDialogue = component.IsLlmDialogue,
            Target = target,
            State = state,
            AttemptCount = target == InteractionMemoryRecoveryTarget.Daily
                ? component.DailyAttempts
                : component.RecentAttempts
        };

    private bool TryResolveWork(
        InteractionMemoryRecoveryWorkItem work,
        out InteractionMemoryRecoveryEntry entry,
        out InteractionMemoryRecoveryComponent component)
    {
        entry = null;
        component = null;
        if (work == null || string.IsNullOrWhiteSpace(work.RecoveryId)
            || !_entries.TryGetValue(work.RecoveryId, out entry)
            || entry.Lifecycle != InteractionMemoryRecoveryLifecycle.Pending
            || !string.Equals(entry.PayloadHash, work.PayloadHash, StringComparison.Ordinal))
        {
            return false;
        }
        component = entry.Components.FirstOrDefault(item => string.Equals(item.Part, work.Part, StringComparison.Ordinal));
        return component != null;
    }

    private static void SetState(
        InteractionMemoryRecoveryComponent component,
        InteractionMemoryRecoveryTarget target,
        InteractionMemoryRecoveryStepState state)
    {
        if (target == InteractionMemoryRecoveryTarget.Daily)
        {
            component.DailyState = state;
        }
        else
        {
            component.RecentState = state;
        }
    }

    private void CompleteIfTerminal(InteractionMemoryRecoveryEntry entry)
    {
        if (entry == null || entry.Lifecycle != InteractionMemoryRecoveryLifecycle.Pending
            || entry.Components.Any(component => !IsTerminal(component.DailyState) || !IsTerminal(component.RecentState)))
        {
            return;
        }
        entry.Lifecycle = InteractionMemoryRecoveryLifecycle.Completed;
        entry.CompletedUtcTicks = DateTime.UtcNow.Ticks;
        entry.ExpectedMarkerMask = ComputeExpectedMarkerMask(entry.Components);
        entry.ClearPayload();
        TrimCompletedTombstones();
    }

    private static bool IsTerminal(InteractionMemoryRecoveryStepState state)
        => state == InteractionMemoryRecoveryStepState.Applied
            || state == InteractionMemoryRecoveryStepState.NotApplicable;

    private static int ComputeExpectedMarkerMask(
        IEnumerable<InteractionMemoryRecoveryComponent> components)
    {
        int mask = 0;
        foreach (InteractionMemoryRecoveryComponent component in components ?? Enumerable.Empty<InteractionMemoryRecoveryComponent>())
        {
            int offset = string.Equals(component.Part, "user", StringComparison.Ordinal) ? 0
                : string.Equals(component.Part, "fact", StringComparison.Ordinal) ? 2
                : string.Equals(component.Part, "assistant", StringComparison.Ordinal) ? 4
                : -1;
            if (offset < 0)
            {
                continue;
            }
            if (component.DailyState != InteractionMemoryRecoveryStepState.NotApplicable)
            {
                mask |= 1 << offset;
            }
            if (component.RecentState != InteractionMemoryRecoveryStepState.NotApplicable)
            {
                mask |= 1 << (offset + 1);
            }
        }
        return mask;
    }

    private static int ComputeAppliedMarkerMask(
        IEnumerable<InteractionMemoryRecoveryComponent> components)
    {
        int mask = 0;
        foreach (InteractionMemoryRecoveryComponent component in components ?? Enumerable.Empty<InteractionMemoryRecoveryComponent>())
        {
            int offset = string.Equals(component.Part, "user", StringComparison.Ordinal) ? 0
                : string.Equals(component.Part, "fact", StringComparison.Ordinal) ? 2
                : string.Equals(component.Part, "assistant", StringComparison.Ordinal) ? 4
                : -1;
            if (offset < 0)
            {
                continue;
            }
            if (component.DailyState == InteractionMemoryRecoveryStepState.Applied)
            {
                mask |= 1 << offset;
            }
            if (component.RecentState == InteractionMemoryRecoveryStepState.Applied)
            {
                mask |= 1 << (offset + 1);
            }
        }
        return mask;
    }

    private void TrimCompletedTombstones()
    {
        List<InteractionMemoryRecoveryEntry> completed = _entries.Values
            .Where(entry => entry.Lifecycle == InteractionMemoryRecoveryLifecycle.Completed)
            .OrderBy(entry => entry.CompletedUtcTicks)
            .ThenBy(entry => entry.RecoveryId, StringComparer.Ordinal)
            .ToList();
        int removeCount = Math.Max(0, completed.Count - MaximumCompletedEntries);
        for (int i = 0; i < removeCount; i++)
        {
            InteractionMemoryRecoveryEntry removed = completed[i];
            _entries.Remove(removed.RecoveryId);
            _markerEvictions.Enqueue(new InteractionMemoryRecoveryEviction
            {
                RecoveryId = removed.RecoveryId,
                SubjectId = removed.SubjectId
            });
        }
    }

    private void AddQuarantine(string sourceKey, string rawValue, string reason)
    {
        if (_quarantine.Count >= MaximumQuarantineEntries)
        {
            Disable("memory_recovery_quarantine_overflow");
            return;
        }
        string safeKey = string.IsNullOrWhiteSpace(sourceKey) ? "missing" : sourceKey.Trim();
        if (safeKey.Length > 256)
        {
            safeKey = safeKey.Substring(0, 256);
        }
        string preservedValue = rawValue ?? string.Empty;
        if (preservedValue.Length > MaximumQuarantinePreservedValueLength)
        {
            string diagnosticPrefix = preservedValue.Substring(
                0,
                Math.Min(MaximumQuarantineDiagnosticPrefixLength, preservedValue.Length));
            string diagnosticHash = Hash(writer =>
            {
                writer.Write(preservedValue.Length);
                writer.Write(diagnosticPrefix);
            });
            preservedValue = "AFMRQ1:" + (reason ?? "memory_recovery_invalid_record")
                + ":length=" + (rawValue ?? string.Empty).Length
                + ":sampleHash=" + diagnosticHash
                + ":sample=" + diagnosticPrefix;
        }
        string blockedId = ExtractRecoveryId(safeKey);
        if (!string.IsNullOrEmpty(blockedId))
        {
            _blockedRecoveryIds.Add(blockedId);
        }
        string quarantineKey = string.IsNullOrEmpty(blockedId)
            ? "q:" + Hash(writer =>
            {
                writer.Write(safeKey);
                writer.Write(reason ?? string.Empty);
                writer.Write(preservedValue);
            })
            : "q:" + blockedId;
        if (_quarantine.ContainsKey(quarantineKey))
        {
            quarantineKey += ":" + Hash(writer => writer.Write(preservedValue)).Substring(0, 16);
        }
        _quarantine[quarantineKey] = preservedValue;
    }

    private void Disable(string reason)
    {
        _disabled = true;
        string normalized = string.IsNullOrWhiteSpace(reason)
            ? "memory_recovery_disabled"
            : reason.Replace("\r", " ").Replace("\n", " ").Trim();
        _disabledReason = normalized.Length <= 128 ? normalized : normalized.Substring(0, 128);
    }

    private static string ExtractRecoveryId(string storageKey)
    {
        string candidate = (storageKey ?? string.Empty).Trim();
        if (candidate.StartsWith("q:", StringComparison.Ordinal))
        {
            candidate = candidate.Substring(2);
            int separator = candidate.IndexOf(':');
            if (separator >= 0)
            {
                candidate = candidate.Substring(0, separator);
            }
        }
        return IsHexDigest(candidate) ? candidate : string.Empty;
    }

    private static bool TryNormalizeSeed(
        InteractionMemoryRecoverySeed seed,
        out InteractionMemoryRecoverySeed normalized,
        out string errorCode)
    {
        normalized = null;
        errorCode = string.Empty;
        if (seed == null || string.IsNullOrWhiteSpace(seed.CommitId))
        {
            errorCode = "memory_recovery_missing_commit_id";
            return false;
        }
        if (seed.CommitId.Length > 512 || seed.Channel < 0 || seed.Channel > 4
            || string.IsNullOrWhiteSpace(seed.SessionId) || seed.SessionId.Length > 512
            || string.IsNullOrWhiteSpace(seed.SubjectId) || seed.SubjectId.Length > 512
            || (seed.NpcName ?? string.Empty).Trim().Length > 256
            || (seed.TraceId ?? string.Empty).Trim().Length > 512
            || (seed.OriginGameDate ?? string.Empty).Trim().Length > 256
            || (seed.OriginScene ?? string.Empty).Trim().Length > 512
            || (seed.DailyStorageDate ?? string.Empty).Trim().Length > 256
            || (seed.MemorySessionKey ?? string.Empty).Trim().Length > 512
            || (seed.TargetName ?? string.Empty).Trim().Length > 256
            || seed.Components == null || seed.Components.Count == 0
            || seed.Components.Count > MaximumComponentCount)
        {
            errorCode = "memory_recovery_invalid_identity";
            return false;
        }
        List<InteractionMemoryRecoveryComponentSeed> components = new List<InteractionMemoryRecoveryComponentSeed>();
        var seenParts = new HashSet<string>(StringComparer.Ordinal);
        int aggregateTextBytes = 0;
        bool hasPayload = false;
        foreach (InteractionMemoryRecoveryComponentSeed source in seed.Components)
        {
            string part = (source?.Part ?? string.Empty).Trim().ToLowerInvariant();
            if (source == null || (part != "user" && part != "fact" && part != "assistant") || !seenParts.Add(part))
            {
                errorCode = "memory_recovery_invalid_component";
                return false;
            }
            string dailySpeaker = NormalizeBounded(source.DailySpeaker, 256);
            string dailyText = NormalizeBounded(source.DailyText, MaximumTextLength);
            string recentText = NormalizeBounded(source.RecentText, MaximumTextLength);
            if (dailySpeaker == null || dailyText == null || recentText == null)
            {
                errorCode = "memory_recovery_component_oversize";
                return false;
            }
            aggregateTextBytes += Encoding.UTF8.GetByteCount(dailySpeaker)
                + Encoding.UTF8.GetByteCount(dailyText)
                + Encoding.UTF8.GetByteCount(recentText);
            hasPayload |= dailyText.Length > 0 || recentText.Length > 0;
            components.Add(new InteractionMemoryRecoveryComponentSeed
            {
                Part = part,
                DailySpeaker = dailySpeaker,
                DailyText = dailyText,
                RecentText = recentText,
                IsAfef = source.IsAfef,
                IsLlmDialogue = source.IsLlmDialogue && !source.IsAfef
            });
        }
        if (!hasPayload)
        {
            errorCode = "memory_recovery_empty_payload";
            return false;
        }
        if (aggregateTextBytes > MaximumAggregateTextBytes)
        {
            errorCode = "memory_recovery_payload_oversize";
            return false;
        }
        normalized = new InteractionMemoryRecoverySeed
        {
            CommitId = seed.CommitId.Trim(),
            Channel = seed.Channel,
            SessionId = seed.SessionId.Trim(),
            SubjectId = seed.SubjectId.Trim(),
            IsNonHero = seed.IsNonHero,
            NpcName = NormalizeBounded(seed.NpcName, 256) ?? string.Empty,
            RuntimeGeneration = Math.Max(0L, seed.RuntimeGeneration),
            SaveGeneration = Math.Max(0L, seed.SaveGeneration),
            TraceId = NormalizeBounded(seed.TraceId, 512) ?? string.Empty,
            OriginGameDay = Math.Max(0, seed.OriginGameDay),
            OriginGameDate = NormalizeBounded(seed.OriginGameDate, 256) ?? string.Empty,
            OriginGameHour = Math.Max(0, Math.Min(23, seed.OriginGameHour)),
            OriginScene = NormalizeBounded(seed.OriginScene, 512) ?? string.Empty,
            DailyStorageDay = Math.Max(0, seed.DailyStorageDay),
            DailyStorageDate = NormalizeBounded(seed.DailyStorageDate, 256) ?? string.Empty,
            SceneSessionId = Math.Max(-1, seed.SceneSessionId),
            DialogueSessionId = Math.Max(-1, seed.DialogueSessionId),
            MemorySessionKey = NormalizeBounded(seed.MemorySessionKey, 512) ?? string.Empty,
            TargetAgentIndex = Math.Max(-1, seed.TargetAgentIndex),
            TargetName = NormalizeBounded(seed.TargetName, 256) ?? string.Empty,
            Components = components
        };
        return true;
    }

    private static string NormalizeBounded(string value, int maximumLength)
    {
        string normalized = (value ?? string.Empty).Trim();
        return normalized.Length <= maximumLength ? normalized : null;
    }

    private static string ComputePayloadHash(InteractionMemoryRecoverySeed seed)
        => Hash(writer =>
        {
            writer.Write(seed.Channel);
            writer.Write(seed.SessionId);
            writer.Write(seed.SubjectId);
            writer.Write(seed.IsNonHero);
            writer.Write(seed.NpcName);
            writer.Write(seed.RuntimeGeneration);
            writer.Write(seed.SaveGeneration);
            writer.Write(seed.TraceId);
            writer.Write(seed.OriginGameDay);
            writer.Write(seed.OriginGameDate);
            writer.Write(seed.OriginGameHour);
            writer.Write(seed.OriginScene);
            writer.Write(seed.SceneSessionId);
            writer.Write(seed.DialogueSessionId);
            writer.Write(seed.MemorySessionKey);
            writer.Write(seed.TargetAgentIndex);
            writer.Write(seed.TargetName);
            writer.Write(seed.Components.Count);
            foreach (InteractionMemoryRecoveryComponentSeed component in seed.Components)
            {
                writer.Write(component.Part);
                writer.Write(component.DailySpeaker);
                writer.Write(component.DailyText);
                writer.Write(component.RecentText);
                writer.Write(component.IsAfef);
                writer.Write(component.IsLlmDialogue);
            }
        });

    private static string Serialize(InteractionMemoryRecoveryEntry entry)
    {
        using (var stream = new MemoryStream())
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
        {
            writer.Write(entry.SchemaVersion);
            writer.Write((int)entry.Lifecycle);
            writer.Write(entry.RecoveryId ?? string.Empty);
            writer.Write(entry.PayloadHash ?? string.Empty);
            writer.Write(entry.CreatedUtcTicks);
            writer.Write(entry.CompletedUtcTicks);
            writer.Write(entry.LastAttemptUtcTicks);
            writer.Write(entry.LastErrorCode ?? string.Empty);
            writer.Write(entry.ExpectedMarkerMask);
            writer.Write(entry.Channel);
            writer.Write(entry.SessionId ?? string.Empty);
            writer.Write(entry.OriginalSubjectId ?? string.Empty);
            writer.Write(entry.SubjectId ?? string.Empty);
            writer.Write(entry.IsNonHero);
            writer.Write(entry.NpcName ?? string.Empty);
            writer.Write(entry.RuntimeGeneration);
            writer.Write(entry.SaveGeneration);
            writer.Write(entry.TraceId ?? string.Empty);
            writer.Write(entry.OriginGameDay);
            writer.Write(entry.OriginGameDate ?? string.Empty);
            writer.Write(entry.OriginGameHour);
            writer.Write(entry.OriginScene ?? string.Empty);
            writer.Write(entry.DailyStorageDay);
            writer.Write(entry.DailyStorageDate ?? string.Empty);
            writer.Write(entry.SceneSessionId);
            writer.Write(entry.DialogueSessionId);
            writer.Write(entry.MemorySessionKey ?? string.Empty);
            writer.Write(entry.TargetAgentIndex);
            writer.Write(entry.TargetName ?? string.Empty);
            writer.Write(entry.Components?.Count ?? 0);
            foreach (InteractionMemoryRecoveryComponent component in entry.Components ?? new List<InteractionMemoryRecoveryComponent>())
            {
                writer.Write(component.Part ?? string.Empty);
                writer.Write(component.DailySpeaker ?? string.Empty);
                writer.Write(component.DailyText ?? string.Empty);
                writer.Write(component.RecentText ?? string.Empty);
                writer.Write(component.IsAfef);
                writer.Write(component.IsLlmDialogue);
                writer.Write((int)component.DailyState);
                writer.Write((int)component.RecentState);
                writer.Write(component.DailyAttempts);
                writer.Write(component.RecentAttempts);
            }
            writer.Flush();
            byte[] payload = stream.ToArray();
            byte[] checksum;
            using (SHA256 sha = SHA256.Create())
            {
                checksum = sha.ComputeHash(payload);
            }
            byte[] wire = new byte[payload.Length + checksum.Length];
            Buffer.BlockCopy(payload, 0, wire, 0, payload.Length);
            Buffer.BlockCopy(checksum, 0, wire, payload.Length, checksum.Length);
            return WirePrefix + Convert.ToBase64String(wire);
        }
    }

    private static bool TryDeserialize(
        string storageKey,
        string value,
        out InteractionMemoryRecoveryEntry entry,
        out string errorCode)
    {
        entry = null;
        errorCode = string.Empty;
        if (string.IsNullOrWhiteSpace(storageKey) || string.IsNullOrWhiteSpace(value)
            || value.Length > MaximumSerializedValueLength || !value.StartsWith(WirePrefix, StringComparison.Ordinal))
        {
            errorCode = "memory_recovery_invalid_wire_value";
            return false;
        }
        try
        {
            byte[] bytes = Convert.FromBase64String(value.Substring(WirePrefix.Length));
            if (bytes.Length <= 32 || bytes.Length > MaximumSerializedValueLength)
            {
                errorCode = "memory_recovery_wire_oversize";
                return false;
            }
            int payloadLength = bytes.Length - 32;
            byte[] payload = new byte[payloadLength];
            byte[] expectedChecksum = new byte[32];
            Buffer.BlockCopy(bytes, 0, payload, 0, payloadLength);
            Buffer.BlockCopy(bytes, payloadLength, expectedChecksum, 0, expectedChecksum.Length);
            byte[] actualChecksum;
            using (SHA256 sha = SHA256.Create())
            {
                actualChecksum = sha.ComputeHash(payload);
            }
            if (!FixedTimeEquals(expectedChecksum, actualChecksum))
            {
                errorCode = "memory_recovery_wire_checksum_mismatch";
                return false;
            }
            using (var stream = new MemoryStream(payload, writable: false))
            using (var reader = new BinaryReader(stream, Encoding.UTF8, false))
            {
                var candidate = new InteractionMemoryRecoveryEntry
                {
                    SchemaVersion = reader.ReadInt32(),
                    Lifecycle = (InteractionMemoryRecoveryLifecycle)reader.ReadInt32(),
                    RecoveryId = reader.ReadString(),
                    PayloadHash = reader.ReadString(),
                    CreatedUtcTicks = reader.ReadInt64(),
                    CompletedUtcTicks = reader.ReadInt64(),
                    LastAttemptUtcTicks = reader.ReadInt64(),
                    LastErrorCode = reader.ReadString(),
                    ExpectedMarkerMask = reader.ReadInt32(),
                    Channel = reader.ReadInt32(),
                    SessionId = reader.ReadString(),
                    OriginalSubjectId = reader.ReadString(),
                    SubjectId = reader.ReadString(),
                    IsNonHero = reader.ReadBoolean(),
                    NpcName = reader.ReadString(),
                    RuntimeGeneration = reader.ReadInt64(),
                    SaveGeneration = reader.ReadInt64(),
                    TraceId = reader.ReadString(),
                    OriginGameDay = reader.ReadInt32(),
                    OriginGameDate = reader.ReadString(),
                    OriginGameHour = reader.ReadInt32(),
                    OriginScene = reader.ReadString(),
                    DailyStorageDay = reader.ReadInt32(),
                    DailyStorageDate = reader.ReadString(),
                    SceneSessionId = reader.ReadInt32(),
                    DialogueSessionId = reader.ReadInt32(),
                    MemorySessionKey = reader.ReadString(),
                    TargetAgentIndex = reader.ReadInt32(),
                    TargetName = reader.ReadString(),
                    Components = new List<InteractionMemoryRecoveryComponent>()
                };
                int componentCount = reader.ReadInt32();
                if (componentCount < 0 || componentCount > MaximumComponentCount)
                {
                    errorCode = "memory_recovery_invalid_component_count";
                    return false;
                }
                for (int i = 0; i < componentCount; i++)
                {
                    candidate.Components.Add(new InteractionMemoryRecoveryComponent
                    {
                        Part = reader.ReadString(),
                        DailySpeaker = reader.ReadString(),
                        DailyText = reader.ReadString(),
                        RecentText = reader.ReadString(),
                        IsAfef = reader.ReadBoolean(),
                        IsLlmDialogue = reader.ReadBoolean(),
                        DailyState = (InteractionMemoryRecoveryStepState)reader.ReadInt32(),
                        RecentState = (InteractionMemoryRecoveryStepState)reader.ReadInt32(),
                        DailyAttempts = reader.ReadInt32(),
                        RecentAttempts = reader.ReadInt32()
                    });
                }
                if (stream.Position != stream.Length || !ValidateEntry(storageKey, candidate, out errorCode))
                {
                    return false;
                }
                entry = candidate;
                return true;
            }
        }
        catch
        {
            errorCode = "memory_recovery_wire_decode_failed";
            return false;
        }
    }

    private static bool ValidateEntry(
        string storageKey,
        InteractionMemoryRecoveryEntry entry,
        out string errorCode)
    {
        errorCode = string.Empty;
        if (entry.SchemaVersion != CurrentSchemaVersion
            || (entry.Lifecycle != InteractionMemoryRecoveryLifecycle.Pending
                && entry.Lifecycle != InteractionMemoryRecoveryLifecycle.Completed)
            || !string.Equals(storageKey, entry.RecoveryId, StringComparison.Ordinal)
            || !IsHexDigest(entry.RecoveryId) || !IsHexDigest(entry.PayloadHash)
            || entry.CreatedUtcTicks <= 0 || entry.Channel < 0 || entry.Channel > 4)
        {
            errorCode = "memory_recovery_invalid_record_identity";
            return false;
        }
        if (entry.Lifecycle == InteractionMemoryRecoveryLifecycle.Completed)
        {
            if (entry.CompletedUtcTicks <= 0 || entry.Components.Count != 0
                || !string.IsNullOrEmpty(entry.SessionId)
                || !string.IsNullOrEmpty(entry.NpcName) || !string.IsNullOrEmpty(entry.TraceId)
                || !string.IsNullOrEmpty(entry.OriginGameDate) || !string.IsNullOrEmpty(entry.OriginScene)
                || !string.IsNullOrEmpty(entry.MemorySessionKey) || !string.IsNullOrEmpty(entry.TargetName))
            {
                errorCode = "memory_recovery_invalid_tombstone";
                return false;
            }
            if (string.IsNullOrWhiteSpace(entry.SubjectId) || entry.SubjectId.Length > 512)
            {
                errorCode = "memory_recovery_invalid_tombstone_subject";
                return false;
            }
            if (string.IsNullOrWhiteSpace(entry.OriginalSubjectId) || entry.OriginalSubjectId.Length > 512)
            {
                errorCode = "memory_recovery_invalid_tombstone_original_subject";
                return false;
            }
            if (entry.LastAttemptUtcTicks != 0L || !string.IsNullOrEmpty(entry.LastErrorCode))
            {
                errorCode = "memory_recovery_invalid_tombstone_retry_state";
                return false;
            }
            if (entry.ExpectedMarkerMask <= 0 || (entry.ExpectedMarkerMask & ~0x3F) != 0)
            {
                errorCode = "memory_recovery_invalid_tombstone_marker_mask";
                return false;
            }
            if (entry.DailyStorageDay < 0 || (entry.DailyStorageDate ?? string.Empty).Length > 256)
            {
                errorCode = "memory_recovery_invalid_tombstone_daily_storage";
                return false;
            }
            return true;
        }
        var seed = new InteractionMemoryRecoverySeed
        {
            CommitId = "not-persisted",
            Channel = entry.Channel,
            SessionId = entry.SessionId,
            SubjectId = entry.OriginalSubjectId,
            IsNonHero = entry.IsNonHero,
            NpcName = entry.NpcName,
            RuntimeGeneration = entry.RuntimeGeneration,
            SaveGeneration = entry.SaveGeneration,
            TraceId = entry.TraceId,
            OriginGameDay = entry.OriginGameDay,
            OriginGameDate = entry.OriginGameDate,
            OriginGameHour = entry.OriginGameHour,
            OriginScene = entry.OriginScene,
            SceneSessionId = entry.SceneSessionId,
            DialogueSessionId = entry.DialogueSessionId,
            MemorySessionKey = entry.MemorySessionKey,
            TargetAgentIndex = entry.TargetAgentIndex,
            TargetName = entry.TargetName,
            Components = entry.Components.Select(component => new InteractionMemoryRecoveryComponentSeed
            {
                Part = component.Part,
                DailySpeaker = component.DailySpeaker,
                DailyText = component.DailyText,
                RecentText = component.RecentText,
                IsAfef = component.IsAfef,
                IsLlmDialogue = component.IsLlmDialogue
            }).ToList()
        };
        if (!TryNormalizeSeed(seed, out InteractionMemoryRecoverySeed normalized, out errorCode)
            || !string.Equals(ComputePayloadHash(normalized), entry.PayloadHash, StringComparison.Ordinal))
        {
            errorCode = string.IsNullOrEmpty(errorCode) ? "memory_recovery_payload_hash_mismatch" : errorCode;
            return false;
        }
        if (string.IsNullOrWhiteSpace(entry.SubjectId) || entry.SubjectId.Length > 512)
        {
            errorCode = "memory_recovery_invalid_projection_subject";
            return false;
        }
        if (entry.DailyStorageDay < 0 || (entry.DailyStorageDate ?? string.Empty).Length > 256)
        {
            errorCode = "memory_recovery_invalid_daily_storage";
            return false;
        }
        foreach (InteractionMemoryRecoveryComponent component in entry.Components)
        {
            if (!IsValidStepState(component.DailyState) || !IsValidStepState(component.RecentState))
            {
                errorCode = "memory_recovery_invalid_step_state";
                return false;
            }
            bool hasDailyText = !string.IsNullOrWhiteSpace(component.DailyText);
            bool hasRecentText = !string.IsNullOrWhiteSpace(component.RecentText);
            if (hasDailyText == (component.DailyState == InteractionMemoryRecoveryStepState.NotApplicable)
                || hasRecentText == (component.RecentState == InteractionMemoryRecoveryStepState.NotApplicable))
            {
                errorCode = "memory_recovery_step_payload_mismatch";
                return false;
            }
            if (component.DailyAttempts < 0 || component.DailyAttempts > MaximumAttemptsPerStep
                || component.RecentAttempts < 0 || component.RecentAttempts > MaximumAttemptsPerStep)
            {
                errorCode = "memory_recovery_invalid_attempt_count";
                return false;
            }
        }
        if (entry.CompletedUtcTicks != 0L
            || entry.LastAttemptUtcTicks < 0L
            || (entry.LastErrorCode ?? string.Empty).Length > 128
            || entry.Components.All(component => IsTerminal(component.DailyState)
                && IsTerminal(component.RecentState)))
        {
            errorCode = "memory_recovery_invalid_pending_lifecycle";
            return false;
        }
        if (entry.ExpectedMarkerMask != ComputeExpectedMarkerMask(entry.Components))
        {
            errorCode = "memory_recovery_marker_mask_mismatch";
            return false;
        }
        return true;
    }

    private static bool IsValidStepState(InteractionMemoryRecoveryStepState state)
        => state == InteractionMemoryRecoveryStepState.Pending
            || state == InteractionMemoryRecoveryStepState.Started
            || state == InteractionMemoryRecoveryStepState.Applied
            || state == InteractionMemoryRecoveryStepState.NotApplicable
            || state == InteractionMemoryRecoveryStepState.Unknown;

    private static bool HasExhaustedStep(InteractionMemoryRecoveryEntry entry)
        => entry?.Components != null && entry.Components.Any(component =>
            (component.DailyState == InteractionMemoryRecoveryStepState.Pending
                && component.DailyAttempts >= MaximumAttemptsPerStep)
            || (component.RecentState == InteractionMemoryRecoveryStepState.Pending
                && component.RecentAttempts >= MaximumAttemptsPerStep));

    private static bool FixedTimeEquals(byte[] left, byte[] right)
    {
        if (left == null || right == null || left.Length != right.Length)
        {
            return false;
        }
        int difference = 0;
        for (int i = 0; i < left.Length; i++)
        {
            difference |= left[i] ^ right[i];
        }
        return difference == 0;
    }

    private static bool IsHexDigest(string value)
        => value != null && value.Length == 64 && value.All(character =>
            (character >= '0' && character <= '9') || (character >= 'A' && character <= 'F'));

    private static string Hash(Action<BinaryWriter> write)
    {
        using (var stream = new MemoryStream())
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
        using (SHA256 sha = SHA256.Create())
        {
            write(writer);
            writer.Flush();
            return BitConverter.ToString(sha.ComputeHash(stream.ToArray())).Replace("-", string.Empty);
        }
    }
}

internal enum InteractionMemoryRecoveryBeginStatus
{
    Began = 0,
    DuplicateCompleted = 1,
    ExistingPending = 2,
    Conflict = 3,
    Rejected = 4,
    CapacityExceeded = 5
}

internal enum InteractionMemoryRecoveryStepState
{
    Unknown = 0,
    Pending = 1,
    Started = 2,
    Applied = 3,
    NotApplicable = 4
}

internal enum InteractionMemoryRecoveryTarget
{
    Daily = 1,
    Recent = 2
}

internal enum InteractionMemoryRecoveryLifecycle
{
    Unknown = 0,
    Pending = 1,
    Completed = 2
}

internal enum InteractionMemoryRecoveryLookupStatus
{
    Unavailable = 0,
    Missing = 1,
    Pending = 2,
    Completed = 3,
    Quarantined = 4,
    Disabled = 5,
    SubjectMismatch = 6,
    PayloadMismatch = 7,
    Invalid = 8
}

internal sealed class InteractionMemoryRecoverySeed
{
    internal string CommitId { get; set; } = string.Empty;
    internal int Channel { get; set; }
    internal string SessionId { get; set; } = string.Empty;
    internal string SubjectId { get; set; } = string.Empty;
    internal bool IsNonHero { get; set; }
    internal string NpcName { get; set; } = string.Empty;
    internal long RuntimeGeneration { get; set; }
    internal long SaveGeneration { get; set; }
    internal string TraceId { get; set; } = string.Empty;
    internal int OriginGameDay { get; set; }
    internal string OriginGameDate { get; set; } = string.Empty;
    internal int OriginGameHour { get; set; }
    internal string OriginScene { get; set; } = string.Empty;
    internal int DailyStorageDay { get; set; }
    internal string DailyStorageDate { get; set; } = string.Empty;
    internal int SceneSessionId { get; set; } = -1;
    internal int DialogueSessionId { get; set; } = -1;
    internal string MemorySessionKey { get; set; } = string.Empty;
    internal int TargetAgentIndex { get; set; } = -1;
    internal string TargetName { get; set; } = string.Empty;
    internal IReadOnlyList<InteractionMemoryRecoveryComponentSeed> Components { get; set; } =
        Array.Empty<InteractionMemoryRecoveryComponentSeed>();
}

internal sealed class InteractionMemoryRecoveryComponentSeed
{
    internal string Part { get; set; } = string.Empty;
    internal string DailySpeaker { get; set; } = string.Empty;
    internal string DailyText { get; set; } = string.Empty;
    internal string RecentText { get; set; } = string.Empty;
    internal bool IsAfef { get; set; }
    internal bool IsLlmDialogue { get; set; }
}

internal sealed class InteractionMemoryRecoveryWorkItem
{
    internal string RecoveryId { get; set; } = string.Empty;
    internal string PayloadHash { get; set; } = string.Empty;
    internal int Channel { get; set; }
    internal string SessionId { get; set; } = string.Empty;
    internal string SubjectId { get; set; } = string.Empty;
    internal bool IsNonHero { get; set; }
    internal string NpcName { get; set; } = string.Empty;
    internal long RuntimeGeneration { get; set; }
    internal long SaveGeneration { get; set; }
    internal string TraceId { get; set; } = string.Empty;
    internal int OriginGameDay { get; set; }
    internal string OriginGameDate { get; set; } = string.Empty;
    internal int OriginGameHour { get; set; }
    internal string OriginScene { get; set; } = string.Empty;
    internal int DailyStorageDay { get; set; }
    internal string DailyStorageDate { get; set; } = string.Empty;
    internal int SceneSessionId { get; set; }
    internal int DialogueSessionId { get; set; }
    internal string MemorySessionKey { get; set; } = string.Empty;
    internal int TargetAgentIndex { get; set; }
    internal string TargetName { get; set; } = string.Empty;
    internal string Part { get; set; } = string.Empty;
    internal string DailySpeaker { get; set; } = string.Empty;
    internal string DailyText { get; set; } = string.Empty;
    internal string RecentText { get; set; } = string.Empty;
    internal bool IsAfef { get; set; }
    internal bool IsLlmDialogue { get; set; }
    internal InteractionMemoryRecoveryTarget Target { get; set; }
    internal InteractionMemoryRecoveryStepState State { get; set; }
    internal int AttemptCount { get; set; }
}

internal sealed class InteractionMemoryRecoveryEntry
{
    internal int SchemaVersion { get; set; }
    internal string RecoveryId { get; set; } = string.Empty;
    internal string PayloadHash { get; set; } = string.Empty;
    internal InteractionMemoryRecoveryLifecycle Lifecycle { get; set; }
    internal long CreatedUtcTicks { get; set; }
    internal long CompletedUtcTicks { get; set; }
    internal long LastAttemptUtcTicks { get; set; }
    internal string LastErrorCode { get; set; } = string.Empty;
    internal int ExpectedMarkerMask { get; set; }
    internal int Channel { get; set; }
    internal string SessionId { get; set; } = string.Empty;
    internal string OriginalSubjectId { get; set; } = string.Empty;
    internal string SubjectId { get; set; } = string.Empty;
    internal bool IsNonHero { get; set; }
    internal string NpcName { get; set; } = string.Empty;
    internal long RuntimeGeneration { get; set; }
    internal long SaveGeneration { get; set; }
    internal string TraceId { get; set; } = string.Empty;
    internal int OriginGameDay { get; set; }
    internal string OriginGameDate { get; set; } = string.Empty;
    internal int OriginGameHour { get; set; }
    internal string OriginScene { get; set; } = string.Empty;
    internal int DailyStorageDay { get; set; }
    internal string DailyStorageDate { get; set; } = string.Empty;
    internal int SceneSessionId { get; set; }
    internal int DialogueSessionId { get; set; }
    internal string MemorySessionKey { get; set; } = string.Empty;
    internal int TargetAgentIndex { get; set; }
    internal string TargetName { get; set; } = string.Empty;
    internal List<InteractionMemoryRecoveryComponent> Components { get; set; } =
        new List<InteractionMemoryRecoveryComponent>();

    internal void ClearPayload()
    {
        Channel = 0;
        SessionId = string.Empty;
        IsNonHero = false;
        NpcName = string.Empty;
        RuntimeGeneration = 0;
        SaveGeneration = 0;
        TraceId = string.Empty;
        OriginGameDay = 0;
        OriginGameDate = string.Empty;
        OriginGameHour = 0;
        OriginScene = string.Empty;
        SceneSessionId = -1;
        DialogueSessionId = -1;
        MemorySessionKey = string.Empty;
        TargetAgentIndex = -1;
        TargetName = string.Empty;
        LastAttemptUtcTicks = 0L;
        LastErrorCode = string.Empty;
        Components.Clear();
    }
}

internal sealed class InteractionMemoryRecoveryEviction
{
    internal string RecoveryId { get; set; } = string.Empty;
    internal string SubjectId { get; set; } = string.Empty;
}

internal sealed class InteractionMemoryRecoveryRetention
{
    internal string RecoveryId { get; set; } = string.Empty;
    internal string PayloadHash { get; set; } = string.Empty;
    internal string SubjectId { get; set; } = string.Empty;
    internal bool IsPending { get; set; }
    internal int ExpectedMarkerMask { get; set; }
    internal int AppliedDailyMarkerMask { get; set; }
    internal int AppliedRecentMarkerMask { get; set; }
    internal int DailyStorageDay { get; set; }
    internal string DailyStorageDate { get; set; } = string.Empty;
}

internal sealed class InteractionMemoryRecoveryComponent
{
    internal string Part { get; set; } = string.Empty;
    internal string DailySpeaker { get; set; } = string.Empty;
    internal string DailyText { get; set; } = string.Empty;
    internal string RecentText { get; set; } = string.Empty;
    internal bool IsAfef { get; set; }
    internal bool IsLlmDialogue { get; set; }
    internal InteractionMemoryRecoveryStepState DailyState { get; set; }
    internal InteractionMemoryRecoveryStepState RecentState { get; set; }
    internal int DailyAttempts { get; set; }
    internal int RecentAttempts { get; set; }
}
