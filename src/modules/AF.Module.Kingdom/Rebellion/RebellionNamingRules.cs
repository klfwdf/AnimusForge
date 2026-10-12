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

		stringBuilder.AppendLine(KingdomCivilWarTexts.Prompt("AF_KINGDOM_CW_RebellionNaming_SystemPrompt_OutputFormat"));
		stringBuilder.AppendLine(KingdomCivilWarTexts.Prompt("AF_KINGDOM_CW_RebellionNaming_SystemPrompt_NameTag"));
		stringBuilder.AppendLine(KingdomCivilWarTexts.Prompt("AF_KINGDOM_CW_RebellionNaming_SystemPrompt_ShortTag"));
		stringBuilder.AppendLine(KingdomCivilWarTexts.Prompt("AF_KINGDOM_CW_RebellionNaming_SystemPrompt_LoreTag"));
		return PromptComposerTexts.ApplyOutputLanguage(stringBuilder.ToString().TrimEnd());
	}

	internal static string BuildUserPrompt(RebellionNamingFacts f)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine(KingdomCivilWarTexts.Prompt("AF_KINGDOM_CW_RebellionNaming_UserPrompt_Header"));
		stringBuilder.AppendLine(KingdomCivilWarTexts.Prompt("KW_Prompt_064") + Math.Max(0, f.WeekIndex) + KingdomCivilWarTexts.Prompt("KW_Prompt_065"));
		stringBuilder.AppendLine(KingdomCivilWarTexts.Prompt("KW_Prompt_066") + f.ClanName);
		stringBuilder.AppendLine(KingdomCivilWarTexts.Prompt("KW_Prompt_067") + f.LeaderName);
		stringBuilder.AppendLine(KingdomCivilWarTexts.Prompt("KW_Prompt_068") + f.CultureName);
		stringBuilder.AppendLine(KingdomCivilWarTexts.Prompt("KW_Prompt_069") + f.KingdomName);
		stringBuilder.AppendLine(KingdomCivilWarTexts.Prompt("KW_Prompt_070") + f.KingName);
		stringBuilder.AppendLine(KingdomCivilWarTexts.Prompt("KW_Prompt_071") + f.RulingClanName);
		stringBuilder.AppendLine(KingdomCivilWarTexts.Prompt("KW_Prompt_072") + f.SettlementNames);
		stringBuilder.AppendLine(f.RebelBackground);
		stringBuilder.AppendLine();
		stringBuilder.AppendLine(KingdomCivilWarTexts.Prompt("AF_KINGDOM_CW_RebellionNaming_UserPrompt_SettlementHeader"));
		stringBuilder.AppendLine(f.SettlementSummary);
		stringBuilder.AppendLine();
		stringBuilder.AppendLine(KingdomCivilWarTexts.Prompt("AF_KINGDOM_CW_RebellionNaming_UserPrompt_FollowerHeader"));
		stringBuilder.AppendLine(f.FollowerSummary);
		stringBuilder.AppendLine();
		stringBuilder.AppendLine(KingdomCivilWarTexts.Prompt("AF_KINGDOM_CW_RebellionNaming_UserPrompt_LeaderBackgroundHeader"));
		stringBuilder.AppendLine(f.LeaderBackground);
		stringBuilder.AppendLine();
		stringBuilder.AppendLine(KingdomCivilWarTexts.Prompt("AF_KINGDOM_CW_RebellionNaming_UserPrompt_KingBackgroundHeader"));
		stringBuilder.AppendLine(f.KingBackground);
		stringBuilder.AppendLine();
		stringBuilder.AppendLine(KingdomCivilWarTexts.Prompt("AF_KINGDOM_CW_RebellionNaming_UserPrompt_KingdomBackgroundHeader"));
		stringBuilder.AppendLine(f.KingdomBackground);
		stringBuilder.AppendLine();
		stringBuilder.AppendLine(KingdomCivilWarTexts.Prompt("AF_KINGDOM_CW_RebellionNaming_UserPrompt_WorldWeeklyHeader"));
		stringBuilder.AppendLine(f.WorldWeekly);
		stringBuilder.AppendLine();
		stringBuilder.AppendLine(KingdomCivilWarTexts.Prompt("AF_KINGDOM_CW_RebellionNaming_UserPrompt_KingdomWeeklyHeader"));
		stringBuilder.AppendLine(f.KingdomWeekly);
		if (!string.IsNullOrWhiteSpace(f.ExistingNames))
		{
			stringBuilder.AppendLine();
			stringBuilder.AppendLine(KingdomCivilWarTexts.Prompt("KW_Prompt_073") + f.ExistingNames);
		}

		stringBuilder.AppendLine(KingdomCivilWarTexts.Prompt("AF_KINGDOM_CW_RebellionNaming_UserPrompt_Footer"));
		return stringBuilder.ToString().TrimEnd();
	}

	internal static string WeeklyLeadIn(string title, string brief, string summary)
	{
		string text = NormalizeSource(brief, 260);
		if (string.IsNullOrWhiteSpace(text))
			text = NormalizeSource(summary, 500);
		if (string.IsNullOrWhiteSpace(text))
			return KingdomCivilWarTexts.Prompt("AF_KINGDOM_CW_RebellionNaming_Weekly_None");
		string heading = NormalizeSource(title, 80);
		return string.IsNullOrWhiteSpace(heading) ? text : heading + "：" + text;
	}

	internal static string HeroBackground(bool exists, string encyclopedia, string heroName, string clanName, string culture)
	{
		if (!exists)
		{
			return KingdomCivilWarTexts.Prompt("AF_KINGDOM_CW_RebellionNaming_Weekly_None");
		}

		string text = NormalizeSource(encyclopedia, 900);
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}

		string heroDisplayName = heroName;
		string clanDisplayName = clanName;
		string text2 = (culture ?? "").Trim();
		string value = string.IsNullOrWhiteSpace(text2) ? "" : (text2 + KingdomCivilWarTexts.Prompt("KW_Prompt_074"));
		return NormalizeSource(heroDisplayName + KingdomCivilWarTexts.Prompt("KW_Prompt_075") + clanDisplayName + KingdomCivilWarTexts.Prompt("KW_Prompt_076") + value + KingdomCivilWarTexts.Prompt("KW_Prompt_077"), 300);
	}

	internal static string KingdomBackground(bool exists, string encyclopedia, string opening, string name)
	{
		if (!exists)
		{
			return KingdomCivilWarTexts.Prompt("AF_KINGDOM_CW_RebellionNaming_Weekly_None");
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

		return name + KingdomCivilWarTexts.Prompt("KW_Prompt_078");
	}

	internal static string SettlementBackground(bool exists, string encyclopedia, string name, bool town, bool castle, string culture, string kingdom)
	{
		if (!exists)
		{
			return KingdomCivilWarTexts.Prompt("AF_KINGDOM_CW_RebellionNaming_Weekly_None");
		}

		string text = NormalizeSource(encyclopedia, 600);
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}

		string settlementDisplayName = name;
		string text2 = town ? KingdomCivilWarTexts.Prompt("AF_KINGDOM_CW_RebellionNaming_Settlement_Town") : (castle ? KingdomCivilWarTexts.Prompt("AF_KINGDOM_CW_RebellionNaming_Settlement_Castle") : KingdomCivilWarTexts.Prompt("AF_KINGDOM_CW_RebellionNaming_Settlement_Default"));
		string text3 = (culture ?? "").Trim();
		string kingdomDisplayName = kingdom;
		return NormalizeSource(settlementDisplayName + KingdomCivilWarTexts.Prompt("KW_Prompt_079") + text2 + KingdomCivilWarTexts.Prompt("KW_Prompt_080") + (string.IsNullOrWhiteSpace(text3) ? KingdomCivilWarTexts.Prompt("AF_KINGDOM_CW_RebellionNaming_Settlement_CultureUnknown") : text3) + KingdomCivilWarTexts.Prompt("KW_Prompt_081") + kingdomDisplayName + "。", 220);
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
			return KingdomCivilWarTexts.Prompt("KW_Prompt_082") + name + KingdomCivilWarTexts.Prompt("KW_Prompt_083") + text + KingdomCivilWarTexts.Prompt("KW_Prompt_084");
		}

		return KingdomCivilWarTexts.Prompt("AF_KINGDOM_CW_RebellionNaming_Background_Fallback");
	}
	internal static string SettlementLine(string name, string background) => "- " + name + "：" + background;

	internal static string JoinSettlementSummary(System.Collections.Generic.List<string> list)
	{
		return list == null || list.Count == 0 ? KingdomCivilWarTexts.Prompt("AF_KINGDOM_CW_RebellionNaming_Weekly_None") : string.Join("\n", list);
	}

	internal static string FollowerLine(string clan, string hero, string fortifications) => clan + KingdomCivilWarTexts.Prompt("KW_Prompt_085") + hero + "；" + fortifications + "）";

	internal static string JoinFollowerSummary(System.Collections.Generic.List<string> list)
	{
		return list == null || list.Count == 0 ? KingdomCivilWarTexts.Prompt("AF_KINGDOM_CW_RebellionNaming_Weekly_None") : string.Join("\n", list.Distinct(StringComparer.OrdinalIgnoreCase).Select((string x) => "- " + x));
	}

}

internal sealed class RebellionNamingFacts
{
	internal int WeekIndex;
	internal string ClanName, LeaderName, CultureName, KingdomName, KingName, RulingClanName, SettlementNames, RebelBackground, SettlementSummary, FollowerSummary, LeaderBackground, KingBackground, KingdomBackground, KingdomWeekly, WorldWeekly, ExistingNames;
}
