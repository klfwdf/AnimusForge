namespace AnimusForge;

/// <summary>
/// Editable prompt configuration used only when an NPC-generated RP item needs an introduction.
/// Keep this model deliberately small so prompt edits do not affect the existing conversation prompts.
/// </summary>
public sealed class RpItemIntroductionPromptsConfigModel
{
	public int Version { get; set; } = 1;

	public string SystemPrompt { get; set; } = "";

	public string UserPromptTemplate { get; set; } = "";
}
