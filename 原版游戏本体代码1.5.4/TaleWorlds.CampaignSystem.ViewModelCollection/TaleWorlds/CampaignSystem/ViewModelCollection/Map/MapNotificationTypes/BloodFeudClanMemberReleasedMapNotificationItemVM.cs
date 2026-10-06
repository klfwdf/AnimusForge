using TaleWorlds.CampaignSystem.MapNotificationTypes;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes;

public class BloodFeudClanMemberReleasedMapNotificationItemVM : MapNotificationItemBaseVM
{
	public BloodFeudClanMemberReleasedMapNotificationItemVM(BloodFeudClanMemberExecuteCancelledMapNotification data)
		: base(data)
	{
		base.NotificationIdentifier = "blood_feud_clan_member_released";
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
