using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;

namespace AnimusForge.Refactor.Domain;

public static class WorldDiplomacyTextRules
{
	public static string BuildExternalFactBody(string action, string initiatorName, string targetName, string reason)
	{
		string result = action switch
		{
			"declare_war" => initiatorName + WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_000", "") + targetName + WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_001", ""),
			"propose_peace" or "accept_peace" => initiatorName + WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_002", "") + targetName + WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_003", ""),
			"propose_alliance" or "accept_alliance" => initiatorName + WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_002", "") + targetName + WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_004", ""),
			"break_alliance" => initiatorName + WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_005", "") + targetName + WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_006", ""),
			"propose_trade" or "accept_trade" => initiatorName + WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_002", "") + targetName + WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_007", ""),
			"cancel_trade" => initiatorName + WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_005", "") + targetName + WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_008", ""),
			_ => initiatorName + WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_002", "") + targetName + WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_009", "")
		};
		return result + (string.IsNullOrWhiteSpace(reason) ? "" : WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_010", "") + reason.Trim());
	}

	public static string NormalizeBody(string value)
	{
		string text = (value ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Trim();
		return Limit(text, 6000);
	}

	public static string NormalizeCanonicalHistoryText(string value)
	{
		// Canonical artifacts and configured-size snapshots must never inherit the per-document
		// display cap. Compression, rather than silent truncation, owns their size control.
		return (value ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Trim();
	}

	public static string FormatDiplomaticBodyForDisplay(string value)
	{
		string text = NormalizeBody(value);
		if (string.IsNullOrWhiteSpace(text)) return "";
		List<string> paragraphs = new List<string>();
		foreach (string line in text.Split('\n'))
		{
			string paragraph = (line ?? "").Trim().TrimStart('　');
			if (string.IsNullOrWhiteSpace(paragraph)) continue;
			AppendDiplomaticDisplayParagraphs(paragraphs, paragraph);
		}
		return string.Join("\n\n", paragraphs.Select(x => "　　" + x));
	}

	public static void AppendDiplomaticDisplayParagraphs(List<string> target, string paragraph)
	{
		if (target == null || string.IsNullOrWhiteSpace(paragraph)) return;
		if (paragraph.Length <= 220)
		{
			target.Add(paragraph.Trim());
			return;
		}
		StringBuilder current = new StringBuilder();
		foreach (char ch in paragraph)
		{
			current.Append(ch);
			bool sentenceEnd = ch == '。' || ch == '！' || ch == '？' || ch == '!' || ch == '?' || ch == '；' || ch == ';';
			bool fallbackBreak = (current.Length >= 220 && (ch == '，' || ch == ',' || ch == '、')) || current.Length >= 260;
			if ((current.Length >= 120 && sentenceEnd) || fallbackBreak)
			{
				target.Add(current.ToString().Trim());
				current.Clear();
			}
		}
		string tail = current.ToString().Trim();
		if (string.IsNullOrWhiteSpace(tail)) return;
		if (target.Count > 0 && tail.Length < 45) target[target.Count - 1] += tail;
		else target.Add(tail);
	}

	public static string DeriveTitle(string body, string fallback)
	{
		string firstLine = (body ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Split('\n').FirstOrDefault()?.Trim() ?? "";
		firstLine = firstLine.Trim('《', '》', '"', '\'', '“', '”');
		return Limit(WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(firstLine, fallback), 36);
	}

	public static string SanitizePublicDiplomacyText(string value)
	{
		string text = value ?? "";
		return text
			.Replace(WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_011", ""), WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_012", ""))
			.Replace(WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_013", ""), WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_012", ""))
			.Replace(WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_014", ""), WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_015", ""))
			.Replace(WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_016", ""), WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_017", ""))
			.Replace(WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_018", ""), WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_019", ""))
			.Replace(WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_020", ""), WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_021", ""))
			.Replace(WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_022", ""), WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_023", ""))
			.Replace(WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_024", ""), WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_025", ""))
			.Replace(WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_026", ""), WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_027", ""))
			.Replace(WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_028", ""), WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_029", ""))
			.Replace(WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_030", ""), WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_031", ""))
			.Replace(WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_032", ""), WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_033", ""))
			.Replace(WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_034", ""), WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_035", ""))
			.Replace(WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_036", ""), WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_037", ""))
			.Replace(WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_038", ""), WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_039", ""))
			.Replace(WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_040", ""), WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_039", ""))
			.Replace(WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_041", ""), WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_042", ""))
			.Replace(WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_043", ""), WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_044", ""))
			.Replace(WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_045", ""), WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_046", ""))
			.Replace(WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_047", ""), WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_048", ""))
			.Replace(WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_049", ""), WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_050", ""))
			.Replace(WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_051", ""), WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_052", ""));
	}


	public static string FormatCessionCandidates(IEnumerable<string> idNamePairs)
	{
		List<string> values = (idNamePairs ?? Enumerable.Empty<string>()).Where(x => x != null).ToList();
		return values.Count == 0 ? "[]" : "[" + string.Join("；", values) + "]";
	}

	public static string BuildBilateralStateLabel(bool atWar, bool allied, bool hasTradeAgreement)
		=> BuildBilateralStateLabel(atWar, allied, hasTradeAgreement, null);

	public static string BuildBilateralStateLabel(bool atWar, bool allied, bool hasTradeAgreement, Func<string, string, string> localize)
	{
		if (atWar) return WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Fact_000", "The two kingdoms are at war");
		if (allied) return WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Fact_001", "The two kingdoms are allied");
		if (hasTradeAgreement) return WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Fact_002", "The two kingdoms are at peace with a trade agreement");
		return WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Fact_003", "The two kingdoms are at peace.");
	}

		public static string CompactPromptFact(string value, int maxChars)
	{
		string compact = string.Join(" ", (value ?? "")
			.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
		return Limit(compact, Math.Max(0, maxChars));
	}

	public static string Limit(string value, int maxChars)
	{
		string text = value ?? "";
		return text.Length <= maxChars ? text : text.Substring(0, Math.Max(0, maxChars));
	}

	public static string DescribePeacePressure(float pressure)
		=> DescribePeacePressure(pressure, null);

	public static string DescribePeacePressure(float pressure, Func<string, string, string> localize)
	{
		if (pressure < 55f) return WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Fact_004", "Almost no interest in peace");
		if (pressure < 125f) return WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Fact_005", "Not urgent to make peace, but willing to weigh terms");
		if (pressure < 210f) return WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Fact_006", "Willing to seriously consider peace terms");
		return WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Fact_007", "Eager to end the war on acceptable terms");
	}

	public static string DescribeStrengthBalance(float authorStrength, float targetStrength)
		=> DescribeStrengthBalance(authorStrength, targetStrength, null);

	public static string DescribeStrengthBalance(float authorStrength, float targetStrength, Func<string, string, string> localize)
	{
		float ratio = authorStrength / Math.Max(1f, targetStrength);
		if (ratio >= 1.75f) return WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Fact_008", "The issuing kingdom has a clear advantage");
		if (ratio >= 1.2f) return WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Fact_009", "The issuing kingdom has a slight advantage");
		if (ratio <= 0.57f) return WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Fact_010", "The issuing kingdom is at a clear disadvantage");
		if (ratio <= 0.83f) return WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Fact_011", "The issuing kingdom is at a slight disadvantage");
		return WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Fact_012", "Broadly evenly matched");
	}

	public static string DescribeWarProgress(float authorProgress, float targetProgress)
		=> DescribeWarProgress(authorProgress, targetProgress, null);

	public static string DescribeWarProgress(float authorProgress, float targetProgress, Func<string, string, string> localize)
	{
		float difference = authorProgress - targetProgress;
		if (difference >= 20f) return WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Fact_013", "The issuing kingdom has gained a clear initiative");
		if (difference >= 5f) return WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Fact_014", "The issuing kingdom has a slight edge");
		if (difference <= -20f) return WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Fact_015", "The issuing kingdom has suffered clear setbacks");
		if (difference <= -5f) return WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Fact_016", "The issuing kingdom is slightly on the back foot");
		return WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Fact_017", "Neither side has a clear advantage yet");
	}

	public static string DescribeOtherWarBurden(int otherWars)
		=> DescribeOtherWarBurden(otherWars, null);

	public static string DescribeOtherWarBurden(int otherWars, Func<string, string, string> localize)
	{
		if (otherWars <= 0) return WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Fact_018", "No other fronts divert its forces");
		if (otherWars == 1) return WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Fact_019", "One other front also needs attention");
		return WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Fact_020", "Multiple fronts are tying down its forces");
	}

	public static string DescribeRulerRelation(int relation)
		=> DescribeRulerRelation(relation, null);

	public static string DescribeRulerRelation(int relation, Func<string, string, string> localize)
	{
		if (relation <= -60) return WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Fact_021", "Mutual hatred");
		if (relation <= -20) return WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Fact_022", "Strained relations");
		if (relation < 20) return WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Fact_023", "Cool relations");
		if (relation < 60) return WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Fact_024", "Reasonable relations");
		return WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Fact_025", "Close relations");
	}

	public static string DescribeRealmRelationProfile(WorldDiplomacyRealmRelationProfile profile)
		=> DescribeRealmRelationProfile(profile, null);

	public static string DescribeRealmRelationProfile(WorldDiplomacyRealmRelationProfile profile, Func<string, string, string> localize)
	{
		if (profile == null || profile.SamplePairCount == 0) return WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Fact_026", "Insufficient reliable records of relations");
		string baseAttitude = profile.AverageRelation >= 25f ? WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Fact_027", "Generally close") : profile.AverageRelation >= 8f ? WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Fact_028", "Broadly friendly")
			: profile.AverageRelation <= -25f ? WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Fact_029", "Generally hostile") : profile.AverageRelation <= -8f ? WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Fact_030", "Deep-seated grievances") : WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Fact_031", "Generally cautious");
		if (profile.Polarization >= 28f) baseAttitude += WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Fact_032", ", but the kingdom's nobles are divided");
		if (profile.RulerEliteGap >= 25f) baseAttitude += WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Fact_033", "; the ruler is closer to the other side than the kingdom's nobles are");
		else if (profile.RulerEliteGap <= -25f) baseAttitude += WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Fact_034", "; the ruler is more hostile to the other side than the kingdom's nobles are");
		return baseAttitude;
	}

	public static string DescribeWarDuration(int days, int daysPerYear)
		=> DescribeWarDuration(days, daysPerYear, null);

	public static string DescribeWarDuration(int days, int daysPerYear, Func<string, string, string> localize)
	{
		if (days < 14) return WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Fact_035", "lasted less than half a month");
		if (days < 35) return WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Fact_036", "lasted about a month");
		if (days < 84) return WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Fact_037", "lasted several months");
		if (days < daysPerYear * 2) return WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Fact_038", "lasted more than a year");
		return WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Fact_039", "continued for years");
	}

	private static readonly Regex InternalMetricWithNumberRegex = new Regex(
		WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_053", ""),
		RegexOptions.Compiled | RegexOptions.CultureInvariant);

	private static readonly Regex InternalMetricTermRegex = new Regex(
		WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_054", ""),
		RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

	public static bool ContainsWholeNumber(string text, int value)
	{
		return Regex.IsMatch(text ?? "", @"(?<!\d)" + Regex.Escape(value.ToString(CultureInfo.InvariantCulture)) + @"(?!\d)", RegexOptions.CultureInvariant);
	}

	public static string StripGeneratedActionReasonPrefix(string reason, out int actionIndex)
	{
		actionIndex = -1;
		string normalized = (reason ?? "").Trim();
		if (!normalized.StartsWith("action[", StringComparison.OrdinalIgnoreCase)) return normalized;
		int close = normalized.IndexOf("]:", StringComparison.Ordinal);
		if (close <= 7
			|| !int.TryParse(normalized.Substring(7, close - 7), NumberStyles.Integer,
				CultureInfo.InvariantCulture, out int parsedIndex)
			|| parsedIndex < 0) return normalized;
		actionIndex = parsedIndex;
		return normalized.Substring(close + 2);
	}

	public static int ParseDayForArchive(string value)
	{
		string text = value ?? "";
		int yearMarker = text.IndexOf(WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_058", "")[0]);
		int dayMarker = text.LastIndexOf(WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_059", "")[0]);
		if (yearMarker > 0 && dayMarker > yearMarker)
		{
			string yearDigits = new string(text.Substring(0, yearMarker).Where(char.IsDigit).ToArray());
			string dayDigits = new string(text.Substring(yearMarker + 1, dayMarker - yearMarker - 1).Where(char.IsDigit).ToArray());
			if (int.TryParse(yearDigits, NumberStyles.Integer, CultureInfo.InvariantCulture, out int year))
			{
				int season = text.IndexOf(WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_060", "")[0]) >= 0 ? 1 : text.IndexOf(WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_061", "")[0]) >= 0 ? 2 : text.IndexOf(WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_062", "")[0]) >= 0 ? 3 : 0;
				int.TryParse(dayDigits, NumberStyles.Integer, CultureInfo.InvariantCulture, out int dayOfSeason);
				return year * 1000 + season * 100 + dayOfSeason;
			}
		}
		string digits = new string(text.Where(char.IsDigit).ToArray());
		return int.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out int day) ? day : 0;
	}

	public static bool TryGetImmersionViolation(string visibleText, out string reason)
	{
		reason = "";
		if (string.IsNullOrWhiteSpace(visibleText)) return false;
		if (InternalMetricTermRegex.IsMatch(visibleText) || InternalMetricWithNumberRegex.IsMatch(visibleText))
		{
			reason = "internal_metric_exposed_in_public_declaration";
			return true;
		}
		if (WorldDiplomacyIntentVocabulary.ContainsAny(visibleText,
			WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_036", ""), WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_038", ""), WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_040", ""), WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_034", ""), WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_045", ""), WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_047", ""), WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_049", ""), WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_051", ""),
			WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_063", ""), WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_014", ""), WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_020", ""), WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_018", ""), WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_016", ""), WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_022", ""), WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_064", ""), WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_030", ""), WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_032", ""),
			WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_065", ""), WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_066", ""), WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_067", ""), WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_068", ""), WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_069", ""), WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_070", ""), WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_071", "")))
		{
			reason = "internal_round_term_exposed_in_public_declaration";
			return true;
		}
		return false;
	}
public static string DocumentTypeLabel(WorldDiplomacyDocument document, Func<string, string, string> localize = null)
	{
		if (document == null)
		{
			return WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_072", "Diplomatic announcement");
		}
		if (document.IsReminder)
		{
			return WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_073", "Negotiation reminder");
		}
		if (document.Actions?.Count > 1) return WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_074", "Compound diplomatic declaration");
		return WorldDiplomacyIntentVocabulary.NormalizeIntent(document.Intent) switch
		{
			"declare_war" => WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_075", "War declaration notice"),
			"propose_peace" => WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_076", "Peace request"),
			"accept_peace" => WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_077", "Peace response"),
			"reject_peace" => WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_078", "Peace rejection"),
			"propose_alliance" => WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_079", "Alliance request"),
			"accept_alliance" => WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_080", "Alliance response"),
			"reject_alliance" => WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_081", "Alliance rejection"),
			"break_alliance" => WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_082", "Alliance dissolution notice"),
			"propose_trade" => WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_083", "Trade request"),
			"accept_trade" => WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_084", "Trade response"),
			"reject_trade" => WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_085", "Trade rejection"),
			"cancel_trade" => WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_086", "Trade termination"),
            "release_subject" => WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_087", "Subject independence notice"),
			"comply_ultimatum" => WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_088", "Concession"),
			"ultimatum" => WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_089", "Ultimatum"),
			"warning" => WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_090", "Condemnation"),
			"condemn" => WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_091", "Public condemnation"),
			"apology" => WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_092", "Diplomatic apology"),
			"concession" => WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_093", "Diplomatic concession"),
			_ => document.IsResponse ? WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_094", "Negotiation response") : (document.RequiresResponse ? WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_095", "Awaiting negotiation") : WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_072", "Diplomatic announcement"))
		};
	}
public static string BuildFallbackDocumentTitle(WorldDiplomacyDocument document, string intent, Func<string, string, string> localize = null)
	{
		string target = string.IsNullOrWhiteSpace(document?.TargetKingdomName) || document.TargetKingdomName == WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_096", "")
			? ""
			: document.TargetKingdomName;
		string subject = WorldDiplomacyIntentVocabulary.NormalizeIntent(intent) switch
		{
			"declare_war" => WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_097", "Formal declaration of war"),
			"propose_peace" => WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_098", "Propose a peace plan"),
			"accept_peace" => WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_099", "Announce acceptance of peace"),
			"reject_peace" => WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_100", "Reject peace terms"),
			"propose_alliance" => WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_101", "Propose an alliance"),
			"accept_alliance" => WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_102", "Announce an alliance"),
			"reject_alliance" => WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_103", "Reject an alliance"),
			"break_alliance" => WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_104", "Announce dissolution of the alliance"),
			"propose_trade" => WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_105", "Propose a trade agreement"),
			"accept_trade" => WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_106", "Announce a trade agreement"),
			"reject_trade" => WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_107", "Reject a trade agreement"),
			"cancel_trade" => WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_108", "Announce termination of trade"),
            "release_subject" => WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_109", "Announce release of a subject kingdom"),
			"comply_ultimatum" => WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_110", "Announce compliance with the ultimatum"),
			"ultimatum" => WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_111", "Issue an ultimatum"),
			"warning" => WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_112", "Issue a condemnation"),
			"condemn" => WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_091", "Public condemnation"),
			"apology" => WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_113", "Public apology"),
			"concession" => WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_114", "Announce diplomatic concessions"),
			_ => document?.IsResponse == true ? WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_115", "Respond to diplomatic claims") : WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_116", "Clarify the kingdom’s position")
		};
		return Limit(string.IsNullOrWhiteSpace(target) ? subject : WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_117", "To ") + target + (localize == null ? "" : localize("FallbackDocumentTitleTargetSeparator", ": ")) + subject, 36);
	}
public static string BuildArchiveIndexDocumentTitle(WorldDiplomacyDocument document, Func<string, string, string> localize = null)
	{
		string title = WorldDiplomacyTextRules.SanitizePublicDiplomacyText(WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(document?.Title, document?.AuthorKingdomName + WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_118", " publishes a diplomatic declaration"), WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_119", "Diplomatic declaration")));
		string cleaned = Regex.Replace(
			title,
			WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_120", ""),
			"",
			RegexOptions.CultureInvariant);
		string targetName = (document?.TargetKingdomName ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(targetName))
		{
			cleaned = Regex.Replace(
				cleaned,
				WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_121", "") + Regex.Escape(targetName) + WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_122", ""),
				"",
				RegexOptions.CultureInvariant);
		}
		return WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(cleaned, title, WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_119", "Diplomatic declaration"));
	}

	public static string BuildCompactDocumentMemoryLine(WorldDiplomacyDocument document,
		Func<int, string> formatDayFallback, Func<string, string, string> localize = null)
	{
		if (document == null)
		{
			return "";
		}
		string target = document.Actions?.Count > 1
			? WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_117", "") + string.Join(PromptPunctuation(localize, "AF_DIP_Source_ActionListSeparator", "、", ", "), document.Actions.Where(x => x != null)
				.Select(x => WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(x.TargetKingdomName, x.TargetKingdomId) + "=" + WorldDiplomacyIntentVocabulary.IntentLabel(x.Intent, localize)))
			: (string.IsNullOrWhiteSpace(document.TargetKingdomName) ? "" : WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_123", "") + document.TargetKingdomName);
		string result = string.IsNullOrWhiteSpace(document.MechanicalResult) ? "" : WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_124", "") + document.MechanicalResult;
		string date = string.IsNullOrWhiteSpace(document.GameDate) ? formatDayFallback(document.Day) : document.GameDate;
		return date
			+ PromptPunctuation(localize, "AF_DIP_Source_DateSeparator", "，", ", ")
			+ document.AuthorKingdomName
			+ target
			+ WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_125", "")
			+ document.Title
			+ PromptPunctuation(localize, "AF_DIP_Source_TitleClose", "》", "\"")
			+ PromptPunctuation(localize, "AF_DIP_Source_ActionOpen", "（", " (")
			+ (document.Actions?.Count > 1 ? WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_126", "") : WorldDiplomacyIntentVocabulary.IntentLabel(document.Intent, localize))
			+ PromptPunctuation(localize, "AF_DIP_Source_ActionClose", "）", ")")
			+ result;
	}
	private static string PromptPunctuation(Func<string, string, string> localize, string key, string chinese, string english)
		=> localize == null ? chinese : localize(key, english);

	public static string BuildDetailedDocumentMemoryLine(WorldDiplomacyDocument document,
		Func<int, string> formatDayFallback, Func<string, string, string> localize = null)
	{
		if (document == null) return "";
		string response = document.RequiresResponse ? WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_127", "") : "";
		string source = string.IsNullOrWhiteSpace(document.SourceDocumentId) ? "" : WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_128", "") + document.SourceDocumentId;
		string body = string.IsNullOrWhiteSpace(document.Body) ? "" : WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_129", "") + Limit(document.Body, 800);
		return BuildCompactDocumentMemoryLine(document, formatDayFallback, localize) + response + source + body;
	}

	public static void AppendProactiveDiscussionDocument(StringBuilder sb, WorldDiplomacyDocument document,
		Func<int, string> formatDayFallback, Func<string, string, string> localize = null)
	{
		if (document == null)
		{
			return;
		}
		sb.AppendLine("- " + BuildCompactDocumentMemoryLine(document, formatDayFallback, localize)
			+ (string.IsNullOrWhiteSpace(document.Body) ? "" : "：" + Limit(document.Body, 240)));
	}

	public static string BuildLocalRoundSummaryText(WorldDiplomacyRound round, List<WorldDiplomacyDocument> documents,
		Func<int, string> formatDayFallback)
	{
		List<string> declarations = (documents ?? new List<WorldDiplomacyDocument>()).Where(x => x != null)
			.Take(12).Select(x => BuildCompactDocumentMemoryLine(x, formatDayFallback)).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
		List<string> results = WorldDiplomacyRoundLifecycleRules.NormalizeIdListPreserveOrder((documents ?? new List<WorldDiplomacyDocument>()).Where(x => x?.ChangedDiplomaticState == true && !string.IsNullOrWhiteSpace(x.MechanicalResult))
			.Select(x => x.MechanicalResult.Trim())).Take(8).ToList();
		StringBuilder sb = new StringBuilder();
		sb.Append(WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_130", "")).Append(WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(round?.RoundTopic, documents?.FirstOrDefault()?.Title, WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_035", "")));
		if (declarations.Count > 0) sb.Append(WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_131", "")).Append(string.Join("；", declarations));
		sb.Append(results.Count > 0 ? WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_132", "") + string.Join("；", results) : WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_133", ""));
		sb.Append(WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_134", "")).Append(WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(round?.CloseReason, WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_135", "")));
		return Limit(sb.ToString(), 2400);
	}

	public static string BuildDisplayedDocumentTitle(WorldDiplomacyDocument document, Func<string, string, string> localize = null)
	{
		return SanitizePublicDiplomacyText(WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(
			document?.Title,
			document?.AuthorKingdomName + WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_118", " publishes a diplomatic declaration"),
			WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_119", "Diplomatic declaration")));
	}

	public static string BuildNotificationDescription(WorldDiplomacyDocument document,
		Func<int, string> formatDayFallback, Func<string, string, string> localize = null,
		Func<string, string> projectDateForUi = null)
	{
		if (document == null)
		{
			return WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_136", "Click to view the diplomatic declaration.");
		}
		string targetNames = document.Actions?.Count > 1
			? string.Join("、", document.Actions.Where(x => x != null)
				.Select(x => WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(x.TargetKingdomName, x.TargetKingdomId))
				.Where(x => !string.IsNullOrWhiteSpace(x))
				.Distinct(StringComparer.CurrentCulture))
			: document.TargetKingdomName;
		string target = string.IsNullOrWhiteSpace(targetNames) ? "" : " · " + targetNames;
		string date = WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(document.GameDate, formatDayFallback(document.Day));
		if (projectDateForUi != null) date = projectDateForUi(date);
		return date
			+ " · "
			+ DocumentTypeLabel(document, localize)
			+ " · "
			+ document.AuthorKingdomName
			+ target
			+ WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_137", ". Click to read the full text.");
	}

	public static string BuildDocumentEventMeta(WorldDiplomacyDocument document,
		Func<string, WorldDiplomacyRound> resolveRound,
		Func<string, WorldDiplomacyDocument> resolveDocument)
	{
		WorldDiplomacyRound round = resolveRound(document?.RoundId);
		string topic = SanitizePublicDiplomacyText(WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(round?.RoundTopic, resolveDocument(round?.RootDocumentId)?.Title));
		return string.IsNullOrWhiteSpace(topic) ? "" : WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_138", "") + Limit(topic, 48);
	}

public static string FormatBattleKingdomNames(IEnumerable<string> kingdomIds,
		Func<string, string> kingdomName, Func<string, string, string> localize = null)
	{
		List<string> names = WorldDiplomacyRoundLifecycleRules.NormalizeIdListPreserveOrder((kingdomIds ?? Enumerable.Empty<string>())
			.Select(id => kingdomName(id)));
		return names.Count == 0 ? WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_096", "Unknown kingdom") : string.Join(localize == null ? "、" : localize("PromptListSeparator", ", "), names);
	}
public static string FormatBattleFactForPrompt(WorldDiplomacyBattleFact fact,
		Func<string, string> kingdomName, Func<int, string> formatDayFallback, Func<string, string, string> localize = null)
	{
		string attackers = FormatBattleKingdomNames(fact?.AttackerKingdomIds, kingdomName, localize);
		string defenders = FormatBattleKingdomNames(fact?.DefenderKingdomIds, kingdomName, localize);
		string winner = string.Equals(fact?.WinnerSide, "attacker", StringComparison.OrdinalIgnoreCase) ? attackers : defenders;
		string attackerLeaders = string.Join("、", fact?.AttackerLeaderNames ?? new List<string>());
		string defenderLeaders = string.Join("、", fact?.DefenderLeaderNames ?? new List<string>());
        return "- " + DiplomacyDateProjection.ForPrompt(WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(fact?.GameDate, formatDayFallback(fact?.Day ?? 0)), localize)
            + (localize == null ? "，" : localize("PromptDateLocationSeparator", ", ")) + WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(DiplomacyLegacyPromptProjection.BattleLocation(fact?.Location, localize), WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_139", "")) + WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_140", "") + WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(ProjectBattleTypeForPrompt(fact?.BattleType, localize), WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_141", ""))
			+ WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_142", "") + attackers + WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_143", "") + defenders + WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_144", "") + winner
			+ (string.IsNullOrWhiteSpace(attackerLeaders) ? "" : WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_145", "") + attackerLeaders)
			+ (string.IsNullOrWhiteSpace(defenderLeaders) ? "" : WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_146", "") + defenderLeaders)
			+ WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_147", "");
	}

    private static readonly string[] BattleTypePromptKeys = { "BattleTypeSiege", "BattleTypeRaid", "BattleTypeField" };

    private static string ProjectBattleTypeForPrompt(string type, Func<string, string, string> localize)
    {
        if (localize == null) return type;
        foreach (string key in BattleTypePromptKeys)
        {
            if (string.Equals(type, WorldDiplomacyPromptContractRules.ContractText(null, key, ""), StringComparison.Ordinal))
                return localize(key, key == "BattleTypeSiege" ? "Siege battle" : key == "BattleTypeRaid" ? "Raid battle" : "Field battle");
        }
        return type;
    }
public static string BuildRoundCompressionPrompt(IEnumerable<WorldDiplomacyDocument> documents,
		Func<int, string> formatDayFallback)
	{
		StringBuilder sb = new StringBuilder();
		foreach (WorldDiplomacyDocument document in (documents ?? Enumerable.Empty<WorldDiplomacyDocument>()).Take(120)) sb.AppendLine("[" + document.DocumentId + "] " + BuildCompactDocumentMemoryLine(document, formatDayFallback));
		return sb.ToString();
	}

	public static string BuildDiplomacyRumor(WorldDiplomacyDocument document,
		Func<string, string> kingdomName)
	{
		string author = WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(document?.AuthorKingdomName, kingdomName(document?.AuthorKingdomId), WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_148", ""));
		string targetId = WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(document?.TargetKingdomId, document?.Actions?.FirstOrDefault()?.TargetKingdomId, document?.AddressedKingdomIds?.FirstOrDefault());
		string target = string.IsNullOrWhiteSpace(targetId) ? "" : kingdomName(targetId);
		HashSet<string> intents = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		if (document?.Actions?.Count > 0)
		{
			foreach (WorldDiplomacyDocumentAction action in document.Actions.Where(x => x != null)) intents.Add(WorldDiplomacyIntentVocabulary.NormalizeIntent(action.Intent));
		}
		else intents.Add(WorldDiplomacyIntentVocabulary.NormalizeIntent(document?.Intent));
		string subject = intents.Contains("declare_war") ? WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_149", "")
			: intents.Any(x => x == "propose_peace" || x == "accept_peace" || x == "reject_peace") ? WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_150", "")
			: intents.Any(x => x == "propose_alliance" || x == "accept_alliance" || x == "reject_alliance" || x == "break_alliance") ? WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_151", "")
			: intents.Any(x => x == "propose_trade" || x == "accept_trade" || x == "reject_trade" || x == "cancel_trade") ? WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_152", "")
			: intents.Any(x => x == "ultimatum" || x == "warning" || x == "comply_ultimatum") ? WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_153", "")
			: intents.Any(x => x == "apology" || x == "concession") ? WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_093", "")
			: WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_154", "");
		return string.IsNullOrWhiteSpace(target)
			? WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_155", "") + author + WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_156", "") + subject + "。"
			: WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_155", "") + author + WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_157", "") + target + WorldDiplomacyPromptContractRules.ContractText(null, "AF_DIP_Source_158", "") + subject + "。";
	}

	internal const int MaxPromptRecentBattles = 5;
	internal const int LowInternationalReputationThreshold = 40;
	internal const int SevereInternationalReputationThreshold = 20;
	internal const int MaxPromptRecentNegativeReputationFacts = 2;
	internal const int MaxPromptRecentOwnReputationReasons = 2;

	public static string BuildRecentBilateralDocumentContext(string activeRoundId,
		IEnumerable<WorldDiplomacyKingdomKnowledge> kingdomKnowledge,
		IEnumerable<WorldDiplomacyDocument> documents,
		string sourceId, string targetId, int maxCount,
		Func<int, string> formatDayFallback)
	{
		if (string.IsNullOrWhiteSpace(activeRoundId)) return "";
		WorldDiplomacyKingdomKnowledge knowledge = (kingdomKnowledge ?? Enumerable.Empty<WorldDiplomacyKingdomKnowledge>()).FirstOrDefault(x => x != null && string.Equals(x.KingdomId, sourceId, StringComparison.OrdinalIgnoreCase));
		HashSet<string> knownIds = new HashSet<string>(knowledge?.DocumentIds ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
		return string.Join("\n", (documents ?? Enumerable.Empty<WorldDiplomacyDocument>())
			.Where(x => x != null
				&& !x.IsCompressed
				&& WorldDiplomacyRoundLifecycleRules.IsRecordInRound(x.RoundId, activeRoundId)
				&& knownIds.Contains(x.DocumentId ?? "")
				&& ((string.Equals(x.AuthorKingdomId, sourceId, StringComparison.OrdinalIgnoreCase)
						&& string.Equals(x.TargetKingdomId, targetId, StringComparison.OrdinalIgnoreCase))
					|| (string.Equals(x.AuthorKingdomId, targetId, StringComparison.OrdinalIgnoreCase)
						&& string.Equals(x.TargetKingdomId, sourceId, StringComparison.OrdinalIgnoreCase))))
			.OrderByDescending(x => x.Day)
			.Take(maxCount)
			.Select(x => "- " + WorldDiplomacyTextRules.BuildCompactDocumentMemoryLine(x, formatDayFallback)));
	}

	public static string BuildRecentBilateralBattleContext(IEnumerable<WorldDiplomacyBattleFact> recentBattles,
		string authorId, string targetId, int currentDay, int retentionDays,
		Func<string, string> kingdomName, Func<int, string> formatDayFallback, Func<string, string, string> localize = null)
	{
		if (string.IsNullOrWhiteSpace(authorId) || string.IsNullOrWhiteSpace(targetId))
		{
			return WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_159", "");
		}
		int cutoff = currentDay - retentionDays;
		List<WorldDiplomacyBattleFact> battles = (recentBattles ?? Enumerable.Empty<WorldDiplomacyBattleFact>())
			.Where(x => WorldDiplomacyDocumentFactRules.IsBilateralBattleFact(x, authorId, targetId) && x.Day >= cutoff)
			.OrderByDescending(x => x.Day)
			.Take(MaxPromptRecentBattles)
			.ToList();
		if (battles.Count == 0)
		{
			return WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_160", "") + retentionDays.ToString(CultureInfo.InvariantCulture)
				+ WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_161", "");
		}
		return string.Join("\n", battles.Select(x => FormatBattleFactForPrompt(x, kingdomName, formatDayFallback, localize)));
	}

	public static List<string> GetRecentOwnInternationalReputationReasons(IReadOnlyList<WorldDiplomacyDocument> documents,
		string kingdomId, int currentDay, int retentionDays, Func<int, string> formatDayFallback, Func<string, string, string> localize = null)
	{
		List<string> reasons = new List<string>(MaxPromptRecentOwnReputationReasons);
		string normalizedKingdomId = (kingdomId ?? "").Trim();
		if (normalizedKingdomId.Length == 0 || documents == null)
		{
			return reasons;
		}
		int cutoffDay = currentDay - retentionDays;
		for (int i = documents.Count - 1;
			i >= 0 && reasons.Count < MaxPromptRecentOwnReputationReasons;
			i--)
		{
			WorldDiplomacyDocument document = documents[i];
			if (document == null
				|| !document.IsReadyForPublication
				|| !document.InternationalReputationSettled
				|| document.InternationalReputationEvaluationDelta == 0
				|| document.Day < cutoffDay
				|| !string.Equals(document.AuthorKingdomId, normalizedKingdomId, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			string reason = WorldDiplomacyTextRules.Limit(document.InternationalReputationEvaluationReason, 120);
			if (string.IsNullOrWhiteSpace(reason)) continue;
			string direction = document.InternationalReputationEvaluationDelta > 0 ? WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_162", "") : WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_163", "");
			reasons.Add(WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(document.GameDate, formatDayFallback(document.Day))
				+ "《" + WorldDiplomacyTextRules.Limit(WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(document.Title, WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_164", "")), 50) + WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_165", "")
				+ direction + WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_166", "") + reason);
		}
		return reasons;
	}

	public static List<string> GetRecentPublicNegativeReputationFacts(IReadOnlyList<WorldDiplomacyDocument> documents,
		string kingdomId, int currentDay, int retentionDays, Func<int, string> formatDayFallback, Func<string, string, string> localize = null)
	{
		List<string> facts = new List<string>(MaxPromptRecentNegativeReputationFacts);
		string normalizedKingdomId = (kingdomId ?? "").Trim();
		if (normalizedKingdomId.Length == 0 || documents == null)
		{
			return facts;
		}
		int cutoffDay = currentDay - retentionDays;
		for (int i = documents.Count - 1;
			i >= 0 && facts.Count < MaxPromptRecentNegativeReputationFacts;
			i--)
		{
			WorldDiplomacyDocument document = documents[i];
			if (document == null
				|| !document.IsReadyForPublication
				|| !document.InternationalReputationSettled
				|| document.InternationalReputationEvaluationDelta >= 0
				|| document.Day < cutoffDay
				|| !string.Equals(document.AuthorKingdomId, normalizedKingdomId, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			string reason = WorldDiplomacyTextRules.Limit(document.InternationalReputationEvaluationReason, 120);
			if (string.IsNullOrWhiteSpace(reason))
			{
				continue;
			}
			facts.Add(WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(document.GameDate, formatDayFallback(document.Day))
				+ "《" + WorldDiplomacyTextRules.Limit(WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(document.Title, WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_164", "")), 50) + "》：" + reason);
		}
		return facts;
	}

	public static string BuildLowReputationConflictOpportunityContext(int reputation,
		IEnumerable<string> legalActions, IReadOnlyList<WorldDiplomacyDocument> documents, string targetId,
		int currentDay, int retentionDays, Func<int, string> formatDayFallback, Func<string, string, string> localize = null)
	{
		if (reputation >= LowInternationalReputationThreshold) return "";
		HashSet<string> legalThreatActions = new HashSet<string>((legalActions ?? Enumerable.Empty<string>())
			.Select(WorldDiplomacyIntentVocabulary.NormalizeIntent)
			.Where(x => x is "warning" or "ultimatum"), StringComparer.OrdinalIgnoreCase);
		if (legalThreatActions.Count == 0)
		{
			return "";
		}

		List<string> recentFacts = GetRecentPublicNegativeReputationFacts(documents, targetId, currentDay, retentionDays, formatDayFallback, localize);
		if (recentFacts.Count == 0)
		{
			return WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_167", "")
				+ WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_168", "");
		}

		string severity = reputation < SevereInternationalReputationThreshold ? WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_169", "") : WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_170", "");
		string legalThreatSummary = string.Join("/", legalThreatActions.OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
		return WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_171", "") + WorldDiplomacyReputationRules.DescribeInternationalReputation(reputation, localize) + WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_172", "") + severity
			+ WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_173", "") + string.Join("；", recentFacts) + "。"
			+ WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_174", "")
			+ WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_175", "") + legalThreatSummary + WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_176", "")
			+ (reputation < SevereInternationalReputationThreshold && legalThreatActions.Contains("ultimatum")
				? WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_177", "")
				: WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_178", ""))
			+ WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Source_179", "");
	}

	public static string DescribeWarPressure(int pressure)
		=> DescribeWarPressure(pressure, null);

	public static string DescribeWarPressure(int pressure, Func<string, string, string> localize)
	{
		if (pressure < 20) return WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Fact_040", "Low pressure");
		if (pressure < 60) return WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Fact_041", "Friction is building");
		if (pressure < 120) return WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Fact_042", "High pressure");
		return WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Fact_043", "The situation is already extremely dangerous");
	}
	public static string DescribeBorderRelation(WorldDiplomacyBorderRelation relation)
		=> DescribeBorderRelation(relation, null);

	public static string DescribeBorderRelation(WorldDiplomacyBorderRelation relation, Func<string, string, string> localize)
	{
		if (relation?.SharesBorder != true) return WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Fact_044", "The two kingdoms currently share no border");
		string first = WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(relation.FirstSettlementName, relation.FirstSettlementId, WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Fact_045", "a frontier stronghold"));
		string second = WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(relation.SecondSettlementName, relation.SecondSettlementId, WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Fact_046", "another frontier stronghold"));
		return WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Fact_047", "The two kingdoms currently share a border; their most direct frontier connection lies around ") + first + WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Fact_048", " and ") + second + WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_Fact_049", "");
	}

}
