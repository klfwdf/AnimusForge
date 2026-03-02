using System.Collections.Generic;

namespace Voxforge;

public class RewardConfig
{
	public bool IsEnabled { get; set; } = false;

	public string Instruction { get; set; } = "";

	public string NonHeroInstruction { get; set; } = "";

	public List<string> TriggerKeywords { get; set; } = new List<string>();
}
