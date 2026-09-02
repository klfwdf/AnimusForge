using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using AnimusForge.Refactor.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AnimusForge.Refactor.Runtime;

/// <summary>
/// Static, startup-time bridge directory. It does not discover assemblies or
/// rebuild state from a Tick. The optional ModuleData configuration is read at
/// most once, before the first request, and is reduced to an immutable
/// allow-list. A malformed configuration disables every bridge (fail closed)
/// while a missing configuration uses the reviewed built-in defaults.
/// </summary>
internal static class FeatureBridgeRuntime
{
    private const string ConfigurationFileName = "FeatureBridges.json";
    private const int MaximumConfigurationBytes = 256 * 1024;
    private static readonly object ConfigurationLock = new object();
    private static readonly FeatureBridgeValidationSnapshot Snapshot = BuildSnapshot();

    private static readonly IReadOnlyDictionary<string, FeatureBridgeDefinition> DefinitionsById =
        BuildDefinitionsById(Snapshot.Definitions);

    private static IReadOnlyDictionary<string, bool> _enabledById =
        BuildDefaultEnabled(Snapshot.Definitions);

    private static string _configurationSource = "built-in";
    private static string _configurationError = string.Empty;
    private static int _configurationInitialized;

    internal static bool ConversationSiegeEnabled => IsEnabled(FeatureBridgeIds.ConversationSiege);
    internal static bool PolicyWorldDiplomacyEnabled => IsEnabled(FeatureBridgeIds.PolicyWorldDiplomacy);
    internal static bool UiRuntimeIntegrationEnabled => IsEnabled(FeatureBridgeIds.UiRuntimeIntegration);

    internal static bool Initialize(out string reason)
    {
        EnsureConfigurationLoaded();
        if (Snapshot.IsValid)
        {
            int enabledCount = Volatile.Read(ref _enabledById).Count(item => item.Value);
            reason = "feature bridge catalog valid; definitions=" + Snapshot.Definitions.Count
                + ", enabled=" + enabledCount
                + ", source=" + _configurationSource;
            return string.IsNullOrWhiteSpace(_configurationError);
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
        EnsureConfigurationLoaded();
        return Snapshot.IsValid
            && !string.IsNullOrWhiteSpace(bridgeId)
            && Volatile.Read(ref _enabledById).TryGetValue(bridgeId.Trim(), out bool enabled)
            && enabled;
    }

    internal static FeatureBridgeDecision Evaluate(
        string bridgeId,
        int contractVersion,
        long expectedGeneration = 0L,
        long currentGeneration = 0L)
    {
        EnsureConfigurationLoaded();
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

        bool enabled = IsEnabled(definition.Id);
        if (!Snapshot.IsValid || !enabled)
        {
            return new FeatureBridgeDecision(
                definition.Id,
                FeatureBridgeDecisionStatus.Disabled,
                definition.Fallback,
                !Snapshot.IsValid
                    ? "bridge.catalog_invalid"
                    : !string.IsNullOrWhiteSpace(_configurationError)
                        ? "bridge.config_invalid"
                        : "bridge.disabled");
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
            // DefaultEnabled is the built-in setting used when the optional
            // ModuleData allow-list is absent. It is intentionally independent
            // from ImplementationState: an ACTIVE_BOUNDARY inventory entry can
            // remain disabled until its owner supplies and reviews a caller.
            new FeatureBridgeDefinition(FeatureBridgeIds.BootstrapHost, FeatureBridgeImplementationState.ActiveBoundary, FeatureBridgeTopology.Pair, false, FeatureBridgeFallback.SafeMode),
            new FeatureBridgeDefinition(FeatureBridgeIds.HostRuntime, FeatureBridgeImplementationState.ActiveBoundary, FeatureBridgeTopology.Pair, false, FeatureBridgeFallback.SafeMode),
            new FeatureBridgeDefinition(FeatureBridgeIds.RuntimeGameAdapter, FeatureBridgeImplementationState.ActiveBoundary, FeatureBridgeTopology.Pair, false, FeatureBridgeFallback.Native),
            new FeatureBridgeDefinition(FeatureBridgeIds.PersistenceDomainOwners, FeatureBridgeImplementationState.DesignInventory, FeatureBridgeTopology.CrossCut, false, FeatureBridgeFallback.SafeMode),
            new FeatureBridgeDefinition(FeatureBridgeIds.ConversationGateway, FeatureBridgeImplementationState.ActiveBoundary, FeatureBridgeTopology.Pair, true, FeatureBridgeFallback.Native),
            new FeatureBridgeDefinition(FeatureBridgeIds.ConversationAction, FeatureBridgeImplementationState.OptIn, FeatureBridgeTopology.Pair, true, FeatureBridgeFallback.NoOp),
            new FeatureBridgeDefinition(FeatureBridgeIds.ActionMemory, FeatureBridgeImplementationState.OptIn, FeatureBridgeTopology.Pair, true, FeatureBridgeFallback.NoOp),
            new FeatureBridgeDefinition(FeatureBridgeIds.ActionEconomy, FeatureBridgeImplementationState.OptIn, FeatureBridgeTopology.Pair, true, FeatureBridgeFallback.NoOp),
            new FeatureBridgeDefinition(FeatureBridgeIds.PolicyWorldDiplomacy, FeatureBridgeImplementationState.ActiveBoundary, FeatureBridgeTopology.Pair, true, FeatureBridgeFallback.Native),
            new FeatureBridgeDefinition(FeatureBridgeIds.ConversationSiege, FeatureBridgeImplementationState.ActiveBoundary, FeatureBridgeTopology.Pair, true, FeatureBridgeFallback.Native),
            new FeatureBridgeDefinition(FeatureBridgeIds.SceneDuel, FeatureBridgeImplementationState.BlockedLive, FeatureBridgeTopology.Pair, false, FeatureBridgeFallback.Native),
            new FeatureBridgeDefinition(FeatureBridgeIds.ConversationCourier, FeatureBridgeImplementationState.OptIn, FeatureBridgeTopology.Pair, true, FeatureBridgeFallback.NoOp),
            new FeatureBridgeDefinition(FeatureBridgeIds.MemorySocialReports, FeatureBridgeImplementationState.OptIn, FeatureBridgeTopology.Pair, true, FeatureBridgeFallback.NoOp),
            new FeatureBridgeDefinition(FeatureBridgeIds.GatewayKnowledgeProfile, FeatureBridgeImplementationState.ActiveBoundary, FeatureBridgeTopology.Pair, true, FeatureBridgeFallback.Native),
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

    private static IReadOnlyDictionary<string, bool> BuildDefaultEnabled(
        IEnumerable<FeatureBridgeDefinition> definitions)
    {
        Dictionary<string, bool> enabled = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        foreach (FeatureBridgeDefinition definition in definitions ?? Enumerable.Empty<FeatureBridgeDefinition>())
        {
            if (definition != null && !string.IsNullOrWhiteSpace(definition.Id))
            {
                enabled[definition.Id] = definition.DefaultEnabled;
            }
        }
        return new ReadOnlyDictionary<string, bool>(enabled);
    }

    private static void EnsureConfigurationLoaded()
    {
        if (Volatile.Read(ref _configurationInitialized) != 0)
        {
            return;
        }

        lock (ConfigurationLock)
        {
            if (_configurationInitialized != 0)
            {
                return;
            }

            try
            {
                string path = ResolveConfigurationPath();
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                {
                    _configurationSource = "built-in";
                }
                else
                {
                    _configurationSource = "module-data";
                    _enabledById = LoadConfiguredEnabled(path);
                }
            }
            catch (BridgeConfigurationException exception)
            {
                _enabledById = BuildDisabledMap();
                _configurationError = exception.Code;
            }
            catch
            {
                _enabledById = BuildDisabledMap();
                _configurationError = "bridge.config.read_failed";
            }

            Volatile.Write(ref _configurationInitialized, 1);
        }
    }

    private static IReadOnlyDictionary<string, bool> LoadConfiguredEnabled(string path)
    {
        FileInfo file = new FileInfo(path);
        if (file.Length <= 0 || file.Length > MaximumConfigurationBytes)
        {
            throw new BridgeConfigurationException("bridge.config.size_invalid");
        }

        string json = File.ReadAllText(path, new UTF8Encoding(false, true));
        JObject root;
        try
        {
            root = JObject.Parse(
                json,
                new JsonLoadSettings
                {
                    DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error
                });
        }
        catch (JsonException)
        {
            throw new BridgeConfigurationException("bridge.config.parse_failed");
        }
        HashSet<string> expectedFields = new HashSet<string>(
            new[] { "schemaVersion", "contractVersion", "enabled" },
            StringComparer.Ordinal);
        if (root.Properties().Any(property => !expectedFields.Contains(property.Name)))
        {
            throw new BridgeConfigurationException("bridge.config.unknown_field");
        }
        JToken schemaToken = root["schemaVersion"];
        JToken contractToken = root["contractVersion"];
        if (schemaToken?.Type != JTokenType.Integer
            || contractToken?.Type != JTokenType.Integer
            || schemaToken.Value<int>() != 1
            || contractToken.Value<int>() != FeatureBridgeIds.ContractVersion)
        {
            throw new BridgeConfigurationException("bridge.config.version_mismatch");
        }

        JToken enabledToken = root["enabled"];
        if (!(enabledToken is JArray enabledArray) || enabledArray.Count > FeatureBridgeIds.All.Count)
        {
            throw new BridgeConfigurationException("bridge.config.enabled_invalid");
        }

        HashSet<string> requested = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (JToken token in enabledArray)
        {
            if (token.Type != JTokenType.String)
            {
                throw new BridgeConfigurationException("bridge.config.enabled_item_invalid");
            }
            string id = (token.Value<string>() ?? string.Empty).Trim();
            if (!requested.Add(id)
                || !DefinitionsById.TryGetValue(id, out FeatureBridgeDefinition definition)
                || !definition.DefaultEnabled
                || definition.ImplementationState == FeatureBridgeImplementationState.BlockedLive
                || definition.ImplementationState == FeatureBridgeImplementationState.DesignInventory
                || definition.ImplementationState == FeatureBridgeImplementationState.DesignOnly)
            {
                throw new BridgeConfigurationException("bridge.config.bridge_not_configurable");
            }
        }

        Dictionary<string, bool> enabled = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        foreach (string id in FeatureBridgeIds.All)
        {
            enabled[id] = requested.Contains(id);
        }
        return new ReadOnlyDictionary<string, bool>(enabled);
    }

    private static IReadOnlyDictionary<string, bool> BuildDisabledMap()
    {
        Dictionary<string, bool> disabled = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        foreach (string id in FeatureBridgeIds.All)
        {
            disabled[id] = false;
        }
        return new ReadOnlyDictionary<string, bool>(disabled);
    }

    private static string ResolveConfigurationPath()
    {
        string assemblyDirectory = string.Empty;
        try
        {
            assemblyDirectory = Path.GetDirectoryName(
                typeof(FeatureBridgeRuntime).Assembly.Location);
        }
        catch
        {
        }

        string moduleConfiguration = FindModuleConfiguration(assemblyDirectory);
        if (!string.IsNullOrWhiteSpace(moduleConfiguration))
        {
            return moduleConfiguration;
        }

        string baseDirectory = string.Empty;
        try
        {
            baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        }
        catch
        {
        }
        moduleConfiguration = FindModuleConfiguration(baseDirectory);
        return string.IsNullOrWhiteSpace(moduleConfiguration)
            ? ConfigurationFileName
            : moduleConfiguration;
    }

    private static string FindModuleConfiguration(string startDirectory)
    {
        string current = startDirectory;
        for (int depth = 0; depth <= 8 && !string.IsNullOrWhiteSpace(current); depth++)
        {
            try
            {
                string folderName = Path.GetFileName(
                    current.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                string moduleData = Path.Combine(current, "ModuleData");
                if (string.Equals(folderName, "AnimusForge", StringComparison.OrdinalIgnoreCase)
                    && File.Exists(Path.Combine(current, "SubModule.xml"))
                    && Directory.Exists(moduleData))
                {
                    return Path.Combine(moduleData, ConfigurationFileName);
                }
                current = Directory.GetParent(current)?.FullName;
            }
            catch
            {
                return string.Empty;
            }
        }

        return string.Empty;
    }

    private sealed class BridgeConfigurationException : Exception
    {
        internal BridgeConfigurationException(string code)
        {
            Code = code ?? "bridge.config.invalid";
        }

        internal string Code { get; }
    }
}
