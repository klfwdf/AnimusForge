using System;
using TaleWorlds.Localization;

namespace AnimusForge;

/// <summary>Localized text used by the MCM settings actions and connection-test notices.</summary>
internal static class DuelSettingsUiLocalization
{
	internal static string Text(string id, string englishFallback)
	{
		return new TextObject("{=" + id + "}" + englishFallback).ToString();
	}

	internal static string Format(string id, string englishFallback, params (string Name, object Value)[] variables)
	{
		TextObject text = new TextObject("{=" + id + "}" + englishFallback);
		if (variables != null)
		{
			foreach ((string name, object value) in variables)
			{
				text.SetTextVariable(name, value?.ToString() ?? string.Empty);
			}
		}
		return text.ToString();
	}
}
