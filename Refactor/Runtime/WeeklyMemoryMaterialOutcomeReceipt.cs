using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using AnimusForge.Refactor.Contracts;

namespace AnimusForge.Refactor.Runtime;

internal enum WeeklyMemoryMaterialOutcomeState
{
    Prepared = 1,
    Confirmed = 2,
    Applied = 3,
    Rejected = 4,
    Partial = 5,
    Unknown = 6,
    Quarantined = 7
}

internal enum WeeklyMemoryMaterialOutcomeOperationStatus
{
    Rejected = 0,
    Accepted = 1,
    Duplicate = 2,
    Conflict = 3,
    CapacityExceeded = 4,
    NotFound = 5,
    NotReady = 6
}

internal enum WeeklyMemoryMaterialKind
{
    GiveAsset = 1,
    GiveGold = 2,
    DebtCreate = 3,
    DebtResolve = 4,
    SettlementTransfer = 5
}

internal static class WeeklyMemoryMaterialKindMapper
{
    internal static bool TryMap(
        EconomyRewardDebtActionKind source,
        out WeeklyMemoryMaterialKind target)
    {
        switch (source)
        {
            case EconomyRewardDebtActionKind.GiveAsset:
                target = WeeklyMemoryMaterialKind.GiveAsset;
                return true;
            case EconomyRewardDebtActionKind.GiveGold:
                target = WeeklyMemoryMaterialKind.GiveGold;
                return true;
            case EconomyRewardDebtActionKind.DebtCreate:
                target = WeeklyMemoryMaterialKind.DebtCreate;
                return true;
            case EconomyRewardDebtActionKind.DebtResolve:
                target = WeeklyMemoryMaterialKind.DebtResolve;
                return true;
            case EconomyRewardDebtActionKind.SettlementTransfer:
                target = WeeklyMemoryMaterialKind.SettlementTransfer;
                return true;
            default:
                target = default;
                return false;
        }
    }

    internal static WeeklyMemoryMaterialKind MapOrDefault(EconomyRewardDebtActionKind source)
        => TryMap(source, out WeeklyMemoryMaterialKind target) ? target : default;
}

/// <summary>
/// Transient, Economy-only projection used before action execution. The
/// source tag, free-text note, capability token and live ActionPlan are never
/// retained here. Fields that must still bind exact execution semantics are
/// covered by SemanticFingerprint and then by the ordered action fingerprint.
/// </summary>
internal sealed class WeeklyMemoryMaterialIntent
{
    internal WeeklyMemoryMaterialKind Kind { get; private set; }
    internal string TargetId { get; private set; } = string.Empty;
    internal string AssetToken { get; private set; } = string.Empty;
    internal string QuantityToken { get; private set; } = string.Empty;
    internal string AmountToken { get; private set; } = string.Empty;
    internal string DebtId { get; private set; } = string.Empty;
    internal string SettlementToken { get; private set; } = string.Empty;
    internal string DirectionToken { get; private set; } = string.Empty;
    internal string DueDaysToken { get; private set; } = string.Empty;
    internal string SemanticFingerprint { get; private set; } = string.Empty;

    private WeeklyMemoryMaterialIntent()
    {
    }

    internal static bool TryProject(
        EconomyRewardDebtAction action,
        out WeeklyMemoryMaterialIntent intent,
        out string errorCode)
    {
        intent = null;
        errorCode = string.Empty;
        if (action == null
            || !WeeklyMemoryMaterialKindMapper.TryMap(
                action.Kind,
                out WeeklyMemoryMaterialKind materialKind))
        {
            errorCode = "weekly_material_action_invalid";
            return false;
        }

        string targetId = WeeklyMemoryMaterialDataGuard.Normalize(action.TargetId);
        string assetToken = WeeklyMemoryMaterialDataGuard.Normalize(action.AssetToken);
        string quantityToken = WeeklyMemoryMaterialDataGuard.Normalize(action.QuantityToken);
        string amountToken = WeeklyMemoryMaterialDataGuard.Normalize(action.AmountToken);
        string debtId = WeeklyMemoryMaterialDataGuard.Normalize(action.DebtId);
        string settlementToken = WeeklyMemoryMaterialDataGuard.Normalize(action.SettlementToken);
        string directionToken = WeeklyMemoryMaterialDataGuard.Normalize(action.DirectionToken);
        string dueDaysToken = WeeklyMemoryMaterialDataGuard.Normalize(action.DueDaysToken);
        string hiddenNote = WeeklyMemoryMaterialDataGuard.Normalize(action.NoteToken);
        string hiddenCapability = WeeklyMemoryMaterialDataGuard.Normalize(action.CapabilityId);

        string[] boundedValues =
        {
            targetId,
            assetToken,
            quantityToken,
            amountToken,
            debtId,
            settlementToken,
            directionToken,
            dueDaysToken
        };
        if (boundedValues.Any(value => value.Length > WeeklyMemoryMaterialFingerprintHelper.MaximumSemanticTokenLength)
            || hiddenNote.Length > WeeklyMemoryMaterialFingerprintHelper.MaximumHiddenSemanticLength
            || hiddenCapability.Length > WeeklyMemoryMaterialFingerprintHelper.MaximumIdentityLength)
        {
            errorCode = "weekly_material_action_oversize";
            return false;
        }

        var candidate = new WeeklyMemoryMaterialIntent
        {
            Kind = materialKind,
            TargetId = targetId,
            AssetToken = assetToken,
            QuantityToken = quantityToken,
            AmountToken = amountToken,
            DebtId = debtId,
            SettlementToken = settlementToken,
            DirectionToken = directionToken,
            DueDaysToken = dueDaysToken
        };
        candidate.SemanticFingerprint = WeeklyMemoryMaterialFingerprintHelper.Hash(writer =>
        {
            writer.Write("AnimusForge.WeeklyMemoryMaterial.Semantic.v1");
            writer.Write(WeeklyMemoryMaterialFingerprintHelper.CurrentVersion);
            writer.Write((int)candidate.Kind);
            writer.Write(candidate.TargetId);
            writer.Write(candidate.AssetToken);
            writer.Write(candidate.QuantityToken);
            writer.Write(candidate.AmountToken);
            writer.Write(candidate.DebtId);
            writer.Write(candidate.SettlementToken);
            writer.Write(candidate.DirectionToken);
            writer.Write(candidate.DueDaysToken);
            writer.Write(hiddenNote);
            writer.Write(hiddenCapability);
        });
        if (!candidate.TryValidate(out errorCode))
        {
            return false;
        }
        intent = candidate;
        return true;
    }

    internal bool TryValidate(out string errorCode)
    {
        errorCode = string.Empty;
        if (!Enum.IsDefined(typeof(WeeklyMemoryMaterialKind), Kind)
            || !WeeklyMemoryMaterialDataGuard.IsHexDigest(SemanticFingerprint)
            || new[]
            {
                TargetId,
                AssetToken,
                QuantityToken,
                AmountToken,
                DebtId,
                SettlementToken,
                DirectionToken,
                DueDaysToken
            }.Any(value => value == null
                || value.Length > WeeklyMemoryMaterialFingerprintHelper.MaximumSemanticTokenLength))
        {
            errorCode = "weekly_material_intent_invalid";
            return false;
        }

        bool validForKind;
        switch (Kind)
        {
            case WeeklyMemoryMaterialKind.GiveAsset:
                validForKind = !string.IsNullOrWhiteSpace(AssetToken)
                    && !string.IsNullOrWhiteSpace(QuantityToken);
                break;
            case WeeklyMemoryMaterialKind.GiveGold:
                validForKind = !string.IsNullOrWhiteSpace(AmountToken);
                break;
            case WeeklyMemoryMaterialKind.DebtCreate:
                validForKind = !string.IsNullOrWhiteSpace(AmountToken)
                    && !string.IsNullOrWhiteSpace(DirectionToken)
                    && !string.IsNullOrWhiteSpace(DueDaysToken);
                break;
            case WeeklyMemoryMaterialKind.DebtResolve:
                validForKind = !string.IsNullOrWhiteSpace(DebtId);
                break;
            case WeeklyMemoryMaterialKind.SettlementTransfer:
                validForKind = !string.IsNullOrWhiteSpace(SettlementToken)
                    && !string.IsNullOrWhiteSpace(DirectionToken);
                break;
            default:
                validForKind = false;
                break;
        }
        if (!validForKind)
        {
            errorCode = "weekly_material_intent_semantics_invalid";
        }
        return validForKind;
    }

}

/// <summary>
/// One non-executable weekly material atom. Label is derived exclusively from
/// the Economy kind and can never be interpreted as an action protocol tag.
/// </summary>
internal sealed class WeeklyMemoryMaterialAtom
{
    internal WeeklyMemoryMaterialAtom(
        int intentIndex,
        EconomyRewardDebtActionKind kind,
        long valueDenars,
        string quantityToken)
        : this(
            intentIndex,
            WeeklyMemoryMaterialKindMapper.MapOrDefault(kind),
            valueDenars,
            quantityToken)
    {
    }

    internal WeeklyMemoryMaterialAtom(
        int intentIndex,
        WeeklyMemoryMaterialKind kind,
        long valueDenars,
        string quantityToken)
    {
        IntentIndex = intentIndex;
        Kind = kind;
        Label = BuildLabel(kind);
        ValueDenars = valueDenars;
        QuantityToken = WeeklyMemoryMaterialDataGuard.Normalize(quantityToken);
    }

    internal int IntentIndex { get; }
    internal WeeklyMemoryMaterialKind Kind { get; }
    internal string Label { get; }
    internal long ValueDenars { get; }
    internal string QuantityToken { get; }

    internal bool TryValidate(out string errorCode)
    {
        errorCode = string.Empty;
        if (IntentIndex < 0
            || !Enum.IsDefined(typeof(WeeklyMemoryMaterialKind), Kind)
            || !string.Equals(Label, BuildLabel(Kind), StringComparison.Ordinal)
            || ValueDenars < 0L
            || QuantityToken == null
            || QuantityToken.Length > WeeklyMemoryMaterialFrozenPayload.MaximumQuantityLength
            || (!string.IsNullOrEmpty(QuantityToken)
                && !string.Equals(QuantityToken, "ALL", StringComparison.OrdinalIgnoreCase)
                && (!long.TryParse(QuantityToken, out long parsedQuantity) || parsedQuantity <= 0L)))
        {
            errorCode = "weekly_material_atom_invalid";
            return false;
        }
        return true;
    }

    private static string BuildLabel(WeeklyMemoryMaterialKind kind)
    {
        switch (kind)
        {
            case WeeklyMemoryMaterialKind.GiveAsset:
                return "[WEEKLY:ECONOMY_GIVE_ASSET]";
            case WeeklyMemoryMaterialKind.GiveGold:
                return "[WEEKLY:ECONOMY_GIVE_GOLD]";
            case WeeklyMemoryMaterialKind.DebtCreate:
                return "[WEEKLY:ECONOMY_DEBT_CREATE]";
            case WeeklyMemoryMaterialKind.DebtResolve:
                return "[WEEKLY:ECONOMY_DEBT_RESOLVE]";
            case WeeklyMemoryMaterialKind.SettlementTransfer:
                return "[WEEKLY:ECONOMY_SETTLEMENT_TRANSFER]";
            default:
                return string.Empty;
        }
    }

}

/// <summary>
/// Frozen weekly presentation payload. It contains only memory/foothold
/// identity and derived semantic material; it contains no action protocol,
/// executor, callback, game object or free-form LLM action note.
/// </summary>
internal sealed class WeeklyMemoryMaterialFrozenPayload
{
    internal const int MaximumAtomCount = 64;
    internal const int MaximumNameLength = 256;
    internal const int MaximumDateLength = 256;
    internal const int MaximumReasonLength = 2048;
    internal const int MaximumQuantityLength = 128;

    private WeeklyMemoryMaterialFrozenPayload()
    {
    }

    internal string MemoryId { get; private set; } = string.Empty;
    internal string NpcName { get; private set; } = string.Empty;
    internal string OriginGameDate { get; private set; } = string.Empty;
    internal string FootholdKingdomId { get; private set; } = string.Empty;
    internal string FootholdSettlementId { get; private set; } = string.Empty;
    internal IReadOnlyList<WeeklyMemoryMaterialAtom> Atoms { get; private set; }
        = Array.Empty<WeeklyMemoryMaterialAtom>();
    internal long EstimatedValueDenars { get; private set; }
    internal string Reason { get; private set; } = string.Empty;

    internal static bool TryCreate(
        string memoryId,
        string npcName,
        string originGameDate,
        string footholdKingdomId,
        string footholdSettlementId,
        IEnumerable<WeeklyMemoryMaterialAtom> atoms,
        long estimatedValueDenars,
        string reason,
        out WeeklyMemoryMaterialFrozenPayload payload,
        out string errorCode)
    {
        payload = new WeeklyMemoryMaterialFrozenPayload
        {
            MemoryId = WeeklyMemoryMaterialDataGuard.Normalize(memoryId),
            NpcName = WeeklyMemoryMaterialDataGuard.Normalize(npcName),
            OriginGameDate = WeeklyMemoryMaterialDataGuard.Normalize(originGameDate),
            FootholdKingdomId = WeeklyMemoryMaterialDataGuard.Normalize(footholdKingdomId),
            FootholdSettlementId = WeeklyMemoryMaterialDataGuard.Normalize(footholdSettlementId),
            Atoms = new ReadOnlyCollection<WeeklyMemoryMaterialAtom>(
                new List<WeeklyMemoryMaterialAtom>(atoms ?? Enumerable.Empty<WeeklyMemoryMaterialAtom>())),
            EstimatedValueDenars = estimatedValueDenars,
            Reason = WeeklyMemoryMaterialDataGuard.Normalize(reason)
        };
        if (!payload.TryValidate(out errorCode))
        {
            payload = null;
            return false;
        }
        return true;
    }

    internal bool MatchesCandidate(
        WeeklyMemoryMaterialOutcomeCandidate candidate,
        out string errorCode)
    {
        errorCode = string.Empty;
        if (candidate == null || !candidate.TryValidate(out errorCode))
        {
            errorCode = string.IsNullOrWhiteSpace(errorCode)
                ? "weekly_material_candidate_invalid"
                : errorCode;
            return false;
        }
        foreach (WeeklyMemoryMaterialAtom atom in Atoms)
        {
            if (atom.IntentIndex >= candidate.Intents.Count
                || candidate.Intents[atom.IntentIndex].Kind != atom.Kind)
            {
                errorCode = "weekly_material_payload_intent_mismatch";
                return false;
            }
            string expectedQuantity = candidate.Intents[atom.IntentIndex].QuantityToken;
            if (!string.IsNullOrWhiteSpace(expectedQuantity)
                && !string.IsNullOrWhiteSpace(atom.QuantityToken)
                && !string.Equals(expectedQuantity, atom.QuantityToken, StringComparison.Ordinal))
            {
                errorCode = "weekly_material_payload_quantity_mismatch";
                return false;
            }
        }
        errorCode = string.Empty;
        return true;
    }

    internal bool TryValidate(out string errorCode)
    {
        errorCode = string.Empty;
        if (!WeeklyMemoryMaterialDataGuard.IsBoundedRequired(
                MemoryId,
                WeeklyMemoryMaterialFingerprintHelper.MaximumIdentityLength)
            || !WeeklyMemoryMaterialDataGuard.IsBoundedRequired(NpcName, MaximumNameLength)
            || !WeeklyMemoryMaterialDataGuard.IsBoundedRequired(OriginGameDate, MaximumDateLength)
            || !WeeklyMemoryMaterialDataGuard.IsBoundedRequired(
                FootholdKingdomId,
                WeeklyMemoryMaterialFingerprintHelper.MaximumIdentityLength)
            || FootholdSettlementId == null
            || FootholdSettlementId.Length > WeeklyMemoryMaterialFingerprintHelper.MaximumIdentityLength
            || Atoms == null
            || Atoms.Count == 0
            || Atoms.Count > MaximumAtomCount
            || EstimatedValueDenars < 0L
            || Reason == null
            || Reason.Length > MaximumReasonLength
            || WeeklyMemoryMaterialDataGuard.ContainsExecutableProtocol(Reason))
        {
            errorCode = "weekly_material_payload_invalid";
            return false;
        }

        long total = 0L;
        int previousIndex = -1;
        foreach (WeeklyMemoryMaterialAtom atom in Atoms)
        {
            if (atom == null || !atom.TryValidate(out errorCode)
                || atom.IntentIndex <= previousIndex)
            {
                errorCode = string.IsNullOrWhiteSpace(errorCode)
                    ? "weekly_material_atom_order_invalid"
                    : errorCode;
                return false;
            }
            previousIndex = atom.IntentIndex;
            try
            {
                total = checked(total + atom.ValueDenars);
            }
            catch (OverflowException)
            {
                errorCode = "weekly_material_payload_value_overflow";
                return false;
            }
        }
        if (total != EstimatedValueDenars)
        {
            errorCode = "weekly_material_payload_value_mismatch";
            return false;
        }
        return true;
    }

    internal string ComputeHash()
        => WeeklyMemoryMaterialFingerprintHelper.Hash(writer =>
        {
            writer.Write("AnimusForge.WeeklyMemoryMaterial.Payload.v1");
            writer.Write(WeeklyMemoryMaterialFingerprintHelper.CurrentVersion);
            writer.Write(MemoryId);
            writer.Write(NpcName);
            writer.Write(OriginGameDate);
            writer.Write(FootholdKingdomId);
            writer.Write(FootholdSettlementId);
            writer.Write(Atoms.Count);
            foreach (WeeklyMemoryMaterialAtom atom in Atoms)
            {
                writer.Write(atom.IntentIndex);
                writer.Write((int)atom.Kind);
                writer.Write(atom.Label);
                writer.Write(atom.ValueDenars);
                writer.Write(atom.QuantityToken);
            }
            writer.Write(EstimatedValueDenars);
            writer.Write(Reason);
        });
}

/// <summary>
/// Canonical transient identity for one exact request/turn and its ordered
/// Economy semantics. Only the bounded digests and turn identity are copied
/// into a persistent receipt; Intents are never serialized to AFWM1.
/// </summary>
internal sealed class WeeklyMemoryMaterialOutcomeCandidate
{
    internal int FingerprintVersion { get; private set; }
    internal string ReceiptId { get; private set; } = string.Empty;
    internal string RequestFingerprint { get; private set; } = string.Empty;
    internal string TraceId { get; private set; } = string.Empty;
    internal InteractionChannel Channel { get; private set; }
    internal string SessionId { get; private set; } = string.Empty;
    internal string SubjectId { get; private set; } = string.Empty;
    internal long RuntimeGeneration { get; private set; }
    internal long SaveGeneration { get; private set; }
    internal string CourierDirection { get; private set; } = string.Empty;
    internal int OriginGameDay { get; private set; }
    internal int OriginGameHour { get; private set; }
    internal string LocationId { get; private set; } = string.Empty;
    internal int SceneSessionId { get; private set; }
    internal int DialogueSessionId { get; private set; }
    internal int TargetAgentIndex { get; private set; }
    internal string TurnFingerprint { get; private set; } = string.Empty;
    internal string ActionFingerprint { get; private set; } = string.Empty;
    internal string CandidateHash { get; private set; } = string.Empty;
    internal IReadOnlyList<WeeklyMemoryMaterialIntent> Intents { get; private set; }
        = Array.Empty<WeeklyMemoryMaterialIntent>();

    private WeeklyMemoryMaterialOutcomeCandidate()
    {
    }

    internal bool TryValidate(out string errorCode)
    {
        errorCode = string.Empty;
        if (FingerprintVersion != WeeklyMemoryMaterialFingerprintHelper.CurrentVersion
            || !WeeklyMemoryMaterialDataGuard.IsHexDigest(ReceiptId)
            || !WeeklyMemoryMaterialDataGuard.IsHexDigest(RequestFingerprint)
            || !WeeklyMemoryMaterialDataGuard.IsHexDigest(TurnFingerprint)
            || !WeeklyMemoryMaterialDataGuard.IsHexDigest(ActionFingerprint)
            || !WeeklyMemoryMaterialDataGuard.IsHexDigest(CandidateHash)
            || !WeeklyMemoryMaterialFingerprintHelper.TryValidateTurnIdentity(
                TraceId,
                Channel,
                SessionId,
                SubjectId,
                RuntimeGeneration,
                SaveGeneration,
                CourierDirection,
                OriginGameDay,
                OriginGameHour,
                LocationId,
                SceneSessionId,
                DialogueSessionId,
                TargetAgentIndex,
                out errorCode)
            || Intents == null
            || Intents.Count == 0
            || Intents.Count > WeeklyMemoryMaterialFingerprintHelper.MaximumIntentCount)
        {
            if (string.IsNullOrWhiteSpace(errorCode))
            {
                errorCode = "weekly_material_candidate_invalid";
            }
            return false;
        }
        foreach (WeeklyMemoryMaterialIntent intent in Intents)
        {
            if (intent == null || !intent.TryValidate(out errorCode))
            {
                return false;
            }
        }

        string expectedTurn = WeeklyMemoryMaterialFingerprintHelper.BuildTurnFingerprint(
            TraceId,
            Channel,
            SessionId,
            SubjectId,
            RuntimeGeneration,
            SaveGeneration,
            CourierDirection,
            OriginGameDay,
            OriginGameHour,
            LocationId,
            SceneSessionId,
            DialogueSessionId,
            TargetAgentIndex);
        string expectedAction = WeeklyMemoryMaterialFingerprintHelper.BuildActionFingerprint(Intents);
        string expectedReceiptId = WeeklyMemoryMaterialFingerprintHelper.BuildReceiptId(RequestFingerprint);
        string expectedCandidateHash = WeeklyMemoryMaterialFingerprintHelper.BuildCandidateHash(
            FingerprintVersion,
            ReceiptId,
            RequestFingerprint,
            TraceId,
            Channel,
            SessionId,
            SubjectId,
            RuntimeGeneration,
            SaveGeneration,
            CourierDirection,
            OriginGameDay,
            OriginGameHour,
            LocationId,
            SceneSessionId,
            DialogueSessionId,
            TargetAgentIndex,
            TurnFingerprint,
            ActionFingerprint);
        if (!string.Equals(expectedTurn, TurnFingerprint, StringComparison.Ordinal)
            || !string.Equals(expectedAction, ActionFingerprint, StringComparison.Ordinal)
            || !string.Equals(expectedReceiptId, ReceiptId, StringComparison.Ordinal)
            || !string.Equals(expectedCandidateHash, CandidateHash, StringComparison.Ordinal))
        {
            errorCode = "weekly_material_candidate_fingerprint_mismatch";
            return false;
        }
        return true;
    }

    internal static WeeklyMemoryMaterialOutcomeCandidate Create(
        string requestFingerprint,
        string traceId,
        InteractionChannel channel,
        string sessionId,
        string subjectId,
        long runtimeGeneration,
        long saveGeneration,
        string courierDirection,
        int originGameDay,
        int originGameHour,
        string locationId,
        int sceneSessionId,
        int dialogueSessionId,
        int targetAgentIndex,
        IReadOnlyList<WeeklyMemoryMaterialIntent> intents)
    {
        string receiptId = WeeklyMemoryMaterialFingerprintHelper.BuildReceiptId(requestFingerprint);
        string turnFingerprint = WeeklyMemoryMaterialFingerprintHelper.BuildTurnFingerprint(
            traceId,
            channel,
            sessionId,
            subjectId,
            runtimeGeneration,
            saveGeneration,
            courierDirection,
            originGameDay,
            originGameHour,
            locationId,
            sceneSessionId,
            dialogueSessionId,
            targetAgentIndex);
        string actionFingerprint = WeeklyMemoryMaterialFingerprintHelper.BuildActionFingerprint(intents);
        return new WeeklyMemoryMaterialOutcomeCandidate
        {
            FingerprintVersion = WeeklyMemoryMaterialFingerprintHelper.CurrentVersion,
            ReceiptId = receiptId,
            RequestFingerprint = requestFingerprint,
            TraceId = traceId,
            Channel = channel,
            SessionId = sessionId,
            SubjectId = subjectId,
            RuntimeGeneration = runtimeGeneration,
            SaveGeneration = saveGeneration,
            CourierDirection = courierDirection,
            OriginGameDay = originGameDay,
            OriginGameHour = originGameHour,
            LocationId = locationId,
            SceneSessionId = sceneSessionId,
            DialogueSessionId = dialogueSessionId,
            TargetAgentIndex = targetAgentIndex,
            TurnFingerprint = turnFingerprint,
            ActionFingerprint = actionFingerprint,
            CandidateHash = WeeklyMemoryMaterialFingerprintHelper.BuildCandidateHash(
                WeeklyMemoryMaterialFingerprintHelper.CurrentVersion,
                receiptId,
                requestFingerprint,
                traceId,
                channel,
                sessionId,
                subjectId,
                runtimeGeneration,
                saveGeneration,
                courierDirection,
                originGameDay,
                originGameHour,
                locationId,
                sceneSessionId,
                dialogueSessionId,
                targetAgentIndex,
                turnFingerprint,
                actionFingerprint),
            Intents = new ReadOnlyCollection<WeeklyMemoryMaterialIntent>(
                new List<WeeklyMemoryMaterialIntent>(intents))
        };
    }
}

internal static class WeeklyMemoryMaterialFingerprintHelper
{
    internal const int CurrentVersion = 1;
    internal const int MaximumIntentCount = 64;
    internal const int MaximumIdentityLength = 512;
    internal const int MaximumDirectionLength = 128;
    internal const int MaximumSemanticTokenLength = 4096;
    internal const int MaximumHiddenSemanticLength = 4096;

    internal static bool TryCreateCandidate(
        string requestId,
        GameInteractionSnapshot snapshot,
        EconomyRewardDebtReplayPlan plan,
        out WeeklyMemoryMaterialOutcomeCandidate candidate,
        out string errorCode)
    {
        candidate = null;
        errorCode = string.Empty;
        if (snapshot?.Identity == null || snapshot.Trace == null)
        {
            errorCode = "weekly_material_snapshot_invalid";
            return false;
        }
        return TryCreateCandidate(
            requestId,
            snapshot.Trace.TraceId,
            snapshot.Identity.Channel,
            snapshot.Identity.SessionId,
            snapshot.Identity.SubjectId,
            snapshot.Trace.RuntimeGeneration,
            snapshot.Trace.SaveGeneration,
            ReadDetached(snapshot, "courier_direction"),
            snapshot.GameDay,
            snapshot.GameHour,
            snapshot.LocationId,
            ReadDetachedInt(snapshot, "scene_session_id"),
            ReadDetachedInt(snapshot, "native_dialogue_session_id", "dialogue_session_id"),
            ReadDetachedInt(snapshot, "target_agent_index"),
            plan,
            out candidate,
            out errorCode);
    }

    internal static bool TryCreateCandidate(
        string requestId,
        string traceId,
        InteractionChannel channel,
        string sessionId,
        string subjectId,
        long runtimeGeneration,
        long saveGeneration,
        string courierDirection,
        int originGameDay,
        int originGameHour,
        string locationId,
        int sceneSessionId,
        int dialogueSessionId,
        int targetAgentIndex,
        EconomyRewardDebtReplayPlan plan,
        out WeeklyMemoryMaterialOutcomeCandidate candidate,
        out string errorCode)
    {
        candidate = null;
        if (plan == null)
        {
            errorCode = "weekly_material_economy_plan_missing";
            return false;
        }
        if (plan.HasExcludedActions)
        {
            errorCode = "weekly_material_economy_plan_excluded";
            return false;
        }
        return TryCreateCandidate(
            requestId,
            traceId,
            channel,
            sessionId,
            subjectId,
            runtimeGeneration,
            saveGeneration,
            courierDirection,
            originGameDay,
            originGameHour,
            locationId,
            sceneSessionId,
            dialogueSessionId,
            targetAgentIndex,
            plan.Actions,
            out candidate,
            out errorCode);
    }

    internal static bool TryCreateCandidate(
        string requestId,
        string traceId,
        InteractionChannel channel,
        string sessionId,
        string subjectId,
        long runtimeGeneration,
        long saveGeneration,
        string courierDirection,
        int originGameDay,
        int originGameHour,
        string locationId,
        int sceneSessionId,
        int dialogueSessionId,
        int targetAgentIndex,
        IEnumerable<EconomyRewardDebtAction> actions,
        out WeeklyMemoryMaterialOutcomeCandidate candidate,
        out string errorCode)
    {
        candidate = null;
        errorCode = string.Empty;
        string normalizedRequestId = WeeklyMemoryMaterialDataGuard.Normalize(requestId);
        string normalizedTraceId = WeeklyMemoryMaterialDataGuard.Normalize(traceId);
        string normalizedSessionId = WeeklyMemoryMaterialDataGuard.Normalize(sessionId);
        string normalizedSubjectId = WeeklyMemoryMaterialDataGuard.Normalize(subjectId);
        string normalizedDirection = WeeklyMemoryMaterialDataGuard.Normalize(courierDirection);
        string normalizedLocationId = WeeklyMemoryMaterialDataGuard.Normalize(locationId);
        if (!WeeklyMemoryMaterialDataGuard.IsBoundedRequired(normalizedRequestId, MaximumIdentityLength))
        {
            errorCode = "weekly_material_request_id_invalid";
            return false;
        }
        if (!TryValidateTurnIdentity(
            normalizedTraceId,
            channel,
            normalizedSessionId,
            normalizedSubjectId,
            runtimeGeneration,
            saveGeneration,
            normalizedDirection,
            originGameDay,
            originGameHour,
            normalizedLocationId,
            sceneSessionId,
            dialogueSessionId,
            targetAgentIndex,
            out errorCode))
        {
            return false;
        }

        if (!TryProjectActionIntents(actions, out List<WeeklyMemoryMaterialIntent> intents, out errorCode))
        {
            return false;
        }

        string requestFingerprint = Hash(writer =>
        {
            writer.Write("AnimusForge.WeeklyMemoryMaterial.Request.v1");
            writer.Write(CurrentVersion);
            writer.Write(normalizedRequestId);
        });
        candidate = WeeklyMemoryMaterialOutcomeCandidate.Create(
            requestFingerprint,
            normalizedTraceId,
            channel,
            normalizedSessionId,
            normalizedSubjectId,
            runtimeGeneration,
            saveGeneration,
            normalizedDirection,
            originGameDay,
            originGameHour,
            normalizedLocationId,
            sceneSessionId,
            dialogueSessionId,
            targetAgentIndex,
            intents);
        if (!candidate.TryValidate(out errorCode))
        {
            candidate = null;
            return false;
        }
        return true;
    }

    internal static bool TryBuildActionFingerprint(
        EconomyRewardDebtReplayPlan plan,
        out string actionFingerprint,
        out string errorCode)
    {
        actionFingerprint = string.Empty;
        errorCode = string.Empty;
        if (plan == null
            || !TryProjectActionIntents(plan.Actions,
                out List<WeeklyMemoryMaterialIntent> intents,
                out errorCode))
        {
            errorCode = string.IsNullOrWhiteSpace(errorCode)
                ? "weekly_material_plan_invalid"
                : errorCode;
            return false;
        }
        actionFingerprint = BuildActionFingerprint(intents);
        return WeeklyMemoryMaterialDataGuard.IsHexDigest(actionFingerprint);
    }

    private static bool TryProjectActionIntents(
        IEnumerable<EconomyRewardDebtAction> actions,
        out List<WeeklyMemoryMaterialIntent> intents,
        out string errorCode)
    {
        List<EconomyRewardDebtAction> sourceActions =
            new List<EconomyRewardDebtAction>(actions ?? Enumerable.Empty<EconomyRewardDebtAction>());
        intents = new List<WeeklyMemoryMaterialIntent>(sourceActions.Count);
        errorCode = string.Empty;
        if (sourceActions.Count == 0 || sourceActions.Count > MaximumIntentCount)
        {
            errorCode = "weekly_material_action_count_invalid";
            return false;
        }
        foreach (EconomyRewardDebtAction action in sourceActions)
        {
            if (!WeeklyMemoryMaterialIntent.TryProject(
                    action,
                    out WeeklyMemoryMaterialIntent intent,
                    out errorCode))
            {
                intents.Clear();
                return false;
            }
            intents.Add(intent);
        }
        return true;
    }

    internal static bool TryValidateTurnIdentity(
        string traceId,
        InteractionChannel channel,
        string sessionId,
        string subjectId,
        long runtimeGeneration,
        long saveGeneration,
        string courierDirection,
        int originGameDay,
        int originGameHour,
        string locationId,
        int sceneSessionId,
        int dialogueSessionId,
        int targetAgentIndex,
        out string errorCode)
    {
        errorCode = string.Empty;
        bool courier = channel == InteractionChannel.Courier;
        if (!Enum.IsDefined(typeof(InteractionChannel), channel)
            || !WeeklyMemoryMaterialDataGuard.IsBoundedRequired(traceId, MaximumIdentityLength)
            || !WeeklyMemoryMaterialDataGuard.IsBoundedRequired(sessionId, MaximumIdentityLength)
            || !WeeklyMemoryMaterialDataGuard.IsBoundedRequired(subjectId, MaximumIdentityLength)
            || runtimeGeneration <= 0L
            || saveGeneration < 0L
            || originGameDay < 0
            || originGameHour < 0
            || originGameHour > 23
            || locationId == null
            || locationId.Length > MaximumIdentityLength
            || sceneSessionId < -1
            || dialogueSessionId < -1
            || targetAgentIndex < -1
            || courierDirection == null
            || courierDirection.Length > MaximumDirectionLength
            || (courier && string.IsNullOrWhiteSpace(courierDirection))
            || (!courier && !string.IsNullOrEmpty(courierDirection)))
        {
            errorCode = "weekly_material_turn_identity_invalid";
            return false;
        }
        return true;
    }

    internal static string BuildReceiptId(string requestFingerprint)
        => Hash(writer =>
        {
            writer.Write("AnimusForge.WeeklyMemoryMaterial.Receipt.v1");
            writer.Write(CurrentVersion);
            writer.Write(requestFingerprint ?? string.Empty);
        });

    internal static string BuildTurnFingerprint(
        string traceId,
        InteractionChannel channel,
        string sessionId,
        string subjectId,
        long runtimeGeneration,
        long saveGeneration,
        string courierDirection,
        int originGameDay,
        int originGameHour,
        string locationId,
        int sceneSessionId,
        int dialogueSessionId,
        int targetAgentIndex)
        => Hash(writer =>
        {
            writer.Write("AnimusForge.WeeklyMemoryMaterial.Turn.v1");
            writer.Write(CurrentVersion);
            writer.Write(traceId ?? string.Empty);
            writer.Write((int)channel);
            writer.Write(sessionId ?? string.Empty);
            writer.Write(subjectId ?? string.Empty);
            writer.Write(runtimeGeneration);
            writer.Write(saveGeneration);
            writer.Write(courierDirection ?? string.Empty);
            writer.Write(originGameDay);
            writer.Write(originGameHour);
            writer.Write(locationId ?? string.Empty);
            writer.Write(sceneSessionId);
            writer.Write(dialogueSessionId);
            writer.Write(targetAgentIndex);
        });

    internal static string BuildActionFingerprint(
        IReadOnlyList<WeeklyMemoryMaterialIntent> intents)
        => Hash(writer =>
        {
            writer.Write("AnimusForge.WeeklyMemoryMaterial.Action.v1");
            writer.Write(CurrentVersion);
            writer.Write(intents?.Count ?? 0);
            foreach (WeeklyMemoryMaterialIntent intent in intents ?? Array.Empty<WeeklyMemoryMaterialIntent>())
            {
                writer.Write((int)intent.Kind);
                writer.Write(intent.TargetId);
                writer.Write(intent.AssetToken);
                writer.Write(intent.QuantityToken);
                writer.Write(intent.AmountToken);
                writer.Write(intent.DebtId);
                writer.Write(intent.SettlementToken);
                writer.Write(intent.DirectionToken);
                writer.Write(intent.DueDaysToken);
                writer.Write(intent.SemanticFingerprint);
            }
        });

    internal static string BuildCandidateHash(
        int fingerprintVersion,
        string receiptId,
        string requestFingerprint,
        string traceId,
        InteractionChannel channel,
        string sessionId,
        string subjectId,
        long runtimeGeneration,
        long saveGeneration,
        string courierDirection,
        int originGameDay,
        int originGameHour,
        string locationId,
        int sceneSessionId,
        int dialogueSessionId,
        int targetAgentIndex,
        string turnFingerprint,
        string actionFingerprint)
        => Hash(writer =>
        {
            writer.Write("AnimusForge.WeeklyMemoryMaterial.Candidate.v1");
            writer.Write(fingerprintVersion);
            writer.Write(receiptId ?? string.Empty);
            writer.Write(requestFingerprint ?? string.Empty);
            writer.Write(traceId ?? string.Empty);
            writer.Write((int)channel);
            writer.Write(sessionId ?? string.Empty);
            writer.Write(subjectId ?? string.Empty);
            writer.Write(runtimeGeneration);
            writer.Write(saveGeneration);
            writer.Write(courierDirection ?? string.Empty);
            writer.Write(originGameDay);
            writer.Write(originGameHour);
            writer.Write(locationId ?? string.Empty);
            writer.Write(sceneSessionId);
            writer.Write(dialogueSessionId);
            writer.Write(targetAgentIndex);
            writer.Write(turnFingerprint ?? string.Empty);
            writer.Write(actionFingerprint ?? string.Empty);
        });

    internal static string Hash(Action<BinaryWriter> write)
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

    private static string ReadDetached(GameInteractionSnapshot snapshot, string key)
        => snapshot != null
            && snapshot.DetachedFacts.TryGetValue(key, out string value)
                ? WeeklyMemoryMaterialDataGuard.Normalize(value)
                : string.Empty;

    private static int ReadDetachedInt(GameInteractionSnapshot snapshot, params string[] keys)
    {
        foreach (string key in keys ?? Array.Empty<string>())
        {
            if (snapshot != null
                && snapshot.DetachedFacts.TryGetValue(key, out string value)
                && int.TryParse(value, out int parsed))
            {
                return Math.Max(-1, parsed);
            }
        }
        return -1;
    }
}

/// <summary>
/// Durable AFWM1 receipt. It deliberately copies only canonical identity
/// digests plus the frozen weekly payload; the transient Economy intents are
/// not referenced and cannot be reconstructed from this record.
/// </summary>
internal sealed class WeeklyMemoryMaterialOutcomeReceipt
{
    internal const int CurrentSchemaVersion = 1;
    internal const int MaximumSerializedLength = 196608;
    private const int MaximumBodyLength = 147456;
    private const int MaximumDiagnosticLength = 512;
    private const string WirePrefix = "AFWM1:";

    private WeeklyMemoryMaterialOutcomeReceipt()
    {
    }

    internal int SchemaVersion { get; private set; }
    internal int FingerprintVersion { get; private set; }
    internal WeeklyMemoryMaterialOutcomeState State { get; private set; }
    internal string ReceiptId { get; private set; } = string.Empty;
    internal string RequestFingerprint { get; private set; } = string.Empty;
    internal string TraceId { get; private set; } = string.Empty;
    internal InteractionChannel Channel { get; private set; }
    internal string SessionId { get; private set; } = string.Empty;
    internal string SubjectId { get; private set; } = string.Empty;
    internal long RuntimeGeneration { get; private set; }
    internal long SaveGeneration { get; private set; }
    internal string CourierDirection { get; private set; } = string.Empty;
    internal int OriginGameDay { get; private set; }
    internal int OriginGameHour { get; private set; }
    internal string LocationId { get; private set; } = string.Empty;
    internal int SceneSessionId { get; private set; }
    internal int DialogueSessionId { get; private set; }
    internal int TargetAgentIndex { get; private set; }
    internal string TurnFingerprint { get; private set; } = string.Empty;
    internal string ActionFingerprint { get; private set; } = string.Empty;
    internal string CandidateHash { get; private set; } = string.Empty;
    internal WeeklyMemoryMaterialFrozenPayload Payload { get; private set; }
    internal string PayloadHash { get; private set; } = string.Empty;
    internal long CreatedUtcTicks { get; private set; }
    internal long ConfirmedUtcTicks { get; private set; }
    internal long TerminalUtcTicks { get; private set; }
    internal long AppliedUtcTicks { get; private set; }
    internal string DiagnosticCode { get; private set; } = string.Empty;

    internal static bool TryCreate(
        WeeklyMemoryMaterialOutcomeCandidate candidate,
        WeeklyMemoryMaterialFrozenPayload payload,
        long createdUtcTicks,
        out WeeklyMemoryMaterialOutcomeReceipt receipt,
        out string errorCode)
    {
        receipt = null;
        errorCode = string.Empty;
        if (candidate == null || !candidate.TryValidate(out errorCode))
        {
            errorCode = string.IsNullOrWhiteSpace(errorCode)
                ? "weekly_material_candidate_invalid"
                : errorCode;
            return false;
        }
        if (payload == null || !payload.TryValidate(out errorCode)
            || !payload.MatchesCandidate(candidate, out errorCode))
        {
            errorCode = string.IsNullOrWhiteSpace(errorCode)
                ? "weekly_material_payload_invalid"
                : errorCode;
            return false;
        }
        if (createdUtcTicks <= 0L)
        {
            errorCode = "weekly_material_created_time_invalid";
            return false;
        }

        var created = new WeeklyMemoryMaterialOutcomeReceipt
        {
            SchemaVersion = CurrentSchemaVersion,
            FingerprintVersion = candidate.FingerprintVersion,
            State = WeeklyMemoryMaterialOutcomeState.Prepared,
            ReceiptId = candidate.ReceiptId,
            RequestFingerprint = candidate.RequestFingerprint,
            TraceId = candidate.TraceId,
            Channel = candidate.Channel,
            SessionId = candidate.SessionId,
            SubjectId = candidate.SubjectId,
            RuntimeGeneration = candidate.RuntimeGeneration,
            SaveGeneration = candidate.SaveGeneration,
            CourierDirection = candidate.CourierDirection,
            OriginGameDay = candidate.OriginGameDay,
            OriginGameHour = candidate.OriginGameHour,
            LocationId = candidate.LocationId,
            SceneSessionId = candidate.SceneSessionId,
            DialogueSessionId = candidate.DialogueSessionId,
            TargetAgentIndex = candidate.TargetAgentIndex,
            TurnFingerprint = candidate.TurnFingerprint,
            ActionFingerprint = candidate.ActionFingerprint,
            CandidateHash = candidate.CandidateHash,
            Payload = payload,
            PayloadHash = payload.ComputeHash(),
            CreatedUtcTicks = createdUtcTicks,
            ConfirmedUtcTicks = 0L,
            TerminalUtcTicks = 0L,
            AppliedUtcTicks = 0L,
            DiagnosticCode = string.Empty
        };
        if (!created.TryValidate(out errorCode))
        {
            return false;
        }
        receipt = created;
        return true;
    }

    internal WeeklyMemoryMaterialOutcomeOperationStatus Transition(
        WeeklyMemoryMaterialOutcomeState targetState,
        string diagnosticCode,
        long utcTicks,
        out string errorCode)
    {
        errorCode = string.Empty;
        if (targetState == State)
        {
            return WeeklyMemoryMaterialOutcomeOperationStatus.Duplicate;
        }
        bool allowed = State == WeeklyMemoryMaterialOutcomeState.Prepared
                && (targetState == WeeklyMemoryMaterialOutcomeState.Confirmed
                    || targetState == WeeklyMemoryMaterialOutcomeState.Rejected
                    || targetState == WeeklyMemoryMaterialOutcomeState.Partial
                    || targetState == WeeklyMemoryMaterialOutcomeState.Unknown
                    || targetState == WeeklyMemoryMaterialOutcomeState.Quarantined)
            || State == WeeklyMemoryMaterialOutcomeState.Confirmed
                && (targetState == WeeklyMemoryMaterialOutcomeState.Applied
                    || targetState == WeeklyMemoryMaterialOutcomeState.Rejected
                    || targetState == WeeklyMemoryMaterialOutcomeState.Unknown
                    || targetState == WeeklyMemoryMaterialOutcomeState.Quarantined);
        long minimumTicks = State == WeeklyMemoryMaterialOutcomeState.Confirmed
            ? Math.Max(CreatedUtcTicks, ConfirmedUtcTicks)
            : CreatedUtcTicks;
        if (!allowed || utcTicks <= 0L)
        {
            errorCode = "weekly_material_state_transition_invalid";
            return WeeklyMemoryMaterialOutcomeOperationStatus.Conflict;
        }
        long transitionTicks = Math.Max(minimumTicks, utcTicks);

        State = targetState;
        if (targetState == WeeklyMemoryMaterialOutcomeState.Confirmed)
        {
            ConfirmedUtcTicks = transitionTicks;
            DiagnosticCode = string.Empty;
        }
        else if (targetState == WeeklyMemoryMaterialOutcomeState.Applied)
        {
            AppliedUtcTicks = transitionTicks;
            TerminalUtcTicks = transitionTicks;
            DiagnosticCode = string.Empty;
        }
        else
        {
            TerminalUtcTicks = transitionTicks;
            DiagnosticCode = WeeklyMemoryMaterialDataGuard.NormalizeDiagnostic(
                diagnosticCode,
                MaximumDiagnosticLength);
        }
        if (!TryValidate(out errorCode))
        {
            return WeeklyMemoryMaterialOutcomeOperationStatus.Conflict;
        }
        return WeeklyMemoryMaterialOutcomeOperationStatus.Accepted;
    }

    internal void Quarantine(string diagnosticCode, long utcTicks)
    {
        State = WeeklyMemoryMaterialOutcomeState.Quarantined;
        TerminalUtcTicks = Math.Max(
            Math.Max(CreatedUtcTicks, ConfirmedUtcTicks),
            Math.Max(1L, utcTicks));
        AppliedUtcTicks = 0L;
        DiagnosticCode = WeeklyMemoryMaterialDataGuard.NormalizeDiagnostic(
            diagnosticCode,
            MaximumDiagnosticLength);
    }

    internal void MarkLoadedPreparedUnknown(long utcTicks)
    {
        if (State != WeeklyMemoryMaterialOutcomeState.Prepared)
        {
            return;
        }
        State = WeeklyMemoryMaterialOutcomeState.Unknown;
        TerminalUtcTicks = Math.Max(CreatedUtcTicks, Math.Max(1L, utcTicks));
        DiagnosticCode = "weekly_material_loaded_prepared_unknown";
    }

    internal string Serialize()
    {
        if (!TryValidate(out string errorCode))
        {
            throw new InvalidOperationException(errorCode);
        }
        byte[] body = SerializeBody();
        if (body.Length <= 0 || body.Length > MaximumBodyLength)
        {
            throw new InvalidOperationException("weekly_material_body_oversize");
        }
        byte[] checksum = ComputeChecksum(body);
        using (var stream = new MemoryStream())
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
        {
            writer.Write(body.Length);
            writer.Write(body);
            writer.Write(checksum.Length);
            writer.Write(checksum);
            writer.Flush();
            string wire = WirePrefix + Convert.ToBase64String(stream.ToArray());
            if (wire.Length > MaximumSerializedLength)
            {
                throw new InvalidOperationException("weekly_material_wire_oversize");
            }
            return wire;
        }
    }

    internal static bool TryDeserialize(
        string serialized,
        out WeeklyMemoryMaterialOutcomeReceipt receipt,
        out string errorCode)
    {
        receipt = null;
        errorCode = string.Empty;
        string wire = serialized ?? string.Empty;
        if (!wire.StartsWith(WirePrefix, StringComparison.Ordinal)
            || wire.Length > MaximumSerializedLength)
        {
            errorCode = "weekly_material_wire_invalid";
            return false;
        }
        try
        {
            byte[] bytes = Convert.FromBase64String(wire.Substring(WirePrefix.Length));
            using (var stream = new MemoryStream(bytes, writable: false))
            using (var reader = new BinaryReader(stream, Encoding.UTF8, true))
            {
                int bodyLength = reader.ReadInt32();
                if (bodyLength <= 0
                    || bodyLength > MaximumBodyLength
                    || bodyLength > stream.Length - stream.Position)
                {
                    errorCode = "weekly_material_body_length_invalid";
                    return false;
                }
                byte[] body = reader.ReadBytes(bodyLength);
                int checksumLength = reader.ReadInt32();
                if (checksumLength != 32 || checksumLength > stream.Length - stream.Position)
                {
                    errorCode = "weekly_material_checksum_mismatch";
                    return false;
                }
                byte[] checksum = reader.ReadBytes(checksumLength);
                if (stream.Position != stream.Length
                    || !FixedTimeEquals(checksum, ComputeChecksum(body)))
                {
                    errorCode = "weekly_material_checksum_mismatch";
                    return false;
                }
                if (!TryDeserializeBody(body, out receipt, out errorCode))
                {
                    receipt = null;
                    return false;
                }
                return true;
            }
        }
        catch
        {
            receipt = null;
            errorCode = "weekly_material_wire_invalid";
            return false;
        }
    }

    private byte[] SerializeBody()
    {
        using (var stream = new MemoryStream())
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
        {
            writer.Write(SchemaVersion);
            writer.Write(FingerprintVersion);
            writer.Write((int)State);
            writer.Write(ReceiptId);
            writer.Write(RequestFingerprint);
            writer.Write(TraceId);
            writer.Write((int)Channel);
            writer.Write(SessionId);
            writer.Write(SubjectId);
            writer.Write(RuntimeGeneration);
            writer.Write(SaveGeneration);
            writer.Write(CourierDirection);
            writer.Write(OriginGameDay);
            writer.Write(OriginGameHour);
            writer.Write(LocationId);
            writer.Write(SceneSessionId);
            writer.Write(DialogueSessionId);
            writer.Write(TargetAgentIndex);
            writer.Write(TurnFingerprint);
            writer.Write(ActionFingerprint);
            writer.Write(CandidateHash);
            writer.Write(Payload.MemoryId);
            writer.Write(Payload.NpcName);
            writer.Write(Payload.OriginGameDate);
            writer.Write(Payload.FootholdKingdomId);
            writer.Write(Payload.FootholdSettlementId);
            writer.Write(Payload.Atoms.Count);
            foreach (WeeklyMemoryMaterialAtom atom in Payload.Atoms)
            {
                writer.Write(atom.IntentIndex);
                writer.Write((int)atom.Kind);
                writer.Write(atom.Label);
                writer.Write(atom.ValueDenars);
                writer.Write(atom.QuantityToken);
            }
            writer.Write(Payload.EstimatedValueDenars);
            writer.Write(Payload.Reason);
            writer.Write(PayloadHash);
            writer.Write(CreatedUtcTicks);
            writer.Write(ConfirmedUtcTicks);
            writer.Write(TerminalUtcTicks);
            writer.Write(AppliedUtcTicks);
            writer.Write(DiagnosticCode);
            writer.Flush();
            return stream.ToArray();
        }
    }

    private static bool TryDeserializeBody(
        byte[] body,
        out WeeklyMemoryMaterialOutcomeReceipt receipt,
        out string errorCode)
    {
        receipt = null;
        errorCode = string.Empty;
        using (var stream = new MemoryStream(body, writable: false))
        using (var reader = new BinaryReader(stream, Encoding.UTF8, true))
        {
            int schemaVersion = reader.ReadInt32();
            int fingerprintVersion = reader.ReadInt32();
            WeeklyMemoryMaterialOutcomeState state = (WeeklyMemoryMaterialOutcomeState)reader.ReadInt32();
            string receiptId = reader.ReadString();
            string requestFingerprint = reader.ReadString();
            string traceId = reader.ReadString();
            InteractionChannel channel = (InteractionChannel)reader.ReadInt32();
            string sessionId = reader.ReadString();
            string subjectId = reader.ReadString();
            long runtimeGeneration = reader.ReadInt64();
            long saveGeneration = reader.ReadInt64();
            string courierDirection = reader.ReadString();
            int originGameDay = reader.ReadInt32();
            int originGameHour = reader.ReadInt32();
            string locationId = reader.ReadString();
            int sceneSessionId = reader.ReadInt32();
            int dialogueSessionId = reader.ReadInt32();
            int targetAgentIndex = reader.ReadInt32();
            string turnFingerprint = reader.ReadString();
            string actionFingerprint = reader.ReadString();
            string candidateHash = reader.ReadString();
            string memoryId = reader.ReadString();
            string npcName = reader.ReadString();
            string originGameDate = reader.ReadString();
            string footholdKingdomId = reader.ReadString();
            string footholdSettlementId = reader.ReadString();
            int atomCount = reader.ReadInt32();
            if (atomCount <= 0 || atomCount > WeeklyMemoryMaterialFrozenPayload.MaximumAtomCount)
            {
                errorCode = "weekly_material_atom_count_invalid";
                return false;
            }
            var atoms = new List<WeeklyMemoryMaterialAtom>(atomCount);
            for (int index = 0; index < atomCount; index++)
            {
                int intentIndex = reader.ReadInt32();
                WeeklyMemoryMaterialKind kind = (WeeklyMemoryMaterialKind)reader.ReadInt32();
                string storedLabel = reader.ReadString();
                long valueDenars = reader.ReadInt64();
                string quantityToken = reader.ReadString();
                var atom = new WeeklyMemoryMaterialAtom(
                    intentIndex,
                    kind,
                    valueDenars,
                    quantityToken);
                if (!string.Equals(storedLabel, atom.Label, StringComparison.Ordinal))
                {
                    errorCode = "weekly_material_atom_label_invalid";
                    return false;
                }
                atoms.Add(atom);
            }
            long estimatedValueDenars = reader.ReadInt64();
            string reason = reader.ReadString();
            if (!WeeklyMemoryMaterialFrozenPayload.TryCreate(
                memoryId,
                npcName,
                originGameDate,
                footholdKingdomId,
                footholdSettlementId,
                atoms,
                estimatedValueDenars,
                reason,
                out WeeklyMemoryMaterialFrozenPayload payload,
                out errorCode))
            {
                return false;
            }
            var candidate = new WeeklyMemoryMaterialOutcomeReceipt
            {
                SchemaVersion = schemaVersion,
                FingerprintVersion = fingerprintVersion,
                State = state,
                ReceiptId = receiptId,
                RequestFingerprint = requestFingerprint,
                TraceId = traceId,
                Channel = channel,
                SessionId = sessionId,
                SubjectId = subjectId,
                RuntimeGeneration = runtimeGeneration,
                SaveGeneration = saveGeneration,
                CourierDirection = courierDirection,
                OriginGameDay = originGameDay,
                OriginGameHour = originGameHour,
                LocationId = locationId,
                SceneSessionId = sceneSessionId,
                DialogueSessionId = dialogueSessionId,
                TargetAgentIndex = targetAgentIndex,
                TurnFingerprint = turnFingerprint,
                ActionFingerprint = actionFingerprint,
                CandidateHash = candidateHash,
                Payload = payload,
                PayloadHash = reader.ReadString(),
                CreatedUtcTicks = reader.ReadInt64(),
                ConfirmedUtcTicks = reader.ReadInt64(),
                TerminalUtcTicks = reader.ReadInt64(),
                AppliedUtcTicks = reader.ReadInt64(),
                DiagnosticCode = reader.ReadString()
            };
            if (stream.Position != stream.Length || !candidate.TryValidate(out errorCode))
            {
                return false;
            }
            receipt = candidate;
            return true;
        }
    }

    private bool TryValidate(out string errorCode)
    {
        errorCode = string.Empty;
        if (SchemaVersion != CurrentSchemaVersion
            || FingerprintVersion != WeeklyMemoryMaterialFingerprintHelper.CurrentVersion
            || !Enum.IsDefined(typeof(WeeklyMemoryMaterialOutcomeState), State)
            || !WeeklyMemoryMaterialDataGuard.IsHexDigest(ReceiptId)
            || !WeeklyMemoryMaterialDataGuard.IsHexDigest(RequestFingerprint)
            || !WeeklyMemoryMaterialDataGuard.IsHexDigest(TurnFingerprint)
            || !WeeklyMemoryMaterialDataGuard.IsHexDigest(ActionFingerprint)
            || !WeeklyMemoryMaterialDataGuard.IsHexDigest(CandidateHash)
            || !WeeklyMemoryMaterialDataGuard.IsHexDigest(PayloadHash)
            || !WeeklyMemoryMaterialFingerprintHelper.TryValidateTurnIdentity(
                TraceId,
                Channel,
                SessionId,
                SubjectId,
                RuntimeGeneration,
                SaveGeneration,
                CourierDirection,
                OriginGameDay,
                OriginGameHour,
                LocationId,
                SceneSessionId,
                DialogueSessionId,
                TargetAgentIndex,
                out errorCode)
            || Payload == null
            || !Payload.TryValidate(out errorCode)
            || CreatedUtcTicks <= 0L
            || ConfirmedUtcTicks < 0L
            || TerminalUtcTicks < 0L
            || AppliedUtcTicks < 0L
            || DiagnosticCode == null
            || DiagnosticCode.Length > MaximumDiagnosticLength)
        {
            if (string.IsNullOrWhiteSpace(errorCode))
            {
                errorCode = "weekly_material_receipt_invalid";
            }
            return false;
        }

        string expectedReceiptId = WeeklyMemoryMaterialFingerprintHelper.BuildReceiptId(RequestFingerprint);
        string expectedTurn = WeeklyMemoryMaterialFingerprintHelper.BuildTurnFingerprint(
            TraceId,
            Channel,
            SessionId,
            SubjectId,
            RuntimeGeneration,
            SaveGeneration,
            CourierDirection,
            OriginGameDay,
            OriginGameHour,
            LocationId,
            SceneSessionId,
            DialogueSessionId,
            TargetAgentIndex);
        string expectedCandidateHash = WeeklyMemoryMaterialFingerprintHelper.BuildCandidateHash(
            FingerprintVersion,
            ReceiptId,
            RequestFingerprint,
            TraceId,
            Channel,
            SessionId,
            SubjectId,
            RuntimeGeneration,
            SaveGeneration,
            CourierDirection,
            OriginGameDay,
            OriginGameHour,
            LocationId,
            SceneSessionId,
            DialogueSessionId,
            TargetAgentIndex,
            TurnFingerprint,
            ActionFingerprint);
        if (!string.Equals(expectedReceiptId, ReceiptId, StringComparison.Ordinal)
            || !string.Equals(expectedTurn, TurnFingerprint, StringComparison.Ordinal)
            || !string.Equals(expectedCandidateHash, CandidateHash, StringComparison.Ordinal)
            || !string.Equals(Payload.ComputeHash(), PayloadHash, StringComparison.Ordinal))
        {
            errorCode = "weekly_material_receipt_hash_mismatch";
            return false;
        }

        bool stateValid;
        switch (State)
        {
            case WeeklyMemoryMaterialOutcomeState.Prepared:
                stateValid = ConfirmedUtcTicks == 0L
                    && TerminalUtcTicks == 0L
                    && AppliedUtcTicks == 0L
                    && string.IsNullOrEmpty(DiagnosticCode);
                break;
            case WeeklyMemoryMaterialOutcomeState.Confirmed:
                stateValid = ConfirmedUtcTicks >= CreatedUtcTicks
                    && TerminalUtcTicks == 0L
                    && AppliedUtcTicks == 0L
                    && string.IsNullOrEmpty(DiagnosticCode);
                break;
            case WeeklyMemoryMaterialOutcomeState.Applied:
                stateValid = ConfirmedUtcTicks >= CreatedUtcTicks
                    && AppliedUtcTicks >= ConfirmedUtcTicks
                    && TerminalUtcTicks == AppliedUtcTicks
                    && string.IsNullOrEmpty(DiagnosticCode);
                break;
            case WeeklyMemoryMaterialOutcomeState.Rejected:
            case WeeklyMemoryMaterialOutcomeState.Partial:
            case WeeklyMemoryMaterialOutcomeState.Unknown:
            case WeeklyMemoryMaterialOutcomeState.Quarantined:
                stateValid = TerminalUtcTicks >= CreatedUtcTicks
                    && (ConfirmedUtcTicks == 0L || TerminalUtcTicks >= ConfirmedUtcTicks)
                    && AppliedUtcTicks == 0L;
                break;
            default:
                stateValid = false;
                break;
        }
        if (!stateValid)
        {
            errorCode = "weekly_material_receipt_state_invalid";
        }
        return stateValid;
    }

    private static byte[] ComputeChecksum(byte[] body)
    {
        using (SHA256 sha = SHA256.Create())
        {
            return sha.ComputeHash(body ?? Array.Empty<byte>());
        }
    }

    private static bool FixedTimeEquals(byte[] left, byte[] right)
    {
        if (left == null || right == null || left.Length != right.Length)
        {
            return false;
        }
        int difference = 0;
        for (int index = 0; index < left.Length; index++)
        {
            difference |= left[index] ^ right[index];
        }
        return difference == 0;
    }
}

/// <summary>
/// Bounded data-only lifecycle store. Prepared and Confirmed entries count as
/// pending authority and are never evicted. Only terminal tombstones are
/// trimmed, oldest first. A loaded Prepared record becomes terminal Unknown;
/// only Confirmed may be offered again for an idempotent data-only attach.
/// </summary>
internal sealed class WeeklyMemoryMaterialOutcomeLedger
{
    internal const int MaximumPendingEntries = 64;
    internal const int MaximumTerminalEntries = 512;

    private readonly Dictionary<string, WeeklyMemoryMaterialOutcomeReceipt> _entries =
        new Dictionary<string, WeeklyMemoryMaterialOutcomeReceipt>(StringComparer.Ordinal);

    internal int PendingCount => _entries.Values.Count(IsPending);
    internal int TerminalCount => _entries.Values.Count(entry => !IsPending(entry));

    internal WeeklyMemoryMaterialOutcomeOperationStatus ProbeExistingCandidate(
        WeeklyMemoryMaterialOutcomeCandidate candidate,
        out string errorCode)
    {
        errorCode = string.Empty;
        if (candidate == null || !candidate.TryValidate(out errorCode))
        {
            errorCode = string.IsNullOrWhiteSpace(errorCode)
                ? "weekly_material_candidate_invalid"
                : errorCode;
            return WeeklyMemoryMaterialOutcomeOperationStatus.Rejected;
        }
        if (!TryResolve(
            candidate.ReceiptId,
            candidate.CandidateHash,
            DateTime.UtcNow.Ticks,
            out _,
            out WeeklyMemoryMaterialOutcomeOperationStatus resolution,
            out errorCode))
        {
            return resolution;
        }
        errorCode = string.Empty;
        return WeeklyMemoryMaterialOutcomeOperationStatus.Duplicate;
    }

    internal WeeklyMemoryMaterialOutcomeOperationStatus Prepare(
        WeeklyMemoryMaterialOutcomeCandidate candidate,
        WeeklyMemoryMaterialFrozenPayload payload,
        out string errorCode)
        => Prepare(candidate, payload, DateTime.UtcNow.Ticks, out errorCode);

    internal WeeklyMemoryMaterialOutcomeOperationStatus Prepare(
        WeeklyMemoryMaterialOutcomeCandidate candidate,
        WeeklyMemoryMaterialFrozenPayload payload,
        long utcTicks,
        out string errorCode)
    {
        if (!WeeklyMemoryMaterialOutcomeReceipt.TryCreate(
            candidate,
            payload,
            utcTicks,
            out WeeklyMemoryMaterialOutcomeReceipt prepared,
            out errorCode))
        {
            return WeeklyMemoryMaterialOutcomeOperationStatus.Rejected;
        }
        if (_entries.TryGetValue(prepared.ReceiptId, out WeeklyMemoryMaterialOutcomeReceipt existing))
        {
            if (string.Equals(existing.CandidateHash, prepared.CandidateHash, StringComparison.Ordinal)
                && string.Equals(existing.PayloadHash, prepared.PayloadHash, StringComparison.Ordinal))
            {
                errorCode = string.Empty;
                return WeeklyMemoryMaterialOutcomeOperationStatus.Duplicate;
            }
            existing.Quarantine("weekly_material_prepare_conflict", utcTicks);
            TrimTerminalEntries();
            errorCode = "weekly_material_prepare_conflict";
            return WeeklyMemoryMaterialOutcomeOperationStatus.Conflict;
        }
        if (PendingCount >= MaximumPendingEntries)
        {
            errorCode = "weekly_material_pending_capacity_exceeded";
            return WeeklyMemoryMaterialOutcomeOperationStatus.CapacityExceeded;
        }
        _entries.Add(prepared.ReceiptId, prepared);
        errorCode = string.Empty;
        return WeeklyMemoryMaterialOutcomeOperationStatus.Accepted;
    }

    internal WeeklyMemoryMaterialOutcomeOperationStatus Complete(
        string receiptId,
        string candidateHash,
        WeeklyMemoryMaterialOutcomeState state,
        string diagnosticCode,
        out string errorCode)
        => Complete(
            receiptId,
            candidateHash,
            state,
            diagnosticCode,
            DateTime.UtcNow.Ticks,
            out errorCode);

    internal WeeklyMemoryMaterialOutcomeOperationStatus Complete(
        string receiptId,
        string candidateHash,
        WeeklyMemoryMaterialOutcomeState state,
        string diagnosticCode,
        long utcTicks,
        out string errorCode)
    {
        if (state != WeeklyMemoryMaterialOutcomeState.Confirmed
            && state != WeeklyMemoryMaterialOutcomeState.Rejected
            && state != WeeklyMemoryMaterialOutcomeState.Partial
            && state != WeeklyMemoryMaterialOutcomeState.Unknown
            && state != WeeklyMemoryMaterialOutcomeState.Quarantined)
        {
            errorCode = "weekly_material_completion_state_invalid";
            return WeeklyMemoryMaterialOutcomeOperationStatus.Rejected;
        }
        if (!TryResolve(
            receiptId,
            candidateHash,
            utcTicks,
            out WeeklyMemoryMaterialOutcomeReceipt receipt,
            out WeeklyMemoryMaterialOutcomeOperationStatus resolution,
            out errorCode))
        {
            return resolution;
        }
        WeeklyMemoryMaterialOutcomeOperationStatus status = receipt.Transition(
            state,
            diagnosticCode,
            utcTicks,
            out errorCode);
        if (status == WeeklyMemoryMaterialOutcomeOperationStatus.Conflict)
        {
            receipt.Quarantine("weekly_material_completion_conflict", utcTicks);
        }
        TrimTerminalEntries();
        return status;
    }

    internal WeeklyMemoryMaterialOutcomeOperationStatus GetPublishWork(
        string receiptId,
        string candidateHash,
        out WeeklyMemoryMaterialOutcomeReceipt receipt,
        out string errorCode)
    {
        if (!TryResolve(
            receiptId,
            candidateHash,
            DateTime.UtcNow.Ticks,
            out receipt,
            out WeeklyMemoryMaterialOutcomeOperationStatus resolution,
            out errorCode))
        {
            return resolution;
        }
        if (receipt.State == WeeklyMemoryMaterialOutcomeState.Confirmed)
        {
            return WeeklyMemoryMaterialOutcomeOperationStatus.Accepted;
        }
        if (receipt.State == WeeklyMemoryMaterialOutcomeState.Applied)
        {
            receipt = null;
            return WeeklyMemoryMaterialOutcomeOperationStatus.Duplicate;
        }
        receipt = null;
        errorCode = "weekly_material_receipt_not_confirmed";
        return WeeklyMemoryMaterialOutcomeOperationStatus.NotReady;
    }

    internal WeeklyMemoryMaterialOutcomeOperationStatus MarkApplied(
        string receiptId,
        string candidateHash,
        out string errorCode)
        => MarkApplied(receiptId, candidateHash, DateTime.UtcNow.Ticks, out errorCode);

    internal WeeklyMemoryMaterialOutcomeOperationStatus MarkApplied(
        string receiptId,
        string candidateHash,
        long utcTicks,
        out string errorCode)
    {
        if (!TryResolve(
            receiptId,
            candidateHash,
            utcTicks,
            out WeeklyMemoryMaterialOutcomeReceipt receipt,
            out WeeklyMemoryMaterialOutcomeOperationStatus resolution,
            out errorCode))
        {
            return resolution;
        }
        if (receipt.State == WeeklyMemoryMaterialOutcomeState.Applied)
        {
            errorCode = string.Empty;
            return WeeklyMemoryMaterialOutcomeOperationStatus.Duplicate;
        }
        if (receipt.State != WeeklyMemoryMaterialOutcomeState.Confirmed)
        {
            errorCode = "weekly_material_receipt_not_confirmed";
            return WeeklyMemoryMaterialOutcomeOperationStatus.NotReady;
        }
        WeeklyMemoryMaterialOutcomeOperationStatus status = receipt.Transition(
            WeeklyMemoryMaterialOutcomeState.Applied,
            string.Empty,
            utcTicks,
            out errorCode);
        if (status == WeeklyMemoryMaterialOutcomeOperationStatus.Conflict)
        {
            receipt.Quarantine("weekly_material_apply_conflict", utcTicks);
        }
        TrimTerminalEntries();
        return status;
    }

    internal IReadOnlyList<WeeklyMemoryMaterialOutcomeReceipt> GetEntries()
        => _entries.Values
            .OrderBy(entry => entry.CreatedUtcTicks)
            .ThenBy(entry => entry.ReceiptId, StringComparer.Ordinal)
            .ToList();

    internal Dictionary<string, string> Export()
    {
        var exported = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (WeeklyMemoryMaterialOutcomeReceipt receipt in GetEntries())
        {
            exported[receipt.ReceiptId] = receipt.Serialize();
        }
        return exported;
    }

    internal bool Import(IDictionary<string, string> storage, out string errorCode)
    {
        errorCode = string.Empty;
        if (storage == null)
        {
            _entries.Clear();
            return true;
        }
        if (storage.Count > MaximumPendingEntries + MaximumTerminalEntries)
        {
            errorCode = "weekly_material_storage_capacity_exceeded";
            return false;
        }

        var staged = new Dictionary<string, WeeklyMemoryMaterialOutcomeReceipt>(StringComparer.Ordinal);
        long loadTicks = Math.Max(1L, DateTime.UtcNow.Ticks);
        foreach (KeyValuePair<string, string> item in storage.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            if (!WeeklyMemoryMaterialOutcomeReceipt.TryDeserialize(
                item.Value,
                out WeeklyMemoryMaterialOutcomeReceipt receipt,
                out string parseError))
            {
                errorCode = parseError;
                return false;
            }

            string storageKey = WeeklyMemoryMaterialDataGuard.Normalize(item.Key);
            if (!string.Equals(storageKey, receipt.ReceiptId, StringComparison.Ordinal))
            {
                errorCode = "weekly_material_storage_key_mismatch";
                return false;
            }
            if (receipt.State == WeeklyMemoryMaterialOutcomeState.Prepared)
            {
                receipt.MarkLoadedPreparedUnknown(loadTicks);
            }
            if (staged.ContainsKey(receipt.ReceiptId))
            {
                errorCode = "weekly_material_duplicate_storage_record";
                return false;
            }
            if (IsPending(receipt)
                && staged.Values.Count(IsPending) >= MaximumPendingEntries)
            {
                errorCode = "weekly_material_confirmed_capacity_exceeded";
                return false;
            }
            staged.Add(receipt.ReceiptId, receipt);
        }

        TrimTerminalEntries(staged);
        _entries.Clear();
        foreach (KeyValuePair<string, WeeklyMemoryMaterialOutcomeReceipt> item in staged)
        {
            _entries.Add(item.Key, item.Value);
        }
        return true;
    }

    private bool TryResolve(
        string receiptId,
        string candidateHash,
        long utcTicks,
        out WeeklyMemoryMaterialOutcomeReceipt receipt,
        out WeeklyMemoryMaterialOutcomeOperationStatus status,
        out string errorCode)
    {
        receipt = null;
        string normalizedReceiptId = WeeklyMemoryMaterialDataGuard.Normalize(receiptId);
        string normalizedCandidateHash = WeeklyMemoryMaterialDataGuard.Normalize(candidateHash);
        if (!WeeklyMemoryMaterialDataGuard.IsHexDigest(normalizedReceiptId)
            || !WeeklyMemoryMaterialDataGuard.IsHexDigest(normalizedCandidateHash))
        {
            errorCode = "weekly_material_identity_invalid";
            status = WeeklyMemoryMaterialOutcomeOperationStatus.Rejected;
            return false;
        }
        if (!_entries.TryGetValue(normalizedReceiptId, out receipt))
        {
            errorCode = "weekly_material_receipt_missing";
            status = WeeklyMemoryMaterialOutcomeOperationStatus.NotFound;
            return false;
        }
        if (!string.Equals(receipt.CandidateHash, normalizedCandidateHash, StringComparison.Ordinal))
        {
            receipt.Quarantine("weekly_material_candidate_hash_conflict", utcTicks);
            TrimTerminalEntries();
            errorCode = "weekly_material_candidate_hash_conflict";
            status = WeeklyMemoryMaterialOutcomeOperationStatus.Conflict;
            receipt = null;
            return false;
        }
        errorCode = string.Empty;
        status = WeeklyMemoryMaterialOutcomeOperationStatus.Accepted;
        return true;
    }

    private void TrimTerminalEntries()
        => TrimTerminalEntries(_entries);

    private static void TrimTerminalEntries(
        IDictionary<string, WeeklyMemoryMaterialOutcomeReceipt> entries)
    {
        List<WeeklyMemoryMaterialOutcomeReceipt> terminal = entries.Values
            .Where(entry => !IsPending(entry))
            .OrderBy(entry => entry.TerminalUtcTicks)
            .ThenBy(entry => entry.CreatedUtcTicks)
            .ThenBy(entry => entry.ReceiptId, StringComparer.Ordinal)
            .ToList();
        int removeCount = Math.Max(0, terminal.Count - MaximumTerminalEntries);
        for (int index = 0; index < removeCount; index++)
        {
            entries.Remove(terminal[index].ReceiptId);
        }
    }

    private static bool IsPending(WeeklyMemoryMaterialOutcomeReceipt receipt)
        => receipt != null
            && (receipt.State == WeeklyMemoryMaterialOutcomeState.Prepared
                || receipt.State == WeeklyMemoryMaterialOutcomeState.Confirmed);
}

internal interface IWeeklyMemoryMaterialCandidateSource
{
    bool TryCreateWeeklyMaterialCandidate(
        ActionPlan actionPlan,
        GameInteractionSnapshot snapshot,
        string requestId,
        out WeeklyMemoryMaterialOutcomeCandidate candidate);
}

internal interface IWeeklyMemoryMaterialExecutionReceipt
{
    string ConfirmedWeeklyMaterialActionFingerprint { get; }
}

internal interface IWeeklyMemoryMaterialOutcomeOwner
{
    WeeklyMemoryMaterialOutcomeOperationStatus Prepare(
        WeeklyMemoryMaterialOutcomeCandidate candidate);

    WeeklyMemoryMaterialOutcomeOperationStatus Complete(
        string receiptId,
        string candidateHash,
        WeeklyMemoryMaterialOutcomeState state,
        string errorCode);

    WeeklyMemoryMaterialOutcomeOperationStatus Publish(
        string receiptId,
        string candidateHash);
}

internal static class WeeklyMemoryMaterialDataGuard
{
    internal static string Normalize(string value)
        => (value ?? string.Empty).Trim();

    internal static string NormalizeDiagnostic(string value, int maximumLength)
    {
        string normalized = Normalize(value).Replace("\r", " ").Replace("\n", " ");
        return normalized.Length <= maximumLength
            ? normalized
            : normalized.Substring(0, maximumLength);
    }

    internal static bool IsBoundedRequired(string value, int maximumLength)
        => !string.IsNullOrWhiteSpace(value)
            && value.Length <= maximumLength;

    internal static bool ContainsExecutableProtocol(string value)
    {
        string text = value ?? string.Empty;
        return text.IndexOf("[ACTION:", StringComparison.OrdinalIgnoreCase) >= 0
            || text.IndexOf("[AD:", StringComparison.OrdinalIgnoreCase) >= 0
            || text.IndexOf("[ADP:", StringComparison.OrdinalIgnoreCase) >= 0
            || text.IndexOf("[ATT:", StringComparison.OrdinalIgnoreCase) >= 0
            || text.IndexOf("[ATP:", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    internal static bool IsHexDigest(string value)
    {
        if ((value ?? string.Empty).Length != 64)
        {
            return false;
        }
        for (int index = 0; index < value.Length; index++)
        {
            char current = value[index];
            if (!((current >= '0' && current <= '9')
                || (current >= 'A' && current <= 'F')
                || (current >= 'a' && current <= 'f')))
            {
                return false;
            }
        }
        return true;
    }
}
