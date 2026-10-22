using System;

namespace AnimusForge;

// Shared game/configuration boundary fixtures for PromptCacheCore-linked tests.
// Production store, adapter, text readers, message reader and protocol code are
// linked from source without edits.
internal static class AnimusForgeModulePaths
{
    internal static string GetModuleDataFilePath(string name) => System.IO.Path.Combine(AppContext.BaseDirectory,
        name == "ConversationRuntimePrompts.json" && Environment.GetEnvironmentVariable("AF_TEST_EDITION") == "EN"
            ? "ConversationRuntimePrompts.en.json" : name);
}

internal sealed class DuelSettings
{
    internal static readonly DuelSettings Settings = new DuelSettings();
    internal static DuelSettings GetSettings() => Settings;
    internal bool ConversationContextEnabled = true;
    internal int ConversationContextRecentTurns = 12, ConversationContextRecentChars = 20000, ConversationContextInputChars = 100000, ConversationContextEstimatedTokens = 128000;
    internal int GetAuxiliaryApiMaxTokens() => 12000;
    internal int MainApiMaxTokens = 12000;
    internal int GetMainApiMaxTokens() => MainApiMaxTokens;
    internal string ApiUrl = "https://relay.test/v1/chat/completions", Model = "gemini-test";
    internal string AuxiliaryApiUrl = "https://auxiliary.test/v1/chat/completions";
    internal string GetEffectiveAuxiliaryModelName() => "auxiliary-model";
    internal string GetEffectiveMainModelName() => Model;
}

internal static class ShoutBehavior { internal static string AppendPlayerCustomPromptRuleToSystemPrompt(string s) => "GLOBAL"; }
internal static class SaveRuntimeGuard { internal static long Generation = 1; internal static bool IsCurrentGeneration(long g) => g == Generation; }
internal static class Logger { internal static void Log(string c, string t) { } }
internal static class IntradayConversationMemoryRuntime { internal static void Clear() { } }

internal static class ConversationContextSettingsCapture
{
    internal static ConversationContextOptions Capture() => new ConversationContextOptions {
        Enabled = DuelSettings.Settings.ConversationContextEnabled, InputChars = DuelSettings.Settings.ConversationContextInputChars };
}
