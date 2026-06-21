namespace AnimusForge;

public sealed class AnimusForgeDialogueHistoryEntry
{
	public long EventSequence { get; set; }

	public int GameDayIndex { get; set; }

	public string GameDate { get; set; } = "";

	public int GameHour { get; set; } = -1;

	public string Scene { get; set; } = "";

	public string Speaker { get; set; } = "";

	public string Text { get; set; } = "";

	public string Kind { get; set; } = "";
}
