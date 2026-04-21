using System.Collections.Generic;

namespace AnimusForge;

public class RewardConfig
{
	public bool IsEnabled { get; set; } = false;

	public int TopicNumber { get; set; } = 0;

	public string TopicLabel { get; set; } = "";

	public string Instruction { get; set; } = "";

	public string NonHeroInstruction { get; set; } = "";

	public List<string> TriggerKeywords { get; set; } = new List<string>();

	public Dictionary<string, string> RuntimeInstructionTemplates { get; set; } = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
}
