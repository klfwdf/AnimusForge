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
		string topicPart = source.FirstOrDefault((string x) => (x ?? "").StartsWith("议题类型：", StringComparison.OrdinalIgnoreCase));
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
		if (text.StartsWith("原版理由：", StringComparison.OrdinalIgnoreCase))
		{
			text = "原版理由：" + TrimPoliticalReasonPart(text.Substring("原版理由：".Length), 62);
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
		AddPoliticalReasonCoreTag(tags, parts, "关系因素", "关系", "不满", "负面", "不高于0", "恶化");
		AddPoliticalReasonCoreTag(tags, parts, "实力对比", "实力对比", "野战力量", "家族强度", "战争部队", "兵员");
		AddPoliticalReasonCoreTag(tags, parts, "模型评分", "评分", "阈值", "支持度", "模型倾向", "倾向");
		AddPoliticalReasonCoreTag(tags, parts, "封地利益", "封地", "要塞", "城镇", "城堡", "连片治理");
		AddPoliticalReasonCoreTag(tags, parts, "政策取向", "政策权重", "王权", "贵族寡头", "平民");
		AddPoliticalReasonCoreTag(tags, parts, "盟约义务", "盟友", "结盟", "召战", "应召");
		AddPoliticalReasonCoreTag(tags, parts, "贸易利益", "贸易协议", "贸易");
		AddPoliticalReasonCoreTag(tags, parts, "战争状态", "宣战", "议和", "参战", "交战");
		AddPoliticalReasonCoreTag(tags, parts, "财政成本", "贡金", "费用", "付款", "资金不足");
		AddPoliticalReasonCoreTag(tags, parts, "议会支持", "投票支持", "支持者表态", "支持获选者");
		if (tags.Count == 0)
		{
			return "";
		}
		return "核心原因：" + string.Join("、", tags.Take(4));
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
		if (text.StartsWith("原版理由：", StringComparison.OrdinalIgnoreCase))
		{
			return 0;
		}
		if (text.IndexOf("评分", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("阈值", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("支持度", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("模型倾向", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return 1;
		}
		if (text.IndexOf("关系", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("实力对比", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("野战力量", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("家族强度", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return 2;
		}
		if (text.IndexOf("政策权重", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("权重方向", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("封地", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("要塞", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return 3;
		}
		if (text.IndexOf("投票支持", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("支持者", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return 4;
		}
		if (text.IndexOf("费用", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("贡金", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("付款", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return 5;
		}
		if (text.IndexOf("战争", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("盟友", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("贸易", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return 6;
		}
		return 8;
	}

	private const int PoliticalReasonMaxMaterialParts = 6;

	private const int PoliticalReasonMaxPartLength = 86;
}
