using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.KillFeed.Personal;

public class MPPersonalKillNotificationVM : ViewModel
{
	private MBBindingList<MPPersonalKillNotificationItemVM> _notificationList;

	[DataSourceProperty]
	public MBBindingList<MPPersonalKillNotificationItemVM> NotificationList
	{
		get
		{
			return _notificationList;
		}
		set
		{
			if (value != _notificationList)
			{
				_notificationList = value;
				OnPropertyChangedWithValue(value, "NotificationList");
			}
		}
	}

	public MPPersonalKillNotificationVM()
	{
		NotificationList = new MBBindingList<MPPersonalKillNotificationItemVM>();
	}

	public void OnGoldChange(int changeAmount, GoldGainFlags goldGainType)
	{
		NotificationList.Add(new MPPersonalKillNotificationItemVM(changeAmount, goldGainType, RemoveItem));
	}

	public void OnPersonalHit(int damageAmount, bool isFatal, bool isMountDamage, bool isFriendlyFire, bool isHeadshot, string killedAgentName)
	{
		NotificationList.Add(new MPPersonalKillNotificationItemVM(damageAmount, isFatal, isMountDamage, isFriendlyFire, isHeadshot, killedAgentName, RemoveItem));
	}

	public void OnPersonalAssist(string killedAgentName)
	{
		NotificationList.Add(new MPPersonalKillNotificationItemVM(killedAgentName, RemoveItem));
	}

	private void RemoveItem(MPPersonalKillNotificationItemVM item)
	{
		NotificationList.Remove(item);
	}
}
