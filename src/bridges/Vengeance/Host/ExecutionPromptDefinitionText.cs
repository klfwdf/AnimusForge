using System;
using System.Collections.Generic;

namespace AnimusForge;

internal static class ExecutionPromptDefinitionText
{
    private static readonly Lazy<Dictionary<string, string>> Keys = new Lazy<Dictionary<string, string>>(
        () =>
        {
            var keys = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string id in new[]
            {
                "REX_Method_Beheading",
                "REX_Method_Beheading_Hint",
                "REX_Method_Hanging",
                "REX_Method_Hanging_Hint",
                "REX_Method_Burning",
                "REX_Method_Burning_Hint",
                "REX_Method_Breaking_Wheel",
                "REX_Method_Breaking_Wheel_Hint",
                "REX_Method_Impalement",
                "REX_Method_Impalement_Hint",
                "REX_Method_Stoning",
                "REX_Method_Stoning_Hint",
                "REX_Method_Crossbow_Execution",
                "REX_Method_Crossbow_Execution_Hint",
                "REX_Charge_Treason",
                "REX_Charge_Treason_Hint",
                "REX_Charge_Raiding",
                "REX_Charge_Raiding_Hint",
                "REX_Charge_Siege",
                "REX_Charge_Siege_Hint",
                "REX_Charge_Revenge",
                "REX_Charge_Revenge_Hint",
                "REX_Charge_Banditry",
                "REX_Charge_Banditry_Hint",
                "REX_Charge_Public_Enemy",
                "REX_Charge_Public_Enemy_Hint",
            })
            {
                keys[LlmSourceTexts.Get("ExecutionDefinition_Cn_" + id)] = "AF_LLM_PROMPT_ExecutionDefinition_" + id;
                keys[LlmSourceTexts.Get("ExecutionDefinition_En_" + id)] = "AF_LLM_PROMPT_ExecutionDefinition_" + id;
            }
            return keys;
        });

    // Only built-in prose is projected. Custom definitions retain their authored text.
    internal static string Project(string value) =>
        value != null && Keys.Value.TryGetValue(value, out string key)
            ? LlmPromptTexts.Get(key, value)
            : value;
}
