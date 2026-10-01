using System;using System.Collections.Generic;using System.Threading.Tasks;using static AnimusForge.MyBehavior;
namespace AnimusForge;
internal sealed class WeeklyGenerationAttemptOwner {private readonly WeeklyGenerationRules _rules;internal WeeklyGenerationAttemptOwner(WeeklyGenerationRules rules){_rules=rules;}
internal async Task<WeeklyReportRequestResult> GenerateWeeklyReportGroupWithRetriesAsync(WeeklyEventMaterialPreviewGroup group, int weekIndex, int startDay, int endDay, int maxAttempts, string systemPrompt, string userPrompt, string preview, string displayLabel, WeeklyGenerationAttemptPort port)
	{
		WeeklyReportRequestResult weeklyReportRequestResult = new WeeklyReportRequestResult();
		string text = systemPrompt;
		string text2 = userPrompt;
		string text3 = preview;
		weeklyReportRequestResult.PromptPreview = text3;
		string text4 = displayLabel;
		for (int i = 1; i <= Math.Max(1, maxAttempts); i++)
		{
			ApiCallResult apiCallResult = await port.CallGroup(text, text2);
			string text5 = apiCallResult.Success ? (apiCallResult.Content ?? "") : ("错误: " + (apiCallResult.ErrorMessage ?? "未知错误"));
			port.LogExchange(text4 + " [尝试 " + i + "/" + maxAttempts + "]", text3, text5);
			if (!apiCallResult.Success)
			{
				weeklyReportRequestResult.FailureReason = WeeklyGenerationRules.BuildWeeklyReportFailureReason(apiCallResult.ErrorMessage, parseFailed: false);
				weeklyReportRequestResult.AttemptsUsed = i;
				weeklyReportRequestResult.IsRateLimit = apiCallResult.IsRateLimit;
				weeklyReportRequestResult.IsRequestsPerMinuteLimit = apiCallResult.IsRequestsPerMinuteLimit;
				weeklyReportRequestResult.IsQuotaLimit = apiCallResult.IsQuotaLimit;
				weeklyReportRequestResult.RetryAfterSeconds = apiCallResult.RetryAfterSeconds;
			}
			else if (!_rules.TryParseWeeklyReportResponse(apiCallResult.Content, group, weekIndex, out var title, out var shortSummary, out var report, out var tagText))
			{
				weeklyReportRequestResult.FailureReason = WeeklyGenerationRules.BuildWeeklyReportFailureReason(apiCallResult.Content, parseFailed: true);
				weeklyReportRequestResult.AttemptsUsed = i;
			}
			else
			{
				weeklyReportRequestResult.Success = true;
				weeklyReportRequestResult.Title = title;
				weeklyReportRequestResult.ShortSummary = shortSummary;
				weeklyReportRequestResult.Report = report;
				weeklyReportRequestResult.TagText = tagText;
				weeklyReportRequestResult.AttemptsUsed = i;
				return weeklyReportRequestResult;
			}
			if (i < maxAttempts)
			{
				port.Log("EventWeeklyReport", text4 + " 第" + i + "次请求失败，准备自动重试。原因：" + weeklyReportRequestResult.FailureReason);
				int num = 1200;
				if (weeklyReportRequestResult.IsRateLimit)
				{
					num = Math.Max(num, 60000);
				}
				if (weeklyReportRequestResult.RetryAfterSeconds.HasValue)
				{
					num = Math.Max(num, weeklyReportRequestResult.RetryAfterSeconds.Value * 1000);
				}
				await port.Delay(num);
			}
		}
		return weeklyReportRequestResult;
	}
internal async Task<WeeklyReportBatchRequestResult> GenerateWeeklyReportBatchWithRetriesAsync(WeeklyReportBatchRequest batch, int maxAttempts, long runtimeGeneration, string displayLabel, WeeklyGenerationAttemptPort port)
	{
		WeeklyReportBatchRequestResult weeklyReportBatchRequestResult = new WeeklyReportBatchRequestResult();
		if (!WeeklyGenerationRules.IsWeeklyReportBatchPromptPrepared(batch))
		{
			weeklyReportBatchRequestResult.FailureReason = "Weekly batch prompt was not prepared on the main thread.";
			weeklyReportBatchRequestResult.MissingReportIds = WeeklyGenerationRules.BuildWeeklyBatchExpectedReportIds(batch);
			return weeklyReportBatchRequestResult;
		}
		string text = batch.SystemPrompt;
		string text2 = batch.UserPrompt;
		string text3 = batch.PromptPreview ?? "";
		string text4 = displayLabel;
		weeklyReportBatchRequestResult.PromptPreview = text3;
		for (int i = 1; i <= Math.Max(1, maxAttempts); i++)
		{
			if (runtimeGeneration > 0L && SaveRuntimeGuard.IsStale(runtimeGeneration, "weekly_batch_before_attempt"))
			{
				weeklyReportBatchRequestResult.Success = false;
				weeklyReportBatchRequestResult.FailureReason = SaveRuntimeGuard.BuildStaleRequestErrorText();
				weeklyReportBatchRequestResult.MissingReportIds = WeeklyGenerationRules.BuildWeeklyBatchExpectedReportIds(batch);
				return weeklyReportBatchRequestResult;
			}
			ApiCallResult apiCallResult = await port.CallBatch(text, text2, runtimeGeneration, i == 1);
			if (runtimeGeneration > 0L && SaveRuntimeGuard.IsStale(runtimeGeneration, "weekly_batch_after_attempt"))
			{
				weeklyReportBatchRequestResult.Success = false;
				weeklyReportBatchRequestResult.FailureReason = SaveRuntimeGuard.BuildStaleRequestErrorText();
				weeklyReportBatchRequestResult.MissingReportIds = WeeklyGenerationRules.BuildWeeklyBatchExpectedReportIds(batch);
				return weeklyReportBatchRequestResult;
			}
			string text5 = apiCallResult.Success ? (apiCallResult.Content ?? "") : (apiCallResult.ErrorMessage ?? "未知错误");
			weeklyReportBatchRequestResult.RawResponse = text5;
			port.LogExchange(text4 + " [灏濊瘯 " + i + "/" + maxAttempts + "]", text3, text5);
			weeklyReportBatchRequestResult.AttemptsUsed = i;
			WeeklyGenerationRules.CaptureWeeklyReportBatchAttemptFailureMetadata(weeklyReportBatchRequestResult, apiCallResult);
			if (!apiCallResult.Success)
			{
				weeklyReportBatchRequestResult.Success = false;
				weeklyReportBatchRequestResult.FailureReason = WeeklyGenerationRules.BuildWeeklyReportFailureReason(apiCallResult.ErrorMessage, parseFailed: false);
				weeklyReportBatchRequestResult.Blocks = new List<WeeklyReportBatchBlockResult>();
				weeklyReportBatchRequestResult.MissingReportIds = WeeklyGenerationRules.BuildWeeklyBatchExpectedReportIds(batch);
			}
			else if (!_rules.TryParseWeeklyBatchResponse(apiCallResult.Content, batch, out var blocks, out var missingReportIds, out var failureReason))
			{
				weeklyReportBatchRequestResult.Success = false;
				weeklyReportBatchRequestResult.FailureReason = LlmRetryPrompt.BuildFailureDetail(failureReason, apiCallResult.Content, apiCallResult.ResponseBody);
				weeklyReportBatchRequestResult.Blocks = blocks ?? new List<WeeklyReportBatchBlockResult>();
				weeklyReportBatchRequestResult.MissingReportIds = missingReportIds ?? WeeklyGenerationRules.BuildWeeklyBatchExpectedReportIds(batch);
			}
			else
			{
				weeklyReportBatchRequestResult.Blocks = blocks ?? new List<WeeklyReportBatchBlockResult>();
				weeklyReportBatchRequestResult.MissingReportIds = missingReportIds ?? new List<string>();
				if (weeklyReportBatchRequestResult.MissingReportIds.Count == 0)
				{
					weeklyReportBatchRequestResult.Success = true;
					weeklyReportBatchRequestResult.FailureReason = "";
					return weeklyReportBatchRequestResult;
				}
				weeklyReportBatchRequestResult.Success = false;
				weeklyReportBatchRequestResult.FailureReason = LlmRetryPrompt.BuildFailureDetail(failureReason, apiCallResult.Content, apiCallResult.ResponseBody);
			}
			if (i < maxAttempts)
			{
#if false
				port.Log("EventWeeklyReport", text4 + " 绗? + i + "娆℃壒閲忚姹傚け璐ワ紝鍑嗗鑷姩閲嶈瘯銆傚師鍥狅細" + weeklyReportBatchRequestResult.FailureReason);
#endif
				port.Log("EventWeeklyReport", text4 + " 第 " + i + " 次批量请求失败，准备自动重试。原因：" + weeklyReportBatchRequestResult.FailureReason);
				int num = 1200;
				if (weeklyReportBatchRequestResult.IsRateLimit)
				{
					num = Math.Max(num, 60000);
				}
				if (weeklyReportBatchRequestResult.RetryAfterSeconds.HasValue)
				{
					num = Math.Max(num, weeklyReportBatchRequestResult.RetryAfterSeconds.Value * 1000);
				}
				await port.Delay(num);
			}
		}
		return weeklyReportBatchRequestResult;
	}
}
internal sealed class WeeklyGenerationAttemptPort {internal Func<string,string,Task<ApiCallResult>> CallGroup;internal Func<string,string,long,bool,Task<ApiCallResult>> CallBatch;internal Action<string,string,string> LogExchange;internal Action<string,string> Log;internal Func<int,Task> Delay;}
