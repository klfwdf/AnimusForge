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
        if (Regex.IsMatch(text, "[?？\"'‘’“”「」『』]|不要|别|不许|不得|停止|取消|暂缓|且慢|等|稍后|如果|假如|再说|谁|何时|什么时候|是否|为何|为什么|怎么|说过|所谓|吗|么|能否|可否|\\b(don['’]?t|do not|not|never|stop|wait|after|until|if|who|when|why|whether)\\b", RegexOptions.IgnoreCase)) return false;
        return Regex.IsMatch(text, "行刑|执行|动手|斩|砍|处决|开始|\\b(execute|proceed|carry out|do it|begin)\\b", RegexOptions.IgnoreCase);
    }
    internal static bool ReplyRefusesImmediateOrder(string text) => Regex.IsMatch(text ?? "",
        "不能|不行刑|不执行|不会动手|不可|拒绝|无法|暂缓|稍后|先等|等他说|\\b(cannot|can['’]?t|won['’]?t|refuse|wait|not yet)\\b", RegexOptions.IgnoreCase);
}
