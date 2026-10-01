using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

namespace AnimusForge;
internal static class RebellionNamingRules
{
	internal static string NormalizeSource(string text, int maxLength = 1200)
	{
		string text2 = (text ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
		while (text2.Contains("  "))
		{
			text2 = text2.Replace("  ", " ");
		}

		if (string.IsNullOrWhiteSpace(text2))
		{
			return "";
		}

		if (text2.Length > maxLength)
		{
			text2 = text2.Substring(0, maxLength).TrimEnd();
		}

		return text2.Trim();
	}

	internal static string NormalizeName(string text, int maxLength)
	{
		string text2 = (text ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
		while (text2.Contains("  "))
		{
			text2 = text2.Replace("  ", " ");
		}

		text2 = text2.Trim().Trim('\"', '\'', '“', '”', '‘', '’', '：', ':', '-', '·');
		if (string.IsNullOrWhiteSpace(text2))
		{
			return "";
		}

		if (text2.Length > maxLength)
		{
			text2 = text2.Substring(0, maxLength).TrimEnd();
		}

		return text2.Trim();
	}

	internal static string NormalizeLore(string text)
	{
		string text2 = (text ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
		while (text2.Contains("  "))
		{
			text2 = text2.Replace("  ", " ");
		}

		return text2.Trim();
	}

	internal static bool TryParse(string rawResponse, out string formalName, out string shortName, out string encyclopediaText)
	{
		formalName = "";
		shortName = "";
		encyclopediaText = "";
		string text = (rawResponse ?? "").Replace("\r", "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}

		Match match = Regex.Match(text, "\\[NAME\\](?<name>[\\s\\S]*?)(?=\\[SHORT\\]|\\[LORE\\]|$)", RegexOptions.IgnoreCase);
		Match match2 = Regex.Match(text, "\\[SHORT\\](?<short>[\\s\\S]*?)(?=\\[LORE\\]|$)", RegexOptions.IgnoreCase);
		Match match3 = Regex.Match(text, "\\[LORE\\](?<lore>[\\s\\S]*)$", RegexOptions.IgnoreCase);
		if (match.Success)
		{
			formalName = NormalizeName(match.Groups["name"]?.Value ?? "", 24);
		}

		if (match2.Success)
		{
			shortName = NormalizeName(match2.Groups["short"]?.Value ?? "", 14);
		}

		if (match3.Success)
		{
			encyclopediaText = NormalizeLore(match3.Groups["lore"]?.Value ?? "");
		}

		return !string.IsNullOrWhiteSpace(formalName) && !string.IsNullOrWhiteSpace(shortName) && !string.IsNullOrWhiteSpace(encyclopediaText);
	}

	internal static int RetryDelay(bool rateLimited, int interval, int? retryAfter)
	{
		int delay = 1200;
		if (rateLimited)
			delay = Math.Max(delay, interval);
		if (retryAfter.HasValue)
			delay = Math.Max(delay, retryAfter.Value * 1000);
		return delay;
	}

	internal static string BuildSystemPrompt(string configuredPrompt)
	{
		StringBuilder stringBuilder = new StringBuilder();
		string text = (configuredPrompt ?? "").Replace("\r", "").Trim();
		if (!string.IsNullOrWhiteSpace(text))
		{
			stringBuilder.AppendLine(text);
			stringBuilder.AppendLine(" ");
		}

		stringBuilder.AppendLine("输出格式：");
		stringBuilder.AppendLine("[NAME]正式国名");
		stringBuilder.AppendLine("[SHORT]简称");
		stringBuilder.AppendLine("[LORE]百科简介");
		return stringBuilder.ToString().TrimEnd();
	}

	internal static string BuildUserPrompt(RebellionNamingFacts f)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("【叛乱建国命名任务】");
		stringBuilder.AppendLine("当前周次：第 " + Math.Max(0, f.WeekIndex) + " 周");
		stringBuilder.AppendLine("主导家族：" + f.ClanName);
		stringBuilder.AppendLine("主导族长：" + f.LeaderName);
		stringBuilder.AppendLine("家族文化：" + f.CultureName);
		stringBuilder.AppendLine("原所属王国：" + f.KingdomName);
		stringBuilder.AppendLine("原所属王国领袖（被背叛者）：" + f.KingName);
		stringBuilder.AppendLine("原王国执政家族：" + f.RulingClanName);
		stringBuilder.AppendLine("当前核心封地：" + f.SettlementNames);
		stringBuilder.AppendLine(f.RebelBackground);
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("【叛乱核心定居点百科】");
		stringBuilder.AppendLine(f.SettlementSummary);
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("【联合响应家族】");
		stringBuilder.AppendLine(f.FollowerSummary);
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("【叛乱家族族长背景】");
		stringBuilder.AppendLine(f.LeaderBackground);
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("【原王国统治者背景】");
		stringBuilder.AppendLine(f.KingBackground);
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("【原王国背景】");
		stringBuilder.AppendLine(f.KingdomBackground);
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("【上周世界周报】");
		stringBuilder.AppendLine(f.WorldWeekly);
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("【上周原王国周报】");
		stringBuilder.AppendLine(f.KingdomWeekly);
		if (!string.IsNullOrWhiteSpace(f.ExistingNames))
		{
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("现有王国名称（禁止重名）：" + f.ExistingNames);
		}

		stringBuilder.AppendLine("请生成一个正式名、一个简称，以及一段百科简介。");
		return stringBuilder.ToString().TrimEnd();
	}

	internal static string WeeklyLeadIn(string title, string brief, string summary)
	{
		string text = NormalizeSource(brief, 260);
		if (string.IsNullOrWhiteSpace(text))
			text = NormalizeSource(summary, 500);
		if (string.IsNullOrWhiteSpace(text))
			return "无";
		string heading = NormalizeSource(title, 80);
		return string.IsNullOrWhiteSpace(heading) ? text : heading + "：" + text;
	}

	internal static string HeroBackground(bool exists, string encyclopedia, string heroName, string clanName, string culture)
	{
		if (!exists)
		{
			return "无";
		}

		string text = NormalizeSource(encyclopedia, 900);
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}

		string heroDisplayName = heroName;
		string clanDisplayName = clanName;
		string text2 = (culture ?? "").Trim();
		string value = string.IsNullOrWhiteSpace(text2) ? "" : (text2 + "文化");
		return NormalizeSource(heroDisplayName + "出身于" + clanDisplayName + "家族，现为" + value + "背景的领主。", 300);
	}

	internal static string KingdomBackground(bool exists, string encyclopedia, string opening, string name)
	{
		if (!exists)
		{
			return "无";
		}

		string text = NormalizeSource(encyclopedia, 1200);
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}

		string kingdomOpeningSummary = opening;
		text = NormalizeSource(kingdomOpeningSummary, 1200);
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}

		return name + "是当前叛乱所脱离的旧政权。";
	}

	internal static string SettlementBackground(bool exists, string encyclopedia, string name, bool town, bool castle, string culture, string kingdom)
	{
		if (!exists)
		{
			return "无";
		}

		string text = NormalizeSource(encyclopedia, 600);
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}

		string settlementDisplayName = name;
		string text2 = town ? "城镇" : (castle ? "城堡" : "定居点");
		string text3 = (culture ?? "").Trim();
		string kingdomDisplayName = kingdom;
		return NormalizeSource(settlementDisplayName + "是一处" + text2 + "，文化为" + (string.IsNullOrWhiteSpace(text3) ? "未知" : text3) + "，当前归属于" + kingdomDisplayName + "。", 220);
	}

	internal static string RebelBackground(string brief, string summary, string name)
	{
		string text = NormalizeSource(brief, 320);
		if (string.IsNullOrWhiteSpace(text))
		{
			text = NormalizeSource(summary, 420);
		}

		if (!string.IsNullOrWhiteSpace(text))
		{
			return "叛乱背景：上周" + name + "周报提到，" + text + "。这场叛乱正是在这样的局势中爆发，主导家族带着现有封地脱离旧王国，准备自立为新的政治实体。";
		}

		return "叛乱背景：该家族因与旧王国统治层关系恶化，在王国内部稳定度恶化时带着现有封地脱离旧王国，并准备自立为新的政治实体。";
	}
	internal static string SettlementLine(string name, string background) => "- " + name + "：" + background;

	internal static string JoinSettlementSummary(System.Collections.Generic.List<string> list)
	{
		return list == null || list.Count == 0 ? "无" : string.Join("\n", list);
	}

	internal static string FollowerLine(string clan, string hero, string fortifications) => clan + "（族长：" + hero + "；" + fortifications + "）";

	internal static string JoinFollowerSummary(System.Collections.Generic.List<string> list)
	{
		return list == null || list.Count == 0 ? "无" : string.Join("\n", list.Distinct(StringComparer.OrdinalIgnoreCase).Select((string x) => "- " + x));
	}

}

internal sealed class RebellionNamingFacts
{
	internal int WeekIndex;
	internal string ClanName, LeaderName, CultureName, KingdomName, KingName, RulingClanName, SettlementNames, RebelBackground, SettlementSummary, FollowerSummary, LeaderBackground, KingBackground, KingdomBackground, KingdomWeekly, WorldWeekly, ExistingNames;
}
