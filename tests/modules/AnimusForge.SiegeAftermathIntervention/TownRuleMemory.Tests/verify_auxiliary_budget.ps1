$ErrorActionPreference = 'Stop'
$repository = (Resolve-Path (Join-Path $PSScriptRoot '../../../..')).Path
$source = [IO.File]::ReadAllText((Join-Path $repository 'src/modules/AF.Module.Prompt/Configuration/AIConfigHandler.cs'))
function Get-ProductionMethod([string]$signature) {
    $start = $source.IndexOf($signature, [StringComparison]::Ordinal)
    if ($start -lt 0) { throw "Missing production method: $signature" }
    $end = $source.IndexOf("`n`t}", $start, [StringComparison]::Ordinal)
    if ($end -lt 0) { throw 'Missing method terminator' }
    return $source.Substring($start, $end + 3 - $start)
}
$methods = @(
    (Get-ProductionMethod 'private static int ResolveAuxiliaryApiMaxTokens('),
    (Get-ProductionMethod 'public static bool TryCallAuxiliarySimpleDialogueOnceForExternal('),
    (Get-ProductionMethod 'internal static bool TryCallBoundedAuxiliarySimpleDialogueOnceForExternal('),
    (Get-ProductionMethod 'private static bool TryCallAuxiliarySimpleDialogueOnce(')
) -join "`n"
# Source-derived business boundary. Network/config/log are stubs; actual gateway HTTP
# serialization and no-fallback behavior are covered by ConfiguredChatGatewayReplayTests.
$prefix = @'
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
namespace TownBudgetReplay {
public static class Probe {
'@
$suffix = @'
    private static int SentTokens;
    private static bool SentThinking, SentFallback;
    private static bool TryGetAuxiliarySimpleDialogueConfig(out string url, out string key, out string model)
    { url = "fixture"; key = ""; model = "fixture"; return true; }
    private static object[] CopyAuxiliaryChatMessagesPreservingNames(IEnumerable<object> messages) => messages.ToArray();
    private static string BuildAuxiliaryRouterExceptionText(Exception ex) => ex.Message;
    private static void LogAuxiliaryRouterTokenTrace(string source, object[] messages, string result, int tokens) { }
    private static LlmGenerateResult GenerateConfiguredGatewayResult(IEnumerable<object> messages, string url,
        string key, string model, int maxTokens, float temperature, bool thinkingEnabled, string reasoningEffort,
        int timeout, string source, InteractionStage stage, bool allowThinkingControlFallback = true)
    {
        SentTokens = maxTokens; SentThinking = thinkingEnabled; SentFallback = allowThinkingControlFallback;
        return new LlmGenerateResult { Status = LlmResultStatus.Succeeded, RawText = "fixture result" };
    }
    public static string Run()
    {
        object[] messages = { "fixture" };
        DuelSettings.Configured = 16000;
        if (!TryCallBoundedAuxiliarySimpleDialogueOnceForExternal(messages, 384, 0.45f, out _, out _)
            || SentTokens != 384 || SentThinking || SentFallback) throw new Exception("bounded budget overridden");
        DuelSettings.Configured = 128;
        TryCallBoundedAuxiliarySimpleDialogueOnceForExternal(messages, 384, 0.45f, out _, out _);
        if (SentTokens != 128) throw new Exception("lower user limit ignored");
        TryCallBoundedAuxiliarySimpleDialogueOnceForExternal(messages, 0, 0.45f, out _, out _);
        if (SentTokens != 16) throw new Exception("minimum budget invalid");
        DuelSettings.Configured = 16000;
        TryCallAuxiliarySimpleDialogueOnceForExternal(messages, 384, 0.45f, out _, out _);
        if (SentTokens != 16000 || !SentFallback) throw new Exception("legacy caller behavior changed");
        return "PASS: actual auxiliary methods clamp 16000->384, preserve lower 128 and floor 16, disable thinking/fallback only for bounded call; legacy 16000/fallback retained.";
    }
}
internal class DuelSettings {
    internal static int Configured;
    internal const int DefaultGeneralApiMaxTokens = 4096, LlmRequestTimeoutMilliseconds = 1000;
    internal const string ReasoningEffortHigh = "high";
    internal static DuelSettings GetSettings() => new DuelSettings();
    internal int GetAuxiliaryApiMaxTokens() => Configured;
    internal float GetAuxiliaryApiTemperature() => 0.45f;
    internal static int ClampApiMaxTokens(int value, int fallback) => Math.Max(16, value);
}
internal static class Logger { internal static void RecordMessageDump(string kind, object[] data, string name) { } internal static int EstimateTokens(string s) => s.Length; }
internal static class FreezeWatchdog { internal static void Mark(string source, string text, bool immediate) { } }
internal static class LlmRetryPrompt { internal static string BuildFailureDetail(string a,string b,string c) => a; }
internal enum InteractionStage { MainReply }
internal enum LlmResultStatus { Succeeded }
internal class LlmGenerateResult { internal LlmResultStatus Status; internal string ErrorCode { get; set; } internal string RawText; }
}
'@
Add-Type -TypeDefinition ($prefix + "`n" + $methods + "`n" + $suffix)
[TownBudgetReplay.Probe]::Run()
