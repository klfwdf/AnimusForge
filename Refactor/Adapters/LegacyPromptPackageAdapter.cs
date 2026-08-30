using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using AnimusForge.Refactor.Contracts;

namespace AnimusForge.Refactor.Adapters;

/// <summary>
/// Converts the legacy role/content message shape at the interaction
/// boundary. It deliberately copies only strings into PromptPackage and never
/// carries a legacy message object, game object, or mutable collection.
/// </summary>
public static class LegacyPromptPackageAdapter
{
    public static PromptPackage FromLegacyMessages(
        IEnumerable<object> messages,
        int maxTokens,
        string model)
    {
        List<PromptMessage> copied = new List<PromptMessage>();
        foreach (object message in messages ?? Enumerable.Empty<object>())
        {
            if (!TryReadMessage(message, out string role, out string content)
                || string.IsNullOrWhiteSpace(content))
            {
                continue;
            }
            copied.Add(new PromptMessage(NormalizeRole(role), content));
        }
        return new PromptPackage(copied, Math.Max(16, maxTokens), string.IsNullOrWhiteSpace(model) ? "legacy" : model.Trim());
    }

    public static IReadOnlyList<object> ToLegacyMessages(PromptPackage prompt)
    {
        if (prompt == null)
        {
            throw new ArgumentNullException(nameof(prompt));
        }
        List<object> result = new List<object>();
        foreach (PromptMessage message in prompt.Messages)
        {
            result.Add(new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["role"] = NormalizeRole(message?.Role),
                ["content"] = message?.Content ?? string.Empty
            });
        }
        return new ReadOnlyCollection<object>(result);
    }

    private static bool TryReadMessage(object message, out string role, out string content)
    {
        role = string.Empty;
        content = string.Empty;
        if (message is IDictionary<string, object> objectMap)
        {
            role = ReadValue(objectMap, "role");
            content = ReadValue(objectMap, "content");
            return true;
        }
        if (message is IDictionary<string, string> stringMap)
        {
            stringMap.TryGetValue("role", out role);
            stringMap.TryGetValue("content", out content);
            return true;
        }
        return false;
    }

    private static string ReadValue(IDictionary<string, object> map, string key)
    {
        return map.TryGetValue(key, out object value) ? value?.ToString() ?? string.Empty : string.Empty;
    }

    private static string NormalizeRole(string role)
    {
        string normalized = (role ?? string.Empty).Trim().ToLowerInvariant();
        return normalized == "system" || normalized == "assistant" || normalized == "user"
            ? normalized
            : "user";
    }
}
