using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Localization;

namespace AnimusForge;

/// <summary>
/// One authoritative clock for settlement ambience.  Keeping the boundaries in
/// one place prevents the player tags, rendered placeholders and line filters
/// from disagreeing around 05:00/19:00/20:00.
/// </summary>
internal enum TownAmbientTimeBand
{
	Dawn,
	Day,
	Evening,
	Night
}

internal static class TownAmbientTime
{
	private static readonly string[] NightTerms =
	{
		ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0989"), ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0990"), ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0991"), ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0992"), ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0993"), ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0994"), ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0995"), ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0996"), ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0997"), ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0998"), ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0999"), ConversationRuntimeTexts.Raw("AF_CONV_Runtime_1000"), ConversationRuntimeTexts.Raw("AF_CONV_Runtime_1001"), ConversationRuntimeTexts.Raw("AF_CONV_Runtime_1002"), ConversationRuntimeTexts.Raw("AF_CONV_Runtime_1003")
	};
	private static readonly string[] EveningTerms = { ConversationRuntimeTexts.Raw("AF_CONV_Runtime_1004"), ConversationRuntimeTexts.Raw("AF_CONV_Runtime_1005"), ConversationRuntimeTexts.Raw("AF_CONV_Runtime_1006"), ConversationRuntimeTexts.Raw("AF_CONV_Runtime_1007") };
	private static readonly string[] DayTerms = { ConversationRuntimeTexts.Raw("AF_CONV_Runtime_1008"), ConversationRuntimeTexts.Raw("AF_CONV_Runtime_1009"), ConversationRuntimeTexts.Raw("AF_CONV_Runtime_1010"), ConversationRuntimeTexts.Raw("AF_CONV_Runtime_1011"), ConversationRuntimeTexts.Raw("AF_CONV_Runtime_1012"), ConversationRuntimeTexts.Raw("AF_CONV_Runtime_1013"), ConversationRuntimeTexts.Raw("AF_CONV_Runtime_1014"), ConversationRuntimeTexts.Raw("AF_CONV_Runtime_1015"), ConversationRuntimeTexts.Raw("AF_CONV_Runtime_1016") };

	public static TownAmbientTimeBand GetBand(int hour)
	{
		hour = NormalizeHour(hour);
		if (hour >= 5 && hour <= 7) return TownAmbientTimeBand.Dawn;
		if (hour >= 8 && hour <= 16) return TownAmbientTimeBand.Day;
		if (hour >= 17 && hour <= 19) return TownAmbientTimeBand.Evening;
		return TownAmbientTimeBand.Night;
	}

	public static string GetDisplayName(int hour)
	{
		switch (GetBand(hour))
		{
			case TownAmbientTimeBand.Dawn: return ConversationRuntimeTexts.Raw("AF_CONV_Runtime_1017");
			case TownAmbientTimeBand.Day: return ConversationRuntimeTexts.Raw("AF_CONV_Runtime_1018");
			case TownAmbientTimeBand.Evening: return ConversationRuntimeTexts.Raw("AF_CONV_Runtime_1019");
			default: return ConversationRuntimeTexts.Raw("AF_CONV_Runtime_1020");
		}
	}

	public static string GetLocalizedDisplayName(int hour)
	{
		switch (GetBand(hour))
		{
			case TownAmbientTimeBand.Dawn:
				return new TextObject("{=AF_CONV_AMBIENT_TimePeriod_Dawn}dawn").ToString();
			case TownAmbientTimeBand.Day:
				return new TextObject("{=AF_CONV_AMBIENT_TimePeriod_Day}daytime").ToString();
			case TownAmbientTimeBand.Evening:
				return new TextObject("{=AF_CONV_AMBIENT_TimePeriod_Evening}evening").ToString();
			default:
				return new TextObject("{=AF_CONV_AMBIENT_TimePeriod_Night}night").ToString();
		}
	}

	public static string GetGreeting(int hour)
	{
		switch (GetBand(hour))
		{
			case TownAmbientTimeBand.Dawn: return ConversationRuntimeTexts.Raw("AF_CONV_Runtime_1021");
			case TownAmbientTimeBand.Day: return ConversationRuntimeTexts.Raw("AF_CONV_Runtime_1022");
			case TownAmbientTimeBand.Evening: return ConversationRuntimeTexts.Raw("AF_CONV_Runtime_1023");
			default: return ConversationRuntimeTexts.Raw("AF_CONV_Runtime_1024");
		}
	}

	public static string GetLocalizedGreeting(int hour)
	{
		switch (GetBand(hour))
		{
			case TownAmbientTimeBand.Dawn:
				return new TextObject("{=AF_CONV_AMBIENT_TimeGreeting_Dawn}Good morning").ToString();
			case TownAmbientTimeBand.Day:
				return new TextObject("{=AF_CONV_AMBIENT_TimeGreeting_Day}Good day").ToString();
			case TownAmbientTimeBand.Evening:
				return new TextObject("{=AF_CONV_AMBIENT_TimeGreeting_Evening}Good evening").ToString();
			default:
				return new TextObject("{=AF_CONV_AMBIENT_TimeGreeting_Night}Good evening").ToString();
		}
	}

	public static bool IsNight(int hour)
	{
		return GetBand(hour) == TownAmbientTimeBand.Night;
	}

	public static string GetTag(int hour)
	{
		return GetBand(hour).ToString().ToLowerInvariant();
	}

	public static bool Matches(TownAmbientLine line, int hour)
	{
		if (line == null) return false;
		TownAmbientTimeBand actualBand = GetBand(hour);
		List<string> configuredBands = line.TimeBands == null
			? new List<string>()
			: line.TimeBands.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

		if (configuredBands.Count > 0)
		{
			return configuredBands.Any(x => IsAnyBand(x) || MatchesBandName(x, actualBand));
		}

		if (line.MinHour.HasValue || line.MaxHour.HasValue)
		{
			if (IsHourInRange(hour, line.MinHour ?? 0, line.MaxHour ?? 23)) return true;
			// Older night entries were authored as 20-23.  Treat that range as
			// crossing midnight so the same line remains valid at 00:00-04:00.
			return GetBand(hour) == TownAmbientTimeBand.Night && IsNightRange(line.MinHour, line.MaxHour);
		}

		// Backward-compatible strictness: old files can omit TimeBands, but a
		// clearly time-specific sentence must not remain an all-day line.
		HashSet<TownAmbientTimeBand> inferred = InferBands(line);
		return inferred.Count == 0 || inferred.Contains(actualBand);
	}

	public static void ValidateLines(IEnumerable<TownAmbientLine> lines)
	{
		if (lines == null) return;
		int warningCount = 0;
		foreach (TownAmbientLine line in lines)
		{
			if (line == null || !line.HasText) continue;
			string text = GetText(line);
			if (string.IsNullOrWhiteSpace(text)) continue;
			HashSet<TownAmbientTimeBand> inferred = InferBands(line);
			bool hasExplicitTime = line.TimeBands != null && line.TimeBands.Count > 0 || line.MinHour.HasValue || line.MaxHour.HasValue;
			if (inferred.Count > 0 && !hasExplicitTime && warningCount < 20)
			{
				Logger.Log("TownAmbient", "time_line_inferred id=" + (line.Id ?? "") + " bands=" + string.Join(",", inferred.Select(x => x.ToString())));
				warningCount++;
			}
			if (inferred.Count > 1 && !hasExplicitTime && warningCount < 20)
			{
				Logger.Log("TownAmbient", "time_line_mixed_terms id=" + (line.Id ?? "") + " text=" + text);
				warningCount++;
			}
		}
		if (warningCount >= 20)
		{
			Logger.Log("TownAmbient", "time_line_audit truncated after 20 warnings.");
		}
	}

	private static HashSet<TownAmbientTimeBand> InferBands(TownAmbientLine line)
	{
		HashSet<TownAmbientTimeBand> result = new HashSet<TownAmbientTimeBand>();
		string text = GetText(line);
		if (ContainsAny(text, NightTerms)) result.Add(TownAmbientTimeBand.Night);
		if (ContainsAny(text, EveningTerms)) result.Add(TownAmbientTimeBand.Evening);
		if (ContainsAny(text, DayTerms)) result.Add(TownAmbientTimeBand.Day);
		return result;
	}

	private static string GetText(TownAmbientLine line)
	{
		return ((line?.Text ?? "") + " " + string.Join(" ", line?.TextVariants ?? new List<string>())).Trim();
	}

	private static bool ContainsAny(string text, IEnumerable<string> terms)
	{
		return terms.Any(term => text.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0);
	}

	private static bool IsAnyBand(string value)
	{
		string normalized = (value ?? "").Trim().ToLowerInvariant();
		return normalized == "any" || normalized == "any_time" || normalized == ConversationRuntimeTexts.Raw("AF_CONV_Runtime_1025") || normalized == ConversationRuntimeTexts.Raw("AF_CONV_Runtime_1026");
	}

	private static bool MatchesBandName(string value, TownAmbientTimeBand band)
	{
		string normalized = (value ?? "").Trim().ToLowerInvariant();
		switch (band)
		{
			case TownAmbientTimeBand.Dawn: return normalized == "dawn" || normalized == ConversationRuntimeTexts.Raw("AF_CONV_Runtime_1027");
			case TownAmbientTimeBand.Day: return normalized == "day" || normalized == ConversationRuntimeTexts.Raw("AF_CONV_Runtime_1028");
			case TownAmbientTimeBand.Evening: return normalized == "evening" || normalized == ConversationRuntimeTexts.Raw("AF_CONV_Runtime_1029");
			default: return normalized == "night" || normalized == ConversationRuntimeTexts.Raw("AF_CONV_Runtime_1030") || normalized == ConversationRuntimeTexts.Raw("AF_CONV_Runtime_1031");
		}
	}

	private static bool IsHourInRange(int hour, int min, int max)
	{
		hour = NormalizeHour(hour);
		min = NormalizeHour(min);
		max = NormalizeHour(max);
		return min <= max ? hour >= min && hour <= max : hour >= min || hour <= max;
	}

	private static bool IsNightRange(int? min, int? max)
	{
		return min.HasValue && max.HasValue && min.Value >= 19 && max.Value <= 23;
	}

	private static int NormalizeHour(int hour)
	{
		return ((hour % 24) + 24) % 24;
	}
}
