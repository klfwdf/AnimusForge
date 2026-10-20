using System;
using System.Diagnostics;

namespace AnimusForge;

// The imported core uses the existing AF settings owner. It has no second MCM/JSON owner.
internal sealed class PromptCacheSettings
{
    internal static readonly PromptCacheSettings Instance = new PromptCacheSettings();
    internal bool EnableHierarchicalPromptSplit => true;
    internal bool EnableSceneCardSplit => true;
    internal bool EnableDynamicBlockDedup => true;
    internal int SceneTranscriptKeepTurns => 2;
    internal int SessionTtlMinutes => DuelSettings.GetSettings()?.ConversationPromptCacheTtlMinutes ?? 10;
    internal int DumpRawDynamicBlockCount { get; set; }
}

internal static class PromptCacheCoreTexts
{
    internal static string Get(string key) => ConversationRuntimeTexts.Prompt("AF_CONV_CacheCore_" + key);
}

// Original verbose calls are compiled out, including interpolation, on the request path.
// The host emits bounded English metadata and prefix fingerprints once per request.
internal static class PromptCacheLogger
{
    [Conditional("AF_PROMPT_CACHE_CORE_VERBOSE")] internal static void Log(string message) { }
    [Conditional("AF_PROMPT_CACHE_CORE_VERBOSE")] internal static void LogVerbose(string message) { }
    [Conditional("AF_PROMPT_CACHE_CORE_VERBOSE")] internal static void LogWarning(string message) { }
    internal static void LogError(string stage, Exception error)
        => Logger.Log("PromptCache", "core_failed type=" + error.GetType().Name);
}
internal static class CacheStatsLogger
{
    internal static void DumpRawBlock(string tag, string block) { }
}
