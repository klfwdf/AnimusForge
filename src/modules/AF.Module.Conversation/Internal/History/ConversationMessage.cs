using System.Collections.Generic;

namespace AnimusForge;

public class ConversationMessage
{
	// Request-only provenance; never part of the persisted/public JSON contract.
	internal string PromptMemorySessionKey = "";
    internal string PromptSourceId = "", PromptContextRevision = "";
    internal bool PromptIsRecall;
    internal bool PromptFactScopeCaptured, PromptIsCurrentFact;

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

	public string TargetName { get; set; } = "";

	public string TargetHeroId { get; set; } = "";

	public float PlayerDistanceMeters { get; set; } = -1f;

	public List<int> VisibleAgentIndices { get; set; } = new List<int>();

	public List<string> VisibleHeroIds { get; set; } = new List<string>();
}

// Request-only metadata. Provider serializers consume the public role/content properties.
internal sealed class ConversationPromptMessage
{
    public string role { get; }
    public string content { get; }
    internal readonly int HistoryGroup;
    internal readonly string Revision;
    internal ConversationPromptMessage(string role, string content, int historyGroup = 0, string revision = "")
    { this.role = role; this.content = content; HistoryGroup = historyGroup; Revision = revision ?? ""; }
}
