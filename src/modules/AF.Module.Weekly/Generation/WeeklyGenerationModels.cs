using System.Collections.Generic;namespace AnimusForge;public partial class MyBehavior {
internal sealed class WeeklyReportRequestResult
	{
		public bool Success;

		public string Title;

		public string ShortSummary;

		public string Report;

		public string TagText;

		public string PromptPreview;

		public string FailureReason;

		public int AttemptsUsed;

		public bool IsRateLimit;

		public bool IsRequestsPerMinuteLimit;

		public bool IsQuotaLimit;

		public int? RetryAfterSeconds;
	}
internal sealed class WeeklyReportBatchBlockResult
	{
		public string ReportId;

		public string Mode;

		public string Kind;

		public string KingdomId;

		public string Title;

		public string ShortSummary;

		public string Report;

		public string TagText;

		public bool Parsed;

		public string FailureReason;
	}
internal sealed class WeeklyReportBatchRequestResult
	{
		public bool Success = false;

		public string PromptPreview = "";

		public string RawResponse = "";

		public string FailureReason = "";

		public List<WeeklyReportBatchBlockResult> Blocks = new List<WeeklyReportBatchBlockResult>();

		public List<string> MissingReportIds = new List<string>();

		public int AttemptsUsed = 0;

		public bool IsRateLimit;

		public bool IsRequestsPerMinuteLimit;

		public bool IsQuotaLimit;

		public int? RetryAfterSeconds;
	}
}
