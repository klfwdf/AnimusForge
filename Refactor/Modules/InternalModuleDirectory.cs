using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace AnimusForge.Refactor.Modules;

internal sealed class InternalModuleDependency
{
    internal InternalModuleDependency(string moduleId, int requiredContractVersion)
    {
        ModuleId = (moduleId ?? string.Empty).Trim();
        RequiredContractVersion = requiredContractVersion;
    }

    internal string ModuleId { get; }
    internal int RequiredContractVersion { get; }
}

internal sealed class InternalCapabilityDefinition
{
    internal InternalCapabilityDefinition(string id, int contractVersion, IEnumerable<string> requiredFeatureBridgeIds = null)
    {
        Id = (id ?? string.Empty).Trim();
        ContractVersion = contractVersion;
        RequiredFeatureBridgeIds = new ReadOnlyCollection<string>(
            (requiredFeatureBridgeIds ?? Enumerable.Empty<string>()).Select(value => (value ?? string.Empty).Trim()).ToArray());
    }

    internal string Id { get; }
    internal int ContractVersion { get; }
    internal IReadOnlyList<string> RequiredFeatureBridgeIds { get; }
}

internal sealed class InternalModuleDefinition
{
    internal InternalModuleDefinition(string id, int contractVersion,
        IEnumerable<InternalCapabilityDefinition> capabilities, IEnumerable<InternalModuleDependency> requiredModules = null)
    {
        Id = (id ?? string.Empty).Trim();
        ContractVersion = contractVersion;
        Capabilities = new ReadOnlyCollection<InternalCapabilityDefinition>(
            (capabilities ?? Enumerable.Empty<InternalCapabilityDefinition>()).ToArray());
        RequiredModules = new ReadOnlyCollection<InternalModuleDependency>(
            (requiredModules ?? Enumerable.Empty<InternalModuleDependency>()).ToArray());
    }

    internal string Id { get; }
    internal int ContractVersion { get; }
    internal IReadOnlyList<InternalModuleDependency> RequiredModules { get; }
    internal IReadOnlyList<InternalCapabilityDefinition> Capabilities { get; }
}

internal enum InternalModuleRuntimeState
{
    NotInitialized,
    Ready,
    Unavailable,
    Failed
}

internal enum InternalCapabilityState
{
    Available,
    RegistrationOpen,
    UnknownCapability,
    Blocked,
    VersionMismatch,
    ModuleNotInitialized,
    ModuleUnavailable,
    ModuleFailed,
    DependencyUnavailable,
    BridgeRejected
}

internal sealed class InternalModuleValidationResult
{
    internal InternalModuleValidationResult(IEnumerable<string> issues)
    {
        Issues = new ReadOnlyCollection<string>(issues.ToArray());
    }

    internal bool IsValid => Issues.Count == 0;
    internal IReadOnlyList<string> Issues { get; }
}

internal sealed class InternalModuleStatus
{
    internal InternalModuleStatus(InternalModuleDefinition definition, InternalModuleRuntimeState state,
        string reasonCode, bool isRegistrationComplete, IReadOnlyList<string> validationIssues)
    {
        Definition = definition;
        State = state;
        ReasonCode = reasonCode;
        IsRegistrationComplete = isRegistrationComplete;
        ValidationIssues = validationIssues;
    }

    internal InternalModuleDefinition Definition { get; }
    internal InternalModuleRuntimeState State { get; }
    internal string ReasonCode { get; }
    internal bool IsRegistrationComplete { get; }
    internal IReadOnlyList<string> ValidationIssues { get; }
    internal bool IsDefinitionValid => IsRegistrationComplete && ValidationIssues.Count == 0;
}

internal sealed class InternalCapabilityStatus
{
    internal InternalCapabilityStatus(string capabilityId, string moduleId, int contractVersion,
        int requestedContractVersion, InternalCapabilityState state, string reasonCode)
    {
        CapabilityId = capabilityId;
        ModuleId = moduleId;
        ContractVersion = contractVersion;
        RequestedContractVersion = requestedContractVersion;
        State = state;
        ReasonCode = reasonCode;
    }

    internal string CapabilityId { get; }
    internal string ModuleId { get; }
    internal int ContractVersion { get; }
    internal int RequestedContractVersion { get; }
    internal InternalCapabilityState State { get; }
    internal string ReasonCode { get; }
    // Available means a registered adapter can be routed to, not that a Campaign is active,
    // a gameplay action is authorized, or this feature has passed live acceptance.
    internal bool IsAvailable => State == InternalCapabilityState.Available;
}

/// <summary>
/// Small, explicitly composed directory inside AnimusForge.dll. It runs no handlers, scans
/// no assemblies, persists no switches, and does not intercept existing legacy entry points.
/// RuntimeState is the composition owner's observed binding state, never a replacement for
/// FeatureBridge gates or the per-request generation/target/main-thread authority checks.
/// </summary>
internal sealed class InternalModuleDirectory
{
    private const int MaximumModules = 64;
    private const int MaximumCapabilities = 256;
    private readonly object _sync = new object();
    private readonly Func<string, bool> _bridgeKnown;
    private readonly Func<string, string> _bridgeRejectionReason;
    private readonly Dictionary<string, ModuleEntry> _modules = new Dictionary<string, ModuleEntry>(StringComparer.Ordinal);
    private readonly Dictionary<string, CapabilityEntry> _capabilities = new Dictionary<string, CapabilityEntry>(StringComparer.Ordinal);
    private InternalModuleValidationResult _validation;
    private IReadOnlyList<InternalModuleStatus> _snapshot;

    private sealed class ModuleEntry
    {
        internal InternalModuleDefinition Definition;
        internal InternalModuleRuntimeState State;
        internal string ReasonCode = "module.not_initialized";
        internal IReadOnlyList<string> Issues = new ReadOnlyCollection<string>(Array.Empty<string>());
        internal ModuleEntry[] RequiredClosure = Array.Empty<ModuleEntry>();
    }

    private sealed class CapabilityEntry
    {
        internal ModuleEntry Module;
        internal InternalCapabilityDefinition Definition;
    }

    internal InternalModuleDirectory(Func<string, bool> bridgeKnown, Func<string, string> bridgeRejectionReason)
    {
        _bridgeKnown = bridgeKnown ?? throw new ArgumentNullException(nameof(bridgeKnown));
        _bridgeRejectionReason = bridgeRejectionReason ?? throw new ArgumentNullException(nameof(bridgeRejectionReason));
    }

    internal bool TryRegister(InternalModuleDefinition definition, out string reasonCode)
    {
        lock (_sync)
        {
            if (_validation != null) { reasonCode = "module.registration_closed"; return false; }
        }
        // The injected gate adapter is caller code: never invoke it while holding the directory lock.
        reasonCode = ValidateDefinition(definition);
        if (reasonCode != null) { return false; }
        lock (_sync)
        {
            if (_validation != null) { reasonCode = "module.registration_closed"; return false; }
            if (_modules.ContainsKey(definition.Id)) { reasonCode = "module.duplicate:" + definition.Id; return false; }
            foreach (InternalCapabilityDefinition capability in definition.Capabilities)
            {
                if (_capabilities.ContainsKey(capability.Id)) { reasonCode = "capability.duplicate:" + capability.Id; return false; }
            }
            if (_modules.Count >= MaximumModules || _capabilities.Count + definition.Capabilities.Count > MaximumCapabilities)
            {
                reasonCode = "module.catalog_capacity";
                return false;
            }
            // Reject the entire second declaration before changing either index. The caller must
            // not install a provider for a rejected registration; accepted definitions are never overwritten.
            ModuleEntry module = new ModuleEntry { Definition = definition };
            _modules.Add(definition.Id, module);
            foreach (InternalCapabilityDefinition capability in definition.Capabilities)
            {
                _capabilities.Add(capability.Id, new CapabilityEntry { Module = module, Definition = capability });
            }
            _snapshot = null;
            reasonCode = "module.registered";
            return true;
        }
    }

    internal InternalModuleValidationResult CompleteRegistration()
    {
        lock (_sync)
        {
            if (_validation != null) { return _validation; }
            List<string> catalogIssues = new List<string>();
            foreach (ModuleEntry module in _modules.Values.OrderBy(item => item.Definition.Id, StringComparer.Ordinal))
            {
                HashSet<ModuleEntry> closure = new HashSet<ModuleEntry>();
                HashSet<string> visited = new HashSet<string>(StringComparer.Ordinal);
                List<string> path = new List<string>();
                HashSet<string> issues = new HashSet<string>(StringComparer.Ordinal);
                BuildClosure(module, closure, visited, path, issues);
                module.RequiredClosure = closure.OrderBy(item => item.Definition.Id, StringComparer.Ordinal).ToArray();
                module.Issues = new ReadOnlyCollection<string>(issues.OrderBy(item => item, StringComparer.Ordinal).ToArray());
                catalogIssues.AddRange(module.Issues.Select(issue => module.Definition.Id + ":" + issue));
            }
            // Invalid dependencies block only their consumers, not unrelated accepted modules.
            _validation = new InternalModuleValidationResult(catalogIssues);
            _snapshot = null;
            return _validation;
        }
    }

    internal bool UpdateRuntimeState(string moduleId, InternalModuleRuntimeState state, string reasonCode = null)
    {
        if (!Enum.IsDefined(typeof(InternalModuleRuntimeState), state)) { return false; }
        lock (_sync)
        {
            if (_validation == null || moduleId == null || !_modules.TryGetValue(moduleId, out ModuleEntry module)) { return false; }
            string reason = string.IsNullOrWhiteSpace(reasonCode) ? DefaultReason(state) : reasonCode.Trim();
            if (reason.Length > 256) { return false; }
            if (module.State != state || !string.Equals(module.ReasonCode, reason, StringComparison.Ordinal))
            {
                module.State = state;
                module.ReasonCode = reason;
                _snapshot = null;
            }
            return true;
        }
    }

    internal InternalCapabilityStatus GetCapabilityStatus(string capabilityId, int requestedContractVersion)
    {
        CapabilityEntry capability;
        lock (_sync)
        {
            if (_validation == null)
            {
                return new InternalCapabilityStatus(capabilityId ?? string.Empty, string.Empty, 0,
                    requestedContractVersion, InternalCapabilityState.RegistrationOpen, "module.registration_open");
            }
            if (capabilityId == null || !_capabilities.TryGetValue(capabilityId, out capability))
            {
                return new InternalCapabilityStatus(capabilityId ?? string.Empty, string.Empty, 0,
                    requestedContractVersion, InternalCapabilityState.UnknownCapability, "capability.unknown");
            }
            InternalCapabilityStatus status = GetLocalStatus(capability, requestedContractVersion);
            if (!status.IsAvailable) { return status; }
        }
        foreach (string bridgeId in capability.Definition.RequiredFeatureBridgeIds)
        {
            string rejection;
            try { rejection = _bridgeRejectionReason(bridgeId); }
            catch { rejection = "bridge.evaluation_failed:" + bridgeId; }
            // Only null is approval. An empty or malformed rejection must not silently grant access.
            if (rejection != null)
            {
                return Result(capability, requestedContractVersion, InternalCapabilityState.BridgeRejected,
                    string.IsNullOrWhiteSpace(rejection) || rejection.Length > 256 ? "bridge.rejected:" + bridgeId : rejection);
            }
        }
        lock (_sync)
        {
            // A gate adapter may reenter and report a changed binding state. Recheck the small,
            // precomputed dependency closure rather than publishing a stale Ready observation.
            return GetLocalStatus(capability, requestedContractVersion);
        }
    }

    internal IReadOnlyList<InternalModuleStatus> GetSnapshot()
    {
        lock (_sync)
        {
            if (_snapshot == null)
            {
                _snapshot = new ReadOnlyCollection<InternalModuleStatus>(_modules.Values
                    .OrderBy(item => item.Definition.Id, StringComparer.Ordinal)
                    .Select(item => new InternalModuleStatus(item.Definition, item.State, item.ReasonCode,
                        _validation != null, item.Issues)).ToArray());
            }
            return _snapshot;
        }
    }

    private string ValidateDefinition(InternalModuleDefinition definition)
    {
        if (definition == null) { return "module.definition_missing"; }
        if (!IsValidId(definition.Id)) { return "module.id_invalid"; }
        if (definition.ContractVersion <= 0) { return "module.version_invalid"; }
        if (definition.Capabilities.Count > MaximumCapabilities || definition.RequiredModules.Count > MaximumModules)
        {
            return "module.definition_capacity";
        }
        HashSet<string> dependencies = new HashSet<string>(StringComparer.Ordinal);
        foreach (InternalModuleDependency dependency in definition.RequiredModules)
        {
            if (dependency == null || !IsValidId(dependency.ModuleId)) { return "module.dependency_invalid"; }
            if (dependency.RequiredContractVersion <= 0) { return "module.dependency_version_invalid"; }
            if (!dependencies.Add(dependency.ModuleId)) { return "module.dependency_duplicate:" + dependency.ModuleId; }
        }
        HashSet<string> capabilityIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (InternalCapabilityDefinition capability in definition.Capabilities)
        {
            if (capability == null || !IsValidId(capability.Id)) { return "capability.id_invalid"; }
            if (capability.ContractVersion <= 0) { return "capability.version_invalid"; }
            if (!capabilityIds.Add(capability.Id)) { return "capability.duplicate:" + capability.Id; }
            if (capability.RequiredFeatureBridgeIds.Count > MaximumModules) { return "capability.bridge_capacity"; }
            HashSet<string> bridges = new HashSet<string>(StringComparer.Ordinal);
            foreach (string bridgeId in capability.RequiredFeatureBridgeIds)
            {
                if (!IsValidId(bridgeId)) { return "capability.bridge_invalid"; }
                if (!bridges.Add(bridgeId)) { return "capability.bridge_duplicate:" + bridgeId; }
                try { if (!_bridgeKnown(bridgeId)) { return "capability.bridge_unknown:" + bridgeId; } }
                catch { return "capability.bridge_validation_failed:" + bridgeId; }
            }
        }
        return null;
    }

    private void BuildClosure(ModuleEntry module, HashSet<ModuleEntry> closure, HashSet<string> visited,
        List<string> path, HashSet<string> issues)
    {
        if (visited.Contains(module.Definition.Id)) { return; }
        path.Add(module.Definition.Id);
        foreach (InternalModuleDependency dependency in module.Definition.RequiredModules)
        {
            if (!_modules.TryGetValue(dependency.ModuleId, out ModuleEntry required))
            {
                issues.Add("module.dependency_missing:" + dependency.ModuleId);
                continue;
            }
            if (required.Definition.ContractVersion != dependency.RequiredContractVersion)
            {
                issues.Add("module.dependency_version_mismatch:" + dependency.ModuleId);
            }
            int cycleStart = path.IndexOf(dependency.ModuleId);
            if (cycleStart >= 0)
            {
                issues.Add("module.dependency_cycle:" + string.Join("->", path.Skip(cycleStart).Concat(new[] { dependency.ModuleId })));
                continue;
            }
            closure.Add(required);
            BuildClosure(required, closure, visited, path, issues);
        }
        path.RemoveAt(path.Count - 1);
        visited.Add(module.Definition.Id);
    }

    private static InternalCapabilityStatus GetLocalStatus(CapabilityEntry capability, int version)
    {
        ModuleEntry module = capability.Module;
        if (version <= 0 || capability.Definition.ContractVersion != version)
        {
            return Result(capability, version, InternalCapabilityState.VersionMismatch, "capability.version_mismatch");
        }
        if (module.Issues.Count != 0) { return Result(capability, version, InternalCapabilityState.Blocked, module.Issues[0]); }
        if (module.State == InternalModuleRuntimeState.NotInitialized)
        {
            return Result(capability, version, InternalCapabilityState.ModuleNotInitialized, module.ReasonCode);
        }
        if (module.State == InternalModuleRuntimeState.Unavailable)
        {
            return Result(capability, version, InternalCapabilityState.ModuleUnavailable, module.ReasonCode);
        }
        if (module.State == InternalModuleRuntimeState.Failed)
        {
            return Result(capability, version, InternalCapabilityState.ModuleFailed, module.ReasonCode);
        }
        foreach (ModuleEntry dependency in module.RequiredClosure)
        {
            if (dependency.State != InternalModuleRuntimeState.Ready)
            {
                return Result(capability, version, InternalCapabilityState.DependencyUnavailable,
                    "module.dependency_unavailable:" + dependency.Definition.Id);
            }
        }
        return Result(capability, version, InternalCapabilityState.Available, "capability.available");
    }

    private static InternalCapabilityStatus Result(CapabilityEntry capability, int version, InternalCapabilityState state, string reason)
    {
        return new InternalCapabilityStatus(capability.Definition.Id, capability.Module.Definition.Id,
            capability.Definition.ContractVersion, version, state, reason);
    }

    private static bool IsValidId(string id)
    {
        return !string.IsNullOrWhiteSpace(id) && id.Length <= 128
            && id.All(character => character >= 'a' && character <= 'z'
                || character >= 'A' && character <= 'Z' || character >= '0' && character <= '9'
                || character == '.' || character == '-' || character == '_');
    }

    private static string DefaultReason(InternalModuleRuntimeState state)
    {
        switch (state)
        {
            case InternalModuleRuntimeState.Ready: return "module.ready";
            case InternalModuleRuntimeState.Unavailable: return "module.unavailable";
            case InternalModuleRuntimeState.Failed: return "module.failed";
            default: return "module.not_initialized";
        }
    }
}
