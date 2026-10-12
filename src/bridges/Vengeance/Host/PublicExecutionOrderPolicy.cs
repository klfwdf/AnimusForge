using System;
using System.Text.RegularExpressions;

namespace AnimusForge;

internal static class PublicExecutionOrderPolicy
{
    internal const string RuleId = "public_execution_start";
    internal const string Tag = "[ACTION:PUBLIC_EXECUTION_START]";
    // A defensive veto, not a substitute for the shared semantic postprocessor.
    internal static bool AllowsImmediateOrder(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > 2000) return false;
        if (Regex.IsMatch(text, LlmSourceTexts.Get("ExecutionBridge_OrderRecognition_001"), RegexOptions.IgnoreCase)) return false;
        return Regex.IsMatch(text, LlmSourceTexts.Get("ExecutionBridge_OrderRecognition_002"), RegexOptions.IgnoreCase);
    }
    internal static bool ReplyRefusesImmediateOrder(string text) => Regex.IsMatch(text ?? "",
        LlmSourceTexts.Get("ExecutionBridge_OrderRecognition_003"), RegexOptions.IgnoreCase);
}
