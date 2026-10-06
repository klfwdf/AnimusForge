using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.HUD.KillFeed.General;

public class SPGeneralKillNotificationVM : ViewModel
{
	private MBBindingList<SPGeneralKillNotificationItemVM> _notificationList;

	[DataSourceProperty]
	public MBBindingList<SPGeneralKillNotificationItemVM> NotificationList
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

	public SPGeneralKillNotificationVM()
	{
		NotificationList = new MBBindingList<SPGeneralKillNotificationItemVM>();
	}

	public void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, bool isHeadshot, bool isSuicide, bool isDrowning)
	{
		NotificationList.Add(new SPGeneralKillNotificationItemVM(affectedAgent, affectorAgent, isHeadshot, isSuicide, isDrowning, RemoveItem));
	}

	private void RemoveItem(SPGeneralKillNotificationItemVM item)
	{
		NotificationList.Remove(item);
	}
}
