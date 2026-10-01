using System;
using System.Collections.Generic;

namespace AnimusForge;

// One stationery preset = shared scroll base + left/right border strips + wax seal.
// Layout numbers are in popup space (1180x740); strips are pre-cropped to their opaque columns.
public sealed class CourierLetterTheme
{
	public string Id { get; }
	public string DisplayName { get; }
	public int LeftPattern { get; }
	public int RightPattern { get; }
	public string SealKey { get; }

	public CourierLetterTheme(string id, string displayName, int leftPattern, int rightPattern, string sealKey)
	{
		Id = id;
		DisplayName = displayName;
		LeftPattern = leftPattern;
		RightPattern = rightPattern;
		SealKey = sealKey;
	}

	public string LeftSpriteName => AnimusForgeCourierUiSprites.PatternSpriteName("left", LeftPattern);
	public string RightSpriteName => AnimusForgeCourierUiSprites.PatternSpriteName("right", RightPattern);
	public string SealSpriteName => AnimusForgeCourierUiSprites.SealSpriteName(SealKey);
	public int LeftWidth => CourierLetterThemes.PatternWidth("left", LeftPattern);
	public int RightWidth => CourierLetterThemes.PatternWidth("right", RightPattern);
	public int RightMargin => CourierLetterThemes.PatternInsetRight + RightWidth;
}

internal static class CourierLetterThemes
{
	public const int PatternInsetLeft = 105;
	public const int PatternInsetRight = 109;
	public const int PatternTop = 55;
	public const int PatternHeight = 631;
	private const int MaxLeftWidth = 215;
	private const int MaxRightWidth = 224;

	// Widths of the cropped strips scaled from 1368 to 1180, capped to the v2 frame.
	private static readonly Dictionary<string, int> PatternWidths = new Dictionary<string, int>(StringComparer.Ordinal)
	{
		["left_1"] = 320, ["left_2"] = 215, ["left_3"] = 192, ["left_4"] = 152, ["left_5"] = 173,
		["right_1"] = 344, ["right_2"] = 105, ["right_3"] = 152, ["right_4"] = 192, ["right_5"] = 173,
	};

	public static readonly IReadOnlyList<CourierLetterTheme> All = new[]
	{
		new CourierLetterTheme("vlandia", "瓦兰迪亚", 1, 1, "vlandia"),
		new CourierLetterTheme("empire", "北帝国", 2, 2, "north_empire"),
		new CourierLetterTheme("empire_w", "西帝国", 3, 3, "west_empire"),
		new CourierLetterTheme("empire_s", "南帝国", 4, 4, "south_empire"),
		new CourierLetterTheme("sturgia", "斯特吉亚", 5, 5, "sturgia"),
		new CourierLetterTheme("battania", "巴旦尼亚", 1, 3, "battania"),
		new CourierLetterTheme("khuzait", "库塞特", 4, 2, "khuzait"),
		new CourierLetterTheme("aserai", "阿塞莱", 3, 5, "aserai"),
		new CourierLetterTheme("nord", "诺德", 5, 1, "nord"),
	};

	public static int PatternWidth(string side, int index)
	{
		int max = side == "left" ? MaxLeftWidth : MaxRightWidth;
		return PatternWidths.TryGetValue(side + "_" + index, out int width) ? Math.Min(width, max) : max;
	}

	public static CourierLetterTheme Resolve(string themeId)
	{
		foreach (CourierLetterTheme theme in All)
		{
			if (string.Equals(theme.Id, themeId, StringComparison.OrdinalIgnoreCase))
			{
				return theme;
			}
		}
		return All[0];
	}

	public static CourierLetterTheme Current
	{
		get
		{
			string id = null;
			try
			{
				id = DuelSettings.GetSettings()?.CourierLetterThemeId;
			}
			catch
			{
			}
			return Resolve(id);
		}
	}
}
