using TaleWorlds.Localization;

namespace AnimusForge;

internal static class WorldEventUiLocalization
{
	internal static string Text(string id, string englishFallback)
		=> new TextObject("{=" + id + "}" + englishFallback).ToString();
}
