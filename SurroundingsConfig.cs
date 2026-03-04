using System.Collections.Generic;

namespace AnimusForge;

public class SurroundingsConfig
{
	public bool IsEnabled { get; set; } = false;

	public string Instruction { get; set; } = "";

	public List<string> TriggerKeywords { get; set; } = new List<string>();
}
