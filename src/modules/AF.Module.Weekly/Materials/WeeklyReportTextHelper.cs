using System;
using System.Text;

namespace AnimusForge;

internal sealed class WeeklyReportSectionSplit
{
	public bool HasExplicitSections { get; set; }

	public string NormalizedBodyText { get; set; } = "";

	public string MilitaryEventsText { get; set; } = "";

	public string DiplomaticAffairsText { get; set; } = "";

	public string DomesticRealmText { get; set; } = "";
}

internal static class WeeklyReportTextHelper
{
	private const string EmptySectionText = "本周未见明显变化。";

	public static void SplitChronicleBodyForDisplay(string bodyText, out string military, out string diplomatic, out string domestic)
	{
		WeeklyReportSectionSplit split = SplitChronicleBody(bodyText);
		if (!split.HasExplicitSections)
		{
			military = string.IsNullOrWhiteSpace(split.NormalizedBodyText) ? EmptySectionText : ("该期旧周报尚未按三类保存，原文如下：\n\n" + split.NormalizedBodyText.Trim());
			diplomatic = EmptySectionText;
			domestic = EmptySectionText;
			return;
		}
		military = NormalizeSectionText(split.MilitaryEventsText, EmptySectionText);
		diplomatic = NormalizeSectionText(split.DiplomaticAffairsText, EmptySectionText);
		domestic = NormalizeSectionText(split.DomesticRealmText, EmptySectionText);
	}

	public static WeeklyReportSectionSplit SplitChronicleBody(string bodyText)
	{
		string text = NormalizeReportBodyText(bodyText);
		StringBuilder militaryBuilder = new StringBuilder();
		StringBuilder diplomaticBuilder = new StringBuilder();
		StringBuilder domesticBuilder = new StringBuilder();
		StringBuilder prefaceBuilder = new StringBuilder();
		int currentSection = 0;
		bool foundSection = false;
		string[] lines = text.Split(new char[1] { '\n' }, StringSplitOptions.None);
		foreach (string rawLine in lines)
		{
			string line = rawLine ?? "";
			if (TryConsumeSectionHeading(line, out int section, out string remainder))
			{
				currentSection = section;
				foundSection = true;
				if (!string.IsNullOrWhiteSpace(remainder))
				{
					AppendSectionLine(GetSectionBuilder(section, militaryBuilder, diplomaticBuilder, domesticBuilder), remainder);
				}
				continue;
			}
			if (!foundSection || currentSection == 0)
			{
				AppendSectionLine(prefaceBuilder, line);
				continue;
			}
			AppendSectionLine(GetSectionBuilder(currentSection, militaryBuilder, diplomaticBuilder, domesticBuilder), line);
		}
		if (foundSection)
		{
			string preface = prefaceBuilder.ToString().Trim();
			if (!string.IsNullOrWhiteSpace(preface))
			{
				AppendSectionLine(militaryBuilder, preface);
			}
		}
		return new WeeklyReportSectionSplit
		{
			HasExplicitSections = foundSection,
			NormalizedBodyText = text,
			MilitaryEventsText = foundSection ? militaryBuilder.ToString().Trim() : text,
			DiplomaticAffairsText = foundSection ? diplomaticBuilder.ToString().Trim() : "",
			DomesticRealmText = foundSection ? domesticBuilder.ToString().Trim() : ""
		};
	}

	public static int CountMeaningfulUnits(string text)
	{
		int count = 0;
		string value = text ?? "";
		for (int i = 0; i < value.Length; i++)
		{
			char ch = value[i];
			if (char.IsWhiteSpace(ch) || char.IsPunctuation(ch) || char.IsSymbol(ch))
			{
				continue;
			}
			if (IsCjkCharacter(ch))
			{
				count++;
				continue;
			}
			if (char.IsLetterOrDigit(ch))
			{
				count++;
				while (i + 1 < value.Length && char.IsLetterOrDigit(value[i + 1]) && !IsCjkCharacter(value[i + 1]))
				{
					i++;
				}
				continue;
			}
			count++;
		}
		return count;
	}

	private static string NormalizeReportBodyText(string bodyText)
	{
		string text = (bodyText ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Trim();
		if (text.StartsWith("[REPORT]", StringComparison.OrdinalIgnoreCase))
		{
			text = text.Substring("[REPORT]".Length).Trim();
		}
		int tagsIndex = text.IndexOf("[TAGS]", StringComparison.OrdinalIgnoreCase);
		if (tagsIndex >= 0)
		{
			text = text.Substring(0, tagsIndex).Trim();
		}
		return text;
	}

	private static StringBuilder GetSectionBuilder(int section, StringBuilder military, StringBuilder diplomatic, StringBuilder domestic)
	{
		if (section == 2)
		{
			return diplomatic;
		}
		if (section == 3)
		{
			return domestic;
		}
		return military;
	}

	private static void AppendSectionLine(StringBuilder builder, string line)
	{
		if (builder == null)
		{
			return;
		}
		if (builder.Length > 0)
		{
			builder.Append('\n');
		}
		builder.Append(line ?? "");
	}

	private static string NormalizeSectionText(string text, string fallback)
	{
		string normalized = (text ?? "").Trim();
		return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
	}

	private static bool TryConsumeSectionHeading(string line, out int section, out string remainder)
	{
		section = 0;
		remainder = line ?? "";
		string text = (line ?? "").TrimStart();
		if (TryConsumeHeading(text, 1, "军事事件", out section, out remainder)
			|| TryConsumeHeading(text, 1, "军事事件与战役", out section, out remainder)
			|| TryConsumeHeading(text, 2, "外交事件", out section, out remainder)
			|| TryConsumeHeading(text, 2, "外交事务", out section, out remainder)
			|| TryConsumeHeading(text, 3, "领地内事件", out section, out remainder)
			|| TryConsumeHeading(text, 3, "领内事件", out section, out remainder)
			|| TryConsumeHeading(text, 3, "内政事件", out section, out remainder))
		{
			return true;
		}
		section = 0;
		remainder = line ?? "";
		return false;
	}

	private static bool TryConsumeHeading(string text, int targetSection, string heading, out int section, out string remainder)
	{
		section = 0;
		remainder = text ?? "";
		string source = text ?? "";
		string bracketed = "【" + heading + "】";
		if (source.StartsWith(bracketed, StringComparison.Ordinal))
		{
			section = targetSection;
			remainder = source.Substring(bracketed.Length).TrimStart();
			return true;
		}
		string square = "[" + heading + "]";
		if (source.StartsWith(square, StringComparison.OrdinalIgnoreCase))
		{
			section = targetSection;
			remainder = source.Substring(square.Length).TrimStart();
			return true;
		}
		if (source.StartsWith(heading + "：", StringComparison.Ordinal))
		{
			section = targetSection;
			remainder = source.Substring(heading.Length + 1).TrimStart();
			return true;
		}
		if (source.StartsWith(heading + ":", StringComparison.Ordinal))
		{
			section = targetSection;
			remainder = source.Substring(heading.Length + 1).TrimStart();
			return true;
		}
		if (string.Equals(source.Trim(), heading, StringComparison.Ordinal))
		{
			section = targetSection;
			remainder = "";
			return true;
		}
		return false;
	}

	private static bool IsCjkCharacter(char ch)
	{
		return (ch >= '\u4E00' && ch <= '\u9FFF')
			|| (ch >= '\u3400' && ch <= '\u4DBF')
			|| (ch >= '\uF900' && ch <= '\uFAFF');
	}
}
