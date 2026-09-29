namespace AnimusForge;

public sealed class AnimusForgeDialogueHistoryEntry
{
	public long EventSequence { get; set; }

	public int GameDayIndex { get; set; }

	public string GameDate { get; set; } = "";

	public int GameHour { get; set; } = -1;

	public string Scene { get; set; } = "";

	public string Speaker { get; set; } = "";

	public int TargetAgentIndex { get; set; } = -1;

	public string TargetName { get; set; } = "";

	public string Text { get; set; } = "";

	public string Kind { get; set; } = "";

	// Set only for entries read from persisted history; identifies the line for deletion.
	// Session-only entries keep the defaults and are not deletable.
	public string MemoryId { get; set; } = "";

	public int LineOrdinal { get; set; } = -1;
}
