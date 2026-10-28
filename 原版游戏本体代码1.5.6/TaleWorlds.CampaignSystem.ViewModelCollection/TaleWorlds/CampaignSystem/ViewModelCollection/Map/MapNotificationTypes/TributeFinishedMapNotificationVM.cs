using TaleWorlds.CampaignSystem.MapNotificationTypes;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes;

public class TributeFinishedMapNotificationVM : MapNotificationItemBaseVM
{
	public TributeFinishedMapNotificationVM(TributeFinishedMapNotification data)
		: base(data)
	{
		TributeFinishedMapNotificationVM tributeFinishedMapNotificationVM = this;
		base.NotificationIdentifier = "ransom";
		_onInspect = delegate
		{
			tributeFinishedMapNotificationVM.OnInspect(data.RelatedFaction);
		};
	}

	private void OnInspect(IFaction relatedFaction)
	{
		base.NavigationHandler?.OpenKingdom(relatedFaction);
		ExecuteRemove();
	}
}
