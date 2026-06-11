namespace AnimusForge;

public sealed class AnimusForgeDialogueHistoryEntry
{
	public int GameDayIndex { get; set; }

	public string GameDate { get; set; } = "";

	public string Speaker { get; set; } = "";

	public string Text { get; set; } = "";

	public string Kind { get; set; } = "";
}
