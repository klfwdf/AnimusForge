using System.Collections.Generic;

namespace AnimusForge;

public class DuelConfig
{
	public bool IsEnabled { get; set; } = false;

	public string TriggerInstruction { get; set; } = "";

	public string NonHeroInstruction { get; set; } = "";

	public List<string> AcceptKeywords { get; set; } = new List<string>();
}
