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
			"declare_war" => initiatorName + "的统治者在面对面交涉中向" + targetName + "正式宣战。",
			"propose_peace" or "accept_peace" => initiatorName + "与" + targetName + "已经通过面对面交涉达成和平。",
			"propose_alliance" or "accept_alliance" => initiatorName + "与" + targetName + "已经通过面对面交涉缔结同盟。",
			"break_alliance" => initiatorName + "在面对面交涉后终止了与" + targetName + "的同盟。",
			"propose_trade" or "accept_trade" => initiatorName + "与" + targetName + "已经通过面对面交涉缔结贸易协定。",
			"cancel_trade" => initiatorName + "在面对面交涉后终止了与" + targetName + "的贸易协定。",
			_ => initiatorName + "与" + targetName + "完成了一次具有公开影响的面对面外交交涉。"
		};
		return result + (string.IsNullOrWhiteSpace(reason) ? "" : "\n\n缘由：" + reason.Trim());
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
			.Replace("预先核验的结果路线", "可行的交涉方向")
			.Replace("预核验结果路线", "可行的交涉方向")
			.Replace("预核验", "事先审议")
			.Replace("既定外交动作", "正式外交决定")
			.Replace("候选路线", "可行方向")
			.Replace("结果路线", "交涉方向")
			.Replace("程序执行", "正式施行")
			.Replace("游戏外交状态", "外交关系")
			.Replace("世界外交状态", "外交局势")
			.Replace("游戏外交动作", "正式外交行动")
			.Replace("世界状态", "当前局势")
			.Replace("硬目标", "首要目标")
			.Replace("外交回合", "外交交涉")
			.Replace("本回合", "本次交涉")
			.Replace("该回合", "此次交涉")
			.Replace("此回合", "此次交涉")
			.Replace("回合开始", "交涉开始")
			.Replace("回合结束", "交涉告一段落")
			.Replace("接力顺序", "公文往来次序")
			.Replace("接力轮次", "公文往来阶段")
			.Replace("最后行动机会", "最后决定")
			.Replace("程序核验", "正式确认");
	}


	public static string FormatCessionCandidates(IEnumerable<string> idNamePairs)
	{
		List<string> values = (idNamePairs ?? Enumerable.Empty<string>()).Where(x => x != null).ToList();
		return values.Count == 0 ? "[]" : "[" + string.Join("；", values) + "]";
	}

	public static string BuildBilateralStateLabel(bool atWar, bool allied, bool hasTradeAgreement)
	{
		if (atWar) return "双方正在交战";
		if (allied) return "双方处于同盟关系";
		if (hasTradeAgreement) return "双方和平并有贸易协定";
		return "双方处于和平状态。";
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
	{
		if (pressure < 55f) return "几乎无意谈和";
		if (pressure < 125f) return "暂不急于谈和，但会衡量条件";
		if (pressure < 210f) return "愿意认真考虑和平条件";
		return "迫切希望以可接受条件结束战争";
	}

	public static string DescribeStrengthBalance(float authorStrength, float targetStrength)
	{
		float ratio = authorStrength / Math.Max(1f, targetStrength);
		if (ratio >= 1.75f) return "发文国明显占优";
		if (ratio >= 1.2f) return "发文国略占优势";
		if (ratio <= 0.57f) return "发文国明显处于劣势";
		if (ratio <= 0.83f) return "发文国略处下风";
		return "大体势均力敌";
	}

	public static string DescribeWarProgress(float authorProgress, float targetProgress)
	{
		float difference = authorProgress - targetProgress;
		if (difference >= 20f) return "发文国取得了明显主动";
		if (difference >= 5f) return "发文国稍占上风";
		if (difference <= -20f) return "发文国明显受挫";
		if (difference <= -5f) return "发文国稍处下风";
		return "尚未分出明显高下";
	}

	public static string DescribeOtherWarBurden(int otherWars)
	{
		if (otherWars <= 0) return "没有其他战线牵制";
		if (otherWars == 1) return "另有一条战线需要兼顾";
		return "正受到多线战争牵制";
	}

	public static string DescribeRulerRelation(int relation)
	{
		if (relation <= -60) return "彼此仇视";
		if (relation <= -20) return "关系紧张";
		if (relation < 20) return "关系冷淡";
		if (relation < 60) return "关系尚可";
		return "彼此亲近";
	}

	public static string DescribeRealmRelationProfile(WorldDiplomacyRealmRelationProfile profile)
	{
		if (profile == null || profile.SamplePairCount == 0) return "缺少可靠往来记录";
		string baseAttitude = profile.AverageRelation >= 25f ? "普遍亲近" : profile.AverageRelation >= 8f ? "大体友善"
			: profile.AverageRelation <= -25f ? "普遍敌视" : profile.AverageRelation <= -8f ? "积怨较深" : "总体谨慎";
		if (profile.Polarization >= 28f) baseAttitude += "但国内贵族意见分裂";
		if (profile.RulerEliteGap >= 25f) baseAttitude += "，统治者比本国贵族更亲近对方";
		else if (profile.RulerEliteGap <= -25f) baseAttitude += "，统治者比本国贵族更敌视对方";
		return baseAttitude;
	}

	public static string DescribeWarDuration(int days, int daysPerYear)
	{
		if (days < 14) return "持续了不到半个月";
		if (days < 35) return "持续了约一个月";
		if (days < 84) return "持续了数月";
		if (days < daysPerYear * 2) return "持续了超过一年";
		return "延续了多年";
	}

	private static readonly Regex InternalMetricWithNumberRegex = new Regex(
		@"(?:战争进展|战争进度|战局进度|议和开放度|和平开放度|劣势评分|优势评分|战争压力(?:值|分数)?|(?:(?:外交|本国在诸国中的)?(?:声誉|信誉|名誉|威信))(?:值|点数|分数)?|统治者关系(?:值|点数)?|(?:家族|封臣|王族|王室)关系(?:值|点数)?|(?:(?:所有|各)?封臣家族|各?封臣|家族)(?:对|与|同|和)(?:当前)?(?:王族|王室)(?:的)?(?:关系|好感)?|(?:王族|王室)(?:对|与|同|和)(?:(?:所有|各)?封臣家族|各?封臣)(?:的)?(?:关系|好感)?|(?:所有|各)?封臣家族和(?:王族|王室)的关系|与(?:王族|王室)关系|关系点数|好感度|战力值|总战力)[^。\r\n]{0,16}(?:[-+]?\d+(?:\.\d+)?|[零〇一二三四五六七八九十百千万]+)(?:分|点)?|(?:领先|落后|高出|低于)[^。\r\n]{0,8}(?:[-+]?\d+(?:\.\d+)?|[零〇一二三四五六七八九十百千万]+)(?:分|点)",
		RegexOptions.Compiled | RegexOptions.CultureInvariant);

	private static readonly Regex InternalMetricTermRegex = new Regex(
		@"(?:议和|和平)开放度|(?:优势|劣势)评分|外交(?:声誉|信誉)(?:值|点数|分数)|数值阈值|(?:系统|模型|AI|程序)(?:判定|评分|数据|数值|字段)|游戏(?:机制|数据|数值)",
		RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

	private static readonly Regex ConversationalDiplomacyPhraseRegex = new Regex(
		@"让我(?:说说|把话说清楚)|你(?:应该谢我|自己选|真不知道|若知道|说得很重|先把|别急)|我(?:替你|跟你|告诉你|不想要|想要的是)|我们之间的(?:对话|话)|等你(?:答复|回话)|先这样|话说回来|说白了|这没什么好谈的",
		RegexOptions.Compiled | RegexOptions.CultureInvariant);

	private static readonly Regex PrivateFirstPersonRegex = new Regex(
		@"我(?!国|方|朝|军|王|邦|境|土)",
		RegexOptions.Compiled | RegexOptions.CultureInvariant);

	private static readonly Regex DirectSecondPersonRegex = new Regex(
		@"你(?:们|的)?|您(?:的)?",
		RegexOptions.Compiled | RegexOptions.CultureInvariant);

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
		int yearMarker = text.IndexOf('年');
		int dayMarker = text.LastIndexOf('日');
		if (yearMarker > 0 && dayMarker > yearMarker)
		{
			string yearDigits = new string(text.Substring(0, yearMarker).Where(char.IsDigit).ToArray());
			string dayDigits = new string(text.Substring(yearMarker + 1, dayMarker - yearMarker - 1).Where(char.IsDigit).ToArray());
			if (int.TryParse(yearDigits, NumberStyles.Integer, CultureInfo.InvariantCulture, out int year))
			{
				int season = text.IndexOf('夏') >= 0 ? 1 : text.IndexOf('秋') >= 0 ? 2 : text.IndexOf('冬') >= 0 ? 3 : 0;
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
			"本回合", "该回合", "此回合", "外交回合", "接力顺序", "接力轮次", "最后行动机会", "程序核验",
			"预先核验", "预核验", "结果路线", "候选路线", "既定外交动作", "程序执行", "游戏外交", "世界状态", "硬目标",
			"提示词", "缓存命中", "JSON字段", "系统字段", "程序字段", "AI模型", "游戏机制"))
		{
			reason = "internal_round_term_exposed_in_public_declaration";
			return true;
		}
		int privateFirstPersonCount = PrivateFirstPersonRegex.Matches(visibleText).Count;
		int directSecondPersonCount = DirectSecondPersonRegex.Matches(visibleText).Count;
		bool hasConversationalPhrase = ConversationalDiplomacyPhraseRegex.IsMatch(visibleText);
		if ((hasConversationalPhrase && privateFirstPersonCount >= 2 && directSecondPersonCount >= 2)
			|| (privateFirstPersonCount >= 4 && directSecondPersonCount >= 4))
		{
			reason = "private_chat_style_in_public_declaration";
			return true;
		}
		return false;
	}
public static string DocumentTypeLabel(WorldDiplomacyDocument document)
	{
		if (document == null)
		{
			return "外交公告";
		}
		if (document.IsReminder)
		{
			return "谈判催促";
		}
		if (document.Actions?.Count > 1) return "复合外交宣言";
		return WorldDiplomacyIntentVocabulary.NormalizeIntent(document.Intent) switch
		{
			"declare_war" => "宣战告知",
			"propose_peace" => "和平申请",
			"accept_peace" => "和平回应",
			"reject_peace" => "和平拒绝",
			"propose_alliance" => "同盟申请",
			"accept_alliance" => "同盟回应",
			"reject_alliance" => "同盟拒绝",
			"break_alliance" => "解盟告知",
			"propose_trade" => "贸易申请",
			"accept_trade" => "贸易回应",
			"reject_trade" => "贸易拒绝",
			"cancel_trade" => "贸易终止",
            "release_subject" => "臣属国独立告知",
			"comply_ultimatum" => "退让",
			"ultimatum" => "最后通牒",
			"warning" => "谴责",
			"condemn" => "公开谴责",
			"apology" => "外交致歉",
			"concession" => "外交让步",
			_ => document.IsResponse ? "谈判回应" : (document.RequiresResponse ? "谈判等待" : "外交公告")
		};
	}
public static string BuildFallbackDocumentTitle(WorldDiplomacyDocument document, string intent)
	{
		string target = string.IsNullOrWhiteSpace(document?.TargetKingdomName) || document.TargetKingdomName == "未知王国"
			? ""
			: document.TargetKingdomName;
		string subject = WorldDiplomacyIntentVocabulary.NormalizeIntent(intent) switch
		{
			"declare_war" => "正式宣战",
			"propose_peace" => "提出和平方案",
			"accept_peace" => "宣布接受和平",
			"reject_peace" => "拒绝和平条件",
			"propose_alliance" => "提出结盟",
			"accept_alliance" => "宣布缔结同盟",
			"reject_alliance" => "拒绝结盟",
			"break_alliance" => "宣布解除同盟",
			"propose_trade" => "提出贸易协定",
			"accept_trade" => "宣布达成贸易协定",
			"reject_trade" => "拒绝贸易协定",
			"cancel_trade" => "宣布终止贸易协定",
            "release_subject" => "宣布释放臣属国",
			"comply_ultimatum" => "宣布服从最后通牒",
			"ultimatum" => "发出最后通牒",
			"warning" => "发布谴责",
			"condemn" => "公开谴责",
			"apology" => "公开致歉",
			"concession" => "公布外交让步",
			_ => document?.IsResponse == true ? "回应外交主张" : "阐明王国立场"
		};
		return Limit(string.IsNullOrWhiteSpace(target) ? subject : "对" + target + subject, 36);
	}
public static string BuildArchiveIndexDocumentTitle(WorldDiplomacyDocument document)
	{
		string title = WorldDiplomacyTextRules.SanitizePublicDiplomacyText(WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(document?.Title, document?.AuthorKingdomName + "发布外交宣言", "外交宣言"));
		string cleaned = Regex.Replace(
			title,
			@"^\s*(?:致|复|回复|回应|答复|答|回)\s*[^：:\r\n]{1,48}[：:]\s*",
			"",
			RegexOptions.CultureInvariant);
		string targetName = (document?.TargetKingdomName ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(targetName))
		{
			cleaned = Regex.Replace(
				cleaned,
				@"^\s*(?:致|复|回复|回应|答复|答|回)\s*" + Regex.Escape(targetName) + @"(?:王国|帝国|王庭)?(?:的)?\s*[：:—\-·]*\s*",
				"",
				RegexOptions.CultureInvariant);
		}
		return WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(cleaned, title, "外交宣言");
	}

	public static string BuildCompactDocumentMemoryLine(WorldDiplomacyDocument document,
		Func<int, string> formatDayFallback)
	{
		if (document == null)
		{
			return "";
		}
		string target = document.Actions?.Count > 1
			? "对" + string.Join("、", document.Actions.Where(x => x != null)
				.Select(x => WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(x.TargetKingdomName, x.TargetKingdomId) + "=" + WorldDiplomacyIntentVocabulary.IntentLabel(x.Intent)))
			: (string.IsNullOrWhiteSpace(document.TargetKingdomName) ? "" : "致" + document.TargetKingdomName);
		string result = string.IsNullOrWhiteSpace(document.MechanicalResult) ? "" : "；结果：" + document.MechanicalResult;
		string date = string.IsNullOrWhiteSpace(document.GameDate) ? formatDayFallback(document.Day) : document.GameDate;
		return date
			+ "，"
			+ document.AuthorKingdomName
			+ target
			+ "发布《"
			+ document.Title
			+ "》（"
			+ (document.Actions?.Count > 1 ? "复合外交动作" : WorldDiplomacyIntentVocabulary.IntentLabel(document.Intent))
			+ "）"
			+ result;
	}

	public static string BuildDetailedDocumentMemoryLine(WorldDiplomacyDocument document,
		Func<int, string> formatDayFallback)
	{
		if (document == null) return "";
		string response = document.RequiresResponse ? "；该公文明确等待回应" : "";
		string source = string.IsNullOrWhiteSpace(document.SourceDocumentId) ? "" : "；回应来源=" + document.SourceDocumentId;
		string body = string.IsNullOrWhiteSpace(document.Body) ? "" : "；具体诉求与条件=" + Limit(document.Body, 800);
		return BuildCompactDocumentMemoryLine(document, formatDayFallback) + response + source + body;
	}

	public static void AppendProactiveDiscussionDocument(StringBuilder sb, WorldDiplomacyDocument document,
		Func<int, string> formatDayFallback)
	{
		if (document == null)
		{
			return;
		}
		sb.AppendLine("- " + BuildCompactDocumentMemoryLine(document, formatDayFallback)
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
		sb.Append("议题：").Append(WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(round?.RoundTopic, documents?.FirstOrDefault()?.Title, "外交交涉"));
		if (declarations.Count > 0) sb.Append("。宣言经过：").Append(string.Join("；", declarations));
		sb.Append(results.Count > 0 ? "。游戏确认结果：" + string.Join("；", results) : "。游戏确认结果：没有正式外交机制发生");
		sb.Append("。结束原因：").Append(WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(round?.CloseReason, "事件结束"));
		return Limit(sb.ToString(), 2400);
	}

	public static string BuildDisplayedDocumentTitle(WorldDiplomacyDocument document)
	{
		return SanitizePublicDiplomacyText(WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(
			document?.Title,
			document?.AuthorKingdomName + "发布外交宣言",
			"外交宣言"));
	}

	public static string BuildNotificationDescription(WorldDiplomacyDocument document,
		Func<int, string> formatDayFallback)
	{
		if (document == null)
		{
			return "点击查看外交宣言。";
		}
		string targetNames = document.Actions?.Count > 1
			? string.Join("、", document.Actions.Where(x => x != null)
				.Select(x => WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(x.TargetKingdomName, x.TargetKingdomId))
				.Where(x => !string.IsNullOrWhiteSpace(x))
				.Distinct(StringComparer.CurrentCulture))
			: document.TargetKingdomName;
		string target = string.IsNullOrWhiteSpace(targetNames) ? "" : " · " + targetNames;
		return WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(document.GameDate, formatDayFallback(document.Day))
			+ " · "
			+ DocumentTypeLabel(document)
			+ " · "
			+ document.AuthorKingdomName
			+ target
			+ "。点击查看全文。";
	}

	public static string BuildDocumentEventMeta(WorldDiplomacyDocument document,
		Func<string, WorldDiplomacyRound> resolveRound,
		Func<string, WorldDiplomacyDocument> resolveDocument)
	{
		WorldDiplomacyRound round = resolveRound(document?.RoundId);
		string topic = SanitizePublicDiplomacyText(WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(round?.RoundTopic, resolveDocument(round?.RootDocumentId)?.Title));
		return string.IsNullOrWhiteSpace(topic) ? "" : "  ·  外交事件：" + Limit(topic, 48);
	}

public static string FormatBattleKingdomNames(IEnumerable<string> kingdomIds,
		Func<string, string> kingdomName)
	{
		List<string> names = WorldDiplomacyRoundLifecycleRules.NormalizeIdListPreserveOrder((kingdomIds ?? Enumerable.Empty<string>())
			.Select(id => kingdomName(id)));
		return names.Count == 0 ? "未知王国" : string.Join("、", names);
	}
public static string FormatBattleFactForPrompt(WorldDiplomacyBattleFact fact,
		Func<string, string> kingdomName, Func<int, string> formatDayFallback)
	{
		string attackers = FormatBattleKingdomNames(fact?.AttackerKingdomIds, kingdomName);
		string defenders = FormatBattleKingdomNames(fact?.DefenderKingdomIds, kingdomName);
		string winner = string.Equals(fact?.WinnerSide, "attacker", StringComparison.OrdinalIgnoreCase) ? attackers : defenders;
		string attackerLeaders = string.Join("、", fact?.AttackerLeaderNames ?? new List<string>());
		string defenderLeaders = string.Join("、", fact?.DefenderLeaderNames ?? new List<string>());
		return "- " + WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(fact?.GameDate, formatDayFallback(fact?.Day ?? 0))
			+ "，" + WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(fact?.Location, "野外") + "的" + WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(fact?.BattleType, "战斗")
			+ "：攻方=" + attackers + "，守方=" + defenders + "，胜方=" + winner
			+ (string.IsNullOrWhiteSpace(attackerLeaders) ? "" : "，攻方已记录领主=" + attackerLeaders)
			+ (string.IsNullOrWhiteSpace(defenderLeaders) ? "" : "，守方已记录领主=" + defenderLeaders)
			+ "。本记录没有提供可靠兵力、伤亡或俘虏信息，不得补写；列出的参战领主不代表其已被俘。";
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
		string author = WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(document?.AuthorKingdomName, kingdomName(document?.AuthorKingdomId), "某国");
		string targetId = WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(document?.TargetKingdomId, document?.Actions?.FirstOrDefault()?.TargetKingdomId, document?.AddressedKingdomIds?.FirstOrDefault());
		string target = string.IsNullOrWhiteSpace(targetId) ? "" : kingdomName(targetId);
		HashSet<string> intents = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		if (document?.Actions?.Count > 0)
		{
			foreach (WorldDiplomacyDocumentAction action in document.Actions.Where(x => x != null)) intents.Add(WorldDiplomacyIntentVocabulary.NormalizeIntent(action.Intent));
		}
		else intents.Add(WorldDiplomacyIntentVocabulary.NormalizeIntent(document?.Intent));
		string subject = intents.Contains("declare_war") ? "战争事宜"
			: intents.Any(x => x == "propose_peace" || x == "accept_peace" || x == "reject_peace") ? "停战事宜"
			: intents.Any(x => x == "propose_alliance" || x == "accept_alliance" || x == "reject_alliance" || x == "break_alliance") ? "盟约事宜"
			: intents.Any(x => x == "propose_trade" || x == "accept_trade" || x == "reject_trade" || x == "cancel_trade") ? "贸易事宜"
			: intents.Any(x => x == "ultimatum" || x == "warning" || x == "comply_ultimatum") ? "最后通牒事宜"
			: intents.Any(x => x == "apology" || x == "concession") ? "外交让步"
			: "当前外交局势";
		return string.IsNullOrWhiteSpace(target)
			? "据传" + author + "王庭发布了一份新的外交宣言，似乎涉及" + subject + "。"
			: "据传" + author + "王庭发布了一份面向" + target + "的外交宣言，似乎涉及" + subject + "。";
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
		Func<string, string> kingdomName, Func<int, string> formatDayFallback)
	{
		if (string.IsNullOrWhiteSpace(authorId) || string.IsNullOrWhiteSpace(targetId))
		{
			return "双方身份无效；不得陈述具体战斗。";
		}
		int cutoff = currentDay - retentionDays;
		List<WorldDiplomacyBattleFact> battles = (recentBattles ?? Enumerable.Empty<WorldDiplomacyBattleFact>())
			.Where(x => WorldDiplomacyDocumentFactRules.IsBilateralBattleFact(x, authorId, targetId) && x.Day >= cutoff)
			.OrderByDescending(x => x.Day)
			.Take(MaxPromptRecentBattles)
			.ToList();
		if (battles.Count == 0)
		{
			return "最近" + retentionDays.ToString(CultureInfo.InvariantCulture)
				+ "个游戏日内没有记录到双方之间已经结束的战斗。双方可能仍处于战争状态，但不得声称发生过任何具体战役或给出战果数字。";
		}
		return string.Join("\n", battles.Select(x => FormatBattleFactForPrompt(x, kingdomName, formatDayFallback)));
	}

	public static List<string> GetRecentOwnInternationalReputationReasons(IReadOnlyList<WorldDiplomacyDocument> documents,
		string kingdomId, int currentDay, int retentionDays, Func<int, string> formatDayFallback)
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
			string direction = document.InternationalReputationEvaluationDelta > 0 ? "改善" : "受损";
			reasons.Add(WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(document.GameDate, formatDayFallback(document.Day))
				+ "《" + WorldDiplomacyTextRules.Limit(WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(document.Title, "公开外交宣言"), 50) + "》：评价方向="
				+ direction + "；原因=" + reason);
		}
		return reasons;
	}

	public static List<string> GetRecentPublicNegativeReputationFacts(IReadOnlyList<WorldDiplomacyDocument> documents,
		string kingdomId, int currentDay, int retentionDays, Func<int, string> formatDayFallback)
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
				+ "《" + WorldDiplomacyTextRules.Limit(WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(document.Title, "公开外交宣言"), 50) + "》：" + reason);
		}
		return facts;
	}

	public static string BuildLowReputationConflictOpportunityContext(int reputation,
		IEnumerable<string> legalActions, IReadOnlyList<WorldDiplomacyDocument> documents, string targetId,
		int currentDay, int retentionDays, Func<int, string> formatDayFallback)
	{
		if (reputation >= LowInternationalReputationThreshold) return "";
		HashSet<string> legalThreatActions = new HashSet<string>((legalActions ?? Enumerable.Empty<string>())
			.Select(WorldDiplomacyIntentVocabulary.NormalizeIntent)
			.Where(x => x is "warning" or "ultimatum"), StringComparer.OrdinalIgnoreCase);
		if (legalThreatActions.Count == 0)
		{
			return "";
		}

		List<string> recentFacts = GetRecentPublicNegativeReputationFacts(documents, targetId, currentDay, retentionDays, formatDayFallback);
		if (recentFacts.Count == 0)
		{
			return "【国际声誉冲突机会】该国国际声誉低下，但近期没有可援引的具体公开失信事实。"
				+ "这只能作为审慎、疏远或要求保证的背景，不能单独支持外交警告或战争最后通牒；不得编造违约、背盟、敌对或军事行为。";
		}

		string severity = reputation < SevereInternationalReputationThreshold ? "严重国际失信" : "国际声誉低下";
		string legalThreatSummary = string.Join("/", legalThreatActions.OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
		return "【国际声誉冲突机会】该国处于“" + WorldDiplomacyReputationRules.DescribeInternationalReputation(reputation) + "”档位，属于" + severity
			+ "。近期主要失信事实为：" + string.Join("；", recentFacts) + "。"
			+ "这构成可利用的外交冲突机会，但不是强制行动。根据本国国家性格、长期战略、军力与现实利益，"
			+ "可以使用当前合法的" + legalThreatSummary + "进行正式谴责、外交警告或索取保证；"
			+ (reputation < SevereInternationalReputationThreshold && legalThreatActions.Contains("ultimatum")
				? "若失信与现实争端都足够严重，也可发出战争最后通牒；"
				: "若不足以升级，也可暂不行动；")
			+ "不得编造未提供的失信与敌对事实。";
	}

	public static string DescribeWarPressure(int pressure)
	{
		if (pressure < 20) return "压力较低";
		if (pressure < 60) return "摩擦正在积累";
		if (pressure < 120) return "压力很高";
		return "局势已经十分危险";
	}
	public static string DescribeBorderRelation(WorldDiplomacyBorderRelation relation)
	{
		if (relation?.SharesBorder != true) return "两国当前没有共同边境";
		string first = WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(relation.FirstSettlementName, relation.FirstSettlementId, "一处边地要塞");
		string second = WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(relation.SecondSettlementName, relation.SecondSettlementId, "另一处边地要塞");
		return "两国当前接壤，最直接的边地联系位于" + first + "与" + second + "一带";
	}

}
