using System;
using System.Threading;
using System.Threading.Tasks;

namespace AnimusForge;
internal sealed class RebellionNamingAttempt
{
	internal bool Success, IsRateLimit, IsRequestsPerMinuteLimit, IsQuotaLimit;
	internal int? RetryAfterSeconds;
	internal string Content, ErrorMessage, ResponseBody;
}

internal sealed class RebellionNamingOutcome
{
	internal bool Success, IsRateLimit, IsRequestsPerMinuteLimit, IsQuotaLimit;
	internal int? RetryAfterSeconds;
	internal int AttemptsUsed;
	internal string FormalName, ShortName, EncyclopediaText, FailureReason;
}

internal static class RebellionNamingOwner
{
	internal const int TimeoutMs = 60000, MaxAttempts = 3;
	// The attempt port contains detached network data only. Caller captures names before worker entry.
	internal static RebellionNamingOutcome Generate(Func<Task<RebellionNamingAttempt>> attempt, Func<string, bool> duplicate, Func<string, string, string, string> failureDetail, Action<int, int, RebellionNamingAttempt> logAttempt, Action<int, int, string> logRetry, Func<int> requestInterval, int maxAttempts = MaxAttempts, Action<int> wait = null, int timeoutMs = TimeoutMs)
	{
		int total = Math.Max(1, maxAttempts);
		var result = new RebellionNamingOutcome
		{
			FailureReason = ""
		};
		for (int i = 1; i <= total; i++)
		{
			RebellionNamingAttempt response;
			try
			{
				var task = attempt();
				response = Task.WhenAny(task, Task.Delay(timeoutMs)).GetAwaiter().GetResult() == task ? task.GetAwaiter().GetResult() : new RebellionNamingAttempt
				{
					ErrorMessage = "叛乱命名请求超时（60 秒）。"
				};
			}
			catch (Exception ex)
			{
				response = new RebellionNamingAttempt
				{
					ErrorMessage = ex.Message
				};
			}

			result.AttemptsUsed = i;
			result.IsRateLimit = response?.IsRateLimit ?? false;
			result.IsRequestsPerMinuteLimit = response?.IsRequestsPerMinuteLimit ?? false;
			result.IsQuotaLimit = response?.IsQuotaLimit ?? false;
			result.RetryAfterSeconds = response?.RetryAfterSeconds;
			logAttempt?.Invoke(i, total, response);
			if (response?.Success == true && RebellionNamingRules.TryParse(response.Content, out var formal, out var shortName, out var lore))
			{
				formal = RebellionNamingRules.NormalizeName(formal, 24);
				shortName = RebellionNamingRules.NormalizeName(shortName, 14);
				lore = RebellionNamingRules.NormalizeLore(lore);
				if (!string.IsNullOrWhiteSpace(formal) && !string.IsNullOrWhiteSpace(shortName) && !string.IsNullOrWhiteSpace(lore) && !duplicate(formal))
					return new RebellionNamingOutcome
					{
						Success = true,
						FormalName = formal,
						ShortName = shortName,
						EncyclopediaText = lore,
						AttemptsUsed = i
					};
				result.FailureReason = failureDetail("模型返回的国名为空、无效或与现有王国重名。", response.Content, response.ResponseBody);
			}
			else
				result.FailureReason = response?.Success == true ? failureDetail("模型返回无法按 [NAME]/[SHORT]/[LORE] 格式解析。", response.Content, response.ResponseBody) : (response?.ErrorMessage ?? "叛乱命名失败。");
			if (i < total)
			{
				logRetry?.Invoke(i, total, result.FailureReason);
				int delay = RebellionNamingRules.RetryDelay(response?.IsRateLimit == true, response?.IsRateLimit == true ? requestInterval() : 0, response?.RetryAfterSeconds);
				if (wait != null)
					wait(delay);
				else
					Thread.Sleep(delay);
			}
		}

		return result;
	}
}
