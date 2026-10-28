using TaleWorlds.Library.EventSystem;

namespace SandBox.Missions;

public class CheckpointLoadedMissionEvent : EventBase
{
	public readonly int LoadedCheckpointUniqueId;

	public CheckpointLoadedMissionEvent(int loadedCheckpointUniqueId)
	{
		LoadedCheckpointUniqueId = loadedCheckpointUniqueId;
	}
}
