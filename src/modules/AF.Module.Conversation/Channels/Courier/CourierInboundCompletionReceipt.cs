using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace AnimusForge.Refactor.Runtime;

internal enum CourierInboundCompletionLifecycle
{
    Unknown = 0,
    Pending = 1,
    Ready = 2,
    Applied = 3,
    Quarantined = 4
}

/// <summary>
/// Immutable Courier-owned intent plus a small lifecycle marker. The receipt
/// freezes only the visible inbound letter and stable session/recovery identity;
/// it cannot retain or replay an action, executor, callback, or postprocess body.
/// </summary>
internal sealed class CourierInboundCompletionReceipt
{
    internal const int CurrentSchemaVersion = 1;
    internal const int MaximumLetterLength = 32768;
    internal const int MaximumSerializedLength = 196608;
    private const string WirePrefix = "AFCI1:";
    private const string InboundDirection = "InboundToPlayer";
    private const int MaximumIdentityLength = 512;
    private const int MaximumDiagnosticLength = 512;

    internal int SchemaVersion { get; private set; }
    internal CourierInboundCompletionLifecycle Lifecycle { get; private set; }
    internal string SessionId { get; private set; } = string.Empty;
    internal string Direction { get; private set; } = InboundDirection;
    internal string SenderHeroId { get; private set; } = string.Empty;
    internal string RecipientHeroId { get; private set; } = string.Empty;
    internal string CourierPartyId { get; private set; } = string.Empty;
    internal string RecoveryId { get; private set; } = string.Empty;
    internal string MemoryPayloadHash { get; private set; } = string.Empty;
    internal string Letter { get; private set; } = string.Empty;
    internal string PayloadHash { get; private set; } = string.Empty;
    internal long CreatedUtcTicks { get; private set; }
    internal long ReadyUtcTicks { get; private set; }
    internal long AppliedUtcTicks { get; private set; }
    internal string DiagnosticCode { get; private set; } = string.Empty;

    private CourierInboundCompletionReceipt()
    {
    }

    internal static bool TryCreate(
        string sessionId,
        string senderHeroId,
        string recipientHeroId,
        string courierPartyId,
        string recoveryId,
        string memoryPayloadHash,
        string letter,
        long createdUtcTicks,
        out CourierInboundCompletionReceipt receipt,
        out string errorCode)
    {
        receipt = new CourierInboundCompletionReceipt
        {
            SchemaVersion = CurrentSchemaVersion,
            Lifecycle = CourierInboundCompletionLifecycle.Pending,
            SessionId = Normalize(sessionId),
            Direction = InboundDirection,
            SenderHeroId = Normalize(senderHeroId),
            RecipientHeroId = Normalize(recipientHeroId),
            CourierPartyId = Normalize(courierPartyId),
            RecoveryId = Normalize(recoveryId),
            MemoryPayloadHash = Normalize(memoryPayloadHash),
            Letter = (letter ?? string.Empty).Trim(),
            CreatedUtcTicks = createdUtcTicks,
            ReadyUtcTicks = 0L,
            AppliedUtcTicks = 0L,
            DiagnosticCode = string.Empty
        };
        receipt.PayloadHash = receipt.ComputePayloadHash();
        if (!receipt.TryValidate(out errorCode))
        {
            receipt = null;
            return false;
        }
        return true;
    }

    internal bool Matches(
        string sessionId,
        string senderHeroId,
        string recipientHeroId,
        string courierPartyId,
        string recoveryId,
        string memoryPayloadHash)
        => string.Equals(SessionId, Normalize(sessionId), StringComparison.Ordinal)
            && string.Equals(Direction, InboundDirection, StringComparison.Ordinal)
            && string.Equals(SenderHeroId, Normalize(senderHeroId), StringComparison.OrdinalIgnoreCase)
            && string.Equals(RecipientHeroId, Normalize(recipientHeroId), StringComparison.OrdinalIgnoreCase)
            && string.Equals(CourierPartyId, Normalize(courierPartyId), StringComparison.OrdinalIgnoreCase)
            && string.Equals(RecoveryId, Normalize(recoveryId), StringComparison.Ordinal)
            && string.Equals(MemoryPayloadHash, Normalize(memoryPayloadHash), StringComparison.Ordinal);

    internal bool HasSamePayload(CourierInboundCompletionReceipt other)
        => other != null
            && Matches(
                other.SessionId,
                other.SenderHeroId,
                other.RecipientHeroId,
                other.CourierPartyId,
                other.RecoveryId,
                other.MemoryPayloadHash)
            && string.Equals(PayloadHash, other.PayloadHash, StringComparison.Ordinal);

    internal void MarkReady(long utcTicks)
    {
        if (Lifecycle == CourierInboundCompletionLifecycle.Pending)
        {
            Lifecycle = CourierInboundCompletionLifecycle.Ready;
            ReadyUtcTicks = Math.Max(1L, utcTicks);
            DiagnosticCode = string.Empty;
        }
    }

    internal void MarkApplied(long utcTicks)
    {
        if (Lifecycle == CourierInboundCompletionLifecycle.Pending)
        {
            MarkReady(utcTicks);
        }
        if (Lifecycle == CourierInboundCompletionLifecycle.Ready)
        {
            Lifecycle = CourierInboundCompletionLifecycle.Applied;
            AppliedUtcTicks = Math.Max(1L, utcTicks);
            DiagnosticCode = string.Empty;
        }
    }

    internal void Quarantine(string diagnosticCode)
    {
        Lifecycle = CourierInboundCompletionLifecycle.Quarantined;
        DiagnosticCode = Truncate(
            Normalize(diagnosticCode).Replace("\r", " ").Replace("\n", " "),
            MaximumDiagnosticLength);
    }

    internal string Serialize()
    {
        if (!TryValidate(out string errorCode))
        {
            throw new InvalidOperationException(errorCode);
        }
        byte[] body = SerializeBody();
        string checksum = Hash(body);
        using (var stream = new MemoryStream())
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
        {
            writer.Write(body.Length);
            writer.Write(body);
            writer.Write(checksum);
            writer.Flush();
            string wire = WirePrefix + Convert.ToBase64String(stream.ToArray());
            if (wire.Length > MaximumSerializedLength)
            {
                throw new InvalidOperationException("courier_completion_wire_oversize");
            }
            return wire;
        }
    }

    internal static bool TryDeserialize(
        string serialized,
        out CourierInboundCompletionReceipt receipt,
        out string errorCode)
    {
        receipt = null;
        errorCode = string.Empty;
        string wire = serialized ?? string.Empty;
        if (!wire.StartsWith(WirePrefix, StringComparison.Ordinal)
            || wire.Length > MaximumSerializedLength)
        {
            errorCode = "courier_completion_wire_invalid";
            return false;
        }
        try
        {
            byte[] bytes = Convert.FromBase64String(wire.Substring(WirePrefix.Length));
            using (var stream = new MemoryStream(bytes, writable: false))
            using (var reader = new BinaryReader(stream, Encoding.UTF8, true))
            {
                int bodyLength = reader.ReadInt32();
                if (bodyLength <= 0 || bodyLength > MaximumSerializedLength
                    || bodyLength > stream.Length - stream.Position)
                {
                    errorCode = "courier_completion_body_length_invalid";
                    return false;
                }
                byte[] body = reader.ReadBytes(bodyLength);
                string checksum = reader.ReadString();
                if (stream.Position != stream.Length
                    || !FixedTimeEquals(checksum, Hash(body)))
                {
                    errorCode = "courier_completion_checksum_mismatch";
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
            errorCode = "courier_completion_wire_invalid";
            return false;
        }
    }

    private byte[] SerializeBody()
    {
        using (var stream = new MemoryStream())
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
        {
            writer.Write(SchemaVersion);
            writer.Write((int)Lifecycle);
            writer.Write(SessionId);
            writer.Write(Direction);
            writer.Write(SenderHeroId);
            writer.Write(RecipientHeroId);
            writer.Write(CourierPartyId);
            writer.Write(RecoveryId);
            writer.Write(MemoryPayloadHash);
            writer.Write(Letter);
            writer.Write(PayloadHash);
            writer.Write(CreatedUtcTicks);
            writer.Write(ReadyUtcTicks);
            writer.Write(AppliedUtcTicks);
            writer.Write(DiagnosticCode);
            writer.Flush();
            return stream.ToArray();
        }
    }

    private static bool TryDeserializeBody(
        byte[] body,
        out CourierInboundCompletionReceipt receipt,
        out string errorCode)
    {
        receipt = null;
        errorCode = string.Empty;
        using (var stream = new MemoryStream(body, writable: false))
        using (var reader = new BinaryReader(stream, Encoding.UTF8, true))
        {
            var candidate = new CourierInboundCompletionReceipt
            {
                SchemaVersion = reader.ReadInt32(),
                Lifecycle = (CourierInboundCompletionLifecycle)reader.ReadInt32(),
                SessionId = reader.ReadString(),
                Direction = reader.ReadString(),
                SenderHeroId = reader.ReadString(),
                RecipientHeroId = reader.ReadString(),
                CourierPartyId = reader.ReadString(),
                RecoveryId = reader.ReadString(),
                MemoryPayloadHash = reader.ReadString(),
                Letter = reader.ReadString(),
                PayloadHash = reader.ReadString(),
                CreatedUtcTicks = reader.ReadInt64(),
                ReadyUtcTicks = reader.ReadInt64(),
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
            || Lifecycle < CourierInboundCompletionLifecycle.Pending
            || Lifecycle > CourierInboundCompletionLifecycle.Quarantined)
        {
            errorCode = "courier_completion_schema_invalid";
            return false;
        }
        if (!IsBoundedRequired(SessionId)
            || !string.Equals(Direction, InboundDirection, StringComparison.Ordinal)
            || !IsBoundedRequired(SenderHeroId)
            || !IsBoundedRequired(RecipientHeroId)
            || !IsBoundedRequired(CourierPartyId)
            || !IsHexDigest(RecoveryId)
            || !IsHexDigest(MemoryPayloadHash)
            || string.IsNullOrWhiteSpace(Letter)
            || Letter.Length > MaximumLetterLength
            || CreatedUtcTicks <= 0L
            || ReadyUtcTicks < 0L
            || AppliedUtcTicks < 0L
            || (DiagnosticCode ?? string.Empty).Length > MaximumDiagnosticLength)
        {
            errorCode = "courier_completion_payload_invalid";
            return false;
        }
        if (Lifecycle == CourierInboundCompletionLifecycle.Pending
            && (ReadyUtcTicks != 0L || AppliedUtcTicks != 0L))
        {
            errorCode = "courier_completion_pending_state_invalid";
            return false;
        }
        if ((Lifecycle == CourierInboundCompletionLifecycle.Ready
                || Lifecycle == CourierInboundCompletionLifecycle.Applied)
            && ReadyUtcTicks <= 0L)
        {
            errorCode = "courier_completion_ready_state_invalid";
            return false;
        }
        if (Lifecycle == CourierInboundCompletionLifecycle.Applied && AppliedUtcTicks <= 0L)
        {
            errorCode = "courier_completion_applied_state_invalid";
            return false;
        }
        if (!string.Equals(PayloadHash, ComputePayloadHash(), StringComparison.Ordinal))
        {
            errorCode = "courier_completion_payload_hash_mismatch";
            return false;
        }
        return true;
    }

    private string ComputePayloadHash()
    {
        using (var stream = new MemoryStream())
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
        {
            writer.Write(CurrentSchemaVersion);
            writer.Write(SessionId ?? string.Empty);
            writer.Write(InboundDirection);
            writer.Write(SenderHeroId ?? string.Empty);
            writer.Write(RecipientHeroId ?? string.Empty);
            writer.Write(CourierPartyId ?? string.Empty);
            writer.Write(RecoveryId ?? string.Empty);
            writer.Write(MemoryPayloadHash ?? string.Empty);
            writer.Write(Letter ?? string.Empty);
            writer.Flush();
            return Hash(stream.ToArray());
        }
    }

    private static string Hash(byte[] bytes)
    {
        using (SHA256 sha = SHA256.Create())
        {
            return BitConverter.ToString(sha.ComputeHash(bytes ?? Array.Empty<byte>()))
                .Replace("-", string.Empty);
        }
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        byte[] leftBytes = Encoding.ASCII.GetBytes(left ?? string.Empty);
        byte[] rightBytes = Encoding.ASCII.GetBytes(right ?? string.Empty);
        if (leftBytes.Length != rightBytes.Length)
        {
            return false;
        }
        int difference = 0;
        for (int index = 0; index < leftBytes.Length; index++)
        {
            difference |= leftBytes[index] ^ rightBytes[index];
        }
        return difference == 0;
    }

    private static bool IsBoundedRequired(string value)
        => !string.IsNullOrWhiteSpace(value) && value.Length <= MaximumIdentityLength;

    private static bool IsHexDigest(string value)
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

    private static string Normalize(string value)
        => (value ?? string.Empty).Trim();

    private static string Truncate(string value, int maximumLength)
        => (value ?? string.Empty).Length <= maximumLength
            ? value ?? string.Empty
            : value.Substring(0, maximumLength);
}
