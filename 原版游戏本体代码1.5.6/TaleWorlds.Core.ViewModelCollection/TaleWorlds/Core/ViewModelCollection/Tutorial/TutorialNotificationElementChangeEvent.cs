using TaleWorlds.Library.EventSystem;

namespace TaleWorlds.Core.ViewModelCollection.Tutorial;

public class TutorialNotificationElementChangeEvent : EventBase
{
	public string NewNotificationElementID { get; private set; }

	public TutorialNotificationElementChangeEvent(string newNotificationElementID)
	{
		NewNotificationElementID = newNotificationElementID ?? string.Empty;
	}
}
