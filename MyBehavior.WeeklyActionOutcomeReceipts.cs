using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Runtime;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem;

namespace AnimusForge;

public partial class MyBehavior
{
    private const string WeeklyActionOutcomeReceiptsStorageKey =
        "_af_weeklyActionOutcomeReceipts_v1";
    private WeeklyActionOutcomePublicationOwner _weeklyActionOutcomePublication = new WeeklyActionOutcomePublicationOwner();
    private Dictionary<string, string> _weeklyActionOutcomeStorage =
        new Dictionary<string, string>(StringComparer.Ordinal);

    private WeeklyActionOutcomePublicationOwner EnsureWeeklyActionOutcomePublication()
    {
        return _weeklyActionOutcomePublication ??= new WeeklyActionOutcomePublicationOwner();
    }

    internal static WeeklyMemoryMaterialOutcomeOperationStatus PrepareWeeklyActionOutcomeForExternal(
        WeeklyMemoryMaterialOutcomeCandidate candidate,
        bool isNonHero,
        string npcName)
    {
        try
        {
            if (!FeatureBridgeRuntime.IsEnabled(FeatureBridgeIds.MemorySocialReports))
            {
                return WeeklyMemoryMaterialOutcomeOperationStatus.NotReady;
            }
            if (!TWParallel.IsMainThread())
            {
                return WeeklyMemoryMaterialOutcomeOperationStatus.Rejected;
            }
            MyBehavior owner = Campaign.Current?.GetCampaignBehavior<MyBehavior>();
            if (owner == null || !owner.IsWeeklyActionOutcomeOwnerActive())
            {
                return WeeklyMemoryMaterialOutcomeOperationStatus.Rejected;
            }
            WeeklyMemoryMaterialOutcomeLedger ledger = owner.EnsureWeeklyActionOutcomeLedger();
            WeeklyMemoryMaterialOutcomeOperationStatus existing =
                ledger.ProbeExistingCandidate(candidate, out string errorCode);
            if (existing != WeeklyMemoryMaterialOutcomeOperationStatus.NotFound)
            {
                owner.RefreshWeeklyActionOutcomeWorkFlag();
                if (existing != WeeklyMemoryMaterialOutcomeOperationStatus.Duplicate)
                {
                    Logger.Log("WeeklyActionOutcome",
                        "prepare identity probe failed state=" + existing + " error=" + errorCode);
                }
                return existing;
            }
            if (!owner.TryBuildWeeklyActionOutcomePayload(
                    candidate,
                    isNonHero,
                    npcName,
                    out WeeklyMemoryMaterialFrozenPayload payload,
                    out errorCode))
            {
                if (!string.Equals(errorCode, "weekly_material_not_eligible", StringComparison.Ordinal))
                {
                    Logger.Log("WeeklyActionOutcome", "prepare rejected error=" + errorCode);
                }
                return WeeklyMemoryMaterialOutcomeOperationStatus.Rejected;
            }

            WeeklyMemoryMaterialOutcomeOperationStatus status = ledger.Prepare(
                candidate, payload, out errorCode);
            owner.RefreshWeeklyActionOutcomeWorkFlag();
            if (status != WeeklyMemoryMaterialOutcomeOperationStatus.Accepted
                && status != WeeklyMemoryMaterialOutcomeOperationStatus.Duplicate)
            {
                Logger.Log("WeeklyActionOutcome", "prepare failed state=" + status + " error=" + errorCode);
            }
            return status;
        }
        catch (Exception ex)
        {
            Logger.Log("WeeklyActionOutcome", "[WARN] prepare isolated error=" + ex.Message);
            return WeeklyMemoryMaterialOutcomeOperationStatus.Rejected;
        }
    }

    internal static WeeklyMemoryMaterialOutcomeOperationStatus CompleteWeeklyActionOutcomeForExternal(
        string receiptId,
        string candidateHash,
        WeeklyMemoryMaterialOutcomeState state,
        string errorCode)
    {
        try
        {
            if (!FeatureBridgeRuntime.IsEnabled(FeatureBridgeIds.MemorySocialReports))
            {
                return WeeklyMemoryMaterialOutcomeOperationStatus.NotReady;
            }
            if (!TWParallel.IsMainThread())
            {
                return WeeklyMemoryMaterialOutcomeOperationStatus.Rejected;
            }
            MyBehavior owner = Campaign.Current?.GetCampaignBehavior<MyBehavior>();
            if (owner == null || !owner.IsWeeklyActionOutcomeOwnerActive())
            {
                return WeeklyMemoryMaterialOutcomeOperationStatus.Rejected;
            }
            WeeklyMemoryMaterialOutcomeOperationStatus status = owner.EnsureWeeklyActionOutcomeLedger()
                .Complete(receiptId, candidateHash, state, errorCode, out string completionError);
            owner.RefreshWeeklyActionOutcomeWorkFlag();
            if (status != WeeklyMemoryMaterialOutcomeOperationStatus.Accepted
                && status != WeeklyMemoryMaterialOutcomeOperationStatus.Duplicate)
            {
                Logger.Log("WeeklyActionOutcome", "complete failed state=" + status + " error=" + completionError);
            }
            return status;
        }
        catch (Exception ex)
        {
            Logger.Log("WeeklyActionOutcome", "[WARN] complete isolated error=" + ex.Message);
            return WeeklyMemoryMaterialOutcomeOperationStatus.Rejected;
        }
    }

    internal static WeeklyMemoryMaterialOutcomeOperationStatus PublishWeeklyActionOutcomeForExternal(
        string receiptId,
        string candidateHash)
    {
        try
        {
            if (!FeatureBridgeRuntime.IsEnabled(FeatureBridgeIds.MemorySocialReports))
            {
                return WeeklyMemoryMaterialOutcomeOperationStatus.NotReady;
            }
            if (!TWParallel.IsMainThread())
            {
                return WeeklyMemoryMaterialOutcomeOperationStatus.Rejected;
            }
            MyBehavior owner = Campaign.Current?.GetCampaignBehavior<MyBehavior>();
            return owner == null || !owner.IsWeeklyActionOutcomeOwnerActive()
                ? WeeklyMemoryMaterialOutcomeOperationStatus.Rejected
                : owner.TryPublishWeeklyActionOutcome(receiptId, candidateHash);
        }
        catch (Exception ex)
        {
            Logger.Log("WeeklyActionOutcome", "[WARN] publish isolated error=" + ex.Message);
            return WeeklyMemoryMaterialOutcomeOperationStatus.NotReady;
        }
    }

    private bool TryBuildWeeklyActionOutcomePayload(
        WeeklyMemoryMaterialOutcomeCandidate candidate,
        bool isNonHero,
        string npcName,
        out WeeklyMemoryMaterialFrozenPayload payload,
        out string errorCode)
    {
        payload = null;
        errorCode = string.Empty;
        if (candidate == null
            || !candidate.TryValidate(out errorCode)
            || !SaveRuntimeGuard.IsCurrentGeneration(candidate.RuntimeGeneration))
        {
            errorCode = string.IsNullOrWhiteSpace(errorCode)
                ? "weekly_material_candidate_stale"
                : errorCode;
            return false;
        }

        string memoryId = NormalizeMemoryHeroId(candidate.SubjectId);
        if (string.IsNullOrWhiteSpace(memoryId)
            || isNonHero != IsNonHeroMemoryId(memoryId)
            || !IsMemoryEntityEligibleForCompressedMemory(memoryId))
        {
            errorCode = "weekly_material_subject_invalid";
            return false;
        }
        Hero memoryHero = isNonHero ? null : (Hero.Find(candidate.SubjectId) ?? FindHeroById(memoryId));
        if (!isNonHero && !IsHeroNpcEligibleForCompressedMemory(memoryHero))
        {
            errorCode = "weekly_material_subject_unavailable";
            return false;
        }
        if (!ResolvePlayerFootholdKingdomForWeeklyMemoryMaterial(
                out string footholdKingdomId,
                out string footholdSettlementId))
        {
            errorCode = "weekly_material_foothold_missing";
            return false;
        }

        var atoms = new List<WeeklyMemoryMaterialAtom>();
        long totalValue = 0L;
        for (int index = 0; index < candidate.Intents.Count; index++)
        {
            WeeklyMemoryMaterialIntent intent = candidate.Intents[index];
            if (!TryFreezeWeeklyActionOutcomeValue(intent, memoryHero, out long valueDenars))
            {
                continue;
            }
            try
            {
                totalValue = checked(totalValue + valueDenars);
            }
            catch (OverflowException)
            {
                errorCode = "weekly_material_value_overflow";
                return false;
            }
            atoms.Add(new WeeklyMemoryMaterialAtom(
                index,
                intent.Kind,
                valueDenars,
                intent.QuantityToken));
        }
        if (atoms.Count == 0 || totalValue <= WeeklyMemoryMaterialValueThresholdDenars)
        {
            errorCode = "weekly_material_not_eligible";
            return false;
        }

        string resolvedName = string.IsNullOrWhiteSpace(npcName)
            ? memoryHero?.Name?.ToString() ?? "NPC"
            : npcName.Trim();
        string originDate = candidate.OriginGameDay == GetCurrentGameDayIndexSafe()
            ? GetCurrentGameDateTextSafe()
            : "day:" + candidate.OriginGameDay.ToString(CultureInfo.InvariantCulture);
        string reason = string.Join("；", atoms
            .Select(atom => BuildWeeklyMemoryMaterialTagLabel(atom.Label)
                + " owner-confirmed " + atom.ValueDenars.ToString(CultureInfo.InvariantCulture) + " 第纳尔")
            .Distinct(StringComparer.OrdinalIgnoreCase))
            + "；本轮已确认估值合计严格大于 "
            + WeeklyMemoryMaterialValueThresholdDenars.ToString(CultureInfo.InvariantCulture)
            + " 第纳尔";
        return WeeklyMemoryMaterialFrozenPayload.TryCreate(
            memoryId,
            resolvedName,
            originDate,
            footholdKingdomId,
            footholdSettlementId,
            atoms,
            totalValue,
            reason,
            out payload,
            out errorCode);
    }

    private static bool TryFreezeWeeklyActionOutcomeValue(
        WeeklyMemoryMaterialIntent intent,
        Hero memoryHero,
        out long valueDenars)
    {
        valueDenars = 0L;
        if (intent == null || !intent.TryValidate(out _))
        {
            return false;
        }
        switch (intent.Kind)
        {
            case WeeklyMemoryMaterialKind.GiveGold:
                return TryParseWeeklyActionPositiveValue(intent.AmountToken, out valueDenars);
            case WeeklyMemoryMaterialKind.GiveAsset:
                if (RewardSystemBehavior.IsGoldAssetTokenForExternal(intent.AssetToken))
                {
                    return TryParseWeeklyActionPositiveValue(intent.QuantityToken, out valueDenars);
                }
                if (!int.TryParse(intent.QuantityToken, NumberStyles.None, CultureInfo.InvariantCulture, out int amount)
                    || amount <= 0)
                {
                    return false;
                }
                try
                {
                    valueDenars = Math.Max(0L, RewardSystemBehavior.Instance?
                        .EstimateItemValueForExternal(memoryHero ?? Hero.MainHero, intent.AssetToken, amount) ?? 0L);
                    return valueDenars > 0L;
                }
                catch
                {
                    valueDenars = 0L;
                    return false;
                }
            case WeeklyMemoryMaterialKind.DebtCreate:
                return string.Equals(intent.DirectionToken, "P", StringComparison.OrdinalIgnoreCase)
                    && TryParseWeeklyActionPositiveValue(intent.AmountToken, out valueDenars);
            case WeeklyMemoryMaterialKind.DebtResolve:
                return TryEstimateDebtValueByIdForWeeklyMemoryMaterial(intent.DebtId, out valueDenars)
                    && valueDenars > 0L;
            default:
                return false;
        }
    }

    private static bool TryParseWeeklyActionPositiveValue(string value, out long result)
        => long.TryParse((value ?? string.Empty).Trim(), NumberStyles.None,
            CultureInfo.InvariantCulture, out result) && result > 0L;

    private WeeklyMemoryMaterialOutcomeOperationStatus TryPublishWeeklyActionOutcome(
        string receiptId,
        string candidateHash)
    {
        WeeklyMemoryMaterialOutcomeLedger ledger = EnsureWeeklyActionOutcomeLedger();
        WeeklyMemoryMaterialOutcomeOperationStatus status = ledger.GetPublishWork(
            receiptId,
            candidateHash,
            out WeeklyMemoryMaterialOutcomeReceipt receipt,
            out string errorCode);
        if (status != WeeklyMemoryMaterialOutcomeOperationStatus.Accepted)
        {
            RefreshWeeklyActionOutcomeWorkFlag();
            return status;
        }

        WeeklyMemoryMaterialFrozenPayload frozen = receipt.Payload;
        string memoryId = NormalizeMemoryHeroId(frozen.MemoryId);
        int currentDay = GetCurrentGameDayIndexSafe();
        if (receipt.OriginGameDay > currentDay || !IsMemoryEntityEligibleForCompressedMemory(memoryId))
        {
            ledger.Complete(receiptId, candidateHash, WeeklyMemoryMaterialOutcomeState.Unknown,
                "weekly_material_publish_target_invalid", out _);
            RefreshWeeklyActionOutcomeWorkFlag();
            return WeeklyMemoryMaterialOutcomeOperationStatus.NotReady;
        }

        int storageDay = receipt.OriginGameDay < currentDay
                && HasCompressedMemoryBlock(memoryId, receipt.OriginGameDay)
            ? currentDay
            : receipt.OriginGameDay;
        List<DailyMemoryDraft> drafts = LoadDailyMemoryDraftsById(memoryId);
        DailyMemoryDraft draft = drafts.FirstOrDefault(item => item != null
            && item.GameDayIndex == storageDay);
        if (draft == null || draft.Lines == null || draft.Lines.Count == 0)
        {
            ScheduleWeeklyActionOutcomeRetry();
            return WeeklyMemoryMaterialOutcomeOperationStatus.NotReady;
        }

        string stableKey = "weekly_outcome:" + receipt.ReceiptId + ":" + receipt.PayloadHash;
        if (!HasExactWeeklyActionOutcomeTrigger(draft, receipt, stableKey))
        {
            List<string> labels = frozen.Atoms
                .Select(atom => atom?.Label)
                .Where(label => !string.IsNullOrWhiteSpace(label))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            AddWeeklyMemoryMaterialTriggerToDraft(draft, new WeeklyMemoryMaterialTrigger
            {
                MemoryId = memoryId,
                NpcName = frozen.NpcName,
                GameDayIndex = storageDay,
                GameDate = storageDay == receipt.OriginGameDay
                    ? frozen.OriginGameDate
                    : GetCurrentGameDateTextSafe(),
                SceneSessionId = receipt.SceneSessionId,
                DialogueSessionId = receipt.DialogueSessionId,
                TargetAgentIndex = receipt.TargetAgentIndex,
                FootholdKingdomId = frozen.FootholdKingdomId,
                FootholdSettlementId = frozen.FootholdSettlementId,
                NormalizedTagText = string.Join("\n", labels),
                Tags = labels,
                EstimatedValueDenars = frozen.EstimatedValueDenars,
                TriggerReason = frozen.Reason,
                StableKey = stableKey,
                OutcomeReceiptId = receipt.ReceiptId,
                OutcomeCandidateHash = receipt.CandidateHash,
                OutcomePayloadHash = receipt.PayloadHash,
                OutcomeActionFingerprint = receipt.ActionFingerprint,
                OutcomeTurnFingerprint = receipt.TurnFingerprint,
                CreatedUtcTicks = receipt.ConfirmedUtcTicks > 0L
                    ? receipt.ConfirmedUtcTicks
                    : DateTime.UtcNow.Ticks
            });
            SaveDailyMemoryDraftsById(memoryId, drafts);
            drafts = LoadDailyMemoryDraftsById(memoryId);
            draft = drafts.FirstOrDefault(item => item != null && item.GameDayIndex == storageDay);
        }
        if (!HasExactWeeklyActionOutcomeTrigger(draft, receipt, stableKey))
        {
            ScheduleWeeklyActionOutcomeRetry();
            return WeeklyMemoryMaterialOutcomeOperationStatus.NotReady;
        }

        status = ledger.MarkApplied(receiptId, candidateHash, out errorCode);
        RefreshWeeklyActionOutcomeWorkFlag();
        if (status == WeeklyMemoryMaterialOutcomeOperationStatus.Accepted
            || status == WeeklyMemoryMaterialOutcomeOperationStatus.Duplicate)
        {
            Logger.Log("WeeklyActionOutcome", "material attached receipt=" + receiptId
                + " memory=" + memoryId + " day=" + storageDay
                + " value=" + frozen.EstimatedValueDenars);
        }
        return status;
    }

    private static bool HasExactWeeklyActionOutcomeTrigger(
        DailyMemoryDraft draft,
        WeeklyMemoryMaterialOutcomeReceipt receipt,
        string stableKey)
    {
        if (draft?.WeeklyMaterialTriggers == null || receipt?.Payload == null)
        {
            return false;
        }
        WeeklyMemoryMaterialFrozenPayload frozen = receipt.Payload;
        List<string> labels = frozen.Atoms
            .Select(atom => atom?.Label)
            .Where(label => !string.IsNullOrWhiteSpace(label))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        string normalizedTagText = string.Join("\n", labels);
        return draft.WeeklyMaterialTriggers.Any(trigger => trigger != null
                && string.Equals(trigger.StableKey, stableKey, StringComparison.Ordinal)
                && string.Equals(trigger.OutcomeReceiptId, receipt.ReceiptId, StringComparison.Ordinal)
                && string.Equals(trigger.OutcomeCandidateHash, receipt.CandidateHash, StringComparison.Ordinal)
                && string.Equals(trigger.OutcomePayloadHash, receipt.PayloadHash, StringComparison.Ordinal)
                && string.Equals(trigger.OutcomeActionFingerprint, receipt.ActionFingerprint, StringComparison.Ordinal)
                && string.Equals(trigger.OutcomeTurnFingerprint, receipt.TurnFingerprint, StringComparison.Ordinal)
                && string.Equals(NormalizeMemoryHeroId(trigger.MemoryId), frozen.MemoryId,
                    StringComparison.OrdinalIgnoreCase)
                && string.Equals(trigger.NpcName, frozen.NpcName, StringComparison.Ordinal)
                && trigger.GameDayIndex == draft.GameDayIndex
                && trigger.SceneSessionId == receipt.SceneSessionId
                && trigger.DialogueSessionId == receipt.DialogueSessionId
                && trigger.TargetAgentIndex == receipt.TargetAgentIndex
                && string.Equals(trigger.FootholdKingdomId, frozen.FootholdKingdomId,
                    StringComparison.Ordinal)
                && string.Equals(trigger.FootholdSettlementId, frozen.FootholdSettlementId,
                    StringComparison.Ordinal)
                && string.Equals(trigger.NormalizedTagText, normalizedTagText,
                    StringComparison.Ordinal)
                && (trigger.Tags ?? new List<string>()).SequenceEqual(
                    labels,
                    StringComparer.OrdinalIgnoreCase)
                && trigger.EstimatedValueDenars == frozen.EstimatedValueDenars
                && string.Equals(trigger.TriggerReason, frozen.Reason, StringComparison.Ordinal));
    }

    private WeeklyMemoryMaterialOutcomeLedger EnsureWeeklyActionOutcomeLedger()
    {
        return EnsureWeeklyActionOutcomePublication().Ledger;
    }

    private bool IsWeeklyActionOutcomeOwnerActive()
        => EnsureWeeklyActionOutcomePublication().IsActive(SaveRuntimeGuard.CurrentGeneration);

    private void RefreshWeeklyActionOutcomeWorkFlag()
    {
        EnsureWeeklyActionOutcomePublication().RefreshWork();
    }

    private void ScheduleWeeklyActionOutcomeRetry()
    {
        EnsureWeeklyActionOutcomePublication().ScheduleRetry(DateTime.UtcNow.Ticks);
    }

    private void ProcessOneWeeklyActionOutcomeOnTick()
    {
        try
        {
            WeeklyMemoryMaterialOutcomeReceipt receipt = EnsureWeeklyActionOutcomePublication()
                .GetDue(SaveRuntimeGuard.CurrentGeneration, DateTime.UtcNow.Ticks);
            if (receipt != null) TryPublishWeeklyActionOutcome(receipt.ReceiptId, receipt.CandidateHash);
        }
        catch (Exception ex)
        {
            ScheduleWeeklyActionOutcomeRetry();
            Logger.Log("WeeklyActionOutcome", "[WARN] tick publish isolated error=" + ex.Message);
        }
    }

    private void ResetWeeklyActionOutcomeTransientState(string reason)
    {
        if (EnsureWeeklyActionOutcomePublication().Reset(reason, SaveRuntimeGuard.CurrentGeneration))
            _weeklyActionOutcomeStorage = new Dictionary<string, string>(StringComparer.Ordinal);
    }

    private void ActivateWeeklyActionOutcomeAfterLoad()
    {
        try
        {
            if (!IsWeeklyActionOutcomeOwnerActive())
            {
                EnsureWeeklyActionOutcomePublication().SuspendWork();
                Logger.Log("WeeklyActionOutcome", "[WARN] load activation not confirmed");
                return;
            }
            RefreshWeeklyActionOutcomeWorkFlag();
            ProcessOneWeeklyActionOutcomeOnTick();
        }
        catch (Exception ex)
        {
            ScheduleWeeklyActionOutcomeRetry();
            Logger.Log("WeeklyActionOutcome", "[WARN] load activation isolated error=" + ex.Message);
        }
    }

    private void SyncWeeklyActionOutcomeData(IDataStore dataStore)
    {
        try
        {
            WeeklyMemoryMaterialOutcomeLedger ledger = EnsureWeeklyActionOutcomeLedger();
            Dictionary<string, string> storage;
            if (dataStore.IsSaving)
            {
                if (EnsureWeeklyActionOutcomePublication().ImportConfirmed)
                {
                    _weeklyActionOutcomeStorage = ledger.Export();
                }
                storage = CampaignSaveChunkHelper.FlattenStringDictionary(
                    _weeklyActionOutcomeStorage,
                    WeeklyActionOutcomeReceiptsStorageKey,
                    "WeeklyActionOutcome");
                dataStore.SyncData(WeeklyActionOutcomeReceiptsStorageKey, ref storage);
                return;
            }

            storage = new Dictionary<string, string>(StringComparer.Ordinal);
            dataStore.SyncData(WeeklyActionOutcomeReceiptsStorageKey, ref storage);
            _weeklyActionOutcomeStorage = CampaignSaveChunkHelper.RestoreStringDictionary(
                storage,
                "WeeklyActionOutcome") ?? new Dictionary<string, string>(StringComparer.Ordinal);
            bool acceptedAll = EnsureWeeklyActionOutcomePublication().Import(
                _weeklyActionOutcomeStorage, SaveRuntimeGuard.CurrentGeneration, out string errorCode);
            if (!acceptedAll)
            {
                Logger.Log("WeeklyActionOutcome", "[WARN] recovery disabled; invalid journal preserved error=" + errorCode);
                return;
            }

        }
        catch (Exception ex)
        {
            EnsureWeeklyActionOutcomePublication().ImportFailed();
            Logger.Log("WeeklyActionOutcome", "[WARN] SyncData isolated error=" + ex.Message);
        }
    }

    private void ResetTailPersistenceTransientState(string reason)
    {
        ResetInteractionMemoryRecoveryTransientState(reason);
        ResetWeeklyActionOutcomeTransientState(reason);
    }

    private void ActivateTailPersistenceAfterLoad()
    {
        ActivateInteractionMemoryRecoveryAfterLoad();
        ActivateWeeklyActionOutcomeAfterLoad();
    }

    private void ProcessOneTailPersistenceRecoveryOnTick()
    {
        ProcessOneInteractionMemoryRecoveryOnTick();
        ProcessOneWeeklyActionOutcomeOnTick();
    }
}
