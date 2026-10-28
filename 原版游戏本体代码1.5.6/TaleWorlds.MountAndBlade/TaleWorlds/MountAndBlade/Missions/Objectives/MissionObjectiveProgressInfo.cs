namespace TaleWorlds.MountAndBlade.Missions.Objectives;

public struct MissionObjectiveProgressInfo
{
	public int RequiredProgressAmount;

	public int CurrentProgressAmount;

	public bool HasProgress => RequiredProgressAmount > 0;
}
