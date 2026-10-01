using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace AnimusForge;

public sealed class PreprocessPromptsConfigModel
{
	public int Version { get; set; }

	public Dictionary<string, string> TemplateVariables { get; set; } = new Dictionary<string, string>();

	public StrictPreprocessPromptConfig StrictJson { get; set; } = new StrictPreprocessPromptConfig();

	public TopicRoutingPreprocessPromptConfig TopicRouting { get; set; } = new TopicRoutingPreprocessPromptConfig();

	public MemorySelectionPreprocessPromptConfig MemorySelection { get; set; } = new MemorySelectionPreprocessPromptConfig();

	public ConnectionTestPreprocessPromptConfig ConnectionTest { get; set; } = new ConnectionTestPreprocessPromptConfig();
}

public sealed class StrictPreprocessPromptConfig
{
	public string SystemPrompt { get; set; } = "";

	public JObject MentionedEntitiesSchema { get; set; } = new JObject();
}

public sealed class TopicRoutingPreprocessPromptConfig
{
	public string RoutingGuidance { get; set; } = "";

	public string EmptyValue { get; set; } = "";

	public string UserPromptTemplate { get; set; } = "";
}

public sealed class MemorySelectionPreprocessPromptConfig
{
	public string ParallelModeInstruction { get; set; } = "";

	public string UnifiedModeInstruction { get; set; } = "";

	public string EmptyValue { get; set; } = "";

	public string UserPromptTemplate { get; set; } = "";

	public string CandidateLineTemplate { get; set; } = "";

	public string FallbackGameDateTemplate { get; set; } = "";
}

public sealed class ConnectionTestPreprocessPromptConfig
{
	public string ExpectedRuleCode { get; set; } = "";

	public string UserPromptTemplate { get; set; } = "";
}
