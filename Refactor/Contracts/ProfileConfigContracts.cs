using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace AnimusForge.Refactor.Contracts;

public sealed class RuntimeConfigSnapshot
{
    public RuntimeConfigSnapshot(
        string profileId,
        long configurationGeneration,
        IDictionary<string, bool> enabledModules,
        IDictionary<string, LlmProviderSnapshot> providers)
    {
        ProfileId = ContractGuard.Required(profileId, nameof(profileId));
        ConfigurationGeneration = configurationGeneration;
        EnabledModules = CopyFlags(enabledModules);
        Providers = CopyProviders(providers);
    }

    public string ProfileId { get; }
    public long ConfigurationGeneration { get; }
    public IReadOnlyDictionary<string, bool> EnabledModules { get; }
    public IReadOnlyDictionary<string, LlmProviderSnapshot> Providers { get; }

    public bool IsModuleEnabled(string moduleId)
    {
        return !string.IsNullOrWhiteSpace(moduleId)
            && EnabledModules.TryGetValue(moduleId.Trim(), out bool enabled)
            && enabled;
    }

    public bool TryGetProvider(string providerId, out LlmProviderSnapshot provider)
    {
        provider = null;
        return !string.IsNullOrWhiteSpace(providerId)
            && Providers.TryGetValue(providerId.Trim(), out provider);
    }

    private static IReadOnlyDictionary<string, bool> CopyFlags(IDictionary<string, bool> values)
    {
        Dictionary<string, bool> copy = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        if (values != null)
        {
            foreach (KeyValuePair<string, bool> pair in values)
            {
                if (!string.IsNullOrWhiteSpace(pair.Key))
                {
                    copy[pair.Key.Trim()] = pair.Value;
                }
            }
        }

        return new ReadOnlyDictionary<string, bool>(copy);
    }

    private static IReadOnlyDictionary<string, LlmProviderSnapshot> CopyProviders(IDictionary<string, LlmProviderSnapshot> values)
    {
        Dictionary<string, LlmProviderSnapshot> copy = new Dictionary<string, LlmProviderSnapshot>(StringComparer.OrdinalIgnoreCase);
        if (values != null)
        {
            foreach (KeyValuePair<string, LlmProviderSnapshot> pair in values)
            {
                if (!string.IsNullOrWhiteSpace(pair.Key) && pair.Value != null)
                {
                    copy[pair.Key.Trim()] = pair.Value;
                }
            }
        }

        return new ReadOnlyDictionary<string, LlmProviderSnapshot>(copy);
    }
}
