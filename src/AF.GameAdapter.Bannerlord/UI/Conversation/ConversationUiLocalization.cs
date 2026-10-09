using TaleWorlds.Localization;

namespace AnimusForge;

/// <summary>
/// Player-visible text owned by the Conversation channel. Keep dynamic input and
/// letter bodies outside this catalog; they are user or model content.
/// </summary>
internal static class ConversationUiLocalization
{
	internal static TextObject Object(string id, string englishFallback)
	{
		return new TextObject("{=" + id + "}" + englishFallback);
	}

	internal static TextObject Object(string id, string englishFallback, params (string Name, object Value)[] variables)
	{
		TextObject text = Object(id, englishFallback);
		if (variables != null)
		{
			foreach ((string name, object value) in variables)
			{
				if (value is TextObject variableText)
					text.SetTextVariable(name, variableText);
				else
					text.SetTextVariable(name, value?.ToString() ?? string.Empty);
			}
		}
		return text;
	}

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

	internal static string Text(string id, string englishFallback, params (string Name, object Value)[] variables)
	{
		return Object(id, englishFallback, variables).ToString();
	}
}
