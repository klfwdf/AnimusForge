using System;using System.Collections.Generic;using System.Linq;using System.Text.RegularExpressions;using static AnimusForge.MyBehavior;
namespace AnimusForge;
internal sealed class WeeklyGenerationRules {private readonly Func<string,string> _render; internal WeeklyGenerationRules(Func<string,string> render){_render=render;}
internal static string BuildWeeklyReportGroupReportId(WeeklyEventMaterialPreviewGroup group)
	{
		if (group == null)
		{
			return "";
		}
		if (string.Equals((group.GroupKind ?? "").Trim(), "world", StringComparison.OrdinalIgnoreCase))
		{
			return "world";
		}
		string text = (group.KingdomId ?? "").Trim();
		return string.IsNullOrWhiteSpace(text) ? "" : ("kingdom:" + text);
	}
internal static Dictionary<string, WeeklyEventMaterialPreviewGroup> BuildWeeklyReportGroupMap(IEnumerable<WeeklyEventMaterialPreviewGroup> groups)
	{
		Dictionary<string, WeeklyEventMaterialPreviewGroup> dictionary = new Dictionary<string, WeeklyEventMaterialPreviewGroup>(StringComparer.OrdinalIgnoreCase);
		foreach (WeeklyEventMaterialPreviewGroup item in groups ?? Enumerable.Empty<WeeklyEventMaterialPreviewGroup>())
		{
			string text = BuildWeeklyReportGroupReportId(item);
			if (!string.IsNullOrWhiteSpace(text) && !dictionary.ContainsKey(text))
			{
				dictionary[text] = item;
			}
		}
		return dictionary;
	}
internal static List<string> BuildWeeklyBatchExpectedReportIds(WeeklyReportBatchRequest batch)
	{
		return (batch?.Groups ?? new List<WeeklyEventMaterialPreviewGroup>()).Select(BuildWeeklyReportGroupReportId).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
	}
internal static string BuildWeeklyReportFailureReason(string response, bool parseFailed)
	{
		string text = (response ?? "").Trim();
		if (parseFailed)
		{
			return LlmRetryPrompt.BuildFailureDetail(string.IsNullOrWhiteSpace(text) ? "模型返回为空，且无法解析。" : "模型返回无法解析。", text);
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			return "接口返回为空响应。";
		}
		return text;
	}
internal static string NormalizeWeeklyReportTagText(string text)
	{
		string text2 = (text ?? "").Replace("\r", "").Trim();
		if (string.IsNullOrWhiteSpace(text2))
		{
			return "";
		}
		List<string> list = new List<string>();
		foreach (string item in text2.Split(new char[1] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
		{
			string text3 = item.Trim();
			if (!string.IsNullOrWhiteSpace(text3))
			{
				list.Add(text3);
			}
		}
		return string.Join("\n", list).Trim();
	}
internal static bool TryValidateWeeklyReportTagText(string rawTagText, out string normalizedTagText, out string stabilityTag, out string failureReason)
	{
		normalizedTagText = "";
		stabilityTag = "";
		failureReason = "";
		string text = NormalizeWeeklyReportTagText(rawTagText);
		if (string.IsNullOrWhiteSpace(text))
		{
			failureReason = "缺少 [TAGS]。";
			return false;
		}
		List<string> list = text.Split(new char[1] { '\n' }, StringSplitOptions.RemoveEmptyEntries).Select((string x) => (x ?? "").Trim()).Where((string x) => !string.IsNullOrWhiteSpace(x)).ToList();
		if (list.Count == 1)
		{
			string text2 = (list[0] ?? "").Trim().ToUpperInvariant();
			if (text2 == "STAB_FLAT" || GetWeeklyReportStabilityDeltaForTag(text2) != 0)
			{
				normalizedTagText = text2;
				stabilityTag = text2;
				return true;
			}
		}
		string text3 = "";
		for (Match match = Regex.Match(text, "(?<![A-Z0-9_])STAB_(?:DOWN_[1-4]|UP_[1-4]|FLAT)(?![A-Z0-9_])", RegexOptions.IgnoreCase); match.Success; match = match.NextMatch())
		{
			string text4 = (match.Value ?? "").Trim().ToUpperInvariant();
			if (string.IsNullOrWhiteSpace(text3))
			{
				text3 = text4;
			}
			else if (!string.Equals(text3, text4, StringComparison.Ordinal))
			{
				failureReason = "包含多个相互冲突的稳定度标签。";
				return false;
			}
		}
		if (string.IsNullOrWhiteSpace(text3))
		{
			failureReason = "缺少合法稳定度标签。";
			return false;
		}
		normalizedTagText = text3;
		stabilityTag = text3;
		return true;
	}
internal static int GetWeeklyReportStabilityDeltaForTag(string tag)
	{
		switch (((tag ?? "").Trim()).ToUpperInvariant())
		{
		case "STAB_DOWN_4":
			return -15;
		case "STAB_DOWN_3":
			return -10;
		case "STAB_DOWN_2":
			return -5;
		case "STAB_DOWN_1":
			return -1;
		case "STAB_UP_1":
			return 1;
		case "STAB_UP_2":
			return 5;
		case "STAB_UP_3":
			return 10;
		case "STAB_UP_4":
			return 15;
		default:
			return 0;
		}
	}
internal static int ExtractWeeklyReportStabilityDelta(string tagText)
	{
		string text = NormalizeWeeklyReportTagText(tagText);
		if (string.IsNullOrWhiteSpace(text))
		{
			return 0;
		}
		int num = 0;
		foreach (string item in text.Split(new char[1] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
		{
			string text2 = (item ?? "").Trim();
			if (string.Equals(text2, "STAB_FLAT", StringComparison.OrdinalIgnoreCase))
			{
				num = 0;
				continue;
			}
			if (text2.StartsWith("STAB_", StringComparison.OrdinalIgnoreCase))
			{
				num = GetWeeklyReportStabilityDeltaForTag(text2);
			}
		}
		return num;
	}
internal static string BuildFallbackWeeklyReportShortSummary(string report)
	{
		string text = NeutralizeWeeklyReportScenarioName(report).Replace("\r", " ").Replace("\n", " ").Trim();
		while (text.Contains("  "))
		{
			text = text.Replace("  ", " ");
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		if (text.Length <= 140)
		{
			return text;
		}
		string text2 = text.Substring(0, 140).TrimEnd();
		int num = Math.Max(text2.LastIndexOf('。'), Math.Max(text2.LastIndexOf('；'), Math.Max(text2.LastIndexOf('，'), text2.LastIndexOf(' '))));
		if (num >= 40)
		{
			text2 = text2.Substring(0, num).TrimEnd();
		}
		return text2.Trim();
	}
internal static string NeutralizeWeeklyReportScenarioName(string text)
	{
		string text2 = (text ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text2))
		{
			return "";
		}
		text2 = text2.Replace("卡拉迪亚大陆", "大陆");
		text2 = text2.Replace("卡拉迪亚", "大陆");
		text2 = Regex.Replace(text2, "\\bCalradia\\b", "大陆", RegexOptions.IgnoreCase);
		while (text2.Contains("大陆大陆"))
		{
			text2 = text2.Replace("大陆大陆", "大陆");
		}
		return text2.Trim();
	}
internal bool TryParseWeeklyBatchResponse(string rawResponse, WeeklyReportBatchRequest batch, out List<WeeklyReportBatchBlockResult> blocks, out List<string> missingReportIds, out string failureReason)
	{
		blocks = new List<WeeklyReportBatchBlockResult>();
		missingReportIds = new List<string>();
		failureReason = "";
		string text = (rawResponse ?? "").Replace("\r", "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			failureReason = "批量周报响应为空。";
			missingReportIds = BuildWeeklyBatchExpectedReportIds(batch);
			return false;
		}
		Dictionary<string, WeeklyEventMaterialPreviewGroup> weeklyReportGroupMap = BuildWeeklyReportGroupMap(batch?.Groups);
		Dictionary<string, WeeklyReportBatchBlockResult> dictionary = new Dictionary<string, WeeklyReportBatchBlockResult>(StringComparer.OrdinalIgnoreCase);
		MatchCollection matchCollection = Regex.Matches(text, "\\[REPORT_BLOCK_BEGIN\\]\\s*(?<body>[\\s\\S]*?)\\s*\\[REPORT_BLOCK_END\\]", RegexOptions.IgnoreCase);
		if (matchCollection.Count == 0)
		{
			failureReason = "响应中没有找到任何 REPORT_BLOCK。";
			missingReportIds = BuildWeeklyBatchExpectedReportIds(batch);
			return false;
		}
		foreach (Match item in matchCollection)
		{
			WeeklyReportBatchBlockResult weeklyReportBatchBlockResult;
			string text2 = (item.Groups["body"]?.Value ?? "").Trim();
			if (!TryParseWeeklyBatchBlock(text2, batch, weeklyReportGroupMap, out weeklyReportBatchBlockResult))
			{
				weeklyReportBatchBlockResult = weeklyReportBatchBlockResult ?? new WeeklyReportBatchBlockResult();
			}
			if (!string.IsNullOrWhiteSpace(weeklyReportBatchBlockResult.ReportId) && (!dictionary.TryGetValue(weeklyReportBatchBlockResult.ReportId, out var value) || (!value.Parsed && weeklyReportBatchBlockResult.Parsed)))
			{
				dictionary[weeklyReportBatchBlockResult.ReportId] = weeklyReportBatchBlockResult;
			}
			blocks.Add(weeklyReportBatchBlockResult);
		}
		List<string> list = new List<string>();
		foreach (string item2 in BuildWeeklyBatchExpectedReportIds(batch))
		{
			if (!dictionary.TryGetValue(item2, out var value2))
			{
				missingReportIds.Add(item2);
			}
			else if (!value2.Parsed)
			{
				missingReportIds.Add(item2);
				list.Add(item2);
			}
		}
		if (missingReportIds.Count > 0)
		{
			failureReason = (list.Count == 0) ? ("批量周报响应缺少部分 report_id：" + string.Join("、", missingReportIds)) : ((list.Count == missingReportIds.Count) ? ("批量周报响应中的 report_id 解析失败：" + string.Join("、", list)) : ("批量周报响应缺少或解析失败的 report_id：" + string.Join("、", missingReportIds)));
		}
		return blocks.Any((WeeklyReportBatchBlockResult x) => x != null && x.Parsed);
	}
internal bool TryParseWeeklyBatchBlock(string rawBlockBody, WeeklyReportBatchRequest batch, Dictionary<string, WeeklyEventMaterialPreviewGroup> groupMap, out WeeklyReportBatchBlockResult block)
	{
		block = new WeeklyReportBatchBlockResult();
		string text = (rawBlockBody ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			block.FailureReason = "空 block。";
			return false;
		}
		block.ReportId = ExtractWeeklyBatchHeaderValue(text, "report_id");
		block.Mode = ExtractWeeklyBatchHeaderValue(text, "mode");
		block.Kind = ExtractWeeklyBatchHeaderValue(text, "kind");
		block.KingdomId = ExtractWeeklyBatchHeaderValue(text, "kingdom_id");
		if (string.IsNullOrWhiteSpace(block.ReportId))
		{
			block.FailureReason = "缺少 report_id。";
			return false;
		}
		if (groupMap == null || !groupMap.TryGetValue(block.ReportId, out var value) || value == null)
		{
			block.FailureReason = "report_id 未在输入批次中找到：" + block.ReportId;
			return false;
		}
		bool flag = value.OutputMode == WeeklyReportOutputMode.TitleShortTagsOnly;
		if (!string.IsNullOrWhiteSpace(block.Mode))
		{
			string text2 = (block.Mode ?? "").Trim().ToLowerInvariant();
			if (flag && !string.Equals(text2, "title_short_tags_only", StringComparison.OrdinalIgnoreCase))
			{
				block.FailureReason = "mode 与输入批次不一致。";
				return false;
			}
			if (!flag && !string.Equals(text2, "full_report", StringComparison.OrdinalIgnoreCase))
			{
				block.FailureReason = "mode 与输入批次不一致。";
				return false;
			}
		}
		if (string.Equals((block.Kind ?? "").Trim(), "kingdom", StringComparison.OrdinalIgnoreCase))
		{
			string text2 = (value.KingdomId ?? "").Trim();
			if (string.IsNullOrWhiteSpace((block.KingdomId ?? "").Trim()) || !string.Equals((block.KingdomId ?? "").Trim(), text2, StringComparison.OrdinalIgnoreCase))
			{
				block.FailureReason = "kingdom_id 与输入批次不一致。";
				return false;
			}
		}
		string text3 = Regex.Replace(text, "^report_id=.*?$", "", RegexOptions.Multiline | RegexOptions.IgnoreCase).Trim();
		text3 = Regex.Replace(text3, "^mode=.*?$", "", RegexOptions.Multiline | RegexOptions.IgnoreCase).Trim();
		text3 = Regex.Replace(text3, "^kind=.*?$", "", RegexOptions.Multiline | RegexOptions.IgnoreCase).Trim();
		text3 = Regex.Replace(text3, "^kingdom_id=.*?$", "", RegexOptions.Multiline | RegexOptions.IgnoreCase).Trim();
		if (!TryParseWeeklyReportResponse(text3, value, (batch != null) ? batch.WeekIndex : 0, out var title, out var shortSummary, out var report, out var tagText))
		{
			block.FailureReason = BuildWeeklyReportFailureReason(text3, parseFailed: true);
			return false;
		}
		block.Title = title;
		block.ShortSummary = shortSummary;
		block.Report = report;
		block.TagText = tagText;
		block.Parsed = true;
		return true;
	}
internal static string ExtractWeeklyBatchHeaderValue(string text, string key)
	{
		if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(key))
		{
			return "";
		}
		Match match = Regex.Match(text, "^" + Regex.Escape(key.Trim()) + "=(?<value>.*)$", RegexOptions.Multiline | RegexOptions.IgnoreCase);
		return match.Success ? ((match.Groups["value"]?.Value ?? "").Trim()) : "";
	}
internal bool TryParseWeeklyReportResponse(string rawResponse, WeeklyEventMaterialPreviewGroup group, int weekIndex, out string title, out string shortSummary, out string report, out string tagText)
	{
		title = "";
		shortSummary = "";
		report = "";
		tagText = "";
		bool flag = (group?.OutputMode ?? WeeklyReportOutputMode.FullReport) == WeeklyReportOutputMode.TitleShortTagsOnly;
		string text = (rawResponse ?? "").Replace("\r", "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		if (flag && text.IndexOf("[REPORT]", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return false;
		}
		Match match = Regex.Match(text, "\\[TITLE\\](?<title>[\\s\\S]*?)(?=\\[SHORT\\]|\\[REPORT\\]|\\[TAGS\\]|$)", RegexOptions.IgnoreCase);
		Match match2 = flag ? Regex.Match(text, "\\[SHORT\\](?<short>[\\s\\S]*?)(?=\\[TAGS\\]|$)", RegexOptions.IgnoreCase) : Regex.Match(text, "\\[SHORT\\](?<short>[\\s\\S]*?)(?=\\[REPORT\\]|\\[TAGS\\]|$)", RegexOptions.IgnoreCase);
		Match match3 = flag ? null : Regex.Match(text, "\\[REPORT\\](?<report>[\\s\\S]*?)(?=\\[TAGS\\]|$)", RegexOptions.IgnoreCase);
		Match match4 = Regex.Match(text, "\\[TAGS\\](?<tags>[\\s\\S]*)$", RegexOptions.IgnoreCase);
		if (match.Success)
		{
			title = (match.Groups["title"]?.Value ?? "").Trim();
		}
		if (match2.Success)
		{
			shortSummary = BuildFallbackWeeklyReportShortSummary(match2.Groups["short"]?.Value ?? "");
		}
		if (match4.Success)
		{
			if (!TryValidateWeeklyReportTagText(match4.Groups["tags"]?.Value ?? "", out tagText, out var _, out _))
			{
				return false;
			}
		}
		if (!flag && match3 != null && match3.Success)
		{
			report = (match3.Groups["report"]?.Value ?? "").Trim();
		}
		if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(shortSummary) || string.IsNullOrWhiteSpace(tagText))
		{
			return false;
		}
		if (flag)
		{
			report = "";
		}
		else if (string.IsNullOrWhiteSpace(report))
		{
			return false;
		}
		if (string.IsNullOrWhiteSpace(shortSummary))
		{
			shortSummary = BuildFallbackWeeklyReportShortSummary(flag ? text : report);
		}
		title = _render(NeutralizeWeeklyReportScenarioName(title));
		shortSummary = BuildFallbackWeeklyReportShortSummary(_render(shortSummary));
		report = _render(NeutralizeWeeklyReportScenarioName(report));
		return true;
	}
internal bool TryParseWeeklyFullOnDemandReportResponse(string rawResponse, out string title, out string shortSummary, out string report)
	{
		title = "";
		shortSummary = "";
		report = "";
		string text = (rawResponse ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		Match match = Regex.Match(text, "\\[TITLE\\](?<title>[\\s\\S]*?)(?=\\[SHORT\\]|\\[REPORT\\]|\\[TAGS\\]|$)", RegexOptions.IgnoreCase);
		Match match2 = Regex.Match(text, "\\[SHORT\\](?<short>[\\s\\S]*?)(?=\\[REPORT\\]|\\[TAGS\\]|$)", RegexOptions.IgnoreCase);
		Match match3 = Regex.Match(text, "\\[REPORT\\](?<report>[\\s\\S]*?)(?=\\[TAGS\\]|$)", RegexOptions.IgnoreCase);
		if (match.Success)
		{
			title = (match.Groups["title"]?.Value ?? "").Trim();
		}
		if (match2.Success)
		{
			shortSummary = BuildFallbackWeeklyReportShortSummary(match2.Groups["short"]?.Value ?? "");
		}
		if (match3.Success)
		{
			report = (match3.Groups["report"]?.Value ?? "").Trim();
		}
		if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(report))
		{
			return false;
		}
		if (string.IsNullOrWhiteSpace(shortSummary))
		{
			shortSummary = BuildFallbackWeeklyReportShortSummary(report);
		}
		title = _render(NeutralizeWeeklyReportScenarioName(title));
		shortSummary = BuildFallbackWeeklyReportShortSummary(_render(shortSummary));
		report = _render(NeutralizeWeeklyReportScenarioName(report));
		return true;
	}
internal static bool IsWeeklyReportBatchPromptPrepared(WeeklyReportBatchRequest batch)
	{
		return batch != null && !string.IsNullOrWhiteSpace(batch.SystemPrompt) && !string.IsNullOrWhiteSpace(batch.UserPrompt);
	}
internal static void CaptureWeeklyReportBatchAttemptFailureMetadata(WeeklyReportBatchRequestResult result, ApiCallResult attempt)
	{
		if (result == null)
		{
			return;
		}
		bool failed = attempt != null && !attempt.Success;
		result.IsRateLimit = failed && attempt.IsRateLimit;
		result.IsRequestsPerMinuteLimit = failed && attempt.IsRequestsPerMinuteLimit;
		result.IsQuotaLimit = failed && attempt.IsQuotaLimit;
		result.RetryAfterSeconds = failed ? attempt.RetryAfterSeconds : null;
	}
}
