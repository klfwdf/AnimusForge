using System.Collections.Generic;

namespace AnimusForge;

public sealed class ProactiveNpcRequestPromptsConfigModel
{
	public int Version { get; set; } = 1;

	public ProactiveNpcRequestPromptEntry Default { get; set; } = new ProactiveNpcRequestPromptEntry();

	public Dictionary<string, ProactiveNpcRequestPromptEntry> Requests { get; set; } = new Dictionary<string, ProactiveNpcRequestPromptEntry>();
}

public sealed class ProactiveNpcRequestPromptEntry
{
	public string OpeningPrompt { get; set; } = "";

	public string LetterIntent { get; set; } = "";

	public string CompanionIntent { get; set; } = "";

	public string NaturalExpressionGuide { get; set; } = "";
}
