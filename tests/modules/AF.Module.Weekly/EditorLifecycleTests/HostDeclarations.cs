using System.Collections.Generic;
namespace AnimusForge;
// Exact host nested declarations, isolated from the game host for this offline harness.
public partial class MyBehavior
{
	internal sealed class EventRecordEntry
	{
		public string EventId;

		public int WeekIndex;

		public string EventKind;

		public string ScopeKingdomId;

		public string Title;

		public string ShortSummary;

		public string Summary;

		public string TagText;

		public string PromptText;

		public int CreatedDay;

		public string CreatedDate;

        public List<string> BulletinKingdomIds = new List<string>();

		public List<EventMaterialReference> Materials = new List<EventMaterialReference>();
	}

	public sealed class WeeklyReportBrowserEntryData
	{
		public string EventId;

		public int WeekIndex;

		public string Title;

		public string BodyText;

		public string CreatedDate;

		public int CreatedDay;

		public string TagText;

		public bool HasFullReport;
	}

	public sealed class WeeklyReportBrowserCountryData
	{
		public string CountryId;

		public string DisplayName;

		public bool IsWorld;

		public List<WeeklyReportBrowserEntryData> Reports = new List<WeeklyReportBrowserEntryData>();
	}

	internal sealed class DevWeeklyReportBatchPreviewEntry
	{
		public string PreviewKey = "";

		public string BatchLabel = "";

		public int WeekIndex;

		public int StartDay;

		public int EndDay;

		public List<string> ReportIds = new List<string>();

		public string PromptPreview = "";

		public string ResponsePreview = "";

		public bool Success;

		public string FailureReason = "";

		public int AttemptsUsed;
	}
}
