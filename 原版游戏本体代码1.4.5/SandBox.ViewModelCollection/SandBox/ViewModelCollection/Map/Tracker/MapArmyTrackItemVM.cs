using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map.Tracker;
using TaleWorlds.Library;

namespace SandBox.ViewModelCollection.Map.Tracker;

public class MapArmyTrackItemVM : MapTrackerItemVM<Army>
{
	public MapArmyTrackItemVM(Army trackableObject)
		: base(trackableObject)
	{
	}

	protected override void OnShowTooltip()
	{
		InformationManager.ShowTooltip(typeof(Army), base.TrackedObject, true, false);
	}

	protected override bool IsVisibleOnMap()
	{
		MobileParty leaderParty = base.TrackedObject.LeaderParty;
		if (leaderParty == null)
		{
			return false;
		}
		return !leaderParty.IsVisible;
	}

	protected override bool GetCanToggleTrack()
	{
		return true;
	}

	protected override string GetTrackerType()
	{
		return "Army";
	}

	protected override CampaignUIHelper.IssueQuestFlags GetRelatedQuests()
	{
		return CampaignUIHelper.IssueQuestFlags.None;
	}
}
