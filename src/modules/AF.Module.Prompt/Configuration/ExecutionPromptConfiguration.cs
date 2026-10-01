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
        using var stream = typeof(ExecutionPromptConfiguration).Assembly.GetManifestResourceStream("AnimusForge.Defaults.ExecutionRulePrompts.json");
        if (stream == null) return new JObject();
        using var reader = new StreamReader(stream);
        return JObject.Parse(reader.ReadToEnd());
    });
    internal static string SystemPrompt => (string)Defaults.Value["ExecutionCeremonySystemPrompt"] ?? "";
    internal static GuardrailRulePromptConfig CreateOrderRule() => Defaults.Value["RulePrompts"]?
        .FirstOrDefault(x => (string)x["Id"] == "public_execution_start")?.ToObject<GuardrailRulePromptConfig>();
}
