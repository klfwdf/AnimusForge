using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.HUD.KillFeed.Personal;

public class SPPersonalKillNotificationVM : ViewModel
{
	private MBBindingList<SPPersonalKillNotificationItemVM> _notificationList;

	[DataSourceProperty]
	public MBBindingList<SPPersonalKillNotificationItemVM> NotificationList
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

	public SPPersonalKillNotificationVM()
	{
		NotificationList = new MBBindingList<SPPersonalKillNotificationItemVM>();
	}

	public void OnPersonalKill(int damageAmount, bool isMountDamage, bool isFriendlyFire, bool isHeadshot, string killedAgentName, bool isUnconscious)
	{
		NotificationList.Add(new SPPersonalKillNotificationItemVM(damageAmount, isMountDamage, isFriendlyFire, isHeadshot, killedAgentName, isUnconscious, RemoveItem));
	}

	public void OnPersonalHit(int damageAmount, bool isMountDamage, bool isFriendlyFire, string killedAgentName)
	{
		NotificationList.Add(new SPPersonalKillNotificationItemVM(damageAmount, isMountDamage, isFriendlyFire, killedAgentName, RemoveItem));
	}

	public void OnPersonalMessage(string message)
	{
		NotificationList.Add(new SPPersonalKillNotificationItemVM(message, RemoveItem));
	}

	private void RemoveItem(SPPersonalKillNotificationItemVM item)
	{
		NotificationList.Remove(item);
	}
}
