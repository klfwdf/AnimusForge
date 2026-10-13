using System;
using System.Text;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace AnimusForge;

public static class LlmRetryPrompt
{
	private static SynchronizationContext _mainThreadContext;
	private static int _mainThreadId;

	public static void CaptureMainThreadContext()
	{
		SynchronizationContext context = SynchronizationContext.Current;
		if (context == null)
		{
			return;
		}
		_mainThreadContext = context;
		_mainThreadId = Thread.CurrentThread.ManagedThreadId;
	}

	public static bool IsRetryableLlmError(string error)
	{
		string text = (error ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		if (text.StartsWith(LlmSourceTexts.Get("RetryableApiRequestPrefix"), StringComparison.Ordinal)
			|| text.StartsWith(LlmSourceTexts.Get("RetryableProgramErrorPrefix"), StringComparison.Ordinal)
			|| text.StartsWith(LlmSourceTexts.Get("RetryableApiResponsePrefix"), StringComparison.Ordinal)
			|| text.StartsWith(LlmSourceTexts.Get("RetryableErrorPrefix"), StringComparison.Ordinal)
			|| text.StartsWith("timeout_", StringComparison.OrdinalIgnoreCase)
			|| text.StartsWith("http_", StringComparison.OrdinalIgnoreCase)
			|| text.EndsWith("_config_invalid", StringComparison.OrdinalIgnoreCase)
			|| text.Equals("empty_content", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		return text.IndexOf("TaskCanceledException", StringComparison.OrdinalIgnoreCase) >= 0
			|| text.IndexOf("HttpRequestException", StringComparison.OrdinalIgnoreCase) >= 0
			|| text.IndexOf("JsonReaderException", StringComparison.OrdinalIgnoreCase) >= 0
			|| text.IndexOf("JsonSerializationException", StringComparison.OrdinalIgnoreCase) >= 0
			|| text.IndexOf("operation was canceled", StringComparison.OrdinalIgnoreCase) >= 0
			|| text.IndexOf("A task was canceled", StringComparison.OrdinalIgnoreCase) >= 0;
	}

	public static string BuildRetryDescription(string stageName, string error)
	{
		string stage = RetryStageDisplay(stageName);
		// The inquiry only owns retry/abort; full diagnostics are reported separately.
		var text = new TextObject("{=AF_UI_Retry_Description}{STAGE} failed. The full error was shown in the lower-left message feed and written to the log.\n\nRetry now?");
		text.SetTextVariable("STAGE", stage);
		return text.ToString();
	}

	private static string RetryStageDisplay(string stageName)
	{
		string stage = (stageName ?? "").Trim();
		if (stage.Length == 0 || stage == LlmSourceTexts.Get("RetryStageRequest")) return new TextObject("{=AF_UI_Retry_StageRequest}LLM request").ToString();
		if (stage == LlmSourceTexts.Get("RetryStagePostprocess")) return new TextObject("{=AF_UI_Retry_StagePostprocess}Action postprocessing").ToString();
		if (stage == LlmSourceTexts.Get("RetryStageAuxiliary")) return new TextObject("{=AF_UI_Retry_StageAuxiliary}Auxiliary conversation").ToString();
		if (stage == LlmSourceTexts.Get("RetryStagePreprocess")) return new TextObject("{=AF_UI_Retry_StagePreprocess}Preprocessing rule selection").ToString();
		if (stage == LlmSourceTexts.Get("RetryStageReply")) return new TextObject("{=AF_UI_Retry_StageReply}Reply generation").ToString();
		if (stage == LlmSourceTexts.Get("RetryStageCourierReply")) return new TextObject("{=AF_UI_Retry_StageCourierReply}Courier reply generation").ToString();
		if (stage == LlmSourceTexts.Get("RetryStageCourierLetter")) return new TextObject("{=AF_UI_Retry_StageCourierLetter}Courier letter generation").ToString();
		return stage;
	}

	private static string RetryFailureDisplay(string stageName, string error)
	{
		var text = new TextObject("{=AF_UI_Retry_FailureDetail}{STAGE} failed:\n\n{DETAIL}");
		text.SetTextVariable("STAGE", RetryStageDisplay(stageName));
		text.SetTextVariable("DETAIL", "AFRETRYDETAILEND");
		return text.ToString().Replace("AFRETRYDETAILEND", NormalizeFullText(ProjectKnownErrorForUi(error), new TextObject("{=AF_UI_Retry_UnknownError}Unknown error").ToString()));
	}

	public static string BuildFailureDetail(string reason, string modelReply, string rawResponse = null)
	{
		StringBuilder stringBuilder = new StringBuilder();
		string fullReason = NormalizeFullText(reason, LlmSourceTexts.Get("RetryUnknownError"));
		stringBuilder.Append(fullReason);
		string fullModelReply = NormalizeFullText(modelReply, "");
		string fullRawResponse = NormalizeFullText(rawResponse, "");
		stringBuilder.AppendLine();
		stringBuilder.AppendLine();
		stringBuilder.AppendLine(LlmSourceTexts.Get("LegacyErrorDetailMarker"));
		stringBuilder.Append(string.IsNullOrWhiteSpace(fullModelReply) ? LlmSourceTexts.Get("RetryNoModelReply") : fullModelReply);
		if (!string.IsNullOrWhiteSpace(fullRawResponse) && !string.Equals(fullRawResponse, fullModelReply, StringComparison.Ordinal))
		{
			stringBuilder.AppendLine();
			stringBuilder.AppendLine();
			stringBuilder.AppendLine(LlmSourceTexts.Get("RetryRawResponseMarker"));
			stringBuilder.Append(fullRawResponse);
		}
		string detail = stringBuilder.ToString().TrimEnd();
		RetryErrorProjection.Remember(detail, fullReason, fullModelReply, fullRawResponse);
		return detail;
	}

    internal static string ProjectKnownErrorForUi(string value)
    {
        return RetryErrorProjection.Project(value);
    }

	public static void ShowFailurePopup(string title, string message)
	{
		void Show()
		{
			NonBlockingErrorReport.Show(
				string.IsNullOrWhiteSpace(title) ? new TextObject("{=AF_UI_Retry_RequestFailed}AnimusForge request failed").ToString() : title.Trim(),
				NormalizeFullText(message, new TextObject("{=AF_UI_Retry_UnknownError}Unknown error").ToString()), true,
				ProjectKnownErrorForUi(message));
		}
		PostToMainThread(Show);
	}

    // Failure-only weak metadata preserves field boundaries without parsing model/provider bodies.
    // A copied or unrelated diagnostic has no metadata and only exact known reasons are projected.
    private static class RetryErrorProjection
    {
        private sealed class Parts
        {
            internal readonly string Reason, Reply, Response;
            internal Parts(string reason, string reply, string response) { Reason = reason; Reply = reply; Response = response; }
        }
        private static readonly ConditionalWeakTable<string, Parts> Details = new ConditionalWeakTable<string, Parts>();

        internal static void Remember(string detail, string reason, string reply, string response)
            => Details.Add(detail, new Parts(reason, reply, response));

        internal static string Project(string value)
        {
            if (value == null) return null;
            if (!Details.TryGetValue(value, out Parts parts)) return Reason(value);
            StringBuilder text = new StringBuilder();
            text.Append(Reason(parts.Reason));
            text.AppendLine(); text.AppendLine();
            text.AppendLine(new TextObject("{=AF_UI_Retry_FullModelReply}Model reply (complete)").ToString());
            text.Append(parts.Reply.Length == 0 ? new TextObject("{=AF_UI_Retry_NoModelReply}(No model reply was received or could be parsed)").ToString() : parts.Reply);
            if (parts.Response.Length != 0 && !string.Equals(parts.Response, parts.Reply, StringComparison.Ordinal))
            {
                text.AppendLine(); text.AppendLine();
                text.AppendLine(new TextObject("{=AF_UI_Retry_FullRawResponse}Raw API response (complete)").ToString());
                text.Append(parts.Response);
            }
            return text.ToString().TrimEnd();
        }

        private static string Reason(string value)
        {
            string reason = (value ?? "").Trim();
            if (reason == SaveRuntimeGuard.BuildStaleRequestErrorText()) return new TextObject("{=AF_UI_Retry_StaleRequest}The request was invalidated by loading a save.").ToString();
            if (reason == LlmSourceTexts.Get("RetryCheckMcm")) return new TextObject("{=AF_UI_Retry_CheckMcm}Please check MCM settings.").ToString();
            if (reason == LlmSourceTexts.Get("RetryUnknownError")) return new TextObject("{=AF_UI_Retry_UnknownError}Unknown error").ToString();
            if (reason == LlmSourceTexts.Get("LegacyErrorPrefixMissingApiKey")) return new TextObject("{=AF_UI_Retry_MissingApiKey}(Error: API Key is not configured)").ToString();
            if (reason == LlmSourceTexts.Get("LegacyErrorPrefixMissingModel")) return new TextObject("{=AF_UI_Retry_MissingModel}(Error: Model name is not configured)").ToString();
            if (reason == LlmSourceTexts.Get("RetryActionTagsMissing")) return new TextObject("{=AF_UI_Retry_ActionTagsMissing}(API response format error) Action postprocessing returned no parseable [ACTION:...] tags.").ToString();
            string prefix = LlmSourceTexts.Get("RetryPreprocessFormatPrefix");
            string suffix = LlmSourceTexts.Get("RetryPreprocessFormatSuffix");
            if (reason.StartsWith(prefix, StringComparison.Ordinal) && reason.EndsWith(suffix, StringComparison.Ordinal) && reason.Length >= prefix.Length + suffix.Length)
            {
                var text = new TextObject("{=AF_UI_Retry_PreprocessFormatInvalid}(API response format error) Preprocessing rule selection returned an invalid format: {REASON}. Output exactly one JSON object containing the rule_codes string array and the mentioned_entities.entities string array.");
                text.SetTextVariable("REASON", "AFRETRYREASONEND");
                return text.ToString().Replace("AFRETRYREASONEND", reason.Substring(prefix.Length, reason.Length - prefix.Length - suffix.Length));
            }
            return value;
        }
    }

	// Inputs are detached strings. Native localization runs only inside the existing dispatcher.
	internal static void ShowLocalizedFailurePopup(string titleTemplate, string diagnostic,
		string titleSource = null, string reasonTemplate = null, string modelReply = null,
		string rawResponse = null, string reasonCode = null)
	{
		void Show()
		{
			string title = ResolveFailureTemplate(titleTemplate, "SOURCE", titleSource);
			string display = reasonTemplate == null ? ProjectKnownErrorForUi(diagnostic) : FailureDetailForDisplay(reasonTemplate, modelReply, rawResponse, reasonCode);
			NonBlockingErrorReport.Show(
				string.IsNullOrWhiteSpace(title) ? new TextObject("{=AF_UI_Retry_RequestFailed}AnimusForge request failed").ToString() : title,
				NormalizeFullText(diagnostic, new TextObject("{=AF_UI_Retry_UnknownError}Unknown error").ToString()), true, display);
		}
		PostToMainThread(Show);
	}

	private static string ResolveFailureTemplate(string template, string variableName, string value)
	{
		string raw = (template ?? "").Trim();
		if (!raw.StartsWith("{=", StringComparison.Ordinal)) return raw;
		var text = new TextObject(raw);
		if (value == null) return text.ToString();
		text.SetTextVariable(variableName, "AFRETRYTEMPLATEVALUEEND");
		return text.ToString().Replace("AFRETRYTEMPLATEVALUEEND", value);
	}

	private static string FailureDetailForDisplay(string reasonTemplate, string modelReply, string rawResponse, string reasonCode)
	{
		string reason = ResolveFailureTemplate(reasonTemplate, "CODE", reasonCode);
		StringBuilder text = new StringBuilder();
		text.Append(NormalizeFullText(reason, new TextObject("{=AF_UI_Retry_UnknownError}Unknown error").ToString()));
		// Null means no reply section in the original diagnostic; empty means no reply was received.
		if (modelReply == null && rawResponse == null) return text.ToString();
		string reply = NormalizeFullText(modelReply, "");
		string response = NormalizeFullText(rawResponse, "");
		text.AppendLine();
		text.AppendLine();
		text.AppendLine(new TextObject("{=AF_UI_Retry_FullModelReply}Model reply (complete)").ToString());
		text.Append(string.IsNullOrWhiteSpace(reply) ? new TextObject("{=AF_UI_Retry_NoModelReply}(No model reply was received or could be parsed)").ToString() : reply);
		if (!string.IsNullOrWhiteSpace(response) && !string.Equals(response, reply, StringComparison.Ordinal))
		{
			text.AppendLine();
			text.AppendLine();
			text.AppendLine(new TextObject("{=AF_UI_Retry_FullRawResponse}Raw API response (complete)").ToString());
			text.Append(response);
		}
		return text.ToString().TrimEnd();
	}

	public static Task<bool> PromptRetryAsync(string stageName, string error)
	{
		if (!IsRetryableLlmError(error))
		{
			return Task.FromResult(false);
		}
		TaskCompletionSource<bool> completion = new TaskCompletionSource<bool>();
		ShowRetryPrompt(stageName, error, completion);
		return completion.Task;
	}

	public static bool PromptRetryBlocking(string stageName, string error)
	{
		if (!IsRetryableLlmError(error))
		{
			return false;
		}
		if (_mainThreadId != 0 && Thread.CurrentThread.ManagedThreadId == _mainThreadId)
		{
			NonBlockingErrorReport.Show(new TextObject("{=AF_UI_Retry_RequestFailed}AnimusForge request failed").ToString(), error, true, RetryFailureDisplay(stageName, error));
			return false;
		}
		using ManualResetEventSlim waitHandle = new ManualResetEventSlim(false);
		bool retry = false;
		TaskCompletionSource<bool> completion = new TaskCompletionSource<bool>();
		completion.Task.ContinueWith(delegate(Task<bool> task)
		{
			try
			{
				retry = task.Status == TaskStatus.RanToCompletion && task.Result;
			}
			catch
			{
				retry = false;
			}
			waitHandle.Set();
		});
		ShowRetryPrompt(stageName, error, completion);
		waitHandle.Wait();
		return retry;
	}

	private static void ShowRetryPrompt(string stageName, string error, TaskCompletionSource<bool> completion)
	{
		void Show()
		{
			try
			{
				// Preserve retry decisions while reporting full API diagnostics outside the inquiry.
				NonBlockingErrorReport.Show(
					new TextObject("{=AF_UI_Retry_RequestFailed}AnimusForge request failed").ToString(),
					error, true, RetryFailureDisplay(stageName, error));
				InformationManager.ShowInquiry(new InquiryData(
					new TextObject("{=AF_UI_Retry_RequestFailed}AnimusForge request failed").ToString(),
					BuildRetryDescription(stageName, error),
					isAffirmativeOptionShown: true,
					isNegativeOptionShown: true,
					new TextObject("{=AF_UI_Retry_Retry}Retry").ToString(),
					new TextObject("{=AF_UI_Retry_Abort}Abort").ToString(),
					delegate
					{
						completion.TrySetResult(true);
					},
					delegate
					{
						completion.TrySetResult(false);
					}),
					pauseGameActiveState: true,
					prioritize: true);
			}
			catch
			{
				completion.TrySetResult(false);
			}
		}

		PostToMainThread(Show);
	}

	private static string NormalizeFullText(string value, string fallback)
	{
		string text = (value ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Trim();
		return string.IsNullOrWhiteSpace(text) ? (fallback ?? "") : text;
	}

	public static void PostToMainThread(Action action)
	{
		if (action == null)
		{
			return;
		}
		SynchronizationContext context = _mainThreadContext;
		if (context != null && (_mainThreadId == 0 || Thread.CurrentThread.ManagedThreadId != _mainThreadId))
		{
			try
			{
				context.Post(_ => action(), null);
				return;
			}
			catch
			{
			}
		}
		action();
	}
}
