using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace AnimusForge;

// Missing-field defaults for existing user configurations. Never rewrites their file.
internal static class ExecutionPromptConfiguration
{
    private static readonly Lazy<JObject> Defaults = new Lazy<JObject>(() =>
    {
        string path = AnimusForgeModulePaths.GetModuleDataFilePath("ExecutionRulePrompts.json");
        return JObject.Parse(File.ReadAllText(path, System.Text.Encoding.UTF8));
    });
    internal static string SystemPrompt => (string)Defaults.Value["ExecutionCeremonySystemPrompt"] ?? "";
    internal static GuardrailRulePromptConfig CreateOrderRule() => Defaults.Value["RulePrompts"]?
        .FirstOrDefault(x => (string)x["Id"] == "public_execution_start")?.ToObject<GuardrailRulePromptConfig>();
}
