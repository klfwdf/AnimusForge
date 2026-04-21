using System.Collections.Generic;

namespace AnimusForge;

public class SurroundingsConfig
{
	public bool IsEnabled { get; set; } = false;

	public int TopicNumber { get; set; } = 0;

	public string TopicLabel { get; set; } = "";

	public string Instruction { get; set; } = "";

	public List<string> TriggerKeywords { get; set; } = new List<string>();
}
