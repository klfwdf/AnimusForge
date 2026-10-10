using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace AnimusForge;

internal static class WeeklyPoliticalMaterialPolicy
{
	internal static string NormalizePoliticalReasonText(string text)
	{
		return (text ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
	}

	internal static string BuildPoliticalReasonSentence(string label, IEnumerable<string> parts)
	{
		List<string> list = (parts ?? Enumerable.Empty<string>()).Where((string x) => !string.IsNullOrWhiteSpace(x)).Select(CondensePoliticalReasonPart).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		if (list.Count == 0)
		{
			return "";
		}
		list = SelectPoliticalReasonMaterialParts(list);
		return label + "：" + string.Join("；", list) + "。";
	}

	internal static List<string> SelectPoliticalReasonMaterialParts(List<string> parts)
	{
		List<string> source = parts ?? new List<string>();
		List<string> selected = new List<string>();
		string topicPart = source.FirstOrDefault((string x) => (x ?? "").StartsWith(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0412"), StringComparison.OrdinalIgnoreCase));
		AddPoliticalReasonPartIfUseful(selected, topicPart);
		string corePart = BuildPoliticalReasonCorePart(source);
		AddPoliticalReasonPartIfUseful(selected, corePart);
		for (int priority = 0; priority <= 8 && selected.Count < PoliticalReasonMaxMaterialParts; priority++)
		{
			foreach (string part in source)
			{
				if (selected.Count >= PoliticalReasonMaxMaterialParts)
				{
					break;
				}
				if (string.Equals(part, topicPart, StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}
				if (GetPoliticalReasonPartPriority(part) == priority)
				{
					AddPoliticalReasonPartIfUseful(selected, part);
				}
			}
		}
		return selected.Count > 0 ? selected : source.Take(PoliticalReasonMaxMaterialParts).ToList();
	}

	internal static void AddPoliticalReasonPartIfUseful(List<string> parts, string part)
	{
		if (parts == null || string.IsNullOrWhiteSpace(part))
		{
			return;
		}
		string text = CondensePoliticalReasonPart(part);
		if (!string.IsNullOrWhiteSpace(text) && !parts.Any((string x) => string.Equals(x, text, StringComparison.OrdinalIgnoreCase)))
		{
			parts.Add(text);
		}
	}

	internal static string CondensePoliticalReasonPart(string part)
	{
		string text = NormalizePoliticalReasonText(part);
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		text = Regex.Replace(text, "\\s+", " ");
		if (text.StartsWith(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0413"), StringComparison.OrdinalIgnoreCase))
		{
			text = WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0414") + TrimPoliticalReasonPart(text.Substring(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0415").Length), 62);
		}
		return TrimPoliticalReasonPart(text, PoliticalReasonMaxPartLength);
	}

	internal static string TrimPoliticalReasonPart(string text, int maxLength)
	{
		string value = (text ?? "").Trim();
		if (string.IsNullOrWhiteSpace(value) || value.Length <= maxLength)
		{
			return value;
		}
		int cut = value.LastIndexOf('，', Math.Min(maxLength - 4, value.Length - 1));
		if (cut < 18)
		{
			cut = value.LastIndexOf('；', Math.Min(maxLength - 4, value.Length - 1));
		}
		if (cut < 18)
		{
			cut = Math.Max(12, maxLength - 3);
		}
		return value.Substring(0, Math.Min(cut, value.Length)).TrimEnd('，', '；', '。', ' ') + "...";
	}

	internal static string BuildPoliticalReasonCorePart(List<string> parts)
	{
		if (parts == null || parts.Count == 0)
		{
			return "";
		}
		List<string> tags = new List<string>();
		AddPoliticalReasonCoreTag(tags, parts, WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0416"), WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0417"), WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0418"), WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0419"), WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0420"), WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0421"));
		AddPoliticalReasonCoreTag(tags, parts, WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0422"), WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0423"), WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0424"), WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0425"), WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0426"), WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0427"));
		AddPoliticalReasonCoreTag(tags, parts, WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0428"), WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0429"), WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0430"), WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0431"), WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0432"), WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0433"));
		AddPoliticalReasonCoreTag(tags, parts, WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0434"), WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0435"), WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0436"), WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0437"), WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0438"), WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0439"));
		AddPoliticalReasonCoreTag(tags, parts, WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0440"), WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0441"), WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0442"), WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0443"), WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0444"));
		AddPoliticalReasonCoreTag(tags, parts, WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0445"), WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0446"), WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0447"), WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0448"), WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0449"));
		AddPoliticalReasonCoreTag(tags, parts, WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0450"), WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0451"), WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0452"));
		AddPoliticalReasonCoreTag(tags, parts, WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0453"), WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0454"), WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0455"), WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0456"), WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0457"));
		AddPoliticalReasonCoreTag(tags, parts, WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0458"), WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0459"), WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0460"), WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0461"), WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0462"));
		AddPoliticalReasonCoreTag(tags, parts, WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0463"), WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0464"), WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0465"), WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0466"));
		if (tags.Count == 0)
		{
			return "";
		}
		return WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0467") + string.Join("、", tags.Take(4));
	}

	internal static void AddPoliticalReasonCoreTag(List<string> tags, List<string> parts, string tag, params string[] keywords)
	{
		if (tags == null || parts == null || string.IsNullOrWhiteSpace(tag) || tags.Any((string x) => string.Equals(x, tag, StringComparison.OrdinalIgnoreCase)))
		{
			return;
		}
		foreach (string part in parts)
		{
			string text = part ?? "";
			if (keywords.Any((string keyword) => !string.IsNullOrWhiteSpace(keyword) && text.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0))
			{
				tags.Add(tag);
				return;
			}
		}
	}

	internal static int GetPoliticalReasonPartPriority(string part)
	{
		string text = part ?? "";
		if (text.StartsWith(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0468"), StringComparison.OrdinalIgnoreCase))
		{
			return 0;
		}
		if (text.IndexOf(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0469"), StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0470"), StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0471"), StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0472"), StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return 1;
		}
		if (text.IndexOf(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0473"), StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0474"), StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0475"), StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0476"), StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return 2;
		}
		if (text.IndexOf(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0477"), StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0478"), StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0479"), StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0480"), StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return 3;
		}
		if (text.IndexOf(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0481"), StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0482"), StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return 4;
		}
		if (text.IndexOf(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0483"), StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0484"), StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0485"), StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return 5;
		}
		if (text.IndexOf(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0486"), StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0487"), StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0488"), StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return 6;
		}
		return 8;
	}

	private const int PoliticalReasonMaxMaterialParts = 6;

	private const int PoliticalReasonMaxPartLength = 86;
}
