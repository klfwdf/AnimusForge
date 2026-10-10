namespace AnimusForge;

internal static class IssuePromptTexts
{
	internal static string Text(string id, string fallback)
		=> ConversationUiLocalization.Text(id, fallback);

	internal static string Format(string id, string fallback, params (string Name, object Value)[] variables)
		=> ConversationUiLocalization.Text(id, fallback, variables ?? System.Array.Empty<(string Name, object Value)>());
}
