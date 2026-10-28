using TaleWorlds.Library.EventSystem;

namespace TaleWorlds.MountAndBlade.View;

public class MissionPlayerToggledOrderViewEvent : EventBase
{
	public bool IsOrderEnabled { get; private set; }

	public MissionPlayerToggledOrderViewEvent(bool newIsEnabledState)
	{
		IsOrderEnabled = newIsEnabledState;
	}
}
