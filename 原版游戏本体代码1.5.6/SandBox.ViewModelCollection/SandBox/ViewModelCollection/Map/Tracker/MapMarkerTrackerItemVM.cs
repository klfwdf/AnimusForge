using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map.Tracker;
using TaleWorlds.Library;

namespace SandBox.ViewModelCollection.Map.Tracker;

public class MapMarkerTrackerItemVM : MapTrackerItemVM<MapMarker>
{
	public MapMarkerTrackerItemVM(MapMarker marker)
		: base(marker)
	{
	}

	protected override void OnShowTooltip()
	{
		InformationManager.ShowTooltip(typeof(MapMarker), base.TrackedObject, true, false);
	}

	protected override bool IsVisibleOnMap()
	{
		return base.TrackedObject.IsVisibleOnMap;
	}

	protected override bool GetCanToggleTrack()
	{
		return true;
	}

	protected override string GetTrackerType()
	{
		return "Default";
	}

	protected override CampaignUIHelper.IssueQuestFlags GetRelatedQuests()
	{
		CampaignUIHelper.IssueQuestFlags result = CampaignUIHelper.IssueQuestFlags.None;
		QuestBase questBase = Campaign.Current.QuestManager.Quests.FirstOrDefault((QuestBase q) => q.StringId == base.TrackedObject.QuestId);
		if (questBase != null)
		{
			result = (questBase.IsSpecialQuest ? CampaignUIHelper.IssueQuestFlags.ActiveStoryQuest : CampaignUIHelper.IssueQuestFlags.ActiveIssue);
		}
		return result;
	}
}
