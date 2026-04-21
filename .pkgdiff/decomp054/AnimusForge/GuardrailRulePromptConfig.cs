using System;
using System.Collections.Generic;

namespace AnimusForge;

public class GuardrailRulePromptConfig
{
	public string Id { get; set; } = "";

	public bool IsEnabled { get; set; } = true;

	public string Group { get; set; } = "";

	public int Priority { get; set; } = 0;

	public string Instruction { get; set; } = "";

	public string NonHeroInstruction { get; set; } = "";

	public List<string> TriggerKeywords { get; set; } = new List<string>();

	public Dictionary<string, string> RuntimeInstructionTemplates { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	public Dictionary<string, string> RuntimeConstraintTemplates { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
