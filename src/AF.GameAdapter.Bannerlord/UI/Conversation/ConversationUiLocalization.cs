using TaleWorlds.Localization;

namespace AnimusForge;

/// <summary>
/// Player-visible text owned by the Conversation channel. Keep dynamic input and
/// letter bodies outside this catalog; they are user or model content.
/// </summary>
internal static class ConversationUiLocalization
{
	internal static string Text(string id, string englishFallback)
	{
		return new TextObject("{=" + id + "}" + englishFallback).ToString();
	}

	internal static string Text(string id, string englishFallback, string variableName, string variableValue)
	{
		TextObject text = new TextObject("{=" + id + "}" + englishFallback);
		text.SetTextVariable(variableName, variableValue ?? string.Empty);
		return text.ToString();
	}
}
