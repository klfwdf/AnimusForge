using AnimusForge.Refactor.Contracts;
using System;using System.Collections.Generic;using System.Linq;using System.Threading;using Newtonsoft.Json;using AnimusForge.Refactor.Runtime;using TaleWorlds.Library;
namespace AnimusForge;
internal sealed class MemoryRecoveryStateOwner {
 private readonly MemoryBusinessStateOwner _state;private MemoryRecoveryPort _port;
 internal MemoryRecoveryStateOwner(MemoryBusinessStateOwner state){_state=state;}
 internal void Bind(MemoryRecoveryPort port){_port=port;}
 internal InteractionMemoryRecoveryLedger Ledger=new();
 internal int HasWork;internal long NextAttemptTicks;internal long LoadedGeneration;internal int LoadConfirmed;
 private const int MaximumPersistedMemoryCommitMarkers=(InteractionMemoryRecoveryLedger.MaximumPendingEntries+InteractionMemoryRecoveryLedger.MaximumCompletedEntries)*InteractionMemoryRecoveryLedger.MaximumComponentCount;
 private const long InteractionMemoryRecoveryRetryDelayTicks=TimeSpan.TicksPerSecond*5L;
internal AnimusForge.Refactor.Contracts.MemoryCommitResult CommitPrepared(InteractionMemoryRecoverySeed seed, string preparedRecoveryId, string preparedPayloadHash, Action<InteractionMemoryRecoverySeed,string,string> completeAuxiliary)
    {
            InteractionMemoryRecoveryLedger ledger = EnsureInteractionMemoryRecoveryLedger();
            InteractionMemoryRecoveryBeginStatus beginStatus = ledger.Begin(
                seed,
                out string recoveryId,
                out string beginError);
            if (beginStatus == InteractionMemoryRecoveryBeginStatus.DuplicateCompleted)
            {
                return new MemoryCommitResult(MemoryCommitStatus.Duplicate);
            }
            if (beginStatus != InteractionMemoryRecoveryBeginStatus.Began
                && beginStatus != InteractionMemoryRecoveryBeginStatus.ExistingPending)
            {
                return new MemoryCommitResult(
                    MemoryCommitStatus.Failed,
                    string.IsNullOrWhiteSpace(beginError) ? "memory_recovery_begin_failed" : beginError);
            }

            RefreshInteractionMemoryRecoveryWorkFlag();
            ProcessInteractionMemoryRecoveryEntry(recoveryId);
            bool recoveryCompleted = ledger.IsCompleted(recoveryId);
            if (recoveryCompleted)
            {
                if (ShouldCompleteInitialInteractionMemoryNotoriety(
                    beginStatus,
                    recoveryCompleted,
                    recoveryId,
                    preparedRecoveryId))
                {
                    try
                    {
                        completeAuxiliary(
                            seed,
                            recoveryId,
                            preparedPayloadHash);
                    }
                    catch (Exception ex)
                    {
                        _port.Log(
                            "MemoryRecovery",
                            "[WARN] auxiliary completion isolated recovery=" + recoveryId
                                + " state=NOT-RECOVERABLE error=" + ex.Message);
                    }
                }
                RefreshInteractionMemoryRecoveryWorkFlag();
                return new MemoryCommitResult(beginStatus == InteractionMemoryRecoveryBeginStatus.ExistingPending
                    ? MemoryCommitStatus.Duplicate
                    : MemoryCommitStatus.Applied);
            }
            RefreshInteractionMemoryRecoveryWorkFlag();
            return new MemoryCommitResult(MemoryCommitStatus.Failed, "memory_recovery_pending");
    }
    internal static bool ShouldCompleteInitialInteractionMemoryNotoriety(InteractionMemoryRecoveryBeginStatus beginStatus, bool recoveryCompleted, string recoveryId, string preparedRecoveryId)
        => beginStatus == InteractionMemoryRecoveryBeginStatus.Began && recoveryCompleted
            && !string.IsNullOrWhiteSpace(recoveryId) && string.Equals(recoveryId, preparedRecoveryId, StringComparison.Ordinal);
internal void ProcessInteractionMemoryRecoveryEntry(string recoveryId)
    {
        InteractionMemoryRecoveryLedger ledger = EnsureInteractionMemoryRecoveryLedger();
        while (ledger.TryGetNextWorkFor(recoveryId, out InteractionMemoryRecoveryWorkItem work))
        {
            if (!TryApplyInteractionMemoryRecoveryWork(work))
            {
                break;
            }
        }
    }
internal bool TryApplyInteractionMemoryRecoveryWork(InteractionMemoryRecoveryWorkItem work)
    {
        InteractionMemoryRecoveryLedger ledger = EnsureInteractionMemoryRecoveryLedger();
        try
        {
            if (HasMatchingInteractionMemoryMarker(work, out bool markerConflict))
            {
                ledger.MarkApplied(work);
                PruneEvictedInteractionMemoryRecoveryMarkers();
                return true;
            }
            if (markerConflict)
            {
                ledger.MarkUnknown(work);
                ledger.QuarantineEntry(work.RecoveryId, "memory_recovery_marker_conflict");
                PruneEvictedInteractionMemoryRecoveryMarkers();
                _port.Log("MemoryRecovery", "[ERROR] marker conflict recovery=" + work.RecoveryId + " part=" + work.Part + " target=" + work.Target);
                return false;
            }

            bool published = work.Target == InteractionMemoryRecoveryTarget.Daily
                ? PublishDailyInteractionMemoryComponent(work)
                : PublishRecentInteractionMemoryComponent(work);
            if (published || HasMatchingInteractionMemoryMarker(work, out markerConflict))
            {
                ledger.MarkApplied(work);
                PruneEvictedInteractionMemoryRecoveryMarkers();
                return true;
            }
            if (markerConflict)
            {
                ledger.MarkUnknown(work);
                ledger.QuarantineEntry(work.RecoveryId, "memory_recovery_marker_conflict");
                PruneEvictedInteractionMemoryRecoveryMarkers();
                return false;
            }

            // Both stores publish by replacing an owner collection reference
            // that already contains the marker. A missing marker proves that
            // the replacement did not happen, so this step alone is safe to
            // retry; no action or other memory component is replayed.
            RegisterInteractionMemoryRecoveryRetryOrQuarantine(
                ledger,
                work,
                "memory_recovery_publish_unconfirmed");
            return false;
        }
        catch (InteractionMemoryRecoveryPermanentException ex)
        {
            ledger.MarkUnknown(work);
            ledger.QuarantineEntry(work.RecoveryId, ex.ErrorCode);
            PruneEvictedInteractionMemoryRecoveryMarkers();
            _port.Log("MemoryRecovery", "[ERROR] permanent component failure recovery=" + work.RecoveryId
                + " part=" + work.Part + " target=" + work.Target + " error=" + ex.ErrorCode);
            return false;
        }
        catch (Exception ex)
        {
            if (HasMatchingInteractionMemoryMarker(work, out bool markerConflict))
            {
                ledger.MarkApplied(work);
                PruneEvictedInteractionMemoryRecoveryMarkers();
                return true;
            }
            if (markerConflict)
            {
                ledger.MarkUnknown(work);
                ledger.QuarantineEntry(work.RecoveryId, "memory_recovery_marker_conflict");
                PruneEvictedInteractionMemoryRecoveryMarkers();
            }
            else
            {
                RegisterInteractionMemoryRecoveryRetryOrQuarantine(
                    ledger,
                    work,
                    "memory_recovery_component_exception");
            }
            _port.Log("MemoryRecovery", "[WARN] component publish deferred recovery=" + work.RecoveryId
                + " part=" + work.Part + " target=" + work.Target + " error=" + ex.Message);
            return false;
        }
        finally
        {
            RefreshInteractionMemoryRecoveryWorkFlag();
        }
    }
internal void RegisterInteractionMemoryRecoveryRetryOrQuarantine(
        InteractionMemoryRecoveryLedger ledger,
        InteractionMemoryRecoveryWorkItem work,
        string errorCode)
    {
        if (!ledger.RegisterRetry(work, errorCode, out bool exhausted))
        {
            ledger.MarkUnknown(work);
            ledger.QuarantineEntry(work.RecoveryId, "memory_recovery_retry_state_invalid");
            PruneEvictedInteractionMemoryRecoveryMarkers();
            return;
        }
        if (exhausted)
        {
            ledger.MarkUnknown(work);
            ledger.QuarantineEntry(work.RecoveryId, "memory_recovery_retry_exhausted");
            PruneEvictedInteractionMemoryRecoveryMarkers();
            return;
        }
        ScheduleInteractionMemoryRecoveryRetry();
    }
internal bool PublishDailyInteractionMemoryComponent(InteractionMemoryRecoveryWorkItem work)
    {
        string memoryId = MemoryRecordRules.NormalizeMemoryHeroId(work.SubjectId);
        if (!_port.IsEntityEligible(memoryId) || string.IsNullOrWhiteSpace(work.DailyText))
        {
            throw new InteractionMemoryRecoveryPermanentException("memory_recovery_target_ineligible");
        }
        int currentDay = _port.CurrentDay();
        if (work.OriginGameDay > currentDay)
        {
            throw new InteractionMemoryRecoveryPermanentException("memory_recovery_origin_day_invalid");
        }
        int candidateStorageDay = work.DailyStorageDay >= 0
            ? work.DailyStorageDay
            : work.OriginGameDay;
        bool selectedDayAlreadySealed = candidateStorageDay < currentDay
            && _state.HasBlock(memoryId, candidateStorageDay);
        int storageDay = selectedDayAlreadySealed ? currentDay : candidateStorageDay;
        string storageDate = selectedDayAlreadySealed
            ? _port.CurrentDate()
            : string.IsNullOrWhiteSpace(work.DailyStorageDate) ? work.OriginGameDate : work.DailyStorageDate;
        if (!EnsureInteractionMemoryRecoveryLedger().RecordDailyStorage(work, storageDay, storageDate))
        {
            throw new InteractionMemoryRecoveryPermanentException("memory_recovery_daily_storage_invalid");
        }
        List<DailyMemoryDraft> current = _state.LoadDrafts(memoryId);
        List<DailyMemoryDraft> clone = CloneForMemoryRecovery(current) ?? new List<DailyMemoryDraft>();
        DailyMemoryDraft draft = clone.FirstOrDefault(item => item != null && item.GameDayIndex == storageDay);
        if (draft == null)
        {
            draft = new DailyMemoryDraft
            {
                HeroId = memoryId,
                HeroName = work.NpcName,
                GameDayIndex = storageDay,
                GameDate = storageDate,
                Lines = new List<DailyMemoryLine>()
            };
            clone.Add(draft);
        }
        draft.Lines = draft.Lines ?? new List<DailyMemoryLine>();
        var line = new DailyMemoryLine
        {
            GameDayIndex = storageDay,
            GameDate = storageDate,
            GameHour = work.OriginGameHour,
            Scene = work.OriginScene,
            Speaker = work.DailySpeaker,
            Text = work.DailyText,
            SceneSessionId = work.SceneSessionId,
            DialogueSessionId = work.DialogueSessionId,
            TargetAgentIndex = work.TargetAgentIndex,
            TargetName = work.TargetName,
            MemorySessionKey = work.MemorySessionKey,
            IsAfef = work.IsAfef,
            IsLlmDialogue = work.IsLlmDialogue && !work.IsAfef,
            MemoryCommitId = work.RecoveryId,
            MemoryCommitPart = work.Part,
            MemoryCommitHash = work.PayloadHash,
            MemoryCommitOriginGameDay = work.OriginGameDay,
            MemoryCommitOriginGameDate = work.OriginGameDate
        };
        draft.Lines.Add(line);
        draft.HasLlmDialogue |= line.IsLlmDialogue;

        // The H journal owns only the Daily/Recent projection. Pending weekly
        // material is a pre-action, process-local candidate and notoriety uses
        // a non-persisted, non-idempotent session counter. Replaying either from
        // this writer would synthesize success or double-count an unknown side
        // effect after a save/load boundary.
        _state.SaveDrafts(memoryId, clone);
        return HasDailyInteractionMemoryMarker(work, out _);
    }
internal bool PublishRecentInteractionMemoryComponent(InteractionMemoryRecoveryWorkItem work)
    {
        string memoryId = MemoryRecordRules.NormalizeMemoryHeroId(work.SubjectId);
        if (!_port.IsEntityEligible(memoryId) || string.IsNullOrWhiteSpace(work.RecentText))
        {
            throw new InteractionMemoryRecoveryPermanentException("memory_recovery_target_ineligible");
        }
        List<MyBehavior.DialogueDay> current = _port.LoadHistory(memoryId);
        List<MyBehavior.DialogueDay> clone = CloneForMemoryRecovery(current) ?? new List<MyBehavior.DialogueDay>();
        if (string.Equals(work.Part, "assistant", StringComparison.Ordinal))
        {
            _port.RemoveExpiredFacts(clone);
        }
        MyBehavior.DialogueDay day = clone.FirstOrDefault(item => item != null && item.GameDayIndex == work.OriginGameDay);
        if (day == null)
        {
            day = new MyBehavior.DialogueDay
            {
                GameDayIndex = work.OriginGameDay,
                GameDate = work.OriginGameDate,
                Lines = new List<string>(),
                MemoryCommitMarkers = new Dictionary<string, string>(StringComparer.Ordinal)
            };
            clone.Add(day);
        }
        day.Lines = day.Lines ?? new List<string>();
        day.MemoryCommitMarkers = SanitizeMemoryCommitMarkers(day.MemoryCommitMarkers);
        string markerKey = BuildMemoryCommitMarkerKey(work.RecoveryId, work.Part);
        int markerCount = clone.Sum(item => item?.MemoryCommitMarkers?.Count ?? 0);
        if (!day.MemoryCommitMarkers.ContainsKey(markerKey)
            && markerCount >= MaximumPersistedMemoryCommitMarkers)
        {
            throw new InteractionMemoryRecoveryPermanentException("memory_recovery_marker_capacity_exceeded");
        }
        string recentLine = work.SceneSessionId >= 0
            ? DialogueHistoryLedger.TagSceneSession(work.RecentText, work.SceneSessionId)
            : work.RecentText;
        day.Lines.Add(recentLine);
        day.MemoryCommitMarkers[markerKey] = work.PayloadHash;
        List<MyBehavior.DialogueDay> trimmed = TrimDialogueHistoryForMemoryRecovery(clone);
        _port.SaveHistory(memoryId, trimmed);
        return HasRecentInteractionMemoryMarker(work, out _);
    }
internal static T CloneForMemoryRecovery<T>(T value)
    {
        if (value == null)
        {
            return default;
        }
        string json = JsonConvert.SerializeObject(value, Formatting.None);
        return JsonConvert.DeserializeObject<T>(json);
    }
internal static List<MyBehavior.DialogueDay> TrimDialogueHistoryForMemoryRecovery(List<MyBehavior.DialogueDay> records)
    {
        var lines = new List<(int Day, string Date, string Line)>();
        foreach (MyBehavior.DialogueDay day in records ?? new List<MyBehavior.DialogueDay>())
        {
            if (day?.Lines == null)
            {
                continue;
            }
            foreach (string line in day.Lines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    lines.Add((day.GameDayIndex, day.GameDate, line));
                }
            }
        }
        if (lines.Count > 260)
        {
            lines = lines.Skip(lines.Count - 260).ToList();
        }
        var trimmed = new List<MyBehavior.DialogueDay>();
        foreach ((int dayIndex, string gameDate, string line) in lines)
        {
            MyBehavior.DialogueDay day = trimmed.FirstOrDefault(item => item.GameDayIndex == dayIndex);
            if (day == null)
            {
                day = new MyBehavior.DialogueDay
                {
                    GameDayIndex = dayIndex,
                    GameDate = gameDate,
                    Lines = new List<string>(),
                    MemoryCommitMarkers = new Dictionary<string, string>(StringComparer.Ordinal)
                };
                trimmed.Add(day);
            }
            day.Lines.Add(line);
        }
        CopyMemoryCommitMarkers(records, trimmed);
        return trimmed;
    }
internal static void CopyMemoryCommitMarkers(IEnumerable<MyBehavior.DialogueDay> source, List<MyBehavior.DialogueDay> target)
    {
        if (target == null)
        {
            return;
        }
        int copied = 0;
        foreach (MyBehavior.DialogueDay sourceDay in source ?? Enumerable.Empty<MyBehavior.DialogueDay>())
        {
            Dictionary<string, string> markers = SanitizeMemoryCommitMarkers(sourceDay?.MemoryCommitMarkers);
            if (markers.Count == 0)
            {
                continue;
            }
            MyBehavior.DialogueDay targetDay = target.FirstOrDefault(item => item != null && item.GameDayIndex == sourceDay.GameDayIndex);
            if (targetDay == null)
            {
                targetDay = new MyBehavior.DialogueDay
                {
                    GameDayIndex = sourceDay.GameDayIndex,
                    GameDate = sourceDay.GameDate,
                    Lines = new List<string>(),
                    MemoryCommitMarkers = new Dictionary<string, string>(StringComparer.Ordinal)
                };
                target.Add(targetDay);
            }
            targetDay.MemoryCommitMarkers = SanitizeMemoryCommitMarkers(targetDay.MemoryCommitMarkers);
            foreach (KeyValuePair<string, string> marker in markers.OrderBy(item => item.Key, StringComparer.Ordinal))
            {
                if (copied >= MaximumPersistedMemoryCommitMarkers)
                {
                    return;
                }
                targetDay.MemoryCommitMarkers[marker.Key] = marker.Value;
                copied++;
            }
        }
    }
internal bool HasMatchingInteractionMemoryMarker(
        InteractionMemoryRecoveryWorkItem work,
        out bool conflict)
        => work.Target == InteractionMemoryRecoveryTarget.Daily
            ? HasDailyInteractionMemoryMarker(work, out conflict)
            : HasRecentInteractionMemoryMarker(work, out conflict);
internal bool HasDailyInteractionMemoryMarker(
        InteractionMemoryRecoveryWorkItem work,
        out bool conflict)
    {
        conflict = false;
        string memoryId = MemoryRecordRules.NormalizeMemoryHeroId(work?.SubjectId);
        if (work == null || string.IsNullOrWhiteSpace(memoryId)
            || _state.Drafts == null
            || !_state.Drafts.TryGetValue(memoryId, out List<DailyMemoryDraft> drafts))
        {
            return false;
        }
        bool matching = false;
        foreach (DailyMemoryLine line in (drafts ?? new List<DailyMemoryDraft>())
            .Where(draft => draft?.Lines != null)
            .SelectMany(draft => draft.Lines)
            .Where(line => line != null
                && string.Equals(line.MemoryCommitId, work.RecoveryId, StringComparison.Ordinal)
                && string.Equals(line.MemoryCommitPart, work.Part, StringComparison.Ordinal)))
        {
            if (string.Equals(line.MemoryCommitHash, work.PayloadHash, StringComparison.Ordinal))
            {
                matching = true;
                continue;
            }
            conflict = true;
        }
        return matching && !conflict;
    }
internal bool HasRecentInteractionMemoryMarker(
        InteractionMemoryRecoveryWorkItem work,
        out bool conflict)
    {
        conflict = false;
        string memoryId = MemoryRecordRules.NormalizeMemoryHeroId(work?.SubjectId);
        string markerKey = BuildMemoryCommitMarkerKey(work?.RecoveryId, work?.Part);
        if (work == null || string.IsNullOrWhiteSpace(memoryId)
            || _port.History() == null
            || !_port.History().TryGetValue(memoryId, out List<MyBehavior.DialogueDay> records))
        {
            return false;
        }
        bool matching = false;
        foreach (MyBehavior.DialogueDay day in records ?? new List<MyBehavior.DialogueDay>())
        {
            if (day?.MemoryCommitMarkers == null
                || !day.MemoryCommitMarkers.TryGetValue(markerKey, out string markerHash))
            {
                continue;
            }
            if (string.Equals(markerHash, work.PayloadHash, StringComparison.Ordinal))
            {
                matching = true;
                continue;
            }
            conflict = true;
        }
        return matching && !conflict;
    }
internal static Dictionary<string, string> SanitizeMemoryCommitMarkers(
        IDictionary<string, string> markers)
    {
        var sanitized = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (KeyValuePair<string, string> marker in markers ?? new Dictionary<string, string>())
        {
            if (TryParseMemoryCommitMarkerKey(marker.Key, out string recoveryId, out string part)
                && IsValidMemoryCommitMarker(recoveryId, part, marker.Value))
            {
                sanitized[BuildMemoryCommitMarkerKey(recoveryId, part)] = marker.Value.Trim();
            }
        }
        return sanitized;
    }
internal static bool IsValidMemoryCommitMarker(string recoveryId, string part, string payloadHash)
        => IsMemoryRecoveryHexDigest((recoveryId ?? string.Empty).Trim())
            && ((part ?? string.Empty).Trim() == "user"
                || (part ?? string.Empty).Trim() == "fact"
                || (part ?? string.Empty).Trim() == "assistant")
            && IsMemoryRecoveryHexDigest((payloadHash ?? string.Empty).Trim());
internal static string BuildMemoryCommitMarkerKey(string recoveryId, string part)
        => (recoveryId ?? string.Empty).Trim() + ":" + (part ?? string.Empty).Trim();
internal static bool TryParseMemoryCommitMarkerKey(
        string markerKey,
        out string recoveryId,
        out string part)
    {
        recoveryId = string.Empty;
        part = string.Empty;
        string value = (markerKey ?? string.Empty).Trim();
        int separator = value.IndexOf(':');
        if (separator <= 0 || separator >= value.Length - 1)
        {
            return false;
        }
        recoveryId = value.Substring(0, separator);
        part = value.Substring(separator + 1);
        return true;
    }
internal static bool IsMemoryRecoveryHexDigest(string value)
        => value != null && value.Length == 64 && value.All(character =>
            (character >= '0' && character <= '9') || (character >= 'A' && character <= 'F'));
internal InteractionMemoryRecoveryLedger EnsureInteractionMemoryRecoveryLedger()
    {
        if (Ledger == null)
        {
            Ledger = new InteractionMemoryRecoveryLedger();
        }
        return Ledger;
    }
internal void RetargetInteractionMemoryRecoveryProjection(string sourceSubjectId, string targetSubjectId)
    {
        int changed = EnsureInteractionMemoryRecoveryLedger().RetargetProjectionSubject(
            MemoryRecordRules.NormalizeMemoryHeroId(sourceSubjectId),
            MemoryRecordRules.NormalizeMemoryHeroId(targetSubjectId));
        if (changed > 0)
        {
            _port.Log("MemoryRecovery", "projection retargeted count=" + changed);
        }
    }
internal IEnumerable<string> GetInteractionMemoryRecoveryProjectionSubjects()
        => EnsureInteractionMemoryRecoveryLedger().GetRetainedEntries()
            .Select(item => item.SubjectId)
            .Where(subjectId => !string.IsNullOrWhiteSpace(subjectId))
            .ToList();
internal void QuarantineInteractionMemoryRecoveryProjection(string subjectId, string reason)
    {
        int quarantined = EnsureInteractionMemoryRecoveryLedger().QuarantineProjectionSubject(
            MemoryRecordRules.NormalizeMemoryHeroId(subjectId),
            reason);
        if (quarantined > 0)
        {
            PruneEvictedInteractionMemoryRecoveryMarkers();
            RefreshInteractionMemoryRecoveryWorkFlag();
            _port.Log("MemoryRecovery", "projection quarantined count=" + quarantined
                + " reason=" + (reason ?? string.Empty));
        }
    }
internal void RefreshInteractionMemoryRecoveryWorkFlag()
    {
        InteractionMemoryRecoveryLedger ledger = EnsureInteractionMemoryRecoveryLedger();
        Volatile.Write(ref HasWork,
            ledger.HasPendingWork || ledger.HasUnresolvedWork ? 1 : 0);
    }
internal void ScheduleInteractionMemoryRecoveryRetry()
    {
        Interlocked.Exchange(
            ref NextAttemptTicks,
            DateTime.UtcNow.Ticks + InteractionMemoryRecoveryRetryDelayTicks);
        Volatile.Write(ref HasWork, 1);
    }
internal void ResetInteractionMemoryRecoveryTransientState(string reason)
    {
        Volatile.Write(ref HasWork, 0);
        Interlocked.Exchange(ref NextAttemptTicks, 0L);
        string normalizedReason = (reason ?? string.Empty).Trim();
        if (string.Equals(normalizedReason, "sync_load", StringComparison.Ordinal))
        {
            Volatile.Write(ref LoadConfirmed, 0);
            Interlocked.Exchange(ref LoadedGeneration, 0L);
        }
        else if (string.Equals(normalizedReason, "new_game_created", StringComparison.Ordinal))
        {
            EnsureInteractionMemoryRecoveryLedger().Import(new Dictionary<string, string>());
            Volatile.Write(ref LoadConfirmed, 1);
            Interlocked.Exchange(ref LoadedGeneration, SaveRuntimeGuard.CurrentGeneration);
        }
        else if (string.Equals(normalizedReason, "game_loaded", StringComparison.Ordinal))
        {
            // Bannerlord can raise this lifecycle after SyncData. Advance the
            // generation only when the tail Import already proved this save;
            // this event must never manufacture a successful load stamp.
            if (Volatile.Read(ref LoadConfirmed) != 0)
            {
                Interlocked.Exchange(ref LoadedGeneration, SaveRuntimeGuard.CurrentGeneration);
            }
        }
    }
internal void ActivateInteractionMemoryRecoveryAfterLoad()
    {
        InteractionMemoryRecoveryLedger ledger = EnsureInteractionMemoryRecoveryLedger();
        if (Interlocked.Read(ref LoadedGeneration)
            != SaveRuntimeGuard.CurrentGeneration
            || Volatile.Read(ref LoadConfirmed) == 0)
        {
            ledger.DisableForCurrentCampaign("memory_recovery_load_not_confirmed");
            ResetInteractionMemoryRecoveryTransientState("load_not_confirmed");
            _port.Log("MemoryRecovery", "[ERROR] recovery disabled because SyncData load was not confirmed");
            return;
        }
        if (ledger.IsDisabled)
        {
            ResetInteractionMemoryRecoveryTransientState("recovery_ledger_disabled");
            return;
        }
        ReconcilePersistedInteractionMemoryRecoveryMarkers();
        if (ledger.IsDisabled)
        {
            ResetInteractionMemoryRecoveryTransientState("owner_marker_validation_failed");
            return;
        }
        foreach (string recoveryId in ledger.GetUnresolvedWork()
            .Select(work => work.RecoveryId)
            .Distinct(StringComparer.Ordinal)
            .ToList())
        {
            IReadOnlyList<InteractionMemoryRecoveryWorkItem> unresolved = ledger.GetUnresolvedWork()
                .Where(work => string.Equals(work.RecoveryId, recoveryId, StringComparison.Ordinal))
                .ToList();
            bool conflictingMarker = false;
            foreach (InteractionMemoryRecoveryWorkItem work in unresolved)
            {
                if (HasMatchingInteractionMemoryMarker(work, out bool conflict) && !conflict)
                {
                    ledger.MarkApplied(work);
                    PruneEvictedInteractionMemoryRecoveryMarkers();
                }
                else if (!conflict)
                {
                    // The store operations publish content and marker in the
                    // same owner collection replacement. Missing marker means
                    // this one step never published and is safe to retry.
                    ledger.MarkPending(work);
                }
                else
                {
                    conflictingMarker = true;
                    break;
                }
            }
            if (conflictingMarker)
            {
                ledger.QuarantineEntry(recoveryId, "memory_recovery_marker_conflict_after_load");
                _port.Log("MemoryRecovery", "[WARN] conflicting record quarantined recovery=" + recoveryId);
            }
        }
        RefreshInteractionMemoryRecoveryWorkFlag();
        PruneEvictedInteractionMemoryRecoveryMarkers();
        if (Volatile.Read(ref HasWork) != 0)
        {
            ProcessOneInteractionMemoryRecoveryOnTick();
        }
    }
internal void ReconcilePersistedInteractionMemoryRecoveryMarkers()
    {
        InteractionMemoryRecoveryLedger ledger = EnsureInteractionMemoryRecoveryLedger();
        Dictionary<string, InteractionMemoryRecoveryRetention> retained = ledger.GetRetainedEntries()
            .ToDictionary(item => item.RecoveryId, item => item, StringComparer.Ordinal);
        var conflicts = new HashSet<string>(StringComparer.Ordinal);
        var observedDailyMarkerMasks = new Dictionary<string, int>(StringComparer.Ordinal);
        var observedRecentMarkerMasks = new Dictionary<string, int>(StringComparer.Ordinal);
        int observedMarkers = 0;

        foreach (KeyValuePair<string, List<DailyMemoryDraft>> ownerEntry in
            _state.Drafts ?? new Dictionary<string, List<DailyMemoryDraft>>())
        {
            string ownerId = MemoryRecordRules.NormalizeMemoryHeroId(ownerEntry.Key);
            foreach (DailyMemoryLine line in (ownerEntry.Value ?? new List<DailyMemoryDraft>())
                .Where(draft => draft?.Lines != null)
                .SelectMany(draft => draft.Lines)
                .Where(line => line != null && !string.IsNullOrWhiteSpace(line.MemoryCommitId)))
            {
                observedMarkers++;
                string recoveryId = (line.MemoryCommitId ?? string.Empty).Trim();
                if (!retained.TryGetValue(recoveryId, out InteractionMemoryRecoveryRetention retention))
                {
                    ClearDailyInteractionMemoryMarker(line);
                    continue;
                }
                if (!string.Equals(ownerId, MemoryRecordRules.NormalizeMemoryHeroId(retention.SubjectId), StringComparison.OrdinalIgnoreCase)
                    || !IsValidMemoryCommitMarker(recoveryId, line.MemoryCommitPart, line.MemoryCommitHash)
                    || !string.Equals(line.MemoryCommitHash, retention.PayloadHash, StringComparison.Ordinal))
                {
                    conflicts.Add(recoveryId);
                    ClearDailyInteractionMemoryMarker(line);
                    continue;
                }
                int markerBit = GetDailyInteractionMemoryMarkerBit(line.MemoryCommitPart);
                observedDailyMarkerMasks[recoveryId] =
                    (observedDailyMarkerMasks.TryGetValue(recoveryId, out int existingMask) ? existingMask : 0)
                    | markerBit;
            }
        }

        foreach (KeyValuePair<string, List<MyBehavior.DialogueDay>> ownerEntry in
            _port.History() ?? new Dictionary<string, List<MyBehavior.DialogueDay>>())
        {
            string ownerId = MemoryRecordRules.NormalizeMemoryHeroId(ownerEntry.Key);
            foreach (MyBehavior.DialogueDay day in ownerEntry.Value ?? new List<MyBehavior.DialogueDay>())
            {
                if (day?.MemoryCommitMarkers == null)
                {
                    continue;
                }
                foreach (KeyValuePair<string, string> marker in day.MemoryCommitMarkers.ToList())
                {
                    observedMarkers++;
                    if (!TryParseMemoryCommitMarkerKey(marker.Key, out string recoveryId, out string part)
                        || !IsValidMemoryCommitMarker(recoveryId, part, marker.Value))
                    {
                        day.MemoryCommitMarkers.Remove(marker.Key);
                        continue;
                    }
                    if (!retained.TryGetValue(recoveryId, out InteractionMemoryRecoveryRetention retention))
                    {
                        day.MemoryCommitMarkers.Remove(marker.Key);
                        continue;
                    }
                    if (!string.Equals(ownerId, MemoryRecordRules.NormalizeMemoryHeroId(retention.SubjectId), StringComparison.OrdinalIgnoreCase)
                        || !string.Equals(marker.Value, retention.PayloadHash, StringComparison.Ordinal))
                    {
                        conflicts.Add(recoveryId);
                        day.MemoryCommitMarkers.Remove(marker.Key);
                        continue;
                    }
                    int markerBit = GetRecentInteractionMemoryMarkerBit(part);
                    observedRecentMarkerMasks[recoveryId] =
                        (observedRecentMarkerMasks.TryGetValue(recoveryId, out int existingMask) ? existingMask : 0)
                        | markerBit;
                }
            }
            ownerEntry.Value?.RemoveAll(day => day != null
                && (day.Lines == null || day.Lines.Count == 0)
                && (day.MemoryCommitMarkers == null || day.MemoryCommitMarkers.Count == 0));
        }

        if (observedMarkers > MaximumPersistedMemoryCommitMarkers * 2)
        {
            ledger.DisableForCurrentCampaign("memory_recovery_owner_marker_overflow");
            _port.Log("MemoryRecovery", "[ERROR] owner marker envelope exceeded validation limit");
            return;
        }
        foreach (InteractionMemoryRecoveryRetention retention in retained.Values)
        {
            int expectedDailyMask = retention.IsPending
                ? retention.AppliedDailyMarkerMask
                : retention.ExpectedMarkerMask & 0x15;
            int observedDailyMask = observedDailyMarkerMasks.TryGetValue(retention.RecoveryId, out int dailyValue)
                ? dailyValue
                : 0;
            bool dailyStorageWasSealed = retention.DailyStorageDay >= 0
                && _state.HasBlock(retention.SubjectId, retention.DailyStorageDay);
            if (!dailyStorageWasSealed
                && (observedDailyMask & expectedDailyMask) != expectedDailyMask)
            {
                conflicts.Add(retention.RecoveryId);
            }
            int expectedRecentMask = retention.IsPending
                ? retention.AppliedRecentMarkerMask
                : retention.ExpectedMarkerMask & 0x2A;
            int observedMask = observedRecentMarkerMasks.TryGetValue(retention.RecoveryId, out int value)
                ? value
                : 0;
            if ((observedMask & expectedRecentMask) != expectedRecentMask)
            {
                conflicts.Add(retention.RecoveryId);
            }
        }
        foreach (string recoveryId in conflicts)
        {
            ledger.QuarantineEntry(recoveryId, "memory_recovery_owner_marker_conflict");
        }
        PruneEvictedInteractionMemoryRecoveryMarkers();
    }
internal static int GetDailyInteractionMemoryMarkerBit(string part)
    {
        if (string.Equals(part, "user", StringComparison.Ordinal))
        {
            return 1 << 0;
        }
        if (string.Equals(part, "fact", StringComparison.Ordinal))
        {
            return 1 << 2;
        }
        if (string.Equals(part, "assistant", StringComparison.Ordinal))
        {
            return 1 << 4;
        }
        return 0;
    }
internal static int GetRecentInteractionMemoryMarkerBit(string part)
    {
        if (string.Equals(part, "user", StringComparison.Ordinal))
        {
            return 1 << 1;
        }
        if (string.Equals(part, "fact", StringComparison.Ordinal))
        {
            return 1 << 3;
        }
        if (string.Equals(part, "assistant", StringComparison.Ordinal))
        {
            return 1 << 5;
        }
        return 0;
    }
internal void ProcessOneInteractionMemoryRecoveryOnTick()
    {
        if (Volatile.Read(ref HasWork) == 0
            || DateTime.UtcNow.Ticks < Interlocked.Read(ref NextAttemptTicks))
        {
            return;
        }
        if (!TWParallel.IsMainThread())
        {
            return;
        }
        using (_port.TickScope())
        {
            InteractionMemoryRecoveryLedger ledger = EnsureInteractionMemoryRecoveryLedger();
            if (ledger.TryGetNextWork(out InteractionMemoryRecoveryWorkItem work))
            {
                TryApplyInteractionMemoryRecoveryWork(work);
            }
            RefreshInteractionMemoryRecoveryWorkFlag();
        }
    }
internal void PruneEvictedInteractionMemoryRecoveryMarkers()
    {
        IReadOnlyList<InteractionMemoryRecoveryEviction> evicted =
            EnsureInteractionMemoryRecoveryLedger().DrainMarkerEvictions();
        foreach (InteractionMemoryRecoveryEviction item in evicted)
        {
            string recoveryId = (item?.RecoveryId ?? string.Empty).Trim();
            if (!IsMemoryRecoveryHexDigest(recoveryId))
            {
                continue;
            }
            string subjectId = MemoryRecordRules.NormalizeMemoryHeroId(item.SubjectId);
            if (!string.IsNullOrWhiteSpace(subjectId))
            {
                ClearDailyInteractionMemoryMarkers(subjectId, recoveryId);
                ClearRecentInteractionMemoryMarkers(subjectId, recoveryId);
            }
        }
    }
internal bool HasPublishedDailyInteractionMemoryComponent(
        string subjectId,
        string recoveryId,
        string payloadHash,
        string part)
    {
        string memoryId = MemoryRecordRules.NormalizeMemoryHeroId(subjectId);
        string normalizedPart = (part ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(memoryId)
            || string.IsNullOrWhiteSpace(recoveryId)
            || string.IsNullOrWhiteSpace(payloadHash)
            || string.IsNullOrWhiteSpace(normalizedPart)
            || _state.Drafts == null
            || !_state.Drafts.TryGetValue(memoryId, out List<DailyMemoryDraft> drafts)
            || drafts == null)
        {
            return false;
        }

        return drafts
            .Where(draft => draft?.Lines != null)
            .SelectMany(draft => draft.Lines)
            .Any(line => line != null
                && string.Equals((line.MemoryCommitId ?? string.Empty).Trim(), recoveryId, StringComparison.Ordinal)
                && string.Equals((line.MemoryCommitHash ?? string.Empty).Trim(), payloadHash, StringComparison.Ordinal)
                && string.Equals((line.MemoryCommitPart ?? string.Empty).Trim(), normalizedPart, StringComparison.Ordinal));
    }
internal bool ClearDailyInteractionMemoryMarkers(string subjectId, string recoveryId)
    {
        string normalized = MemoryRecordRules.NormalizeMemoryHeroId(subjectId);
        if (string.IsNullOrWhiteSpace(normalized) || _state.Drafts == null
            || !_state.Drafts.TryGetValue(normalized, out List<DailyMemoryDraft> drafts))
        {
            return false;
        }
        bool found = false;
        foreach (DailyMemoryLine line in (drafts ?? new List<DailyMemoryDraft>())
            .Where(draft => draft?.Lines != null)
            .SelectMany(draft => draft.Lines)
            .Where(line => line != null && string.Equals(line.MemoryCommitId, recoveryId, StringComparison.Ordinal)))
        {
            found = true;
            ClearDailyInteractionMemoryMarker(line);
        }
        return found;
    }
internal static void ClearDailyInteractionMemoryMarker(DailyMemoryLine line)
    {
        if (line == null)
        {
            return;
        }
        line.MemoryCommitId = string.Empty;
        line.MemoryCommitPart = string.Empty;
        line.MemoryCommitHash = string.Empty;
        line.MemoryCommitOriginGameDay = -1;
        line.MemoryCommitOriginGameDate = string.Empty;
    }
internal bool ClearRecentInteractionMemoryMarkers(string subjectId, string recoveryId)
    {
        string normalized = MemoryRecordRules.NormalizeMemoryHeroId(subjectId);
        if (string.IsNullOrWhiteSpace(normalized) || _port.History() == null
            || !_port.History().TryGetValue(normalized, out List<MyBehavior.DialogueDay> records))
        {
            return false;
        }
        bool found = false;
        string prefix = recoveryId + ":";
        foreach (MyBehavior.DialogueDay day in records ?? new List<MyBehavior.DialogueDay>())
        {
            if (day?.MemoryCommitMarkers == null)
            {
                continue;
            }
            List<string> keys = day.MemoryCommitMarkers.Keys
                .Where(key => (key ?? string.Empty).StartsWith(prefix, StringComparison.Ordinal))
                .ToList();
            foreach (string key in keys)
            {
                found |= day.MemoryCommitMarkers.Remove(key);
            }
        }
        records?.RemoveAll(day => day != null
            && (day.Lines == null || day.Lines.Count == 0)
            && (day.MemoryCommitMarkers == null || day.MemoryCommitMarkers.Count == 0));
        return found;
    }
private sealed class InteractionMemoryRecoveryPermanentException : Exception
    {
        internal InteractionMemoryRecoveryPermanentException(string errorCode)
            : base(errorCode)
        {
            ErrorCode = string.IsNullOrWhiteSpace(errorCode)
                ? "memory_recovery_permanent_failure"
                : errorCode.Trim();
        }

        internal string ErrorCode { get; }
    }
}
internal sealed class MemoryRecoveryPort {
 internal Func<string,bool> IsEntityEligible;internal Func<int> CurrentDay;internal Func<string> CurrentDate;
 internal Func<Dictionary<string,List<MyBehavior.DialogueDay>>> History;
 internal Func<string,List<MyBehavior.DialogueDay>> LoadHistory;
 internal Action<string,List<MyBehavior.DialogueDay>> SaveHistory;
 internal Func<List<MyBehavior.DialogueDay>,bool> RemoveExpiredFacts;
 internal Action<string,string> Log;internal Func<IDisposable> TickScope;
}
