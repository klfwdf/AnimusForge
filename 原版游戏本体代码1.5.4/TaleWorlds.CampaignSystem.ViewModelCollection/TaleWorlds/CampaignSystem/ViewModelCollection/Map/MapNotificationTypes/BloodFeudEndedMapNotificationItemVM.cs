using TaleWorlds.CampaignSystem.MapNotificationTypes;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes;

public class BloodFeudEndedMapNotificationItemVM : MapNotificationItemBaseVM
{
	public BloodFeudEndedMapNotificationItemVM(BloodFeudEndedMapNotification data)
		: base(data)
	{
		base.NotificationIdentifier = "blood_feud_ended";
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
