using TaleWorlds.CampaignSystem.MapNotificationTypes;
using TaleWorlds.CampaignSystem.SceneInformationPopupTypes;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes;

public class KingdomDestroyedNotificationItemVM : MapNotificationItemBaseVM
{
	public KingdomDestroyedNotificationItemVM(KingdomDestroyedMapNotification data)
		: base(data)
	{
		KingdomDestroyedNotificationItemVM kingdomDestroyedNotificationItemVM = this;
		base.NotificationIdentifier = "kingdomdestroyed";
		_onInspect = delegate
		{
			kingdomDestroyedNotificationItemVM.OnInspect(data);
		};
	}

	private void OnInspect(KingdomDestroyedMapNotification data)
	{
		MBInformationManager.ShowSceneNotification(new KingdomDestroyedSceneNotificationItem(data.DestroyedKingdom, data.CreationTime));
		ExecuteRemove();
	}
}
