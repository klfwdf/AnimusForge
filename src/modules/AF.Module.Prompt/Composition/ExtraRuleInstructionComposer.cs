using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
namespace AnimusForge.Refactor.Modules;
internal static class ExtraRuleInstructionComposer
{
    internal static string ResolveSelectedInstruction(string ruleId,string body,bool hasAnyHero,string runtime,string heroJoinRuntime = null)
    {
        string id=(ruleId ?? "").Trim();
        if (string.IsNullOrWhiteSpace(id)) return "";
        string text=(body ?? "").Trim();
        if (string.Equals(id,"kingdom_service",StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(runtime) || !string.IsNullOrWhiteSpace(heroJoinRuntime))
                text=string.Join("\n",new[]{text,runtime,heroJoinRuntime}.Where(x=>!string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase)).Trim();
        }
        else if (hasAnyHero && string.Equals(id,"diplomacy",StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(runtime)) text=string.Join("\n",new[]{text,runtime}.Where(x=>!string.IsNullOrWhiteSpace(x))).Trim();
        }
        else if (hasAnyHero && string.Equals(id,"vanilla_issue",StringComparison.OrdinalIgnoreCase)) text=runtime ?? "";
        else if (!string.IsNullOrWhiteSpace(runtime)) text=runtime;
        return text;
    }
    internal static string AssembleMatchedInstructions(IEnumerable<KeyValuePair<string,string>> captured)
    {
        var sb=new StringBuilder();
        foreach (var pair in captured) PromptRuleBlockText.Append(sb,pair.Key,pair.Value);
        return sb.ToString().Trim();
    }
}
