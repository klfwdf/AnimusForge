using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace AnimusForge;

internal static class AnimusForgeModulePaths
{
    internal static string GetModuleDataFilePath(string name) => name;
}

internal static class Logger
{
    internal static void Log(string category, string message) { }
}

public class AIConfigModel { public string Marker { get; set; } = "default"; }
public class GuardrailConfigModel
{
    public string Marker { get; set; } = "default";
    public DuelConfig Duel { get; set; } = new DuelConfig();
    public RewardConfig Reward { get; set; } = new RewardConfig();
    public LoanConfig Loan { get; set; } = new LoanConfig();
    public SurroundingsConfig Surroundings { get; set; } = new SurroundingsConfig();
    public List<GuardrailRulePromptConfig> RulePrompts { get; set; } = new List<GuardrailRulePromptConfig>();
}
public class DuelConfig
{
    public bool IsEnabled { get; set; } = true;
    public string TriggerInstruction { get; set; } = "";
    public string DialogueInstruction { get; set; } = "";
    public List<string> AcceptKeywords { get; set; } = new List<string>();
    public int TopicNumber { get; set; }
    public string TopicLabel { get; set; } = "";
    public string Code { get; set; } = "";
    public string PreprocessExcludedInstruction { get; set; } = "";
}
public class RewardConfig
{
    public bool IsEnabled { get; set; } = true;
    public string Instruction { get; set; } = "";
    public List<string> TriggerKeywords { get; set; } = new List<string>();
    public int TopicNumber { get; set; }
    public string TopicLabel { get; set; } = "";
    public string Code { get; set; } = "";
    public string PreprocessExcludedInstruction { get; set; } = "";
}
public class LoanConfig : RewardConfig { }
public class SurroundingsConfig : RewardConfig { }
public class PostprocessRuleEntry
{
    public string Tag { get; set; } = "";
    public string Description { get; set; } = "";
    public string SingleFramedNpcDescription { get; set; } = "";
}
public class GuardrailRulePromptConfig
{
    public string Id { get; set; } = "";
    public bool IsEnabled { get; set; } = true;
    public string Group { get; set; } = "";
    public int Priority { get; set; }
    public int TopicNumber { get; set; }
    public string TopicLabel { get; set; } = "";
    public string Code { get; set; } = "";
    public string Instruction { get; set; } = "";
    public string NonHeroInstruction { get; set; } = "";
    public string PreprocessExcludedInstruction { get; set; } = "";
    public List<PostprocessRuleEntry> PostprocessRules { get; set; } = new List<PostprocessRuleEntry>();
    public List<string> TriggerKeywords { get; set; } = new List<string>();
    public Dictionary<string, string> RuntimeInstructionTemplates { get; set; } = new Dictionary<string, string>();
    public Dictionary<string, string> RuntimeConstraintTemplates { get; set; } = new Dictionary<string, string>();
}
public class ActionPostprocessConfigModel { public string Marker { get; set; } = "default"; }
public class ProactiveNpcRequestPromptsConfigModel { public string Marker { get; set; } = "default"; }
public class RpItemIntroductionPromptsConfigModel
{
    public int Version { get; set; } = 1;
    public string SystemPrompt { get; set; } = "";
    public string UserPromptTemplate { get; set; } = "";
}
public class PreprocessPromptsConfigModel
{
    public int Version { get; set; }
    public StrictJsonConfig StrictJson { get; set; } = new StrictJsonConfig();
    public TopicRoutingConfig TopicRouting { get; set; } = new TopicRoutingConfig();
    public MemorySelectionConfig MemorySelection { get; set; } = new MemorySelectionConfig();
    public ConnectionTestConfig ConnectionTest { get; set; } = new ConnectionTestConfig();
}
public class StrictJsonConfig { public string SystemPrompt { get; set; } = ""; public JObject MentionedEntitiesSchema { get; set; } = new JObject(); }
public class TopicRoutingConfig { public string EmptyValue { get; set; } = ""; public string UserPromptTemplate { get; set; } = ""; }
public class MemorySelectionConfig
{
    public string ParallelModeInstruction { get; set; } = "";
    public string UnifiedModeInstruction { get; set; } = "";
    public string EmptyValue { get; set; } = "";
    public string UserPromptTemplate { get; set; } = "";
    public string CandidateLineTemplate { get; set; } = "";
    public string FallbackGameDateTemplate { get; set; } = "";
}
public class ConnectionTestConfig { public string ExpectedRuleCode { get; set; } = ""; public string UserPromptTemplate { get; set; } = ""; }
