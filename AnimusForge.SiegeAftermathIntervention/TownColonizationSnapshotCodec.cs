using System;
using System.Text;

namespace AnimusForge.SiegeAftermathIntervention;

public static class TownColonizationSnapshotCodec
{
    private const string Version = "v1";

    public static string Encode(TownColonizationSnapshot snapshot)
    {
        if (snapshot == null || snapshot.State == TownColonizationState.None)
        {
            return string.Empty;
        }

        return string.Join("|",
            Version,
            ((int)snapshot.State).ToString(),
            ((int)snapshot.CommitReason).ToString(),
            EncodeText(snapshot.SettlementId),
            EncodeText(snapshot.TargetCultureId),
            snapshot.CapturedTargetCount.ToString(),
            snapshot.SettlementOutcomeCommitted ? "1" : "0");
    }

    public static bool TryDecode(string payload, out TownColonizationSnapshot snapshot)
    {
        snapshot = null;
        if (string.IsNullOrWhiteSpace(payload))
        {
            return false;
        }

        string[] parts = payload.Split('|');
        if (parts.Length != 7
            || !string.Equals(parts[0], Version, StringComparison.Ordinal)
            || !int.TryParse(parts[1], out int stateValue)
            || !int.TryParse(parts[2], out int reasonValue)
            || !int.TryParse(parts[5], out int capturedTargetCount)
            || (parts[6] != "0" && parts[6] != "1")
            || !Enum.IsDefined(typeof(TownColonizationState), stateValue)
            || !Enum.IsDefined(typeof(TownColonizationCommitReason), reasonValue))
        {
            return false;
        }

        TownColonizationState state = (TownColonizationState)stateValue;
        if (state == TownColonizationState.None
            || capturedTargetCount < 0
            || !TryDecodeText(parts[3], out string settlementId)
            || !TryDecodeText(parts[4], out string targetCultureId)
            || string.IsNullOrWhiteSpace(settlementId)
            || string.IsNullOrWhiteSpace(targetCultureId))
        {
            return false;
        }

        snapshot = new TownColonizationSnapshot(
            state,
            (TownColonizationCommitReason)reasonValue,
            settlementId,
            targetCultureId,
            capturedTargetCount,
            parts[6] == "1");
        return true;
    }

    private static string EncodeText(string value)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(value ?? string.Empty));
    }

    private static bool TryDecodeText(string value, out string decoded)
    {
        decoded = string.Empty;
        try
        {
            decoded = Encoding.UTF8.GetString(Convert.FromBase64String(value ?? string.Empty));
            return true;
        }
        catch
        {
            return false;
        }
    }
}
