using TaleWorlds.CampaignSystem.MapNotificationTypes;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes;

public class BloodFeudStartedMapNotificationItemVM : MapNotificationItemBaseVM
{
	public BloodFeudStartedMapNotificationItemVM(BloodFeudStartedMapNotification data)
		: base(data)
	{
		base.NotificationIdentifier = "blood_feud_started";
		_onInspect = OnInspect;
	}

	private void OnInspect()
	{
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
	}
}
