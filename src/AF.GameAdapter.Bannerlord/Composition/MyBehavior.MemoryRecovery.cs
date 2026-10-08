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

    private MemoryRecoveryStateOwner _memoryRecoveryState;
    private MemoryRecoveryPort _memoryRecoveryPort;
    private MemoryRecoveryStateOwner MemoryRecoveryState { get { _memoryRecoveryState ??= new MemoryRecoveryStateOwner(_memoryBusinessState); _memoryRecoveryState.Bind(_memoryRecoveryPort ??= new MemoryRecoveryPort { IsEntityEligible = IsMemoryEntityEligibleForCompressedMemory, CurrentDay = GetCurrentGameDayIndexSafe, CurrentDate = GetCurrentGameDateTextSafe, History = () => _dialogueHistory, LoadHistory = LoadDialogueHistoryById, SaveHistory = SaveDialogueHistoryById, RemoveExpiredFacts = RemoveExpiredSingleUseNpcFactLines, Log = Logger.Log, TickScope = () => PerfProbe.Scope("MyBehavior.OnCampaignTick.ProcessInteractionMemoryRecovery") }); return _memoryRecoveryState; } }
    private ref InteractionMemoryRecoveryLedger _interactionMemoryRecoveryLedger => ref MemoryRecoveryState.Ledger;
    private readonly CampaignMemoryRecoveryPersistenceAdapter _memoryRecoveryPersistence;
    private CampaignMemoryRecoveryPersistenceAdapter MemoryRecoveryPersistence => _memoryRecoveryPersistence;
    private Dictionary<string,string> _interactionMemoryRecoveryStorage { get => _memoryRecoveryPersistence.Storage; set => _memoryRecoveryPersistence.Storage = value; }
    private ref int _hasInteractionMemoryRecoveryWork => ref MemoryRecoveryState.HasWork;
    private ref long _interactionMemoryRecoveryNextAttemptUtcTicks => ref MemoryRecoveryState.NextAttemptTicks;
    private ref long _interactionMemoryRecoveryLoadedGeneration => ref MemoryRecoveryState.LoadedGeneration;
    private ref int _interactionMemoryRecoveryLoadImportConfirmed => ref MemoryRecoveryState.LoadConfirmed;

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
            if (!TryPrepareExternalDialogueHistoryRecovery(
                commit,
                isNonHero,
                npcName,
                out MyBehavior owner,
                out InteractionMemoryRecoverySeed seed,
                out string preparedRecoveryId,
                out string preparedPayloadHash,
                out string preparationError,
                out MemoryCommitStatus preparationFailureStatus))
            {
                return new MemoryCommitResult(
                    preparationFailureStatus,
                    preparationError);
            }
            return owner._memoryHistoryCommit.CommitPreparedDialogueHistoryRecovery(seed, preparedRecoveryId, preparedPayloadHash);
        }
        catch (Exception ex)
        {
            Logger.Log("MemoryRecovery", "[ERROR] recoverable commit failed: " + ex.Message);
            return new MemoryCommitResult(MemoryCommitStatus.Failed, "memory_recovery_commit_failed");
        }
    }

    internal static bool TryPrepareExternalDialogueHistoryRecoveryIdentity(
        InteractionMemoryCommit commit,
        bool isNonHero,
        string npcName,
        out string recoveryId,
        out string payloadHash,
        out string errorCode)
    {
        try
        {
            return TryPrepareExternalDialogueHistoryRecovery(
                commit,
                isNonHero,
                npcName,
                out _,
                out _,
                out recoveryId,
                out payloadHash,
                out errorCode,
                out _);
        }
        catch
        {
            recoveryId = string.Empty;
            payloadHash = string.Empty;
            errorCode = "memory_recovery_identity_failed";
            return false;
        }
    }

    internal static InteractionMemoryRecoveryLookupStatus GetExternalDialogueHistoryRecoveryStatus(
        string recoveryId,
        string expectedSubjectId,
        string expectedPayloadHash)
    {
        try
        {
            if (!TWParallel.IsMainThread()) return InteractionMemoryRecoveryLookupStatus.Unavailable;
            MyBehavior owner = Campaign.Current?.GetCampaignBehavior<MyBehavior>();
            return owner == null ? InteractionMemoryRecoveryLookupStatus.Unavailable
                : owner._memoryHistoryCommit.GetExternalDialogueHistoryRecoveryStatus(recoveryId, expectedSubjectId, expectedPayloadHash);
        }
        catch { return InteractionMemoryRecoveryLookupStatus.Unavailable; }
    }

    private static bool TryPrepareExternalDialogueHistoryRecovery(
        InteractionMemoryCommit commit,
        bool isNonHero,
        string npcName,
        out MyBehavior owner,
        out InteractionMemoryRecoverySeed seed,
        out string recoveryId,
        out string payloadHash,
        out string errorCode,
        out MemoryCommitStatus failureStatus)
    {
        owner = null;
        seed = null;
        recoveryId = string.Empty;
        payloadHash = string.Empty;
        errorCode = string.Empty;
        failureStatus = MemoryCommitStatus.Rejected;
        if (!TWParallel.IsMainThread())
        {
            errorCode = "memory_not_main_thread";
            return false;
        }
        if (commit == null)
        {
            errorCode = "missing_memory_commit";
            return false;
        }
        owner = Campaign.Current?.GetCampaignBehavior<MyBehavior>();
        if (owner == null)
        {
            failureStatus = MemoryCommitStatus.Failed;
            errorCode = "memory_owner_missing";
            return false;
        }
        return owner._memoryHistoryCommit.TryPrepareExternalDialogueHistoryRecovery(commit,isNonHero,npcName,out seed,out recoveryId,out payloadHash,out errorCode,out failureStatus);
    }

    private static string ResolveInteractionMemoryOriginGameDate(int originDay, int currentDay) => MemoryHistoryCommitBannerlordAdapter.ResolveInteractionMemoryOriginGameDate(originDay, currentDay);

    private InteractionMemoryRecoverySeed BuildInteractionMemoryRecoverySeed(InteractionMemoryCommit commit,string normalizedMemoryId,bool isNonHero,string npcName,Hero hero) => _memoryHistoryCommit.BuildInteractionMemoryRecoverySeed(commit, normalizedMemoryId, isNonHero, npcName, hero);

    private string BuildInteractionMemoryRecoverySessionKey(InteractionMemoryCommit commit,int sceneSessionId,int dialogueSessionId) => _memoryHistoryCommit.BuildInteractionMemoryRecoverySessionKey(commit,sceneSessionId,dialogueSessionId);

    private static string RenderInteractionMemoryFact(string factsText) => MemoryRecoverySeedRules.RenderInteractionMemoryFact(factsText);

    private static string RenderInteractionMemoryAssistant(string npcName, string assistantText) => MemoryRecoverySeedRules.RenderInteractionMemoryAssistant(npcName, assistantText);

    private void ProcessInteractionMemoryRecoveryEntry(string recoveryId) => MemoryRecoveryState.ProcessInteractionMemoryRecoveryEntry(recoveryId);

    private bool TryApplyInteractionMemoryRecoveryWork(InteractionMemoryRecoveryWorkItem work) => MemoryRecoveryState.TryApplyInteractionMemoryRecoveryWork(work);

    private void RegisterInteractionMemoryRecoveryRetryOrQuarantine(
        InteractionMemoryRecoveryLedger ledger,
        InteractionMemoryRecoveryWorkItem work,
        string errorCode) => MemoryRecoveryState.RegisterInteractionMemoryRecoveryRetryOrQuarantine(ledger, work, errorCode);

    private bool PublishDailyInteractionMemoryComponent(InteractionMemoryRecoveryWorkItem work) => MemoryRecoveryState.PublishDailyInteractionMemoryComponent(work);

    private void CompleteInitialInteractionMemoryNotorietyOutcome(InteractionMemoryRecoverySeed seed, string recoveryId, string payloadHash) => _memoryHistoryCommit.CompleteInitialInteractionMemoryNotorietyOutcome(seed, recoveryId, payloadHash);

    private static MemoryAuxiliaryReceiptOutcome NotifyInitialMemoryNotorietyComponent(InteractionMemoryRecoverySeed seed, string recoveryId, string payloadHash, string part) => MemoryHistoryCommitBannerlordAdapter.NotifyInitialMemoryNotorietyComponent(seed, recoveryId, payloadHash, part);

    private static bool IsInitialInteractionMemoryNotorietyComponentEligible(
        InteractionMemoryRecoveryComponentSeed component)
    { return InteractionMemoryAuxiliaryCompletionCoordinator.IsEligible(component); }

    private static bool ShouldCompleteInitialInteractionMemoryNotoriety(InteractionMemoryRecoveryBeginStatus beginStatus, bool recoveryCompleted, string recoveryId, string preparedRecoveryId)
        => MemoryRecoveryStateOwner.ShouldCompleteInitialInteractionMemoryNotoriety(beginStatus, recoveryCompleted, recoveryId, preparedRecoveryId);

    private bool HasPublishedDailyInteractionMemoryComponent(
        string subjectId,
        string recoveryId,
        string payloadHash,
        string part)
    { return MemoryRecoveryState.HasPublishedDailyInteractionMemoryComponent(subjectId, recoveryId, payloadHash, part); }

    private bool PublishRecentInteractionMemoryComponent(InteractionMemoryRecoveryWorkItem work) => MemoryRecoveryState.PublishRecentInteractionMemoryComponent(work);

    private static T CloneForMemoryRecovery<T>(T value) => MemoryRecoveryStateOwner.CloneForMemoryRecovery<T>(value);

    private static List<DialogueDay> TrimDialogueHistoryForMemoryRecovery(List<DialogueDay> records) => MemoryRecoveryStateOwner.TrimDialogueHistoryForMemoryRecovery(records);

    private static void CopyMemoryCommitMarkers(IEnumerable<DialogueDay> source, List<DialogueDay> target) => MemoryRecoveryStateOwner.CopyMemoryCommitMarkers(source, target);

    private bool HasMatchingInteractionMemoryMarker(
        InteractionMemoryRecoveryWorkItem work,
        out bool conflict) => MemoryRecoveryState.HasMatchingInteractionMemoryMarker(work, out conflict);

    private bool HasDailyInteractionMemoryMarker(
        InteractionMemoryRecoveryWorkItem work,
        out bool conflict) => MemoryRecoveryState.HasDailyInteractionMemoryMarker(work, out conflict);

    private bool HasRecentInteractionMemoryMarker(
        InteractionMemoryRecoveryWorkItem work,
        out bool conflict) => MemoryRecoveryState.HasRecentInteractionMemoryMarker(work, out conflict);

    private static Dictionary<string, string> SanitizeMemoryCommitMarkers(
        IDictionary<string, string> markers) => MemoryRecoveryStateOwner.SanitizeMemoryCommitMarkers(markers);

    internal static bool IsValidMemoryCommitMarker(string recoveryId, string part, string payloadHash) => MemoryRecoveryStateOwner.IsValidMemoryCommitMarker(recoveryId, part, payloadHash);

    private static string BuildMemoryCommitMarkerKey(string recoveryId, string part) => MemoryRecoveryStateOwner.BuildMemoryCommitMarkerKey(recoveryId, part);

    private static bool TryParseMemoryCommitMarkerKey(
        string markerKey,
        out string recoveryId,
        out string part) => MemoryRecoveryStateOwner.TryParseMemoryCommitMarkerKey(markerKey, out recoveryId, out part);

    internal static bool IsMemoryRecoveryHexDigest(string value) => MemoryRecoveryStateOwner.IsMemoryRecoveryHexDigest(value);

    private InteractionMemoryRecoveryLedger EnsureInteractionMemoryRecoveryLedger() => MemoryRecoveryState.EnsureInteractionMemoryRecoveryLedger();

    private void RetargetInteractionMemoryRecoveryProjection(string sourceSubjectId, string targetSubjectId) => MemoryRecoveryState.RetargetInteractionMemoryRecoveryProjection(sourceSubjectId, targetSubjectId);

    private IEnumerable<string> GetInteractionMemoryRecoveryProjectionSubjects() => MemoryRecoveryState.GetInteractionMemoryRecoveryProjectionSubjects();

    private void QuarantineInteractionMemoryRecoveryProjection(string subjectId, string reason) => MemoryRecoveryState.QuarantineInteractionMemoryRecoveryProjection(subjectId, reason);

    private void RefreshInteractionMemoryRecoveryWorkFlag() => MemoryRecoveryState.RefreshInteractionMemoryRecoveryWorkFlag();

    private void ScheduleInteractionMemoryRecoveryRetry() => MemoryRecoveryState.ScheduleInteractionMemoryRecoveryRetry();

    private void ResetInteractionMemoryRecoveryTransientState(string reason) => MemoryRecoveryState.ResetInteractionMemoryRecoveryTransientState(reason);

    private void ActivateInteractionMemoryRecoveryAfterLoad() => MemoryRecoveryState.ActivateInteractionMemoryRecoveryAfterLoad();

    private void ReconcilePersistedInteractionMemoryRecoveryMarkers() => MemoryRecoveryState.ReconcilePersistedInteractionMemoryRecoveryMarkers();

    private static int GetDailyInteractionMemoryMarkerBit(string part) => MemoryRecoveryStateOwner.GetDailyInteractionMemoryMarkerBit(part);

    private static int GetRecentInteractionMemoryMarkerBit(string part) => MemoryRecoveryStateOwner.GetRecentInteractionMemoryMarkerBit(part);

    private void ProcessOneInteractionMemoryRecoveryOnTick() => MemoryRecoveryState.ProcessOneInteractionMemoryRecoveryOnTick();

    private void PruneEvictedInteractionMemoryRecoveryMarkers() => MemoryRecoveryState.PruneEvictedInteractionMemoryRecoveryMarkers();

    private bool ClearDailyInteractionMemoryMarkers(string subjectId, string recoveryId) => MemoryRecoveryState.ClearDailyInteractionMemoryMarkers(subjectId, recoveryId);

    private static void ClearDailyInteractionMemoryMarker(DailyMemoryLine line) => MemoryRecoveryStateOwner.ClearDailyInteractionMemoryMarker(line);

    private bool ClearRecentInteractionMemoryMarkers(string subjectId, string recoveryId) => MemoryRecoveryState.ClearRecentInteractionMemoryMarkers(subjectId, recoveryId);

    private void SyncTailPersistenceData(IDataStore dataStore)
    {
        SyncPatienceData(dataStore);
        SyncInteractionMemoryRecoveryData(dataStore);
        SyncWeeklyActionOutcomeData(dataStore);
        SyncWorldBulletinData(dataStore);
    }

    private void ClearInteractionMemoryRecoveryForDeveloperClear()
    {
        MemoryRecoveryState.ClearVisibleMemoryForDeveloperClear();
        _interactionMemoryRecoveryStorage = EnsureInteractionMemoryRecoveryLedger().Export();
    }

    private void SyncInteractionMemoryRecoveryData(IDataStore dataStore) => MemoryRecoveryPersistence.Sync(dataStore);

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
