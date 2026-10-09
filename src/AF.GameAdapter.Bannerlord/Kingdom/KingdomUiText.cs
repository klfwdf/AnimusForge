using TaleWorlds.Localization;

namespace AnimusForge;

/// <summary>Player-facing kingdom UI text. The fallback stays English; Chinese is supplied by the language XML.</summary>
internal static class KingdomUiText
{
	internal static string T(string id, string englishFallback)
		=> new TextObject("{=" + id + "}" + englishFallback).ToString();

	internal static string F(string id, string englishFallback, params (string Name, object Value)[] variables)
	{
		TextObject text = new TextObject("{=" + id + "}" + englishFallback);
		if (variables != null)
		{
			foreach ((string name, object value) in variables)
				text.SetTextVariable(name, value?.ToString() ?? "");
		}
		return text.ToString();
	}
}
