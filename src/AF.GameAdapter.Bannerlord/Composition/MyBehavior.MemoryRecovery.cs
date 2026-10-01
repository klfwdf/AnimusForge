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
    private Dictionary<string, string> _interactionMemoryRecoveryStorage =
        new Dictionary<string, string>(StringComparer.Ordinal);
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
            return owner.MemoryRecoveryState.CommitPrepared(seed, preparedRecoveryId, preparedPayloadHash,
                owner.CompleteInitialInteractionMemoryNotorietyOutcome);
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
            if (!TWParallel.IsMainThread())
            {
                return InteractionMemoryRecoveryLookupStatus.Unavailable;
            }
            MyBehavior owner = Campaign.Current?.GetCampaignBehavior<MyBehavior>();
            if (owner == null)
            {
                return InteractionMemoryRecoveryLookupStatus.Unavailable;
            }
            InteractionMemoryRecoveryLedger ledger = owner.EnsureInteractionMemoryRecoveryLedger();
            if (ledger.IsDisabled)
            {
                return InteractionMemoryRecoveryLookupStatus.Disabled;
            }
            if (Interlocked.Read(ref owner._interactionMemoryRecoveryLoadedGeneration)
                    != SaveRuntimeGuard.CurrentGeneration
                || Volatile.Read(ref owner._interactionMemoryRecoveryLoadImportConfirmed) == 0)
            {
                return InteractionMemoryRecoveryLookupStatus.Unavailable;
            }
            return ledger.GetLookupStatus(
                recoveryId,
                expectedSubjectId,
                expectedPayloadHash);
        }
        catch
        {
            return InteractionMemoryRecoveryLookupStatus.Unavailable;
        }
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
        if (Interlocked.Read(ref owner._interactionMemoryRecoveryLoadedGeneration)
            != SaveRuntimeGuard.CurrentGeneration
            || Volatile.Read(ref owner._interactionMemoryRecoveryLoadImportConfirmed) == 0)
        {
            failureStatus = MemoryCommitStatus.Failed;
            errorCode = "memory_recovery_not_activated";
            return false;
        }
        string normalizedMemoryId = NormalizeMemoryHeroId(commit.SubjectId);
        if (string.IsNullOrEmpty(normalizedMemoryId)
            || isNonHero != IsNonHeroMemoryId(normalizedMemoryId))
        {
            errorCode = "memory_identity_invalid";
            return false;
        }
        if (string.IsNullOrWhiteSpace(commit.UserText)
            && string.IsNullOrWhiteSpace(commit.AssistantText)
            && !(commit.ConfirmedFacts ?? Array.Empty<FactRecord>()).Any(fact =>
                fact != null && !string.IsNullOrWhiteSpace(fact.Text)))
        {
            errorCode = "memory_empty_commit";
            return false;
        }
        Hero hero = isNonHero
            ? null
            : (Hero.Find(commit.SubjectId.Trim()) ?? FindHeroById(normalizedMemoryId));
        if (!isNonHero && !IsHeroNpcEligibleForCompressedMemory(hero))
        {
            errorCode = "memory_target_ineligible";
            return false;
        }
        seed = owner.BuildInteractionMemoryRecoverySeed(
            commit,
            normalizedMemoryId,
            isNonHero,
            npcName,
            hero);
        if (!InteractionMemoryRecoveryLedger.TryBuildRecoveryIdentity(
            seed,
            out recoveryId,
            out payloadHash,
            out errorCode))
        {
            failureStatus = MemoryCommitStatus.Failed;
            return false;
        }
        return true;
    }

    private static string ResolveInteractionMemoryOriginGameDate(int originDay, int currentDay)
    {
        if (originDay == currentDay) return GetCurrentGameDateTextSafe();
        try
        {
            // CampaignTime.Days is an absolute campaign date in both supported APIs.
            // Do not relabel delayed delivery with the current date or invent a calendar.
            string date = CampaignTime.Days(Math.Max(0, originDay)).ToString();
            if (!string.IsNullOrWhiteSpace(date)) return date.Trim();
        }
        catch { }
        return "第 " + Math.Max(0, originDay).ToString(CultureInfo.InvariantCulture) + " 日";
    }

    private InteractionMemoryRecoverySeed BuildInteractionMemoryRecoverySeed(InteractionMemoryCommit commit,string normalizedMemoryId,bool isNonHero,string npcName,Hero hero) {
 string memoryName=string.IsNullOrWhiteSpace(npcName)?hero?.Name?.ToString()??"NPC":npcName.Trim();if(string.IsNullOrWhiteSpace(memoryName))memoryName="NPC";
 Hero memoryHero=hero??FindHeroById(normalizedMemoryId);string userText=(commit.UserText??string.Empty).Trim();string assistantText=(commit.AssistantText??string.Empty).Trim();string factsText=MemoryRecoverySeedRules.Facts(commit);
 string renderedUser=string.IsNullOrWhiteSpace(userText)?string.Empty:BuildPlayerAddressedInputForName(memoryName,userText,memoryHero,commit.TargetName);
 string renderedFact=MemoryRecoverySeedRules.RenderInteractionMemoryFact(factsText);string renderedAssistant=MemoryRecoverySeedRules.RenderInteractionMemoryAssistant(memoryName,assistantText);
 int currentDay=GetCurrentGameDayIndexSafe();int originDay=MemoryRecoverySeedRules.OriginDay(commit,currentDay);int originHour=MemoryRecoverySeedRules.OriginHour(commit,MemoryRecoverySeedRules.HasDetachedProvenance(commit)?0:GetCurrentHourOfDaySafeForPrompt());
 string originDate=ResolveInteractionMemoryOriginGameDate(originDay,currentDay);string originScene=!string.IsNullOrWhiteSpace(commit.LocationId)?commit.LocationId.Trim():ResolveCurrentMemorySceneLabel();
 int sceneSessionId=commit.Channel==InteractionChannel.SceneShout?Math.Max(-1,commit.SceneSessionId):-1;int dialogueSessionId=commit.Channel==InteractionChannel.NativeConversation?GetOrStartActiveNativeConversationMemorySessionId():-1;
 string memorySessionKey=BuildInteractionMemoryRecoverySessionKey(commit,sceneSessionId,dialogueSessionId);
 return MemoryRecoverySeedRules.Build(commit,normalizedMemoryId,isNonHero,new MemoryRecoverySeedCapture{Name=memoryName,User=renderedUser,Fact=renderedFact,Assistant=renderedAssistant,Day=originDay,Date=originDate,Hour=originHour,Scene=originScene,SceneSessionId=sceneSessionId,DialogueSessionId=dialogueSessionId,SessionKey=memorySessionKey,PlayerName=string.IsNullOrWhiteSpace(renderedUser)?string.Empty:BuildPlayerPublicDisplayNameForPrompt(memoryHero)});
}

    private string BuildInteractionMemoryRecoverySessionKey(InteractionMemoryCommit commit,int sceneSessionId,int dialogueSessionId) => MemoryRecoverySeedRules.BuildInteractionMemoryRecoverySessionKey(commit,sceneSessionId,dialogueSessionId,BuildCurrentMemorySessionKey(sceneSessionId,dialogueSessionId));

    private static string RenderInteractionMemoryFact(string factsText) => MemoryRecoverySeedRules.RenderInteractionMemoryFact(factsText);

    private static string RenderInteractionMemoryAssistant(string npcName, string assistantText) => MemoryRecoverySeedRules.RenderInteractionMemoryAssistant(npcName, assistantText);

    private void ProcessInteractionMemoryRecoveryEntry(string recoveryId) => MemoryRecoveryState.ProcessInteractionMemoryRecoveryEntry(recoveryId);

    private bool TryApplyInteractionMemoryRecoveryWork(InteractionMemoryRecoveryWorkItem work) => MemoryRecoveryState.TryApplyInteractionMemoryRecoveryWork(work);

    private void RegisterInteractionMemoryRecoveryRetryOrQuarantine(
        InteractionMemoryRecoveryLedger ledger,
        InteractionMemoryRecoveryWorkItem work,
        string errorCode) => MemoryRecoveryState.RegisterInteractionMemoryRecoveryRetryOrQuarantine(ledger, work, errorCode);

    private bool PublishDailyInteractionMemoryComponent(InteractionMemoryRecoveryWorkItem work) => MemoryRecoveryState.PublishDailyInteractionMemoryComponent(work);

    private void CompleteInitialInteractionMemoryNotorietyOutcome(InteractionMemoryRecoverySeed seed, string recoveryId, string payloadHash)
    {
        var receipt = InteractionMemoryAuxiliaryCompletionCoordinator.CompleteInitial(seed, recoveryId, payloadHash,
            HasPublishedDailyInteractionMemoryComponent, NotifyInitialMemoryNotorietyComponent);
        if (receipt.HasAttempt)
            Logger.Log("MemoryRecovery", "auxiliary_outcome recovery=" + recoveryId
                + " notoriety_line=confirmed count=" + receipt.Accepted + " duplicate=" + receipt.Duplicate
                + " unavailable=" + receipt.Unavailable + " weekly=not_replayed_or_consumed");
    }

    private static MemoryAuxiliaryReceiptOutcome NotifyInitialMemoryNotorietyComponent(InteractionMemoryRecoverySeed seed, string recoveryId, string payloadHash, string part)
    {
        var status = PlayerNotorietyBehavior.NoteConversationLineRecoverableForExternal(seed.SubjectId, seed.MemorySessionKey,
            seed.RuntimeGeneration, seed.SaveGeneration, seed.OriginGameDay, seed.OriginGameHour, recoveryId, payloadHash, part);
        return status == NotorietyConversationOutcomeOperationStatus.Accepted ? MemoryAuxiliaryReceiptOutcome.Accepted
            : status == NotorietyConversationOutcomeOperationStatus.Duplicate ? MemoryAuxiliaryReceiptOutcome.Duplicate
            : MemoryAuxiliaryReceiptOutcome.Unavailable;
    }

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
