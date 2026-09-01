using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace AnimusForge.Refactor.Runtime;

internal enum NotorietyConversationOutcomeState
{
    Open = 1,
    Confirmed = 2,
    Applied = 3,
    Unknown = 4,
    Rejected = 5,
    Quarantined = 6
}

internal enum NotorietyConversationOutcomeOperationStatus
{
    Rejected = 0,
    Accepted = 1,
    Duplicate = 2,
    Conflict = 3,
    CapacityExceeded = 4,
    NotFound = 5,
    NotReady = 6
}

/// <summary>
/// Transient identity for one exact notoriety conversation. The raw memory
/// session key is used only to produce an opaque digest and is never retained.
/// </summary>
internal sealed class NotorietyConversationOutcomeCandidate
{
    internal const int CurrentFingerprintVersion = 1;

    private NotorietyConversationOutcomeCandidate()
    {
    }

    internal int FingerprintVersion { get; private set; }
    internal string ReceiptId { get; private set; } = string.Empty;
    internal string CandidateHash { get; private set; } = string.Empty;
    internal string SubjectId { get; private set; } = string.Empty;
    internal string MemorySessionKeyHash { get; private set; } = string.Empty;
    internal string RuntimeId { get; private set; } = string.Empty;
    internal string SaveId { get; private set; } = string.Empty;
    internal int StartDay { get; private set; }
    internal int StartHour { get; private set; }
    internal int KnownRollChance { get; private set; }
    internal bool KnowsMajorThisSession { get; private set; }

    internal static bool TryCreate(
        string subjectId,
        string memorySessionKey,
        string runtimeId,
        string saveId,
        int startDay,
        int startHour,
        int knownRollChance,
        bool knowsMajorThisSession,
        out NotorietyConversationOutcomeCandidate candidate,
        out string errorCode)
    {
        candidate = null;
        if (!NotorietyConversationLineFingerprintHelper.TryBuildSessionIdentity(
            subjectId,
            memorySessionKey,
            runtimeId,
            saveId,
            startDay,
            startHour,
            knownRollChance,
            knowsMajorThisSession,
            out string receiptId,
            out string memorySessionKeyHash,
            out errorCode))
        {
            return false;
        }

        var created = new NotorietyConversationOutcomeCandidate
        {
            FingerprintVersion = CurrentFingerprintVersion,
            ReceiptId = receiptId,
            SubjectId = NotorietyConversationDataGuard.Normalize(subjectId),
            MemorySessionKeyHash = memorySessionKeyHash,
            RuntimeId = NotorietyConversationDataGuard.Normalize(runtimeId),
            SaveId = NotorietyConversationDataGuard.Normalize(saveId),
            StartDay = startDay,
            StartHour = startHour,
            KnownRollChance = knownRollChance,
            KnowsMajorThisSession = knowsMajorThisSession
        };
        created.CandidateHash = NotorietyConversationLineFingerprintHelper.BuildCandidateHash(
            created.FingerprintVersion,
            created.ReceiptId,
            created.SubjectId,
            created.MemorySessionKeyHash,
            created.RuntimeId,
            created.SaveId,
            created.StartDay,
            created.StartHour,
            created.KnownRollChance,
            created.KnowsMajorThisSession);
        if (!created.TryValidate(out errorCode))
        {
            return false;
        }
        candidate = created;
        return true;
    }

    internal bool TryValidate(out string errorCode)
    {
        errorCode = string.Empty;
        if (FingerprintVersion != CurrentFingerprintVersion
            || !NotorietyConversationDataGuard.IsBoundedRequired(
                SubjectId,
                NotorietyConversationLineFingerprintHelper.MaximumIdentityLength)
            || !NotorietyConversationDataGuard.IsHexDigest(MemorySessionKeyHash)
            || !NotorietyConversationDataGuard.IsBoundedRequired(
                RuntimeId,
                NotorietyConversationLineFingerprintHelper.MaximumIdentityLength)
            || !NotorietyConversationDataGuard.IsBoundedRequired(
                SaveId,
                NotorietyConversationLineFingerprintHelper.MaximumIdentityLength)
            || StartDay < 0
            || StartHour < 0
            || StartHour > 23
            || KnownRollChance < 0
            || KnownRollChance > 100
            || !NotorietyConversationDataGuard.IsHexDigest(ReceiptId)
            || !NotorietyConversationDataGuard.IsHexDigest(CandidateHash))
        {
            errorCode = "notoriety_conversation_candidate_invalid";
            return false;
        }

        string expectedReceiptId = NotorietyConversationLineFingerprintHelper.BuildReceiptIdFromHash(
            FingerprintVersion,
            SubjectId,
            MemorySessionKeyHash,
            RuntimeId,
            SaveId,
            StartDay,
            StartHour);
        string expectedCandidateHash = NotorietyConversationLineFingerprintHelper.BuildCandidateHash(
            FingerprintVersion,
            ReceiptId,
            SubjectId,
            MemorySessionKeyHash,
            RuntimeId,
            SaveId,
            StartDay,
            StartHour,
            KnownRollChance,
            KnowsMajorThisSession);
        if (!string.Equals(ReceiptId, expectedReceiptId, StringComparison.Ordinal)
            || !string.Equals(CandidateHash, expectedCandidateHash, StringComparison.Ordinal))
        {
            errorCode = "notoriety_conversation_candidate_hash_mismatch";
            return false;
        }
        return true;
    }
}

/// <summary>
/// Produces exact session and line identities without retaining raw dialogue or
/// a raw memory session key. Line identity binds the H recovery witness.
/// </summary>
internal static class NotorietyConversationLineFingerprintHelper
{
    internal const int MaximumIdentityLength = 256;
    internal const int DigestLength = 64;

    internal static bool TryBuildSessionIdentity(
        string subjectId,
        string memorySessionKey,
        string runtimeId,
        string saveId,
        int startDay,
        int startHour,
        int knownRollChance,
        bool knowsMajorThisSession,
        out string receiptId,
        out string memorySessionKeyHash,
        out string errorCode)
    {
        if (knownRollChance < 0 || knownRollChance > 100)
        {
            receiptId = string.Empty;
            memorySessionKeyHash = string.Empty;
            errorCode = "notoriety_conversation_session_identity_invalid";
            return false;
        }
        return TryBuildSessionIdentity(
            subjectId,
            memorySessionKey,
            runtimeId,
            saveId,
            startDay,
            startHour,
            out receiptId,
            out memorySessionKeyHash,
            out errorCode);
    }

    internal static bool TryBuildSessionIdentity(
        string subjectId,
        string memorySessionKey,
        string runtimeId,
        string saveId,
        int startDay,
        int startHour,
        out string receiptId,
        out string memorySessionKeyHash,
        out string errorCode)
    {
        receiptId = string.Empty;
        memorySessionKeyHash = string.Empty;
        errorCode = string.Empty;
        string normalizedSubjectId = NotorietyConversationDataGuard.Normalize(subjectId);
        string normalizedSessionKey = NotorietyConversationDataGuard.Normalize(memorySessionKey);
        string normalizedRuntimeId = NotorietyConversationDataGuard.Normalize(runtimeId);
        string normalizedSaveId = NotorietyConversationDataGuard.Normalize(saveId);
        if (!NotorietyConversationDataGuard.IsBoundedRequired(normalizedSubjectId, MaximumIdentityLength)
            || !NotorietyConversationDataGuard.IsBoundedRequired(normalizedSessionKey, MaximumIdentityLength)
            || !NotorietyConversationDataGuard.IsBoundedRequired(normalizedRuntimeId, MaximumIdentityLength)
            || !NotorietyConversationDataGuard.IsBoundedRequired(normalizedSaveId, MaximumIdentityLength)
            || startDay < 0
            || startHour < 0
            || startHour > 23)
        {
            errorCode = "notoriety_conversation_session_identity_invalid";
            return false;
        }

        memorySessionKeyHash = Hash(writer =>
        {
            writer.Write("AnimusForge.NotorietyConversation.MemorySession.v1");
            writer.Write(NotorietyConversationOutcomeCandidate.CurrentFingerprintVersion);
            writer.Write(normalizedSessionKey);
        });
        receiptId = BuildReceiptIdFromHash(
            NotorietyConversationOutcomeCandidate.CurrentFingerprintVersion,
            normalizedSubjectId,
            memorySessionKeyHash,
            normalizedRuntimeId,
            normalizedSaveId,
            startDay,
            startHour);
        return true;
    }

    internal static bool TryBuildLineId(
        string recoveryId,
        string payloadHash,
        string part,
        int day,
        int hour,
        out string lineId,
        out string errorCode)
    {
        lineId = string.Empty;
        errorCode = string.Empty;
        string normalizedRecoveryId = NotorietyConversationDataGuard.Normalize(recoveryId);
        string normalizedPayloadHash = NotorietyConversationDataGuard.Normalize(payloadHash);
        string normalizedPart = NotorietyConversationDataGuard.Normalize(part).ToLowerInvariant();
        if (!NotorietyConversationDataGuard.IsHexDigest(normalizedRecoveryId)
            || !NotorietyConversationDataGuard.IsHexDigest(normalizedPayloadHash)
            || (normalizedPart != "user" && normalizedPart != "assistant")
            || day < 0
            || hour < 0
            || hour > 23)
        {
            errorCode = "notoriety_conversation_line_identity_invalid";
            return false;
        }
        lineId = Hash(writer =>
        {
            writer.Write("AnimusForge.NotorietyConversation.Line.v1");
            writer.Write(NotorietyConversationOutcomeCandidate.CurrentFingerprintVersion);
            writer.Write(normalizedRecoveryId);
            writer.Write(normalizedPayloadHash);
            writer.Write(normalizedPart);
            writer.Write(day);
            writer.Write(hour);
        });
        return true;
    }

    internal static string BuildReceiptIdFromHash(
        int fingerprintVersion,
        string subjectId,
        string memorySessionKeyHash,
        string runtimeId,
        string saveId,
        int startDay,
        int startHour)
        => Hash(writer =>
        {
            writer.Write("AnimusForge.NotorietyConversation.Receipt.v1");
            writer.Write(fingerprintVersion);
            writer.Write(subjectId ?? string.Empty);
            writer.Write(memorySessionKeyHash ?? string.Empty);
            writer.Write(runtimeId ?? string.Empty);
            writer.Write(saveId ?? string.Empty);
        });

    internal static string BuildCandidateHash(
        int fingerprintVersion,
        string receiptId,
        string subjectId,
        string memorySessionKeyHash,
        string runtimeId,
        string saveId,
        int startDay,
        int startHour,
        int knownRollChance,
        bool knowsMajorThisSession)
        => Hash(writer =>
        {
            writer.Write("AnimusForge.NotorietyConversation.Candidate.v1");
            writer.Write(fingerprintVersion);
            writer.Write(receiptId ?? string.Empty);
            writer.Write(subjectId ?? string.Empty);
            writer.Write(memorySessionKeyHash ?? string.Empty);
            writer.Write(runtimeId ?? string.Empty);
            writer.Write(saveId ?? string.Empty);
            writer.Write(startDay);
            writer.Write(startHour);
            writer.Write(knownRollChance);
            writer.Write(knowsMajorThisSession);
        });

    internal static string Hash(Action<BinaryWriter> write)
    {
        using (var stream = new MemoryStream())
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
        {
            write(writer);
            writer.Flush();
            using (SHA256 sha = SHA256.Create())
            {
                return ToHex(sha.ComputeHash(stream.ToArray()));
            }
        }
    }

    private static string ToHex(byte[] bytes)
    {
        var builder = new StringBuilder((bytes?.Length ?? 0) * 2);
        foreach (byte value in bytes ?? Array.Empty<byte>())
        {
            builder.Append(value.ToString("X2"));
        }
        return builder.ToString();
    }
}

/// <summary>
/// Absolute aggregate target used for idempotent readback and retry. No live
/// state accessor, delegate, action or game object is retained.
/// </summary>
internal sealed class NotorietyConversationFinalizeTarget
{
    private NotorietyConversationFinalizeTarget()
    {
    }

    internal bool Known { get; private set; }
    internal int KnownDay { get; private set; }
    internal double Bonus { get; private set; }
    internal int CompletedSessions { get; private set; }
    internal int LastDay { get; private set; }
    internal string TargetHash { get; private set; } = string.Empty;

    internal static bool TryCreate(
        bool known,
        int knownDay,
        double bonus,
        int completedSessions,
        int lastDay,
        out NotorietyConversationFinalizeTarget target,
        out string errorCode)
    {
        target = new NotorietyConversationFinalizeTarget
        {
            Known = known,
            KnownDay = knownDay,
            Bonus = bonus,
            CompletedSessions = completedSessions,
            LastDay = lastDay
        };
        target.TargetHash = ComputeHash(
            target.Known,
            target.KnownDay,
            target.Bonus,
            target.CompletedSessions,
            target.LastDay);
        if (!target.TryValidate(out errorCode))
        {
            target = null;
            return false;
        }
        return true;
    }

    internal bool TryValidate(out string errorCode)
    {
        errorCode = string.Empty;
        if ((Known && KnownDay < 0)
            || (!Known && KnownDay != -1)
            || double.IsNaN(Bonus)
            || double.IsInfinity(Bonus)
            || Bonus < 0.0
            || Bonus > 100.0
            || CompletedSessions < 0
            || LastDay < -1
            || !NotorietyConversationDataGuard.IsHexDigest(TargetHash)
            || !string.Equals(
                TargetHash,
                ComputeHash(Known, KnownDay, Bonus, CompletedSessions, LastDay),
                StringComparison.Ordinal))
        {
            errorCode = "notoriety_conversation_finalize_target_invalid";
            return false;
        }
        return true;
    }

    internal NotorietyConversationFinalizeTarget Clone()
        => new NotorietyConversationFinalizeTarget
        {
            Known = Known,
            KnownDay = KnownDay,
            Bonus = Bonus,
            CompletedSessions = CompletedSessions,
            LastDay = LastDay,
            TargetHash = TargetHash
        };

    private static string ComputeHash(
        bool known,
        int knownDay,
        double bonus,
        int completedSessions,
        int lastDay)
        => NotorietyConversationLineFingerprintHelper.Hash(writer =>
        {
            writer.Write("AnimusForge.NotorietyConversation.FinalizeTarget.v1");
            writer.Write(NotorietyConversationOutcomeCandidate.CurrentFingerprintVersion);
            writer.Write(known);
            writer.Write(knownDay);
            writer.Write(bonus);
            writer.Write(completedSessions);
            writer.Write(lastDay);
        });
}

/// <summary>
/// Persistent data-only receipt. It contains opaque hashes and bounded scalar
/// facts only; raw session keys, dialogue text and TaleWorlds objects are absent.
/// </summary>
internal sealed class NotorietyConversationOutcomeReceipt
{
    internal const int CurrentSchemaVersion = 1;
    internal const int MaximumLineCount = 260;
    internal const int MaximumSerializedLength = 131072;
    private const int MaximumBodyLength = 98304;
    private const int MaximumDiagnosticLength = 512;
    private const string WirePrefix = "AFNR1:";

    private NotorietyConversationOutcomeReceipt()
    {
    }

    internal int SchemaVersion { get; private set; }
    internal int FingerprintVersion { get; private set; }
    internal NotorietyConversationOutcomeState State { get; private set; }
    internal string ReceiptId { get; private set; } = string.Empty;
    internal string CandidateHash { get; private set; } = string.Empty;
    internal string SubjectId { get; private set; } = string.Empty;
    internal string MemorySessionKeyHash { get; private set; } = string.Empty;
    internal string RuntimeId { get; private set; } = string.Empty;
    internal string SaveId { get; private set; } = string.Empty;
    internal int StartDay { get; private set; }
    internal int StartHour { get; private set; }
    internal int LastDay { get; private set; }
    internal int LastHour { get; private set; }
    internal int KnownRollChance { get; private set; }
    internal bool KnowsMajorThisSession { get; private set; }
    internal IReadOnlyList<string> LineIds => _lineIds.AsReadOnly();
    internal NotorietyConversationFinalizeTarget FinalizeTarget { get; private set; }
    internal string FinalizeTargetHash => FinalizeTarget?.TargetHash ?? string.Empty;
    internal long CreatedUtcTicks { get; private set; }
    internal long UpdatedUtcTicks { get; private set; }
    internal long ConfirmedUtcTicks { get; private set; }
    internal long TerminalUtcTicks { get; private set; }
    internal long AppliedUtcTicks { get; private set; }
    internal string DiagnosticCode { get; private set; } = string.Empty;

    private readonly List<string> _lineIds = new List<string>();

    internal static bool TryCreate(
        NotorietyConversationOutcomeCandidate candidate,
        long createdUtcTicks,
        out NotorietyConversationOutcomeReceipt receipt,
        out string errorCode)
    {
        receipt = null;
        errorCode = string.Empty;
        if (candidate == null || !candidate.TryValidate(out errorCode))
        {
            errorCode = string.IsNullOrWhiteSpace(errorCode)
                ? "notoriety_conversation_candidate_invalid"
                : errorCode;
            return false;
        }
        if (createdUtcTicks <= 0L)
        {
            errorCode = "notoriety_conversation_created_time_invalid";
            return false;
        }
        var created = new NotorietyConversationOutcomeReceipt
        {
            SchemaVersion = CurrentSchemaVersion,
            FingerprintVersion = candidate.FingerprintVersion,
            State = NotorietyConversationOutcomeState.Open,
            ReceiptId = candidate.ReceiptId,
            CandidateHash = candidate.CandidateHash,
            SubjectId = candidate.SubjectId,
            MemorySessionKeyHash = candidate.MemorySessionKeyHash,
            RuntimeId = candidate.RuntimeId,
            SaveId = candidate.SaveId,
            StartDay = candidate.StartDay,
            StartHour = candidate.StartHour,
            LastDay = candidate.StartDay,
            LastHour = candidate.StartHour,
            KnownRollChance = candidate.KnownRollChance,
            KnowsMajorThisSession = candidate.KnowsMajorThisSession,
            CreatedUtcTicks = createdUtcTicks,
            UpdatedUtcTicks = createdUtcTicks
        };
        if (!created.TryValidate(out errorCode))
        {
            return false;
        }
        receipt = created;
        return true;
    }

    internal NotorietyConversationOutcomeOperationStatus AddLine(
        string lineId,
        int day,
        int hour,
        long utcTicks,
        out string errorCode)
    {
        errorCode = string.Empty;
        string normalizedLineId = NotorietyConversationDataGuard.Normalize(lineId);
        if (!NotorietyConversationDataGuard.IsHexDigest(normalizedLineId)
            || day < 0
            || hour < 0
            || hour > 23
            || utcTicks <= 0L)
        {
            errorCode = "notoriety_conversation_line_invalid";
            return NotorietyConversationOutcomeOperationStatus.Rejected;
        }
        if (_lineIds.Contains(normalizedLineId))
        {
            return NotorietyConversationOutcomeOperationStatus.Duplicate;
        }
        if (State != NotorietyConversationOutcomeState.Open)
        {
            errorCode = "notoriety_conversation_receipt_not_open";
            return NotorietyConversationOutcomeOperationStatus.NotReady;
        }
        if (_lineIds.Count >= MaximumLineCount)
        {
            errorCode = "notoriety_conversation_line_capacity_exceeded";
            return NotorietyConversationOutcomeOperationStatus.CapacityExceeded;
        }

        ClampGameClock(StartDay, StartHour, day, hour, out int clampedDay, out int clampedHour);
        _lineIds.Add(normalizedLineId);
        if (clampedDay > LastDay || clampedDay == LastDay && clampedHour >= LastHour)
        {
            LastDay = clampedDay;
            LastHour = clampedHour;
        }
        UpdatedUtcTicks = ClampUtcTicks(utcTicks);
        return NotorietyConversationOutcomeOperationStatus.Accepted;
    }

    internal NotorietyConversationOutcomeOperationStatus Confirm(
        NotorietyConversationFinalizeTarget target,
        long utcTicks,
        out string errorCode)
    {
        errorCode = string.Empty;
        if (target == null || !target.TryValidate(out errorCode) || utcTicks <= 0L)
        {
            errorCode = string.IsNullOrWhiteSpace(errorCode)
                ? "notoriety_conversation_confirm_invalid"
                : errorCode;
            return NotorietyConversationOutcomeOperationStatus.Rejected;
        }
        if (State == NotorietyConversationOutcomeState.Confirmed
            || State == NotorietyConversationOutcomeState.Applied)
        {
            if (FinalizeTarget != null
                && string.Equals(FinalizeTarget.TargetHash, target.TargetHash, StringComparison.Ordinal))
            {
                return NotorietyConversationOutcomeOperationStatus.Duplicate;
            }
            errorCode = "notoriety_conversation_finalize_target_conflict";
            return NotorietyConversationOutcomeOperationStatus.Conflict;
        }
        if (State != NotorietyConversationOutcomeState.Open)
        {
            errorCode = "notoriety_conversation_receipt_not_open";
            return NotorietyConversationOutcomeOperationStatus.NotReady;
        }
        if (_lineIds.Count <= 0)
        {
            errorCode = "notoriety_conversation_finalize_without_line";
            return NotorietyConversationOutcomeOperationStatus.NotReady;
        }

        FinalizeTarget = target.Clone();
        State = NotorietyConversationOutcomeState.Confirmed;
        ConfirmedUtcTicks = ClampUtcTicks(utcTicks);
        UpdatedUtcTicks = ConfirmedUtcTicks;
        DiagnosticCode = string.Empty;
        return NotorietyConversationOutcomeOperationStatus.Accepted;
    }

    internal NotorietyConversationOutcomeOperationStatus MarkApplied(
        string targetHash,
        long utcTicks,
        out string errorCode)
    {
        errorCode = string.Empty;
        string normalizedTargetHash = NotorietyConversationDataGuard.Normalize(targetHash);
        if (!NotorietyConversationDataGuard.IsHexDigest(normalizedTargetHash) || utcTicks <= 0L)
        {
            errorCode = "notoriety_conversation_apply_identity_invalid";
            return NotorietyConversationOutcomeOperationStatus.Rejected;
        }
        if (FinalizeTarget == null
            || !string.Equals(FinalizeTarget.TargetHash, normalizedTargetHash, StringComparison.Ordinal))
        {
            errorCode = "notoriety_conversation_apply_target_conflict";
            return NotorietyConversationOutcomeOperationStatus.Conflict;
        }
        if (State == NotorietyConversationOutcomeState.Applied)
        {
            return NotorietyConversationOutcomeOperationStatus.Duplicate;
        }
        if (State != NotorietyConversationOutcomeState.Confirmed)
        {
            errorCode = "notoriety_conversation_receipt_not_confirmed";
            return NotorietyConversationOutcomeOperationStatus.NotReady;
        }
        State = NotorietyConversationOutcomeState.Applied;
        AppliedUtcTicks = ClampUtcTicks(utcTicks);
        TerminalUtcTicks = AppliedUtcTicks;
        UpdatedUtcTicks = AppliedUtcTicks;
        return NotorietyConversationOutcomeOperationStatus.Accepted;
    }

    internal NotorietyConversationOutcomeOperationStatus Finish(
        NotorietyConversationOutcomeState targetState,
        string diagnosticCode,
        long utcTicks,
        out string errorCode)
    {
        errorCode = string.Empty;
        if ((targetState != NotorietyConversationOutcomeState.Unknown
                && targetState != NotorietyConversationOutcomeState.Rejected
                && targetState != NotorietyConversationOutcomeState.Quarantined)
            || utcTicks <= 0L)
        {
            errorCode = "notoriety_conversation_terminal_state_invalid";
            return NotorietyConversationOutcomeOperationStatus.Rejected;
        }
        if (State == targetState)
        {
            return NotorietyConversationOutcomeOperationStatus.Duplicate;
        }
        if (State == NotorietyConversationOutcomeState.Applied
            || State == NotorietyConversationOutcomeState.Unknown
            || State == NotorietyConversationOutcomeState.Rejected
            || State == NotorietyConversationOutcomeState.Quarantined)
        {
            errorCode = "notoriety_conversation_terminal_state_conflict";
            return NotorietyConversationOutcomeOperationStatus.Conflict;
        }
        State = targetState;
        TerminalUtcTicks = ClampUtcTicks(utcTicks);
        UpdatedUtcTicks = TerminalUtcTicks;
        AppliedUtcTicks = 0L;
        DiagnosticCode = NotorietyConversationDataGuard.NormalizeDiagnostic(
            diagnosticCode,
            MaximumDiagnosticLength);
        return NotorietyConversationOutcomeOperationStatus.Accepted;
    }

    internal void Quarantine(string diagnosticCode, long utcTicks)
    {
        State = NotorietyConversationOutcomeState.Quarantined;
        TerminalUtcTicks = ClampUtcTicks(Math.Max(1L, utcTicks));
        UpdatedUtcTicks = TerminalUtcTicks;
        AppliedUtcTicks = 0L;
        DiagnosticCode = NotorietyConversationDataGuard.NormalizeDiagnostic(
            diagnosticCode,
            MaximumDiagnosticLength);
    }

    internal void MarkLoadedOpenUnknown(long utcTicks)
    {
        if (State != NotorietyConversationOutcomeState.Open)
        {
            return;
        }
        State = NotorietyConversationOutcomeState.Unknown;
        TerminalUtcTicks = ClampUtcTicks(Math.Max(1L, utcTicks));
        UpdatedUtcTicks = TerminalUtcTicks;
        DiagnosticCode = "notoriety_conversation_loaded_open_unknown";
    }

    internal bool ContainsLine(string lineId)
        => NotorietyConversationDataGuard.IsHexDigest(
                NotorietyConversationDataGuard.Normalize(lineId))
            && _lineIds.Contains(NotorietyConversationDataGuard.Normalize(lineId));

    internal NotorietyConversationOutcomeReceipt Clone()
    {
        var clone = new NotorietyConversationOutcomeReceipt
        {
            SchemaVersion = SchemaVersion,
            FingerprintVersion = FingerprintVersion,
            State = State,
            ReceiptId = ReceiptId,
            CandidateHash = CandidateHash,
            SubjectId = SubjectId,
            MemorySessionKeyHash = MemorySessionKeyHash,
            RuntimeId = RuntimeId,
            SaveId = SaveId,
            StartDay = StartDay,
            StartHour = StartHour,
            LastDay = LastDay,
            LastHour = LastHour,
            KnownRollChance = KnownRollChance,
            KnowsMajorThisSession = KnowsMajorThisSession,
            FinalizeTarget = FinalizeTarget?.Clone(),
            CreatedUtcTicks = CreatedUtcTicks,
            UpdatedUtcTicks = UpdatedUtcTicks,
            ConfirmedUtcTicks = ConfirmedUtcTicks,
            TerminalUtcTicks = TerminalUtcTicks,
            AppliedUtcTicks = AppliedUtcTicks,
            DiagnosticCode = DiagnosticCode
        };
        clone._lineIds.AddRange(_lineIds);
        return clone;
    }

    internal string Serialize()
    {
        if (!TryValidate(out string errorCode))
        {
            throw new InvalidOperationException(errorCode);
        }
        byte[] body = SerializeBody();
        if (body.Length == 0 || body.Length > MaximumBodyLength)
        {
            throw new InvalidOperationException("notoriety_conversation_body_oversize");
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
                throw new InvalidOperationException("notoriety_conversation_wire_oversize");
            }
            return wire;
        }
    }

    internal static bool TryDeserialize(
        string serialized,
        out NotorietyConversationOutcomeReceipt receipt,
        out string errorCode)
    {
        receipt = null;
        errorCode = string.Empty;
        string wire = serialized ?? string.Empty;
        if (!wire.StartsWith(WirePrefix, StringComparison.Ordinal)
            || wire.Length > MaximumSerializedLength)
        {
            errorCode = "notoriety_conversation_wire_invalid";
            return false;
        }
        try
        {
            byte[] bytes = Convert.FromBase64String(wire.Substring(WirePrefix.Length));
            using (var stream = new MemoryStream(bytes, false))
            using (var reader = new BinaryReader(stream, Encoding.UTF8, true))
            {
                int bodyLength = reader.ReadInt32();
                if (bodyLength <= 0
                    || bodyLength > MaximumBodyLength
                    || bodyLength > stream.Length - stream.Position)
                {
                    errorCode = "notoriety_conversation_body_length_invalid";
                    return false;
                }
                byte[] body = reader.ReadBytes(bodyLength);
                int checksumLength = reader.ReadInt32();
                if (checksumLength != 32 || checksumLength > stream.Length - stream.Position)
                {
                    errorCode = "notoriety_conversation_checksum_mismatch";
                    return false;
                }
                byte[] checksum = reader.ReadBytes(checksumLength);
                if (stream.Position != stream.Length
                    || !FixedTimeEquals(checksum, ComputeChecksum(body)))
                {
                    errorCode = "notoriety_conversation_checksum_mismatch";
                    return false;
                }
                return TryDeserializeBody(body, out receipt, out errorCode);
            }
        }
        catch
        {
            receipt = null;
            errorCode = "notoriety_conversation_wire_invalid";
            return false;
        }
    }

    internal bool TryValidate(out string errorCode)
    {
        errorCode = string.Empty;
        if (SchemaVersion != CurrentSchemaVersion
            || FingerprintVersion != NotorietyConversationOutcomeCandidate.CurrentFingerprintVersion
            || !Enum.IsDefined(typeof(NotorietyConversationOutcomeState), State)
            || !NotorietyConversationDataGuard.IsHexDigest(ReceiptId)
            || !NotorietyConversationDataGuard.IsHexDigest(CandidateHash)
            || !NotorietyConversationDataGuard.IsBoundedRequired(
                SubjectId,
                NotorietyConversationLineFingerprintHelper.MaximumIdentityLength)
            || !NotorietyConversationDataGuard.IsHexDigest(MemorySessionKeyHash)
            || !NotorietyConversationDataGuard.IsBoundedRequired(
                RuntimeId,
                NotorietyConversationLineFingerprintHelper.MaximumIdentityLength)
            || !NotorietyConversationDataGuard.IsBoundedRequired(
                SaveId,
                NotorietyConversationLineFingerprintHelper.MaximumIdentityLength)
            || StartDay < 0
            || StartHour < 0
            || StartHour > 23
            || LastDay < StartDay
            || LastHour < 0
            || LastHour > 23
            || LastDay == StartDay && LastHour < StartHour
            || KnownRollChance < 0
            || KnownRollChance > 100
            || _lineIds.Count > MaximumLineCount
            || _lineIds.Any(lineId => !NotorietyConversationDataGuard.IsHexDigest(lineId))
            || _lineIds.Distinct(StringComparer.Ordinal).Count() != _lineIds.Count
            || CreatedUtcTicks <= 0L
            || UpdatedUtcTicks < CreatedUtcTicks
            || DiagnosticCode == null
            || DiagnosticCode.Length > MaximumDiagnosticLength)
        {
            errorCode = "notoriety_conversation_receipt_invalid";
            return false;
        }

        string expectedReceiptId = NotorietyConversationLineFingerprintHelper.BuildReceiptIdFromHash(
            FingerprintVersion,
            SubjectId,
            MemorySessionKeyHash,
            RuntimeId,
            SaveId,
            StartDay,
            StartHour);
        string expectedCandidateHash = NotorietyConversationLineFingerprintHelper.BuildCandidateHash(
            FingerprintVersion,
            ReceiptId,
            SubjectId,
            MemorySessionKeyHash,
            RuntimeId,
            SaveId,
            StartDay,
            StartHour,
            KnownRollChance,
            KnowsMajorThisSession);
        if (!string.Equals(ReceiptId, expectedReceiptId, StringComparison.Ordinal)
            || !string.Equals(CandidateHash, expectedCandidateHash, StringComparison.Ordinal))
        {
            errorCode = "notoriety_conversation_receipt_hash_mismatch";
            return false;
        }

        bool targetValid = FinalizeTarget == null
            || FinalizeTarget.TryValidate(out errorCode);
        if (!targetValid)
        {
            return false;
        }
        bool stateValid;
        switch (State)
        {
            case NotorietyConversationOutcomeState.Open:
                stateValid = FinalizeTarget == null
                    && ConfirmedUtcTicks == 0L
                    && TerminalUtcTicks == 0L
                    && AppliedUtcTicks == 0L
                    && string.IsNullOrEmpty(DiagnosticCode);
                break;
            case NotorietyConversationOutcomeState.Confirmed:
                stateValid = FinalizeTarget != null
                    && ConfirmedUtcTicks >= CreatedUtcTicks
                    && UpdatedUtcTicks >= ConfirmedUtcTicks
                    && TerminalUtcTicks == 0L
                    && AppliedUtcTicks == 0L
                    && string.IsNullOrEmpty(DiagnosticCode);
                break;
            case NotorietyConversationOutcomeState.Applied:
                stateValid = FinalizeTarget != null
                    && ConfirmedUtcTicks >= CreatedUtcTicks
                    && AppliedUtcTicks >= ConfirmedUtcTicks
                    && TerminalUtcTicks == AppliedUtcTicks
                    && UpdatedUtcTicks >= AppliedUtcTicks
                    && string.IsNullOrEmpty(DiagnosticCode);
                break;
            case NotorietyConversationOutcomeState.Unknown:
            case NotorietyConversationOutcomeState.Rejected:
            case NotorietyConversationOutcomeState.Quarantined:
                stateValid = TerminalUtcTicks >= CreatedUtcTicks
                    && (ConfirmedUtcTicks == 0L || TerminalUtcTicks >= ConfirmedUtcTicks)
                    && UpdatedUtcTicks >= TerminalUtcTicks
                    && AppliedUtcTicks == 0L;
                break;
            default:
                stateValid = false;
                break;
        }
        if (!stateValid)
        {
            errorCode = "notoriety_conversation_receipt_state_invalid";
        }
        return stateValid;
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
            writer.Write(CandidateHash);
            writer.Write(SubjectId);
            writer.Write(MemorySessionKeyHash);
            writer.Write(RuntimeId);
            writer.Write(SaveId);
            writer.Write(StartDay);
            writer.Write(StartHour);
            writer.Write(LastDay);
            writer.Write(LastHour);
            writer.Write(KnownRollChance);
            writer.Write(KnowsMajorThisSession);
            writer.Write(_lineIds.Count);
            foreach (string lineId in _lineIds)
            {
                writer.Write(lineId);
            }
            writer.Write(FinalizeTarget != null);
            if (FinalizeTarget != null)
            {
                writer.Write(FinalizeTarget.Known);
                writer.Write(FinalizeTarget.KnownDay);
                writer.Write(FinalizeTarget.Bonus);
                writer.Write(FinalizeTarget.CompletedSessions);
                writer.Write(FinalizeTarget.LastDay);
                writer.Write(FinalizeTarget.TargetHash);
            }
            writer.Write(CreatedUtcTicks);
            writer.Write(UpdatedUtcTicks);
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
        out NotorietyConversationOutcomeReceipt receipt,
        out string errorCode)
    {
        receipt = null;
        errorCode = string.Empty;
        try
        {
            using (var stream = new MemoryStream(body ?? Array.Empty<byte>(), false))
            using (var reader = new BinaryReader(stream, Encoding.UTF8, true))
            {
                var parsed = new NotorietyConversationOutcomeReceipt
                {
                    SchemaVersion = reader.ReadInt32(),
                    FingerprintVersion = reader.ReadInt32(),
                    State = (NotorietyConversationOutcomeState)reader.ReadInt32(),
                    ReceiptId = reader.ReadString(),
                    CandidateHash = reader.ReadString(),
                    SubjectId = reader.ReadString(),
                    MemorySessionKeyHash = reader.ReadString(),
                    RuntimeId = reader.ReadString(),
                    SaveId = reader.ReadString(),
                    StartDay = reader.ReadInt32(),
                    StartHour = reader.ReadInt32(),
                    LastDay = reader.ReadInt32(),
                    LastHour = reader.ReadInt32(),
                    KnownRollChance = reader.ReadInt32(),
                    KnowsMajorThisSession = reader.ReadBoolean()
                };
                int lineCount = reader.ReadInt32();
                if (lineCount < 0 || lineCount > MaximumLineCount)
                {
                    errorCode = "notoriety_conversation_line_count_invalid";
                    return false;
                }
                for (int index = 0; index < lineCount; index++)
                {
                    parsed._lineIds.Add(reader.ReadString());
                }
                if (reader.ReadBoolean())
                {
                    if (!NotorietyConversationFinalizeTarget.TryCreate(
                        reader.ReadBoolean(),
                        reader.ReadInt32(),
                        reader.ReadDouble(),
                        reader.ReadInt32(),
                        reader.ReadInt32(),
                        out NotorietyConversationFinalizeTarget target,
                        out errorCode))
                    {
                        return false;
                    }
                    string storedHash = reader.ReadString();
                    if (!string.Equals(storedHash, target.TargetHash, StringComparison.Ordinal))
                    {
                        errorCode = "notoriety_conversation_target_hash_mismatch";
                        return false;
                    }
                    parsed.FinalizeTarget = target;
                }
                parsed.CreatedUtcTicks = reader.ReadInt64();
                parsed.UpdatedUtcTicks = reader.ReadInt64();
                parsed.ConfirmedUtcTicks = reader.ReadInt64();
                parsed.TerminalUtcTicks = reader.ReadInt64();
                parsed.AppliedUtcTicks = reader.ReadInt64();
                parsed.DiagnosticCode = reader.ReadString();
                if (stream.Position != stream.Length || !parsed.TryValidate(out errorCode))
                {
                    errorCode = string.IsNullOrWhiteSpace(errorCode)
                        ? "notoriety_conversation_body_trailing_data"
                        : errorCode;
                    return false;
                }
                receipt = parsed;
                return true;
            }
        }
        catch
        {
            receipt = null;
            errorCode = "notoriety_conversation_body_invalid";
            return false;
        }
    }

    private long ClampUtcTicks(long utcTicks)
        => Math.Max(Math.Max(CreatedUtcTicks, UpdatedUtcTicks), utcTicks);

    private static void ClampGameClock(
        int startDay,
        int startHour,
        int day,
        int hour,
        out int clampedDay,
        out int clampedHour)
    {
        if (day < startDay || day == startDay && hour < startHour)
        {
            clampedDay = startDay;
            clampedHour = startHour;
            return;
        }
        clampedDay = day;
        clampedHour = hour;
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

internal sealed class NotorietyConversationConfirmedWorkItem
{
    internal NotorietyConversationConfirmedWorkItem(NotorietyConversationOutcomeReceipt receipt)
    {
        Receipt = receipt?.Clone();
    }

    internal NotorietyConversationOutcomeReceipt Receipt { get; }
    internal string ReceiptId => Receipt?.ReceiptId ?? string.Empty;
    internal string CandidateHash => Receipt?.CandidateHash ?? string.Empty;
    internal string SubjectId => Receipt?.SubjectId ?? string.Empty;
    internal NotorietyConversationFinalizeTarget Target => Receipt?.FinalizeTarget?.Clone();
}

/// <summary>
/// Bounded data-only owner. Open and Confirmed records are pending and cannot
/// be evicted. Oldest terminal tombstones are trimmed beyond the terminal cap.
/// Import is staged and publishes only after every record validates.
/// </summary>
internal sealed class NotorietyConversationOutcomeLedger
{
    internal const int MaximumPendingEntries = 64;
    internal const int MaximumTerminalEntries = 512;

    private readonly Dictionary<string, NotorietyConversationOutcomeReceipt> _entries =
        new Dictionary<string, NotorietyConversationOutcomeReceipt>(StringComparer.Ordinal);

    internal int PendingCount => _entries.Values.Count(IsPending);
    internal int TerminalCount => _entries.Values.Count(entry => !IsPending(entry));

    internal NotorietyConversationOutcomeOperationStatus Prepare(
        NotorietyConversationOutcomeCandidate candidate,
        out NotorietyConversationOutcomeReceipt receipt,
        out string errorCode)
        => Prepare(candidate, DateTime.UtcNow.Ticks, out receipt, out errorCode);

    internal NotorietyConversationOutcomeOperationStatus Prepare(
        NotorietyConversationOutcomeCandidate candidate,
        long utcTicks,
        out NotorietyConversationOutcomeReceipt receipt,
        out string errorCode)
    {
        receipt = null;
        if (!NotorietyConversationOutcomeReceipt.TryCreate(
            candidate,
            utcTicks,
            out NotorietyConversationOutcomeReceipt created,
            out errorCode))
        {
            return NotorietyConversationOutcomeOperationStatus.Rejected;
        }
        if (_entries.TryGetValue(created.ReceiptId, out NotorietyConversationOutcomeReceipt existing))
        {
            if (string.Equals(existing.CandidateHash, created.CandidateHash, StringComparison.Ordinal))
            {
                receipt = existing.Clone();
                errorCode = string.Empty;
                return NotorietyConversationOutcomeOperationStatus.Duplicate;
            }
            existing.Quarantine("notoriety_conversation_prepare_conflict", utcTicks);
            TrimTerminalEntries();
            errorCode = "notoriety_conversation_prepare_conflict";
            return NotorietyConversationOutcomeOperationStatus.Conflict;
        }
        if (PendingCount >= MaximumPendingEntries)
        {
            errorCode = "notoriety_conversation_pending_capacity_exceeded";
            return NotorietyConversationOutcomeOperationStatus.CapacityExceeded;
        }
        _entries.Add(created.ReceiptId, created);
        receipt = created.Clone();
        return NotorietyConversationOutcomeOperationStatus.Accepted;
    }

    internal NotorietyConversationOutcomeOperationStatus ProbeLine(
        string receiptId,
        string lineId,
        string recoveryId,
        string payloadHash,
        string part,
        int day,
        int hour,
        out NotorietyConversationOutcomeReceipt receipt,
        out string errorCode)
    {
        receipt = null;
        if (!TryValidateExactLine(
            lineId,
            recoveryId,
            payloadHash,
            part,
            day,
            hour,
            out string normalizedLineId,
            out errorCode))
        {
            return NotorietyConversationOutcomeOperationStatus.Conflict;
        }
        string normalizedReceiptId = NotorietyConversationDataGuard.Normalize(receiptId);
        if (!NotorietyConversationDataGuard.IsHexDigest(normalizedReceiptId))
        {
            errorCode = "notoriety_conversation_receipt_identity_invalid";
            return NotorietyConversationOutcomeOperationStatus.Rejected;
        }
        if (!_entries.TryGetValue(normalizedReceiptId, out NotorietyConversationOutcomeReceipt existing))
        {
            errorCode = "notoriety_conversation_receipt_missing";
            return NotorietyConversationOutcomeOperationStatus.NotFound;
        }
        receipt = existing.Clone();
        if (existing.ContainsLine(normalizedLineId))
        {
            errorCode = string.Empty;
            return NotorietyConversationOutcomeOperationStatus.Duplicate;
        }
        errorCode = "notoriety_conversation_line_missing";
        return NotorietyConversationOutcomeOperationStatus.NotFound;
    }

    internal NotorietyConversationOutcomeOperationStatus AddLine(
        string receiptId,
        string candidateHash,
        string lineId,
        string recoveryId,
        string payloadHash,
        string part,
        int day,
        int hour,
        long utcTicks,
        out NotorietyConversationOutcomeReceipt receipt,
        out string errorCode)
    {
        receipt = null;
        if (!TryValidateExactLine(
            lineId,
            recoveryId,
            payloadHash,
            part,
            day,
            hour,
            out string normalizedLineId,
            out errorCode))
        {
            QuarantineIfPresent(receiptId, "notoriety_conversation_line_hash_conflict", utcTicks);
            return NotorietyConversationOutcomeOperationStatus.Conflict;
        }
        if (!TryResolve(
            receiptId,
            candidateHash,
            utcTicks,
            out NotorietyConversationOutcomeReceipt existing,
            out NotorietyConversationOutcomeOperationStatus resolution,
            out errorCode))
        {
            return resolution;
        }
        NotorietyConversationOutcomeOperationStatus status = existing.AddLine(
            normalizedLineId,
            day,
            hour,
            utcTicks,
            out errorCode);
        receipt = existing.Clone();
        return status;
    }

    internal NotorietyConversationOutcomeOperationStatus Confirm(
        string receiptId,
        string candidateHash,
        NotorietyConversationFinalizeTarget target,
        long utcTicks,
        out NotorietyConversationOutcomeReceipt receipt,
        out string errorCode)
    {
        receipt = null;
        if (!TryResolve(
            receiptId,
            candidateHash,
            utcTicks,
            out NotorietyConversationOutcomeReceipt existing,
            out NotorietyConversationOutcomeOperationStatus resolution,
            out errorCode))
        {
            return resolution;
        }
        NotorietyConversationOutcomeOperationStatus status = existing.Confirm(
            target,
            utcTicks,
            out errorCode);
        if (status == NotorietyConversationOutcomeOperationStatus.Conflict)
        {
            existing.Quarantine("notoriety_conversation_confirm_conflict", utcTicks);
            TrimTerminalEntries();
        }
        receipt = existing.Clone();
        return status;
    }

    internal bool GetConfirmedWork(out NotorietyConversationConfirmedWorkItem work)
    {
        NotorietyConversationOutcomeReceipt receipt = _entries.Values
            .Where(entry => entry.State == NotorietyConversationOutcomeState.Confirmed)
            .OrderBy(entry => entry.ConfirmedUtcTicks)
            .ThenBy(entry => entry.CreatedUtcTicks)
            .ThenBy(entry => entry.ReceiptId, StringComparer.Ordinal)
            .FirstOrDefault();
        work = receipt == null ? null : new NotorietyConversationConfirmedWorkItem(receipt);
        return work != null;
    }

    internal NotorietyConversationOutcomeOperationStatus MarkApplied(
        string receiptId,
        string candidateHash,
        string targetHash,
        long utcTicks,
        out string errorCode)
    {
        if (!TryResolve(
            receiptId,
            candidateHash,
            utcTicks,
            out NotorietyConversationOutcomeReceipt receipt,
            out NotorietyConversationOutcomeOperationStatus resolution,
            out errorCode))
        {
            return resolution;
        }
        NotorietyConversationOutcomeOperationStatus status = receipt.MarkApplied(
            targetHash,
            utcTicks,
            out errorCode);
        if (status == NotorietyConversationOutcomeOperationStatus.Conflict)
        {
            receipt.Quarantine("notoriety_conversation_apply_conflict", utcTicks);
        }
        TrimTerminalEntries();
        return status;
    }

    internal NotorietyConversationOutcomeOperationStatus Finish(
        string receiptId,
        string candidateHash,
        NotorietyConversationOutcomeState state,
        string diagnosticCode,
        long utcTicks,
        out string errorCode)
    {
        if (!TryResolve(
            receiptId,
            candidateHash,
            utcTicks,
            out NotorietyConversationOutcomeReceipt receipt,
            out NotorietyConversationOutcomeOperationStatus resolution,
            out errorCode))
        {
            return resolution;
        }
        NotorietyConversationOutcomeOperationStatus status = receipt.Finish(
            state,
            diagnosticCode,
            utcTicks,
            out errorCode);
        TrimTerminalEntries();
        return status;
    }

    internal IReadOnlyList<NotorietyConversationOutcomeReceipt> GetEntries()
        => _entries.Values
            .OrderBy(entry => entry.CreatedUtcTicks)
            .ThenBy(entry => entry.ReceiptId, StringComparer.Ordinal)
            .Select(entry => entry.Clone())
            .ToList();

    internal IReadOnlyList<NotorietyConversationOutcomeReceipt> GetOpenForSubject(string subjectId)
    {
        string normalizedSubjectId = NotorietyConversationDataGuard.Normalize(subjectId);
        return _entries.Values
            .Where(entry => entry.State == NotorietyConversationOutcomeState.Open
                && string.Equals(entry.SubjectId, normalizedSubjectId, StringComparison.Ordinal))
            .OrderBy(entry => entry.CreatedUtcTicks)
            .ThenBy(entry => entry.ReceiptId, StringComparer.Ordinal)
            .Select(entry => entry.Clone())
            .ToList();
    }

    internal Dictionary<string, string> Export()
    {
        var exported = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (NotorietyConversationOutcomeReceipt receipt in GetEntries())
        {
            exported.Add(receipt.ReceiptId, receipt.Serialize());
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
            errorCode = "notoriety_conversation_storage_capacity_exceeded";
            return false;
        }

        var staged = new Dictionary<string, NotorietyConversationOutcomeReceipt>(StringComparer.Ordinal);
        long loadTicks = Math.Max(1L, DateTime.UtcNow.Ticks);
        foreach (KeyValuePair<string, string> item in storage.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            if (!NotorietyConversationOutcomeReceipt.TryDeserialize(
                item.Value,
                out NotorietyConversationOutcomeReceipt receipt,
                out errorCode))
            {
                return false;
            }
            string storageKey = NotorietyConversationDataGuard.Normalize(item.Key);
            if (!string.Equals(storageKey, receipt.ReceiptId, StringComparison.Ordinal))
            {
                errorCode = "notoriety_conversation_storage_key_mismatch";
                return false;
            }
            if (receipt.State == NotorietyConversationOutcomeState.Open)
            {
                receipt.MarkLoadedOpenUnknown(loadTicks);
            }
            if (staged.ContainsKey(receipt.ReceiptId))
            {
                errorCode = "notoriety_conversation_duplicate_storage_record";
                return false;
            }
            if (IsPending(receipt) && staged.Values.Count(IsPending) >= MaximumPendingEntries)
            {
                errorCode = "notoriety_conversation_confirmed_capacity_exceeded";
                return false;
            }
            staged.Add(receipt.ReceiptId, receipt);
        }
        TrimTerminalEntries(staged);
        _entries.Clear();
        foreach (KeyValuePair<string, NotorietyConversationOutcomeReceipt> item in staged)
        {
            _entries.Add(item.Key, item.Value);
        }
        return true;
    }

    internal NotorietyConversationOutcomeLedger Clone()
    {
        var clone = new NotorietyConversationOutcomeLedger();
        foreach (KeyValuePair<string, NotorietyConversationOutcomeReceipt> item in _entries)
        {
            clone._entries.Add(item.Key, item.Value.Clone());
        }
        return clone;
    }

    private bool TryResolve(
        string receiptId,
        string candidateHash,
        long utcTicks,
        out NotorietyConversationOutcomeReceipt receipt,
        out NotorietyConversationOutcomeOperationStatus status,
        out string errorCode)
    {
        receipt = null;
        string normalizedReceiptId = NotorietyConversationDataGuard.Normalize(receiptId);
        string normalizedCandidateHash = NotorietyConversationDataGuard.Normalize(candidateHash);
        if (!NotorietyConversationDataGuard.IsHexDigest(normalizedReceiptId)
            || !NotorietyConversationDataGuard.IsHexDigest(normalizedCandidateHash))
        {
            errorCode = "notoriety_conversation_identity_invalid";
            status = NotorietyConversationOutcomeOperationStatus.Rejected;
            return false;
        }
        if (!_entries.TryGetValue(normalizedReceiptId, out receipt))
        {
            errorCode = "notoriety_conversation_receipt_missing";
            status = NotorietyConversationOutcomeOperationStatus.NotFound;
            return false;
        }
        if (!string.Equals(receipt.CandidateHash, normalizedCandidateHash, StringComparison.Ordinal))
        {
            receipt.Quarantine("notoriety_conversation_candidate_hash_conflict", utcTicks);
            TrimTerminalEntries();
            receipt = null;
            errorCode = "notoriety_conversation_candidate_hash_conflict";
            status = NotorietyConversationOutcomeOperationStatus.Conflict;
            return false;
        }
        errorCode = string.Empty;
        status = NotorietyConversationOutcomeOperationStatus.Accepted;
        return true;
    }

    private static bool TryValidateExactLine(
        string lineId,
        string recoveryId,
        string payloadHash,
        string part,
        int day,
        int hour,
        out string normalizedLineId,
        out string errorCode)
    {
        normalizedLineId = NotorietyConversationDataGuard.Normalize(lineId);
        if (!NotorietyConversationLineFingerprintHelper.TryBuildLineId(
            recoveryId,
            payloadHash,
            part,
            day,
            hour,
            out string expectedLineId,
            out errorCode))
        {
            return false;
        }
        if (!NotorietyConversationDataGuard.IsHexDigest(normalizedLineId)
            || !string.Equals(normalizedLineId, expectedLineId, StringComparison.Ordinal))
        {
            errorCode = "notoriety_conversation_line_hash_conflict";
            return false;
        }
        return true;
    }

    private void QuarantineIfPresent(string receiptId, string diagnosticCode, long utcTicks)
    {
        string normalizedReceiptId = NotorietyConversationDataGuard.Normalize(receiptId);
        if (_entries.TryGetValue(normalizedReceiptId, out NotorietyConversationOutcomeReceipt receipt))
        {
            receipt.Quarantine(diagnosticCode, utcTicks);
            TrimTerminalEntries();
        }
    }

    private void TrimTerminalEntries()
        => TrimTerminalEntries(_entries);

    private static void TrimTerminalEntries(
        IDictionary<string, NotorietyConversationOutcomeReceipt> entries)
    {
        List<NotorietyConversationOutcomeReceipt> terminal = entries.Values
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

    private static bool IsPending(NotorietyConversationOutcomeReceipt receipt)
        => receipt != null
            && (receipt.State == NotorietyConversationOutcomeState.Open
                || receipt.State == NotorietyConversationOutcomeState.Confirmed);
}

internal static class NotorietyConversationDataGuard
{
    internal static string Normalize(string value)
        => (value ?? string.Empty).Trim();

    internal static string NormalizeDiagnostic(string value, int maximumLength)
    {
        string normalized = Normalize(value);
        return normalized.Length <= maximumLength
            ? normalized
            : normalized.Substring(0, maximumLength);
    }

    internal static bool IsBoundedRequired(string value, int maximumLength)
        => !string.IsNullOrWhiteSpace(value) && value.Length <= maximumLength;

    internal static bool IsHexDigest(string value)
        => value != null
            && value.Length == NotorietyConversationLineFingerprintHelper.DigestLength
            && value.All(character => character >= '0' && character <= '9'
                || character >= 'A' && character <= 'F');
}
