using System;
using System.Text.RegularExpressions;

namespace AnimusForge;
internal enum PatienceMood
{
	Neutral,
	Delighted,
	Joy,
	Annoyed,
	Bored
}

internal static class PatienceRules
{
	private static readonly string[] PatienceLevelTexts = new[]
	{
		"枯竭",
		"烦躁",
		"不耐",
		"冷淡",
		"一般",
		"尚可",
		"愿听",
		"投入",
		"热络",
		"兴致高"
	};
	private static readonly Regex MoodTagRegex = new Regex(@"\[ACTION:MOOD:([^\]\r\n]+)\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);
	private static int ClampInt(int v, int min, int max) => Math.Max(min, Math.Min(max, v));
	private static float ClampFloat(float v, float min, float max) => Math.Max(min, Math.Min(max, v));
	internal static int ComputePatienceMaxFromRelation(int relation)
	{
		double a = ((relation >= 0) ? (30.0 + (double)relation * 0.5) : (30.0 + (double)relation * 0.18));
		return ClampInt((int)Math.Round(a), 10, 80);
	}

	private static readonly string[] RelationLevelTexts = new string[10] { "死敌", "敌对", "厌恶", "疏离", "冷漠", "中立", "熟络", "友好", "亲近", "至交" };

	internal static int ToTenLevelIndexByRelation(int relation)
	{
		double num = ((double)ClampInt(relation, -100, 100) + 100.0) / 200.0;
		int num2 = (int)Math.Floor(num * 10.0) + 1;
		if (num2 < 1)
		{
			num2 = 1;
		}
		if (num2 > 10)
		{
			num2 = 10;
		}
		return num2;
	}

	internal static string GetRelationLevelText(int relation)
	{
		int num = ToTenLevelIndexByRelation(relation);
		return RelationLevelTexts[num - 1];
	}

	internal static int GetRelationLevelIndex(int relation)
	{
		return ToTenLevelIndexByRelation(relation);
	}

	internal static int ToTenLevelIndexByRatio(float current, int max)
	{
		if (max <= 0)
		{
			return 1;
		}

		double num = ClampFloat(current / (float)max, 0f, 1f);
		int num2 = (int)Math.Ceiling(num * 10.0);
		if (num2 < 1)
		{
			num2 = 1;
		}

		if (num2 > 10)
		{
			num2 = 10;
		}

		return num2;
	}

	internal static string GetPatienceLevelText(float current, int max)
	{
		int num = ToTenLevelIndexByRatio(current, max);
		return PatienceLevelTexts[num - 1];
	}

	internal static PatienceMood ParseMoodToken(string token)
	{
		string text = (token ?? "").Trim().ToLower();
		if (string.IsNullOrEmpty(text))
		{
			return PatienceMood.Neutral;
		}

		switch (text)
		{
			case "delighted":
			case "very_happy":
			case "thrilled":
			case "heartwarmed":
			case "heart_warmed":
			case "affectionate":
			case "fond":
			case "sweet":
				return PatienceMood.Delighted;
			default:
				if (!(text == "鎰夊揩"))
				{
					switch (text)
					{
						default:
							if (!(text == "鐢熸皵"))
							{
								if (text == "bored" || text == "boring" || text == "鏃犺亰")
								{
									return PatienceMood.Bored;
								}

								return PatienceMood.Neutral;
							}

							goto case "annoyed";
						case "annoyed":
						case "angry":
						case "upset":
						case "irritated":
						case "displeased":
						case "涓嶆偊":
							return PatienceMood.Annoyed;
					}
				}

				goto case "joy";
			case "joy":
			case "happy":
			case "positive":
			case "amused":
			case "friendly":
			case "鍠滄偊":
				return PatienceMood.Joy;
		}
	}

	internal static PatienceMood ExtractMoodAndStripTag(ref string text)
	{
		PatienceMood result = PatienceMood.Neutral;
		string text2 = text ?? "";
		MatchCollection matchCollection = MoodTagRegex.Matches(text2);
		if (matchCollection != null && matchCollection.Count > 0)
		{
			string value = matchCollection[matchCollection.Count - 1].Groups[1].Value;
			result = ParseMoodToken(value);
			text2 = MoodTagRegex.Replace(text2, "");
		}

		text = (text2 ?? "").Trim();
		return result;
	}

	internal static int ComputePatienceDelta(PatienceMood mood, ref int noInterestRounds)
	{
		int num;
		switch (mood)
		{
			case PatienceMood.Delighted:
				num = 2;
				noInterestRounds = Math.Max(0, noInterestRounds - 3);
				break;
			case PatienceMood.Joy:
				num = 1;
				noInterestRounds = Math.Max(0, noInterestRounds - 2);
				break;
			case PatienceMood.Annoyed:
				num = -3;
				noInterestRounds++;
				break;
			case PatienceMood.Bored:
				num = -2;
				noInterestRounds++;
				break;
			default:
				num = -1;
				noInterestRounds++;
				break;
		}

		if (mood != PatienceMood.Joy && mood != PatienceMood.Delighted && noInterestRounds >= 3)
		{
			num--;
		}

		return num;
	}

	internal static int ComputeNativeRelationDelta(PatienceMood mood, int currentRelation)
	{
		switch (mood)
		{
			case PatienceMood.Delighted:
				if (currentRelation >= 95)
				{
					return 0;
				}

				return 1;
			case PatienceMood.Annoyed:
				if (currentRelation <= -95)
				{
					return 0;
				}

				return -1;
			default:
				return 0;
		}
	}

	internal static int ComputePrivateLoveDelta(PatienceMood mood)
	{
		switch (mood)
		{
			case PatienceMood.Delighted:
				return 2;
			case PatienceMood.Joy:
				return 1;
			case PatienceMood.Bored:
				return -1;
			case PatienceMood.Annoyed:
				return -2;
			default:
				return 0;
		}
	}

	internal static int ComputeRoyalDomainConversationLoyaltyDelta(PatienceMood mood)
	{
		switch (mood)
		{
			case PatienceMood.Delighted:
				return 4;
			case PatienceMood.Joy:
				return 2;
			default:
				return 0;
		}
	}

	internal static int MaxFromRelation(int relation) => ComputePatienceMaxFromRelation(relation);
	internal static int RelationDelta(PatienceMood mood, int relation) => ComputeNativeRelationDelta(mood, relation);
	internal static PatienceMood StripMood(ref string text) => ExtractMoodAndStripTag(ref text);
	internal static string HeroKey(string id)
	{
		string key = (id ?? "").Trim().ToLower();
		return key.Length == 0 ? "" : "hero:" + key;
	}

	internal static string UnnamedKey(string key, string name)
	{
		string value = (key ?? "").Trim().ToLower();
		if (value.Length > 0)
			return "unnamed:" + value;
		value = (name ?? "").Trim().ToLower();
		return value.Length == 0 ? "" : "name:" + value;
	}

	internal static int HeroRelationEffect(PatienceMood mood, int relation, int before, bool correction) => ComputeNativeRelationDelta(mood, relation) + (!correction && before <= 0 ? -1 : 0);
	internal static bool RoyalLoyaltyEligible(bool hasHero, bool mainHero, bool prisoner, bool family, bool lord) => !hasHero || (!mainHero && !prisoner && !family && !lord);
}
