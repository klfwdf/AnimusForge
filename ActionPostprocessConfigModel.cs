using System.Collections.Generic;

namespace AnimusForge;

public class ActionPostprocessConfigModel
{
	public int Version { get; set; } = 1;

	public bool IsEnabled { get; set; } = true;

	public string SystemPrompt { get; set; } = "";

	public string UserPromptTemplate { get; set; } = "";

	public string FallbackMoodTag { get; set; } = "[ACTION:MOOD:NEUTRAL]";

	public List<PostprocessRuleEntry> MoodRules { get; set; } = new List<PostprocessRuleEntry>();
}
