using System.Collections.Generic;

namespace AnimusForge;

public class LoanConfig
{
	public bool IsEnabled { get; set; } = false;

	public string Instruction { get; set; } = "";

	public string NonHeroInstruction { get; set; } = "";

	public List<string> TriggerKeywords { get; set; } = new List<string>();

	public Dictionary<string, string> RuntimeInstructionTemplates { get; set; } = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
}
