namespace TaleWorlds.CampaignSystem;

public interface IMapTrackersHandler
{
	void OnTrackerAdded(ITrackableCampaignObject trackable);

	void OnTrackerRemoved(ITrackableCampaignObject trackable);

	void ResetTrackers();
}
