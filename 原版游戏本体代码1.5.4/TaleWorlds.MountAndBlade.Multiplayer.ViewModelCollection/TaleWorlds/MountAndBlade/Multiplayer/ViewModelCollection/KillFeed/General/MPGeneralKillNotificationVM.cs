using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.KillFeed.General;

public class MPGeneralKillNotificationVM : ViewModel
{
	private MBBindingList<MPGeneralKillNotificationItemVM> _notificationList;

	[DataSourceProperty]
	public MBBindingList<MPGeneralKillNotificationItemVM> NotificationList
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

	public MPGeneralKillNotificationVM()
	{
		NotificationList = new MBBindingList<MPGeneralKillNotificationItemVM>();
	}

	public void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, Agent assistedAgent, WeaponClass killWeaponClass = WeaponClass.Undefined)
	{
		NotificationList.Add(new MPGeneralKillNotificationItemVM(affectedAgent, affectorAgent, assistedAgent, RemoveItem, killWeaponClass));
	}

	private void RemoveItem(MPGeneralKillNotificationItemVM item)
	{
		NotificationList.Remove(item);
	}
}
