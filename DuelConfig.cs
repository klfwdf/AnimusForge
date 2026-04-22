using System.Collections.Generic;

namespace AnimusForge;

public class DuelConfig
{
	public bool IsEnabled { get; set; } = false;

	public int TopicNumber { get; set; } = 0;

	public string TopicLabel { get; set; } = "";

	public string DialogueInstruction { get; set; } = "";

	public string TriggerInstruction { get; set; } = "";

	public string LegacyFollowupInstruction { get; set; } = "";

	public string NonHeroInstruction { get; set; } = "";

	public List<string> AcceptKeywords { get; set; } = new List<string>();

	public List<PostprocessRuleEntry> PostprocessRules { get; set; } = new List<PostprocessRuleEntry>();
}
