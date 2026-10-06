using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map.Tracker;
using TaleWorlds.Library;

namespace SandBox.ViewModelCollection.Map.Tracker;

public class MapMobilePartyTrackItemVM : MapTrackerItemVM<MobileParty>
{
	public MapMobilePartyTrackItemVM(MobileParty party)
		: base(party)
	{
	}

	protected override void OnShowTooltip()
	{
		InformationManager.ShowTooltip(typeof(MobileParty), base.TrackedObject, true, false);
	}

	protected override bool IsVisibleOnMap()
	{
		if (base.TrackedObject.AttachedTo == null)
		{
			return !base.TrackedObject.IsVisible;
		}
		return false;
	}

	protected override bool GetCanToggleTrack()
	{
		return true;
	}

	protected override string GetTrackerType()
	{
		return "MobileParty";
	}

	protected override CampaignUIHelper.IssueQuestFlags GetRelatedQuests()
	{
		return CampaignUIHelper.IssueQuestFlags.None;
	}
}
