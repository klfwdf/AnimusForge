using System.Collections.Generic;

namespace Voxforge;

public class GuardrailRulePromptConfig
{
	public string Id { get; set; } = "";

	public bool IsEnabled { get; set; } = true;

	public string Group { get; set; } = "";

	public int Priority { get; set; } = 0;

	public string Instruction { get; set; } = "";

	public string NonHeroInstruction { get; set; } = "";

	public List<string> TriggerKeywords { get; set; } = new List<string>();
}
