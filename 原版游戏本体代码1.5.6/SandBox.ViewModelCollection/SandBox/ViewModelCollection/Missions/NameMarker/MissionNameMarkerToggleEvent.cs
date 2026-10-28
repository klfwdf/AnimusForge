using TaleWorlds.Library.EventSystem;

namespace SandBox.ViewModelCollection.Missions.NameMarker;

public class MissionNameMarkerToggleEvent : EventBase
{
	public bool NewState { get; private set; }

	public MissionNameMarkerToggleEvent(bool newState)
	{
		NewState = newState;
	}
}
