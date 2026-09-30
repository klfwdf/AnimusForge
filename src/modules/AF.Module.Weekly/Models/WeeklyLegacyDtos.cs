using System.Collections.Generic;

namespace AnimusForge;

// CLR-nested DTO identities are retained for existing reflection consumers.
public partial class MyBehavior
{
	internal enum WeeklyReportOutputMode
	{
		FullReport,
		TitleShortTagsOnly
	}

	internal sealed class EventMaterialReference
	{
		public string MaterialType;

		public string Label;

		public string SnapshotText;

		public string HeroId;

		public string KingdomId;

		public string SettlementId;

		public bool RecentOnly;

		public string ActionKind;

		public string ActorHeroId;

		public string ActorClanId;

		public string ActorKingdomId;

		public string TargetHeroId;

		public string TargetClanId;

		public string TargetKingdomId;

		public string SettlementOwnerHeroId;

		public string SettlementOwnerClanId;

		public string SettlementOwnerKingdomId;

		public string PreviousSettlementOwnerHeroId;

		public string PreviousSettlementOwnerClanId;

		public string PreviousSettlementOwnerKingdomId;

		public string LocationText;

		public bool? Won;

		public List<string> RelatedHeroIds = new List<string>();

		public List<string> RelatedClanIds = new List<string>();

		public List<string> RelatedKingdomIds = new List<string>();

		public List<string> SourceStableKeys = new List<string>();

		public List<string> SourceActionKinds = new List<string>();

		public int SourceMaterialCount;

		public string ActionStableKey;

		public int? ActionDay;

		public int? ActionOrder;

		public int? ActionSequence;
	}

	internal sealed class WeeklyEventMaterialPreviewGroup
	{
		public string GroupKind;

		public string KingdomId;

		public string Title;

		public string Summary;

		public List<EventMaterialReference> Materials = new List<EventMaterialReference>();

		public List<EventMaterialReference> PromptMaterials = new List<EventMaterialReference>();

		public WeeklyReportOutputMode OutputMode = WeeklyReportOutputMode.FullReport;

		public bool IncludePreviousReportInPrompt = true;
	}

	internal sealed class WeeklyReportBatchRequest
	{
		public int WeekIndex;

		public int StartDay;

		public int EndDay;

		public WeeklyReportOutputMode OutputMode = WeeklyReportOutputMode.FullReport;

		public List<WeeklyEventMaterialPreviewGroup> Groups = new List<WeeklyEventMaterialPreviewGroup>();

		public string SystemPrompt = "";

		public string UserPrompt = "";

		public string PromptPreview = "";

		public string DisplayLabel = "";
	}
}
