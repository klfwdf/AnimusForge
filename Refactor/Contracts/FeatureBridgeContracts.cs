using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace AnimusForge.Refactor.Contracts;

/// <summary>
/// Stable identifiers shared by the offline bridge catalog and the runtime
/// gates.  These IDs are metadata, not assembly names or save identities.
/// </summary>
public static class FeatureBridgeIds
{
    public const int ContractVersion = 1;

    public const string BootstrapHost = "bootstrap-host";
    public const string HostRuntime = "host-runtime";
    public const string RuntimeGameAdapter = "runtime-game-adapter";
    public const string PersistenceDomainOwners = "persistence-domain-owners";
    public const string ConversationGateway = "conversation-gateway";
    public const string ConversationAction = "conversation-action";
    public const string ActionMemory = "action-memory";
    public const string ActionEconomy = "action-economy";
    public const string PolicyWorldDiplomacy = "policy-world-diplomacy";
    public const string ConversationSiege = "conversation-siege";
    public const string SceneDuel = "scene-duel";
    public const string ConversationCourier = "conversation-courier";
    public const string MemorySocialReports = "memory-social-reports";
    public const string GatewayKnowledgeProfile = "gateway-knowledge-profile";
    public const string UiRuntimeIntegration = "ui-runtime-integration";
    public const string ToolsContentRelease = "tools-content-release";

    private static readonly IReadOnlyList<string> AllIds =
        new ReadOnlyCollection<string>(new[]
        {
            BootstrapHost,
            HostRuntime,
            RuntimeGameAdapter,
            PersistenceDomainOwners,
            ConversationGateway,
            ConversationAction,
            ActionMemory,
            ActionEconomy,
            PolicyWorldDiplomacy,
            ConversationSiege,
            SceneDuel,
            ConversationCourier,
            MemorySocialReports,
            GatewayKnowledgeProfile,
            UiRuntimeIntegration,
            ToolsContentRelease
        });

    public static IReadOnlyList<string> All => AllIds;
}

public enum FeatureBridgeTopology
{
    Pair,
    CrossCut
}

public enum FeatureBridgeImplementationState
{
    ActiveBoundary,
    OptIn,
    BlockedLive,
    DesignInventory,
    DesignOnly
}

public enum FeatureBridgeFallback
{
    Native,
    NoOp,
    SafeMode,
    RetryAtBoundary
}

public enum FeatureBridgeDecisionStatus
{
    Allowed,
    Disabled,
    UnknownBridge,
    ContractVersionMismatch,
    StaleGeneration
}

/// <summary>
/// Runtime-safe metadata for one bridge.  It deliberately contains no live
/// Bannerlord object, delegate, reflection handle, save dictionary or prompt.
/// </summary>
public sealed class FeatureBridgeDefinition
{
    public FeatureBridgeDefinition(
        string id,
        FeatureBridgeImplementationState implementationState,
        FeatureBridgeTopology topology,
        bool defaultEnabled,
        FeatureBridgeFallback fallback)
    {
        Id = Required(id, nameof(id));
        ImplementationState = implementationState;
        Topology = topology;
        DefaultEnabled = defaultEnabled;
        Fallback = fallback;
    }

    public string Id { get; }
    public FeatureBridgeImplementationState ImplementationState { get; }
    public FeatureBridgeTopology Topology { get; }
    public bool DefaultEnabled { get; }
    public FeatureBridgeFallback Fallback { get; }

    private static string Required(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        return value.Trim();
    }
}

/// <summary>
/// A bounded decision returned by a bridge gate before any cross-domain side
/// effect.  The decision is intentionally separate from a gameplay result.
/// </summary>
public sealed class FeatureBridgeDecision
{
    public FeatureBridgeDecision(
        string bridgeId,
        FeatureBridgeDecisionStatus status,
        FeatureBridgeFallback fallback,
        string reasonCode)
    {
        BridgeId = bridgeId ?? string.Empty;
        Status = status;
        Fallback = fallback;
        ReasonCode = reasonCode ?? string.Empty;
    }

    public string BridgeId { get; }
    public FeatureBridgeDecisionStatus Status { get; }
    public FeatureBridgeFallback Fallback { get; }
    public string ReasonCode { get; }

    public bool IsAllowed => Status == FeatureBridgeDecisionStatus.Allowed;
}

/// <summary>
/// One-time validation result for the static runtime directory.
/// </summary>
public sealed class FeatureBridgeValidationSnapshot
{
    public FeatureBridgeValidationSnapshot(
        bool isValid,
        IEnumerable<string> issues,
        IEnumerable<FeatureBridgeDefinition> definitions)
    {
        IsValid = isValid;
        Issues = new ReadOnlyCollection<string>((issues ?? Enumerable.Empty<string>())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Take(32)
            .ToList());
        Definitions = new ReadOnlyCollection<FeatureBridgeDefinition>(
            (definitions ?? Enumerable.Empty<FeatureBridgeDefinition>())
            .Where(value => value != null)
            .ToList());
    }

    public bool IsValid { get; }
    public IReadOnlyList<string> Issues { get; }
    public IReadOnlyList<FeatureBridgeDefinition> Definitions { get; }
}
