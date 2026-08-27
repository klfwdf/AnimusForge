using System;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>Recovery decision for a restored capture operation checked against live world state.</summary>
public enum SetsUrbanCaptureRecoveryDecision
{
    /// <summary>Record is unusable and produced no side effects; drop it silently.</summary>
    Abandon = 0,

    /// <summary>Live world still matches; continue from the last verified stage.</summary>
    Continue = 1,

    /// <summary>Owner already the player clan with victory committed; treat ownership stage as done.</summary>
    ContinueOwnershipAlreadyApplied = 2,

    /// <summary>Live world moved on (third-party owner, missing clan); freeze without side effects.</summary>
    Suspend = 3
}

/// <summary>
/// Immutable identity of one SETS hostile urban-capture operation.
/// Construction fails closed: only hostile enemy towns and castles qualify.
/// Owned/attached incidents and villages never build a context.
/// </summary>
public sealed class SetsUrbanCaptureContext
{
    public const int CurrentSchemaVersion = 1;

    private SetsUrbanCaptureContext(
        string operationId,
        string settlementId,
        SetsSettlementSceneKind sceneKind,
        string previousOwnerClanId,
        string playerClanId,
        int selectedFollowerCount)
    {
        OperationId = operationId;
        SettlementId = settlementId;
        SceneKind = sceneKind;
        PreviousOwnerClanId = previousOwnerClanId;
        PlayerClanId = playerClanId;
        SelectedFollowerCount = selectedFollowerCount;
        SchemaVersion = CurrentSchemaVersion;
    }

    /// <summary>Unique id for this capture operation; commits and retries dedupe against it.</summary>
    public string OperationId { get; }

    public string SettlementId { get; }

    /// <summary>Town or Castle only.</summary>
    public SetsSettlementSceneKind SceneKind { get; }

    /// <summary>Owner clan id captured before any transfer, for relation handling and recovery checks.</summary>
    public string PreviousOwnerClanId { get; }

    /// <summary>The player clan expected to receive ownership. Never empty.</summary>
    public string PlayerClanId { get; }

    public int SelectedFollowerCount { get; }

    public int SchemaVersion { get; }

    /// <summary>
    /// Build a hostile capture context, or null when the facts do not describe a
    /// legal hostile town/castle capture. Callers must treat null as "no session".
    /// Rejected: non-town/castle scenes, missing ids, and player-owned targets.
    /// </summary>
    public static SetsUrbanCaptureContext TryCreateHostile(
        string operationId,
        string settlementId,
        SetsSettlementSceneKind sceneKind,
        string previousOwnerClanId,
        string playerClanId,
        int selectedFollowerCount)
    {
        string normalizedOperationId = Normalize(operationId);
        string normalizedSettlementId = Normalize(settlementId);
        string normalizedPreviousOwner = Normalize(previousOwnerClanId);
        string normalizedPlayerClan = Normalize(playerClanId);

        if (normalizedOperationId.Length == 0
            || normalizedSettlementId.Length == 0
            || normalizedPlayerClan.Length == 0)
        {
            return null;
        }

        if (sceneKind != SetsSettlementSceneKind.Town && sceneKind != SetsSettlementSceneKind.Castle)
        {
            return null;
        }

        // A settlement the player already owns can never be a hostile capture target.
        if (string.Equals(normalizedPreviousOwner, normalizedPlayerClan, StringComparison.Ordinal))
        {
            return null;
        }

        return new SetsUrbanCaptureContext(
            normalizedOperationId,
            normalizedSettlementId,
            sceneKind,
            normalizedPreviousOwner,
            normalizedPlayerClan,
            selectedFollowerCount < 0 ? 0 : selectedFollowerCount);
    }

    public bool IsValid
    {
        get
        {
            return OperationId.Length > 0
                && SettlementId.Length > 0
                && PlayerClanId.Length > 0
                && (SceneKind == SetsSettlementSceneKind.Town || SceneKind == SetsSettlementSceneKind.Castle)
                && !string.Equals(PreviousOwnerClanId, PlayerClanId, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// Recovery table for a restored record (handoff section 8.2). Fail closed:
    /// anything other than an exact expected owner or a provably completed
    /// transfer suspends or abandons rather than guessing.
    /// </summary>
    public SetsUrbanCaptureRecoveryDecision ResolveRecovery(
        bool settlementExists,
        string liveOwnerClanId,
        bool livePlayerClanExists,
        string livePlayerClanId,
        bool victoryCommitted)
    {
        if (!IsValid)
        {
            return SetsUrbanCaptureRecoveryDecision.Abandon;
        }

        if (!settlementExists)
        {
            return SetsUrbanCaptureRecoveryDecision.Abandon;
        }

        if (!livePlayerClanExists
            || !string.Equals(Normalize(livePlayerClanId), PlayerClanId, StringComparison.Ordinal))
        {
            return SetsUrbanCaptureRecoveryDecision.Suspend;
        }

        string normalizedLiveOwner = Normalize(liveOwnerClanId);
        if (string.Equals(normalizedLiveOwner, PreviousOwnerClanId, StringComparison.Ordinal))
        {
            return SetsUrbanCaptureRecoveryDecision.Continue;
        }

        if (string.Equals(normalizedLiveOwner, PlayerClanId, StringComparison.Ordinal))
        {
            return victoryCommitted
                ? SetsUrbanCaptureRecoveryDecision.ContinueOwnershipAlreadyApplied
                : SetsUrbanCaptureRecoveryDecision.Suspend;
        }

        // Third-party owner: the world moved on. Never continue the capture.
        return SetsUrbanCaptureRecoveryDecision.Suspend;
    }

    private static string Normalize(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "" : value.Trim();
    }
}
