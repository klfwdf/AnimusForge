using TaleWorlds.Localization;

namespace AnimusForge;

internal static class DiplomacyUiLocalization
{
    internal static string CampaignDate(int day, int daysInSeason, int daysInYear)
    {
        day = System.Math.Max(0, day);
        daysInSeason = daysInSeason > 0 ? daysInSeason : 21;
        daysInYear = daysInYear > 0 ? daysInYear : daysInSeason * 4;
        int dayOfYear = day % daysInYear;
        string season = ((dayOfYear / daysInSeason) % 4) switch {
            0 => Text("AF_DIP_UI_EventDate_Spring", "Spring"),
            1 => Text("AF_DIP_UI_EventDate_Summer", "Summer"),
            2 => Text("AF_DIP_UI_EventDate_Autumn", "Autumn"),
            _ => Text("AF_DIP_UI_EventDate_Winter", "Winter")
        };
        return Format("AF_DIP_UI_EventDate", "Year {YEAR}, {SEASON} {DAY}",
            ("YEAR", day / daysInYear), ("SEASON", season), ("DAY", dayOfYear % daysInSeason + 1));
    }
    // Resolve the template once, then insert display data without re-parsing its braces.
    private static readonly System.Text.RegularExpressions.Regex LiteralValues = new System.Text.RegularExpressions.Regex(@"AFSWEEPLITERAL([0-9]+)END", System.Text.RegularExpressions.RegexOptions.CultureInvariant);

	internal static string Text(string id, string englishFallback)
	{
		return new TextObject("{=" + id + "}" + englishFallback).ToString();
	}

	internal static TextObject Object(string id, string englishFallback)
	{
		return new TextObject("{=" + id + "}" + englishFallback);
	}

	internal static string Format(string id, string englishFallback, params (string Key, object Value)[] variables)
    {
        var text = new TextObject("{=" + id + "}" + englishFallback);
        if (variables != null)
        for (int i = 0; i < variables.Length; i++)
            if (variables[i].Value is TextObject nested)
                text.SetTextVariable(variables[i].Key, nested);
            else
                text.SetTextVariable(variables[i].Key, "AFSWEEPLITERAL" + i.ToString(System.Globalization.CultureInfo.InvariantCulture) + "END");
        return LiteralValues.Replace(text.ToString(), match => variables[int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture)].Value?.ToString() ?? string.Empty);
    }
}
