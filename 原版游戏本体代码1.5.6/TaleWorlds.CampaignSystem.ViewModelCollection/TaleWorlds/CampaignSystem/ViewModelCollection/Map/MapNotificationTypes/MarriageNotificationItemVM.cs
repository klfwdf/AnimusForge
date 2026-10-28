using TaleWorlds.CampaignSystem.MapNotificationTypes;
using TaleWorlds.CampaignSystem.SceneInformationPopupTypes;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes;

public class MarriageNotificationItemVM : MapNotificationItemBaseVM
{
	public Hero Suitor { get; private set; }

	public Hero Maiden { get; private set; }

	public MarriageNotificationItemVM(MarriageMapNotification data)
		: base(data)
	{
		MarriageNotificationItemVM marriageNotificationItemVM = this;
		Suitor = data.Suitor;
		Maiden = data.Maiden;
		base.NotificationIdentifier = "marriage";
		_onInspect = delegate
		{
			MBInformationManager.ShowSceneNotification(new MarriageSceneNotificationItem(data.Suitor, data.Maiden, data.CreationTime));
			marriageNotificationItemVM.ExecuteRemove();
		};
	}
}
