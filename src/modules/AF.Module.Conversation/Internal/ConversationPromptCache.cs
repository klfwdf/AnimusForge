using System;
using System.Collections.Generic;
using System.Globalization;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using Newtonsoft.Json.Linq;

namespace AnimusForge;

// Typed main/auxiliary boundaries; no global response interception or FIFO attribution.
internal static class ConversationPromptCache
{
    private static readonly ConversationPromptCacheStore Store = new ConversationPromptCacheStore();
    private static readonly ConcurrentDictionary<Type, Func<object, string>[]> Accessors =
        new ConcurrentDictionary<Type, Func<object, string>[]>();
    internal static void Clear() { Store.Clear(); IntradayConversationMemoryRuntime.Clear(); }

    internal sealed class Request : IDisposable
    {
        internal readonly List<object> Messages;
        private readonly ConversationPromptCacheStore.Lease _lease;
        private readonly long _generation;
        internal Request(List<object> messages, ConversationPromptCacheStore.Lease lease, long generation)
        { Messages = messages; _lease = lease; _generation = generation; }
        internal void Accept(string reply, bool sourceCurrent = true)
            => _lease?.Commit(reply, sourceCurrent && SaveRuntimeGuard.IsCurrentGeneration(_generation), DateTime.UtcNow);
        public void Dispose() => _lease?.Dispose();
    }

    internal static Request Begin(List<object> messages, string channel, long generation, int sessionId, string actor)
    {
        ConversationPromptCacheStore.Lease lease = null;
        try
        {
            DuelSettings settings = DuelSettings.GetSettings();
            if (!SaveRuntimeGuard.IsCurrentGeneration(generation)) return new Request(messages, null, generation);
            var original = new List<PromptCacheMessage>();
            foreach (object message in messages)
            {
                if (!TryRead(message, out string role, out string content)
                    || (role != "system" && role != "user" && role != "assistant") || content == null)
                    return new Request(messages, null, generation);
                var metadata = message as ConversationPromptMessage;
                original.Add(new PromptCacheMessage(role, content, metadata?.HistoryGroup ?? 0, metadata?.Revision ?? ""));
            }
            // History selection/compression is upstream. Final request estimates are
            // diagnostics only and must not prevent reuse of this stable prompt prefix.
            if (settings == null) return new Request(messages, null, generation);
            var text = new PromptCacheText
            {
                Policy = "[AF_PROMPT_CACHE_V1]\n" + ConversationRuntimeTexts.Prompt("AF_CONV_Cache_Policy"),
                Snapshot = ConversationRuntimeTexts.Prompt("AF_CONV_Cache_Snapshot"),
                Section = ConversationRuntimeTexts.Prompt("AF_CONV_Cache_Section"),
                Input = ConversationRuntimeTexts.Prompt("AF_CONV_Cache_Input")
            };
            // The system already contains this exact source-owned wrapper and rule.
            string globalRule = ShoutBehavior.AppendPlayerCustomPromptRuleToSystemPrompt("");
            bool auxiliary = channel == "scene_auxiliary";
            string provider = auxiliary
                ? settings.AuxiliaryApiUrl + "\n" + settings.GetEffectiveAuxiliaryModelName()
                : settings.ApiUrl + "\n" + settings.GetEffectiveMainModelName();
            // The public system text is byte-identical across scene speakers, while
            // leases and persona/history snapshots remain isolated per viewer.
            bool sceneTranscript = channel == "scene";
            string key = channel + "\n" + sessionId.ToString(CultureInfo.InvariantCulture) + "\n" + provider
                + "\n" + actor;
            lease = Store.Begin(generation, key, actor, original, globalRule, text,
                PromptCacheSettings.MaxTranscriptCharacters,
                PromptCacheSettings.DefaultSessionTtlMinutes, DateTime.UtcNow,
                sceneTranscript: sceneTranscript);
            var built = new List<object>(lease.Messages.Count);
            foreach (var message in lease.Messages) built.Add(new { role = message.Role, content = message.Content });
            Logger.Log("PromptCache", "channel=" + channel + " session=" + sessionId + " actor=" + actor
                + " mode=" + lease.Reason + " warm=" + lease.Warm + " chars=" + lease.Chars + " messages=" + built.Count
                + " prefix_append=" + lease.PrefixAppend + " shared_chars=" + lease.SharedChars + " core=latest_snapshot");
            return new Request(built, lease, generation);
        }
        catch (Exception ex)
        {
            lease?.Dispose();
            Logger.Log("PromptCache", "build_failed type=" + ex.GetType().Name + "; using complete original request");
            return new Request(messages, null, generation);
        }
    }

    private static bool TryRead(object message, out string role, out string content)
    {
        role = content = null;
        if (message == null) return false;
        if (message is JObject json)
        {
            if (json["role"]?.Type != JTokenType.String || json["content"]?.Type != JTokenType.String) return false;
            role = json["role"].Value<string>(); content = json["content"].Value<string>();
        }
        else if (message is IDictionary<string, object> dictionary)
        {
            if (!dictionary.TryGetValue("role", out var r) || !dictionary.TryGetValue("content", out var c)
                || !(r is string) || !(c is string)) return false;
            role = (string)r; content = (string)c;
        }
        else
        {
            var getters = Accessors.GetOrAdd(message.GetType(), type =>
            {
                var result = new Func<object, string>[2];
                string[] names = { "role", "content" };
                for (int i = 0; i < names.Length; i++)
                {
                    var property = type.GetProperty(names[i]) ?? type.GetProperty(i == 0 ? "Role" : "Content");
                    if (property == null || property.PropertyType != typeof(string)) continue;
                    var value = Expression.Parameter(typeof(object), "message");
                    result[i] = Expression.Lambda<Func<object, string>>(
                        Expression.Property(Expression.Convert(value, type), property), value).Compile();
                }
                return result;
            });
            if (getters[0] == null || getters[1] == null) return false;
            role = getters[0](message); content = getters[1](message);
        }
        return role != null && content != null;
    }
}
