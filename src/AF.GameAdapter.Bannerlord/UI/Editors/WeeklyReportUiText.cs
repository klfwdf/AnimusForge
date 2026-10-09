using TaleWorlds.Core;

namespace AnimusForge;

internal static class WeeklyReportUiText
{
    internal static string Get(string id, string englishFallback)
        => new TextObject("{" + "=" + id + "}" + englishFallback).ToString();
}
