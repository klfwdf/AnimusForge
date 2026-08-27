using System;
using System.Collections.Generic;
using System.Linq;

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
		"今晚", "今夜", "夜里", "夜间", "夜班", "夜哨", "守夜", "夜风", "夜巡", "夜里的", "夜色", "入夜", "过夜", "月亮", "星空"
	};
	private static readonly string[] EveningTerms = { "傍晚", "黄昏", "日落", "落日前" };
	private static readonly string[] DayTerms = { "清晨", "早上", "上午", "正午", "午后", "下午", "日头", "太阳", "天亮" };

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
			case TownAmbientTimeBand.Dawn: return "清晨";
			case TownAmbientTimeBand.Day: return "白天";
			case TownAmbientTimeBand.Evening: return "傍晚";
			default: return "夜里";
		}
	}

	public static string GetGreeting(int hour)
	{
		switch (GetBand(hour))
		{
			case TownAmbientTimeBand.Dawn: return "早安";
			case TownAmbientTimeBand.Day: return "午安";
			case TownAmbientTimeBand.Evening: return "傍晚好";
			default: return "晚上好";
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
		return normalized == "any" || normalized == "any_time" || normalized == "全天" || normalized == "任何时间";
	}

	private static bool MatchesBandName(string value, TownAmbientTimeBand band)
	{
		string normalized = (value ?? "").Trim().ToLowerInvariant();
		switch (band)
		{
			case TownAmbientTimeBand.Dawn: return normalized == "dawn" || normalized == "清晨";
			case TownAmbientTimeBand.Day: return normalized == "day" || normalized == "白天";
			case TownAmbientTimeBand.Evening: return normalized == "evening" || normalized == "傍晚";
			default: return normalized == "night" || normalized == "夜里" || normalized == "夜晚";
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
