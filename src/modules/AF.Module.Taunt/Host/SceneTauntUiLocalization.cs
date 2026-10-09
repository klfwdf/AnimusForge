using TaleWorlds.Localization;

namespace AnimusForge;

internal static class SceneTauntUiLocalization
{
	internal static string Text(string id, string englishFallback)
	{
		return new TextObject("{=" + id + "}" + englishFallback).ToString();
	}

	internal static string Format(string id, string englishFallback, params (string Key, object Value)[] variables)
	{
		TextObject textObject = new TextObject("{=" + id + "}" + englishFallback);
		if (variables != null)
		{
			foreach ((string key, object value) in variables)
			{
				textObject.SetTextVariable(key, value?.ToString() ?? "");
			}
		}
		return textObject.ToString();
	}
}
