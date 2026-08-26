using System;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Immutable identity of one SETS settlement-entry capture operation.
/// Holds stable campaign ids only; live Bannerlord objects stay in the runtime adapter.
/// </summary>
public sealed class SetsUrbanCaptureContext
{
    public SetsUrbanCaptureContext(
        string operationId,
        string settlementId,
        SetsSettlementSceneKind sceneKind,
        SetsUrbanCaptureOwnershipClassification ownershipClassification,
        string previousOwnerClanId,
        int selectedFollowerCount)
    {
        OperationId = Normalize(operationId);
        SettlementId = Normalize(settlementId);
        SceneKind = sceneKind;
        OwnershipClassification = ownershipClassification;
        PreviousOwnerClanId = Normalize(previousOwnerClanId);
        SelectedFollowerCount = selectedFollowerCount < 0 ? 0 : selectedFollowerCount;
    }

    /// <summary>Unique id for this capture operation; commits and retries dedupe against it.</summary>
    public string OperationId { get; }

    public string SettlementId { get; }

    public SetsSettlementSceneKind SceneKind { get; }

    public SetsUrbanCaptureOwnershipClassification OwnershipClassification { get; }

    /// <summary>Owner clan id captured before any transfer, for later relation handling. May be empty.</summary>
    public string PreviousOwnerClanId { get; }

    public int SelectedFollowerCount { get; }

    public bool IsValid
    {
        get
        {
            return OperationId.Length > 0
                && SettlementId.Length > 0
                && SetsSettlementEntryProfile.IsSupported(SceneKind)
                && OwnershipClassification != SetsUrbanCaptureOwnershipClassification.Unknown;
        }
    }

    public bool IsHostileCapture
    {
        get { return OwnershipClassification == SetsUrbanCaptureOwnershipClassification.Hostile; }
    }

    public bool IsOwnedOrAttachedIncident
    {
        get
        {
            return OwnershipClassification == SetsUrbanCaptureOwnershipClassification.PlayerOwned
                || OwnershipClassification == SetsUrbanCaptureOwnershipClassification.RulerAttached;
        }
    }

    /// <summary>A loaded or retried record may resume only when its identity still agrees with live state.</summary>
    public bool MatchesLiveState(string liveSettlementId, string liveOwnerClanId, bool ownershipAlreadyCommitted)
    {
        if (!IsValid)
        {
            return false;
        }

        if (!string.Equals(SettlementId, Normalize(liveSettlementId), StringComparison.Ordinal))
        {
            return false;
        }

        if (ownershipAlreadyCommitted)
        {
            // After a committed transfer the live owner is expected to differ from the previous owner.
            return true;
        }

        // Before transfer, a changed owner means the world moved on; fail closed.
        return PreviousOwnerClanId.Length == 0
            || string.Equals(PreviousOwnerClanId, Normalize(liveOwnerClanId), StringComparison.Ordinal);
    }

    private static string Normalize(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "" : value.Trim();
    }
}
