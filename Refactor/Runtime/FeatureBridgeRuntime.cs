using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using AnimusForge.Refactor.Contracts;

namespace AnimusForge.Refactor.Runtime;

/// <summary>
/// Static, startup-time bridge directory.  It does not discover assemblies,
/// read files, or rebuild state from a Tick.  Only the three already-existing
/// cross-domain entry points currently consult a runtime gate; the remaining
/// bridges are declared in the offline binding manifest until their owners
/// provide a real adapter.
/// </summary>
internal static class FeatureBridgeRuntime
{
    private static readonly FeatureBridgeValidationSnapshot Snapshot = BuildSnapshot();

    private static readonly IReadOnlyDictionary<string, FeatureBridgeDefinition> DefinitionsById =
        BuildDefinitionsById(Snapshot.Definitions);

    internal static bool ConversationSiegeEnabled => IsEnabled(FeatureBridgeIds.ConversationSiege);
    internal static bool PolicyWorldDiplomacyEnabled => IsEnabled(FeatureBridgeIds.PolicyWorldDiplomacy);
    internal static bool UiRuntimeIntegrationEnabled => IsEnabled(FeatureBridgeIds.UiRuntimeIntegration);

    internal static bool Initialize(out string reason)
    {
        if (Snapshot.IsValid)
        {
            int runtimeWired = Snapshot.Definitions.Count(item => item.DefaultEnabled);
            reason = "feature bridge catalog valid; definitions=" + Snapshot.Definitions.Count + ", runtimeWired=" + runtimeWired;
            return true;
        }

        reason = "feature bridge catalog invalid: " + string.Join("; ", Snapshot.Issues);
        return false;
    }

    internal static FeatureBridgeValidationSnapshot GetValidationSnapshot()
    {
        return Snapshot;
    }

    internal static bool IsEnabled(string bridgeId)
    {
        return Snapshot.IsValid
            && !string.IsNullOrWhiteSpace(bridgeId)
            && DefinitionsById.TryGetValue(bridgeId.Trim(), out FeatureBridgeDefinition definition)
            && definition.DefaultEnabled;
    }

    internal static FeatureBridgeDecision Evaluate(
        string bridgeId,
        int contractVersion,
        long expectedGeneration = 0L,
        long currentGeneration = 0L)
    {
        if (string.IsNullOrWhiteSpace(bridgeId)
            || !DefinitionsById.TryGetValue(bridgeId.Trim(), out FeatureBridgeDefinition definition))
        {
            return new FeatureBridgeDecision(
                bridgeId,
                FeatureBridgeDecisionStatus.UnknownBridge,
                FeatureBridgeFallback.SafeMode,
                "bridge.unknown");
        }

        if (contractVersion != FeatureBridgeIds.ContractVersion)
        {
            return new FeatureBridgeDecision(
                definition.Id,
                FeatureBridgeDecisionStatus.ContractVersionMismatch,
                definition.Fallback,
                "bridge.contract_version_mismatch");
        }

        // A caller that supplies an expected generation has opted into an
        // exact snapshot check.  An unavailable current generation (zero) is
        // therefore stale as well; silently accepting it would let a detached
        // result cross a lifecycle boundary.
        if (expectedGeneration > 0L && expectedGeneration != currentGeneration)
        {
            return new FeatureBridgeDecision(
                definition.Id,
                FeatureBridgeDecisionStatus.StaleGeneration,
                definition.Fallback,
                "bridge.stale_generation");
        }

        if (!Snapshot.IsValid || !definition.DefaultEnabled)
        {
            return new FeatureBridgeDecision(
                definition.Id,
                FeatureBridgeDecisionStatus.Disabled,
                definition.Fallback,
                Snapshot.IsValid ? "bridge.disabled" : "bridge.catalog_invalid");
        }

        return new FeatureBridgeDecision(
            definition.Id,
            FeatureBridgeDecisionStatus.Allowed,
            definition.Fallback,
            "bridge.allowed");
    }

    private static FeatureBridgeValidationSnapshot BuildSnapshot()
    {
        FeatureBridgeDefinition[] definitions =
        {
            // DefaultEnabled means an already reviewed runtime caller exists.
            // It is intentionally independent from ImplementationState: an
            // ACTIVE_BOUNDARY inventory entry can remain declared-only until
            // its owner supplies and reviews a concrete caller.
            new FeatureBridgeDefinition(FeatureBridgeIds.BootstrapHost, FeatureBridgeImplementationState.ActiveBoundary, FeatureBridgeTopology.Pair, false, FeatureBridgeFallback.SafeMode),
            new FeatureBridgeDefinition(FeatureBridgeIds.HostRuntime, FeatureBridgeImplementationState.ActiveBoundary, FeatureBridgeTopology.Pair, false, FeatureBridgeFallback.SafeMode),
            new FeatureBridgeDefinition(FeatureBridgeIds.RuntimeGameAdapter, FeatureBridgeImplementationState.ActiveBoundary, FeatureBridgeTopology.Pair, false, FeatureBridgeFallback.Native),
            new FeatureBridgeDefinition(FeatureBridgeIds.PersistenceDomainOwners, FeatureBridgeImplementationState.DesignInventory, FeatureBridgeTopology.CrossCut, false, FeatureBridgeFallback.SafeMode),
            new FeatureBridgeDefinition(FeatureBridgeIds.ConversationGateway, FeatureBridgeImplementationState.ActiveBoundary, FeatureBridgeTopology.Pair, false, FeatureBridgeFallback.Native),
            new FeatureBridgeDefinition(FeatureBridgeIds.ConversationAction, FeatureBridgeImplementationState.OptIn, FeatureBridgeTopology.Pair, false, FeatureBridgeFallback.NoOp),
            new FeatureBridgeDefinition(FeatureBridgeIds.ActionMemory, FeatureBridgeImplementationState.OptIn, FeatureBridgeTopology.Pair, false, FeatureBridgeFallback.NoOp),
            new FeatureBridgeDefinition(FeatureBridgeIds.ActionEconomy, FeatureBridgeImplementationState.OptIn, FeatureBridgeTopology.Pair, false, FeatureBridgeFallback.NoOp),
            new FeatureBridgeDefinition(FeatureBridgeIds.PolicyWorldDiplomacy, FeatureBridgeImplementationState.ActiveBoundary, FeatureBridgeTopology.Pair, true, FeatureBridgeFallback.Native),
            new FeatureBridgeDefinition(FeatureBridgeIds.ConversationSiege, FeatureBridgeImplementationState.ActiveBoundary, FeatureBridgeTopology.Pair, true, FeatureBridgeFallback.Native),
            new FeatureBridgeDefinition(FeatureBridgeIds.SceneDuel, FeatureBridgeImplementationState.BlockedLive, FeatureBridgeTopology.Pair, false, FeatureBridgeFallback.Native),
            new FeatureBridgeDefinition(FeatureBridgeIds.ConversationCourier, FeatureBridgeImplementationState.OptIn, FeatureBridgeTopology.Pair, false, FeatureBridgeFallback.NoOp),
            new FeatureBridgeDefinition(FeatureBridgeIds.MemorySocialReports, FeatureBridgeImplementationState.OptIn, FeatureBridgeTopology.Pair, false, FeatureBridgeFallback.NoOp),
            new FeatureBridgeDefinition(FeatureBridgeIds.GatewayKnowledgeProfile, FeatureBridgeImplementationState.ActiveBoundary, FeatureBridgeTopology.Pair, false, FeatureBridgeFallback.Native),
            new FeatureBridgeDefinition(FeatureBridgeIds.UiRuntimeIntegration, FeatureBridgeImplementationState.ActiveBoundary, FeatureBridgeTopology.CrossCut, true, FeatureBridgeFallback.SafeMode),
            new FeatureBridgeDefinition(FeatureBridgeIds.ToolsContentRelease, FeatureBridgeImplementationState.DesignOnly, FeatureBridgeTopology.CrossCut, false, FeatureBridgeFallback.SafeMode)
        };

        List<string> issues = new List<string>();
        if (definitions.Length != FeatureBridgeIds.All.Count)
        {
            issues.Add("definition count does not match canonical ID count");
        }

        HashSet<string> ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (FeatureBridgeDefinition definition in definitions)
        {
            if (!ids.Add(definition.Id))
            {
                issues.Add("duplicate bridge ID: " + definition.Id);
            }
        }

        foreach (string id in FeatureBridgeIds.All)
        {
            if (!ids.Contains(id))
            {
                issues.Add("missing canonical bridge ID: " + id);
            }
        }

        return new FeatureBridgeValidationSnapshot(issues.Count == 0, issues, definitions);
    }

    private static IReadOnlyDictionary<string, FeatureBridgeDefinition> BuildDefinitionsById(
        IEnumerable<FeatureBridgeDefinition> definitions)
    {
        Dictionary<string, FeatureBridgeDefinition> indexed =
            new Dictionary<string, FeatureBridgeDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (FeatureBridgeDefinition definition in definitions ?? Enumerable.Empty<FeatureBridgeDefinition>())
        {
            if (definition != null && !indexed.ContainsKey(definition.Id))
            {
                indexed.Add(definition.Id, definition);
            }
        }

        return new ReadOnlyDictionary<string, FeatureBridgeDefinition>(indexed);
    }
}
