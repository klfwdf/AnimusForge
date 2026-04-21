using System.Collections.Generic;

namespace AnimusForge;

public class ConversationMessage
{
	public string Role { get; set; }

	public string Content { get; set; }

	public string SpeakerName { get; set; }

	public int SpeakerAgentIndex { get; set; } = -1;

	public int TargetAgentIndex { get; set; } = -1;

	public string TargetName { get; set; }

	public List<int> VisibleAgentIndices { get; set; } = new List<int>();
}
