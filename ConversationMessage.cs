using System.Collections.Generic;

namespace AnimusForge;

public class ConversationMessage
{
	public long EventSequence { get; set; }

	public int GameDayIndex { get; set; } = -1;

	public string GameDate { get; set; } = "";

	public int GameHour { get; set; } = -1;

	public string Scene { get; set; } = "";

	public string Role { get; set; }

	public string Content { get; set; }

	public string SpeakerName { get; set; }

	public int SpeakerAgentIndex { get; set; } = -1;

	public string SpeakerHeroId { get; set; } = "";

	public int TargetAgentIndex { get; set; } = -1;

	public string TargetName { get; set; }

	public string TargetHeroId { get; set; } = "";

	public float PlayerDistanceMeters { get; set; } = -1f;

	public List<int> VisibleAgentIndices { get; set; } = new List<int>();

	public List<string> VisibleHeroIds { get; set; } = new List<string>();
}
