using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Runtime;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace AnimusForge;

public sealed partial class PlayerNotorietyBehavior
{
    private NotorietyConversationOutcomeLedger _notorietyConversationOutcomeLedger =
        new NotorietyConversationOutcomeLedger();
    private bool _notorietyConversationOutcomeImportConfirmed = true;
    private string _expectedNotorietyFinalizeMemorySessionKey = string.Empty;
    private bool _allowStaleExactNotorietyFinalize;

    private static bool IsSocialReportsBridgeEnabled()
    {
        return FeatureBridgeRuntime.IsEnabled(FeatureBridgeIds.MemorySocialReports);
    }

    internal static NotorietyConversationOutcomeOperationStatus
        NoteConversationLineRecoverableForExternal(
            string subjectId,
            string memorySessionKey,
            long runtimeGeneration,
            long saveGeneration,
            int originDay,
            int originHour,
            string recoveryId,
            string payloadHash,
            string part)
    {
        try
        {
            if (!IsSocialReportsBridgeEnabled())
            {
                return NotorietyConversationOutcomeOperationStatus.NotReady;
            }
            if (!TWParallel.IsMainThread())
            {
                return NotorietyConversationOutcomeOperationStatus.Rejected;
            }
            PlayerNotorietyBehavior owner = Instance;
            if (owner == null || !owner._notorietyConversationOutcomeImportConfirmed)
            {
                return NotorietyConversationOutcomeOperationStatus.NotReady;
            }
            if (runtimeGeneration > 0L
                && !SaveRuntimeGuard.IsCurrentGeneration(runtimeGeneration))
            {
                return NotorietyConversationOutcomeOperationStatus.Rejected;
            }
            return owner.NoteConversationLineRecoverable(
                subjectId,
                memorySessionKey,
                runtimeGeneration,
                saveGeneration,
                originDay,
                originHour,
                recoveryId,
                payloadHash,
                part);
        }
        catch (Exception ex)
        {
            Logger.Log("PlayerNotoriety", "[WARN] exact line owner isolated: " + ex.Message);
            return NotorietyConversationOutcomeOperationStatus.NotReady;
        }
    }

    internal static void FinalizeConversationForExternal(
        IEnumerable<CharacterObject> characters,
        string memorySessionKey)
    {
        try
        {
            if (!IsSocialReportsBridgeEnabled())
            {
                Logger.Log("PlayerNotoriety", "[INFO] social reports bridge disabled; exact finalize skipped");
                return;
            }
            Instance?.FinalizeConversationWithExpectedSession(characters, memorySessionKey);
        }
        catch (Exception ex)
        {
            Logger.Log("PlayerNotoriety", "[WARN] exact finalize owner isolated: " + ex.Message);
        }
    }

    private NotorietyConversationOutcomeOperationStatus NoteConversationLineRecoverable(
        string subjectId,
        string memorySessionKey,
        long runtimeGeneration,
        long saveGeneration,
        int originDay,
        int originHour,
        string recoveryId,
        string payloadHash,
        string part)
    {
        string normalizedSubjectId = NormalizeObserverKey(subjectId);
        string runtimeId = runtimeGeneration.ToString(CultureInfo.InvariantCulture);
        string saveId = saveGeneration.ToString(CultureInfo.InvariantCulture);
        if (string.IsNullOrWhiteSpace(normalizedSubjectId)
            || normalizedSubjectId == PlayerHeroId
            || !NotorietyConversationLineFingerprintHelper.TryBuildSessionIdentity(
                normalizedSubjectId,
                memorySessionKey,
                runtimeId,
                saveId,
                originDay,
                originHour,
                out string receiptId,
                out _,
                out _)
            || !NotorietyConversationLineFingerprintHelper.TryBuildLineId(
                recoveryId,
                payloadHash,
                part,
                originDay,
                originHour,
                out string lineId,
                out _))
        {
            return NotorietyConversationOutcomeOperationStatus.Rejected;
        }

        NotorietyConversationOutcomeLedger current = EnsureNotorietyConversationOutcomeLedger();
        NotorietyConversationOutcomeOperationStatus probe = current.ProbeLine(
            receiptId,
            lineId,
            recoveryId,
            payloadHash,
            part,
            originDay,
            originHour,
            out NotorietyConversationOutcomeReceipt existing,
            out _);
        if (probe == NotorietyConversationOutcomeOperationStatus.Duplicate)
        {
            return probe;
        }
        if (probe != NotorietyConversationOutcomeOperationStatus.NotFound)
        {
            return probe;
        }
        if (existing != null && existing.State != NotorietyConversationOutcomeState.Open)
        {
            // A loaded Open receipt becomes Unknown. Its applied line tombstones
            // still deduplicate known lines, but a missing line must never reopen
            // the session or roll again.
            return NotorietyConversationOutcomeOperationStatus.NotReady;
        }
        if (existing == null
            && current.PendingCount >= NotorietyConversationOutcomeLedger.MaximumPendingEntries)
        {
            return NotorietyConversationOutcomeOperationStatus.CapacityExceeded;
        }

        ActiveConversationState active;
        if (!_activeConversationStates.TryGetValue(
                normalizedSubjectId,
                out active)
            || active == null)
        {
            if (existing != null
                || current.GetOpenForSubject(normalizedSubjectId).Count > 0)
            {
                return NotorietyConversationOutcomeOperationStatus.NotReady;
            }
            Hero observer = FindHeroById(normalizedSubjectId);
            active = IsValidObserver(observer)
                ? GetOrCreateActiveConversation(observer)
                : GetOrCreateActiveConversation(normalizedSubjectId, string.Empty);
        }
        string normalizedMemorySessionKey =
            (memorySessionKey ?? string.Empty).Trim();
        if (active != null
            && !string.IsNullOrWhiteSpace(active.ExactOutcomeReceiptId)
            && !string.Equals(
                active.ExactMemorySessionKey,
                normalizedMemorySessionKey,
                StringComparison.Ordinal))
        {
            // Exact sessions must never share the legacy hero-only bucket.
            // Finalize the prior witnessed session by absolute target before
            // accepting the first line of the new channel/session.
            FinalizeStaleNotorietyConversationByHeroId(normalizedSubjectId);
            _activeConversationStates.TryGetValue(normalizedSubjectId, out active);
            if (active == null)
            {
                Hero observer = FindHeroById(normalizedSubjectId);
                active = IsValidObserver(observer)
                    ? GetOrCreateActiveConversation(observer)
                    : GetOrCreateActiveConversation(normalizedSubjectId, string.Empty);
            }
        }
        if (active == null
            || active.HasLegacyLines
            || (!string.IsNullOrWhiteSpace(active.ExactOutcomeReceiptId)
                && (!string.Equals(active.ExactOutcomeReceiptId, receiptId, StringComparison.Ordinal)
                    || !string.Equals(
                        active.ExactMemorySessionKey,
                        normalizedMemorySessionKey,
                        StringComparison.Ordinal))))
        {
            return NotorietyConversationOutcomeOperationStatus.Conflict;
        }

        if (!NotorietyConversationOutcomeCandidate.TryCreate(
            normalizedSubjectId,
            memorySessionKey,
            runtimeId,
            saveId,
            active.StartDay,
            active.StartHour,
            active.KnownRollChance,
            active.KnowsMajorThisSession,
            out NotorietyConversationOutcomeCandidate candidate,
            out _))
        {
            return NotorietyConversationOutcomeOperationStatus.Rejected;
        }
        if (!string.Equals(candidate.ReceiptId, receiptId, StringComparison.Ordinal))
        {
            return NotorietyConversationOutcomeOperationStatus.Conflict;
        }

        NotorietyConversationOutcomeLedger staged = current.Clone();
        NotorietyConversationOutcomeOperationStatus prepared = staged.Prepare(
            candidate,
            DateTime.UtcNow.Ticks,
            out _,
            out _);
        if (prepared != NotorietyConversationOutcomeOperationStatus.Accepted
            && prepared != NotorietyConversationOutcomeOperationStatus.Duplicate)
        {
            PublishNotorietyConversationOutcomeLedgerIfConflict(staged, prepared);
            return prepared;
        }
        NotorietyConversationOutcomeOperationStatus added = staged.AddLine(
            candidate.ReceiptId,
            candidate.CandidateHash,
            lineId,
            recoveryId,
            payloadHash,
            part,
            originDay,
            originHour,
            DateTime.UtcNow.Ticks,
            out NotorietyConversationOutcomeReceipt addedReceipt,
            out _);
        if (added == NotorietyConversationOutcomeOperationStatus.Duplicate)
        {
            return added;
        }
        if (added != NotorietyConversationOutcomeOperationStatus.Accepted)
        {
            PublishNotorietyConversationOutcomeLedgerIfConflict(staged, added);
            return added;
        }

        PlayerNpcKnowledgeState currentKnowledge = GetNpcKnowledgeState(
            normalizedSubjectId,
            create: true);
        if (currentKnowledge == null)
        {
            return NotorietyConversationOutcomeOperationStatus.Rejected;
        }
        PlayerNpcKnowledgeState nextKnowledge = CloneNpcKnowledgeState(currentKnowledge);
        if (active.KnowsMajorThisSession)
        {
            nextKnowledge.KnowsMajorHistory = true;
            if (nextKnowledge.KnownAtDay < 0)
            {
                nextKnowledge.KnownAtDay = Math.Max(0, active.StartDay);
            }
        }

        // Both assignments are main-thread, in-memory owner publication with no
        // callback between them. The existing Notoriety JSON serializes the
        // aggregate and AFNR1 witness together on the next game save.
        Dictionary<string, string> stagedStorage = staged.Export();
        _state.NpcKnowledge[normalizedSubjectId] = nextKnowledge;
        PublishNotorietyConversationOutcomeLedger(staged, stagedStorage);
        active.ExactOutcomeReceiptId = candidate.ReceiptId;
        active.ExactOutcomeCandidateHash = candidate.CandidateHash;
        active.ExactMemorySessionKey = normalizedMemorySessionKey;
        active.LineCount = addedReceipt?.LineIds?.Count ?? active.LineCount + 1;
        active.LastDay = originDay;
        active.LastHour = originHour;
        return HasExactNotorietyLineReadback(
                normalizedSubjectId,
                active,
                lineId)
            ? NotorietyConversationOutcomeOperationStatus.Accepted
            : NotorietyConversationOutcomeOperationStatus.NotReady;
    }

    private void PublishKnownRollForLegacyLine(ActiveConversationState active)
    {
        if (active?.KnowsMajorThisSession != true)
        {
            return;
        }
        PlayerNpcKnowledgeState state = GetNpcKnowledgeState(active.HeroId, create: true);
        if (state == null)
        {
            return;
        }
        state.KnowsMajorHistory = true;
        if (state.KnownAtDay < 0)
        {
            state.KnownAtDay = Math.Max(0, active.StartDay);
        }
    }

    private void DowngradeExactNotorietyOutcomeToLegacy(
        ActiveConversationState active)
    {
        if (active == null
            || string.IsNullOrWhiteSpace(active.ExactOutcomeReceiptId)
            || string.IsNullOrWhiteSpace(active.ExactOutcomeCandidateHash)
            || !_notorietyConversationOutcomeImportConfirmed)
        {
            return;
        }
        NotorietyConversationOutcomeLedger staged =
            EnsureNotorietyConversationOutcomeLedger().Clone();
        NotorietyConversationOutcomeOperationStatus status = staged.Finish(
            active.ExactOutcomeReceiptId,
            active.ExactOutcomeCandidateHash,
            NotorietyConversationOutcomeState.Unknown,
            "notoriety_conversation_mixed_legacy_line",
            DateTime.UtcNow.Ticks,
            out _);
        if (status == NotorietyConversationOutcomeOperationStatus.Accepted
            || status == NotorietyConversationOutcomeOperationStatus.Duplicate)
        {
            PublishNotorietyConversationOutcomeLedger(staged);
            active.ExactOutcomeReceiptId = string.Empty;
            active.ExactOutcomeCandidateHash = string.Empty;
            active.ExactMemorySessionKey = string.Empty;
        }
    }

    private bool TryFinalizeExactNotorietyConversation(
        string subjectId,
        ActiveConversationState active,
        PlayerNpcKnowledgeState state,
        out bool handled)
    {
        handled = active != null
            && !string.IsNullOrWhiteSpace(active.ExactOutcomeReceiptId);
        if (!handled)
        {
            return false;
        }
        if (!_notorietyConversationOutcomeImportConfirmed
            || active.HasLegacyLines
            || active.LineCount <= 0
            || (!_allowStaleExactNotorietyFinalize
                && (string.IsNullOrWhiteSpace(_expectedNotorietyFinalizeMemorySessionKey)
                    || !string.Equals(
                        active.ExactMemorySessionKey,
                        _expectedNotorietyFinalizeMemorySessionKey,
                        StringComparison.Ordinal))))
        {
            return false;
        }

        NotorietyConversationOutcomeLedger staged =
            EnsureNotorietyConversationOutcomeLedger().Clone();
        bool known = state.KnowsMajorHistory || active.KnowsMajorThisSession;
        int knownDay = known
            ? state.KnownAtDay >= 0 ? state.KnownAtDay : Math.Max(0, active.StartDay)
            : -1;
        double bonus = known
            ? state.PersonalKnownBonus
            : ClampPercentDouble(
                state.PersonalKnownBonus + active.LineCount * PersonalKnownBonusPerLine);
        int completedSessions = Math.Max(0, state.CompletedConversationSessions) + 1;
        int lastDay = Math.Max(0, GetCurrentGameDayIndex());
        if (!NotorietyConversationFinalizeTarget.TryCreate(
            known,
            knownDay,
            bonus,
            completedSessions,
            lastDay,
            out NotorietyConversationFinalizeTarget target,
            out _))
        {
            return false;
        }
        NotorietyConversationOutcomeOperationStatus confirmed = staged.Confirm(
            active.ExactOutcomeReceiptId,
            active.ExactOutcomeCandidateHash,
            target,
            DateTime.UtcNow.Ticks,
            out NotorietyConversationOutcomeReceipt confirmedReceipt,
            out _);
        if (confirmed != NotorietyConversationOutcomeOperationStatus.Accepted
            && confirmed != NotorietyConversationOutcomeOperationStatus.Duplicate)
        {
            PublishNotorietyConversationOutcomeLedgerIfConflict(staged, confirmed);
            return false;
        }

        PlayerNpcKnowledgeState next = CloneNpcKnowledgeState(state);
        ApplyNotorietyFinalizeTarget(next, confirmedReceipt?.FinalizeTarget ?? target);
        if (!MatchesNotorietyFinalizeTarget(next, target))
        {
            return false;
        }
        NotorietyConversationOutcomeOperationStatus applied = staged.MarkApplied(
            active.ExactOutcomeReceiptId,
            active.ExactOutcomeCandidateHash,
            target.TargetHash,
            DateTime.UtcNow.Ticks,
            out _);
        if (applied != NotorietyConversationOutcomeOperationStatus.Accepted
            && applied != NotorietyConversationOutcomeOperationStatus.Duplicate)
        {
            PublishNotorietyConversationOutcomeLedgerIfConflict(staged, applied);
            return false;
        }

        Dictionary<string, string> stagedStorage = staged.Export();
        _state.NpcKnowledge[subjectId] = next;
        PublishNotorietyConversationOutcomeLedger(staged, stagedStorage);
        LogDebug("finalize exact conversation observer=" + subjectId
            + " lines=" + active.LineCount
            + " receipt=" + active.ExactOutcomeReceiptId);
        return true;
    }

    private void FinalizeConversationWithExpectedSession(
        IEnumerable<CharacterObject> characters,
        string memorySessionKey)
    {
        string previous = _expectedNotorietyFinalizeMemorySessionKey;
        _expectedNotorietyFinalizeMemorySessionKey =
            (memorySessionKey ?? string.Empty).Trim();
        try
        {
            FinalizeConversation(characters);
        }
        finally
        {
            _expectedNotorietyFinalizeMemorySessionKey = previous;
        }
    }

    private void FinalizeStaleNotorietyConversationByHeroId(string heroId)
    {
        bool previous = _allowStaleExactNotorietyFinalize;
        _allowStaleExactNotorietyFinalize = true;
        try
        {
            FinalizeConversationByHeroId(heroId);
        }
        finally
        {
            _allowStaleExactNotorietyFinalize = previous;
        }
    }

    private void AbandonOpenNotorietyConversationOutcomes(string diagnosticCode)
    {
        if (!_notorietyConversationOutcomeImportConfirmed)
        {
            return;
        }
        NotorietyConversationOutcomeLedger staged =
            EnsureNotorietyConversationOutcomeLedger().Clone();
        bool changed = false;
        foreach (ActiveConversationState active in _activeConversationStates.Values)
        {
            if (active == null
                || string.IsNullOrWhiteSpace(active.ExactOutcomeReceiptId)
                || string.IsNullOrWhiteSpace(active.ExactOutcomeCandidateHash))
            {
                continue;
            }
            NotorietyConversationOutcomeOperationStatus status = staged.Finish(
                active.ExactOutcomeReceiptId,
                active.ExactOutcomeCandidateHash,
                NotorietyConversationOutcomeState.Unknown,
                diagnosticCode,
                DateTime.UtcNow.Ticks,
                out _);
            changed |= status == NotorietyConversationOutcomeOperationStatus.Accepted
                || status == NotorietyConversationOutcomeOperationStatus.Duplicate;
        }
        if (changed)
        {
            PublishNotorietyConversationOutcomeLedger(staged);
        }
    }

    private void PrepareNotorietyConversationOutcomeStorageForSave()
    {
        if (_notorietyConversationOutcomeImportConfirmed)
        {
            _state.ConversationOutcomeReceipts =
                EnsureNotorietyConversationOutcomeLedger().Export();
        }
    }

    private void ActivateNotorietyConversationOutcomeStorageAfterLoad()
    {
        var staged = new NotorietyConversationOutcomeLedger();
        Dictionary<string, string> raw = _state.ConversationOutcomeReceipts
            ?? new Dictionary<string, string>(StringComparer.Ordinal);
        if (!staged.Import(raw, out string errorCode))
        {
            _notorietyConversationOutcomeImportConfirmed = false;
            Logger.Log("PlayerNotoriety",
                "[WARN] outcome recovery disabled; invalid embedded journal preserved error="
                    + errorCode);
            return;
        }
        _notorietyConversationOutcomeLedger = staged;
        _notorietyConversationOutcomeImportConfirmed = true;
        ReconcileConfirmedNotorietyConversationOutcomes();
        _state.ConversationOutcomeReceipts =
            EnsureNotorietyConversationOutcomeLedger().Export();
    }

    private void ResetNotorietyConversationOutcomeStorageAfterFailedLoad()
    {
        _notorietyConversationOutcomeLedger =
            new NotorietyConversationOutcomeLedger();
        _notorietyConversationOutcomeImportConfirmed = true;
        _state.ConversationOutcomeReceipts =
            new Dictionary<string, string>(StringComparer.Ordinal);
    }

    private void ReconcileConfirmedNotorietyConversationOutcomes()
    {
        int guard = NotorietyConversationOutcomeLedger.MaximumPendingEntries;
        while (guard-- > 0 && EnsureNotorietyConversationOutcomeLedger().GetConfirmedWork(
            out NotorietyConversationConfirmedWorkItem work))
        {
            PlayerNpcKnowledgeState current = GetNpcKnowledgeState(
                work.SubjectId,
                create: true);
            if (current == null || work.Target == null)
            {
                break;
            }
            PlayerNpcKnowledgeState next = CloneNpcKnowledgeState(current);
            ApplyNotorietyFinalizeTarget(next, work.Target);
            if (!MatchesNotorietyFinalizeTarget(next, work.Target))
            {
                break;
            }
            NotorietyConversationOutcomeLedger staged =
                EnsureNotorietyConversationOutcomeLedger().Clone();
            NotorietyConversationOutcomeOperationStatus applied = staged.MarkApplied(
                work.ReceiptId,
                work.CandidateHash,
                work.Target.TargetHash,
                DateTime.UtcNow.Ticks,
                out _);
            if (applied != NotorietyConversationOutcomeOperationStatus.Accepted
                && applied != NotorietyConversationOutcomeOperationStatus.Duplicate)
            {
                break;
            }
            Dictionary<string, string> stagedStorage = staged.Export();
            _state.NpcKnowledge[work.SubjectId] = next;
            PublishNotorietyConversationOutcomeLedger(staged, stagedStorage);
        }
    }

    private NotorietyConversationOutcomeLedger EnsureNotorietyConversationOutcomeLedger()
    {
        if (_notorietyConversationOutcomeLedger == null)
        {
            _notorietyConversationOutcomeLedger =
                new NotorietyConversationOutcomeLedger();
        }
        return _notorietyConversationOutcomeLedger;
    }

    private void PublishNotorietyConversationOutcomeLedger(
        NotorietyConversationOutcomeLedger ledger)
    {
        NotorietyConversationOutcomeLedger published = ledger
            ?? new NotorietyConversationOutcomeLedger();
        PublishNotorietyConversationOutcomeLedger(
            published,
            published.Export());
    }

    private void PublishNotorietyConversationOutcomeLedger(
        NotorietyConversationOutcomeLedger ledger,
        Dictionary<string, string> storage)
    {
        _notorietyConversationOutcomeLedger = ledger
            ?? new NotorietyConversationOutcomeLedger();
        _state.ConversationOutcomeReceipts = storage
            ?? new Dictionary<string, string>(StringComparer.Ordinal);
    }

    private void PublishNotorietyConversationOutcomeLedgerIfConflict(
        NotorietyConversationOutcomeLedger ledger,
        NotorietyConversationOutcomeOperationStatus status)
    {
        if (status == NotorietyConversationOutcomeOperationStatus.Conflict)
        {
            PublishNotorietyConversationOutcomeLedger(ledger);
        }
    }

    private bool HasExactNotorietyLineReadback(
        string subjectId,
        ActiveConversationState active,
        string lineId)
    {
        if (active == null
            || string.IsNullOrWhiteSpace(active.ExactOutcomeReceiptId)
            || string.IsNullOrWhiteSpace(active.ExactOutcomeCandidateHash))
        {
            return false;
        }
        NotorietyConversationOutcomeReceipt receipt =
            EnsureNotorietyConversationOutcomeLedger().GetEntries()
                .FirstOrDefault(item => item != null
                    && string.Equals(
                        item.ReceiptId,
                        active.ExactOutcomeReceiptId,
                        StringComparison.Ordinal)
                    && string.Equals(
                        item.CandidateHash,
                        active.ExactOutcomeCandidateHash,
                        StringComparison.Ordinal));
        PlayerNpcKnowledgeState knowledge = GetNpcKnowledgeState(
            subjectId,
            create: false);
        return receipt?.LineIds?.Contains(lineId) == true
            && (!active.KnowsMajorThisSession
                || knowledge?.KnowsMajorHistory == true);
    }

    private static PlayerNpcKnowledgeState CloneNpcKnowledgeState(
        PlayerNpcKnowledgeState source)
        => new PlayerNpcKnowledgeState
        {
            HeroId = source?.HeroId ?? string.Empty,
            KnowsMajorHistory = source?.KnowsMajorHistory == true,
            KnownAtDay = source?.KnownAtDay ?? -1,
            PersonalKnownBonus = source?.PersonalKnownBonus ?? 0.0,
            CompletedConversationSessions = source?.CompletedConversationSessions ?? 0,
            LastConversationDay = source?.LastConversationDay ?? -1,
            LastCourierSentDistance = source?.LastCourierSentDistance ?? -1f,
            LastCourierSentDay = source?.LastCourierSentDay ?? -1
        };

    private static void ApplyNotorietyFinalizeTarget(
        PlayerNpcKnowledgeState state,
        NotorietyConversationFinalizeTarget target)
    {
        if (state == null || target == null)
        {
            return;
        }
        state.KnowsMajorHistory |= target.Known;
        if (target.Known && (state.KnownAtDay < 0
            || target.KnownDay >= 0 && target.KnownDay < state.KnownAtDay))
        {
            state.KnownAtDay = target.KnownDay;
        }
        state.PersonalKnownBonus = Math.Max(
            state.PersonalKnownBonus,
            target.Bonus);
        state.CompletedConversationSessions = Math.Max(
            state.CompletedConversationSessions,
            target.CompletedSessions);
        state.LastConversationDay = Math.Max(
            state.LastConversationDay,
            target.LastDay);
    }

    private static bool MatchesNotorietyFinalizeTarget(
        PlayerNpcKnowledgeState state,
        NotorietyConversationFinalizeTarget target)
        => state != null
            && target != null
            && (!target.Known || state.KnowsMajorHistory)
            && (!target.Known || state.KnownAtDay >= 0
                && state.KnownAtDay <= target.KnownDay)
            && state.PersonalKnownBonus + 0.000001 >= target.Bonus
            && state.CompletedConversationSessions >= target.CompletedSessions
            && state.LastConversationDay >= target.LastDay;
}
