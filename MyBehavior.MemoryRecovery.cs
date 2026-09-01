using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Runtime;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem;

namespace AnimusForge;

public partial class MyBehavior
{
    private const string InteractionMemoryRecoveryStorageKey = "_af_interactionMemoryRecovery_v1";
    private const int MaximumPersistedMemoryCommitMarkers =
        (InteractionMemoryRecoveryLedger.MaximumPendingEntries
            + InteractionMemoryRecoveryLedger.MaximumCompletedEntries)
        * InteractionMemoryRecoveryLedger.MaximumComponentCount;
    private const long InteractionMemoryRecoveryRetryDelayTicks = TimeSpan.TicksPerSecond * 5L;

    private InteractionMemoryRecoveryLedger _interactionMemoryRecoveryLedger =
        new InteractionMemoryRecoveryLedger();
    private Dictionary<string, string> _interactionMemoryRecoveryStorage =
        new Dictionary<string, string>(StringComparer.Ordinal);
    private int _hasInteractionMemoryRecoveryWork;
    private long _interactionMemoryRecoveryNextAttemptUtcTicks;
    private long _interactionMemoryRecoveryLoadedGeneration;
    private int _interactionMemoryRecoveryLoadImportConfirmed;

    /// <summary>
    /// Durable memory-only entry point. The projection intentionally accepts
    /// InteractionMemoryCommit rather than the whole pipeline result, so no action
    /// plan, postprocess payload, executor, or after-commit callback can enter
    /// the recovery journal or its tick path.
    /// </summary>
    internal static MemoryCommitResult CommitExternalDialogueHistoryRecoverable(
        InteractionMemoryCommit commit,
        bool isNonHero,
        string npcName)
    {
        try
        {
            if (!TWParallel.IsMainThread())
            {
                return new MemoryCommitResult(MemoryCommitStatus.Rejected, "memory_not_main_thread");
            }
            if (commit == null)
            {
                return new MemoryCommitResult(MemoryCommitStatus.Rejected, "missing_memory_commit");
            }
            MyBehavior owner = Campaign.Current?.GetCampaignBehavior<MyBehavior>();
            if (owner == null)
            {
                return new MemoryCommitResult(MemoryCommitStatus.Failed, "memory_owner_missing");
            }
            if (Interlocked.Read(ref owner._interactionMemoryRecoveryLoadedGeneration)
                != SaveRuntimeGuard.CurrentGeneration
                || Volatile.Read(ref owner._interactionMemoryRecoveryLoadImportConfirmed) == 0)
            {
                return new MemoryCommitResult(MemoryCommitStatus.Failed, "memory_recovery_not_activated");
            }
            string normalizedMemoryId = NormalizeMemoryHeroId(commit.SubjectId);
            if (string.IsNullOrEmpty(normalizedMemoryId) || isNonHero != IsNonHeroMemoryId(normalizedMemoryId))
            {
                return new MemoryCommitResult(MemoryCommitStatus.Rejected, "memory_identity_invalid");
            }
            if (string.IsNullOrWhiteSpace(commit.UserText)
                && string.IsNullOrWhiteSpace(commit.AssistantText)
                && !(commit.ConfirmedFacts ?? Array.Empty<FactRecord>()).Any(fact =>
                    fact != null && !string.IsNullOrWhiteSpace(fact.Text)))
            {
                return new MemoryCommitResult(MemoryCommitStatus.Rejected, "memory_empty_commit");
            }
            Hero hero = isNonHero
                ? null
                : (Hero.Find(commit.SubjectId.Trim()) ?? FindHeroById(normalizedMemoryId));
            if (!isNonHero && !IsHeroNpcEligibleForCompressedMemory(hero))
            {
                return new MemoryCommitResult(MemoryCommitStatus.Rejected, "memory_target_ineligible");
            }

            InteractionMemoryRecoverySeed seed = owner.BuildInteractionMemoryRecoverySeed(
                commit,
                normalizedMemoryId,
                isNonHero,
                npcName,
                hero);
            InteractionMemoryRecoveryLedger ledger = owner.EnsureInteractionMemoryRecoveryLedger();
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

            owner.RefreshInteractionMemoryRecoveryWorkFlag();
            owner.ProcessInteractionMemoryRecoveryEntry(recoveryId);
            if (ledger.IsCompleted(recoveryId))
            {
                owner.RefreshInteractionMemoryRecoveryWorkFlag();
                return new MemoryCommitResult(beginStatus == InteractionMemoryRecoveryBeginStatus.ExistingPending
                    ? MemoryCommitStatus.Duplicate
                    : MemoryCommitStatus.Applied);
            }
            owner.RefreshInteractionMemoryRecoveryWorkFlag();
            return new MemoryCommitResult(MemoryCommitStatus.Failed, "memory_recovery_pending");
        }
        catch (Exception ex)
        {
            Logger.Log("MemoryRecovery", "[ERROR] recoverable commit failed: " + ex.Message);
            return new MemoryCommitResult(MemoryCommitStatus.Failed, "memory_recovery_commit_failed");
        }
    }

    private InteractionMemoryRecoverySeed BuildInteractionMemoryRecoverySeed(
        InteractionMemoryCommit commit,
        string normalizedMemoryId,
        bool isNonHero,
        string npcName,
        Hero hero)
    {
        string memoryName = string.IsNullOrWhiteSpace(npcName)
            ? hero?.Name?.ToString() ?? "NPC"
            : npcName.Trim();
        if (string.IsNullOrWhiteSpace(memoryName))
        {
            memoryName = "NPC";
        }
        Hero memoryHero = hero ?? FindHeroById(normalizedMemoryId);
        string userText = (commit.UserText ?? string.Empty).Trim();
        string assistantText = (commit.AssistantText ?? string.Empty).Trim();
        string factsText = string.Join("\n", (commit.ConfirmedFacts ?? Array.Empty<FactRecord>())
            .Where(fact => fact != null && !string.IsNullOrWhiteSpace(fact.Text))
            .Select(fact => fact.Text.Trim()));

        string renderedUser = string.IsNullOrWhiteSpace(userText)
            ? string.Empty
            : BuildPlayerAddressedInputForName(memoryName, userText, memoryHero, commit.TargetName);
        string renderedFact = RenderInteractionMemoryFact(factsText);
        string renderedAssistant = RenderInteractionMemoryAssistant(memoryName, assistantText);
        int currentDay = GetCurrentGameDayIndexSafe();
        bool hasDetachedProvenance = commit.RuntimeGeneration > 0L || commit.SaveGeneration > 0L
            || !string.IsNullOrWhiteSpace(commit.TraceId);
        int originDay = hasDetachedProvenance ? Math.Max(0, commit.GameDay) : currentDay;
        int originHour = hasDetachedProvenance
            ? Math.Max(0, Math.Min(23, commit.GameHour))
            : GetCurrentHourOfDaySafeForPrompt();
        string originDate = originDay == currentDay
            ? GetCurrentGameDateTextSafe()
            : "day:" + originDay.ToString(CultureInfo.InvariantCulture);
        string originScene = !string.IsNullOrWhiteSpace(commit.LocationId)
            ? commit.LocationId.Trim()
            : ResolveCurrentMemorySceneLabel();
        int sceneSessionId = commit.Channel == InteractionChannel.SceneShout
            ? Math.Max(-1, commit.SceneSessionId)
            : -1;
        int dialogueSessionId = commit.Channel == InteractionChannel.NativeConversation
            ? GetOrStartActiveNativeConversationMemorySessionId()
            : -1;
        string memorySessionKey = BuildInteractionMemoryRecoverySessionKey(
            commit,
            sceneSessionId,
            dialogueSessionId);

        return new InteractionMemoryRecoverySeed
        {
            CommitId = commit.CommitId,
            Channel = (int)commit.Channel,
            SessionId = commit.SessionId,
            SubjectId = normalizedMemoryId,
            IsNonHero = isNonHero,
            NpcName = memoryName,
            RuntimeGeneration = commit.RuntimeGeneration,
            SaveGeneration = commit.SaveGeneration,
            TraceId = commit.TraceId,
            OriginGameDay = originDay,
            OriginGameDate = originDate,
            OriginGameHour = originHour,
            OriginScene = originScene,
            DailyStorageDay = originDay,
            DailyStorageDate = originDate,
            SceneSessionId = sceneSessionId,
            DialogueSessionId = dialogueSessionId,
            MemorySessionKey = memorySessionKey,
            TargetAgentIndex = commit.Channel == InteractionChannel.SceneShout
                ? Math.Max(-1, commit.TargetAgentIndex)
                : -1,
            TargetName = commit.TargetName,
            Components = new[]
            {
                new InteractionMemoryRecoveryComponentSeed
                {
                    Part = "user",
                    DailySpeaker = string.IsNullOrWhiteSpace(renderedUser)
                        ? string.Empty
                        : BuildPlayerPublicDisplayNameForPrompt(memoryHero),
                    DailyText = renderedUser,
                    RecentText = renderedUser,
                    IsAfef = false,
                    IsLlmDialogue = true
                },
                new InteractionMemoryRecoveryComponentSeed
                {
                    Part = "fact",
                    DailySpeaker = string.IsNullOrWhiteSpace(renderedFact) ? string.Empty : "AFEF",
                    DailyText = renderedFact,
                    RecentText = renderedFact,
                    IsAfef = true,
                    IsLlmDialogue = false
                },
                new InteractionMemoryRecoveryComponentSeed
                {
                    Part = "assistant",
                    DailySpeaker = string.IsNullOrWhiteSpace(renderedAssistant) ? string.Empty : memoryName,
                    DailyText = renderedAssistant,
                    RecentText = renderedAssistant,
                    IsAfef = false,
                    IsLlmDialogue = true
                }
            }
        };
    }

    private string BuildInteractionMemoryRecoverySessionKey(
        InteractionMemoryCommit commit,
        int sceneSessionId,
        int dialogueSessionId)
    {
        if (sceneSessionId >= 0 || dialogueSessionId >= 0)
        {
            return BuildCurrentMemorySessionKey(sceneSessionId, dialogueSessionId);
        }
        string loose = BuildCurrentMemorySessionKey(-1, -1);
        string prefix = loose.EndsWith(":loose", StringComparison.Ordinal)
            ? loose.Substring(0, loose.Length - ":loose".Length)
            : loose;
        string sessionDigest;
        using (SHA256 sha = SHA256.Create())
        {
            sessionDigest = BitConverter.ToString(sha.ComputeHash(
                Encoding.UTF8.GetBytes(commit?.SessionId ?? string.Empty)))
                .Replace("-", string.Empty)
                .Substring(0, 16);
        }
        return prefix + ":" + (commit?.Channel.ToString() ?? "unknown").ToLowerInvariant()
            + ":" + sessionDigest;
    }

    private static string RenderInteractionMemoryFact(string factsText)
    {
        string rendered = (factsText ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(rendered))
        {
            return string.Empty;
        }
        if (!rendered.StartsWith("[AFEF玩家行为补充]", StringComparison.Ordinal)
            && !rendered.StartsWith("[AFEF NPC行为补充]", StringComparison.Ordinal))
        {
            rendered = "[AFEF玩家行为补充] " + rendered;
        }
        return rendered;
    }

    private static string RenderInteractionMemoryAssistant(string npcName, string assistantText)
    {
        string rendered = (assistantText ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(rendered) || rendered.StartsWith("[场景喊话]", StringComparison.Ordinal))
        {
            return rendered;
        }
        return (string.IsNullOrWhiteSpace(npcName) ? "NPC" : npcName.Trim()) + ": " + rendered;
    }

    private void ProcessInteractionMemoryRecoveryEntry(string recoveryId)
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

    private bool TryApplyInteractionMemoryRecoveryWork(InteractionMemoryRecoveryWorkItem work)
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
                Logger.Log("MemoryRecovery", "[ERROR] marker conflict recovery=" + work.RecoveryId + " part=" + work.Part + " target=" + work.Target);
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
            Logger.Log("MemoryRecovery", "[ERROR] permanent component failure recovery=" + work.RecoveryId
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
            Logger.Log("MemoryRecovery", "[WARN] component publish deferred recovery=" + work.RecoveryId
                + " part=" + work.Part + " target=" + work.Target + " error=" + ex.Message);
            return false;
        }
        finally
        {
            RefreshInteractionMemoryRecoveryWorkFlag();
        }
    }

    private void RegisterInteractionMemoryRecoveryRetryOrQuarantine(
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

    private bool PublishDailyInteractionMemoryComponent(InteractionMemoryRecoveryWorkItem work)
    {
        string memoryId = NormalizeMemoryHeroId(work.SubjectId);
        if (!IsMemoryEntityEligibleForCompressedMemory(memoryId) || string.IsNullOrWhiteSpace(work.DailyText))
        {
            throw new InteractionMemoryRecoveryPermanentException("memory_recovery_target_ineligible");
        }
        int currentDay = GetCurrentGameDayIndexSafe();
        if (work.OriginGameDay > currentDay)
        {
            throw new InteractionMemoryRecoveryPermanentException("memory_recovery_origin_day_invalid");
        }
        int candidateStorageDay = work.DailyStorageDay >= 0
            ? work.DailyStorageDay
            : work.OriginGameDay;
        bool selectedDayAlreadySealed = candidateStorageDay < currentDay
            && HasCompressedMemoryBlock(memoryId, candidateStorageDay);
        int storageDay = selectedDayAlreadySealed ? currentDay : candidateStorageDay;
        string storageDate = selectedDayAlreadySealed
            ? GetCurrentGameDateTextSafe()
            : string.IsNullOrWhiteSpace(work.DailyStorageDate) ? work.OriginGameDate : work.DailyStorageDate;
        if (!EnsureInteractionMemoryRecoveryLedger().RecordDailyStorage(work, storageDay, storageDate))
        {
            throw new InteractionMemoryRecoveryPermanentException("memory_recovery_daily_storage_invalid");
        }
        List<DailyMemoryDraft> current = LoadDailyMemoryDraftsById(memoryId);
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

        List<WeeklyMemoryMaterialTrigger> originalPending = _pendingWeeklyMemoryMaterialTriggers;
        List<WeeklyMemoryMaterialTrigger> pendingClone = CloneForMemoryRecovery(
            originalPending ?? new List<WeeklyMemoryMaterialTrigger>()) ?? new List<WeeklyMemoryMaterialTrigger>();
        _pendingWeeklyMemoryMaterialTriggers = pendingClone;
        try
        {
            AttachPendingWeeklyMemoryMaterialTriggers(draft, line);
            SaveDailyMemoryDraftsById(memoryId, clone);
        }
        catch
        {
            _pendingWeeklyMemoryMaterialTriggers = originalPending ?? new List<WeeklyMemoryMaterialTrigger>();
            throw;
        }
        if (line.IsLlmDialogue)
        {
            PlayerNotorietyBehavior.NoteConversationLineForExternal(memoryId);
        }
        return HasDailyInteractionMemoryMarker(work, out _);
    }

    private bool PublishRecentInteractionMemoryComponent(InteractionMemoryRecoveryWorkItem work)
    {
        string memoryId = NormalizeMemoryHeroId(work.SubjectId);
        if (!IsMemoryEntityEligibleForCompressedMemory(memoryId) || string.IsNullOrWhiteSpace(work.RecentText))
        {
            throw new InteractionMemoryRecoveryPermanentException("memory_recovery_target_ineligible");
        }
        List<DialogueDay> current = LoadDialogueHistoryById(memoryId);
        List<DialogueDay> clone = CloneForMemoryRecovery(current) ?? new List<DialogueDay>();
        if (string.Equals(work.Part, "assistant", StringComparison.Ordinal))
        {
            RemoveExpiredSingleUseNpcFactLines(clone);
        }
        DialogueDay day = clone.FirstOrDefault(item => item != null && item.GameDayIndex == work.OriginGameDay);
        if (day == null)
        {
            day = new DialogueDay
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
            ? TagSceneSessionHistoryLine(work.RecentText, work.SceneSessionId)
            : work.RecentText;
        day.Lines.Add(recentLine);
        day.MemoryCommitMarkers[markerKey] = work.PayloadHash;
        List<DialogueDay> trimmed = TrimDialogueHistoryForMemoryRecovery(clone);
        SaveDialogueHistoryById(memoryId, trimmed);
        return HasRecentInteractionMemoryMarker(work, out _);
    }

    private static T CloneForMemoryRecovery<T>(T value)
    {
        if (value == null)
        {
            return default;
        }
        string json = JsonConvert.SerializeObject(value, Formatting.None);
        return JsonConvert.DeserializeObject<T>(json);
    }

    private static List<DialogueDay> TrimDialogueHistoryForMemoryRecovery(List<DialogueDay> records)
    {
        var lines = new List<(int Day, string Date, string Line)>();
        foreach (DialogueDay day in records ?? new List<DialogueDay>())
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
        var trimmed = new List<DialogueDay>();
        foreach ((int dayIndex, string gameDate, string line) in lines)
        {
            DialogueDay day = trimmed.FirstOrDefault(item => item.GameDayIndex == dayIndex);
            if (day == null)
            {
                day = new DialogueDay
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

    private static void CopyMemoryCommitMarkers(IEnumerable<DialogueDay> source, List<DialogueDay> target)
    {
        if (target == null)
        {
            return;
        }
        int copied = 0;
        foreach (DialogueDay sourceDay in source ?? Enumerable.Empty<DialogueDay>())
        {
            Dictionary<string, string> markers = SanitizeMemoryCommitMarkers(sourceDay?.MemoryCommitMarkers);
            if (markers.Count == 0)
            {
                continue;
            }
            DialogueDay targetDay = target.FirstOrDefault(item => item != null && item.GameDayIndex == sourceDay.GameDayIndex);
            if (targetDay == null)
            {
                targetDay = new DialogueDay
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

    private bool HasMatchingInteractionMemoryMarker(
        InteractionMemoryRecoveryWorkItem work,
        out bool conflict)
        => work.Target == InteractionMemoryRecoveryTarget.Daily
            ? HasDailyInteractionMemoryMarker(work, out conflict)
            : HasRecentInteractionMemoryMarker(work, out conflict);

    private bool HasDailyInteractionMemoryMarker(
        InteractionMemoryRecoveryWorkItem work,
        out bool conflict)
    {
        conflict = false;
        string memoryId = NormalizeMemoryHeroId(work?.SubjectId);
        if (work == null || string.IsNullOrWhiteSpace(memoryId)
            || _dailyMemoryDrafts == null
            || !_dailyMemoryDrafts.TryGetValue(memoryId, out List<DailyMemoryDraft> drafts))
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

    private bool HasRecentInteractionMemoryMarker(
        InteractionMemoryRecoveryWorkItem work,
        out bool conflict)
    {
        conflict = false;
        string memoryId = NormalizeMemoryHeroId(work?.SubjectId);
        string markerKey = BuildMemoryCommitMarkerKey(work?.RecoveryId, work?.Part);
        if (work == null || string.IsNullOrWhiteSpace(memoryId)
            || _dialogueHistory == null
            || !_dialogueHistory.TryGetValue(memoryId, out List<DialogueDay> records))
        {
            return false;
        }
        bool matching = false;
        foreach (DialogueDay day in records ?? new List<DialogueDay>())
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

    private static Dictionary<string, string> SanitizeMemoryCommitMarkers(
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

    private static bool IsValidMemoryCommitMarker(string recoveryId, string part, string payloadHash)
        => IsMemoryRecoveryHexDigest((recoveryId ?? string.Empty).Trim())
            && ((part ?? string.Empty).Trim() == "user"
                || (part ?? string.Empty).Trim() == "fact"
                || (part ?? string.Empty).Trim() == "assistant")
            && IsMemoryRecoveryHexDigest((payloadHash ?? string.Empty).Trim());

    private static string BuildMemoryCommitMarkerKey(string recoveryId, string part)
        => (recoveryId ?? string.Empty).Trim() + ":" + (part ?? string.Empty).Trim();

    private static bool TryParseMemoryCommitMarkerKey(
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

    private static bool IsMemoryRecoveryHexDigest(string value)
        => value != null && value.Length == 64 && value.All(character =>
            (character >= '0' && character <= '9') || (character >= 'A' && character <= 'F'));

    private InteractionMemoryRecoveryLedger EnsureInteractionMemoryRecoveryLedger()
    {
        if (_interactionMemoryRecoveryLedger == null)
        {
            _interactionMemoryRecoveryLedger = new InteractionMemoryRecoveryLedger();
        }
        return _interactionMemoryRecoveryLedger;
    }

    private void RetargetInteractionMemoryRecoveryProjection(string sourceSubjectId, string targetSubjectId)
    {
        int changed = EnsureInteractionMemoryRecoveryLedger().RetargetProjectionSubject(
            NormalizeMemoryHeroId(sourceSubjectId),
            NormalizeMemoryHeroId(targetSubjectId));
        if (changed > 0)
        {
            Logger.Log("MemoryRecovery", "projection retargeted count=" + changed);
        }
    }

    private IEnumerable<string> GetInteractionMemoryRecoveryProjectionSubjects()
        => EnsureInteractionMemoryRecoveryLedger().GetRetainedEntries()
            .Select(item => item.SubjectId)
            .Where(subjectId => !string.IsNullOrWhiteSpace(subjectId))
            .ToList();

    private void QuarantineInteractionMemoryRecoveryProjection(string subjectId, string reason)
    {
        int quarantined = EnsureInteractionMemoryRecoveryLedger().QuarantineProjectionSubject(
            NormalizeMemoryHeroId(subjectId),
            reason);
        if (quarantined > 0)
        {
            PruneEvictedInteractionMemoryRecoveryMarkers();
            RefreshInteractionMemoryRecoveryWorkFlag();
            Logger.Log("MemoryRecovery", "projection quarantined count=" + quarantined
                + " reason=" + (reason ?? string.Empty));
        }
    }

    private void RefreshInteractionMemoryRecoveryWorkFlag()
    {
        InteractionMemoryRecoveryLedger ledger = EnsureInteractionMemoryRecoveryLedger();
        Volatile.Write(ref _hasInteractionMemoryRecoveryWork,
            ledger.HasPendingWork || ledger.HasUnresolvedWork ? 1 : 0);
    }

    private void ScheduleInteractionMemoryRecoveryRetry()
    {
        Interlocked.Exchange(
            ref _interactionMemoryRecoveryNextAttemptUtcTicks,
            DateTime.UtcNow.Ticks + InteractionMemoryRecoveryRetryDelayTicks);
        Volatile.Write(ref _hasInteractionMemoryRecoveryWork, 1);
    }

    private void ResetInteractionMemoryRecoveryTransientState(string reason)
    {
        Volatile.Write(ref _hasInteractionMemoryRecoveryWork, 0);
        Interlocked.Exchange(ref _interactionMemoryRecoveryNextAttemptUtcTicks, 0L);
        string normalizedReason = (reason ?? string.Empty).Trim();
        if (string.Equals(normalizedReason, "sync_load", StringComparison.Ordinal))
        {
            Volatile.Write(ref _interactionMemoryRecoveryLoadImportConfirmed, 0);
            Interlocked.Exchange(ref _interactionMemoryRecoveryLoadedGeneration, 0L);
        }
        else if (string.Equals(normalizedReason, "new_game_created", StringComparison.Ordinal))
        {
            EnsureInteractionMemoryRecoveryLedger().Import(new Dictionary<string, string>());
            Volatile.Write(ref _interactionMemoryRecoveryLoadImportConfirmed, 1);
            Interlocked.Exchange(ref _interactionMemoryRecoveryLoadedGeneration, SaveRuntimeGuard.CurrentGeneration);
        }
        else if (string.Equals(normalizedReason, "game_loaded", StringComparison.Ordinal))
        {
            // Bannerlord can raise this lifecycle after SyncData. Advance the
            // generation only when the tail Import already proved this save;
            // this event must never manufacture a successful load stamp.
            if (Volatile.Read(ref _interactionMemoryRecoveryLoadImportConfirmed) != 0)
            {
                Interlocked.Exchange(ref _interactionMemoryRecoveryLoadedGeneration, SaveRuntimeGuard.CurrentGeneration);
            }
        }
    }

    private void ActivateInteractionMemoryRecoveryAfterLoad()
    {
        InteractionMemoryRecoveryLedger ledger = EnsureInteractionMemoryRecoveryLedger();
        if (Interlocked.Read(ref _interactionMemoryRecoveryLoadedGeneration)
            != SaveRuntimeGuard.CurrentGeneration
            || Volatile.Read(ref _interactionMemoryRecoveryLoadImportConfirmed) == 0)
        {
            ledger.DisableForCurrentCampaign("memory_recovery_load_not_confirmed");
            ResetInteractionMemoryRecoveryTransientState("load_not_confirmed");
            Logger.Log("MemoryRecovery", "[ERROR] recovery disabled because SyncData load was not confirmed");
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
                Logger.Log("MemoryRecovery", "[WARN] conflicting record quarantined recovery=" + recoveryId);
            }
        }
        RefreshInteractionMemoryRecoveryWorkFlag();
        PruneEvictedInteractionMemoryRecoveryMarkers();
        if (Volatile.Read(ref _hasInteractionMemoryRecoveryWork) != 0)
        {
            ProcessOneInteractionMemoryRecoveryOnTick();
        }
    }

    private void ReconcilePersistedInteractionMemoryRecoveryMarkers()
    {
        InteractionMemoryRecoveryLedger ledger = EnsureInteractionMemoryRecoveryLedger();
        Dictionary<string, InteractionMemoryRecoveryRetention> retained = ledger.GetRetainedEntries()
            .ToDictionary(item => item.RecoveryId, item => item, StringComparer.Ordinal);
        var conflicts = new HashSet<string>(StringComparer.Ordinal);
        var observedDailyMarkerMasks = new Dictionary<string, int>(StringComparer.Ordinal);
        var observedRecentMarkerMasks = new Dictionary<string, int>(StringComparer.Ordinal);
        int observedMarkers = 0;

        foreach (KeyValuePair<string, List<DailyMemoryDraft>> ownerEntry in
            _dailyMemoryDrafts ?? new Dictionary<string, List<DailyMemoryDraft>>())
        {
            string ownerId = NormalizeMemoryHeroId(ownerEntry.Key);
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
                if (!string.Equals(ownerId, NormalizeMemoryHeroId(retention.SubjectId), StringComparison.OrdinalIgnoreCase)
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

        foreach (KeyValuePair<string, List<DialogueDay>> ownerEntry in
            _dialogueHistory ?? new Dictionary<string, List<DialogueDay>>())
        {
            string ownerId = NormalizeMemoryHeroId(ownerEntry.Key);
            foreach (DialogueDay day in ownerEntry.Value ?? new List<DialogueDay>())
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
                    if (!string.Equals(ownerId, NormalizeMemoryHeroId(retention.SubjectId), StringComparison.OrdinalIgnoreCase)
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
            Logger.Log("MemoryRecovery", "[ERROR] owner marker envelope exceeded validation limit");
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
                && HasCompressedMemoryBlock(retention.SubjectId, retention.DailyStorageDay);
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

    private static int GetDailyInteractionMemoryMarkerBit(string part)
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

    private static int GetRecentInteractionMemoryMarkerBit(string part)
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

    private void ProcessOneInteractionMemoryRecoveryOnTick()
    {
        if (Volatile.Read(ref _hasInteractionMemoryRecoveryWork) == 0
            || DateTime.UtcNow.Ticks < Interlocked.Read(ref _interactionMemoryRecoveryNextAttemptUtcTicks))
        {
            return;
        }
        if (!TWParallel.IsMainThread())
        {
            return;
        }
        using (PerfProbe.Scope("MyBehavior.OnCampaignTick.ProcessInteractionMemoryRecovery"))
        {
            InteractionMemoryRecoveryLedger ledger = EnsureInteractionMemoryRecoveryLedger();
            if (ledger.TryGetNextWork(out InteractionMemoryRecoveryWorkItem work))
            {
                TryApplyInteractionMemoryRecoveryWork(work);
            }
            RefreshInteractionMemoryRecoveryWorkFlag();
        }
    }

    private void PruneEvictedInteractionMemoryRecoveryMarkers()
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
            string subjectId = NormalizeMemoryHeroId(item.SubjectId);
            if (!string.IsNullOrWhiteSpace(subjectId))
            {
                ClearDailyInteractionMemoryMarkers(subjectId, recoveryId);
                ClearRecentInteractionMemoryMarkers(subjectId, recoveryId);
            }
        }
    }

    private bool ClearDailyInteractionMemoryMarkers(string subjectId, string recoveryId)
    {
        string normalized = NormalizeMemoryHeroId(subjectId);
        if (string.IsNullOrWhiteSpace(normalized) || _dailyMemoryDrafts == null
            || !_dailyMemoryDrafts.TryGetValue(normalized, out List<DailyMemoryDraft> drafts))
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

    private static void ClearDailyInteractionMemoryMarker(DailyMemoryLine line)
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

    private bool ClearRecentInteractionMemoryMarkers(string subjectId, string recoveryId)
    {
        string normalized = NormalizeMemoryHeroId(subjectId);
        if (string.IsNullOrWhiteSpace(normalized) || _dialogueHistory == null
            || !_dialogueHistory.TryGetValue(normalized, out List<DialogueDay> records))
        {
            return false;
        }
        bool found = false;
        string prefix = recoveryId + ":";
        foreach (DialogueDay day in records ?? new List<DialogueDay>())
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

    private void SyncTailPersistenceData(IDataStore dataStore)
    {
        SyncPatienceData(dataStore);
        SyncInteractionMemoryRecoveryData(dataStore);
    }

    private void SyncInteractionMemoryRecoveryData(IDataStore dataStore)
    {
        try
        {
            InteractionMemoryRecoveryLedger ledger = EnsureInteractionMemoryRecoveryLedger();
            Dictionary<string, string> storage;
            if (dataStore.IsSaving)
            {
                _interactionMemoryRecoveryStorage = ledger.Export();
                storage = CampaignSaveChunkHelper.FlattenStringDictionary(
                    _interactionMemoryRecoveryStorage,
                    InteractionMemoryRecoveryStorageKey,
                    "MemoryRecovery");
                dataStore.SyncData(InteractionMemoryRecoveryStorageKey, ref storage);
                return;
            }

            storage = new Dictionary<string, string>(StringComparer.Ordinal);
            dataStore.SyncData(InteractionMemoryRecoveryStorageKey, ref storage);
            _interactionMemoryRecoveryStorage = CampaignSaveChunkHelper.RestoreStringDictionary(
                storage,
                "MemoryRecovery") ?? new Dictionary<string, string>(StringComparer.Ordinal);
            ledger.Import(_interactionMemoryRecoveryStorage);
            Volatile.Write(ref _interactionMemoryRecoveryLoadImportConfirmed, 1);
            Interlocked.Exchange(ref _interactionMemoryRecoveryLoadedGeneration, SaveRuntimeGuard.CurrentGeneration);
            RefreshInteractionMemoryRecoveryWorkFlag();
        }
        catch (Exception ex)
        {
            EnsureInteractionMemoryRecoveryLedger().DisableForCurrentCampaign(
                "memory_recovery_sync_failed");
            Volatile.Write(ref _interactionMemoryRecoveryLoadImportConfirmed, 0);
            ResetInteractionMemoryRecoveryTransientState("sync_failure");
            Logger.Log("MemoryRecovery", "[ERROR] recovery SyncData isolated: " + ex.Message);
        }
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
